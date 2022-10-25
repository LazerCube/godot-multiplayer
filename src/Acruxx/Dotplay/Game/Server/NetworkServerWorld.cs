using System;
using System.Collections.Generic;
using System.Linq;
using Dotplay.Input;
using Dotplay.Network;
using Dotplay.Network.Commands;
using Dotplay.Physics;
using Dotplay.Utils;
using Framework.Network.Commands;
using Godot;
using LiteNetLib;

namespace Dotplay.Game.Server;

/// <summary>
/// Base class for an server world
/// </summary>
public partial class NetworkServerWorld : NetworkWorld
{
    /// <summary>
    /// Time after player totaly deleted (0 means directly)
    /// </summary>
    [Export]
    public float DeleteTimeForPlayer = 10;

    private readonly PlayerInputProcessor _playerInputProcessor = new();
    private readonly HashSet<short> _unprocessedPlayerIds = new();

    private IGameRule _activeGameRule;
    private bool _heartTest = true;
    private int _missedInputs;
    private FixedTimer _worldStateBroadcastTimer;

    /// <summary>
    /// Set or get the active game rule
    /// </summary>
    public IGameRule ActiveGameRule
    {
        get
        {
            return this._activeGameRule;
        }
        private set
        {
            this.ActivateGameRule(value);
        }
    }

    /// <summary>
    /// The network service for the server world
    /// </summary>
    public ServerNetworkService NetService { get; set; }

    /// <summary>
    /// Add an player to the server world
    /// </summary>
    /// <param name="clientId">The id of the new client</param>
    /// <param name="resourcePath">The resource path (.res) to the scene package</param>
    /// <param name="scriptPaths">The script paths used by the scene</param>
    public void AddPlayer(short clientId, string resourcePath, string[] scriptPaths)
    {
        if (scriptPaths != null)
        {
            foreach (var path in scriptPaths)
            {
                GD.Load<CSharpScript>(path);
            }
        }

        AsyncLoader.Loader.LoadResource(resourcePath, (res) =>
        {
            if (!this.Players.ContainsKey(clientId))
            {
                var resource = (res as PackedScene);
                // resource.ResourceLocalToScene = true;
                NetworkCharacter serverPlayer = resource.Instantiate<NetworkCharacter>();
                serverPlayer.Mode = NetworkMode.SERVER;
                serverPlayer.Name = clientId.ToString();
                serverPlayer.ResourcePath = resourcePath;
                serverPlayer.ScriptPaths = scriptPaths;
                serverPlayer.NetworkId = clientId;
                serverPlayer.GameWorld = this;
                serverPlayer.State = PlayerConnectionState.Connected;

                this.PlayerHolder.AddChild(serverPlayer);
                this.Players.Add(clientId, serverPlayer);

                this.ActiveGameRule?.OnNewPlayerJoined(serverPlayer);
                this.SendPlayerUpdate();
            }
            else
            {
                var serverPlayer = this.Players[clientId];
                serverPlayer.State = serverPlayer.PreviousState;

                this.ActiveGameRule?.OnPlayerRejoined((INetworkCharacter)serverPlayer);
                this.SendPlayerUpdate();
            }

            var message = new ClientWorldLoader
            {
                WorldName = this.ResourceWorldPath,
                ScriptPath = this.ResourceWorldScriptPath,
                WorldTick = this.WorldTick
            };

            this.NetService.SendMessageSerialisable(clientId, message);
        });
    }

    /// <summary>
    /// Send an heartbeat to all players
    /// Hearbeat contains player informations, server latency, states, etc
    /// </summary>
    public void BroadcastWorldHearbeat(float dt)
    {
        // get player states
        var states = new List<PlayerState>();
        foreach (var client in this.Players.Where(df => df.Value.IsServer() && df.Value.State == PlayerConnectionState.Initialized).Select(df => df.Value).ToArray())
        {
            states.Add(client.ToNetworkState());
        }

        // send to each player data package
        foreach (var player in this.Players.Where(df => df.Value.IsServer()).Select(df => df.Value).Where(df => df.State == PlayerConnectionState.Initialized).ToArray())
        {
            var cmd = new WorldHeartbeat
            {
                WorldTick = WorldTick,
                YourLatestInputTick = player.LatestInputTick,
                PlayerStates = states.ToArray(),
            };

            this.NetService.SendMessage(player.NetworkId, cmd, DeliveryMethod.Sequenced);
        }
    }

