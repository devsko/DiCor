using System;
using System.Text;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void WriteAscii(ReadOnlySpan<char> value)
        {
            ushort length = (ushort)value.Length;
            Write(length);
            ushort bytesWritten;
            if (Span.Length < length)
            {
                bytesWritten = (ushort)Encoding.ASCII.GetBytes(value.Slice(0, Span.Length), Span);
                length -= bytesWritten;
                Advance(bytesWritten);
                value = value.Slice(bytesWritten);
                Ensure(length);
            }
            bytesWritten = (ushort)Encoding.ASCII.GetBytes(value, Span);
            Advance(bytesWritten);
        }

        public void WriteAsciiFixed(ReadOnlySpan<char> value, int length)
        {
            Ensure(length);
            int padding = length - value.Length;
            if (padding <= 0)
            {
                Encoding.ASCII.GetBytes(value.Slice(0, length), Span);
            }
            else
            {
                int bytesWritten = Encoding.ASCII.GetBytes(value, Span);
                Span.Slice(bytesWritten, padding).Fill((byte)' ');
            }
            Advance(length);
        }
    }
}
