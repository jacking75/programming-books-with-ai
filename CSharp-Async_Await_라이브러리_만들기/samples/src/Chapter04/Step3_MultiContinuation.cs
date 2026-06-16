// 파일: samples/src/Chapter04/Step3_MultiContinuation.cs
// 책 §4.6 — 콜백 다중 등록을 지원하는 MultiSignal<T>.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitLab.Chapter04;

[System.Diagnostics.DebuggerDisplay("Done={_done} Result={_result} Waiters={_continuations.Count}")]
public sealed class MultiSignal<T>
{
    private readonly object _lock = new();
    private bool _done;
    private T? _result;
    private Exception? _exception;
    private readonly List<Action> _continuations = new();

    public Awaiter GetAwaiter() => new(this);

    public bool TrySetResult(T value)
    {
        List<Action>? cs = null;
        lock (_lock)
        {
            if (_done) return false;
            _done = true;
            _result = value;
            cs = new(_continuations);
            _continuations.Clear();
        }
        foreach (var c in cs) c();
        return true;
    }

    public bool TrySetException(Exception ex)
    {
        List<Action>? cs = null;
        lock (_lock)
        {
            if (_done) return false;
            _done = true;
            _exception = ex;
            cs = new(_continuations);
            _continuations.Clear();
        }
        foreach (var c in cs) c();
        return true;
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly MultiSignal<T> _o;
        public Awaiter(MultiSignal<T> o) => _o = o;
        public bool IsCompleted { get { lock (_o._lock) return _o._done; } }

        public T GetResult()
        {
            lock (_o._lock)
            {
                if (!_o._done) throw new InvalidOperationException();
                if (_o._exception is not null)
                    ExceptionDispatchInfo.Capture(_o._exception).Throw();
                return _o._result!;
            }
        }

        public void OnCompleted(Action c)
        {
            bool now;
            lock (_o._lock)
            {
                now = _o._done;
                if (!now) _o._continuations.Add(c);
            }
            if (now) c();
        }

        public void UnsafeOnCompleted(Action c) => OnCompleted(c);
    }
}
