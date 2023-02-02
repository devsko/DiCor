using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct SLValue : IValue<SLValue>
    {
        private readonly int _integer;

        public SLValue(int integer)
            => _integer = integer;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(int);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SLValue Create<T>(T content)
        {
            if (typeof(T) == typeof(int))
            {
                return new SLValue(Unsafe.As<T, int>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(SLValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(int))
            {
                return Unsafe.As<int, T>(ref Unsafe.AsRef(in _integer));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _integer;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(SLValue));
                return default;
            }
        }

        public override string ToString()
            => _integer.ToString();
    }
}
