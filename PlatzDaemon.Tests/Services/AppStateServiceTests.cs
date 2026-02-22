using NSubstitute;
using PlatzDaemon.Hubs;
using PlatzDaemon.Models;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.SignalR;

namespace PlatzDaemon.Tests.Services;

public class AppStateServiceTests
{
    private readonly AppStateService _service;
    private readonly IClientProxy _clientProxy;

    public AppStateServiceTests()
    {
        var hubContext = Substitute.For<IHubContext<LogHub>>();
        var clients = Substitute.For<IHubClients>();
        _clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(clients);
        clients.All.Returns(_clientProxy);
        _service = new AppStateService(hubContext);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatus()
    {
        await _service.UpdateStatusAsync(DaemonStatus.Running);
        Assert.Equal(DaemonStatus.Running, _service.State.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesLastResult()
    {
        await _service.UpdateStatusAsync(DaemonStatus.Error, "algo fallo");
        Assert.Equal("algo fallo", _service.State.LastResult);
    }

    [Fact]
    public async Task UpdateStatusAsync_NullResult_PreservesExisting()
    {
        await _service.UpdateStatusAsync(DaemonStatus.Completed, "exito");
        await _service.UpdateStatusAsync(DaemonStatus.Idle);

        Assert.Equal("exito", _service.State.LastResult);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithRunning_SetsLastRunTime()
    {
        var before = DateTime.Now;
        await _service.UpdateStatusAsync(DaemonStatus.Running);

        Assert.NotNull(_service.State.LastRunTime);
        Assert.True(_service.State.LastRunTime >= before);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNonRunning_DoesNotSetLastRunTime()
    {
        await _service.UpdateStatusAsync(DaemonStatus.Idle);
        Assert.Null(_service.State.LastRunTime);
    }

    [Fact]
    public void SetNextRun_UpdatesNextRunTime()
    {
        var next = new DateTime(2025, 6, 15, 8, 0, 0);
        _service.SetNextRun(next);
        Assert.Equal(next, _service.State.NextRunTime);
    }

    [Fact]
    public void SetNextRun_WithNull_ClearsNextRunTime()
    {
        _service.SetNextRun(DateTime.Now);
        _service.SetNextRun(null);
        Assert.Null(_service.State.NextRunTime);
    }

    [Fact]
    public void SetWhatsAppConnected_SetsTrue()
    {
        _service.SetWhatsAppConnected(true);
        Assert.True(_service.State.WhatsAppConnected);
    }

    [Fact]
    public void SetWhatsAppConnected_CanToggle()
    {
        _service.SetWhatsAppConnected(true);
        _service.SetWhatsAppConnected(false);
        Assert.False(_service.State.WhatsAppConnected);
    }

    [Fact]
    public async Task UpdateStatusAsync_SendsSignalRUpdate()
    {
        await _service.UpdateStatusAsync(DaemonStatus.Waiting, "esperando");

        await _clientProxy.Received(1).SendCoreAsync(
            "StatusUpdate",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }
}
