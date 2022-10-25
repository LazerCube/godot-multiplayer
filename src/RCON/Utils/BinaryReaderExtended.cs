using System;
using System.IO;
using System.Text;

namespace RCON.Utils
{
    internal class BinaryReaderExt : BinaryReader
    {
        public BinaryReaderExt(Stream stream)
            : base(stream, Encoding.Unicode)
        {
        }

        /// <summary>
        ///     Reads the next 4-Bytes as Little-Endian if we're in a big-endian environment
        /// </summary>
        /// <returns></returns>
        public int ReadInt32LittleEndian()
        {
            var intBytes = ReadBytes(4);
            var bytes = new byte[4];
            Array.Copy(intBytes, 0, bytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        ///     Reads a ASCII string (Null-terminated string) without the null-terminator
        /// </summary>
        /// <returns></returns>
        public string ReadAscii()
        {
            var sb = new StringBuilder();
            byte val;
            do
            {
                val = ReadByte();
                if (val > 0)
                    sb.Append((char)val);
            } while (val > 0);

            return sb.ToString();
        }
    }
}