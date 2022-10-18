using System.Collections.Generic;
using Acruxx.Game.Common.StateMachine;
using Godot;

namespace Acruxx.Game.Player.States;

/// <summary>
/// The idle state.
/// </summary>
public partial class Idle : PlayerState
{
    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc />
    public override void UnhandledInput(InputEvent @event)
    {
        (this._parent as Move)!.UnhandledInput(@event);
    }

    /// <inheritdoc />
    public override void PhysicsProcess(double delta)
    {
        (this._parent as Move)!.PhysicsProcess(delta);

        if (this._player!.IsOnFloor() && (this._parent as Move)!.Velocity.Length() > 0.01)
        {
            (this._stateMachine as StateMachine)!.TransitionTo("Move/Run");
        }
        else if (!this._player!.IsOnFloor())
        {
            (this._stateMachine as StateMachine)!.TransitionTo("Move/Air");
        }
    }

    /// <inheritdoc />
    public override void Enter(Dictionary<string, object>? msg = null)
    {
        if (this._parent is null)
        {
            return;
        }

        (this._parent as Move)!.Velocity = Vector3.Zero;
        this._skin!.TransitionTo(Mannequiny.States.IDLE);
        this._parent.Enter(msg);
    }

    /// <inheritdoc />
    public override void Exit()
    {
        (this._parent as Move)!.Exit();
    }
}