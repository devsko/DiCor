using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace DiCor
{
    public readonly struct AsciiString : IEquatable<AsciiString>, ISpanFormattable
    {
        private readonly byte[]? _value;

        public AsciiString(ReadOnlySpan<byte> value, bool validate = true)
        {
            if (!validate || Ascii.IsValid(value))
                _value = value.ToArray();
            else
                Throw(value);

            [DoesNotReturn]
            [StackTraceHidden]
            static void Throw(ReadOnlySpan<byte> value)
                => throw new ArgumentException($"'{Encoding.ASCII.GetString(value)}' contains invalid characters.", nameof(value));
        }

        public AsciiString(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                _value = new byte[s.Length];
                if (Ascii.FromUtf16(s, _value, out _) == OperationStatus.InvalidData)
                    Throw(s);
            }

            [DoesNotReturn]
            [StackTraceHidden]
            static void Throw(string s)
                => throw new ArgumentException($"'{s}' contains invalid characters.", nameof(s));
        }

        public int Length
            => _value.AsSpan().Length;

        public ReadOnlySpan<byte> Value
            => _value;

        public bool Equals(AsciiString other)
            => _value.AsSpan().SequenceEqual(other._value);

        public override string ToString()
            => string.Create(_value.AsSpan().Length, _value, (span, value) => Ascii.ToUtf16(value, span, out _));

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is AsciiString other && Equals(other);

        public override unsafe int GetHashCode()
        {
            byte[]? value = _value;
            if (value is null)
                return 0;

            int length = value.Length;
            fixed (byte* src = &MemoryMarshal.GetArrayDataReference(value))
            {
                uint hash1 = (5381 << 16) + 5381;
                uint hash2 = hash1;

                uint* ptrUInt32 = (uint*)src;
                while (length > 7)
                {
                    hash1 = BitOperations.RotateLeft(hash1, 5) + hash1 ^ ptrUInt32[0];
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ ptrUInt32[1];
                    ptrUInt32 += 2;
                    length -= 8;
                }

                byte* ptrByte = (byte*)ptrUInt32;
                while (length-- > 0)
                {
                    hash2 = BitOperations.RotateLeft(hash2, 5) + hash2 ^ *ptrByte++;
                }

                return (int)(hash1 + (hash2 * 1_566_083_941));
            }
        }

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            => Ascii.ToUtf16(_value.AsSpan(), destination, out charsWritten) == OperationStatus.Done;

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
            => ToString();
    }
}
