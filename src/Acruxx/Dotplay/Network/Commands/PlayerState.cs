using System;
using System.Collections.Generic;
using System.Linq;
using Dotplay.Game;
using Godot;
using LiteNetLib.Utils;

namespace Dotplay.Network.Commands;

/// <summary>
/// The player state var type.
/// </summary>
public enum PlayerStateVarType
{
    Permanent,
    Sync
}

/// <summary>
/// The player state struct.
/// </summary>
public struct PlayerNetworkVarState : INetSerializable
{
    public byte[] Data;
    public short Key;

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.Key = reader.GetShort();
        this.Data = reader.GetBytesWithLength();
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.Key);
        writer.PutBytesWithLength(this.Data);
    }
}

/// <summary>
/// The player states structures
/// Contains all player realted informations eg. position, rotation, velocity
/// </summary>
public struct PlayerState : INetSerializable
{
    /// <summary>
    /// Current latency
    /// </summary>
    public short Latency;

    /// <summary>
    /// The id of this player
    /// </summary>
    public short NetworkId;

    /// <summary>
    /// Uncomposed list of components and component states
    /// </summary>
    public List<PlayerNetworkVarState> NetworkSyncedVars;

    /// <inheritdoc />
    public void Deserialize(NetDataReader reader)
    {
        this.NetworkId = reader.GetShort();
        this.Latency = reader.GetShort();

        this.NetworkSyncedVars = reader.GetArray<PlayerNetworkVarState>().ToList();
    }

    /// <summary>
    /// Gets the var.
    /// </summary>
    /// <param name="netObject">The net object.</param>
    /// <param name="key">The key.</param>
    /// <returns>An object.</returns>
    public object GetVar(INetworkObject netObject, string key)
    {
        var checkCollection = new Dictionary<string, NetworkAttribute>();

        var collection = this.NetworkSyncedVars ?? new List<PlayerNetworkVarState>();
        checkCollection = netObject.NetworkSyncVars;

        if (collection == null || checkCollection == null || netObject == null)
        {
            return null;
        }

        if (!checkCollection.ContainsKey(key))
        {
            Logger.LogDebug(this, "Key " + key + " not registered.");
            return null;
        }

        var origNetVar = checkCollection[key];
        if (!collection.Any(df => df.Key == origNetVar.AttributeIndex))
        {
            Logger.LogDebug(this, "Key " + key + " not in network package.");
            return null;
        }

        PlayerNetworkVarState value = collection.First(df => df.Key == origNetVar.AttributeIndex);
        if (value.Data == null)
        {
            return null;
        }

        var result = ParseBytesToAnyType(origNetVar.AttributeType, value.Data);
        return result ?? throw new Exception("Result cant be null for " + key + "from type" + origNetVar.AttributeType + " with length of " + value.Data.Length);
    }

    /// <summary>
    /// Gets the var.
    /// </summary>
    /// <param name="netObject">The net object.</param>
    /// <param name="key">The key.</param>
    /// <param name="fallback">The fallback.</param>
    /// <returns>A T.</returns>
    public T GetVar<T>(INetworkObject netObject, string key, T fallback = default)
    {
        if (netObject is null)
        {
            return fallback;
        }

        var var = this.GetVar(netObject, key);
        return var == null ? fallback : var is T t ? t : fallback;
    }

    /// <inheritdoc />
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(this.NetworkId);
        writer.Put(this.Latency);

        if (this.NetworkSyncedVars == null)
        {
            this.NetworkSyncedVars = new List<PlayerNetworkVarState>();
        }

        writer.PutArray(this.NetworkSyncedVars.ToArray());
    }

    /// <summary>
    /// Sets the var.
    /// </summary>
    /// <param name="netObject">The net object.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void SetVar(INetworkObject netObject, string key, object? value)
    {
        if (value == null)
        {
            throw new Exception("Cant be null;");
        }

        var collection = this.NetworkSyncedVars ?? new List<PlayerNetworkVarState>();
        var checkCollection = new Dictionary<string, NetworkAttribute>();

        collection = this.NetworkSyncedVars;
        checkCollection = netObject.NetworkSyncVars;

        if (!checkCollection.ContainsKey(key))
        {
            throw new Exception("PermanentNetworkVar with  " + key + " or from type  " + value.GetType() + " not found.");
        }
        else
        {
            var orig = checkCollection[key];
            var bytes = ParseAnyTypeToBytes(value);
            if (bytes.Length > 0)
            {
                var newValue = new PlayerNetworkVarState { Key = orig.AttributeIndex, Data = bytes };
                var findIndex = collection.FindIndex(df => df.Key == orig.AttributeIndex);
                if (findIndex == -1)
                {
                    collection.Add(newValue);
                }
                else
                {
                    collection[findIndex] = newValue;
                }
            }
            else
            {
                throw new Exception("Empty byte array??");
            }
        }
    }

    /// <summary>
    /// Parses the any type to bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>An array of byte.</returns>
    internal static byte[] ParseAnyTypeToBytes(object value)
    {
        if (value == null)
        {
            throw new Exception("Cant be null");
        }

        var writer = new NetDataWriter();

        if (value.GetType().IsEnum)
        {
            writer.Put((uint)value);
        }
        else if (value is float floatValue)
        {
            writer.Put(floatValue);
        }
        else if (value is double doubleValue)
        {
            writer.Put(doubleValue);
        }
        else if (value is string stringValue)
        {
            writer.Put(stringValue);
        }
        else if (value is int intValue)
        {
            writer.Put(intValue);
        }
        else if (value is bool boolValue)
        {
            writer.Put(boolValue);
        }
        else if (value is short shortValue)
        {
            writer.Put(shortValue);
        }
        else if (value is uint uintValue)
        {
            writer.Put(uintValue);
        }
        else if (value is Quaternion quaternion)
        {
            writer.Put(quaternion);
        }
        else if (value is Vector3 vector3)
        {
            writer.Put(vector3);
        }
        else if (value is Vector2 vector2)
        {
            writer.Put(vector2);
        }
        else
        {
            throw new Exception(value.GetType() + " unknown parse type.");
        }

        return writer.CopyData();
    }

    /// <summary>
    /// Parses the bytes to any type.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <param name="value">The value.</param>
    /// <returns>An object.</returns>
    internal static object ParseBytesToAnyType(Type t, byte[] value)
    {
        var reader = new NetDataReader(value);
        if (t.IsEnum)
        {
            return Enum.ToObject(t, reader.GetUInt());
        }
        else if (t == typeof(int))
        {
            return reader.GetInt();
        }
        else if (t == typeof(double))
        {
            return reader.GetDouble();
        }
        else if (t == typeof(float))
        {
            return reader.GetFloat();
        }
        else if (t == typeof(string))
        {
            return reader.GetString();
        }
        else if (t == typeof(Vector3))
        {
            return reader.GetVector3();
        }
        else if (t == typeof(Vector2))
        {
            return reader.GetVector2();
        }
        else if (t == typeof(bool))
        {
            return reader.GetBool();
        }
        else if (t == typeof(short))
        {
            return reader.GetShort();
        }
        else if (t == typeof(uint))
        {
            return reader.GetUInt();
        }
        else if (t == typeof(Quaternion))
        {
            return reader.GetQuaternion();
        }
        else
        {
            return null;
        }
    }
}