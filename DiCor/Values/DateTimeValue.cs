using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct DateTimeValue<TDateTime> : IValue<DateTimeValue<TDateTime>>
        where TDateTime : struct
    {
        private readonly TDateTime _value;

        public DateTimeValue(TDateTime value)
            => _value = value;

        public static int PageSize
            => 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(TDateTime);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTimeValue<TDateTime> Create<T>(T content)
        {
            if (typeof(T) == typeof(TDateTime))
            {
                return new DateTimeValue<TDateTime>(Unsafe.As<T, TDateTime>(ref content));
            }
            else
            {
                Values.Value.ThrowIncompatible<T>(nameof(DateTimeValue<TDateTime>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(TDateTime))
            {
                return Unsafe.As<TDateTime, T>(ref Unsafe.AsRef(in _value));
            }
            else
            {
                Values.Value.ThrowIncompatible<T>(nameof(DateTimeValue<TDateTime>));
                return default;
            }
        }

        public override string? ToString()
        {
            if (typeof(TDateTime) == typeof(PartialTimeOnly))
                return $"Time {_value}";

            if (typeof(TDateTime) == typeof(DateOnly))
                return $"Date {_value:yyyyMMdd}";

            return $"Date/Time {_value}";
        }

        internal static string ToString(TDateTime dateTime)
        {
            return typeof(TDateTime) == typeof(DateOnly)
                    ? Unsafe.As<TDateTime, DateOnly>(ref dateTime).ToString("yyyyMMdd")
                    : dateTime.ToString()!;
        }
    }
}
