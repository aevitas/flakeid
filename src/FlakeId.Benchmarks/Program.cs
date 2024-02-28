using BenchmarkDotNet.Running;

namespace FlakeId.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run<IdCreationBenchmarks>();
            BenchmarkRunner.Run<FlakeIdMultipleRuntimesBenchmarks>();
        }
    }
}
