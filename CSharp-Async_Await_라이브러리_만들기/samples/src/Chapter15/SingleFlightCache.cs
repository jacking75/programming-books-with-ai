// 파일: samples/src/Chapter15/SingleFlightCache.cs
using System.Collections.Concurrent;

namespace AsyncAwaitLab.Chapter15;

public sealed class SingleFlightCache<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map = new();

    public Task<TValue> GetOrCreateAsync(TKey key, Func<TKey, Task<TValue>> factory)
    {
        var lazy = _map.GetOrAdd(key,
            k => new Lazy<Task<TValue>>(() => factory(k), LazyThreadSafetyMode.ExecutionAndPublication));

        var task = lazy.Value;
        task.ContinueWith(t =>
        {
            if (t.IsFaulted) _map.TryRemove(key, out _);
        }, TaskContinuationOptions.ExecuteSynchronously);

        return task;
    }
}
