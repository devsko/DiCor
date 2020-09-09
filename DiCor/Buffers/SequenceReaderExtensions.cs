using System.Diagnostics.CodeAnalysis;
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

        public static bool TryRead<T>(ref this SequenceReader<byte> reader, out T value) where T : Enum
        {
            if (!reader.TryRead(out byte b))
            {
                value = default!;
                return false;
            }
            if (!Enum.IsDefined(typeof(T), b))
                // TODO InvalidPduException
                throw new InvalidOperationException();

            value = (T)Enum.ToObject(typeof(T), b);
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

        public static bool TryRead(ref this SequenceReader<byte> reader, [NotNullWhen(true)] out string? value)
        {
            if (!TryReadLength(ref reader, out ushort length))
            {
                value = null;
                return false;
            }

            return TryRead(ref reader, length, out value);
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, int length, [NotNullWhen(true)] out string? value)
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < length)
                return TryReadAsciiMultiSegment(ref reader, length, out value);

            value = Encoding.ASCII.GetString(reader.UnreadSpan.Slice(0, length));
            reader.Advance(length);
            return true;
        }

        private static bool TryReadAsciiMultiSegment(ref SequenceReader<byte> reader, int length, [NotNullWhen(true)] out string? value)
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

        public static void Reserved(ref this SequenceReader<byte> reader, int length)
        {
            if (reader.Remaining < length)
                // TODO InvalidPduException
                throw new InvalidOperationException();

            reader.Advance(length);
        }
    }
}
