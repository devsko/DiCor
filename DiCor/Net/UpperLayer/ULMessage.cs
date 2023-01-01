using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor.Net.UpperLayer
{
    public struct ULMessage
    {
        private Pdu.Type _type;
        private long _data;

        public Pdu.Type Type => _type;

        public static ULMessage FromData<TData>(TData data) where TData : struct
            => new()
            {
                _type = GetType(ref data),
                _data = Unsafe.As<TData, long>(ref data),
            };

        public ULMessage(Pdu.Type type)
        {
            _type = type;
        }

        public TData GetData<TData>() where TData : struct
        {
            TData data = Unsafe.As<long, TData>(ref _data);
            if (GetType(ref data) != _type)
                throw new ArgumentException();

            return data;
        }

        private static Pdu.Type GetType<TData>(ref TData data) where TData : struct
            => data switch
            {
                AAssociateRqData => Pdu.Type.AAssociateRq,
                AAssociateAcData => Pdu.Type.AAssociateAc,
                AAssociateRjData => Pdu.Type.AAssociateRj,
                PDataTfData => Pdu.Type.PDataTf,
                ARelaseRqData => Pdu.Type.AReleaseRq,
                ARelaseRpData => Pdu.Type.AReleaseRp,
                AAbortData => Pdu.Type.AAbort,

                _ => throw new InvalidOperationException(),
            };
    }

    public struct AAssociateRqData
    { }

    public struct AAssociateAcData
    { }

    public struct AAssociateRjData
    {
        public Pdu.RejectResult Result;
        public Pdu.RejectSource Source;
        public Pdu.RejectReason Reason;
    }

    public struct PDataTfData
    {

    }

    public struct ARelaseRqData
    { }

    public struct ARelaseRpData
    { }

    public struct AAbortData
    {
        public Pdu.AbortSource Source;
        public Pdu.AbortReason Reason;
    }
}
