using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DiCor.Values
{
    public interface IValue<T> where T : IValue<T>
    {
        static abstract int MaximumLength { get; }
        static virtual byte Padding => (byte)' ';
        static abstract bool IsCompatible<TValue>();

        virtual bool IsEmptyValue => false;
        TValue Get<TValue>();
        //void Set<TValue>(TValue value);
    }

    public struct EmptyValue
    { }

    public static class Value
    {
        public static ReadOnlySpan<byte> DoubleQuotationMark => "??"u8;

        [DoesNotReturn]
        [StackTraceHidden]
        internal static void ThrowIncompatible<T>(string valueTypeName)
            => throw new ArgumentException($"{nameof(T)} is not compatible with {valueTypeName}.");
    }
}
