# 제2장: 개발 환경 구축
DuckDB를 배우는 가장 빠른 길은 직접 설치하고 실행해 보는 것이다. 이 장에서는 Windows 11 환경에서 DuckDB CLI를 설치하고, C# (.NET 9) 프로젝트를 생성하여 DuckDB.NET NuGet 패키지를 연동하는 과정을 단계별로 따라간다. 설치 과정이 끝나면 간단한 SQL을 직접 실행해 보고, VS Code 개발 환경까지 깔끔하게 세팅한다. 마지막에는 자주 쓰는 CLI 명령어를 한 장짜리 치트시트로 정리해 두었으니 북마크해 두면 유용하다.

---

## 2.1 Windows 11에 DuckDB CLI 설치
DuckDB는 설치 과정이 매우 단순하다. 단일 실행 파일(바이너리) 하나로 동작하기 때문에, 별도의 서비스나 데몬을 설치할 필요가 없다. Windows 11에서 DuckDB CLI를 설치하는 방법은 크게 두 가지다. **winget(Windows Package Manager)**을 이용하는 방법과 공식 사이트에서 직접 다운로드하는 방법이다.

### 방법 1: winget으로 설치 (권장)
winget은 Windows 11에 기본 포함된 패키지 관리자다. 명령 한 줄로 DuckDB를 설치하고 이후 업데이트도 간편하게 할 수 있다.

**1단계**: Windows 터미널 또는 PowerShell을 **관리자 권한**으로 실행한다.
- 작업 표시줄의 검색창에 `Windows Terminal`을 입력하고 "관리자 권한으로 실행"을 선택한다.

**2단계**: 다음 명령을 입력하여 DuckDB를 설치한다.

```powershell
winget install DuckDB.DuckDB
```

**3단계**: 설치가 완료되면 다음과 같은 출력이 나타난다.

```
이미 찾았습니다. DuckDB [DuckDB.DuckDB] 버전 1.2.2
이 응용 프로그램의 사용 조건은 그 소유자가 귀하에게 허가합니다.
Microsoft는 타사 패키지에 대한 책임을 지지 않습니다.
다운로드 중 https://github.com/duckdb/duckdb/releases/...
  ██████████████████████████████  10.2 MB / 10.2 MB
설치 관리자 해시를 확인했습니다.
패키지를 설치하는 중...
설치 완료
```

**4단계**: 설치가 완료되면 터미널을 닫고 새로 열어서 환경 변수가 적용되도록 한다. 그런 다음 버전을 확인한다.

```powershell
duckdb --version
```

```
v1.2.2 1a89980c24
```

> **참고**: 이 책에서는 DuckDB 1.5.1 기준으로 설명하지만, winget이 제공하는 최신 안정 버전을 설치해도 이 책의 예제 대부분이 동일하게 동작한다. 특정 버전을 지정하려면 아래 방법 2를 사용한다.

### 방법 2: 공식 사이트에서 직접 다운로드
winget이 동작하지 않거나, 특정 버전(1.5.1)을 정확히 설치해야 할 경우에는 공식 사이트에서 직접 다운로드한다.

**1단계**: 브라우저에서 DuckDB 공식 설치 페이지로 이동한다.

```
https://duckdb.org/docs/installation/
```

**2단계**: 다음 조건에 맞는 패키지를 선택한다.
- **Environment**: Command Line
- **Platform**: Windows
- **Download method**: Direct
- **Architecture**: AMD64(x86_64) 또는 ARM64 (자신의 PC에 맞게 선택)

**3단계**: 다운로드된 ZIP 파일(`duckdb_cli-windows-amd64.zip` 등)의 압축을 해제하면 `duckdb.exe` 파일 하나가 나온다.

**4단계**: `duckdb.exe`를 PATH가 등록된 디렉토리로 이동시킨다. 가장 쉬운 방법은 `C:\Windows\System32` 폴더에 복사하는 것이지만, 권장 방식은 별도 디렉토리를 만들어 환경 변수에 추가하는 것이다.

