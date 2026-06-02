using CleanEverydayMobile.Services;
using CleanEverydayMobile.Views;
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using DatadogSdk.Maui.Hosting;
using Microsoft.Extensions.Logging;

namespace CleanEverydayMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        const string apiHost = "dd-cleaneveryday-api.azurewebsites.net";

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseDatadogSdk(new DdSdkConfiguration
            {

                ClientToken = AppSecrets.DatadogClientToken,
                Service = "cleaneveryday-maui",
                Environment = "demo",
                Site = DatadogSite.Us1,
                TrackingConsent = TrackingConsent.Granted,
                NativeCrashReportEnabled = true,
                FirstPartyHosts =
                [
                    new ()
                    {
                        Match = apiHost, HeaderTypes = new List<TracingHeaderType>
                        {
                            TracingHeaderType.Datadog, TracingHeaderType.TraceContext
                        }
                    }
                ]
            })
            .UseDatadogTrace()
            .UseDatadogLogs(new DdLogsConfiguration { })
            .UseDatadogRum(new DdRumConfiguration
            {
                ApplicationId = AppSecrets.DatadogRumApplicationId,
                SessionSampleRate = 100.0,
                ResourceTraceSampleRate = 100.0,
                TrackFrustrations = true,
                TrackBackgroundEvents = true,
                TrackMemoryWarnings = true

            })
            .UseDatadogSessionReplay(new SessionReplayConfiguration
            {
                ReplaySampleRate = 100.0,
                TextAndInputPrivacyLevel = TextAndInputPrivacy.MaskSensitiveInputs,
                ImagePrivacyLevel = ImagePrivacy.MaskNone,
                TouchPrivacyLevel = TouchPrivacy.Show,
            });

        builder.Logging.AddDebug();
        builder.Logging.AddProvider(new DatadogLoggerProvider());

        builder.Services.AddTransient(_ => new DatadogTracingHandler([apiHost]));
        builder.Services
            .AddHttpClient<ApiService>()
            .AddHttpMessageHandler<DatadogTracingHandler>();
        builder.Services.AddSingleton<SessionService>();

        builder.Services.AddTransient<AppShell>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<LocationSelectionPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ChecklistPage>();
        builder.Services.AddTransient<PrintersPage>();
        builder.Services.AddTransient<TemperatureCheckerPage>();

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application starting up");

        return app;
    }
}
