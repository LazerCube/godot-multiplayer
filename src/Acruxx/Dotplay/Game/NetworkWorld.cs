using System.Collections.Generic;
using System.Linq;
using Dotplay.Network;
using Dotplay.Physics;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// The core world class for server and client worlds
/// </summary>
public abstract partial class NetworkWorld : Node3D, INetworkWorld
{
    /// <summary>
    /// The maximum possible ticks stored and managed by the client and server
    /// </summary>
    public const int MaxTicks = 1024;

    internal double Accumulator;
    internal bool IsInit;
    internal Node PlayerHolder = new();

    private readonly InterpolationController _interpController = new();

    /// <inheritdoc />
    public GameLogic GameInstance { get; private set; }

    /// <inheritdoc />
    public INetworkLevel NetworkLevel { get; internal set; }

    /// <inheritdoc />
    public Dictionary<short, NetworkCharacter> Players { get; internal set; } = new();

    /// <inheritdoc />
    public string ResourceWorldPath { get; internal set; }

    /// <inheritdoc />
    public string ResourceWorldScriptPath { get; internal set; }

    /// <inheritdoc />
    public VarsCollection ServerVars { get; internal set; } = new();

    /// <summary>
    /// Adjust the tick rate of the server
    /// </summary>
    public ISimulationAdjuster SimulationAdjuster { get; set; } = new ServerSimulationAdjuster();

    /// <inheritdoc />
    public uint WorldTick { get; protected set; } = 0;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        this.InternalTreeEntered();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        this.InternalTreeExit();
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        this.InternalPhysicsProcess((float)delta);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        this.InternalProcess(delta);
    }

    /// <inheritdoc />
    public virtual void Destroy()
    {
        this.QueueFree();
    }

    /// <inheritdoc />
    public virtual void OnLevelAddToScene()
    { }

    /// <inheritdoc />
    public virtual void OnPlayerInitilaized(INetworkCharacter p)
    { }

    /// <inheritdoc />
    public virtual void Tick(float interval)
    { }

    /// <summary>
    /// Applies the glow.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplyGlow(bool isEnabled)
    {
        var env = this.NetworkLevel?.Environment;
        if (env != null)
        {
            env.GlowEnabled = isEnabled;
        }
    }

    /// <summary>
    /// Applies SDFGI.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplySDFGI(bool isEnabled)
    {
        var env = this.NetworkLevel?.Environment;
        if (env != null)
        {
            env.SdfgiEnabled = isEnabled;
        }
    }

    /// <summary>
    /// Applies the SSAO.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplySSAO(bool isEnabled)
    {
        var env = this.NetworkLevel?.Environment;
        if (env != null)
        {
            env.SsaoEnabled = isEnabled;
        }
    }

    /// <summary>
    /// Applies the SSIL.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplySSIL(bool isEnabled)
    {
        var env = this.NetworkLevel?.Environment;
        if (env != null)
        {
            env.SsilEnabled = isEnabled;
        }
    }

    /// <summary>
    /// Inits the.
    /// </summary>
    /// <param name="serverVars">The server vars.</param>
    /// <param name="initalWorldTick">The inital world tick.</param>
    internal virtual void Init(VarsCollection serverVars, uint initalWorldTick)
    {
        this.IsInit = true;
        this.ServerVars = serverVars;
        foreach (var sv in serverVars.Vars.AllVariables)
        {
            Logger.LogDebug(this, "[Config] " + sv.Key + " => " + sv.Value);
        }
    }

    /// <summary>
    /// Instances the level.
    /// </summary>
    /// <param name="res">The res.</param>
    internal void InstanceLevel(PackedScene res)
    {
        //res.ResourceLocalToScene = true;
        var node = res.Instantiate<NetworkLevel>();
        node.Name = "level";
        this.AddChild(node);

        this.NetworkLevel = node;

        this.OnLevelInternalAddToScene();
    }

    /// <summary>
    /// Internals the physics process.
    /// </summary>
    /// <param name="delta">The delta.</param>
    internal virtual void InternalPhysicsProcess(float delta)
    {
        if (this.IsInit)
        {
            //physics tick replacement
            var tickInterval = delta;
            this.Accumulator += delta;
            var adjustedTickInterval = tickInterval * this.SimulationAdjuster.AdjustedInterval;
            while (this.Accumulator >= adjustedTickInterval)
            {
                this.Accumulator -= adjustedTickInterval;
                this._interpController.ExplicitFixedUpdate(adjustedTickInterval);

                this.InternalTick(tickInterval);
            }
        }
    }

    /// <summary>
    /// Internals the process.
    /// </summary>
    /// <param name="delta">The delta.</param>
    internal virtual void InternalProcess(double delta)
    {
        this._interpController.ExplicitUpdate((float)delta);
    }

    /// <summary>
    /// Internals the tick.
    /// </summary>
    /// <param name="interval">The interval.</param>
    internal virtual void InternalTick(float interval)
    { }

    /// <summary>
    /// Internals the tree entered.
    /// </summary>
    internal virtual void InternalTreeEntered()
    {
        this.PlayerHolder.Name = "playerHolder";
        this.GameInstance = this.GetParent<GameLogic>();

        this.AddChild(this.PlayerHolder);
    }

    /// <summary>
    /// Internals the tree exit.
    /// </summary>
    internal virtual void InternalTreeExit()
    { }

    /// <summary>
    /// Ons the level internal add to scene.
    /// </summary>
    internal virtual void OnLevelInternalAddToScene()
    {
        this.OnLevelAddToScene();
    }

    /// <summary>
    /// Runs after the main update.
    /// </summary>
    internal virtual void PostUpdate()
    { }

    /// <summary>
    /// Simulates the world.
    /// </summary>
    /// <param name="dt">The dt.</param>
    internal void SimulateWorld(float dt)
    {
        foreach (var player in this.Players.
            Where(df => df.Value.State == PlayerConnectionState.Initialized).
            Select(df => df.Value).ToArray())
        {
            player.InternalTick(dt);
        }
    }
}