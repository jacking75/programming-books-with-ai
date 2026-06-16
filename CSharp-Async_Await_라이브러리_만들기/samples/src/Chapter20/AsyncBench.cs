// 파일: samples/src/Chapter20/AsyncBench.cs
// 본문 §20.1 — BenchmarkDotNet 기본 벤치 골격.
// `dotnet run -c Release --project src/Chapter20 -- --filter "*AsyncBench*"`로 실행한다.
//
// 이 파일은 BenchmarkDotNet 패키지가 추가되어야 정상 컴파일된다.
// Chapter20.csproj에 <PackageReference Include="BenchmarkDotNet" /> 가 필요.

#if BENCHMARKDOTNET
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace AsyncAwaitLab.Chapter20;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 3, iterationCount: 5)]
public class AsyncBench
{
    private readonly TaskCompletionSource<int> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [GlobalSetup]
    public void Setup() => _tcs.TrySetResult(1);

    [Benchmark(Baseline = true)]
    public async Task<int> AwaitTask() => await _tcs.Task;

    [Benchmark]
    public async ValueTask<int> AwaitValueTask() => await new ValueTask<int>(_tcs.Task);

    [Benchmark]
    public async ValueTask<int> AwaitInstant() => await new ValueTask<int>(42);
}
#endif