```powershell
# 디렉토리 생성
mkdir C:\Tools\DuckDB

# duckdb.exe 이동 (다운로드 폴더 기준)
Move-Item "$env:USERPROFILE\Downloads\duckdb.exe" "C:\Tools\DuckDB\duckdb.exe"
```

**5단계**: 환경 변수에 `C:\Tools\DuckDB`를 추가한다.
- `Windows 키 + R`을 누르고 `sysdm.cpl`을 입력한 후 확인을 누른다.
- "고급" 탭 → "환경 변수" 버튼을 클릭한다.
- "사용자 변수"의 `Path` 항목을 선택하고 "편집"을 클릭한다.
- "새로 만들기"를 클릭하고 `C:\Tools\DuckDB`를 입력한 후 확인을 누른다.

**6단계**: 새 터미널을 열고 버전을 확인한다.

```powershell
duckdb --version
```

```
v1.5.1 ...
```

---

## 2.2 .NET 9 프로젝트 생성 및 DuckDB.NET NuGet 패키지 설치
DuckDB는 C#에서 **DuckDB.NET** 라이브러리를 통해 사용할 수 있다. DuckDB.NET은 두 개의 NuGet 패키지로 구성된다.

| 패키지 | 역할 |
|--------|------|
| `DuckDB.NET.Bindings` | DuckDB C API를 C#에서 직접 호출할 수 있는 저수준(low-level) 바인딩 |
| `DuckDB.NET.Data` | ADO.NET 표준 인터페이스(`DbConnection`, `DbCommand` 등)를 구현한 고수준(high-level) 래퍼 |

일반적인 개발에서는 `DuckDB.NET.Data`를 주로 사용하며, 이 패키지가 `DuckDB.NET.Bindings`에 의존하므로 둘 다 설치해야 한다.

### .NET 9 설치 확인
.NET 9가 설치되어 있는지 먼저 확인한다.

```powershell
dotnet --version
```

```
9.0.200
```

9.x 버전이 출력되지 않는다면 [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0) 에서 .NET 9 SDK를 다운로드하여 설치한다.

### 프로젝트 생성

**1단계**: 프로젝트를 만들 디렉토리로 이동한다. 이 책에서는 `code/ch02/` 디렉토리를 기준으로 예제를 관리한다.

```powershell
# 책 프로젝트의 code/ch02 디렉토리로 이동 (경로는 자신의 환경에 맞게 수정)
mkdir code\ch02\ex01-hello
cd code\ch02\ex01-hello
```

**2단계**: `dotnet new console` 명령으로 새 콘솔 애플리케이션을 생성한다.

```powershell
dotnet new console --name DuckDBHello
cd DuckDBHello
```

```
템플릿 "콘솔 앱"이 성공적으로 만들어졌습니다.

처리 중: 만들기 후 작업 실행 중...
경로: C:\...\DuckDBHello\DuckDBHello.csproj에 있는 프로젝트 복원:
  복원 완료 (0.2s)
```

**3단계**: 프로젝트 디렉토리로 이동하면 아래와 같은 구조가 생성되어 있다.

```
DuckDBHello/
├── DuckDBHello.csproj
├── Program.cs
└── obj/
```

### DuckDB.NET NuGet 패키지 설치

**4단계**: `DuckDB.NET.Bindings`와 `DuckDB.NET.Data` 패키지를 설치한다.

```powershell
dotnet add package DuckDB.NET.Bindings
dotnet add package DuckDB.NET.Data
```

각 명령을 실행하면 다음과 같이 패키지가 추가된다.

```
  패키지 'DuckDB.NET.Bindings'가 프로젝트 'DuckDBHello'에 추가되었습니다.
  패키지 'DuckDB.NET.Data'가 프로젝트 'DuckDBHello'에 추가되었습니다.
```

**5단계**: `DuckDBHello.csproj` 파일을 열어 패키지 참조가 올바르게 추가되었는지 확인한다.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DuckDB.NET.Bindings" Version="1.x.x" />
    <PackageReference Include="DuckDB.NET.Data" Version="1.x.x" />
  </ItemGroup>

