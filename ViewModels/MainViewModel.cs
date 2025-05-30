using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalDropApp.Models;

namespace LocalDropApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _deviceName = Environment.MachineName;

    [ObservableProperty]
    private bool _isDiscovering;

    [ObservableProperty]
    private string _statusMessage = "Ready to discover peers...";

    [ObservableProperty]
    private PeerDevice? _selectedPeer;

    [ObservableProperty]
    private bool _hasSelectedFiles;

    public ObservableCollection<PeerDevice> DiscoveredPeers { get; } = new();
    public ObservableCollection<FileTransfer> ActiveTransfers { get; } = new();
    public ObservableCollection<FileTransfer> TransferHistory { get; } = new();
    public ObservableCollection<string> SelectedFiles { get; } = new();

    public MainViewModel()
    {
        // Initialize with some mock data for UI development
        InitializeMockData();
    }

    [RelayCommand]
    private async Task StartDiscovery()
    {
        IsDiscovering = true;
        StatusMessage = "Discovering peers...";

        // TODO: Implement actual peer discovery
        await Task.Delay(2000); // Simulate discovery time

        // Add mock discovered peer
        var mockPeer = new PeerDevice
        {
            Name = "John's MacBook",
            IpAddress = "192.168.1.105",
            IsOnline = true,
            LastSeen = DateTime.Now
        };

        if (!DiscoveredPeers.Any(p => p.IpAddress == mockPeer.IpAddress))
        {
            DiscoveredPeers.Add(mockPeer);
        }

        IsDiscovering = false;
        StatusMessage = $"Found {DiscoveredPeers.Count} peer(s)";
    }

    [RelayCommand]
    private async Task SelectFiles()
    {
        try
        {
            // TODO: Implement actual file picker
            // For now, simulate file selection
            SelectedFiles.Clear();
            SelectedFiles.Add("Document.pdf");
            SelectedFiles.Add("Image.jpg");
            
            HasSelectedFiles = SelectedFiles.Count > 0;
            StatusMessage = $"Selected {SelectedFiles.Count} file(s)";
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
            // Create transfer objects for each selected file
            foreach (var fileName in SelectedFiles)
            {
                var transfer = new FileTransfer
                {
                    FileName = fileName,
                    FileSize = Random.Shared.Next(1024 * 1024, 100 * 1024 * 1024), // Random size 1MB-100MB
                    Direction = TransferDirection.Sending,
                    TargetPeer = SelectedPeer,
                    Status = TransferStatus.Pending,
                    StartTime = DateTime.Now
                };

                ActiveTransfers.Add(transfer);
            }

            StatusMessage = $"Started sending {SelectedFiles.Count} file(s) to {SelectedPeer.Name}";
            
            // Simulate transfer progress
            _ = Task.Run(async () => await SimulateTransferProgress());
            
            // Clear selected files
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
    private void RefreshPeers()
    {
        _ = Task.Run(StartDiscovery);
    }

    [RelayCommand]
    private void RemoveSelectedFile(string fileName)
    {
        SelectedFiles.Remove(fileName);
        HasSelectedFiles = SelectedFiles.Count > 0;
        StatusMessage = HasSelectedFiles ? $"{SelectedFiles.Count} file(s) selected" : "No files selected";
    }

    private async Task SimulateTransferProgress()
    {
        var activeTransfers = ActiveTransfers.ToList();
        
        foreach (var transfer in activeTransfers)
        {
            transfer.Status = TransferStatus.InProgress;
            
            // Simulate progress
            for (int progress = 0; progress <= 100; progress += 5)
            {
                transfer.BytesTransferred = (long)(transfer.FileSize * (progress / 100.0));
                
                // Simulate estimated time remaining
                if (progress > 0)
                {
                    var elapsed = DateTime.Now - transfer.StartTime;
                    var totalEstimated = TimeSpan.FromTicks(elapsed.Ticks * 100 / progress);
                    transfer.EstimatedTimeRemaining = totalEstimated - elapsed;
                }
                
                await Task.Delay(200); // Simulate transfer time
            }
            
            transfer.Status = TransferStatus.Completed;
            transfer.EstimatedTimeRemaining = TimeSpan.Zero;
            
            // Move to history
            TransferHistory.Insert(0, transfer);
            ActiveTransfers.Remove(transfer);
        }
        
        StatusMessage = "All transfers completed successfully";
    }

    private void InitializeMockData()
    {
        // Add some mock peers for UI development
        DiscoveredPeers.Add(new PeerDevice
        {
            Name = "Sarah's iPhone",
            IpAddress = "192.168.1.102",
            IsOnline = true,
            LastSeen = DateTime.Now.AddMinutes(-2)
        });

        DiscoveredPeers.Add(new PeerDevice
        {
            Name = "Office Desktop",
            IpAddress = "192.168.1.150",
            IsOnline = false,
            LastSeen = DateTime.Now.AddHours(-1)
        });

        // Add some mock transfer history
        TransferHistory.Add(new FileTransfer
        {
            FileName = "Presentation.pptx",
            FileSize = 15 * 1024 * 1024,
            BytesTransferred = 15 * 1024 * 1024,
            Status = TransferStatus.Completed,
            Direction = TransferDirection.Sending,
            StartTime = DateTime.Now.AddMinutes(-10),
            TargetPeer = DiscoveredPeers.First()
        });
    }
} 