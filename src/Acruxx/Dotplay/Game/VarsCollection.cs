using System;
using System.Globalization;
using System.Linq;
using Godot;

namespace Dotplay.Game;

/// <summary>
/// An collection of vars and config files
/// </summary>
public class VarsCollection
{
    /// <summary>
    /// Constructor for  vars
    /// </summary>
    /// <param name="vars">Dictonary which contains  varaibles</param>
    public VarsCollection(Vars vars)
    {
        this.Vars = vars;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VarsCollection"/> class.
    /// </summary>
    public VarsCollection()
    {
        this.Vars = new Vars();
    }

    /// <summary>
    /// The handler for event changes
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public delegate void ValueChangeHandler(KeyChangeEnum type, string name, string value);

    /// <summary>
    /// Called when an value inside the collection changed
    /// </summary>
    public event ValueChangeHandler OnChange;

    /// <summary>
    /// Change state of collection key
    /// </summary>
    public enum KeyChangeEnum
    {
        /// <summary>
        /// Key was updates
        /// </summary>
        Update = 0,

        /// <summary>
        /// Key was insert
        /// </summary>
        Insert = 1,

        /// <summary>
        /// Key was deleted
        /// </summary>
        Delete = 2,
    }

    /// <summary>
    /// List of all variables
    /// </summary>
    /// <returns></returns>
    public Vars Vars { get; private set; } = new Vars();

    /// <summary>
    /// Get an server variable
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="defaultValue"></param>
    /// <typeparam name="T"></typeparam>
    public T Get<T>(string varName, T defaultValue = default(T)) where T : IConvertible
    {
        if (this.Vars.AllVariables is null || !this.Vars.AllVariables.ContainsKey(varName))
        {
            return defaultValue;
        }

        try
        {
            var value = this.Vars.AllVariables[varName].ToString();
            var formatProvider = CultureInfo.InvariantCulture;

            if (default(T) is float)
            {
                value = value.Replace(",", ".");
            }

            return (T)Convert.ChangeType(value, typeof(T), formatProvider);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Get an key or button id from config
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="defaultValue"></param>
    public object GetKeyValue(string varName, object defaultValue = null)
    {
        if (this.Vars.AllVariables?.ContainsKey(varName) == true)
        {
            var value = this.Vars.AllVariables[varName].ToString();
            if (value.StartsWith("KEY_"))
            {
                var key = value.Replace("KEY_", "");
                if (Enum.TryParse(key, true, out Key myKey))
                {
                    return myKey;
                }
            }
            else if (value.StartsWith("BTN_"))
            {
                var key = value.Replace("BTN_", "");
                if (Enum.TryParse(key, true, out MouseButton myBtn))
                {
                    return myBtn;
                }
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Get value by given key
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="defaultValue"></param>
    public string GetValue(string varName, string defaultValue = null)
    {
        return this.Vars.AllVariables is null || !this.Vars.AllVariables.ContainsKey(varName)
            ? defaultValue
            : this.Vars.AllVariables[varName];
    }

    /// <summary>
    /// Check if key or mouse button pressed
    /// </summary>
    /// <param name="varName"></param>
    /// <param name="defaultValue"></param>
    public bool IsKeyValuePressed(string varName, object defaultValue)
    {
        var result = this.GetKeyValue(varName, defaultValue);
        if (result is Key key)
        {
            return Godot.Input.IsKeyPressed(key);
        }
        else if (result is MouseButton mouseButton)
        {
            return Godot.Input.IsMouseButtonPressed(mouseButton);
        }

        return false;
    }

    /// <summary>
    /// Load an config file by given filename
    /// </summary>
    /// <param name="configFilename"></param>
    public void LoadConfig(string configFilename)
    {
        var cfg = new ConfigFile();
        var loadError = cfg.Load("user://" + configFilename);
        if (loadError == Error.Ok)
        {
            Logger.LogDebug(this, "Load config file " + configFilename);
            if (cfg.HasSection("vars"))
            {
                foreach (var element in this.Vars.AllVariables.ToArray())
                {
                    this.Set(element.Key, cfg.GetValue("vars", element.Key).ToString());
                }
            }
        }
    }

    /// <summary>
    /// Store an value and trigger event
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public void Set(string key, string value)
    {
        lock (this.Vars.AllVariables)
        {
            if (this.Vars.AllVariables.ContainsKey(key))
            {
                if (this.Vars.AllVariables[key] != value)
                {
                    this.Vars.AllVariables[key] = value;
                    this.OnChange?.Invoke(KeyChangeEnum.Update, key, value);
                }
            }
            else
            {
                this.Vars.AllVariables.Add(key, value);
                this.OnChange?.Invoke(KeyChangeEnum.Insert, key, value);
            }
        }
    }

    /// <summary>
    /// Store an config file by given values
    /// </summary>
    /// <param name="configFilename"></param>
    public void StoreConfig(string configFilename)
    {
        var cfg = new ConfigFile();
        foreach (var element in this.Vars.AllVariables)
        {
            cfg.SetValue("vars", element.Key, element.Value);
        }

        var loadError = cfg.Save("user://" + configFilename);
        if (loadError != Error.Ok)
        {
            GD.PrintErr("Cant store file " + configFilename + " with reason " + loadError);
        }
        else
        {
            Logger.LogDebug(this, "Store config file " + configFilename);
        }
    }
}