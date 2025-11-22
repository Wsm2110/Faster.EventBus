using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MediatR;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ResultBenchmarks
{
    private Faster.EventBus.EventBus _faster;
    private IMediator _mediatr;

    // Parameter that controls simulated load size
    [Params(1, 100, 1_000)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _faster = FasterSetup.Build();
        _mediatr = MediatRSetup.Build();
    }

    [Benchmark(Baseline = true)]
    public async Task Faster_EventBus_Result()
    {
        for (int i = 0; i < Length; i++)
        {
            await _faster.Send(new TestResultCommand(Length));
        }
    }

    [Benchmark]
    public async Task Mediatr_Result()
    {
        for (int i = 0; i < Length; i++)
        {
            await _mediatr.Send(new TestMediatrCommand(Length));
        }       
    }

}
