using LocalDropApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LocalDropApp;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        MainPage = services.GetRequiredService<AppShell>();
    }
}