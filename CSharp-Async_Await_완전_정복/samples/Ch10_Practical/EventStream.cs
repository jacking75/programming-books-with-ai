using System.Threading.Channels;

namespace Ch10_Practical;

public sealed class EventStream<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
        new UnboundedChannelOptions { SingleReader = false });

    public void Publish(T evt) => _channel.Writer.TryWrite(evt);

    public IAsyncEnumerable<T> Subscribe(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);

    public void Complete() => _channel.Writer.Complete();
}
