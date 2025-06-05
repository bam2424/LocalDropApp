using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using LocalDropApp.Models;

namespace LocalDropApp.Services;

public class FileTransferService : IFileTransferService
{
    private const int BUFFER_SIZE = 64 * 1024; // 64KB buffer
    private const int DEFAULT_PORT = 35732;
    
    private readonly ConcurrentDictionary<string, FileTransfer> _activeTransfers = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pendingRequests = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _transferCancellations = new();
    
    private TcpListener? _tcpListener;
    private CancellationTokenSource? _listenerCancellation;
    private bool _isListening;
    private int _listenPort;

    public event EventHandler<FileTransfer>? TransferStarted;
    public event EventHandler<FileTransfer>? TransferProgressUpdated;
    public event EventHandler<FileTransfer>? TransferCompleted;
    public event EventHandler<FileTransfer>? TransferFailed;
    public event EventHandler<FileTransferRequestPayload>? IncomingTransferRequest;
    public event EventHandler<string>? TransferError;
    public event EventHandler<(FileTransfer Transfer, string DownloadPath)>? IncomingFileCompleted;

    public IReadOnlyList<FileTransfer> ActiveTransfers => _activeTransfers.Values.ToList();
    public bool IsListening => _isListening;
    public int ListenPort => _listenPort;

