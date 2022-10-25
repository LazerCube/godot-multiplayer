using System.Collections.Generic;
using Acruxx.Common.StateMachine;
using Godot;

namespace Acruxx.Player.States;

/// <summary>
/// The move state.
/// </summary>
public partial class Move : PlayerState
{
    public Vector3 MoveDirection = Vector3.Zero;

    public float MoveSpeed = 100.0f;

    public Vector3 Velocity = Vector3.Zero;

    [Export]
    private float _gravity = -80.0f;

    [Export]
    private float _jumpImpulse = 25.0f;

    [Export]
    private float _maxSpeed = 50.0f;

    [Export]
    private float _rotationSpeedFactor = 12.0f;

    /// <summary>
    /// Gets the input direction.
    /// </summary>
    /// <returns>A Vector3.</returns>
    public static Vector3 GetInputDirection()
    {
        return new Vector3(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            0.0f,
            Input.GetActionStrength("move_back") - Input.GetActionStrength("move_front")
        );
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
    }

    /// <inheritdoc />
    public override void PhysicsProcess(double delta)
    {
        Vector3 inputDirection = GetInputDirection();

        // only forward direction if player trying to move forward or right
        Vector3 forwards = this._player!.Camera!.GlobalTransform.basis.z * inputDirection.z;
        Vector3 right = this._player.Camera.GlobalTransform.basis.x * inputDirection.x;

        // Get Move Direction relative to the camera
        this.MoveDirection = forwards + right;
        if (this.MoveDirection.Length() > 1.0f)
        {
            this.MoveDirection = this.MoveDirection.Normalized();
        }

        this.MoveDirection.y = 0.0f;
        this._skin!.SetMoveDirection(this.MoveDirection);

        // check if player hits key and rotate
        if (this.MoveDirection.Length() > 0.001)
        {
            var targetDirection = this._player.Transform.LookingAt(this._player.GlobalTransform.origin + this.MoveDirection, Vector3.Up);
            this._player.Transform = this._player.Transform.SphericalInterpolateWith(targetDirection, this._rotationSpeedFactor * (float)delta);
        }

        // move character
        this._player.Velocity = this.CalculateVelocity(this.Velocity, this.MoveDirection, (float)delta);
        this._player.MoveAndSlide();
        this.Velocity = this._player.Velocity;
    }

    /// <inheritdoc />
    public override void UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("jump"))
        {
            var msg = new Dictionary<string, object>
            {
                { "velocity", this.Velocity },
                { "jumpImpulse", this._jumpImpulse }
            };

            (this._stateMachine as StateMachine)!.TransitionTo("Move/Air", msg);
        }
    }

    /// <summary>
    /// Calculates the velocity.
    /// </summary>
    /// <param name="velocityCurrent">The velocity current.</param>
    /// <param name="moveDirection">The move direction.</param>
    /// <param name="delta">The delta.</param>
    /// <returns>A Vector3.</returns>
    private Vector3 CalculateVelocity(Vector3 velocityCurrent, Vector3 moveDirection, float delta)
    {
        Vector3 velocityNew = moveDirection * delta * this.MoveSpeed;
        if (velocityNew.Length() > this._maxSpeed)
        {
            velocityNew = velocityNew.Normalized() * this._maxSpeed;
        }

        // override because start value is 0.0f
        velocityNew.y = velocityCurrent.y + (this._gravity * delta);

        return velocityNew;
    }
}