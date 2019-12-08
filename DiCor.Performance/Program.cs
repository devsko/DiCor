using System;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace DiCor.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<BufferWriterTests>();
        }
    }
}
