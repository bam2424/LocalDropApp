using LocalDropApp.Models;

namespace LocalDropApp.Services;

public interface IPeerDiscoveryService : IDisposable
{

    event EventHandler<PeerDevice>? PeerDiscovered;

    event EventHandler<PeerDevice>? PeerLost;

    event EventHandler<PeerDevice>? PeerUpdated;

    event EventHandler<string>? DiscoveryError;
  
    IReadOnlyList<PeerDevice> DiscoveredPeers { get; }

    bool IsRunning { get; }

    int DiscoveryPort { get; }

    Task StartAsync(string deviceName, int tcpPort);

    Task StopAsync();

    Task RefreshPeersAsync();

    PeerDevice GetLocalDevice();
} 