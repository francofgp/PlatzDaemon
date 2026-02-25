using NSubstitute;
using PlatzDaemon.Models;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.Hosting;

namespace PlatzDaemon.Tests.Services;

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

    [Fact]
    public void Get_WithNoFile_ReturnsDefaults()
    {
        var store = new ConfigStore(_env);
        var config = store.Get();

        Assert.Equal("5493534407576", config.BotPhoneNumber);
        Assert.Equal("08:00", config.TriggerTime);
        Assert.True(config.Enabled);
    }

    [Fact]
    public async Task SaveAsync_ThenGet_ReturnsUpdatedConfig()
    {
        var store = new ConfigStore(_env);
        var config = new BookingConfig { Dni = "12345678", TriggerTime = "09:00" };

        await store.SaveAsync(config);
        var loaded = store.Get();

        Assert.Equal("12345678", loaded.Dni);
        Assert.Equal("09:00", loaded.TriggerTime);
    }

    [Fact]
    public async Task SaveAsync_PersistsToDisk_NewInstanceLoadsIt()
    {
        var store = new ConfigStore(_env);
        await store.SaveAsync(new BookingConfig { Dni = "99999" });

        var store2 = new ConfigStore(_env);
        var loaded = store2.Get();

        Assert.Equal("99999", loaded.Dni);
    }

    [Fact]
    public void Get_WithCorruptFile_ReturnsDefaults()
    {
        var dataDir = Path.Combine(_tempDir, "Data");
        Directory.CreateDirectory(dataDir);
        File.WriteAllText(Path.Combine(dataDir, "config.json"), "not valid json {{{");

        var store = new ConfigStore(_env);
        var config = store.Get();

        Assert.Equal("5493534407576", config.BotPhoneNumber);
    }

    [Fact]
    public void MigrateConfig_AddsHsSuffix_WhenMissing()
    {
        var config = new BookingConfig
        {
            PreferredTimeSlots = new List<string> { "18:00", "19:00" }
        };

        ConfigStore.MigrateConfig(config);

        Assert.Equal("18:00hs", config.PreferredTimeSlots[0]);
        Assert.Equal("19:00hs", config.PreferredTimeSlots[1]);
    }

    [Fact]
    public void MigrateConfig_DoesNotDuplicateHsSuffix()
    {
        var config = new BookingConfig
        {
            PreferredTimeSlots = new List<string> { "18:00hs", "19:00HS" }
        };

        ConfigStore.MigrateConfig(config);

        Assert.Equal("18:00hs", config.PreferredTimeSlots[0]);
        Assert.Equal("19:00HS", config.PreferredTimeSlots[1]);
    }

    [Fact]
    public void MigrateConfig_HandlesEmptyList()
    {
        var config = new BookingConfig
        {
            PreferredTimeSlots = new List<string>()
        };

        ConfigStore.MigrateConfig(config);

        Assert.Empty(config.PreferredTimeSlots);
    }

    [Fact]
    public void MigrateConfig_HandlesNullList()
    {
        var config = new BookingConfig
        {
            PreferredTimeSlots = null!
        };

        var ex = Record.Exception(() => ConfigStore.MigrateConfig(config));
        Assert.Null(ex);
    }

    [Fact]
    public void MigrateConfig_TrimsWhitespace_BeforeAppendingSuffix()
    {
        var config = new BookingConfig
        {
            PreferredTimeSlots = new List<string> { " 18:00 " }
        };

        ConfigStore.MigrateConfig(config);

        Assert.Equal("18:00hs", config.PreferredTimeSlots[0]);
    }
}
