using System.Diagnostics;
using System.Text;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void WriteAscii(ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length <= ushort.MaxValue);

            ushort length = (ushort)value.Length;
            Write(length);

            if (Span.Length < length)
            {
                ushort bytesWritten = (ushort)Encoding.ASCII.GetBytes(value.Slice(0, Span.Length), Span);
                length -= bytesWritten;
                Advance(bytesWritten);
                value = value.Slice(bytesWritten);
                Ensure(length);
            }
            Advance((ushort)Encoding.ASCII.GetBytes(value, Span));
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
