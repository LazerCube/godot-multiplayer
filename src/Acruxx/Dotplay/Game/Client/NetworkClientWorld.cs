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

namespace Dotplay.Game.Client;

/// <summary>
/// The misc extensions.
/// </summary>
public static class MiscExtensions
{
    /// <summary>
    /// Takes the last. Ex: collection.TakeLast(5);
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="n">The n.</param>
    /// <returns>A list of TS.</returns>
    public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int n)
    {
        return source.Skip(Math.Max(0, source.Count() - n));
    }
}

/// <summary>
/// Base class for the client world
/// </summary>
public partial class NetworkClientWorld : NetworkWorld
{
    internal ClientNetworkService? NetService;
    internal LineDrawer3d RayCastTester = new();
    internal int ReplayedStates;
    private readonly MovingAverage _excessWorldStateAvg = new(10);
    private readonly List<short> _playerCreationInProcess = new();
    private readonly Queue<WorldHeartbeat> _worldStateQueue = new();

    /// <summary>
    /// Client adjuster
    /// Handles server ticks and make them accuracy
    /// </summary>
    public ClientSimulationAdjuster? ClientSimulationAdjuster { get; set; }

    /// <summary>
    /// Gets the last acked input tick.
    /// </summary>
    public uint LastAckedInputTick { get; private set; } = 0;

    /// <summary>
    /// Gets the last server world tick.
    /// </summary>
    public uint LastServerWorldTick { get; private set; } = 0;

    /// <summary>
    /// The local character of the client world
    /// </summary>
    public NetworkCharacter? LocalPlayer { get; set; }

    /// <summary>
    /// The local player input snapshots
    /// </summary>
    public GeneralPlayerInput[] LocalPlayerInputsSnapshots { get; set; } = new GeneralPlayerInput[MaxTicks];

    /// <summary>
    /// The local player states
    /// </summary>
    public PlayerState[] LocalPlayerStateSnapshots { get; set; } = new PlayerState[MaxTicks];

    /// <summary>
    /// The last world player ticks related to the state snapshots
    /// </summary>
    public uint[] LocalPlayerWorldTickSnapshots { get; set; } = new uint[MaxTicks];

    /// <summary>
    /// The current client player id  the server
    /// </summary>
    public short MyServerId { get; internal set; } = -1;

    /// <summary>
    /// Handles the config updates.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="name">The name.</param>
    /// <param name="value">The value.</param>
    internal void HandleConfigUpdates(VarsCollection.KeyChangeEnum state, string name, string value)
    {
        if (name == "cl_draw_glow")
        {
            this.ApplyGlow(ClientSettings.Variables.Get<bool>("cl_draw_glow"));
        }
        if (name == "cl_draw_sdfgi")
        {
            this.ApplySDFGI(ClientSettings.Variables.Get<bool>("cl_draw_sdfgi"));
        }
        if (name == "cl_draw_ssao")
        {
            this.ApplySSAO(ClientSettings.Variables.Get<bool>("cl_draw_ssao"));
        }
        if (name == "cl_draw_ssil")
        {
            this.ApplySSIL(ClientSettings.Variables.Get<bool>("cl_draw_ssil"));
        }
    }

    /// <inheritdoc />
    internal override void Init(VarsCollection serverVars, uint initalWorldTick)
    {
        this.LocalPlayerInputsSnapshots = new GeneralPlayerInput[MaxTicks];
        this.LocalPlayerStateSnapshots = new PlayerState[MaxTicks];
        this.LocalPlayerWorldTickSnapshots = new uint[MaxTicks];
        this.LastServerWorldTick = initalWorldTick;
        this.LastAckedInputTick = initalWorldTick;

        base.Init(serverVars, 0);

        var simTickRate = 1f / (float)this.GetPhysicsProcessDeltaTime();

        this.SimulationAdjuster = this.ClientSimulationAdjuster = new ClientSimulationAdjuster(simTickRate / 2);
        this.WorldTick = this.ClientSimulationAdjuster.GuessClientTick((float)this.GetPhysicsProcessDeltaTime(), initalWorldTick, this.NetService.Ping);
    }

