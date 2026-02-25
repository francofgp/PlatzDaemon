# Abrir chat del bot por URL directa

## Cómo funciona

En `WhatsAppAutomationService.cs`, el flujo abre el chat **por URL directa**:

- `OpenBotChatAsync(page, phoneNumber, countryCode)` navega a `https://web.whatsapp.com/send?phone={normalizado}`.
- Luego espera el selector del cuadro de texto (compose box) con timeout 20 s.
- No se busca el chat en la UI; se abre directamente la conversación.

## Normalización (`NormalizePhoneForWhatsAppUrl`)

El helper `NormalizePhoneForWhatsAppUrl(phoneNumber, countryCode)` normaliza el número al formato internacional que WhatsApp espera:

Pasos internos:
1. Extraer solo dígitos.
2. Quitar el `0` inicial (prefijo troncal de discado nacional, ej: `0353...`).
3. Aplicar reglas por cantidad de dígitos.

| Input del usuario | Dígitos tras strip | Resultado | Regla |
|-------------------|--------------------|-----------|-------|
| `5493534090496` | `5493534090496` (13) | `5493534090496` | Ya internacional, sin cambios |
| `93534090496` | `93534090496` (11) | `5493534090496` | Empieza con 9, prepend countryCode |
| `3534090496` | `3534090496` (10) | `5493534090496` | Area + número, prepend countryCode + 9 |
| `03534090496` | `3534090496` (10) | `5493534090496` | Strip 0 troncal, luego area + número |
| `+54 9 353-409-0496` | `5493534090496` (13) | `5493534090496` | Se extraen solo dígitos, ya internacional |

El `countryCode` se lee de `BookingConfig.CountryCode` (default `"54"`, Argentina). Si el día de mañana se necesita otro país, basta cambiar ese valor en `config.json`.

## Posibles causas de timeout

1. **Formato del número incorrecto**: si no se normaliza bien, WhatsApp Web no abre la conversación.
2. **Timeout fijo de 20 s**: en conexiones lentas puede no alcanzar.
3. **Selector del compose box**: si WhatsApp Web cambia el DOM (`data-testid` o `data-tab`).

## Resumen

- **URL directa:** `https://web.whatsapp.com/send?phone={numero_normalizado}`.
- **Default del número del bot:** `5493534407576` (ya normalizado, funciona de entrada).
- **Log:** "Abriendo chat del bot por URL (5493534090496)..." muestra el número ya normalizado.
- **Código de país configurable:** `BookingConfig.CountryCode` (default `"54"`).
