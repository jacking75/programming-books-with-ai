// 파일: samples/src/Chapter03/Section_02_CustomYield.cs
// 책 §3 연습문제 #3 — 자기만의 Yield() awaitable.

using System.Runtime.CompilerServices;
using AsyncAwaitLab.Common;

namespace AsyncAwaitLab.Chapter03;

internal static class CustomYieldDemo
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§3.6 - Custom Yield()");
        ConsoleHelpers.Log($"전 — T#{Environment.CurrentManagedThreadId} TP={Thread.CurrentThread.IsThreadPoolThread}");
        await MyTask.Yield();
        ConsoleHelpers.Log($"후 — T#{Environment.CurrentManagedThreadId} TP={Thread.CurrentThread.IsThreadPoolThread}");
    }
}

internal static class MyTask
{
    public static YieldAwaitable Yield() => default;
}

internal readonly struct YieldAwaitable
{
    public YieldAwaiter GetAwaiter() => default;
}

internal readonly struct YieldAwaiter : ICriticalNotifyCompletion
{
    // 항상 비동기 경로 — 의도적으로 false
    public bool IsCompleted => false;

    public void GetResult() { }

    public void OnCompleted(Action continuation)
        => ThreadPool.QueueUserWorkItem(static s => ((Action)s!)(), continuation);

    public void UnsafeOnCompleted(Action continuation)
        => ThreadPool.UnsafeQueueUserWorkItem(static s => ((Action)s!)(), continuation);
}
