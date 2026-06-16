# 부록 A. 용어집

| 용어 | 영문 | 설명 |
|------|------|------|
| 비동기 | Asynchronous | 결과를 기다리지 않고 진행하는 실행 방식. 결과는 나중에 콜백/await로 받는다. |
| 동기 | Synchronous | 결과가 나올 때까지 현재 스레드가 점유되는 방식. |
| 병렬 | Parallel | 여러 작업이 *물리적으로 동시*에 진행되는 것. 스레드/코어 사용. |
| 동시성 | Concurrency | 여러 작업이 *논리적으로 동시*에 진행되는 것. 시분할로도 구현 가능. |
| 태스크 | Task | C#에서 비동기 작업의 미래 결과를 표현하는 객체 (Promise). |
| Awaitable | Awaitable | `await` 가능한 모든 것. `GetAwaiter` 메서드를 노출하는 패턴. |
| Awaiter | Awaiter | `GetAwaiter()`가 반환하는 객체. `IsCompleted`, `GetResult`, `OnCompleted` 보유. |
| 상태 머신 | State machine | `async` 메서드가 컴파일러에 의해 변환된 형태. `MoveNext`로 진입 상태가 바뀌어 가며 코드 조각을 실행. |
| 컨티뉴에이션 | Continuation | `await` 이후에 실행될 코드 조각. 상태 머신의 다음 state. |
| SynchronizationContext | — | 컨티뉴에이션을 어디서 실행할지 결정하는 추상화. GUI 디스패처, ASP.NET 요청 컨텍스트 등. |
| ExecutionContext | — | `AsyncLocal`, 보안 컨텍스트 등을 담는 그릇. 비동기 호출 트리를 따라 흐른다. |
| ConfigureAwait | — | `await`가 캡처한 컨텍스트로 복귀할지 여부 등을 설정하는 옵션. |
| 스레드 풀 | ThreadPool | .NET이 관리하는 worker 스레드의 집합. `Task.Run` 등이 사용. |
| IOCP | I/O Completion Port | Windows의 비동기 I/O 완료 알림 메커니즘. .NET 스레드풀이 활용. |
| Task.Run | — | 동기 작업을 ThreadPool에 던지는 일반적인 방법. CPU 바운드용. |
| 데드락 | Deadlock | 두 코드가 서로의 자원을 기다리며 영원히 멈추는 상황. `await`+`.Result` 조합에서 잘 일어남. |
| 스레드 고갈 | Thread starvation | ThreadPool의 모든 스레드가 블로킹되어 새 작업을 처리 못 하는 상황. |
| 백프레셔 | Backpressure | 생산자가 소비자보다 빨라 폭주하지 않도록 흐름을 제어하는 메커니즘. |
| ValueTask | — | `Task`의 struct 버전. 동기 완료 경로의 할당을 줄이기 위한 타입. |
| IAsyncEnumerable | — | 비동기 스트림. `await foreach`로 소비. |
| CancellationToken | — | 협조적 취소 신호. 모든 비동기 메서드 시그니처에 흘러야 함. |
| Channel<T> | — | 비동기 친화적 큐. Producer/Consumer 패턴의 표준. |
| AsyncLocal<T> | — | 비동기 호출 트리에서 카피온라이트로 흐르는 값. `ThreadStatic`의 비동기 버전. |
| SemaphoreSlim | — | 비동기 대기를 지원하는 세마포어. `SemaphoreSlim(1,1)`로 비동기 mutex. |
| Fire-and-forget | — | 반환 Task를 무시하고 호출하는 패턴. 예외/완료 추적 불가능 → 위험. |
| sync-over-async | — | 비동기 메서드를 동기로 강제 사용하는 안티패턴 (`.Result`, `.Wait()`). |
| async void | — | Task를 반환하지 않는 async 메서드. 이벤트 핸들러 외엔 금지. |
| Task.WhenAll | — | 여러 Task가 *모두* 완료될 때까지 대기. |
| Task.WhenAny | — | 여러 Task 중 *하나라도* 완료되면 반환. |
