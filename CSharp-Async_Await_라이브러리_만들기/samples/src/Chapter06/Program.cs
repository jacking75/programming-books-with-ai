// 파일: samples/src/Chapter06/Program.cs
// 6장 — 취소와 메모리 안전성

using AsyncAwaitLab.Chapter06;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 06 — Cancellation & memory safety");

using (var cts = new CancellationTokenSource(50))
{
    try
    {
        await DelayApi.DelayAsync(TimeSpan.FromSeconds(2), cts.Token);
    }
    catch (OperationCanceledException)
    {
        ConsoleHelpers.Log("CancellableDelay → cancelled as expected");
    }
}

// WithTimeoutAsync 예제
try
{
    var result = await TimeoutHelper.WithTimeoutAsync(
        async ct => { await Task.Delay(500, ct); return 42; },
        TimeSpan.FromMilliseconds(100));
    ConsoleHelpers.Log($"WithTimeoutAsync → {result}");
}
catch (TimeoutException)
{
    ConsoleHelpers.Log("WithTimeoutAsync → TimeoutException 발생 (예상대로)");
}

ConsoleHelpers.Log("Chapter 06 done.");
