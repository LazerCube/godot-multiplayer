using System.Net;
using System.Text;
using Godot;
using RCON;
using RCON.Utils;

namespace Dotplay.Network;

/// <summary>
/// The RCON network service
/// </summary>
public class RconServerService : IService
{
    /// <summary>
    /// The RCON server port
    /// </summary>
    [Export]
    public int RconPort = 27020;

    private readonly RemoteConServer? _server;

    public delegate void ServerStartedHandler(CommandManager manager);

    public event ServerStartedHandler? ServerStarted;

    /// <summary>
    /// Gets the commands.
    /// </summary>
    public CommandManager? Commands => this._server?.CommandManager;

    /// <inheritdoc />
    public void Register()
    {
        var server = new RemoteConServer(IPAddress.Any, this.RconPort)
        {
            SendAuthImmediately = true,
            Debug = true
        };

        server.CommandManager.Add("help", "(command)", "Shows this help", (cmd, arguments) =>
        {
            if (arguments.Count == 1)
            {
                var helpCmdStr = arguments[0];
                var helpCmd = server.CommandManager.GetCommand(helpCmdStr);
                if (helpCmd == null)
                {
                    return "Command not found.";
                }

                return string.Format("{0} - {1}", helpCmd.Name, helpCmd.Description);
            }

            var sb = new StringBuilder();

            var all = server.CommandManager.Commands.Count;
            var i = 0;
            foreach (var command in server.CommandManager.Commands)
            {
                if (command.Value.Usage?.Length == 0)
                {
                    sb.AppendFormat("{0}", command.Value.Name);
                }
                else
                {
                    sb.AppendFormat("{0} {1}", command.Value.Name, command.Value.Usage);
                }

                if (i < all)
                {
                    sb.Append(", ");
                }

                i++;
            }

            return sb.ToString();
        });

        server.StartListening();
        ServerStarted?.Invoke(server.CommandManager);
    }

    /// <inheritdoc />
    public void Render(float delta)
    { }

    /// <inheritdoc />
    public virtual void Unregister()
    {
        Logger.LogDebug(this, "Shutdown.");
        this._server?.StopListening();
    }

    /// <inheritdoc />
    public void Update(float delta)
    { }
}