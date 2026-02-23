# Testing

## Stack

| Herramienta | Version | Uso |
|---|---|---|
| **xUnit** | 2.9.3 | Framework de tests |
| **NSubstitute** | 5.3.0 | Mocking (interfaz-based) |
| **coverlet** | 6.0.4 | Recoleccion de cobertura |
| **Microsoft.NET.Test.Sdk** | 17.14.1 | SDK de testing |

El proyecto de tests esta en `PlatzDaemon.Tests/` con su propio `.csproj` que referencia al proyecto principal.

---

## Estructura de tests

```
PlatzDaemon.Tests/
├── Models/
│   ├── AppStateTests.cs           # Tests del enum DaemonStatus y propiedades de AppState
│   ├── BookingConfigTests.cs      # Tests de defaults y propiedades de BookingConfig
│   └── LogEntryTests.cs           # Tests de formateo (FormattedTime, Prefix, CssClass)
├── Services/
│   ├── AppStateServiceTests.cs    # Tests de UpdateStatusAsync, SetNextRun, SetWhatsAppConnected
│   ├── BookingSchedulerServiceTests.cs  # Tests de FormatTimeSpan, CalculateNextTrigger
│   ├── ConfigStoreTests.cs        # Tests de persistencia, migracion, corrupcion
│   ├── LogStoreTests.cs           # Tests de buffer, broadcast, Clear
│   └── WhatsAppAutomationServiceTests.cs  # Tests de IsBrowserClosedException
└── Pages/
    ├── ConfigModelTests.cs        # Tests de OnGet, OnPost, merge parcial
    ├── IndexModelTests.cs         # Tests de OnGet, propiedades derivadas
    └── SistemaModelTests.cs       # Tests de OnGet, OnPost, NotifyConfigChanged
```

---

## Patrones de testing usados

### Mocking con NSubstitute

Para servicios que dependen de interfaces o clases con efectos secundarios:

```csharp
var configStore = Substitute.For<IConfigStore>();
configStore.Get().Returns(new BookingConfig { Dni = "12345678" });
```

### Mock de IHubContext

`LogStore` y `AppStateService` necesitan `IHubContext<LogHub>`. Se mockea asi:

```csharp
var hubContext = Substitute.For<IHubContext<LogHub>>();
var hubClients = Substitute.For<IHubClients>();
var clientProxy = Substitute.For<IClientProxy>();
hubContext.Clients.Returns(hubClients);
hubClients.All.Returns(clientProxy);
var logStore = new LogStore(hubContext);
```

### Temp directories para ConfigStore

`ConfigStoreTests` usa directorios temporales para testear la persistencia en disco:

```csharp
public class ConfigStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IWebHostEnvironment _env;

    public ConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PlatzDaemonTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _env = Substitute.For<IWebHostEnvironment>();
        _env.ContentRootPath.Returns(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
```

### InternalsVisibleTo

El proyecto principal expone internals al proyecto de tests:

```xml
<!-- PlatzDaemon.csproj -->
<ItemGroup>
  <InternalsVisibleTo Include="PlatzDaemon.Tests" />
</ItemGroup>
```

Esto permite testear metodos `internal` como:
- `BookingSchedulerService.FormatTimeSpan`
- `BookingSchedulerService.CalculateNextTrigger`
- `ConfigStore.MigrateConfig`
- `WhatsAppAutomationService.IsBrowserClosedException`

### Theory + InlineData

Para tests parametricos:

```csharp
[Theory]
[InlineData(2, 30, 0, "2h 30m")]
[InlineData(0, 5, 10, "5m 10s")]
[InlineData(0, 0, 45, "45s")]
public void FormatTimeSpan_ReturnsExpectedFormat(int h, int m, int s, string expected)
{
    var ts = new TimeSpan(h, m, s);
    Assert.Equal(expected, BookingSchedulerService.FormatTimeSpan(ts));
}
```

---

## Correr tests

### Basico

```bash
dotnet test
```

### Con reporte de cobertura

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory ./coverage
```

### Generar reporte HTML

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
```

Abrir `coverage/report/index.html` en el navegador.

---

## Coverage

### Configuracion de exclusiones

`coverage.runsettings` excluye del reporte:

```xml
<ExcludeByFile>**/Program.cs</ExcludeByFile>
<ExcludeByAttribute>ExcludeFromCodeCoverage</ExcludeByAttribute>
<Include>[PlatzDaemon]*</Include>
<Exclude>[PlatzDaemon]PlatzDaemon.Pages.Pages_*</Exclude>
```

- `Program.cs`: entry point con config de DI, no tiene logica testeable.
- `[ExcludeFromCodeCoverage]`: `WhatsAppAutomationService` y `LogHub`.
- `Pages_*`: las Razor views compiladas (solo testear los code-behind).

### Que esta cubierto

| Area | Coverage | Notas |
|---|---|---|
| Models | Alta | Todos los defaults, formateo, enums |
| ConfigStore | Alta | Persistencia, corrupcion, migracion |
| LogStore | Alta | Buffer, broadcast, Clear |
| AppStateService | Alta | Estados, SetNextRun |
| BookingSchedulerService | Parcial | Helpers (FormatTimeSpan, CalculateNextTrigger). El loop principal no se testea (requiere async infrastructure compleja). |
| WhatsAppAutomationService | Minima | Solo `IsBrowserClosedException`. El servicio esta `[ExcludeFromCodeCoverage]` porque depende de Playwright (browser real). |
| Pages (code-behind) | Alta | OnGet, OnPost, binding, merge parcial |

### Que no se puede cubrir facilmente

- **WhatsAppAutomationService**: requiere un browser Chromium real y WhatsApp Web. Tests de integracion serian posibles pero:
  - Necesitan un numero de WhatsApp de prueba.
  - WhatsApp Web es un target movil (cambia el DOM).
  - Los tests serian flakey y lentos.
- **Program.cs**: es pura configuracion de DI y middleware.
- **Razor views**: el HTML generado se testea indirectamente via los code-behind.

### Coverage actual

~65.3% de line coverage, 91 tests pasando.

---

## Como agregar tests

### Para un nuevo modelo

Crear `PlatzDaemon.Tests/Models/NuevoModeloTests.cs`:

```csharp
namespace PlatzDaemon.Tests.Models;

public class NuevoModeloTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var model = new NuevoModelo();
        Assert.Equal("expected", model.Propiedad);
    }
}
```

### Para un nuevo servicio

Crear `PlatzDaemon.Tests/Services/NuevoServicioTests.cs`. Si el servicio depende de `IConfigStore` u otros, mockearlos con NSubstitute.

### Para un nuevo page model

Crear `PlatzDaemon.Tests/Pages/NuevaPaginaModelTests.cs`. Mockear las dependencias del constructor y testear `OnGet` y `OnPost`.

### Convencion de nombres

- Clase: `{ClaseTesteada}Tests`
- Metodo: `{Metodo}_{Escenario}_{ResultadoEsperado}` o `{Metodo}_{Escenario}` cuando el resultado es obvio.
- Ejemplo: `CalculateNextTrigger_AfterTriggerTimePlus5Min_ReturnsTomorrow`
