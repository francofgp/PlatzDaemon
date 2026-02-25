using PlatzDaemon.Models;

namespace PlatzDaemon.Tests.Models;

public class BookingConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new BookingConfig();

        Assert.Equal("5493534407576", config.BotPhoneNumber);
        Assert.Equal("", config.Dni);
        Assert.Equal("08:00", config.TriggerTime);
        Assert.True(config.CompetitiveMode);
        Assert.Equal("Noche", config.PreferredPeriod);
        Assert.Equal("Doble", config.GameType);
        Assert.Equal("Hoy", config.BookingDay);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void DefaultTimeSlots_ContainsExpectedValues()
    {
        var config = new BookingConfig();

        Assert.Equal(3, config.PreferredTimeSlots.Count);
        Assert.Contains("18:00hs", config.PreferredTimeSlots);
        Assert.Contains("19:00hs", config.PreferredTimeSlots);
        Assert.Contains("17:30hs", config.PreferredTimeSlots);
    }

    [Fact]
    public void DefaultCourts_ContainsExpectedValues()
    {
        var config = new BookingConfig();

        Assert.Equal(2, config.PreferredCourts.Count);
        Assert.Contains("Cancha Central", config.PreferredCourts);
        Assert.Contains("Cancha 9", config.PreferredCourts);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var config = new BookingConfig
        {
            BotPhoneNumber = "111",
            Dni = "99999999",
            TriggerTime = "09:30",
            CompetitiveMode = false,
            PreferredPeriod = "Tarde",
            GameType = "Single",
            BookingDay = "Mañana",
            Enabled = false,
            PreferredTimeSlots = new List<string> { "10:00hs" },
            PreferredCourts = new List<string> { "Cancha 1" }
        };

        Assert.Equal("111", config.BotPhoneNumber);
        Assert.Equal("99999999", config.Dni);
        Assert.False(config.CompetitiveMode);
        Assert.Equal("Single", config.GameType);
        Assert.Single(config.PreferredTimeSlots);
        Assert.Single(config.PreferredCourts);
    }
}
