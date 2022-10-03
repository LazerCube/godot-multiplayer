namespace Acruxx.Camera;

using Godot;

/// <summary>
/// The interpolated camera3 d.
/// </summary>
public partial class InterpolatedCamera3D2 : Camera3D
{
    [Export(PropertyHint.Range, "0, 1, 0.001")]
    private float _translateSpeed = 0.95f;

    [Export(PropertyHint.Range, "0, 1, 0.001")]
    private float _rotateSpeed = 0.95f;

    [Export]
    private NodePath? _targetPath;

    private Node3D? _target;

    /// <inheritdoc />
    public override void _Ready()
    {
        if (this._targetPath != null)
        {
            this._target = (Node3D)this.GetNode(this._targetPath);
        }
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        if (this._targetPath is null)
        {
            return;
        }
      
        var translateFactor = this._translateSpeed * delta * 10;
        var rotateFactor = this._rotateSpeed * delta * 10;
        var target_xform = this._target!.GlobalTransform;

        var localTransformOnlyOrigin = new Transform3D(
            Basis.Identity,
            this.Transform.origin);
        var localTransformOnlyBasic = new Transform3D(
            this.Transform.basis,
            Vector3.Zero);

        localTransformOnlyOrigin = localTransformOnlyOrigin.InterpolateWith(target_xform, (float)translateFactor);
        localTransformOnlyBasic = localTransformOnlyBasic.InterpolateWith(target_xform, (float)rotateFactor);

        this.GlobalTransform = new Transform3D(
            localTransformOnlyBasic.basis,
            localTransformOnlyOrigin.origin);
    }
}
