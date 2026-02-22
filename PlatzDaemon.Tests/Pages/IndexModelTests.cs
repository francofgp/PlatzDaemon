using NSubstitute;
using PlatzDaemon.Hubs;
using PlatzDaemon.Models;
using PlatzDaemon.Pages;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.SignalR;

namespace PlatzDaemon.Tests.Pages;

public class IndexModelTests
{
    private readonly IConfigStore _configStore;
    private readonly LogStore _logStore;
    private readonly AppStateService _appState;
    private readonly IndexModel _model;

    public IndexModelTests()
    {
        var hubContext = Substitute.For<IHubContext<LogHub>>();
        var clients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(clients);
        clients.All.Returns(clientProxy);

        _configStore = Substitute.For<IConfigStore>();
        _configStore.Get().Returns(new BookingConfig());
        _logStore = new LogStore(hubContext);
        _appState = new AppStateService(hubContext);

        var env = Substitute.For<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        env.ContentRootPath.Returns(Path.GetTempPath());
        var notification = new NotificationService();
        var whatsApp = new WhatsAppAutomationService(env, _logStore, _configStore, notification, _appState);
        var scheduler = new BookingSchedulerService(_configStore, whatsApp, _logStore, _appState, notification);

        _model = new IndexModel(_logStore, _appState, scheduler, _configStore);
    }

    [Fact]
    public void OnGet_LoadsStatusText()
    {
        _model.OnGet();

        Assert.Equal("IDLE", _model.StatusText);
        Assert.Equal("idle", _model.StatusCss);
    }

    [Fact]
    public void OnGet_WithNullNextRunTime_ShowsDesactivado()
    {
        _appState.SetNextRun(null);

        _model.OnGet();

        Assert.Equal("Desactivado", _model.NextRunDisplay);
        Assert.Equal("", _model.NextRunIso);
        Assert.True(_model.IsCountdownDisabled);
    }

    [Fact]
    public void OnGet_WithNextRunTime_FormatsCorrectly()
    {
        var nextRun = new DateTime(2025, 6, 15, 8, 0, 0);
        _appState.SetNextRun(nextRun);

        _model.OnGet();

        Assert.Equal("08:00:00", _model.NextRunDisplay);
        Assert.False(_model.IsCountdownDisabled);
        Assert.NotEmpty(_model.NextRunIso);
    }

    [Fact]
    public void OnGet_LoadsConfigSummary()
    {
        _configStore.Get().Returns(new BookingConfig
        {
            GameType = "Single",
            PreferredPeriod = "Tarde",
            BookingDay = "Mañana",
            PreferredTimeSlots = new List<string> { "10:00hs", "11:00hs" },
            PreferredCourts = new List<string> { "Cancha 1" }
        });

        _model.OnGet();

        Assert.Equal("Single", _model.GameType);
        Assert.Equal("Tarde", _model.PreferredPeriod);
        Assert.Equal("Mañana", _model.BookingDay);
        Assert.Equal("10:00hs, 11:00hs", _model.TimeSlots);
        Assert.Equal("Cancha 1", _model.Courts);
    }

    [Fact]
    public void OnGet_WithEmptyConfig_ShowsDashes()
    {
        _configStore.Get().Returns(new BookingConfig
        {
            PreferredTimeSlots = new List<string>(),
            PreferredCourts = new List<string>()
        });

        _model.OnGet();

        Assert.Equal("---", _model.TimeSlots);
        Assert.Equal("---", _model.Courts);
    }

    [Fact]
    public void OnGet_WhatsAppConnected_ReflectsState()
    {
        _appState.SetWhatsAppConnected(true);

        _model.OnGet();

        Assert.True(_model.WhatsAppConnected);
    }

    [Fact]
    public async Task OnGet_LoadsLogs()
    {
        await _logStore.LogInfoAsync("test log");

        _model.OnGet();

        Assert.Single(_model.Logs);
        Assert.Equal("test log", _model.Logs[0].Message);
    }
}
