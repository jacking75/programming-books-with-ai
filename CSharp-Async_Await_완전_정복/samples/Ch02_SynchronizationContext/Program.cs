// 2장. SynchronizationContext의 비밀
//
// 사용법:
//   dotnet run --project Ch02_SynchronizationContext -- console
//   dotnet run --project Ch02_SynchronizationContext -- gui

using System.Collections.Concurrent;

var which = args.Length > 0 ? args[0] : "console";

switch (which)
{
    case "console":     await ConsoleDemo(); break;
    case "gui":         WinFormsLikeDemo(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [console|gui]");
        break;
}

// ───────────────────────────────────────────────────────────────────────
// 콘솔 데모: Current 가 null 이고 await 후 스레드가 바뀐다
// ───────────────────────────────────────────────────────────────────────
static async Task ConsoleDemo()
{
    Console.WriteLine($"Context: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
    Console.WriteLine($"Before await: thread {Environment.CurrentManagedThreadId}");
    await Task.Run(() =>
        Console.WriteLine($"In Task.Run : thread {Environment.CurrentManagedThreadId}"));
    Console.WriteLine($"After  await: thread {Environment.CurrentManagedThreadId}");
}

// ───────────────────────────────────────────────────────────────────────
// GUI 흉내 데모: 단일 스레드 펌프 + 컨텍스트 설치
// ───────────────────────────────────────────────────────────────────────
static void WinFormsLikeDemo()
{
    SingleThreadSyncContext.Run(async () =>
    {
        Console.WriteLine($"Context: {SynchronizationContext.Current?.GetType().Name ?? "null"}");
        Console.WriteLine($"Before await: thread {Environment.CurrentManagedThreadId}");
        await Task.Run(() =>
            Console.WriteLine($"In Task.Run : thread {Environment.CurrentManagedThreadId}"));
        Console.WriteLine($"After  await: thread {Environment.CurrentManagedThreadId}");
    });
}

// ───────────────────────────────────────────────────────────────────────
// 교육용 단일-스레드 펌프 컨텍스트
// (WinForms / WPF SynchronizationContext 와 본질적으로 같은 구조)
// ───────────────────────────────────────────────────────────────────────
internal sealed class SingleThreadSyncContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback cb, object? state)> _queue = new();

    public override void Post(SendOrPostCallback d, object? state)
        => _queue.Add((d, state));

    public override void Send(SendOrPostCallback d, object? state)
        => throw new NotSupportedException();

    public void Complete() => _queue.CompleteAdding();

    public void Pump()
    {
        foreach (var (cb, state) in _queue.GetConsumingEnumerable())
            cb(state);
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
        finally
        {
            SetSynchronizationContext(prev);
        }
    }
}
