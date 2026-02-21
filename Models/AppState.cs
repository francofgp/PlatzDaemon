namespace PlatzDaemon.Models;

public class AppState
{
    public DaemonStatus Status { get; set; } = DaemonStatus.Idle;
    public string? LastResult { get; set; }
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }
    public bool WhatsAppConnected { get; set; } = false;
}

public enum DaemonStatus
{
    Idle,
    Waiting,
    Running,
    Completed,
    Error
}
