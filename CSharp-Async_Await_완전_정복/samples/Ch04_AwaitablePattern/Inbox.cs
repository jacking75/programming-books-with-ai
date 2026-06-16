using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Ch04_AwaitablePattern;

public sealed class Inbox<T> where T : class
{
    private readonly ConcurrentQueue<T> _queue = new();
    private Action? _waiter;
    private readonly object _gate = new();

    public void Send(T msg)
    {
        _queue.Enqueue(msg);
        Action? w;
        lock (_gate) { w = _waiter; _waiter = null; }
        w?.Invoke();
    }

    public ReceiveAwaiter ReceiveAsync() => new(this);

    public sealed class ReceiveAwaiter : INotifyCompletion
    {
        private readonly Inbox<T> _owner;
        internal ReceiveAwaiter(Inbox<T> o) => _owner = o;

        public ReceiveAwaiter GetAwaiter() => this;
        public bool IsCompleted => !_owner._queue.IsEmpty;

        public T GetResult()
        {
            if (_owner._queue.TryDequeue(out var item)) return item;
            throw new InvalidOperationException("queue empty");
        }

        public void OnCompleted(Action continuation)
        {
            lock (_owner._gate) _owner._waiter = continuation;

            // race-safe: 등록 사이에 메시지가 도착했는지 다시 확인
            if (!_owner._queue.IsEmpty)
            {
                Action? w;
                lock (_owner._gate) { w = _owner._waiter; _owner._waiter = null; }
                w?.Invoke();
            }
        }
    }
}
