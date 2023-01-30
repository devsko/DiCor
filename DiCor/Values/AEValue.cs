﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct AEValue<TIsQuery> : IQueryableValue<AEValue<TIsQuery>>
        where TIsQuery : struct, IIsInQuery
    {
        private static readonly IndexOfAnyValues<byte> s_invalidChars = IndexOfAnyValues.Create("\0\x1\x2\x3\x4\x5\x6\x7\x8\x9\xA\xB\xC\xD\xE\xF\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F\\"u8);

        private readonly AsciiString _ascii;
        private readonly bool _isEmpty;

        public AEValue(AsciiString ascii)
        {
            if (ascii.Length > 16)
                throw new InvalidOperationException($"'{ascii}' is too long.");
            if (ascii.Bytes.IndexOfAny(s_invalidChars) != -1)
                throw new InvalidOperationException($"'{ascii}' contains invalid characters.");

            ReadOnlySpan<byte> trimmed = ascii.Bytes.Trim((byte)' ');

            if (!TIsQuery.Value && trimmed.Length == 0)
                throw new InvalidOperationException($"'{ascii}' contains only white space characters.");

            if (trimmed.Length != ascii.Length)
            {
                ascii = new AsciiString(trimmed, false);
            }
            _ascii = ascii;
        }

        public AEValue(QueryEmptyValue _)
        {
            if (!TIsQuery.Value)
                throw new InvalidOperationException("AEValue can only be an empty value in context of a query.");

            _isEmpty = true;
        }

        public bool IsEmptyValue
            => _isEmpty;

        public AsciiString Ascii
            => !_isEmpty ? _ascii : throw new InvalidOperationException("The AEValue is empty.");

        public static VR VR
            => VR.AE;

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
                (typeof(T) == typeof(QueryEmptyValue) && TIsQuery.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AEValue<TIsQuery> Create<T>(T content)
        {
            if (typeof(T) == typeof(AsciiString))
            {
                return new AEValue<TIsQuery>(Unsafe.As<T, AsciiString>(ref content));
            }
            else if (typeof(T) == typeof(QueryEmptyValue))
            {
                return new AEValue<TIsQuery>(Value.QueryEmpty);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(AEValue<TIsQuery>));
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
