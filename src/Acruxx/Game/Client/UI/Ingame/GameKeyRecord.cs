using Godot;

namespace Acruxx.Client.UI.Ingame;

/// <summary>
/// The game key record.
/// </summary>
public partial class GameKeyRecord : HBoxContainer
{
    private string _currentKey = string.Empty;

    public delegate void NotifyNewKey(string keyName);

    public event NotifyNewKey OnKeyChangeStart;

    /// <summary>
    /// Gets or sets the collection key.
    /// </summary>
    public string CollectionKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current key.
    /// </summary>
    public string CurrentKey
    {
        get
        {
            return this._currentKey;
        }

        set
        {
            this._currentKey = value;

            if (this.GetNode("MovementCurrentKey") != null)
            {
                var labelKey = this.GetNode("MovementCurrentKey") as Label;
                labelKey!.Text = this.CurrentKey;
            }
        }
    }

    /// <inheritdoc/>
    public override void _Ready()
    {
        var label = this.GetNode("MovementLabel") as Label;
        label!.Text = this.Name;

        var button = this.GetNode("ChangeKeyButton") as Button;
        button!.Pressed += () => OnKeyChangeStart(this.Name);
    }
}