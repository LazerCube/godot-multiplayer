using Acruxx.Client.UI.Ingame;
using Acruxx.Client.UI.Welcome;
using Dotplay.Game.Client;
using Godot;

namespace Acruxx.Client;

/// <summary>
/// The default client logic.
/// </summary>
public partial class DefaultClientLogic : NetworkClientLogic
{
    /// <summary>
    /// Gets or sets a value indicating whether show menu.
    /// </summary>
    public bool ShowMenu { get; set; }

    /// <inheritdoc />
    public override void _EnterTree()
    {
        this.Components.AddComponent<PreConnectComponent>("res://src/Acruxx/Game/Client/UI/Welcome/PreConnectComponent.tscn");

        Input.MouseMode = Input.MouseModeEnum.Visible;

        base._EnterTree();
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event.IsActionReleased("abort") && this._currentWorld != null)
        {
            if (this.ShowMenu)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                this.Components.DeleteComponent<MenuComponent>();
                this.ShowMenu = false;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                this.Components.AddComponent<MenuComponent>("res://src/Acruxx/Game/Client/UI/Ingame/MenuComponent.tscn");
                this.ShowMenu = true;
            }
        }

        @event.Dispose();
    }

    /// <inheritdoc />
    public override void AfterMapDestroy()
    {
        this.Components.DeleteComponent<MenuComponent>();
        this.Components.DeleteComponent<MapLoadingComponent>();
    }

    /// <inheritdoc />
    public override void AfterMapLoaded()
    {
        this.Components.DeleteComponent<MapLoadingComponent>();
        this.Components.AddComponent<HudComponent>("res://src/Acruxx/Game/Client/UI/Ingame/HudComponent.tscn");

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <inheritdoc />
    public override void OnConnected()
    {
        this.Components.DeleteComponent<PreConnectComponent>();
        this.Components.AddComponent<DebugMenuComponent>("res://src/Acruxx/Game/Client/UI/Welcome/DebugMenuComponent.tscn");
        this.Components.AddComponent<MapLoadingComponent>("res://src/Acruxx/Game/Client/UI/Welcome/MapLoadingComponent.tscn");
    }

    /// <inheritdoc />
    public override void OnDisconnect()
    {
        this.Components.DeleteComponent<PreConnectComponent>();
        this.Components.DeleteComponent<DebugMenuComponent>();
        this.Components.AddComponent<PreConnectComponent>("res://src/Acruxx/Game/Client/UI/Welcome/PreConnectComponent.tscn");
    }
}