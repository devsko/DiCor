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

        public static bool TryReadEnumFromByte<TEnum>(ref this SequenceReader<byte> reader, out TEnum value)
            where TEnum : struct, Enum
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

        public static bool TryRead(ref this SequenceReader<byte> reader, out AsciiString ascii)
        {
            if (!TryReadLength(ref reader, out ushort length))
            {
                ascii = default;
                return false;
            }

            return TryRead(ref reader, length, out ascii);
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, int length, out AsciiString ascii)
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < length)
                return TryReadMultiSegment(ref reader, length, out ascii);

            ascii = new AsciiString(span.Slice(0, length).TrimEnd((byte)' '), false);
            reader.Advance(length);

            return true;

            static bool TryReadMultiSegment(ref SequenceReader<byte> reader, int length, out AsciiString ascii)
            {
                Span<byte> buffer = stackalloc byte[length];
                if (!reader.TryCopyTo(buffer))
                {
                    ascii = default;
                    return false;
                }
                ascii = new AsciiString(buffer.TrimEnd((byte)' '), false);
                reader.Advance(length);
                return true;
            }
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, out Uid uid)
        {
            if (TryRead(ref reader, out AsciiString value))
            {
                uid = new Uid(value, false);
                return true;
            }
            uid = default;
            return false;
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, int length, out Uid uid)
        {
            if (TryRead(ref reader, length, out AsciiString value))
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
