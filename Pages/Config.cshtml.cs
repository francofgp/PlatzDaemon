using PlatzDaemon.Models;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlatzDaemon.Pages;

public class ConfigModel : PageModel
{
    private readonly IConfigStore _configStore;

    [BindProperty]
    public string PreferredPeriod { get; set; } = "Noche";

    [BindProperty]
    public string GameType { get; set; } = "Doble";

    [BindProperty]
    public string BookingDay { get; set; } = "Hoy";

    [BindProperty]
    public List<string> PreferredTimeSlots { get; set; } = new();

    [BindProperty]
    public List<string> PreferredCourts { get; set; } = new();

    [TempData]
    public bool SavedOk { get; set; }

    public ConfigModel(IConfigStore configStore)
    {
        _configStore = configStore;
    }

    public void OnGet()
    {
        var cfg = _configStore.Get();
        PreferredPeriod = cfg.PreferredPeriod;
        GameType = cfg.GameType;
        BookingDay = cfg.BookingDay;
        PreferredTimeSlots = cfg.PreferredTimeSlots;
        PreferredCourts = cfg.PreferredCourts;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Clean up empty entries from lists
        PreferredTimeSlots = PreferredTimeSlots?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? new List<string>();

        PreferredCourts = PreferredCourts?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? new List<string>();

        // Load full config, update only booking fields, save back
        var cfg = _configStore.Get();
        cfg.PreferredPeriod = PreferredPeriod;
        cfg.GameType = GameType;
        cfg.BookingDay = BookingDay;
        cfg.PreferredTimeSlots = PreferredTimeSlots;
        cfg.PreferredCourts = PreferredCourts;

        await _configStore.SaveAsync(cfg);
        SavedOk = true;
        return RedirectToPage();
    }
}
