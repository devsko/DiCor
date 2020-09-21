using System;

namespace DiCor.Net.UpperLayer
{
    public class ULProtocolException : Exception
    {
        public ULProtocolException()
            : base()
        { }

        public ULProtocolException(string message)
            : base(message)
        { }

        public ULProtocolException(ULConnection.State expected, ULConnection.State actual)
            : this($"Expected connection state: '{expected}', actual connection state: '{actual}'.")
        { }
    }
}
