using System;
using DiCor.Buffers;
using DiCor.Values;
using Xunit;

namespace DiCor.Test.Buffers
{
    public class ValueWriterTests
    {
        [Fact]
        public void DATest()
        {
            Assert.Produces("20220508-"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new DAValue<InQuery>(new DateOnly(2022, 5, 8), DateOnly.MaxValue));
                    buffer.Commit();
                });
            Assert.Produces("-20220508"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new DAValue<InQuery>(DateOnly.MinValue, new DateOnly(2022, 5, 8)));
                    buffer.Commit();
                });
            Assert.Produces("20220508-20220509"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new DAValue<InQuery>(new DateOnly(2022, 5, 8), new DateOnly(2022, 5, 9)));
                    buffer.Commit();
                });
            Assert.Produces("\"\""u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new DAValue<InQuery>(default(EmptyValue)));
                    buffer.Commit();
                });
            Assert.Produces("20220508"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new DAValue<NotInQuery>(new DateOnly(2022, 5, 8)));
                    buffer.Commit();
                });
        }

        [Fact]
        public void AETest()
        {
            Assert.Produces("\"\""u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new AEValue<InQuery>(default(EmptyValue)));
                    buffer.Commit();
                });
            Assert.Produces("Hallo"u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new AEValue<NotInQuery>(new AsciiString("  Hallo  "u8)));
                    buffer.Commit();
                });
            Assert.Produces("\"\""u8.ToArray(),
                writer =>
                {
                    var buffer = new BufferWriter(writer);
                    buffer.Write(new AEValue<NotInQuery>(new AsciiString("\"\""u8)));
                    buffer.Commit();
                });
        }
    }
}
