using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;

namespace FlakeId.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<IdCreationBenchmarks>();
        }
    }

    [DisassemblyDiagnoser]
    [InliningDiagnoser(true, null)]
    public class IdCreationBenchmarks
    {
        [Benchmark]
        public void Create_Single()
        {
            Id.Create();
        }

        [Benchmark]
        public void Create_Guid()
        {
            Guid.NewGuid();
        }
    }
}
