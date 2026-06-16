// 1장. 동기와 비동기, 그리고 Task — 예제 모음
//
// 사용법:
//   dotnet run --project Ch01_Basics -- basics
//   dotnet run --project Ch01_Basics -- factories
//   dotnet run --project Ch01_Basics -- threads-vs-tasks

using System.Net.Http;

var which = args.Length > 0 ? args[0] : "basics";

switch (which)
{
    case "basics":          await Example01_Basics(); break;
    case "factories":       await Example02_TaskFactories(); break;
    case "threads-vs-tasks": await Example03_ThreadsVsTasks(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [basics|factories|threads-vs-tasks]");
        break;
}

// ───────────────────────────────────────────────────────────────────────
// Example01: 가장 단순한 async/await
// ───────────────────────────────────────────────────────────────────────
static async Task Example01_Basics()
{
    Console.WriteLine("== Example01: Basics ==");
    using var client = new HttpClient();
    string body = await DownloadAsync(client, "https://example.com");
    Console.WriteLine($"length={body.Length}");

    static async Task<string> DownloadAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}

// ───────────────────────────────────────────────────────────────────────
// Example02: Task를 만드는 5가지 방법
// ───────────────────────────────────────────────────────────────────────
static async Task Example02_TaskFactories()
{
    Console.WriteLine("== Example02: TaskFactories ==");

    // (1) async 메서드
    var s = await AsAsyncMethod();
    Console.WriteLine($"(1) async method: {s}");

    // (2) Task.Run — CPU 작업
    int sum = await Task.Run(() =>
    {
        int total = 0;
        for (int i = 0; i < 1_000_000; i++) total += i;
        return total;
    });
    Console.WriteLine($"(2) Task.Run    : {sum}");

    // (3) Task.FromResult — 이미 결과가 있을 때
    var cached = await CachedAsync();
    Console.WriteLine($"(3) FromResult  : {cached}");

    // (4) TaskCompletionSource — 콜백 API를 Task로
    var notifier = new SimpleNotifier();
    var waiter = WaitForFirstAsync(notifier);
    notifier.Fire("hello");
    Console.WriteLine($"(4) TCS         : {await waiter}");

    // (5) Task.WhenAll
    var all = await Task.WhenAll(
        Task.FromResult(1),
        Task.FromResult(2),
        Task.FromResult(3));
    Console.WriteLine($"(5) WhenAll     : [{string.Join(",", all)}]");

    static async Task<string> AsAsyncMethod()
    {
        await Task.Delay(10);
        return "from async method";
    }

    static Task<string> CachedAsync() => Task.FromResult("cached");

    static Task<string> WaitForFirstAsync(SimpleNotifier notifier)
    {
        var tcs = new TaskCompletionSource<string>();
        notifier.OnFire += s => tcs.TrySetResult(s);
        return tcs.Task;
    }
}

// ───────────────────────────────────────────────────────────────────────
// Example03: Thread vs Task 비교
// ───────────────────────────────────────────────────────────────────────
static async Task Example03_ThreadsVsTasks()
{
    Console.WriteLine("== Example03: Threads vs Tasks ==");

    // I/O 바운드 작업 1000개 — Thread로는 불가능, Task로는 가뿐히
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var tasks = Enumerable.Range(0, 1000)
        .Select(async _ => { await Task.Delay(100); return 1; });
    int[] results = await Task.WhenAll(tasks);
    sw.Stop();

    Console.WriteLine($"1000 비동기 작업: {sw.ElapsedMilliseconds}ms, 합계={results.Sum()}");
    Console.WriteLine($"실제로 만들어진 스레드는 ThreadPool의 worker 몇 개에 불과하다.");
}

internal sealed class SimpleNotifier
{
    public event Action<string>? OnFire;
    public void Fire(string s) => OnFire?.Invoke(s);
}
