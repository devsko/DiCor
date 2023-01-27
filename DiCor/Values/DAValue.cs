using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct DAValue : IValue<DAValue>
    {
        private readonly DateOnly _date;

        public DAValue(DateOnly date)
            => _date = date;

        public DateOnly Date
            => _date;

        public static VR VR
            => VR.DA;

        public static int MaximumLength
            => 8;

        public static bool IsFixedLength
            => true;

        public static byte Padding
            => (byte)' ';

        public static int PageSize
            => 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(DateOnly);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DAValue Create<T>(T content)
        {
            if (typeof(T) == typeof(DateOnly))
            {
                return new DAValue(Unsafe.As<T, DateOnly>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DAValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(DateOnly))
            {
                return Unsafe.As<DateOnly, T>(ref Unsafe.AsRef(in _date));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DAValue));
                return default;
            }
        }
    }
}
