using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct DAValue<TIsQueryContext> : IValue<DAValue<TIsQueryContext>>
        where TIsQueryContext : struct, IRuntimeConst
    {
        private enum Flags : byte
        {
            IsEmpty = 0x01,
            IsRange = 0x02,
        }
        private readonly (DateOnly, DateOnly) _dates;
        private readonly Flags _flags;

        public DAValue(DateOnly date)
        {
            _dates = (date, default);
        }

        public DAValue(DateOnly minDate, DateOnly maxDate)
        {
            if (!TIsQueryContext.Value)
                throw new InvalidOperationException("DAValue can only accept 2 dates in context of a query.");

            _dates = (minDate, maxDate);
            _flags = Flags.IsRange;
        }

        public DAValue(EmptyValue emptyValue)
        {
            if (!TIsQueryContext.Value)
                throw new InvalidOperationException("DAValue can only be an empty value in context of a query.");

            _flags = Flags.IsEmpty;
        }

        public static int MaximumLength
            => TIsQueryContext.Value ? 18 : 8;

        public static bool IsCompatible<T>()
            => TIsQueryContext.Value
            ? typeof(T) == typeof(ValueTuple<DateOnly, DateOnly>)
            : typeof(T) == typeof(DateOnly);

        public bool IsSingleValue => _flags == (Flags)0;

        public bool IsEmptyValue => _flags == Flags.IsEmpty;

        public bool IsRange => _flags == Flags.IsRange;

        public T Get<T>()
        {
            if (typeof(T) == typeof(DateOnly) && IsSingleValue)
                return Unsafe.As<DateOnly, T>(ref Unsafe.AsRef(in _dates.Item1));

            if (TIsQueryContext.Value && typeof(T) == typeof((DateOnly, DateOnly)) && IsRange)
                return Unsafe.As<(DateOnly, DateOnly), T>(ref Unsafe.AsRef(in _dates));

            Value.ThrowIncompatible<T>(nameof(DAValue<TIsQueryContext>));
            return default;
        }

        //public void Set<T>(T value)
        //{
        //    if (typeof(T) == typeof(DateOnly))
        //    {
        //        _dates = (Unsafe.As<T, DateOnly>(ref value), default);
        //    }
        //    else if (typeof(T) == typeof((DateOnly, DateOnly)))
        //    {
        //        _dates = Unsafe.As<T, (DateOnly, DateOnly)>(ref value);
        //    }
        //    else
        //    {
        //        Value.ThrowIncompatible<T>(nameof(DAValue<TIsQueryContext>));
        //    }
        //}
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
