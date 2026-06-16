// 파일: samples/src/Chapter13/Program.cs
// 13장 — WhenAll / WhenAny variants

using AsyncAwaitLab.Chapter13;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 13 — WhenAll / WhenAny variants");

// (1) MyWhenAllAsync
var results = await WhenHelpers.MyWhenAllAsync(
    new[] { Task.FromResult(1), Task.FromResult(2), Task.FromResult(3) });
ConsoleHelpers.Log($"WhenAll → [{string.Join(',', results)}]");

// (2) RaceAsync
var fastest = await WhenHelpers.RaceAsync<int>(new Func<CancellationToken, Task<int>>[]
{
    async ct => { await Task.Delay(100, ct); return 1; },
    async ct => { await Task.Delay(30,  ct); return 2; },   // 빨리 나오는 친구
    async ct => { await Task.Delay(200, ct); return 3; },
});
ConsoleHelpers.Log($"Race → {fastest}");

// (3) WhenAllSettledAsync
var settled = await WhenHelpers.WhenAllSettledAsync(new[]
{
    Task.FromResult(10),
    Task.FromException<int>(new InvalidOperationException("boom")),
    Task.FromResult(30),
});
foreach (var s in settled)
    ConsoleHelpers.Log($"  settled: success={s.IsSuccess}, result={s.Result}, err={s.Error?.Message}");

ConsoleHelpers.Log("Chapter 13 done.");
