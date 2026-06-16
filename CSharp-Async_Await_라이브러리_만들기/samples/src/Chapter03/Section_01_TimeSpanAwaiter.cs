// 파일: samples/src/Chapter03/Section_01_TimeSpanAwaiter.cs
// 책 §3.3.2 — TimeSpan을 직접 awaitable로 만드는 확장 메서드 예제.

using System.Runtime.CompilerServices;
using AsyncAwaitLab.Common;

namespace AsyncAwaitLab.Chapter03;

internal static class TimeSpanAwaiter
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§3.3.2 - await TimeSpan extension");
        ConsoleHelpers.Log("await TimeSpan.FromMilliseconds(50) 시작");
        await TimeSpan.FromMilliseconds(50);
        ConsoleHelpers.Log("완료");
    }
}

internal static class TimeSpanAwaitableExtensions
{
    public static TaskAwaiter GetAwaiter(this TimeSpan delay)
        => Task.Delay(delay).GetAwaiter();
}
