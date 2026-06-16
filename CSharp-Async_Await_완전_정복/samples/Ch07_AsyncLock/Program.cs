// 7장. 비동기 환경의 동기화 객체
//
// 사용법:
//   dotnet run --project Ch07_AsyncLock -- semaphore
//   dotnet run --project Ch07_AsyncLock -- asynclock
//   dotnet run --project Ch07_AsyncLock -- channel
//   dotnet run --project Ch07_AsyncLock -- limit

using System.Threading.Channels;
using Ch07_AsyncLock;

var which = args.Length > 0 ? args[0] : "asynclock";

switch (which)
{
    case "semaphore":  await SemaphoreDemo(); break;
    case "asynclock":  await AsyncLockDemo(); break;
    case "channel":    await ChannelDemo(); break;
    case "limit":      await ConcurrencyLimitDemo(); break;
    default:
        Console.WriteLine("usage: dotnet run -- [semaphore|asynclock|channel|limit]");
        break;
}

// ─────────────────────────────────────────────
static async Task SemaphoreDemo()
{
    Console.WriteLine("== SemaphoreSlim 으로 비동기 mutex ==");
    var sem = new SemaphoreSlim(1, 1);

    var tasks = Enumerable.Range(0, 5).Select(async i =>
    {
        await sem.WaitAsync();
        try
        {
            Console.WriteLine($"[{i}] enter");
            await Task.Delay(200);
            Console.WriteLine($"[{i}] exit");
        }
        finally { sem.Release(); }
    });
    await Task.WhenAll(tasks);
}

// ─────────────────────────────────────────────
static async Task AsyncLockDemo()
{
    Console.WriteLine("== AsyncLock + using 패턴 ==");
    var asyncLock = new AsyncLock();
    var counter = 0;

    var tasks = Enumerable.Range(0, 10).Select(async _ =>
    {
        using (await asyncLock.LockAsync())
        {
            // 임계 영역에서 비동기 작업도 안전하게
            var cur = counter;
            await Task.Delay(10);
            counter = cur + 1;
        }
    });
    await Task.WhenAll(tasks);

    Console.WriteLine($"counter = {counter} (정확히 10이면 락이 동작)");
}

// ─────────────────────────────────────────────
static async Task ChannelDemo()
{
    Console.WriteLine("== Channel<T> 로 producer/consumer ==");

    var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(10)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
    });

    var consumer = Task.Run(async () =>
    {
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            Console.WriteLine($"got: {item}");
            await Task.Delay(50);
        }
    });

    for (int i = 0; i < 20; i++)
    {
        await channel.Writer.WriteAsync(i);
        Console.WriteLine($"sent: {i}");
    }
    channel.Writer.Complete();

    await consumer;
}

// ─────────────────────────────────────────────
static async Task ConcurrencyLimitDemo()
{
    Console.WriteLine("== SemaphoreSlim(4,4) 로 동시 제한 ==");
    var limit = new SemaphoreSlim(4, 4);
    var active = 0;
    var max = 0;

    async Task WorkAsync(int id)
    {
        await limit.WaitAsync();
        try
        {
            var cur = Interlocked.Increment(ref active);

            // 안전한 InterlockedMax 패턴
            int prev = Volatile.Read(ref max);
            while (cur > prev)
            {
                int actual = Interlocked.CompareExchange(ref max, cur, prev);
                if (actual == prev) break;
                prev = actual;
            }

            await Task.Delay(100);
            Interlocked.Decrement(ref active);
        }
        finally { limit.Release(); }
    }

    var tasks = Enumerable.Range(0, 50).Select(WorkAsync);
    await Task.WhenAll(tasks);

    Console.WriteLine($"동시 실행 최대치: {max} (예상: 4)");
}
