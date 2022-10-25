namespace Acruxx.Camera;

using Acruxx.Common.StateMachine;

/// <summary>
/// The camera state.
/// </summary>
public partial class CameraState : State
{
    protected CameraRig? _cameraRig;

    /// <summary>
    /// _S the ready.
    /// </summary>
    public override void _Ready()
    {
        base._Ready();
        this.WaitForParentNode();
        this._cameraRig = (CameraRig)this.Owner;
    }

    /// <summary>
    /// Waits the for parent node.
    /// </summary>
    private async void WaitForParentNode()
    {
        await this.ToSignal(this.Owner, "ready");
    }
}