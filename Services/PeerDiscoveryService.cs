using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using LocalDropApp.Models;

namespace LocalDropApp.Services;

public class PeerDiscoveryService : IPeerDiscoveryService
{
    private static int _baseDiscoveryPort = 35731;
    private const int HEARTBEAT_INTERVAL_MS = 3000; // More frequent heartbeats for better discovery
    private const int PEER_TIMEOUT_MS = 12000; // Shorter timeout to match faster heartbeat
    
    private readonly ConcurrentDictionary<string, PeerDevice> _discoveredPeers = new();
    private readonly Timer _heartbeatTimer;
    private readonly Timer _peerTimeoutTimer;
    
    private UdpClient? _udpListener;
    private UdpClient? _udpBroadcaster;
    private CancellationTokenSource? _cancellationTokenSource;
    
    private string _localDeviceId = Guid.NewGuid().ToString();
    private string _localDeviceName = Environment.MachineName;
    private string _localIpAddress = string.Empty;
    private int _localTcpPort;
    private bool _isRunning;

    public event EventHandler<PeerDevice>? PeerDiscovered;
    public event EventHandler<PeerDevice>? PeerLost;
    public event EventHandler<PeerDevice>? PeerUpdated;
    public event EventHandler<string>? DiscoveryError;

    public IReadOnlyList<PeerDevice> DiscoveredPeers => _discoveredPeers.Values.ToList();
    public bool IsRunning => _isRunning;
    public int DiscoveryPort { get; private set; } = _baseDiscoveryPort;

    public PeerDiscoveryService()
    {
        _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, Timeout.Infinite);
        _peerTimeoutTimer = new Timer(CheckPeerTimeouts, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task StartAsync(string deviceName, int tcpPort)
    {
        if (_isRunning) return;

        _localDeviceName = deviceName;
        _localTcpPort = tcpPort;
        _localIpAddress = GetLocalIpAddress();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await StartUdpListener();
            await StartUdpBroadcaster();
            
            _heartbeatTimer.Change(0, HEARTBEAT_INTERVAL_MS);
            _peerTimeoutTimer.Change(PEER_TIMEOUT_MS, PEER_TIMEOUT_MS);
            
            _isRunning = true;
            
            // Send multiple discovery messages on startup for better reliability
            _ = Task.Run(async () =>
            {
                for (int i = 0; i < 3; i++)
                {
                    await SendDiscoveryMessage();
                    await Task.Delay(500); // 500ms between discovery attempts
                }
            });
            
            // Debug logging
            DiscoveryError?.Invoke(this, $"DEBUG: Started discovery for {_localDeviceName} (ID: {_localDeviceId[..8]}...) on IP {_localIpAddress}, TCP port {_localTcpPort}");
        }
        catch (Exception ex)
        {
            DiscoveryError?.Invoke(this, $"Failed to start discovery: {ex.Message}");
            await StopAsync();
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        
        _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _peerTimeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);

        _udpListener?.Close();
        _udpBroadcaster?.Close();
        
        _discoveredPeers.Clear();
        
        await Task.CompletedTask;
    }

    public async Task RefreshPeersAsync()
    {
        if (!_isRunning) return;
        
        // Send multiple discovery messages for better reliability
        for (int i = 0; i < 3; i++)
        {
            await SendDiscoveryMessage();
            if (i < 2) await Task.Delay(300); // Small delay between attempts
        }
    }

    public PeerDevice GetLocalDevice()
    {
        return new PeerDevice
        {
            Id = _localDeviceId,
            Name = _localDeviceName,
            IpAddress = _localIpAddress,
            IsOnline = true,
            LastSeen = DateTime.UtcNow
        };
    }

