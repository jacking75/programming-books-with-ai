// 파일: samples/src/Chapter10/ConfiguredAwaitable.cs
// 책 §10.5 — 우리 ManualSignal<T>에 ConfigureAwait를 입힌다.
// (4장의 ManualSignal<T>을 그대로 가져온다.)

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitLab.Chapter10;

public sealed class ManualSignal<T>
{
    private readonly object _lock = new();
    private bool _done;
    private T? _result;
    private Exception? _exception;
    private Action? _cont;

    public Awaiter GetAwaiter() => new(this);
    public ConfigurableAwaitable ConfigureAwait(bool capture) => new(this, capture);

    public void SetResult(T value)
    {
        Action? c;
        lock (_lock)
        {
            if (_done) throw new InvalidOperationException();
            _done = true; _result = value; c = _cont; _cont = null;
        }
        c?.Invoke();
    }

    public void SetException(Exception ex)
    {
        Action? c;
        lock (_lock)
        {
            if (_done) throw new InvalidOperationException();
            _done = true; _exception = ex; c = _cont; _cont = null;
        }
        c?.Invoke();
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly ManualSignal<T> _o;
        public Awaiter(ManualSignal<T> o) => _o = o;
        public bool IsCompleted { get { lock (_o._lock) return _o._done; } }
        public T GetResult()
        {
            lock (_o._lock)
            {
                if (_o._exception is not null) ExceptionDispatchInfo.Capture(_o._exception).Throw();
                return _o._result!;
            }
        }

        public void OnCompleted(Action c) => Register(c);
        public void UnsafeOnCompleted(Action c) => Register(c);

        private void Register(Action c)
        {
            bool run;
            lock (_o._lock)
            {
                run = _o._done;
                if (!run) _o._cont = c;
            }
            if (run) c();
        }
    }

    public readonly struct ConfigurableAwaitable
    {
        private readonly ManualSignal<T> _signal;
        private readonly bool _capture;
        public ConfigurableAwaitable(ManualSignal<T> s, bool capture) { _signal = s; _capture = capture; }
        public ConfigurableAwaiter GetAwaiter() => new(_signal, _capture);
    }

    public readonly struct ConfigurableAwaiter : ICriticalNotifyCompletion
    {
        private readonly Awaiter _inner;
        private readonly bool _capture;
        public ConfigurableAwaiter(ManualSignal<T> s, bool capture)
        {
            _inner = new Awaiter(s);
            _capture = capture;
        }
        public bool IsCompleted => _inner.IsCompleted;
        public T GetResult() => _inner.GetResult();

        public void OnCompleted(Action c) => Schedule(c);
        public void UnsafeOnCompleted(Action c) => Schedule(c);

        private void Schedule(Action c)
        {
            if (_capture && SynchronizationContext.Current is { } ctx)
                _inner.OnCompleted(() => ctx.Post(static s => ((Action)s!)(), c));
            else
                _inner.OnCompleted(c);
        }
    }
}
