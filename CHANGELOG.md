# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

## [1.6.0] - 2026-03-04

### Changed
- **Reset del bot**: se reemplazó el envío de "Salir" por "menu" en `ClearPendingConversationAsync`. El bot no reconocía "Salir"; "menu" es el comando documentado por el bot para volver al menú principal.
- **Inicio de reserva**: en vez de enviar "turno" como texto, ahora se clickea el botón "🕒Pedir turno" usando `ClickButtonInRecentMessagesAsync`. Enviar texto no funcionaba; el bot solo responde al click del botón interactivo.
- **Modo competitivo (pre-arm)**: el pre-arm ahora envía "menu" y espera a que el menú principal aparezca (botón "Pedir turno" visible). Al disparar, `SendPreArmedMessageAsync` clickea el botón en vez de presionar Enter. Incluye fallback: si el botón no se encuentra, re-envía "menu" y reintenta.
- **Docs**: actualizados `booking-flow.md`, `maintenance.md`, `getting-started.md` y `scheduler-and-timing.md` para reflejar los nuevos comandos.

## [1.5.9] - 2026-02-23

### Changed
- **Normalización robusta del número del bot**: se quita el `0` inicial (prefijo troncal argentino, ej: `03534090496` → `3534090496`) antes de aplicar las reglas de formato internacional. Ahora cubre todos los formatos comunes: `5493534090496`, `93534090496`, `3534090496`, `03534090496`. Código de país (`CountryCode`) configurable en `config.json` (default `"54"`).
- **Default del número del bot** cambiado de `93534407576` a `5493534407576` (ya normalizado, funciona de entrada al descargar el EXE).
- **Tests**: actualizados para reflejar el nuevo default.

### Fixed
- **Prevención de suspensión (Windows)**: `SetThreadExecutionState` ahora se refresca en cada poll (cada 5 min) en vez de llamarse una sola vez. En código async el thread puede cambiar entre polls; sin refrescar, el estado podía perderse y la PC suspenderse igual.

## [1.5.8] - 2026-02-23

### Added
- **Normalización del número del bot**: al abrir el chat de WhatsApp se usa formato internacional (solo dígitos); si el número es local Argentina (10-11 dígitos empezando en 9), se antepone 54. La URL siempre es `https://web.whatsapp.com/send?phone=XXX`. Log cambiado a "Abriendo chat del bot por URL (XXX)".

### Changed
- **Validación del número del bot (Sistema)**: el campo acepta solo entre 10 y 15 dígitos (se ignoran espacios/guiones). Mensajes de error en español ("El numero del bot es obligatorio." / "El numero del bot debe tener entre 10 y 15 digitos..."). Si el campo llega vacío al guardar, se usa el valor ya guardado en config (re-guardar sin tocar el número). El error visual (borde rojo) se aplica solo a ese campo (clase `form-group-error`), no al DNI.
- **Test**: `OnPostAsync_UpdatesOnlySystemFields_PreservesBookingFields` usa número válido (5493534407576) para que pase la validación.

### Fixed
- **ArgumentNullException** al guardar en Sistema cuando `BotPhoneNumber` llegaba null (ej. envío del formulario sin valor).

## [1.5.6] - 2026-02-23

### Changed
- **Prevención de suspensión**: ventana por defecto aumentada de 6 h a **24 h** (`SleepPrevention:HoursAhead`) para cubrir el caso "disparo a las 8am desde la noche anterior". Mensaje de arranque aclara que está "disponible" y "se activará cuando el próximo disparo esté en las próximas X h" (evita confusión: antes parecía que ya estaba protegiendo). Log explícito al **activar** ("La PC no se suspendera") y al **desactivar** ("no hay disparo en la ventana. La PC puede suspenderse") para que en el Terminal se vea claro si en ese momento se está bloqueando el sleep o no.

## [1.5.5] - 2026-02-23

