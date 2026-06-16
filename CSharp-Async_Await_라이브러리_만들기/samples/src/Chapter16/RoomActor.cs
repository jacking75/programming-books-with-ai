// 파일: samples/src/Chapter16/RoomActor.cs
using System.Threading.Channels;

namespace AsyncAwaitLab.Chapter16;

public abstract record Msg;
public sealed record Join(int PlayerId) : Msg;
public sealed record Leave(int PlayerId) : Msg;
public sealed record Chat(int PlayerId, string Text) : Msg;
public sealed record Tick(DateTime Now) : Msg;
public sealed record Snapshot(TaskCompletionSource<int> Reply) : Msg;

public sealed class RoomActor : IAsyncDisposable
{
    private readonly Channel<Msg> _mailbox;
    private readonly HashSet<int> _players = new();
    private readonly Task _loop;

    public RoomActor(int capacity = 1024)
    {
        _mailbox = Channel.CreateBounded<Msg>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
        _loop = Task.Run(LoopAsync);
    }

    public ValueTask SendAsync(Msg msg, CancellationToken ct = default)
        => _mailbox.Writer.WriteAsync(msg, ct);

    public Task<int> GetPlayerCountAsync()
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _mailbox.Writer.TryWrite(new Snapshot(tcs));
        return tcs.Task;
    }

    private async Task LoopAsync()
    {
        await foreach (var msg in _mailbox.Reader.ReadAllAsync())
        {
            switch (msg)
            {
                case Join j: _players.Add(j.PlayerId); break;
                case Leave l: _players.Remove(l.PlayerId); break;
                case Chat: break;
                case Tick: break;
                case Snapshot s: s.Reply.SetResult(_players.Count); break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _mailbox.Writer.TryComplete();
        await _loop.ConfigureAwait(false);
    }
}
