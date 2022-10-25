using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Dotplay;

/// <summary>
/// Custom logger of the Dotplay
/// </summary>
public static class Logger
{
    /// <summary>
    /// Currently not in use! (TODO)
    /// </summary>
    public static Dictionary<string, string> DebugUI = new();

    /// <summary>
    /// The log message handler
    /// </summary>
    /// <param name="message"></param>
    public delegate void LogMessageHandler(string message);

    /// <summary>
    /// Event triggered when an new log message received
    /// </summary>
    public static event LogMessageHandler OnLogMessage;

    /// <summary>
    /// Debug logging an object with messsage
    /// </summary>
    /// <param name="service"></param>
    /// <param name="message"></param>
    public static void LogDebug(object service, string message)
    {
        var format = string.Format(
            "[{3}][{0}][{1}] {2}",
            Process.GetCurrentProcess().StartTime,
            service.GetType().Name,
            message,
            Environment.CurrentManagedThreadId
        );

        Debug.WriteLine(format);
        OnLogMessage?.Invoke(message);
    }

    /// <summary>
    /// For client-side logging (not finish)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SetDebugUI(string name, string value)
    {
        DebugUI[name] = value;
    }
}