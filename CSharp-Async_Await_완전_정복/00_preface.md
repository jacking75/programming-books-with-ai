# 머리말

## 왜 또 한 권의 async/await 책인가

C#에 `async/await`가 도입된 것은 2012년의 C# 5.0이다. 10년이 훨씬 지난 지금, 인터넷에는 `async/await` 자료가 넘쳐난다. 그런데도 현장에서 코드 리뷰를 하다 보면 다음과 같은 코드가 매주 한 번씩은 등장한다.

```csharp
// 1. async 메서드를 만들고 그 안에서 .Result로 결과를 꺼낸다
var data = httpClient.GetStringAsync(url).Result;

// 2. async void가 일반 메서드에 섞여 있다
public async void SaveAsync() { ... }

// 3. ConfigureAwait(false)를 "성능이 좋다고 들어서" 모든 곳에 붙인다
await foo.LoadAsync().ConfigureAwait(false);
await bar.SaveAsync().ConfigureAwait(false);
return View(model);

// 4. Task를 만들고 await도 wait도 안 한다
public Task<int> LoadAsync() => Task.Run(() => ...);
public void Init() => LoadAsync();  // fire-and-forget
```

전부 코드는 동작한다. 컴파일도 잘 된다. 단지 *언젠가* 데드락이 나거나, 예외가 사라지거나, 스레드 풀이 고갈되거나, 컨텍스트가 깨질 뿐이다. 그리고 그게 운영 서비스에서 터진다.

이 책은 그런 사고를 두 번 다시 만들지 않게 하려고 썼다.

## 이 책이 다루는 것

```
                    ┌──────────────────────────────┐
                    │   C# async / await 의 모든것  │
                    └──────────────┬───────────────┘
                                   │
        ┌──────────────────────────┼──────────────────────────┐
        │                          │                          │
   ┌────▼────┐               ┌─────▼─────┐              ┌────▼─────┐
   │ 동작 원리│               │  실전 패턴 │              │ .NET 10  │
   │  (1~4장)│               │  (5~8장)  │              │ 신기능    │
   └─────────┘               └───────────┘              │ (9~10장) │
                                                        └──────────┘
```

1장부터 4장까지는 *어떻게 동작하는가*를 다룬다. 컴파일러가 만들어 주는 상태 머신을 직접 만들어 보고, `SynchronizationContext`가 무엇인지, `ConfigureAwait(false)`가 실제로 무엇을 끄는지 본다.

5장부터 8장까지는 *현장에서 마주치는 패턴*을 다룬다. ASP.NET Core와 게임 서버에서 비동기를 어떻게 써야 하고, 어떤 함정을 피해야 하며, 비동기 환경에서 락을 어떻게 거는지 본다.

9장과 10장은 *.NET 10 시대의 도구*를 다룬다. `ValueTask`, `IAsyncEnumerable`, `CancellationToken`, `System.Threading.Channels`, `Parallel.ForEachAsync` — 그리고 이런 도구들을 조합해 만드는 실전 패턴을 본다.

## 이 책의 약속

```
┌─────────────────────────────────────────────────────────┐
│  모든 예제는 .NET 10 / C# 14에서 그대로 컴파일 / 실행된다. │
│  추측이 아니라 디스어셈블/상태 머신 단계까지 들여다본다.    │
│  "그냥 이렇게 쓰세요"가 아니라 "왜 그래야 하는지"를 보여준다.│
└─────────────────────────────────────────────────────────┘
```

각 챕터의 마지막에는 **체크리스트**와 **다음 챕터로 가기 전에 풀어야 할 질문**을 정리해 두었다. 책을 한 번 통독한 뒤 이 리스트만 다시 보아도 90%는 복습이 된다.

## 예제 코드 안내

이 책의 모든 예제는 `samples/` 폴더 아래의 단일 솔루션 (`AsyncAwaitBook.sln`)에 챕터별 콘솔 프로젝트로 들어 있다. 다음 명령으로 바로 실행할 수 있다.

```bash
cd samples
dotnet run --project Ch01_Basics
```

각 챕터 본문에 등장하는 코드 블록의 우측 상단에는 다음과 같은 표기가 있다.

> `Ch03_ConfigureAwait/Program.cs · ExampleA`

이는 해당 예제가 `samples/Ch03_ConfigureAwait/Program.cs` 안의 `ExampleA` 메서드에 들어 있다는 뜻이다. 책을 읽다가 직접 돌려보고 싶을 때 바로 찾을 수 있게 했다.

## 표기 규약

- **굵은 글씨**: 처음 등장하는 용어
- `고정폭 글씨`: 코드, 식별자, 키워드
- ⚠️ : 주의해야 할 함정
- ✅ : 권장 패턴
- 💡 : 알아두면 좋은 팁

자, 그럼 시작하자.
