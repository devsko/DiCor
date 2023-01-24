using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DiCor.Values
{
    public interface IValue<TValue> where TValue : IValue<TValue>
    {
        static abstract int MaximumLength { get; }
        static abstract bool IsFixedLength { get; }
        static virtual byte Padding => (byte)' ';
        static abstract bool IsCompatible<T>();

        virtual bool IsEmptyValue => false;
        T Get<T>();
        //void Set<TValue>(T value);
    }

    public struct EmptyValue
    { }

    public static class Value
    {
        public static ReadOnlySpan<byte> DoubleQuotationMark => "\"\""u8;

        [DoesNotReturn]
        [StackTraceHidden]
        internal static T ThrowIncompatible<T>(string valueTypeName)
            => throw new ArgumentException($"{nameof(T)} is not compatible with {valueTypeName}.");
    }
    public interface IRuntimeConst
    {
        static abstract bool Value { get; }
    }

    public struct TrueConst : IRuntimeConst
    {
        public static bool Value => true;
    }
    public struct FalseConst : IRuntimeConst
    {
        public static bool Value => false;
    }
}
