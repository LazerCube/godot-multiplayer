using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RCON.Utils;

namespace RCON
{
    /// <summary>
    ///     Client connected to the remote console
    /// </summary>
    internal class RemoteConTcpClient
    {
        /// <summary>
        ///     The maximum packet size to receive
        /// </summary>
        private const int MaxAllowedPacketSize = 4096;

        /// <summary>
        ///     Used to determine if we're in unit test mode (Means no actual connection)
        /// </summary>
        private readonly bool _isUnitTest;

        /// <summary>
        ///     Underlaying NetworkStream
        /// </summary>
        private readonly NetworkStream _ns;

        /// <summary>
        ///     Reference to RemoteConServer for getting settings
        /// </summary>
        private readonly RemoteConServer _remoteConServer;

        /// <summary>
        ///     The TCP Client
        /// </summary>
        private readonly TcpClient _tcp;

        /// <summary>
        ///     How many failed login attempts the client has
        /// </summary>
        private int _authTries;

        /// <summary>
        ///     A buffer containing the packet
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        ///     Wether or not the client is connected
        /// </summary>
        private bool _connected;

        /// <summary>
        ///     Has the client been successfully authenticated
        /// </summary>
        internal bool Authenticated;

        public RemoteConTcpClient(TcpClient tcp, RemoteConServer remoteConServer)
        {
            _tcp = tcp;
            _remoteConServer = remoteConServer;

            _ns = tcp.GetStream();
            _connected = true;
            Authenticated = false;

            try
            {
                // Connection was closed.
                if (!_tcp.Connected)
                    return;

                // As indicated by specification the maximum packet size is 4096
                // NOTE: Not sure if only the server is allowed to sent packets with max 4096 or both parties!
                _buffer = new byte[MaxAllowedPacketSize];
                _ns.BeginRead(_buffer, 0, MaxAllowedPacketSize, OnPacket, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        ///     UnitTest only constructor
        /// </summary>
        internal RemoteConTcpClient(RemoteConServer remoteConServer)
        {
            _remoteConServer = remoteConServer;
            _isUnitTest = true;
        }

        /// <summary>
        ///     Closes the TCP connection
        /// </summary>
        private void CloseConnection()
        {
            if (_isUnitTest)
                return;
            _remoteConServer.RemoveClient(_tcp);
            _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address + " connection closed.");

            _connected = false;

            if (!_tcp.Connected)
                return;

            _tcp.Client.Disconnect(false);
            _tcp.Close();
        }

        /// <summary>
        ///     Sends the specified packet to the client
        /// </summary>
        /// <param name="packet">The packet to send</param>
        /// <exception cref="Exception">Not connected</exception>
        private void SendPacket(RemoteConPacket packet)
        {
            if (_isUnitTest)
                return;

            if (!_connected)
                throw new Exception("Not connected.");

            var ackBytes = packet.GetBytes();
            _ns.Write(ackBytes, 0, ackBytes.Length - 1);
        }

        /// <summary>
        ///     Handles packets
        /// </summary>
        /// <param name="result"></param>
        private void OnPacket(IAsyncResult result)
        {
            try
            {
                var bytesRead = _ns.EndRead(result);
                if (!_tcp.Connected || !_connected)
                {
                    _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address + " lost connection.");
                    CloseConnection();
                    return;
                }

                if (bytesRead == 0)
                {
                    _buffer = new byte[MaxAllowedPacketSize];
                    _ns.BeginRead(_buffer, 0, MaxAllowedPacketSize, OnPacket, null);
                    return;
                }

                if (_buffer[_buffer.Length - 1] != 0x0 || _buffer[_buffer.Length - 2] != 0x0)
                {
                    if (!_isUnitTest)
                        _remoteConServer.LogDebug("Missing null-terminators!");
#if DEBUG
                    Console.WriteLine("Packet missing null-terminators!");
#else
                    CloseConnection();
#endif
                    return;
                }

                // Resize buffer to actual amount read.
                Array.Resize(ref _buffer, bytesRead);

                ParsePacket(_buffer);

                if (!_connected || !_tcp.Connected)
                {
                    _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address + " lost connection.");
                    CloseConnection();
                    return;
                }

                _buffer = new byte[MaxAllowedPacketSize];
                _ns.BeginRead(_buffer, 0, MaxAllowedPacketSize, OnPacket, null);
            }
            catch (IOException)
            {
                _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address + " lost connection.");
                CloseConnection();
            }
            catch (ThreadAbortException)
            {
                _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address + " socket closed.");
                CloseConnection();
            }
            catch (RconServerException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        ///     Parses raw bytes to RemoteConPacket
        /// </summary>
        /// <param name="rawPacket"></param>
        internal void ParsePacket(byte[] rawPacket)
        {
            try
            {
                var packet = new RemoteConPacket(rawPacket, _remoteConServer.UseUtf8);

                if (!_isUnitTest)
                    _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address + " sent packet " +
                                              packet.Type + "!");

                // Do not allow any other packets than auth to be sent when client is not authenticated
                if (!Authenticated)
                {
                    if (packet.Type != RemoteConPacket.PacketType.Auth)
                    {
                        if (_isUnitTest)
                            throw new NotAuthenticatedException();
                        CloseConnection();
                    }

                    _authTries++;

                    if (packet.Payload == _remoteConServer.Password)
                    {
                        if (!_isUnitTest)
                            _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address +
                                                      " successfully authenticated!");
                        Authenticated = true;

                        if (!_remoteConServer.SendAuthImmediately)
                            SendPacket(new RemoteConPacket(packet.Id, RemoteConPacket.PacketType.ResponseValue, "",
                                _remoteConServer.UseUtf8));

                        SendPacket(new RemoteConPacket(packet.Id, RemoteConPacket.PacketType.ExecCommand, "",
                            _remoteConServer.UseUtf8));
                        return;
                    }

                    if (_authTries >= _remoteConServer.MaxPasswordTries)
                    {
                        if (_remoteConServer.BanMinutes > 0)
                            _remoteConServer.IpBanList.Add(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address.ToString(),
                                DateTime.Now.AddMinutes(_remoteConServer.BanMinutes).ToUnixTimestamp());

                        CloseConnection();
                        return;
                    }

                    if (!_isUnitTest)
                        _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address +
                                                  " entered wrong password!");

                    if (!_remoteConServer.SendAuthImmediately)
                        SendPacket(new RemoteConPacket(packet.Id, RemoteConPacket.PacketType.ResponseValue, "",
                            _remoteConServer.UseUtf8));

                    SendPacket(new RemoteConPacket(-1, RemoteConPacket.PacketType.ExecCommand, "",
                        _remoteConServer.UseUtf8));

                    return;
                }

