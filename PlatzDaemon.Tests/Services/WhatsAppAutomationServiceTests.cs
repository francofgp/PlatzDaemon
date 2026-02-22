using Microsoft.Playwright;
using PlatzDaemon.Services;

namespace PlatzDaemon.Tests.Services;

public class WhatsAppAutomationServiceTests
{
    [Theory]
    [InlineData("Target closed")]
    [InlineData("browser has been closed")]
    [InlineData("Browser closed unexpectedly")]
    [InlineData("connection disposed")]
    [InlineData("Object is not connected")]
    [InlineData("Session closed")]
    [InlineData("Target crashed")]
    [InlineData("Connection refused")]
    public void IsBrowserClosedException_WithKnownMessages_ReturnsTrue(string message)
    {
        var ex = new Exception(message);
        Assert.True(WhatsAppAutomationService.IsBrowserClosedException(ex));
    }

    [Fact]
    public void IsBrowserClosedException_WithPlaywrightException_ContainingClosed_ReturnsTrue()
    {
        var ex = new PlaywrightException("Target page, context or browser has been closed");
        Assert.True(WhatsAppAutomationService.IsBrowserClosedException(ex));
    }

    [Theory]
    [InlineData("Timeout exceeded")]
    [InlineData("Element not found")]
    [InlineData("Navigation failed")]
    [InlineData("")]
    public void IsBrowserClosedException_WithUnrelatedMessages_ReturnsFalse(string message)
    {
        var ex = new Exception(message);
        Assert.False(WhatsAppAutomationService.IsBrowserClosedException(ex));
    }

    [Fact]
    public void IsBrowserClosedException_CaseInsensitive()
    {
        var ex = new Exception("TARGET CLOSED");
        Assert.True(WhatsAppAutomationService.IsBrowserClosedException(ex));
    }
}
