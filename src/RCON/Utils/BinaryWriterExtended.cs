using System;
using System.IO;
using System.Text;

namespace RCON.Utils
{
    internal class BinaryWriterExt : BinaryWriter
    {
        public BinaryWriterExt(Stream stream)
            : base(stream, Encoding.Unicode)
        {
        }

        /// <summary>
        ///     Writes the int as little endian if we're in a big-endian environment
        /// </summary>
        /// <param name="val">The value to write</param>
        public void WriteLittleEndian(int val)
        {
            var bytes = BitConverter.GetBytes(val);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            Write(bytes);
        }
    }
}