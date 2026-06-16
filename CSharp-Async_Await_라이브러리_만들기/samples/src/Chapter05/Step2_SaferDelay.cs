// 파일: samples/src/Chapter05/Step2_SaferDelay.cs
using System.Runtime.CompilerServices;

namespace AsyncAwaitLab.Chapter05;

public sealed class Delay
{
    private readonly TimeSpan _duration;
    public Delay(TimeSpan d) => _duration = d;
    public Awaiter GetAwaiter() => new(_duration);

    public sealed class Awaiter : ICriticalNotifyCompletion
    {
        private readonly TimeSpan _duration;
        private int _state;          // 0=Pending, 1=Completed
        private Action? _continuation;
        private Timer? _timer;

        public Awaiter(TimeSpan d) => _duration = d;
        public bool IsCompleted => Volatile.Read(ref _state) == 1;
        public void GetResult() { }

        public void OnCompleted(Action c) => Schedule(c);
        public void UnsafeOnCompleted(Action c) => Schedule(c);

        private void Schedule(Action c)
        {
            if (Interlocked.CompareExchange(ref _continuation, c, null) is not null)
                throw new InvalidOperationException("multiple await on the same awaiter");

            _timer = new Timer(static state =>
            {
                var self = (Awaiter)state!;
                if (Interlocked.Exchange(ref self._state, 1) == 0)
                {
                    self._timer?.Dispose();
                    self._continuation?.Invoke();
                }
            }, this, _duration, Timeout.InfiniteTimeSpan);
        }
    }
}
