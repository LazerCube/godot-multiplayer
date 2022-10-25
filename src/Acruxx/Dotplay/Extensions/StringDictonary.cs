using System;
using System.Collections.Generic;
using System.Linq;

namespace Dotplay.Extensions;

/// <summary>
/// String Dictonary contains an string key and an generic object value
/// </summary>
public class StringDictonary<T2>
{
    private readonly Dictionary<string, T2> _values = new();

    /// <summary>
    /// Get a full list of registered services
    /// </summary>
    public T2[] All => this._values.Values.ToArray();

    /// <summary>
    /// Create and register service by given type
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    public T Create<T>(string name) where T : class, T2
    {
        lock (this._values)
        {
            T newService = Activator.CreateInstance<T>();
            this._values.Add(name, newService);

            return newService;
        }
    }

    /// <summary>
    /// Get an registered service by given type
    /// </summary>
    /// <typeparam name="T">Type of service</typeparam>
    public T Get<T>(string name) where T : class, T2
    {
        return !this.TryGetService(name, out T2 service) ? null : (T)service;
    }

    /// <inheritdoc />
    private bool TryGetService(string name, out T2 service)
    {
        lock (this._values)
        {
            return this._values.TryGetValue(name, out service);
        }
    }
}