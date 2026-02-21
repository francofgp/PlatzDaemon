using Microsoft.AspNetCore.SignalR;

namespace PlatzDaemon.Hubs;

public class LogHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
