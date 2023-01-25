using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct DAValue<TIsQueryContext> : IValue<DAValue<TIsQueryContext>>
        where TIsQueryContext : struct, IIsQueryContext
    {
        private readonly (DateOnly Low, DateOnly Hi) _dates;

        public DAValue(DateOnly date)
        {
            _dates = (date, s_InvalidDate);
        }

        public DAValue(DateOnly lowDate, DateOnly hiDate)
        {
            if (!TIsQueryContext.Value)
                throw new InvalidOperationException("DAValue can only accept 2 dates in context of a query.");
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lowDate, hiDate);

            _dates = (lowDate, hiDate);
        }

        private DAValue((DateOnly LowDate, DateOnly HiDate) dates)
            : this(dates.LowDate, dates.HiDate)
        { }

        public DAValue(EmptyValue emptyValue)
        {
            if (!TIsQueryContext.Value)
                throw new InvalidOperationException("DAValue can only be an empty value in context of a query.");

            _dates = (s_InvalidDate, s_InvalidDate);
        }

        public bool IsEmptyValue
            => _dates == (s_InvalidDate, s_InvalidDate);

        public bool IsSingleDate
            => _dates.Low != s_InvalidDate && _dates.Hi == s_InvalidDate;

        public bool IsDateRange
            => _dates.Low != s_InvalidDate && _dates.Hi != s_InvalidDate;

        public DateOnly Date
            => IsSingleDate ? _dates.Low : throw new InvalidOperationException("The DAValue is empty or contains a date range.");

        public (DateOnly loDate, DateOnly hiDate) DateRange
            => IsDateRange ? _dates : throw new InvalidOperationException("The DAValue is empty or contains a single date.");

        public static VR VR
            => VR.DA;

        public static int MaximumLength
            => TIsQueryContext.Value ? 18 : 8;

        public static bool IsFixedLength
            => !TIsQueryContext.Value;

        public static byte Padding
            => (byte)' ';

        public static int PageSize
            => 10;

        //public static bool IsCompatible<T>()
        //    => TIsQueryContext.Value
        //    ? typeof(T) == typeof(ValueTuple<DateOnly, DateOnly>)
        //    : typeof(T) == typeof(DateOnly);

        public static DAValue<TIsQueryContext> Create<T>(T content)
        {
            if (typeof(T) == typeof(DateOnly))
                return new DAValue<TIsQueryContext>(Unsafe.As<T, DateOnly>(ref content));

            if (typeof(T) == typeof((DateOnly, DateOnly)))
                return new DAValue<TIsQueryContext>(Unsafe.As<T, (DateOnly, DateOnly)>(ref content));

            if (typeof(T) == typeof(EmptyValue))
                return new DAValue<TIsQueryContext>(default(EmptyValue));

            Value.ThrowIncompatible<T>(nameof(DAValue<TIsQueryContext>));
            return default;
        }

        public T Get<T>()
        {
            if (typeof(T) == typeof(DateOnly) && IsSingleDate)
                return Unsafe.As<DateOnly, T>(ref Unsafe.AsRef(in _dates.Low));

            if (TIsQueryContext.Value && typeof(T) == typeof((DateOnly, DateOnly)) && IsDateRange)
                return Unsafe.As<(DateOnly, DateOnly), T>(ref Unsafe.AsRef(in _dates));

            Value.ThrowIncompatible<T>(nameof(DAValue<TIsQueryContext>));
            return default;
        }

        private static readonly DateOnly s_InvalidDate = InvalidDate();
        private static DateOnly InvalidDate()
        {
            int i = -1;
            return (Unsafe.As<int, DateOnly>(ref i));
        }
    }
}
