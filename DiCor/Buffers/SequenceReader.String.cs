using System.Buffers.Text;
using DiCor;

namespace System.Buffers
{
    public static partial class SequenceReaderExtensions
    {
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

        public static bool TryRead(ref this SequenceReader<byte> reader, out DateOnly date)
        {
            // TODO .NET x Utf8Parser.TryParse(DateOnly)

            scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < 8)
            {
                Span<byte> copy = stackalloc byte[8];
                span = copy;
                if (!reader.TryCopyTo(copy))
                {
                    date = default;
                    return false;
                }
            }

            int bytesConsumed;
            if (!Utf8Parser.TryParse(span.Slice(0, 4), out int year, out bytesConsumed) || bytesConsumed != 4 ||
                !Utf8Parser.TryParse(span.Slice(4, 2), out int month, out bytesConsumed) || bytesConsumed != 2 ||
                !Utf8Parser.TryParse(span.Slice(6, 2), out int day, out bytesConsumed) || bytesConsumed != 2)
            {
                // TODO
                throw new Exception("invalid date format");
            }

            reader.Advance(8);

            date = new DateOnly(year, month, day);
            return true;
        }
    }
}
