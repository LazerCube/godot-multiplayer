using System;
using System.IO;
using System.Text;
using RCON.Utils;

namespace RCON
{
    /// <summary>
    ///     Class for Source RCON Packets
    ///     TODO: Split Packets (https://developer.valvesoftware.com/wiki/Source_RCON_Protocol#Multiple-packet_Responses)
    /// </summary>
    internal class RemoteConPacket
    {
        public enum PacketType
        {
            ResponseValue = 0,
            ExecCommand = 2,
            Auth = 3
        }

        private const int MaxAllowedPacketSize = 4096;

        /// <summary>
        ///     Indicates the encoding of the packet payload.
        ///     Is either ASCII or UTF8
        /// </summary>
        private readonly Encoding _packetEncoding = Encoding.ASCII;

        /// <summary>
        ///     The identifier of the packet
        ///     (It need not be unique, but if a unique packet id is assigned,
        ///     it can be used to match incoming responses to their corresponding requests.)
        /// </summary>
        public readonly int Id;

        /// <summary>
        ///     The payload of the packet
        /// </summary>
        public readonly string Payload;

        /// <summary>
        ///     The size of the packet, excluding the size field itself.
        /// </summary>
        public readonly int Size;

        /// <summary>
        ///     Indication of the purpose of the packet
        ///     Can be any of <see cref="PacketType" />
        /// </summary>
        public readonly PacketType Type;

        public RemoteConPacket(byte[] packetBytes, bool useUtf8 = false)
        {
            if (useUtf8)
                _packetEncoding = Encoding.UTF8;

            using (var ms = new MemoryStream(packetBytes))
            {
                using (var reader = new BinaryReaderExt(ms))
                {
                    Size = reader.ReadInt32LittleEndian();

                    // The size field (4-Bytes Little Endian Int) is, according to specification, not included.
                    if (Size + 4 != packetBytes.Length)
                        throw new LengthMismatchException("packet length mismatch");

                    Id = reader.ReadInt32LittleEndian();

                    var packetType = reader.ReadInt32LittleEndian();
                    if (!Enum.IsDefined(typeof(PacketType), packetType))
                        throw new InvalidPacketTypeException("Invalid packet type");
                    Type = (PacketType)Enum.ToObject(typeof(PacketType), packetType);

                    if (!useUtf8)
                    {
                        Payload = reader.ReadAscii();

                        // Get payload length by subtracting 9 bytes (ID 4-Bytes, Type 4-Bytes, Null-terminator 1-Byte)
                        if (Encoding.ASCII.GetByteCount(Payload) > Size - 9)
                            throw new LengthMismatchException("Payload length mismatch");
                    }
                    else
                    {
                        Payload = Encoding.UTF8.GetString(reader.ReadBytes(Size - 10));
                        reader.ReadByte();
                    }

                    var nullTerminator = reader.ReadByte();
                    if (nullTerminator != 0x00)
                        throw new NullTerminatorMissingException("Missing last null-terminator");

                    if (reader.BaseStream.Position != reader.BaseStream.Length)
                        throw new Exception("More data to read");
                }
            }
        }

        public RemoteConPacket(int id, PacketType type, string payload, bool useUtf8 = false)
        {
            if (useUtf8)
                _packetEncoding = Encoding.UTF8;

            Payload = payload;
            Id = id;
            Type = type;
            if (Length > MaxAllowedPacketSize)
                throw new PacketTooLongException();
        }

        /// <summary>
        ///     The total size of the packet
        /// </summary>
        private int Length
        {
            get { return _packetEncoding.GetBytes(Payload + '\0').Length + 13; }
        }

        /// <summary>
        ///     Writes the packet to a byte-array
        /// </summary>
        /// <returns>The packet in byte array</returns>
        public byte[] GetBytes()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriterExt(ms))
                {
                    var bodyBytes = _packetEncoding.GetBytes(Payload + '\0');
                    writer.WriteLittleEndian(bodyBytes.Length + 9);
                    writer.WriteLittleEndian(Id);
                    writer.WriteLittleEndian((int)Type);
                    writer.Write(bodyBytes);
                    writer.Write('\0');

                    return ms.ToArray();
                }
            }
        }
    }
}