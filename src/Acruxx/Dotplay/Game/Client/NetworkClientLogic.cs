using System;
using System.Collections.Generic;
using System.Linq;
using Dotplay.Network;
using Dotplay.Network.Commands;
using Godot;
using LiteNetLib;

namespace Dotplay.Game.Client;

/// <summary>
/// Is the base class for any client (SubViewport)
/// </summary>
public partial class NetworkClientLogic : GameLogic
{
    /// <summary>
    /// The dictonary with all default settings (vars);
    /// </summary>
    ///[Export]
    public Dictionary<string, string> DefaultVars = new()
    {
       { "cl_sensitivity_x", "2.0"},
       { "cl_sensitivity_y", "2.0"},
       { "cl_debug_server", "true"},

       { "cl_fov", "70"},

       { "cl_resolution", "640x480"},
       { "cl_draw_shadow", "SoftLow"},

       { "cl_window_mode", nameof(ClientSettings.WindowModes.Windowed)},
       { "cl_draw_msaa", nameof(MSAA.Msaa2x)},
       { "cl_draw_aa", nameof(ScreenSpaceAA.Disabled)},
       { "cl_draw_debug",  nameof(DebugDrawEnum.Disabled)},

       { "cl_draw_glow", "false"},
       { "cl_draw_sdfgi", "false"},
       { "cl_draw_ssao", "false"},
       { "cl_draw_ssil", "false"},
       { "cl_draw_occulision", "false"},
       { "cl_draw_debanding", "false"},
       { "cl_draw_vsync", "false"},

       // movement (requires key_ at beginning)
       {"key_forward", "KEY_W"},
       {"key_backward", "KEY_S"},
       {"key_right", "KEY_D"},
       {"key_left", "KEY_A"},
       {"key_jump", "KEY_Space"},
       {"key_crouch", "KEY_Ctrl"},
       {"key_shift", "KEY_Shift"},
       {"key_attack", "BTN_Left"},
    };

    private string _loadedWorldName = string.Empty;

    private ClientNetworkService? _netService;

    /// <summary>
    /// Connect with an server
    /// </summary>
    /// <param name="hostname"></param>
    /// <param name="port"></param>
    public void Connect(string hostname, int port)
    {
        if (this.CurrentWorld == null)
        {
            this._netService.Connect(new ClientConnectionSettings
            {
                Port = port,
                Hostname = hostname,
                SecureKey = this.SecureConnectionKey
            });
        }
    }

    /// <summary>
    /// Create an client world
    /// </summary>
    /// <returns></returns>
    public virtual NetworkClientWorld CreateWorld()
    {
        return new NetworkClientWorld();
    }

    /// <summary>
    /// Disconnect the client
    /// </summary>
    public void Disconnect()
    {
        this._netService.Disconnect();
        this.DestroyMapInternal();
    }

    /// <summary>
    /// On local client is connected
    /// </summary>
    public virtual void OnConnected()
    {
    }

    /// <summary>
    /// On local client are disconneted
    /// </summary>
    public virtual void OnDisconnect()
    {
    }

    /// <summary>
    /// Applies antialiasing.
    /// </summary>
    /// <param name="debug">The debug.</param>
    internal void ApplyAA(string debug)
    {
        if (Enum.TryParse(debug, true, out ScreenSpaceAA result))
        {
            this.ScreenSpaceAa = result;
        }
    }

    /// <summary>
    /// Applies debanding.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplyDebanding(bool isEnabled)
    {
        this.UseDebanding = isEnabled;
    }

    /// <summary>
    /// Applies debug draw.
    /// </summary>
    /// <param name="debug">The debug.</param>
    internal void ApplyDebug(string debug)
    {
        if (Enum.TryParse(debug, true, out DebugDrawEnum result))
        {
            this.DebugDraw = result;
        }
    }

    /// <summary>
    /// Applies the MSAA.
    /// </summary>
    /// <param name="debug">The debug.</param>
    internal void ApplyMSAA(string debug)
    {
        if (Enum.TryParse(debug, true, out MSAA result))
        {
            this.Msaa3d = result;
        }
    }

    /// <summary>
    /// Applies the occlusion.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplyOcclusion(bool isEnabled)
    {
        this.UseOcclusionCulling = isEnabled;
    }

    /// <summary>
    /// Applies the resolution.
    /// </summary>
    /// <param name="resolution">The resolution.</param>
    internal void ApplyResolution(string resolution)
    {
        if (ClientSettings.Resolutions.Contains(resolution))
        {
            var values = resolution.Split("x");
            var res = new Vector2i(int.Parse(values[0]), int.Parse(values[1]));

            DisplayServer.WindowSetSize(res);
            this.GetTree().Root.ContentScaleSize = res;
        }
    }

    /// <summary>
    /// Applies the shadow.
    /// </summary>
    /// <param name="shadowLevel">The shadow level.</param>
    internal void ApplyShadow(string shadowLevel)
    {
        if (Enum.TryParse(shadowLevel, true, out RenderingServer.ShadowQuality result))
        {
            RenderingServer.DirectionalSoftShadowFilterSetQuality(result);
            RenderingServer.PositionalSoftShadowFilterSetQuality(result);
        }
    }

