using System;
using System.Buffers.Binary;
using System.Linq;

using DiCor.Buffers;

using Xunit;

namespace DiCor.Test.Buffers
{
    public class LengthPrefixTests
    {
        [Fact]
        public void WriteSmallBlock()
        {
            Assert.Produces(new byte[] { (byte)'A', (byte)'B', (byte)'C', 0x00, 0x07, (byte)'7', (byte)' ', (byte)'B', (byte)'y', (byte)'t', (byte)'e', (byte)'s', (byte)'X', (byte)'Y', (byte)'Z' },
                writer =>
                {
                    scoped var buffer = new BufferWriter(writer);
                    buffer.Write(new AsciiString("ABC"u8, false), 3);
                    using (buffer.BeginLengthPrefix())
                    {
                        buffer.Write(new AsciiString("7 Bytes"u8, false), 7);
                    }
                    buffer.Write(new AsciiString("XYZ"u8, false), 3);
                    buffer.Commit();
                });
        }

        [Theory]
        [InlineData(5, 1)]
        [InlineData(5, 10000)]
        [InlineData(20000, 1)]
        [InlineData(1000, 10)]
        [InlineData(4000, 5)]
        [InlineData(10000, 3)]
        public void WriteBlocks(int blockLength, int blockCount)
        {
            int length = blockLength * blockCount;
            Assert.Produces(
                ("ABC"u8.ToArray())
                    .Concat(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)length)))
                    .Concat(Enumerable.Repeat((byte)0x42, length))
                    .Concat("XYZ"u8.ToArray()),
                writer =>
                {
                    scoped var buffer = new BufferWriter(writer);
                    buffer.Write(new AsciiString("ABC"u8, false), 3);
                    using (buffer.BeginLengthPrefix(4))
                    {
                        for (int i = 0; i < blockCount; i++)
                        {
                            buffer.Write(0x42, blockLength);
                        }
                    }
                    buffer.Write(new AsciiString("XYZ"u8, false), 3);
                    buffer.Commit();
                });
        }

        [Fact]
        public void NestedBlocks()
        {
            Assert.Produces(
                ("ABC"u8.ToArray())
                    .Concat(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)15)))
                    .Concat("DEF123"u8.ToArray())
                    .Concat(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)7)))
                    .Concat("GHI_456"u8.ToArray()),
                writer =>
                {
                    scoped var buffer = new BufferWriter(writer);
                    buffer.Write(new AsciiString("ABC"u8, false), 3);
                    using (buffer.BeginLengthPrefix())
                    {
                        buffer.Write(new AsciiString("DEF123"u8, false), 6);
                        buffer.Write(new AsciiString("GHI_456"u8, false));
                    }
                    buffer.Commit();
                });
        }

    }
}
