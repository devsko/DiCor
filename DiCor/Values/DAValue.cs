using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct DAValue<TIsQuery> : IQueryableValue<DAValue<TIsQuery>>
        where TIsQuery : struct, IIsInQuery
    {
        private readonly (DateOnly Low, DateOnly Hi) _dates;

        public DAValue(DateOnly date)
        {
            _dates = (date, s_InvalidDate);
        }

        public DAValue(DateOnly lowDate, DateOnly hiDate)
        {
            if (!TIsQuery.Value)
                throw new InvalidOperationException("DAValue can only accept 2 dates in context of a query.");
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lowDate, hiDate);

            _dates = (lowDate, hiDate);
        }

        private DAValue((DateOnly LowDate, DateOnly HiDate) dates)
            : this(dates.LowDate, dates.HiDate)
        { }

        public DAValue(EmptyValue emptyValue)
        {
            if (!TIsQuery.Value)
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
            => TIsQuery.Value ? 18 : 8;

        public static bool IsFixedLength
            => !TIsQuery.Value;

        public static byte Padding
            => (byte)' ';

        public static int PageSize
            => 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
        {
            if (typeof(T) == typeof(DateOnly))
            {
                return true;
            }
            else if (typeof(T) == typeof((DateOnly, DateOnly)) || typeof(T) == typeof(EmptyValue))
            {
                return TIsQuery.Value;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DAValue<TIsQuery> Create<T>(T content)
        {
            if (typeof(T) == typeof(DateOnly))
            {
                return new DAValue<TIsQuery>(Unsafe.As<T, DateOnly>(ref content));
            }
            else if (typeof(T) == typeof((DateOnly, DateOnly)))
            {
                return new DAValue<TIsQuery>(Unsafe.As<T, (DateOnly, DateOnly)>(ref content));
            }
            else if (typeof(T) == typeof(EmptyValue))
            {
                return new DAValue<TIsQuery>(default(EmptyValue));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DAValue<TIsQuery>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(DateOnly) && IsSingleDate)
            {
                return Unsafe.As<DateOnly, T>(ref Unsafe.AsRef(in _dates.Low));
            }
            else if (typeof(T) == typeof((DateOnly, DateOnly)) && TIsQuery.Value && IsDateRange)
            {
                return Unsafe.As<(DateOnly, DateOnly), T>(ref Unsafe.AsRef(in _dates));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DAValue<TIsQuery>));
                return default;
            }
        }

        private static readonly DateOnly s_InvalidDate = InvalidDate();
        private static DateOnly InvalidDate()
        {
            int i = -1;
            return (Unsafe.As<int, DateOnly>(ref i));
        }
    }
}
