﻿using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DiCor.Values
{
    internal readonly struct TextValue<TTextMaxLength, TIsInQuery> : IValue<TextValue<TTextMaxLength, TIsInQuery>>
        where TTextMaxLength : struct, ITextMaxLength
        where TIsInQuery : struct, IIsInQuery
    {
        private static readonly IndexOfAnyValues<char> s_invalidChars = IndexOfAnyValues.Create("\0\x1\x2\x3\x4\x5\x6\x7\x8\x9\xA\xB\xC\xD\xE\xF\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1A\x1B\x1C\x1D\x1E\x1F\\");

        // A query empty value ("") is encode as null.
        private readonly string? _string;

        public TextValue(string @string)
        {
            ArgumentNullException.ThrowIfNull(@string);
            if (@string.Length > TTextMaxLength.Value)
                throw new InvalidOperationException($"'{@string}' is too long.");
            if (@string.AsSpan().IndexOfAny(s_invalidChars) != -1)
                throw new InvalidOperationException($"'{@string}' contains invalid characters.");

            _string = @string;
        }

        public TextValue(QueryEmpty _)
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
        public static TextValue<TTextMaxLength, TIsInQuery> Create<T>(T content)
        {
            if (typeof(T) == typeof(string))
            {
                return new TextValue<TTextMaxLength, TIsInQuery>(Unsafe.As<T, string>(ref content));
            }
            else if (typeof(T) == typeof(QueryEmpty))
            {
                return new TextValue<TTextMaxLength, TIsInQuery>(Value.QueryEmpty);
            }
            else
            {
                Value.ThrowIncompatible<T>(nameof(TextValue<TTextMaxLength, TIsInQuery>));
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
                Value.ThrowIncompatible<T>(nameof(TextValue<TTextMaxLength, TIsInQuery>));
                return default;
            }
        }
    }
    internal interface ITextMaxLength
    {
        static abstract int Value { get; }
    }

    internal struct ShortText : ITextMaxLength
    {
        public static int Value => 1024;
    }

    internal struct LongText : ITextMaxLength
    {
        public static int Value => 10240;
    }
}
