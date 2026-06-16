namespace Ch07_AsyncLock;

public sealed class AsyncLazy<T>
{
    private readonly Func<Task<T>> _factory;
    private readonly object _gate = new();
    private Task<T>? _task;

    public AsyncLazy(Func<Task<T>> factory) => _factory = factory;

    public Task<T> Value
    {
        get
        {
            if (_task is not null) return _task;
            lock (_gate)
            {
                _task ??= _factory();
            }
            return _task!;
        }
    }
}
