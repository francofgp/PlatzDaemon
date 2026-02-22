# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/),
y este proyecto adhiere a [Semantic Versioning](https://semver.org/lang/es/).

## [Unreleased]

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

[Unreleased]: https://github.com/francofgp/PlatzDaemon/compare/v1.3.0...HEAD
[1.3.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/francofgp/PlatzDaemon/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/francofgp/PlatzDaemon/releases/tag/v1.0.0
