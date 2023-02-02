using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct FLValue : IValue<FLValue>
    {
        private readonly float _float;

        public FLValue(float @float)
            => _float = @float;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(float);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FLValue Create<T>(T content)
        {
            if (typeof(T) == typeof(float))
            {
                return new FLValue(Unsafe.As<T, float>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(FLValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(float))
            {
                return Unsafe.As<float, T>(ref Unsafe.AsRef(in _float));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _float;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(FLValue));
                return default;
            }
        }

        public override string ToString()
            => _float.ToString();
    }
}
