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
        // Si el campo llega vacío, usar el valor ya guardado (re-guardar sin cambiar el número)
        if (string.IsNullOrWhiteSpace(BotPhoneNumber))
        {
            var cfgGet = _configStore.Get();
            BotPhoneNumber = cfgGet.BotPhoneNumber ?? "";
            ModelState.Remove(nameof(BotPhoneNumber)); // evita el mensaje por defecto "The field is required"
        }

        // Validar: solo dígitos, entre 10 y 15 (formato internacional)
        var digitsOnly = new string((BotPhoneNumber ?? "").Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
        {
            ModelState.AddModelError(nameof(BotPhoneNumber),
                digitsOnly.Length == 0
                    ? "El numero del bot es obligatorio."
                    : "El numero del bot debe tener entre 10 y 15 digitos (solo numeros). Ej: 5493534407576 o 93534407576.");
            return Page();
        }

        // Load full config, update only system fields, save back (store digits only)
        var cfg = _configStore.Get();
        cfg.Enabled = Enabled;
        cfg.BotPhoneNumber = digitsOnly;
        cfg.Dni = Dni;
        cfg.TriggerTime = TriggerTime;
        cfg.CompetitiveMode = CompetitiveMode;

        await _configStore.SaveAsync(cfg);
        _scheduler.NotifyConfigChanged();
        SavedOk = true;
        return RedirectToPage();
    }
}
