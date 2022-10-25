using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;

namespace Dotplay.Network;

/// <summary>
/// The server network service
/// </summary>
public class ServerNetworkService : NetworkService
{
    private EventBasedNetListener? _listener;

    public delegate void ClientConnectedHandler(short clientId);

    public delegate void ClientDisconnectHandler(short clientId, DisconnectReason reason);

    public delegate void ClientLatencyUpdateHandler(short clientId, int latency);

    public delegate void ConnectionEstablishedHandler();

    public delegate void ConnectionRequestHandler(ConnectionRequest request);

    public delegate void ConnectionShutdownHandler();

    public event ClientConnectedHandler? ClientConnected;

    public event ClientDisconnectHandler? ClientDisconnect;

    public event ClientLatencyUpdateHandler? ClientLatencyUpdate;

    public event ConnectionEstablishedHandler? ConnectionEstablished;

    public event ConnectionRequestHandler? ConnectionRequest;

    public event ConnectionShutdownHandler? ConnectionShutdown;

    /// <summary>
    /// Bind server on specific port
    /// </summary>
    /// <param name="port"></param>
    public void Bind(int port)
    {
        this._listener = new EventBasedNetListener();
        this.NetManager = new NetManager(this._listener)
        {
            DebugName = "ServerNetworkLayer",
            AutoRecycle = true,
            EnableStatistics = true,
            UnconnectedMessagesEnabled = true
        };

        this._listener.NetworkErrorEvent += this.HandleNetworkError;
        this._listener.ConnectionRequestEvent += this.HandleRequest;
        this._listener.PeerConnectedEvent += this.HandlePeerConnect;
        this._listener.NetworkReceiveEvent += this.HandleReceive;
        this._listener.PeerDisconnectedEvent += this.HandlePeerDisconnect;
        this._listener.NetworkLatencyUpdateEvent += this.HandleLatency;

        Logger.LogDebug(this, "Try to start on port " + port);

        var result = this.NetManager.Start(port);
        if (result)
        {
            Logger.LogDebug(this, "Bind on port " + port);
            ConnectionEstablished?.Invoke();
        }
    }

    /// <summary>
    /// Get all connected clients
    /// </summary>
    public List<NetPeer> GetConnectedPeers()
    {
        return this.NetManager!.ConnectedPeerList;
    }

    /// <summary>
    /// Get count of current active connections
    /// </summary>
    public int GetConnectionCount()
    {
        return this.NetManager!.ConnectedPeersCount;
    }

    /// <inheritdoc />
    public override void Register()
    {
        base.Register();
    }

    /// <inheritdoc />
    public override void Unregister()
    {
        if (this._listener is null)
        {
            return;
        }

        this._listener.NetworkErrorEvent += this.HandleNetworkError;
        this._listener.ConnectionRequestEvent += this.HandleRequest;
        this._listener.PeerConnectedEvent += this.HandlePeerConnect;
        this._listener.NetworkReceiveEvent += this.HandleReceive;
        this._listener.PeerDisconnectedEvent += this.HandlePeerDisconnect;
        this._listener.NetworkLatencyUpdateEvent += this.HandleLatency;

        base.Unregister();
        ConnectionShutdown?.Invoke();
    }

    /// <summary>
    /// Handles the latency.
    /// </summary>
    /// <param name="peer">The peer.</param>
    /// <param name="latency">The latency.</param>
    private void HandleLatency(NetPeer peer, int latency)
    {
        this.PeerLatency[peer] = latency;
        this.ClientLatencyUpdate?.Invoke((short)peer.Id, latency);
    }

    /// <summary>
    /// Handles the network error.
    /// </summary>
    /// <param name="endPoint">The end point.</param>
    /// <param name="socketErrorCode">The socket error code.</param>
    private void HandleNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        Logger.LogDebug(this, $"Issue on {endPoint.Address}:{endPoint.Port} with error {socketErrorCode}");
    }

    /// <summary>
    /// Handles the peer connect.
    /// </summary>
    /// <param name="peer">The peer.</param>
    private void HandlePeerConnect(NetPeer peer)
    {
        Logger.LogDebug(this, $"{peer.Id} => {peer.EndPoint.Address}:{peer.EndPoint.Port} conected.");
        ClientConnected?.Invoke((short)peer.Id);
    }

    /// <summary>
    /// Handles the peer disconnect.
    /// </summary>
    /// <param name="peer">The peer.</param>
    /// <param name="disconnectInfo">The disconnect info.</param>
    private void HandlePeerDisconnect(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.LogDebug(this, $"{peer.Id} => {peer.EndPoint.Address}:{peer.EndPoint.Port} disconnect with reason {disconnectInfo.Reason}");
        ClientDisconnect?.Invoke((short)peer.Id, disconnectInfo.Reason);
    }

    /// <summary>
    /// Handles the receive.
    /// </summary>
    /// <param name="peer">The peer.</param>
    /// <param name="reader">The reader.</param>
    /// <param name="channelNumber">The channel number.</param>
    /// <param name="deliveryMethod">The delivery method.</param>
    private void HandleReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        this.NetPacketProcessor.ReadAllPackets(reader, peer);
    }

    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request.</param>
    private void HandleRequest(ConnectionRequest request)
    {
        ConnectionRequest?.Invoke(request);
    }
}