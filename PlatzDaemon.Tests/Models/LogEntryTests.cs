using PlatzDaemon.Models;

namespace PlatzDaemon.Tests.Models;

public class LogEntryTests
{
    [Fact]
    public void FormattedTime_ReturnsHHmmssfffFormat()
    {
        var entry = new LogEntry { Timestamp = new DateTime(2025, 1, 15, 14, 30, 45, 123) };
        Assert.Equal("14:30:45.123", entry.FormattedTime);
    }

    [Theory]
    [InlineData(LogLevel.Info, "[INFO]")]
    [InlineData(LogLevel.Success, "[OK]")]
    [InlineData(LogLevel.Warning, "[WARN]")]
    [InlineData(LogLevel.Error, "[ERR]")]
    public void Prefix_ReturnsCorrectValueForLevel(LogLevel level, string expected)
    {
        var entry = new LogEntry { Level = level };
        Assert.Equal(expected, entry.Prefix);
    }

    [Theory]
    [InlineData(LogLevel.Info, "log-info")]
    [InlineData(LogLevel.Success, "log-success")]
    [InlineData(LogLevel.Warning, "log-warning")]
    [InlineData(LogLevel.Error, "log-error")]
    public void CssClass_ReturnsCorrectValueForLevel(LogLevel level, string expected)
    {
        var entry = new LogEntry { Level = level };
        Assert.Equal(expected, entry.CssClass);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var entry = new LogEntry();
        Assert.Equal("", entry.Message);
        Assert.Equal(LogLevel.Info, entry.Level);
    }
}
