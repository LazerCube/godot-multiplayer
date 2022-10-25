using LiteNetLib.Utils;

namespace Dotplay.Input;

/// <summary>
/// Network command to send client input to server
/// </summary>
public struct PlayerInput : INetSerializable
{
    /// <summary>
    /// For each input:
    /// Delta between the input world tick and the tick the server was at for that input.
    /// TODO: This may be overkill, determining an average is probably better, but for now
    /// this will give us 100% accuracy over lag compensation.
    /// </summary>
    public short[] ClientWorldTickDeltas;

    /// <summary>
    /// An array of inputs, one entry for tick.  Ticks are guaranteed to be contiguous.
    /// </summary>
    public GeneralPlayerInput[] Inputs;

    /// <summary>
    /// The world tick for the first input in the array.
    /// </summary>
    public uint StartWorldTick;

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.StartWorldTick = reader.GetUInt();

        var length = reader.GetInt();

        this.ClientWorldTickDeltas = new short[length];
        this.Inputs = new GeneralPlayerInput[length];

        for (int i = 0; i < length; i++)
        {
            this.ClientWorldTickDeltas[i] = reader.GetShort();
            this.Inputs[i].Deserialize(reader);
        }
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.StartWorldTick);

        if (this.Inputs != null)
        {
            writer.Put(this.Inputs.Length);

            for (int i = 0; i < this.Inputs.Length; i++)
            {
                writer.Put(this.ClientWorldTickDeltas[i]);
                this.Inputs[i].Serialize(writer);
            }
        }
        else
        {
            writer.Put(0);
        }
    }
}