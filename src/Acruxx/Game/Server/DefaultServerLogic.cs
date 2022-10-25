using Dotplay.Game.Server;
using Godot;

namespace Acruxx.Server;

/// <summary>
/// The default server logic.
/// </summary>
public partial class DefaultServerLogic : NetworkServerLogic
{
    [Export(PropertyHint.File, "*.tscn")]
    public string DefaultMap = "res://src/Acruxx/Game/Shared/Maps/Default/Default.tscn";

    /// <inheritdoc />
    public override void AfterMapLoaded()
    {
        (this._currentWorld as NetworkServerWorld)?.SetGameRule<PlaygroundGameRule>("playground");
    }

    /// <inheritdoc />
    public override NetworkServerWorld CreateWorld()
    {
        return GD.Load<PackedScene>("res://src/Acruxx/Game/Server/DefaultServerWorld.tscn").Instantiate<DefaultServerWorld>();
    }

    /// <inheritdoc />
    public override void OnServerStarted()
    {
        this.LoadWorld(this.DefaultMap, "res://src/Acruxx/Game/Shared/Maps/Default/DefaultGameLevel.cs", 0);
    }
}