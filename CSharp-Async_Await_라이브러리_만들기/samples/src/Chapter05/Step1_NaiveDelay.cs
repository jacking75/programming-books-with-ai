// 파일: samples/src/Chapter05/Step1_NaiveDelay.cs
using System.Runtime.CompilerServices;

namespace AsyncAwaitLab.Chapter05;

public sealed class NaiveDelay
{
    private readonly TimeSpan _duration;
    public NaiveDelay(TimeSpan duration) => _duration = duration;
    public Awaiter GetAwaiter() => new(_duration);

    public sealed class Awaiter : ICriticalNotifyCompletion
    {
        private readonly TimeSpan _duration;
        private bool _completed;

        public Awaiter(TimeSpan d) => _duration = d;
        public bool IsCompleted => _completed;
        public void GetResult() { }

        public void OnCompleted(Action c) => Schedule(c);
        public void UnsafeOnCompleted(Action c) => Schedule(c);

        private void Schedule(Action c)
        {
            Timer? t = null;
            t = new Timer(_ =>
            {
                _completed = true;
                t!.Dispose();
                c();
            }, null, _duration, Timeout.InfiniteTimeSpan);
        }
    }
}
