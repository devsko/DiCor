using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct UIValue : IValue<UIValue>
    {
        private readonly Uid _uid;

        public UIValue(Uid uid)
            => _uid = uid;

        public Uid Uid
            => _uid;

        public static VR VR
            => VR.UI;

        public static int MaximumLength
            => 64;

        public static bool IsFixedLength
            => false;

        public static byte Padding
            => 0;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(Uid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UIValue Create<T>(T content)
        {
            if (typeof(T) == typeof(Uid))
            {
                return new UIValue(Unsafe.As<T, Uid>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(UIValue));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(Uid))
            {
                return Unsafe.As<Uid, T>(ref Unsafe.AsRef(in _uid));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(UIValue));
                return default;
            }
        }
    }
}
