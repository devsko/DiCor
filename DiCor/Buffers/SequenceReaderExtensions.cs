using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Policy;
using System.Text;

namespace System.Buffers
{
    public static class SequenceReaderExtensions
    {
        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ushort value)
        {
            if (!reader.TryReadBigEndian(out short val))
            {
                value = default;
                return false;
            }
            value = unchecked((ushort)val);
            return true;
        }

        public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out uint value)
        {
            if (!reader.TryReadBigEndian(out int val))
            {
                value = default;
                return false;
            }
            value = unchecked((uint)val);
            return true;
        }

        public static bool TryReadAscii(ref this SequenceReader<byte> reader, [NotNullWhen(true)] out string? value)
        {
            if (!reader.TryReadBigEndian(out ushort length))
            {
                value = null;
                return false;
            }

            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < length)
                return TryReadAsciiMultiSegment(ref reader, length, out value);

            value = Encoding.ASCII.GetString(reader.UnreadSpan.Slice(0, length));
            reader.Advance(length);
            return true;
        }

        private static bool TryReadAsciiMultiSegment(ref SequenceReader<byte> reader, int length, [NotNullWhen(false)]out string? value)
        {
            Span<byte> buffer = length > 1024 ? new byte[length] : stackalloc byte[length];
            if (!reader.TryCopyTo(buffer))
            {
                value = null;
                return false;
            }
            reader.Advance(length);
            value = Encoding.ASCII.GetString(buffer);
            return true;
        }
    }
}
