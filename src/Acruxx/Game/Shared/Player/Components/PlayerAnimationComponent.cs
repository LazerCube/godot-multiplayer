using System;
using Acruxx.Shared.Player;
using Dotplay;
using Dotplay.Game;
using Godot;

namespace Acruxx.Game.Shared.Player.Components;

/// <summary>
/// The player animation component.
/// </summary>
public partial class PlayerAnimationComponent : Node, IPlayerComponent
{
    /// <inheritdoc />
    public short NetworkId { get; set; } = 4;

    /// <inheritdoc />
    [Export]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the animation tree path.
    /// </summary>
    [Export]
    public NodePath? AnimationTreePath { get; set; }

    /// <summary>
    /// Gets or sets the animation player path.
    /// </summary>
    [Export]
    public NodePath? AnimationPlayerPath { get; set; }

    /// <summary>
    /// Gets or sets the input path.
    /// </summary>
    [Export]
    public NodePath? InputPath { get; set; }

    /// <summary>
    /// Gets or sets the camera path.
    /// </summary>
    [Export]
    public NodePath? CameraPath { get; set; }

    /// <summary>
    /// The animation states.
    /// </summary>
    public enum AnimationStates
    {
        IDLE = 0,
        RUN = 1,
        AIR = 2,
    }

    /// <inheritdoc />
    public NetworkCharacter BaseComponent { get; set; }

    private AnimationPlayer? _animationPlayer;
    private AnimationTree? _animationTree;
    private AnimationNodeStateMachinePlayback? _playback;
    private CharacterCamera? _characterCamera;
    private NetworkInput? _networkInput;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();

        this._animationTree = this.GetNode<AnimationTree>(this.AnimationTreePath);
        this._animationPlayer = this.GetNode<AnimationPlayer>(this.AnimationPlayerPath);
        this._characterCamera = this.GetNode<CharacterCamera>(this.CameraPath);
        this._networkInput = this.GetNode<NetworkInput>(this.InputPath);

        if (this._animationTree is null || this._animationPlayer is null || this._characterCamera is null || this._networkInput is null)
        {
            throw new Exception("PlayerAnimationComponent is missing references!");
        }

        this._playback = (AnimationNodeStateMachinePlayback)this._animationTree.Get("parameters/playback");

        this._animationTree.Active = true;
    }

    /// <inheritdoc />
    public void Tick(float delta)
    {
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        base._Process(delta);
        var factor = ((DefaultPlayer)this.BaseComponent).AnimationMoveSpeed;
        var blendPosition = ((DefaultPlayer)this.BaseComponent).AnimationBlendPosition;

        if (!this.IsPuppet())
        {
            if (this._networkInput is null || this._characterCamera is null)
            {
                return;
            }

            // Client only animations.
        }

        if (this.BaseComponent.IsOnGround())
        {
            if (blendPosition.Length() > 0.001)
            {
                this._animationPlayer.PlaybackSpeed = factor + 1f;
                this.TransitionTo(AnimationStates.RUN);
            }
            else
            {
                this.TransitionTo(AnimationStates.IDLE);
            }
        }
        else
        {
            this.TransitionTo(AnimationStates.AIR);
        }

        //this._animationTree!.Set("parameters/move_ground/blend_position", blendPosition);
        //this._animationTree!.Set("parameters/move_velocity/blend_position", blendPosition);
        //this._animationTree.Set("parameters/move_speed/scale", 1f + factor);
        //this._animationTree.Set("parameters/move_state/current", factor > 0 ? 1 : 0);
    }

    /// <summary>
    /// Transitions the to.
    /// </summary>
    /// <param name="stateID">The state i d.</param>
    public void TransitionTo(AnimationStates stateID)
    {
        switch (stateID)
        {
            case AnimationStates.IDLE:
                this._playback!.Travel("idle");
                break;

            case AnimationStates.RUN:
                this._playback!.Travel("move_ground");
                break;

            case AnimationStates.AIR:
                this._playback!.Travel("jump");
                break;

            default:
                this._playback!.Travel("idle");
                break;
        }
    }
}
