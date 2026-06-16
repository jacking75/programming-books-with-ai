// 3장. ConfigureAwait 깊이 파기
//
// 사용법:
//   dotnet run --project Ch03_ConfigureAwait -- gui-compare
//   dotnet run --project Ch03_ConfigureAwait -- two-awaits
//   dotnet run --project Ch03_ConfigureAwait -- options

using System.Collections.Concurrent;

var which = args.Length > 0 ? args[0] : "gui-compare";

switch (which)
{
    case "gui-compare": SingleThreadSyncContext.Run(GuiCompareAsync); break;
    case "two-awaits":  SingleThreadSyncContext.Run(TwoAwaitsAsync); break;
    case "options":     await OptionsDemoAsync(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [gui-compare|two-awaits|options]");
        break;
}

// ───────────────────────────────────────────────────────────────────────
// 기본 await vs ConfigureAwait(false) 비교 — GUI 컨텍스트에서만 의미 있음
// ───────────────────────────────────────────────────────────────────────
static async Task GuiCompareAsync()
{
    Print("=== Default await (캡처된 컨텍스트로 복귀) ===");
    await DefaultAwaitAsync();

    Print("=== ConfigureAwait(false) (컨텍스트 캡처 안 함) ===");
    await ConfiguredAwaitAsync();
}

static async Task DefaultAwaitAsync()
{
    Print($"Before: {Environment.CurrentManagedThreadId}");
    await Task.Run(() => Print($"In Run: {Environment.CurrentManagedThreadId}"));
    Print($"After : {Environment.CurrentManagedThreadId}");
}

static async Task ConfiguredAwaitAsync()
{
    Print($"Before: {Environment.CurrentManagedThreadId}");
    await Task.Run(() => Print($"In Run: {Environment.CurrentManagedThreadId}"))
        .ConfigureAwait(false);
    Print($"After : {Environment.CurrentManagedThreadId}");
}

// ───────────────────────────────────────────────────────────────────────
// 한 번 false 면 그 후 true 도 의미 없음
// ───────────────────────────────────────────────────────────────────────
static async Task TwoAwaitsAsync()
{
    Print($"Before: {Environment.CurrentManagedThreadId}");

    await Task.Run(() => Print($"Run1  : {Environment.CurrentManagedThreadId}"))
        .ConfigureAwait(false);
    Print($"After1: {Environment.CurrentManagedThreadId}");

    await Task.Run(() => Print($"Run2  : {Environment.CurrentManagedThreadId}"))
        .ConfigureAwait(true);  // 이제 와서 true 여도 컨텍스트 없음
    Print($"After2: {Environment.CurrentManagedThreadId}");
}

// ───────────────────────────────────────────────────────────────────────
// .NET 8+ ConfigureAwaitOptions
// ───────────────────────────────────────────────────────────────────────
static async Task OptionsDemoAsync()
{
    Print("=== SuppressThrowing: 예외/취소를 await 에서 던지지 않음 ===");
    var failing = Task.Run<int>(() => throw new InvalidOperationException("boom"));
    await failing.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    Print($"IsFaulted={failing.IsFaulted}, Exception={failing.Exception?.InnerException?.Message}");

    Print("=== ForceYielding: 동기 완료여도 강제 비동기 양보 ===");
    Print($"Before: {Environment.CurrentManagedThreadId}");
    await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
    Print($"After : {Environment.CurrentManagedThreadId}");
}

static void Print(string msg)
{
    Console.WriteLine(
        $"[{DateTime.Now:HH:mm:ss.fff}] ctx={SynchronizationContext.Current?.GetType().Name ?? "null"} {msg}");
}

// ───────────────────────────────────────────────────────────────────────
// 교육용 GUI 흉내 컨텍스트 (2장과 동일)
// ───────────────────────────────────────────────────────────────────────
internal sealed class SingleThreadSyncContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback cb, object? state)> _q = new();
    public override void Post(SendOrPostCallback d, object? state) => _q.Add((d, state));
    public void Complete() => _q.CompleteAdding();
    public void Pump()
    {
        foreach (var (cb, st) in _q.GetConsumingEnumerable()) cb(st);
    }

    public static void Run(Func<Task> entry)
    {
        var prev = Current;
        var ctx = new SingleThreadSyncContext();
        SetSynchronizationContext(ctx);
        try
        {
            var t = entry();
            t.ContinueWith(_ => ctx.Complete(), TaskScheduler.Default);
            ctx.Pump();
            t.GetAwaiter().GetResult();
        }
        finally { SetSynchronizationContext(prev); }
    }
}
