using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;

namespace PlatzDaemon.Hubs;

[ExcludeFromCodeCoverage]
public class LogHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
