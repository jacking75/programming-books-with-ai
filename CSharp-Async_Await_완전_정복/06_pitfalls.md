# 6장. 비동기의 함정 12가지

현장에서 마주치는 함정을 12개로 추렸다. 각 함정마다 *왜 그런가*, *어떻게 피하는가*, *분석기로 잡을 수 있는가*를 함께 정리한다.

```
함정 지도
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│   ① async가 비동기가 아니다       ② Task.Delay만 호출         │
│   ③ async void                    ④ async void 람다           │
│   ⑤ 중첩 Task                     ⑥ .Wait() / .Result          │
│   ⑦ Fire-and-forget               ⑧ CancellationToken 누락     │
│   ⑨ 예외가 사라진다               ⑩ await 안 한 Task가 GC됨    │
│   ⑪ ValueTask 두 번 await         ⑫ Parallel.ForEach + async   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## 6.1 함정 ①: async 메서드는 비동기로 시작하지 않는다

```csharp
async Task WorkThenWaitAsync()
{
    Thread.Sleep(1000);                  // ★ 호출 스레드에서 동기 실행됨
    Console.WriteLine("work");
    await Task.Delay(1000);
}

async Task DemoAsync()
{
    var child = WorkThenWaitAsync();     // 여기서 1초 블로킹된다
    Console.WriteLine("started");
    await child;
    Console.WriteLine("completed");
}
```

기대: `started` → `work` → `completed`
실제: `work` → `started` → `completed`

이유는 4장에서 본 상태 머신에 있다. `async` 메서드의 *첫 `await` 이전 코드는 호출자의 스레드에서 동기로 실행*된다. 메서드 안에 `await`가 등장하기 전까지는 그냥 일반 함수와 같다.

✅ **해결:** 정말로 즉시 양보가 필요하면 `await Task.Yield();`를 첫 줄에 넣는다.

```csharp
async Task WorkThenWaitAsync()
{
    await Task.Yield();                  // 호출자에게 즉시 제어 반환
    Thread.Sleep(1000);
    Console.WriteLine("work");
    await Task.Delay(1000);
}
```

## 6.2 함정 ②: await 없는 Task.Delay

```csharp
async Task HandlerAsync()
{
    Console.WriteLine("Before");
    Task.Delay(1000);                    // ❌ 결과를 무시
    Console.WriteLine("After");
}
```

`Task.Delay(1000)`은 *1초 후에 완료되는 Task 객체*를 반환만 한다. 그걸 `await`하지 않으면 그냥 객체 하나가 만들어지고 버려질 뿐이다. "Before" 직후 "After"가 출력된다.

✅ **해결:** 컴파일러 분석기(`CS4014`, `CA2012` 등)가 보통 잡아 준다. `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`를 켜 두자.

## 6.3 함정 ③: async void

```csharp
async void ThrowExceptionAsync()
{
    throw new InvalidOperationException();
}

