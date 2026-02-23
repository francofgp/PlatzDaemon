# Mantenimiento y puntos fragiles

## Puntos fragiles del sistema

### 1. Selectores de WhatsApp Web

**Riesgo: ALTO**

WhatsApp Web actualiza su frontend sin previso. Cuando cambian selectores CSS, atributos `data-testid`, o la estructura del DOM, la automatizacion deja de funcionar.

**Mitigacion actual:**
- Multiples selectores de fallback para cada operacion (6 estrategias de conteo, 10+ selectores de sesion, 3 pasadas de busqueda de botones).
- `LogVisibleOptionsAsync` para diagnosticar rapidamente que cambio.

**Que hacer cuando falla:**
1. Revisar los logs del dashboard — el ultimo log antes del error indica que paso se rompio.
2. Abrir WhatsApp Web manualmente en Chrome y usar DevTools (F12) para encontrar los nuevos selectores.
3. Agregar los nuevos selectores como fallback adicional — no eliminar los viejos (pueden volver en otra version).
4. Testear con una ejecucion manual desde el dashboard.

**Senales de que los selectores se rompieron:**
- `NOT_FOUND:class:msgs=0:recent=0` en los logs (no encuentra mensajes).
- `No se pudo detectar sesion activa con ninguno de los selectores conocidos`.
- `No se encontro el boton '...' en mensajes recientes`.
- El conteo de mensajes siempre es 0.

### 2. JavaScript inline en C#

**Riesgo: MEDIO**

~15 bloques de JavaScript estan embebidos como strings C# (`@"..."`) en `WhatsAppAutomationService`. Problemas:

- **No hay type-checking**: errores de JS solo se detectan en runtime.
- **No hay test coverage**: el JS se ejecuta en el browser de Playwright, imposible de testear con xUnit.
- **Dificil de mantener**: el JS esta indentado dentro de strings C#, sin syntax highlighting ni linting.
- **Escapado de comillas**: las dobles comillas en JS deben escribirse como `""` dentro de `@"..."`.

**Mitigacion:**
- Los bloques de JS estan bien documentados con comentarios.
- Cada bloque tiene un proposito especifico y delimitado.

**Alternativa considerada:** extraer el JS a archivos `.js` separados y cargarlos con `page.AddScriptTagAsync`. No se implemento porque:
- Agrega complejidad de deployment (los archivos JS tienen que estar disponibles en la ruta correcta).
- El JS necesita acceso a variables C# (parametros de busqueda), lo cual requiere interpolacion.

### 3. Keywords de deteccion en espanol

**Riesgo: MEDIO**

La deteccion de estados del bot se basa en buscar textos en espanol hardcodeados:

```javascript
text.includes('ya tiene') && text.includes('turno')
text.includes('no hay turnos disponibles')
text.includes('confirmad')
text.includes('documento') || text.includes('dni')
text.includes('a nombre de')
text.includes('cómo puedo ayudarte')
```

**Si el bot del club cambia su texto:**
- Los detectores dejan de funcionar.
- El flujo se puede colgar esperando una respuesta que ya llego pero no se reconocio.

**Mitigacion:**
- Los keywords son genericos ("turno", "cancha", "confirmar") y cubren variaciones comunes.
- El sistema tiene timeouts para cada paso, asi que nunca se cuelga indefinidamente.

**Para adaptar a otro bot:**
- Buscar todas las ocurrencias de `text.includes(` en `WhatsAppAutomationService.cs`.
- Ajustar los keywords segun los mensajes del nuevo bot.

### 4. SemaphoreSlim global

**Riesgo: BAJO-MEDIO**

`WhatsAppAutomationService` usa un unico `SemaphoreSlim(1,1)` para todas las operaciones. Si una operacion se cuelga dentro del lock (ej: un timeout de Playwright de 30 segundos), todas las demas operaciones quedan bloqueadas hasta que se libere.

**Escenario problematico:**
1. `ExecuteBookingAsync` adquiere el lock.
2. Un `WaitForSelectorAsync` con timeout de 30 segundos se cuelga.
3. El usuario intenta "Verificar sesion" desde la UI → queda esperando el lock.
4. Despues de 30 segundos, el timeout libera el lock.

**Mitigacion:**
- Todos los metodos de Playwright tienen timeouts configurados (10s-30s).
- El lock se libera en `finally`, asi que siempre se libera aunque haya excepciones.

### 5. Datos de browser corrompidos

**Riesgo: BAJO**

Si `Data/browser-data/` se corrompe (ej: crash durante escritura, cierre abrupto de la app), Chromium podria no arrancar o WhatsApp Web podria no cargar la sesion.

**Sintomas:**
- "Error abriendo navegador" en los logs.
- Chromium se abre pero muestra pagina en blanco.
- WhatsApp Web pide escanear QR aunque antes funcionaba.

**Solucion:**
1. Cerrar la aplicacion.
2. Borrar la carpeta `Data/browser-data/`.
3. Re-abrir la app.
4. Escanear QR nuevamente desde la pagina WhatsApp.

### 6. Zona horaria hardcodeada

**Riesgo: BAJO**

Toda la logica de scheduling esta hardcodeada a Argentina (UTC-3). Si se quisiera usar en otro pais:
- La hora de disparo no coincidiria con la hora local.
- El countdown mostraria la hora argentina, no la local.

