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
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAsciiFixed("ABC", 3);
                    using (buffer.BeginLengthPrefix())
                    {
                        buffer.WriteAsciiFixed("7 Bytes", 7);
                    }
                    buffer.WriteAsciiFixed("XYZ", 3);
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
                (new byte[] { (byte)'A', (byte)'B', (byte)'C' })
                    .Concat(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((uint)length)))
                    .Concat(Enumerable.Repeat((byte)0x42, length))
                    .Concat(new byte[] { (byte)'X', (byte)'Y', (byte)'Z' }),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAsciiFixed((ReadOnlySpan<char>)"ABC", 3);
                    using (buffer.BeginLengthPrefix(4))
                    {
                        for (int i = 0; i < blockCount; i++)
                        {
                            buffer.Write(0x42, blockLength);
                        }
                    }
                    buffer.WriteAsciiFixed((ReadOnlySpan<char>)"XYZ", 3);
                    buffer.Commit();
                });
        }

        [Fact]
        public void NestedBlocks()
        {
            Assert.Produces(
                (new byte[] { (byte)'A', (byte)'B', (byte)'C' })
                    .Concat(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)15)))
                    .Concat(new byte[] { (byte)'D', (byte)'E', (byte)'F', (byte)'1', (byte)'2', (byte)'3' })
                    .Concat(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)7)))
                    .Concat(new byte[] { (byte)'G', (byte)'H', (byte)'I', (byte)'_', (byte)'4', (byte)'5', (byte)'6' }),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAsciiFixed((ReadOnlySpan<char>)"ABC", 3);
                    using (buffer.BeginLengthPrefix())
                    {
                        buffer.WriteAsciiFixed((ReadOnlySpan<char>)"DEF123", 6);
                        buffer.WriteAscii((ReadOnlySpan<char>)"GHI_456");
                    }
                    buffer.Commit();
                });
        }

    }
}
