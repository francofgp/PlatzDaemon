# UI y Frontend

## Stack

| Tecnologia | Uso |
|---|---|
| **ASP.NET Core Razor Pages** | Paginas server-rendered con code-behind (.cshtml + .cshtml.cs) |
| **terminal.css** | Libreria CSS de terceros que aplica un tema retro de terminal |
| **site.css** | Estilos propios que extienden terminal.css |
| **SignalR** | WebSocket para logs y estado en tiempo real |
| **JavaScript vanilla** | Logica del cliente (countdown, tags, validacion) |

No hay framework JS (React, Vue, etc.). Todo el JS es vanilla, inline en las paginas Razor dentro de `@section Scripts`.

---

## Layout compartido

**Archivo**: `Pages/Shared/_Layout.cshtml`

Define la estructura HTML comun a todas las paginas:

```html
<body class="terminal">          <!-- clase de terminal.css -->
  <div class="container">
    <header>
      <nav>                      <!-- Navbar con version dinamica -->
        <a>Platz Daemon v1.5.0</a>
        <ul>Dashboard | Mi Reserva | Sistema | WhatsApp</ul>
      </nav>
    </header>
    <main>@RenderBody()</main>   <!-- Contenido de cada pagina -->
    <footer>...</footer>
  </div>
  <script src="signalr.min.js"> <!-- CDN de SignalR -->
  <script src="site.js">        <!-- Auto-scroll de la consola -->
  @RenderSection("Scripts")      <!-- Scripts especificos de cada pagina -->
</body>
```

### Version dinamica

La navbar lee la version del assembly en runtime:

```csharp
v@(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?")
```

Esto refleja el `<Version>` del `.csproj`, evitando hardcodear.

### Navegacion activa

Cada link tiene logica para resaltar la pagina actual:

```csharp
class="menu-item @(ViewContext.RouteData.Values["page"]?.ToString() == "/Index" ? "active" : "")"
```

---

## Paginas

### Dashboard (`Pages/Index.cshtml` + `Index.cshtml.cs`)

La pagina principal. Tiene tres secciones:

**1. Panel de estado**
- Estado del daemon (IDLE/WAITING/RUNNING/COMPLETED/ERROR) con colores.
- Countdown al proximo disparo (actualizado cada segundo con JS).
- Ultimo resultado.
- Estado de WhatsApp (3 estados: Conectado, Sesion guardada, Desconectado).

**2. Acciones + resumen de config**
- Boton "Ejecutar ahora" → POST a `OnPostManualRunAsync`. Lanza la reserva en un `Task.Run` fire-and-forget y redirige.
- Boton "Limpiar logs" → POST a `OnPostClearLogs`. Limpia el buffer del `LogStore`.
- Resumen de la reserva configurada (tipo de juego, periodo, horarios, canchas).

**3. Terminal de logs**
- Renderiza el historial de logs al cargar la pagina (server-side).
- Los nuevos logs llegan via SignalR y se agregan al DOM en real-time.

#### SignalR en el Dashboard

Dos handlers:

```javascript
// Logs en tiempo real
connection.on("ReceiveLog", (time, prefix, message, cssClass) => {
    // Crea un <div> y lo agrega al logConsole
});

// Actualizaciones de estado
connection.on("StatusUpdate", (status, result, nextRunIso) => {
    // Actualiza status, resultado, y countdown
});
```

#### Countdown

El countdown es un timer JS (`setInterval(1000)`) que calcula la diferencia entre `Date.now()` y `currentNextRun`. Se actualiza reactivamente: cuando llega un `StatusUpdate` con un nuevo `nextRunIso`, el countdown se recalcula automaticamente.

### Mi Reserva (`Pages/Config.cshtml` + `Config.cshtml.cs`)

Configuracion de los parametros que el usuario cambia frecuentemente.

**Campos:**
- Periodo preferido (select: Mañana/Tarde/Noche)
- Tipo de juego (select: Single/Doble)
- Dia de reserva (select: Hoy/Mañana)
- Horarios prioritarios (tag list)
- Canchas prioritarias (tag list)

#### Tag system

Los horarios y canchas usan un sistema de tags custom (no hay componente de terminal.css para esto):

- Cada tag es un `<div class="tag">` con un `<input type="hidden">` que lleva el valor.
- El boton X (`tag-remove`) hace `this.parentElement.remove()`.
- Al submit, ASP.NET Core bindea los hidden inputs como `List<string>`.
- Se agregan tags con JS (`addTag`) que crea el HTML y lo inserta.

#### Validacion de horarios

`parseTimeSlot` en JS valida y normaliza la entrada:
- `18:00` → `18:00hs` (agrega suffix).
- `8:00` → `08:00hs` (padea hora).
- `25:00` → rechazado (hora invalida).
- `18:60` → rechazado (minutos invalidos).
- Detecta duplicados revisando los hidden inputs existentes.

#### Merge parcial

`ConfigModel.OnPostAsync` carga el config completo, actualiza solo `PreferredPeriod`, `GameType`, `BookingDay`, `PreferredTimeSlots`, `PreferredCourts`, y guarda. Los campos de sistema (`Dni`, `TriggerTime`, etc.) no se tocan.

### Sistema (`Pages/Sistema.cshtml` + `Sistema.cshtml.cs`)

Configuracion del sistema que rara vez cambia.

