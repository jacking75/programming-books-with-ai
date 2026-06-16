// 8장. AsyncLocal과 실행 컨텍스트
//
// 사용법:
//   dotnet run --project Ch08_AsyncLocal -- basic
//   dotnet run --project Ch08_AsyncLocal -- callback
//   dotnet run --project Ch08_AsyncLocal -- scope

using Ch08_AsyncLocal;

var which = args.Length > 0 ? args[0] : "basic";

switch (which)
{
    case "basic":    await BasicDemo(); break;
    case "callback": await CallbackDemo(); break;
    case "scope":    await ScopeDemo(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [basic|callback|scope]");
        break;
}

// ─────────────────────────────────────────────────────
// 본문 8.2 와 동일한 데모 — 카피온라이트 동작 확인
// ─────────────────────────────────────────────────────
static async Task BasicDemo()
{
    var x = new AsyncLocal<int>();

    Console.WriteLine($"Step 0. {x.Value}");
    x.Value = 1;
    Console.WriteLine($"Step 1. {x.Value}");

    await Task.Run(async () =>
    {
        Console.WriteLine($"Step 2. {x.Value}");
        x.Value = 2;
        Console.WriteLine($"Step 3. {x.Value}");

        await Task.Run(async () =>
        {
            Console.WriteLine($"Step 4. {x.Value}");
            x.Value = 3;
            Console.WriteLine($"Step 5. {x.Value}");

            await Task.Delay(1);

            Console.WriteLine($"Step 6. {x.Value}");
            x.Value = 4;
            Console.WriteLine($"Step 7. {x.Value}");
        });

        Console.WriteLine($"Step 8. {x.Value}     // 자식 변경 안 보임 (카피온라이트)");
    });

    Console.WriteLine($"Step 9. {x.Value}     // 자식/손자 변경 안 보임");
}

// ─────────────────────────────────────────────────────
static async Task CallbackDemo()
{
    var x = new AsyncLocal<int>(args =>
    {
        Console.WriteLine(
            $"  callback: prev={args.PreviousValue}, curr={args.CurrentValue}, " +
            $"ctxChanged={args.ThreadContextChanged}");
    });

    Console.WriteLine("x.Value = 1");
    x.Value = 1;
    Console.WriteLine("await Task.Run(...)");
    await Task.Run(() =>
    {
        Console.WriteLine($"  in task: x={x.Value}");
        x.Value = 2;
    });
    Console.WriteLine($"after: x={x.Value}");
}

// ─────────────────────────────────────────────────────
// 라이브러리 친화 패턴: Begin → using 블록 → 자동 복원
// ─────────────────────────────────────────────────────
static async Task ScopeDemo()
{
    Console.WriteLine($"outer: {RequestContext.Current}");
    using (RequestContext.Begin("req-001", "user-42"))
    {
        Console.WriteLine($"inner: {RequestContext.Current}");
        await DeeplyAsync();
    }
    Console.WriteLine($"after using: {RequestContext.Current}");
}

static async Task DeeplyAsync()
{
    await Task.Delay(10);
    Console.WriteLine($"  deep: {RequestContext.Current}");
    await DeeperAsync();
}

static async Task DeeperAsync()
{
    await Task.Yield();
    Console.WriteLine($"  deeper: {RequestContext.Current}");
}
