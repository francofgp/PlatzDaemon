using PlatzDaemon.Models;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlatzDaemon.Pages;

public class SistemaModel : PageModel
{
    private readonly IConfigStore _configStore;
    private readonly BookingSchedulerService _scheduler;

    [BindProperty]
    public bool Enabled { get; set; }

    [BindProperty]
    public string BotPhoneNumber { get; set; } = "";

    [BindProperty]
    public string Dni { get; set; } = "";

    [BindProperty]
    public string TriggerTime { get; set; } = "08:00";

    [BindProperty]
    public bool CompetitiveMode { get; set; }

    [TempData]
    public bool SavedOk { get; set; }

    public SistemaModel(IConfigStore configStore, BookingSchedulerService scheduler)
    {
        _configStore = configStore;
        _scheduler = scheduler;
    }

    public void OnGet()
    {
        var cfg = _configStore.Get();
        Enabled = cfg.Enabled;
        BotPhoneNumber = cfg.BotPhoneNumber;
        Dni = cfg.Dni;
        TriggerTime = cfg.TriggerTime;
        CompetitiveMode = cfg.CompetitiveMode;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Load full config, update only system fields, save back
        var cfg = _configStore.Get();
        cfg.Enabled = Enabled;
        cfg.BotPhoneNumber = BotPhoneNumber;
        cfg.Dni = Dni;
        cfg.TriggerTime = TriggerTime;
        cfg.CompetitiveMode = CompetitiveMode;

        await _configStore.SaveAsync(cfg);
        _scheduler.NotifyConfigChanged();
        SavedOk = true;
        return RedirectToPage();
    }
}
