using LiteNetLib.Utils;

namespace Dotplay.Network.Commands;

/// <summary>
/// Network package for notification of new players or players changes.
/// </summary>
public struct PlayerInfo : INetSerializable
{
    /// <summary>
    /// Current network id
    /// </summary>
    public short NetworkId;

    /// <summary>
    /// Current player name
    /// </summary>
    public string PlayerName;

    /// <summary>
    /// Required local player components
    /// </summary>
    public short[] RequiredComponents;

    /// <summary>
    /// Required puppet components
    /// </summary>
    public short[] RequiredPuppetComponents;

    /// <summary>
    /// Resource path to the scene
    /// </summary>
    public string ResourcePath;

    /// <summary>
    /// Script path of the scene
    /// </summary>
    public string[] ScriptPaths;

    /// <summary>
    /// Current network connection state
    /// </summary>
    public PlayerConnectionState State;

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.NetworkId = reader.GetShort();
        this.State = (PlayerConnectionState)reader.GetByte();

        this.PlayerName = reader.GetString();
        this.ResourcePath = reader.GetString();

        this.ScriptPaths = reader.GetStringArray();
        this.RequiredComponents = reader.GetShortArray();
        this.RequiredPuppetComponents = reader.GetShortArray();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.NetworkId);
        writer.Put((byte)this.State);

        writer.Put(this.PlayerName);
        writer.Put(this.ResourcePath);

        writer.PutArray(this.ScriptPaths);
        writer.PutArray(this.RequiredComponents);
        writer.PutArray(this.RequiredPuppetComponents);
    }
}