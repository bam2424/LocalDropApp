using LocalDropApp.Models;

namespace LocalDropApp.Services;

public interface IFileTransferService : IDisposable
{
    event EventHandler<FileTransfer>? TransferStarted;
    event EventHandler<FileTransfer>? TransferProgressUpdated;
    event EventHandler<FileTransfer>? TransferCompleted;
    event EventHandler<FileTransfer>? TransferFailed;
    event EventHandler<FileTransferRequestPayload>? IncomingTransferRequest;
    event EventHandler<string>? TransferError;

    IReadOnlyList<FileTransfer> ActiveTransfers { get; }
    bool IsListening { get; }
    int ListenPort { get; }

    Task StartListeningAsync(int port = 0);
    Task StopListeningAsync();
    Task<bool> SendFileAsync(string filePath, PeerDevice targetPeer);
    Task<bool> AcceptIncomingTransferAsync(string transferId, string downloadPath);
    Task<bool> RejectIncomingTransferAsync(string transferId, string reason = "");
    Task<bool> CancelTransferAsync(string transferId);
} 