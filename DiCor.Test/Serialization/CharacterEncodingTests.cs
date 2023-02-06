using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiCor.IO;
using Xunit;

namespace DiCor.Test.Serialization
{
    public class CharacterEncodingTests
    {
        [Fact]
        public async Task SequenceWithCharacterSet()
        {
            using FileStream stream = new FileStream(
                @"C:\Users\stefa\OneDrive\Dokumente\DICOM\chrSQEncoding.dcm", FileMode.Open, FileAccess.Read, FileShare.Read);
            DataSet set = await new FileReader(new DataSetSerializerFactory()).ReadAsync(stream, CancellationToken.None).ConfigureAwait(false);

            Assert.True(set.TryGet(new Tag(0x0032, 0x1064), out DataSet? sequence));
            Assert.True(sequence!.TryGet(Tag.PatientName, out string? name));
            Assert.Equal("ﾔﾏﾀﾞ^ﾀﾛｳ=山田^太郎=やまだ^たろう", name);
        }
    }
}