    private async Task StartUdpListener()
    {
        // Try the base port first, then find an available port
        var port = _baseDiscoveryPort;
        UdpClient? client = null;
        
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                client = new UdpClient(port);
                client.EnableBroadcast = true;
                break;
            }
            catch (SocketException)
            {
                client?.Dispose();
                port++;
            }
        }
        
        if (client == null)
        {
            throw new InvalidOperationException("Could not find an available UDP port for discovery");
        }
        
        _udpListener = client;
        
        // Update our discovery port to the actual port we're using
        DiscoveryPort = ((IPEndPoint)_udpListener.Client.LocalEndPoint!).Port;
        
        // Debug logging for port allocation
        DiscoveryError?.Invoke(this, $"DEBUG: UDP listener started on port {DiscoveryPort}");
        
        _ = Task.Run(async () =>
        {
            while (!_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync();
                    await ProcessReceivedMessage(result.Buffer, result.RemoteEndPoint);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DiscoveryError?.Invoke(this, $"Error receiving UDP message: {ex.Message}");
                }
            }
        }, _cancellationTokenSource.Token);
    }

    private async Task StartUdpBroadcaster()
    {
        _udpBroadcaster = new UdpClient();
        _udpBroadcaster.EnableBroadcast = true;
        await SendDiscoveryMessage();
    }

    private async Task ProcessReceivedMessage(byte[] data, IPEndPoint remoteEndPoint)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            var message = NetworkMessage.FromJson(json);
            
            if (message == null)
                return;
                
            // Debug: log all received messages
            DiscoveryError?.Invoke(this, $"DEBUG: Received message from {message.SenderName} (ID: {message.SenderId[..8]}...)");
            
            if (message.SenderId == _localDeviceId)
            {
                DiscoveryError?.Invoke(this, $"DEBUG: Ignoring message from self (ID: {_localDeviceId[..8]}...)");
                return;
            }

            var peer = GetOrCreatePeer(message);
            peer.LastSeen = DateTime.UtcNow;
            peer.IsOnline = true;

            switch (message.Type)
            {
                case MessageType.Discovery:
                    if (_discoveredPeers.TryAdd(peer.Id, peer))
                    {
                        PeerDiscovered?.Invoke(this, peer);
                        await SendHeartbeatResponse(remoteEndPoint);
                    }
                    else
                    {
                        PeerUpdated?.Invoke(this, peer);
                    }
                    break;
                    
                case MessageType.Heartbeat:
                    if (_discoveredPeers.ContainsKey(peer.Id))
                    {
                        PeerUpdated?.Invoke(this, peer);
                    }
                    else
                    {
                        _discoveredPeers.TryAdd(peer.Id, peer);
                        PeerDiscovered?.Invoke(this, peer);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            DiscoveryError?.Invoke(this, $"Error processing message: {ex.Message}");
        }
    }

    private PeerDevice GetOrCreatePeer(NetworkMessage message)
    {
        if (_discoveredPeers.TryGetValue(message.SenderId, out var existingPeer))
        {
            existingPeer.Name = message.SenderName;
            existingPeer.IpAddress = message.SenderIpAddress;
            existingPeer.Port = message.SenderPort;
            return existingPeer;
        }

        return new PeerDevice
        {
            Id = message.SenderId,
            Name = message.SenderName,
            IpAddress = message.SenderIpAddress,
            Port = message.SenderPort,
            IsOnline = true,
            LastSeen = DateTime.UtcNow
        };
    }

    private async void SendHeartbeat(object? state)
    {
        if (!_isRunning) return;
        
        try
        {
            var message = CreateNetworkMessage(MessageType.Heartbeat);
            await BroadcastMessage(message);
        }
        catch (Exception ex)
        {
            DiscoveryError?.Invoke(this, $"Error sending heartbeat: {ex.Message}");
        }
    }

    private async Task SendDiscoveryMessage()
    {
        try
        {
            var message = CreateNetworkMessage(MessageType.Discovery);
            await BroadcastMessage(message);
        }
        catch (Exception ex)
        {
            DiscoveryError?.Invoke(this, $"Error sending discovery: {ex.Message}");
        }
    }

    private async Task SendHeartbeatResponse(IPEndPoint target)
    {
        try
        {
            var message = CreateNetworkMessage(MessageType.Heartbeat);
            var data = Encoding.UTF8.GetBytes(message.ToJson());
            await _udpBroadcaster!.SendAsync(data, data.Length, target);
        }
        catch (Exception ex)
        {
            DiscoveryError?.Invoke(this, $"Error sending heartbeat response: {ex.Message}");
        }
    }

    private async Task BroadcastMessage(NetworkMessage message)
    {
        var data = Encoding.UTF8.GetBytes(message.ToJson());
        
        // Get all available broadcast addresses for better network coverage
        var broadcastAddresses = GetBroadcastAddresses();
        
        foreach (var broadcastAddr in broadcastAddresses)
        {
            // Broadcast to a range of ports to reach all instances
            for (int port = _baseDiscoveryPort; port < _baseDiscoveryPort + 10; port++)
            {
                try
                {
                    var broadcastEndpoint = new IPEndPoint(broadcastAddr, port);
                    await _udpBroadcaster!.SendAsync(data, data.Length, broadcastEndpoint);
                }
                catch
                {
                    // Ignore send errors for individual ports/addresses
                }
            }
        }
    }

    private List<IPAddress> GetBroadcastAddresses()
    {
        var broadcastAddresses = new List<IPAddress> { IPAddress.Broadcast }; // 255.255.255.255
        
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up 
                           && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                           && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();
                var unicastAddresses = ipProperties.UnicastAddresses
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork
                               && !IPAddress.IsLoopback(ua.Address));

                foreach (var unicastAddress in unicastAddresses)
                {
                    try
                    {
                        // Calculate broadcast address for this subnet
                        var ip = unicastAddress.Address.GetAddressBytes();
                        var mask = unicastAddress.IPv4Mask?.GetAddressBytes();
                        
                        if (mask != null && mask.Length == 4)
                        {
                            var broadcast = new byte[4];
                            for (int i = 0; i < 4; i++)
                            {
                                broadcast[i] = (byte)(ip[i] | ~mask[i]);
                            }
                            
                            var broadcastAddr = new IPAddress(broadcast);
                            if (!broadcastAddresses.Contains(broadcastAddr))
                            {
                                broadcastAddresses.Add(broadcastAddr);
                            }
                        }
                    }
                    catch
                    {
                        // Skip this interface if we can't calculate broadcast
                    }
                }
            }
        }
        catch
        {
            // If anything fails, we still have the global broadcast
        }
        
        return broadcastAddresses;
    }

    private NetworkMessage CreateNetworkMessage(MessageType type)
    {
        return new NetworkMessage
        {
            Type = type,
            SenderId = _localDeviceId,
            SenderName = _localDeviceName,
            SenderIpAddress = _localIpAddress,
            SenderPort = _localTcpPort
        };
    }

    private async void CheckPeerTimeouts(object? state)
    {
        if (!_isRunning) return;

        var now = DateTime.UtcNow;
        var timeoutThreshold = now.AddMilliseconds(-PEER_TIMEOUT_MS);
        var timedOutPeers = new List<PeerDevice>();

        foreach (var kvp in _discoveredPeers)
        {
            if (kvp.Value.LastSeen < timeoutThreshold)
            {
                kvp.Value.IsOnline = false;
                timedOutPeers.Add(kvp.Value);
                _discoveredPeers.TryRemove(kvp.Key, out _);
            }
        }

        foreach (var peer in timedOutPeers)
        {
            PeerLost?.Invoke(this, peer);
        }

        await Task.CompletedTask;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up 
                           && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                           && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            var allAddresses = new List<string>();

            foreach (var networkInterface in networkInterfaces)
            {
                var ipProperties = networkInterface.GetIPProperties();
                var unicastAddresses = ipProperties.UnicastAddresses
                    .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork
                               && !IPAddress.IsLoopback(ua.Address))
                    .Select(ua => ua.Address.ToString())
                    .ToList();

                allAddresses.AddRange(unicastAddresses);
            }

            // Prefer private network addresses
            var privateAddress = allAddresses.FirstOrDefault(addr => 
                addr.StartsWith("192.168.") || 
                addr.StartsWith("10.") || 
                (addr.StartsWith("172.") && int.TryParse(addr.Split('.')[1], out var second) && second >= 16 && second <= 31));

            return privateAddress ?? allAddresses.FirstOrDefault() ?? "127.0.0.1";
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _heartbeatTimer?.Dispose();
        _peerTimeoutTimer?.Dispose();
        _cancellationTokenSource?.Dispose();
        _udpListener?.Dispose();
        _udpBroadcaster?.Dispose();
    }
} 