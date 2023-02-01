using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct CSValue<TIsInQuery> : IQueryableValue<CSValue<TIsInQuery>>
        where TIsInQuery : struct, IIsInQuery
    {
        private static readonly IndexOfAnyValues<byte> s_validChars =
            TIsInQuery.Value
            ? IndexOfAnyValues.Create(" _0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8)
            : IndexOfAnyValues.Create("*? _0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8);


        private readonly AsciiString _ascii;
        private readonly bool _isEmpty;

        public CSValue(AsciiString ascii)
        {
            if (ascii.Length > 16)
                throw new InvalidOperationException($"'{ascii}' is too long.");
            if (ascii.Bytes.IndexOfAnyExcept(s_validChars) != -1)
                throw new InvalidOperationException($"'{ascii}' contains invalid characters.");

            ReadOnlySpan<byte> trimmed = ascii.Bytes.Trim((byte)' ');

            if (!TIsInQuery.Value && trimmed.Length == 0)
                throw new InvalidOperationException($"'{ascii}' contains only white space characters.");

            if (trimmed.Length != ascii.Length)
            {
                ascii = new AsciiString(trimmed, false);
            }
            _ascii = ascii;
        }

        public CSValue(QueryEmpty _)
        {
            if (!TIsInQuery.Value)
                throw new InvalidOperationException("CSValue can only be an empty value in context of a query.");

            _isEmpty = true;
        }

        public bool IsEmptyValue
            => _isEmpty;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => (typeof(T) == typeof(AsciiString) ||
                (typeof(T) == typeof(QueryEmpty) && TIsInQuery.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CSValue<TIsInQuery> Create<T>(T content)
        {
            if (typeof(T) == typeof(AsciiString))
            {
                return new CSValue<TIsInQuery>(Unsafe.As<T, AsciiString>(ref content));
            }
            else if (typeof(T) == typeof(QueryEmpty))
            {
                return new CSValue<TIsInQuery>(Value.QueryEmpty);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(CSValue<TIsInQuery>));
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
                Value.ThrowIncompatible<T>(nameof(AEValue<TIsInQuery>));
                return default;
            }
        }

        public override string ToString()
            => IsEmptyValue ? Value.QueryEmptyDisplay : _ascii.ToString();
    }
}