    /// <summary>
    /// Delete an player from the server world
    /// </summary>
    /// <param name="clientId">player id</param>
    /// <param name="withDelay">use an delay for deletion</param>
    public void DeletePlayer(short clientId, bool withDelay = true)
    {
        if (this.Players.ContainsKey(clientId))
        {
            var serverPlayer = this.Players[clientId];

            serverPlayer.PreviousState = serverPlayer.State;
            serverPlayer.State = PlayerConnectionState.Disconnected;

            if (withDelay)
            {
                serverPlayer.DisconnectTime = this.DeleteTimeForPlayer;
                this.ActiveGameRule?.OnPlayerLeaveTemporary(serverPlayer);
            }
            else
            {
                serverPlayer.QueueFree();
                this.Players.Remove(clientId);
                this.ActiveGameRule?.OnPlayerLeave(serverPlayer);
                this.SendPlayerDelete(clientId);
            }
        }
    }

    /// <summary>
    /// Event called after client is connected to server
    /// </summary>
    /// <param name="clientId"></param>
    public virtual void OnPlayerConnected(short clientId)
    { }

    /// <summary>
    /// Event called after client is disconnected from server
    /// </summary>
    /// <param name="clientId">he client id of the network player</param>
    /// <param name="reason">The reason why the player are disconnted</param>
    public virtual void OnPlayerDisconnect(short clientId, DisconnectReason reason)
    {
        Logger.LogDebug(this, "[" + clientId + "] Disconnected");
        this.DeletePlayer(clientId, true);
    }

    /// <summary>
    /// Create and init a new game rule
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    public void SetGameRule<T>(string name) where T : GameRule
    {
        T gameRule = Activator.CreateInstance(typeof(T)) as T;

        gameRule.GameWorld = this;
        gameRule.RuleName = name;

        this.ActiveGameRule = gameRule;
    }

    /// <inheritdoc />
    internal override void Init(VarsCollection serverVars, uint initalWorldTick)
    {
        base.Init(serverVars, initalWorldTick);

        this.ServerVars.OnChange += (_, _, _) =>
        {
            foreach (var player in this.Players.Where(df => df.Value.State == PlayerConnectionState.Initialized))
            {
                this.NetService.SendMessage(player.Key, new ServerVarUpdate { ServerVars = this.ServerVars.Vars });
            }
        };
    }

    /// <inheritdoc />
    internal override void InternalProcess(double delta)
    {
        base.InternalProcess(delta);

        //check if players are realy disconnected and delete them totaly
        foreach (var player in this.Players.Where(df => df.Value.State == PlayerConnectionState.Disconnected).ToArray())
        {
            var serverPlayer = player.Value;
            if (serverPlayer.DisconnectTime <= 0)
            {
                this.DeletePlayer(player.Key, false);
            }
            else
            {
                serverPlayer.DisconnectTime -= (float)delta;
            }
        }
    }

    /// <inheritdoc />
    internal override void InternalTick(float interval)
    {
        // Apply inputs to each player.
        this._unprocessedPlayerIds.Clear();
        this._unprocessedPlayerIds.UnionWith(this.Players.Where(df => df.Value.State ==
        PlayerConnectionState.Initialized && df.Value.IsServer()).Select(df => df.Key).ToArray());

        foreach (var tickInput in this._playerInputProcessor.DequeueInputsForTick(this.WorldTick))
        {
            if (this.Players.ContainsKey(tickInput.PlayerId))
            {
                var serverPlayer = this.Players[tickInput.PlayerId];
                if (serverPlayer.State != PlayerConnectionState.Initialized)
                {
                    return;
                }

                //decompose but with what?
                var input = serverPlayer.Components.Get<NetworkInput>();
                if (input != null)
                {
                    input.SetPlayerInputs(tickInput.Inputs);
                }

                serverPlayer.CurrentPlayerInput = tickInput;
                this._unprocessedPlayerIds.Remove(tickInput.PlayerId);

                // Mark the player as synchronized.
                serverPlayer.IsSynchronized = true;
            }
        }

        // Any remaining players without inputs have their latest input command repeated,
        // but we notify them that they need to fast-forward their simulation to improve buffering.
        foreach (var playerId in this._unprocessedPlayerIds)
        {
            // If the player is not yet synchronized, this isn't an error.
            if (!this.Players.ContainsKey(playerId) ||
                !this.Players[playerId].IsSynchronized)
            {
                continue;
            }

            var serverPlayer = this.Players[playerId];
            if (serverPlayer != null)
            {
                ++this._missedInputs;
                Logger.SetDebugUI("sv missed inputs", this._missedInputs.ToString());

                if (this._playerInputProcessor.TryGetLatestInput(playerId, out TickInput latestInput))
                {
                    var input = serverPlayer.Components.Get<NetworkInput>();
                    input?.SetPlayerInputs(latestInput.Inputs);
                }
                else
                {
                    Logger.LogDebug(this, $"No inputs for player #{playerId} and no history to replay.");
                }
            }
        }

        // Advance the world simulation.
        this.SimulateWorld(interval);

        //increase the world tick
        ++this.WorldTick;

        // Snapshot everything.
        var bufidx = this.WorldTick % MaxTicks;

        foreach (var player in this.Players.Where(df => df.Value.State == PlayerConnectionState.Initialized && df.Value.IsServer())
        .Select(df => df.Value).ToArray())
        {
            player.States[bufidx] = player.ToNetworkState();
        }

        // Update post-tick timers.
        this._worldStateBroadcastTimer.Update(interval);

        this.Tick(interval);
        this._activeGameRule?.Tick(interval);
    }

