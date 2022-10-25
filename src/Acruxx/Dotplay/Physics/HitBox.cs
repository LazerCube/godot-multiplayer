using Dotplay.Game;
using Godot;

namespace Dotplay.Physics;

/// <summary>
/// Hit box collider
/// </summary>
public partial class HitBox : StaticBody3D
{
    /// <summary>
    /// The name of the collider group
    /// </summary>
    [Export]
    public string GroupName { get; set; } = string.Empty;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();

        this.CollisionLayer = 0;
        this.CollisionMask = 0;
        this.SetCollisionLayerValue(32, true);
        this.SetCollisionMaskValue(32, true);
    }

    /// <summary>
    /// Get the player instance of an hitbox
    /// </summary>
    public INetworkCharacter? GetPlayer()
    {
        return this.FindPlayer(this);
    }

    /// <inheritdoc />
    internal INetworkCharacter? FindPlayer(Node node)
    {
        if (node is NetworkCharacter networkCharacter)
        {
            return networkCharacter;
        }

        var parent = node.GetParent();
        return parent == null || parent is Viewport ? null : parent is not null ? this.FindPlayer(parent) : null;
    }
}