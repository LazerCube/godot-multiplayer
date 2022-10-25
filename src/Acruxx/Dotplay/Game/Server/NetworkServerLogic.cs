using System.Collections.Generic;
using Dotplay.Network;
using Godot;
using RCON.Utils;

namespace Dotplay.Game.Server;

/// <summary>
/// The core server logic
/// </summary>
public partial class NetworkServerLogic : GameLogic
{
    /// <summary>
    /// If server can be visible (enable 3d)
    /// </summary>
    [Export]
    public bool CanBeVisible = false;

    /// <summary>
    /// The dictonary with all server settings (vars);
    /// </summary>
    //[Export]
    public Dictionary<string, string> DefaultVars = new()
    {
        // enable or disable interpolation
        { "sv_interpolate", "true" },

        // enable or disable showing raycasts
        { "sv_raycast", "true" },

        // force for soft or hard lag reduction
        { "sv_agressive_lag_reduction", "true" },
        // maximal input stages per ms
        { "sv_max_stages_ms", "500" },
        // force freeze client in case of lags
        { "sv_freze_client", "false" },

        // crouching
        { "sv_crouching", "true" },
        { "sv_crouching_down_speed", "9.0" },
        { "sv_crouching_up_speed", "4.0" },

        { "sv_crouching_accel", "8.0" },
        { "sv_crouching_deaccel", "4.0" },
        { "sv_crouching_friction", "3.0" },
        { "sv_crouching_speed", "4.0" },
        { "sv_max_speed", "6.0" },

        // walking
        { "sv_walk_accel", "8.0" },
        { "sv_walk_deaccel", "4.0" },

        { "sv_walk_speed", "7.0" },
        { "sv_walk_friction", "6.0" },

        // air control
        { "sv_air_control", "0.3" },
        { "sv_air_accel", "2.0" },
        { "sv_air_deaccel", "2.0" },
        { "sv_air_friction", "0.4" },

        // gravity and jump
        { "sv_gravity", "20.0" },

        { "sv_max_air_speed", "7.5" },
        { "sv_jumpspeed", "7" },

        // side movement
        { "sv_strafe_min_speed", "9" },
        { "sv_strafe_max_speed", "18" },

        { "sv_strafe_speed", "1.0" },
        { "sv_strafe_friction", "14.0" },
        { "sv_strafe_delay", "0.4" },
        { "sv_strafe_accel", "50.0" },
    };

    internal ServerNetworkService NetService;
    internal RconServerService RconService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkServerLogic"/> class.
    /// </summary>
    public NetworkServerLogic()
    {
        this.Disable3d = !this.CanBeVisible;
    }

    /// <summary>
    /// If clients can connect
    /// </summary>
    [Export]
    public bool AcceptClients { get; set; }

    /// <summary>
    /// maximal possible connections
    /// </summary>
    [Export(PropertyHint.Range, "0,32")]
    public int MaxConnections { get; set; } = 16;

    /// <summary>
    /// Network server port
    /// </summary>
    [Export]
    public int NetworkPort { get; set; } = 27015;

    /// <summary>
    ///  Contains the server vars
    /// </summary>
    public VarsCollection Variables { get; private set; } = new VarsCollection(new Vars());

    /// <summary>
    /// Create an server world
    /// </summary>
    public virtual NetworkServerWorld CreateWorld()
    {
        return new NetworkServerWorld();
    }

