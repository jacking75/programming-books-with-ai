// 파일: samples/src/Chapter11/Program.cs
// 11장 — Progress, priority, custom TaskScheduler

using AsyncAwaitLab.Chapter11;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 11 — Progress, priority, custom TaskScheduler");

// (1) Progress<T>
var progress = new Progress<int>(p => ConsoleHelpers.Log($"progress {p}%"));
for (int p = 0; p <= 100; p += 25)
{
    ((IProgress<int>)progress).Report(p);
    await Task.Delay(20);
}

// (2) ForEachAsync (제한 동시성)
await ParallelHelpers.ForEachAsync(Enumerable.Range(1, 10), maxParallel: 3,
    async (n, ct) =>
    {
        ConsoleHelpers.Log($"  work {n,2} start");
        await Task.Delay(30, ct);
        ConsoleHelpers.Log($"  work {n,2} done");
    });

ConsoleHelpers.Log("Chapter 11 done.");
