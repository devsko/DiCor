using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DiCor.IO;
using DotNext.Threading;
using FellowOakDicom;
using Xunit;
using static DiCor.Test.Serialization.DataSetTests;

namespace DiCor.Test.Serialization
{
    public class DataSetTests
    {
        [Fact]
        public void FoDicom()
        {
            var file = DicomFile.Open(@"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI");
        }

        [Fact]
        public async Task SmokeTest()
        {
            // TODO Test perf of bufferSize (Default, 0, 1), isAsync

            using FileStream stream = new FileStream(
                @"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI",
                FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            await FileReader.ReadAsync(stream).ConfigureAwait(false);
        }
    }
}
