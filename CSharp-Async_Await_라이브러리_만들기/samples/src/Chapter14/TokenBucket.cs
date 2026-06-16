// 파일: samples/src/Chapter14/TokenBucket.cs
namespace AsyncAwaitLab.Chapter14;

public sealed class TokenBucketLimiter
{
    private readonly object _lock = new();
    private readonly double _capacity;
    private readonly double _refillPerSec;
    private double _tokens;
    private long _lastRefillTicks;

    public TokenBucketLimiter(double capacity, double refillPerSec)
    {
        _capacity = capacity; _refillPerSec = refillPerSec;
        _tokens = capacity; _lastRefillTicks = Stopwatch.GetTimestamp();
    }

    public async ValueTask WaitAsync(CancellationToken ct = default)
    {
        while (true)
        {
            TimeSpan wait;
            lock (_lock)
            {
                Refill();
                if (_tokens >= 1)
                {
                    _tokens -= 1;
                    return;
                }
                double need = 1 - _tokens;
                wait = TimeSpan.FromSeconds(need / _refillPerSec);
            }
            await Task.Delay(wait, ct).ConfigureAwait(false);
        }
    }

    private void Refill()
    {
        long now = Stopwatch.GetTimestamp();
        double elapsed = (double)(now - _lastRefillTicks) / Stopwatch.Frequency;
        _tokens = Math.Min(_capacity, _tokens + elapsed * _refillPerSec);
        _lastRefillTicks = now;
    }
}