                // Invalid packet type.
                if (packet.Type != RemoteConPacket.PacketType.ExecCommand)
                {
                    if (_isUnitTest)
                        throw new InvalidPacketTypeException();

                    _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address +
                                              " sent a packet with invalid type!");

                    if (_remoteConServer.InvalidPacketKick)
                        CloseConnection();
                    return;
                }

                if (packet.Payload == "")
                {
                    if (_isUnitTest)
                        throw new EmptyPacketPayloadException();
                    _remoteConServer.LogDebug(((IPEndPoint)_tcp.Client.RemoteEndPoint).Address +
                                              " sent a packet with empty payload!");

                    if (_remoteConServer.EmptyPayloadKick)
                        CloseConnection();
                    return;
                }

                var args = ArgumentParser.ParseLine(packet.Payload);
                var cmd = args[0];
                args.RemoveAt(0);

                if (_remoteConServer.UseCustomCommandHandler)
                {
                    var result = _remoteConServer.ExecuteCustomCommandHandler(cmd, args);
                    SendPacket(new RemoteConPacket(packet.Id, RemoteConPacket.PacketType.ResponseValue,
                        result, _remoteConServer.UseUtf8));
                    return;
                }

                var command = _remoteConServer.CommandManager.GetCommand(cmd);
                if (command == null)
                {
                    SendPacket(new RemoteConPacket(packet.Id, RemoteConPacket.PacketType.ResponseValue,
                        "Invalid command \"" + packet.Payload + "\"", _remoteConServer.UseUtf8));
                }
                else
                {
                    var commandResult = command.Handler(cmd, args);
                    // TODO: Split packets?
                    SendPacket(new RemoteConPacket(packet.Id, RemoteConPacket.PacketType.ResponseValue,
                        commandResult, _remoteConServer.UseUtf8));
                }
            }
            catch (RconServerException)
            {
                throw;
            }
            catch (Exception e)
            {
                if (_isUnitTest)
                    throw;

                if (!_isUnitTest)
                    _remoteConServer.LogDebug(string.Format("Client {0} caused an exception: {1} and was killed.",
                        ((IPEndPoint)_tcp.Client.RemoteEndPoint).Address, e.Message));

                CloseConnection();
            }
        }
    }
}