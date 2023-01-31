using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct ASValue : IValue<ASValue>
    {
        private readonly Age _age;

        public ASValue(Age age)
            => _age = age;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(Age);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ASValue Create<T>(T content)
        {
            if (typeof(T) == typeof(Age))
            {
                return new ASValue(Unsafe.As<T, Age>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ASValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(Age))
            {
                return Unsafe.As<Age, T>(ref Unsafe.AsRef(in _age));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ASValue));
                return default;
            }
        }
    }
}
