namespace Dotplay.Network;

/// <summary>
/// Connection states of the player
/// </summary>
public enum PlayerConnectionState
{
    /// <summary>
    /// Only for pre-connection
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// When player is connected
    /// </summary>
    Connected = 1,

    /// <summary>
    /// When player have completly loading the server world
    /// </summary>
    Initialized = 2,

    /// <summary>
    /// When the player is disconnected
    /// </summary>
    Disconnected = 3
}