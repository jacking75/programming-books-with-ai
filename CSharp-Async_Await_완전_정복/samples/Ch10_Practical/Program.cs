// 10장. 실전 패턴과 응용
//
// 사용법:
//   dotnet run --project Ch10_Practical -- frame
//   dotnet run --project Ch10_Practical -- backpressure
//   dotnet run --project Ch10_Practical -- shutdown
//   dotnet run --project Ch10_Practical -- retry
//   dotnet run --project Ch10_Practical -- fanout
//   dotnet run --project Ch10_Practical -- singleflight
//   dotnet run --project Ch10_Practical -- observer

using System.Threading.Channels;
using Ch10_Practical;

var which = args.Length > 0 ? args[0] : "frame";

switch (which)
{
    case "frame":         await FrameLoopDemo(); break;
    case "backpressure":  await BackpressureDemo(); break;
    case "shutdown":      await GracefulShutdownDemo(); break;
    case "retry":         await RetryDemo(); break;
    case "fanout":        await FanOutDemo(); break;
    case "singleflight":  await SingleFlightDemo(); break;
    case "observer":      await AsyncObserverDemo(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [frame|backpressure|shutdown|retry|fanout|singleflight|observer]");
        break;
}

// ───────────────────────────────────────
// 패턴 1: 프레임 루프
// ───────────────────────────────────────
static async Task FrameLoopDemo()
{
    Console.WriteLine("== Frame loop (30 FPS, 1초) ==");

    var inbox = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
    {
        SingleReader = true, SingleWriter = false,
    });

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

    // 가짜 패킷 생산자
    var producer = Task.Run(async () =>
    {
        int id = 0;
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await inbox.Writer.WriteAsync(id++, cts.Token);
                await Task.Delay(15, cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        inbox.Writer.Complete();
    });

    // 프레임 루프
    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
    int frame = 0;
    try
    {
        while (await timer.WaitForNextTickAsync(cts.Token))
        {
            var batch = new List<int>();
            while (inbox.Reader.TryRead(out var p)) batch.Add(p);
            Console.WriteLine($"frame {frame++}: 패킷 {batch.Count}개");
        }
    }
    catch (OperationCanceledException) { }

    await producer;
}

// ───────────────────────────────────────
// 패턴 2: 백프레셔 파이프라인
// ───────────────────────────────────────
static async Task BackpressureDemo()
{
    Console.WriteLine("== Backpressure pipeline ==");

    var stage1 = Channel.CreateBounded<int>(new BoundedChannelOptions(4)
    {
        FullMode = BoundedChannelFullMode.Wait,
    });
    var stage2 = Channel.CreateBounded<int>(new BoundedChannelOptions(4)
    {
        FullMode = BoundedChannelFullMode.Wait,
    });

    var s1 = Task.Run(async () =>
    {
        await foreach (var x in stage1.Reader.ReadAllAsync())
        {
            await Task.Delay(50);          // 천천히 처리
            await stage2.Writer.WriteAsync(x * 10);
        }
        stage2.Writer.Complete();
    });

    var s2 = Task.Run(async () =>
    {
        await foreach (var x in stage2.Reader.ReadAllAsync())
        {
            Console.WriteLine($"  sink: {x}");
        }
    });

    for (int i = 0; i < 10; i++)
    {
        Console.WriteLine($"producer enqueue {i}");
        await stage1.Writer.WriteAsync(i);
    }
    stage1.Writer.Complete();

    await Task.WhenAll(s1, s2);
}

// ───────────────────────────────────────
// 패턴 3: 그래스풀 셧다운
// ───────────────────────────────────────
static async Task GracefulShutdownDemo()
{
    Console.WriteLine("== Graceful shutdown ==");

    using var acceptCts = new CancellationTokenSource();
    using var workCts = new CancellationTokenSource();
    var inFlight = new List<Task>();

    // 가짜 워크로드
    for (int i = 0; i < 5; i++)
    {
        int id = i;
        inFlight.Add(Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500 + id * 200, workCts.Token);
                Console.WriteLine($"  worker {id} 완료");
            }
            catch (OperationCanceledException) { Console.WriteLine($"  worker {id} 취소"); }
        }));
    }

    // 셧다운 시작
    await Task.Delay(300);
    Console.WriteLine("셧다운 시작...");
    acceptCts.Cancel();

    var allDone = Task.WhenAll(inFlight);

    try
    {
        await allDone.WaitAsync(TimeSpan.FromMilliseconds(800));
        Console.WriteLine("정상 종료");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("타임아웃 — 강제 취소");
        workCts.Cancel();
        await allDone.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}

