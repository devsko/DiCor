using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal static partial class SequenceReaderExtensions
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

        public static bool TryRead<TEnum>(ref this SequenceReader<byte> reader, out TEnum value)
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

        public static void Reserved(ref this SequenceReader<byte> reader, int length)
        {
            if (reader.Remaining < length)
                // TODO InvalidPduException
                throw new InvalidOperationException();

            reader.Advance(length);
        }
    }
}
