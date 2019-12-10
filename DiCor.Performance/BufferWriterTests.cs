using System.IO.Pipelines;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

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
            new PduWriter(_pipe.Writer)
                .WriteAAssociateRq(_association);
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
            _pipe.Reset();
        }
    }
}
