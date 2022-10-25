using Dotplay.Extensions;
using Dotplay.Game;
using Dotplay.Input;
using Godot;

namespace Dotplay.Physics;

/// <summary>
/// The required interface for movement processors
/// </summary>
public interface IMovementProcessor
{
    /// <summary>
    /// Get the forward backward axis
    /// </summary>
    public float ForwardBackwardAxis { get; }

    /// <summary>
    /// Get the left right axis
    /// </summary>
    public float LeftRightAxis { get; }

    /// <summary>
    /// The current velocity of the moveable object
    /// </summary>
    public Vector3 Velocity { get; set; }

    /// <summary>
    /// Get maximium speed
    /// </summary>
    public float GetMovementSpeedFactor();

    /// <summary>
    /// Get the walking speed
    /// </summary>
    public float GetWalkingSpeed();

    /// <summary>
    /// Set server related vars
    /// </summary>
    /// <param name="vars"></param>
    public void SetServerVars(VarsCollection vars);

    /// <summary>
    /// Calls on each tick for produce movement
    /// </summary>
    /// <param name="component"></param>
    /// <param name="inputs"></param>
    /// <param name="dt"></param>
    public Vector3 Simulate(NetworkCharacter component, GeneralPlayerInput inputs, float dt);
}

/// <summary>
/// An default movement calculator
/// Handles friction, air control, jumping and accelerate
/// </summary>
public class DefaultMovementProcessor : IMovementProcessor
{
    private NetworkCharacter _component;
    private IPlayerInput _inputs;
    private VarsCollection _serverVars;
    private bool _wishJump;
    private float _wishspeed2;

    /// <summary>
    /// Get forward and back axis (currently use input keys "Forward" and "Back")
    /// </summary>
    public virtual float ForwardBackwardAxis
    {
        get
        {
            return this._inputs == null ? 0 : this._inputs.GetInput("Back") ? 1f : this._inputs.GetInput("Forward") ? -1f : 0f;
        }
    }

    /// <summary>
    /// Get left and right axis (currently use input keys "Forward" and "Back")
    /// </summary>
    public virtual float LeftRightAxis
    {
        get
        {
            return this._inputs == null ? 0 : this._inputs.GetInput("Right") ? 1f : this._inputs.GetInput("Left") ? -1f : 0f;
        }
    }

    /// <inheritdoc />
    public Vector3 Velocity { get; set; } = Vector3.Zero;

    /// <summary>
    /// Check if player allows to crouch
    /// </summary>
    public virtual bool CanCrouch()
    {
        return this._serverVars.Get("sv_crouching", true);
    }

    /// <summary>
    /// Get the default air accel factor
    /// </summary>
    public virtual float GetAirAcceleration()
    {
        return this._serverVars.Get("sv_air_accel", 2f);
    }

    /// <summary>
    /// Get the current air control value
    /// </summary>
    public virtual float GetAirControl()
    {
        return this._serverVars.Get("sv_air_control", 0.3f);
    }

    /// <summary>
    /// Get the default air deaccel factor
    /// </summary>
    public virtual float GetAirDecceleration()
    {
        return this._serverVars.Get("sv_air_deaccel", 2f);
    }

    /// <summary>
    /// Get the default gravity
    /// </summary>
    public virtual float GetGravity()
    {
        return this._serverVars.Get("sv_gravity", 20f);
    }

    /// <summary>
    /// Get the default ground accel factor
    /// </summary>
    public virtual float GetGroundAccelerationFactor()
    {
        return ((this._inputs.GetInput("Crouch") && this.CanCrouch())
                || this._inputs.GetInput("Shifting"))
              ? this._serverVars.Get("sv_crouching_accel", 20.0f) : this._serverVars.Get("sv_walk_accel", 7.5f);
    }

    /// <summary>
    /// Get the default ground deaccel factor
    /// </summary>
    public virtual float GetGroundDeaccelerationFactor()
    {
        return ((this._inputs.GetInput("Crouch") && this.CanCrouch())
                || this._inputs.GetInput("Shifting"))
              ? this._serverVars.Get("sv_crouching_deaccel", 14.0f) : this._serverVars.Get("sv_walk_deaccel", 10f);
    }

    /// <summary>
    /// Get the friction for crouching
    /// </summary>
    public virtual float GetGroundFriction()
    {
        return ((this._inputs.GetInput("Crouch") && this.CanCrouch())
                || this._inputs.GetInput("Shifting"))
              ? this._serverVars.Get("sv_crouching_friction", 3.0f) : this._serverVars.Get("sv_walk_friction", 6.0f);
    }

