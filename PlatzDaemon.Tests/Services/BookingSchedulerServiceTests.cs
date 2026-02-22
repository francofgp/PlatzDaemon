using PlatzDaemon.Services;

namespace PlatzDaemon.Tests.Services;

public class BookingSchedulerServiceTests
{
    [Theory]
    [InlineData(2, 30, 0, "2h 30m")]
    [InlineData(1, 0, 0, "1h 0m")]
    [InlineData(0, 5, 10, "5m 10s")]
    [InlineData(0, 1, 0, "1m 0s")]
    [InlineData(0, 0, 45, "45s")]
    [InlineData(0, 0, 0, "0s")]
    public void FormatTimeSpan_ReturnsExpectedFormat(int hours, int minutes, int seconds, string expected)
    {
        var ts = new TimeSpan(hours, minutes, seconds);
        var result = BookingSchedulerService.FormatTimeSpan(ts);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatTimeSpan_WithLargeHours_ShowsTotalHours()
    {
        var ts = new TimeSpan(25, 15, 0);
        var result = BookingSchedulerService.FormatTimeSpan(ts);
        Assert.Equal("25h 15m", result);
    }

    [Fact]
    public void FormatTimeSpan_ExactlyOneHour_ShowsHoursFormat()
    {
        var ts = TimeSpan.FromHours(1);
        var result = BookingSchedulerService.FormatTimeSpan(ts);
        Assert.Equal("1h 0m", result);
    }

    [Fact]
    public void FormatTimeSpan_ExactlyOneMinute_ShowsMinutesFormat()
    {
        var ts = TimeSpan.FromMinutes(1);
        var result = BookingSchedulerService.FormatTimeSpan(ts);
        Assert.Equal("1m 0s", result);
    }

    [Fact]
    public void FormatTimeSpan_59Minutes59Seconds_ShowsMinutesFormat()
    {
        var ts = new TimeSpan(0, 59, 59);
        var result = BookingSchedulerService.FormatTimeSpan(ts);
        Assert.Equal("59m 59s", result);
    }

    // ── CalculateNextTrigger tests ──

    [Fact]
    public void CalculateNextTrigger_BeforeTriggerTime_ReturnsTodayTrigger()
    {
        var now = new DateTime(2025, 6, 15, 7, 30, 0);
        var trigger = TimeSpan.Parse("08:00");

        var result = BookingSchedulerService.CalculateNextTrigger(now, trigger);

        Assert.Equal(new DateTime(2025, 6, 15, 8, 0, 0), result);
    }

    [Fact]
    public void CalculateNextTrigger_AfterTriggerTimePlus5Min_ReturnsTomorrow()
    {
        var now = new DateTime(2025, 6, 15, 8, 6, 0);
        var trigger = TimeSpan.Parse("08:00");

        var result = BookingSchedulerService.CalculateNextTrigger(now, trigger);

        Assert.Equal(new DateTime(2025, 6, 16, 8, 0, 0), result);
    }

    [Fact]
    public void CalculateNextTrigger_Within5MinGracePeriod_ReturnsToday()
    {
        var now = new DateTime(2025, 6, 15, 8, 4, 0);
        var trigger = TimeSpan.Parse("08:00");

        var result = BookingSchedulerService.CalculateNextTrigger(now, trigger);

        Assert.Equal(new DateTime(2025, 6, 15, 8, 0, 0), result);
    }

    [Fact]
    public void CalculateNextTrigger_ExactlyAtTriggerTime_ReturnsToday()
    {
        var now = new DateTime(2025, 6, 15, 8, 0, 0);
        var trigger = TimeSpan.Parse("08:00");

        var result = BookingSchedulerService.CalculateNextTrigger(now, trigger);

        Assert.Equal(new DateTime(2025, 6, 15, 8, 0, 0), result);
    }

    [Fact]
    public void CalculateNextTrigger_Midnight_SchedulesCorrectly()
    {
        var now = new DateTime(2025, 6, 15, 23, 50, 0);
        var trigger = TimeSpan.Parse("08:00");

        var result = BookingSchedulerService.CalculateNextTrigger(now, trigger);

        Assert.Equal(new DateTime(2025, 6, 16, 8, 0, 0), result);
    }

    [Fact]
    public void CalculateNextTrigger_EarlyMorning_ReturnsTodayTrigger()
    {
        var now = new DateTime(2025, 6, 15, 5, 0, 0);
        var trigger = TimeSpan.Parse("08:00");

        var result = BookingSchedulerService.CalculateNextTrigger(now, trigger);

        Assert.Equal(new DateTime(2025, 6, 15, 8, 0, 0), result);
    }
}
