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

                // ClientToken = DeviceInfo.Current.Platform == DevicePlatform.Android ? "pubced8336aefca9f20f1042281751cf327" : "pubdc8921c28adb3cafd21e366141a0b501",
                // Service = DeviceInfo.Current.Platform == DevicePlatform.Android ? "cleaneveryday-android" : "cleaneveryday-ios",
                ClientToken = "pub47b5608dcd99227e3b921bb39eab7af4",
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
                // ApplicationId = DeviceInfo.Current.Platform == DevicePlatform.Android ? "f3bedbc4-4281-4aa2-b5f7-381d30575a34" : "e4043840-b4b8-455d-adb2-8b9fc8a7f157",
                ApplicationId = "08378444-1c4e-4555-988e-57143bd49100",
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

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("Application starting up");

        return app;
    }
}
