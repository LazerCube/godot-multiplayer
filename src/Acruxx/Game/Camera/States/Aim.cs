using Acruxx.Common.StateMachine;
using Godot;

namespace Acruxx.Camera.States;

/// <summary>
/// The aim.
/// </summary>
public partial class Aim : CameraState
{
    [Export]
    private float _fov = 90.0f;

    [Export]
    private Vector3 _offsetCamera = new(0.75f, -0.7f, 0);

    [Export]
    private float _tweenTime = 0.5f;

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc />
    public override void Enter(System.Collections.Generic.Dictionary<string, object>? msg = null)
    {
        (this._parent as Camera)!.IsAiming = true;
        this._cameraRig!.AimTarget!.Visible = true;

        this._cameraRig!.SpringArm!.Position = this._cameraRig.PositionStart + this._offsetCamera;

        var tween = this.CreateTween();
        tween.TweenProperty(this._cameraRig.Camera, "fov", this._fov, this._tweenTime)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <inheritdoc />
    public override void Exit()
    {
        (this._parent as Camera)!.IsAiming = false;
        this._cameraRig!.AimTarget!.Visible = false;

        this._cameraRig!.SpringArm!.Position = this._cameraRig.SpringArm.PositionStart;

        var tween = this.CreateTween();
        tween.TweenProperty(this._cameraRig.Camera, "fov", (this._parent as Camera)!.FovDefault, this._tweenTime)
            .SetTrans(Tween.TransitionType.Quart)
            .SetEase(Tween.EaseType.InOut);
    }

    /// <inheritdoc />
    public override void Process(double delta)
    {
        this._parent!.Process(delta);
        this._cameraRig!.AimTarget!.Update(this._cameraRig.AimRay!);
    }

    /// <inheritdoc />
    public override void UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_aim"))
        {
            (this._stateMachine as StateMachine)!.TransitionTo("Camera/Default");
        }
        else
        {
            this._parent!.UnhandledInput(@event);
        }
    }
}