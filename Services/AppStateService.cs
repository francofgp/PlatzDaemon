using PlatzDaemon.Hubs;
using PlatzDaemon.Models;
using Microsoft.AspNetCore.SignalR;

namespace PlatzDaemon.Services;

public class AppStateService
{
    private readonly IHubContext<LogHub> _hubContext;
    private readonly Lock _lock = new();

    public AppState State { get; } = new();

    public AppStateService(IHubContext<LogHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task UpdateStatusAsync(DaemonStatus status, string? result = null)
    {
        lock (_lock)
        {
            State.Status = status;
            if (result != null)
                State.LastResult = result;
            if (status == DaemonStatus.Running)
                State.LastRunTime = DateTime.Now;
        }

        await _hubContext.Clients.All.SendAsync("StatusUpdate",
            State.Status.ToString(),
            State.LastResult ?? "",
            State.NextRunTime?.ToString("o") ?? "");
    }

    public void SetNextRun(DateTime? nextRun)
    {
        lock (_lock)
        {
            State.NextRunTime = nextRun;
        }
    }

    public void SetWhatsAppConnected(bool connected)
    {
        lock (_lock)
        {
            State.WhatsAppConnected = connected;
        }
    }
}
