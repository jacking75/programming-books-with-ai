// 파일: samples/src/Chapter06/Step2_WithTimeout.cs
// 책 §6.5 — WithTimeoutAsync 패턴.

namespace AsyncAwaitLab.Chapter06;

public static class TimeoutHelper
{
    public static async Task<T> WithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> op,
        TimeSpan timeout,
        CancellationToken external = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(external);
        cts.CancelAfter(timeout);
        try
        {
            return await op(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!external.IsCancellationRequested)
        {
            throw new TimeoutException($"{timeout} 안에 완료되지 않음");
        }
    }
}
