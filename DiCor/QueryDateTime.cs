using System;
using System.Runtime.CompilerServices;

namespace DiCor
{
    public readonly struct QueryDateTime<T>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private static readonly (T Min, T Max, T Invalid) s_valuesOfT = GetValuesOfT();

        private readonly T _lo;
        private readonly T _hi;

        private QueryDateTime(T lo, T hi)
        {
            _lo = lo;
            _hi = hi;
        }

        public bool IsQueryEmpty
            => _lo.Equals(s_valuesOfT.Invalid) && _hi.Equals(s_valuesOfT.Invalid);

        public bool IsSingle
            => !_lo.Equals(s_valuesOfT.Invalid) && _hi.Equals(s_valuesOfT.Invalid);

        public bool IsRange
            => !_lo.Equals(s_valuesOfT.Invalid) && !_hi.Equals(s_valuesOfT.Invalid);

        public T Single
            => IsSingle ? _lo : throw new InvalidOperationException();

        public (T Lo, T Hi) Range
            => IsRange ? (_lo, _hi) : throw new InvalidOperationException();

        public static QueryDateTime<T> FromSingle(T value)
            => new QueryDateTime<T>(value, s_valuesOfT.Invalid);

        public static QueryDateTime<T> FromRange(T lo, T hi)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lo, hi);

            return new QueryDateTime<T>(lo, hi);
        }

        public static QueryDateTime<T> FromRange((T Lo, T Hi) range)
            => FromRange(range.Lo, range.Hi);

        public static QueryDateTime<T> FromLo(T lo)
            => new QueryDateTime<T>(lo, s_valuesOfT.Max);

        public static QueryDateTime<T> FromHi(T hi)
            => new QueryDateTime<T>(s_valuesOfT.Min, hi);

        public static QueryDateTime<T> FromQueryEmpty()
            => new QueryDateTime<T>(s_valuesOfT.Invalid, s_valuesOfT.Invalid);

        private static (T, T, T) GetValuesOfT()
        {
            if (typeof(T) == typeof(DateOnly))
            {
                int invalid = -1;
                DateOnly min = DateOnly.MinValue;
                DateOnly max = DateOnly.MaxValue;

                return (Unsafe.As<DateOnly, T>(ref min), Unsafe.As<DateOnly, T>(ref max), Unsafe.As<int, T>(ref invalid));
            }
            if (typeof(T) == typeof(TimeOnly))
            {
                long invalid = -1;
                TimeOnly min = TimeOnly.MinValue;
                TimeOnly max = TimeOnly.MaxValue;

                return (Unsafe.As<TimeOnly, T>(ref min), Unsafe.As<TimeOnly, T>(ref max), Unsafe.As<long, T>(ref invalid));
            }
            if (typeof(T) == typeof(PartialDateTime))
            {
                Int128 invalid = -1;
                PartialDateTime min = new(DateTime.MinValue, TimeSpan.Zero, containsUtcOffset: false);
                PartialDateTime max = new(DateTime.MaxValue, TimeSpan.Zero, containsUtcOffset: false);

                return (Unsafe.As<PartialDateTime, T>(ref min), Unsafe.As<PartialDateTime, T>(ref max), Unsafe.As<Int128, T>(ref invalid));
            }

            throw new NotImplementedException();
        }
    }
}
