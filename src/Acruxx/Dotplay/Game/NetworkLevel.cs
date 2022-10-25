using System.Linq;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// A basic class for an game level
/// </summary>
public partial class NetworkLevel : Node3D, INetworkLevel
{
    /// <summary>
    /// The node path to the world environment node
    /// </summary>
    [Export]
    public NodePath EnvironmentPath;

    /// <summary>
    /// The world environment of the levelk
    /// </summary>
    public Environment Environment
    {
        get
        {
            var worldEnv = this.GetNodeOrNull<WorldEnvironment>(this.EnvironmentPath);
            return worldEnv != null ? worldEnv.Environment : null;
        }
    }

    /// <inheritdoc />
    public SpawnPoint[] GetAllSpawnPoints()
    {
        return this.GetChildren().OfType<SpawnPoint>().ToArray();
    }

    /// <inheritdoc />
    public SpawnPoint[] GetFreeSpawnPoints()
    {
        return this.GetAllSpawnPoints().Where(df => df.isFree()).ToArray();
    }
}