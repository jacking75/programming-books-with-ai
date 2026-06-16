// 파일: samples/src/Chapter02/Section_01_MoveNextCount.cs
// 책 §2.5.1 — 동기 완료 vs 비동기 완료의 MoveNext 호출 수와 스레드 점프를 측정한다.

using AsyncAwaitLab.Common;

namespace AsyncAwaitLab.Chapter02;

internal static class MoveNextCount
{
    public static async Task RunAsync()
    {
        ConsoleHelpers.Banner("§2.5 - MoveNext call count probe");

        // (1) 동기 완료 — Task.CompletedTask
        await RunCaseAsync("sync-complete (Task.CompletedTask)", static () => Task.CompletedTask);

        // (2) 비동기 완료 — Task.Delay(1)
        await RunCaseAsync("async-complete (Task.Delay(1))", static () => Task.Delay(1));
    }

    private static async Task RunCaseAsync(string label, Func<Task> source)
    {
        var counter = new MoveNextCounter();
        var startThread = Environment.CurrentManagedThreadId;
        await counter.DriveAsync(source);
        var endThread = Environment.CurrentManagedThreadId;

        ConsoleHelpers.Log(
            $"{label}: {counter.Transitions} transitions, " +
            $"start T#{startThread}, end T#{endThread} → {(startThread == endThread ? 0 : 1)} thread hop");
    }

    // 트릭: 직접 카운트하려면 상태 머신이 노출되지 않으니, 콜백을 가로채는 awaitable로 우회한다.
    private sealed class MoveNextCounter
    {
        public int Transitions { get; private set; }

        public async Task DriveAsync(Func<Task> source)
        {
            Transitions++;
            await new CountingTaskAwaitable(source(), this);
            Transitions++;
        }
    }

    private readonly struct CountingTaskAwaitable
    {
        private readonly Task _task;
        private readonly MoveNextCounter _counter;
        public CountingTaskAwaitable(Task t, MoveNextCounter c) { _task = t; _counter = c; }
        public CountingTaskAwaiter GetAwaiter() => new(_task, _counter);
    }

    private readonly struct CountingTaskAwaiter : System.Runtime.CompilerServices.INotifyCompletion
    {
        private readonly Task _task;
        private readonly MoveNextCounter _counter;
        public CountingTaskAwaiter(Task t, MoveNextCounter c) { _task = t; _counter = c; }
        public bool IsCompleted => _task.IsCompleted;
        public void GetResult() => _task.GetAwaiter().GetResult();
        public void OnCompleted(Action continuation)
            => _task.GetAwaiter().OnCompleted(continuation);
    }
}