### Changed
- **Nota prevención de suspensión**: en la pantalla Sistema se muestra un aviso que explica que la app evita la suspensión cuando hay un próximo disparo, recomienda revisar la configuración de energía de la PC (o desactivar suspensión) y aclara que en Windows debe usarse "Suspender" y no "Hibernar". Misma información actualizada en el README (Paso 6).

## [1.5.4] - 2026-02-23

### Added
- **Prevención de suspensión (SleepPreventionService)**: cuando hay un próximo disparo programado en las próximas X horas (por defecto 6), el daemon indica al sistema operativo que no suspenda la PC. Al no haber tarea próxima o al deshabilitar, se libera el bloqueo. Por OS: Windows (SetThreadExecutionState), Linux (systemd-inhibit), macOS (caffeinate). Fallos se registran en el Terminal de logs; la app no crashea si la inhibición falla. Al cerrar la app la prevención se desactiva y la PC puede suspenderse con normalidad. Configuración opcional en `SleepPrevention:HoursAhead`, `PollIntervalMinutes`, `InhibitProcessTimeoutSeconds`.

## [1.5.3] - 2026-02-23

### Changed
- **Aviso en pagina Session**: cuando Chromium no esta instalado, la pagina WhatsApp muestra un aviso antes del boton "Conectar WhatsApp" indicando que la primera conexion descargara Chromium (~140 MB) y puede tardar unos minutos.

## [1.5.2] - 2026-02-23

### Changed
- **Feedback de descarga de Chromium**: la primera vez que se ejecuta, el dashboard ahora muestra "Primera ejecucion: descargando navegador Chromium (~140 MB)..." en vez de solo "Verificando navegador Chromium..." sin explicar la espera de ~50 segundos. Al terminar muestra "Chromium descargado e instalado correctamente." En ejecuciones posteriores muestra "Chromium verificado." de forma inmediata.

## [1.5.1] - 2026-02-23

### Fixed
- **Playwright "Driver not found" en EXE publicado**: el EXE se publicaba con `IncludeNativeLibrariesForSelfExtract=true`, que empaquetaba el driver Node.js de Playwright dentro del single-file. Playwright buscaba el driver en disco y fallaba con `Driver not found: D:\.playwright\node\win32_x64\node.exe`. Se eliminó la propiedad del `.csproj` y del `release.yml` para que `.playwright/` quede al lado del EXE.
- **Auto-instalación de Chromium**: agregado `EnsureBrowserInstalledAsync()` que llama a `Microsoft.Playwright.Program.Main(new[] { "install", "chromium" })` antes de cada uso de Playwright. La primera ejecución descarga Chromium automáticamente (~100 MB). Ejecuciones posteriores no descargan nada.
- **Docs**: actualizado README con nota sobre la descarga automática de Chromium y corregidos los comandos de publicación. Actualizado `docs/playwright-guide.md` con explicación del problema y la solución. Agregados escenarios de troubleshooting en `docs/maintenance.md`.

## [1.5.0] - 2026-02-23

### Changed
- **Multi-plataforma**: el proyecto ahora compila y corre en **Windows**, **Linux** y **macOS**. TFM cambiado de `net10.0-windows` a `net10.0`, eliminado `RuntimeIdentifier` y `Microsoft.Windows.Compatibility`.
- **CI/CD multi-plataforma**: `ci.yml` ahora corre en `ubuntu-latest`. `release.yml` usa matrix build para generar binarios de 3 plataformas (`win-x64`, `linux-x64`, `osx-arm64`) en paralelo. Windows genera `.zip`, Linux/macOS generan `.tar.gz`. Changelog se extrae con `awk` en bash.
- **Auto-open browser**: detección de OS con `RuntimeInformation.IsOSPlatform` → `Process.Start` (Windows), `open` (macOS), `xdg-open` (Linux).
- **Timezone cross-platform**: nuevo helper `ResolveArgentinaTimeZone()` que prueba Windows ID (`Argentina Standard Time`), IANA ID (`America/Argentina/Buenos_Aires`), y fallback manual UTC-3.
- **Docs**: README y DOCS actualizados con instrucciones multi-plataforma, badges, rutas y comandos para las 3 plataformas.
- **Badges**: badge de versión, tests (91) y coverage (65.3%) actualizados. Agregado badge de versión.
- **`.gitignore`**: agregado `**/TestResults/` para ignorar artefactos de coverage.

