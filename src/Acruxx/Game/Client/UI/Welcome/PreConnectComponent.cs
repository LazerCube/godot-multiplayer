using Dotplay;
using Dotplay.Game;
using Godot;

namespace Acruxx.Client.UI.Welcome;

/// <summary>
/// The pre connect component.
/// </summary>
public partial class PreConnectComponent : CanvasLayer, IChildComponent<GameLogic>
{
    /// <summary>
    /// The default hostname for the client
    /// </summary>
    [Export]
    public string DefaultNetworkHostname = "localhost";

    /// <inheritdoc />
    public GameLogic BaseComponent { get; set; }

    /// <summary>
    /// The default port of the server
    /// </summary>
    [Export]
    public int DefaultNetworkPort { get; set; } = 27015;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();
    }

    /// <summary>
    /// on the connect button pressed.
    /// </summary>
    private void OnConnectButtonPressed()
    {
        var component = this.BaseComponent as DefaultClientLogic;
        component?.Connect(this.DefaultNetworkHostname, this.DefaultNetworkPort);
    }
}