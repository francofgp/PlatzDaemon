using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PlatzDaemon.Services;

public class SleepPreventionService : BackgroundService
{
    private readonly BookingSchedulerService _scheduler;
    private readonly LogStore _log;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SleepPreventionService>? _logger;

    private readonly double _hoursAhead;
    private readonly int _pollIntervalMinutes;
    private readonly int _inhibitProcessTimeoutSeconds;

    private bool _isInhibiting;
    private Process? _inhibitProcess;
    private bool _startupLogged;

    [DllImport("kernel32.dll", SetLastError = false)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;

    public SleepPreventionService(
        BookingSchedulerService scheduler,
        LogStore log,
        IConfiguration configuration,
        ILogger<SleepPreventionService>? logger = null)
    {
        _scheduler = scheduler;
        _log = log;
        _configuration = configuration;
        _logger = logger;

        _hoursAhead = _configuration.GetValue("SleepPrevention:HoursAhead", 24.0);
        _pollIntervalMinutes = _configuration.GetValue("SleepPrevention:PollIntervalMinutes", 5);
        _inhibitProcessTimeoutSeconds = _configuration.GetValue("SleepPrevention:InhibitProcessTimeoutSeconds", 3600);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var nextRun = _scheduler.GetNextScheduledRun();
                    var now = _scheduler.GetNowArgentina();

                    if (!_startupLogged)
                    {
                        LogStartupMechanism();
                        _startupLogged = true;
                    }

                    var shouldInhibit = nextRun.HasValue &&
                        (nextRun.Value - now) <= TimeSpan.FromHours(_hoursAhead) &&
                        (nextRun.Value - now).TotalSeconds > 0;

                    if (shouldInhibit)
                        await ActivateInhibitionAsync();
                    else
                        await ReleaseInhibitionAsync();
                }
                catch (Exception ex)
                {
                    await _log.LogErrorAsync($"Prevencion de suspension: error en ciclo. {ex.Message}");
                    _logger?.LogWarning(ex, "SleepPreventionService cycle error");
                }

                await Task.Delay(TimeSpan.FromMinutes(_pollIntervalMinutes), stoppingToken);
            }
        }
        finally
        {
            try
            {
                await ReleaseInhibitionAsync();
            }
            catch (Exception ex)
            {
                _ = _log.LogWarningAsync("Prevencion de suspension: error al liberar al cerrar. " + ex.Message);
                _logger?.LogWarning(ex, "SleepPreventionService release on shutdown failed");
            }
        }
    }

    private void LogStartupMechanism()
    {
        var msg = $"Prevencion de suspension: disponible. Se activara cuando el proximo disparo este en las proximas {_hoursAhead:F0} h.";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _ = _log.LogInfoAsync(msg + " (Windows API)");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _ = _log.LogInfoAsync(msg + " (Linux: systemd-inhibit)");
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            _ = _log.LogInfoAsync(msg + " (macOS: caffeinate)");
        else
            _ = _log.LogWarningAsync("Prevencion de suspension: OS no soportado, desactivada.");
    }

    private async Task ActivateInhibitionAsync()
    {
        if (_isInhibiting)
        {
            EnsureInhibitProcessRunning();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var result = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            if (result == 0)
            {
                await _log.LogWarningAsync("Prevencion de suspension (Windows): no se pudo activar. La PC podria suspenderse antes del proximo disparo.");
                return;
            }
            _isInhibiting = true;
            await _log.LogInfoAsync($"Prevencion de suspension: activada (proximo disparo en las proximas {_hoursAhead:F0} h). La PC no se suspendera.");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (StartLinuxInhibitProcess())
            {
                _isInhibiting = true;
                _ = _log.LogInfoAsync($"Prevencion de suspension: activada (proximo disparo en las proximas {_hoursAhead:F0} h). La PC no se suspendera.");
            }
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (StartMacOsInhibitProcess())
            {
                _isInhibiting = true;
                _ = _log.LogInfoAsync($"Prevencion de suspension: activada (proximo disparo en las proximas {_hoursAhead:F0} h). La PC no se suspendera.");
            }
            return;
        }
    }

    private void EnsureInhibitProcessRunning()
    {
        if (_inhibitProcess == null || _inhibitProcess.HasExited)
        {
            _isInhibiting = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                StartLinuxInhibitProcess();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                StartMacOsInhibitProcess();
            if (_inhibitProcess != null && !_inhibitProcess.HasExited)
                _isInhibiting = true;
        }
    }

    private bool StartLinuxInhibitProcess()
    {
        try
        {
            KillInhibitProcessIfNeeded();
            var psi = new ProcessStartInfo
            {
                FileName = "systemd-inhibit",
                ArgumentList = { "--what=sleep", "--who=PlatzDaemon", "--why=Scheduled task soon", "sleep", _inhibitProcessTimeoutSeconds.ToString() },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _inhibitProcess = Process.Start(psi);
            if (_inhibitProcess == null)
            {
                _ = _log.LogWarningAsync("Prevencion de suspension (Linux): systemd-inhibit no disponible o fallo. La PC podria suspenderse.");
                return false;
            }
            _inhibitProcess.WaitForExit(500);
            if (_inhibitProcess.HasExited)
            {
                _ = _log.LogWarningAsync("Prevencion de suspension (Linux): systemd-inhibit no disponible o termino de inmediato. Instale systemd o ejecute en un entorno con systemd. La PC podria suspenderse.");
                _inhibitProcess.Dispose();
                _inhibitProcess = null;
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _ = _log.LogWarningAsync($"Prevencion de suspension (Linux): systemd-inhibit no disponible o fallo. {ex.Message} La PC podria suspenderse.");
            _logger?.LogWarning(ex, "Linux systemd-inhibit failed");
            return false;
        }
    }

    private bool StartMacOsInhibitProcess()
    {
        try
        {
            KillInhibitProcessIfNeeded();
            var psi = new ProcessStartInfo
            {
                FileName = "caffeinate",
                ArgumentList = { "-t", _inhibitProcessTimeoutSeconds.ToString() },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _inhibitProcess = Process.Start(psi);
            if (_inhibitProcess == null)
            {
                _ = _log.LogWarningAsync("Prevencion de suspension (macOS): caffeinate no disponible. La PC podria suspenderse.");
                return false;
            }
            _inhibitProcess.WaitForExit(500);
            if (_inhibitProcess.HasExited)
            {
                _ = _log.LogWarningAsync("Prevencion de suspension (macOS): caffeinate no disponible o termino de inmediato. La PC podria suspenderse.");
                _inhibitProcess.Dispose();
                _inhibitProcess = null;
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _ = _log.LogWarningAsync($"Prevencion de suspension (macOS): caffeinate no disponible. {ex.Message} La PC podria suspenderse.");
            _logger?.LogWarning(ex, "macOS caffeinate failed");
            return false;
        }
    }

    private async Task ReleaseInhibitionAsync()
    {
        if (!_isInhibiting)
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                SetThreadExecutionState(ES_CONTINUOUS);
            }
            catch (Exception ex)
            {
                await _log.LogWarningAsync("Prevencion de suspension (Windows): error al liberar. " + ex.Message);
                _logger?.LogWarning(ex, "SetThreadExecutionState(ES_CONTINUOUS) failed");
            }
            _isInhibiting = false;
            await _log.LogInfoAsync("Prevencion de suspension: desactivada (no hay disparo en la ventana). La PC puede suspenderse.");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                KillInhibitProcessIfNeeded();
            }
            catch (Exception ex)
            {
                await _log.LogWarningAsync("Prevencion de suspension: error al liberar (proceso hijo).");
                _logger?.LogWarning(ex, "Failed to kill inhibit process");
            }
            _inhibitProcess = null;
            _isInhibiting = false;
            await _log.LogInfoAsync("Prevencion de suspension: desactivada (no hay disparo en la ventana). La PC puede suspenderse.");
        }
    }

    private void KillInhibitProcessIfNeeded()
    {
        if (_inhibitProcess == null)
            return;
        if (!_inhibitProcess.HasExited)
        {
            try
            {
                _inhibitProcess.Kill(entireProcessTree: false);
            }
            catch (InvalidOperationException) { /* already exited */ }
        }
        _inhibitProcess.Dispose();
        _inhibitProcess = null;
    }
}