**Campos:**
- Automatizacion habilitada (checkbox).
- Hora de disparo (input time, formato HH:mm).
- Modo competitivo (checkbox).
- Numero del bot (input text).
- DNI del socio (input text).

#### NotifyConfigChanged

Al guardar, `SistemaModel.OnPostAsync` llama a `_scheduler.NotifyConfigChanged()`. Esto cancela el `Task.Delay` del scheduler, forzando un recalculo inmediato del proximo disparo. Sin esto, el countdown no se actualizaria hasta que el delay actual termine (potencialmente horas).

### WhatsApp (`Pages/Session.cshtml` + `Session.cshtml.cs`)

Gestion de la sesion del navegador.

**Estados visuales:**
- CONECTADO (verde): browser abierto + sesion verificada.
- NAVEGADOR ABIERTO (amarillo): browser abierto, pendiente de verificacion.
- SESION GUARDADA (amarillo): browser cerrado pero hay datos en `Data/browser-data/`.
- DESCONECTADO (rojo): sin browser ni datos guardados.

**Acciones:**
- Conectar WhatsApp → `OnPostConnectAsync` → `_whatsApp.OpenSessionForQrScanAsync()`.
- Verificar sesion → `OnPostCheckSessionAsync` → `_whatsApp.CheckSessionAsync()`.
- Desconectar → `OnPostDisconnectAsync` → `_whatsApp.CloseSessionAsync()`.

Los botones se muestran condicionalmente:
- Si no hay browser abierto → solo "Conectar".
- Si hay browser abierto → "Verificar sesion" y "Desconectar".

---

## CSS

### terminal.css

Libreria de terceros (`wwwroot/css/terminal.min.css`) que provee el tema base. No se modifica.

Incluye componentes como: `.terminal-card`, `.terminal-alert`, `.btn`, `.terminal-nav`, `.terminal-menu`, `.container`, tablas, formularios.

### site.css

`wwwroot/css/site.css` extiende terminal.css con:

**Variables del tema (override de terminal.css):**

```css
:root {
    --background-color: #0a0a0a;   /* Negro profundo */
    --font-color: #33ff33;          /* Verde fosforo */
    --primary-color: #33ff33;
    --error-color: #ff3333;
    /* ... */
}
```

**Fixes para terminal.css:**
- `select, option` — terminal.css no les aplica el tema.
- `input[type=time]` — tampoco esta estilizado en terminal.css.
- `.terminal-card > header` — usa primary en vez de secondary para mejor contraste.

**Componentes propios (no existen en terminal.css):**

| Componente | Uso |
|---|---|
| `.log-console` | Consola de logs con scroll, 400px de alto |
| `.log-entry`, `.log-info/success/warning/error` | Entradas de log con colores por nivel |
| `.status-panel`, `.status-row`, `.status-item` | Panel de estado del dashboard |
| `.countdown` | Timer grande con letter-spacing |
| `.tag`, `.tag-list`, `.tag-remove`, `.tag-input-group` | Sistema de tags para config |
| `.session-dot` | Circulo de estado (verde/amarillo/rojo) |
| `.ascii-art` | Estilo para el banner ASCII del dashboard |
| `.config-summary` | Resumen de config en el dashboard |

**Efectos CRT (cosmeticos):**
- **Phosphor glow**: `text-shadow: 0 0 2px #33ff3366` en todo el body.
- **Scanlines**: pseudo-elemento `::after` con gradiente lineal repetido.
- **Vignette**: pseudo-elemento `::before` con `box-shadow` inset.
- **Flicker**: animacion de opacidad 97-100% en ciclo de 4 segundos.
- **Scrollbar verde**: estilizado para Webkit en `.log-console`.

**Responsive:**
- A 768px o menos: status panel en columna, countdown mas chico, log console de 300px, ASCII art de 7px.

---

## JavaScript

### site.js

Solo hace auto-scroll de la consola de logs al cargar:

```javascript
document.addEventListener('DOMContentLoaded', function () {
    const logConsole = document.getElementById('logConsole');
    if (logConsole) logConsole.scrollTop = logConsole.scrollHeight;
});
```

### JS inline por pagina

Cada pagina tiene su JS dentro de `@section Scripts`:

- **Index**: conexion SignalR, handlers de `ReceiveLog` y `StatusUpdate`, countdown timer, `escapeHtml`.
- **Config**: `addTag`, `addCourt`, `addTimeSlot`, `parseTimeSlot`, handlers de Enter, limpieza de errores.

No hay JS en las paginas Sistema y Session (son formularios HTML puros).

---

## Como agregar una nueva pagina

1. Crear `Pages/NuevaPagina.cshtml`:

```razor
@page
@model PlatzDaemon.Pages.NuevaPaginaModel
@{
    ViewData["Title"] = "Nueva Pagina";
}

<h2>/// Nueva Pagina</h2>
<!-- contenido -->
```

2. Crear `Pages/NuevaPagina.cshtml.cs`:

```csharp
namespace PlatzDaemon.Pages;

public class NuevaPaginaModel : PageModel
{
    public void OnGet() { }
}
```

3. Agregar a la navbar en `Pages/Shared/_Layout.cshtml`:

```html
<li><a asp-page="/NuevaPagina" class="menu-item @(...)">Nueva</a></li>
```

4. Si necesita SignalR, agregar un `@section Scripts` con la conexion y handlers.

5. Si necesita estilos custom, agregarlos en `wwwroot/css/site.css` (nunca en `terminal.min.css`).
