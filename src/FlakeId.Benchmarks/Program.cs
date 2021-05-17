using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Running;
using IdGen;
using MassTransit;

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
