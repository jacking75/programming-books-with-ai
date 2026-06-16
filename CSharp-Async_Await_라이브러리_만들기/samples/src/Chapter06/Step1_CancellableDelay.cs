// 파일: samples/src/Chapter06/Step1_CancellableDelay.cs
using System.Runtime.CompilerServices;

namespace AsyncAwaitLab.Chapter06;

public sealed class CancellableDelay
{
    private readonly TimeSpan _duration;
    private readonly CancellationToken _ct;
    public CancellableDelay(TimeSpan d, CancellationToken ct) { _duration = d; _ct = ct; }
    public Awaiter GetAwaiter() => new(_duration, _ct);

    public sealed class Awaiter : ICriticalNotifyCompletion
    {
        private readonly TimeSpan _duration;
        private readonly CancellationToken _ct;
        private int _state;
        private Action? _continuation;
        private Timer? _timer;
        private CancellationTokenRegistration _ctr;

        public Awaiter(TimeSpan d, CancellationToken ct)
        {
            _duration = d;
            _ct = ct;
            if (ct.IsCancellationRequested) _state = 2;
        }

        public bool IsCompleted => Volatile.Read(ref _state) != 0;

        public void GetResult()
        {
            if (Volatile.Read(ref _state) == 2)
                throw new OperationCanceledException(_ct);
        }

        public void OnCompleted(Action c) => Schedule(c);
        public void UnsafeOnCompleted(Action c) => Schedule(c);

        private void Schedule(Action c)
        {
            if (Interlocked.CompareExchange(ref _continuation, c, null) is not null)
                throw new InvalidOperationException();

            _timer = new Timer(static s => Complete((Awaiter)s!, 1), this,
                               _duration, Timeout.InfiniteTimeSpan);

            _ctr = _ct.UnsafeRegister(static (s, _) => Complete((Awaiter)s!, 2), this);
        }

        private static void Complete(Awaiter self, int completedState)
        {
            if (Interlocked.CompareExchange(ref self._state, completedState, 0) == 0)
            {
                self._timer?.Dispose();
                self._ctr.Dispose();
                self._continuation?.Invoke();
            }
        }
    }
}

public static class DelayApi
{
    public static CancellableDelay DelayAsync(TimeSpan d, CancellationToken ct = default)
        => new(d, ct);
}
