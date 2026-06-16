// 6장. 비동기의 함정 12가지 — 실제로 동작을 보여주는 데모
//
// 사용법:
//   dotnet run --project Ch06_Pitfalls -- 1  ... -- 12

var which = args.Length > 0 ? args[0] : "1";

switch (which)
{
    case "1":  await Pitfall1_AsyncNotAsync(); break;
    case "2":  await Pitfall2_IgnoredTask(); break;
    case "3":  Pitfall3_AsyncVoidException(); break;
    case "4":  Pitfall4_AsyncVoidLambda(); break;
    case "5":  await Pitfall5_NestedTask(); break;
    case "6":  Pitfall6_ResultException(); break;
    case "11": await Pitfall11_ValueTaskTwice(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [1|2|3|4|5|6|11]");
        break;
}

// ──────────────────────────────────
// 함정 ①: async 메서드의 첫 await 이전 코드는 호출자 스레드에서 동기 실행
// ──────────────────────────────────
static async Task Pitfall1_AsyncNotAsync()
{
    Console.WriteLine("== Pitfall 1: async 메서드는 비동기로 시작하지 않는다 ==");

    var child = WorkThenWaitAsync();
    Console.WriteLine("started");
    await child;
    Console.WriteLine("completed");

    static async Task WorkThenWaitAsync()
    {
        Thread.Sleep(500);                  // 호출자 스레드 점유
        Console.WriteLine("work");
        await Task.Delay(500);
    }

    // 출력 순서: work → started → completed   (started 가 1등이 아니다!)
}

// ──────────────────────────────────
// 함정 ②: await 없는 Task.Delay
// ──────────────────────────────────
static async Task Pitfall2_IgnoredTask()
{
    Console.WriteLine("== Pitfall 2: 결과 무시 ==");
    Console.WriteLine("Before");
    _ = Task.Delay(500);    // 의도적인 무시 (실제로는 버그)
    Console.WriteLine("After (즉시 출력됨)");
    await Task.Delay(700);  // 종료 전 대기
}

// ──────────────────────────────────
// 함정 ③: async void 의 예외는 catch에 안 잡힘
// ──────────────────────────────────
static void Pitfall3_AsyncVoidException()
{
    Console.WriteLine("== Pitfall 3: async void 예외는 catch 못 함 ==");
    try
    {
        ThrowAsync();
    }
    catch (Exception)
    {
        Console.WriteLine("Caught (X)");
    }
    Console.WriteLine("(catch에 안 잡혀서 이 줄이 보인다. 실제 환경이라면 잠시 후 프로세스 다운)");
    Thread.Sleep(200);

    static async void ThrowAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }
}

// ──────────────────────────────────
// 함정 ④: Parallel.For 의 async 람다 → async void 폭주
// ──────────────────────────────────
static void Pitfall4_AsyncVoidLambda()
{
    Console.WriteLine("== Pitfall 4: Parallel.For + async = async void ==");
    var sw = System.Diagnostics.Stopwatch.StartNew();
    Parallel.For(0, 10, async i =>
    {
        await Task.Delay(500);
        // 이 출력은 보이지 않을 수 있다 (Parallel.For 가 이미 끝남)
    });
    sw.Stop();
    Console.WriteLine($"Parallel.For 완료: {sw.ElapsedMilliseconds}ms (500ms 이하면 비동기 무시됨)");
    Thread.Sleep(700);
}

// ──────────────────────────────────
// 함정 ⑤: Task.Factory.StartNew + async 람다 = Task<Task>
// ──────────────────────────────────
static async Task Pitfall5_NestedTask()
{
    Console.WriteLine("== Pitfall 5: 중첩 Task ==");
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await Task.Factory.StartNew(async () =>
    {
        await Task.Delay(500);
    });
    sw.Stop();
    Console.WriteLine($"StartNew 결과: {sw.ElapsedMilliseconds}ms (500ms가 아니라 거의 즉시)");

    sw.Restart();
    await Task.Run(async () =>
    {
        await Task.Delay(500);
    });
    sw.Stop();
    Console.WriteLine($"Task.Run  결과: {sw.ElapsedMilliseconds}ms (정상)");
}

// ──────────────────────────────────
// 함정 ⑥: .Result 의 예외 래핑
// ──────────────────────────────────
static void Pitfall6_ResultException()
{
    Console.WriteLine("== Pitfall 6: .Result 의 AggregateException 래핑 ==");
    try
    {
        var _ = FaultAsync().Result;
    }
    catch (InvalidOperationException)
    {
        Console.WriteLine("InvalidOperationException 으로 잡힘 (X)");
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($".Result는 AggregateException 으로 감쌌다: inner={ex.InnerException?.GetType().Name}");
    }

    try
    {
        var _ = FaultAsync().GetAwaiter().GetResult();
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"GetAwaiter().GetResult() 는 원본 예외 그대로: {ex.GetType().Name}");
    }

    static async Task<int> FaultAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom");
    }
}

// ──────────────────────────────────
// 함정 ⑪: ValueTask 두 번 await
// ──────────────────────────────────
static async Task Pitfall11_ValueTaskTwice()
{
    Console.WriteLine("== Pitfall 11: ValueTask 두 번 await ==");

    ValueTask<int> vt = HotAsync();

    try
    {
        int a = await vt;
        int b = await vt;       // 미정의 — 환경에 따라 예외 또는 잘못된 값
        Console.WriteLine($"a={a}, b={b}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"두 번째 await에서 예외: {ex.GetType().Name}");
    }

    // ✅ 올바른 사용: AsTask 로 변환 후 보관
    Task<int> t = HotAsync().AsTask();
    int x = await t;
    int y = await t;
    Console.WriteLine($"AsTask로 변환: x={x}, y={y}");

    static async ValueTask<int> HotAsync()
    {
        await Task.Yield();
        return 42;
    }
}
