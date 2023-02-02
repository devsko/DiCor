using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct USValue : IValue<USValue>
    {
        private readonly ushort _integer;

        public USValue(ushort integer)
            => _integer = integer;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(ushort);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static USValue Create<T>(T content)
        {
            if (typeof(T) == typeof(ushort))
            {
                return new USValue(Unsafe.As<T, ushort>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(USValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(ushort))
            {
                return Unsafe.As<ushort, T>(ref Unsafe.AsRef(in _integer));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _integer;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(USValue));
                return default;
            }
        }

        public override string ToString()
            => _integer.ToString();
    }
}
