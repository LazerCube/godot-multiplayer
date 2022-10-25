namespace Dotplay.Network.Commands;

/// <summary>
/// Network package for notification when player leaves the game.
/// </summary>
public class PlayerLeave
{
    /// <summary>
    /// Current network id
    /// </summary>
    public short NetworkId { get; set; }
}