using PlatzDaemon.Models;

namespace PlatzDaemon.Services;

public class BookingSchedulerService : BackgroundService
{
    private readonly IConfigStore _configStore;
    private readonly WhatsAppAutomationService _whatsApp;
    private readonly LogStore _log;
    private readonly AppStateService _appState;
    private readonly NotificationService _notification;

    // Argentina timezone (UTC-3)
    private static readonly TimeZoneInfo ArgentinaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");

    public BookingSchedulerService(
        IConfigStore configStore,
        WhatsAppAutomationService whatsApp,
        LogStore log,
        AppStateService appState,
        NotificationService notification)
    {
        _configStore = configStore;
        _whatsApp = whatsApp;
        _log = log;
        _appState = appState;
        _notification = notification;
    }

    /// <summary>
    /// Manually trigger a booking (from the UI "Ejecutar ahora" button).
    /// </summary>
    public async Task TriggerManualRunAsync()
    {
        await _log.LogInfoAsync("=== EJECUCION MANUAL INICIADA ===");
        await _whatsApp.ExecuteBookingAsync(competitivePreArm: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _log.LogInfoAsync("Scheduler iniciado. Esperando configuracion...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var config = _configStore.Get();

                if (!config.Enabled)
                {
                    _appState.SetNextRun(null);
                    await _appState.UpdateStatusAsync(DaemonStatus.Idle, "Deshabilitado");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                if (!TimeSpan.TryParse(config.TriggerTime, out var triggerTimeOfDay))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ArgentinaTimeZone);
                var todayTrigger = CalculateNextTrigger(now, triggerTimeOfDay);

                _appState.SetNextRun(todayTrigger);
                await _appState.UpdateStatusAsync(DaemonStatus.Waiting,
                    $"Proximo disparo: {todayTrigger:dd/MM HH:mm}");

                // Calculate wait times
                var preArmTime = todayTrigger.AddSeconds(-20);
                var waitUntilPreArm = preArmTime - now;
                var waitUntilTrigger = todayTrigger - now;

                if (waitUntilTrigger.TotalSeconds > 0)
                {
                    await _log.LogInfoAsync($"Esperando hasta {todayTrigger:HH:mm:ss} (Argentina). Faltan {FormatTimeSpan(waitUntilTrigger)}");

                    if (config.CompetitiveMode && waitUntilPreArm.TotalSeconds > 0)
                    {
                        // Wait until pre-arm time
                        await _log.LogInfoAsync($"Modo competitivo activo. Pre-carga a las {preArmTime:HH:mm:ss}");
                        await Task.Delay(waitUntilPreArm, stoppingToken);

                        // Pre-arm: open browser, navigate to chat, type message
                        await _log.LogInfoAsync(">>> PRE-ARMANDO mensaje (modo competitivo)...");
                        await _whatsApp.ExecuteBookingAsync(competitivePreArm: true);

                        // Wait for exact trigger time
                        var remainingWait = todayTrigger - TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ArgentinaTimeZone);
                        if (remainingWait.TotalMilliseconds > 0)
                        {
                            await _log.LogInfoAsync($"Mensaje listo. Esperando {remainingWait.TotalSeconds:F1}s para enviar...");
                            await PrecisionWaitAsync(todayTrigger, stoppingToken);
                        }

                        // FIRE!
                        await _log.LogInfoAsync(">>> DISPARANDO! Enviando mensaje...");
                        await _whatsApp.SendPreArmedMessageAsync();

                        // Continue with rest of booking flow
                        // The main flow continues from where we left off
                        await Task.Delay(2000, stoppingToken);
                        await _whatsApp.ExecuteBookingAsync(competitivePreArm: false);
                    }
                    else
                    {
                        // Normal mode: just wait and execute
                        await Task.Delay(waitUntilTrigger, stoppingToken);
                        await _log.LogInfoAsync(">>> Hora de disparo alcanzada!");
                        await _whatsApp.ExecuteBookingAsync(competitivePreArm: false);
                    }
                }

                // After execution, immediately calculate next trigger for tomorrow
                var nextNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ArgentinaTimeZone);
                var tomorrowTrigger = nextNow.Date.AddDays(1) + triggerTimeOfDay;
                _appState.SetNextRun(tomorrowTrigger);
                await _appState.UpdateStatusAsync(DaemonStatus.Waiting,
                    $"Proximo disparo: {tomorrowTrigger:dd/MM HH:mm}");
                await _log.LogInfoAsync($"Ejecucion completada. Proximo disparo: {tomorrowTrigger:dd/MM HH:mm}");

                // Brief wait before re-entering the loop to avoid tight iteration
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                await _log.LogErrorAsync($"Error en scheduler: {ex.Message}");
                await _appState.UpdateStatusAsync(DaemonStatus.Error, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task PrecisionWaitAsync(DateTime targetTime, CancellationToken ct)
    {
        // Coarse wait (sleep most of the time)
        while (!ct.IsCancellationRequested)
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ArgentinaTimeZone);
            var remaining = targetTime - now;

            if (remaining.TotalMilliseconds <= 50)
                break;

            if (remaining.TotalMilliseconds > 1000)
                await Task.Delay(500, ct);
            else if (remaining.TotalMilliseconds > 100)
                await Task.Delay(10, ct);
            else
                await Task.Yield(); // Busy-wait for last 50ms for precision
        }
    }

    internal static DateTime CalculateNextTrigger(DateTime now, TimeSpan triggerTimeOfDay)
    {
        var todayTrigger = now.Date + triggerTimeOfDay;

        if (now > todayTrigger.AddMinutes(5))
            todayTrigger = todayTrigger.AddDays(1);

        return todayTrigger;
    }

    internal static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
