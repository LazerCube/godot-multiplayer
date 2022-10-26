using System.Collections.Generic;

namespace Dotplay.Game.Client;

/// <summary>
/// Static class for client settings
/// </summary>
public static class ClientSettings
{
    /// <summary>
    /// Possible resoltuions
    /// </summary>
    public static readonly string[] Resolutions = new string[] {
       "640x480", "1280x1024", "1280x960", "1280x800","1280x768","1280x720", "1920x1080", "2560x1440"
    };

    /// <summary>
    /// Possible shadow quality values
    /// </summary>
    public static readonly Dictionary<string, int> ShadowQualities = new()
    {
       {"Disabled", 0},
       {"UltraLow", 1024},
       {"Low", 2048},
       {"Middle", 4096},
       {"High", 8192},
    };

    /// <summary>
    /// Possible window modes
    /// </summary>
    public enum WindowModes : long
    {
        /// <summary>
        /// Is windowed
        /// </summary>
        Windowed,

        /// <summary>
        /// Is borderless window
        /// </summary>
        Borderless,

        /// <summary>
        /// Is fullscreen
        /// </summary>
        Fullscreen,

        /// <summary>
        /// Is exclusive fullscreen
        /// </summary>
        ExclusiveFullscreen
    };

    /// <summary>
    /// Contains all vars for the client instance
    /// </summary>
    public static VarsCollection Variables { get; set; } = new VarsCollection(new Vars());
}