### Removed
- **NotificationService**: eliminadas las notificaciones toast de Windows y los sonidos (`System.Media.SystemSounds`). Se removió el servicio, sus tests, y todas las ~15 referencias en 5 archivos. La complejidad no justificaba el beneficio — el resultado se verifica en el Dashboard o en WhatsApp.
- **Microsoft.Windows.Compatibility**: paquete NuGet eliminado (ya no se necesitan APIs de Windows).

## [1.4.1] - 2026-02-23

### Added
- **Screenshots en README**: sección "Interfaz" con capturas de las 4 pantallas (Dashboard, Mi Reserva, Sistema, WhatsApp). El dashboard se muestra directo y las otras 3 en `<details>` colapsables.

### Fixed
- **Versión hardcodeada en navbar**: el layout mostraba "Platz Daemon v1.0" fijo. Ahora se lee dinámicamente desde el `AssemblyVersion` del proyecto, reflejando siempre la versión actual del `.csproj`.

## [1.4.0] - 2026-02-23

> 🎉 **Milestone: primer flujo completo exitoso.** Con esta versión el daemon logró ejecutar el ciclo entero de reserva de forma autónoma por primera vez — desde abrir el chat del bot, navegar menús, seleccionar horario, cancha y tipo de juego, hasta confirmar y detectar el mensaje de éxito. Todos los bugs críticos de interacción con el DOM de WhatsApp fueron corregidos.

### Added
- **Busqueda dinámica de periodos**: el sistema ahora busca los horarios preferidos en TODOS los periodos disponibles (Mañana, Tarde, Noche), no solo en el periodo configurado. Empieza por el periodo preferido y, si no encuentra horarios, cierra el popup con Escape y prueba el siguiente periodo automáticamente.
- **Config UI**: texto de ayuda bajo "Periodo preferido" indicando que el sistema busca automáticamente en otros periodos.

### Changed
- **Estado WhatsApp (Dashboard + Session)**: ahora muestra tres estados en vez de dos: 🟢 "Conectado" (navegador activo con sesión verificada), 🟡 "Sesión guardada" (datos de sesión guardados de una ejecución anterior, se reconecta automáticamente al ejecutar) y 🔴 "Desconectado" (primera vez, necesita escanear QR). Aplica tanto al Dashboard como a la página `/Session`.

