using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace FlakeId.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [SimpleJob(RuntimeMoniker.Net80)]
    [SimpleJob(RuntimeMoniker.Net90)]
#if _WINDOWS
    [DisassemblyDiagnoser]
    [InliningDiagnoser(true, null)]
#endif
    public class FlakeIdMultipleRuntimesBenchmarks
    {
        [Benchmark]
        public void Single_FlakeId()
        {
            Id.Create();
        }
    }
}
