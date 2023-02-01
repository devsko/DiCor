using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal interface IValue<TValue>
        where TValue : struct, IValue<TValue>
    {
        static abstract int PageSize { get; }
        static abstract bool IsCompatible<T>();
        static abstract TValue Create<T>(T content);
        T Get<T>();
    }

    internal interface IQueryableValue<TValue> : IValue<TValue>
        where TValue : struct, IQueryableValue<TValue>
    {
        bool IsEmptyValue { get; }
    }

    internal readonly ref struct ValueRef
    {
        private struct AbstractValue
        { }

        private readonly ref AbstractValue _value;

        private ValueRef(ref AbstractValue value)
        {
            _value = ref value;
        }

        public bool IsNullRef()
            => Unsafe.IsNullRef(ref Unsafe.AsRef(in _value));

        public ref TValue As<TValue>()
            where TValue : struct, IValue<TValue>
            => ref Unsafe.As<AbstractValue, TValue>(ref _value);

        public bool Set<TValue>(TValue value)
            where TValue : struct, IValue<TValue>
        {
            ref TValue valueRef = ref Unsafe.As<AbstractValue, TValue>(ref _value);
            valueRef = value;
            return true;
        }

        public static ValueRef Of<TValue>(ref TValue value)
            where TValue : struct, IValue<TValue>
            => new ValueRef(ref Unsafe.As<TValue, AbstractValue>(ref value));
    }

    public readonly struct QueryEmpty
    { }

    internal static class Value
    {
        public const byte Backslash = (byte)'\\';
        public static ReadOnlySpan<byte> DoubleQuotationMark => "\"\""u8;

        public const string QueryEmptyDisplay = "<query empty>";

        public static QueryEmpty QueryEmpty => default;

        [DoesNotReturn]
        [StackTraceHidden]
        internal static void ThrowIncompatible<T>(string valueTypeName)
            => throw new ArgumentException($"{nameof(T)} is not compatible with {valueTypeName}.");
    }
    internal interface IIsInQuery
    {
        static abstract bool Value { get; }
    }

    internal struct InQuery : IIsInQuery
    {
        public static bool Value => true;
    }

    internal struct NotInQuery : IIsInQuery
    {
        public static bool Value => false;
    }
}
