using System.Collections.Generic;
using System.Linq;
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
                    buffer.WriteAsciiFixed("Hello world!"u8, 5);
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
                    buffer.WriteAsciiFixed("He"u8, 5);
                    buffer.Commit();
                });
        }

        [Fact]
        public void WriteAsciiLong()
        {
            static IEnumerable<byte> Expected()
            {
                yield return 100 / 256;
                yield return 100 % 256;
                for (int i = 0; i < 100; i++)
                    yield return (byte)' ';
                yield return 5_000 / 256;
                yield return 5_000 % 256;
                for (int i = 0; i < 5_000; i++)
                    yield return (byte)' ';
            }
            Assert.Produces(Expected(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.WriteAscii(Enumerable.Repeat((byte)' ', 100).ToArray());
                    buffer.WriteAscii(Enumerable.Repeat((byte)' ', 5_000).ToArray());
                    buffer.Commit();
                });
        }

    }
}
