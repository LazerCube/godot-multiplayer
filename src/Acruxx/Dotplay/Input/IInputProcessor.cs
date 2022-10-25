using System.Collections.Generic;

namespace Dotplay.Input;

/// <summary>
/// Required interface for local players with input eg. shifting, crouching, moving, etc.
/// </summary>
public interface IInputProcessor : IPlayerComponent
{
    /// <summary>
    /// List of avaiable input keys
    /// </summary>
    public List<string> AvaiableInputs { get; }

    /// <summary>
    /// Last input storage
    /// </summary>
    public GeneralPlayerInput LastInput { get; }

    /// <summary>
    /// The current player input for the actualy local tick
    /// </summary>
    public GeneralPlayerInput GetInput();

    /// <summary>
    /// Sets the player inputs.
    /// </summary>
    /// <param name="inputs">The inputs.</param>
    public void SetPlayerInputs(GeneralPlayerInput inputs);

    /// <summary>
    /// List of all pressed or unpressed keys
    /// </summary>
    /// <returns></returns>
    /// public Dictionary<string, bool> GetKeys();
}