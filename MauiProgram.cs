using CleanEverydayMobile.Services;
using CleanEverydayMobile.Views;
using Microsoft.Extensions.Logging;

namespace CleanEverydayMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Logging.AddDebug();

        builder.Services.AddHttpClient<ApiService>();
        builder.Services.AddSingleton<SessionService>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LocationSelectionPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ChecklistPage>();
        builder.Services.AddTransient<PrintersPage>();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application starting up");

        return app;
    }
}