    /// <inheritdoc />
    internal override void InternalTreeEntered()
    {
        base.InternalTreeEntered();

        this.NetService = this.GameInstance.Services.Get<ServerNetworkService>();
        this.NetService.ClientConnected += this.OnPlayerConnectedInternal;
        this.NetService.ClientDisconnect += this.OnPlayerDisconnect;

        this.NetService.SubscribeSerialisable<ServerInitializer>(this.InitializeClient);
        this.NetService.SubscribeSerialisable<PlayerInput>(this.OnPlayerInput);

        this.NetService.ClientLatencyUpdate += (clientId, latency) =>
        {
            if (this.Players.ContainsKey(clientId))
            {
                this.Players[clientId].Latency = latency;
            }
        };

        float serverSendRate = 1 / (float)this.GetPhysicsProcessDeltaTime();

        Logger.LogDebug(this, "Set server send rate to " + serverSendRate);

        this._worldStateBroadcastTimer = new FixedTimer(serverSendRate, this.BroadcastWorldHearbeat);
        this._worldStateBroadcastTimer.Start();
    }

    /// <inheritdoc />
    internal override void OnLevelInternalAddToScene()
    {
        this.ApplyGlow(false);
        this.ApplySDFGI(false);
        this.ApplySSAO(false);
        this.ApplySSIL(false);

        base.OnLevelInternalAddToScene();
    }

    /// <summary>
    /// Ons the player connected internal.
    /// </summary>
    /// <param name="clientId">The client id.</param>
    internal void OnPlayerConnectedInternal(short clientId)
    {
        Logger.LogDebug(this, "[" + clientId + "] Connected");
        this.OnPlayerConnected(clientId);
    }

    /// <inheritdoc />
    internal override void PostUpdate()
    {
        if (this.Players.Count > 0)
        {
            this._playerInputProcessor.LogQueueStatsForPlayer(this.Players.First().Key, this.WorldTick);
        }
    }

    /// <summary>
    /// Processes the player attack.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="range">The range.</param>
    internal void ProcessPlayerAttack(NetworkCharacter player, float range = 1000)
    {
        if (player.State != PlayerConnectionState.Initialized)
        {
            return;
        }

        Logger.LogDebug(this, "Get player attack for player " + player.NetworkId);

        // First, rollback the state of all attackable entities (for now just players).
        // The world is not rolled back to the tick the players input was for, but rather
        // the tick of the server world state the player was seeing at the time of attack.
        // TODO: Clean up the whole player delegate path, it sucks.
        var remoteViewTick = player.CurrentPlayerInput.RemoteViewTick;

        // If client interp is enabled, we estimate by subtracting another tick, but I'm not sure
        // if this is correct or not, needs more work.
        if (this.ServerVars.Get("sv_interpolate", true))
        {
            remoteViewTick--;
        }

        uint bufidx = remoteViewTick % MaxTicks;
        var head = new Dictionary<int, PlayerState>();
        foreach (var entry in this.Players)
        {
            var otherPlayer = entry.Value;
            head[entry.Key] = otherPlayer.ToNetworkState();
            var historicalState = otherPlayer.States[bufidx];
            otherPlayer.ApplyNetworkState(historicalState);
        }

        // var currentState = player.ToNetworkState();

        // Now check for collisions.
        var playerObjectHit = player.DetechtHit(range);
        if (playerObjectHit != null)
        {
            if (this.ServerVars.Get("sv_raycast", true))
            {
                var raycastHit = new RaycastTest { From = playerObjectHit.From, To = playerObjectHit.To };
                Logger.LogDebug(this, "Found raycast at " + raycastHit.From + " => " + raycastHit.To);
                this.NetService.SentMessageToAllSerialized(raycastHit);
            }

            if (playerObjectHit.PlayerDestination != null &&
                playerObjectHit.PlayerDestination is NetworkCharacter character)
            {
                Logger.LogDebug(this, $"Player ${player.NetworkId} for remote view tick ${remoteViewTick} was hit Player ${playerObjectHit.PlayerDestination.NetworkId}");
                character.OnHit(playerObjectHit);
            }
            else if (playerObjectHit.Collider != null)
            {
                Logger.LogDebug(this, $"Player ${player.NetworkId} for remote view tick ${remoteViewTick} was hit ${playerObjectHit.Collider.Name}");
            }

            this.ActiveGameRule?.OnHit(playerObjectHit);
        }

        // Finally, revert all the players to their head state.
        foreach (var entry in this.Players)
        {
            var otherPlayer = entry.Value;
            otherPlayer.ApplyNetworkState(head[entry.Key]);
        }
    }

