# Getting started (para desarrolladores)

## Requisitos

- **.NET 10 SDK** — [descargar](https://dotnet.microsoft.com/download)
- **Git**
- **Editor**: VS Code, Visual Studio, Rider, o cualquier editor con soporte C#.
- **WhatsApp** vinculado a un telefono (para probar la automatizacion).
- Conexion a internet (WhatsApp Web la necesita).

---

## Setup rapido

```bash
# 1. Clonar el repositorio
git clone <url-del-repositorio>
cd court-daemon

# 2. Restaurar dependencias y compilar
dotnet build

# 3. Instalar el navegador de Playwright (Chromium)
pwsh bin/Debug/net10.0/playwright.ps1 install chromium

# 4. Correr la aplicacion
dotnet run
```

La app se abre en `http://localhost:5203` (modo desarrollo) o `http://localhost:5000` (modo produccion).

> En Windows sin `pwsh`, usar: `powershell -ExecutionPolicy Bypass -File bin/Debug/net10.0/playwright.ps1 install chromium`

### Correr tests

```bash
dotnet test
```

---

## Estructura del proyecto

```
court-daemon/
├── Program.cs                        # Entry point: configura DI, Kestrel, middleware, auto-open browser
├── PlatzDaemon.csproj                # Proyecto .NET 10 Web SDK, referencia a Playwright
├── PlatzDaemon.slnx                  # Solution (incluye app + tests)
│
├── Models/                           # Modelos de datos (POCOs)
│   ├── BookingConfig.cs              #   Configuracion completa de la reserva y el sistema
│   ├── AppState.cs                   #   Estado del daemon (status, ultimo resultado, proximo disparo)
│   └── LogEntry.cs                   #   Entrada de log (timestamp, mensaje, nivel, formateo)
│
├── Services/                         # Logica de negocio (todos Singleton)
│   ├── WhatsAppAutomationService.cs  #   Automatizacion Playwright: controla Chromium, ejecuta el flujo de reserva
│   ├── BookingSchedulerService.cs    #   BackgroundService: espera la hora de disparo, lanza la reserva
│   ├── ConfigStore.cs                #   Persistencia de config en Data/config.json
│   ├── IConfigStore.cs               #   Interfaz de ConfigStore (para testing)
│   ├── LogStore.cs                   #   Buffer de logs en memoria + broadcast via SignalR
│   └── AppStateService.cs            #   Estado de la app + broadcast via SignalR
│
├── Hubs/
│   └── LogHub.cs                     # Hub de SignalR (minimal, solo define OnConnectedAsync)
│
├── Pages/                            # Razor Pages (UI)
│   ├── Shared/
│   │   └── _Layout.cshtml            #   Layout compartido: navbar, version, scripts SignalR
│   ├── _ViewImports.cshtml           #   Tag helpers globales
│   ├── _ViewStart.cshtml             #   Layout por defecto
│   ├── Index.cshtml / .cshtml.cs     #   Dashboard: estado, countdown, logs, ejecutar manualmente
│   ├── Config.cshtml / .cshtml.cs    #   Mi Reserva: periodo, horarios, canchas, tipo de juego
│   ├── Sistema.cshtml / .cshtml.cs   #   Sistema: habilitado, hora de disparo, modo competitivo, DNI
│   ├── Session.cshtml / .cshtml.cs   #   WhatsApp: conectar, verificar, desconectar sesion
│   └── Error.cshtml / .cshtml.cs     #   Pagina de error generica
│
├── wwwroot/                          # Archivos estaticos
│   ├── css/
│   │   ├── terminal.min.css          #   Libreria CSS de terceros (tema terminal retro)
│   │   └── site.css                  #   Estilos propios: log console, status panel, tags, CRT effects
│   └── js/
│       └── site.js                   #   Auto-scroll del log console al cargar
│
├── PlatzDaemon.Tests/                # Tests (xUnit + NSubstitute)
│   ├── Models/                       #   Tests de modelos
│   ├── Services/                     #   Tests de servicios
│   └── Pages/                        #   Tests de page models
│
├── Data/                             # (gitignored) Datos en runtime
│   ├── config.json                   #   Configuracion persistida
│   └── browser-data/                 #   Datos de sesion de Chromium
│
├── .github/workflows/                # CI/CD
│   ├── ci.yml                        #   Build + tests + coverage en push/PR
│   └── release.yml                   #   Build multi-plataforma + publish Release
│
├── coverage.runsettings              # Config de coverlet (exclusiones de coverage)
├── README.md                         # Documentacion publica (descarga, uso, desarrollo)
├── DOCS.md                           # Documentacion de usuario final
├── CHANGELOG.md                      # Historial de cambios
└── docs/                             # Esta documentacion tecnica
```

---

## Por donde empezar a leer el codigo

Si nunca viste el proyecto antes, este es el orden sugerido para entender el flujo:

### Paso 1: Entry point

Abri `Program.cs`. Son 59 lineas. Muestra como se configura todo: DI, middleware, SignalR, y el auto-open del browser. Presta atencion a que **todos los servicios son Singleton** y que `BookingSchedulerService` se registra tambien como `HostedService`.

### Paso 2: El modelo de datos

Abri `Models/BookingConfig.cs`. Es un POCO con 10 propiedades que representan toda la configuracion de la app. Cada propiedad tiene un default razonable. Este modelo es el contrato entre la UI, la persistencia y la automatizacion.

Revisa tambien `Models/AppState.cs` (5 propiedades, 1 enum con 5 estados) y `Models/LogEntry.cs` (timestamp + mensaje + nivel con formateo).

### Paso 3: Persistencia

Abri `Services/ConfigStore.cs`. Carga el JSON al startup, cachea en memoria, y escribe a disco en cada save. Nota el metodo `MigrateConfig` que normaliza datos viejos.

### Paso 4: El scheduler

Abri `Services/BookingSchedulerService.cs`. Es un `BackgroundService` con un loop infinito que:
1. Lee la config.
2. Calcula cuando disparar.
3. Espera (con `Task.Delay` o precision wait en modo competitivo).
4. Llama a `WhatsAppAutomationService.ExecuteBookingAsync()`.
5. Calcula el proximo disparo para manana.

### Paso 5: La automatizacion

Abri `Services/WhatsAppAutomationService.cs`. Es el archivo mas grande (~1900 lineas). El metodo central es `ExecuteBookingAsync`, que ejecuta todo el flujo de reserva: abrir browser, navegar a WhatsApp Web, enviar mensajes, clickear botones, manejar errores y reintentos.

No intentes leer todo de una. Enfocate primero en `ExecuteBookingAsync` (linea ~190) y segui el flujo paso a paso. Los metodos privados auxiliares se entienden en contexto.

---

## Glosario

| Termino | Significado |
|---|---|
| **Daemon** | La aplicacion completa corriendo como servidor local. No es un daemon Unix; es un proceso de consola con servidor web integrado. |
| **Disparo** | El momento exacto en que se ejecuta la reserva automatica. Configurado en "Hora de disparo" (ej: 08:00). |
| **Periodo** | Franja horaria del dia: Mañana, Tarde, Noche. El bot del club agrupa los horarios disponibles por periodo. |
| **Horario** | Un slot especifico (ej: `18:00hs`). El usuario configura una lista ordenada por prioridad. |
| **Cancha** | Un recurso reservable (ej: "Cancha Central", "Cancha 9"). Tambien con lista de prioridad. |
| **Modo competitivo** | Optimizacion de velocidad: pre-carga el mensaje 20 segundos antes del disparo y lo envia en el milisegundo exacto. |
| **Pre-arm** | Paso del modo competitivo donde se abre el chat del bot y se escribe "turno" sin enviarlo. |
| **Merge parcial** | Patron de persistencia donde cada pagina solo actualiza sus campos de `BookingConfig` sin sobreescribir los de la otra pagina. |
| **Persistent context** | Modo de Playwright que guarda cookies, localStorage y datos de sesion del navegador en disco (`Data/browser-data/`), evitando re-login. |
| **Headful** | Navegador con ventana visible (opuesto a headless). Obligatorio porque WhatsApp Web bloquea navegadores headless. |
| **Bot** | El numero de WhatsApp del club que responde automaticamente con menus de reserva. No es un bot de la app; es un servicio externo del club. |
