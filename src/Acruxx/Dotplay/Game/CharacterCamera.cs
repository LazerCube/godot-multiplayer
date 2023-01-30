using Dotplay.Game.Client;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// The camera mode for the player camera
/// </summary>
public enum CameraMode
{
    /// <summary>
    /// FPS Mode
    /// </summary>
    FPS,

    /// <summary>
    /// TPS Mode
    /// </summary>
    TPS,

    /// <summary>
    /// Follow player mode
    /// </summary>
    Follow,

    /// <summary>
    /// Dungeon camera mode
    /// </summary>
    Dungeon
}

/// <summary>
/// The player camera for an physics player
/// </summary>
public partial class CharacterCamera : Camera3D, IPlayerComponent
{
    public Vector3 FPSCameraOffset = Vector3.Zero;

    /// <summary>
    /// The Camera Distance from the character in TPS Mode
    /// </summary>
    [Export]
    public Vector3 TPSCameraOffset = new(0, 0.5f, 0);

    /// <summary>
    /// The Camera Radis
    /// </summary>
    [Export]
    public float TPSCameraRadius = 1.7f;

    internal float TempPitch = 0.0f;

    internal float TempRotX = 0.0f;

    internal float TempRotY = 0.0f;

    internal float TempRotZ = 0.0f;

    internal float TempYaw = 0.0f;

    /// <summary>
    /// The base component of the child component
    /// </summary>

    public NetworkCharacter BaseComponent { get; set; }

    [Export]
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// The current camera mode in use
    /// </summary>
    /// /// <value></value>
    [Export(PropertyHint.Enum)]
    public CameraMode Mode { get; set; } = CameraMode.TPS;

    /// <inheritdoc />
    public short NetworkId { get; set; } = -5;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();

        var rotation = this.GlobalTransform.basis.GetEuler();

        this.TempRotX = rotation.x;
        this.TempRotY = rotation.y;
        this.TempRotZ = rotation.z;

        this.Current = this.IsEnabled;
        this.TopLevel = true;
        this.FPSCameraOffset = this.Position;
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        this.HandleInput(@event);

        @event.Dispose();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (this.BaseComponent == null)
            return;

        if (this.Current != this.IsEnabled)
        {
            this.Current = (!this.IsPuppet() && this.IsEnabled);
        }

        if (this.IsServer())
        {
            var transform = this.BaseComponent.GlobalTransform;
            var targetPos = this.BaseComponent.GlobalTransform.origin + this.FPSCameraOffset + Vector3.Up * this.BaseComponent.GetShapeHeight();
            transform.origin = targetPos;
            transform.basis = new Basis(new Vector3(this.BaseComponent.CurrentPlayerInput.Inputs.ViewDirection.x, transform.basis.GetEuler().y, transform.basis.GetEuler().z));
            this.GlobalTransform = transform;
        }
        else if (this.Mode == CameraMode.TPS)
        {
            var cam_pos = this.BaseComponent.GlobalTransform.origin + this.TPSCameraOffset;
            if (!this.IsServer())
            {
                cam_pos.x += this.TPSCameraRadius * Mathf.Sin(Mathf.DegToRad(this.TempYaw)) * Mathf.Cos(Mathf.DegToRad(this.TempPitch));
                cam_pos.y += this.TPSCameraRadius * Mathf.Sin(Mathf.DegToRad(this.TempPitch));
                cam_pos.z += this.TPSCameraRadius * Mathf.Cos(Mathf.DegToRad(this.TempYaw)) * Mathf.Cos(Mathf.DegToRad(this.TempPitch));

                this.LookAtFromPosition(cam_pos, this.BaseComponent.GlobalTransform.origin + this.TPSCameraOffset, new Vector3(0, 1, 0));
            }
        }
        else if (this.Mode == CameraMode.FPS)
        {
            var transform = this.BaseComponent.GlobalTransform;

            var target = this.BaseComponent.GlobalTransform.origin + this.FPSCameraOffset + Vector3.Up * this.BaseComponent.GetShapeHeight();
            transform.origin = target;
            transform.basis = new Basis(new Vector3(this.TempRotX, this.TempRotY, 0));
            this.GlobalTransform = transform;
        }

        if (this.IsLocal())
        {
            this.Fov = ClientSettings.Variables.Get<int>("cl_fov");
        }
    }

    /// <summary>
    /// Get the view rotation of an local player
    /// </summary>
    public virtual Vector3 GetViewRotation()
    {
        return this.GlobalTransform.basis.GetEuler();
    }

    /// <summary>
    /// Called on each physics network tick for component
    /// </summary>
    /// <param name="delta"></param>
    public virtual void Tick(float delta)
    {
    }

    /// <inheritdoc />
    internal void HandleInput(InputEvent @event)
    {
        if (this.BaseComponent == null)
            return;

        if (this.IsLocal())
        {
            var sensX = ClientSettings.Variables.Get("cl_sensitivity_y", 2.0f);
            var sensY = ClientSettings.Variables.Get("cl_sensitivity_x", 2.0f);

            if (@event is InputEventMouseMotion && this.IsEnabled)
            {
                // Handle cursor lock state
                if (Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured)
                {
                    var ev = @event as InputEventMouseMotion;
                    this.TempRotX -= ev.Relative.y * (sensY / 100);
                    this.TempRotX = Mathf.Clamp(this.TempRotX, Mathf.DegToRad(-90), Mathf.DegToRad(90));
                    this.TempRotY -= ev.Relative.x * (sensX / 100);

                    this.TempYaw = (this.TempYaw - (ev.Relative.x * (sensX))) % 360;
                    this.TempPitch = Mathf.Max(Mathf.Min(this.TempPitch + (ev.Relative.y * (sensY)), 85), -85);
                }
            }

            //if (@event.IsActionReleased("camera"))
            //{
            //    if (this.Mode == CameraMode.FPS)
            //    {
            //        this.Mode = CameraMode.TPS;
            //    }
            //    else if (this.Mode == CameraMode.TPS)
            //    {
            //        this.Mode = CameraMode.FPS;
            //    }
            //}
        }
    }
}