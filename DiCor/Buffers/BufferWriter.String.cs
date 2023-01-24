using System;
using System.Diagnostics;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void Write(AsciiString ascii)
        {
            ReadOnlySpan<byte> value = ascii.Bytes;
            ushort length = (ushort)value.Length;
            Debug.Assert(length <= ushort.MaxValue);
            Write(length);

            if (Span.Length < length)
            {
                ushort bytesWritten = (ushort)Span.Length;
                value.Slice(0, bytesWritten).CopyTo(Span);
                length -= bytesWritten;
                Advance(bytesWritten);
                value = value.Slice(bytesWritten);
                Ensure(length);
            }

            value.CopyTo(Span);
            Advance(value.Length);
        }

        public void Write(AsciiString ascii, int length)
        {
            ReadOnlySpan<byte> value = ascii.Bytes;
            Ensure(length);
            int padding = length - value.Length;
            if (padding <= 0)
            {
                value.Slice(0, length).CopyTo(Span);
            }
            else
            {
                value.CopyTo(Span);
                Span.Slice(value.Length, padding).Fill((byte)' ');
            }
            Advance(length);
        }

        public void Write(Uid uid)
            => Write(uid.Value);
    }
}
