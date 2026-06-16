# 예제 코드 솔루션

`AsyncAwaitLab.sln`은 본문 20개 장이 각각 자체 콘솔 프로젝트로 빌드되는 단일 .NET 10 솔루션이다.

## 빌드

```bash
# .NET 10 SDK가 설치돼 있어야 한다 (global.json 참고)
dotnet --version

# 한 번에 전체 빌드
dotnet build

# 특정 장 실행
dotnet run --project src/Chapter04
```

`Directory.Build.props`에 모든 장이 공유하는 컴파일 옵션이 들어 있다. 새 장이 추가되어도 동일한 LangVersion(`preview`)과 NullableContext가 적용된다.

## 폴더 구조

```
samples/
├─ AsyncAwaitLab.sln            솔루션 파일
├─ Directory.Build.props        공통 MSBuild 속성
├─ Directory.Packages.props     중앙 집중식 패키지 버전
├─ global.json                  SDK 핀
├─ .editorconfig                포매팅 규약
└─ src/
   ├─ Common/                   ConsoleHelpers, AsyncDiagnostics 등
   └─ Chapter01..20/            장별 콘솔 프로젝트
```

## 의존성

`Common`은 `Microsoft.Extensions.Logging` 패키지만 사용한다. 일부 장에서 `System.Threading.Channels`, `BenchmarkDotNet` 등을 추가로 참조한다. 모든 버전은 `Directory.Packages.props`에 모여 있다.

## 자주 묻는 문제

| 증상 | 해결 |
|------|------|
| `error NU1100`: BenchmarkDotNet 못 찾음 | `dotnet restore` 한 번 더, 또는 `--packages` 캐시 경로 확인 |
| `CS9057`: SDK가 너무 낮음 | `global.json`의 `version`을 자기 SDK에 맞춰 수정 |
| Linux에서 `Console.ReadLine()` 멈춤 | 비대화형 환경 — `ConsoleHelpers.Pause`가 자동 스킵한다 |
| .NET 10 SDK 미설치 | `Directory.Build.props`의 `<TargetFramework>net10.0</TargetFramework>`을 `net9.0`으로 임시 변경해 빌드 가능 |
| Chapter20에서 BenchmarkDotNet 빠짐 | csproj에 `<PackageReference Include="BenchmarkDotNet" />` 추가하고 `Chapter20/AsyncBench.cs`의 `BENCHMARKDOTNET` 심볼 정의 |
