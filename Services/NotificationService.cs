using System.Diagnostics;
using System.Media;

namespace PlatzDaemon.Services;

public class NotificationService
{
    public void NotifySuccess(string title, string message)
    {
        ShowWindowsNotification(title, message);
        PlaySound();
    }

    public void NotifyError(string title, string message)
    {
        ShowWindowsNotification(title, message);
        PlaySound();
    }

    private void ShowWindowsNotification(string title, string message)
    {
        try
        {
            // Use PowerShell to display a Windows toast notification
            var script = $@"
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
                $xml = @""
                <toast>
                    <visual>
                        <binding template=""ToastGeneric"">
                            <text>{EscapeXml(title)}</text>
                            <text>{EscapeXml(message)}</text>
                        </binding>
                    </visual>
                </toast>
""@
                $xDoc = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xDoc.LoadXml($xml)
                $toast = [Windows.UI.Notifications.ToastNotification]::new($xDoc)
                [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Platz Daemon').Show($toast)
            ";

            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch
        {
            // Silently fail if toast is not available
        }
    }

    private void PlaySound()
    {
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            // Sound may fail silently
        }
    }

    private static string EscapeXml(string text)
    {
        return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
