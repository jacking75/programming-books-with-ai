// 파일: samples/src/Chapter13/WhenAll.cs
namespace AsyncAwaitLab.Chapter13;

public static class WhenHelpers
{
    public static Task<T[]> MyWhenAllAsync<T>(IEnumerable<Task<T>> tasks)
    {
        var list = tasks.ToArray();
        var tcs = new TaskCompletionSource<T[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var results = new T[list.Length];
        int remaining = list.Length;
        var exceptions = new List<Exception>();

        if (list.Length == 0)
        {
            tcs.SetResult(Array.Empty<T>());
            return tcs.Task;
        }

        for (int i = 0; i < list.Length; i++)
        {
            int index = i;
            list[i].ContinueWith(t =>
            {
                if (t.IsFaulted)
                    lock (exceptions) exceptions.AddRange(t.Exception!.InnerExceptions);
                else if (t.IsCanceled)
                    lock (exceptions) exceptions.Add(new OperationCanceledException());
                else
                    results[index] = t.Result;

                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    if (exceptions.Count > 0) tcs.SetException(new AggregateException(exceptions));
                    else tcs.SetResult(results);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        return tcs.Task;
    }

    public static async Task<T> RaceAsync<T>(
        IEnumerable<Func<CancellationToken, Task<T>>> ops,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var tasks = ops.Select(o => o(cts.Token)).ToList();
        var winner = await Task.WhenAny(tasks);
        cts.Cancel();
        return await winner;
    }

    public readonly record struct Settled<T>(bool IsSuccess, T? Result, Exception? Error);

    public static async Task<Settled<T>[]> WhenAllSettledAsync<T>(IEnumerable<Task<T>> tasks)
    {
        var arr = tasks.ToArray();
        var results = new Settled<T>[arr.Length];
        await Task.WhenAll(arr.Select(async (t, i) =>
        {
            try { results[i] = new Settled<T>(true, await t, null); }
            catch (Exception ex) { results[i] = new Settled<T>(false, default, ex); }
        }));
        return results;
    }
}
