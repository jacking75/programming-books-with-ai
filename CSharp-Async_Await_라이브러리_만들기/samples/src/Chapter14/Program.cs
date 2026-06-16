// 파일: samples/src/Chapter14/Program.cs
// 14장 — Rate limiter, retry, circuit breaker

using AsyncAwaitLab.Chapter14;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 14 — Rate limiter, retry, circuit breaker");

// (1) TokenBucketLimiter — 초당 2건
var limiter = new TokenBucketLimiter(capacity: 3, refillPerSec: 2);
var sw = Stopwatch.StartNew();
for (int i = 0; i < 8; i++)
{
    await limiter.WaitAsync();
    ConsoleHelpers.Log($"  fire #{i} @ {sw.ElapsedMilliseconds} ms");
}

// (2) RetryPolicy — 3번 시도 후 성공
int tries = 0;
var policy = new RetryPolicy { MaxAttempts = 4, BaseDelay = TimeSpan.FromMilliseconds(30) };
var ok = await policy.ExecuteAsync<int>(async ct =>
{
    tries++;
    if (tries < 3) throw new InvalidOperationException($"fail #{tries}");
    return 42;
});
ConsoleHelpers.Log($"retry → ok={ok} after {tries} tries");

// (3) CircuitBreaker
var breaker = new CircuitBreaker(failureThreshold: 2, cooldown: TimeSpan.FromMilliseconds(100));
for (int i = 0; i < 4; i++)
{
    try
    {
        await breaker.ExecuteAsync<int>(_ => throw new Exception("boom"));
    }
    catch (Exception ex)
    {
        ConsoleHelpers.Log($"  attempt {i}: {ex.GetType().Name} → state={breaker.Current}");
    }
}

ConsoleHelpers.Log("Chapter 14 done.");