public void Caller()
{
    try { ThrowExceptionAsync(); }
    catch (Exception) { Console.WriteLine("Failed"); }   // 실행 안 됨!
}
```

`async void`는 *반환 Task 없이* 동작한다. 그래서 호출자가 `await`할 수 없고, 던져진 예외도 호출 스택을 타고 올라오지 못한다. 예외는 `SynchronizationContext`(또는 ThreadPool의 unobserved 예외 핸들러)로 흘러간다.

✅ **해결:** `async void`는 *이벤트 핸들러에만 한정*한다. WPF/WinForms의 `Button_Click` 같은 자리. 그 외에는 모두 `async Task` 또는 `async ValueTask`.

⚠️ async void가 호출되면 unhandled exception이 프로세스 종료로 이어진다. 게임 서버에서 이 한 줄로 야간에 서버가 죽은 사례를 여러 번 봤다.

## 6.4 함정 ④: async void 람다

```csharp
Parallel.For(0, 10, async i =>          // ❌ 람다가 Action으로 추론 → async void
{
    await Task.Delay(1000);
});
```

`Parallel.For`의 두 번째 인자는 `Action<int>`다. `async i => ...` 람다가 여기에 맞춰지면서 `async void` 형태가 된다. `Parallel.For`는 람다가 *동기적으로 즉시 끝났다*고 판단하고 모든 반복을 즉시 시작/종료한다. 1초 기다리지 않는다.

✅ **해결 1:** .NET 6+ 에서는 `Parallel.ForEachAsync`를 쓴다.

```csharp
await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (i, ct) =>
{
    await Task.Delay(1000, ct);
});
```

✅ **해결 2:** Task 컬렉션 + `Task.WhenAll`.

```csharp
var tasks = Enumerable.Range(0, 10).Select(async i =>
{
    await Task.Delay(1000);
});
await Task.WhenAll(tasks);
```

## 6.5 함정 ⑤: 중첩 Task

```csharp
await Task.Factory.StartNew(async () =>
{
    await Task.Delay(1000);
});
```

기대: 1초 후 완료
실제: 거의 즉시 완료

`Task.Factory.StartNew`는 *람다의 반환값 타입*을 그대로 `Task<T>`에 담는다. 람다가 `async`이므로 반환값은 `Task`. 따라서 `StartNew`의 반환 타입은 `Task<Task>`다. 바깥 `await`는 *바깥 Task만* 기다리고 안쪽 Task는 무시한다.

✅ **해결 1:** `Task.Run`을 쓴다. `Task.Run`은 `Task<Task>` 케이스를 자동으로 *언래핑*한다.

```csharp
await Task.Run(async () =>
{
    await Task.Delay(1000);
});
```

✅ **해결 2:** 정말 `StartNew`가 필요하다면 `.Unwrap()`을 명시한다.

```csharp
await Task.Factory.StartNew(async () =>
{
    await Task.Delay(1000);
}).Unwrap();
```

기본적으로 `Task.Factory.StartNew`는 *옵션이 너무 많아 실수하기 쉬운 도구*다. 일상 코드에선 `Task.Run`을 쓴다.

## 6.6 함정 ⑥: .Wait() / .Result로 동기화

```csharp
public string Load() => LoadAsync().Result;   // ❌
```

데드락 위험은 2장에서 본 대로다. 추가로 *예외가 `AggregateException`으로 감싸진다*는 문제도 있다.

```csharp
try { LoadAsync().Result; }
catch (HttpRequestException) { /* 못 잡는다 */ }
catch (AggregateException ex) { /* ex.InnerException이 진짜 예외 */ }
```

✅ **해결:** 가능한 한 위까지 비동기로 전파한다. 정말 동기 컨텍스트에서 비동기를 호출해야 한다면 `.GetAwaiter().GetResult()`를 쓴다. 이건 `AggregateException` 래핑 없이 원본 예외를 다시 던진다.

```csharp
public string Load() => LoadAsync().GetAwaiter().GetResult();
```

데드락 위험은 여전하다. 가능한 한 안 쓰는 게 최선.

## 6.7 함정 ⑦: Fire-and-Forget

```csharp
public class HomeController : Controller
{
    async Task DoAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    public ActionResult Index()
    {
        DoAsync();                        // ❌ Task 무시
        return View();
    }
}
```

처음에는 동작한다. 그런데 일정 시간 후 `UnobservedTaskException` 또는 *컨텍스트 소멸 예외*가 발생한다. `DoAsync()`가 반환한 Task에 아무도 await/예외처리를 안 하니, GC 시점에 unobserved로 잡힌다.

또한 ASP.NET (구) 환경이라면 `View()`가 반환된 후 컨텍스트가 사라지고, 뒤늦게 깨어난 컨티뉴에이션이 갈 곳을 잃는다.

✅ **해결 1:** await로 끝까지 기다린다 (정상적인 경우).

```csharp
public async Task<ActionResult> Index()
{
    await DoAsync();
    return View();
}
```

✅ **해결 2:** 진짜로 fire-and-forget이 의도라면 *명시적 백그라운드 큐*에 넣는다. ASP.NET Core라면 `IHostedService` + `Channel`, 또는 `Microsoft.Extensions.Hosting`의 `BackgroundService`.

```csharp
public ActionResult Index()
{
    _backgroundQueue.Enqueue(DoAsync);     // 인큐만, 응답은 즉시
    return View();
}
```

⚠️ 응답 이후의 작업은 *응답 컨텍스트와 독립적인 곳*에서 돌려야 한다는 점이 핵심.

## 6.8 함정 ⑧: CancellationToken 누락

```csharp
public async Task<User> LoadAsync(int id)
{
    var conn = await _db.OpenConnectionAsync();           // ❌ 토큰 안 넘김
    var user = await conn.QueryAsync<User>(...);           // ❌
    return user;
}
```

요청이 취소돼도 (클라이언트가 끊거나 서버가 셧다운) 이 메서드는 끝까지 진행된다. 의미 없는 DB 작업이 계속 돌아간다.

✅ **해결:** 토큰을 *맨 아래까지* 전달.

```csharp
public async Task<User> LoadAsync(int id, CancellationToken ct)
{
    var conn = await _db.OpenConnectionAsync(ct);
    var user = await conn.QueryAsync<User>(..., ct);
    return user;
}
```

ASP.NET Core는 `HttpContext.RequestAborted`를 액션 메서드 매개변수로 자동 주입한다.

```csharp
[HttpGet("/user/{id}")]
public Task<User> Get(int id, CancellationToken ct) => _service.LoadAsync(id, ct);
```

## 6.9 함정 ⑨: 예외가 사라진다

```csharp
async Task<int> Inner() => throw new InvalidOperationException();

