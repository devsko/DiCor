using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DiCor.Values
{
    public interface IValue<TValue>
        where TValue : struct, IValue<TValue>
    {
        static abstract VR VR { get; }
        static abstract int MaximumLength { get; }
        static abstract bool IsFixedLength { get; }
        static abstract byte Padding { get; }
        static abstract int PageSize { get; }
        static abstract bool IsCompatible<T>();
        static abstract TValue Create<T>(T content);
        T Get<T>();
    }

    public interface IQueryableValue<TValue> : IValue<TValue>
        where TValue : struct, IQueryableValue<TValue>
    {
        bool IsEmptyValue { get; }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct AbstractValue
    {
        public TValue As<TValue>()
            where TValue : struct, IValue<TValue>
            => Unsafe.As<AbstractValue, TValue>(ref this);

        public static AbstractValue Of<TValue>(TValue value)
            where TValue : struct, IValue<TValue>
            => Unsafe.As<TValue, AbstractValue>(ref value);
    }

    public struct EmptyValue
    { }

    public static class Value
    {
        public static ReadOnlySpan<byte> DoubleQuotationMark => "\"\""u8;

        [DoesNotReturn]
        [StackTraceHidden]
        internal static void ThrowIncompatible<T>(string valueTypeName)
            => throw new ArgumentException($"{nameof(T)} is not compatible with {valueTypeName}.");
    }
    public interface IIsInQuery
    {
        static abstract bool Value { get; }
    }

    public struct InQuery : IIsInQuery
    {
        public static bool Value => true;
    }
    public struct NotInQuery : IIsInQuery
    {
        public static bool Value => false;
    }
}
