using Godot;

namespace Acruxx.Client.UI.Ingame;

/// <summary>
/// The key confirmation dialog.
/// </summary>
public partial class KeyConfirmationDialog : ConfirmationDialog
{
    private bool _isSelected = false;

    /// <summary>
    /// Gets the key name.
    /// </summary>
    public string KeyName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the selected key.
    /// </summary>
    public string SelectedKey { get; private set; } = string.Empty;

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (!this._isSelected)
        {
            // Receives key input
            if (@event is InputEventKey eventKey)
            {
                this.SelectedKey = "KEY_" + eventKey.Keycode;
                this.DialogText = "Current selected key is " + eventKey.Keycode.ToString() + ". Please press apply to confirm.";
                this._isSelected = true;
            }

            if (@event is InputEventMouseButton eventMouseButton)
            {
                this.SelectedKey = "BTN_" + eventMouseButton.ButtonIndex;
                this.DialogText = "Current selected button is " + eventMouseButton.ButtonIndex.ToString() + ". Please press apply to confirm.";
                this._isSelected = true;
            }
        }

        @event.Dispose();
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        this.GetOkButton().FocusMode = Control.FocusModeEnum.None;
        this.GetCancelButton().FocusMode = Control.FocusModeEnum.None;
    }

    /// <summary>
    /// Opens the changer.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    public void OpenChanger(string keyName)
    {
        this.KeyName = keyName;
        this.DialogText = "Press a key to continue...";
        this.PopupCentered();
    }
}