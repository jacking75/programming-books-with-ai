// 파일: samples/src/Chapter09/ConnectionPool.cs
// 책 §9.2 — Semaphore + Channel 기반 단순 connection pool.

using System.Threading.Channels;

namespace AsyncAwaitLab.Chapter09;

public sealed class ConnectionPool<T> : IAsyncDisposable where T : class, IAsyncDisposable
{
    private readonly Func<CancellationToken, Task<T>> _factory;
    private readonly Channel<T> _idle;
    private readonly SemaphoreSlim _capacity;
    private bool _disposed;

    public ConnectionPool(int maxSize, Func<CancellationToken, Task<T>> factory)
    {
        _factory = factory;
        _capacity = new SemaphoreSlim(maxSize, maxSize);
        _idle = Channel.CreateUnbounded<T>();
    }

    public async ValueTask<Lease> RentAsync(CancellationToken ct)
    {
        await _capacity.WaitAsync(ct).ConfigureAwait(false);
        if (_idle.Reader.TryRead(out var conn))
            return new Lease(this, conn);

        try
        {
            conn = await _factory(ct).ConfigureAwait(false);
            return new Lease(this, conn);
        }
        catch
        {
            _capacity.Release();
            throw;
        }
    }

    private void Return(T conn)
    {
        if (_disposed) { _ = conn.DisposeAsync(); _capacity.Release(); return; }
        _idle.Writer.TryWrite(conn);
        _capacity.Release();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _idle.Writer.TryComplete();
        await foreach (var conn in _idle.Reader.ReadAllAsync())
            await conn.DisposeAsync();
    }

    public readonly struct Lease : IAsyncDisposable
    {
        private readonly ConnectionPool<T> _pool;
        public T Connection { get; }
        public Lease(ConnectionPool<T> p, T c) { _pool = p; Connection = c; }
        public ValueTask DisposeAsync() { _pool.Return(Connection); return default; }
    }
}

public sealed class FakeConnection : IAsyncDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public ValueTask DisposeAsync() => default;
    public Task<int> ExecuteAsync(string sql, CancellationToken ct = default)
        => Task.FromResult(1);
}
