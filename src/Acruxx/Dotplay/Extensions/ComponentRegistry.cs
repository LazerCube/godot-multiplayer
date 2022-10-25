using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Dotplay;

/// <summary>
/// Component registry class
/// </summary>
public class ComponentRegistry<T> where T : IBaseComponent
{
    private readonly T _baseComponent;

    // required for fix multiple loads in async
    private readonly List<string> _onHold = new();

    /// <summary>
    /// Create a component registry by given base component
    /// </summary>
    /// <param name="baseComponent">The base component</param>
    public ComponentRegistry(T baseComponent)
    {
        this._baseComponent = baseComponent;
    }

    /// <summary>
    /// The event handler for adding components
    /// </summary>
    /// <param name="component"></param>
    public delegate void OnComponentAddedHandler(Node component);

    public event OnComponentAddedHandler? OnComponentAdded;

    /// <summary>
    /// Get all avaiable components
    /// </summary>
    public Node[] All
    {
        get
        {
            var list = new List<Node>();
            foreach (var element in this._baseComponent.GetChildren())
            {
                if (element is IChildComponent<T> && element is Node)
                {
                    list.Add((Node)element);
                }
            }

            return list.ToArray();
        }
    }

    /// <summary>
    /// Add an new component to base component
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    public T2 AddComponent<T2>() where T2 : Node, IChildComponent<T>
    {
        var element = Array.Find(this.All, df => df.GetType() == typeof(T2));
        if (element == null)
        {
            T2 component = Activator.CreateInstance<T2>();

            component.Name = typeof(T2).Name;
            component.BaseComponent = this._baseComponent;

            if (this._baseComponent.IsInsideTree())
            {
                this._baseComponent.AddChild(component);
            }

            this.OnComponentAdded?.Invoke(component);
            return component;
        }
        else
        {
            Logger.LogDebug(this, "An node from this type already exist");
        }

        return null;
    }

    /// <summary>
    /// Add an new component to base component
    /// </summary>
    /// <param name="type">Type of the component which have to be added</param>
    public Node AddComponent(Type type)
    {
        var element = Array.Find(this.All, df => df.GetType() == type);
        if (element == null)
        {
            object createdObject = Activator.CreateInstance(type);
            if (createdObject is IChildComponent<T> component && createdObject is Node node)
            {
                node.Name = type.Name;
                component.BaseComponent = this._baseComponent;

                if (this._baseComponent.IsInsideTree())
                {
                    this._baseComponent.AddChild(node);
                }

                this.OnComponentAdded?.Invoke(node);
                return node;
            }
        }

        return null;
    }

    /// <summary>
    /// Add an new component to base component by given resource name
    /// </summary>
    /// <param name="type">Type of the component which have to be added</param>
    /// <param name="resourcePath">Path to the godot resource</param>
    public Node AddComponent(Type type, string resourcePath)
    {
        var scene = GD.Load<PackedScene>(resourcePath);
        return this.AddComponent(type, scene);
    }

    /// <summary>
    /// /// Add component as packed scene
    /// </summary>
    /// <param name="type"></param>
    /// <param name="scene"></param>
    public Node AddComponent(Type type, PackedScene scene)
    {
        var element = Array.Find(this.All, df => df.GetType() == type);
        if (element == null)
        {
            //scene.ResourceLocalToScene = true;
            var createdObject = scene.Instantiate();
            if (createdObject is IChildComponent<T> component && createdObject is not null)
            {
                createdObject.Name = type.Name;
                component.BaseComponent = this._baseComponent;

                if (this._baseComponent.IsInsideTree())
                {
                    this._baseComponent.AddChild(createdObject as Node);
                }

                this.OnComponentAdded?.Invoke(createdObject as Node);
                return createdObject as Node;
            }
        }

        return null;
    }

    /// <summary>
    /// Add an new component to base component by given resource path
    /// </summary>
    /// <param name="resourcePath"></param>
    /// <typeparam name="T2"></typeparam>
    public T2 AddComponent<T2>(string resourcePath) where T2 : Node, IChildComponent<T>
    {
        var element = Array.Find(this.All, df => df.GetType() == typeof(T2));
        if (element != null)
        {
            this.DeleteComponent<T2>();
        }

        var scene = GD.Load<PackedScene>(resourcePath);
        //  scene.ResourceLocalToScene = true;
        var component = scene.Instantiate<T2>();
        component.Name = typeof(T2).Name;
        component.BaseComponent = this._baseComponent;

        if (this._baseComponent.IsInsideTree())
        {
            this._baseComponent.AddChild(component);
        }

        this.OnComponentAdded?.Invoke(component);
        return component;
    }

    /// <summary>
    /// Add an new component to base component by given resource path (async)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="resourcePath"></param>
    /// <param name="callback"></param>
    public void AddComponentAsync(Type type, string resourcePath, Action<Node> callback)
    {
        lock (this._onHold)
        {
            if (!this._onHold.Contains(resourcePath))
            {
                this._onHold.Add(resourcePath);

                Utils.AsyncLoader.Loader.LoadResource(resourcePath, (res) =>
                {
                    var result = this.AddComponent(type, (PackedScene)res);
                    callback?.Invoke(result);

                    this._onHold.Remove(resourcePath);
                });
            }
        }
    }

    /// <summary>
    /// Delete all exist components and remove them from base component
    /// </summary>
    public void Clear()
    {
        foreach (var comp in this.All)
        {
            if (comp.IsInsideTree())
            {
                this._baseComponent.RemoveChild(comp);
            }
        }
    }

    /// <summary>
    /// Delete a given componentn from base component
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    public void DeleteComponent<T2>() where T2 : Node, IChildComponent<T>
    {
        var element = Array.Find(this.All, df => df.GetType() == typeof(T2));
        element?.QueueFree();
    }

    /// <summary>
    /// Delete a given componentn from base component by given type
    /// </summary>
    /// <param name="componentType">Type of component which have to be deleted</param>
    ///
    public void DeleteComponent(Type componentType)
    {
        var element = Array.Find(this.All, df => df.GetType() == componentType);
        if (element?.IsInsideTree() == true)
        {
            this._baseComponent.RemoveChild(element);
        }
    }

    /// <summary>
    /// Get an existing component of the base component
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    public T2 Get<T2>() where T2 : Node, IChildComponent<T>
    {
        var element = Array.Find(this.All, df => df is T2);
        return element != null ? element as T2 : null;
    }

    /// <summary>
    /// Get an existing component of the base component
    /// </summary>
    public object Get(Type t)
    {
        var element = Array.Find(this.All, df => df.GetType() == t);
        return element != null ? element : (object?)null;
    }

    /// <summary>
    /// Check if component exist
    /// </summary>
    /// <param name="type"></param>
    public bool HasComponent(Type type)
    {
        return this.All.Any(df => df.GetType() == type);
    }
}