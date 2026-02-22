using PlatzDaemon.Services;

namespace PlatzDaemon.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public void EscapeXml_EscapesAmpersand()
    {
        Assert.Equal("a &amp; b", NotificationService.EscapeXml("a & b"));
    }

    [Fact]
    public void EscapeXml_EscapesLessThan()
    {
        Assert.Equal("&lt;tag", NotificationService.EscapeXml("<tag"));
    }

    [Fact]
    public void EscapeXml_EscapesGreaterThan()
    {
        Assert.Equal("tag&gt;", NotificationService.EscapeXml("tag>"));
    }

    [Fact]
    public void EscapeXml_EscapesQuotes()
    {
        Assert.Equal("a &quot;b&quot;", NotificationService.EscapeXml("a \"b\""));
    }

    [Fact]
    public void EscapeXml_EscapesAllSpecialChars()
    {
        var input = "Tom & Jerry <\"friends\">";
        var expected = "Tom &amp; Jerry &lt;&quot;friends&quot;&gt;";
        Assert.Equal(expected, NotificationService.EscapeXml(input));
    }

    [Fact]
    public void EscapeXml_NoSpecialChars_ReturnsSameString()
    {
        Assert.Equal("hello world", NotificationService.EscapeXml("hello world"));
    }

    [Fact]
    public void EscapeXml_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", NotificationService.EscapeXml(""));
    }
}
