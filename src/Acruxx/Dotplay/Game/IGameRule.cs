using Dotplay.Game.Server;
using Dotplay.Physics;

namespace Dotplay.Game;

/// <summary>
/// Required interface for game rules
/// </summary>
public interface IGameRule
{
    /// <summary>
    /// The server world
    /// </summary>
    public NetworkServerWorld GameWorld { get; }

    /// <summary>
    /// The name of the game rule
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// Add an component to an local player instance on client side
    /// </summary>
    /// <param name="player"></param>
    /// <typeparam name="T">Type of component</typeparam>
    public void AddComponentToLocalPlayer<T>(INetworkCharacter player) where T : IPlayerComponent;

    /// <summary>
    /// Add an component to an server player instance
    /// </summary>
    /// <param name="player"></param>
    /// <typeparam name="T">Type of component</typeparam>
    public void AddComponentToPuppetPlayer<T>(INetworkCharacter player) where T : IPlayerComponent;

    /// <summary>
    /// Add an component to an server side player
    /// </summary>
    /// <param name="player"></param>
    /// <typeparam name="T">Type of component</typeparam>
    public void AddComponentToServerPlayer<T>(INetworkCharacter player) where T : IPlayerComponent;

    /// <summary>
    /// Triggered when the game rule will be activated
    /// </summary>
    public void OnGameRuleActivated();

    /// <summary>
    /// Triggered when an player got an hit
    /// </summary>
    /// <param name="player"></param>
    public void OnHit(RayCastHit player);

    /// <summary>
    /// Called on new player joined the game
    /// </summary>
    /// <param name="player"></param>
    public void OnNewPlayerJoined(INetworkCharacter player);

    /// <summary>
    /// Called when players finanly leave the game
    /// </summary>
    /// <param name="player"></param>
    public void OnPlayerLeave(INetworkCharacter player);

    /// <summary>
    /// Called when players are disconnected (as eg. timeouts)
    /// </summary>
    /// <param name="player"></param>
    public void OnPlayerLeaveTemporary(INetworkCharacter player);

    /// <summary>
    /// Called when player are rejoined the game (after previous disconnect)
    /// </summary>
    /// <param name="player"></param>
    public void OnPlayerRejoined(INetworkCharacter player);

    /// <summary>
    /// Remove an component from an local player (server sided)
    /// </summary>
    /// <param name="player"></param>
    /// <typeparam name="T">Type of component</typeparam>
    public void RemoteComponentFromPuppetPlayer<T>(INetworkCharacter player) where T : IPlayerComponent;

    /// <summary>
    /// Remove an component from an local player (client sided)
    /// </summary>
    /// <param name="player"></param>
    /// <typeparam name="T">Type of component</typeparam>
    public void RemoveComponentFromLocalPlayer<T>(INetworkCharacter player) where T : IPlayerComponent;

    /// <summary>
    /// Execute on each server tick
    /// </summary>
    /// <param name="delta"></param>
    public void Tick(float delta);
}