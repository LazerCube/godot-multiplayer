using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using RCON.Utils;

namespace RCON
{
    public class RemoteConClient
    {
        /// <summary>
        /// </summary>
        /// <param name="success">If the authentication request was successful or not</param>
        public delegate void AuthEventHandler(bool success);

        /// <summary>
        /// </summary>
        /// <param name="result">A string containing the answer of the server</param>
        public delegate void CommandResult(string result);

        /// <summary>
        /// </summary>
        /// <param name="type">The type of the change</param>
        public delegate void ConnectionEventHandler(ConnectionStateChange type);

        /// <summary>
        /// </summary>
        /// <param name="message">The message we want to log</param>
        public delegate void LogEventHandler(string message);

        public enum ConnectionStateChange
        {
            Connected,
            Disconnected,
            NoConnection,
            ConnectionTimeout,
            ConnectionLost
        }

        private const int MaxAllowedPacketSize = 4096;

        /// <summary>
        ///     A list containing all requested commands for event handling
        /// </summary>
        private readonly Dictionary<int, CommandResult> _requestedCommands;

        /// <summary>
        ///     A buffer containing the packet
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        ///     The TCP Client
        /// </summary>
        private TcpClient _client;

        /// <summary>
        ///     Underlaying NetworkStream
        /// </summary>
        private NetworkStream _ns;

        /// <summary>
        ///     Current packetId we're on
        /// </summary>
        private int _packetId;

        /// <summary>
        ///     If the client is authenticated
        /// </summary>
        public bool Authenticated;

        public RemoteConClient()
        {
            _client = new TcpClient();

            _packetId = 0;
            _requestedCommands = new Dictionary<int, CommandResult>();

            UseUtf8 = false;
        }

        /// <summary>
        ///     Wether or not the TcpClient is still connected
        /// </summary>
        public bool Connected
        {
            get { return _client.Connected; }
        }

        /// <summary>
        ///     Whether to use UTF8 to encode the packet payload
        /// </summary>
        public bool UseUtf8 { get; set; }

        /// <summary>
        ///     An event handler when the result of the authentication is received
        /// </summary>
        public event AuthEventHandler OnAuthResult;

        /// <summary>
        ///     An event handler when the class wants to log something
        ///     Supressed when empty.
        /// </summary>
        public event LogEventHandler OnLog;

        /// <summary>
        ///     An event handler when the connection state changes
        ///     Ex. when disconnected or connection is lost
        /// </summary>
        public event ConnectionEventHandler OnConnectionStateChange;

        /// <summary>
        ///     Connects to the specified RCON Server
        /// </summary>
        /// <param name="hostname">The hostname of the RCON Server</param>
        /// <param name="port">The port to connect to</param>
        public void Connect(string hostname, int port)
        {
            Log(string.Format("Connecting to {0}:{1}", hostname, port));
            try
            {
                IAsyncResult asyncResult = null;
                try
                {
                    asyncResult = _client.BeginConnect(hostname, port, null, null);
                }
                catch (ObjectDisposedException)
                {
                    _client = new TcpClient();
                    try
                    {
                        asyncResult = _client.BeginConnect(hostname, port, null, null);
                    }
                    catch (Exception)
                    {
                        Log("Unknown Exception");
                    }
                }

                if (asyncResult == null)
                {
                    Log("Async connect failed!");
                    return;
                }

                asyncResult.AsyncWaitHandle.WaitOne(2000); // wait 2 seconds
                if (!asyncResult.IsCompleted)
                {
                    if (OnConnectionStateChange != null) OnConnectionStateChange(ConnectionStateChange.NoConnection);
                    _client.Client.Close();
                }
            }
            catch (SocketException)
            {
                if (OnConnectionStateChange != null)
                {
                    OnConnectionStateChange(ConnectionStateChange.ConnectionTimeout);
                    _client.Client.Close();
                }

                return;
            }

            if (!_client.Connected) return;
            _ns = _client.GetStream();

            // As indicated by specification the maximum packet size is 4096
            // NOTE: Not sure if only the server is allowed to sent packets with max 4096 or both parties!
            _buffer = new byte[MaxAllowedPacketSize];
            _ns.BeginRead(_buffer, 0, MaxAllowedPacketSize, OnPacket, null);

            Log("Connected.");
            if (OnConnectionStateChange != null)
                OnConnectionStateChange(ConnectionStateChange.Connected);
        }

        /// <summary>
        ///     Outputs a log to <see cref="OnLog" />
        /// </summary>
        /// <param name="message"></param>
        private void Log(string message)
        {
            if (OnLog != null)
                OnLog(message);
        }

