// 파일: samples/src/Chapter18/PriorityMailbox.cs
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AsyncAwaitLab.Chapter18;

public sealed class PriorityMailbox<TMsg>
{
    private readonly Channel<TMsg> _hi
        = Channel.CreateUnbounded<TMsg>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Channel<TMsg> _lo
        = Channel.CreateBounded<TMsg>(new BoundedChannelOptions(4096)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    public bool TrySendHigh(TMsg m) => _hi.Writer.TryWrite(m);
    public ValueTask SendLowAsync(TMsg m, CancellationToken ct = default) => _lo.Writer.WriteAsync(m, ct);
    public void Complete() { _hi.Writer.TryComplete(); _lo.Writer.TryComplete(); }

    public async IAsyncEnumerable<TMsg> ConsumeAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            while (_hi.Reader.TryRead(out var hi)) yield return hi;

            Task<bool> hiTask = _hi.Reader.WaitToReadAsync(ct).AsTask();
            Task<bool> loTask = _lo.Reader.WaitToReadAsync(ct).AsTask();
            var winner = await Task.WhenAny(hiTask, loTask).ConfigureAwait(false);
            if (winner == hiTask) continue;

            if (_lo.Reader.TryRead(out var lo)) yield return lo;
            else if (!await loTask) yield break;
        }
    }
}

public sealed class Bulkhead
{
    private readonly SemaphoreSlim _sem;
    public Bulkhead(int maxParallel) => _sem = new SemaphoreSlim(maxParallel);

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> op, CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct).ConfigureAwait(false);
        try { return await op(ct).ConfigureAwait(false); }
        finally { _sem.Release(); }
    }
}
