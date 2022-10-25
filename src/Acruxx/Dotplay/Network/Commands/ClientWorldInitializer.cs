using Dotplay.Game;
using Dotplay.Network.Commands;
using LiteNetLib.Utils;

namespace Framework.Network.Commands;

/// <summary>
/// Network command for an client, after map was loaded sucessfull
/// Contains all server relevated settings and vars
/// </summary>
public class ClientWorldInitializer
{
    /// <summary>
    /// Current server world tick
    /// </summary>
    public uint GameTick { get; set; }

    /// <summary>
    /// Gets or sets the init state.
    /// </summary>
    public PlayerState InitState { get; set; }

    /// <summary>
    /// Own player id on server
    /// </summary>
    public short PlayerId { get; set; }

    /// <summary>
    /// Server variables
    /// </summary>
    public Vars ServerVars { get; set; }

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.GameTick = reader.GetUInt();
        this.PlayerId = reader.GetShort();
        this.InitState = reader.Get<PlayerState>();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.GameTick);
        writer.Put(this.PlayerId);
        writer.Put(this.InitState);
    }
}