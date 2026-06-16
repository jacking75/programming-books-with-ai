// 5장. ASP.NET / 서버에서 async/await — 실측 데모
//
// 사용법:
//   dotnet run --project Ch05_ServerAsync -- sync
//   dotnet run --project Ch05_ServerAsync -- async
//
// 동일한 부하 (100개 작업, 각 200ms) 를 동기/비동기로 처리할 때
// ThreadPool 점유와 완료 시간이 어떻게 다른지 보여준다.

using System.Diagnostics;

var which = args.Length > 0 ? args[0] : "async";

// 일부러 ThreadPool 작게 만들기 — 차이를 극명하게 보기 위해
ThreadPool.SetMinThreads(8, 8);
ThreadPool.SetMaxThreads(8, 8);

const int RequestCount = 100;
const int WorkMs = 200;

Console.WriteLine($"=== {which.ToUpperInvariant()} 부하: 요청 {RequestCount}개, 작업 {WorkMs}ms ===");
Console.WriteLine($"ThreadPool max: 8 workers");

var sw = Stopwatch.StartNew();

if (which == "sync")
{
    // 동기: ThreadPool 스레드 8개가 200ms 씩 다 막힌다
    var tasks = Enumerable.Range(0, RequestCount)
        .Select(i => Task.Run(() => SyncHandle(i)))
        .ToArray();
    await Task.WhenAll(tasks);
}
else
{
    // 비동기: 8개 스레드로도 거의 동시에 처리
    var tasks = Enumerable.Range(0, RequestCount)
        .Select(AsyncHandle)
        .ToArray();
    await Task.WhenAll(tasks);
}

sw.Stop();
Console.WriteLine($"완료까지: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"이론적 최소(병렬 무한대): {WorkMs}ms");

static void SyncHandle(int id) => Thread.Sleep(WorkMs);
static async Task AsyncHandle(int id) => await Task.Delay(WorkMs);