    /// <inheritdoc />
    internal override void InternalTick(float interval)
    {
        if (this.LocalPlayer == null || this.LocalPlayer.State != PlayerConnectionState.Initialized)
            return;

        this.ProcessLocalInput();

        //simulate
        this.SimulateWorld(interval);

        //increase worldTick
        ++this.WorldTick;

        //procese player inputs
        this.ProcessServerWorldState();

        //forward tick
        this.Tick(interval);
    }

    /// <inheritdoc />
    internal override void InternalTreeEntered()
    {
        base.InternalTreeEntered();

        this.NetService = this.GameInstance.Services.Get<ClientNetworkService>();
        this.NetService.Subscribe<WorldHeartbeat>(this.HandleWorldState);
        this.NetService.Subscribe<PlayerCollection>(this.OnPlayerUpdate);

        this.NetService.Subscribe<PlayerLeave>(this.OnPlayerDelete);
        this.NetService.Subscribe<ClientWorldInitializer>(this.InitWorld);
        this.NetService.Subscribe<ServerVarUpdate>(this.UpdateWorld);
        this.NetService.SubscribeSerialisable<RaycastTest>(this.RayCastTest);

        this.AddChild(this.RayCastTester);
    }

    /// <summary>
    /// Internals the tree exit.
    /// </summary>
    internal override void InternalTreeExit()
    {
        base.InternalTreeExit();
        ClientSettings.Variables.OnChange -= this.HandleConfigUpdates;
    }

    /// <inheritdoc />
    internal override void OnLevelInternalAddToScene()
    {
        this.ApplyGlow(ClientSettings.Variables.Get("cl_draw_glow", false));
        this.ApplySDFGI(ClientSettings.Variables.Get("cl_draw_sdfgi", false));
        this.ApplySSAO(ClientSettings.Variables.Get("cl_draw_ssao", false));
        this.ApplySSIL(ClientSettings.Variables.Get("cl_draw_ssil", false));

        ClientSettings.Variables.OnChange += this.HandleConfigUpdates;

        base.OnLevelInternalAddToScene();
    }

    /// <inheritdoc />
    internal override void PostUpdate()
    {
        // Process the remaining world states if there are any, though we expect this to be empty?
        // TODO: This is going to need to be structured pretty differently with other players.
        this._excessWorldStateAvg.Push(this._worldStateQueue.Count);
        //while (worldStateQueue.Count > 0) {
        //  ProcessServerWorldState();
        //}
        // Show some debug monitoring values.
        // Logger.SetDebugUI("cl_rewinds", replayedStates.ToString());
        //    Logger.SetDebugUI("incoming_state_excess", excessWorldStateAvg.Average().ToString());

        this.ClientSimulationAdjuster.Monitoring();
    }

