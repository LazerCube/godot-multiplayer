using System.Collections.Generic;
using Acruxx.Game.Common.StateMachine;
using Godot;

namespace Acruxx.Game.Player.States;

/// <summary>
/// The air state.
/// </summary>
public partial class Air : PlayerState
{
    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc />
    public override void PhysicsProcess(double delta)
    {
        (this._parent as Move)!.PhysicsProcess(delta);

        if (this._player!.IsOnFloor())
        {
            Vector3 inputDirection = Move.GetInputDirection();
            // See if moving then return after fall to run else idle
            if (inputDirection.Length() > 0.001)
            {
                (this._stateMachine as StateMachine)!.TransitionTo("Move/Run");
            }
            else
            {
                (this._stateMachine as StateMachine)!.TransitionTo("Move/Idle");
            }
        }
    }

    /// <inheritdoc />
    public override void Enter(Dictionary<string, object>? msg)
    {
        if (msg != null)
        {
            // TODO: maybe change to consts or an enum for dictionary keys for easier use
            if (msg.ContainsKey("velocity") && msg.ContainsKey("jumpImpulse"))
            {
                var v = (Vector3)msg["velocity"];
                var j = (float)msg["jumpImpulse"];
                (this._parent as Move)!.Velocity = v + new Vector3(0.0f, j, 0.0f);
            }
        }
        this._skin!.TransitionTo(Mannequiny.States.AIR);
        this._parent!.Enter(msg);
    }

    /// <inheritdoc />
    public override void Exit()
    {
        (this._parent as Move)!.Exit();
    }
}