void Caller()
{
    var t = Inner();          // 예외가 Task에 담겨 있을 뿐, 던져지지 않는다
    Console.WriteLine("OK");  // 이게 찍힌다
}
```

`async` 메서드에서 던져진 예외는 *반환 Task의 Exception 속성*에 담긴다. `await`하지 않거나 `.Wait()`로 풀지 않으면 예외는 그냥 거기 머문다. 결국 GC 시점에 `TaskScheduler.UnobservedTaskException`으로 떠오른다.

✅ **해결:** 반드시 `await`한다. 정 fire-and-forget이라면 `try/catch`로 감싼 헬퍼 메서드를 통한다.

```csharp
public static class FireAndForget
{
    public static void Run(Func<Task> action, ILogger log)
        => _ = RunInner(action, log);

    private static async Task RunInner(Func<Task> action, ILogger log)
    {
        try { await action(); }
        catch (Exception ex) { log.LogError(ex, "fire-and-forget failed"); }
    }
}
```

## 6.10 함정 ⑩: await 안 한 Task가 GC됨

위 ⑨의 변형. 라이브러리 안에서 `Task`를 만들어 두고 *변수에도 안 담아 두면* GC가 일찍 수거하면서 컨티뉴에이션 체인이 깨지는 경우가 있다. 보통은 `Task`가 살아 있도록 내부적으로 root에 들고 있지만, 자작 awaiter나 외부 자원 연결 시 종종 일어난다.

✅ **해결:** 만든 Task는 반드시 반환하거나 변수로 보유. 자작 awaiter에서는 GC 가능 상태로 두면 안 됨.

## 6.11 함정 ⑪: ValueTask를 두 번 await

```csharp
public async ValueTask<int> ReadAsync() { ... }

// ❌
ValueTask<int> vt = ReadAsync();
int a = await vt;
int b = await vt;        // 미정의 동작! (예외 또는 잘못된 값)
```

`ValueTask`는 *완료 후 풀로 반환되는 경우가 있는 경량 구조체*다. 한 번 결과를 꺼내면 내부 상태가 재사용될 수 있다. 두 번째 `await`에서는 *다른 결과*가 나올 수도, 예외가 나올 수도 있다.

✅ **해결 1:** ValueTask는 *한 번만* await한다.

✅ **해결 2:** 결과를 보관하고 싶다면 `vt.AsTask()`로 변환 후 보관/await한다.

```csharp
ValueTask<int> vt = ReadAsync();
Task<int> t = vt.AsTask();
int a = await t;
int b = await t;      // 안전
```

## 6.12 함정 ⑫: Parallel.ForEach + async 람다

⑤번과 비슷하지만 변형. `Parallel.ForEach`(비동기 버전 아닌 클래식)는 `async` 람다를 *Action*으로만 받는다 → `async void` 함정.

```csharp
Parallel.ForEach(items, async item =>      // ❌ async void
{
    await ProcessAsync(item);
});
```

✅ **해결:** `Parallel.ForEachAsync` (.NET 6+).

```csharp
await Parallel.ForEachAsync(items,
    new ParallelOptions { MaxDegreeOfParallelism = 8 },
    async (item, ct) =>
    {
        await ProcessAsync(item, ct);
    });
```

또는 단순한 경우엔 `Task.WhenAll(items.Select(ProcessAsync))`.

## 6.13 정적 분석기 활용

위 함정의 대부분은 분석기로 잡을 수 있다.

```xml
<!-- Directory.Build.props 또는 csproj -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <AnalysisMode>All</AnalysisMode>
  <AnalysisLevel>latest</AnalysisLevel>
</PropertyGroup>
```

특히 다음 진단 ID들을 켜 두면 좋다.

| 진단 ID | 잡는 함정 |
|---------|-----------|
| CS1998 | `async` 메서드인데 `await`가 없음 |
| CS4014 | Task를 await 안 함 |
| CA2007 | `ConfigureAwait` 누락 (라이브러리용) |
| CA2008 | `Task.Factory.StartNew`에 스케줄러 미지정 |
| CA2012 | `ValueTask` 사용 위반 |
| VSTHRD110 | (Microsoft.VisualStudio.Threading) `Task` 결과 무시 |

## 6.14 체크리스트

- [ ] `async void`는 이벤트 핸들러에만.
- [ ] `Task.Run`이 기본, `Task.Factory.StartNew`는 특수 옵션이 필요한 경우만.
- [ ] `.Wait()` / `.Result`를 만나면 위쪽까지 비동기로 바꿀 수 있는지 먼저 검토.
- [ ] `CancellationToken`을 끝까지 전파.
- [ ] `ValueTask`는 한 번만 await.
- [ ] 분석기를 켜고 경고를 에러로 취급.

## 6.15 다음 챕터로 가기 전에

비동기 코드의 함정 중 절반은 *동기화* 문제다. 비동기 메서드 안에서 `lock`을 쓸 수 없다는 것은 6.x에서 잠깐 비쳤다. 그렇다면 비동기 환경에서는 어떻게 동기화를 해야 할까? 다음 장, **비동기 환경의 동기화 객체** 에서.
