using System;
using System.Collections.Generic;
using Godot;

namespace Dotplay.Utils;

/// <summary>
/// Helper class to load resources in background
/// </summary>
public class AsyncLoader
{
    private readonly Queue<LoadingRequest> _resourceLoader = new();
    private LoadingRequest? _currentResource;

    /// <summary>
    /// The event handler for on progress
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="percent"></param>
    public delegate void ProgressHandler(string filename, float percent);

    public event ProgressHandler? OnProgress;

    /// <summary>
    /// The static async loader
    /// </summary>
    public static AsyncLoader Loader { get; } = new AsyncLoader();

    /// <summary>
    /// Load an resource by given path
    /// </summary>
    /// <param name="resourceName">Path to resource file</param>
    /// <param name="callback">An action with returning an Resource</param>
    public void LoadResource(string resourceName, Action<Resource> callback)
    {
        Logger.LogDebug(this, "Try to start load  " + resourceName);

        this._resourceLoader.Enqueue(new LoadingRequest
        {
            ResourceName = resourceName,
            OnSucess = callback
        });
    }

    /// <summary>
    /// Brings the async loader to the next state.
    /// Binded on your _Process Method
    /// </summary>
    public void Tick()
    {
        if (this._resourceLoader.Count > 0 && this._currentResource == null)
        {
            this._currentResource = this._resourceLoader.Dequeue();

            var result = ResourceLoader.LoadThreadedRequest(this._currentResource.ResourceName);
            if (result != Error.Ok)
            {
                Logger.LogDebug(this, "Cant load resource " + this._currentResource.ResourceName + " - Reason: " + result.ToString());
                this._currentResource = null;
            }
            else
            {
                this.OnProgress?.Invoke(this._currentResource.ResourceName, 0);
            }
        }

        this.CheckLoad();
    }

    /// <summary>
    /// Checks the load.
    /// </summary>
    private void CheckLoad()
    {
        if (this._currentResource != null)
        {
            var mapLoaderProgress = new Godot.Collections.Array();
            if (this._currentResource != null)
            {
                var status = ResourceLoader.LoadThreadedGetStatus(this._currentResource.ResourceName, mapLoaderProgress);
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    Resource res = ResourceLoader.LoadThreadedGet(this._currentResource.ResourceName);
                    Logger.LogDebug(this, "Resource loaded " + this._currentResource.ResourceName);

                    this._currentResource.OnSucess?.Invoke(res);
                    this._currentResource = null;
                }
                else if (status == ResourceLoader.ThreadLoadStatus.InvalidResource || status == ResourceLoader.ThreadLoadStatus.Failed)
                {
                    Logger.LogDebug(this, "Error loading  " + this._currentResource.ResourceName);
                    this._currentResource = null;
                }
                else if (status == ResourceLoader.ThreadLoadStatus.InProgress)
                {
                    if (mapLoaderProgress.Count > 0)
                    {
                        this.OnProgress?.Invoke(this._currentResource.ResourceName, (float)mapLoaderProgress[0]);
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    internal class LoadingRequest
    {
        /// <summary>
        /// Gets or sets the on sucess.
        /// </summary>
        public Action<Resource>? OnSucess { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public string ResourceName { get; set; } = string.Empty;
    }
}