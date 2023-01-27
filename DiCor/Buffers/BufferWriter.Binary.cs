using System.Buffers.Binary;

namespace DiCor.Buffers
{
    internal partial struct BufferWriter
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

        public void WriteBE(ushort value)
        {
            Ensure(sizeof(ushort));
            BinaryPrimitives.WriteUInt16BigEndian(Span, value);
            Advance(sizeof(ushort));
        }

        public void WriteBE(uint value)
        {
            Ensure(sizeof(uint));
            BinaryPrimitives.WriteUInt32BigEndian(Span, value);
            Advance(sizeof(uint));
        }

        public unsafe void Write(Tag tag)
        {
            Ensure(sizeof(Tag));
            BinaryPrimitives.WriteUInt16LittleEndian(Span, tag.Group);
            BinaryPrimitives.WriteUInt16LittleEndian(Span, tag.Element);
            Advance(sizeof(Tag));
        }

        public void Reserved(int count)
        {
            Ensure(count);
            Span.Slice(0, count).Clear();
            Advance(count);
        }
    }
}
