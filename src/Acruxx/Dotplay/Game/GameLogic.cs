using System.Diagnostics;
using Dotplay.Extensions;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// Basic game logic component.
/// </summary>
public partial class GameLogic : SubViewport, IGameLogic
{
    /// <summary>
    /// Secure passphrase for network connection.
    /// </summary>
    public string SecureConnectionKey = "ConnectionKey";

    //prevent to load async loader for multiply game logics
    internal static bool ResourceLoaderTick;

    protected INetworkWorld _currentWorld;

    private bool _canRunAysncLoader;
    private bool _mapLoadInProcess;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameLogic"/> class.
    /// </summary>
    public GameLogic()
    {
        Process.GetCurrentProcess().PriorityBoostEnabled = true;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;

        // Of course this only affects the main thread rather than child threads.
        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        this.OwnWorld3d = true;
        this.RenderTargetUpdateMode = UpdateMode.Always;
        this.RenderTargetClearMode = ClearMode.Always;
        this.ProcessMode = ProcessModeEnum.Always;
        this.Scaling3dMode = Scaling3DMode.Fsr;

        this.Components = new ComponentRegistry<GameLogic>(this);

        if (!ResourceLoaderTick)
        {
            ResourceLoaderTick = true;
            this._canRunAysncLoader = true;
        }
    }

    /// <summary>
    /// Gets the components.
    /// </summary>
    public ComponentRegistry<GameLogic> Components { get; }

    /// <summary>
    /// The active game world
    /// </summary>
    public INetworkWorld CurrentWorld => this._currentWorld;

    /// <inheritdoc />
    public TypeDictonary<IService> Services { get; } = new TypeDictonary<IService>();

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
    public override void _Process(double delta)
    {
        this.InternalProcess(delta);
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        this.InternalReady();
    }

    /// <summary>
    /// Called when an map unloaded
    /// </summary>
    public virtual void AfterMapDestroy()
    { }

    /// <summary>
    /// Call when an map loaded succesfully
    /// </summary>
    public virtual void AfterMapLoaded()
    { }

    /// <summary>
    /// Load an world by given resource path
    /// </summary>
    /// <param name="path"></param>
    /// <param name="scriptPath"></param>
    /// <param name="worldTick"></param>
    ///
    public void LoadWorld(string path, string scriptPath, uint worldTick = 0)
    {
        this.LoadWorldInternal(path, scriptPath, worldTick);
    }

    /// <summary>
    /// Destroys the map internal.
    /// </summary>
    internal virtual void DestroyMapInternal()
    { }

    /// <summary>
    /// Internals the process.
    /// </summary>
    /// <param name="delta">The delta.</param>
    internal virtual void InternalProcess(double delta)
    {
        foreach (var service in this.Services.All)
        {
            service.Update((float)delta);
        }

        // Prevent to call the async loader twice!
        if (this._canRunAysncLoader)
        {
            Utils.AsyncLoader.Loader.Tick();
        }
    }

    /// <summary>
    /// Internals the ready.
    /// </summary>
    internal virtual void InternalReady()
    {
    }

    /// <summary>
    /// On tree enter.
    /// </summary>
    internal virtual void InternalTreeEntered()
    {
        var parent = this.GetParentOrNull<Control>();
        if (parent != null)
        {
            parent.ProcessMode = ProcessModeEnum.Always;
            Logger.LogDebug(this, "Found parent set size: " + parent.Size);
            this.Size = new Vector2i((int)parent.Size.x, (int)parent.Size.y);
        }

        Logger.LogDebug(this, "Service amount: " + this.Services.All.Length);
        foreach (var service in this.Services.All)
        {
            service.Register();
        }
    }

    /// <summary>
    /// On internal tree exit.
    /// </summary>
    internal virtual void InternalTreeExit()
    {
        foreach (var service in this.Services.All)
        {
            service.Unregister();
        }
    }

    /// <summary>
    /// Loads the world internal.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="scriptPath">The script path.</param>
    /// <param name="worldTick">The world tick.</param>
    internal virtual void LoadWorldInternal(string path, string scriptPath, uint worldTick = 0)
    {
        if (this._mapLoadInProcess)
        {
            Logger.LogDebug(this, "There is currently an map in progress.");
            return;
        }

        GD.Load<CSharpScript>(scriptPath);

        this.DestroyMapInternal();
        this._mapLoadInProcess = true;

        Utils.AsyncLoader.Loader.LoadResource(path, (res) =>
        {
            this._mapLoadInProcess = false;
            this.OnMapInstanceInternal((PackedScene)res, scriptPath, worldTick);
        });
    }

    /// <summary>
    /// On map instance internal.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <param name="scriptPath">The script path.</param>
    /// <param name="worldTick">The world tick.</param>
    internal virtual void OnMapInstanceInternal(PackedScene level, string scriptPath, uint worldTick)
    { }
}