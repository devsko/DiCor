using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DiCor;

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

        public static bool TryReadEnumFromByte<TEnum>(ref this SequenceReader<byte> reader, out TEnum value) where TEnum : struct, Enum
        {
            if (!reader.TryRead(out byte b))
            {
                value = default!;
                return false;
            }

            value = Unsafe.As<byte, TEnum>(ref b);
            if (!Enum.IsDefined(value))
                // TODO InvalidPduException
                throw new InvalidOperationException();

            return true;
        }

        public static bool TryReadLength(ref this SequenceReader<byte> reader, out ushort length)
        {
            if (!reader.TryReadBigEndian(out length))
                return false;

            if (length > reader.Remaining)
                // TODO InvalidPduException
                throw new InvalidOperationException();

            return true;
        }

        public static bool TryReadAscii(ref this SequenceReader<byte> reader, [NotNullWhen(true)] out byte[]? value)
        {
            if (!TryReadLength(ref reader, out ushort length))
            {
                value = null;
                return false;
            }

            return TryReadAscii(ref reader, length, out value);
        }

        public static bool TryReadAscii(ref this SequenceReader<byte> reader, int length, [NotNullWhen(true)] out byte[]? value)
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < length)
                return TryReadAsciiMultiSegment(ref reader, length, out value);

            value = span.Slice(0, length).TrimEnd((byte)' ').ToArray();
            reader.Advance(length);
            return true;

            static bool TryReadAsciiMultiSegment(ref SequenceReader<byte> reader, int length, [NotNullWhen(true)] out byte[]? value)
            {
                Span<byte> buffer = stackalloc byte[length];
                if (!reader.TryCopyTo(buffer))
                {
                    value = null;
                    return false;
                }
                value = buffer.TrimEnd((byte)' ').ToArray();
                reader.Advance(length);
                return true;
            }
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, out Uid uid)
        {
            if (TryReadAscii(ref reader, out byte[]? value))
            {
                uid = new Uid(value, false);
                return true;
            }
            uid = default;
            return false;
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, int length, out Uid uid)
        {
            if (TryReadAscii(ref reader, length, out byte[]? value))
            {
                uid = new Uid(value, false);
                return true;
            }
            uid = default;
            return false;
        }

        public static void Reserved(ref this SequenceReader<byte> reader, int length)
        {
            if (reader.Remaining < length)
                // TODO InvalidPduException
                throw new InvalidOperationException();

            reader.Advance(length);
        }
    }
}