    /// <summary>
    /// Processes the local input.
    /// </summary>
    internal void ProcessLocalInput()
    {
        float simTickRate = 1f / (float)this.GetPhysicsProcessDeltaTime();
        var serverSendRate = simTickRate / 2;

        // The maximum age of the last server state in milliseconds the client will continue simulating.
        var maxStaleServerStateTicks = (int)MathF.Ceiling(this.ServerVars.Get("sv_max_stages_ms", 500) / serverSendRate);

        var inputs = new GeneralPlayerInput();

        var inputComponent = this.LocalPlayer.Components.Get<NetworkInput>();
        var lastTicks = this.WorldTick - this.LastServerWorldTick;
        if (this.ServerVars.Get("sv_freze_client", false) && lastTicks >= maxStaleServerStateTicks)
        {
            Logger.LogDebug(this, "Server state is too old (is the network connection dead?) - max ticks " + maxStaleServerStateTicks + " - currentTicks => " + lastTicks);
            inputs = new GeneralPlayerInput();
        }
        else if (inputComponent != null)
        {
            if (!this.GameInstance.GuiDisableInput)
            {
                inputs = inputComponent.GetInput();
            }
            else
            {
                inputs.ViewDirection = inputComponent.LastInput.ViewDirection;
                inputs.Apply(inputComponent.AvaiableInputs, new Dictionary<string, bool>());
            }
        }

        // Update our snapshot buffers.
        uint bufidx = this.WorldTick % MaxTicks;
        this.LocalPlayerInputsSnapshots[bufidx] = inputs;
        this.LocalPlayerStateSnapshots[bufidx] = this.LocalPlayer.ToNetworkState();
        this.LocalPlayerWorldTickSnapshots[bufidx] = this.LastServerWorldTick;

        // Send a command for all inputs not yet acknowledged from the server.
        var unackedInputs = new List<GeneralPlayerInput>();
        var clientWorldTickDeltas = new List<short>();

        // TODO: lastServerWorldTick is technically not the same as lastAckedInputTick, fix this.
        for (uint tick = this.LastServerWorldTick; tick <= this.WorldTick; ++tick)
        {
            unackedInputs.Add(this.LocalPlayerInputsSnapshots[tick % MaxTicks]);
            clientWorldTickDeltas.Add((short)(tick - this.LocalPlayerWorldTickSnapshots[tick % MaxTicks]));
        }

        var command = new PlayerInput
        {
            StartWorldTick = this.LastServerWorldTick,
            Inputs = unackedInputs.ToArray(),
            ClientWorldTickDeltas = clientWorldTickDeltas.ToArray(),
        };

        // send to server => command
        this.NetService?.SendMessageSerialisable(this.NetService.ServerPeer.Id, command, DeliveryMethod.Sequenced);

        // SetPlayerInputs
        inputComponent?.SetPlayerInputs(inputs);
    }

    /// <summary>
    /// Rays the cast test.
    /// </summary>
    /// <param name="cmd">The cmd.</param>
    /// <param name="peed">The peed.</param>
    internal void RayCastTest(RaycastTest cmd, NetPeer peed)
    {
        this.RayCastTester.AddLine(cmd.From, cmd.To);
    }

    /// <summary>
    /// Handles the world state.
    /// </summary>
    /// <param name="cmd">The cmd.</param>
    /// <param name="peed">The peed.</param>
    private void HandleWorldState(WorldHeartbeat cmd, NetPeer peed)
    {
        if (this.LocalPlayer?.State == PlayerConnectionState.Initialized)
        {
            this._worldStateQueue.Enqueue(cmd);
        }
    }

    /// <summary>
    /// Inits the world.
    /// </summary>
    /// <param name="cmd">The cmd.</param>
    /// <param name="peer">The peer.</param>
    private void InitWorld(ClientWorldInitializer cmd, NetPeer peer)
    {
        Logger.LogDebug(this, "Init world with server user id " + cmd.PlayerId);

        this.MyServerId = cmd.PlayerId;
        this?.Init(new VarsCollection(cmd.ServerVars), cmd.GameTick);
    }

    /// <summary>
    /// Ons the player delete.
    /// </summary>
    /// <param name="playerDelete">The player delete.</param>
    /// <param name="peer">The peer.</param>
    private void OnPlayerDelete(PlayerLeave playerDelete, NetPeer peer)
    {
        if (!this.IsInsideTree())
            return;

        if (this.Players.ContainsKey(playerDelete.NetworkId))
        {
            var networkPlayer = this.Players[playerDelete.NetworkId];
            networkPlayer.QueueFree();
            this.Players.Remove(playerDelete.NetworkId);

            if (networkPlayer.NetworkId == this.MyServerId)
            {
                Logger.LogDebug(this, "Local player are realy disconnected!");
                (this.GameInstance as NetworkClientLogic)?.Disconnect();
                return;
            }
        }
    }

