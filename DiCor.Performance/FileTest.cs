using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using FellowOakDicom;
using DiCor.IO;

namespace DiCor.Performance
{
    [MemoryDiagnoser]
    public class FileTest
    {
        [Benchmark]
        public async Task TestAsync()
        {
            using FileStream stream = new FileStream(
                @"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI",
                FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            DataSet dataSet = await new FileReader(stream).ReadAsync().ConfigureAwait(false);
            EnumerateDataSet(dataSet);

            void EnumerateDataSet(DataSet set)
            {
                foreach ((Tag Tag, VR VR, object? BoxedValue) item in set.EnumerateBoxed())
                {
                    if (item.BoxedValue is DataSet nested)
                    {
                        EnumerateDataSet(nested);
                    }
                    else if (item.BoxedValue is DataSet[] sequence)
                    {
                        foreach (DataSet sequenceItem in sequence)
                        {
                            EnumerateDataSet(sequenceItem);
                        }
                    }
                }
            }
        }

        [Benchmark(Baseline = true)]
        public void FoDicom()
        {
            var file = DicomFile.Open(@"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI");


            void EnumerateDataSet(DicomDataset set)
            {
                foreach (var item in set)
                {
                    if (item is DicomElement element)
                    {
                        element.Get<string[]>();
                    }
                    else if (item is DicomFragmentSequence fragment)
                    {
                        foreach (var buffer in fragment)
                        {
                            buffer.Data.ToString();
                        }
                    }

                    if (item is DicomSequence sequence)
                    {
                        foreach (var sequenceItem in sequence)
                        {
                            EnumerateDataSet(sequenceItem);
                        }
                    }
                }
            }
        }
    }
}
