using Godot;
using LiteNetLib.Utils;

namespace Dotplay.Input;

/// <summary>
/// The network package required for sending inputs
/// </summary>
public interface IPlayerInput : INetSerializable
{
    /// <summary>
    /// The current view direction / camera direction
    /// </summary>
    public Vector3 ViewDirection { get; set; }

    /// <summary>
    /// Get input by given string, related to booleans with PlayerInputAttribute
    /// </summary>
    /// <param name="name"></param>
    public bool GetInput(string name);
}