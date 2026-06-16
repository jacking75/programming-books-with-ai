# 10장. 실전 패턴과 응용

이 장에서는 앞에서 배운 도구들을 *조합*해 현장에서 자주 쓰는 패턴 7가지를 정리한다. 코드는 모두 .NET 10 / C# 14 기준이며, 예제 프로젝트 `Ch10_Practical`에 그대로 들어 있다.

## 10.1 패턴 1: 프레임 단위 처리 (게임 서버 틱 루프)

게임 서버는 보통 *고정 프레임*으로 돈다. 33ms마다 (30 FPS) 또는 50ms마다 (20 FPS) 누적된 패킷을 일괄 처리하고 월드 상태를 업데이트한다.

```
                 [ 프레임 루프 구조 ]
                                                              
        ┌─────────────────────────────────────────────┐
        │  1. 입력 패킷 수신 (네트워크 task가 채널에 적재) │
        │                                              │
        │  2. PeriodicTimer 틱마다:                    │
        │     a. 채널에서 큐잉된 패킷 일괄 dequeue       │
        │     b. 시뮬레이션 업데이트                    │
        │     c. 출력 패킷 broadcast                    │
        │                                              │
        │  3. 다음 틱 시각까지 await                    │
        └─────────────────────────────────────────────┘
```

> `Ch10_Practical/Program.cs · FrameLoopDemo`

```csharp
var inbox = Channel.CreateUnbounded<Packet>(new UnboundedChannelOptions
{
    SingleReader = true, SingleWriter = false,
});

// 네트워크 task (여러 연결이 채널에 패킷 적재)
foreach (var session in sessions)
    _ = Task.Run(() => session.ReceiveLoopAsync(inbox.Writer, ct));

// 게임 루프 task
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
var frame = 0L;

while (await timer.WaitForNextTickAsync(ct))
{
    // 이번 프레임에 쌓인 패킷만 일괄 처리
    var batch = new List<Packet>();
    while (inbox.Reader.TryRead(out var p)) batch.Add(p);

    foreach (var p in batch) world.Apply(p);
    world.Tick(frame++);
    await world.BroadcastAsync(ct);
}
```

핵심은 **PeriodicTimer + Channel + TryRead 루프** 조합이다. 시뮬레이션이 무거워져 한 프레임을 놓치는 경우도 `WaitForNextTickAsync`가 자연스럽게 흡수한다 (다음 틱 시각으로 알아서 정렬).

## 10.2 패턴 2: 백프레셔 파이프라인

여러 단계를 거치는 작업 흐름. 한 단계가 막히면 *앞 단계도 자연스럽게 늦춰져야* 한다. 메모리 폭발을 막는 가장 단순한 방법은 *바운디드 채널*의 체인이다.

```
[ Producer ] ─► [Bounded(100)] ─► [Stage 1] ─► [Bounded(100)] ─► [Stage 2] ─► sink
                  ^                                ^
                  큐가 가득 차면                    큐가 가득 차면
                  producer가 자동으로 대기            stage 1이 자동으로 대기
```

> `Ch10_Practical/Program.cs · BackpressureDemo`

```csharp
var stage1In = Channel.CreateBounded<Raw>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
});
var stage2In = Channel.CreateBounded<Parsed>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
});

var stage1 = Task.Run(async () =>
{
    await foreach (var raw in stage1In.Reader.ReadAllAsync(ct))
    {
        var parsed = Parse(raw);
        await stage2In.Writer.WriteAsync(parsed, ct);
    }
    stage2In.Writer.Complete();
});

var stage2 = Task.Run(async () =>
{
    await foreach (var p in stage2In.Reader.ReadAllAsync(ct))
        await SinkAsync(p, ct);
});

await foreach (var raw in producer.ReadAllAsync(ct))
    await stage1In.Writer.WriteAsync(raw, ct);
stage1In.Writer.Complete();

await Task.WhenAll(stage1, stage2);
```

각 단계가 *자신의 속도*로만 일하면 된다. 흐름 제어는 채널이 자동으로 한다.

## 10.3 패턴 3: 그래스풀 셧다운

서버 종료 시 *진행 중인 요청만 마무리하고 새 요청은 거부*하는 패턴. 단순해 보여도 정확히 하려면 토큰 분리와 순서가 중요하다.

```
┌─────────────────────────────────────────────────────────────┐
│  CancellationTokenSource 두 개를 둔다                        │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ acceptCts  : 새 요청 수신을 멈춤                       │  │
│  │ workCts    : 진행 중 작업까지 강제 취소                │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                             │
│  순서:                                                       │
│   1. acceptCts.Cancel()        ← 더 이상 받지 않는다         │
│   2. await 진행중인 작업 완료대기 (최대 timeout)              │
│   3. workCts.Cancel()          ← 타임아웃 도달, 강제 종료    │
│   4. 자원 정리 (await using)                                 │
└─────────────────────────────────────────────────────────────┘
```