### Fixed
- **Confirmacion de reserva no se detectaba**: despues de clickear "Si", el daemon esperaba solo 3 segundos (delay fijo) y luego hacia una unica verificacion del texto de confirmacion. El bot tarda mas de 3s en responder, asi que la verificacion siempre fallaba. Ahora usa un polling loop de hasta 20 segundos que chequea cada 2s buscando texto de confirmacion o rechazo. Tambien se removio `'confirmando turno'` de los keywords de confirmacion porque es un mensaje intermedio que puede ser seguido de un rechazo.
- **Bot respondía "Single" en vez de "Si"**: `WaitForBotResponseAsync` retornaba prematuramente porque contaba mensajes entrantes + salientes. Al clickear un botón, el mensaje saliente (+1) satisfacía la espera antes de que llegara la respuesta del bot. Luego `ClickButtonInRecentMessagesAsync` buscaba "si" pero encontraba "👤Single" (`.includes("si")` match). Fix: `GetMessageCountAsync` ahora cuenta solo `message-in` (entrantes), `WaitForBotResponseAsync` espera +1 entrante, y el matching de botones normaliza emojis y usa word-boundary en vez de substring.
- **Popup de canchas no se abría**: `TryOpenListPopupInRecentAsync` no encontraba el botón de lista porque el HTML real usa `data-icon="list-msg-icon"` en vez de `data-testid="list-msg-action"`, y el texto "Canchas: 7-14" no matcheaba ningún keyword. Ahora busca primero por `[data-icon="list-msg-icon"]` (el icono ≡ de los botones de lista) y se agregaron `"canchas"` y `"turnos"` como keywords de fallback. Logging mejorado en `SelectCourtAsync` para diagnosticar cuando el popup no se abre.
- **Debug log mostraba chats del sidebar**: `LogVisibleOptionsAsync` buscaba `[role="gridcell"]` en todo el DOM, encontrando contactos y grupos del sidebar en vez de opciones del bot. Ahora los gridcells y listItems solo se buscan dentro de popups (`[role="dialog"]`) o del chat activo (`#main`).
- **Reloj de próximo disparo no se actualizaba al cambiar la hora**: el scheduler quedaba atrapado en un `Task.Delay` largo y no reaccionaba a cambios de configuración. Ahora usa un `CancellationTokenSource` que se interrumpe al guardar en `/sistema`, recalculando inmediatamente el próximo disparo, la cuenta regresiva y la hora de pre-carga.
- **Selectores de radio buttons**: corregida selección de horarios en popup — ahora usa `aria-label` del `[role="radio"]` en vez de `textContent` (que contenía texto del SVG, no el horario). Agregado fallback por `[role="gridcell"]` con búsqueda del radio asociado en la misma fila.
- **Botón de enviar popup**: corregida detección del botón verde de envío — ahora busca por `data-icon="wds-ic-send-filled"` (independiente del idioma) antes de caer a selectores por texto.
- **FindFirstAvailableOptionAsync**: corregida selección del primer horario disponible para usar `aria-label` del radio button.
- **LogVisibleOptionsAsync**: mejorado logging de debug para mostrar `aria-label` de radio buttons y texto de `gridcell` en popups abiertos.
- **README**: corregido nombre del proyecto `court-daemon` → `PlatzDaemon` en instrucciones de instalación y estructura del proyecto.
- **README**: agregado comando alternativo con `powershell` para instalar Playwright.

## [1.3.0] - 2026-02-22

### Added
- **Suite de tests**: 98 tests unitarios con xUnit + NSubstitute cubriendo modelos, servicios y páginas (67.8% line coverage, 81% method coverage).
- **CI pipeline**: GitHub Actions workflow (`ci.yml`) que ejecuta build + tests + reporte de cobertura en cada push a `main` y pull request.
- **IConfigStore**: interfaz extraída de `ConfigStore` para permitir mocking en tests.
- **CalculateNextTrigger**: lógica de cálculo del próximo disparo extraída como método testeable.
- **Badges**: badges de cobertura y tests en el README.
- **Coverage**: archivo `coverage.runsettings` para excluir código no-testeable (Playwright, Razor views, Program.cs) del reporte de cobertura.

### Changed
- **Release pipeline**: ahora ejecuta todos los tests antes de publicar el EXE. Si fallan, el release se cancela.
- **Release pipeline**: `dotnet publish` apunta explícitamente a `PlatzDaemon.csproj` para evitar publicar el proyecto de tests.
- **Visibilidad de helpers**: `FormatTimeSpan`, `EscapeXml`, `MigrateConfig` e `IsBrowserClosedException` cambiados a `internal` para testing vía `InternalsVisibleTo`.
- **Documentación**: sección de tests agregada al README, estructura del proyecto actualizada.

### Fixed
- **Countdown post-ejecución**: el reloj del próximo disparo quedaba en `00:00:00` después de ejecutarse. Ahora calcula inmediatamente el disparo de mañana y actualiza el dashboard vía SignalR.
- **Documentación**: agregadas instrucciones concretas para desactivar la suspensión automática de Windows (Configuración > Energía y suspensión > "Nunca") en README y DOCS.

