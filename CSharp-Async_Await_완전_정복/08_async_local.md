# 8장. AsyncLocal과 실행 컨텍스트

## 8.1 비동기 호출 트리에서의 "지역 변수"

스레드별 변수에 해당하는 것은 `ThreadStatic` 필드다. 하지만 비동기 코드에서는 `await` 한 번이면 스레드가 바뀌니 `ThreadStatic`은 무용지물이다. 다음과 같은 시나리오가 필요하다.

```
요청이 들어옴 → 요청 ID 세팅 → 비동기 호출 트리 어디서나 그 ID가 보임
                                  (스레드가 바뀌어도)
```

이걸 해 주는 것이 `AsyncLocal<T>`다. .NET 4.6에서 추가된 후 ASP.NET Core가 내부적으로 광범위하게 쓰고 있다.

## 8.2 가장 단순한 예제

> `Ch08_AsyncLocal/Program.cs · BasicDemo`

```csharp
static async Task TestAsync()
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

        Console.WriteLine($"Step 8. {x.Value}");   // ← 핵심
    });

    Console.WriteLine($"Step 9. {x.Value}");        // ← 핵심
}
```

출력은 다음과 같다.

```
Step 0. 0
Step 1. 1
Step 2. 1     ← 부모의 값이 자식 task로 흘러감
Step 3. 2
Step 4. 2     ← 손자에게도 흘러감
Step 5. 3
Step 6. 3
Step 7. 4
Step 8. 2     ← 자식의 변경이 부모로 안 올라옴! (자동 복원)
Step 9. 1     ← 손자/자식 모두의 변경이 안 올라옴
```

핵심 규칙은 이렇다.

> **AsyncLocal은 호출 트리 아래로는 *카피온라이트(copy-on-write)* 로 전파되고, 위로는 변경이 보이지 않는다.**

부모는 자식의 변경을 못 본다. 자식에서 값을 바꾸면 그 자식의 호출 트리(+ 그 안의 await로 연결된 컨티뉴에이션)까지만 보인다.

## 8.3 ExecutionContext와의 관계

`AsyncLocal`은 *ExecutionContext*라는 더 큰 그릇 안에 저장된다. `Task`가 만들어질 때, `Thread.Start`가 호출될 때, `await`가 컨티뉴에이션을 등록할 때 — 모두 *현재 ExecutionContext의 스냅샷*을 캡처해 같이 넘긴다.

```
┌─────────────────────────────────────────────────────────┐
│  ExecutionContext                                        │
│  ┌─────────────────────────────────────────────────┐    │
│  │ AsyncLocalValueMap                              │    │
│  │  - "RequestId" → "abc-123"                      │    │
│  │  - "UserId"    → 42                             │    │
│  │  - ...                                          │    │
│  └─────────────────────────────────────────────────┘    │
│  + SecurityContext, CallContext, ...                     │
└─────────────────────────────────────────────────────────┘
        │
        ▼  await에서 캡처
    스냅샷 ── 컨티뉴에이션에 부착 ── 새 스레드에서 복원
```

수정이 *카피온라이트*로 되는 이유가 여기 있다. 새 값을 쓰면 새 `AsyncLocalValueMap`을 만들어 현재 ExecutionContext를 교체한다. 그 결과 부모가 보유한 *이전 맵*은 변하지 않는다.

## 8.4 콜백으로 변경 감지

`AsyncLocal<T>`은 *값이 바뀔 때 콜백*을 받을 수 있다.

```csharp
var x = new AsyncLocal<int>(args =>
{
    Console.WriteLine(
        $"prev={args.PreviousValue} curr={args.CurrentValue} " +
        $"ctxChanged={args.ThreadContextChanged}");
});
```

`ThreadContextChanged`가 `true`면 *스레드가 ExecutionContext를 갈아끼우면서* 값이 바뀐 것이고, `false`면 *명시적으로 .Value = x* 한 것이다.

이 콜백은 *모든 컨텍스트 교체에서 호출*되기 때문에 무거운 로직을 넣으면 성능에 치명적이다. 디버깅용 / 로깅용에 한정.

## 8.5 실전: 요청 단위 컨텍스트

라이브러리에서 자주 쓰는 패턴.

> `Ch08_AsyncLocal/RequestContext.cs`

```csharp
public static class RequestContext
{
    private static readonly AsyncLocal<RequestInfo?> _current = new();

    public static RequestInfo? Current => _current.Value;

    public static IDisposable Begin(string requestId, string userId)
    {
        var previous = _current.Value;
        _current.Value = new RequestInfo(requestId, userId);
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly RequestInfo? _previous;
        public Scope(RequestInfo? previous) => _previous = previous;
        public void Dispose() => _current.Value = _previous;
    }
}

public sealed record RequestInfo(string RequestId, string UserId);

// 사용
public async Task HandleAsync(HttpContext ctx)
{
    using (RequestContext.Begin(ctx.TraceIdentifier, ctx.User.GetId()))
    {
        await DoStuffAsync();   // 이 안에서는 어디서든 RequestContext.Current 사용 가능
    }   // 자동 복원
}
```

