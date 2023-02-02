using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct FDValue : IValue<FDValue>
    {
        private readonly double _double;

        public FDValue(double @double)
            => _double = @double;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(double);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FDValue Create<T>(T content)
        {
            if (typeof(T) == typeof(double))
            {
                return new FDValue(Unsafe.As<T, double>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(FDValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(double))
            {
                return Unsafe.As<double, T>(ref Unsafe.AsRef(in _double));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _double;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(FDValue));
                return default;
            }
        }

        public override string ToString()
            => _double.ToString();
    }
}
