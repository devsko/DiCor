using System;
using System.Text;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void WriteAscii(ReadOnlySpan<char> value)
        {
            Ensure(value.Length);
            int bytesWritten = Encoding.ASCII.GetBytes(value, Span);
            Advance(bytesWritten);
        }

        public void WriteAsciiWithLength(ReadOnlySpan<char> value)
        {
            ushort length = (ushort)value.Length;
            Ensure(length + 2);
            Write(length);
            int bytesWritten = Encoding.ASCII.GetBytes(value, Span);
            Advance(bytesWritten + 2);
        }

        public void WriteAscii(ReadOnlySpan<char> value, int fixedLength)
        {
            Ensure(fixedLength);
            int padding = fixedLength - value.Length;
            if (padding <= 0)
            {
                Encoding.ASCII.GetBytes(value.Slice(0, fixedLength), Span);
            }
            else
            {
                int bytesWritten = Encoding.ASCII.GetBytes(value, Span);
                Span.Slice(bytesWritten, padding).Fill((byte)' ');
            }
            Advance(fixedLength);
        }

    }
}
