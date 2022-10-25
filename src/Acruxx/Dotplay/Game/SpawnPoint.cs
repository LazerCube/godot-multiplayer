using Godot;

namespace Dotplay.Game;

/// <summary>
/// The gernal spawn point class
/// </summary>
public partial class SpawnPoint : Area3D
{
    private int enteredBodies = 0;

    /// <summary>
    /// Set or get the usage of the spawnpoint
    /// </summary>
    public bool inUsage { get; set; } = false;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();

        this.BodyEntered += (body) =>
        {
            if (body is RigidBody3D || body is CharacterBody3D)
            {
                this.enteredBodies++;
            }
        };

        this.BodyExited += (body) =>
        {
            if (body is RigidBody3D || body is CharacterBody3D)
            {
                this.enteredBodies--;
            }
        };
    }

    /// <summary>
    /// Returns if the spawn point are free and not in use
    /// </summary>
    /// <returns></returns>
    public bool isFree()
    {
        return (this.enteredBodies == 0);
    }
}