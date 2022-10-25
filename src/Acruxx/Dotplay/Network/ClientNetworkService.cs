using Godot;
using LiteNetLib;

namespace Dotplay.Network;

/// <summary>
/// Settings for the client connection
/// </summary>
public struct ClientConnectionSettings
{
    /// <summary>
    /// Server hostname
    /// </summary>
    public string Hostname { get; set; }

    /// <summary>
    /// Server port
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// The server connect password
    /// </summary>
    public string SecureKey { get; set; }
}

/// <summary>
/// The client network service
/// </summary>
public partial class ClientNetworkService : NetworkService
{
    /// <summary>
    /// Delay between reconnect
    /// </summary>
    [Export]
    public int ConnectionRetryDelay = 2;

    /// <summary>
    /// Maximum of retries
    /// </summary>
    [Export]
    public int MaxRetriesPerConnection = 10;

    private bool _canRetry;
    private NetPeer _currentPeer;
    private int _currentRetries;
    private ClientConnectionSettings _lastConnectionSettings;
    private EventBasedNetListener _listener;
    private double _nextStaticsUpdate;
    private double _retryTime;

    /// <summary>
    /// Connection handler for connect event
    /// </summary>
    public delegate void ConnectedHandler();

    /// <summary>
    /// Disconnect handler for disconnect event
    /// </summary>
    /// <param name="reason"></param>
    /// <param name="fullDisconnect"></param>
    public delegate void DisconnectHandler(DisconnectReason reason, bool fullDisconnect);

    /// <summary>
    /// Triggered when the client is connected.
    /// </summary>
    public event ConnectedHandler Connected;

    /// <summary>
    /// Triggered when the client is disconnected
    /// </summary>
    public event DisconnectHandler OnDisconnect;

    /// <summary>
    /// Received bytes since last 1 second
    /// </summary>
    public long BytesReceived { get; private set; }

    /// <summary>
    /// Sended bytes since last 1 second
    /// </summary>
    public long BytesSended { get; private set; }

    /// <summary>
    /// Lossing packages since last 1 second
    /// </summary>
    public long PackageLoss { get; private set; }

    /// <summary>
    /// Package loose in percent (avg) since last 1 second
    /// </summary>
    public long PackageLossPercent { get; private set; }

    /// <summary>
    /// Get the current latency between server and client
    /// </summary>
    public int Ping { get; private set; }

    /// <summary>
    /// The network server peer
    /// </summary>
    public NetPeer ServerPeer { get; private set; }

    /// <summary>
    /// Gets the statistics.
    /// </summary>
    private NetStatistics Statistics => this.NetManager.Statistics;

    /// <summary>
    /// Connect to an server
    /// </summary>
    /// <param name="settings">The connection settings eg. hostname, port, secure</param>
    public void Connect(ClientConnectionSettings settings)
    {
        var task = new System.Threading.Tasks.Task(() =>
        {
            this._canRetry = false;
            this._lastConnectionSettings = settings;
            Logger.LogDebug(this, "Try to start to tcp://" + settings.Hostname + ":" + settings.Port);
            this._currentPeer = this.NetManager.Connect(settings.Hostname, settings.Port, settings.SecureKey);
            if (this._currentPeer != null)
            {
                Logger.LogDebug(this, "Receive server handshake");
                Connected?.Invoke();
            }
        });

        task.Start();
    }

    /// <summary>
    /// Disconnect from server
    /// </summary>
    public void Disconnect()
    {
        this._currentRetries = 0;
        this._retryTime = 0;
        this._canRetry = false;
        this._currentPeer?.Disconnect();
    }

    /// <inheritdoc />
    public override void Register()
    {
        base.Register();
        this.Disconnect();

        this._listener = new EventBasedNetListener();

        this.NetManager = new NetManager(this._listener)
        {
            DebugName = "ClientNetworkLayer",
            AutoRecycle = true,
            EnableStatistics = true,
            UnconnectedMessagesEnabled = true
        };

        this._listener.NetworkReceiveEvent += this.HandleReceive;
        this._listener.PeerDisconnectedEvent += this.HandlePeerDisconnect;
        this._listener.PeerConnectedEvent += this.HandlePeerConnect;
        this._listener.NetworkLatencyUpdateEvent += this.HandleLatency;

        this.NetManager.Start();
    }

    /// <inheritdoc />
    public override void Unregister()
    {
        this._listener.NetworkReceiveEvent -= this.HandleReceive;
        this._listener.PeerDisconnectedEvent -= this.HandlePeerDisconnect;
        this._listener.PeerConnectedEvent -= this.HandlePeerConnect;
        this._listener.NetworkLatencyUpdateEvent -= this.HandleLatency;

        base.Unregister();
    }

    /// <inheritdoc />
    public override void Update(float delta)
    {
        if (this._canRetry && !this._lastConnectionSettings.Equals(default(ClientConnectionSettings)))
        {
            if (this._retryTime <= 0)
            {
                this.Connect(this._lastConnectionSettings);
                this._retryTime += this.ConnectionRetryDelay;
            }
            else if (this._retryTime >= 0)
            {
                this._retryTime -= delta;
            }
        }

        base.Update(delta);

        if (this._nextStaticsUpdate >= 1f)
        {
            this._nextStaticsUpdate = 0f;
            this.BytesSended = this.Statistics.BytesSent;
            this.BytesReceived = this.Statistics.BytesReceived;
            this.PackageLoss = this.Statistics.PacketLoss;
            this.PackageLossPercent = this.Statistics.PacketLossPercent;

            this.Statistics.Reset();
        }
        else
        {
            this._nextStaticsUpdate += delta;
        }
    }

    /// <summary>
    /// Handles the latency.
    /// </summary>
    /// <param name="peer">The peer.</param>
    /// <param name="latency">The latency.</param>
    private void HandleLatency(NetPeer peer, int latency)
    {
        if (peer == this.ServerPeer)
        {
            this.Ping = latency;
        }

        this.PeerLatency[peer] = latency;
    }

    /// <summary>
    /// Handles the peer connect.
    /// </summary>
    /// <param name="peer">The peer.</param>
    private void HandlePeerConnect(NetPeer peer)
    {
        this.ServerPeer = peer;
    }

    /// <summary>
    /// Handles the peer disconnect.
    /// </summary>
    /// <param name="peer">The peer.</param>
    /// <param name="disconnectInfo">The disconnect info.</param>
    private void HandlePeerDisconnect(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Logger.LogDebug(this, "Disconnected with reason " + disconnectInfo.Reason);

        this._currentRetries++;
        if (this._currentRetries <= this.MaxRetriesPerConnection && disconnectInfo.Reason == DisconnectReason.RemoteConnectionClose)
        {
            this._canRetry = true;
            this.OnDisconnect?.Invoke(disconnectInfo.Reason, false);
        }
        else
        {
            this._canRetry = false;
            this.OnDisconnect?.Invoke(disconnectInfo.Reason, true);
        }
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
        // after last receive increase max retries
        this._currentRetries = this.MaxRetriesPerConnection;
        this.NetPacketProcessor.ReadAllPackets(reader, peer);
    }
}