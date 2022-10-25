namespace Dotplay.Input;

/// <summary>
/// The input for an given world tick of on specfic player (server-sided)
/// </summary>
public struct TickInput
{
    /// <summary>
    /// The input
    /// </summary>
    public GeneralPlayerInput Inputs;

    /// <summary>
    /// The player id
    /// </summary>
    public short PlayerId;

    /// <summary>
    /// The remote world tick the player saw other entities at for this input.
    /// (This is equivalent to lastServerWorldTick on the client).
    /// </summary>
    public uint RemoteViewTick;

    /// <summary>
    /// The tick of the input
    /// </summary>
    public uint WorldTick;
}