    /// <summary>
    /// Ons the player update.
    /// </summary>
    /// <param name="playerUpdateList">The player update list.</param>
    /// <param name="peer">The peer.</param>
    private void OnPlayerUpdate(PlayerCollection playerUpdateList, NetPeer peer)
    {
        if (playerUpdateList.Updates == null)
            return;

        foreach (var playerUpdate in playerUpdateList.Updates)
        {
            if (!this.Players.ContainsKey(playerUpdate.NetworkId))
            {
                if (this._playerCreationInProcess.Contains(playerUpdate.NetworkId))
                {
                    return;
                }

                if (playerUpdate.ScriptPaths != null)
                {
                    foreach (var path in playerUpdate.ScriptPaths)
                    {
                        GD.Load<CSharpScript>(path);
                    }
                }

                this._playerCreationInProcess.Add(playerUpdate.NetworkId);
                AsyncLoader.Loader.LoadResource(playerUpdate.ResourcePath, (res) =>
                {
                    var resource = (res as PackedScene);
                    // resource.ResourceLocalToScene = true;
                    NetworkCharacter player = resource.Instantiate<NetworkCharacter>();

                    if (playerUpdate.NetworkId == this.MyServerId)
                    {
                        Logger.LogDebug(this, "Attach new local player with id " + playerUpdate.NetworkId);
                        player.Mode = NetworkMode.CLIENT;
                        player.NetworkId = playerUpdate.NetworkId;
                        player.GameWorld = this;
                        player.Name = playerUpdate.NetworkId.ToString();
                        this.PlayerHolder.AddChild(player);
                        this.Players.Add(playerUpdate.NetworkId, player);
                        this.LocalPlayer = player;
                    }
                    else
                    {
                        Logger.LogDebug(this, "Attach new puppet player with id " + playerUpdate.NetworkId);
                        player.Mode = NetworkMode.PUPPET;
                        player.NetworkId = playerUpdate.NetworkId;
                        player.GameWorld = this;
                        player.Name = playerUpdate.NetworkId.ToString();
                        this.PlayerHolder.AddChild(player);
                        this.Players.Add(playerUpdate.NetworkId, player);
                    }

                    this.OnPlayerInitilaized(player);

                    this.UpdatePlayerValues(playerUpdate, player);
                    this._playerCreationInProcess.Remove(playerUpdate.NetworkId);
                });
            }
            else
            {
                var player = this.Players[playerUpdate.NetworkId];
                this.UpdatePlayerValues(playerUpdate, player);
            }
        }
    }

