using DiCor.Buffers;

using Xunit;

namespace DiCor.Test.Buffers
{
    public class StringWriterTests
    {
        [Fact]
        public void WriteAsciiTrims()
        {
            Assert.Produces("Hello"u8.ToArray(),
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
            Assert.Produces("He   "u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAsciiFixed("He", 5);
                    buffer.Commit();
                });
        }

    }
}
