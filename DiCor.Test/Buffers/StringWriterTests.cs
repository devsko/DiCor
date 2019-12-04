using DiCor.Buffers;

using Xunit;

namespace DiCor.Test.Buffers
{
    public class StringWriterTests
    {
        [Fact]
        public void WriteASCIITrims()
        {
            Assert.Produces(new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' },
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAscii("Hello world!", 5);
                    buffer.Commit();
                });
        }

        [Fact]
        public void WriteASCIIPads()
        {
            Assert.Produces(new byte[] { (byte)'H', (byte)'e', (byte)' ', (byte)' ', (byte)' ' },
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAscii("He", 5);
                    buffer.Commit();
                });
        }

    }
}
