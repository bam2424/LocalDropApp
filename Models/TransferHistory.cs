using System.ComponentModel;

namespace LocalDropApp.Models;

public class TransferHistory : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _fileName = string.Empty;
    private long _fileSize;
    private DateTime _timestamp;
    private TransferDirection _direction;
    private TransferStatus _status;
    private string _peerName = string.Empty;
    private string _peerIpAddress = string.Empty;
    private string _filePath = string.Empty;
    private TimeSpan _duration;
    private double _transferSpeed;
    private string _errorMessage = string.Empty;

    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public long FileSize
    {
        get => _fileSize;
        set { _fileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeFormatted)); }
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeAgo)); OnPropertyChanged(nameof(FormattedDate)); }
    }

    public TransferDirection Direction
    {
        get => _direction;
        set { _direction = value; OnPropertyChanged(); OnPropertyChanged(nameof(DirectionIcon)); OnPropertyChanged(nameof(DirectionText)); }
    }

    public TransferStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); OnPropertyChanged(nameof(StatusColor)); }
    }

    public string PeerName
    {
        get => _peerName;
        set { _peerName = value; OnPropertyChanged(); }
    }

    public string PeerIpAddress
    {
        get => _peerIpAddress;
        set { _peerIpAddress = value; OnPropertyChanged(); }
    }

    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }

    public TimeSpan Duration
    {
        get => _duration;
        set { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationFormatted)); }
    }

    public double TransferSpeed
    {
        get => _transferSpeed;
        set { _transferSpeed = value; OnPropertyChanged(); OnPropertyChanged(nameof(TransferSpeedFormatted)); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    // Computed Properties
    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }

    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.Now - Timestamp;
            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalHours < 1) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalDays < 1) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            return Timestamp.ToString("MMM dd");
        }
    }

    public string FormattedDate => Timestamp.ToString("MMM dd, yyyy HH:mm");

    public string DirectionIcon => Status == TransferStatus.Completed ? "✅" : "❌";
    public string DirectionText => Direction == TransferDirection.Sending ? "Sent" : "Received";

    public string StatusIcon => Status switch
    {
        TransferStatus.Completed => "✅",
        TransferStatus.Failed => "❌",
        TransferStatus.Cancelled => "⏹️",
        TransferStatus.InProgress => "🔄",
        _ => "❓"
    };

    public Color StatusColor => Status switch
    {
        TransferStatus.Completed => Colors.Green,
        TransferStatus.Failed => Colors.Red,
        TransferStatus.Cancelled => Colors.Orange,
        TransferStatus.InProgress => Colors.Blue,
        _ => Colors.Gray
    };

    public string DurationFormatted
    {
        get
        {
            if (Duration.TotalSeconds < 60) return $"{Duration.TotalSeconds:F1}s";
            if (Duration.TotalMinutes < 60) return $"{Duration.TotalMinutes:F1}m";
            return $"{Duration.TotalHours:F1}h";
        }
    }

    public string TransferSpeedFormatted
    {
        get
        {
            if (TransferSpeed < 1024) return $"{TransferSpeed:F1} B/s";
            if (TransferSpeed < 1024 * 1024) return $"{TransferSpeed / 1024.0:F1} KB/s";
            return $"{TransferSpeed / (1024.0 * 1024.0):F1} MB/s";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TransferTrendData
{
    public DateTime Date { get; set; }
    public int TransferCount { get; set; }
    public long TotalBytes { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }

    public string FormattedDate => Date.ToString("MMM dd");
    
    public string TotalBytesFormatted
    {
        get
        {
            if (TotalBytes < 1024) return $"{TotalBytes} B";
            if (TotalBytes < 1024 * 1024) return $"{TotalBytes / 1024.0:F1} KB";
            if (TotalBytes < 1024 * 1024 * 1024) return $"{TotalBytes / (1024.0 * 1024.0):F1} MB";
            return $"{TotalBytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
    }

    public double SuccessRate => TransferCount > 0 ? (SuccessfulCount * 100.0 / TransferCount) : 0;
} 