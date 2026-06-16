// 파일: samples/src/Chapter04/Demo.cs
// 4장 본문의 두 데모를 실행한다.

using AsyncAwaitLab.Common;

namespace AsyncAwaitLab.Chapter04;

internal static class Demo
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§4.1 - InstantValue<T>");
        string greeting = await new InstantValue<string>("Hello!");
        ConsoleHelpers.Log($"InstantValue → {greeting}");

        ConsoleHelpers.Banner("§4.2 - ManualSignal<T>");
        var signal = new ManualSignal<int>();
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            signal.SetResult(42);
        });
        int result = await signal;
        ConsoleHelpers.Log($"ManualSignal → {result}");

        ConsoleHelpers.Banner("§4.6 - MultiSignal<T> startGate");
        var startGate = new MultiSignal<DateTime>();
        var workers = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            int id = i;
            workers.Add(Task.Run(async () =>
            {
                var t = await startGate;
                ConsoleHelpers.Log($"  worker {id} started at {t:O}");
            }));
        }
        await Task.Delay(200);
        startGate.TrySetResult(DateTime.UtcNow);
        await Task.WhenAll(workers);
    }
}
