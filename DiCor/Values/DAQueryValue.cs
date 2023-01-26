using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct DAQueryValue : IQueryableValue<DAQueryValue>
    {
        private readonly (DateOnly Low, DateOnly Hi) _dates;

        public DAQueryValue(DateOnly date)
            => _dates = (date, s_InvalidDate);

        public DAQueryValue(DateOnly lowDate, DateOnly hiDate)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lowDate, hiDate);

            _dates = (lowDate, hiDate);
        }

        private DAQueryValue((DateOnly LowDate, DateOnly HiDate) dates)
            : this(dates.LowDate, dates.HiDate)
        { }

        public DAQueryValue(EmptyValue emptyValue)
            => _dates = (s_InvalidDate, s_InvalidDate);

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
            => 18;

        public static bool IsFixedLength
            => false;

        public static byte Padding
            => (byte)' ';

        public static int PageSize
            => 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => (typeof(T) == typeof(DateOnly) ||
                typeof(T) == typeof((DateOnly, DateOnly)) ||
                typeof(T) == typeof(EmptyValue));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DAQueryValue Create<T>(T content)
        {
            if (typeof(T) == typeof(DateOnly))
            {
                return new DAQueryValue(Unsafe.As<T, DateOnly>(ref content));
            }
            else if (typeof(T) == typeof((DateOnly, DateOnly)))
            {
                return new DAQueryValue(Unsafe.As<T, (DateOnly, DateOnly)>(ref content));
            }
            else if (typeof(T) == typeof(EmptyValue))
            {
                return new DAQueryValue(default(EmptyValue));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DAQueryValue));
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
            else if (typeof(T) == typeof((DateOnly, DateOnly)) && IsDateRange)
            {
                return Unsafe.As<(DateOnly, DateOnly), T>(ref Unsafe.AsRef(in _dates));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DAQueryValue));
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
