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
        static AbstractValue AsAbstract(TValue value)
            => Unsafe.As<TValue, AbstractValue>(ref value);
        static abstract VR VR { get; }
        static abstract int MaximumLength { get; }
        static abstract bool IsFixedLength { get; }
        static abstract byte Padding { get; }
        static abstract int PageSize { get; }
        static abstract TValue Create<T>(T content);
        virtual bool IsEmptyValue => false;
        T Get<T>();
        //AbstractValue AsAbstract()
        //{
        //    // BOXING?
        //    TValue value = (TValue)this;
        //    return Unsafe.As<TValue, AbstractValue>(ref value);
        //}
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct AbstractValue
    {
        public TValue As<TValue>()
            where TValue : struct, IValue<TValue>
            => Unsafe.As<AbstractValue, TValue>(ref this);
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
    public interface IIsQueryContext
    {
        static abstract bool Value { get; }
    }

    public struct IsQueryContext : IIsQueryContext
    {
        public static bool Value => true;
    }
    public struct IsNotQueryContext : IIsQueryContext
    {
        public static bool Value => false;
    }
}
