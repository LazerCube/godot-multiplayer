using Dotplay;
using Dotplay.Game;
using Godot;

namespace Acruxx.Client.UI.Ingame;

/// <summary>
/// The hud component.
/// </summary>
public partial class HudComponent : CanvasLayer, IChildComponent<GameLogic>
{
    /// <inheritdoc/>
    public GameLogic BaseComponent { get; set; }
}