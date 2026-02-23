# Guia de modificacion

Referencia rapida: "quiero hacer X, toco Y".

---

## Tabla de referencia

| Quiero... | Archivos a tocar | Notas |
|---|---|---|
| **Agregar un campo de configuracion** | `Models/BookingConfig.cs`, la pagina Razor correspondiente (`.cshtml` + `.cshtml.cs`), opcionalmente `ConfigStore.MigrateConfig` | Ver [configuration.md](configuration.md) seccion "Como agregar un nuevo campo" |
| **Cambiar el flujo de reserva** | `Services/WhatsAppAutomationService.cs` → `ExecuteBookingAsync` | El flujo esta secuencial dentro de un loop `for`. Cada paso tiene un comentario `// STEP N` |
| **Agregar un nuevo paso en la conversacion con el bot** | `Services/WhatsAppAutomationService.cs` | Crear un metodo `CheckFor...InRecentAsync` y llamarlo dentro de `ExecuteBookingAsync` |
| **Cambiar como se detectan los mensajes del bot** | `Services/WhatsAppAutomationService.cs` → JS inline en los metodos `CheckFor...Async` | Modificar los `text.includes(...)` dentro de los bloques `EvaluateAsync` |
| **Agregar un nuevo selector de WhatsApp Web** | `Services/WhatsAppAutomationService.cs` → el metodo que falla | Agregar como fallback adicional, nunca eliminar selectores existentes |
| **Cambiar la UI / estilos** | `wwwroot/css/site.css` | Nunca tocar `terminal.min.css`. Usar las variables CSS de `:root` |
| **Agregar una nueva pagina** | Crear `Pages/Nueva.cshtml` + `.cshtml.cs`, agregar a `Pages/Shared/_Layout.cshtml` | Ver [ui-and-frontend.md](ui-and-frontend.md) seccion "Como agregar una nueva pagina" |
| **Cambiar la navbar** | `Pages/Shared/_Layout.cshtml` | Agregar/quitar `<li>` en el `<ul>` de `terminal-menu` |
| **Cambiar el timing / hora de disparo** | `Services/BookingSchedulerService.cs` | `ExecuteAsync` para el loop, `CalculateNextTrigger` para la logica de proximo disparo |
| **Cambiar el modo competitivo** | `Services/BookingSchedulerService.cs` → dentro del `if (config.CompetitiveMode)` | Pre-arm en `WhatsAppAutomationService.ExecuteBookingAsync(competitivePreArm: true)` |
| **Cambiar el precision timing** | `Services/BookingSchedulerService.cs` → `PrecisionWaitAsync` | Tres fases: sleep grueso, fino, busy-wait |
| **Cambiar la zona horaria** | `Services/BookingSchedulerService.cs` → `ResolveArgentinaTimeZone` | Cambiar los IDs de timezone |
| **Cambiar la logica de deteccion de sesion WhatsApp** | `Services/WhatsAppAutomationService.cs` → `CheckSessionAsync` | Agregar/modificar selectores CSS y la evaluacion JS |
| **Cambiar como se abre el browser** | `Services/WhatsAppAutomationService.cs` → `LaunchPersistentContextAsync` llamadas | Opciones de Playwright (viewport, args, etc.) |
| **Cambiar los logs / formato** | `Models/LogEntry.cs` para formato, `Services/LogStore.cs` para buffer/broadcast | `Prefix` y `CssClass` son computed properties |
| **Cambiar el estado del daemon** | `Models/AppState.cs` para el modelo, `Services/AppStateService.cs` para la logica | Agregar nuevos estados al enum `DaemonStatus` |
| **Agregar logs en tiempo real a otra pagina** | Copiar el bloque `@section Scripts` de `Index.cshtml` | Necesita conexion SignalR a `/loghub` |
| **Cambiar el formato del config JSON** | `Services/ConfigStore.cs` → `JsonOptions` | Cambiar `PropertyNamingPolicy`, `WriteIndented`, etc. |
| **Agregar tests** | `PlatzDaemon.Tests/{Models,Services,Pages}/` | Espejar la estructura del proyecto principal. Ver [testing.md](testing.md) |
| **Cambiar el pipeline CI** | `.github/workflows/ci.yml` | Build, tests, coverage |
| **Cambiar el pipeline de release** | `.github/workflows/release.yml` | Matrix build, publish, GitHub Release |
| **Cambiar que plataformas se publican** | `.github/workflows/release.yml` → `strategy.matrix` | Agregar/quitar entradas `{os, rid, artifact}` |
| **Cambiar el puerto** | `Program.cs` → `builder.WebHost.UseUrls(...)` | Tambien actualizar `Properties/launchSettings.json` para desarrollo |

