using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using FellowOakDicom;

namespace DiCor
{
    [InliningDiagnoser(false, true)]
    [MemoryDiagnoser]
    public class DataSetTest
    {
        [Benchmark]
        public void Test()
        {
            DataSet set = new(true);
            set.Set(Tag.InstanceCreationDate, new DateOnly(2022, 1, 1));
            set.TryGet(Tag.InstanceCreationDate, out DateOnly date);
        }

        //[Benchmark(Baseline = true)]
        public void FoDicom()
        {
            DicomDataset set = new();
            set.Add(DicomTag.InstanceCreationDate, new DateTime(2022, 1, 1));
            set.TryGetSingleValue(DicomTag.InstanceCreationDate, out DateTime date);
        }
    }
}
