using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct DAValue<TIsQueryContext> : IValue<DAValue<TIsQueryContext>>
        where TIsQueryContext : struct, IRuntimeConst
    {
        private static readonly DateOnly s_InvalidDate = InvalidDate();

        private readonly (DateOnly, DateOnly) _dates;

        public DAValue(DateOnly date)
        {
            _dates = (date, s_InvalidDate);
        }

        public DAValue(DateOnly minDate, DateOnly maxDate)
        {
            if (!TIsQueryContext.Value)
                throw new InvalidOperationException("DAValue can only accept 2 dates in context of a query.");
            ArgumentOutOfRangeException.ThrowIfGreaterThan(minDate, maxDate);

            _dates = (minDate, maxDate);
        }

        public DAValue(EmptyValue emptyValue)
        {
            if (!TIsQueryContext.Value)
                throw new InvalidOperationException("DAValue can only be an empty value in context of a query.");

            _dates = (s_InvalidDate, s_InvalidDate);
        }

        public static int MaximumLength
            => TIsQueryContext.Value ? 18 : 8;

        public static bool IsFixedLength
            => !TIsQueryContext.Value;

        public static bool IsCompatible<T>()
            => TIsQueryContext.Value
            ? typeof(T) == typeof(ValueTuple<DateOnly, DateOnly>)
            : typeof(T) == typeof(DateOnly);

        public bool IsSingleValue => _dates.Item1 != s_InvalidDate && _dates.Item2 == s_InvalidDate;

        public bool IsEmptyValue => _dates == (s_InvalidDate, s_InvalidDate);

        public bool IsRange => _dates.Item1 != s_InvalidDate && _dates.Item2 != s_InvalidDate;

        public T Get<T>()
        {
            if (typeof(T) == typeof(DateOnly) && IsSingleValue)
                return Unsafe.As<DateOnly, T>(ref Unsafe.AsRef(in _dates.Item1));

            if (TIsQueryContext.Value && typeof(T) == typeof((DateOnly, DateOnly)) && IsRange)
                return Unsafe.As<(DateOnly, DateOnly), T>(ref Unsafe.AsRef(in _dates));

            return Value.ThrowIncompatible<T>(nameof(DAValue<TIsQueryContext>));
        }

        private static DateOnly InvalidDate()
        {
            int i = -1;
            return (Unsafe.As<int, DateOnly>(ref i));
        }
    }
}
