# Platz Daemon — Documentacion tecnica

Documentacion interna para desarrolladores. Si sos usuario final, consulta [DOCS.md](../DOCS.md) o el [README principal](../README.md).

---

## Mapa de documentacion

| Documento | Descripcion |
|---|---|
| [architecture.md](architecture.md) | Arquitectura del sistema, componentes, diagrama end-to-end, patron singleton, flujo de datos |
| [getting-started.md](getting-started.md) | Onboarding para devs nuevos: setup, estructura del proyecto, por donde empezar a leer el codigo, glosario |
| [services-deep-dive.md](services-deep-dive.md) | Analisis detallado de cada servicio: responsabilidad, estado interno, metodos, decisiones de diseno |
| [playwright-guide.md](playwright-guide.md) | Integracion de Playwright: como funciona, como debuggear, selectores, como extenderlo |
| [ui-and-frontend.md](ui-and-frontend.md) | Razor Pages, SignalR, CSS, JavaScript: cada pagina, componentes, como agregar UI nueva |
| [booking-flow.md](booking-flow.md) | Flujo completo de reserva paso a paso: logica, reintentos, recovery, multi-periodo |
| [scheduler-and-timing.md](scheduler-and-timing.md) | BackgroundService, calculo de timezone, precision timing, modo competitivo |
| [configuration.md](configuration.md) | Modelo de configuracion, persistencia JSON, merge parcial, migracion, como agregar campos |
| [testing.md](testing.md) | Suite de tests: stack, patrones, coverage, que esta cubierto y que no |
| [ci-cd.md](ci-cd.md) | GitHub Actions: CI pipeline, release pipeline, como publicar una nueva version |
| [maintenance.md](maintenance.md) | Puntos fragiles, troubleshooting avanzado, mantenimiento periodico |
| [modification-guide.md](modification-guide.md) | Guia rapida: "quiero cambiar X, toco Y" |

---

## Orden de lectura sugerido

Si sos nuevo en el proyecto, lee en este orden:

1. **[getting-started.md](getting-started.md)** — Setup del entorno y vision general de la estructura.
2. **[architecture.md](architecture.md)** — Como encajan las piezas.
3. **[booking-flow.md](booking-flow.md)** — El flujo core de la aplicacion.
4. **[services-deep-dive.md](services-deep-dive.md)** — Detalles de cada servicio.
5. **[configuration.md](configuration.md)** — Como se persiste y lee la configuracion.
6. **[playwright-guide.md](playwright-guide.md)** — La parte mas compleja: automatizacion del navegador.
7. **[scheduler-and-timing.md](scheduler-and-timing.md)** — Como se programa la ejecucion.
8. **[ui-and-frontend.md](ui-and-frontend.md)** — La capa de presentacion.
9. **[modification-guide.md](modification-guide.md)** — Referencia rapida para cuando necesites hacer un cambio.

Los demas documentos ([testing.md](testing.md), [ci-cd.md](ci-cd.md), [maintenance.md](maintenance.md)) son de referencia y se consultan cuando se necesitan.

---

## Links utiles

- [README.md](../README.md) — Descripcion general, descarga, instrucciones de uso e instalacion.
- [DOCS.md](../DOCS.md) — Documentacion completa para usuarios finales (FAQ, troubleshooting, configuracion).
- [CHANGELOG.md](../CHANGELOG.md) — Historial de cambios por version.
