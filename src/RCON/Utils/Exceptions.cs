using System;

namespace RCON.Utils
{
    public class RconServerException : Exception
    {
        protected RconServerException()
        {
        }

        protected RconServerException(string message) : base(message)
        {
        }
    }

    public class NotAuthenticatedException : RconServerException
    {
    }

    public class EmptyPacketPayloadException : RconServerException
    {
    }

    public class PacketTooLongException : RconServerException
    {
    }

    public class NullTerminatorMissingException : RconServerException
    {
        public NullTerminatorMissingException(string message) : base(message)
        {
        }
    }

    public class LengthMismatchException : RconServerException
    {
        public LengthMismatchException(string message) : base(message)
        {
        }
    }

    public class InvalidPacketTypeException : RconServerException
    {
        public InvalidPacketTypeException()
        {
        }

        public InvalidPacketTypeException(string message) : base(message)
        {
        }
    }
}