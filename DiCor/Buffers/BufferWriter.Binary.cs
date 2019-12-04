using System.Buffers.Binary;

namespace DiCor.Buffers
{
    public partial struct BufferWriter
    {
        public void Write(byte value)
        {
            Ensure(1);
            Span[0] = value;
            Advance(1);
        }

        public void Write(byte value, int count)
        {
            Ensure(count);
            Span.Slice(0, count).Fill(value);
            Advance(count);
        }

        public void Write(ushort value)
        {
            Ensure(sizeof(ushort));
            BinaryPrimitives.WriteUInt16BigEndian(Span, value);
            Advance(sizeof(ushort));
        }

        public void Write(uint value)
        {
            Ensure(sizeof(uint));
            BinaryPrimitives.WriteUInt32BigEndian(Span, value);
            Advance(sizeof(uint));
        }

        public void Reserved(int count)
        {
            Ensure(count);
            Span.Slice(0, count).Fill(0x00);
            Advance(count);
        }

    }
}
