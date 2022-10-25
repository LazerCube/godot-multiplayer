using System;

namespace Dotplay.Network;

/// <summary>
/// The network sync from.
/// </summary>
[Flags]
public enum NetworkSyncFrom
{
    None = 0,
    FromServer = 1,
    FromClient = 2
}

/// <summary>
/// The network sync to.
/// </summary>
[Flags]
public enum NetworkSyncTo
{
    None = 0,
    ToPuppet = 1,
    ToClient = 2,
    ToServer = 3
}

/// <summary>
/// The network var.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class NetworkVar : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkVar"/> class.
    /// </summary>
    public NetworkVar()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkVar"/> class.
    /// </summary>
    /// <param name="from">The from.</param>
    public NetworkVar(NetworkSyncFrom from)
    {
        this.From = from;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkVar"/> class.
    /// </summary>
    /// <param name="to">The to.</param>
    public NetworkVar(NetworkSyncTo to)
    {
        this.To = to;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkVar"/> class.
    /// </summary>
    /// <param name="from">The from.</param>
    /// <param name="to">The to.</param>
    public NetworkVar(NetworkSyncFrom from, NetworkSyncTo to)
    {
        this.To = to;
        this.From = from;
    }

    /// <summary>
    /// Gets or sets the from.
    /// </summary>
    public NetworkSyncFrom From { get; set; } = NetworkSyncFrom.FromServer;

    /// <summary>
    /// Gets or sets the to.
    /// </summary>
    public NetworkSyncTo To { get; set; } = NetworkSyncTo.ToClient | NetworkSyncTo.ToPuppet;
}