---

## Escenarios comunes

### "El club cambio el bot y ahora los menus son distintos"

1. Abrir WhatsApp en el celular y hacer el flujo de reserva manualmente. Anotar los textos exactos de cada paso.
2. En `WhatsAppAutomationService.cs`, buscar las ocurrencias de `text.includes(` y ajustar los keywords:
   - `CheckForDniRequestInRecentAsync`: keywords de solicitud de DNI.
   - `CheckForNameConfirmationInRecentAsync`: keywords de confirmacion de nombre.
   - `CheckForBookingBlockerAsync`: keywords de "ya tiene turno" y "no hay turnos".
   - `CheckForCourtRejectionAsync`: keywords de rechazo de cancha.
   - `CheckForConfirmationInRecentAsync`: keywords de confirmacion exitosa.
   - `ClearPendingConversationAsync`: keywords del menu principal.
3. Testear con ejecucion manual desde el dashboard.

### "Quiero agregar soporte para un nuevo tipo de reserva"

Ejemplo: ademas de Single y Doble, el club agrego "Escuela".

1. En `Models/BookingConfig.cs`: no se necesita cambiar nada (el campo `GameType` ya es un string libre).
2. En `Pages/Config.cshtml`: agregar `<option value="Escuela">Escuela</option>` al select de tipo de juego.
3. El resto funciona automaticamente: `ClickButtonInRecentMessagesAsync(page, config.GameType, ...)` busca el texto del boton, que seria "Escuela".

### "Quiero que el sistema avise por Telegram cuando la reserva se confirme"

1. Crear un nuevo servicio `TelegramNotificationService` con la logica de envio.
2. Registrarlo como Singleton en `Program.cs`.
3. Inyectarlo en `WhatsAppAutomationService` (o en `BookingSchedulerService`).
4. Llamar al servicio despues de `await _log.LogSuccessAsync("RESERVA CONFIRMADA ...")`.

### "Quiero soportar otro club con menus diferentes"

Opciones:
- **Simple**: cambiar los keywords en `WhatsAppAutomationService` para que matcheen el nuevo bot.
- **Multi-club**: agregar un campo `ClubProfile` a `BookingConfig` y usar distintos conjuntos de keywords segun el perfil. Requiere refactorizar la deteccion de texto para ser configurable.

### "Quiero agregar un boton de 'Cancelar reserva'"

1. En `Pages/Index.cshtml`: agregar un nuevo form con `asp-page-handler="CancelBooking"`.
2. En `Pages/Index.cshtml.cs`: agregar `OnPostCancelBookingAsync`.
3. En `WhatsAppAutomationService`: agregar un metodo `CancelBookingAsync` que envie "Cancelar" o el comando que el bot acepte.
4. Llamar al nuevo metodo desde el page handler.

### "Quiero que el sistema guarde un historial de reservas"

1. Crear un modelo `BookingHistory` con campos como `DateTime`, `Court`, `TimeSlot`, `Success`.
2. Crear un servicio `BookingHistoryStore` que persista en un archivo JSON (similar a `ConfigStore`).
3. Registrarlo en `Program.cs`.
4. Llamarlo desde `WhatsAppAutomationService` al final de `ExecuteBookingAsync` para guardar cada resultado.
5. Crear una nueva pagina `/Historial` para mostrar los datos.
