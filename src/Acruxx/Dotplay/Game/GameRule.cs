using System.Linq;
using Dotplay.Game.Server;
using Dotplay.Physics;

namespace Dotplay.Game;

/// <summary>
/// Basic class for an game rule
/// </summary>
public abstract class GameRule : IGameRule
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public GameRule()
    { }

    /// <inheritdoc />
    public NetworkServerWorld GameWorld { get; set; }

    /// <inheritdoc />
    public string RuleName { get; set; }

    /// <inheritdoc />
    public void AddComponentToLocalPlayer<T>(INetworkCharacter player) where T : IPlayerComponent
    {
        if (player.IsServer())
        {
            var hasComp = (player as NetworkCharacter).Components.Get(typeof(T));
            if (hasComp != null && hasComp is IPlayerComponent)
            {
                var list = (player as NetworkCharacter).RequiredComponents.ToList();
                var component = (hasComp as IPlayerComponent);

                if (!list.Contains(component.NetworkId))
                {
                    list.Add(component.NetworkId);
                }

                Logger.LogDebug(this, "Active local component: " + typeof(T).ToString());
                (player as NetworkCharacter).RequiredComponents = list.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void AddComponentToPuppetPlayer<T>(INetworkCharacter player) where T : IPlayerComponent
    {
        if (player.IsServer())
        {
            var hasComp = (player as NetworkCharacter).Components.Get(typeof(T));
            if (hasComp != null && hasComp is IPlayerComponent)
            {
                var list = (player as NetworkCharacter).RequiredComponents.ToList();
                var component = (hasComp as IPlayerComponent);

                if (!list.Contains(component.NetworkId))
                {
                    list.Add(component.NetworkId);
                }

                Logger.LogDebug(this, "Active local component: " + typeof(T).ToString());
                (player as NetworkCharacter).RequiredComponents = list.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void AddComponentToServerPlayer<T>(INetworkCharacter player) where T : IPlayerComponent
    {
        if (player is NetworkCharacter)
        {
            var hasComp = (player as NetworkCharacter).Components.Get(typeof(T));
            if (hasComp != null && hasComp is IPlayerComponent)
            {
                Logger.LogDebug(this, "Active server component: " + typeof(T).ToString());
                player.ActivateComponent((hasComp as IPlayerComponent).NetworkId, true);
            }
        }
    }

    /// <inheritdoc />
    public virtual void OnGameRuleActivated()
    { }

    /// <summary>
    /// Trigger when an player got an hit
    /// </summary>
    public virtual void OnHit(RayCastHit player)
    { }

    /// <inheritdoc />
    public virtual void OnNewPlayerJoined(INetworkCharacter player)
    { }

    /// <inheritdoc />
    public virtual void OnPlayerLeave(INetworkCharacter player)
    { }

    /// <inheritdoc />
    public virtual void OnPlayerLeaveTemporary(INetworkCharacter player)
    { }

    /// <inheritdoc />
    public virtual void OnPlayerRejoined(INetworkCharacter player)
    { }

    /// <inheritdoc />
    public void RemoteComponentFromPuppetPlayer<T>(INetworkCharacter player) where T : IPlayerComponent
    {
        if (player.IsServer())
        {
            var hasComp = (player as NetworkCharacter).Components.Get(typeof(T));
            if (hasComp != null && hasComp is Godot.Node)
            {
                var list = (player as NetworkCharacter).RequiredPuppetComponents.ToList();
                var index = (short)(player as NetworkCharacter).Components.All.ToList().IndexOf(hasComp as Godot.Node);

                if (list.Contains(index))
                {
                    list.Remove(index);
                }

                Logger.LogDebug(this, "Deactive puppet component: " + typeof(T).ToString());
                (player as NetworkCharacter).RequiredPuppetComponents = list.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void RemoveComponentFromLocalPlayer<T>(INetworkCharacter player) where T : IPlayerComponent
    {
        if (player.IsServer())
        {
            var hasComp = (player as NetworkCharacter).Components.Get(typeof(T));
            if (hasComp != null && hasComp is Godot.Node)
            {
                var list = (player as NetworkCharacter).RequiredComponents.ToList();
                var index = (short)(player as NetworkCharacter).Components.All.ToList().IndexOf(hasComp as Godot.Node);

                if (list.Contains(index))
                {
                    list.Remove(index);
                }

                Logger.LogDebug(this, "Deactive local component: " + typeof(T).ToString());
                (player as NetworkCharacter).RequiredComponents = list.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public virtual void Tick(float delta)
    { }
}