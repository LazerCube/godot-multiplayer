using Acruxx.Game.Common.StateMachine;
using Godot;

namespace Acruxx.Game.Player;

/// <summary>
/// The player state.
/// </summary>
public partial class PlayerState : State
{
    protected Player? _player;
    protected Mannequiny? _skin;

    /// <inheritdoc />
    public override void _Ready()
    {
        base._Ready();
        this.WaitForParentNode();
    }

    /// <summary>
    /// Waits the for parent node to be ready.
    /// </summary>
    private async void WaitForParentNode()
    {
        await this.ToSignal(this.Owner, "ready");

        this._player = (Player)this.Owner;

        this._skin = ((Player)this.Owner).Skin;

        if (this._player == null)
        {
            GD.PushError("_player reference is null!");
        }

        if (this._skin == null)
        {
            GD.PushError("_player.skin reference is null!");
        }
    }
}