    /// <summary>
    /// Get the default movement speed
    /// </summary>
    /// <returns></returns>
    public virtual float GetMovementSpeed()
    {
        return ((this._inputs.GetInput("Crouch") && this.CanCrouch())
                || this._inputs.GetInput("Shifting"))
              ? this._serverVars.Get("sv_crouching_speed", 4.0f) : this.GetWalkingSpeed();
    }

    /// <summary>
    /// Get the movement speed factor (current speed / walking speed)
    /// </summary>
    public virtual float GetMovementSpeedFactor()
    {
        var vel = this.Velocity;
        vel.y = 0;
        return vel.Length().SafeDivision(this.GetWalkingSpeed());
    }

    /// <summary>
    /// Get the default walking speed
    /// </summary>
    public virtual float GetWalkingSpeed()
    {
        return this._serverVars == null ? 0 : this._serverVars.Get("sv_walk_speed", 7.0f);
    }

    /// <summary>
    /// Check if player is on ground
    /// </summary>
    public bool IsOnGround()
    {
        return this._component.IsOnGround();
    }

    /// <summary>
    /// The maximum air speed
    /// </summary>
    public virtual float MaxAirSpeed()
    {
        return this._serverVars.Get("sv_max_air_speed", 1.3f);
    }

    /// <inheritdoc />
    public void SetServerVars(VarsCollection vars)
    {
        this._serverVars = vars;
    }

    /// <inheritdoc />
    public Vector3 Simulate(NetworkCharacter component, GeneralPlayerInput inputs, float dt)
    {
        if (component == null)
        {
            return Vector3.Zero;
        }

        this._inputs = inputs;
        this._component = component;

        //Set rotation
        var comp = this._component.Transform;
        comp.basis = new Basis(new Vector3(0, inputs.ViewDirection.y, 0));
        this._component.Transform = comp;

        // Process movement.
        this.QueueJump();

        if (component.IsOnGround())
        {
            this.Velocity = this.GroundMove(dt, this.Velocity);
        }
        else if (!component.IsOnGround())
        {
            this.Velocity = this.AirMove(dt, this.Velocity);
        }

        float couchLevel = 1.0f;
        if (this.CanCrouch())
        {
            couchLevel = dt * (inputs.GetInput("Crouch") ?
                this._serverVars.Get("sv_crouching_down_speed", 8.0f) :
                this._serverVars.Get("sv_crouching_up_speed", 4.0f));
        }

        component.SetCrouchingLevel(inputs.GetInput("Crouch") ? couchLevel * -1 : couchLevel);
        component.Move(this.Velocity);

        // HACK: Reset to zero when falling off the edge for now.
        if (this._component.GlobalTransform.origin.y < -100)
        {
            var gt = this._component.GlobalTransform;
            gt.origin = Vector3.Zero;
            this._component.GlobalTransform = gt;
            this._component.Velocity = Vector3.Zero;
            this.Velocity = Vector3.Zero;
        }

        return this.Velocity;
    }

    /// <summary>
    /// Accelerates the.
    /// </summary>
    /// <param name="velocity">The velocity.</param>
    /// <param name="wishdir">The wishdir.</param>
    /// <param name="wishspeed">The wishspeed.</param>
    /// <param name="accel">The accel.</param>
    /// <param name="dt">The dt.</param>
    /// <returns>A Vector3.</returns>
    internal static Vector3 Accelerate(Vector3 velocity, Vector3 wishdir, float wishspeed, float accel, float dt)
    {
        // speed you want - current speed.
        var addspeed = wishspeed - velocity.Dot(wishdir);

        if (addspeed <= 0)
        {
            return velocity;
        }

        var accelspeed = Mathf.Min(accel * dt * wishspeed, addspeed);

        if (accelspeed > addspeed)
        {
            accelspeed = addspeed;
        }

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;

        return velocity;
    }

