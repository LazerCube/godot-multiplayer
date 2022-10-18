using Godot;

namespace Acruxx.Game;

/// <summary>
/// The game.
/// </summary>
public partial class Game : Node
{
    /// <inheritdoc />
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        // Toggle Cursor mode on click on window or escape key

        if (@event.IsActionPressed("click") && Input.MouseMode is not Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        if (@event.IsActionPressed("toggle_mouse_captured"))
        {
            if (Input.MouseMode is Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }

            // no other input function should get this
            this.GetViewport().SetInputAsHandled();
        }
    }
}