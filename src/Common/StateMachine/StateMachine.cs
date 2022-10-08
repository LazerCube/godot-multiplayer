using System.Collections.Generic;
using Godot;

namespace Acruxx.Common.StateMachine;

/// <summary>
/// The state machine.
/// </summary>
public partial class StateMachine : Node
{
    [Signal]
    public delegate void TransitionedEventHandler(NodePath statePath);

    [Export]
    public NodePath? InitialState;

    private State? _state;

    // private string _stateName = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateMachine"/> class.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public StateMachine()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        this.AddToGroup("stateMachine");
    }

    /// <summary>
    /// Gets or sets a value indicating whether is active.
    /// </summary>
    public bool IsActive
    {
        get
        {
            return this.IsActive;
        }
        set
        {
            this.SetIsActive(value);
        }
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        this.WaitForParentNode();
        this._state = (State)this.GetNode(this.InitialState);
    }

    /// <summary>
    /// Unhandled input.
    /// </summary>
    /// <param name="event">The event.</param>
    public override void _UnhandledInput(InputEvent @event)
    {
        this._state!.UnhandledInput(@event);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        this._state!.Process(delta);
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        this._state!.PhysicsProcess(delta);
    }

    /// <summary>
    /// Transitions the to.
    /// </summary>
    /// <param name="targetStatePath">The target state path.</param>
    /// <param name="msg">The message.</param>
    public void TransitionTo(string targetStatePath, Dictionary<string, object>? msg = null)
    {
        if (!this.HasNode(targetStatePath))
        {
            return;
        }

        State targetState = (State)this.GetNode(targetStatePath);

        this._state!.Exit();
        this._state = targetState;
        this._state.Enter(msg);
        //this.EmitSignal(nameof(TransitionedEventHandler), targetStatePath);
    }

    /// <summary>
    /// Sets the state.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetState(State value)
    {
        this._state = value;
        //this._stateName = this._state.Name;
    }

    /// <summary>
    /// Waits the for parent node.
    /// </summary>
    private async void WaitForParentNode()
    {
        await this.ToSignal(this.Owner, "ready");

        if (this._state is null)
        {
            GD.PushError("Initial state in state machine is null!");
        }

        this._state!.Enter();
    }

    /// <summary>
    /// Sets the is active.
    /// </summary>
    /// <param name="value">If true, value.</param>
    private void SetIsActive(bool value)
    {
        this.IsActive = value;
        this.SetProcess(value);
        this.SetPhysicsProcess(value);
        this.SetProcessUnhandledInput(value);
        this._state!.IsActive = value;
    }
}