using System.Collections.Generic;
using Acruxx.Game.Common.StateMachine;
using Godot;

namespace Acruxx.Game.Player.States;

/// <summary>
/// The run state.
/// </summary>
public partial class Run : PlayerState
{
    [Export]
    private float _speedRun = 500.0f;

    [Export]
    private float _speedSprint = 800.0f;

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc />
    public override void UnhandledInput(InputEvent @event)
    {
        this._parent!.UnhandledInput(@event);
    }

    /// <inheritdoc />
    public override void PhysicsProcess(double delta)
    {
        var moveState = (Move)this._parent!;
        var stateMachine = (StateMachine)this._stateMachine!;

        moveState.MoveSpeed = Input.IsActionPressed("sprint") ? this._speedSprint : this._speedRun;
        this._player!.Skin!.SetPlaybackSpeed(moveState.MoveSpeed / this._speedRun);

        this._parent!.PhysicsProcess(delta);

        if (this._player!.IsOnFloor() || this._player.IsOnWall())
        {
            if (moveState!.Velocity.Length() < 0.01)
            {
                stateMachine.TransitionTo("Move/Idle");
            }
        }
        else
        {
            stateMachine.TransitionTo("Move/Air");
        }
    }

    /// <inheritdoc />
    public override void Enter(Dictionary<string, object>? msg = null)
    {
        this._skin!.TransitionTo(Mannequiny.States.RUN);
        this._parent!.Enter(msg);
    }

    /// <inheritdoc />
    public override void Exit()
    {
        this._parent!.Exit();
    }
}