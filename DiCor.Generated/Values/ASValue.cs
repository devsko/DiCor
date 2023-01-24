using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct ASValue : IValue<ASValue>
    {
        private readonly Age _age;

        public ASValue(Age age)
            => _age = age;

        public static int MaximumLength
            => 4;

        public static bool IsFixedLength
            => true;

        public static bool IsCompatible<T>()
            => typeof(T) == typeof(Age);

        public T Get<T>()
            => typeof(T) == typeof(Age)
                ? Unsafe.As<Age, T>(ref Unsafe.AsRef(in _age))
                : Value.ThrowIncompatible<T>(nameof(ASValue));
    }
}
