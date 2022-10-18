namespace Acruxx.Game.Camera;

using Godot;

/// <summary>
/// The aim target.
/// </summary>
public partial class AimTarget : Sprite3D
{
    /// <inheritdoc />
    public override void _Ready()
    {
        this.TopLevel = true;
        this.Visible = false;
    }

    /// <summary>
    /// Updates the.
    /// </summary>
    /// <param name="ray">The ray.</param>
    public void Update(RayCast3D ray)
    {
        // update manually instead only once per frame in process
        ray.ForceRaycastUpdate();
        var isColliding = ray.IsColliding();
        this.Visible = isColliding;

        if (isColliding)
        {
            Vector3 collisionPoint = ray.GetCollisionPoint();
            Vector3 collisionNormal = ray.GetCollisionNormal();

            var transform = this.GlobalTransform;
            transform.origin = collisionPoint + (collisionNormal * 0.01f);
            this.GlobalTransform = transform;

            this.LookAt(collisionPoint - collisionNormal, this.GlobalTransform.basis.y.Normalized());
        }
    }


}
