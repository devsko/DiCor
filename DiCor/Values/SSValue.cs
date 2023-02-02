using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct SSValue : IValue<SSValue>
    {
        private readonly short _integer;

        public SSValue(short integer)
            => _integer = integer;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(short);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SSValue Create<T>(T content)
        {
            if (typeof(T) == typeof(short))
            {
                return new SSValue(Unsafe.As<T, short>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(SSValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(short))
            {
                return Unsafe.As<short, T>(ref Unsafe.AsRef(in _integer));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _integer;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(SSValue));
                return default;
            }
        }

        public override string ToString()
            => _integer.ToString();
    }
}
