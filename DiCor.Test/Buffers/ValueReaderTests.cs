using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiCor.Values;
using DotNext.Buffers;
using Xunit;

namespace DiCor.Test.Buffers
{
    public static class ValueReaderTests
    {
        [Fact]
        public static async Task DATest()
        {
            Pipe pipe = new();
            await pipe.Writer.WriteAsync("20220508-  "u8.ToArray());
            await pipe.Writer.FlushAsync();
            ReadResult result = await pipe.Reader.ReadAsync();
            Read(result);

            static void Read(ReadResult result)
            {
                SequenceReader<byte> reader = new(result.Buffer);
                reader.TryReadValue(11, out DAValue<IsQueryContext> value);
            }
        }

        [Fact]
        public static async Task AETest()
        {
            Pipe pipe = new();
            await pipe.Writer.WriteAsync("\"\"    xyz"u8.ToArray());
            await pipe.Writer.FlushAsync();
            ReadResult result = await pipe.Reader.ReadAsync();
            Read(result);

            static void Read(ReadResult result)
            {
                SequenceReader<byte> reader = new(result.Buffer);
                reader.TryReadValue(9, out AEValue<IsQueryContext> value);
            }
        }
    }
}
