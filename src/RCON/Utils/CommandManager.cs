using System.Collections.Generic;

namespace RCON.Utils
{
    /// <summary>
    ///     Command manager
    /// </summary>
    public class CommandManager
    {
        public CommandManager()
        {
            Commands = new Dictionary<string, Command>();
        }

        public Dictionary<string, Command> Commands { get; private set; }

        /// <summary>
        ///     Adds command to list of command handlers.
        /// </summary>
        /// <param name="command"></param>
        public void Add(Command command)
        {
            Commands[command.Name] = command;
        }

        /// <summary>
        ///     Adds new command handler.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="handler"></param>
        public void Add(string name, CommandHandler handler)
        {
            Add(name, "", "", handler);
        }

        /// <summary>
        ///     Adds new command handler.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="handler"></param>
        public void Add(string name, string description, CommandHandler handler)
        {
            Add(name, "", description, handler);
        }

        /// <summary>
        ///     Adds new command handler.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="usage"></param>
        /// <param name="description"></param>
        /// <param name="handler"></param>
        public void Add(string name, string usage, string description, CommandHandler handler)
        {
            Commands[name] = new Command(name, usage, description, handler);
        }

        /// <summary>
        ///     Returns command or null, if the command doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Command GetCommand(string name)
        {
            Command command;
            Commands.TryGetValue(name, out command);
            return command;
        }
    }

    /// <summary>
    ///     Generalized command holder
    /// </summary>
    public class Command
    {
        public Command(string name, string usage, string description, CommandHandler handler)
        {
            Name = name;
            Usage = usage;
            Description = description;
            Handler = handler;
        }

        public string Name { get; private set; }
        public string Usage { get; private set; }
        public string Description { get; private set; }
        public CommandHandler Handler { get; private set; }
    }

    /// <summary>
    /// </summary>
    /// <param name="command">The command that was entered</param>
    /// <param name="args">List containing all arguments, does not contain command</param>
    public delegate string CommandHandler(string command, IList<string> args);
}