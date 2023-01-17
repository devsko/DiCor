using BenchmarkDotNet.Running;

namespace DiCor.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<UidFrozenDictionary>();
            //BenchmarkRunner.Run<Net.UpperLayer.BufferWriterTests>();
        }
    }
}
