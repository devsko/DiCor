using System;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct OValue<TBinary> : IValue<OValue<TBinary>>
        where TBinary : unmanaged
    {
        private readonly TBinary[] _array;

        public OValue(TBinary[] array)
            => _array = array;

        public TBinary[] Array
            => _array;

        public static VR VR
        {
            get
            {
                if (typeof(TBinary) == typeof(byte))
                    return VR.OB;

                throw new NotImplementedException();
            }
        }

        public static int MaximumLength
            => 0;

        public static bool IsFixedLength
            => false;

        public static byte Padding
            => 0;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(TBinary[]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OValue<TBinary> Create<T>(T content)
        {
            if (typeof(T) == typeof(TBinary[]))
            {
                return new OValue<TBinary>(Unsafe.As<T, TBinary[]>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(OValue<TBinary>));
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
                Value.ThrowIncompatible<T>(nameof(OValue<TBinary>));
                return default;
            }
        }
    }
}
