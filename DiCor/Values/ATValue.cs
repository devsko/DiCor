using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct ATValue : IValue<ATValue>
    {
        private readonly Tag _tag;

        public ATValue(Tag tag)
            => _tag = tag;

        public Tag Tag
            => _tag;

        public static VR VR
            => VR.AT;

        public static int MaximumLength
            => 4;

        public static bool IsFixedLength
            => true;

        public static byte Padding
            => 0;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(Tag);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ATValue Create<T>(T content)
        {
            if (typeof(T) == typeof(Tag))
            {
                return new ATValue(Unsafe.As<T, Tag>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ATValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(Tag))
            {
                return Unsafe.As<Tag, T>(ref Unsafe.AsRef(in _tag));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(ATValue));
                return default;
            }
        }
    }
}
