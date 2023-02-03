using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DiCor.IO;

namespace DiCor.Performance
{
    public class FileStreamTest
    {
        [Params(false, true)]
        public bool Async { get; set; }

        [Params(false, true)]
        public bool Sequential { get; set; }

        [Params(0, 1, 4096, 0x10_0000)]
        public int BufferSize { get; set; }

        [Benchmark]
        public async Task Test()
        {
            using FileStream stream = new FileStream(
                @"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI",
                FileMode.Open, FileAccess.Read, FileShare.Read,
                BufferSize, (Sequential ? FileOptions.SequentialScan : 0) | (Async ? FileOptions.Asynchronous : 0));

            DataSet dataSet = await new FileReader().ReadAsync(stream).ConfigureAwait(false);

        }
    }
}
