using System.Linq;

using DiCor.Buffers;

using Xunit;

namespace DiCor.Test.Buffers
{
    public class BinaryTests
    {
        [Fact]
        public void WriteByte()
        {
            Assert.Produces("B"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(0x42);
                    buffer.Commit();
                });
        }

        [Fact]
        public void WriteUShort()
        {
            Assert.Produces("BC"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(0x4243);
                    buffer.Commit();
                });
        }

        [Fact]
        public void WriteUInt()
        {
            Assert.Produces("BCDE"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(0x42434445);
                    buffer.Commit();
                });
        }

        [Theory]
        [InlineData(5)]
        [InlineData(20000)]
        public void WriteMultipleBytes(int length)
        {
            Assert.Produces(Enumerable.Repeat((byte)0x42, length),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(0x42, length);
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
            Assert.Produces(Enumerable.Repeat((byte)0x42, blockLength * blockCount),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    for (int i = 0; i < blockCount; i++)
                    {
                        buffer.Write(0x42, blockLength);
                    }

                    buffer.Commit();
                });
        }

    }
}