</Project>
```

이제 C# 프로젝트에서 DuckDB를 사용할 준비가 완료되었다.

### 간단한 연결 테스트
`Program.cs`에 아래 코드를 입력하여 DuckDB 연결이 정상적으로 동작하는지 확인한다. 이 예제의 전체 코드는 `code/ch02/ex01-hello/DuckDBHello/Program.cs`에 위치한다.

```csharp
using DuckDB.NET.Data;

// 인메모리 DuckDB 데이터베이스에 연결
using var connection = new DuckDBConnection("Data Source=:memory:");
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = "SELECT 'Hello, DuckDB!' AS message, 42 AS answer;";

using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"Message: {reader.GetString(0)}");
    Console.WriteLine($"Answer:  {reader.GetInt32(1)}");
}
```

```powershell
dotnet run
```

```
Message: Hello, DuckDB!
Answer:  42
```

출력이 위와 같다면 DuckDB.NET이 성공적으로 설치되어 동작하고 있는 것이다.

---

## 2.3 DuckDB CLI 첫 실행 — Hello, DuckDB!
설치가 완료되었다면 DuckDB CLI를 직접 실행해 보자. CLI는 DuckDB의 핵심 기능을 터미널에서 바로 실행할 수 있는 강력한 도구다.

### CLI 인터랙티브 셸 실행

터미널에서 `duckdb`를 입력하면 **인터랙티브 셸(interactive shell)**이 시작된다.

```powershell
duckdb
```

```
v1.5.1 ...
Enter ".help" for usage hints.
Connected to a transient in-memory database.
Use ".open FILENAME" to reopen on a persistent database.
D
```

`D`가 표시되면 DuckDB 프롬프트가 활성화된 상태다. 이제 SQL을 직접 입력할 수 있다.

### 첫 번째 SQL 실행
프롬프트에서 다음 SQL을 입력한다. 세미콜론(`;`)으로 문장을 끝내야 실행된다.

```sql
SELECT 'Hello, DuckDB!' AS message;
```

```
┌────────────────┐
│    message     │
│    varchar     │
├────────────────┤
│ Hello, DuckDB! │
└────────────────┘
```

DuckDB CLI는 결과를 보기 좋은 테이블 형식으로 출력한다. 컬럼명과 데이터 타입, 그리고 구분선까지 자동으로 그려주기 때문에 결과를 한눈에 파악할 수 있다.

### 버전 정보 확인

```sql
SELECT version();
```

```
┌───────────────────┐
│     version()     │
│      varchar      │
├───────────────────┤
│ v1.5.1 ...        │
└───────────────────┘
```

### 간단한 수식 계산
DuckDB CLI를 계산기처럼 사용할 수도 있다.

```sql
SELECT 2 + 2 AS result;
```

```
┌────────┐
│ result │
│ int32  │
├────────┤
│      4 │
└────────┘
```

### 파일 기반 데이터베이스 열기
인메모리가 아닌, 파일에 데이터를 저장하려면 CLI 실행 시 파일 경로를 인수로 넘기거나 `.open` 명령을 사용한다.

```powershell
# 처음부터 파일을 지정하여 실행
duckdb my_database.db
```

```
v1.5.1 ...
Enter ".help" for usage hints.
Connected to a persistent database at path: my_database.db
D
```

이렇게 실행하면 `my_database.db` 파일이 생성되며, 이후 작업한 내용이 파일에 영구적으로 저장된다.

### CLI 종료
셸을 종료하려면 `.quit` 또는 `Ctrl + D`를 입력한다.

```
D .quit
```

---

## 2.4 VS Code + C# DevKit 환경 설정
DuckDB와 C#으로 개발할 때 가장 많이 사용하는 에디터 중 하나는 **Visual Studio Code(VS Code)**다. 가볍고 빠르며, 강력한 확장 기능 생태계를 갖추고 있다. 여기서는 C# 개발에 최적화된 환경을 구성하는 방법을 안내한다.

### VS Code 설치

**1단계**: VS Code 공식 사이트에서 설치 파일을 다운로드한다.

```
https://code.visualstudio.com/
```

**2단계**: 다운로드한 설치 파일(`VSCodeSetup-x64-x.xx.x.exe`)을 실행하고 설치 마법사의 안내에 따라 설치를 완료한다. 설치 중 **"PATH에 추가"** 옵션이 체크되어 있는지 확인한다. 이 옵션이 활성화되어야 터미널에서 `code .` 명령으로 VS Code를 바로 열 수 있다.

### 필수 확장 기능 설치
VS Code를 열고 확장 탭(`Ctrl + Shift + X`)에서 다음 확장 기능들을 설치한다.

**1단계**: **C# DevKit** 설치
- 검색창에 `C# DevKit`을 입력한다.
- Microsoft에서 만든 **C# DevKit** 확장을 설치한다.
- C# DevKit은 기본 C# 언어 지원, IntelliSense, 디버깅, 테스트 기능을 모두 포함한다.
- 설치 시 **C#** 확장도 자동으로 함께 설치된다.

