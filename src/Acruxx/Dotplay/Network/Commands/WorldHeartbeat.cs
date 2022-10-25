namespace Dotplay.Network.Commands;

/// <summary>
/// The world heartbeat structure for network syncronisation
/// </summary>
public class WorldHeartbeat
{
    /// <summary>
    /// States of all players
    /// </summary>
    public PlayerState[] PlayerStates { get; set; }

    /// <summary>
    /// The world tick this data represents.
    /// </summary>
    public uint WorldTick { get; set; }

    /// <summary>
    /// The last world tick the server acknowledged for you.
    /// The client should use this to determine the last acked input, as well as to compute
    /// its relative simulation offset.
    /// </summary>
    public uint YourLatestInputTick { get; set; }
}