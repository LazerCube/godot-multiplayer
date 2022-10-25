using System.Collections.Generic;
using System.Linq;
using Godot;
using LiteNetLib.Utils;

namespace Dotplay.Input;

/// <summary>
/// Default class for player input
/// </summary>
public struct GeneralPlayerInput : IPlayerInput
{
    /// <summary>
    /// The current activated input keys
    /// </summary>
    public string[] CurrentInput { get; private set; }

    /// <summary>
    /// The byte which contains all inputs
    /// </summary>
    public byte Input { get; set; }

    /// <summary>
    /// The view direction or camera direction
    /// </summary>
    public Vector3 ViewDirection { get; set; }

    /// <summary>
    /// Apply input keys with an existing list of avaible keys.
    /// Creating an bit mask
    /// </summary>
    /// <param name="inputKeys"></param>
    /// <param name="activeKeys"></param>
    public void Apply(List<string> inputKeys, Dictionary<string, bool> activeKeys)
    {
        if (inputKeys is null || activeKeys is null)
        {
            return;
        }

        int i = 1;
        byte input = 0;
        var listOfActiveKeys = new List<string>();
        foreach (var key in inputKeys.OrderBy(df => df))
        {
            if (activeKeys.ContainsKey(key) && activeKeys[key])
            {
                input |= byte.Parse((i).ToString());
                listOfActiveKeys.Add(key);
            }

            i *= 2;
        }

        this.CurrentInput = listOfActiveKeys.ToArray();
        this.Input = input;
    }

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.Input = reader.GetByte();
        this.ViewDirection = reader.GetVector3();
    }

    /// <summary>
    /// Deserialize input byte with an given list of input keys
    /// </summary>
    /// <param name="InputKeys"></param>
    public GeneralPlayerInput DeserliazeWithInputKeys(List<string> InputKeys)
    {
        if (InputKeys is null)
        {
            return this;
        }

        int i = 1;
        var listOfkeys = new List<string>();
        foreach (var key in InputKeys.OrderBy(df => df))
        {
            var currentByte = byte.Parse((i).ToString());
            var isInUse = (this.Input & currentByte) != 0;
            if (isInUse)
            {
                listOfkeys.Add(key);
            }
            i *= 2;
        }

        this.CurrentInput = listOfkeys.ToArray();
        return this;
    }

    /// <summary>
    /// Get input by given string, related to booleans with PlayerInputAttribute
    /// </summary>
    /// <param name="name"></param>
    public bool GetInput(string name)
    {
        return this.CurrentInput?.Length > 0 && this.CurrentInput.Contains(name);
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.Input);
        writer.Put(this.ViewDirection);
    }
}