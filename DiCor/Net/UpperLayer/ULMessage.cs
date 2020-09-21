using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor.Net.UpperLayer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ULMessage
    {
        public uint Length;
        public Pdu.Type Type;
        private IntPtr _;
    }

    public static class ULMessageExtensions
    {
        public static ref ULMessage<TData> To<TData>(ref this ULMessage message) where TData : struct
            => ref Unsafe.As<ULMessage, ULMessage<TData>>(ref message);

        public static ref ULMessage ToMessage<TData>(ref this ULMessage<TData> message) where TData : struct
            => ref Unsafe.As<ULMessage<TData>, ULMessage>(ref message);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ULMessage<TData> where TData : struct
    {
        public Pdu.Type Type;
        public uint Length;
        public TData Data;

        public ULMessage(TData data)
        {
            Type = GetType(typeof(TData));
            Length = 0;
            Data = data;
        }

        private static readonly Dictionary<System.Type, Pdu.Type> s_typeMap = new Dictionary<System.Type, Pdu.Type>
        {
            { typeof(AAssociateRqData), Pdu.Type.AAssociateRq },
            { typeof(AAssociateAcData), Pdu.Type.AAssociateAc },
            { typeof(AAssociateRjData), Pdu.Type.AAssociateRj },
            { typeof(AAbortData), Pdu.Type.AAbort },
        };

        private static Pdu.Type GetType(System.Type dataType)
            => s_typeMap[dataType];

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
