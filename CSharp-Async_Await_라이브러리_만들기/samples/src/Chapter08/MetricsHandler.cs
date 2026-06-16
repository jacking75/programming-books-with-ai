// 파일: samples/src/Chapter08/MetricsHandler.cs
using System.Net.Http;

namespace AsyncAwaitLab.Chapter08;

public sealed class MetricsHandler : DelegatingHandler
{
    private long _count;
    private long _ms;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
    {
        var sw = Stopwatch.GetTimestamp();
        try
        {
            return await base.SendAsync(r, ct).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Increment(ref _count);
            Interlocked.Add(ref _ms, (long)Stopwatch.GetElapsedTime(sw).TotalMilliseconds);
        }
    }

    public (long Count, double AvgMs) Snapshot()
    {
        var c = Interlocked.Read(ref _count);
        var m = Interlocked.Read(ref _ms);
        return (c, c == 0 ? 0 : (double)m / c);
    }
}
