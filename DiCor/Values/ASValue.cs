using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    public readonly struct ASValue : IValue<ASValue>
    {
        private readonly Age _age;

        public ASValue(Age age)
            => _age = age;

        public Age Age
            => _age;

        public static VR VR
            => VR.AS;

        public static int MaximumLength
            => 4;

        public static bool IsFixedLength
            => true;

        public static byte Padding
            => 0;

        public static int PageSize
            => 5;

        public static ASValue Create<T>(T content)
        {
            if (typeof(T) == typeof(Age))
                return new ASValue(Unsafe.As<T, Age>(ref content));

            Value.ThrowIncompatible<T>(nameof(ASValue));
            return default;
        }

        bool IValue<ASValue>.IsEmptyValue => false;

        public T Get<T>()
        {
            if (typeof(T) == typeof(Age))
                return Unsafe.As<Age, T>(ref Unsafe.AsRef(in _age));

            Value.ThrowIncompatible<T>(nameof(ASValue));
            return default;
        }
    }
}
