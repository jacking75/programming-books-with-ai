// 파일: samples/src/Chapter15/ReplayStream.cs
using System.Runtime.CompilerServices;

namespace AsyncAwaitLab.Chapter15;

public sealed class ReplayStream<T>
{
    private readonly object _lock = new();
    private readonly Queue<T> _buffer;
    private readonly int _capacity;
    private readonly List<TaskCompletionSource<T>> _waiters = new();
    private bool _completed;

    public ReplayStream(int capacity) { _capacity = capacity; _buffer = new(capacity); }

    public void Publish(T item)
    {
        List<TaskCompletionSource<T>>? snapshot = null;
        lock (_lock)
        {
            if (_buffer.Count >= _capacity) _buffer.Dequeue();
            _buffer.Enqueue(item);
            snapshot = new(_waiters);
            _waiters.Clear();
        }
        foreach (var w in snapshot) w.TrySetResult(item);
    }

    public void Complete()
    {
        List<TaskCompletionSource<T>>? snapshot = null;
        lock (_lock)
        {
            _completed = true;
            snapshot = new(_waiters);
            _waiters.Clear();
        }
        foreach (var w in snapshot) w.TrySetCanceled();
    }

    public async IAsyncEnumerable<T> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        T[] snapshot;
        lock (_lock) snapshot = _buffer.ToArray();
        foreach (var it in snapshot) yield return it;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock)
            {
                if (_completed) yield break;
                _waiters.Add(tcs);
            }
            T next;
            try { next = await tcs.Task.WaitAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { yield break; }
            yield return next;
        }
    }
}