**2단계**: **.NET Install Tool** 설치 (선택)
- C# DevKit이 자동으로 설치를 권장하는 경우가 많다.
- .NET SDK 버전 관리를 VS Code에서 쉽게 할 수 있게 해주는 도구다.

### 프로젝트 열기

**3단계**: 앞서 생성한 프로젝트를 VS Code에서 연다.

```powershell
# 프로젝트 폴더로 이동 후 VS Code 실행
cd code\ch02\ex01-hello\DuckDBHello
code .
```

VS Code가 열리면서 프로젝트 파일을 인식한다. 처음 C# 프로젝트를 열면 우측 하단에 **"필요한 에셋을 추가하시겠습니까?"** 팝업이 나타날 수 있다. **"예"**를 클릭하면 `.vscode/launch.json`과 `.vscode/tasks.json`이 자동으로 생성되어 디버깅 환경이 구성된다.

### IntelliSense 동작 확인

**4단계**: `Program.cs` 파일을 열고 `DuckDB`를 입력하기 시작하면 자동 완성이 동작하는 것을 확인한다. `DuckDBConnection`을 입력할 때 클래스 목록이 나타나면 IntelliSense가 정상적으로 동작하고 있는 것이다.

### 빌드 및 실행

**5단계**: VS Code 터미널(`Ctrl + ~`)을 열고 다음 명령을 실행한다.

```powershell
dotnet build
```

```
빌드했습니다.
    0개 경고
    0개 오류

빌드 시간(초): 1.8초
```

```powershell
dotnet run
```

```
Message: Hello, DuckDB!
Answer:  42
```

### 디버그 실행

**6단계**: `F5`를 누르면 디버그 모드로 실행된다. 코드의 원하는 줄 왼쪽 여백을 클릭하여 **중단점(Breakpoint)**을 설정하면, 그 줄에서 실행이 일시 중단되고 변수 상태를 검사할 수 있다.

이것으로 VS Code + C# DevKit 개발 환경 설정이 완료되었다. 앞으로의 예제는 모두 이 환경을 기준으로 진행한다.

---

## 2.5 DuckDB CLI 기본 명령어 치트시트
DuckDB CLI에서 자주 사용하는 명령어를 정리했다. `.`으로 시작하는 명령은 **도트 명령(dot command)**이라고 하며, SQL이 아닌 CLI 자체의 내장 명령이다.

### 기본 셸 명령어

| 명령어 | 설명 |
|--------|------|
| `duckdb` | 인메모리 데이터베이스로 CLI 시작 |
| `duckdb <파일명>.db` | 파일 기반 데이터베이스로 CLI 시작 |
| `.help` | 사용 가능한 모든 도트 명령 목록 출력 |
| `.quit` 또는 `.exit` | CLI 종료 |
| `.open <파일명>` | 지정한 파일 데이터베이스 열기 |
| `.open :memory:` | 인메모리 데이터베이스로 전환 |

### 출력 형식 설정

| 명령어 | 설명 |
|--------|------|
| `.mode table` | 기본 테이블 형식으로 출력 (기본값) |
| `.mode csv` | CSV 형식으로 출력 |
| `.mode json` | JSON 형식으로 출력 |
| `.mode markdown` | Markdown 테이블 형식으로 출력 |
| `.mode line` | 각 컬럼을 한 줄씩 출력 |
| `.headers on` | 컬럼 헤더 표시 켜기 |
| `.headers off` | 컬럼 헤더 표시 끄기 |

