using System.Runtime.CompilerServices;

namespace Ch04_AwaitablePattern;

public sealed class MyAwaitable
{
    public MyAwaiter GetAwaiter() => new();
}

public sealed class MyAwaiter : INotifyCompletion
{
    private Action? _continuation;
    private volatile bool _completed;

    public MyAwaiter()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            _completed = true;
            var c = Interlocked.Exchange(ref _continuation, null);
            c?.Invoke();
        });
    }

    public bool IsCompleted => _completed;
    public void GetResult() { }
    public void OnCompleted(Action continuation)
    {
        // race-safe 컨티뉴에이션 등록
        Interlocked.Exchange(ref _continuation, continuation);
        if (_completed)
        {
            var c = Interlocked.Exchange(ref _continuation, null);
            c?.Invoke();
        }
    }
}
