using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct OxValue<TBinary> : IValue<OxValue<TBinary>>
        where TBinary : unmanaged
    {
        private readonly TBinary[] _array;

        public OxValue(TBinary[] array)
            => _array = array;

        public TBinary[] Array
            => _array;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(TBinary[]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OxValue<TBinary> Create<T>(T content)
        {
            if (typeof(T) == typeof(TBinary[]))
            {
                return new OxValue<TBinary>(Unsafe.As<T, TBinary[]>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(OxValue<TBinary>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(TBinary[]))
            {
                return Unsafe.As<TBinary[], T>(ref Unsafe.AsRef(in _array));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(OxValue<TBinary>));
                return default;
            }
        }
    }
}
