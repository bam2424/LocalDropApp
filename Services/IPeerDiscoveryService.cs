using LocalDropApp.Models;

namespace LocalDropApp.Services;

/// <summary>
/// Interface for peer discovery functionality using UDP broadcast.
/// 
/// This service is responsible for:
/// 1. Broadcasting device presence on the local network
/// 2. Listening for other devices announcing themselves
/// 3. Maintaining a list of discovered peers with their online status
/// 4. Managing heartbeat/keep-alive mechanism
/// 
/// Design Principles:
/// - Thread-safe operations for concurrent access
/// - Graceful error handling with proper logging
/// - Event-driven architecture for UI responsiveness
/// - Clean resource disposal to prevent memory leaks
/// </summary>
public interface IPeerDiscoveryService : IDisposable
{
    /// <summary>
    /// Event fired when a new peer is discovered on the network
    /// </summary>
    event EventHandler<PeerDevice>? PeerDiscovered;
    
    /// <summary>
    /// Event fired when a peer goes offline or is no longer reachable
    /// </summary>
    event EventHandler<PeerDevice>? PeerLost;
    
    /// <summary>
    /// Event fired when a peer's information is updated (name change, status, etc.)
    /// </summary>
    event EventHandler<PeerDevice>? PeerUpdated;
    
    /// <summary>
    /// Event fired when there's an error in the discovery process
    /// UI can show appropriate error messages to the user
    /// </summary>
    event EventHandler<string>? DiscoveryError;
    
    /// <summary>
    /// Current list of discovered peers
    /// This is a live collection that updates as peers come and go
    /// </summary>
    IReadOnlyList<PeerDevice> DiscoveredPeers { get; }
    
    /// <summary>
    /// Whether the discovery service is currently running
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Port number used for UDP broadcast discovery
    /// Default: 35731 (chosen to avoid common port conflicts)
    /// </summary>
    int DiscoveryPort { get; }
    
    /// <summary>
    /// Starts the peer discovery service
    /// Begins broadcasting device presence and listening for other devices
    /// </summary>
    /// <param name="deviceName">Name of this device to broadcast to peers</param>
    /// <param name="tcpPort">TCP port this device listens on for file transfers</param>
    /// <returns>Task that completes when service is started</returns>
    Task StartAsync(string deviceName, int tcpPort);
    
    /// <summary>
    /// Stops the peer discovery service
    /// Stops broadcasting and listening, cleans up resources
    /// </summary>
    /// <returns>Task that completes when service is stopped</returns>
    Task StopAsync();
    
    /// <summary>
    /// Manually triggers a discovery broadcast
    /// Useful for refreshing the peer list on demand
    /// </summary>
    /// <returns>Task that completes when broadcast is sent</returns>
    Task RefreshPeersAsync();
    
    /// <summary>
    /// Gets information about the local device
    /// </summary>
    /// <returns>PeerDevice representing this device</returns>
    PeerDevice GetLocalDevice();
} 