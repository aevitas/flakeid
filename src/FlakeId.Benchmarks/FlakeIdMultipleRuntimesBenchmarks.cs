using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Jobs;

namespace FlakeId.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [DisassemblyDiagnoser]
    [InliningDiagnoser(true, null)]
    public class FlakeIdMultipleRuntimesBenchmarks
    {
        [Benchmark]
        public void Single_FlakeId()
        {
            Id.Create();
        }
    }
}