⚠️ **함정:** `_current.Value = ...` 후 `using` 블록의 *외부에서* 그 값을 또 쓰려고 하면 빈다. 또한 *상위 호출자가 Begin한 값을 우리가 덮어쓰면*, 우리가 빠져나갈 때 복원하지 않으면 상위 호출자가 망가진다. 위 패턴은 `Scope`로 자동 복원해 그 함정을 막는다.

## 8.6 ASP.NET Core의 IHttpContextAccessor

`HttpContextAccessor`의 내부 구현이 정확히 위 패턴이다. ASP.NET Core가 `SynchronizationContext`를 버리고 대신 *AsyncLocal* 기반의 명시적 컨텍스트로 옮겼다는 게 이 의미다.

```csharp
public class HttpContextAccessor : IHttpContextAccessor
{
    private static readonly AsyncLocal<HttpContextHolder?> _holder = new();
    // ... (단순화)

    public HttpContext? HttpContext
    {
        get => _holder.Value?.Context;
        set
        {
            var holder = _holder.Value;
            if (holder is not null) holder.Context = null;
            if (value is not null)
                _holder.Value = new HttpContextHolder { Context = value };
        }
    }
}
```

DI로 `IHttpContextAccessor`를 주입받으면, 어떤 비동기 깊이에서든 그 요청의 `HttpContext`에 접근할 수 있다.

## 8.7 분산 추적과 Activity

.NET 6+에서 도입된 `System.Diagnostics.Activity`도 내부적으로 `AsyncLocal<Activity?>`를 쓴다. `Activity.Current`가 비동기 호출 트리를 따라 흐른다.

```csharp
using var activity = Source.StartActivity("LoadPlayer");
activity?.SetTag("playerId", id);

await LoadFromDbAsync(id);   // 자식 Activity는 자동으로 부모 Activity를 참조
```

OpenTelemetry, Application Insights 등이 모두 이 기반 위에 트레이싱을 구현했다.

## 8.8 ExecutionContext 흐름 제어

성능에 민감한 경로에서는 ExecutionContext 캡처 비용이 부담이 될 수 있다. 캡처를 끄려면 `ExecutionContext.SuppressFlow()`.

```csharp
using (ExecutionContext.SuppressFlow())
{
    _ = Task.Run(() => { /* AsyncLocal 값들이 안 흘러옴 */ });
}
```

라이브러리 내부에서 *순수 백그라운드 작업*을 띄울 때 가끔 쓴다. 일반 앱 코드에서는 거의 쓸 일 없다.

## 8.9 흔한 함정

```
함정 ① AsyncLocal을 set하면 즉시 부모도 바뀐다 (오해)
       → 부모에게는 안 보인다. 카피온라이트.

함정 ② 라이브러리에서 AsyncLocal에 직접 .Value = X 하고 끝
       → 호출자에게 값이 누수돼서 영영 남는다. Scope 패턴 권장.

함정 ③ Thread.ManagedThreadId로 추적 → 비동기에서는 안 됨
       → Activity.Current 또는 직접 AsyncLocal로 RequestId 추적.

함정 ④ AsyncLocal 값으로 mutable 객체를 두고 트리 어디서나 수정
       → 객체 자체는 한 개라 변경은 모든 노드에서 보인다.
         (AsyncLocal이 보호하는 건 "참조"지 "내용물"이 아니다)
```

함정 ④ 예시:

```csharp
var bag = new AsyncLocal<List<string>>();
bag.Value = new List<string>();
bag.Value.Add("hello");                 // 부모도 본다 (같은 객체)
await Task.Run(() => bag.Value!.Add("world"));   // 부모도 본다
// bag.Value 에는 [hello, world] 가 모두 보인다
```

요청 단위 컨테이너로 가변 객체를 넣을 때는 *수정 대신 교체*를 한다.

## 8.10 체크리스트

- [ ] `AsyncLocal<T>`은 호출 트리 아래로 흐른다. 자식의 수정은 부모로 안 올라간다.
- [ ] 내부는 ExecutionContext + 카피온라이트.
- [ ] 라이브러리에서 쓸 때는 `Begin/Scope` 패턴으로 자동 복원.
- [ ] mutable 객체를 담으면 보호되는 건 참조뿐, 내용물은 공유된다.
- [ ] `Activity`와 `IHttpContextAccessor`가 동작하는 기반이다.

## 8.11 다음 챕터로 가기 전에

여기까지가 *전통적 async/await*의 모든 것이다. 이제부터는 .NET이 더해 준 *현대적 비동기 도구*를 본다. `ValueTask`, `IAsyncEnumerable`, `Channels`, `Parallel.ForEachAsync` — 다음 장에서.