    /// <summary>
    /// Applies the vsync.
    /// </summary>
    /// <param name="isEnabled">If true, is enabled.</param>
    internal void ApplyVsync(bool isEnabled)
    {
        if (isEnabled)
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled, 0);
        }
        else
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled, 0);
        }
    }

    /// <summary>
    /// Applies the window mode.
    /// </summary>
    /// <param name="windowMode">The window mode.</param>
    internal void ApplyWindowMode(string windowMode)
    {
        if (Enum.TryParse(windowMode, true, out ClientSettings.WindowModes mode))
        {
            if (mode == ClientSettings.WindowModes.Borderless)
            {
                this.GetTree().Root.Mode = Window.ModeEnum.Windowed;
                this.GetTree().Root.Borderless = true;
            }
            else if (mode == ClientSettings.WindowModes.Windowed)
            {
                this.GetTree().Root.Mode = Window.ModeEnum.Windowed;
                this.GetTree().Root.Borderless = false;
            }
            else if (mode == ClientSettings.WindowModes.Fullscreen)
            {
                this.GetTree().Root.Mode = Window.ModeEnum.Fullscreen;
                this.GetTree().Root.Borderless = false;
            }
            else if (mode == ClientSettings.WindowModes.ExclusiveFullscreen)
            {
                this.GetTree().Root.Mode = Window.ModeEnum.ExclusiveFullscreen;
                this.GetTree().Root.Borderless = false;
            }
        }
    }

    /// <inheritdoc />
    internal override void DestroyMapInternal()
    {
        Logger.LogDebug(this, "Destroy map");
        if (this._currentWorld != null)
        {
            this.RemoveChild(this._currentWorld as Node);
            this._currentWorld?.Destroy();
        }
        this._currentWorld = null;
        this._loadedWorldName = null;

        this.AfterMapDestroy();
    }

    /// <inheritdoc />
    internal override void InternalReady()
    {
        this.ApplyWindowMode(ClientSettings.Variables.GetValue("cl_window_mode", "Windowed"));
        this.ApplyResolution(ClientSettings.Variables.GetValue("cl_resolution", "640x480"));
        this.ApplyDebug(ClientSettings.Variables.GetValue("cl_draw_debug", "Disabled"));
        this.ApplyAA(ClientSettings.Variables.GetValue("cl_draw_aa", "Disabled"));
        this.ApplyMSAA(ClientSettings.Variables.GetValue("cl_draw_msaa", "Msaa2x"));
        this.ApplyShadow(ClientSettings.Variables.GetValue("cl_draw_shadow", "SoftLow"));
        this.ApplyOcclusion(ClientSettings.Variables.Get("cl_draw_occulision", false));
        this.ApplyDebanding(ClientSettings.Variables.Get("cl_draw_debanding", false));
        this.ApplyVsync(ClientSettings.Variables.Get("cl_draw_vsync", false));
    }

    /// <inheritdoc />
    internal override void InternalTreeEntered()
    {
        ClientSettings.Variables = new VarsCollection(new Vars(this.DefaultVars));
        ClientSettings.Variables.LoadConfig("client.cfg");

        ClientSettings.Variables.OnChange += (_, name, value) =>
        {
            if (name == "cl_resolution")
            {
                this.ApplyResolution(value);
            }
            if (name == "cl_window_mode")
            {
                this.ApplyWindowMode(value);
            }
            if (name == "cl_draw_debug")
            {
                this.ApplyDebug(value);
            }

            if (name == "cl_draw_aa")
            {
                this.ApplyAA(value);
            }

            if (name == "cl_draw_msaa")
            {
                this.ApplyMSAA(value);
            }

            if (name == "cl_draw_shadow")
            {
                this.ApplyShadow(value);
            }

            if (name == "cl_draw_occulision")
            {
                this.ApplyOcclusion(ClientSettings.Variables.Get<bool>("cl_draw_occulision"));
            }

            if (name == "cl_draw_debanding")
            {
                this.ApplyDebanding(ClientSettings.Variables.Get<bool>("cl_draw_debanding"));
            }

            if (name == "cl_draw_vsync")
            {
                this.ApplyVsync(ClientSettings.Variables.Get<bool>("cl_draw_vsync"));
            }
        };

        this.AudioListenerEnable3d = true;

        this._netService = this.Services.Create<ClientNetworkService>();
        this._netService.OnDisconnect += this.OnInternalDisconnect;
        this._netService.Connected += this.OnConnected;

        this._netService.SubscribeSerialisable<ClientWorldLoader>((package, _) =>
        {
            this.OnConnected();

            if (this._loadedWorldName != package.WorldName)
            {
                this._loadedWorldName = package.WorldName;
                this.LoadWorldInternal(package.WorldName, package.ScriptPath, package.WorldTick);
            }
        });

        base.InternalTreeEntered();
    }

    /// <inheritdoc />
    internal override void OnMapInstanceInternal(PackedScene res, string scriptPath, uint worldTick)
    {
        NetworkClientWorld newWorld = this.CreateWorld();
        newWorld.Name = "world";
        this.AddChild(newWorld);

        newWorld.InstanceLevel(res);

        this._currentWorld = newWorld;

        //send server map loading was completed
        this._netService.SendMessageSerialisable(0, new ServerInitializer());
        this.AfterMapLoaded();
    }

    /// <summary>
    /// Ons the internal disconnect.
    /// </summary>
    /// <param name="reason">The reason.</param>
    /// <param name="fullDisconnect">If true, full disconnect.</param>
    private void OnInternalDisconnect(DisconnectReason reason, bool fullDisconnect)
    {
        if (fullDisconnect)
        {
            Logger.LogDebug(this, "Full disconnected");
            this.OnDisconnect();
            this.DestroyMapInternal();
        }
    }
}