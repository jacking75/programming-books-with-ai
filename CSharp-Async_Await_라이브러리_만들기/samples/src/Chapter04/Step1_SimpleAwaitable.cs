// 파일: samples/src/Chapter04/Step1_SimpleAwaitable.cs
// 책 §4.1 — 가장 작은 awaitable.

using System.Runtime.CompilerServices;

namespace AsyncAwaitLab.Chapter04;

public readonly struct InstantValue<T>
{
    private readonly T _value;
    public InstantValue(T value) => _value = value;
    public Awaiter GetAwaiter() => new(_value);

    public readonly struct Awaiter : INotifyCompletion
    {
        private readonly T _value;
        public Awaiter(T value) => _value = value;

        public bool IsCompleted => true;
        public T GetResult() => _value;
        public void OnCompleted(Action continuation) => continuation();
    }
}
