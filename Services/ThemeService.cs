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
                UpdateFlyoutColors();
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

        private void UpdateFlyoutColors()
        {
            if (Application.Current?.Resources == null) return;

            var resources = Application.Current.Resources;
            var isDark = IsDarkTheme;

            // Update flyout background color
            if (resources.TryGetValue(isDark ? "FlyoutBackgroundColorDark" : "FlyoutBackgroundColorLight", out var bgColor))
            {
                resources["FlyoutBackgroundColor"] = bgColor;
            }

            // Update flyout card background color
            if (resources.TryGetValue(isDark ? "FlyoutCardBackgroundColorDark" : "FlyoutCardBackgroundColorLight", out var cardBgColor))
            {
                resources["FlyoutCardBackgroundColor"] = cardBgColor;
            }

            // Update flyout title color
            if (resources.TryGetValue(isDark ? "FlyoutTitleColorDark" : "FlyoutTitleColorLight", out var titleColor))
            {
                resources["FlyoutTitleColor"] = titleColor;
            }

            // Update flyout subtitle color
            if (resources.TryGetValue(isDark ? "FlyoutSubtitleColorDark" : "FlyoutSubtitleColorLight", out var subtitleColor))
            {
                resources["FlyoutSubtitleColor"] = subtitleColor;
            }
        }

        public void InitializeFlyoutColors()
        {
            UpdateFlyoutColors();
        }
    }
}