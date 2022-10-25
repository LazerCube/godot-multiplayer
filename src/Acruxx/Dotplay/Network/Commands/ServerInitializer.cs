using LiteNetLib.Utils;

namespace Dotplay.Network.Commands;

/// <summary>
/// Network command to tell the server that the client world is initalized
/// </summary>
public struct ServerInitializer : INetSerializable
{
    /// <summary>
    /// Gets or sets the server handshake for id.
    /// </summary>
    public int Handshake { get; set; }

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.Handshake = reader.GetInt();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.Handshake);
    }
}