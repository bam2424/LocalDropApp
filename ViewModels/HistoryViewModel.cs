using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LocalDropApp.Models;
using CommunityToolkit.Mvvm.Input;

namespace LocalDropApp.ViewModels;

public class HistoryViewModel : INotifyPropertyChanged
{
    private ObservableCollection<TransferHistory> _allTransfers = new();
    private ObservableCollection<TransferHistory> _filteredTransfers = new();
    private string _searchText = string.Empty;
    private string _selectedFilter = "All";
    private bool _isLoading;
    private int _totalTransfers;
    private long _totalDataTransferred;
    private int _successfulTransfers;
    private int _failedTransfers;

    public ObservableCollection<TransferHistory> FilteredTransfers
    {
        get => _filteredTransfers;
        set { _filteredTransfers = value; OnPropertyChanged(); }
    }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); ApplyFilters(); }
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set { _selectedFilter = value; OnPropertyChanged(); ApplyFilters(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public int TotalTransfers
    {
        get => _totalTransfers;
        set { _totalTransfers = value; OnPropertyChanged(); }
    }

    public long TotalDataTransferred
    {
        get => _totalDataTransferred;
        set { _totalDataTransferred = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalDataTransferredFormatted)); }
    }

    public int SuccessfulTransfers
    {
        get => _successfulTransfers;
        set { _successfulTransfers = value; OnPropertyChanged(); OnPropertyChanged(nameof(SuccessRate)); }
    }

    public int FailedTransfers
    {
        get => _failedTransfers;
        set { _failedTransfers = value; OnPropertyChanged(); OnPropertyChanged(nameof(SuccessRate)); }
    }

    // Computed Properties
    public string TotalDataTransferredFormatted
    {
        get
        {
            if (TotalDataTransferred < 1024) return $"{TotalDataTransferred} B";
            if (TotalDataTransferred < 1024 * 1024) return $"{TotalDataTransferred / 1024.0:F1} KB";
            if (TotalDataTransferred < 1024L * 1024 * 1024) return $"{TotalDataTransferred / (1024.0 * 1024.0):F1} MB";
            return $"{TotalDataTransferred / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }

    public string SuccessRate
    {
        get
        {
            if (TotalTransfers == 0) return "0%";
            return $"{(SuccessfulTransfers * 100.0 / TotalTransfers):F1}%";
        }
    }

    public List<string> FilterOptions { get; } = new()
    {
        "All",
        "Sent",
        "Received",
        "Completed",
        "Failed",
        "Today",
        "This Week",
        "This Month"
    };

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ExportHistoryCommand { get; }
    public ICommand DeleteTransferCommand { get; }
    public ICommand RetryTransferCommand { get; }
    public ICommand OpenFileLocationCommand { get; }

    public HistoryViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshHistory);
        ClearHistoryCommand = new AsyncRelayCommand(ClearHistory);
        ExportHistoryCommand = new AsyncRelayCommand(ExportHistory);
        DeleteTransferCommand = new AsyncRelayCommand<TransferHistory>(DeleteTransfer);
        RetryTransferCommand = new AsyncRelayCommand<TransferHistory>(RetryTransfer);
        OpenFileLocationCommand = new AsyncRelayCommand<TransferHistory>(OpenFileLocation);

        LoadSampleData();
    }

    private async Task RefreshHistory()
    {
        IsLoading = true;

        try
        {
            // Simulate loading from database/storage
            await Task.Delay(500);
            
            // In real implementation, load from your data service
            // _allTransfers = await _historyService.GetAllTransfersAsync();
            
            ApplyFilters();
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            // Handle error
            System.Diagnostics.Debug.WriteLine($"Error refreshing history: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ClearHistory()
    {
        try
        {
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Clear History", 
                "Are you sure you want to clear all transfer history? This action cannot be undone.", 
                "Clear", 
                "Cancel");

            if (confirmed)
            {
                _allTransfers.Clear();
                ApplyFilters();
                UpdateStatistics();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to clear history: {ex.Message}", "OK");
        }
    }

    private async Task ExportHistory()
    {
        try
        {
            // In real implementation, export to CSV/JSON
            await Application.Current.MainPage.DisplayAlert("Export", "History exported successfully!", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export history: {ex.Message}", "OK");
        }
    }

    private async Task DeleteTransfer(TransferHistory? transfer)
    {
        if (transfer == null) return;

        try
        {
            bool confirmed = await Application.Current.MainPage.DisplayAlert(
                "Delete Transfer", 
                $"Are you sure you want to delete the transfer record for '{transfer.FileName}'?", 
                "Delete", 
                "Cancel");

            if (confirmed)
            {
                _allTransfers.Remove(transfer);
                ApplyFilters();
                UpdateStatistics();
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete transfer: {ex.Message}", "OK");
        }
    }

    private async Task RetryTransfer(TransferHistory? transfer)
    {
        if (transfer == null || transfer.Status != TransferStatus.Failed) return;

        try
        {
            // In real implementation, retry the transfer
            await Application.Current.MainPage.DisplayAlert("Retry", $"Retrying transfer for '{transfer.FileName}'", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to retry transfer: {ex.Message}", "OK");
        }
    }

    private async Task OpenFileLocation(TransferHistory? transfer)
    {
        if (transfer == null || string.IsNullOrEmpty(transfer.FilePath)) return;

        try
        {
            // In real implementation, open file location
            await Application.Current.MainPage.DisplayAlert("Open Location", $"Opening location for '{transfer.FileName}'", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open file location: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allTransfers.AsEnumerable();

        // Apply text search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(t => 
                t.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                t.PeerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Apply category filter
        filtered = SelectedFilter switch
        {
            "Sent" => filtered.Where(t => t.Direction == TransferDirection.Sending),
            "Received" => filtered.Where(t => t.Direction == TransferDirection.Receiving),
            "Completed" => filtered.Where(t => t.Status == TransferStatus.Completed),
            "Failed" => filtered.Where(t => t.Status == TransferStatus.Failed),
            "Today" => filtered.Where(t => t.Timestamp.Date == DateTime.Today),
            "This Week" => filtered.Where(t => t.Timestamp >= DateTime.Today.AddDays(-7)),
            "This Month" => filtered.Where(t => t.Timestamp >= DateTime.Today.AddDays(-30)),
            _ => filtered
        };

        FilteredTransfers = new ObservableCollection<TransferHistory>(
            filtered.OrderByDescending(t => t.Timestamp));
    }

    private void UpdateStatistics()
    {
        TotalTransfers = _allTransfers.Count;
        TotalDataTransferred = _allTransfers.Sum(t => t.FileSize);
        SuccessfulTransfers = _allTransfers.Count(t => t.Status == TransferStatus.Completed);
        FailedTransfers = _allTransfers.Count(t => t.Status == TransferStatus.Failed);
    }

    private void LoadSampleData()
    {
        var random = new Random();
        var fileNames = new[] { "Document.pdf", "Photo.jpg", "Video.mp4", "Archive.zip", "Spreadsheet.xlsx", "Presentation.pptx" };
        var peerNames = new[] { "John's Laptop", "Sarah's Phone", "Office PC", "Home Desktop", "MacBook Pro", "Android Tablet" };

        for (int i = 0; i < 20; i++)
        {
            var transfer = new TransferHistory
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileNames[random.Next(fileNames.Length)],
                FileSize = random.Next(1024, 100 * 1024 * 1024), // 1KB to 100MB
                Timestamp = DateTime.Now.AddDays(-random.Next(0, 30)).AddHours(-random.Next(0, 24)),
                Direction = random.Next(2) == 0 ? TransferDirection.Sending : TransferDirection.Receiving,
                Status = random.Next(10) < 8 ? TransferStatus.Completed : TransferStatus.Failed, // 80% success rate
                PeerName = peerNames[random.Next(peerNames.Length)],
                PeerIpAddress = $"192.168.1.{random.Next(2, 254)}",
                FilePath = @"C:\Downloads\LocalDrop",
                Duration = TimeSpan.FromSeconds(random.Next(1, 300)),
                TransferSpeed = random.Next(100, 10000) * 1024.0 // 100KB/s to 10MB/s
            };

            _allTransfers.Add(transfer);
        }

        ApplyFilters();
        UpdateStatistics();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 