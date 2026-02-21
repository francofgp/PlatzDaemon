using PlatzDaemon.Models;
using Microsoft.Playwright;
using System.Diagnostics;

namespace PlatzDaemon.Services;

public class WhatsAppAutomationService : IAsyncDisposable
{
    private readonly IWebHostEnvironment _env;
    private readonly LogStore _log;
    private readonly ConfigStore _configStore;
    private readonly NotificationService _notification;
    private readonly AppStateService _appState;

    private IPlaywright? _playwright;
    private IBrowserContext? _browserContext;
    private readonly string _browserDataPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsSessionActive => _browserContext != null;

    public WhatsAppAutomationService(
        IWebHostEnvironment env,
        LogStore log,
        ConfigStore configStore,
        NotificationService notification,
        AppStateService appState)
    {
        _env = env;
        _log = log;
        _configStore = configStore;
        _notification = notification;
        _appState = appState;
        _browserDataPath = Path.Combine(env.ContentRootPath, "Data", "browser-data");
        Directory.CreateDirectory(_browserDataPath);
    }

    /// <summary>
    /// Opens a visible browser window for WhatsApp Web QR scanning.
    /// </summary>
    public async Task<bool> OpenSessionForQrScanAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await CloseSessionInternalAsync();

            await _log.LogInfoAsync("Abriendo navegador para escanear QR de WhatsApp Web...");

            _playwright = await Playwright.CreateAsync();
            _browserContext = await _playwright.Chromium.LaunchPersistentContextAsync(
                _browserDataPath,
                new BrowserTypeLaunchPersistentContextOptions
                {
                    Headless = false,
                    Channel = "chromium",
                    Args = new[] { "--disable-blink-features=AutomationControlled" },
                    ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
                });

