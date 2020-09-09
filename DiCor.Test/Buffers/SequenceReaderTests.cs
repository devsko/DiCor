using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
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
        [MemberData(nameof(StringData))]
        public async Task ReadAscii(string? value)
        {
            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: ushort.MaxValue * 2));
            Write();
            await pipe.Writer.FlushAsync();
            ReadResult result = await pipe.Reader.ReadAsync();
            Read();

            void Write()
            {
                var writer = new BufferWriter(pipe.Writer);
                writer.WriteAscii(value);
                writer.Commit();
            }

            void Read()
            {
                var reader = new SequenceReader<byte>(result.Buffer);
                reader.TryRead(out string? readValue);

                if (string.IsNullOrEmpty(value))
                    Assert.True(string.IsNullOrEmpty(readValue));
                else
                    Assert.Equal(value, readValue);
            }
        }

        public static IEnumerable<object[]> StringData
            => new[]
            {
                new object[] { (string)null! },
                new object[] { "" },
                new object[] { "ABCDEFGH" },
                new object[] { new string('x', ushort.MaxValue - 8) + "ABCDEFGH" },
            };
    }
}
