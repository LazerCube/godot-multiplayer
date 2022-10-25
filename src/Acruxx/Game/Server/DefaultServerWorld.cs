using Dotplay;
using Dotplay.Game.Server;

namespace Acruxx.Server;

/// <summary>
/// The default server world.
/// </summary>
public partial class DefaultServerWorld : NetworkServerWorld
{
    /// <inheritdoc/>
    public override void OnPlayerConnected(short clientId)
    {
        Logger.LogDebug(this, $"Player {clientId} connected");
        this.AddPlayer(clientId, "res://src/Acruxx/Game/Shared/Player/DefaultPlayer.tscn",
            new string[]
            {
                "res://src/Acruxx/Game/Shared/Player/DefaultPlayer.cs",
                "res://src/Acruxx/Dotplay/Game/CharacterCamera.cs",
                "res://src/Acruxx/Dotplay/Game/NetworkInput.cs",
            });
    }
}