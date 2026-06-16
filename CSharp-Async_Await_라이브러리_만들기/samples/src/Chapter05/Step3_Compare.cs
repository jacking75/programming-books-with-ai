// 파일: samples/src/Chapter05/Step3_Compare.cs
using AsyncAwaitLab.Common;

namespace AsyncAwaitLab.Chapter05;

internal static class Step3_Compare
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§5.4 - DelayCompare");

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++) await new Delay(TimeSpan.FromMilliseconds(1));
        sw.Stop();
        ConsoleHelpers.Log($"Custom Delay 100회: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        for (int i = 0; i < 100; i++) await Task.Delay(1);
        sw.Stop();
        ConsoleHelpers.Log($"Task.Delay 100회 : {sw.ElapsedMilliseconds} ms");
    }
}
