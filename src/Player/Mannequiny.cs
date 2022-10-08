namespace Acruxx.Player;

using Godot;

/// <summary>
/// The mannequiny.
/// </summary>
public partial class Mannequiny : Node3D
{
    /// <summary>
    /// The states.
    /// </summary>
    public enum States
    {
        IDLE = 0,
        RUN = 1,
        AIR = 2,
    }

    private AnimationTree? _animationTree;
    private AnimationNodeStateMachinePlayback? _playback;
    private AnimationPlayer? _animationPlayer;

    private float _playbackSpeed;
    private Vector3 _moveDirection;

    /// <inheritdoc />
    public override void _Ready()
    {
        this._moveDirection = Vector3.Zero;

        this._animationTree = this.GetNode<AnimationTree>("AnimationTree");

        if (this._animationTree is null)
        {
            GD.PushError("AnimationTree reference is null!");
        }
        else
        {
            this._animationTree.Active = true;
            this._playback = (AnimationNodeStateMachinePlayback)this._animationTree.Get("parameters/playback");
        }

        this._animationPlayer = this.GetNode<AnimationPlayer>("AnimationPlayer");

        if (this._animationPlayer == null)
        {
            GD.PushError("AnimationPlayer reference is null!");
        }
    }

    /// <summary>
    /// Sets the playback speed.
    /// </summary>
    /// <param name="speed">The speed.</param>
    public void SetPlaybackSpeed(float speed)
    {
        this._playbackSpeed = speed;
        this._animationPlayer!.PlaybackSpeed = speed;
    }

    /// <summary>
    /// sets the move direction.
    /// </summary>
    /// <param name="direction">The direction.</param>
    public void SetMoveDirection(Vector3 direction)
    {
        this._moveDirection = direction;
        this._animationTree!.Set("parameters/move_ground/blend_position", direction.Length());
    }

    /// <summary>
    /// Transitions the to.
    /// </summary>
    /// <param name="stateID">The state i d.</param>
    public void TransitionTo(States stateID)
    {
        switch (stateID)
        {
            case States.IDLE:
                this._playback!.Travel("idle");
                break;

            case States.RUN:
                this._playback!.Travel("move_ground");
                break;

            case States.AIR:
                this._playback!.Travel("jump");
                break;

            default:
                this._playback!.Travel("idle");
                break;
        }
    }
}