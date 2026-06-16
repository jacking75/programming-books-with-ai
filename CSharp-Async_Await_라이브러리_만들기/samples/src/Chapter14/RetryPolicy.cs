// 파일: samples/src/Chapter14/RetryPolicy.cs
namespace AsyncAwaitLab.Chapter14;

public sealed class RetryPolicy
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(100);
    public double Multiplier { get; init; } = 2.0;
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(10);
    public Predicate<Exception> ShouldRetry { get; init; } =
        e => e is not OperationCanceledException;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct = default)
    {
        var rnd = Random.Shared;
        for (int attempt = 1; ; attempt++)
        {
            try { return await op(ct).ConfigureAwait(false); }
            catch (Exception ex) when (attempt < MaxAttempts && ShouldRetry(ex))
            {
                var raw = BaseDelay.TotalMilliseconds * Math.Pow(Multiplier, attempt - 1);
                var capped = Math.Min(raw, MaxDelay.TotalMilliseconds);
                var jitter = capped * (0.5 + rnd.NextDouble() * 0.5);
                await Task.Delay(TimeSpan.FromMilliseconds(jitter), ct).ConfigureAwait(false);
            }
        }
    }
}
