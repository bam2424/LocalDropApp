using System.Text.Json;

namespace LocalDropApp.Models;

/// <summary>
/// Represents a network message exchanged between peers.
/// Used for peer discovery, file transfer negotiation, and status updates.
/// 
/// Message Protocol:
/// - Discovery: Announces device presence on the network
/// - Heartbeat: Periodic keep-alive to maintain peer status
/// - FileTransferRequest: Initiates a file transfer
/// - FileTransferResponse: Accepts/rejects a transfer request
/// - TransferStatus: Updates transfer progress/completion
/// </summary>
public class NetworkMessage
{
    /// <summary>
    /// Unique identifier for this message
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of message being sent (Discovery, Heartbeat, FileTransfer, etc.)
    /// </summary>
    public MessageType Type { get; set; }
    
    /// <summary>
    /// Device ID of the sender
    /// </summary>
    public string SenderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Device name of the sender for display purposes
    /// </summary>
    public string SenderName { get; set; } = string.Empty;
    
    /// <summary>
    /// IP address of the sender
    /// </summary>
    public string SenderIpAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Port number the sender is listening on for TCP connections
    /// </summary>
    public int SenderPort { get; set; }
    
    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional payload data specific to the message type
    /// Serialized as JSON for flexibility
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Serializes the message to JSON for network transmission
    /// </summary>
    /// <returns>JSON string representation of the message</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
    
    /// <summary>
    /// Deserializes a JSON string back to a NetworkMessage
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <returns>NetworkMessage instance or null if deserialization fails</returns>
    public static NetworkMessage? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<NetworkMessage>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            // Return null for invalid JSON - calling code should handle this gracefully
            return null;
        }
    }
    
    /// <summary>
    /// Sets the payload with automatic JSON serialization
    /// </summary>
    /// <typeparam name="T">Type of payload object</typeparam>
    /// <param name="payload">Object to serialize as payload</param>
    public void SetPayload<T>(T payload)
    {
        PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
    
    /// <summary>
    /// Gets the payload deserialized to the specified type
    /// </summary>
    /// <typeparam name="T">Type to deserialize payload to</typeparam>
    /// <returns>Deserialized payload or default(T) if deserialization fails</returns>
    public T? GetPayload<T>()
    {
        if (string.IsNullOrEmpty(PayloadJson))
            return default(T);
            
        try
        {
            return JsonSerializer.Deserialize<T>(PayloadJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return default(T);
        }
    }
}

/// <summary>
/// Defines the types of messages that can be exchanged between peers
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Device announcement for peer discovery
    /// </summary>
    Discovery,
    
    /// <summary>
    /// Periodic heartbeat to maintain peer status
    /// </summary>
    Heartbeat,
    
    /// <summary>
    /// Request to start a file transfer
    /// </summary>
    FileTransferRequest,
    
    /// <summary>
    /// Response to a file transfer request (accept/reject)
    /// </summary>
    FileTransferResponse,
    
    /// <summary>
    /// Update on transfer progress or completion
    /// </summary>
    TransferStatus
}

/// <summary>
/// Payload for file transfer request messages
/// </summary>
public class FileTransferRequestPayload
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string TransferId { get; set; } = string.Empty;
}

/// <summary>
/// Payload for file transfer response messages
/// </summary>
public class FileTransferResponsePayload
{
    public string TransferId { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public string RejectReason { get; set; } = string.Empty;
    public int ListenPort { get; set; }
}

/// <summary>
/// Payload for transfer status messages
/// </summary>
public class TransferStatusPayload
{
    public string TransferId { get; set; } = string.Empty;
    public TransferStatus Status { get; set; }
    public long BytesTransferred { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
} 