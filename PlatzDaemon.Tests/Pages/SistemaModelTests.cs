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

public class SistemaModelTests
{
    private readonly IConfigStore _configStore;
    private readonly SistemaModel _model;

    public SistemaModelTests()
    {
        _configStore = Substitute.For<IConfigStore>();
        _model = new SistemaModel(_configStore);
        SetupPageContext(_model);
    }

    [Fact]
    public void OnGet_LoadsAllSystemFields()
    {
        var config = new BookingConfig
        {
            Enabled = true,
            BotPhoneNumber = "123456",
            Dni = "99999999",
            TriggerTime = "09:30",
            CompetitiveMode = false
        };
        _configStore.Get().Returns(config);

        _model.OnGet();

        Assert.True(_model.Enabled);
        Assert.Equal("123456", _model.BotPhoneNumber);
        Assert.Equal("99999999", _model.Dni);
        Assert.Equal("09:30", _model.TriggerTime);
        Assert.False(_model.CompetitiveMode);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesOnlySystemFields_PreservesBookingFields()
    {
        var existingConfig = new BookingConfig
        {
            PreferredPeriod = "Noche",
            GameType = "Doble",
            PreferredTimeSlots = new List<string> { "18:00hs" },
            PreferredCourts = new List<string> { "Cancha Central" }
        };
        _configStore.Get().Returns(existingConfig);

        _model.Enabled = false;
        _model.BotPhoneNumber = "555";
        _model.Dni = "11111";
        _model.TriggerTime = "07:00";
        _model.CompetitiveMode = true;

        await _model.OnPostAsync();

        await _configStore.Received(1).SaveAsync(Arg.Is<BookingConfig>(c =>
            c.Enabled == false &&
            c.BotPhoneNumber == "555" &&
            c.Dni == "11111" &&
            c.TriggerTime == "07:00" &&
            c.CompetitiveMode == true &&
            c.PreferredPeriod == "Noche" &&
            c.GameType == "Doble"));
    }

    [Fact]
    public async Task OnPostAsync_ReturnsRedirectResult()
    {
        _configStore.Get().Returns(new BookingConfig());

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
