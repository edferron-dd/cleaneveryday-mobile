# Previewing the Datadog MAUI SDK

This bundle contains a pre-built preview of the Datadog SDK for .NET MAUI, distributed as NuGet packages so you can evaluate it before it ships on nuget.org.

## What's in the bundle

- `DatadogSdk.Maui.0.1.0.nupkg` — the meta-package you reference from your app
- `DatadogSdk.iOS.Binding.0.1.0.nupkg` — iOS native binding
- `DatadogSdk.Android.Binding.0.1.0.nupkg` — Android binding wrapper
- `DatadogSdk.Android.Core.0.1.0.nupkg` — Android core binding
- `DatadogSdk.Android.Internal.0.1.0.nupkg` — Android internal binding
- `DatadogSdk.Android.Logs.0.1.0.nupkg` — Android logs binding
- `DatadogSdk.Android.Trace.0.1.0.nupkg` — Android trace binding
- `DatadogSdk.Android.Rum.0.1.0.nupkg` — Android RUM binding
- `DatadogSdk.Android.SessionReplay.0.1.0.nupkg` — Android Session Replay binding
- `INSTALL.md` — this guide

You only need to reference `DatadogSdk.Maui` from your app — the rest resolve transitively.

## Prerequisites

- A .NET MAUI app targeting `net9.0-*` or `net10.0-*` (iOS and/or Android).
- The .NET MAUI workload installed (`dotnet workload install maui`).
- **iOS:** minimum deployment target `15.0`.
- **Android:** minimum SDK level `23` (Android 6.0 Marshmallow).

## 1. Drop the packages into your app

Unzip the bundle into a folder at your solution or repo root, for example `datadog-packages/`:

```
your-app/
├── datadog-packages/
│   ├── DatadogSdk.Maui.0.1.0.nupkg
│   ├── DatadogSdk.iOS.Binding.0.1.0.nupkg
│   ├── DatadogSdk.Android.Binding.0.1.0.nupkg
│   ├── DatadogSdk.Android.Core.0.1.0.nupkg
│   ├── DatadogSdk.Android.Internal.0.1.0.nupkg
│   ├── DatadogSdk.Android.Logs.0.1.0.nupkg
│   ├── DatadogSdk.Android.Trace.0.1.0.nupkg
│   ├── DatadogSdk.Android.Rum.0.1.0.nupkg
│   └── DatadogSdk.Android.SessionReplay.0.1.0.nupkg
├── NuGet.Config
└── YourApp.csproj
```

## 2. Add a `NuGet.Config`

Create `NuGet.Config` next to your solution / `.csproj` so NuGet knows where to look:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="datadog-maui-local" value="./datadog-packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

The path is relative to the `NuGet.Config` file. Keep `nuget.org` in the list — the bindings have transitive dependencies (`Microsoft.Maui.Controls`, `Xamarin.AndroidX.*`, etc.) that resolve from there.

## 3. Reference the package

Add the following to your app's `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="DatadogSdk.Maui" Version="0.1.0" />
</ItemGroup>
```

Then restore:

```bash
dotnet restore
```

## 4. Initialize the SDK in `MauiProgram.cs`

Add the using directives at the top of the file:

```csharp
using DatadogSdk.Maui;
using DatadogSdk.Maui.Configuration;
using DatadogSdk.Maui.Hosting;
```

Then chain the Datadog calls onto your `MauiAppBuilder`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(...)
        .UseDatadogSdk(new DdSdkConfiguration
        {
            ClientToken = "<CLIENT_TOKEN>",
            Environment = "dev",
            Service = "<SERVICE_NAME>",
            Site = DatadogSite.Us1,
            TrackingConsent = TrackingConsent.Granted,
        })
        .UseDatadogLogs(new DdLogsConfiguration { })
        .UseDatadogTrace(new DdTraceConfiguration { })
        .UseDatadogRum(new DdRumConfiguration
        {
            ApplicationId = "<RUM_APPLICATION_ID>",
            SessionSampleRate = 100.0,
            ResourceTraceSampleRate = 100.0,
            TrackFrustrations = true,
            TrackBackgroundEvents = true,
            TrackMemoryWarnings = true,
        })
        .UseDatadogSessionReplay(new SessionReplayConfiguration
        {
            ReplaySampleRate = 100.0,
            TextAndInputPrivacyLevel = TextAndInputPrivacy.MaskSensitiveInputs,
            ImagePrivacyLevel = ImagePrivacy.MaskNone,
            TouchPrivacyLevel = TouchPrivacy.Show,
        });

    return builder.Build();
}
```

Replace `<CLIENT_TOKEN>` and `<RUM_APPLICATION_ID>` with the values from your Datadog org. Set your `<SERVICE_NAME>` and pick the `Site` that matches your account.

Each `.UseDatadog*(...)` call is optional — include only the features you want to evaluate.

## 5. Build and run

```bash
dotnet build -f net10.0-ios     # or net10.0-android, net9.0-ios, net9.0-android
```

Events should start flowing into your Datadog org within a minute or two. To confirm the SDK is initialized correctly, add `Verbosity = SdkVerbosity.DEBUG` to `DdSdkConfiguration` — you'll then see SDK initialization and event-upload logs at startup. Remove or lower the verbosity once you've verified things are working.

## Troubleshooting

**Restore picks up an old / wrong version of a package.** NuGet caches packages globally in `~/.nuget/packages/`. If you ever receive an updated `0.1.0` bundle, clear the cache:

```bash
rm -rf ~/.nuget/packages/datadogsdk.*
dotnet restore
```

**`NU1101: Unable to find package DatadogSdk.Maui`.** NuGet isn't seeing the local feed. Confirm `NuGet.Config` is at or above your `.csproj` in the directory tree, and that the `value=` path resolves to the folder containing the `.nupkg` files.