### 파일 입출력

| 명령어 | 설명 |
|--------|------|
| `.read <파일명.sql>` | SQL 파일을 읽어서 실행 |
| `.output <파일명>` | 이후 결과를 파일로 저장 |
| `.output stdout` | 결과 출력을 다시 화면으로 전환 |

### 스키마 탐색

| 명령어 / SQL | 설명 |
|--------------|------|
| `.tables` | 현재 데이터베이스의 모든 테이블 목록 출력 |
| `.schema <테이블명>` | 특정 테이블의 CREATE 문 출력 |
| `SHOW TABLES;` | 테이블 목록 (SQL 방식) |
| `DESCRIBE <테이블명>;` | 테이블 컬럼 정보 출력 |
| `SHOW ALL TABLES;` | 모든 스키마의 테이블 목록 |

### 타이머 및 설정

| 명령어 | 설명 |
|--------|------|
| `.timer on` | 쿼리 실행 시간 측정 켜기 |
| `.timer off` | 쿼리 실행 시간 측정 끄기 |
| `PRAGMA version;` | DuckDB 버전 확인 |
| `PRAGMA database_list;` | 열린 데이터베이스 목록 |

### 자주 쓰는 SQL 패턴

| SQL | 설명 |
|-----|------|
| `SELECT version();` | 현재 DuckDB 버전 출력 |
| `SELECT current_date;` | 현재 날짜 출력 |
| `SELECT * FROM duckdb_functions() LIMIT 10;` | 내장 함수 목록 조회 |
| `COPY tbl TO 'output.csv' (HEADER, DELIMITER ',');` | 테이블을 CSV로 내보내기 |
| `COPY tbl FROM 'input.csv' (HEADER, DELIMITER ',');` | CSV를 테이블로 가져오기 |
| `SELECT * FROM read_csv_auto('data.csv');` | CSV 파일 즉시 조회 |
| `SELECT * FROM read_parquet('data.parquet');` | Parquet 파일 즉시 조회 |

> **팁**: `.timer on`을 설정해 두면 모든 쿼리의 실행 시간이 자동으로 표시된다. 성능을 확인할 때 유용하다.

---

## 이 장의 핵심 정리

이 장에서 배운 내용을 정리하면 다음과 같다.

1. **DuckDB CLI 설치**: `winget install DuckDB.DuckDB` 명령 한 줄로 Windows 11에 DuckDB를 설치할 수 있다. 특정 버전이 필요하다면 공식 사이트에서 직접 다운로드한다.

2. **.NET 9 프로젝트 생성**: `dotnet new console` 명령으로 콘솔 애플리케이션을 생성하고, `dotnet add package` 명령으로 `DuckDB.NET.Bindings`와 `DuckDB.NET.Data` 패키지를 추가한다.

3. **DuckDB CLI 첫 실행**: `duckdb` 명령으로 인터랙티브 셸을 시작하고, SQL을 직접 입력하여 즉시 결과를 확인할 수 있다. 파일 데이터베이스는 `duckdb <파일명>.db` 형식으로 연다.

4. **VS Code 환경 설정**: C# DevKit 확장을 설치하면 IntelliSense, 빌드, 디버깅이 모두 VS Code 안에서 가능해진다.

5. **CLI 치트시트**: `.help`, `.tables`, `.mode`, `.timer on` 등 도트 명령을 활용하면 CLI를 훨씬 효율적으로 사용할 수 있다.

---

## 다음 장 예고

환경 구성이 완료되었다. 이제 DuckDB의 내부를 들여다볼 차례다.

**3장: DuckDB 핵심 아키텍처 이해**에서는 DuckDB가 어떻게 데이터를 저장하고 처리하는지, 컬럼형 스토리지(columnar storage)와 벡터화 실행 엔진(vectorized execution engine)이 무엇인지를 학습한다. DuckDB가 왜 분석 쿼리에서 그토록 빠른 성능을 내는지 원리를 이해하면, 이후의 쿼리 작성과 성능 최적화에서 큰 도움이 된다.  