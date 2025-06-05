using System.ComponentModel;

namespace LocalDropApp.Models;

public enum TransferStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum TransferDirection
{
    Sending,
    Receiving
}

public class FileTransfer : INotifyPropertyChanged
{
    private string _fileName = string.Empty;
    private long _fileSize;
    private long _bytesTransferred;
    private TransferStatus _status = TransferStatus.Pending;
    private double _progressPercentage;
    private string _errorMessage = string.Empty;
    private DateTime _startTime;
    private TimeSpan _estimatedTimeRemaining;
    private string _downloadPath = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged(nameof(FileName));
        }
    }

    public long FileSize
    {
        get => _fileSize;
        set
        {
            _fileSize = value;
            OnPropertyChanged(nameof(FileSize));
            OnPropertyChanged(nameof(FileSizeFormatted));
            UpdateProgress();
        }
    }

    public long BytesTransferred
    {
        get => _bytesTransferred;
        set
        {
            _bytesTransferred = value;
            OnPropertyChanged(nameof(BytesTransferred));
            UpdateProgress();
        }
    }

    public TransferStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    public TransferDirection Direction { get; set; }

    public PeerDevice? TargetPeer { get; set; }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        private set
        {
            _progressPercentage = value;
            OnPropertyChanged(nameof(ProgressPercentage));
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
        }
    }

    public DateTime StartTime
    {
        get => _startTime;
        set
        {
            _startTime = value;
            OnPropertyChanged(nameof(StartTime));
        }
    }

    public TimeSpan EstimatedTimeRemaining
    {
        get => _estimatedTimeRemaining;
        set
        {
            _estimatedTimeRemaining = value;
            OnPropertyChanged(nameof(EstimatedTimeRemaining));
            OnPropertyChanged(nameof(EstimatedTimeRemainingText));
        }
    }

    public string DownloadPath
    {
        get => _downloadPath;
        set
        {
            _downloadPath = value;
            OnPropertyChanged(nameof(DownloadPath));
        }
    }

    // Computed properties
    public string StatusText => Status switch
    {
        TransferStatus.Pending => "Pending",
        TransferStatus.InProgress => "In Progress",
        TransferStatus.Completed => "Completed",
        TransferStatus.Failed => "Failed",
        TransferStatus.Cancelled => "Cancelled",
        _ => "Unknown"
    };

    public Color StatusColor => Status switch
    {
        TransferStatus.Pending => Colors.Orange,
        TransferStatus.InProgress => Colors.Blue,
        TransferStatus.Completed => Colors.Green,
        TransferStatus.Failed => Colors.Red,
        TransferStatus.Cancelled => Colors.Gray,
        _ => Colors.Black
    };

    public string FileSizeFormatted => FormatFileSize(FileSize);
    public string BytesTransferredFormatted => FormatFileSize(BytesTransferred);
    
    public string EstimatedTimeRemainingText => EstimatedTimeRemaining.TotalSeconds > 0 
        ? $"{EstimatedTimeRemaining:mm\\:ss} remaining" 
        : "";

    private void UpdateProgress()
    {
        if (FileSize > 0)
        {
            ProgressPercentage = (double)BytesTransferred / FileSize * 100;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 