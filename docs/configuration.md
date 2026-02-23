# Configuracion

## Modelo: BookingConfig

**Archivo**: `Models/BookingConfig.cs`

Un POCO con 10 propiedades que representan toda la configuracion de la aplicacion. No tiene validacion ni logica — es puro datos.

### Campos

| Campo | Tipo | Default | Pagina que lo edita | Descripcion |
|---|---|---|---|---|
| `BotPhoneNumber` | `string` | `"93534407576"` | Sistema | Numero de WhatsApp del bot (sin +) |
| `Dni` | `string` | `""` | Sistema | DNI del socio |
| `TriggerTime` | `string` | `"08:00"` | Sistema | Hora de disparo (HH:mm, hora Argentina) |
| `CompetitiveMode` | `bool` | `true` | Sistema | Modo competitivo activado |
| `Enabled` | `bool` | `true` | Sistema | Automatizacion habilitada |
| `PreferredPeriod` | `string` | `"Noche"` | Mi Reserva | Periodo: "Mañana", "Tarde", "Noche" |
| `PreferredTimeSlots` | `List<string>` | `["18:00hs", "19:00hs", "17:30hs"]` | Mi Reserva | Horarios en orden de prioridad |
| `PreferredCourts` | `List<string>` | `["Cancha Central", "Cancha 9"]` | Mi Reserva | Canchas en orden de prioridad |
| `GameType` | `string` | `"Doble"` | Mi Reserva | "Single" o "Doble" |
| `BookingDay` | `string` | `"Hoy"` | Mi Reserva | "Hoy" o "Mañana" |

> Nota: los defaults estan pensados para un uso tipico. El usuario los cambia desde la UI en el primer uso.

---

## Persistencia: ConfigStore

**Archivo**: `Services/ConfigStore.cs`

### Como funciona

1. **Startup**: el constructor lee `Data/config.json`. Si no existe o esta corrupto, usa un `BookingConfig` con defaults.
2. **Lectura**: `Get()` devuelve el objeto cacheado en memoria. Sin I/O.
3. **Escritura**: `SaveAsync(config)` serializa a JSON y escribe a disco. Actualiza la cache.

### Directorio de datos

El config se guarda en `Data/config.json` dentro del `ContentRootPath`:
- En desarrollo: `court-daemon/Data/config.json`.
- En publicacion: junto al ejecutable, `publish/Data/config.json`.

El directorio `Data/` se crea automaticamente si no existe (tanto en `ConfigStore` como en `WhatsAppAutomationService`).

### Formato JSON

Se usa `System.Text.Json` con opciones:

