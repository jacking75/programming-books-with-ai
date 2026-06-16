// 파일: samples/src/Chapter15/Program.cs
// 15장 — Async cache & IAsyncEnumerable

using AsyncAwaitLab.Chapter15;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 15 — Async cache & IAsyncEnumerable");

// (1) Single-flight cache
var cache = new SingleFlightCache<string, int>();
int factoryCalled = 0;
var tasks = Enumerable.Range(0, 100).Select(_ => cache.GetOrCreateAsync("hot-key", async _ =>
{
    Interlocked.Increment(ref factoryCalled);
    await Task.Delay(100);
    return 42;
}));
var values = await Task.WhenAll(tasks);
ConsoleHelpers.Log($"single-flight: factoryCalled={factoryCalled}, all-equal={values.All(v => v == 42)}");

// (2) ReplayStream + IAsyncEnumerable
var stream = new ReplayStream<int>(capacity: 3);
stream.Publish(1); stream.Publish(2); stream.Publish(3);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
_ = Task.Run(async () =>
{
    await Task.Delay(50);
    stream.Publish(4);
    stream.Publish(5);
    stream.Complete();
});

await foreach (var x in stream.SubscribeAsync(cts.Token).WithCancellation(cts.Token))
{
    ConsoleHelpers.Log($"  recv {x}");
}

ConsoleHelpers.Log("Chapter 15 done.");
