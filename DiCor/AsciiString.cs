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
        private readonly byte[]? _bytes;

        public AsciiString(ReadOnlySpan<byte> bytes, bool validate = true)
        {
            if (!validate || Ascii.IsValid(bytes))
                _bytes = bytes.ToArray();
            else
                Throw(bytes);

            [DoesNotReturn]
            [StackTraceHidden]
            static void Throw(ReadOnlySpan<byte> bytes)
                => throw new ArgumentException($"'{Encoding.ASCII.GetString(bytes)}' contains invalid characters.", nameof(bytes));
        }

        public static implicit operator AsciiString(ReadOnlySpan<byte> bytes)
            => new AsciiString(bytes);

        public AsciiString(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                _bytes = new byte[s.Length];
                if (Ascii.FromUtf16(s, _bytes, out _) == OperationStatus.InvalidData)
                    Throw(s);
            }

            [DoesNotReturn]
            [StackTraceHidden]
            static void Throw(string s)
                => throw new ArgumentException($"'{s}' contains invalid characters.", nameof(s));
        }

        public int Length
            => _bytes.AsSpan().Length;

        public ReadOnlySpan<byte> Bytes
            => _bytes;

        public bool Equals(AsciiString other)
            => _bytes.AsSpan().SequenceEqual(other._bytes);

        public override string ToString()
            => string.Create(_bytes.AsSpan().Length, _bytes, (span, bytes) => Ascii.ToUtf16(bytes, span, out _));

        public override bool Equals([NotNullWhen(true)] object? obj)
            => obj is AsciiString other && Equals(other);

        public override unsafe int GetHashCode()
        {
            byte[]? bytes = _bytes;
            if (bytes is null)
                return 0;

            int length = bytes.Length;
            fixed (byte* src = &MemoryMarshal.GetArrayDataReference(bytes))
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
            => Ascii.ToUtf16(_bytes.AsSpan(), destination, out charsWritten) == OperationStatus.Done;

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
            => ToString();
    }
}
