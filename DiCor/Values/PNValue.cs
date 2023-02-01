﻿using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct PNValue<TIsInQuery> : IValue<PNValue<TIsInQuery>>
        where TIsInQuery : struct, IIsInQuery
    {
        private static readonly IndexOfAnyValues<char> s_invalidChars = IndexOfAnyValues.Create("\0\x1\x2\x3\x4\x5\x6\x7\x8\x9\xA\xB\xC\xD\xE\xF\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F\\");

        // A query empty value ("") is encode as null.
        private readonly string? _string;

        public PNValue(string @string)
        {
            ArgumentNullException.ThrowIfNull(@string);
            if (@string.Length > 64 * 3)
                throw new InvalidOperationException($"'{@string}' is too long.");
            if (@string.AsSpan().IndexOfAny(s_invalidChars) != -1)
                throw new InvalidOperationException($"'{@string}' contains invalid characters.");

            _string = @string;
        }

        public PNValue(QueryEmpty _)
        {
            if (!TIsInQuery.Value)
                throw new InvalidOperationException("AEValue can only be an empty value in context of a query.");

            _string = null;
        }

        [MemberNotNullWhen(false, nameof(_string))]
        public bool IsEmptyValue
            => _string is null;

        public static int PageSize
            => 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompatible<T>()
            => typeof(T) == typeof(string);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PNValue<TIsInQuery> Create<T>(T content)
        {
            if (typeof(T) == typeof(string))
            {
                return new PNValue<TIsInQuery>(Unsafe.As<T, string>(ref content));
            }
            else if (typeof(T) == typeof(QueryEmpty))
            {
                return new PNValue<TIsInQuery>(Value.QueryEmpty);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(PNValue<TIsInQuery>));
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>()
        {
            if (typeof(T) == typeof(string) && !IsEmptyValue)
            {
                return Unsafe.As<string, T>(ref Unsafe.AsRef(in _string));
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(PNValue<TIsInQuery>));
                return default;
            }
        }

        public override string ToString()
            => IsEmptyValue ? Value.QueryEmptyDisplay : _string.ToString();
    }
}