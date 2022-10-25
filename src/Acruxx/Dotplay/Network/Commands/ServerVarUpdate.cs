using Dotplay.Game;

namespace Dotplay.Network.Commands;

/// <summary>
/// Network command for updaing server vars on client side
/// </summary>
public class ServerVarUpdate
{
    /// <summary>
    /// All server vars
    /// </summary>
    public Vars ServerVars { get; set; }
}