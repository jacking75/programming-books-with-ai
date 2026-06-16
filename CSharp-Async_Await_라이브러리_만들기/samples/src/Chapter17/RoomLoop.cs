// 파일: samples/src/Chapter17/RoomLoop.cs
// 16장 RoomActor를 그대로 가져와서 PeriodicTimer로 정기 tick을 추가한다.

using System.Threading.Channels;

namespace AsyncAwaitLab.Chapter17;

public abstract record Msg;
public sealed record Tick(DateTime Now) : Msg;
public sealed record Join(int PlayerId) : Msg;
public sealed record Leave(int PlayerId) : Msg;
public sealed record GoodBye : Msg;
public sealed record Snapshot(TaskCompletionSource<RoomState> Reply) : Msg;
public readonly record struct RoomState(int PlayerCount, long Ticks);

public sealed class TickingRoom : IAsyncDisposable
{
    private readonly Channel<Msg> _mailbox;
    private readonly HashSet<int> _players = new();
    private long _ticks;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private readonly Task _ticker;

    public TickingRoom(TimeSpan tickInterval, int capacity = 1024)
    {
        _mailbox = Channel.CreateBounded<Msg>(new BoundedChannelOptions(capacity) { SingleReader = true });
        _timer = new PeriodicTimer(tickInterval);
        _loop = Task.Run(LoopAsync);
        _ticker = Task.Run(TickerAsync);
    }

    public ValueTask SendAsync(Msg m) => _mailbox.Writer.WriteAsync(m);

    public Task<RoomState> SnapshotAsync()
    {
        var tcs = new TaskCompletionSource<RoomState>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mailbox.Writer.TryWrite(new Snapshot(tcs));
        return tcs.Task;
    }

    private async Task TickerAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
                await _mailbox.Writer.WriteAsync(new Tick(DateTime.UtcNow));
        }
        catch (OperationCanceledException) { }
    }

    private async Task LoopAsync()
    {
        await foreach (var msg in _mailbox.Reader.ReadAllAsync())
        {
            switch (msg)
            {
                case Join j: _players.Add(j.PlayerId); break;
                case Leave l: _players.Remove(l.PlayerId); break;
                case Tick: _ticks++; break;
                case Snapshot s: s.Reply.SetResult(new RoomState(_players.Count, _ticks)); break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _timer.Dispose();
        _mailbox.Writer.TryComplete();
        await Task.WhenAll(_loop, _ticker).ConfigureAwait(false);
        _cts.Dispose();
    }
}
