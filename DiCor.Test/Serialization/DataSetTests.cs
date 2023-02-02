using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiCor.IO;
using FellowOakDicom;
using Xunit;
using Xunit.Abstractions;

namespace DiCor.Test.Serialization
{
    public class DataSetTests
    {
        private readonly ITestOutputHelper _output;

        public DataSetTests(ITestOutputHelper output)
            => _output = output;

        ReadOnlySpan<char> Spaces => "                                ";

        [Fact]
        public void FoDicom()
        {
            var file = DicomFile.Open(@"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI");

            StringBuilder line = new();
            DumpDataset(file.Dataset, 1);

            void DumpDataset(DicomDataset set, int indent)
            {
                foreach (var item in set)
                {
                    line.Append(Spaces.Slice(0, indent));
                    if (item is DicomElement element)
                    {
                        line.Append(element.ToString());
                        line.Append("   ");
                        if (element.Count > 1)
                            line.Append($"[{element.Count}] ");
                        line.Append(string.Join('\\', element.Get<string[]>()));
                    }
                    else if (item is DicomFragmentSequence fragment)
                    {
                        line.Append($"{fragment}    [{fragment.Count()}] ");
                        foreach (var buffer in fragment)
                        {
                            line.Append(buffer.Size).Append(' ');
                        }
                    }
                    else
                    {
                        line.Append($"{item.GetType().Name} {item}");
                    }
                    Out();
                    if (item is DicomSequence sequence)
                    {
                        int i = 0;
                        foreach (var sequenceItem in sequence)
                        {
                            line.Append(Spaces.Slice(0, indent));
                            line.Append($"=== Item {i++} ===");
                            Out();
                            DumpDataset(sequenceItem, indent + 2);
                        }
                    }
                }

                void Out()
                {
                    _output.WriteLine(line.ToString());
                    line.Clear();
                }
            }
        }

        [Fact]
        public async Task SmokeTest()
        {
            // TODO Test perf of bufferSize (Default, 0, 1), isAsync

            using FileStream stream = new FileStream(
                @"C:\Users\stefa\OneDrive\Dokumente\DICOM\CT1_J2KI",
                FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            DataSet set = await new FileReader(stream).ReadAsync().ConfigureAwait(false);

            StringBuilder line = new();
            DumpDataset(set, 1);

            void DumpDataset(DataSet set, int indent)
            {
                foreach ((Tag Tag, VR VR, object? BoxedValue) item in set.EnumerateBoxed())
                {
                    line.Append(Spaces.Slice(0, indent));
                    line.Append($"{item.Tag} {item.VR} ");
                    if (item.BoxedValue is object[] array)
                    {
                        line.Append($"[{array.Length}] ");
                        line.Append(string.Join('\\', array));
                    }
                    else if (item.BoxedValue is byte[] bytes)
                    {
                        line.Append($"[{bytes.Length}] ");
                        line.Append(string.Join(' ', bytes));
                    }
                    else
                    {
                        line.Append(item.BoxedValue);
                    }
                    Out();
                    if (item.BoxedValue is DataSet nested)
                    {
                        DumpDataset(nested, indent + 2);
                    }
                    else if (item.BoxedValue is DataSet[] sequence)
                    {
                        int i = 0;
                        foreach (DataSet sequenceItem in sequence)
                        {
                            line.Append(Spaces.Slice(0, indent));
                            line.Append($"=== Item {i++} ===");
                            Out();
                            DumpDataset(sequenceItem, indent + 2);
                        }
                    }
                }
                void Out()
                {
                    _output.WriteLine(line.ToString());
                    line.Clear();
                }
            }
        }
    }
}
