﻿using Shiny;
using Microsoft.Extensions.Configuration;

namespace Sample;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Configuration.AddJsonPlatformBundle(optional: false);
        builder.Services.AddSingleton<SampleSqliteConnection>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<SetupPage>();
        builder.Services.AddTransient<SetupViewModel>();
        builder.Services.AddTransient<LogsPage>();
        builder.Services.AddTransient<LogsViewModel>();
        builder.Services.AddTransient<TagsPage>();
        builder.Services.AddTransient<TagsViewModel>();

#if FIREBASE
#if USE_PUSH_CONFIG
        // ios or android
        var cfg = builder.Configuration.GetSection("Firebase");
        builder.Services.AddPushFirebaseMessaging<MyPushDelegate>(new(
            false,
            cfg["AppId"],
            cfg["SenderId"],
            cfg["ProjectId"],
            cfg["ApiKey"]
        ));
#else
        builder.Services.AddFirebaseMessaging<MyPushDelegate>();
#endif

#elif NATIVE
#if USE_PUSH_CONFIG && ANDROID
        var cfg = builder.Configuration.GetSection("Firebase");
        builder.Services.AddPush<MyPushDelegate>(new (
            false,
            cfg["AppId"],
            cfg["SenderId"],
            cfg["ProjectId"],
            cfg["ApiKey"]
        ));
#else
        builder.Services.AddPush<MyPushDelegate>();
#endif
#elif AZURE
        var cfg = builder.Configuration.GetSection("AzureNotificationHubs");
        builder.Services.AddPushAzureNotificationHubs<MyPushDelegate>(
            cfg["ListenerConnectionString"],
            cfg["HubName"]
        );
#else
        throw new InvalidProgramException("No push provider configuration");
#endif

        return builder.Build();
    }
}

