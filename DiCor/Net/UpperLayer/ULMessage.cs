using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor.Net.UpperLayer
{
    public struct ULMessage
    {
        public uint Length;
        public Pdu.Type Type;
        public long Data;

        public static ULMessage FromData<TData>(TData data) where TData : struct
            => new()
        {
            Length = 0,
            Type = GetType(ref data),
            Data = Unsafe.As<TData, long>(ref data),
        };

        private static Pdu.Type GetType<TData>(ref TData data) where TData : struct
            => data switch
            {
                AAssociateRqData => Pdu.Type.AAssociateRq,
                AAssociateAcData => Pdu.Type.AAssociateAc,
                AAssociateRjData => Pdu.Type.AAssociateRj,
                AAbortData => Pdu.Type.AAbort,
                _ => throw new InvalidOperationException(),
            };

        public TData GetData<TData>() where TData : struct
        {
            Debug.Assert(Unsafe.SizeOf<TData>() <= sizeof(long));

            return Unsafe.As<long, TData>(ref Data);
        }
    }

    public struct AAssociateRqData
    {
        public Association Association;
    }

    public struct AAssociateAcData
    {
        public Association Association;
    }

    public struct AAssociateRjData
    {
        public Pdu.RejectResult Result;
        public Pdu.RejectSource Source;
        public Pdu.RejectReason Reason;
    }

    public struct AAbortData
    {
        public Pdu.AbortSource Source;
        public Pdu.AbortReason Reason;
    }
}
