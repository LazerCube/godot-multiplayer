using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using RCON.Utils;

namespace RCON
{
    public class RemoteConServer
    {
        public delegate string CommandEventHandler(string command, IList<string> args);

        private readonly List<TcpClient> _clients;

        private readonly TcpListener _listener;

        public readonly CommandManager CommandManager = new CommandManager();

        public RemoteConServer(IPAddress bindAddress, int port)
        {
            UseUtf8 = false;
            EmptyPayloadKick = true;
            EnableIpWhitelist = true;
            InvalidPacketKick = true;
            IpWhitelist = new[]
            {
                "127.0.0.1",
                "192.*.*.*"
            };
            MaxPasswordTries = 3;
            BanMinutes = 15;
            Password = "changeme";
            SendAuthImmediately = false;
            IpBanList = new Dictionary<string, int>();
            MaxConnectionsPerIp = 1;
            MaxConnections = 5;

#if DEBUG
            Debug = true;
#else
            Debug = false;
#endif

            _clients = new List<TcpClient>();
            _listener = new TcpListener(bindAddress, port);
        }

        /// <summary>
        /// </summary>
        public Dictionary<string, int> IpBanList { get; set; }

        /// <summary>
        ///     Whether to use UTF8 to encode packet payloads.
        ///     Default: False
        /// </summary>
        public bool UseUtf8 { get; set; }

        /// <summary>
        ///     When true closes the connection if the payload of the packet is empty.
        ///     Default: True
        /// </summary>
        public bool EmptyPayloadKick { get; set; }

        /// <summary>
        ///     Wether or not to match incoming ips to our whitelist
        ///     <see cref="IpWhitelist" />
        ///     Default: True
        /// </summary>
        public bool EnableIpWhitelist { get; set; }

        /// <summary>
        ///     When true closes the connection if the packet is not of type "ExecCommand"
        ///     Default: True
        /// </summary>
        public bool InvalidPacketKick { get; set; }

        /// <summary>
        ///     An array containing IP Patterns to allow connecting
        ///     eg.
        ///     192.*.*.* matches all ips starting with 192
        ///     127.0.0.* matches all ips starting with 127.0.0.*
        ///     Default: 127.0.0.1, 192.*.*.*
        /// </summary>
        public string[] IpWhitelist { get; set; }

        /// <summary>
        ///     How many failed password attempts a client has before he gets kicked
        ///     (Setting this to zero means close connection after first try)
        ///     Default: 3
        /// </summary>
        public uint MaxPasswordTries { get; set; }

        /// <summary>
        ///     How many minutes a client should be banned when he reached max password tries
        ///     (Setting this to zero means no ban)
        ///     Default: 15
        /// </summary>
        public int BanMinutes { get; set; }

        /// <summary>
        ///     The password to access RCON
        ///     Default: changeme
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     This provides a fix for "stupid" written clients,
        ///     which do not expect a RESPONSE_VALUE packet before the auth answer
        ///     Default: False
        /// </summary>
        public bool SendAuthImmediately { get; set; }

        /// <summary>
        ///     Wether or not we should invoke <see cref="OnCommandReceived" /> instead of internally parsing the command
        ///     Default: False
        /// </summary>
        public bool UseCustomCommandHandler { get; set; }

        /// <summary>
        ///     Should we debug the library
        ///     Default: False (In Release), True (In Debug)
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        ///     How many clients a single IP can connect to the server
        ///     (Setting this to zero means unlimited)
        ///     Default: 1
        /// </summary>
        public uint MaxConnectionsPerIp { get; set; }

        /// <summary>
        ///     How many clients can connect to the server
        ///     (Setting this to zero means unlimited)
        ///     Default: 5
        /// </summary>
        public uint MaxConnections { get; set; }

        /// <summary>
        ///     Event Handler to parse custom commands
        /// </summary>
        public event CommandEventHandler OnCommandReceived;

        /// <summary>
        ///     Starts the TCPListener and begins accepting clients
        /// </summary>
        public void StartListening()
        {
            _listener.Start();
            _listener.BeginAcceptTcpClient(OnAccept, _listener);
            LogDebug("Started listening on " + ((IPEndPoint)_listener.LocalEndpoint).Address + ", Password is: \"" +
                     Password + "\"");
        }

        public void StopListening()
        {
            if (!_listener.Server.IsBound)
                return;

            _listener.Stop();
        }

        private void OnAccept(IAsyncResult result)
        {
            TcpClient tcpClient;
            try
            {
                tcpClient = _listener.EndAcceptTcpClient(result);
            }
            catch (ObjectDisposedException)
            {
                LogDebug("Socket was closed");
                return;
            }

            var ip = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;

            if (MaxConnections > 0)
                if (_clients.Count >= MaxConnections)
                {
                    LogDebug("Rejected new connection from " + ip + " (Server full.)");
                    tcpClient.Close();
                    return;
                }

            if (MaxConnectionsPerIp > 0)
            {
                var count = 0;
                foreach (var tcpClient1 in _clients)
                    if (((IPEndPoint)tcpClient1.Client.RemoteEndPoint).Address.ToString() == ip.ToString())
                        count++;

                if (count >= MaxConnectionsPerIp)
                {
                    LogDebug("Rejected new connection from " + ip + " (Too many connections from this IP)");
                    tcpClient.Close();
                    return;
                }
            }

            if (EnableIpWhitelist)
                if (!IpWhitelist.Any(p =>
                        IpExtension.Match(p, ip.ToString())))
                {
                    LogDebug("Rejected new connection from " + ip + " (Not in whitelist)");
                    tcpClient.Close();
                    return;
                }

            if (IpBanList.ContainsKey(ip.ToString()))
            {
                if (IpBanList[ip.ToString()] - DateTime.Now.ToUnixTimestamp() > 0)
                {
                    LogDebug("Rejected new connection from " + ip + " (Banned till " +
                             DateTimeExtensions.FromUnixTimestamp(IpBanList[ip.ToString()]).ToString("F") + ")");
                    tcpClient.Close();
                    return;
                }

                IpBanList.Remove(ip.ToString());
            }

            LogDebug("Accepted new connection from " + ip);
            new RemoteConTcpClient(tcpClient, this);

            _clients.Add(tcpClient);

            if (!_listener.Server.IsBound)
                return;

            _listener.BeginAcceptTcpClient(OnAccept, _listener);
        }

        internal string ExecuteCustomCommandHandler(string cmd, IList<string> args)
        {
            return UseCustomCommandHandler && OnCommandReceived != null ? OnCommandReceived(cmd, args) : "";
        }

        internal void LogDebug(string message)
        {
            if (!Debug)
                return;

            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        internal void RemoveClient(TcpClient client)
        {
            _clients.Remove(client);
        }
    }
}