            var page = _browserContext.Pages.FirstOrDefault() ?? await _browserContext.NewPageAsync();
            await page.GotoAsync("https://web.whatsapp.com", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            await _log.LogInfoAsync("Navegador abierto. Escanea el codigo QR en la ventana del navegador.");
            return true;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync($"Error abriendo navegador: {ex.Message}");
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Checks if WhatsApp Web is logged in by looking for the chat list.
    /// </summary>
    public async Task<bool> CheckSessionAsync()
    {
        if (_browserContext == null) return false;

        try
        {
            var page = _browserContext.Pages.FirstOrDefault();
            if (page == null) return false;

            // Multiple selectors to detect logged-in state across WhatsApp Web versions
            var loggedInSelectors = new[]
            {
                "[data-testid='chat-list']",
                "[data-testid='chatlist']",
                "div#pane-side",
                "[data-testid='chat-list-search']",
                "[data-testid='search']",
                "div[contenteditable='true'][data-tab='3']",
                "[data-testid='chatlist-header']",
                "[data-testid='conversation-compose-box-input']",
                "div[role='navigation']",
                "header[data-testid='chatlist-header-bar']"
            };

            foreach (var selector in loggedInSelectors)
            {
                try
                {
                    var element = await page.QuerySelectorAsync(selector);
                    if (element != null)
                    {
                        await _log.LogInfoAsync($"Sesion detectada via selector: {selector}");
                        _appState.SetWhatsAppConnected(true);
                        return true;
                    }
                }
                catch { }
            }

            try
            {
                var hasChats = await page.EvaluateAsync<bool>(@"() => {
                    const qrCanvas = document.querySelector('canvas[aria-label]');
                    if (qrCanvas) return false;
                    const sidePane = document.getElementById('pane-side');
                    if (sidePane) return true;
                    const listItems = document.querySelectorAll('[role=""listitem""]');
                    if (listItems.length > 0) return true;
                    const title = document.title;
                    if (title && (title.includes('WhatsApp') || /\(\d+\)/.test(title))) return true;
                    return false;
                }");

                if (hasChats)
                {
                    await _log.LogInfoAsync("Sesion detectada via evaluacion JavaScript del DOM.");
                    _appState.SetWhatsAppConnected(true);
                    return true;
                }
            }
            catch { }

            await _log.LogWarningAsync("No se pudo detectar sesion activa con ninguno de los selectores conocidos.");
            return false;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync($"Error verificando sesion: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Closes the browser session.
    /// </summary>
    public async Task CloseSessionAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await CloseSessionInternalAsync();
            await _log.LogInfoAsync("Sesion de navegador cerrada.");
            _appState.SetWhatsAppConnected(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Executes the full booking flow with proper wait-for-response logic.
    /// </summary>
    public async Task<bool> ExecuteBookingAsync(bool competitivePreArm = false)
    {
        var config = _configStore.Get();

        await _lock.WaitAsync();
        try
        {
            await _appState.UpdateStatusAsync(DaemonStatus.Running);
            await _log.LogInfoAsync("=== INICIO DE RESERVA ===");

            // Ensure browser is ready
            if (_browserContext == null)
            {
                await _log.LogInfoAsync("Iniciando navegador en modo headless...");
                _playwright = await Playwright.CreateAsync();
                _browserContext = await _playwright.Chromium.LaunchPersistentContextAsync(
                    _browserDataPath,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = true,
                        Channel = "chromium",
                        Args = new[] { "--disable-blink-features=AutomationControlled" },
                        ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
                    });
            }

            var page = _browserContext.Pages.FirstOrDefault() ?? await _browserContext.NewPageAsync();

            // Navigate to WhatsApp Web
            await _log.LogInfoAsync("Navegando a WhatsApp Web...");
            await page.GotoAsync("https://web.whatsapp.com", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 30000
            });

            // Wait for session to load
            await _log.LogInfoAsync("Esperando que cargue la sesion...");
            try
            {
                await page.WaitForSelectorAsync(
                    "[data-testid='chat-list'], [data-testid='chatlist'], div#pane-side, [data-testid='chatlist-header-bar']",
                    new PageWaitForSelectorOptions { Timeout = 30000 });
            }
            catch
            {
                var loggedIn = await page.EvaluateAsync<bool>(@"() => {
                    const sidePane = document.getElementById('pane-side');
                    if (sidePane) return true;
                    const listItems = document.querySelectorAll('[role=""listitem""]');
                    return listItems.length > 0;
                }");

                if (!loggedIn)
                {
                    await _log.LogErrorAsync("No se pudo cargar la sesion de WhatsApp. Escanea el QR primero.");
                    await _appState.UpdateStatusAsync(DaemonStatus.Error, "Sesion no disponible");
                    return false;
                }
            }

            await _log.LogSuccessAsync("Sesion de WhatsApp activa.");

            // Open chat with bot
            await _log.LogInfoAsync($"Buscando chat del bot ({config.BotPhoneNumber})...");
            if (!await OpenBotChatAsync(page, config.BotPhoneNumber))
            {
                await _log.LogErrorAsync("No se pudo abrir el chat del bot.");
                await _appState.UpdateStatusAsync(DaemonStatus.Error, "Chat del bot no encontrado");
                return false;
            }

            // Competitive mode: type but don't send yet
            if (competitivePreArm)
            {
                await _log.LogInfoAsync("Modo competitivo: mensaje pre-cargado, esperando hora exacta...");
                await TypeMessageAsync(page, "turno");
                return true;
            }

            // ====== NORMAL FLOW WITH RETRY ON COURT REJECTION ======
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    await _log.LogWarningAsync($"=== REINTENTO {attempt}/{maxAttempts} ===");
                    await _log.LogInfoAsync("Reiniciando flujo de reserva...");
                    await Task.Delay(2000);
                }

                await ScrollToBottomAsync(page);

                // STEP 0: Clear any pending conversation (send "Salir" to reset bot)
                await ClearPendingConversationAsync(page);
                await Task.Delay(1000);

                // STEP 1: Send "turno" and WAIT for bot to respond
                var countBeforeTurno = await GetMessageCountAsync(page, logDebug: attempt == 1);
                await _log.LogInfoAsync($"Mensajes actuales en el chat: {countBeforeTurno}");
                await _log.LogInfoAsync("Enviando 'turno' al bot...");
                await SendMessageAsync(page, "turno");

                // Soft wait: try to detect bot response, but continue even if detection fails
                await _log.LogInfoAsync("Esperando respuesta del bot...");
                var botResponded = await WaitForBotResponseAsync(page, countBeforeTurno, 15000);
                if (botResponded)
                {
                    await _log.LogSuccessAsync("Bot respondio.");
                }
                else
                {
                    await _log.LogWarningAsync("No se detecto nueva respuesta por conteo de mensajes. Continuando de todas formas...");
                    await Task.Delay(5000); // Extra safety delay
                    await ScrollToBottomAsync(page);
                }

                // STEP 2: Check if bot requests DNI (only in last 3 messages)
                if (await CheckForDniRequestInRecentAsync(page))
                {
                    if (!string.IsNullOrEmpty(config.Dni))
                    {
                        var countBeforeDni = await GetMessageCountAsync(page);
                        await _log.LogInfoAsync($"Bot solicito DNI. Enviando: {config.Dni}...");
                        await SendMessageAsync(page, config.Dni);

                        await _log.LogInfoAsync("Esperando respuesta del bot despues del DNI...");
                        if (!await WaitForBotResponseAsync(page, countBeforeDni, 20000))
                        {
                            await _log.LogWarningAsync("Bot no respondio despues del DNI, continuando...");
                        }
                        else
                        {
                            await _log.LogSuccessAsync("DNI aceptado por el bot.");
                        }

                        // STEP 2b: Bot asks "¿A nombre de ...?" - click "Sí" to confirm
                        if (await CheckForNameConfirmationInRecentAsync(page))
                        {
                            await _log.LogInfoAsync("Bot solicita confirmacion de nombre. Clickeando 'Si'...");
                            var countBeforeNameConfirm = await GetMessageCountAsync(page);
                            if (await ClickButtonInRecentMessagesAsync(page, "Si", 10000) ||
                                await ClickButtonInRecentMessagesAsync(page, "Sí", 3000))
                            {
                                await _log.LogSuccessAsync("Nombre confirmado.");
                                if (!await WaitForBotResponseAsync(page, countBeforeNameConfirm, 20000))
                                {
                                    await _log.LogWarningAsync("Bot no respondio despues de confirmar nombre, continuando...");
                                    await Task.Delay(3000);
                                    await ScrollToBottomAsync(page);
                                }
                            }
                            else
                            {
                                await _log.LogWarningAsync("No se pudo clickear 'Si' para confirmar nombre, continuando...");
                            }
                        }
                    }
                    else
                    {
                        await _log.LogWarningAsync("Bot pidio DNI pero no esta configurado.");
                    }
                }

                // STEP 3: Click date button (e.g. "Hoy") - ONLY in recent messages
                await _log.LogInfoAsync($"Buscando boton '{config.BookingDay}' en mensajes recientes...");
                var countBeforeDate = await GetMessageCountAsync(page);
                if (!await ClickButtonInRecentMessagesAsync(page, config.BookingDay, 30000, 5))
                {
                    await _log.LogErrorAsync($"No se encontro el boton '{config.BookingDay}' en mensajes recientes.");
                    await LogVisibleOptionsAsync(page, "fecha");
                    await _appState.UpdateStatusAsync(DaemonStatus.Error, $"Boton '{config.BookingDay}' no encontrado");
                    return false;
                }
                await _log.LogSuccessAsync($"Seleccionado: {config.BookingDay}");

                // Wait for bot to respond with period options
                await _log.LogInfoAsync("Esperando opciones de periodo...");
                if (!await WaitForBotResponseAsync(page, countBeforeDate, 20000))
                {
                    await _log.LogWarningAsync("No se detecto respuesta del bot despues de la fecha, continuando...");
                    await Task.Delay(3000);
                    await ScrollToBottomAsync(page);
                }

                // CHECK: Did the bot say we already have a reservation or no slots?
                var blocker = await CheckForBookingBlockerAsync(page);
                if (blocker == "already_booked")
                {
                    await _log.LogWarningAsync("=== YA TENES UN TURNO RESERVADO ===");
                    await _log.LogInfoAsync("El bot indico que ya existe una reserva activa.");
                    await _log.LogInfoAsync("Si necesitas cancelarla, hacelo manualmente en WhatsApp.");
                    await _appState.UpdateStatusAsync(DaemonStatus.Completed, "Ya tiene turno reservado");
                    _notification.NotifySuccess("Platz Daemon", "Ya tenes un turno reservado. Cancelalo primero si queres sacar otro.");
                    return false;
                }
                else if (blocker == "no_slots")
                {
                    await _log.LogWarningAsync("=== NO HAY TURNOS DISPONIBLES ===");
                    await _log.LogInfoAsync("El bot indico que hoy ya no hay turnos disponibles.");
                    await _log.LogInfoAsync("Espera hasta mañana para reservar un turno.");
                    await _appState.UpdateStatusAsync(DaemonStatus.Completed, "No hay turnos disponibles hoy");
                    _notification.NotifyError("Platz Daemon", "Hoy ya no hay turnos disponibles. Esperá hasta mañana.");
                    return false;
                }

                // STEP 4: Click period button - ONLY in recent messages
                var periodText = config.PreferredPeriod switch
                {
                    "Mañana" => "Turno mañana",
                    "Tarde" => "Turnos tarde",
                    "Noche" => "Turnos noche",
                    _ => "Turnos noche"
                };
                await _log.LogInfoAsync($"Seleccionando periodo: {periodText}...");
                var countBeforePeriod = await GetMessageCountAsync(page);
                if (!await ClickButtonInRecentMessagesAsync(page, periodText, 30000, 5))
                {
                    // Try alternative text
                    var altText = config.PreferredPeriod switch
                    {
                        "Mañana" => "mañana",
                        "Tarde" => "tarde",
                        "Noche" => "noche",
                        _ => "noche"
                    };
                    if (!await ClickButtonInRecentMessagesAsync(page, altText, 5000))
                    {
                        await _log.LogErrorAsync($"No se encontro el boton del periodo '{periodText}'");
                        await LogVisibleOptionsAsync(page, "periodo");
                        await _appState.UpdateStatusAsync(DaemonStatus.Error, "Periodo no encontrado");
                        return false;
                    }
                }
                await _log.LogSuccessAsync($"Periodo seleccionado: {config.PreferredPeriod}");

                // Wait for bot to respond with time slots
                await _log.LogInfoAsync("Esperando horarios disponibles...");
                if (!await WaitForBotResponseAsync(page, countBeforePeriod, 20000))
                {
                    await _log.LogWarningAsync("No se detecto respuesta del bot despues del periodo, continuando...");
                }

                // STEP 5: Select time slot - ONLY in recent messages
                var selectedTime = await SelectTimeSlotAsync(page, config.PreferredTimeSlots);
                if (selectedTime == null)
                {
                    await _log.LogErrorAsync("NINGUNO de los horarios preferidos esta disponible.");
                    await _appState.UpdateStatusAsync(DaemonStatus.Error, "Horarios no disponibles");
                    _notification.NotifyError("Platz Daemon", "No hay horarios disponibles en tu lista de prioridad.");
                    return false;
                }
                await _log.LogSuccessAsync($"Horario seleccionado: {selectedTime}");

                // Wait for bot to respond with court options
                var countBeforeCourt = await GetMessageCountAsync(page);
                await _log.LogInfoAsync("Esperando opciones de canchas...");
                if (!await WaitForBotResponseAsync(page, countBeforeCourt, 20000))
                {
                    await _log.LogWarningAsync("No se detecto respuesta del bot despues del horario, continuando...");
                }

                // STEP 6: Select court - ONLY in recent messages
                var selectedCourt = await SelectCourtAsync(page, config.PreferredCourts);
                if (selectedCourt == null)
                {
                    await _log.LogErrorAsync("No se pudo seleccionar ninguna cancha.");
                    await _appState.UpdateStatusAsync(DaemonStatus.Error, "Canchas no disponibles");
                    return false;
                }
                await _log.LogSuccessAsync($"Cancha seleccionada: {selectedCourt}");

                // Wait for bot to respond with game type options
                var countBeforeGameType = await GetMessageCountAsync(page);
                await _log.LogInfoAsync("Esperando tipo de juego...");
                if (!await WaitForBotResponseAsync(page, countBeforeGameType, 20000))
                {
                    await _log.LogWarningAsync("No se detecto respuesta del bot despues de la cancha, continuando...");
                }

                // STEP 7: Select game type - ONLY in recent messages
                await _log.LogInfoAsync($"Seleccionando tipo de juego: {config.GameType}...");
                var countBeforeConfirm = await GetMessageCountAsync(page);
                if (!await ClickButtonInRecentMessagesAsync(page, config.GameType, 30000, 5))
                {
                    await _log.LogErrorAsync($"No se encontro el boton '{config.GameType}'");
                    await LogVisibleOptionsAsync(page, "tipo de juego");
                    await _appState.UpdateStatusAsync(DaemonStatus.Error, "Tipo de juego no encontrado");
                    return false;
                }
                await _log.LogSuccessAsync($"Tipo de juego: {config.GameType}");

                // Wait for confirmation prompt
                await _log.LogInfoAsync("Esperando confirmacion del bot...");
                if (!await WaitForBotResponseAsync(page, countBeforeConfirm, 20000))
                {
                    await _log.LogWarningAsync("No se detecto pregunta de confirmacion, intentando de todas formas...");
                }

                // STEP 8: Confirm booking - ONLY in recent messages
                await _log.LogInfoAsync("Confirmando reserva...");
                if (!await ClickButtonInRecentMessagesAsync(page, "Si", 10000))
                {
                    if (!await ClickButtonInRecentMessagesAsync(page, "Sí", 3000) &&
                        !await ClickButtonInRecentMessagesAsync(page, "Confirmar", 3000))
                    {
                        await _log.LogErrorAsync("No se pudo confirmar la reserva.");
                        await _appState.UpdateStatusAsync(DaemonStatus.Error, "Confirmacion fallida");
                        return false;
                    }
                }

                // Wait for final confirmation message
                await _log.LogInfoAsync("Esperando resultado de la confirmacion...");
                await Task.Delay(3000);
                await ScrollToBottomAsync(page);

                // CHECK: Did the bot reject at the last moment? (court taken by someone else)
                if (await CheckForCourtRejectionAsync(page))
                {
                    await _log.LogWarningAsync("=== CANCHA RECHAZADA ===");
                    await _log.LogInfoAsync("La cancha fue reservada por otra persona justo antes de confirmar.");

                    if (attempt < maxAttempts)
                    {
                        await _log.LogInfoAsync($"Reintentando automaticamente... (intento {attempt + 1}/{maxAttempts})");
                        _notification.NotifyError("Platz Daemon", $"Cancha ocupada. Reintentando ({attempt + 1}/{maxAttempts})...");
                        continue; // Retry the whole flow
                    }
                    else
                    {
                        await _log.LogErrorAsync($"Se agotaron los {maxAttempts} intentos. Todas las canchas fueron tomadas.");
                        await _appState.UpdateStatusAsync(DaemonStatus.Error, "Cancha tomada - intentos agotados");
                        _notification.NotifyError("Platz Daemon", $"No se pudo reservar despues de {maxAttempts} intentos. Las canchas fueron tomadas.");
                        return false;
                    }
                }

                // CHECK: Successful confirmation?
                var confirmed = await CheckForConfirmationInRecentAsync(page);
                if (confirmed)
                {
                    var resultMsg = $"RESERVA CONFIRMADA - {selectedTime} - {selectedCourt}";
                    await _log.LogSuccessAsync(resultMsg);
                    await _log.LogInfoAsync("=== RESERVA EXITOSA ===");
                    await _appState.UpdateStatusAsync(DaemonStatus.Completed, resultMsg);
                    _notification.NotifySuccess("Platz Daemon - Reserva Exitosa!", $"{selectedTime} - {selectedCourt} - {config.GameType}");
                    return true;
                }
                else
                {
                    await _log.LogWarningAsync("No se detecto mensaje de confirmacion. Revisar WhatsApp manualmente.");
                    await _appState.UpdateStatusAsync(DaemonStatus.Completed, $"Posible reserva: {selectedTime} - {selectedCourt} (verificar)");
                    _notification.NotifySuccess("Platz Daemon", $"Posible reserva realizada: {selectedTime} - {selectedCourt}. Verificar en WhatsApp.");
                    return true;
                }
            } // end retry loop

            // Should not reach here, but just in case
            await _log.LogErrorAsync("Flujo de reserva finalizo sin resultado.");
            await _appState.UpdateStatusAsync(DaemonStatus.Error, "Sin resultado");
            return false;
        }
        catch (Exception ex) when (IsBrowserClosedException(ex))
        {
            await _log.LogWarningAsync("El navegador se cerro inesperadamente. Limpiando sesion...");
            await CleanupDeadBrowserAsync();
            await _log.LogInfoAsync("Sesion limpiada. Se re-creara automaticamente en la proxima ejecucion.");
            await _appState.UpdateStatusAsync(DaemonStatus.Error, "Navegador cerrado - se reintentara automaticamente");
            _notification.NotifyError("Platz Daemon", "El navegador se cerro. Se reintentara en la proxima ejecucion.");
            return false;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync($"Error durante la reserva: {ex.Message}");
            await _appState.UpdateStatusAsync(DaemonStatus.Error, ex.Message);
            _notification.NotifyError("Platz Daemon - Error", ex.Message);
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Sends Enter key on a pre-armed message (competitive mode).
    /// </summary>
    public async Task SendPreArmedMessageAsync()
    {
        if (_browserContext == null) return;
        var page = _browserContext.Pages.FirstOrDefault();
        if (page == null) return;

        await page.Keyboard.PressAsync("Enter");
        await _log.LogSuccessAsync("Mensaje 'turno' enviado (modo competitivo)!");
    }

    // ========================================================================
    // Private helpers
    // ========================================================================

    private async Task<bool> OpenBotChatAsync(IPage page, string phoneNumber)
    {
        try
        {
            var chatUrl = $"https://web.whatsapp.com/send?phone={phoneNumber}";
            await page.GotoAsync(chatUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 20000
            });

            await page.WaitForSelectorAsync(
                "[data-testid='conversation-compose-box-input'], div[contenteditable='true'][data-tab='10']",
                new PageWaitForSelectorOptions { Timeout = 20000 });

            await Task.Delay(1000);
            await _log.LogSuccessAsync("Chat del bot abierto.");
            return true;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync($"Error abriendo chat: {ex.Message}");
            return false;
        }
    }

    private async Task TypeMessageAsync(IPage page, string message)
    {
        var inputBox = await page.WaitForSelectorAsync(
            "[data-testid='conversation-compose-box-input'], div[contenteditable='true'][data-tab='10']",
            new PageWaitForSelectorOptions { Timeout = 10000 });

        if (inputBox != null)
        {
            await inputBox.ClickAsync();
            await page.Keyboard.TypeAsync(message);
        }
    }

    private async Task SendMessageAsync(IPage page, string message)
    {
        await TypeMessageAsync(page, message);
        await page.Keyboard.PressAsync("Enter");
    }

    // ========================================================================
    // Clear any pending conversation before starting a new booking
    // ========================================================================

    /// <summary>
    /// Sends "Salir" to reset the bot to its main menu.
    /// Handles the case where the bot asks "Por favor selecciona una opción"
    /// by sending "Salir" a second time. Max 2 rounds.
    /// </summary>
    private async Task ClearPendingConversationAsync(IPage page)
    {
        try
        {
            await _log.LogInfoAsync("Limpiando conversacion pendiente (enviando 'Salir')...");
            await ScrollToBottomAsync(page);

            for (int round = 1; round <= 2; round++)
            {
                var countBefore = await GetMessageCountAsync(page);
                await SendMessageAsync(page, "Salir");
                await WaitForBotResponseAsync(page, countBefore, 10000);
                await ScrollToBottomAsync(page);

                // Check if bot is back at main menu
                var status = await page.EvaluateAsync<string>(@"() => {
                    const main = document.querySelector('#main');
                    if (!main) return 'unknown';

                    let messages = [...main.querySelectorAll('[class*=""message-in""]')];
                    if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                    if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];

                    const recent = messages.slice(-3);
                    for (const msg of recent) {
                        const text = (msg.textContent || '').toLowerCase();
                        if (text.includes('cómo puedo ayudarte') || text.includes('como puedo ayudarte') ||
                            text.includes('pedir turno'))
                            return 'menu';
                        if (text.includes('selecciona una opción') || text.includes('selecciona una opcion') ||
                            text.includes('por favor selecciona'))
                            return 'pending';
                    }
                    return 'unknown';
                }");

                if (status == "menu")
                {
                    await _log.LogSuccessAsync("Bot reiniciado al menu principal.");
                    return;
                }
                else if (status == "pending" && round < 2)
                {
                    await _log.LogWarningAsync("Bot todavia tiene conversacion pendiente. Enviando 'Salir' de nuevo...");
                    await Task.Delay(1000);
                    continue;
                }
            }

            await _log.LogInfoAsync("Limpieza completada. Continuando con el flujo...");
        }
        catch (Exception ex)
        {
            await _log.LogWarningAsync($"Error durante limpieza de conversacion: {ex.Message}. Continuando de todas formas...");
        }
    }

    // ========================================================================
    // NEW: Scroll, count messages, and wait for bot response
    // ========================================================================

    /// <summary>
    /// Scrolls the conversation panel to the very bottom so latest messages are visible.
    /// </summary>
    private async Task ScrollToBottomAsync(IPage page)
    {
        try
        {
            await page.EvaluateAsync(@"() => {
                const selectors = [
                    '#main div[role=""application""]',
                    '[data-testid=""conversation-panel-body""]',
                    '#main div[tabindex=""-1""]'
                ];
                for (const s of selectors) {
                    const el = document.querySelector(s);
                    if (el && el.scrollHeight > 0) {
                        el.scrollTop = el.scrollHeight;
                        return;
                    }
                }
            }");
            await Task.Delay(300);
        }
        catch { }
    }

    /// <summary>
    /// Counts the current number of message elements in the conversation.
    /// Uses multiple strategies and logs which one works for debugging.
    /// </summary>
    private async Task<int> GetMessageCountAsync(IPage page, bool logDebug = false)
    {
        try
        {
            var result = await page.EvaluateAsync<int[]>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return [0, 0, 0, 0, 0, 0];

                const byClassIn = main.querySelectorAll('[class*=""message-in""]').length;
                const byClassOut = main.querySelectorAll('[class*=""message-out""]').length;
                const byDataId = main.querySelectorAll('[data-id]').length;
                const byFocusable = main.querySelectorAll('.focusable-list-item').length;
                const byRole = main.querySelectorAll('[role=""row""]').length;
                // Count any div that is a direct child of the scrollable conversation container
                const panel = main.querySelector('[data-testid=""conversation-panel-body""]') || 
                              main.querySelector('[role=""application""]') ||
                              main.querySelector('div[tabindex=""-1""]');
                const byPanelChildren = panel ? panel.querySelectorAll(':scope > div > div').length : 0;

                return [byClassIn, byClassOut, byDataId, byFocusable, byRole, byPanelChildren];
            }");

            if (result == null || result.Length == 0) return 0;

            var byClassIn = result[0];
            var byClassOut = result[1];
            var byDataId = result[2];
            var byFocusable = result[3];
            var byRole = result[4];
            var byPanelChildren = result[5];

            if (logDebug)
            {
                await _log.LogInfoAsync($"DEBUG conteo: message-in={byClassIn}, message-out={byClassOut}, " +
                    $"data-id={byDataId}, focusable={byFocusable}, role=row={byRole}, panel-children={byPanelChildren}");
            }

            // Use the first strategy that returns a meaningful count
            if (byClassIn + byClassOut > 0) return byClassIn + byClassOut;
            if (byDataId > 0) return byDataId;
            if (byRole > 0) return byRole;
            if (byFocusable > 0) return byFocusable;
            if (byPanelChildren > 0) return byPanelChildren;

            return 0;
        }
        catch { return 0; }
    }

