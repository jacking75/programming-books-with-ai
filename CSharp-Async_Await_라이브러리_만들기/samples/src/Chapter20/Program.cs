// 파일: samples/src/Chapter20/Program.cs
// 20장 — Microbenchmark & debugging
//
// BenchmarkDotNet 패키지를 추가하지 않은 기본 실행은 간이 측정만 한다.
// 실제 벤치를 돌리려면 csproj에 BenchmarkDotNet 패키지를 추가하고
// AsyncBench.cs의 BENCHMARKDOTNET 심볼을 정의한다.

using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 20 — Microbenchmark & debugging (lite)");

const int N = 100_000;

var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
tcs.SetResult(1);

var before = AsyncDiagnostics.Snapshot();
var sw = Stopwatch.StartNew();
for (int i = 0; i < N; i++) _ = await tcs.Task;
sw.Stop();
var after = AsyncDiagnostics.Snapshot();
ConsoleHelpers.Log($"await Task   : {sw.ElapsedMilliseconds} ms, {AsyncDiagnostics.Diff(before, after)}");

before = AsyncDiagnostics.Snapshot();
sw.Restart();
for (int i = 0; i < N; i++) _ = await new ValueTask<int>(42);
sw.Stop();
after = AsyncDiagnostics.Snapshot();
ConsoleHelpers.Log($"await ValueT : {sw.ElapsedMilliseconds} ms, {AsyncDiagnostics.Diff(before, after)}");

ConsoleHelpers.Log("Chapter 20 done. (BenchmarkDotNet 실행은 본문 참고)");
