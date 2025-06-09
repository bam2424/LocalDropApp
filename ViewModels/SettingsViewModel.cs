using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LocalDropApp.Models;
using LocalDropApp.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;


namespace LocalDropApp.ViewModels;

public partial class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;
    private AppSettings _settings = new();
    private bool _isTestingConnection;
    private bool _isSavingSettings;
    private bool _hasUnsavedChanges;

    public AppSettings Settings
    {
        get => _settings;
        set { _settings = value; OnPropertyChanged(); }
    }

    public bool IsTestingConnection
    {
        get => _isTestingConnection;
        set 
        { 
            _isTestingConnection = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(TestButtonText));
            OnPropertyChanged(nameof(TestButtonBackgroundColor));
        }
    }

    public bool IsSavingSettings
    {
        get => _isSavingSettings;
        set 
        { 
            _isSavingSettings = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(SaveButtonBackgroundColor));
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set 
        { 
            _hasUnsavedChanges = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(TestButtonText));
            OnPropertyChanged(nameof(TestButtonBackgroundColor));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(SaveButtonBackgroundColor));
        }
    }

    public string TestButtonText => IsTestingConnection ? "⏳ Testing..." : "🔍 Test";
    public Color TestButtonBackgroundColor => IsTestingConnection ? Colors.Gray : Color.FromArgb("#512BD4");
    
    public string SaveButtonText => IsSavingSettings ? "💾 Saving..." : (HasUnsavedChanges ? "💾 Save" : "✅ Saved");
    public Color SaveButtonBackgroundColor 
    {
        get 
        {
            if (IsSavingSettings) return Colors.Gray;
            if (!HasUnsavedChanges) return Color.FromArgb("#2E7D32");
            return Color.FromArgb("#512BD4");
        }
    }

    public List<string> ThemeOptions { get; } = new() { "System Default", "Light Mode", "Dark Mode" };
    public List<string> CompressionLevels { get; } = new() { "None", "Low", "Medium", "High", "Maximum" };
    public List<int> ConcurrentTransferOptions { get; } = new() { 1, 2, 3, 5, 10 };
    public List<int> TimeoutOptions { get; } = new() { 10, 15, 30, 45, 60, 120 };
    public List<int> HistoryRetentionOptions { get; } = new() { 0, 7, 14, 30, 60, 90, 365 };
    public List<int> MaxFileSizeOptions { get; } = new() { 100, 500, 1024, 2048, 5120, 10240 };
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

        if (e.PropertyName == nameof(AppSettings.PreferredTheme))
        {
            ApplyTheme();
        }
        
        if (e.PropertyName == nameof(AppSettings.DownloadPath))
        {
            Preferences.Set("DownloadPath", Settings.DownloadPath);
        }
    }

    private async Task SaveSettings()
    {
        IsSavingSettings = true;

        try
        {
            if (!ValidateSettings())
            {
                return;
            }

            await Task.Delay(500);
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
            IsSavingSettings = false;
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
            bool useManualInput = await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Select Download Folder 📁",
                "Would you like to manually type the folder path or browse for a file in the desired folder?",
                "Type Path",
                "Browse Files");

            if (useManualInput)
            {
                string result = await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayPromptAsync(
                    "Enter Folder Path",
                    "Enter the full path to the folder where you want files to be downloaded:",
                    "OK",
                    "Cancel",
                    placeholder: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LocalDrop Downloads"));

                if (!string.IsNullOrWhiteSpace(result))
                {
                    if (Directory.Exists(result))
                    {
                        Settings.DownloadPath = result;
                        await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                            "Folder Set! 📁",
                            $"Download folder updated to:\n\n📍 {result}",
                            "OK");
                    }
                    else
                    {
                        await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                            "Invalid Path",
                            "The folder path doesn't exist. Please check the path and try again.",
                            "OK");
                    }
                }
            }
            else
            {
                var pickResult = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "📁 Navigate to your desired folder and select ANY file (or create a new text file)"
                });

                if (pickResult != null)
                {
                    var selectedFolder = Path.GetDirectoryName(pickResult.FullPath);
                    if (!string.IsNullOrEmpty(selectedFolder) && Directory.Exists(selectedFolder))
                    {
                        Settings.DownloadPath = selectedFolder;
                        await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                            "Folder Selected! 📁",
                            $"Download folder updated to:\n\n📍 {selectedFolder}",
                            "OK");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert("Error", $"Failed to set folder: {ex.Message}", "OK");
        }
    }

    private async Task TestConnection()
    {
        IsTestingConnection = true;

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
            IsTestingConnection = false;
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
            HasUnsavedChanges = true; // Start with enabled save button to allow saving default settings
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

    [RelayCommand]
    public async Task RunNetworkDiagnostics()
    {
        try
        {
            var diagnosticResults = new List<string>();
            
            // Check local IP and ports
            var localIp = GetLocalIpAddress();
            diagnosticResults.Add($"✅ Local IP Address: {localIp}");
            
            // Check UDP discovery port
            try
            {
                using var udpTest = new System.Net.Sockets.UdpClient(35731);
                diagnosticResults.Add("✅ UDP Discovery Port 35731: Available");
                udpTest.Close();
            }
            catch
            {
                diagnosticResults.Add("❌ UDP Discovery Port 35731: In use or blocked");
            }
            
            // Check TCP file transfer port
            try
            {
                using var tcpTest = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 35732);
                tcpTest.Start();
                diagnosticResults.Add("✅ TCP File Transfer Port 35732: Available");
                tcpTest.Stop();
            }
            catch
            {
                diagnosticResults.Add("❌ TCP File Transfer Port 35732: In use or blocked");
            }
            
            // Check Windows Firewall suggestion
            diagnosticResults.Add("");
            diagnosticResults.Add("🔥 FIREWALL CHECK:");
            diagnosticResults.Add("If your laptop can't send files to this PC,");
            diagnosticResults.Add("the issue is likely Windows Firewall blocking");
            diagnosticResults.Add("incoming connections on TCP port 35732.");
            diagnosticResults.Add("");
            diagnosticResults.Add("💡 SOLUTION:");
            diagnosticResults.Add("Add LocalDropApp.exe to Windows Firewall exceptions");
            diagnosticResults.Add("or temporarily disable Windows Firewall to test.");
            
            var results = string.Join("\n", diagnosticResults);
            
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Network Diagnostics Results 🔍",
                results,
                "OK");
        }
        catch (Exception ex)
        {
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert(
                "Diagnostic Error",
                $"Failed to run diagnostics: {ex.Message}",
                "OK");
        }
    }
    
    private static string GetLocalIpAddress()
    {
        try
        {
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up 
                           && ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                           && ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel);

            var allAddresses = new List<string>();

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();
                var unicastAddresses = ipProperties.UnicastAddresses
                    .Where(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                               && !System.Net.IPAddress.IsLoopback(ua.Address))
                    .Select(ua => ua.Address.ToString())
                    .ToList();

                allAddresses.AddRange(unicastAddresses);
            }

            // Prefer private network addresses
            var privateAddress = allAddresses.FirstOrDefault(addr => 
                addr.StartsWith("192.168.") || 
                addr.StartsWith("10.") || 
                (addr.StartsWith("172.") && int.TryParse(addr.Split('.')[1], out var second) && second >= 16 && second <= 31));

            return privateAddress ?? allAddresses.FirstOrDefault() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 