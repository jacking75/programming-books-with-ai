// 9장. .NET 10 비동기 신무기
//
// 사용법:
//   dotnet run --project Ch09_ModernFeatures -- valuetask
//   dotnet run --project Ch09_ModernFeatures -- enumerable
//   dotnet run --project Ch09_ModernFeatures -- timer
//   dotnet run --project Ch09_ModernFeatures -- channel
//   dotnet run --project Ch09_ModernFeatures -- parallel
//   dotnet run --project Ch09_ModernFeatures -- waitasync
//   dotnet run --project Ch09_ModernFeatures -- options

using System.Runtime.CompilerServices;
using System.Threading.Channels;

var which = args.Length > 0 ? args[0] : "enumerable";

switch (which)
{
    case "valuetask":   await ValueTaskBenchmark(); break;
    case "enumerable":  await AsyncEnumerableDemo(); break;
    case "timer":       await PeriodicTimerDemo(); break;
    case "channel":     await ChannelPipeline(); break;
    case "parallel":    await ParallelForEachAsyncDemo(); break;
    case "waitasync":   await WaitAsyncDemo(); break;
    case "options":     await OptionsDemo(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [valuetask|enumerable|timer|channel|parallel|waitasync|options]");
        break;
}

// ───────────────────────────────────────
static async Task ValueTaskBenchmark()
{
    Console.WriteLine("== ValueTask vs Task (캐시 hit 시나리오) ==");

    var sw = System.Diagnostics.Stopwatch.StartNew();
    long acc1 = 0;
    for (int i = 0; i < 1_000_000; i++) acc1 += await TaskHit(i);
    sw.Stop();
    Console.WriteLine($"Task<int> :  {sw.ElapsedMilliseconds}ms  acc={acc1}");

    sw.Restart();
    long acc2 = 0;
    for (int i = 0; i < 1_000_000; i++) acc2 += await ValueTaskHit(i);
    sw.Stop();
    Console.WriteLine($"ValueTask :  {sw.ElapsedMilliseconds}ms  acc={acc2}");

    static Task<int> TaskHit(int n) => Task.FromResult(n);
    static ValueTask<int> ValueTaskHit(int n) => new(n);
}

// ───────────────────────────────────────
static async Task AsyncEnumerableDemo()
{
    Console.WriteLine("== IAsyncEnumerable: 페이지 스트림 ==");

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    try
    {
        await foreach (var item in ReadPagesAsync().WithCancellation(cts.Token))
        {
            Console.WriteLine($"got: {item}");
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("(2초 후 자동 취소)");
    }

    static async IAsyncEnumerable<int> ReadPagesAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int n = 0;
        while (true)
        {
            await Task.Delay(200, ct);
            yield return n++;
        }
    }
}

// ───────────────────────────────────────
static async Task PeriodicTimerDemo()
{
    Console.WriteLine("== PeriodicTimer: 30 FPS 틱 루프 ==");
    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

    int frame = 0;
    try
    {
        while (await timer.WaitForNextTickAsync(cts.Token))
        {
            frame++;
        }
    }
    catch (OperationCanceledException) { }

    Console.WriteLine($"1초 동안 {frame} 틱 (이상적으로는 약 30)");
}

// ───────────────────────────────────────
static async Task ChannelPipeline()
{
    Console.WriteLine("== Channel: 1 producer / N workers ==");

    var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(64)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = false,
    });

    var workers = Enumerable.Range(0, 4).Select(id => Task.Run(async () =>
    {
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            await Task.Delay(50);
            Console.WriteLine($"  worker {id}: {item}");
        }
    })).ToArray();

    for (int i = 0; i < 16; i++)
        await channel.Writer.WriteAsync(i);
    channel.Writer.Complete();

    await Task.WhenAll(workers);
}

// ───────────────────────────────────────
static async Task ParallelForEachAsyncDemo()
{
    Console.WriteLine("== Parallel.ForEachAsync: 동시성 제한 ==");

    var sw = System.Diagnostics.Stopwatch.StartNew();
    await Parallel.ForEachAsync(
        Enumerable.Range(0, 20),
        new ParallelOptions { MaxDegreeOfParallelism = 4 },
        async (i, ct) =>
        {
            await Task.Delay(100, ct);
        });
    sw.Stop();
    Console.WriteLine($"20개 작업 (각 100ms, 동시 4): {sw.ElapsedMilliseconds}ms (예상 ~500ms)");
}

// ───────────────────────────────────────
static async Task WaitAsyncDemo()
{
    Console.WriteLine("== Task.WaitAsync: 외부에서 타임아웃 걸기 ==");

    var slow = Task.Run(async () =>
    {
        await Task.Delay(2000);
        return "done";
    });

    try
    {
        var result = await slow.WaitAsync(TimeSpan.FromMilliseconds(500));
        Console.WriteLine($"result={result}");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("TimeoutException 발생 — Task는 계속 돌아가지만 await는 풀린다");
    }
}

// ───────────────────────────────────────
static async Task OptionsDemo()
{
    Console.WriteLine("== ConfigureAwaitOptions: SuppressThrowing ==");

    var tasks = new[]
    {
        Task.Run<int>(() => throw new InvalidOperationException("a")),
        Task.Run(() => 1),
        Task.Run<int>(() => throw new InvalidOperationException("b")),
    };

    foreach (var t in tasks)
    {
        await t.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    var faulted = tasks.Count(t => t.IsFaulted);
    var ok = tasks.Count(t => t.IsCompletedSuccessfully);
    Console.WriteLine($"실패={faulted}, 성공={ok}  (예외가 await로 새지 않았다)");
}
