// 파일: samples/src/Chapter04/Step2_ManualSignalAwaitable.cs
// 책 §4.2 — 외부 신호로 완료되는 awaitable. 동기/비동기 양쪽 지원.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace AsyncAwaitLab.Chapter04;

[System.Diagnostics.DebuggerDisplay("Done={_isCompleted} Result={_result}")]
public sealed class ManualSignal<T>
{
    private readonly object _lock = new();
    private bool _isCompleted;
    private T? _result;
    private Exception? _exception;
    private Action? _continuation;

    public Awaiter GetAwaiter() => new(this);

    public void SetResult(T value)
    {
        Action? c;
        lock (_lock)
        {
            if (_isCompleted) throw new InvalidOperationException("이미 완료");
            _isCompleted = true;
            _result = value;
            c = _continuation;
            _continuation = null;
        }
        c?.Invoke();
    }

    public void SetException(Exception ex)
    {
        Action? c;
        lock (_lock)
        {
            if (_isCompleted) throw new InvalidOperationException("이미 완료");
            _isCompleted = true;
            _exception = ex;
            c = _continuation;
            _continuation = null;
        }
        c?.Invoke();
    }

    public readonly struct Awaiter : ICriticalNotifyCompletion
    {
        private readonly ManualSignal<T> _owner;
        public Awaiter(ManualSignal<T> owner) => _owner = owner;

        public bool IsCompleted
        {
            get { lock (_owner._lock) return _owner._isCompleted; }
        }

        public T GetResult()
        {
            lock (_owner._lock)
            {
                if (!_owner._isCompleted)
                    throw new InvalidOperationException("아직 완료되지 않음");
                if (_owner._exception is not null)
                    ExceptionDispatchInfo.Capture(_owner._exception).Throw();
                return _owner._result!;
            }
        }

        public void OnCompleted(Action continuation)
        {
            bool runNow = false;
            lock (_owner._lock)
            {
                if (_owner._isCompleted) runNow = true;
                else _owner._continuation = continuation;
            }
            if (runNow) continuation();
        }

        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);
    }
}
