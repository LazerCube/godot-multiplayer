namespace Acruxx.Camera;

using Godot;

/// <summary>
/// The camera rig.
/// </summary>
public partial class CameraRig : Node3D
{
    public RayCast3D? AimRay;

    public AimTarget? AimTarget;

    public Camera3D? Camera;

    public SpringArm? SpringArm;

    public CharacterBody3D? Player;

    public Vector3 PositionStart;

    private float _zoom = 0.5f;

    /// <summary>
    /// Gets or sets the zoom.
    /// </summary>
    public float Zoom
    {
        get { return this._zoom; }
        set
        {
            this._zoom = Mathf.Clamp(value, 0.0f, 1.0f);
            this.SpringArm!.Zoom = this._zoom;
        }
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        this.AimTarget = this.GetNode<AimTarget>("AimTarget");
        if (this.AimTarget is null) GD.PushError("AimTarget reference not found!");

        this.AimRay = this.GetNode<RayCast3D>("InterpolatedCamera/AimRay");
        if (this.AimRay is null) GD.PushError("AimRay reference not found!");

        this.Camera = this.GetNode<Camera3D>("InterpolatedCamera");
        if (this.Camera is null) GD.PushError("InterpolatedCamera reference not found!");

        this.SpringArm = this.GetNode<SpringArm>("SpringArm");
        if (this.SpringArm is null) GD.PushError("SpringArm reference not found!");

        this.PositionStart = this.Transform.origin;

        // node transformations only in Global Space
        this.TopLevel = true;

        this.WaitForParentNode();
    }

    /// <summary>
    /// Waits the for parent node.
    /// </summary>
    private async void WaitForParentNode()
    {
        await this.ToSignal(this.Owner, "ready");
        this.Player = (CharacterBody3D)this.Owner;
    }
}