    /// <inheritdoc />
    private void ProcessServerWorldState()
    {
        //run world state queue
        if (this._worldStateQueue.Count < 1)
        {
            return;
        }

        var incomingState = this._worldStateQueue.Dequeue();

        //set the last server world tick
        this.LastServerWorldTick = incomingState.WorldTick;

        // Calculate our actual tick lead on the server perspective. We add one because the world
        // state the server sends to use is always 1 higher than the latest input that has been
        // processed.
        if (incomingState.YourLatestInputTick > 0)
        {
            this.LastAckedInputTick = incomingState.YourLatestInputTick;

            int actualTickLead = (int)this.LastAckedInputTick - (int)this.LastServerWorldTick + 1;
            this.ClientSimulationAdjuster.NotifyActualTickLead(actualTickLead, false, this.ServerVars.Get("sv_agressive_lag_reduction", true));
        }

        // For debugging purposes, log the local lead we're running at
        var localWorldTickLead = this.WorldTick - this.LastServerWorldTick;
        Logger.SetDebugUI("cl_local_tick", localWorldTickLead.ToString());

        PlayerState incomingLocalPlayerState = new();
        if (incomingState.PlayerStates != null)
        {
            foreach (var playerState in incomingState.PlayerStates)
            {
                if (playerState.NetworkId == this.LocalPlayer!.NetworkId)
                {
                    incomingLocalPlayerState = playerState;
                    this.LocalPlayer.IncomingLocalPlayerState = incomingLocalPlayerState;
                }
                else
                {
                    if (this.Players.ContainsKey(playerState.NetworkId))
                    {
                        var puppet = this.Players[playerState.NetworkId];
                        puppet.IncomingLocalPlayerState = playerState;

                        puppet.ApplyNetworkState(playerState);
                    }
                }
            }
        }

        if (default(PlayerState).Equals(incomingLocalPlayerState))
        {
            // This is unexpected.
            Logger.LogDebug(this, "No local player state found!");
        }

        if (incomingState.WorldTick >= this.WorldTick)
        {
            // We're running behind the server at this point, which can happen
            // if the application is suspended for some reason, so just snap our
            // state.
            // TODO: Look into interpolation here as well.
            Logger.LogDebug(this, "Got a future world state, snapping to latest state.");
            // TODO: We need to add local estimated latency here like we do during init.
            this.WorldTick = incomingState.WorldTick;
            this.LocalPlayer.ApplyNetworkState(incomingLocalPlayerState);
            return;
        }

        //test
        uint bufidx = incomingState.WorldTick % 1024;
        var stateSnapshot = this.LocalPlayerStateSnapshots[bufidx];

        var incomingPosition = incomingLocalPlayerState.GetVar(this.LocalPlayer, "NetworkPosition", Vector3.Zero);
        var snapshotPosition = stateSnapshot.GetVar(this.LocalPlayer, "NetworkPosition", Vector3.Zero);

        var error = incomingPosition - snapshotPosition;

        if (error.LengthSquared() > 0.0001f)
        {
            Logger.LogDebug(this, $"Rewind tick#{incomingState.WorldTick}, Error: {error.Length()}, Range: {this.WorldTick - incomingState.WorldTick}");
            this.ReplayedStates++;

            // TODO: If the error was too high, snap rather than interpolate.

            // Rewind local player state to the correct state from the server.
            // TODO: Cleanup a lot of this when its merged with how rockets are spawned.
            this.LocalPlayer.ApplyNetworkState(incomingLocalPlayerState);

            // Loop through and replay all captured input snapshots up to the current tick.
            uint replayTick = incomingState.WorldTick;
            while (replayTick < this.WorldTick)
            {
                // Grab the historical input.
                bufidx = replayTick % 1024;
                var inputSnapshot = this.LocalPlayerInputsSnapshots[bufidx];

                // Rewrite the historical sate snapshot.
                this.LocalPlayerStateSnapshots[bufidx] = this.LocalPlayer.ToNetworkState();

                // Apply inputs to the associated player controller and simulate the world.
                var input = this.LocalPlayer.Components.Get<NetworkInput>();
                input?.SetPlayerInputs(inputSnapshot);

                this.SimulateWorld((float)this.GetPhysicsProcessDeltaTime());
                ++replayTick;
            }
        }
    }

    /// <summary>
    /// updates the player values.
    /// </summary>
    /// <param name="playerUpdate">The player update.</param>
    /// <param name="player">The player.</param>
    private void UpdatePlayerValues(PlayerInfo playerUpdate, NetworkCharacter player)
    {
        player.NetworkId = playerUpdate.NetworkId;
        player.PlayerName = playerUpdate.PlayerName;
        player.State = playerUpdate.State;

        if (playerUpdate.State == PlayerConnectionState.Initialized && player.IsInsideTree() && playerUpdate.RequiredComponents != null)
        {
            foreach (var avaiableComponent in player.Components.All.Where(df => df is IPlayerComponent).Select(df => df as IPlayerComponent))
            {
                var channel = avaiableComponent.NetworkId;
                var enable = playerUpdate.RequiredComponents.Contains(channel);
                if (enable != avaiableComponent.IsEnabled)
                {
                    player.ActivateComponent(avaiableComponent.NetworkId, enable);
                }
            }
        }
    }

    /// <summary>
    /// Updates the world.
    /// </summary>
    /// <param name="cmd">The cmd.</param>
    /// <param name="peer">The peer.</param>
    private void UpdateWorld(ServerVarUpdate cmd, NetPeer peer)
    {
        this.ServerVars = new VarsCollection(cmd.ServerVars);
    }
}