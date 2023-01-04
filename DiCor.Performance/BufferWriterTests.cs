using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using DiCor.Net;
using DiCor.Net.UpperLayer;

namespace DiCor.Performance
{
    [MemoryDiagnoser]
    [InliningDiagnoser(true, true)]
    [TailCallDiagnoser]
    [ThreadingDiagnoser]
    public class BufferWriterTests
    {
        private readonly Pipe _pipe;
        private readonly Association _association;

        public BufferWriterTests()
        {
            _pipe = new Pipe();
            _association = new Association(AssociationType.Find);
        }

        [Benchmark]
        public void Write()
        {
            scoped var writer = new PduWriter(_pipe.Writer);
            writer.WriteAAssociateRq(_association);
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
        }
    }
}
