using NSubstitute;
using PlatzDaemon.Hubs;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.SignalR;
using LogLevel = PlatzDaemon.Models.LogLevel;

namespace PlatzDaemon.Tests.Services;

public class LogStoreTests
{
    private readonly LogStore _store;
    private readonly IClientProxy _clientProxy;

    public LogStoreTests()
    {
        var hubContext = Substitute.For<IHubContext<LogHub>>();
        var clients = Substitute.For<IHubClients>();
        _clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(clients);
        clients.All.Returns(_clientProxy);
        _store = new LogStore(hubContext);
    }

    [Fact]
    public async Task LogAsync_AddsEntry()
    {
        await _store.LogAsync("test message");

        var logs = _store.GetAll();
        Assert.Single(logs);
        Assert.Equal("test message", logs[0].Message);
    }

    [Fact]
    public async Task LogInfoAsync_SetsInfoLevel()
    {
        await _store.LogInfoAsync("info msg");
        Assert.Equal(LogLevel.Info, _store.GetAll()[0].Level);
    }

    [Fact]
    public async Task LogSuccessAsync_SetsSuccessLevel()
    {
        await _store.LogSuccessAsync("ok");
        Assert.Equal(LogLevel.Success, _store.GetAll()[0].Level);
    }

    [Fact]
    public async Task LogWarningAsync_SetsWarningLevel()
    {
        await _store.LogWarningAsync("warn");
        Assert.Equal(LogLevel.Warning, _store.GetAll()[0].Level);
    }

    [Fact]
    public async Task LogErrorAsync_SetsErrorLevel()
    {
        await _store.LogErrorAsync("err");
        Assert.Equal(LogLevel.Error, _store.GetAll()[0].Level);
    }

    [Fact]
    public async Task GetAll_ReturnsImmutableCopy()
    {
        await _store.LogAsync("a");
        var first = _store.GetAll();

        await _store.LogAsync("b");
        var second = _store.GetAll();

        Assert.Single(first);
        Assert.Equal(2, second.Count);
    }

    [Fact]
    public async Task LogAsync_CapsAt500Messages()
    {
        for (int i = 0; i < 505; i++)
            await _store.LogAsync($"msg {i}");

        var logs = _store.GetAll();
        Assert.Equal(500, logs.Count);
        Assert.Equal("msg 5", logs[0].Message);
        Assert.Equal("msg 504", logs[^1].Message);
    }

    [Fact]
    public async Task Clear_RemovesAllLogs()
    {
        await _store.LogAsync("a");
        await _store.LogAsync("b");

        _store.Clear();

        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public async Task LogAsync_SendsSignalRMessage()
    {
        await _store.LogAsync("hello", LogLevel.Info);

        await _clientProxy.Received(1).SendCoreAsync(
            "ReceiveLog",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }
}
