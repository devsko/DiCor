using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using DiCor;

namespace System.Buffers
{
    internal static partial class SequenceReaderExtensions
    {
        public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out float value)
        {
            scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < 4)
            {
                Span<byte> copy = stackalloc byte[4];
                span = copy;
                if (!reader.TryCopyTo(copy))
                {
                    value = default;
                    return false;
                }
            }

            BinaryPrimitives.TryReadSingleLittleEndian(span, out value);
            reader.Advance(4);

            return true;
        }

        public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out double value)
        {
            scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < 8)
            {
                Span<byte> copy = stackalloc byte[8];
                span = copy;
                if (!reader.TryCopyTo(copy))
                {
                    value = default;
                    return false;
                }
            }

            BinaryPrimitives.TryReadDoubleLittleEndian(span, out value);
            reader.Advance(8);

            return true;
        }

        public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out ushort value)
        {
            if (!reader.TryReadLittleEndian(out short val))
            {
                value = default;
                return false;
            }
            value = unchecked((ushort)val);
            return true;
        }

        public static bool TryReadLittleEndian(ref this SequenceReader<byte> reader, out uint value)
        {
            if (!reader.TryReadLittleEndian(out int val))
            {
                value = default;
                return false;
            }
            value = unchecked((uint)val);
            return true;
        }

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

        public static bool TryReadByte<TEnum>(ref this SequenceReader<byte> reader, out TEnum value)
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

        public static bool TryReadLengthBE(ref this SequenceReader<byte> reader, out ushort length)
        {
            if (!reader.TryReadBigEndian(out length))
                return false;

            if (length > reader.Remaining)
                // TODO InvalidPduException
                throw new InvalidOperationException();

            return true;
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, out Tag tag)
        {
            scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < 4)
            {
                Span<byte> copy = stackalloc byte[4];
                span = copy;
                if (!reader.TryCopyTo(copy))
                {
                    tag = default;
                    return false;
                }
            }

            reader.TryReadLittleEndian(out short group);
            reader.TryReadLittleEndian(out short element);

            tag = new Tag(unchecked((ushort)group), unchecked((ushort)element));
            return true;
        }

        public static bool TryRead(ref this SequenceReader<byte> reader, out VR vr)
        {
            scoped ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < 2)
            {
                Span<byte> copy = stackalloc byte[2];
                span = copy;
                if (!reader.TryCopyTo(copy))
                {
                    vr = default;
                    return false;
                }
            }

            vr = new VR(span.Slice(0, 2));
            reader.Advance(2);

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
