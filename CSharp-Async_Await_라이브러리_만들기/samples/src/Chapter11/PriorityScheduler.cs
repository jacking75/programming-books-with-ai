// 파일: samples/src/Chapter11/PriorityScheduler.cs
namespace AsyncAwaitLab.Chapter11;

public sealed class PriorityScheduler : TaskScheduler, IDisposable
{
    private readonly PriorityQueue<Task, int> _queue = new();
    private readonly object _lock = new();
    private readonly Thread[] _workers;
    private readonly CancellationTokenSource _cts = new();

    public PriorityScheduler(int threadCount = 4)
    {
        _workers = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            _workers[i] = new Thread(WorkerLoop) { IsBackground = true, Name = $"PrioWorker-{i}" };
            _workers[i].Start();
        }
    }

    protected override void QueueTask(Task task)
    {
        int prio = task.AsyncState is int p ? p : 100;
        lock (_lock)
        {
            _queue.Enqueue(task, prio);
            Monitor.Pulse(_lock);
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
    protected override IEnumerable<Task>? GetScheduledTasks()
    {
        lock (_lock) return _queue.UnorderedItems.Select(x => x.Element).ToArray();
    }

    private void WorkerLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            Task? task;
            lock (_lock)
            {
                while (_queue.Count == 0 && !_cts.IsCancellationRequested)
                    Monitor.Wait(_lock, 1000);
                if (_cts.IsCancellationRequested) return;
                _queue.TryDequeue(out task, out _);
            }
            if (task is not null) TryExecuteTask(task);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        lock (_lock) Monitor.PulseAll(_lock);
        foreach (var w in _workers) w.Join();
        _cts.Dispose();
    }
}

public static class ParallelHelpers
{
    public static async Task ForEachAsync<T>(IEnumerable<T> items, int maxParallel,
        Func<T, CancellationToken, Task> body, CancellationToken ct = default)
    {
        using var sem = new SemaphoreSlim(maxParallel);
        var tasks = items.Select(async item =>
        {
            await sem.WaitAsync(ct);
            try { await body(item, ct); }
            finally { sem.Release(); }
        }).ToList();
        await Task.WhenAll(tasks);
    }
}
