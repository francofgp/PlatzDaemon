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

    public IReadOnlyList<LogEntry> Logs { get; set; } = Array.Empty<LogEntry>();
    public string StatusText { get; set; } = "IDLE";
    public string StatusCss { get; set; } = "idle";
    public string? LastResult { get; set; }
    public string NextRunDisplay { get; set; } = "--:--:--";
    public string NextRunIso { get; set; } = "";
    public bool WhatsAppConnected { get; set; }

    public IndexModel(LogStore logStore, AppStateService appState, BookingSchedulerService scheduler)
    {
        _logStore = logStore;
        _appState = appState;
        _scheduler = scheduler;
    }

    public void OnGet()
    {
        Logs = _logStore.GetAll();
        var state = _appState.State;

        StatusText = state.Status.ToString().ToUpper();
        StatusCss = state.Status.ToString().ToLower();
        LastResult = state.LastResult;
        WhatsAppConnected = state.WhatsAppConnected;

        if (state.NextRunTime.HasValue)
        {
            NextRunDisplay = state.NextRunTime.Value.ToString("HH:mm:ss");
            NextRunIso = state.NextRunTime.Value.ToString("o");
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