**Para cambiar la zona horaria:**
- Modificar `ResolveArgentinaTimeZone()` en `BookingSchedulerService` para usar la zona deseada.
- O hacer el timezone configurable agregando un campo a `BookingConfig`.

---

## Troubleshooting avanzado

### "Driver not found: D:\.playwright\node\win32_x64\node.exe"

Este error ocurre si el EXE fue publicado con `IncludeNativeLibrariesForSelfExtract=true`. Esa propiedad empaqueta los binarios nativos (incluyendo el driver Node.js de Playwright) dentro del EXE single-file. Playwright espera encontrar el driver en disco, no embebido.

**Solucion:**
1. Verificar que `PlatzDaemon.csproj` **no** tenga `<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>`.
2. Verificar que `release.yml` **no** pase `-p:IncludeNativeLibrariesForSelfExtract=true` al `dotnet publish`.
3. Re-publicar. El directorio de publish tendra el EXE + `.playwright/` + otros archivos nativos al lado.

Si ya tenes un EXE publicado con el bug: descargar la version corregida desde Releases.

### "Playwright install retorno codigo X" (distinto de 0)

La auto-instalacion de Chromium fallo. Posibles causas:
- Sin conexion a internet (la primera vez necesita descargar ~100 MB).
- Firewall o proxy bloqueando la descarga.
- Permisos insuficientes en el directorio de destino.

Intentar ejecutar manualmente: `pwsh playwright.ps1 install chromium` desde la carpeta del EXE.

### El bot no responde despues de "turno"

1. Verificar que el numero del bot sea correcto (sin +, con codigo de pais).
2. Enviar "turno" manualmente al bot desde el celular para verificar que funcione.
3. Revisar los logs: el conteo de `message-in` deberia incrementar. Si no incrementa, el conteo puede estar fallando.
4. Si el conteo siempre es 0, los selectores de mensaje cambiaron. Inspeccionar el DOM con DevTools.

### El countdown no se actualiza despues de cambiar la hora

Verificar que `SistemaModel.OnPostAsync` llame a `_scheduler.NotifyConfigChanged()`. Este metodo cancela el `Task.Delay` del scheduler y fuerza un recalculo.

### "Timeout buscando 'Hoy' despues de 30 intentos"

El bot no envio las opciones de dia, o las envio con un texto diferente al esperado. Revisar `LogVisibleOptionsAsync` en los logs para ver que opciones ve el browser.

### Chromium consume mucha memoria

Comportamiento normal de Chromium. El persistent context acumula datos con el tiempo. Para limpiar:
1. Cerrar la app.
2. Borrar `Data/browser-data/`.
3. Re-abrir y escanear QR.

### La reserva se ejecuta pero selecciona el horario incorrecto

Revisar el orden de `PreferredTimeSlots` en `Data/config.json`. El sistema intenta el primero, luego el segundo, etc. Si el primer horario esta disponible, siempre lo selecciona.

### "Error: Sesion no disponible" aunque antes funcionaba

WhatsApp Web desvincula dispositivos despues de ~14 dias de inactividad. Soluciones:
- Re-escanear QR desde la pagina WhatsApp.
- Verificar en el celular que "Dispositivos vinculados" sigue mostrando una sesion web activa.

### Los tests fallan en CI pero pasan localmente

Posibles causas:
- **Timezone**: algunos tests de `CalculateNextTrigger` dependen de la hora. El CI corre en UTC, local en UTC-3.
- **Paths**: los tests de `ConfigStore` usan `Path.GetTempPath()`, que difiere entre Windows y Linux.
- **Paralelismo**: xUnit corre tests en paralelo por defecto. Si dos tests escriben al mismo temp dir, pueden interferir. Cada test crea su propio `Guid`-based temp dir para evitar esto.

---

## Mantenimiento periodico

### Actualizar Playwright

Cuando WhatsApp Web deja de funcionar (selectores rotos), puede ser necesario actualizar Playwright a una version mas nueva:

1. Actualizar el NuGet:
   ```bash
   dotnet add package Microsoft.Playwright --version <nueva-version>
   ```
2. Re-instalar browsers:
   ```bash
   dotnet build
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```
3. Testear que todo sigue funcionando.

### Verificar selectores

Periodicamente (~cada 1-2 meses), abrir WhatsApp Web manualmente y verificar con DevTools que los selectores principales sigan existiendo:
- `[data-testid='chat-list']` o `div#pane-side` (sesion activa).
- `[class*="message-in"]` (mensajes entrantes).
- `[data-icon="list-msg-icon"]` (boton de popup de lista).
- `[role="radio"]` (opciones en popups de lista).

### Actualizar .NET SDK

El proyecto usa .NET 10. Cuando salgan nuevas versiones:
1. Actualizar `TargetFramework` en ambos `.csproj`.
2. Actualizar `dotnet-version` en `ci.yml` y `release.yml`.
3. Verificar que el build y los tests pasen.

### Limpiar browser data

Si la app lleva mucho tiempo corriendo, `Data/browser-data/` puede crecer. Borrar y re-escanear QR para liberar espacio:
```bash
rm -rf Data/browser-data/
```