    /// <summary>
    /// Initialized the rcon server, eg attaching commands
    /// </summary>
    /// <param name="manager"></param>
    public virtual void InitRconServer(CommandManager manager)
    {
        manager.Add("set", "Set an server variable", (_, arguments) =>
        {
            if (arguments.Count != 2)
            {
                return "Required arguments: key, variable";
            }

            if (this._currentWorld != null)
            {
                var key = this._currentWorld.ServerVars.Vars.AllVariables.ContainsKey(arguments[0]);
                if (!key)
                {
                    return "Varaible " + arguments[0] + " not exist.";
                }
                else
                {
                    this._currentWorld.ServerVars.Set(arguments[0], arguments[1]);
                    return "Successfull changed.";
                }
            }
            else
            {
                return "No active game world";
            }
        });

        manager.Add("get", "Get an server variable", (_, arguments) =>
        {
            if (arguments.Count != 1)
            {
                return "Required arguments: key";
            }

            if (this._currentWorld != null)
            {
                var key = this._currentWorld.ServerVars.Vars.AllVariables.ContainsKey(arguments[0]);
                if (!key)
                {
                    return "Varaible " + arguments[0] + " not exist.";
                }
                else
                {
                    return this._currentWorld.ServerVars.Vars.AllVariables[arguments[0]];
                }
            }
            else
            {
                return "No active game world";
            }
        });
    }

    /// <summary>
    /// Called when client connected
    /// </summary>
    /// <param name="request">boolean for rejected or accepted</param>
    public virtual bool OnPreConnect(LiteNetLib.ConnectionRequest request)
    {
        return true;
    }

    /// <summary>
    /// Called when after server started succeffull
    /// </summary>
    public virtual void OnServerStarted()
    { }

    /// <inheritdoc />
    internal override void DestroyMapInternal()
    {
        this._currentWorld?.Destroy();
        this._currentWorld = null;

        //reject clients
        this.AcceptClients = false;

        this.AfterMapDestroy();
    }

    /// <inheritdoc />
    internal override void InternalTreeEntered()
    {
        this.RenderTargetUpdateMode = UpdateMode.Always;
        this.PhysicsObjectPicking = true;
        this.ProcessMode = ProcessModeEnum.Always;
        //this.Disable3d = true;
        this.HandleInputLocally = false;

        this.NetService = this.Services.Create<ServerNetworkService>();

        this.NetService.ConnectionEstablished += () =>
        {
            Logger.LogDebug(this, "Server was started.");
            this.OnServerStarted();
        };

        this.NetService.ConnectionRequest += (request) =>
        {
            if (this._currentWorld == null)
            {
                request.Reject();
            }
            else
            {
                if (this.NetService.GetConnectionCount() < this.MaxConnections && this.AcceptClients && this.OnPreConnect(request))
                {
                    Logger.LogDebug(this, request.RemoteEndPoint.Address + ":" + request.RemoteEndPoint.Port + " established");
                    request.AcceptIfKey(this.SecureConnectionKey);
                }
                else
                {
                    Logger.LogDebug(this, request.RemoteEndPoint.Address + ":" + request.RemoteEndPoint.Port + " rejected");
                    request.Reject();
                }
            }
        };

        this.RconService = this.Services.Create<RconServerService>();
        this.RconService.ServerStarted += this.InitRconServer;

        base.InternalTreeEntered();
        this.NetService.Bind(this.NetworkPort);
    }

    /// <inheritdoc />
    internal override void LoadWorldInternal(string mapName, string scriptPath, uint worldTick)
    {
        this.AcceptClients = false;

        // disconnect all clients
        foreach (var connectedClient in this.NetService.GetConnectedPeers())
        {
            connectedClient.Disconnect();
        }

        base.LoadWorldInternal(mapName, scriptPath, worldTick);
    }

    /// <inheritdoc />
    internal override void OnMapInstanceInternal(PackedScene res, string scriptPath, uint worldTick)
    {
        NetworkServerWorld newWorld = this.CreateWorld();
        newWorld.Name = "world";
        this.AddChild(newWorld);
        newWorld.ResourceWorldPath = res.ResourcePath;
        newWorld.ResourceWorldScriptPath = scriptPath;
        newWorld.InstanceLevel(res);

        this._currentWorld = newWorld;

        this.Variables = new VarsCollection(new Vars(this.DefaultVars));
        this.Variables.LoadConfig("server.cfg");

        newWorld.Init(this.Variables, 0);

        // accept clients
        this.AcceptClients = true;
        this.AfterMapLoaded();
    }
}