> `Ch10_Practical/Program.cs · GracefulShutdown`

```csharp
public sealed class GracefulServer : IAsyncDisposable
{
    private readonly CancellationTokenSource _acceptCts = new();
    private readonly CancellationTokenSource _workCts = new();
    private readonly List<Task> _inFlight = new();

    public async Task RunAsync()
    {
        while (!_acceptCts.IsCancellationRequested)
        {
            try
            {
                var req = await AcceptAsync(_acceptCts.Token);
                var t = HandleAsync(req, _workCts.Token);
                lock (_inFlight) _inFlight.Add(t);
                _ = t.ContinueWith(c => {
                    lock (_inFlight) _inFlight.Remove(c);
                }, TaskScheduler.Default);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _acceptCts.Cancel();

        Task[] snapshot;
        lock (_inFlight) snapshot = _inFlight.ToArray();

        var shutdownTask = Task.WhenAll(snapshot)
            .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        var completed = await Task.WhenAny(
            shutdownTask, Task.Delay(TimeSpan.FromSeconds(30)));

        if (completed != shutdownTask)
            _workCts.Cancel();      // 30초 안에 안 끝났으면 강제 취소
        else
            _workCts.Cancel();      // 정상 종료여도 토큰 dispose 위해

        _acceptCts.Dispose();
        _workCts.Dispose();
    }
}
```

`Task.WhenAny`와 `Task.Delay` 조합으로 *우아한 종료 + 타임아웃 강제*를 구현했다. .NET 7+ 라면 `Task.WhenAll(...).WaitAsync(TimeSpan.FromSeconds(30))`로 더 깔끔하게 쓸 수 있다.

## 10.4 패턴 4: 재시도 + 지수 백오프 + 타임아웃

분산 환경에서 외부 호출은 반드시 *재시도/타임아웃/회로차단* 중 일부를 구현해야 한다.

> `Ch10_Practical/Program.cs · RetryDemo`

```csharp
public static async Task<T> WithRetryAsync<T>(
    Func<CancellationToken, Task<T>> op,
    int maxAttempts,
    TimeSpan perCallTimeout,
    CancellationToken ct)
{
    for (int attempt = 1; ; attempt++)
    {
        using var perCallCts = CancellationTokenSource
            .CreateLinkedTokenSource(ct);
        perCallCts.CancelAfter(perCallTimeout);

        try
        {
            return await op(perCallCts.Token);
        }
        catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
        {
            var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, ct);
        }
    }
}

static bool IsTransient(Exception ex) => ex switch
{
    HttpRequestException => true,
    TimeoutException => true,
    OperationCanceledException => false,    // 사용자 취소는 재시도 X
    _ => false,
};
```

