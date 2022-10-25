namespace Dotplay.Network.Commands;

/// <summary>
/// Network package for notification of new players or players changes over all players.
/// </summary>
public class PlayerCollection
{
    /// <summary>
    /// Gets or sets the updates.
    /// </summary>
    public PlayerInfo[] Updates { get; set; }

    /// <summary>
    /// Current network id
    /// </summary>
    public uint WorldTick { get; set; }
}