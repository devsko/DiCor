using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace DiCor.Net.UpperLayer
{
    [MemoryDiagnoser]
    [InliningDiagnoser(true, true)]
    [TailCallDiagnoser]
    [ThreadingDiagnoser]
    [ExceptionDiagnoser]
    public class BufferWriterTests
    {
        [Benchmark]
        public static void Write()
        {
            Pipe pipe = new();
            Association association = new(AssociationType.Find);
            scoped var writer = new PduWriter(pipe.Writer);
            writer.WriteAAssociateRq(association);
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }
    }
}
