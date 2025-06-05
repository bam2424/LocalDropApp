using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalDropApp.Models;
using LocalDropApp.Services;

namespace LocalDropApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPeerDiscoveryService _peerDiscoveryService;
    private readonly IFileTransferService _fileTransferService;

    [ObservableProperty]
    private string _deviceName = GenerateInstanceDeviceName();

    private bool _isDiscovering;
    public bool IsDiscovering 
    { 
        get => _isDiscovering; 
        set 
        { 
            if (SetProperty(ref _isDiscovering, value))
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] IsDiscovering property changed to: {value}");
                OnPropertyChanged(nameof(DiscoverButtonText));
                OnPropertyChanged(nameof(DiscoverButtonBackgroundColor));
            }
        } 
    }

    public string DiscoverButtonText => IsDiscovering ? "⏳ Searching..." : "🔍 Discover Peers";
    public Color DiscoverButtonBackgroundColor => IsDiscovering ? Colors.Gray : Color.FromArgb("#512BD4");
    
    public Color SendFilesButtonBackgroundColor => CanSendFiles ? Color.FromArgb("#2E7D32") : Colors.Gray;
    public Color SendFilesButtonTextColor => CanSendFiles ? Colors.White : Colors.Gray;

    [ObservableProperty]
    private string _statusMessage = "Ready to discover peers...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendFiles))]
    [NotifyPropertyChangedFor(nameof(SendFilesButtonBackgroundColor))]
    [NotifyPropertyChangedFor(nameof(SendFilesButtonTextColor))]
    private bool _hasSelectedFiles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendFiles))]
    [NotifyPropertyChangedFor(nameof(SendFilesButtonBackgroundColor))]
    [NotifyPropertyChangedFor(nameof(SendFilesButtonTextColor))]
    private PeerDevice? _selectedPeer;

    public bool CanSendFiles => SelectedPeer != null && HasSelectedFiles;

    public ObservableCollection<PeerDevice> DiscoveredPeers { get; } = new();
    public ObservableCollection<FileTransfer> ActiveTransfers { get; } = new();
    public ObservableCollection<FileTransfer> TransferHistory { get; } = new();
    public ObservableCollection<string> SelectedFiles { get; } = new();

    public MainViewModel(IPeerDiscoveryService peerDiscoveryService, IFileTransferService fileTransferService)
    {
        _peerDiscoveryService = peerDiscoveryService;
        _fileTransferService = fileTransferService;
        
        InitializeServices();
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

    [RelayCommand]
    private async Task StartDiscovery()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsDiscovering = true;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] IsDiscovering set to TRUE on UI thread");
            });
            
            if (_peerDiscoveryService.IsRunning)
            {
                await _peerDiscoveryService.RefreshPeersAsync();
                MainThread.BeginInvokeOnMainThread(() => StatusMessage = "Refreshing peer search...");
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() => StatusMessage = "Starting discovery...");
                await _peerDiscoveryService.StartAsync(DeviceName, _fileTransferService.ListenPort);
                MainThread.BeginInvokeOnMainThread(() => StatusMessage = "Searching for peers...");
            }
            
            // Keep discovery active for 5 seconds to make hourglass more visible
            await Task.Delay(5000);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsDiscovering = false;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] IsDiscovering set to FALSE on UI thread");
                
                StatusMessage = _peerDiscoveryService.IsRunning ? 
                    $"Discovery active - Found {DiscoveredPeers.Count} peer(s)" : 
                    "Ready to discover peers...";
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusMessage = $"Failed to start discovery: {ex.Message}";
                IsDiscovering = false;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] IsDiscovering set to FALSE (exception) on UI thread");
            });
        }
    }

    [RelayCommand]
    private async Task SelectFiles()
    {
        try
        {
            var result = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Select files to send"
            });

            if (result != null)
            {
                SelectedFiles.Clear();
                foreach (var file in result)
                {
                    SelectedFiles.Add(file.FullPath);
                }
                
                HasSelectedFiles = SelectedFiles.Count > 0;
                StatusMessage = $"Selected {SelectedFiles.Count} file(s)";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error selecting files: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendFiles()
    {
        if (SelectedPeer == null || !HasSelectedFiles)
        {
            StatusMessage = "Please select a peer and files to send";
            return;
        }

        try
        {
            StatusMessage = $"Starting transfer to {SelectedPeer.Name}...";
            
            foreach (var filePath in SelectedFiles.ToList())
            {
                await _fileTransferService.SendFileAsync(filePath, SelectedPeer);
            }
            
            SelectedFiles.Clear();
            HasSelectedFiles = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error sending files: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        TransferHistory.Clear();
        StatusMessage = "Transfer history cleared";
    }

    [RelayCommand]
    private async Task RefreshPeers()
    {
        await StartDiscovery();
    }

    [RelayCommand]
    private void RemoveSelectedFile(string filePath)
    {
        SelectedFiles.Remove(filePath);
        HasSelectedFiles = SelectedFiles.Count > 0;
        StatusMessage = HasSelectedFiles ? $"{SelectedFiles.Count} file(s) selected" : "No files selected";
    }

    private void InitializeServices()
    {
        _peerDiscoveryService.PeerDiscovered += OnPeerDiscovered;
        _peerDiscoveryService.PeerLost += OnPeerLost;
        _peerDiscoveryService.PeerUpdated += OnPeerUpdated;
        _peerDiscoveryService.DiscoveryError += OnDiscoveryError;

        _fileTransferService.TransferStarted += OnTransferStarted;
        _fileTransferService.TransferProgressUpdated += OnTransferProgressUpdated;
        _fileTransferService.TransferCompleted += OnTransferCompleted;
        _fileTransferService.TransferFailed += OnTransferFailed;
        _fileTransferService.IncomingTransferRequest += OnIncomingTransferRequest;
        _fileTransferService.TransferError += OnTransferError;
        _fileTransferService.IncomingFileCompleted += OnIncomingFileCompleted;

        _ = Task.Run(async () =>
        {
            await _fileTransferService.StartListeningAsync();
            StatusMessage = $"Ready - Listening on port {_fileTransferService.ListenPort}";
        });
    }

    private void OnPeerDiscovered(object? sender, PeerDevice peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (!DiscoveredPeers.Any(p => p.Id == peer.Id))
            {
                DiscoveredPeers.Add(peer);
                StatusMessage = IsDiscovering ? 
                    $"Found {DiscoveredPeers.Count} peer(s) - still searching..." :
                    $"Found {DiscoveredPeers.Count} peer(s)";
            }
        });
    }

    private void OnPeerLost(object? sender, PeerDevice peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existingPeer = DiscoveredPeers.FirstOrDefault(p => p.Id == peer.Id);
            if (existingPeer != null)
            {
                DiscoveredPeers.Remove(existingPeer);
                StatusMessage = $"Peer {peer.Name} disconnected";
            }
        });
    }

    private void OnPeerUpdated(object? sender, PeerDevice peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existingPeer = DiscoveredPeers.FirstOrDefault(p => p.Id == peer.Id);
            if (existingPeer != null)
            {
                existingPeer.Name = peer.Name;
                existingPeer.IpAddress = peer.IpAddress;
                existingPeer.IsOnline = peer.IsOnline;
                existingPeer.LastSeen = peer.LastSeen;
            }
        });
    }

    private void OnDiscoveryError(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"Discovery error: {error}";
            // Don't interfere with the normal discovery flow
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Discovery error: {error}");
        });
    }

    private void OnTransferStarted(object? sender, FileTransfer transfer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActiveTransfers.Add(transfer);
            StatusMessage = $"Started {transfer.Direction.ToString().ToLower()} {transfer.FileName}";
        });
    }

    private void OnTransferProgressUpdated(object? sender, FileTransfer transfer)
    {
        // UI already bound to transfer properties via INotifyPropertyChanged
    }

    private void OnTransferCompleted(object? sender, FileTransfer transfer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActiveTransfers.Remove(transfer);
            TransferHistory.Insert(0, transfer);
            StatusMessage = $"Completed {transfer.FileName}";
        });
    }

    private void OnTransferFailed(object? sender, FileTransfer transfer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActiveTransfers.Remove(transfer);
            TransferHistory.Insert(0, transfer);
            StatusMessage = $"Failed: {transfer.FileName} - {transfer.ErrorMessage}";
        });
    }

    private void OnIncomingTransferRequest(object? sender, FileTransferRequestPayload request)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var accept = await Application.Current!.MainPage!.DisplayAlert(
                "Incoming File",
                $"Receive '{request.FileName}' ({FormatFileSize(request.FileSize)})?",
                "Accept",
                "Decline");

            if (accept)
            {
                var downloadPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "LocalDropApp Downloads",
                    request.FileName);

                await _fileTransferService.AcceptIncomingTransferAsync(request.TransferId, downloadPath);
            }
            else
            {
                await _fileTransferService.RejectIncomingTransferAsync(request.TransferId, "User declined");
            }
        });
    }

    private void OnTransferError(object? sender, string error)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusMessage = $"Transfer error: {error}";
        });
    }

    private void OnIncomingFileCompleted(object? sender, (FileTransfer Transfer, string DownloadPath) e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var transfer = e.Transfer;
            var downloadPath = e.DownloadPath;
            var folderPath = Path.GetDirectoryName(downloadPath);

            var result = await Application.Current!.MainPage!.DisplayAlert(
                "File Received! 📁",
                $"'{transfer.FileName}' has been saved successfully!\n\n" +
                $"📍 Location: {folderPath}\n\n" +
                $"Would you like to open the folder?",
                "Open Folder",
                "Close");

            if (result)
            {
                try
                {
                    // Open the folder containing the downloaded file
                    await OpenFolderAsync(folderPath!);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        $"Could not open folder: {ex.Message}",
                        "OK");
                }
            }
        });
    }

    private async Task OpenFolderAsync(string folderPath)
    {
        try
        {
#if WINDOWS
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
#elif MACCATALYST
            System.Diagnostics.Process.Start("open", folderPath);
#elif ANDROID
            // Android doesn't have a simple way to open file manager to specific folder
            // We could use an intent, but for now just show a message
            await Application.Current!.MainPage!.DisplayAlert(
                "Folder Location",
                $"File saved to: {folderPath}",
                "OK");
#elif IOS
            // iOS doesn't allow opening arbitrary folders
            await Application.Current!.MainPage!.DisplayAlert(
                "File Saved",
                $"File saved to: {folderPath}",
                "OK");
#else
            // Generic fallback - try to open with default system handler
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
#endif
        }
        catch (Exception ex)
        {
            // If all else fails, just show the path
            await Application.Current!.MainPage!.DisplayAlert(
                "Folder Location",
                $"Could not open folder automatically.\n\nFile saved to: {folderPath}",
                "OK");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
} 