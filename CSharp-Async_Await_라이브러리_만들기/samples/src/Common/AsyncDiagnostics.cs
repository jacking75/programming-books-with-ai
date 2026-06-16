// 파일: samples/src/Common/AsyncDiagnostics.cs
// 비동기 동작을 들여다보기 위한 작은 도구들.

namespace AsyncAwaitLab.Common;

public static class AsyncDiagnostics
{
    /// <summary>
    /// 현재 SynchronizationContext와 TaskScheduler를 한 줄로 요약한다.
    /// </summary>
    public static string CurrentContext()
    {
        var sync = SynchronizationContext.Current?.GetType().FullName ?? "(none)";
        var sched = TaskScheduler.Current.GetType().FullName ?? "(unknown)";
        return $"SyncCtx={sync} | Scheduler={sched} | Thread=#{Environment.CurrentManagedThreadId} | TP={Thread.CurrentThread.IsThreadPoolThread}";
    }

    /// <summary>
    /// 핵심 카운터를 한 번에 캡처한다 (할당 비교용).
    /// </summary>
    public readonly record struct GcSnapshot(long Gen0, long Gen1, long Gen2, long Bytes);

    public static GcSnapshot Snapshot() => new(
        GC.CollectionCount(0),
        GC.CollectionCount(1),
        GC.CollectionCount(2),
        GC.GetTotalAllocatedBytes(precise: true));

    public static string Diff(GcSnapshot before, GcSnapshot after)
        => $"ΔGen0={after.Gen0 - before.Gen0}, ΔGen1={after.Gen1 - before.Gen1}, " +
           $"ΔGen2={after.Gen2 - before.Gen2}, ΔBytes={(after.Bytes - before.Bytes):N0} B";
}
