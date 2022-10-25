using System.Collections.Generic;

namespace Dotplay.Game;

/// <summary>
/// The required interface for an game world
/// </summary>
public interface INetworkWorld
{
    /// <summary>
    /// Game logic controller
    /// </summary>
    public GameLogic GameInstance { get; }

    /// <summary>
    /// The loaded game level of the world
    /// </summary>
    public INetworkLevel NetworkLevel { get; }

    /// <summary>
    /// All players of the world
    /// </summary>
    public Dictionary<short, NetworkCharacter> Players { get; }

    /// <summary>
    /// Path of the world resource
    /// </summary>
    public string ResourceWorldPath { get; }

    /// <summary>
    /// Path of the world resource script
    /// </summary>
    public string ResourceWorldScriptPath { get; }

    /// <summary>
    /// The server vars of the world
    /// </summary>
    public VarsCollection ServerVars { get; }

    /// <summary>
    /// The current tick of the world
    /// </summary>
    public uint WorldTick { get; }

    /// <summary>
    /// Destroy the game world
    /// </summary>
    public void Destroy();

    /// <summary>
    ///  When level was adding complelty to scene
    /// </summary>
    public void OnLevelAddToScene();

    /// <summary>
    /// Calls when an player is initialized and the map was loaded sucessfulll
    /// </summary>
    /// <param name="p"></param>
    public void OnPlayerInitilaized(INetworkCharacter p);

    /// <summary>
    /// The physics and network related tick process method
    /// </summary>
    public void Tick(float tickInterval);
}