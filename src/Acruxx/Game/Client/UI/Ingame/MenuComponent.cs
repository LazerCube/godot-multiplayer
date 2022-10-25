using System;
using Dotplay;
using Dotplay.Game;
using Godot;

namespace Acruxx.Client.UI.Ingame;

/// <summary>
/// The menu component.
/// </summary>
public partial class MenuComponent : CanvasLayer, IChildComponent<GameLogic>
{
    /// <inheritdoc />
    public GameLogic BaseComponent { get; set; }

    /// <summary>
    /// Gets or sets the close path.
    /// </summary>
    [Export]
    public NodePath? ClosePath { get; set; }

    /// <summary>
    /// Gets or sets the disconnect path.
    /// </summary>
    [Export]
    public NodePath? DisconnectPath { get; set; }

    /// <summary>
    /// Gets or sets the settings path.
    /// </summary>
    [Export]
    public NodePath? SettingsPath { get; set; }

    /// <inheritdoc />
    public override void _EnterTree()
    {
        this.GetNode<Button>(this.DisconnectPath).Pressed += () =>
        {
            this.GetDefaultClientLogic().Disconnect();
        };

        this.GetNode<Button>(this.SettingsPath).Pressed += () =>
        {
            var component = this.GetDefaultClientLogic();
            this.BaseComponent.Components.DeleteComponent<MenuComponent>();
            component.Components.AddComponent<GameSettings>("res://src/Acruxx/Game/Client/UI/Ingame/GameSettings.tscn");
        };

        this.GetNode<Button>(this.ClosePath).Pressed += () =>
        {
            var component = this.GetDefaultClientLogic();
            this.BaseComponent.Components.DeleteComponent<MenuComponent>();
            Input.MouseMode = Input.MouseModeEnum.Captured;
        };
    }

    /// <summary>
    /// Gets the default client logic.
    /// </summary>
    /// <returns>A DefaultClientLogic.</returns>
    private DefaultClientLogic GetDefaultClientLogic()
    {
        DefaultClientLogic? component = (DefaultClientLogic)this.BaseComponent;
        return component ?? throw new Exception("Cannot cast component to 'DefaultClientLogic'");
    }
}