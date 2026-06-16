using System.Runtime.CompilerServices;

namespace Ch04_AwaitablePattern;

public sealed class DelayedValue<T>
{
    private readonly T _value;
    private readonly TimeSpan _delay;

    public DelayedValue(T value, TimeSpan delay)
    {
        _value = value;
        _delay = delay;
    }

    public Awaiter GetAwaiter() => new(_value, _delay);

    public sealed class Awaiter : INotifyCompletion
    {
        private readonly T _value;
        private Action? _continuation;
        private volatile bool _completed;

        public Awaiter(T value, TimeSpan delay)
        {
            _value = value;
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);
                _completed = true;
                var c = Interlocked.Exchange(ref _continuation, null);
                c?.Invoke();
            });
        }

        public bool IsCompleted => _completed;
        public T GetResult() => _value;
        public void OnCompleted(Action continuation)
        {
            Interlocked.Exchange(ref _continuation, continuation);
            if (_completed)
            {
                var c = Interlocked.Exchange(ref _continuation, null);
                c?.Invoke();
            }
        }
    }
}