        /// <summary>
        ///     Disconnects the client from the server
        /// </summary>
        public void Disconnect()
        {
            if (_client.Connected)
            {
                _client.Client.Disconnect(false);
                if (OnConnectionStateChange != null)
                    OnConnectionStateChange(ConnectionStateChange.Disconnected);
            }

            _client.Close();
        }

        /// <summary>
        ///     Sends the authentication to the server
        /// </summary>
        /// <param name="password">RCON Password</param>
        public void Authenticate(string password)
        {
            _packetId++;
            var packet = new RemoteConPacket(_packetId, RemoteConPacket.PacketType.Auth, password, UseUtf8);
            SendPacket(packet);
        }

        /// <summary>
        ///     Sends a RCON Command to the server
        /// </summary>
        /// <param name="command">The RCON command with parameters</param>
        /// <param name="resultFunc">A function that will be executed after the server has processed the request</param>
        /// <exception cref="NotAuthenticatedException">If we're not authenticated</exception>
        public void SendCommand(string command, CommandResult resultFunc)
        {
            if (!_client.Connected)
                return;

            if (!Authenticated)
                throw new NotAuthenticatedException();

            _packetId++;
            _requestedCommands.Add(_packetId, resultFunc);

            var packet = new RemoteConPacket(_packetId, RemoteConPacket.PacketType.ExecCommand, command, UseUtf8);
            SendPacket(packet);
        }

        /// <summary>
        ///     Sends the specified packet to the client
        /// </summary>
        /// <param name="packet">The packet to send</param>
        /// <exception cref="Exception">Not connected</exception>
        private void SendPacket(RemoteConPacket packet)
        {
            if (_client == null || !_client.Connected)
                throw new Exception("Not connected.");

            var packetBytes = packet.GetBytes();

            try
            {
                _ns.BeginWrite(packetBytes, 0, packetBytes.Length - 1, ar => { _ns.EndWrite(ar); }, null);
            }
            catch (ObjectDisposedException)
            {
            } // Do not write to NetworkStream when it's closed.
            catch (IOException)
            {
            } // Do not write to Socket when it's closed.
        }

        /// <summary>
        /// </summary>
        /// <param name="result"></param>
        private void OnPacket(IAsyncResult result)
        {
            try
            {
                var bytesRead = _ns.EndRead(result);
                if (!_client.Connected)
                {
                    if (OnConnectionStateChange != null)
                        OnConnectionStateChange(ConnectionStateChange.ConnectionLost);
                    return;
                }

                if (bytesRead == 0)
                {
                    _buffer = new byte[MaxAllowedPacketSize];
                    _ns.BeginRead(_buffer, 0, MaxAllowedPacketSize, OnPacket, null);
                    return;
                }

                Array.Resize(ref _buffer, bytesRead);

                ParsePacket(_buffer);

                if (!_client.Connected)
                {
                    if (OnConnectionStateChange != null)
                        OnConnectionStateChange(ConnectionStateChange.ConnectionLost);
                    return;
                }

                _buffer = new byte[MaxAllowedPacketSize];
                _ns.BeginRead(_buffer, 0, MaxAllowedPacketSize, OnPacket, null);
            }
            catch (IOException)
            {
                if (OnConnectionStateChange != null)
                    OnConnectionStateChange(ConnectionStateChange.ConnectionLost);
                Disconnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Log(e.ToString());
            }
        }

        /// <summary>
        ///     Parses raw bytes to RemoteConPacket
        /// </summary>
        /// <param name="rawPacket"></param>
        private void ParsePacket(byte[] rawPacket)
        {
            try
            {
                var packet = new RemoteConPacket(rawPacket, UseUtf8);
                if (!Authenticated)
                {
                    // ExecCommand is AuthResponse too.
                    if (packet.Type == RemoteConPacket.PacketType.ExecCommand)
                    {
                        if (packet.Id == -1)
                        {
                            Log("Authentication failed.");
                            Authenticated = false;
                        }
                        else
                        {
                            Log("Authentication success.");
                            Authenticated = true;
                        }

                        if (OnAuthResult != null)
                            OnAuthResult(Authenticated);
                    }

                    return;
                }

                if (_requestedCommands.ContainsKey(packet.Id) &&
                    packet.Type == RemoteConPacket.PacketType.ResponseValue)
                    _requestedCommands[packet.Id](packet.Payload);
                else
                    Log("Got packet with invalid id " + packet.Id);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Log(e.ToString());
            }
        }
    }
}