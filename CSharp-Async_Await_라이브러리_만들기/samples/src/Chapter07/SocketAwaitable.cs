// 파일: samples/src/Chapter07/SocketAwaitable.cs
// 책 §7.2 — SAEA를 IValueTaskSource<int>로 감싸는 awaitable.

using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace AsyncAwaitLab.Chapter07;

public sealed class SocketAwaitable : IValueTaskSource<int>
{
    private static readonly Action<object?> CONTINUATION_SENTINEL = _ => { };

    private readonly SocketAsyncEventArgs _args;
    private Action<object?>? _continuation;
    private object? _state;

    public SocketAwaitable(SocketAsyncEventArgs args)
    {
        _args = args;
        _args.Completed += (_, e) => OnCompletedInternal(e);
    }

    public SocketAsyncEventArgs Args => _args;

    private void OnCompletedInternal(SocketAsyncEventArgs e)
    {
        var c = Interlocked.Exchange(ref _continuation, CONTINUATION_SENTINEL);
        if (c != null && c != CONTINUATION_SENTINEL) c.Invoke(_state);
    }

    public ValueTask<int> ReceiveAsync(Socket socket)
    {
        _continuation = null;
        _state = null;
        if (!socket.ReceiveAsync(_args))
        {
            return new ValueTask<int>(_args.SocketError == SocketError.Success
                ? _args.BytesTransferred
                : throw new SocketException((int)_args.SocketError));
        }
        return new ValueTask<int>(this, 0);
    }

    public int GetResult(short token)
    {
        if (_args.SocketError != SocketError.Success)
            throw new SocketException((int)_args.SocketError);
        return _args.BytesTransferred;
    }

    public ValueTaskSourceStatus GetStatus(short token)
        => _continuation == CONTINUATION_SENTINEL
            ? ValueTaskSourceStatus.Succeeded
            : ValueTaskSourceStatus.Pending;

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags)
    {
        _state = state;
        if (Interlocked.CompareExchange(ref _continuation, continuation, null) == CONTINUATION_SENTINEL)
            continuation(state);
    }
}
