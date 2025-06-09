using LocalDropApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LocalDropApp;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        
        // Set dark mode as default theme
        UserAppTheme = AppTheme.Dark;
        
        // Initialize flyout colors
        var themeService = services.GetRequiredService<ThemeService>();
        themeService.InitializeFlyoutColors();
        
        MainPage = services.GetRequiredService<AppShell>();
    }
}