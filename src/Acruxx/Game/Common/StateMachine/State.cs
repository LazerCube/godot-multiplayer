using System.Collections.Generic;
using Godot;

namespace Acruxx.Common.StateMachine;

/// <summary>
/// The state.
/// </summary>
public partial class State : Node
{
    protected State? _parent;
    protected Node? _stateMachine;

    /// <summary>
    /// Gets or sets a value indicating whether is active.
    /// </summary>
    public bool IsActive
    {
        set
        {
            this.SetIsActive(value);
        }
    }

    /// <inheritdoc />
    public override void _Ready() => this.WaitForParentNode();

    /// <summary>
    /// Enters the.
    /// </summary>
    /// <param name="msg">The message.</param>
    public virtual void Enter(Dictionary<string, object>? msg = null)
    { }

    /// <summary>
    /// Exits the.
    /// </summary>
    public virtual void Exit()
    { }

    /// <summary>
    /// Physics the process.
    /// </summary>
    /// <param name="delta">The delta.</param>
    public virtual void PhysicsProcess(double delta)
    { }

    /// <summary>
    /// Processes the.
    /// </summary>
    /// <param name="delta">The delta.</param>
    public virtual void Process(double delta)
    { }

    /// <summary>
    /// Unhandled input.
    /// </summary>
    /// <param name="event">The event.</param>
    public virtual void UnhandledInput(InputEvent @event)
    { }

    /// <summary>
    /// Gets the state machine.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>A Node.</returns>
    private Node GetStateMachine(Node node)
    {
        var val = node?.IsInGroup("stateMachine") is false ? this.GetStateMachine(node.GetParent()) : node;
        if (val is null)
        {
            GD.PushError("State machine is null!");
        }

        return val!;
    }

    /// <summary>
    /// Sets the is active.
    /// </summary>
    /// <param name="value">If true, value.</param>
    private void SetIsActive(bool value)
    {
        this.IsActive = value;
        this.SetBlockSignals(!value);
    }

    /// <summary>
    /// Waits the for parent node.
    /// </summary>
    private async void WaitForParentNode()
    {
        await this.ToSignal(this.Owner, "ready");

        this._stateMachine = this.GetStateMachine(this);

        var parent = this.GetParent();

        if (parent is null)
        {
            GD.PushError("parent in State is null!");
        }

        if (!parent!.IsInGroup("stateMachine"))
        {
            this._parent = (State)this.GetParent();
        }
    }
}