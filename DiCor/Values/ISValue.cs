using System.Globalization;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct ISValue : IValue<ISValue>
    {
        private readonly int _int;

        public ISValue(int @int)
            => _int = @int;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(int);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ISValue Create<T>(T content)
        {
            if (typeof(T) == typeof(int))
            {
                return new ISValue(Unsafe.As<T, int>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ISValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(int))
            {
                return Unsafe.As<int, T>(ref Unsafe.AsRef(in _int));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _int;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ISValue));
                return default;
            }
        }

        public override string ToString()
            => _int.ToString(CultureInfo.InvariantCulture);
    }
}
