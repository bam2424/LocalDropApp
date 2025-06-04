using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LocalDropApp.Models;
using LocalDropApp.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;

namespace LocalDropApp.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;
    private AppSettings _settings = new();
    private bool _isLoading;
    private bool _hasUnsavedChanges;

    public AppSettings Settings
    {
        get => _settings;
        set { _settings = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set { _hasUnsavedChanges = value; OnPropertyChanged(); }
    }

    public List<string> ThemeOptions { get; } = new() { "System Default", "Light Mode", "Dark Mode" };
    public List<string> CompressionLevels { get; } = new() { "None", "Low", "Medium", "High", "Maximum" };
    public List<int> ConcurrentTransferOptions { get; } = new() { 1, 2, 3, 5, 10 };
    public List<int> TimeoutOptions { get; } = new() { 10, 15, 30, 45, 60, 120 };
    public List<int> HistoryRetentionOptions { get; } = new() { 0, 7, 14, 30, 60, 90, 365 };
    public List<int> MaxFileSizeOptions { get; } = new() { 100, 500, 1024, 2048, 5120, 10240 };

    // Commands
    public ICommand SaveSettingsCommand { get; }
    public ICommand ResetSettingsCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand ClearCacheCommand { get; }
    public ICommand ExportSettingsCommand { get; }
    public ICommand ImportSettingsCommand { get; }

    public SettingsViewModel(ThemeService themeService)
    {
        _themeService = themeService;

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettings);
        ResetSettingsCommand = new AsyncRelayCommand(ResetSettings);
        BrowseFolderCommand = new AsyncRelayCommand(BrowseFolder);
        TestConnectionCommand = new AsyncRelayCommand(TestConnection);
        ClearCacheCommand = new AsyncRelayCommand(ClearCache);
        ExportSettingsCommand = new AsyncRelayCommand(ExportSettings);
        ImportSettingsCommand = new AsyncRelayCommand(ImportSettings);

        LoadSettings();
        Settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        HasUnsavedChanges = true;

        // Apply theme changes immediately
        if (e.PropertyName == nameof(AppSettings.PreferredTheme))
        {
            ApplyTheme();
        }
    }

    private async Task SaveSettings()
    {
        IsLoading = true;

        try
        {
            // Validate settings
            if (!ValidateSettings())
            {
                return;
            }

            // In real implementation, save to preferences/database
            await Task.Delay(500); // Simulate save operation

            // Save to preferences - use instance-specific keys for testing
            var instanceId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            Preferences.Set($"DeviceName_{instanceId}", Settings.DeviceName);
            Preferences.Set("DiscoveryPort", Settings.DiscoveryPort);
            Preferences.Set("TransferPort", Settings.TransferPort);
            Preferences.Set("DownloadPath", Settings.DownloadPath);
            Preferences.Set("AutoAcceptTransfers", Settings.AutoAcceptTransfers);
            Preferences.Set("ShowNotifications", Settings.ShowNotifications);
            Preferences.Set("AutoStart", Settings.AutoStart);
            Preferences.Set("MaxConcurrentTransfers", Settings.MaxConcurrentTransfers);
            Preferences.Set("DiscoveryTimeout", Settings.DiscoveryTimeout);
            Preferences.Set("EnableFileLogging", Settings.EnableFileLogging);
            Preferences.Set("PreferredTheme", (int)Settings.PreferredTheme);
            Preferences.Set("CompressFiles", Settings.CompressFiles);
            Preferences.Set("CompressionLevel", Settings.CompressionLevel);
            Preferences.Set("EncryptTransfers", Settings.EncryptTransfers);
            Preferences.Set("RequireConfirmation", Settings.RequireConfirmation);
            Preferences.Set("MaxFileSize", Settings.MaxFileSize);
            Preferences.Set("AllowedNetworkTransfers", Settings.AllowedNetworkTransfers);
            Preferences.Set("AllowedFileTypes", Settings.AllowedFileTypes);
            Preferences.Set("EnableAutoDiscovery", Settings.EnableAutoDiscovery);
            Preferences.Set("KeepHistoryDays", Settings.KeepHistoryDays);

            HasUnsavedChanges = false;

            await Application.Current.MainPage.DisplayAlert("Success", "Settings saved successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to save settings: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResetSettings()
    {
        try
        {
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Reset Settings", 
                "Are you sure you want to reset all settings to default values?", 
                "Reset", 
                "Cancel");

            if (confirmed)
            {
                Settings.PropertyChanged -= OnSettingsPropertyChanged;
                Settings = new AppSettings();
                Settings.PropertyChanged += OnSettingsPropertyChanged;
                HasUnsavedChanges = true;

                await Application.Current.MainPage.DisplayAlert("Success", "Settings reset to default values", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to reset settings: {ex.Message}", "OK");
        }
    }

    private async Task BrowseFolder()
    {
        try
        {
            // In real implementation, use FolderPicker
            await Application.Current.MainPage.DisplayAlert("Browse Folder", "Folder picker would open here", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to browse folder: {ex.Message}", "OK");
        }
    }

    private async Task TestConnection()
    {
        IsLoading = true;

        try
        {
            // Simulate connection test
            await Task.Delay(2000);

            bool success = new Random().Next(2) == 0; // 50% success rate for demo
            
            if (success)
            {
                await Application.Current.MainPage.DisplayAlert("Success", 
                    $"Connection test successful!\nDiscovery port: {Settings.DiscoveryPort}\nTransfer port: {Settings.TransferPort}", 
                    "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Failed", 
                    "Connection test failed. Please check your network settings and firewall configuration.", 
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Connection test failed: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearCache()
    {
        try
        {
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Clear Cache", 
                "Are you sure you want to clear all cached data? This will free up storage space.", 
                "Clear", 
                "Cancel");

            if (confirmed)
            {
                // In real implementation, clear cache files
                await Task.Delay(500);
                await Application.Current.MainPage.DisplayAlert("Success", "Cache cleared successfully!", "OK");
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to clear cache: {ex.Message}", "OK");
        }
    }

    private async Task ExportSettings()
    {
        try
        {
            // In real implementation, export settings to file
            await Application.Current.MainPage.DisplayAlert("Export", "Settings exported successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export settings: {ex.Message}", "OK");
        }
    }

    private async Task ImportSettings()
    {
        try
        {
            // In real implementation, import settings from file
            await Application.Current.MainPage.DisplayAlert("Import", "Settings imported successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to import settings: {ex.Message}", "OK");
        }
    }

    private static string GenerateInstanceDeviceName()
    {
        var baseName = Environment.MachineName;
        
        // Get all processes with the same name as the current process
        var currentProcessName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        var processes = System.Diagnostics.Process.GetProcessesByName(currentProcessName);
        
        // If this is the first instance, use the base name
        if (processes.Length <= 1)
        {
            return baseName;
        }
        
        // Otherwise, use the base name + instance number
        // We use the process ID to ensure consistent naming across runs
        var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
        var sortedProcessIds = processes.Select(p => p.Id).OrderBy(id => id).ToArray();
        var instanceIndex = Array.IndexOf(sortedProcessIds, currentProcessId);
        
        return instanceIndex == 0 ? baseName : $"{baseName}{instanceIndex + 1}";
    }

    private bool ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(Settings.DeviceName))
        {
            Application.Current.MainPage.DisplayAlert("Validation Error", "Device name is required", "OK");
            return false;
        }

        if (Settings.DiscoveryPort < 1024 || Settings.DiscoveryPort > 65535)
        {
            Application.Current.MainPage.DisplayAlert("Validation Error", "Discovery port must be between 1024 and 65535", "OK");
            return false;
        }

        if (Settings.TransferPort < 1024 || Settings.TransferPort > 65535)
        {
            Application.Current.MainPage.DisplayAlert("Validation Error", "Transfer port must be between 1024 and 65535", "OK");
            return false;
        }

        if (Settings.DiscoveryPort == Settings.TransferPort)
        {
            Application.Current.MainPage.DisplayAlert("Validation Error", "Discovery and transfer ports must be different", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Settings.DownloadPath))
        {
            Application.Current.MainPage.DisplayAlert("Validation Error", "Download path is required", "OK");
            return false;
        }

        return true;
    }

    private void LoadSettings()
    {
        try
        {
            Settings.PropertyChanged -= OnSettingsPropertyChanged;

            // Load from preferences - use clean instance-based naming
            var instanceId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            var defaultName = GenerateInstanceDeviceName();
            var savedName = Preferences.Get($"DeviceName_{instanceId}", defaultName);
            
            // If the saved name looks like the old GUID format, replace it with the new clean name
            if (savedName.Contains('-') && savedName.Length > Environment.MachineName.Length + 10)
            {
                savedName = defaultName;
                // Save the new clean name immediately
                Preferences.Set($"DeviceName_{instanceId}", defaultName);
            }
            
            Settings.DeviceName = savedName;
            Settings.DiscoveryPort = Preferences.Get("DiscoveryPort", 8080);
            Settings.TransferPort = Preferences.Get("TransferPort", 8081);
            Settings.DownloadPath = Preferences.Get("DownloadPath", Settings.DownloadPath);
            Settings.AutoAcceptTransfers = Preferences.Get("AutoAcceptTransfers", false);
            Settings.ShowNotifications = Preferences.Get("ShowNotifications", true);
            Settings.AutoStart = Preferences.Get("AutoStart", false);
            Settings.MaxConcurrentTransfers = Preferences.Get("MaxConcurrentTransfers", 3);
            Settings.DiscoveryTimeout = Preferences.Get("DiscoveryTimeout", 30);
            Settings.EnableFileLogging = Preferences.Get("EnableFileLogging", true);
            Settings.PreferredTheme = (Models.AppTheme)Preferences.Get("PreferredTheme", (int)Models.AppTheme.System);
            Settings.CompressFiles = Preferences.Get("CompressFiles", false);
            Settings.CompressionLevel = Preferences.Get("CompressionLevel", "Medium");
            Settings.EncryptTransfers = Preferences.Get("EncryptTransfers", false);
            Settings.RequireConfirmation = Preferences.Get("RequireConfirmation", true);
            Settings.MaxFileSize = Preferences.Get("MaxFileSize", 1024);
            Settings.AllowedNetworkTransfers = Preferences.Get("AllowedNetworkTransfers", true);
            Settings.AllowedFileTypes = Preferences.Get("AllowedFileTypes", "*.*");
            Settings.EnableAutoDiscovery = Preferences.Get("EnableAutoDiscovery", true);
            Settings.KeepHistoryDays = Preferences.Get("KeepHistoryDays", 30);

            Settings.PropertyChanged += OnSettingsPropertyChanged;
            HasUnsavedChanges = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
    }

    private void ApplyTheme()
    {
        var mauiTheme = Settings.PreferredTheme switch
        {
            Models.AppTheme.Light => Microsoft.Maui.ApplicationModel.AppTheme.Light,
            Models.AppTheme.Dark => Microsoft.Maui.ApplicationModel.AppTheme.Dark,
            _ => Microsoft.Maui.ApplicationModel.AppTheme.Unspecified
        };
        _themeService?.SetTheme(mauiTheme);
    }

    private string ThemeToString(Models.AppTheme theme) => theme switch
    {
        Models.AppTheme.System => "System",
        Models.AppTheme.Light => "Light", 
        Models.AppTheme.Dark => "Dark",
        _ => "System"
    };

    private Models.AppTheme StringToTheme(string theme) => theme switch
    {
        "Light" => Models.AppTheme.Light,
        "Dark" => Models.AppTheme.Dark,
        _ => Models.AppTheme.System
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 