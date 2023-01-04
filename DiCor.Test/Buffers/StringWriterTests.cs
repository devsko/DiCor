using System;
using System.Collections;
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

        [Fact]
        public void WriteAsciiLong()
        {
            IEnumerable<byte> Expected()
            {
                yield return 0;
                yield return 100;
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
                    buffer.WriteAscii(new string(' ', 100).AsSpan());
                    buffer.WriteAscii(new string(' ', 5_000).AsSpan());
                    buffer.Commit();
                });
        }

    }
}
