using Dotplay;
using Dotplay.Game;
using Dotplay.Utils;
using Godot;

namespace Acruxx.Client.UI.Welcome;

/// <summary>
/// The map loading component.
/// </summary>
public partial class MapLoadingComponent : CanvasLayer, IChildComponent<GameLogic>
{
    /// <inheritdoc />
    public GameLogic BaseComponent { get; set; }

    /// <summary>
    /// Gets or sets the path to loading text box.
    /// </summary>
    [Export]
    public NodePath? PathToLoadingTextBox { get; set; }

    /// <summary>
    /// Gets or sets the path to progress bar.
    /// </summary>
    [Export]
    public NodePath? PathToProgressBar { get; set; }

    /// <inheritdoc />
    public override void _EnterTree()
    {
        base._EnterTree();

        var component = this.BaseComponent as IGameLogic;
        AsyncLoader.Loader.OnProgress += this.HandleProcess;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        AsyncLoader.Loader.OnProgress -= this.HandleProcess;
        base._ExitTree();
    }

    /// <summary>
    /// Handles the process.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="process">The process.</param>
    public void HandleProcess(string file, float process)
    {
        this.GetNode<Label>(this.PathToLoadingTextBox).Text = $"Loading: {file}";
        this.GetNode<ProgressBar>(this.PathToProgressBar).Value = process;
    }
}