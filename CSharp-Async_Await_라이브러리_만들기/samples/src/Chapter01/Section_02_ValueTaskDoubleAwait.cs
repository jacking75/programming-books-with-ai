// 파일: samples/src/Chapter01/Section_02_ValueTaskDoubleAwait.cs
// 책 §1.2 의 Pitfall — ValueTask<T>를 두 번 await하면 어떤 일이 벌어지는가?

using AsyncAwaitLab.Common;
using System.Threading.Tasks.Sources;

namespace AsyncAwaitLab.Chapter01;

internal static class ValueTaskDoubleAwait
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§1.2 - ValueTask double-await pitfall");

        // (A) 동기 완료 — 두 번 await해도 운 좋게 동작한다 (그러나 보장되지 않음)
        var ok = await TwiceOnSyncCompletedAsync();
        ConsoleHelpers.Log($"(A) sync-completed twice OK={ok}");

        // (C) 풀링 소스 — 두 번 await 시 예외
        try
        {
            await TwiceOnPooledAsync();
        }
        catch (Exception ex)
        {
            ConsoleHelpers.Log($"(C) pooled twice → {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async Task<bool> TwiceOnSyncCompletedAsync()
    {
        ValueTask<int> v = new ValueTask<int>(42);
        var a = await v;
        var b = await v; // 위험하지만 동기 완료 케이스에서는 동작
        return a == b;
    }

    private static async Task TwiceOnPooledAsync()
    {
        var src = new ManualValueTaskSource<int>();
        src.SetResult(7);
        var vt = new ValueTask<int>(src, src.Version);
        _ = await vt;
        _ = await vt; // 여기서 InvalidOperationException 발생
    }

    // IValueTaskSource<T> 골격 — 자세한 구현은 12장에서.
    private sealed class ManualValueTaskSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> _core;
        public short Version => _core.Version;
        public void SetResult(T value) => _core.SetResult(value);
        public T GetResult(short token) => _core.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);
    }
}
