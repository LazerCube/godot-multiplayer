using Dotplay.Game;
using Godot;

namespace Dotplay;

/// <summary>
/// The base component interface required for root components
/// </summary>
public interface IBaseComponent
{
    /// <summary>
    /// Handle the tree events of godot node tree
    /// </summary>
    public delegate void TreeEnteredEventHandler();

    /// <summary>
    /// Add an child to the base component
    /// </summary>
    /// <param name="node"></param>
    /// <param name="legibleUniqueName"></param>
    /// <param name="internal"></param>
    public void AddChild(Node node, bool legibleUniqueName = false, Node.InternalMode @internal = Node.InternalMode.Disabled);

    /// <summary>
    /// Return the childrens of an component
    /// </summary>
    /// <param name="includeInternal"></param>
    public Godot.Collections.Array<Node> GetChildren(bool includeInternal = false);

    /// <summary>
    /// Check if the component is already inside an tree
    /// </summary>
    public bool IsInsideTree();

    /// <summary>
    /// Delete an child form the base component
    /// </summary>
    /// <param name="path"></param>
    public void RemoveChild(Node path);
}

/// <summary>
/// The child component interface for childs of an base component
/// </summary>
public interface IChildComponent<T> where T : IBaseComponent
{
    /// <summary>
    /// The bas component
    /// </summary>
    public T BaseComponent { get; set; }

    /// <summary>
    /// Check if the child is inside the engine tree
    /// </summary>
    public bool IsInsideTree();
}

/// <summary>
/// Interface required for player components
/// </summary>
public interface IPlayerComponent : IChildComponent<NetworkCharacter>
{
    /// <summary>
    /// If the component is enabled or not
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The component id for network transfer
    /// </summary>
    public short NetworkId { get; set; }

    /// <summary>
    /// Called on each physics network tick for component
    /// </summary>
    /// <param name="delta"></param>
    public void Tick(float delta);
}

/// <inheritdoc />
public static class IPlayerComponentExtensions
{
    /// <inheritdoc />
    public static bool IsLocal(this IPlayerComponent client)
    {
        return client.BaseComponent.IsLocal();
    }

    /// <inheritdoc />
    public static bool IsPuppet(this IPlayerComponent client)
    {
        return client.BaseComponent.IsPuppet();
    }

    /// <inheritdoc />
    public static bool IsServer(this IPlayerComponent client)
    {
        return client.BaseComponent.IsServer();
    }
}