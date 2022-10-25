using System.Collections.Generic;
using Dotplay;
using Dotplay.Game;

namespace Acruxx.Server;

/// <summary>
/// The playground game rule.
/// </summary>
public class PlaygroundGameRule : GameRule
{
    public Queue<INetworkCharacter> PlayersWaitingForSlot = new();

    /// <inheritdoc/>
    public override void OnNewPlayerJoined(INetworkCharacter player)
    {
        var spawnpoints = this.GameWorld.NetworkLevel.GetFreeSpawnPoints();
        if (spawnpoints.Length > 0)
        {
            this.AddPlayerWithSlot(spawnpoints[0], player);
        }
        else
        {
            Logger.LogDebug(this, "Cant find free slot for player " + player.NetworkId);
            this.PlayersWaitingForSlot.Enqueue(player);
        }
    }

    /// <inheritdoc/>
    public override void Tick(float delta)
    {
        base.Tick(delta);

        if (this.PlayersWaitingForSlot.Count > 0)
        {
            var spawnpoints = this.GameWorld.NetworkLevel.GetFreeSpawnPoints();
            if (spawnpoints.Length > 0)
            {
                var player = this.PlayersWaitingForSlot.Dequeue();
                this.AddPlayerWithSlot(spawnpoints[0], player);
            }
        }
    }

    /// <summary>
    /// Adds the player with slot.
    /// </summary>
    /// <param name="spawnPoint">The spawn point.</param>
    /// <param name="player">The player.</param>
    private void AddPlayerWithSlot(SpawnPoint spawnPoint, INetworkCharacter player)
    {
        var origin = spawnPoint.GlobalTransform.origin;

        Logger.LogDebug(this, "Player was joined to " + origin.ToString());

        //this.AddComponentToServerPlayer<PlayerAnimationComponent>(player);
        this.AddComponentToServerPlayer<CharacterCamera>(player);
        //this.AddComponentToServerPlayer<PlayerFootstepComponent>(player);
        this.AddComponentToServerPlayer<NetworkInput>(player);

        //this.AddComponentToLocalPlayer<PlayerAnimationComponent>(player);
        this.AddComponentToLocalPlayer<NetworkInput>(player);
        this.AddComponentToLocalPlayer<CharacterCamera>(player);
        //this.AddComponentToLocalPlayer<PlayerFootstepComponent>(player);
        //this.AddComponentToLocalPlayer<FPSWeaponAnimator>(player);

        //this.AddComponentToPuppetPlayer<FPSWeaponAnimator>(player);
        //this.AddComponentToPuppetPlayer<PlayerAnimationComponent>(player);
        //this.AddComponentToPuppetPlayer<PlayerFootstepComponent>(player);

        player.DoTeleport(origin);
    }
}