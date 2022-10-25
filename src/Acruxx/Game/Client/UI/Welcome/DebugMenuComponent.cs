using System;
using Dotplay;
using Dotplay.Game;
using Dotplay.Network;
using Godot;

namespace Acruxx.Client.UI.Welcome;

/// <summary>
/// The debug menu component.
/// </summary>
public partial class DebugMenuComponent : CanvasLayer, IChildComponent<GameLogic>
{
    private Label? _fps;
    private Label? _idleTime;
    private ClientNetworkService? _netService;
    private Label? _packageData;
    private Label? _packageLosse;
    private Label? _physicsTime;
    private Label? _ping;
    private Timer? _timer;

    /// <inheritdoc/>
    public GameLogic BaseComponent { get; set; }

    /// <summary>
    /// Gets or sets the fps path.
    /// </summary>
    [Export]
    public NodePath? FPSPath { get; set; }

    /// <summary>
    /// Gets or sets the idle time path.
    /// </summary>
    [Export]
    public NodePath? IdleTimePath { get; set; }

    /// <summary>
    /// Gets or sets the log message path.
    /// </summary>
    [Export]
    public NodePath? LogMessagePath { get; set; }

    /// <summary>
    /// Gets or sets the package data path.
    /// </summary>
    [Export]
    public NodePath? PackageDataPath { get; set; }

    /// <summary>
    /// Gets or sets the package loose path.
    /// </summary>
    [Export]
    public NodePath? PackageLoosePath { get; set; }

    /// <summary>
    /// Gets or sets the physics time path.
    /// </summary>
    [Export]
    public NodePath? PhysicsTimePath { get; set; }

    /// <summary>
    /// Gets or sets the ping path.
    /// </summary>
    [Export]
    public NodePath? PingPath { get; set; }

    /// <summary>
    /// Gets or sets the timer path.
    /// </summary>
    [Export]
    public NodePath? TimerPath { get; set; }

    /// <inheritdoc/>
    public override void _EnterTree()
    {
        Logger.OnLogMessage += this.LogToScreen;

        var componnent = this.BaseComponent as IGameLogic;
        this._netService = componnent.Services.Get<ClientNetworkService>();

        this._packageData = this.GetNode<Label>(this.PackageDataPath);
        this._packageLosse = this.GetNode<Label>(this.PackageLoosePath);
        this._ping = this.GetNode<Label>(this.PingPath);
        this._fps = this.GetNode<Label>(this.FPSPath);
        this._idleTime = this.GetNode<Label>(this.IdleTimePath);
        this._physicsTime = this.GetNode<Label>(this.PhysicsTimePath);

        this._timer = this.GetNode<Timer>(this.TimerPath);
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        Logger.OnLogMessage -= this.LogToScreen;
        this._timer!.Timeout -= this.ProcessStats;
    }

    /// <inheritdoc/>
    public override void _Ready()
    {
        base._Ready();
        this._timer!.Timeout += this.ProcessStats;
        this._timer.Autostart = true;
        this._timer.Start();
    }

    /// <summary>
    /// Processes the stats.
    /// </summary>
    public void ProcessStats()
    {
        if (this._netService != null)
        {
            this._packageData!.Text = $"Send: {this._netService.BytesSended / 1000}kB/s, Rec: {this._netService.BytesReceived / 1000}kB/s";
            this._packageLosse!.Text = $"{this._netService.PackageLoss} ({this._netService.PackageLossPercent}%)";
            this._ping!.Text = $"{this._netService.Ping} ms";
        }

        this._fps!.Text = Engine.GetFramesPerSecond().ToString();
        this._idleTime!.Text = $"{Math.Round(this.GetProcessDeltaTime() * 1000, 6)} ms";
        this._physicsTime!.Text = $"{Math.Round(this.GetPhysicsProcessDeltaTime() * 1000, 6)} ms";
    }

    /// <summary>
    /// Logs the to screen.
    /// </summary>
    /// <param name="message">The message.</param>
    private void LogToScreen(string message)
    {
        this.GetNode<Label>(this.LogMessagePath).Text = message;
    }
}