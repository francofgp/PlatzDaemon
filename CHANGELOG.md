# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

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
- **Limpieza de conversaciones**: envía "Salir" para resetear el bot antes de cada intento.
- **Dashboard en tiempo real**: logs con colores, estado del daemon, cuenta regresiva y estado de WhatsApp vía SignalR.
- **Notificaciones de escritorio**: toasts de Windows al confirmar o fallar la reserva.
- **Sesión persistente**: datos de WhatsApp Web guardados en `Data/browser-data/`.
- **Interfaz retro**: tema de terminal verde sobre negro con `terminal.css`.
- **Configuración separada**: "Mi Reserva" (horarios, canchas, preferencias) y "Sistema" (DNI, hora de disparo, modo competitivo).
- **Detección de bloqueos**: maneja "Ya tiene turno reservado" y "No hay turnos disponibles".
- **Confirmación de nombre**: acepta automáticamente la verificación de identidad del bot.
- **Día de reserva**: opción "Hoy" o "Mañana" para clubes con anticipación.
- **Validación de horarios**: formato `HH:MMhs` con auto-formateo y detección de duplicados.
- **CI/CD**: GitHub Actions workflow para publicar releases automáticamente con tags.

[Unreleased]: https://github.com/francofgp/PlatzDaemon/compare/v1.4.0...HEAD
[1.4.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.3.0...v1.4.0
[1.3.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/francofgp/PlatzDaemon/releases/tag/v1.0.0
