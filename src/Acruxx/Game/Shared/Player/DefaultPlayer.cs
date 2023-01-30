using Dotplay.Game;
using Dotplay.Network;
using Dotplay.Network.Commands;
using Godot;

namespace Acruxx.Shared.Player;

/// <summary>
/// The default player.
/// </summary>
public partial class DefaultPlayer : NetworkCharacter
{
    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToPuppet)]
    public Vector2 AnimationBlendPosition = Vector2.Zero;

    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToPuppet)]
    public float AnimationMoveSpeed;

    [NetworkVar(NetworkSyncFrom.FromServer, NetworkSyncTo.ToClient | NetworkSyncTo.ToPuppet)]
    public float HP;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!this.IsPuppet())
        {
            this.AnimationMoveSpeed = this.MovementProcessor.GetMovementSpeedFactor();
            this.AnimationBlendPosition = new Vector2(this.MovementProcessor.ForwardBackwardAxis,
            this.MovementProcessor.LeftRightAxis);
        }

        if (this.IsLocal())
        {
            var camera = this.Components.Get<CharacterCamera>();
            if (camera == null)
            {
                return;
            }
        }
    }

    /// <inheritdoc />
    public override PlayerState Interpolate(float theta, PlayerState lastState, PlayerState nextState)
    {
        var currentInterpolation = base.Interpolate(theta, lastState, nextState);

        var aPos = lastState.GetVar<Vector2>(this, "AnimationBlendPosition");
        var bPos = nextState.GetVar<Vector2>(this, "AnimationBlendPosition");

        var aSpeedPos = lastState.GetVar<float>(this, "AnimationMoveSpeed");
        var bSpeedPos = nextState.GetVar<float>(this, "AnimationMoveSpeed");

        currentInterpolation.SetVar(this, "AnimationBlendPosition", aPos.Slerp(bPos, theta));
        currentInterpolation.SetVar(this, "AnimationMoveSpeed", Mathf.Lerp(aSpeedPos, bSpeedPos, theta));

        var ahp = lastState.GetVar<float>(this, "HP");
        var bhp = nextState.GetVar<float>(this, "HP");

        currentInterpolation.SetVar(this, "HP", Mathf.Lerp(ahp, bhp, theta));

        return currentInterpolation;
    }

    /// <summary>
    /// Sets the shadow modes.
    /// </summary>
    /// <param name="t">The t.</param>
    /// <param name="mode">The mode.</param>
    private void SetShadowModes(Node t, GeometryInstance3D.ShadowCastingSetting mode)
    {
        if (t is MeshInstance3D meshInstance3D)
        {
            meshInstance3D.CastShadow = mode;
        }

        foreach (var element in t.GetChildren())
        {
            if (element is not null)
            {
                this.SetShadowModes(element, mode);
            }
        }
    }
}