    /// <summary>
    /// Waits until new messages appear in the chat (message count increases).
    /// After we send a message or click a button, we expect:
    ///   - Our action adds 1 message (outgoing)
    ///   - The bot responds adding 1+ messages (incoming)
    /// So we wait for count to increase by at least 2.
    /// </summary>
    private async Task<bool> WaitForBotResponseAsync(IPage page, int countBefore, int timeoutMs = 20000)
    {
        // If we couldn't count messages, fall back to a fixed delay
        if (countBefore <= 0)
        {
            await _log.LogInfoAsync("No se pudo contar mensajes, esperando 8 segundos como fallback...");
            await Task.Delay(8000);
            await ScrollToBottomAsync(page);
            return true;
        }

        var targetCount = countBefore + 2; // Our message + bot response
        var sw = Stopwatch.StartNew();
        var pollNum = 0;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(1000);
            pollNum++;
            var currentCount = await GetMessageCountAsync(page);

            // Log progress every 3 polls
            if (pollNum % 3 == 0)
            {
                await _log.LogInfoAsync($"  Esperando... conteo actual={currentCount}, objetivo>={targetCount} (poll #{pollNum})");
            }

            if (currentCount >= targetCount)
            {
                await Task.Delay(1000);
                await ScrollToBottomAsync(page);
                await _log.LogInfoAsync($"Nuevos mensajes detectados ({currentCount - countBefore} nuevos).");
                return true;
            }

            // Also accept if count went up by at least 1 (bot may combine in one message)
            if (currentCount > countBefore)
            {
                await Task.Delay(2000); // Extra wait for full message render
                await ScrollToBottomAsync(page);
                await _log.LogInfoAsync($"Mensaje(s) nuevo(s) detectado(s) ({currentCount - countBefore} nuevo(s)).");
                return true;
            }
        }

