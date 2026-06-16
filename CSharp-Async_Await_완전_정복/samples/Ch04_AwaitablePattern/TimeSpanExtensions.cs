using System.Runtime.CompilerServices;

namespace Ch04_AwaitablePattern;

public static class TimeSpanExtensions
{
    // TimeSpan을 await 가능하게 만드는 확장 메서드
    public static TaskAwaiter GetAwaiter(this TimeSpan span)
        => Task.Delay(span).GetAwaiter();
}
