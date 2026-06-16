// 파일: samples/src/Chapter14/CircuitBreaker.cs
namespace AsyncAwaitLab.Chapter14;

public sealed class CircuitBreaker
{
    public enum State { Closed, Open, HalfOpen }

    private readonly object _lock = new();
    private readonly int _failureThreshold;
    private readonly TimeSpan _cooldown;
    private State _state = State.Closed;
    private int _failures;
    private DateTime _openedAt;

    public CircuitBreaker(int failureThreshold = 5, TimeSpan? cooldown = null)
    {
        _failureThreshold = failureThreshold;
        _cooldown = cooldown ?? TimeSpan.FromSeconds(10);
    }

    public State Current { get { lock (_lock) return _state; } }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_state == State.Open)
            {
                if (DateTime.UtcNow - _openedAt > _cooldown) _state = State.HalfOpen;
                else throw new InvalidOperationException("Circuit is OPEN");
            }
        }

        try
        {
            var result = await op(ct).ConfigureAwait(false);
            lock (_lock) { _failures = 0; _state = State.Closed; }
            return result;
        }
        catch
        {
            lock (_lock)
            {
                _failures++;
                if (_failures >= _failureThreshold || _state == State.HalfOpen)
                {
                    _state = State.Open;
                    _openedAt = DateTime.UtcNow;
                }
            }
            throw;
        }
    }
}
