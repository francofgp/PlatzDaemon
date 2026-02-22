using NSubstitute;
using PlatzDaemon.Models;
using PlatzDaemon.Pages;
using PlatzDaemon.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace PlatzDaemon.Tests.Pages;

public class ConfigModelTests
{
    private readonly IConfigStore _configStore;
    private readonly ConfigModel _model;

    public ConfigModelTests()
    {
        _configStore = Substitute.For<IConfigStore>();
        _model = new ConfigModel(_configStore);
        SetupPageContext(_model);
    }

    [Fact]
    public void OnGet_LoadsConfigValues()
    {
        var config = new BookingConfig
        {
            PreferredPeriod = "Tarde",
            GameType = "Single",
            BookingDay = "Mañana",
            PreferredTimeSlots = new List<string> { "10:00hs" },
            PreferredCourts = new List<string> { "Cancha 1" }
        };
        _configStore.Get().Returns(config);

        _model.OnGet();

        Assert.Equal("Tarde", _model.PreferredPeriod);
        Assert.Equal("Single", _model.GameType);
        Assert.Equal("Mañana", _model.BookingDay);
        Assert.Single(_model.PreferredTimeSlots);
        Assert.Single(_model.PreferredCourts);
    }

    [Fact]
    public async Task OnPostAsync_FiltersEmptyTimeSlots()
    {
        _configStore.Get().Returns(new BookingConfig());
        _model.PreferredTimeSlots = new List<string> { "18:00hs", "", "  ", "19:00hs" };
        _model.PreferredCourts = new List<string> { "Cancha 1" };

        await _model.OnPostAsync();

        await _configStore.Received(1).SaveAsync(Arg.Is<BookingConfig>(c =>
            c.PreferredTimeSlots.Count == 2 &&
            c.PreferredTimeSlots[0] == "18:00hs" &&
            c.PreferredTimeSlots[1] == "19:00hs"));
    }

    [Fact]
    public async Task OnPostAsync_FiltersEmptyCourts()
    {
        _configStore.Get().Returns(new BookingConfig());
        _model.PreferredTimeSlots = new List<string> { "18:00hs" };
        _model.PreferredCourts = new List<string> { "Cancha 1", "", "  ", "Cancha 2" };

        await _model.OnPostAsync();

        await _configStore.Received(1).SaveAsync(Arg.Is<BookingConfig>(c =>
            c.PreferredCourts.Count == 2 &&
            c.PreferredCourts[0] == "Cancha 1" &&
            c.PreferredCourts[1] == "Cancha 2"));
    }

    [Fact]
    public async Task OnPostAsync_SavesBookingFields()
    {
        _configStore.Get().Returns(new BookingConfig());
        _model.PreferredPeriod = "Mañana";
        _model.GameType = "Single";
        _model.BookingDay = "Mañana";
        _model.PreferredTimeSlots = new List<string> { "10:00hs" };
        _model.PreferredCourts = new List<string> { "Cancha 5" };

        await _model.OnPostAsync();

        await _configStore.Received(1).SaveAsync(Arg.Is<BookingConfig>(c =>
            c.PreferredPeriod == "Mañana" &&
            c.GameType == "Single" &&
            c.BookingDay == "Mañana"));
    }

    [Fact]
    public async Task OnPostAsync_ReturnsRedirectResult()
    {
        _configStore.Get().Returns(new BookingConfig());
        _model.PreferredTimeSlots = new List<string>();
        _model.PreferredCourts = new List<string>();

        var result = await _model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    private static void SetupPageContext(PageModel page)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor());
        page.PageContext = new PageContext(actionContext);
        page.TempData = new TempDataDictionary(httpContext, Substitute.For<ITempDataProvider>());
    }
}
