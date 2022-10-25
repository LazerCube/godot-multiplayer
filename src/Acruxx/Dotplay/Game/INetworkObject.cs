using System;
using System.Collections.Generic;
using System.Reflection;
using Dotplay.Network;

namespace Dotplay.Game;

/// <summary>
/// The game object network mode
/// </summary>
public enum NetworkMode
{
    /// <summary>
    /// Game object is an server object
    /// </summary>
    SERVER = 0,

    /// <summary>
    /// Game object is an local object
    /// </summary>
    CLIENT = 1,

    /// <summary>
    /// Game object is an local object as puppet
    /// </summary>
    PUPPET = 2,
}

/// <summary>
/// Required interface for network objects
/// </summary>
public interface INetworkObject : IBaseComponent
{
    /// <summary>
    /// The network mode for the game object
    /// </summary>
    public NetworkMode Mode { get; set; }

    /// <summary>
    /// Id of game object
    /// </summary>
    public short NetworkId { get; set; }

    /// <summary>
    /// Gets the network sync vars.
    /// </summary>
    public Dictionary<string, NetworkAttribute> NetworkSyncVars { get; }

    /// <summary>
    /// The resource path of the component
    /// </summary>
    public string ResourcePath { get; set; }

    /// <summary>
    /// The script path (mono) of the component
    /// </summary>
    public string[] ScriptPaths { get; set; }
}

/// <summary>
/// Network attribute
/// </summary>
public struct NetworkAttribute
{
    /// <summary>
    /// Gets or sets the attribute index.
    /// </summary>
    public short AttributeIndex { get; set; }

    /// <summary>
    /// Gets or sets the attribute type.
    /// </summary>
    public Type AttributeType { get; set; }

    /// <summary>
    /// Gets or sets the from.
    /// </summary>
    public NetworkSyncFrom From { get; set; }

    /// <summary>
    /// Gets or sets the to.
    /// </summary>
    public NetworkSyncTo To { get; set; }
}

/// <inheritdoc />
public static class INetworkObjectExtension
{
    /// <summary>
    /// Get the player network attributes
    /// </summary>
    public static Dictionary<string, NetworkAttribute> GetNetworkAttributes(this INetworkObject networkObject)
    {
        var list = new Dictionary<string, NetworkAttribute>();
        short i = 0;
        foreach (var prop in networkObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (Attribute.IsDefined(prop, typeof(NetworkVar)))
            {
                NetworkVar customattribute = (NetworkVar)prop.GetCustomAttribute(typeof(NetworkVar), false);

                list.Add(prop.Name, new NetworkAttribute { AttributeType = prop.FieldType, AttributeIndex = i, From = customattribute.From, To = customattribute.To });
                i++;
            }
        }

        foreach (var prop in networkObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (Attribute.IsDefined(prop, typeof(NetworkVar)))
            {
                list.Add(prop.Name, new NetworkAttribute { AttributeType = prop.GetType(), AttributeIndex = i });
                i++;
            }
        }

        return list;
    }

    /// <summary>
    /// Are the local.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A bool.</returns>
    public static bool IsLocal(this INetworkObject client)
    {
        return client.Mode == NetworkMode.CLIENT;
    }

    /// <summary>
    /// Are the puppet.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A bool.</returns>
    public static bool IsPuppet(this INetworkObject client)
    {
        return client.Mode == NetworkMode.PUPPET;
    }

    /// <summary>
    /// Are the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>A bool.</returns>
    public static bool IsServer(this INetworkObject client)
    {
        return client.Mode == NetworkMode.SERVER;
    }
}