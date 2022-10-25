using System.Linq;
using Dotplay;
using Dotplay.Game;
using Godot;

namespace Acruxx;

/// <summary>
/// The single bootloader.
/// </summary>
public partial class SingleBootloader : Bootloader
{
    /// <summary>
    /// The current mode.
    /// </summary>
    public static BootloaderMode CurrentMode = BootloaderMode.Both;

    /// <summary>
    /// The bootloader mode.
    /// </summary>
    public enum BootloaderMode
    {
        Client = 0,
        Server = 1,
        Both = 2
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
        this.ProcessMode = ProcessModeEnum.Always;

        if (OS.GetCmdlineArgs().Contains("-client"))
        {
            CurrentMode = BootloaderMode.Client;
            this.GetTree().Root.Title = "Client Version";
            this.CreateClientWindow();
        }
        else if (OS.GetCmdlineArgs().Contains("-server"))
        {
            CurrentMode = BootloaderMode.Server;
            //  RenderingServer.RenderLoopEnabled = false;
            //DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled, 0);
            //Engine.TargetFps = 60;
            this.GetTree().Root.Title = "Server Version";

            this.CreateServerWindow();
        }
    }

    /// <summary>
    /// Creates the client window.
    /// </summary>
    /// <param name="name">The name.</param>
    private void CreateClientWindow(string name = "window")
    {
        Logger.LogDebug(this, "Load client..");

        var viewport = GD.Load<PackedScene>(this.ClientLogicScenePath).Instantiate<GameLogic>();
        viewport.Name = name;
        this.GetNode("SubViewportContainer").AddChild(viewport);
    }

    /// <summary>
    /// Creates the server window.
    /// </summary>
    /// <param name="name">The name.</param>
    private void CreateServerWindow(string name = "window")
    {
        Logger.LogDebug(this, "Load server..");

        var viewport = GD.Load<PackedScene>(this.ServerLogicScenePath).Instantiate<GameLogic>();
        viewport.Name = name;
        this.GetNode("SubViewportContainer").AddChild(viewport);
    }
}