namespace Dotplay.Game;

/// <summary>
/// Required interface for levels
/// </summary>
public interface INetworkLevel
{
    /// <summary>
    /// The environment of the level
    /// </summary>
    public Godot.Environment Environment { get; }

    /// <summary>
    /// List of all spawn points in map
    /// </summary>
    public SpawnPoint[] GetAllSpawnPoints();

    /// <summary>
    /// List of free spawn points
    /// </summary>
    public SpawnPoint[] GetFreeSpawnPoints();
}