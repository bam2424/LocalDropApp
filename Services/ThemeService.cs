using Microsoft.Maui.Controls;

namespace LocalDropApp.Services
{
    public class ThemeService
    {
        public bool IsDarkTheme
        {
            get => Application.Current?.RequestedTheme == AppTheme.Dark;
        }

        public void SetTheme(AppTheme theme)
        {
            if (Application.Current != null)
            {
                Application.Current.UserAppTheme = theme;
            }
        }

        public void ToggleTheme()
        {
            var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Dark;
            var newTheme = currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            SetTheme(newTheme);
        }

        public string GetThemeDisplayName()
        {
            return IsDarkTheme ? "Dark Mode" : "Light Mode";
        }

        public string GetThemeIcon()
        {
            return IsDarkTheme ? "🌙" : "☀️";
        }
    }
}