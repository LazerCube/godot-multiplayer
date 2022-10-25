using System;
using Godot;

namespace Acruxx;

/// <summary>
/// The bootloader.
/// </summary>
public partial class Bootloader : Node
{
    [Export(PropertyHint.File, "*.tscn")]
    public string? ClientLogicScenePath;

    [Export(PropertyHint.File, "*.tscn")]
    public string? ServerLogicScenePath;

    /// <inheritdoc />
    public override void _EnterTree()
    {
        if (this.ServerLogicScenePath is null || this.ClientLogicScenePath is null)
        {
            throw new Exception("ClientLogicScenePath or ServerLogicScenePath is null");
        }

        base._EnterTree();
    }
}