// 파일: samples/src/Chapter12/Program.cs
// 12장 — ICriticalNotifyCompletion & ValueTask optimization

using AsyncAwaitLab.Chapter12;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 12 — ICriticalNotifyCompletion & ValueTask optimization");

var signal = SignalPool<int>.Rent();
_ = Task.Run(async () => { await Task.Delay(50); signal.SetResult(42); });
int v = await signal.WaitAsync();
ConsoleHelpers.Log($"first await → {v}");
SignalPool<int>.Return(signal);

// 같은 인스턴스를 다시 빌려서 사용 — 새 할당 없음
signal = SignalPool<int>.Rent();
_ = Task.Run(async () => { await Task.Delay(20); signal.SetResult(43); });
v = await signal.WaitAsync();
ConsoleHelpers.Log($"second await (reused) → {v}");
SignalPool<int>.Return(signal);

// 할당 비교
var before = AsyncDiagnostics.Snapshot();
for (int i = 0; i < 10_000; i++)
{
    var s = SignalPool<int>.Rent();
    s.SetResult(i);
    _ = await s.WaitAsync();
    SignalPool<int>.Return(s);
}
var after = AsyncDiagnostics.Snapshot();
ConsoleHelpers.Log($"10K reuse → {AsyncDiagnostics.Diff(before, after)}");

ConsoleHelpers.Log("Chapter 12 done.");
