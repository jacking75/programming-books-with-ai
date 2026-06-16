using System.Collections.Concurrent;

namespace Ch10_Practical;

/// <summary>
/// 같은 키에 대한 동시 요청을 하나로 합치는 캐시.
/// 학습용 단순화 버전이라 실패 결과도 캐시한다 — 운영에서는 Invalidate 결합.
/// </summary>
public sealed class SingleFlightCache<TKey, TValue> where TKey : notnull
{
    private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map = new();

    public SingleFlightCache(Func<TKey, CancellationToken, Task<TValue>> factory)
        => _factory = factory;

    public Task<TValue> GetAsync(TKey key, CancellationToken ct)
    {
        var lazy = _map.GetOrAdd(key,
            k => new Lazy<Task<TValue>>(() => _factory(k, ct)));
        return lazy.Value;
    }

    public void Invalidate(TKey key) => _map.TryRemove(key, out _);
}
