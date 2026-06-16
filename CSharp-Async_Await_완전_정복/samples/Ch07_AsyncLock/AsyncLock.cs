namespace Ch07_AsyncLock;

public sealed class AsyncLock
{
    private readonly SemaphoreSlim _sem = new(1, 1);

    public async ValueTask<Releaser> LockAsync(CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct);
        return new Releaser(this);
    }

    public readonly struct Releaser : IDisposable
    {
        private readonly AsyncLock _owner;
        internal Releaser(AsyncLock owner) => _owner = owner;
        public void Dispose() => _owner._sem.Release();
    }
}
