using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

using DiCor.Buffers;

using Xunit;

namespace DiCor.Test.Buffers
{
    public class SequenceReaderTests
    {
        [Theory]
        [InlineData((uint)1)]
        [InlineData(uint.MaxValue)]
        [InlineData(((uint)int.MaxValue) + 1)]
        public async Task ReadUInt(uint value)
        {
            var pipe = new Pipe();
            Write();
            await pipe.Writer.FlushAsync();
            ReadResult result = await pipe.Reader.ReadAsync();
            Read();

            void Write()
            {
                var writer = new BufferWriter(pipe.Writer);
                writer.Write(value);
                writer.Commit();
            }

            void Read()
            {
                var reader = new SequenceReader<byte>(result.Buffer);
                reader.TryReadBigEndian(out uint readValue);

                Assert.Equal(value, readValue);
            }
        }

        [Theory]
        [InlineData((ushort)1)]
        [InlineData(ushort.MaxValue)]
        [InlineData(((ushort)short.MaxValue) + 1)]
        public async Task ReadUShort(ushort value)
        {
            var pipe = new Pipe();
            Write();
            await pipe.Writer.FlushAsync();
            ReadResult result = await pipe.Reader.ReadAsync();
            Read();

            void Write()
            {
                var writer = new BufferWriter(pipe.Writer);
                writer.Write(value);
                writer.Commit();
            }

            void Read()
            {
                var reader = new SequenceReader<byte>(result.Buffer);
                reader.TryReadBigEndian(out ushort readValue);

                Assert.Equal(value, readValue);
            }
        }

        [Theory]
        [MemberData(nameof(Utf8Data))]
        public async Task ReadAscii(byte[]? value)
        {
            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: ushort.MaxValue * 2));
            Write();
            await pipe.Writer.FlushAsync();
            ReadResult result = await pipe.Reader.ReadAsync();
            Read();

            void Write()
            {
                var writer = new BufferWriter(pipe.Writer);
                writer.Write(new AsciiString(value, false));
                writer.Commit();
            }

            void Read()
            {
                var reader = new SequenceReader<byte>(result.Buffer);
                reader.TryRead(out AsciiString readValue);

                Assert.Equal(new AsciiString(value ?? Array.Empty<byte>()), readValue);
            }
        }

        public static IEnumerable<object[]> Utf8Data
            => new[]
            {
                new object[] { (byte[])null! },
                new object[] { ""u8.ToArray() },
                new object[] { "ABCDEFGH"u8.ToArray() },
                new object[] { Enumerable.Repeat((byte)'x', ushort.MaxValue - 8).Concat("ABCDEFGH"u8.ToArray()).ToArray() },
            };
    }
}
