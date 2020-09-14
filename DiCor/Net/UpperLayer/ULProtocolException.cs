using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiCor.Net.UpperLayer
{
    public class ULProtocolException : Exception
    {
        public ULProtocolException(string message)
            : base(message)
        { }

        public ULProtocolException(ULConnectionState expected, ULConnectionState actual)
            : this($"Expected connection state: '{expected}', actual connection state: '{actual}'.")
        { }
    }
}
