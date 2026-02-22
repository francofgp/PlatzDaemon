using System.Text.Json;
using PlatzDaemon.Models;

namespace PlatzDaemon.Services;

public class ConfigStore : IConfigStore
{
    private readonly string _configPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BookingConfig _cachedConfig;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ConfigStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _configPath = Path.Combine(dataDir, "config.json");
        _cachedConfig = LoadFromDisk();
    }

    public BookingConfig Get()
    {
        return _cachedConfig;
    }

    public async Task SaveAsync(BookingConfig config)
    {
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
    }

    private BookingConfig LoadFromDisk()
    {
        if (!File.Exists(_configPath))
            return new BookingConfig();

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<BookingConfig>(json, JsonOptions) ?? new BookingConfig();
            MigrateConfig(config);
            return config;
        }
        catch
        {
            return new BookingConfig();
        }
    }

    /// <summary>
    /// Migra configuraciones viejas al formato actual.
    /// Ej: horarios "18:00" -> "18:00hs"
    /// </summary>
    internal static void MigrateConfig(BookingConfig config)
    {
        if (config.PreferredTimeSlots is { Count: > 0 })
        {
            for (int i = 0; i < config.PreferredTimeSlots.Count; i++)
            {
                var slot = config.PreferredTimeSlots[i].Trim();
                if (!slot.EndsWith("hs", StringComparison.OrdinalIgnoreCase))
                {
                    config.PreferredTimeSlots[i] = slot + "hs";
                }
            }
        }
    }
}
