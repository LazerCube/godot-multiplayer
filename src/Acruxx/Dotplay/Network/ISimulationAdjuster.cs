namespace Dotplay.Network;

/// <summary>
/// Adjust the server or client tickrate
/// </summary>
public interface ISimulationAdjuster
{
    /// <summary>
    /// Adjust value for server tickrate
    /// </summary>
    public float AdjustedInterval { get; }
}

/// <summary>
/// The default simulation adapter for a server instance
/// </summary>
public class ServerSimulationAdjuster : ISimulationAdjuster
{
    /// <inheritdoc />
    public float AdjustedInterval { get; } = 1.0f;
}