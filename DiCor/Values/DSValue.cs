using System.Globalization;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct DSValue : IValue<DSValue>
    {
        private readonly decimal _decimal;

        public DSValue(decimal @decimal)
            => _decimal = @decimal;

        public decimal Decimal
            => _decimal;

        public static VR VR
            => VR.DS;

        public static int MaximumLength
            => 16;

        public static bool IsFixedLength
            => false;

        public static byte Padding
            => (byte)' ';

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(decimal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DSValue Create<T>(T content)
        {
            if (typeof(T) == typeof(decimal))
            {
                return new DSValue(Unsafe.As<T, decimal>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DSValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(decimal))
            {
                return Unsafe.As<decimal, T>(ref Unsafe.AsRef(in _decimal));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(DSValue));
                return default;
            }
        }

        public override string ToString()
            => _decimal.ToString("G15", CultureInfo.InvariantCulture);
    }
}