    /// <summary>
    /// Airs the control.
    /// </summary>
    /// <param name="velocity">The velocity.</param>
    /// <param name="wishdir">The wishdir.</param>
    /// <param name="wishspeed">The wishspeed.</param>
    /// <param name="dt">The dt.</param>
    /// <returns>A Vector3.</returns>
    internal Vector3 AirControl(Vector3 velocity, Vector3 wishdir, float wishspeed, float dt)
    {
        // Can't control movement if not moving forward or backward
        if (Mathf.Abs(this.ForwardBackwardAxis) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
        {
            return velocity;
        }

        var zspeed = velocity.y;
        velocity.y = 0;

        /* Next two lines are equivalent to idTech's VectorNormalize() */
        var speed = velocity.Length();
        velocity = velocity.Normalized();

        var dot = velocity.Dot(wishdir);
        var k = 32f;
        k *= this.GetAirControl() * dot * dot * dt;

        // Change direction while slowing down
        if (dot > 0)
        {
            velocity.x = (velocity.x * speed) + (wishdir.x * k);
            velocity.y = (velocity.y * speed) + (wishdir.y * k);
            velocity.z = (velocity.z * speed) + (wishdir.z * k);

            velocity = velocity.Normalized();
        }

        velocity.x *= speed;
        velocity.y = zspeed; // Note this line
        velocity.z *= speed;

        return velocity;
    }

    /// <inheritdoc />
    internal Vector3 AirMove(float dt, Vector3 velocity)
    {
        var wishdir = this._component.GlobalTransform.basis.x * this.LeftRightAxis;
        wishdir += this._component.GlobalTransform.basis.z * this.ForwardBackwardAxis;

        float wishspeed = wishdir.Length();
        wishspeed *= this.GetMovementSpeed();

        if (wishspeed > this.MaxAirSpeed())
        {
            wishspeed = this.MaxAirSpeed();
        }

        wishdir = wishdir.Normalized();

        // CPM: Aircontrol
        this._wishspeed2 = wishspeed;

        var accel = velocity.Dot(wishdir) < 0 ? this.GetAirDecceleration() : this.GetAirAcceleration();

        // If the player is ONLY strafing left or right
        if (this.ForwardBackwardAxis == 0 && this.LeftRightAxis != 0)
        {
            if (wishspeed > this._serverVars.Get("sv_strafe_speed", 1.0f))
            {
                wishspeed = this._serverVars.Get("sv_strafe_speed", 1.0f);
            }

            accel = this._serverVars.Get("sv_strafe_accel", 50.0f);
        }

        velocity = Accelerate(velocity, wishdir, wishspeed, accel, dt);

        if (this.GetAirControl() > 0)
        {
            velocity = this.AirControl(velocity, wishdir, this._wishspeed2, dt);
        }

        velocity.y -= this.GetGravity() * dt;

        return velocity;
    }

    /// <summary>
    /// Applies the friction.
    /// </summary>
    /// <param name="velocity">The velocity.</param>
    /// <param name="t">The t.</param>
    /// <param name="dt">The dt.</param>
    /// <param name="yAffected">If true, y affected.</param>
    /// <returns>A Vector3.</returns>
    internal Vector3 ApplyFriction(Vector3 velocity, float t, float dt, bool yAffected = true)
    {
        Vector3 vec = velocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.Length();
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (this._component.IsOnGround())
        {
            var deaccl = this.GetGroundDeaccelerationFactor();
            control = speed < deaccl ? deaccl : speed;
            var friction = this.GetGroundFriction();
            drop = control * friction * dt * t;
        }

        newspeed = Mathf.Max(speed - drop, 0f);
        if (speed > 0.0f)
        {
            newspeed /= speed;
        }

        velocity.x *= newspeed;
        if (yAffected)
        {
            velocity.y *= newspeed;
        }

        velocity.z *= newspeed;

        return velocity;
    }

    /// <summary>
    /// Grounds the move.
    /// </summary>
    /// <param name="dt">The dt.</param>
    /// <param name="velocity">The velocity.</param>
    /// <returns>A Vector3.</returns>
    internal Vector3 GroundMove(float dt, Vector3 velocity)
    {
        // Do not apply friction if the player is queueing up the next jump
        velocity = !this._wishJump ? this.ApplyFriction(velocity, 1.0f, dt) : this.ApplyFriction(velocity, 0, dt);

        var wishdir = this._component.GlobalTransform.basis.x * this.LeftRightAxis;
        wishdir += this._component.GlobalTransform.basis.z * this.ForwardBackwardAxis;
        wishdir = wishdir.Normalized();

        var wishspeed = wishdir.Length();
        wishspeed *= this.GetMovementSpeed();

        velocity = Accelerate(velocity, wishdir, wishspeed, this.GetGroundAccelerationFactor(), dt);

        // Reset the gravity velocity
        velocity.y = -this.GetGravity() * dt;

        if (this._wishJump)
        {
            velocity.y = this._serverVars.Get("sv_jumpspeed", 8f);
            this._wishJump = false;
        }

        return velocity;
    }

    /// <summary>
    /// Queues the jump.
    /// </summary>
    internal void QueueJump()
    {
        this._wishJump = this._inputs.GetInput("Jump");
    }
}