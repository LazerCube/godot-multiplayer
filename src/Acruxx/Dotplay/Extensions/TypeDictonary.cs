using System;
using System.Collections.Generic;
using System.Linq;

namespace Dotplay.Extensions;

/// <summary>
/// TypeDictonary contains an generic type as key and an generic object as value
/// </summary>
public class TypeDictonary<T2>
{
    private readonly Dictionary<Type, T2> _values = new();

    /// <summary>
    /// Get a full list of registered services
    /// </summary>
    /// <returns></returns>
    public T2[] All => this._values.Values.ToArray();

    /// <summary>
    /// Create and register service by given type
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    public T Create<T>() where T : class, T2
    {
        lock (this._values)
        {
            T newService = Activator.CreateInstance<T>();
            this._values.Add(typeof(T), newService);

            return newService;
        }
    }

    /// <summary>
    /// Get an registered service by given type
    /// </summary>
    /// <typeparam name="T">Type of service</typeparam>
    public T Get<T>() where T : class, T2
    {
        return !this.TryGetService(typeof(T), out T2 service) ? null : (T)service;
    }

    /// <inheritdoc />
    private bool TryGetService(Type type, out T2 service)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        lock (this._values)
        {
            return this._values.TryGetValue(type, out service);
        }
    }
}