    public async Task StartListeningAsync(int port = 0)
    {
        if (_isListening) return;

        // Try default port first, then use dynamic port allocation if it fails
        var startPort = port == 0 ? DEFAULT_PORT : port;
        TcpListener? listener = null;
        
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                var portToTry = startPort == DEFAULT_PORT && attempt > 0 ? 0 : startPort + attempt;
                listener = new TcpListener(IPAddress.Any, portToTry);
                listener.Start();
                _listenPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                break;
            }
            catch (SocketException)
            {
                listener?.Stop();
                listener = null;
                if (startPort != DEFAULT_PORT) break; // Don't retry if user specified a port
            }
        }

        if (listener == null)
        {
            TransferError?.Invoke(this, "Could not find an available port for file transfers");
            return;
        }

        _tcpListener = listener;
        _listenerCancellation = new CancellationTokenSource();
        _isListening = true;

        _ = Task.Run(AcceptConnectionsAsync, _listenerCancellation.Token);
    }

    public async Task StopListeningAsync()
    {
        if (!_isListening) return;

        _isListening = false;
        _listenerCancellation?.Cancel();
        _tcpListener?.Stop();

        foreach (var cancellation in _transferCancellations.Values)
        {
            cancellation.Cancel();
        }

        _activeTransfers.Clear();
        _pendingRequests.Clear();
        _transferCancellations.Clear();

        await Task.CompletedTask;
    }

    public async Task<bool> SendFileAsync(string filePath, PeerDevice targetPeer)
    {
        if (!File.Exists(filePath)) return false;

        var fileInfo = new FileInfo(filePath);
        const long maxFileSize = 10L * 1024 * 1024 * 1024; // 10GB limit
        
        if (fileInfo.Length > maxFileSize)
        {
            TransferError?.Invoke(this, $"File too large. Maximum size is {maxFileSize / (1024 * 1024 * 1024)}GB");
            return false;
        }
        var transferId = Guid.NewGuid().ToString();
        var cancellationSource = new CancellationTokenSource();
        
        var transfer = new FileTransfer
        {
            Id = transferId,
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            Direction = TransferDirection.Sending,
            TargetPeer = targetPeer,
            Status = TransferStatus.Pending,
            StartTime = DateTime.UtcNow
        };

        _activeTransfers.TryAdd(transferId, transfer);
        _transferCancellations.TryAdd(transferId, cancellationSource);
        TransferStarted?.Invoke(this, transfer);

        try
        {
            using var client = new TcpClient();
            var targetPort = targetPeer.Port > 0 ? targetPeer.Port : DEFAULT_PORT;
            await client.ConnectAsync(targetPeer.IpAddress, targetPort);
            
            var request = new FileTransferRequestPayload
            {
                TransferId = transferId,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FileHash = await CalculateFileHashAsync(filePath)
            };

            await SendRequestAsync(client.GetStream(), request);
            
            var response = await ReceiveResponseAsync(client.GetStream());
            if (response?.Accepted != true)
            {
                transfer.Status = TransferStatus.Failed;
                transfer.ErrorMessage = response?.RejectReason ?? "Transfer rejected";
                TransferFailed?.Invoke(this, transfer);
                return false;
            }

            transfer.Status = TransferStatus.InProgress;
            TransferProgressUpdated?.Invoke(this, transfer);

            await SendFileDataAsync(client.GetStream(), filePath, transfer, cancellationSource.Token);
            
            transfer.Status = TransferStatus.Completed;
            TransferCompleted?.Invoke(this, transfer);
            return true;
        }
        catch (Exception ex)
        {
            transfer.Status = TransferStatus.Failed;
            transfer.ErrorMessage = ex.Message;
            TransferFailed?.Invoke(this, transfer);
            return false;
        }
        finally
        {
            _activeTransfers.TryRemove(transferId, out _);
            _transferCancellations.TryRemove(transferId, out _);
            cancellationSource.Dispose();
        }
    }

    public async Task<bool> AcceptIncomingTransferAsync(string transferId, string downloadPath)
    {
        if (!_pendingRequests.TryGetValue(transferId, out var tcs))
            return false;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);
            tcs.SetResult(true);
            return true;
        }
        catch
        {
            tcs.SetResult(false);
            return false;
        }
    }

    public async Task<bool> RejectIncomingTransferAsync(string transferId, string reason = "")
    {
        if (!_pendingRequests.TryGetValue(transferId, out var tcs))
            return false;

        tcs.SetResult(false);
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> CancelTransferAsync(string transferId)
    {
        if (_transferCancellations.TryGetValue(transferId, out var cancellation))
        {
            cancellation.Cancel();
            
            if (_activeTransfers.TryGetValue(transferId, out var transfer))
            {
                transfer.Status = TransferStatus.Cancelled;
                TransferFailed?.Invoke(this, transfer);
            }
        }

        await Task.CompletedTask;
        return true;
    }

    private async Task AcceptConnectionsAsync()
    {
        while (!_listenerCancellation!.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _tcpListener!.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleIncomingConnectionAsync(client), _listenerCancellation.Token);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                TransferError?.Invoke(this, $"Error accepting connection: {ex.Message}");
            }
        }
    }

    private async Task HandleIncomingConnectionAsync(TcpClient client)
    {
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var request = await ReceiveRequestAsync(stream);
                
                if (request == null) return;

                var tcs = new TaskCompletionSource<bool>();
                _pendingRequests.TryAdd(request.TransferId, tcs);
                
                IncomingTransferRequest?.Invoke(this, request);
                
                var accepted = await tcs.Task;
                var response = new FileTransferResponsePayload
                {
                    TransferId = request.TransferId,
                    Accepted = accepted,
                    RejectReason = accepted ? "" : "User rejected transfer",
                    ListenPort = _listenPort
                };

                await SendResponseAsync(stream, response);

                if (accepted)
                {
                    await ReceiveFileDataAsync(stream, request);
                }
            }
        }
        catch (Exception ex)
        {
            TransferError?.Invoke(this, $"Error handling incoming connection: {ex.Message}");
        }
    }

    private async Task SendRequestAsync(NetworkStream stream, FileTransferRequestPayload request)
    {
        var message = new NetworkMessage
        {
            Type = MessageType.FileTransferRequest
        };
        message.SetPayload(request);
        
        var data = System.Text.Encoding.UTF8.GetBytes(message.ToJson());
        var lengthBytes = BitConverter.GetBytes(data.Length);
        
        await stream.WriteAsync(lengthBytes);
        await stream.WriteAsync(data);
    }

    private async Task<FileTransferRequestPayload?> ReceiveRequestAsync(NetworkStream stream)
    {
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes);
        var length = BitConverter.ToInt32(lengthBytes);
        
        var data = new byte[length];
        await stream.ReadExactlyAsync(data);
        
        var json = System.Text.Encoding.UTF8.GetString(data);
        var message = NetworkMessage.FromJson(json);
        
        return message?.GetPayload<FileTransferRequestPayload>();
    }

    private async Task SendResponseAsync(NetworkStream stream, FileTransferResponsePayload response)
    {
        var message = new NetworkMessage
        {
            Type = MessageType.FileTransferResponse
        };
        message.SetPayload(response);
        
        var data = System.Text.Encoding.UTF8.GetBytes(message.ToJson());
        var lengthBytes = BitConverter.GetBytes(data.Length);
        
        await stream.WriteAsync(lengthBytes);
        await stream.WriteAsync(data);
    }

    private async Task<FileTransferResponsePayload?> ReceiveResponseAsync(NetworkStream stream)
    {
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes);
        var length = BitConverter.ToInt32(lengthBytes);
        
        var data = new byte[length];
        await stream.ReadExactlyAsync(data);
        
        var json = System.Text.Encoding.UTF8.GetString(data);
        var message = NetworkMessage.FromJson(json);
        
        return message?.GetPayload<FileTransferResponsePayload>();
    }

    private async Task SendFileDataAsync(NetworkStream stream, string filePath, FileTransfer transfer, CancellationToken cancellationToken)
    {
        using var fileStream = File.OpenRead(filePath);
        var buffer = new byte[BUFFER_SIZE];
        var totalBytes = fileStream.Length;
        var bytesRead = 0L;

        while (bytesRead < totalBytes && !cancellationToken.IsCancellationRequested)
        {
            var chunkSize = await fileStream.ReadAsync(buffer, cancellationToken);
            if (chunkSize == 0) break;

            await stream.WriteAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
            bytesRead += chunkSize;

            transfer.BytesTransferred = bytesRead;
            TransferProgressUpdated?.Invoke(this, transfer);
        }
    }

    private async Task ReceiveFileDataAsync(NetworkStream stream, FileTransferRequestPayload request)
    {
        var downloadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LocalDropApp Downloads",
            request.FileName);

        var transfer = new FileTransfer
        {
            Id = request.TransferId,
            FileName = request.FileName,
            FileSize = request.FileSize,
            Direction = TransferDirection.Receiving,
            Status = TransferStatus.InProgress,
            StartTime = DateTime.UtcNow,
            DownloadPath = downloadPath
        };

        _activeTransfers.TryAdd(request.TransferId, transfer);
        TransferStarted?.Invoke(this, transfer);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(downloadPath)!);
            
            using var fileStream = File.Create(downloadPath);
            var buffer = new byte[BUFFER_SIZE];
            var totalBytes = request.FileSize;
            var bytesReceived = 0L;

            while (bytesReceived < totalBytes)
            {
                var remainingBytes = (int)Math.Min(BUFFER_SIZE, totalBytes - bytesReceived);
                var bytesRead = await stream.ReadAsync(buffer, 0, remainingBytes);
                
                if (bytesRead == 0) break;

                await fileStream.WriteAsync(buffer, 0, bytesRead);
                bytesReceived += bytesRead;

                transfer.BytesTransferred = bytesReceived;
                TransferProgressUpdated?.Invoke(this, transfer);
            }

            transfer.Status = TransferStatus.Completed;
            TransferCompleted?.Invoke(this, transfer);
            
            // Fire special event for incoming files with download path
            if (transfer.Direction == TransferDirection.Receiving)
            {
                IncomingFileCompleted?.Invoke(this, (transfer, downloadPath));
            }
        }
        catch (Exception ex)
        {
            transfer.Status = TransferStatus.Failed;
            transfer.ErrorMessage = ex.Message;
            TransferFailed?.Invoke(this, transfer);
        }
        finally
        {
            _activeTransfers.TryRemove(request.TransferId, out _);
            _pendingRequests.TryRemove(request.TransferId, out _);
        }
    }

    private static async Task<string> CalculateFileHashAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
        return Convert.ToHexString(hashBytes);
    }

    public void Dispose()
    {
        StopListeningAsync().Wait();
        _listenerCancellation?.Dispose();
        
        foreach (var cancellation in _transferCancellations.Values)
        {
            cancellation.Dispose();
        }
        
        _transferCancellations.Clear();
    }
} 