운영 환경이라면 직접 짜기보다 [Polly](https://github.com/App-vNext/Polly) 같은 라이브러리를 권장한다. 위 코드는 *동작 원리 학습용*이다.

## 10.5 패턴 5: 팬아웃-팬인 (Fan-out / Fan-in)

여러 외부 호출을 동시에 발사하고, 결과를 모아 합치는 패턴.

```
       ┌─► API #1 ─┐
입력 ──┼─► API #2 ─┼─► 결합 ─► 결과
       └─► API #3 ─┘
```

> `Ch10_Practical/Program.cs · FanOutDemo`

```csharp
public async Task<PlayerView> BuildPlayerViewAsync(int id, CancellationToken ct)
{
    var profileTask  = _profile.GetAsync(id, ct);
    var inventoryTask = _inv.GetAsync(id, ct);
    var friendsTask   = _social.GetFriendsAsync(id, ct);

    await Task.WhenAll(profileTask, inventoryTask, friendsTask);

    return new PlayerView(
        await profileTask,
        await inventoryTask,
        await friendsTask);
}
```

⚠️ **함정 1:** 위 코드는 *모든 호출이 성공해야* 한다. 일부 실패를 허용하고 싶다면 `WhenAll` 대신 각 task에 try/catch나 `ConfigureAwait(SuppressThrowing)`을 적용.

⚠️ **함정 2:** 결과의 *제일 빠른 것만 쓰고 나머지 취소*하려면 `WhenAny` + linked CancellationToken.

```csharp
// 첫 응답이 오면 나머지 취소
using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
var winner = await Task.WhenAny(
    _a.GetAsync(linked.Token), _b.GetAsync(linked.Token), _c.GetAsync(linked.Token));
linked.Cancel();
return await winner;
```

## 10.6 패턴 6: 비동기 캐시 (Single-flight)

같은 키에 대한 *동시 요청을 하나로 합쳐* 중복 호출을 막는 패턴.

> `Ch10_Practical/Program.cs · SingleFlightCache`

```csharp
public sealed class SingleFlightCache<TKey, TValue>
    where TKey : notnull
{
    private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map = new();

    public SingleFlightCache(Func<TKey, CancellationToken, Task<TValue>> factory)
        => _factory = factory;

    public Task<TValue> GetAsync(TKey key, CancellationToken ct)
    {
        var lazy = _map.GetOrAdd(key,
            k => new Lazy<Task<TValue>>(() => _factory(k, ct)));

        return lazy.Value;
    }

    public void Invalidate(TKey key) => _map.TryRemove(key, out _);
}

// 사용
var cache = new SingleFlightCache<int, User>(LoadUserFromDbAsync);

// 같은 id로 동시 호출이 100번 와도 LoadUserFromDb는 한 번만 호출됨
await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => cache.GetAsync(42, ct)));
```

⚠️ 위 단순 버전은 *실패한 결과도 캐시*한다. 운영에서는 실패 시 `Invalidate`를 호출하고, TTL과 결합한다.

## 10.7 패턴 7: 비동기 옵저버 (IAsyncEnumerable 기반)

이벤트를 비동기 스트림으로 노출하면, 소비자가 `await foreach`로 깔끔하게 받을 수 있다.

> `Ch10_Practical/Program.cs · AsyncObserver`

```csharp
public sealed class EventStream<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>(
        new UnboundedChannelOptions { SingleReader = false });

    public void Publish(T evt) => _channel.Writer.TryWrite(evt);
    public IAsyncEnumerable<T> Subscribe(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
    public void Complete() => _channel.Writer.Complete();
}

// 소비자
await foreach (var evt in stream.Subscribe(ct))
{
    await HandleAsync(evt);
}
```

⚠️ 위 구현은 *한 채널을 모든 구독자가 공유*한다. 즉 "구독자 N명이 같은 이벤트를 모두 받는" 진짜 옵저버가 아니라, "한 이벤트를 N명이 나눠 가져가는" 워커 큐다. 진짜 멀티캐스트가 필요하면 구독자별 채널을 두거나 `Microsoft.Reactive.AsyncRx` 같은 라이브러리를 쓴다.

## 10.8 정리: 패턴 매트릭스

| 상황 | 패턴 | 핵심 도구 |
|------|------|-----------|
| 고정 주기 처리 | 프레임 루프 | `PeriodicTimer` + `Channel` |
| 단계별 처리 | 백프레셔 파이프라인 | `Bounded Channel` 체인 |
| 종료 시 우아하게 | 그래스풀 셧다운 | `CTS` 2개 + `WhenAny` + `Delay` |
| 외부 호출 신뢰성 | 재시도/타임아웃 | `CTS.CancelAfter` + 백오프 |
| 여러 API 동시 호출 | 팬아웃-팬인 | `Task.WhenAll` / `WhenAny` |
| 중복 호출 차단 | 단일-비행 캐시 | `ConcurrentDictionary` + `Lazy<Task<T>>` |
| 이벤트 스트림 | 비동기 옵저버 | `IAsyncEnumerable` + `Channel` |

## 10.9 마지막 체크리스트

여기까지 왔다면 다음을 모두 확신할 수 있어야 한다.

- [ ] 동기/비동기/병렬의 차이를 누군가에게 5분 안에 설명할 수 있다.
- [ ] `await` 한 줄이 컴파일 시 어떻게 펼쳐지는지 그릴 수 있다.
- [ ] `SynchronizationContext`가 어디서 `null`이고 어디서 의미 있는지 안다.
- [ ] `ConfigureAwait(false)`를 *언제 붙여야 하는지* 결정할 수 있다.
- [ ] `async void`, `.Result`, `Task.Run`-wrap의 함정을 안다.
- [ ] `SemaphoreSlim`과 `Channel<T>`로 비동기 동기화를 구현할 수 있다.
- [ ] `AsyncLocal<T>`이 호출 트리에서 어떻게 흐르는지 안다.
- [ ] `ValueTask`, `IAsyncEnumerable`, `CancellationToken`, `Parallel.ForEachAsync`를 적재적소에 쓸 수 있다.
- [ ] 그래스풀 셧다운 / 백프레셔 파이프라인 / 팬아웃 패턴을 코드로 그릴 수 있다.

이 책의 본문은 여기서 끝난다. 부록에서는 *용어집*과 *디버깅 치트시트*를 정리해 두었다.
