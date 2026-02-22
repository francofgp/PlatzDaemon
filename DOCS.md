# 📖 Platz Daemon — Documentación Completa

## Tabla de contenidos

1. [Descripción general](#descripción-general)
2. [Descarga y uso rápido](#descarga-y-uso-rápido)
3. [Arquitectura](#arquitectura)
4. [Interfaz de usuario](#interfaz-de-usuario)
5. [Configuración detallada](#configuración-detallada)
6. [Flujo de automatización](#flujo-de-automatización)
7. [Modo competitivo](#modo-competitivo)
8. [Manejo de errores y reintentos](#manejo-de-errores-y-reintentos)
9. [Sesión de WhatsApp](#sesión-de-whatsapp)
10. [Ejecución y despliegue](#ejecución-y-despliegue)
11. [Preguntas frecuentes (FAQ)](#preguntas-frecuentes-faq)
12. [Troubleshooting](#troubleshooting)
13. [Detalles técnicos](#detalles-técnicos)

---

## Descripción general

Platz Daemon es una aplicación ASP.NET Core Razor Pages que automatiza la reserva de canchas de tenis a través del bot de WhatsApp de un club. Utiliza **Playwright** para controlar un navegador Chromium que interactúa con WhatsApp Web, simulando el flujo manual de reserva.

### Problema que resuelve

Muchos clubes de tenis habilitan los turnos a una hora específica (ej: 8:00 AM). Si no estás atento en ese momento, las mejores canchas y horarios se ocupan rápidamente. Platz Daemon se encarga de estar listo y ejecutar la reserva automáticamente en el momento exacto.

### Características principales

- ⏰ **Reserva automática**: se dispara a la hora configurada sin intervención manual.
- 🏎️ **Modo competitivo**: pre-carga el mensaje 20 segundos antes y lo envía en el milisegundo exacto.
- 🔄 **Reintentos automáticos**: si una cancha es tomada por otro usuario durante la reserva, reintenta automáticamente (hasta 3 veces).
- 📋 **Prioridades configurables**: define múltiples horarios y canchas en orden de preferencia.
- 📊 **Dashboard en tiempo real**: logs, estado, cuenta regresiva y notificaciones vía SignalR.
- 🔔 **Notificaciones de escritorio**: alerta de Windows cuando la reserva se completa o falla.
- 💾 **Sesión persistente**: el login de WhatsApp Web se guarda; no necesitás escanear el QR cada vez.
- 🖥️ **Interfaz retro**: UI con tema de terminal verde sobre negro usando `terminal.css`.

---

## Descarga y uso rápido

> **Esta sección es para usuarios que solo quieren usar la aplicación, sin saber nada de programación.**
> Si te mandaron este link por WhatsApp o por cualquier medio y no sabés qué es GitHub, tranquilo, esta guía es para vos.

### ¿Qué es esta página?

GitHub es una plataforma donde se guardan programas. Vos no necesitás crear cuenta ni saber usarla. Solo necesitás **descargar un archivo** y listo.

### ¿Cómo descargo el programa?

1. **Ir a la página de descarga**: buscá el link de **"Releases"** en la página principal del proyecto (generalmente [acá](../../releases/latest)). Si te pasaron un link directo a Releases, mejor todavía.

2. **Encontrar el archivo para descargar**: en la página de Releases, bajá hasta donde dice **"Assets"** (activos). Ahí vas a ver archivos para descargar. Buscá el que diga algo como **`PlatzDaemon-v1.0.0-win-x64.zip`** y hacé click para descargarlo.

3. **Extraer el ZIP**:
   - Andá a tu carpeta de **Descargas**.
   - Hacé **click derecho** en el archivo `.zip` → **"Extraer todo..."**
   - Elegí dónde querés guardarlo. Recomiendo: `C:\PlatzDaemon` o en el Escritorio.

4. **Ejecutar**:
   - Abrí la carpeta que extrajiste.
   - Buscá **`PlatzDaemon.exe`** y hacé **doble click**.

### Windows me muestra una advertencia azul, ¿qué hago?

Es normal. Windows muestra una advertencia ("Windows protegió tu equipo") con cualquier programa nuevo descargado de internet. Para continuar:

1. Hacé click en **"Más información"** (es un link chiquito que aparece abajo del texto).
2. Hacé click en **"Ejecutar de todas formas"**.

Esto solo pasa la primera vez que lo abrís.

### ¿Qué pasa cuando ejecuto el programa?

1. Se abre una **ventana negra de consola** → ⚠️ **no la cierres**, es el programa corriendo.
2. Automáticamente se abre tu **navegador web** (Chrome, Edge, etc.) con la interfaz de Platz Daemon.
3. Si el navegador no se abrió solo, escribí **http://localhost:5000** en la barra de direcciones de tu navegador.

### Primeros pasos después de instalar

#### 1. Conectar WhatsApp

- En la interfaz web, andá a la pestaña **"WhatsApp"**.
- Click en **"Conectar WhatsApp"** → se abre WhatsApp Web en otro navegador.
- En tu **celular**: WhatsApp → ⋮ Menú (los tres puntitos) → **"Dispositivos vinculados"** → **"Vincular un dispositivo"** → escaneá el QR de la pantalla.
- Click en **"Verificar sesión"**.

> Solo necesitás hacer esto una vez. Las próximas veces se conecta solo.

#### 2. Configurar

- **"Mi Reserva"**: elegí si querés jugar a la mañana, tarde o noche. Agregá los horarios que preferís (ej: 18:00hs, 19:00hs). Poné las canchas que te gustan. Elegí si jugás Single o Doble.
- **"Sistema"**: poné tu **DNI**, el **número de WhatsApp del bot del club** (ej: 93534407576, sin el +), y la **hora a la que se habilitan los turnos** en tu club (ej: 08:00).
- Hacé click en **"Guardar"** en cada página.

#### 3. Dejar corriendo

Dejá la computadora **prendida** (no en modo suspender/hibernar). La app se encarga de todo. Podés bloquear la pantalla con Win+L sin problema.

> ⚠️ **Configurar Windows para que no se suspenda**: andá a **Configuración > Sistema > Energía y suspensión** y poné **"Nunca"** en las opciones de suspensión (tanto con batería como enchufado). Si la PC se suspende o hiberna, el programa se detiene.

### ¿Cómo sé si funcionó?

- Abrí **http://localhost:5000** en tu navegador → en el **Dashboard** vas a ver los logs en tiempo real.
- También te llega una **notificación de Windows** cuando la reserva se confirma o falla.
- Podés verificar directamente en **WhatsApp** mirando la conversación con el bot.

### ¿Cómo paro el programa?

Cerrá la **ventana negra de consola**. Eso para todo.

### ¿Necesito instalar algo?

**No.** El programa incluye todo lo que necesita para funcionar. Solo descargás, extraés y ejecutás. La primera vez puede tardar unos segundos más en abrir porque descarga un componente interno del navegador.

### ¿Funciona en Mac o Linux?

No. Actualmente solo funciona en **Windows 10 o 11** (64 bits).

---

## Arquitectura

```
┌─────────────────────────────────────────────────────────┐
│                    NAVEGADOR DEL USUARIO                │
│              http://localhost:5000                       │
│  ┌──────────┐  ┌──────────┐  ┌─────────┐  ┌─────────┐ │
│  │Dashboard │  │Mi Reserva│  │ Sistema  │  │WhatsApp │ │
│  │ (Index)  │  │ (Config) │  │(Sistema) │  │(Session)│ │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬────┘ │
│       │SignalR       │POST        │POST         │POST   │
└───────┼──────────────┼────────────┼─────────────┼───────┘
        │              │            │             │
┌───────┼──────────────┼────────────┼─────────────┼───────┐
│       ▼              ▼            ▼             ▼       │
│  ┌─────────┐   ┌───────────┐  ┌────────────────────┐   │
│  │ LogHub  │   │ConfigStore│  │WhatsAppAutomation  │   │
│  │(SignalR)│   │  (JSON)   │  │    Service         │   │
│  └─────────┘   └───────────┘  │  (Playwright)      │   │
│                               └────────┬───────────┘   │
│  ┌──────────────────────┐              │               │
│  │BookingSchedulerService│◄────────────┘               │
│  │  (BackgroundService)  │                             │
│  └───────────────────────┘                             │
│                                                         │
│              ASP.NET Core (Kestrel :5000)               │
└─────────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────┐
│   Chromium       │
│  (WhatsApp Web)  │
│  Datos en        │
│  Data/browser-   │
│  data/           │
└─────────────────┘
```

### Servicios principales

| Servicio | Responsabilidad |
|---|---|
| `WhatsAppAutomationService` | Controla Chromium vía Playwright. Ejecuta el flujo completo de reserva. |
| `BookingSchedulerService` | `BackgroundService` que espera hasta la hora de disparo y lanza la reserva. |
| `ConfigStore` | Carga y guarda la configuración en `Data/config.json`. |
| `LogStore` | Almacena logs en memoria y los emite vía SignalR al Dashboard. |
| `AppStateService` | Mantiene el estado de la app (Idle, Waiting, Running, Completed, Error) y lo notifica vía SignalR. |
| `NotificationService` | Envía notificaciones de escritorio de Windows (toast). |
| `LogHub` | Hub de SignalR para comunicación en tiempo real con el navegador. |

Todos los servicios se registran como **Singleton** para compartir estado en toda la aplicación.

---

## Interfaz de usuario

La aplicación tiene 4 páginas principales, accesibles desde la barra de navegación:

### 1. Dashboard (`/`)

Pantalla principal con:
- **Estado del daemon**: Idle, Waiting, Running, Completed, Error.
- **Cuenta regresiva**: tiempo restante hasta el próximo disparo.
- **Último resultado**: resultado de la última ejecución.
- **Estado de WhatsApp**: indicador visual (conectado/desconectado).
- **Botón "Ejecutar ahora"**: dispara una ejecución manual inmediata.
- **Botón "Limpiar logs"**: limpia la consola de logs.
- **Terminal de logs**: muestra todos los logs en tiempo real con colores por nivel (info, success, warning, error).

### 2. Mi Reserva (`/Config`)

Configuración de los parámetros de la reserva que el usuario cambia frecuentemente:
- **Periodo preferido**: Mañana, Tarde o Noche.
- **Tipo de juego**: Single o Doble.
- **Día de reserva**: Hoy o Mañana (ver sección [Día de reserva](#día-de-reserva)).
- **Horarios prioritarios**: lista ordenada por prioridad con formato `HH:MMhs`.
- **Canchas prioritarias**: lista ordenada por prioridad (ej: "Cancha Central", "Cancha 9").

### 3. Sistema (`/Sistema`)

Configuración del sistema que rara vez cambia:
- **Automatización habilitada**: activa/desactiva el disparo automático.
- **Hora de disparo**: hora a la que se ejecuta la reserva (hora Argentina, UTC-3).
- **Modo competitivo**: pre-carga el mensaje antes de la hora exacta.
- **Número del bot**: número de WhatsApp del bot del club (sin +).
- **DNI del socio**: documento de identidad que el bot solicita para identificarte.

### 4. WhatsApp (`/Session`)

Gestión de la sesión de WhatsApp Web:
- Estado de conexión (Conectado / Navegador Abierto / Desconectado).
- Instrucciones paso a paso para vincular.
- Botones: Conectar, Verificar sesión, Desconectar.
- Información de la sesión almacenada.

---

## Configuración detallada

La configuración se persiste en `Data/config.json`. Estos son todos los campos:

### Parámetros de reserva (Mi Reserva)

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `PreferredPeriod` | string | `"Noche"` | Periodo del día: `"Mañana"`, `"Tarde"`, `"Noche"` |
| `GameType` | string | `"Doble"` | Tipo de juego: `"Single"`, `"Doble"` |
| `BookingDay` | string | `"Hoy"` | Día de reserva: `"Hoy"`, `"Mañana"` |
| `PreferredTimeSlots` | string[] | `["18:00hs", "19:00hs", "17:30hs"]` | Horarios en orden de prioridad. Formato: `HH:MMhs` |
| `PreferredCourts` | string[] | `["Cancha Central", "Cancha 9"]` | Canchas en orden de prioridad |

### Parámetros del sistema (Sistema)

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `Enabled` | bool | `true` | Si la automatización está activa |
| `TriggerTime` | string | `"08:00"` | Hora de disparo en formato `HH:mm` (hora Argentina) |
| `CompetitiveMode` | bool | `true` | Activa el modo competitivo |
| `BotPhoneNumber` | string | `"93534407576"` | Número de WhatsApp del bot (sin +) |
| `Dni` | string | `""` | DNI del socio |

### Día de reserva

Esta opción controla qué botón presionar cuando el bot pregunta para qué día querés reservar:

- **Hoy**: el club habilita turnos para el mismo día. Ejemplo: disparás a las 8:00 AM y jugás hoy.
- **Mañana**: el club habilita turnos con anticipación. Ejemplo: querés jugar el viernes a las 19:00, configurás el disparo a las 00:00 del jueves con día "Mañana".

### Formato de horarios

Los horarios deben ingresarse en formato `HH:MMhs` (ej: `18:00hs`, `09:30hs`). Este es el formato que muestra el bot de WhatsApp. La interfaz valida el formato automáticamente y acepta las siguientes entradas:

- `18:00` → se convierte a `18:00hs`
- `18:00hs` → se acepta directamente
- `8:00` → se convierte a `08:00hs`
- `25:00` → rechazado (hora inválida)
- `18:60` → rechazado (minutos inválidos)

---

## Flujo de automatización

El proceso de reserva sigue estos pasos:

```
1. LIMPIEZA
   └─ Enviar "Salir" para resetear conversaciones pendientes

2. ENVIAR "turno"
   └─ Esperar respuesta del bot

3. DNI (si el bot lo solicita)
   ├─ Enviar el DNI configurado
   └─ Confirmar nombre ("Sí") si el bot pregunta

4. SELECCIONAR DÍA
   └─ Click en "Hoy" o "Mañana" según configuración

5. VERIFICAR BLOQUEOS
   ├─ "Ya tiene turno reservado" → abortar (ya tenés reserva)
   └─ "No hay turnos disponibles" → abortar (sin turnos)

6. SELECCIONAR PERIODO
   └─ Click en "Turno mañana", "Turnos tarde" o "Turnos noche"

7. SELECCIONAR HORARIO
   └─ Probar cada horario de la lista de prioridad hasta encontrar uno disponible

8. SELECCIONAR CANCHA
   ├─ Probar cada cancha de la lista de prioridad
   └─ Si ninguna está en la lista, tomar la primera disponible

9. SELECCIONAR TIPO DE JUEGO
   └─ Click en "Single" o "Doble"

10. CONFIRMAR RESERVA
    └─ Click en "Sí" / "Confirmar"

11. VERIFICAR RESULTADO
    ├─ ✅ Confirmación exitosa → notificación de éxito
    ├─ ❌ Cancha rechazada → reintentar (hasta 3 veces)
    └─ ⚠️ Sin confirmación clara → notificar para verificar manualmente
```

### Lógica de selección de horarios

El sistema intenta los horarios en el orden configurado. Por ejemplo, si configuraste `["18:00hs", "19:00hs", "17:30hs"]`:

1. Busca `18:00hs` → si está disponible, lo selecciona.
2. Si no, busca `19:00hs` → si está disponible, lo selecciona.
3. Si no, busca `17:30hs` → si está disponible, lo selecciona.
4. Si **ninguno** está disponible → error, se cancela la reserva.

### Lógica de selección de canchas

Funciona igual que los horarios, pero con un fallback:

1. Prueba cada cancha de la lista de prioridad en orden.
2. Si **ninguna** de las preferidas está disponible, selecciona la **primera cancha disponible** que encuentre.

---

## Modo competitivo

El modo competitivo está diseñado para situaciones donde hay mucha competencia por las canchas. En lugar de empezar todo el proceso a la hora exacta, hace lo siguiente:

### Flujo del modo competitivo

```
T-20 segundos:
  1. Abrir navegador (si no está abierto)
  2. Navegar al chat del bot
  3. Escribir "turno" en el campo de texto (SIN enviar)
  4. Mensaje queda "armado" y listo

T=0 (hora exacta):
  5. Presionar Enter para enviar el mensaje
  6. Continuar con el flujo normal de reserva
```

### Espera de precisión

Para enviar en el momento exacto, el sistema usa una espera de tres fases:

1. **Sleep grueso** (>1s): `Task.Delay(500ms)` en loop.
2. **Sleep fino** (100ms-1s): `Task.Delay(10ms)` en loop.
3. **Busy-wait** (<100ms): `Task.Yield()` para máxima precisión en los últimos milisegundos.

---

## Manejo de errores y reintentos

### Escenarios manejados automáticamente

| Escenario | Comportamiento |
|---|---|
| **"Ya tiene turno reservado"** | Se detecta y se notifica. No se reintenta (ya tenés reserva). |
| **"No hay turnos disponibles"** | Se detecta y se notifica. Se espera al día siguiente. |
| **Cancha rechazada al confirmar** | Reintenta automáticamente hasta 3 veces el flujo completo. |
| **Conversación pendiente con el bot** | Envía "Salir" para resetear antes de cada intento. |
| **Confirmación de nombre (DNI)** | Clickea "Sí" automáticamente cuando el bot pregunta. |
| **Navegador cerrado inesperadamente** | Detecta que el navegador fue cerrado, lo limpia y recrea automáticamente. Si fue cerrado entre ejecuciones, se recupera en el mismo intento. Si fue cerrado durante una ejecución, se recupera en el siguiente intento. |
| **Horario no disponible** | Prueba el siguiente horario de la lista de prioridad. |
| **Cancha preferida no disponible** | Prueba la siguiente cancha, o toma la primera disponible. |

### Mecanismo de reintento (cancha rechazada)

Cuando el bot confirma el turno pero luego dice que la cancha no está disponible (fue tomada por otro usuario en ese instante), el sistema:

1. Detecta el mensaje de rechazo ("cancha no está disponible").
2. Envía "Salir" para resetear la conversación.
3. Vuelve a enviar "turno" y repite todo el flujo.
4. Intenta hasta **3 veces** antes de rendirse.

### Limpieza de conversaciones pendientes

Antes de cada intento de reserva, el sistema:

1. Envía "Salir" al bot.
2. Si el bot responde con "selecciona una opción", envía "Salir" otra vez.
3. Repite hasta que el bot responda con "¿Cómo puedo ayudarte?" (estado limpio).

Esto previene problemas si una ejecución anterior quedó a mitad de camino.

---

## Sesión de WhatsApp

### Primer uso

1. Ir a la pestaña **WhatsApp** en la UI.
2. Click en **"Conectar WhatsApp"** → se abre Chromium con WhatsApp Web.
3. Escanear el QR con el teléfono.
4. Click en **"Verificar sesión"**.

### Sesiones posteriores

La sesión se guarda en `Data/browser-data/`. Mientras estos datos estén intactos, no necesitás volver a escanear el QR. WhatsApp Web mantiene la sesión activa por varias semanas.

### Navegador visible (headful)

Chromium siempre se abre en modo visible (con ventana). Esto es obligatorio porque WhatsApp Web detecta y bloquea navegadores invisibles (headless). Si cerrás la ventana de Chromium, la sesión de WhatsApp **no se pierde** (está guardada en disco). Al ejecutar la reserva de nuevo, se abre un nuevo Chromium automáticamente.

### Verificación de sesión

El sistema usa múltiples estrategias para detectar si la sesión está activa:

- Selectores CSS conocidos de WhatsApp Web (`chat-list`, `pane-side`, etc.).
- Evaluación JavaScript del DOM (detecta presencia de chats, verifica que no haya QR visible).
- Compatible con múltiples versiones de WhatsApp Web gracias a la redundancia de selectores.

---

## Ejecución y despliegue

### Para usuarios finales (sin programar)

Ir a la sección [Descarga y uso rápido](#descarga-y-uso-rápido) de este documento o al [README](README.md) del proyecto.

### Modo desarrollo

```bash
cd court-daemon
dotnet run
```

Se abre en `http://localhost:5203` (según `Properties/launchSettings.json`).

### Modo producción

```bash
dotnet run --environment Production
```

Se abre automáticamente en `http://localhost:5000` en el navegador por defecto.

### Publicar como EXE (manual)

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Genera un EXE self-contained en `bin/Release/net10.0-windows/win-x64/publish/`.

Para distribuir: comprimir la carpeta `publish/` en un ZIP.

> **Nota**: el EXE incluye el runtime de .NET, no requiere instalación adicional en la PC del usuario.

### Instalar Playwright (navegadores)

Después de compilar o publicar, se necesitan los navegadores de Playwright:

```bash
pwsh bin/Debug/net10.0-windows/win-x64/playwright.ps1 install chromium
```

O en la ruta de publicación:

```bash
pwsh bin/Release/net10.0-windows/win-x64/publish/playwright.ps1 install chromium
```

### Publicar Release automáticamente con GitHub Actions

El proyecto incluye un workflow en `.github/workflows/release.yml` que **compila y publica el EXE automáticamente** cuando se pushea un tag de versión.

#### Cómo funciona

1. Vos creás un tag con formato `v*.*.*` (ej: `v1.0.0`).
2. GitHub Actions se dispara automáticamente y:
   - Compila el proyecto en un runner `windows-latest`.
   - Genera el EXE self-contained con `dotnet publish`.
   - Lo empaqueta en `PlatzDaemon-v1.0.0-win-x64.zip`.
   - Crea un Release en GitHub con el ZIP adjunto, listo para descargar.

#### Publicar una nueva versión

```bash
# 1. Commitear tus cambios
git add -A
git commit -m "feat: nueva funcionalidad"

# 2. Crear el tag de versión
git tag v1.0.0

# 3. Pushear todo (código + tag)
git push origin main
git push origin v1.0.0
```

En unos minutos, el Release aparece automáticamente en la pestaña **Releases** del repositorio con el ZIP listo para descargar.

#### Versiones siguientes

Para cada nueva versión, solo cambiás el número del tag:

```bash
git tag v1.1.0
git push origin v1.1.0
```

#### Crear un Release manualmente (alternativa)

Si preferís no usar GitHub Actions, también podés crear un Release manualmente:

1. Ir al repositorio en GitHub.
2. Click en la pestaña **"Releases"** → **"Draft a new release"**.
3. En **"Choose a tag"**, escribí un tag nuevo (ej: `v1.0.0`) y seleccioná "Create new tag on publish".
4. Poné un **título** (ej: `Platz Daemon v1.0.0`) y una **descripción**.
5. En **"Attach binaries by dropping them here"**, arrastrá el archivo ZIP generado.
6. Click en **"Publish release"**.

### Compartir con usuarios

Después de publicar el Release (automático o manual), compartí este link:

```
https://github.com/<tu-usuario>/<tu-repositorio>/releases/latest
```

Este link siempre apunta a la versión más reciente. El usuario entra, baja hasta "Assets", descarga el ZIP, lo extrae, y ejecuta `PlatzDaemon.exe`. No necesita cuenta de GitHub, ni Git, ni ninguna herramienta de desarrollo.

---

## Preguntas frecuentes (FAQ)

### ¿Necesito instalar algo para usar Platz Daemon?

**No.** Si descargaste el ZIP desde la página de Releases, no necesitás instalar nada. El programa ya incluye todo lo necesario. Solo extraés el ZIP y ejecutás `PlatzDaemon.exe`.

### ¿Necesito tener cuenta en GitHub para descargar?

**No.** La página de descarga (Releases) es pública. Cualquiera puede entrar y descargar el archivo sin registrarse.

### ¿Me pueden banear el número de WhatsApp?

No debería haber riesgo. La aplicación envía como máximo unos pocos mensajes al día, de forma humana (con delays entre acciones). El bot del club no detecta automatización.

### ¿La aplicación sigue corriendo si cierro la pestaña del navegador?

**Sí.** Cerrar la pestaña de `localhost:5000` solo cierra la interfaz web. El servidor ASP.NET Core sigue ejecutándose en segundo plano junto con el scheduler. Podés volver a abrir `http://localhost:5000` en cualquier momento para ver el estado.

### ¿Y si la computadora se bloquea (no suspendida)?

**Sí, sigue corriendo.** Bloquear la pantalla (Win+L) no afecta los procesos en ejecución. Solo si la computadora **se suspende o hiberna** se detendrá.

Para evitar que Windows suspenda la PC automáticamente:
1. Ir a **Configuración > Sistema > Energía y suspensión**.
2. En **"Suspender el equipo tras"**, poner **"Nunca"** (tanto con batería como enchufado).
3. Opcionalmente, en **"Apagar la pantalla tras"**, podés poner lo que quieras — apagar el monitor no afecta al programa.

### ¿El navegador Chromium siempre se abre?

**Sí.** Chromium siempre se abre en modo **visible** (con ventana). Esto es obligatorio porque WhatsApp Web detecta y bloquea navegadores invisibles (headless) mediante técnicas de fingerprinting. Si se intentara en modo invisible, WhatsApp no cargaría la sesión y pediría escanear el QR nuevamente.

### ¿Qué pasa si cierro el navegador Chromium?

**No pasa nada.** La aplicación detecta automáticamente que el navegador fue cerrado y se recupera:

- **Si lo cerrás entre ejecuciones**: al momento de la siguiente ejecución (manual o programada), la app detecta que el navegador ya no responde, lo limpia y abre uno nuevo automáticamente. Todo en un solo intento, sin errores.
- **Si lo cerrás durante una ejecución**: la reserva en curso se interrumpe, pero la sesión se limpia. Si ejecutás de nuevo, se abre un nuevo Chromium y funciona normalmente.
- **No perdés la sesión de WhatsApp**: los datos de sesión se guardan en `Data/browser-data/`, no en la ventana del navegador. Cerrar Chromium no borra la sesión.

### ¿Puedo reservar para mañana?

Sí. Configurá el **Día de reserva** en "Mañana" y ajustá la **Hora de disparo** al momento en que tu club habilita turnos con anticipación. Ejemplo: si querés jugar el viernes a las 19:00 y el club habilita turnos a medianoche, configurá disparo a las 00:00 del jueves con día "Mañana".

### ¿Cómo sé si la reserva fue exitosa?

De tres formas:
1. **Dashboard**: el log muestra "RESERVA CONFIRMADA" con el horario y cancha.
2. **Notificación de Windows**: aparece un toast en el escritorio.
3. **WhatsApp**: podés abrir la conversación con el bot para verificar.

### ¿Puedo ejecutar la reserva manualmente?

Sí. En el Dashboard, hacé click en **"Ejecutar ahora"**. Esto dispara la reserva inmediatamente, ignorando la hora de disparo. Es útil para probar que todo funciona.

---

## Troubleshooting

### El bot no responde después de enviar "turno"

- Verificá que el **número del bot** esté correctamente configurado en Sistema (sin el signo +).
- Asegurate de que la sesión de WhatsApp esté activa (verificar en la pestaña WhatsApp).
- Probá enviar "turno" manualmente al bot para confirmar que funciona.

### Error "Target closed" o "Browser has been closed"

El navegador Chromium se cerró inesperadamente. La aplicación se recupera sola: detecta que el navegador no responde, lo limpia y crea uno nuevo automáticamente. Si lo cerrás entre ejecuciones, la próxima ejecución funciona sin problemas. Si lo cerrás durante una ejecución en curso, ejecutá de nuevo y se recupera.

### No encuentra el botón "Hoy" o "Mañana"

- El bot puede tardar en responder. Los tiempos de espera son de hasta 30 segundos.
- Verificá que el bot no esté caído probando manualmente.
- Revisá los logs del Dashboard para ver qué opciones detecta el sistema.

### "Ya tiene turno reservado"

El bot del club solo permite una reserva activa a la vez. Cancelá tu turno existente directamente en WhatsApp antes de intentar sacar otro.

### "No hay turnos disponibles"

Todos los horarios del día ya fueron reservados por otros usuarios. La aplicación esperará al día siguiente para reintentar.

### El formato de horario no es aceptado

Usá el formato `HH:MMhs` (ej: `18:00hs`, `09:30hs`). La interfaz acepta también `HH:MM` sin el "hs" y lo formatea automáticamente.

### La sesión de WhatsApp se desconectó

Puede pasar si WhatsApp desvincula el dispositivo (por inactividad prolongada o por vincular otro dispositivo). Volvé a escanear el QR desde la pestaña WhatsApp.

### Error al compilar: "The file is locked"

Si el EXE está en ejecución, no se puede recompilar. Cerrá la aplicación o usá:

```bash
taskkill /F /IM PlatzDaemon.exe
```

---

## Detalles técnicos

### Zona horaria

Toda la lógica de scheduling usa **hora Argentina (UTC-3)**, independientemente de la zona horaria de la PC. Se usa `TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time")`.

### Persistencia de configuración

La configuración se guarda en `Data/config.json` como JSON plano. Las páginas "Mi Reserva" y "Sistema" hacen **merge parcial**: cada una solo actualiza sus campos, sin sobreescribir los de la otra página.

### Detección de mensajes en WhatsApp Web

El servicio de automatización usa **6 estrategias diferentes** para contar mensajes en el chat, porque los selectores de WhatsApp Web cambian entre versiones:

1. `[class*="message-in"], [class*="message-out"]`
2. `div[data-id]` con prefijo `true_` o `false_`
3. `div[role="row"]` dentro del panel de mensajes
4. `[data-testid="msg-container"]`
5. Items de lista focalizables
6. Hijos directos del panel de conversación

### Interacción con botones del bot

Los botones de WhatsApp (como "Hoy", "Turnos noche", etc.) se buscan en los **últimos 5 mensajes** del chat para evitar clickear opciones de conversaciones anteriores. Se prueban tres estrategias de matching:

1. **Coincidencia exacta** del texto del botón.
2. **Coincidencia parcial** (contiene el texto).
3. **Búsqueda en `<span>`**: para botones renderizados como spans.

### Concurrencia

Se usa un `SemaphoreSlim(1, 1)` para serializar el acceso al navegador. Solo una operación de automatización puede ejecutarse a la vez.

### Notificaciones de Windows

Se usa `Microsoft.Toolkit.Uwp.Notifications` para toasts de Windows y `System.Media.SystemSounds` para el sonido de notificación. Estas APIs son específicas de Windows.

### Puerto por defecto

- **Producción**: `http://localhost:5000`
- **Desarrollo**: `http://localhost:5203`

El navegador se abre automáticamente en modo producción después de 1.5 segundos de iniciado el servidor.
