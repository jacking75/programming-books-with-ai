// 파일: samples/src/Chapter12/PooledSignal.cs
using System.Threading.Tasks.Sources;

namespace AsyncAwaitLab.Chapter12;

public sealed class PooledSignal<T> : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> _core;

    public short Version => _core.Version;

    public ValueTask<T> WaitAsync() => new(this, _core.Version);

    public void SetResult(T value) => _core.SetResult(value);
    public void SetException(Exception ex) => _core.SetException(ex);
    public void Reset() => _core.Reset();

    public T GetResult(short token) => _core.GetResult(token);
    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}

public static class SignalPool<T>
{
    private static readonly System.Collections.Concurrent.ConcurrentBag<PooledSignal<T>> _bag = new();

    public static PooledSignal<T> Rent()
        => _bag.TryTake(out var s) ? s : new PooledSignal<T>();

    public static void Return(PooledSignal<T> s)
    {
        s.Reset();
        _bag.Add(s);
    }
}