```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

Resultado:

```json
{
  "botPhoneNumber": "93534407576",
  "dni": "12345678",
  "triggerTime": "08:00",
  "competitiveMode": true,
  "preferredPeriod": "Noche",
  "preferredTimeSlots": [
    "18:00hs",
    "19:00hs"
  ],
  "preferredCourts": [
    "Cancha Central",
    "Cancha 9"
  ],
  "gameType": "Doble",
  "bookingDay": "Hoy",
  "enabled": true
}
```

### Thread safety

`SaveAsync` usa un `SemaphoreSlim(1,1)` para evitar escrituras concurrentes al archivo:

```csharp
await _lock.WaitAsync();
try
{
    _cachedConfig = config;
    var json = JsonSerializer.Serialize(config, JsonOptions);
    await File.WriteAllTextAsync(_configPath, json);
}
finally
{
    _lock.Release();
}
```

`Get()` no necesita lock porque devuelve una referencia al objeto cacheado. En el peor caso, se lee una version "vieja" del config que todavia esta siendo escrita, pero esto es aceptable porque el config solo cambia cuando el usuario guarda (accion poco frecuente).

---

## Merge parcial

Cada pagina solo actualiza **sus campos** sin sobreescribir los de la otra.

### Config (Mi Reserva)

```csharp
public async Task<IActionResult> OnPostAsync()
{
    var cfg = _configStore.Get();       // Carga completa
    cfg.PreferredPeriod = PreferredPeriod;
    cfg.GameType = GameType;
    cfg.BookingDay = BookingDay;
    cfg.PreferredTimeSlots = PreferredTimeSlots;
    cfg.PreferredCourts = PreferredCourts;
    await _configStore.SaveAsync(cfg);  // Guarda completa
}
```

### Sistema

```csharp
public async Task<IActionResult> OnPostAsync()
{
    var cfg = _configStore.Get();       // Carga completa
    cfg.Enabled = Enabled;
    cfg.BotPhoneNumber = BotPhoneNumber;
    cfg.Dni = Dni;
    cfg.TriggerTime = TriggerTime;
    cfg.CompetitiveMode = CompetitiveMode;
    await _configStore.SaveAsync(cfg);  // Guarda completa
    _scheduler.NotifyConfigChanged();   // Recalcular disparo
}
```

Este patron evita que guardar "Mi Reserva" borre el DNI, o que guardar "Sistema" borre los horarios preferidos.

> **Advertencia**: como `Get()` devuelve una referencia mutable, si dos paginas guardan simultaneamente, la segunda podria sobreescribir los cambios de la primera. En la practica esto no pasa porque un solo usuario usa la app, pero no es thread-safe en teoria.

---

## Migracion de configuracion

`MigrateConfig` se ejecuta al cargar el JSON del disco. Actualmente normaliza horarios:

```csharp
internal static void MigrateConfig(BookingConfig config)
{
    if (config.PreferredTimeSlots is { Count: > 0 })
    {
        for (int i = 0; i < config.PreferredTimeSlots.Count; i++)
        {
            var slot = config.PreferredTimeSlots[i].Trim();
            if (!slot.EndsWith("hs", StringComparison.OrdinalIgnoreCase))
                config.PreferredTimeSlots[i] = slot + "hs";
        }
    }
}
```

Esto convierte horarios viejos sin suffix: `"18:00"` → `"18:00hs"`.

### Cuando agregar migraciones

Si cambias el formato de un campo o agregas un nuevo campo con un default que depende de datos existentes, agrega logica en `MigrateConfig`. Esto permite que configs guardadas con versiones anteriores sigan funcionando.

---

## Interfaz IConfigStore

```csharp
public interface IConfigStore
{
    BookingConfig Get();
    Task SaveAsync(BookingConfig config);
}
```

Existe exclusivamente para testing. Permite inyectar un mock en los tests de pages y servicios sin depender del filesystem.

---

## Como agregar un nuevo campo de configuracion

### Paso 1: Agregar la propiedad al modelo

En `Models/BookingConfig.cs`:

```csharp
/// <summary>Descripcion del nuevo campo</summary>
public string NuevoCampo { get; set; } = "valor_default";
```

### Paso 2: Agregar a la pagina Razor correspondiente

Si es un campo frecuente (como tipo de juego) → `Pages/Config.cshtml` + `Config.cshtml.cs`.
Si es un campo de sistema (como DNI) → `Pages/Sistema.cshtml` + `Sistema.cshtml.cs`.

En el code-behind:

```csharp
[BindProperty]
public string NuevoCampo { get; set; } = "";

public void OnGet()
{
    var cfg = _configStore.Get();
    NuevoCampo = cfg.NuevoCampo;
}

public async Task<IActionResult> OnPostAsync()
{
    var cfg = _configStore.Get();
    cfg.NuevoCampo = NuevoCampo;  // Agregar esta linea en OnPost
    await _configStore.SaveAsync(cfg);
    // ...
}
```

En el Razor:

```html
<div class="form-group">
    <label asp-for="NuevoCampo">Label</label>
    <input type="text" asp-for="NuevoCampo" />
    <p class="field-help">Texto de ayuda.</p>
</div>
```

### Paso 3: Usarlo en los servicios

Donde se necesite, leer con `_configStore.Get().NuevoCampo`.

### Paso 4: Migracion (si es necesario)

Si el campo reemplaza o transforma un campo existente, agregar logica en `ConfigStore.MigrateConfig`.

### Paso 5: Tests

Agregar tests para:
- El default del campo en `BookingConfigTests`.
- La persistencia en `ConfigStoreTests`.
- El binding de la pagina en `ConfigModelTests` o `SistemaModelTests`.
