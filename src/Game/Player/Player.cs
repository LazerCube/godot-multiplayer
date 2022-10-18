namespace Acruxx.Game.Player;

using Acruxx.Game.Camera;
using Acruxx.Game.Common.StateMachine;
using Godot;

/// <summary>
/// The player.
/// </summary>
public partial class Player : CharacterBody3D
{
    public CameraRig? Camera;
    public Mannequiny? Skin;
    public StateMachine? StateMachine;

    /// <inheritdoc />
    public override void _Ready()
    {
        this.Camera = this.GetNode<CameraRig>("CameraRig");
        if (this.Camera is null)
        {
            GD.PushError("CameraRig reference is null!");
        }
        
        this.Skin = this.GetNode<Mannequiny>("Mannequiny");
        if (this.Skin == null)
        {
            GD.PushError("Mannequiny reference is null!");
        }

        this.StateMachine = this.GetNode<StateMachine>("StateMachine");
        if (this.StateMachine is null)
        {
            GD.PushError("stateMachine reference is null!");
        }
    }
}