        // Final check
        var finalCount = await GetMessageCountAsync(page);
        if (finalCount > countBefore)
        {
            await Task.Delay(1000);
            await ScrollToBottomAsync(page);
            await _log.LogInfoAsync($"Mensajes detectados al final ({finalCount - countBefore} nuevos).");
            return true;
        }

        await _log.LogWarningAsync($"Conteo no cambio: antes={countBefore}, ahora={finalCount}. El conteo podria no funcionar con esta version de WhatsApp.");
        return false;
    }

    // ========================================================================
    // NEW: Click buttons ONLY in recent messages (replaces ClickButtonByTextAsync)
    // ========================================================================

    /// <summary>
    /// Clicks a button by text, but ONLY searching within the last N message containers.
    /// This prevents clicking old buttons from previous conversations.
    /// Also checks list popup overlay items (which are outside message containers).
    /// Polls with retry until timeout.
    /// </summary>
    private async Task<bool> ClickButtonInRecentMessagesAsync(IPage page, string text, int timeoutMs = 15000, int lastN = 5)
    {
        var sw = Stopwatch.StartNew();
        var pollCount = 0;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            await ScrollToBottomAsync(page);
            pollCount++;

            try
            {
                var result = await page.EvaluateAsync<string>(@"(arg) => {
                    const searchText = arg.searchText.toLowerCase().trim();
                    const maxRecent = arg.maxRecent;

                    // === STRATEGY 1: Check open list popup overlay ===
                    const popupItems = document.querySelectorAll(
                        '[data-testid=""list-msg-title""], [role=""radio""], [role=""option""]'
                    );
                    for (const item of popupItems) {
                        const t = (item.textContent || '').trim().toLowerCase();
                        if (t === searchText || t.includes(searchText)) {
                            item.scrollIntoView({ block: 'center' });
                            item.click();
                            return 'CLICKED:popup:' + t;
                        }
                    }

                    // === STRATEGY 2: Search in last N message containers ===
                    const main = document.querySelector('#main');
                    if (!main) return 'NO_MAIN';

                    // Find message rows using multiple strategies
                    let messages = [...main.querySelectorAll('[class*=""message-in""], [class*=""message-out""]')];
                    let strategy = 'class';
                    if (messages.length === 0) {
                        messages = [...main.querySelectorAll('[data-id]')];
                        strategy = 'data-id';
                    }
                    if (messages.length === 0) {
                        messages = [...main.querySelectorAll('[role=""row""]')];
                        strategy = 'role-row';
                    }
                    if (messages.length === 0) {
                        messages = [...main.querySelectorAll('.focusable-list-item')];
                        strategy = 'focusable';
                    }
                    if (messages.length === 0) return 'NO_MESSAGES';

                    // ONLY look at the last N messages
                    const recent = messages.slice(-maxRecent);

                    // Broad clickable selectors - WhatsApp uses various elements for interactive buttons
                    const clickableSelector = 'div[role=""button""], span[role=""button""], button, ' +
                        '[data-testid*=""list""], [data-testid*=""quick_reply""], ' +
                        'span.selectable-text, div.quoted-mention';

                    // First pass: exact match on clickable elements
                    for (const msg of [...recent].reverse()) {
                        const clickables = msg.querySelectorAll(clickableSelector);
                        for (const el of [...clickables].reverse()) {
                            const elText = (el.textContent || '').trim().toLowerCase();
                            if (elText === searchText) {
                                el.scrollIntoView({ block: 'center' });
                                el.click();
                                return 'CLICKED:' + strategy + ':exact:' + elText;
                            }
                        }
                    }

                    // Second pass: contains match on clickable elements
                    for (const msg of [...recent].reverse()) {
                        const clickables = msg.querySelectorAll(clickableSelector);
                        for (const el of [...clickables].reverse()) {
                            const elText = (el.textContent || '').trim().toLowerCase();
                            if (elText.includes(searchText)) {
                                el.scrollIntoView({ block: 'center' });
                                el.click();
                                return 'CLICKED:' + strategy + ':contains:' + elText;
                            }
                        }
                    }

                    // Third pass: find ANY span in recent messages matching the text and click it
                    // WhatsApp interactive buttons are often just plain spans
                    for (const msg of [...recent].reverse()) {
                        const spans = msg.querySelectorAll('span');
                        for (const span of [...spans].reverse()) {
                            const t = (span.textContent || '').trim().toLowerCase();
                            if (t === searchText && span.offsetParent !== null) {
                                span.scrollIntoView({ block: 'center' });
                                span.click();
                                return 'CLICKED:' + strategy + ':span:' + t;
                            }
                        }
                    }

                    return 'NOT_FOUND:' + strategy + ':msgs=' + messages.length + ':recent=' + recent.length;
                }", new { searchText = text, maxRecent = lastN });

                if (result != null && result.StartsWith("CLICKED"))
                {
                    await _log.LogInfoAsync($"Boton encontrado: {result}");
                    return true;
                }

                // Log debug info periodically (every 5 polls = ~5 seconds)
                if (pollCount % 5 == 0 && result != null)
                {
                    await _log.LogInfoAsync($"Polling #{pollCount} buscando '{text}': {result}");
                }
            }
            catch (Exception ex)
            {
                if (pollCount % 5 == 0)
                    await _log.LogWarningAsync($"Error en poll #{pollCount}: {ex.Message}");
            }

            await Task.Delay(1000);
        }

        await _log.LogWarningAsync($"Timeout buscando '{text}' despues de {pollCount} intentos ({timeoutMs / 1000}s).");
        return false;
    }

    // ========================================================================
    // Updated: DNI check - only in recent messages
    // ========================================================================

    /// <summary>
    /// Checks if the bot requested DNI in the last 5 messages.
    /// Does NOT send the DNI - the caller handles that.
    /// </summary>
    private async Task<bool> CheckForDniRequestInRecentAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return false;

                let messages = [...main.querySelectorAll('[class*=""message-in""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('.focusable-list-item')];

                // Only check the last 5 messages
                const recent = messages.slice(-5);

                for (const msg of recent) {
                    const text = (msg.textContent || '').toLowerCase();
                    if (text.includes('documento') || text.includes('dni') ||
                        text.includes('número de documento') || text.includes('numero de documento')) {
                        return true;
                    }
                }
                return false;
            }");
        }
        catch { return false; }
    }

    // ========================================================================
    // Name confirmation check - "¿A nombre de ...?" with Sí/No
    // ========================================================================

    /// <summary>
    /// Checks if the bot is asking for name confirmation (e.g. "¿A nombre de PERTILE, FRANCO?")
    /// in the last 5 messages.
    /// </summary>
    private async Task<bool> CheckForNameConfirmationInRecentAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return false;

                let messages = [...main.querySelectorAll('[class*=""message-in""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('.focusable-list-item')];

                const recent = messages.slice(-5);

                for (const msg of recent) {
                    const text = (msg.textContent || '').toLowerCase();
                    if (text.includes('a nombre de') || text.includes('nombre de')) {
                        return true;
                    }
                }
                return false;
            }");
        }
        catch { return false; }
    }

    // ========================================================================
    // Updated: Select time slot - uses ClickButtonInRecentMessagesAsync
    // ========================================================================

    private async Task<string?> SelectTimeSlotAsync(IPage page, List<string> preferredSlots)
    {
        try
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                await Task.Delay(1500);
                await ScrollToBottomAsync(page);

                // Try to open list popup (only from recent messages)
                var listOpened = await TryOpenListPopupInRecentAsync(page);
                if (listOpened)
                {
                    await _log.LogInfoAsync("Popup de lista de horarios abierto.");
                    await Task.Delay(1500);
                }

                // Try each preferred slot (searches popup items + recent message buttons)
                foreach (var preferred in preferredSlots)
                {
                    await _log.LogInfoAsync($"Buscando horario: {preferred}...");

                    if (await ClickButtonInRecentMessagesAsync(page, preferred, 2000))
                    {
                        await Task.Delay(500);
                        await TryClickListSubmitAsync(page);
                        return preferred;
                    }
                }

                if (attempt < 3)
                {
                    await _log.LogInfoAsync($"Reintentando busqueda de horarios ({attempt + 1}/3)...");
                }
                else
                {
                    await LogVisibleOptionsAsync(page, "horario");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync($"Error seleccionando horario: {ex.Message}");
            return null;
        }
    }

    // ========================================================================
    // Updated: Select court - uses ClickButtonInRecentMessagesAsync
    // ========================================================================

    private async Task<string?> SelectCourtAsync(IPage page, List<string> preferredCourts)
    {
        try
        {
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                await Task.Delay(1500);
                await ScrollToBottomAsync(page);

                // Try to open list popup
                var listOpened = await TryOpenListPopupInRecentAsync(page);
                if (listOpened)
                {
                    await _log.LogInfoAsync("Popup de lista de canchas abierto.");
                    await Task.Delay(1500);
                }

                // Try preferred courts
                foreach (var preferred in preferredCourts)
                {
                    await _log.LogInfoAsync($"Buscando cancha: {preferred}...");

                    if (await ClickButtonInRecentMessagesAsync(page, preferred, 2000))
                    {
                        await Task.Delay(500);
                        await TryClickListSubmitAsync(page);
                        return preferred;
                    }
                }

                // Fallback: try first available option in popup
                var firstOption = await FindFirstAvailableOptionAsync(page);
                if (firstOption != null)
                {
                    await _log.LogWarningAsync($"Ninguna cancha preferida disponible. Seleccionando: {firstOption}");
                    return firstOption;
                }

                if (attempt < 3)
                {
                    await _log.LogInfoAsync($"Reintentando busqueda de canchas ({attempt + 1}/3)...");
                }
                else
                {
                    await LogVisibleOptionsAsync(page, "cancha");
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync($"Error seleccionando cancha: {ex.Message}");
            return null;
        }
    }

    // ========================================================================
    // Check if bot says user already has a reservation or no slots available
    // ========================================================================

    /// <summary>
    /// Checks if the bot replied with a blocking message.
    /// Returns: "already_booked" | "no_slots" | null (no blocker detected).
    /// </summary>
    private async Task<string?> CheckForBookingBlockerAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<string?>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return null;

                let messages = [...main.querySelectorAll('[class*=""message-in""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('.focusable-list-item')];

                // Only check the last 5 messages
                const recent = messages.slice(-5);

                for (const msg of recent) {
                    const text = (msg.textContent || '').toLowerCase();

                    // Check: already has a reservation
                    if ((text.includes('ya tiene') && text.includes('turno')) ||
                        text.includes('turno reservado') ||
                        (text.includes('no se puede reservar') && text.includes('turno')) ||
                        text.includes('cancelar el turno') ||
                        text.includes('ya tiene una reserva')) {
                        return 'already_booked';
                    }

                    // Check: no slots available today
                    if (text.includes('no hay turnos disponibles') ||
                        (text.includes('no hay turnos') && text.includes('espera')) ||
                        (text.includes('no hay turnos') && text.includes('mañana')) ||
                        text.includes('ya no hay turnos')) {
                        return 'no_slots';
                    }
                }
                return null;
            }");
        }
        catch { return null; }
    }

    // ========================================================================
    // Check if bot rejected the booking at confirmation (court taken by someone else)
    // ========================================================================

    /// <summary>
    /// After clicking "Sí", checks if the bot said the court is no longer available.
    /// This happens when someone else booked it between selection and confirmation.
    /// </summary>
    private async Task<bool> CheckForCourtRejectionAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return false;

                let messages = [...main.querySelectorAll('[class*=""message-in""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('.focusable-list-item')];

                const recent = messages.slice(-5);

                for (const msg of recent) {
                    const text = (msg.textContent || '').toLowerCase();
                    if ((text.includes('no se puede reservar') && text.includes('cancha')) ||
                        text.includes('cancha no esta disponible') ||
                        text.includes('cancha no está disponible') ||
                        (text.includes('no disponible') && text.includes('cancha')) ||
                        (text.includes('lo sentimos') && text.includes('no se puede reservar')) ||
                        (text.includes('ocupad') && text.includes('cancha')) ||
                        (text.includes('no se pudo') && text.includes('reservar'))) {
                        return true;
                    }
                }
                return false;
            }");
        }
        catch { return false; }
    }

    // ========================================================================
    // Updated: Confirmation check - only in recent messages
    // ========================================================================

    private async Task<bool> CheckForConfirmationInRecentAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return false;

                let messages = [...main.querySelectorAll('[class*=""message-in""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('.focusable-list-item')];

                // Only check the last 5 messages
                const recent = messages.slice(-5);

                for (const msg of recent) {
                    const text = (msg.textContent || '').toLowerCase();
                    if (text.includes('confirmad') || text.includes('éxito') ||
                        text.includes('reservado') || text.includes('reserva realizada') ||
                        text.includes('confirmando turno')) {
                        return true;
                    }
                }
                return false;
            }");
        }
        catch { return false; }
    }

    // ========================================================================
    // Updated: List popup - only from recent messages
    // ========================================================================

    /// <summary>
    /// Tries to open a list popup, but ONLY looking at the last 2 messages
    /// to avoid opening old list popups from previous conversations.
    /// </summary>
    private async Task<bool> TryOpenListPopupInRecentAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(@"() => {
                const main = document.querySelector('#main');
                if (!main) return false;

                let messages = [...main.querySelectorAll('[class*=""message-in""], [class*=""message-out""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[data-id]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('[role=""row""]')];
                if (messages.length === 0) messages = [...main.querySelectorAll('.focusable-list-item')];

                // Only look at the last 5 messages for the list button
                const recent = messages.slice(-5);

                for (const msg of [...recent].reverse()) {
                    // Try data-testid selectors for list action buttons
                    const listBtns = msg.querySelectorAll(
                        '[data-testid=""list-msg-action""], [data-testid=""list-msg-action-button""]'
                    );
                    if (listBtns.length > 0) {
                        listBtns[listBtns.length - 1].click();
                        return true;
                    }

                    // Fallback: look for buttons with text like 'Ver opciones'
                    const allBtns = msg.querySelectorAll('div[role=""button""], span[role=""button""], button');
                    for (const btn of allBtns) {
                        const text = (btn.textContent || '').trim().toLowerCase();
                        const keywords = ['ver opciones', 'seleccionar', 'elegir', 'ver horarios', 'ver canchas', 'opciones'];
                        if (keywords.some(k => text.includes(k))) {
                            btn.click();
                            return true;
                        }
                    }
                }

                return false;
            }");
        }
        catch { return false; }
    }

    /// <summary>
    /// Tries to click the submit/confirm button in a WhatsApp list popup.
    /// The popup is a global overlay, so searching globally is fine.
    /// </summary>
    private async Task TryClickListSubmitAsync(IPage page)
    {
        try
        {
            var submitSelectors = new[]
            {
                "[data-testid='list-msg-submit']",
                "[data-testid='send']",
                "button:has-text('Enviar')",
                "div[role='button']:has-text('Enviar')",
            };

            foreach (var selector in submitSelectors)
            {
                try
                {
                    var el = await page.QuerySelectorAsync(selector);
                    if (el != null)
                    {
                        await el.ClickAsync();
                        return;
                    }
                }
                catch { }
            }

            // JS fallback
            await page.EvaluateAsync(@"() => {
                const buttons = document.querySelectorAll('div[role=""button""], button, span[role=""button""]');
                for (const btn of buttons) {
                    const text = (btn.textContent || '').trim().toLowerCase();
                    if (text === 'enviar' || text === 'confirmar' || text === 'aceptar') {
                        btn.click();
                        return;
                    }
                }
            }");
        }
        catch { }
    }

    /// <summary>
    /// Finds and clicks the first available option in a list popup overlay.
    /// Popup items are global (not inside message containers), so global search is fine.
    /// </summary>
    private async Task<string?> FindFirstAvailableOptionAsync(IPage page)
    {
        try
        {
            // Search in popup overlay (global)
            var result = await page.EvaluateAsync<string>(@"() => {
                const popupSelectors = [
                    '[data-testid=""list-msg-title""]',
                    '[role=""radio""]',
                    '[role=""option""]',
                ];

                for (const selector of popupSelectors) {
                    const items = document.querySelectorAll(selector);
                    if (items.length > 0) {
                        const text = (items[0].textContent || '').trim();
                        items[0].click();
                        return text;
                    }
                }

                return '';
            }") ?? "";

            if (!string.IsNullOrEmpty(result))
            {
                await Task.Delay(500);
                await TryClickListSubmitAsync(page);
                return result;
            }
        }
        catch { }
        return null;
    }

    /// <summary>
    /// Logs visible interactive options on the page for debugging.
    /// </summary>
    private async Task LogVisibleOptionsAsync(IPage page, string context)
    {
        try
        {
            var options = await page.EvaluateAsync<string[]>(@"() => {
                const results = [];
                const spans = document.querySelectorAll('span');
                for (const span of [...spans].slice(-50)) {
                    const text = span.textContent.trim();
                    if (text && text.length > 0 && text.length < 80) {
                        const rect = span.getBoundingClientRect();
                        if (rect.width > 0 && rect.height > 0) {
                            results.push(text);
                        }
                    }
                }
                return [...new Set(results)].slice(-20);
            }");

            if (options != null && options.Length > 0)
            {
                await _log.LogInfoAsync($"DEBUG - Textos visibles en pantalla ({context}):");
                foreach (var opt in options)
                {
                    await _log.LogInfoAsync($"  > \"{opt}\"");
                }
            }
            else
            {
                await _log.LogWarningAsync($"DEBUG - No se encontraron textos visibles para {context}.");
            }
        }
        catch (Exception ex)
        {
            await _log.LogWarningAsync($"Error al logear opciones visibles: {ex.Message}");
        }
    }

    // ========================================================================
    // Browser health helpers
    // ========================================================================

    /// <summary>
    /// Detects if an exception was caused by the browser being closed externally.
    /// </summary>
    private static bool IsBrowserClosedException(Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("target closed") ||
               msg.Contains("browser has been closed") ||
               msg.Contains("browser closed") ||
               msg.Contains("connection disposed") ||
               msg.Contains("object is not connected") ||
               msg.Contains("session closed") ||
               msg.Contains("target crashed") ||
               msg.Contains("connection refused") ||
               ex is PlaywrightException pe && pe.Message.Contains("closed");
    }

    /// <summary>
    /// Cleans up a dead browser context so it can be re-created on the next execution.
    /// </summary>
    private async Task CleanupDeadBrowserAsync()
    {
        try
        {
            if (_browserContext != null)
            {
                try { await _browserContext.CloseAsync(); } catch { }
                _browserContext = null;
            }
        }
        catch { _browserContext = null; }

        try
        {
            _playwright?.Dispose();
            _playwright = null;
        }
        catch { _playwright = null; }

        _appState.SetWhatsAppConnected(false);
    }

    // ========================================================================
    // Dispose
    // ========================================================================

    public async ValueTask DisposeAsync()
    {
        if (_browserContext != null)
        {
            await _browserContext.CloseAsync();
            _browserContext = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }

    private async Task CloseSessionInternalAsync()
    {
        if (_browserContext != null)
        {
            try { await _browserContext.CloseAsync(); } catch { }
            _browserContext = null;
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
            _playwright = null;
        }
    }
}
