using Dotplay.Game;
using Godot;

namespace Dotplay.Physics;

/// <summary>
/// The hit result of an ray cast
/// </summary>
public class RayCastHit
{
    /// <summary>
    /// The collider which hitted
    /// </summary>
    public Node Collider { get; set; }

    /// <summary>
    /// The 3d vector from where the raycast starts
    /// </summary>
    public Vector3 From { get; set; }

    /// <summary>
    /// The collider which hitted
    /// </summary>
    public INetworkCharacter PlayerDestination { get; set; }

    /// <summary>
    /// The collider which hitted
    /// </summary>
    public INetworkCharacter PlayerSource { get; set; }

    /// <summary>
    /// The 3d vector where the raycast ends
    /// </summary>
    public Vector3 To { get; set; }
}