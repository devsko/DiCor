using System;
using System.Collections.Generic;
using System.Text;

namespace DiCor.Net.UpperLayer
{
    public enum ULPduType : byte
    {
        AAssociateRq = 0x01,
        AAssociateAc = 0x02,
        AAssociateRj = 0x03,
        AAbort = 0x07,
    }

    public unsafe struct ULMessage
    {
        public ULPduType Type { get; }
        public byte B1 { get; }
        public byte B2 { get; }

        public ULMessage(ULPduType type, byte b1 = 0, byte b2 = 0)
        {
            Type = type;
            B1 = b1;
            B2 = b2;
        }
    }
}
