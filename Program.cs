using System.Diagnostics;
using PlatzDaemon.Hubs;
using PlatzDaemon.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
builder.WebHost.UseUrls("http://localhost:5000");

// Add services
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Register app services as singletons (shared state across the app)
builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<LogStore>();
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<WhatsAppAutomationService>();
builder.Services.AddSingleton<BookingSchedulerService>();

// Register the scheduler as a hosted background service
builder.Services.AddHostedService(sp => sp.GetRequiredService<BookingSchedulerService>());

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseRouting();
app.UseStaticFiles();
app.MapRazorPages();
app.MapHub<LogHub>("/loghub");

// Auto-open browser in production mode
if (!app.Environment.IsDevelopment())
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(1500);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:5000",
                UseShellExecute = true
            });
        }
        catch { }
    });
}

app.Run();
