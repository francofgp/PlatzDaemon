using PlatzDaemon.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PlatzDaemon.Pages;

public class SessionModel : PageModel
{
    private readonly WhatsAppAutomationService _whatsApp;
    private readonly AppStateService _appState;
    private readonly IWebHostEnvironment _env;

    public bool IsConnected { get; set; }
    public bool IsBrowserOpen { get; set; }
    public bool HasSavedSession { get; set; }
    public string? Message { get; set; }
    public string MessageCss { get; set; } = "";
    public string BrowserDataPath { get; set; } = "";
    public bool NeedsChromiumDownload { get; set; }

    public SessionModel(WhatsAppAutomationService whatsApp, AppStateService appState, IWebHostEnvironment env)
    {
        _whatsApp = whatsApp;
        _appState = appState;
        _env = env;
    }

    public void OnGet()
    {
        LoadState();
    }

    public async Task<IActionResult> OnPostConnectAsync()
    {
        var success = await _whatsApp.OpenSessionForQrScanAsync();
        if (success)
        {
            Message = "Navegador abierto. Escanea el codigo QR en la ventana de Chromium.";
            MessageCss = "";
        }
        else
        {
            Message = "Error al abrir el navegador. Revisa los logs.";
            MessageCss = "terminal-alert-error";
        }
        LoadState();
        return Page();
    }

    public async Task<IActionResult> OnPostCheckSessionAsync()
    {
        var connected = await _whatsApp.CheckSessionAsync();
        if (connected)
        {
            Message = "Sesion verificada! WhatsApp Web esta conectado.";
            MessageCss = "terminal-alert-primary";
        }
        else
        {
            Message = "No se detecto sesion activa. Asegurate de haber escaneado el QR.";
            MessageCss = "terminal-alert-error";
        }
        LoadState();
        return Page();
    }

    public async Task<IActionResult> OnPostDisconnectAsync()
    {
        await _whatsApp.CloseSessionAsync();
        Message = "Sesion cerrada.";
        MessageCss = "";
        LoadState();
        return Page();
    }

    private void LoadState()
    {
        IsConnected = _appState.State.WhatsAppConnected;
        IsBrowserOpen = _whatsApp.IsSessionActive;
        HasSavedSession = _whatsApp.HasSavedSessionData;
        BrowserDataPath = Path.Combine(_env.ContentRootPath, "Data", "browser-data");
        NeedsChromiumDownload = !WhatsAppAutomationService.IsBrowserInstalled();
    }
}
