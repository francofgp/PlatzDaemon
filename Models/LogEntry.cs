namespace PlatzDaemon.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Message { get; set; } = "";
    public LogLevel Level { get; set; } = LogLevel.Info;

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public string Prefix => Level switch
    {
        LogLevel.Success => "[OK]",
        LogLevel.Error => "[ERR]",
        LogLevel.Warning => "[WARN]",
        _ => "[INFO]"
    };

    public string CssClass => Level switch
    {
        LogLevel.Success => "log-success",
        LogLevel.Error => "log-error",
        LogLevel.Warning => "log-warning",
        _ => "log-info"
    };
}

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}