// ───────────────────────────────────────
// 패턴 4: 재시도 + 백오프 + 타임아웃
// ───────────────────────────────────────
static async Task RetryDemo()
{
    Console.WriteLine("== Retry with exponential backoff ==");

    int attempts = 0;
    var result = await WithRetryAsync(async ct =>
    {
        attempts++;
        await Task.Delay(50, ct);
        if (attempts < 3) throw new HttpRequestException("transient");
        return $"OK after {attempts} attempts";
    }, maxAttempts: 5, perCallTimeout: TimeSpan.FromSeconds(1), CancellationToken.None);

    Console.WriteLine(result);

    static async Task<T> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> op,
        int maxAttempts,
        TimeSpan perCallTimeout,
        CancellationToken ct)
    {
        for (int attempt = 1; ; attempt++)
        {
            using var perCallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            perCallCts.CancelAfter(perCallTimeout);
            try
            {
                return await op(perCallCts.Token);
            }
            catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
            {
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1));
                Console.WriteLine($"  attempt {attempt} 실패: {ex.Message} → {delay.TotalMilliseconds}ms 후 재시도");
                await Task.Delay(delay, ct);
            }
        }
    }

    static bool IsTransient(Exception ex) => ex is HttpRequestException or TimeoutException;
}

// ───────────────────────────────────────
// 패턴 5: 팬아웃-팬인
// ───────────────────────────────────────
static async Task FanOutDemo()
{
    Console.WriteLine("== Fan-out / Fan-in ==");

    var sw = System.Diagnostics.Stopwatch.StartNew();

    var profile  = LoadAsync("profile", 200);
    var inv      = LoadAsync("inventory", 300);
    var friends  = LoadAsync("friends", 250);

    await Task.WhenAll(profile, inv, friends);
    sw.Stop();

    Console.WriteLine($"3개 동시 호출 완료: {sw.ElapsedMilliseconds}ms (예상 ~300ms)");
    Console.WriteLine($"  {await profile}, {await inv}, {await friends}");

    static async Task<string> LoadAsync(string name, int ms)
    {
        await Task.Delay(ms);
        return $"{name}=ok";
    }
}

// ───────────────────────────────────────
// 패턴 6: Single-flight 캐시
// ───────────────────────────────────────
static async Task SingleFlightDemo()
{
    Console.WriteLine("== Single-flight cache ==");

    int callCount = 0;
    var cache = new SingleFlightCache<int, string>(async (id, ct) =>
    {
        Interlocked.Increment(ref callCount);
        await Task.Delay(200, ct);
        return $"value-{id}";
    });

    // 같은 id로 100개 동시 호출 → factory 는 1번만 호출돼야 함
    var tasks = Enumerable.Range(0, 100)
        .Select(_ => cache.GetAsync(42, CancellationToken.None));
    var results = await Task.WhenAll(tasks);

    Console.WriteLine($"factory 호출 횟수: {callCount} (예상: 1)");
    Console.WriteLine($"모두 같은 값: {results.Distinct().Count() == 1}");
}

// ───────────────────────────────────────
// 패턴 7: 비동기 옵저버
// ───────────────────────────────────────
static async Task AsyncObserverDemo()
{
    Console.WriteLine("== Async observer ==");

    var stream = new EventStream<string>();
    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

    var consumer = Task.Run(async () =>
    {
        try
        {
            await foreach (var evt in stream.Subscribe(cts.Token))
                Console.WriteLine($"  received: {evt}");
        }
        catch (OperationCanceledException) { }
    });

    for (int i = 0; i < 5; i++)
    {
        stream.Publish($"event-{i}");
        await Task.Delay(80);
    }

    await consumer;
}
