using PlatzDaemon.Models;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlatzDaemon.Pages;

public class IndexModel : PageModel
{
    private readonly LogStore _logStore;
    private readonly AppStateService _appState;
    private readonly BookingSchedulerService _scheduler;
    private readonly IConfigStore _configStore;
    private readonly WhatsAppAutomationService _whatsApp;

    public IReadOnlyList<LogEntry> Logs { get; set; } = Array.Empty<LogEntry>();
    public string StatusText { get; set; } = "IDLE";
    public string StatusCss { get; set; } = "idle";
    public string? LastResult { get; set; }
    public string NextRunDisplay { get; set; } = "--:--:--";
    public string NextRunIso { get; set; } = "";
    public bool IsCountdownDisabled { get; set; }
    public bool WhatsAppConnected { get; set; }
    public bool HasSavedSession { get; set; }

    // Config summary
    public string GameType { get; set; } = "";
    public string PreferredPeriod { get; set; } = "";
    public string BookingDay { get; set; } = "";
    public string TimeSlots { get; set; } = "";
    public string Courts { get; set; } = "";

    public IndexModel(LogStore logStore, AppStateService appState, BookingSchedulerService scheduler, IConfigStore configStore, WhatsAppAutomationService whatsApp)
    {
        _logStore = logStore;
        _appState = appState;
        _scheduler = scheduler;
        _configStore = configStore;
        _whatsApp = whatsApp;
    }

    public void OnGet()
    {
        Logs = _logStore.GetAll();
        var state = _appState.State;

        // Config summary
        var cfg = _configStore.Get();
        GameType = cfg.GameType;
        PreferredPeriod = cfg.PreferredPeriod;
        BookingDay = cfg.BookingDay;
        TimeSlots = cfg.PreferredTimeSlots.Count > 0 ? string.Join(", ", cfg.PreferredTimeSlots) : "---";
        Courts = cfg.PreferredCourts.Count > 0 ? string.Join(", ", cfg.PreferredCourts) : "---";

        StatusText = state.Status.ToString().ToUpper();
        StatusCss = state.Status.ToString().ToLower();
        LastResult = state.LastResult;
        WhatsAppConnected = state.WhatsAppConnected;
        HasSavedSession = _whatsApp.HasSavedSessionData;

        if (state.NextRunTime.HasValue)
        {
            NextRunDisplay = state.NextRunTime.Value.ToString("HH:mm:ss");
            NextRunIso = state.NextRunTime.Value.ToString("o");
        }
        else
        {
            NextRunDisplay = "Desactivado";
            NextRunIso = "";
            IsCountdownDisabled = true;
        }
    }

    public async Task<IActionResult> OnPostManualRunAsync()
    {
        _ = Task.Run(async () => await _scheduler.TriggerManualRunAsync());
        return RedirectToPage();
    }

    public IActionResult OnPostClearLogs()
    {
        _logStore.Clear();
        return RedirectToPage();
    }
}
