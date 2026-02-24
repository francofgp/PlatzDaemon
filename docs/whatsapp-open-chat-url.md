# Plan: Abrir chat del bot por URL directa (y por qué ya lo hacemos)

## Lo que reportó tu amigo

- Log: "Buscando chat del bot (93534407576)..." y luego **Timeout 20000ms** esperando el cuadro de escribir.
- Sensación: "no se está abriendo WhatsApp para el número del bot" y que sería mejor "abrir directamente la conversación" con ese número.

## Qué hace el código hoy

En [Services/WhatsAppAutomationService.cs](Services/WhatsAppAutomationService.cs) el flujo **ya es por URL directa**:

- `OpenBotChatAsync(page, config.BotPhoneNumber)` (línea ~340).
- Dentro (líneas 668–691):
  - `var chatUrl = $"https://web.whatsapp.com/send?phone={phoneNumber}";`
  - `await page.GotoAsync(chatUrl, ...);`
  - Luego se espera el selector del cuadro de texto (compose box) con timeout 20 s.

Es decir: **no se busca el chat en la UI**; se navega a `https://web.whatsapp.com/send?phone=XXXXXXXX` y se espera que cargue esa conversación. El mensaje de log "Buscando chat del bot" es engañoso porque suena a “búsqueda en la lista”, pero en realidad es “abriendo por URL”.

Formato de URL que usamos (y el que comentaste):  
`https://web.whatsapp.com/send?phone=5493534407576` (número en formato internacional, sin `+`).

## Por qué puede estar fallando (timeout)

1. **Formato del número en config**  
   En el log aparece `(93534407576)` → sin código de país. Si en la config está guardado así, la URL queda `...?phone=93534407576`. WhatsApp Web espera número en formato internacional (ej. Argentina: `54` + número). Con `93534407576` la página puede no abrir bien la conversación o tardar/cambiar de pantalla y el selector del compose box no aparece en 20 s.

2. **Timeout fijo de 20 s**  
   En conexiones lentas o con WhatsApp Web pesado, 20 s puede no alcanzar para que aparezca el cuadro de escribir.

3. **Selector del compose box**  
   Si WhatsApp Web cambia el DOM (`data-testid` o `data-tab`), el `WaitForSelectorAsync` puede no encontrar el elemento y disparar el timeout aunque la conversación sí se haya abierto.

## Plan de mejoras (sin cambiar la idea: seguir abriendo por URL)

| Acción | Objetivo |
|--------|----------|
| **1. Normalizar número para la URL** | Que la URL siempre use formato internacional. Ej.: si el número tiene 10 dígitos y empieza en `9` (típico celular Argentina), anteponer `54`. Así aunque en Sistema pongan "93534407576", se abra `send?phone=5493534407576`. Hacerlo en un helper y usarlo solo al construir `chatUrl`. |
| **2. Ajustar el mensaje de log** | Cambiar "Buscando chat del bot (XXX)..." por algo como "Abriendo chat del bot por URL (XXX)..." para que quede claro que se usa la URL directa y no una búsqueda en la lista. |
| **3. (Opcional) Timeout o reintento** | Subir el timeout (ej. 30 s) o hacer un único reintento si falla la primera vez, para dar margen en conexiones lentas. |

No hace falta “cambiar” a abrir por URL: **ya se abre por URL**. Lo que conviene es asegurar que el número vaya bien formateado en esa URL y que los logs y el comportamiento ante fallos reflejen eso (y, si querés, ser un poco más tolerantes al tiempo de carga).

## Resumen

- **¿Abrimos ya la conversación por URL?** Sí: `https://web.whatsapp.com/send?phone={numero}`.
- **¿Por qué el timeout?** Muy probablemente número sin código de país en la config (ej. `93534407576` en vez de `5493534407576`) o 20 s insuficientes para que cargue el compose box.
- **Qué hacer:** normalizar el número a formato internacional para la URL, actualizar el log y, opcionalmente, subir timeout o añadir un reintento.
