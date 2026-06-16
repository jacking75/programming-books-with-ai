# 부록 B. 비동기 디버깅 치트시트

## B.1 증상별 진단표

```
┌─────────────────────────────────┬───────────────────────────────────┐
│ 증상                            │ 의심 지점                          │
├─────────────────────────────────┼───────────────────────────────────┤
│ 데드락 (UI 멈춤 / 서버 응답 없음) │ .Result / .Wait() in 컨텍스트 코드 │
│ 예외가 catch에 안 잡힘           │ async void / await 누락            │
│ 1초 기다린다 했는데 즉시 끝남     │ Task.Delay 결과 무시 / async void  │
│ 스레드 ID가 매번 바뀐다           │ SynchronizationContext null       │
│ 스레드 ID가 안 바뀐다 (이상함)    │ GUI 컨텍스트로 자동 복귀           │
│ UnobservedTaskException          │ fire-and-forget Task 가 GC됨      │
│ HttpClient: socket exhausted     │ HttpClient를 매번 new로 만듦       │
│ ValueTask 결과가 이상함           │ ValueTask 두 번 await             │
│ 백프레셔 없이 메모리 폭증         │ Unbounded Channel / async void 폭주│
│ 캐시되었는데 매번 호출됨           │ Task.FromResult 미사용, Lazy 패턴│
└─────────────────────────────────┴───────────────────────────────────┘
```

## B.2 데드락이 의심될 때

```
[ 진단 순서 ]
1. 스레드 덤프를 찍는다 (dotnet-dump collect, !threads)
2. 모든 워커 스레드가 .Result / Monitor.Wait 에서 멈춰 있나 확인
3. .Result/.Wait() 호출 위치를 찾는다
4. 그 위 메서드의 모든 await를 ConfigureAwait(false) 검토하거나
   호출자를 async로 바꿔 비동기 전파
```

빠른 임시 해결: `.Result` → `.GetAwaiter().GetResult()` (예외 래핑은 풀리지만 데드락은 그대로다. 진짜 해결은 호출 트리 전체 async).

## B.3 자주 쓰는 Visual Studio / dotnet 도구

| 도구 | 용도 |
|------|------|
| Tasks 창 (VS) | 실행 중인 Task 목록과 상태 |
| Parallel Stacks (VS) | 비동기 호출 스택 (async stack) |
| dotnet-counters | ThreadPool 큐 길이, Task 수 실시간 모니터링 |
| dotnet-dump | 라이브 / 크래시 덤프 |
| dotnet-trace | 이벤트 트레이싱 (await 컨티뉴에이션 등) |
| PerfView | 깊은 분석. Async-Causality 뷰 강력 |

### dotnet-counters 예시

```bash
dotnet-counters monitor --process-id <PID> System.Runtime
```

`threadpool-thread-count`, `threadpool-queue-length`, `total-tasks-scheduled`가 핵심 지표.

## B.4 분석기로 미리 막기

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <AnalysisMode>All</AnalysisMode>
  <AnalysisLevel>latest</AnalysisLevel>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

추가로 Microsoft.VisualStudio.Threading.Analyzers 권장.

```xml
<PackageReference Include="Microsoft.VisualStudio.Threading.Analyzers" Version="17.*" />
```

다음 진단을 잡아 준다.

| ID | 내용 |
|----|------|
| VSTHRD003 | async/await로 풀어야 할 동기 호출 |
| VSTHRD100 | async void |
| VSTHRD110 | Task 결과 무시 |
| VSTHRD200 | 비동기 메서드 이름에 Async 접미사 |

## B.5 ThreadPool 튜닝 (서버)

```csharp
// 시작 시 한 번만 호출
ThreadPool.SetMinThreads(Environment.ProcessorCount * 2, Environment.ProcessorCount * 2);
```

기본값(코어당 1)이 너무 작아 *부하 급증 직후* 스레드 부족이 자주 일어난다. .NET이 알아서 늘려 주지만 *1초당 약 한두 개씩만* 늘린다. 워밍업 부하가 큰 게임 서버에서는 `SetMinThreads`로 미리 충분히 띄워 둔다.

⚠️ 일반 ASP.NET Core 웹서버는 굳이 안 건드려도 된다. 게임 서버처럼 일시적 폭주가 잦은 경우에 한정.

## B.6 await 추적용 로깅 매크로

```csharp
public static class AwaitLog
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Mark(string label,
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] " +
            $"thread={Environment.CurrentManagedThreadId,3} " +
            $"ctx={SynchronizationContext.Current?.GetType().Name ?? "null"} " +
            $"{member}:{line} {label}");
    }
}

// 사용
AwaitLog.Mark("before await");
await SomethingAsync();
AwaitLog.Mark("after await");
```

스레드 ID와 컨텍스트가 시간 순서로 찍히므로, *어디서 컨텍스트가 바뀌는지* 한눈에 보인다.

## B.7 마지막 한 마디

비동기 디버깅의 90%는 **"어디서 컨텍스트가 바뀌었는가"** 와 **"어디서 await가 빠졌는가"** 의 추적이다. 위 도구들로 첫째 질문에 답하고, 분석기로 둘째 질문을 미리 막으면 대부분의 사고는 일어나지 않는다.
