// 파일: samples/src/Chapter01/Section_01_TaskAllocationProbe.cs
// 책 §1.1.2 에서 본 할당 측정 예제.
// 결과 타입을 바꿔 가며 Task<T> vs ValueTask<T>의 할당 차이를 직접 본다.

using AsyncAwaitLab.Common;

namespace AsyncAwaitLab.Chapter01;

internal static class TaskAllocationProbe
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§1.1 - Task allocation probe");

        const int N = 1_000_000;

        // (1) Task<int> — 작은 정수는 캐시되므로 거의 0
        var before = AsyncDiagnostics.Snapshot();
        for (int i = 0; i < N; i++) _ = await CalcSyncResultAsync();
        var after = AsyncDiagnostics.Snapshot();
        ConsoleHelpers.Log($"Task<int>     : {AsyncDiagnostics.Diff(before, after)}");

        // (2) ValueTask<int>
        before = AsyncDiagnostics.Snapshot();
        for (int i = 0; i < N; i++) _ = await CalcSyncResultValueAsync();
        after = AsyncDiagnostics.Snapshot();
        ConsoleHelpers.Log($"ValueTask<int>: {AsyncDiagnostics.Diff(before, after)}");

        // (3) Task<Guid> — 캐시되지 않는다
        before = AsyncDiagnostics.Snapshot();
        for (int i = 0; i < N; i++) _ = await CalcGuidAsync();
        after = AsyncDiagnostics.Snapshot();
        ConsoleHelpers.Log($"Task<Guid>    : {AsyncDiagnostics.Diff(before, after)}");

        // (4) ValueTask<Guid>
        before = AsyncDiagnostics.Snapshot();
        for (int i = 0; i < N; i++) _ = await CalcGuidValueAsync();
        after = AsyncDiagnostics.Snapshot();
        ConsoleHelpers.Log($"ValueTask<Guid>: {AsyncDiagnostics.Diff(before, after)}");
    }

    private static async Task<int> CalcSyncResultAsync() => 42;
    private static async ValueTask<int> CalcSyncResultValueAsync() => 42;
    private static async Task<Guid> CalcGuidAsync() => Guid.Empty;
    private static async ValueTask<Guid> CalcGuidValueAsync() => Guid.Empty;
}