## [1.2.0] - 2026-02-22

### Added
- **CHANGELOG.md**: archivo de changelog con formato [Keep a Changelog](https://keepachangelog.com/) para documentar versiones.
- **Efecto CRT flicker**: animación sutil de parpadeo tipo monitor CRT en toda la interfaz (opacidad 97-100%, ciclo de 4s).

### Changed
- **Release workflow**: ahora extrae automáticamente las notas de la versión desde `CHANGELOG.md` y las incluye en el release de GitHub.

## [1.1.0] - 2026-02-22

### Added
- **Dashboard**: panel de resumen con la configuración de la reserva (horarios, canchas, tipo de juego, día).
- **Auto-recovery del navegador**: si Chromium se cierra durante una reserva, se reintenta automáticamente con un nuevo navegador.
- **Health check proactivo**: verifica que el navegador esté vivo antes de cada ejecución.
- **Aviso en página WhatsApp**: nota informativa sobre el modo visible de Chromium.

### Changed
- Chromium ahora siempre se ejecuta en modo **visible** (headful). WhatsApp Web bloquea navegadores headless.

### Fixed
- Corregida la detección de botones de periodo ("Turno mañana", "Turnos tarde", "Turnos noche") con selectores más robustos.
- Corregido banner ASCII que aún mostraba "CourtDaemon" en lugar de "PlatzDaemon".
- Corregida estética del checkbox desactivado (ahora respeta el tema retro verde/negro).
- Corregido overflow del texto "Desactivado" en la cuenta regresiva.

## [1.0.0] - 2026-02-21

### Added
- **Reserva automática** vía WhatsApp Web usando Playwright.
- **Modo competitivo**: pre-carga el mensaje 20 segundos antes y lo envía en el milisegundo exacto.
- **Prioridades configurables**: múltiples horarios y canchas en orden de preferencia.
- **Reintentos automáticos**: hasta 3 reintentos si la cancha es tomada por otro usuario.
- **Limpieza de conversaciones**: envía "menu" para resetear el bot antes de cada intento.
- **Dashboard en tiempo real**: logs con colores, estado del daemon, cuenta regresiva y estado de WhatsApp vía SignalR.
- **Notificaciones de escritorio**: toasts de Windows al confirmar o fallar la reserva (removido en versiones posteriores).
- **Sesión persistente**: datos de WhatsApp Web guardados en `Data/browser-data/`.
- **Interfaz retro**: tema de terminal verde sobre negro con `terminal.css`.
- **Configuración separada**: "Mi Reserva" (horarios, canchas, preferencias) y "Sistema" (DNI, hora de disparo, modo competitivo).
- **Detección de bloqueos**: maneja "Ya tiene turno reservado" y "No hay turnos disponibles".
- **Confirmación de nombre**: acepta automáticamente la verificación de identidad del bot.
- **Día de reserva**: opción "Hoy" o "Mañana" para clubes con anticipación.
- **Validación de horarios**: formato `HH:MMhs` con auto-formateo y detección de duplicados.
- **CI/CD**: GitHub Actions workflow para publicar releases automáticamente con tags.

[Unreleased]: https://github.com/francofgp/PlatzDaemon/compare/v1.6.0...HEAD
[1.6.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.9...v1.6.0
[1.5.9]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.8...v1.5.9
[1.5.8]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.7...v1.5.8
[1.5.7]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.6...v1.5.7
[1.5.6]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.5...v1.5.6
[1.5.5]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.4...v1.5.5
[1.5.4]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.3...v1.5.4
[1.5.3]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.2...v1.5.3
[1.5.2]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.1...v1.5.2
[1.5.1]: https://github.com/francofgp/PlatzDaemon/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.4.1...v1.5.0
[1.4.1]: https://github.com/francofgp/PlatzDaemon/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/francofgp/PlatzDaemon/releases/tag/v1.0.0
