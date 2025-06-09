using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using LocalDropApp.Models;
using CommunityToolkit.Mvvm.Input;
using System.Text.Json;

namespace LocalDropApp.ViewModels;

public class HistoryViewModel : INotifyPropertyChanged
{
    private ObservableCollection<TransferHistory> _allTransfers = new();
    private ObservableCollection<TransferHistory> _filteredTransfers = new();
    private ObservableCollection<TransferTrendData> _trendData = new();
    private string _searchText = string.Empty;
    private string _selectedFilter = "All";
    private bool _isLoading;
    private int _totalTransfers;
    private long _totalDataTransferred;
    private int _successfulTransfers;
    private int _failedTransfers;
    private double _averageTransferSpeed;
    private TimeSpan _totalTransferTime;
    private string _selectedTimeRange = "All Time";

    public ObservableCollection<TransferHistory> FilteredTransfers
    {
        get => _filteredTransfers;
        set { _filteredTransfers = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TransferTrendData> TrendData
    {
        get => _trendData;
        set { _trendData = value; OnPropertyChanged(); }
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

    public string SelectedTimeRange
    {
        get => _selectedTimeRange;
        set { _selectedTimeRange = value; OnPropertyChanged(); ApplyFilters(); UpdateStatistics(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set 
        { 
            _isLoading = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(RefreshButtonBackgroundColor));
        }
    }

    // Button properties
    public Color RefreshButtonBackgroundColor => IsLoading ? Colors.Gray : Color.FromArgb("#512BD4");

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

    public double AverageTransferSpeed
    {
        get => _averageTransferSpeed;
        set { _averageTransferSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(AverageTransferSpeedFormatted)); }
    }

    public TimeSpan TotalTransferTime
    {
        get => _totalTransferTime;
        set { _totalTransferTime = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTransferTimeFormatted)); }
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

    public string AverageTransferSpeedFormatted
    {
        get
        {
            if (AverageTransferSpeed < 1024) return $"{AverageTransferSpeed:F1} B/s";
            if (AverageTransferSpeed < 1024 * 1024) return $"{AverageTransferSpeed / 1024.0:F1} KB/s";
            return $"{AverageTransferSpeed / (1024.0 * 1024.0):F1} MB/s";
        }
    }

    public string TotalTransferTimeFormatted
    {
        get
        {
            if (TotalTransferTime.TotalMinutes < 60) return $"{TotalTransferTime.TotalMinutes:F1}m";
            if (TotalTransferTime.TotalHours < 24) return $"{TotalTransferTime.TotalHours:F1}h";
            return $"{TotalTransferTime.TotalDays:F1}d";
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

    public List<string> TimeRangeOptions { get; } = new()
    {
        "All Time",
        "Last 24 Hours",
        "Last 7 Days",
        "Last 30 Days",
        "Last 90 Days"
    };

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand ExportHistoryCommand { get; }
    public ICommand DeleteTransferCommand { get; }
    public ICommand RetryTransferCommand { get; }
    public ICommand OpenFileLocationCommand { get; }
    public ICommand ShowDetailedStatsCommand { get; }
    public ICommand ShowTrendDetailsCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportJsonCommand { get; }

    public HistoryViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshHistory);
        ClearHistoryCommand = new AsyncRelayCommand(ClearHistory);
        ExportHistoryCommand = new AsyncRelayCommand(ExportHistory);
        DeleteTransferCommand = new AsyncRelayCommand<TransferHistory>(DeleteTransfer);
        RetryTransferCommand = new AsyncRelayCommand<TransferHistory>(RetryTransfer);
        OpenFileLocationCommand = new AsyncRelayCommand<TransferHistory>(OpenFileLocation);
        ShowDetailedStatsCommand = new AsyncRelayCommand(ShowDetailedStats);
        ShowTrendDetailsCommand = new AsyncRelayCommand<TransferTrendData>(ShowTrendDetails);
        ExportCsvCommand = new AsyncRelayCommand(ExportToCsv);
        ExportJsonCommand = new AsyncRelayCommand(ExportToJson);

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
            GenerateTrendData();
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
                GenerateTrendData();
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
            var choice = await Application.Current.MainPage.DisplayActionSheet(
                "Export Format", 
                "Cancel", 
                null, 
                "CSV (Excel)", 
                "JSON (Data)", 
                "Text Summary");

            switch (choice)
            {
                case "CSV (Excel)":
                    await ExportToCsv();
                    break;
                case "JSON (Data)":
                    await ExportToJson();
                    break;
                case "Text Summary":
                    await ExportToTextSummary();
                    break;
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to export history: {ex.Message}", "OK");
        }
    }

    private async Task ExportToCsv()
    {
        try
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Date,Time,FileName,FileSize,Direction,Peer,Status,Duration,Speed,Error");

            foreach (var transfer in _allTransfers.OrderByDescending(t => t.Timestamp))
            {
                csv.AppendLine($"{transfer.Timestamp:yyyy-MM-dd},{transfer.Timestamp:HH:mm:ss}," +
                              $"\"{transfer.FileName}\",{transfer.FileSize}," +
                              $"{transfer.Direction},{transfer.PeerName}," +
                              $"{transfer.Status},{transfer.Duration.TotalSeconds:F1}," +
                              $"{transfer.TransferSpeed:F1},\"{transfer.ErrorMessage}\"");
            }

            var fileName = $"LocalDrop_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);

            await File.WriteAllTextAsync(filePath, csv.ToString());
            
            await Application.Current.MainPage.DisplayAlert(
                "Export Complete", 
                $"History exported to:\n{filePath}", 
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"CSV export failed: {ex.Message}", "OK");
        }
    }

    private async Task ExportToJson()
    {
        try
        {
            var exportData = new
            {
                ExportDate = DateTime.Now,
                Statistics = new
                {
                    TotalTransfers,
                    TotalDataTransferred,
                    TotalDataTransferredFormatted,
                    SuccessfulTransfers,
                    FailedTransfers,
                    SuccessRate,
                    AverageTransferSpeed = AverageTransferSpeedFormatted,
                    TotalTransferTime = TotalTransferTimeFormatted
                },
                Transfers = _allTransfers.Select(t => new
                {
                    t.Id,
                    t.FileName,
                    t.FileSize,
                    FileSizeFormatted = t.FileSizeFormatted,
                    t.Timestamp,
                    Direction = t.Direction.ToString(),
                    Status = t.Status.ToString(),
                    t.PeerName,
                    t.PeerIpAddress,
                    t.FilePath,
                    Duration = t.Duration.ToString(),
                    TransferSpeed = t.TransferSpeedFormatted,
                    t.ErrorMessage
                }).OrderByDescending(t => t.Timestamp)
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            var fileName = $"LocalDrop_History_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);

            await File.WriteAllTextAsync(filePath, json);
            
            await Application.Current.MainPage.DisplayAlert(
                "Export Complete", 
                $"History exported to:\n{filePath}", 
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"JSON export failed: {ex.Message}", "OK");
        }
    }

    private async Task ExportToTextSummary()
    {
        try
        {
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("LocalDrop Transfer History Summary");
            summary.AppendLine("=====================================");
            summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine();
            
            summary.AppendLine("STATISTICS:");
            summary.AppendLine($"• Total Transfers: {TotalTransfers}");
            summary.AppendLine($"• Total Data: {TotalDataTransferredFormatted}");
            summary.AppendLine($"• Success Rate: {SuccessRate}");
            summary.AppendLine($"• Average Speed: {AverageTransferSpeedFormatted}");
            summary.AppendLine($"• Total Time: {TotalTransferTimeFormatted}");
            summary.AppendLine();

            summary.AppendLine("RECENT TRANSFERS:");
            foreach (var transfer in _allTransfers.OrderByDescending(t => t.Timestamp).Take(20))
            {
                summary.AppendLine($"• {transfer.FormattedDate} - {transfer.FileName} " +
                                  $"({transfer.FileSizeFormatted}) - {transfer.DirectionText} " +
                                  $"{transfer.Status} in {transfer.DurationFormatted}");
            }

            var fileName = $"LocalDrop_Summary_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);

            await File.WriteAllTextAsync(filePath, summary.ToString());
            
            await Application.Current.MainPage.DisplayAlert(
                "Export Complete", 
                $"Summary exported to:\n{filePath}", 
                "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Summary export failed: {ex.Message}", "OK");
        }
    }

    private async Task ShowDetailedStats()
    {
        var topPeers = _allTransfers
            .GroupBy(t => t.PeerName)
            .Select(g => new { Peer = g.Key, Count = g.Count(), Data = g.Sum(t => t.FileSize) })
            .OrderByDescending(x => x.Count)
            .Take(5);

        var topFileTypes = _allTransfers
            .GroupBy(t => Path.GetExtension(t.FileName).ToLower())
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5);

        var stats = new System.Text.StringBuilder();
        stats.AppendLine("📊 Detailed Statistics\n");
        
        stats.AppendLine("Top Transfer Partners:");
        foreach (var peer in topPeers)
        {
            var dataFormatted = peer.Data < 1024 * 1024 * 1024 
                ? $"{peer.Data / (1024.0 * 1024.0):F1} MB"
                : $"{peer.Data / (1024.0 * 1024.0 * 1024.0):F1} GB";
            stats.AppendLine($"• {peer.Peer}: {peer.Count} transfers ({dataFormatted})");
        }

        stats.AppendLine("\nMost Transferred File Types:");
        foreach (var type in topFileTypes)
        {
            var typeName = string.IsNullOrEmpty(type.Type) ? "No extension" : type.Type;
            stats.AppendLine($"• {typeName}: {type.Count} files");
        }

        await Application.Current.MainPage.DisplayAlert("Detailed Statistics", stats.ToString(), "OK");
    }

    private async Task ShowTrendDetails(TransferTrendData? trendData)
    {
        if (trendData == null) return;

        var details = new System.Text.StringBuilder();
        details.AppendLine($"📅 {trendData.Date:dddd, MMMM dd, yyyy}\n");
        details.AppendLine($"📊 Total Transfers: {trendData.TransferCount}");
        details.AppendLine($"💾 Data Transferred: {trendData.TotalBytesFormatted}");
        details.AppendLine($"✅ Successful: {trendData.SuccessfulCount}");
        details.AppendLine($"❌ Failed: {trendData.FailedCount}");
        details.AppendLine($"📈 Success Rate: {trendData.SuccessRate:F1}%");

        await Application.Current.MainPage.DisplayAlert("Daily Transfer Details", details.ToString(), "OK");
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
                GenerateTrendData();
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
#if WINDOWS
            // Open Windows Explorer to the file location
            var processStartInfo = new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "explorer.exe",
                Arguments = $"/select, \"{transfer.FilePath}\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(processStartInfo);
#else
            await Application.Current.MainPage.DisplayAlert("Open Location", $"File location: {transfer.FilePath}", "OK");
#endif
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open file location: {ex.Message}", "OK");
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allTransfers.AsEnumerable();

        // Apply time range filter first
        filtered = SelectedTimeRange switch
        {
            "Last 24 Hours" => filtered.Where(t => t.Timestamp >= DateTime.Now.AddDays(-1)),
            "Last 7 Days" => filtered.Where(t => t.Timestamp >= DateTime.Now.AddDays(-7)),
            "Last 30 Days" => filtered.Where(t => t.Timestamp >= DateTime.Now.AddDays(-30)),
            "Last 90 Days" => filtered.Where(t => t.Timestamp >= DateTime.Now.AddDays(-90)),
            _ => filtered
        };

        // Apply text search
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(t => 
                t.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                t.PeerName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                t.ErrorMessage.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
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
        var relevantTransfers = GetRelevantTransfersForTimeRange();
        
        TotalTransfers = relevantTransfers.Count();
        TotalDataTransferred = relevantTransfers.Sum(t => t.FileSize);
        SuccessfulTransfers = relevantTransfers.Count(t => t.Status == TransferStatus.Completed);
        FailedTransfers = relevantTransfers.Count(t => t.Status == TransferStatus.Failed);
        
        var completedTransfers = relevantTransfers.Where(t => t.Status == TransferStatus.Completed);
        AverageTransferSpeed = completedTransfers.Any() ? completedTransfers.Average(t => t.TransferSpeed) : 0;
        TotalTransferTime = TimeSpan.FromTicks(completedTransfers.Sum(t => t.Duration.Ticks));
    }

    private IEnumerable<TransferHistory> GetRelevantTransfersForTimeRange()
    {
        return SelectedTimeRange switch
        {
            "Last 24 Hours" => _allTransfers.Where(t => t.Timestamp >= DateTime.Now.AddDays(-1)),
            "Last 7 Days" => _allTransfers.Where(t => t.Timestamp >= DateTime.Now.AddDays(-7)),
            "Last 30 Days" => _allTransfers.Where(t => t.Timestamp >= DateTime.Now.AddDays(-30)),
            "Last 90 Days" => _allTransfers.Where(t => t.Timestamp >= DateTime.Now.AddDays(-90)),
            _ => _allTransfers
        };
    }

    private void GenerateTrendData()
    {
        var trendData = new List<TransferTrendData>();
        
        // Group transfers by day for trend analysis
        var dailyData = _allTransfers
            .Where(t => t.Timestamp >= DateTime.Now.AddDays(-30))
            .GroupBy(t => t.Timestamp.Date)
            .OrderBy(g => g.Key);

        foreach (var day in dailyData)
        {
            trendData.Add(new TransferTrendData
            {
                Date = day.Key,
                TransferCount = day.Count(),
                TotalBytes = day.Sum(t => t.FileSize),
                SuccessfulCount = day.Count(t => t.Status == TransferStatus.Completed),
                FailedCount = day.Count(t => t.Status == TransferStatus.Failed)
            });
        }

        TrendData = new ObservableCollection<TransferTrendData>(trendData);
    }

    private void LoadSampleData()
    {
        var random = new Random();
        var fileNames = new[] { 
            "Document.pdf", "Photo.jpg", "Video.mp4", "Archive.zip", "Spreadsheet.xlsx", 
            "Presentation.pptx", "Music.mp3", "Database.sql", "Code.cs", "Image.png",
            "Setup.exe", "Report.docx", "Data.csv", "Backup.7z", "Movie.mkv"
        };
        var peerNames = new[] { 
            "John's Laptop", "Sarah's Phone", "Office PC", "Home Desktop", 
            "MacBook Pro", "Android Tablet", "Gaming Rig", "Work Station"
        };

        for (int i = 0; i < 50; i++)
        {
            var fileName = fileNames[random.Next(fileNames.Length)];
            var fileSize = random.Next(1024, 500 * 1024 * 1024); // 1KB to 500MB
            var duration = TimeSpan.FromSeconds(random.Next(1, 600)); // 1s to 10m
            var speed = fileSize / duration.TotalSeconds;

            var transfer = new TransferHistory
            {
                Id = Guid.NewGuid().ToString(),
                FileName = fileName,
                FileSize = fileSize,
                Timestamp = DateTime.Now.AddDays(-random.Next(0, 60)).AddHours(-random.Next(0, 24)),
                Direction = random.Next(2) == 0 ? TransferDirection.Sending : TransferDirection.Receiving,
                Status = random.Next(10) < 8 ? TransferStatus.Completed : TransferStatus.Failed, // 80% success rate
                PeerName = peerNames[random.Next(peerNames.Length)],
                PeerIpAddress = $"192.168.1.{random.Next(2, 254)}",
                FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LocalDrop Downloads", fileName),
                Duration = duration,
                TransferSpeed = speed,
                ErrorMessage = random.Next(10) >= 8 ? "Network timeout occurred" : string.Empty
            };

            _allTransfers.Add(transfer);
        }

        ApplyFilters();
        UpdateStatistics();
        GenerateTrendData();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 