using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using LocalDropApp.ViewModels;
using LocalDropApp.Services;
using LocalDropApp.Views;

namespace LocalDropApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services first - networking services must be singletons
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<IPeerDiscoveryService, PeerDiscoveryService>();
        builder.Services.AddSingleton<IFileTransferService, FileTransferService>();
        
        // Register ViewModels that might depend on services
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        
        // Register Pages and App
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<HistoryPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<App>();
        
        // Register Shell last since it depends on App and services
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}