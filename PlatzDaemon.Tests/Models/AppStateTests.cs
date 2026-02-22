using PlatzDaemon.Models;

namespace PlatzDaemon.Tests.Models;

public class AppStateTests
{
    [Fact]
    public void DefaultStatus_IsIdle()
    {
        var state = new AppState();
        Assert.Equal(DaemonStatus.Idle, state.Status);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var state = new AppState();

        Assert.Null(state.LastResult);
        Assert.Null(state.LastRunTime);
        Assert.Null(state.NextRunTime);
        Assert.False(state.WhatsAppConnected);
    }

    [Fact]
    public void DaemonStatus_HasAllExpectedValues()
    {
        var values = Enum.GetValues<DaemonStatus>();

        Assert.Equal(5, values.Length);
        Assert.Contains(DaemonStatus.Idle, values);
        Assert.Contains(DaemonStatus.Waiting, values);
        Assert.Contains(DaemonStatus.Running, values);
        Assert.Contains(DaemonStatus.Completed, values);
        Assert.Contains(DaemonStatus.Error, values);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var state = new AppState
        {
            Status = DaemonStatus.Running,
            LastResult = "OK",
            LastRunTime = DateTime.Now,
            NextRunTime = DateTime.Now.AddHours(1),
            WhatsAppConnected = true
        };

        Assert.Equal(DaemonStatus.Running, state.Status);
        Assert.Equal("OK", state.LastResult);
        Assert.NotNull(state.LastRunTime);
        Assert.NotNull(state.NextRunTime);
        Assert.True(state.WhatsAppConnected);
    }
}
