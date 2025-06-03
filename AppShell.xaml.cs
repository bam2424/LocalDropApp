using LocalDropApp.Services;

namespace LocalDropApp;

public partial class AppShell : Shell
{
    private readonly ThemeService _themeService;

    public AppShell(ThemeService themeService)
    {
        _themeService = themeService;
        InitializeComponent();
        UpdateThemeButtonText();
    }

    private void OnThemeToggleClicked(object sender, EventArgs e)
    {
        _themeService.ToggleTheme();
        UpdateThemeButtonText();
    }

    private void UpdateThemeButtonText()
    {
        if (ThemeToggleButton != null)
        {
            ThemeToggleButton.Text = $"{_themeService.GetThemeIcon()} {_themeService.GetThemeDisplayName()}";
        }
    }
}