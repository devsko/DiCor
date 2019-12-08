using DiCor.Buffers;

using Xunit;

namespace DiCor.Test.Buffers
{
    public class StringWriterTests
    {
        [Fact]
        public void WriteAsciiTrims()
        {
            Assert.Produces(new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' },
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAsciiFixed("Hello world!", 5);
                    buffer.Commit();
                });
        }

        [Fact]
        public void WriteAsciiPads()
        {
            Assert.Produces(new byte[] { (byte)'H', (byte)'e', (byte)' ', (byte)' ', (byte)' ' },
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAsciiFixed("He", 5);
                    buffer.Commit();
                });
        }

    }
}