    /// <summary>
    /// Activates the game rule.
    /// </summary>
    /// <param name="rule">The rule.</param>
    private void ActivateGameRule(IGameRule rule)
    {
        Logger.LogDebug(this, "Activate game rule: " + rule.GetType().Name);
        this._activeGameRule = rule;
        this._activeGameRule.OnGameRuleActivated();

        foreach (var player in this.Players.Select(df => df.Value))
        {
            //clear previous components
            player.Components.Clear();

            player.RequiredPuppetComponents = Array.Empty<short>();
            player.RequiredComponents = Array.Empty<short>();

            if (this._activeGameRule != null)
            {
                rule.OnNewPlayerJoined(player);
            }
        }
    }

    /// <summary>
    /// Initializes the client.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="peer">The peer.</param>
    private void InitializeClient(ServerInitializer package, NetPeer peer)
    {
        var clientId = (short)peer.Id;

        if (this.Players.ContainsKey(clientId))
        {
            var player = this.Players[clientId];
            Logger.LogDebug(this, $"[{clientId}] Initialize player with previous state: {player.State}");

            var oldState = player.State;
            if (oldState != PlayerConnectionState.Initialized)
            {
                player.State = PlayerConnectionState.Initialized;
                this.OnPlayerInitilaized(player);
            }

            this.NetService.SendMessage(clientId, new ClientWorldInitializer
            {
                PlayerId = clientId,
                ServerVars = this.ServerVars.Vars,
                GameTick = this.WorldTick,
                InitState = player.ToNetworkState()
            });

            this.SendPlayerUpdate();
        }
    }

    /// <summary>
    /// Enqueue new player input
    /// </summary>
    /// <param name="package"></param>
    /// <param name="peer"></param>
    private void OnPlayerInput(PlayerInput package, NetPeer peer)
    {
        var clientId = (short)peer.Id;
        if (this.Players.ContainsKey(clientId))
        {
            var player = this.Players[clientId];
            var input = player.Components.Get<NetworkInput>();
            if (input != null)
            {
                package.Inputs = package.Inputs.Select(df => df.DeserliazeWithInputKeys(input.AvaiableInputs)).ToArray();
                var lastAckedInputTick = player.LatestInputTick;
                this._playerInputProcessor.EnqueueInput(package, clientId, lastAckedInputTick);
                player.LatestInputTick = this._playerInputProcessor.GetLatestPlayerInputTick(player.NetworkId);
            }
        }
    }

    /// <summary>
    /// Sends the player delete.
    /// </summary>
    /// <param name="playerId">The player id.</param>
    private void SendPlayerDelete(short playerId)
    {
        var delete = new PlayerLeave
        {
            NetworkId = playerId
        };

        foreach (var player in this.Players.Where(df => df.Value.IsServer()).Select(df => df.Value).Where(df => df.State == PlayerConnectionState.Initialized).ToArray())
        {
            this.NetService.SendMessage((int)player.NetworkId, delete, DeliveryMethod.ReliableOrdered);
        }
    }

    /// <summary>
    /// Sends the player update.
    /// </summary>
    private void SendPlayerUpdate()
    {
        var heartbeatUpdateList = this.Players
            .Where(df => df.Value.IsServer())
            .Select(df => new PlayerInfo
            {
                NetworkId = df.Key,
                State = df.Value.State,
                ResourcePath = df.Value.ResourcePath,
                ScriptPaths = df.Value.ScriptPaths,
                RequiredComponents = df.Value.RequiredComponents.ToArray(),
                RequiredPuppetComponents = df.Value.RequiredPuppetComponents.ToArray(),
            }).ToArray();

        var update = new PlayerCollection
        {
            Updates = heartbeatUpdateList.ToArray(),
            WorldTick = this.WorldTick
        };

        foreach (var player in this.Players.Where(df => df.Value.IsServer()).Select(df => df.Value).Where(df => df.State == PlayerConnectionState.Initialized).ToArray())
        {
            this.NetService.SendMessage(player.NetworkId, update, DeliveryMethod.ReliableOrdered);
        }
    }
}