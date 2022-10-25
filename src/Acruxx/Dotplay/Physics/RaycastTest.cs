using Godot;
using LiteNetLib.Utils;

namespace Dotplay.Physics;

/// <summary>
/// Network command for debugging raycasts between server nd client
/// </summary>
public struct RaycastTest : INetSerializable
{
    /// <summary>
    /// Gets or sets the from.
    /// </summary>
    public Vector3 From { get; set; }

    /// <summary>
    /// Gets or sets the to.
    /// </summary>
    public Vector3 To { get; set; }

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.From = reader.GetVector3();
        this.To = reader.GetVector3();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.From);
        writer.Put(this.To);
    }
}