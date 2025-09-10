using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using IdGen;
using MassTransit;

namespace FlakeId.Benchmarks
{
#if _WINDOWS
    [DisassemblyDiagnoser]
    [InliningDiagnoser(true, null)]
#endif
    public class IdCreationBenchmarks
    {
        private static readonly IdGenerator s_idGenerator = new IdGenerator(10,
            new IdGeneratorOptions(sequenceOverflowStrategy: SequenceOverflowStrategy.SpinWait));

        [Benchmark]
        public void Single_FlakeId()
        {
            Id.Create();
        }

        [Benchmark]
        public void Single_Guid()
        {
            Guid.NewGuid();
        }

        [Benchmark]
        public void Single_NewId()
        {
            NewId.Next();
        }

        [Benchmark]
        public void Single_IdGen()
        {
            s_idGenerator.CreateId();
        }
    }
}
