using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<ResultBenchmarks>(new DebugInProcessConfig());