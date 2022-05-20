using System;

namespace IxMilia.Stl
{
    internal class StlReadException : Exception
    {
        public StlReadException()
            : base()
        {
        }

        public StlReadException(string message)
            : base(message)
        {
        }

        public StlReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
