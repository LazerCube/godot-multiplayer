using Dotplay.Network;

namespace Dotplay.Game;

/// <summary>
/// Required interface for players
/// </summary>
public interface INetworkCharacter : IBaseComponent, INetworkObject
{
    /// <summary>
    /// Current latency (ping)
    /// </summary>
    public int Latency { get; set; }

    /// <summary>
    /// Name of player
    /// </summary>
    public string PlayerName { get; set; }

    /// <summary>
    /// Current connection state
    /// </summary>
    public PlayerConnectionState State { get; set; }

    /// <summary>
    /// Activate or disable an component by given id
    /// </summary>
    /// <param name="index"></param>
    /// <param name="isEnabled"></param>
    public void ActivateComponent(int index, bool isEnabled);

    /// <summary>
    /// Telport to a given position
    /// </summary>
    /// <param name="origin"></param>
    public void DoTeleport(Godot.Vector3 origin);
}