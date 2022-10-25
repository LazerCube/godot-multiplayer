using LiteNetLib.Utils;

namespace Dotplay.Network.Commands;

/// <summary>
/// Network package for initialize game level and client side
/// </summary>
public struct ClientWorldLoader : INetSerializable
{
    /// <summary>
    /// Resource path to level script
    /// </summary>
    public string ScriptPath;

    /// <summary>
    /// Resource path to level
    /// </summary>
    public string WorldName;

    /// <summary>
    /// Current server world tick
    /// </summary>
    public uint WorldTick;

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.WorldName = reader.GetString();
        this.ScriptPath = reader.GetString();
        this.WorldTick = reader.GetUInt();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.WorldName);
        writer.Put(this.ScriptPath);
        writer.Put(this.WorldTick);
    }
}