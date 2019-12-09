using System;
using System.Collections.Generic;
using System.Text;

namespace DiCor.Net.UpperLayer
{

    public unsafe struct ULMessage
    {
        public Pdu.Type Type { get; }
        public byte B1 { get; }
        public byte B2 { get; }

        public ULMessage(Pdu.Type type, byte b1 = 0, byte b2 = 0)
        {
            Type = type;
            B1 = b1;
            B2 = b2;
        }
    }
}
