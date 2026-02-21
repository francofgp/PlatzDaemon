# 🎾 Platz Daemon

**Automatización de reservas de canchas de tenis vía WhatsApp**

Platz Daemon es una aplicación de escritorio que automatiza el proceso de reserva de canchas de tenis a través del bot de WhatsApp de tu club. Se ejecuta como un servidor local con interfaz web retro estilo terminal de los '90.

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-purple)
![Playwright](https://img.shields.io/badge/Playwright-1.51-green)
![Platform](https://img.shields.io/badge/Platform-Windows%20x64-blue)

---

## ¿Para qué sirve?

Si tu club de tenis utiliza un bot de WhatsApp para reservar canchas y los turnos se habilitan a una hora específica (ej: 8:00 AM), Platz Daemon se encarga de:

1. **Esperar** hasta la hora exacta en que se habilitan los turnos.
2. **Enviar** automáticamente los mensajes al bot de WhatsApp.
3. **Seleccionar** el horario, cancha y tipo de juego que configuraste.
4. **Confirmar** la reserva por vos.

Ya no necesitás despertarte a las 8 AM ni competir manualmente por las canchas.

---

## 📥 Descarga e instalación (no se necesita instalar nada)

> **Para usuarios que solo quieren usar la app.** No necesitás saber programar ni instalar herramientas. Solo seguí estos pasos.

### Paso 1 — Descargar

1. Entrá a la página de **Releases** del proyecto: [click acá para ir a Releases](../../releases/latest).
2. En esa página, bajá hasta la sección **Assets** (está abajo de todo).
3. Descargá el archivo **`PlatzDaemon-v1.0.0-win-x64.zip`** (o la versión más reciente).

> 💡 **¿Qué es esa página?** Es simplemente la página de descarga del programa. El archivo ZIP contiene todo lo necesario para ejecutarlo.

### Paso 2 — Extraer el ZIP

1. Buscá el archivo `.zip` que descargaste (generalmente queda en tu carpeta **Descargas**).
2. Hacé **click derecho** sobre el ZIP → **"Extraer todo..."** → Elegí una carpeta donde quieras guardar la app (ej: `C:\PlatzDaemon`).
3. Se va a crear una carpeta con varios archivos adentro.

### Paso 3 — Ejecutar

1. Abrí la carpeta que extrajiste.
2. Buscá el archivo **`PlatzDaemon.exe`** y hacé **doble click** para ejecutarlo.

> ⚠️ **Windows puede mostrar una advertencia** diciendo "Windows protegió tu equipo" (pantalla azul de SmartScreen). Esto pasa con cualquier programa nuevo descargado de internet. Para continuar:
> 1. Hacé click en **"Más información"**.
> 2. Hacé click en **"Ejecutar de todas formas"**.
> Esto solo pasa la primera vez.

3. Se va a abrir una **ventana de consola negra** (es normal, no la cierres) y automáticamente se abre tu **navegador** con la interfaz de Platz Daemon.

> 🌐 Si el navegador no se abre solo, abrí manualmente **http://localhost:5000** en Chrome, Edge, Firefox, o el navegador que uses.

### Paso 4 — Conectar tu WhatsApp

1. En la interfaz web, andá a la pestaña **"WhatsApp"**.
2. Hacé click en **"Conectar WhatsApp"** → se abre una ventana de navegador con WhatsApp Web.
3. En tu **celular**, abrí WhatsApp → ⋮ Menú → **"Dispositivos vinculados"** → **"Vincular un dispositivo"**.
4. Escaneá el **código QR** que aparece en la pantalla.
5. Esperá unos segundos y hacé click en **"Verificar sesión"** para confirmar.

> ✅ La sesión se guarda. **No necesitás escanear el QR cada vez** que abrís la app.

### Paso 5 — Configurar tu reserva

1. Ir a **"Mi Reserva"**: elegí tu periodo preferido (Mañana/Tarde/Noche), los horarios que querés, las canchas preferidas y el tipo de juego.
2. Ir a **"Sistema"**: poné tu DNI, el número del bot de WhatsApp del club, la hora a la que se habilitan los turnos, y si querés activar el modo competitivo.
3. Hacé click en **"Guardar"** en cada sección.

### Paso 6 — ¡Listo!

Dejá la computadora prendida (no suspendida). El programa se va a encargar de reservar la cancha automáticamente a la hora que configuraste. Podés ver el estado en tiempo real en el **Dashboard**.

> 💡 **Tip**: podés cerrar la pestaña del navegador tranquilo, el programa sigue corriendo. Podés volver a entrar a **http://localhost:5000** cuando quieras para ver cómo va. Lo que **no** tenés que cerrar es la ventana de consola negra.

### Resumen rápido

| Qué hacer | Cómo |
|---|---|
| **Descargar** | Ir a Releases → descargar el ZIP |
| **Instalar** | No se instala nada, solo extraer el ZIP |
| **Ejecutar** | Doble click en `PlatzDaemon.exe` |
| **Configurar** | Desde el navegador en `http://localhost:5000` |
| **Parar la app** | Cerrar la ventana de consola negra |

---

## 🔧 Para desarrolladores

> Las secciones siguientes son para usuarios técnicos que quieran compilar, modificar o contribuir al proyecto.

### Requisitos de desarrollo

- **Windows 10/11** (x64)
- **.NET 10 SDK** ([descargar](https://dotnet.microsoft.com/download))
- **WhatsApp** vinculado a tu teléfono
- Conexión a internet estable

### Compilar y ejecutar desde el código fuente

#### 1. Clonar e instalar dependencias

```bash
git clone <url-del-repositorio>
cd court-daemon
dotnet build
```

#### 2. Instalar navegador de Playwright

```bash
pwsh bin/Debug/net10.0-windows/win-x64/playwright.ps1 install chromium
```

> Si no tenés `pwsh`, usá `powershell` en su lugar.

#### 3. Ejecutar la aplicación

```bash
dotnet run
```

La aplicación se abre en `http://localhost:5000`.

### Publicar como EXE

Para generar un ejecutable distribuible (self-contained, no requiere .NET instalado):

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

El resultado queda en `bin/Release/net10.0-windows/win-x64/publish/`. Para distribuir, comprimir esa carpeta en un ZIP y subirla a GitHub Releases.

### Crear un Release en GitHub (automático)

El proyecto incluye un **GitHub Action** que compila y publica el EXE automáticamente. Solo tenés que crear un tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Esto dispara el workflow `.github/workflows/release.yml` que:
1. Compila el proyecto en `windows-latest`.
2. Genera el EXE self-contained.
3. Lo empaqueta en `PlatzDaemon-v1.0.0-win-x64.zip`.
4. Crea el Release en GitHub con el ZIP listo para descargar.

Para la próxima versión: `git tag v1.1.0 && git push origin v1.1.0`, y así.

> 💡 También podés crear un Release manual desde GitHub: pestaña **"Releases"** → **"Draft a new release"** → subir el ZIP manualmente.

---

## Estructura del proyecto

```
court-daemon/
├── .github/workflows/        # GitHub Actions
│   └── release.yml           # Build & publish automático
├── Pages/                    # Páginas Razor (UI)
│   ├── Index.cshtml          # Dashboard con logs en tiempo real
│   ├── Config.cshtml         # Configuración "Mi Reserva"
│   ├── Sistema.cshtml        # Configuración del sistema
│   └── Session.cshtml        # Gestión de sesión WhatsApp
├── Services/                 # Lógica de negocio
│   ├── WhatsAppAutomationService.cs  # Automatización con Playwright
│   ├── BookingSchedulerService.cs    # Scheduler (BackgroundService)
│   ├── ConfigStore.cs                # Persistencia de configuración
│   ├── LogStore.cs                   # Almacén de logs + SignalR
│   └── AppStateService.cs           # Estado de la aplicación
├── Models/                   # Modelos de datos
│   ├── BookingConfig.cs      # Modelo de configuración
│   ├── AppState.cs           # Estado del daemon
│   └── LogEntry.cs           # Entrada de log
├── Hubs/
│   └── LogHub.cs             # Hub de SignalR para logs en tiempo real
├── Data/                     # (gitignored)
│   ├── config.json           # Configuración persistida
│   └── browser-data/         # Datos de sesión del navegador
├── wwwroot/
│   ├── css/
│   │   ├── terminal.min.css  # terminal.css (tema retro)
│   │   └── site.css          # Estilos personalizados
│   └── js/
│       └── site.js           # JavaScript del cliente
├── PlatzDaemon.csproj        # Proyecto .NET
├── Program.cs                # Entry point
├── README.md
└── DOCS.md
```

---

## Tecnologías

| Tecnología | Uso |
|---|---|
| **ASP.NET Core Razor Pages** | Interfaz web y servidor |
| **Playwright** | Automatización del navegador Chromium |
| **SignalR** | Logs y estado en tiempo real |
| **terminal.css** | UI retro estilo terminal |
| **BackgroundService** | Scheduler para ejecución programada |

---

## Documentación completa

Consultá **[DOCS.md](DOCS.md)** para la documentación detallada que incluye:

- Guía completa de configuración
- Flujo de automatización paso a paso
- Modo competitivo
- Manejo de errores y reintentos
- Preguntas frecuentes
- Troubleshooting

---

## Licencia

Uso personal. Diseñado para automatizar reservas en clubes de tenis que usan bots de WhatsApp.
