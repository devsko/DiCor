using System.IO.Pipelines;
using System.Runtime.CompilerServices;
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
            var message = ULMessage.FromData<AAssociateRqData>(new() { Association = _association });
            scoped var writer = new PduWriter(_pipe.Writer);
            writer.WriteAAssociateRq(ref Unsafe.As<long, AAssociateRqData>(ref message.Data));
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
            _pipe.Reset();
        }
    }
}
