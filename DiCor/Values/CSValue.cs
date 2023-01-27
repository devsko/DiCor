using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct CSValue<TIsQuery> : IQueryableValue<CSValue<TIsQuery>>
        where TIsQuery : struct, IIsInQuery
    {
        private static readonly IndexOfAnyValues<byte> s_validChars = IndexOfAnyValues.Create(" _0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8);

        private readonly AsciiString _ascii;
        private readonly bool _isEmpty;

        public CSValue(AsciiString ascii)
        {
            if (ascii.Length > 16)
                throw new InvalidOperationException($"'{ascii}' is too long.");
            if (ascii.Bytes.IndexOfAnyExcept(s_validChars) != -1)
                throw new InvalidOperationException($"'{ascii}' contains invalid characters.");

            ReadOnlySpan<byte> trimmed = ascii.Bytes.Trim((byte)' ');

            if (trimmed.Length == 0)
                throw new InvalidOperationException($"'{ascii}' contains only white space characters.");

            if (trimmed.Length != ascii.Length)
            {
                ascii = new AsciiString(trimmed, false);
            }
            _ascii = ascii;
        }

        public CSValue(EmptyValue _)
        {
            if (!TIsQuery.Value)
                throw new InvalidOperationException("CSValue can only be an empty value in context of a query.");

            _isEmpty = true;
        }

        public bool IsEmptyValue
            => _isEmpty;

        public AsciiString Ascii
            => !_isEmpty ? _ascii : throw new InvalidOperationException("The AEValue is empty.");

        public static VR VR
            => VR.CS;

        public static int MaximumLength
            => 16;

        public static bool IsFixedLength
            => false;

        public static byte Padding
            => (byte)' ';

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => (typeof(T) == typeof(AsciiString) ||
                (typeof(T) == typeof(EmptyValue) && TIsQuery.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CSValue<TIsQuery> Create<T>(T content)
        {
            if (typeof(T) == typeof(AsciiString))
            {
                return new CSValue<TIsQuery>(Unsafe.As<T, AsciiString>(ref content));
            }
            else if (typeof(T) == typeof(EmptyValue))
            {
                return new CSValue<TIsQuery>(Value.Empty);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(CSValue<TIsQuery>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(AsciiString) && !IsEmptyValue)
            {
                return Unsafe.As<AsciiString, T>(ref Unsafe.AsRef(in _ascii));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(AEValue<TIsQuery>));
                return default;
            }
        }
    }
}
