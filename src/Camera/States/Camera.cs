using Godot;

namespace Acruxx.Camera.States;

/// <summary>
/// The camera.
/// </summary>
public partial class Camera : CameraState
{
    /// <summary>
    /// The zoom step.
    /// </summary>
    private const float ZOOM_STEP = 0.1f;

    [Export]
    public float FovDefault = 70.0f;

    [Export]
    public bool IsYInverted = true;

    [Export]
    public float DeadZoneBackwards = 0.3f;

    [Export]
    public Vector2 SensivityGamePad = new(2.5f, 2.5f);

    [Export]
    public Vector2 SensivityMouse = new(0.1f, 0.1f);

    public bool IsAiming = false;

    Vector2 _input_relative = Vector2.Zero;

    /// <inheritdoc/>
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc/>
    public override void Process(double delta)
    {
        //TODO: is there a way to make this better?

        var transform = this._cameraRig!.Transform;
        transform.origin = this._cameraRig.Player!.GlobalTransform.origin + this._cameraRig.PositionStart;
        this._cameraRig.GlobalTransform = transform;

        Vector2 lookDirection = GetLookDirection();
        Vector3 moveDirection = GetMoveDirection();

        if (this._input_relative.Length() > 0.0f)
        {
            this.UpdateRotation(this._input_relative * this.SensivityMouse * (float)delta);
            this._input_relative = Vector2.Zero;
        }

        if (lookDirection.Length() > 0.0f)
        {
            this.UpdateRotation(lookDirection * this.SensivityGamePad * (float)delta);
        }

        bool isMovingTowardsCamera =
            (moveDirection.x >= -this.DeadZoneBackwards) &&
            (moveDirection.x <= this.DeadZoneBackwards);

        if (!isMovingTowardsCamera && !this.IsAiming)
        {
            this.AutoRotate();
        }

        // prevent winding
        var rot = this._cameraRig.Rotation;
        rot.y = Mathf.Wrap(this._cameraRig.Rotation.y, -Mathf.Pi, Mathf.Pi);
        this._cameraRig.Rotation = rot;
    }

    /// <inheritdoc/>
    public override void UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("zoom_in"))
        {
            this._cameraRig!.Zoom += ZOOM_STEP;
        }
        else if (@event.IsActionPressed("zoom_out"))
        {
            this._cameraRig!.Zoom -= ZOOM_STEP;
        }
        else if ((@event is InputEventMouseMotion) && (Input.MouseMode == Input.MouseModeEnum.Captured))
        {
            this._input_relative += (@event as InputEventMouseMotion)!.Relative;
        }
    }

    /// <summary>
    /// Autos the rotate.
    /// </summary>
    private void AutoRotate()
    {
        float offset = this._cameraRig!.Player!.Rotation.y - this._cameraRig.Rotation.y;
        float targetAngle = this.CalculateTargetAngle(offset);
        var rot = this._cameraRig.Rotation;
        rot.y = Mathf.Lerp(rot.y, targetAngle, 0.015f);
        this._cameraRig.Rotation = rot;
    }

    /// <summary>
    /// Calculates the target angle.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <returns>A float.</returns>
    private float CalculateTargetAngle(float offset)
    {
        return offset > Mathf.Pi
            ? this._cameraRig!.Player!.Rotation.y - (2 * Mathf.Pi)
            : offset < -Mathf.Pi ? this._cameraRig!.Player!.Rotation.y + (2 * Mathf.Pi) : this._cameraRig!.Player!.Rotation.y;
    }

    /// <summary>
    /// Updates the rotation.
    /// </summary>
    /// <param name="offset">The offset.</param>
    private void UpdateRotation(Vector2 offset)
    {
        // left right rotation
        var rot = this._cameraRig!.Rotation;
        rot.y -= offset.x;
        this._cameraRig!.Rotation = rot;

        // up down rotation
        rot.x += (this.IsYInverted ? (offset.y * -1.0f) : offset.y);
        this._cameraRig!.Rotation = rot;

        // limit camera rotation
        rot.x = Mathf.Clamp(rot.x, -0.75f, 1.25f);
        this._cameraRig!.Rotation = rot;

        // not z rotation
        rot.z = 0.0f;
        this._cameraRig!.Rotation = rot;
    }

    /// <summary>
    /// Gets the look direction.
    /// </summary>
    /// <returns>A Vector2.</returns>
    private static Vector2 GetLookDirection()
    {
        return new Vector2(
            Input.GetActionStrength("look_right") - Input.GetActionStrength("look_left"),
            Input.GetActionStrength("look_up") - Input.GetActionStrength("look_down")
        );
    }

    /// <summary>
    /// Gets the move direction.
    /// </summary>
    /// <returns>A Vector3.</returns>
    private static Vector3 GetMoveDirection()
    {
        return new Vector3(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            0.0f,
            Input.GetActionStrength("move_back") - Input.GetActionStrength("move_front")
        );
    }
}