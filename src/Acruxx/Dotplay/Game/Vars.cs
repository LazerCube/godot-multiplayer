using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Dotplay.Game;

/// <summary>
/// An dictonary for settings (vars)
/// </summary>
public struct Vars : INetSerializable
{
    /// <summary>
    /// Constructor for server vars
    /// </summary>
    /// <param name="vars">Dictonary which contains server varaibles</param>
    public Vars(Dictionary<string, string> vars)
    {
        this.AllVariables = vars;
    }

    /// <summary>
    /// Dictonary which contains server varaibles
    /// </summary>
    public Dictionary<string, string> AllVariables { get; private set; } = new();

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.AllVariables = reader.GetDictonaryString();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.AllVariables);
    }
}