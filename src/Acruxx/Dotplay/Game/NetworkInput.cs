using System.Collections.Generic;
using System.Linq;
using Dotplay.Input;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// The character or kinematic 3d body node for an network player
/// </summary>
public partial class NetworkInput : Node3D, IInputProcessor
{
    private bool _lastJump;

    /// <inheritdoc />
    public List<string> AvaiableInputs => this.GetKeys().Keys.ToList();

    /// <inheritdoc />
    public NetworkCharacter BaseComponent { get; set; }

    /// <inheritdoc />
    [Export]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The last player input
    /// </summary>
    public GeneralPlayerInput LastInput { get; private set; }

    /// <inheritdoc />
    public short NetworkId { get; set; } = -7;

    /// <summary>
    /// Get the current player input
    /// </summary>
    public virtual GeneralPlayerInput GetInput()
    {
        var camera = this.BaseComponent.Components.Get<CharacterCamera>();
        var input = new GeneralPlayerInput();
        var activeKeys = new Dictionary<string, bool>();
        if (Godot.Input.MouseMode == Godot.Input.MouseModeEnum.Captured && this.IsEnabled)
        {
            activeKeys = this.GetKeys();
        }

        input.ViewDirection = camera != null ? camera.GetViewRotation() : Vector3.Zero;
        input.Apply(this.AvaiableInputs, activeKeys);

        return input;
    }

    /// <inheritdoc />
    public virtual Dictionary<string, bool> GetKeys()
    {
        var newJump = Client.ClientSettings.Variables.IsKeyValuePressed("key_jump", Key.Space);

        var canJump = false;
        if (newJump && !this._lastJump)
        {
            canJump = true;
        }
        else if (newJump && this._lastJump)
        {
            canJump = false;
        }
        else if (!newJump && this._lastJump)
        {
            canJump = false;
        }

        this._lastJump = newJump;

        return new Dictionary<string, bool>{
            { "Forward", Client.ClientSettings.Variables.IsKeyValuePressed("key_forward", Key.W)},
            { "Back", Client.ClientSettings.Variables.IsKeyValuePressed("key_backward", Key.S)},
            { "Right", Client.ClientSettings.Variables.IsKeyValuePressed("key_right", Key.D)},
            { "Left", Client.ClientSettings.Variables.IsKeyValuePressed("key_left", Key.A)},
            { "Jump", canJump},
            { "Crouch", Client.ClientSettings.Variables.IsKeyValuePressed("key_crouch", Key.Ctrl)},
            { "Shifting", Client.ClientSettings.Variables.IsKeyValuePressed("key_shift", Key.Shift)},
            { "Fire",  Client.ClientSettings.Variables.IsKeyValuePressed("key_attack", MouseButton.Left)},
        };
    }

    /// <summary>
    /// Sets the player inputs.
    /// </summary>
    /// <param name="inputs">The inputs.</param>
    public void SetPlayerInputs(GeneralPlayerInput inputs)
    {
        this.LastInput = inputs;
    }

    /// <inheritdoc />
    public void Tick(float delta)
    { }
}