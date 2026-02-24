# Plan: ¿Se recalcula la prevención de suspensión al cambiar la hora de disparo?

## Pregunta

Cuando el usuario cambia la **hora de disparo** (o deshabilita la automatización) en la pantalla Sistema y guarda, ¿el SleepPreventionService recalcula si debe activar o liberar la inhibición?

## Flujo actual

1. **Usuario guarda en Sistema**  
   [Pages/Sistema.cshtml.cs](Pages/Sistema.cshtml.cs) `OnPostAsync()`:
   - Actualiza `cfg.TriggerTime` (y otros campos).
   - Llama `_configStore.SaveAsync(cfg)`.
   - Llama `_scheduler.NotifyConfigChanged()`.

2. **ConfigStore**  
   [Services/ConfigStore.cs](Services/ConfigStore.cs) `SaveAsync()`:
   - Asigna `_cachedConfig = config` (memoria).
   - Escribe en disco.  
   `Get()` devuelve siempre `_cachedConfig` (no lee disco en cada lectura).

3. **SleepPreventionService**  
   [Services/SleepPreventionService.cs](Services/SleepPreventionService.cs) `ExecuteAsync()`:
   - Cada **PollIntervalMinutes** (default 5) ejecuta:
     - `nextRun = _scheduler.GetNextScheduledRun()`
     - `now = _scheduler.GetNowArgentina()`
     - `shouldInhibit = nextRun dentro de _hoursAhead`
     - `ActivateInhibitionAsync()` o `ReleaseInhibitionAsync()`.
   - No recibe ninguna señal cuando el usuario guarda en Sistema; solo hace polling.

4. **GetNextScheduledRun()**  
   [Services/BookingSchedulerService.cs](Services/BookingSchedulerService.cs):
   - Llama `_configStore.Get()` → obtiene la config **actual en memoria**.
   - Calcula el próximo disparo con `config.Enabled` y `config.TriggerTime`.

## Conclusión

**Sí se recalcula**, porque:

- Al guardar en Sistema, `ConfigStore._cachedConfig` se actualiza de inmediato.
- En el siguiente ciclo del loop de SleepPreventionService (como máximo a los **5 minutos**), `GetNextScheduledRun()` usa `_configStore.Get()` y obtiene la config nueva.
- Con esa config se recalcula `nextRun` y por tanto `shouldInhibit`, y se activa o libera la inhibición según corresponda.

**Retraso máximo:** hasta **PollIntervalMinutes** (por defecto 5). Ejemplos:

- Cambio de 08:00 a 20:00: en el próximo ciclo se usa la nueva hora y se mantiene o se activa/desactiva según la ventana de 24 h.
- Deshabilitar automatización: en el próximo ciclo `GetNextScheduledRun()` devuelve `null` y se llama a `ReleaseInhibitionAsync()`.

## ¿Hace falta recalcular al instante?

- **Comportamiento actual:** recálculo en ≤ 5 min. Para el uso típico (cambiar hora de disparo o deshabilitar) suele ser aceptable.
- **Si se quisiera recálculo inmediato** al guardar en Sistema, habría que:
  - Hacer que SleepPreventionService reaccione a un “config changed” (por ejemplo que `NotifyConfigChanged()` también dispare un ciclo del servicio), o
  - Introducir un `CancellationTokenSource` en SleepPreventionService que se cancele al guardar, para interrumpir el `Task.Delay` y ejecutar un ciclo en seguida.

Eso añade acoplamiento y complejidad. Recomendación: **dejar el diseño actual** (polling cada 5 min) salvo que se detecte un caso donde 5 min de retraso sea un problema.

## Resumen

| Pregunta | Respuesta |
|----------|-----------|
| ¿Se recalcula la suspensión al cambiar la hora de disparo? | **Sí**, en el siguiente ciclo del loop (≤ 5 min). |
| ¿De dónde sale la config? | `ConfigStore.Get()` (actualizado en memoria al guardar en Sistema). |
| ¿Retraso? | Hasta **PollIntervalMinutes** (default 5). |
| ¿Cambio de diseño necesario? | No; el flujo es correcto. Opcional: recálculo inmediato si en el futuro se requiere.
