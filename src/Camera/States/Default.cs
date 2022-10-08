using Acruxx.Common.StateMachine;
using Godot;

namespace Acruxx.Camera.States;

/// <summary>
/// The default.
/// </summary>
public partial class Default : CameraState
{
    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc />
    public override void UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_aim") || @event.IsActionPressed("fire"))
        {
            (this._stateMachine as StateMachine)?.TransitionTo("Camera/Aim");
        }
        else
        {
            this._parent?.UnhandledInput(@event);
        }
    }

    /// <inheritdoc />
    public override void Process(double delta)
    {
        this._parent?.Process(delta);
    }
}