using System.Runtime.CompilerServices;
using System.Text;

namespace DiCor.Values
{
    internal readonly struct OtherBinaryValue<TBinary> : IValue<OtherBinaryValue<TBinary>>
        where TBinary : unmanaged
    {
        private readonly TBinary[] _array;

        public OtherBinaryValue(TBinary[] array)
            => _array = array;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(TBinary[]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OtherBinaryValue<TBinary> Create<T>(T content)
        {
            if (typeof(T) == typeof(TBinary[]))
            {
                return new OtherBinaryValue<TBinary>(Unsafe.As<T, TBinary[]>(ref content));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(OtherBinaryValue<TBinary>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(TBinary[]))
            {
                return Unsafe.As<TBinary[], T>(ref Unsafe.AsRef(in _array));
            }
            else if (typeof(T) == typeof(object))
            {
                object boxed = _array;
                return Unsafe.As<object, T>(ref boxed);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(OtherBinaryValue<TBinary>));
                return default;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(typeof(TBinary).Name).Append(' ');
            if (_array.Length <= 10)
            {
                sb.Append('{');
                bool first = true;
                foreach (TBinary binary in _array)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;
                    sb.Append(binary);
                }
                sb.Append('}');
            }
            else
            {
                sb.Append($"({_array.Length} items)");
            }

            return sb.ToString();
        }
    }
}
