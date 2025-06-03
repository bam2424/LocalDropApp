using System.ComponentModel;

namespace LocalDropApp.Models;

public class AppSettings : INotifyPropertyChanged
{
    private string _deviceName = Environment.MachineName;
    private int _discoveryPort = 8080;
    private int _transferPort = 8081;
    private string _downloadPath = string.Empty;
    private bool _autoAcceptTransfers;
    private bool _showNotifications = true;
    private bool _autoStart;
    private int _maxConcurrentTransfers = 3;
    private int _discoveryTimeout = 30;
    private bool _enableFileLogging = true;
    private AppTheme _preferredTheme = AppTheme.System;
    private bool _compressFiles;
    private string _compressionLevel = "Medium";
    private bool _encryptTransfers;
    private bool _requireConfirmation = true;
    private int _maxFileSize = 1024; // MB
    private bool _allowedNetworkTransfers = true;
    private string _allowedFileTypes = "*.*";
    private bool _enableAutoDiscovery = true;
    private int _keepHistoryDays = 30;

    public string DeviceName
    {
        get => _deviceName;
        set { _deviceName = value; OnPropertyChanged(); }
    }

    public int DiscoveryPort
    {
        get => _discoveryPort;
        set { _discoveryPort = value; OnPropertyChanged(); }
    }

    public int TransferPort
    {
        get => _transferPort;
        set { _transferPort = value; OnPropertyChanged(); }
    }

    public string DownloadPath
    {
        get => _downloadPath;
        set { _downloadPath = value; OnPropertyChanged(); }
    }

    public bool AutoAcceptTransfers
    {
        get => _autoAcceptTransfers;
        set { _autoAcceptTransfers = value; OnPropertyChanged(); }
    }

    public bool ShowNotifications
    {
        get => _showNotifications;
        set { _showNotifications = value; OnPropertyChanged(); }
    }

    public bool AutoStart
    {
        get => _autoStart;
        set { _autoStart = value; OnPropertyChanged(); }
    }

    public int MaxConcurrentTransfers
    {
        get => _maxConcurrentTransfers;
        set { _maxConcurrentTransfers = value; OnPropertyChanged(); }
    }

    public int DiscoveryTimeout
    {
        get => _discoveryTimeout;
        set { _discoveryTimeout = value; OnPropertyChanged(); }
    }

    public bool EnableFileLogging
    {
        get => _enableFileLogging;
        set { _enableFileLogging = value; OnPropertyChanged(); }
    }

    public AppTheme PreferredTheme
    {
        get => _preferredTheme;
        set { _preferredTheme = value; OnPropertyChanged(); OnPropertyChanged(nameof(ThemeDisplayText)); }
    }

    public bool CompressFiles
    {
        get => _compressFiles;
        set { _compressFiles = value; OnPropertyChanged(); }
    }

    public string CompressionLevel
    {
        get => _compressionLevel;
        set { _compressionLevel = value; OnPropertyChanged(); }
    }

    public bool EncryptTransfers
    {
        get => _encryptTransfers;
        set { _encryptTransfers = value; OnPropertyChanged(); }
    }

    public bool RequireConfirmation
    {
        get => _requireConfirmation;
        set { _requireConfirmation = value; OnPropertyChanged(); }
    }

    public int MaxFileSize
    {
        get => _maxFileSize;
        set { _maxFileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(MaxFileSizeFormatted)); }
    }

    public bool AllowedNetworkTransfers
    {
        get => _allowedNetworkTransfers;
        set { _allowedNetworkTransfers = value; OnPropertyChanged(); }
    }

    public string AllowedFileTypes
    {
        get => _allowedFileTypes;
        set { _allowedFileTypes = value; OnPropertyChanged(); }
    }

    public bool EnableAutoDiscovery
    {
        get => _enableAutoDiscovery;
        set { _enableAutoDiscovery = value; OnPropertyChanged(); }
    }

    public int KeepHistoryDays
    {
        get => _keepHistoryDays;
        set { _keepHistoryDays = value; OnPropertyChanged(); OnPropertyChanged(nameof(KeepHistoryDescription)); }
    }

    // Computed Properties
    public string ThemeDisplayText => PreferredTheme switch
    {
        AppTheme.Light => "Light Mode",
        AppTheme.Dark => "Dark Mode",
        AppTheme.System => "System Default",
        _ => "System Default"
    };

    public string MaxFileSizeFormatted => $"{MaxFileSize} MB";

    public string KeepHistoryDescription => KeepHistoryDays == 0 
        ? "Keep forever" 
        : $"Keep for {KeepHistoryDays} days";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public AppSettings()
    {
        // Set default download path
        DownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LocalDrop Downloads");
    }
}

public enum AppTheme
{
    System,
    Light,
    Dark
} 