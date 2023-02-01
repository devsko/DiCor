using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct ULValue : IValue<ULValue>
    {
        private readonly uint _integer;

        public ULValue(uint integer)
            => _integer = integer;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(uint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ULValue Create<T>(T content)
        {
            if (typeof(T) == typeof(uint))
            {
                return new ULValue(Unsafe.As<T, uint>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ULValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(uint))
            {
                return Unsafe.As<uint, T>(ref Unsafe.AsRef(in _integer));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ULValue));
                return default;
            }
        }

        public override string ToString()
            => _integer.ToString();
    }
}
