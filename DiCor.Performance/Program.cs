using System.IO;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CoreRun;

namespace DiCor.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance
                .AddJob(Job
                    .Default
                    .WithToolchain(new CoreRunToolchain(
                        new FileInfo("C:\\repos\\dotnet\\runtime-main\\artifacts\\bin\\testhost\\net8.0-windows-Release-x64\\shared\\Microsoft.NETCore.App\\8.0.0\\corerun.exe"),
                        createCopy: true,
                        targetFrameworkMoniker: "net8.0",
                        displayName: "main")))
                .AddJob(Job
                    .Default
                    .WithToolchain(new CoreRunToolchain(
                        new FileInfo("C:\\repos\\dotnet\\runtime\\artifacts\\bin\\testhost\\net8.0-windows-Release-x64\\shared\\Microsoft.NETCore.App\\8.0.0\\corerun.exe"),
                        createCopy: true,
                        targetFrameworkMoniker: "net8.0",
                        displayName: "pr")));

            
            //BenchmarkRunner.Run<FileStreamTest>();
            BenchmarkRunner.Run<FileCompareFoDicom>();
            //BenchmarkRunner.Run<DataSetTest>();
            //BenchmarkRunner.Run<UidFrozenDictionary>();
            //BenchmarkRunner.Run<TagTests>();
            //BenchmarkRunner.Run<Net.UpperLayer.BufferWriterTests>(config);
        }
    }
}
