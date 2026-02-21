using PlatzDaemon.Hubs;
using PlatzDaemon.Models;
using Microsoft.AspNetCore.SignalR;

namespace PlatzDaemon.Services;

public class LogStore
{
    private readonly List<LogEntry> _logs = new();
    private readonly Lock _lock = new();
    private readonly IHubContext<LogHub> _hubContext;
    private const int MaxLogs = 500;

    public LogStore(IHubContext<LogHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public IReadOnlyList<LogEntry> GetAll()
    {
        lock (_lock)
        {
            return _logs.ToList().AsReadOnly();
        }
    }

    public async Task LogAsync(string message, Models.LogLevel level = Models.LogLevel.Info)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Message = message,
            Level = level
        };

        lock (_lock)
        {
            _logs.Add(entry);
            if (_logs.Count > MaxLogs)
                _logs.RemoveAt(0);
        }

        await _hubContext.Clients.All.SendAsync("ReceiveLog", entry.FormattedTime, entry.Prefix, entry.Message, entry.CssClass);
    }

    public async Task LogInfoAsync(string message) => await LogAsync(message, Models.LogLevel.Info);
    public async Task LogSuccessAsync(string message) => await LogAsync(message, Models.LogLevel.Success);
    public async Task LogWarningAsync(string message) => await LogAsync(message, Models.LogLevel.Warning);
    public async Task LogErrorAsync(string message) => await LogAsync(message, Models.LogLevel.Error);

    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }
}
