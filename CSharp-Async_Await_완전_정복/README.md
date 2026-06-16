# C# Async/Await 완전 정복 (.NET 10 기준)

저자: 최흥배, AI-Assisted  
  
  
## 책 소개

이 책은 C#의 `async/await`를 *문법*이 아니라 *동작 원리*부터 설명한다. 컴파일러가 만들어 주는 상태 머신을 직접 들여다보고, `SynchronizationContext`, `ConfigureAwait`, `ValueTask`, `IAsyncEnumerable`, `CancellationToken`, `Channels` 같은 .NET 10 시대의 비동기 도구들을 한 권에 정리했다.

대상 독자는 다음과 같다.

- C#을 1년 이상 쓰면서 `async/await`를 "그냥 붙이면 되는 것"으로 알고 있는 개발자
- ASP.NET Core / 게임 서버 등 동시성이 중요한 백엔드를 만드는 개발자
- 디버깅 중 `Deadlock`, `UnobservedTaskException`, "왜 스레드 ID가 바뀌었지?" 같은 미스터리를 만난 경험이 있는 개발자

## 챕터 구성

| 챕터 | 제목 | 핵심 내용 |
|------|------|-----------|
| 00 | [머리말](00_preface.md) | 이 책을 읽기 전에 |
| 01 | [동기와 비동기, 그리고 Task](01_basics.md) | 스레드 vs 태스크, async/await 기초 |
| 02 | [SynchronizationContext의 비밀](02_synchronization_context.md) | 콘솔/GUI/ASP.NET별 동작 차이 |
| 03 | [ConfigureAwait 깊이 파기](03_configure_await.md) | false의 의미, .NET 8+ 옵션 플래그 |
| 04 | [Awaitable 패턴 직접 구현](04_awaitable_pattern.md) | GetAwaiter, INotifyCompletion, 상태 머신 |
| 05 | [ASP.NET / 서버에서 async/await](05_server_async.md) | 스레드 고갈, IOCP, 게임 서버 사례 |
| 06 | [비동기의 함정 12가지](06_pitfalls.md) | async void, Wait(), 데드락, 결과 무시 |
| 07 | [비동기 환경의 동기화 객체](07_async_lock.md) | SemaphoreSlim, AsyncLock, Channel |
| 08 | [AsyncLocal과 실행 컨텍스트](08_async_local.md) | 비동기 호출 트리에서의 값 전파 |
| 09 | [.NET 10 비동기 신무기](09_modern_features.md) | ValueTask, IAsyncEnumerable, CancellationToken, Channels, Parallel.ForEachAsync |
| 10 | [실전 패턴과 응용](10_practical_patterns.md) | 프레임 단위 처리, 백프레셔, 그래스풀 셧다운 |
| 부록 A | [용어집](A_glossary.md) | |
| 부록 B | [디버깅 치트시트](B_debugging.md) | |

## 예제 코드 프로젝트

`samples/AsyncAwaitBook.sln`을 열면 챕터별 콘솔 프로젝트가 들어 있다. 모두 **.NET 10 / C# 14** 기준이며 `nullable` 활성화, `ImplicitUsings` on 상태다.

```
samples/
├── AsyncAwaitBook.sln
├── Directory.Build.props          # 공통 컴파일 설정
├── Ch01_Basics/
├── Ch02_SynchronizationContext/
├── Ch03_ConfigureAwait/
├── Ch04_AwaitablePattern/
├── Ch05_ServerAsync/
├── Ch06_Pitfalls/
├── Ch07_AsyncLock/
├── Ch08_AsyncLocal/
├── Ch09_ModernFeatures/
└── Ch10_Practical/
```

### 실행 방법

```bash
cd samples
dotnet build
dotnet run --project Ch01_Basics
```

## 라이선스

본 원고와 예제 코드는 학습/사내 교육 자료로 자유롭게 활용해도 좋다.
