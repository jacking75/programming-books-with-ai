# 제5장: C#에서 DuckDB 다루기
4장에서는 DuckDB CLI를 통해 SQL의 기초를 익혔다. 이제 실제 애플리케이션 개발 환경인 C#에서 DuckDB를 제어하는 방법을 배울 차례다. 이 장에서는 DuckDB.NET 라이브러리를 사용하여 데이터베이스 연결부터 시작해, 테이블 생성·삭제, 데이터 삽입·수정·삭제, 쿼리 결과 매핑, 파라미터 바인딩, 트랜잭션, 비동기 처리까지 C# 개발자가 실무에서 꼭 필요한 모든 패턴을 다룬다. 예제는 온라인 게임 맥락(플레이어, 전투 로그, 아이템)을 중심으로 구성하여 실전 감각도 함께 키운다.

---

## 5.1 DuckDB.NET 라이브러리 구조 이해

### NuGet 패키지 구성
DuckDB.NET은 크게 두 개의 NuGet 패키지로 구성된다.

| 패키지 | 역할 |
|--------|------|
| `DuckDB.NET.Bindings` | DuckDB C API를 C#에서 직접 호출할 수 있도록 래핑한 저수준 바인딩 |
| `DuckDB.NET.Data` | ADO.NET 표준 인터페이스(`IDbConnection`, `IDbCommand`, `IDataReader`)를 구현한 고수준 API |

일반적인 애플리케이션 개발에서는 `DuckDB.NET.Data`만 참조하면 된다. `DuckDB.NET.Bindings`는 `DuckDB.NET.Data`가 내부적으로 사용하므로 자동으로 함께 설치된다.

### 프로젝트 생성 및 패키지 설치
Visual Studio Code 또는 터미널에서 다음 명령을 실행하여 프로젝트를 생성하고 패키지를 설치한다.

```bash
# 콘솔 앱 프로젝트 생성
dotnet new console -n GameAnalytics -f net9.0

# 프로젝트 디렉터리로 이동
cd GameAnalytics

# DuckDB.NET.Data 패키지 설치 (Bindings는 자동으로 함께 설치됨)
dotnet add package DuckDB.NET.Data
dotnet add package DuckDB.NET.Bindings.Full
```

설치가 완료되면 `.csproj` 파일에 다음과 같이 패키지 참조가 추가된다.

```xml
<ItemGroup>
  <PackageReference Include="DuckDB.NET.Bindings.Full" Version="1.1.0" />
  <PackageReference Include="DuckDB.NET.Data" Version="1.1.0" />
</ItemGroup>
```

### 핵심 클래스 개요
DuckDB.NET.Data가 제공하는 주요 클래스는 다음과 같다.

| 클래스 | 역할 | ADO.NET 인터페이스 |
|--------|------|--------------------|
| `DuckDBConnection` | 데이터베이스 연결 관리 | `IDbConnection` |
| `DuckDBCommand` | SQL 명령 실행 | `IDbCommand` |
| `DuckDBDataReader` | 쿼리 결과 읽기 | `IDataReader` |
| `DuckDBTransaction` | 트랜잭션 관리 | `IDbTransaction` |
| `DuckDBParameter` | 파라미터 바인딩 | `IDbDataParameter` |

이 클래스들은 .NET의 표준 ADO.NET 인터페이스를 구현하기 때문에, 기존에 `SqlConnection`이나 `NpgsqlConnection` 등을 사용해 본 경험이 있다면 거의 동일한 패턴으로 사용할 수 있다. 처음 접하더라도 이 장의 예제를 따라가면 자연스럽게 익히게 된다.

### 네임스페이스
C# 코드에서 DuckDB.NET을 사용하려면 다음 네임스페이스를 임포트한다.

```csharp
using DuckDB.NET.Data;
```

`DuckDB.NET.Data` 네임스페이스 하나만 임포트하면 `DuckDBConnection`, `DuckDBCommand`, `DuckDBDataReader` 등 모든 핵심 클래스를 사용할 수 있다.

---
  

## 5.2 연결(Connection) 열기/닫기 — 파일 DB vs 인메모리 DB

### 연결 문자열(Connection String) 이해
DuckDB는 두 가지 운영 모드를 지원한다.

- **파일 DB 모드**: 데이터를 디스크의 `.duckdb` 파일에 영속적으로 저장한다. 서버를 재시작해도 데이터가 유지된다.
- **인메모리 DB 모드**: 데이터를 메모리에만 저장한다. 프로세스가 종료되면 데이터도 사라진다. 테스트나 임시 분석에 적합하다.

연결 문자열 형식은 다음과 같다.

| 모드 | 연결 문자열 |
|------|------------|
| 파일 DB | `"DataSource=game_analytics.duckdb"` |
| 인메모리 DB | `"DataSource=:memory:"` |

### 파일 DB 연결 예제

```csharp
// code/ch05/ex01-connection/Program.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// 파일 DB 연결 예제
// - DataSource에 파일 경로를 지정하면 파일 DB 모드로 동작한다.
// - 파일이 없으면 자동으로 생성된다.
// -------------------------------------------------------

// using 블록을 사용하면 블록이 끝날 때 자동으로 연결이 닫히고 자원이 해제된다.
using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");

// 연결을 열기 전에는 쿼리를 실행할 수 없다.
connection.Open();

Console.WriteLine($"연결 상태: {connection.State}");  // Open
Console.WriteLine($"데이터베이스: {connection.Database}");

// 간단한 쿼리로 연결 정상 동작 확인
using var command = connection.CreateCommand();
command.CommandText = "SELECT current_date AS today";

using var reader = command.ExecuteReader();
if (reader.Read())
{
    Console.WriteLine($"오늘 날짜: {reader.GetValue(0)}");
}

// using 블록 종료 시 connection.Dispose()가 자동 호출되어 연결이 닫힌다.
```

### 인메모리 DB 연결 예제

```csharp
// code/ch05/ex01-connection/InMemoryExample.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// 인메모리 DB 연결 예제
// - DataSource=:memory: 로 지정하면 인메모리 모드로 동작한다.
// - 빠른 테스트, 임시 집계 작업에 적합하다.
// -------------------------------------------------------

using var connection = new DuckDBConnection("DataSource=:memory:");
connection.Open();

using var command = connection.CreateCommand();

// 인메모리 DB에 테이블 생성
command.CommandText = @"
    CREATE TABLE player (
        player_id   BIGINT PRIMARY KEY,
        player_name VARCHAR NOT NULL,
        level       INTEGER DEFAULT 1,
        created_at  TIMESTAMP DEFAULT current_timestamp
    )";
command.ExecuteNonQuery();

Console.WriteLine("인메모리 DB에 player 테이블 생성 완료");

// 데이터 삽입 및 조회 확인
command.CommandText = "INSERT INTO player (player_id, player_name, level) VALUES (1001, '홍길동', 50)";
command.ExecuteNonQuery();

command.CommandText = "SELECT player_id, player_name, level FROM player";
using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"[{reader.GetInt64(0)}] {reader.GetString(1)} (레벨: {reader.GetInt32(2)})");
}
```

### 연결 수명 관리 모범 사례
DuckDB 연결 객체는 `IDisposable`을 구현하므로 반드시 `using` 패턴을 사용하여 적시에 자원을 해제해야 한다.

```csharp
// 권장: using 선언 방식 (.NET 8+)
using var connection = new DuckDBConnection("DataSource=game.duckdb");
connection.Open();
// ... 작업 수행 ...
// 메서드 종료 시 자동 Dispose

// 권장: using 블록 방식 (명시적 범위 지정)
using (var connection = new DuckDBConnection("DataSource=game.duckdb"))
{
    connection.Open();
    // ... 작업 수행 ...
} // 블록 종료 시 자동 Dispose
```

> **주의**: DuckDB는 기본적으로 단일 쓰기 프로세스 모델이다. 동일한 `.duckdb` 파일을 여러 프로세스에서 동시에 쓰기 모드로 열면 오류가 발생한다. 읽기 전용 접근은 `:read_only:` 옵션으로 가능하다.

---
  

## 5.3 DDL 실행 — 테이블 생성/삭제

### DDL(Data Definition Language)이란?
DDL은 데이터베이스의 구조(스키마)를 정의하는 SQL 구문이다. `CREATE TABLE`, `DROP TABLE`, `ALTER TABLE` 등이 DDL에 해당한다. C#에서 DDL을 실행할 때는 `ExecuteNonQuery()` 메서드를 사용한다.

### 게임 테이블 생성 예제

온라인 게임에서 자주 사용하는 테이블 구조를 생성하는 예제를 살펴보자.

```csharp
// code/ch05/ex02-ddl/Program.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// DDL 실행 예제 — 게임 관련 테이블 생성
// -------------------------------------------------------

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// 1. 플레이어 테이블 생성
// IF NOT EXISTS: 이미 테이블이 있으면 오류 없이 건너뛴다.
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS player (
        player_id       BIGINT       PRIMARY KEY,
        player_name     VARCHAR(50)  NOT NULL,
        level           INTEGER      NOT NULL DEFAULT 1,
        experience      BIGINT       NOT NULL DEFAULT 0,
        gold            BIGINT       NOT NULL DEFAULT 0,
        server_id       INTEGER      NOT NULL DEFAULT 1,
        created_at      TIMESTAMP    NOT NULL DEFAULT current_timestamp,
        last_login_at   TIMESTAMP
    )";
int result = command.ExecuteNonQuery();
Console.WriteLine("player 테이블 생성 완료");

// 2. 전투 로그 테이블 생성
// DuckDB의 SEQUENCE 타입을 사용해 자동 증가 ID를 구현한다.
command.CommandText = @"
    CREATE SEQUENCE IF NOT EXISTS seq_battle_log_id START 1";
command.ExecuteNonQuery();

command.CommandText = @"
    CREATE TABLE IF NOT EXISTS battle_log (
        log_id          BIGINT       PRIMARY KEY DEFAULT nextval('seq_battle_log_id'),
        player_id       BIGINT       NOT NULL,
        monster_id      INTEGER      NOT NULL,
        damage_dealt    INTEGER      NOT NULL DEFAULT 0,
        damage_taken    INTEGER      NOT NULL DEFAULT 0,
        is_victory      BOOLEAN      NOT NULL DEFAULT false,
        battle_duration DOUBLE,              -- 전투 시간 (초)
        logged_at       TIMESTAMP    NOT NULL DEFAULT current_timestamp
    )";
command.ExecuteNonQuery();
Console.WriteLine("battle_log 테이블 생성 완료");

// 3. 아이템 테이블 생성
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS item (
        item_id         INTEGER      PRIMARY KEY,
        item_name       VARCHAR(100) NOT NULL,
        item_type       VARCHAR(20)  NOT NULL,   -- 'weapon', 'armor', 'potion', ...
        rarity          VARCHAR(10)  NOT NULL DEFAULT 'common', -- 'common','rare','epic','legendary'
        base_power      INTEGER      NOT NULL DEFAULT 0
    )";
command.ExecuteNonQuery();
Console.WriteLine("item 테이블 생성 완료");

// 4. 플레이어-아이템 보유 현황 테이블
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS player_item (
        player_id       BIGINT       NOT NULL,
        item_id         INTEGER      NOT NULL,
        quantity        INTEGER      NOT NULL DEFAULT 1,
        obtained_at     TIMESTAMP    NOT NULL DEFAULT current_timestamp,
        PRIMARY KEY (player_id, item_id)
    )";
command.ExecuteNonQuery();
Console.WriteLine("player_item 테이블 생성 완료");
```

### 테이블 삭제 예제

```csharp
// code/ch05/ex02-ddl/DropTableExample.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// IF EXISTS: 테이블이 없어도 오류 없이 건너뛴다.
// 삭제 순서 주의: 외래 키 의존성이 있는 경우 자식 테이블을 먼저 삭제해야 한다.
command.CommandText = "DROP TABLE IF EXISTS player_item";
command.ExecuteNonQuery();
Console.WriteLine("player_item 테이블 삭제 완료");

command.CommandText = "DROP TABLE IF EXISTS battle_log";
command.ExecuteNonQuery();
Console.WriteLine("battle_log 테이블 삭제 완료");
```

### 스키마 정보 조회
현재 데이터베이스에 어떤 테이블들이 있는지 확인할 수 있다.

```csharp
// DuckDB의 정보 스키마(information_schema)를 사용해 테이블 목록 조회
command.CommandText = @"
    SELECT table_name, table_type
    FROM information_schema.tables
    WHERE table_schema = 'main'
    ORDER BY table_name";

using var reader = command.ExecuteReader();
Console.WriteLine("\n=== 현재 테이블 목록 ===");
while (reader.Read())
{
    Console.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
}
```

---
  

## 5.4 DML 실행 — INSERT / UPDATE / DELETE

### DML(Data Manipulation Language)이란?
DML은 테이블의 데이터를 조작하는 SQL 구문이다. `INSERT`(삽입), `UPDATE`(수정), `DELETE`(삭제)가 DML에 해당한다. DDL과 마찬가지로 `ExecuteNonQuery()` 메서드를 사용하며, 반환값은 영향을 받은 행(row)의 수다.

### INSERT — 데이터 삽입

```csharp
// code/ch05/ex03-dml/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// -------------------------------------------------------
// INSERT 예제
// -------------------------------------------------------

// 단건 삽입
command.CommandText = @"
    INSERT INTO player (player_id, player_name, level, experience, gold, server_id)
    VALUES (1001, '홍길동', 50, 125000, 30000, 1)";
int rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"삽입된 행 수: {rowsAffected}");  // 1

// 여러 행 한 번에 삽입 (VALUES 절 나열)
command.CommandText = @"
    INSERT INTO player (player_id, player_name, level, experience, gold, server_id)
    VALUES
        (1002, '이순신', 75, 280000, 55000, 1),
        (1003, '강감찬', 33, 42000, 18000, 2),
        (1004, '세종대왕', 99, 9999999, 100000, 1)";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"삽입된 행 수: {rowsAffected}");  // 3

// 아이템 데이터 삽입
command.CommandText = @"
    INSERT INTO item (item_id, item_name, item_type, rarity, base_power)
    VALUES
        (101, '불꽃의 검', 'weapon', 'epic', 350),
        (102, '강철 갑옷', 'armor', 'rare', 220),
        (103, '체력 물약', 'potion', 'common', 0),
        (104, '용의 심장', 'weapon', 'legendary', 999)";
command.ExecuteNonQuery();
Console.WriteLine("아이템 데이터 삽입 완료");
```

### UPDATE — 데이터 수정

```csharp
// -------------------------------------------------------
// UPDATE 예제
// -------------------------------------------------------

// 플레이어 레벨업 처리
command.CommandText = @"
    UPDATE player
    SET level = level + 1,
        experience = experience + 5000
    WHERE player_id = 1001";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"레벨업 처리된 플레이어 수: {rowsAffected}");

// 서버 1의 모든 플레이어에게 골드 지급 (이벤트)
command.CommandText = @"
    UPDATE player
    SET gold = gold + 1000
    WHERE server_id = 1";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"골드 지급된 플레이어 수: {rowsAffected}");
```

### DELETE — 데이터 삭제

```csharp
// -------------------------------------------------------
// DELETE 예제
// -------------------------------------------------------

// 특정 플레이어 삭제 (탈퇴 처리)
command.CommandText = "DELETE FROM player WHERE player_id = 1003";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"삭제된 플레이어 수: {rowsAffected}");

// 레벨 30 미만의 비활성 플레이어 정리
command.CommandText = @"
    DELETE FROM player
    WHERE level < 30
    AND last_login_at < current_timestamp - INTERVAL 90 DAY";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"비활성 플레이어 정리: {rowsAffected}명 삭제");
```

### UPSERT — 삽입 또는 수정
DuckDB는 `INSERT OR REPLACE`와 `ON CONFLICT DO UPDATE` 구문을 지원한다. 게임에서는 플레이어가 이미 존재하면 업데이트하고, 없으면 새로 삽입해야 하는 경우가 많다.

```csharp
// -------------------------------------------------------
// UPSERT 예제 (INSERT OR REPLACE)
// -------------------------------------------------------

// player_item 테이블에서: 이미 보유한 아이템이면 수량 증가, 없으면 새로 삽입
command.CommandText = @"
    INSERT INTO player_item (player_id, item_id, quantity)
    VALUES (1001, 103, 5)
    ON CONFLICT (player_id, item_id)
    DO UPDATE SET quantity = player_item.quantity + excluded.quantity";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"아이템 지급 처리: {rowsAffected}행 영향");
```

---
  

## 5.5 쿼리 결과를 C# 객체로 매핑

### DuckDBDataReader 사용법
`SELECT` 쿼리를 실행하면 `DuckDBDataReader` 객체가 반환된다. 이 객체는 커서(cursor) 방식으로 결과 행을 한 줄씩 순회한다.

```csharp
// ExecuteReader()로 SELECT 쿼리 실행
using var reader = command.ExecuteReader();

// Read()가 true를 반환하는 동안 다음 행으로 이동
while (reader.Read())
{
    // 컬럼 인덱스(0부터) 또는 컬럼명으로 값을 읽는다.
    long playerId   = reader.GetInt64(0);          // 인덱스 방식
    string name     = reader.GetString("player_name");  // 컬럼명 방식
}
```

### 주요 타입별 데이터 읽기 메서드

| C# 타입 | 메서드 | DuckDB 타입 |
|---------|--------|------------|
| `long` | `GetInt64(i)` | `BIGINT` |
| `int` | `GetInt32(i)` | `INTEGER` |
| `string` | `GetString(i)` | `VARCHAR` |
| `double` | `GetDouble(i)` | `DOUBLE` |
| `bool` | `GetBoolean(i)` | `BOOLEAN` |
| `DateTime` | `GetDateTime(i)` | `TIMESTAMP` |
| `decimal` | `GetDecimal(i)` | `DECIMAL` |
| `object` | `GetValue(i)` | 모든 타입 |

NULL 값을 처리할 때는 `IsDBNull(i)`를 먼저 확인해야 한다.

### C# 모델 클래스 정의 및 매핑

```csharp
// code/ch05/ex04-mapping/Models.cs

// 플레이어 모델
public record Player(
    long PlayerId,
    string PlayerName,
    int Level,
    long Experience,
    long Gold,
    int ServerId,
    DateTime CreatedAt,
    DateTime? LastLoginAt  // NULL 허용 필드는 Nullable 타입으로
);

// 전투 로그 모델
public record BattleLog(
    long LogId,
    long PlayerId,
    int MonsterId,
    int DamageDealt,
    int DamageTaken,
    bool IsVictory,
    double? BattleDuration,
    DateTime LoggedAt
);

// 아이템 모델
public record Item(
    int ItemId,
    string ItemName,
    string ItemType,
    string Rarity,
    int BasePower
);
```

```csharp
// code/ch05/ex04-mapping/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// -------------------------------------------------------
// 쿼리 결과를 Player 리스트로 매핑
// -------------------------------------------------------

command.CommandText = @"
    SELECT player_id, player_name, level, experience, gold, server_id,
           created_at, last_login_at
    FROM player
    WHERE server_id = 1
    ORDER BY level DESC";

using var reader = command.ExecuteReader();

var players = new List<Player>();
while (reader.Read())
{
    var player = new Player(
        PlayerId:    reader.GetInt64(0),
        PlayerName:  reader.GetString(1),
        Level:       reader.GetInt32(2),
        Experience:  reader.GetInt64(3),
        Gold:        reader.GetInt64(4),
        ServerId:    reader.GetInt32(5),
        CreatedAt:   reader.GetDateTime(6),
        // NULL 가능한 컬럼은 IsDBNull로 확인 후 처리
        LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    );
    players.Add(player);
}

Console.WriteLine($"조회된 플레이어: {players.Count}명");
foreach (var p in players)
{
    Console.WriteLine($"  [{p.PlayerId}] {p.PlayerName} | Lv.{p.Level} | 골드: {p.Gold:N0}");
}
```

### 단일 값 조회 — ExecuteScalar()
단순히 하나의 값(COUNT, SUM 등)만 조회할 때는 `ExecuteScalar()`를 사용하면 더 간결하다.

```csharp
// -------------------------------------------------------
// ExecuteScalar() — 단일 값 조회
// -------------------------------------------------------

// 전체 플레이어 수 조회
command.CommandText = "SELECT COUNT(*) FROM player";
long totalPlayers = (long)command.ExecuteScalar()!;
Console.WriteLine($"전체 플레이어: {totalPlayers}명");

// 최고 레벨 플레이어의 레벨 조회
command.CommandText = "SELECT MAX(level) FROM player";
int maxLevel = (int)command.ExecuteScalar()!;
Console.WriteLine($"최고 레벨: {maxLevel}");

// 서버 1의 평균 골드 조회
command.CommandText = "SELECT AVG(gold) FROM player WHERE server_id = 1";
double avgGold = (double)command.ExecuteScalar()!;
Console.WriteLine($"서버 1 평균 골드: {avgGold:N0}");
```

### 확장 메서드로 매핑 코드 재사용
반복적인 매핑 코드를 줄이기 위해 확장 메서드를 만들어 두면 편리하다.

```csharp
// code/ch05/ex04-mapping/Extensions.cs
using DuckDB.NET.Data;

public static class DataReaderExtensions
{
    // DuckDBDataReader에서 Player 객체를 생성하는 확장 메서드
    public static Player ToPlayer(this DuckDBDataReader reader) => new(
        PlayerId:    reader.GetInt64(0),
        PlayerName:  reader.GetString(1),
        Level:       reader.GetInt32(2),
        Experience:  reader.GetInt64(3),
        Gold:        reader.GetInt64(4),
        ServerId:    reader.GetInt32(5),
        CreatedAt:   reader.GetDateTime(6),
        LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    );

    // 제네릭 방식으로 전체 결과를 리스트로 변환
    public static List<T> ToList<T>(this DuckDBDataReader reader, Func<DuckDBDataReader, T> mapper)
    {
        var list = new List<T>();
        while (reader.Read())
            list.Add(mapper(reader));
        return list;
    }
}

// 사용 예
// using var reader = command.ExecuteReader();
// var players = reader.ToList(r => r.ToPlayer());
```

---
  

## 5.6 파라미터 바인딩과 SQL 인젝션 방지

### SQL 인젝션이란?
SQL 인젝션은 사용자 입력값이 SQL 구문에 직접 삽입될 때 발생하는 보안 취약점이다. 예를 들어 다음과 같이 문자열을 직접 조합하는 코드는 위험하다.

```csharp
// 절대 이렇게 하면 안 된다!
string userInput = "'; DROP TABLE player; --";
command.CommandText = $"SELECT * FROM player WHERE player_name = '{userInput}'";
// 실행되는 SQL: SELECT * FROM player WHERE player_name = ''; DROP TABLE player; --'
// player 테이블이 통째로 삭제될 수 있다!
```

파라미터 바인딩을 사용하면 입력값이 SQL 구문으로 해석되지 않고 단순한 데이터로 처리되므로 SQL 인젝션을 완벽하게 방지할 수 있다.

### Named Parameter 방식
DuckDB.NET은 `$파라미터명` 형식의 named parameter를 지원한다.

```csharp
// code/ch05/ex05-parameter/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// -------------------------------------------------------
// Named Parameter 방식 — 파라미터 바인딩
// -------------------------------------------------------

// SQL 문에서 파라미터는 $이름 형식으로 표시한다.
command.CommandText = @"
    SELECT player_id, player_name, level, gold
    FROM player
    WHERE server_id = $serverId
    AND level >= $minLevel
    ORDER BY level DESC
    LIMIT $pageSize";

// Parameters.Add()로 파라미터 값을 바인딩한다.
command.Parameters.Add(new DuckDBParameter("serverId", 1));
command.Parameters.Add(new DuckDBParameter("minLevel", 50));
command.Parameters.Add(new DuckDBParameter("pageSize", 10));

using var reader = command.ExecuteReader();
Console.WriteLine("=== 서버 1 레벨 50 이상 플레이어 ===");
while (reader.Read())
{
    Console.WriteLine($"[{reader.GetInt64(0)}] {reader.GetString(1)} " +
                      $"Lv.{reader.GetInt32(2)} | 골드: {reader.GetInt64(3):N0}");
}
```

### INSERT에 파라미터 바인딩 적용

```csharp
// -------------------------------------------------------
// INSERT에 파라미터 바인딩 적용
// -------------------------------------------------------

// 파라미터를 사용하면 다양한 플레이어 데이터를 안전하게 삽입할 수 있다.
var newPlayers = new[]
{
    (Id: 2001L, Name: "신플레이어1", Level: 1, Gold: 100L, ServerId: 2),
    (Id: 2002L, Name: "신플레이어2", Level: 1, Gold: 100L, ServerId: 2),
    (Id: 2003L, Name: "신플레이어3", Level: 1, Gold: 100L, ServerId: 2),
};

command.CommandText = @"
    INSERT INTO player (player_id, player_name, level, gold, server_id)
    VALUES ($playerId, $playerName, $level, $gold, $serverId)";

// 먼저 파라미터 슬롯을 생성해 두고
command.Parameters.Add(new DuckDBParameter("playerId",   0L));
command.Parameters.Add(new DuckDBParameter("playerName", ""));
command.Parameters.Add(new DuckDBParameter("level",      0));
command.Parameters.Add(new DuckDBParameter("gold",       0L));
command.Parameters.Add(new DuckDBParameter("serverId",   0));

foreach (var p in newPlayers)
{
    // 파라미터 값만 교체하여 반복 실행 (쿼리 파싱은 1번만 수행됨 — 성능상 유리)
    command.Parameters["playerId"].Value   = p.Id;
    command.Parameters["playerName"].Value = p.Name;
    command.Parameters["level"].Value      = p.Level;
    command.Parameters["gold"].Value       = p.Gold;
    command.Parameters["serverId"].Value   = p.ServerId;

    command.ExecuteNonQuery();
    Console.WriteLine($"플레이어 '{p.Name}' 등록 완료");
}
```

### 파라미터 타입 명시
DuckDB.NET은 대부분의 경우 C# 타입에서 DuckDB 타입을 자동으로 추론하지만, 명시적으로 타입을 지정하면 더욱 안정적으로 동작한다.

```csharp
// 타입을 명시하는 경우
command.Parameters.Add(new DuckDBParameter
{
    ParameterName = "loggedAt",
    Value         = DateTime.UtcNow,
    DbType        = System.Data.DbType.DateTime
});
```

---

## 5.7 트랜잭션 처리 패턴

### 트랜잭션이 필요한 이유
트랜잭션(Transaction)은 여러 개의 데이터베이스 작업을 하나의 논리적 단위로 묶어, 전부 성공하거나 전부 실패하도록 보장하는 메커니즘이다.

온라인 게임에서 전형적인 예시를 들면, 플레이어 A가 플레이어 B에게 골드를 보내는 경우 다음 두 작업이 반드시 원자적으로 처리되어야 한다.

1. A의 골드 차감
2. B의 골드 증가

만약 1번이 성공하고 2번이 실패한다면, A의 골드만 사라지는 심각한 버그가 발생한다. 트랜잭션을 사용하면 이런 문제를 방지할 수 있다.

### 기본 트랜잭션 패턴

```csharp
// code/ch05/ex06-transaction/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

// -------------------------------------------------------
// 기본 트랜잭션 패턴
// -------------------------------------------------------

// BeginTransaction()으로 트랜잭션 시작
using var transaction = connection.BeginTransaction();

try
{
    using var command = connection.CreateCommand();

    // 트랜잭션 내에서 실행할 명령에 트랜잭션을 연결한다.
    command.Transaction = transaction;

    // 1단계: 골드 송금 - 보내는 플레이어 골드 차감
    command.CommandText = @"
        UPDATE player
        SET gold = gold - $amount
        WHERE player_id = $senderId AND gold >= $amount";
    command.Parameters.Add(new DuckDBParameter("amount",   5000L));
    command.Parameters.Add(new DuckDBParameter("senderId", 1001L));

    int rowsAffected = command.ExecuteNonQuery();
    if (rowsAffected == 0)
    {
        // 골드가 부족하거나 플레이어가 없는 경우
        throw new InvalidOperationException("골드가 부족하거나 플레이어를 찾을 수 없습니다.");
    }

    // 2단계: 골드 송금 - 받는 플레이어 골드 증가
    command.CommandText = @"
        UPDATE player
        SET gold = gold + $amount
        WHERE player_id = $receiverId";
    command.Parameters.Clear();
    command.Parameters.Add(new DuckDBParameter("amount",     5000L));
    command.Parameters.Add(new DuckDBParameter("receiverId", 1002L));

    rowsAffected = command.ExecuteNonQuery();
    if (rowsAffected == 0)
    {
        throw new InvalidOperationException("수신 플레이어를 찾을 수 없습니다.");
    }

    // 모든 작업이 성공하면 커밋 — 변경사항을 영구적으로 저장
    transaction.Commit();
    Console.WriteLine("골드 송금 완료: 1001 → 1002, 5,000골드");
}
catch (Exception ex)
{
    // 오류가 발생하면 롤백 — 모든 변경사항을 되돌림
    transaction.Rollback();
    Console.WriteLine($"오류로 인해 롤백: {ex.Message}");
}
```

### 배치 삽입 트랜잭션
대량의 데이터를 삽입할 때 트랜잭션을 사용하면 성능이 크게 향상된다. 트랜잭션 없이 1,000건을 삽입하면 각각 개별 트랜잭션으로 처리되어 느리지만, 하나의 트랜잭션으로 묶으면 훨씬 빠르다.

```csharp
// code/ch05/ex06-transaction/BatchInsertExample.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

// -------------------------------------------------------
// 배치 삽입 트랜잭션 — 전투 로그 대량 삽입
// -------------------------------------------------------

// 테스트용 전투 로그 1,000건 생성
var random = new Random(42);
var battleLogs = Enumerable.Range(1, 1000).Select(i => new
{
    PlayerId       = (long)(1001 + random.Next(4)),   // 4명의 플레이어 중 랜덤
    MonsterId      = random.Next(1, 50),
    DamageDealt    = random.Next(100, 5000),
    DamageTaken    = random.Next(0, 2000),
    IsVictory      = random.NextDouble() > 0.3,        // 70% 승률
    BattleDuration = 10.0 + random.NextDouble() * 290
}).ToList();

var stopwatch = System.Diagnostics.Stopwatch.StartNew();

using var transaction = connection.BeginTransaction();
try
{
    using var command = connection.CreateCommand();
    command.Transaction = transaction;
    command.CommandText = @"
        INSERT INTO battle_log
            (player_id, monster_id, damage_dealt, damage_taken, is_victory, battle_duration)
        VALUES
            ($playerId, $monsterId, $damageDealt, $damageTaken, $isVictory, $battleDuration)";

    // 파라미터 슬롯 사전 생성
    command.Parameters.Add(new DuckDBParameter("playerId",       0L));
    command.Parameters.Add(new DuckDBParameter("monsterId",      0));
    command.Parameters.Add(new DuckDBParameter("damageDealt",    0));
    command.Parameters.Add(new DuckDBParameter("damageTaken",    0));
    command.Parameters.Add(new DuckDBParameter("isVictory",      false));
    command.Parameters.Add(new DuckDBParameter("battleDuration", 0.0));

    foreach (var log in battleLogs)
    {
        command.Parameters["playerId"].Value       = log.PlayerId;
        command.Parameters["monsterId"].Value      = log.MonsterId;
        command.Parameters["damageDealt"].Value    = log.DamageDealt;
        command.Parameters["damageTaken"].Value    = log.DamageTaken;
        command.Parameters["isVictory"].Value      = log.IsVictory;
        command.Parameters["battleDuration"].Value = log.BattleDuration;
        command.ExecuteNonQuery();
    }

    transaction.Commit();
    stopwatch.Stop();
    Console.WriteLine($"전투 로그 {battleLogs.Count}건 삽입 완료 ({stopwatch.ElapsedMilliseconds}ms)");
}
catch (Exception ex)
{
    transaction.Rollback();
    Console.WriteLine($"배치 삽입 실패: {ex.Message}");
}
```

### 트랜잭션 범위를 명시하는 헬퍼 메서드
트랜잭션 패턴을 매번 반복하지 않도록 헬퍼 메서드로 추상화하면 코드가 깔끔해진다.

```csharp
// code/ch05/ex06-transaction/TransactionHelper.cs
using DuckDB.NET.Data;

public static class DuckDBTransactionHelper
{
    /// <summary>
    /// 트랜잭션 내에서 주어진 작업을 실행한다.
    /// 성공 시 커밋, 실패 시 롤백한다.
    /// </summary>
    public static void ExecuteInTransaction(
        DuckDBConnection connection,
        Action<DuckDBConnection> action)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            action(connection);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;  // 예외를 다시 던져서 호출자가 처리하도록 한다.
        }
    }
}

// 사용 예
// DuckDBTransactionHelper.ExecuteInTransaction(connection, conn =>
// {
//     using var cmd = conn.CreateCommand();
//     cmd.CommandText = "UPDATE player SET gold = gold - 500 WHERE player_id = 1001";
//     cmd.ExecuteNonQuery();
//     // ... 추가 작업 ...
// });
```

---
  

## 5.8 비동기(async/await) 패턴

### DuckDB.NET의 비동기 지원 현황
DuckDB.NET은 ADO.NET의 표준 비동기 메서드(`ExecuteReaderAsync`, `ExecuteNonQueryAsync`, `ExecuteScalarAsync`)를 부분적으로 지원한다. 다만, DuckDB의 C 라이브러리 자체가 내부적으로는 동기 방식으로 동작하기 때문에, DuckDB.NET의 비동기 메서드는 실제로는 동기 작업을 `Task.Run`으로 래핑하는 형태로 구현되어 있다.

따라서 CPU 집약적인 쿼리에서 스레드 풀 분리 효과는 얻을 수 있지만, I/O 비동기 처리와 같은 완전한 비동기 이점을 기대하기는 어렵다. 그럼에도 불구하고 `async/await` 패턴을 사용하면 UI 애플리케이션이나 ASP.NET Core 환경에서 메인 스레드를 블로킹하지 않는 이점이 있다.

### 비동기 연결 및 쿼리 실행

```csharp
// code/ch05/ex07-async/Program.cs
using DuckDB.NET.Data;

// .NET 9 top-level statements + async
await RunAsyncExampleAsync();

async Task RunAsyncExampleAsync()
{
    // -------------------------------------------------------
    // 비동기 패턴 예제
    // -------------------------------------------------------

    using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");

    // OpenAsync()로 비동기 연결 열기
    await connection.OpenAsync();
    Console.WriteLine("비동기 연결 완료");

    using var command = connection.CreateCommand();

    // 1. ExecuteNonQueryAsync — 비동기 DDL/DML 실행
    command.CommandText = @"
        CREATE TABLE IF NOT EXISTS async_test (
            id      INTEGER PRIMARY KEY,
            message VARCHAR
        )";
    await command.ExecuteNonQueryAsync();

    // 2. 비동기 데이터 삽입
    command.CommandText = "INSERT INTO async_test VALUES ($id, $message)";
    command.Parameters.Add(new DuckDBParameter("id",      1));
    command.Parameters.Add(new DuckDBParameter("message", "비동기 테스트"));
    await command.ExecuteNonQueryAsync();

    // 3. ExecuteScalarAsync — 비동기 단일 값 조회
    command.CommandText = "SELECT COUNT(*) FROM async_test";
    command.Parameters.Clear();
    long count = (long)(await command.ExecuteScalarAsync())!;
    Console.WriteLine($"비동기 COUNT: {count}");

    // 4. ExecuteReaderAsync — 비동기 쿼리 결과 읽기
    command.CommandText = "SELECT id, message FROM async_test";
    using var reader = (DuckDBDataReader)await command.ExecuteReaderAsync();

    Console.WriteLine("=== 비동기 조회 결과 ===");
    while (await reader.ReadAsync())  // ReadAsync()로 비동기 행 읽기
    {
        Console.WriteLine($"  [{reader.GetInt32(0)}] {reader.GetString(1)}");
    }
}
```

### 비동기 패턴을 활용한 게임 서비스 클래스
실제 서버 애플리케이션에서는 서비스 클래스에 비동기 메서드를 구현하여 사용하는 패턴이 일반적이다.

```csharp
// code/ch05/ex07-async/PlayerService.cs
using DuckDB.NET.Data;

/// <summary>
/// 플레이어 데이터 접근 서비스 — 비동기 패턴 적용
/// </summary>
public class PlayerService : IDisposable
{
    private readonly DuckDBConnection _connection;

    public PlayerService(string dataSource)
    {
        _connection = new DuckDBConnection($"DataSource={dataSource}");
    }

    // 서비스 초기화 (비동기 연결 열기)
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    /// <summary>
    /// 플레이어 ID로 단일 플레이어 조회 (비동기)
    /// </summary>
    public async Task<Player?> GetPlayerByIdAsync(long playerId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT player_id, player_name, level, experience, gold, server_id,
                   created_at, last_login_at
            FROM player
            WHERE player_id = $playerId";
        command.Parameters.Add(new DuckDBParameter("playerId", playerId));

        using var reader = (DuckDBDataReader)await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Player(
                PlayerId:    reader.GetInt64(0),
                PlayerName:  reader.GetString(1),
                Level:       reader.GetInt32(2),
                Experience:  reader.GetInt64(3),
                Gold:        reader.GetInt64(4),
                ServerId:    reader.GetInt32(5),
                CreatedAt:   reader.GetDateTime(6),
                LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            );
        }
        return null;  // 플레이어가 없으면 null 반환
    }

    /// <summary>
    /// 서버 내 상위 N명의 플레이어 조회 (비동기)
    /// </summary>
    public async Task<List<Player>> GetTopPlayersAsync(int serverId, int topN = 10)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT player_id, player_name, level, experience, gold, server_id,
                   created_at, last_login_at
            FROM player
            WHERE server_id = $serverId
            ORDER BY level DESC, experience DESC
            LIMIT $topN";
        command.Parameters.Add(new DuckDBParameter("serverId", serverId));
        command.Parameters.Add(new DuckDBParameter("topN",     topN));

        var players = new List<Player>();
        using var reader = (DuckDBDataReader)await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            players.Add(new Player(
                PlayerId:    reader.GetInt64(0),
                PlayerName:  reader.GetString(1),
                Level:       reader.GetInt32(2),
                Experience:  reader.GetInt64(3),
                Gold:        reader.GetInt64(4),
                ServerId:    reader.GetInt32(5),
                CreatedAt:   reader.GetDateTime(6),
                LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            ));
        }
        return players;
    }

    /// <summary>
    /// 플레이어 골드 업데이트 (비동기)
    /// </summary>
    public async Task<bool> UpdatePlayerGoldAsync(long playerId, long goldDelta)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE player
            SET gold = gold + $delta
            WHERE player_id = $playerId AND (gold + $delta) >= 0";
        command.Parameters.Add(new DuckDBParameter("playerId", playerId));
        command.Parameters.Add(new DuckDBParameter("delta",    goldDelta));

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
```

```csharp
// code/ch05/ex07-async/Program.cs — PlayerService 사용 예
using DuckDB.NET.Data;

// PlayerService 초기화
await using var service = new PlayerService("game_analytics.duckdb");
// IAsyncDisposable 미구현 시 Dispose() 직접 호출
// using var service = new PlayerService("game_analytics.duckdb");

await service.InitializeAsync();

// 플레이어 조회
var player = await service.GetPlayerByIdAsync(1001);
if (player is not null)
{
    Console.WriteLine($"플레이어 조회: {player.PlayerName} (Lv.{player.Level})");
}

// 상위 플레이어 목록 조회
var topPlayers = await service.GetTopPlayersAsync(serverId: 1, topN: 5);
Console.WriteLine("\n=== 서버 1 TOP 5 플레이어 ===");
for (int i = 0; i < topPlayers.Count; i++)
{
    Console.WriteLine($"  {i + 1}위. {topPlayers[i].PlayerName} (Lv.{topPlayers[i].Level})");
}

// 골드 업데이트
bool success = await service.UpdatePlayerGoldAsync(1001, -500);
Console.WriteLine($"\n골드 차감 {(success ? "성공" : "실패")}: 플레이어 1001, -500골드");
```

### 여러 비동기 작업을 병렬로 실행
DuckDB는 단일 연결 내에서는 병렬 실행이 지원되지 않으므로, 병렬로 쿼리를 실행하려면 각각 별도의 연결을 사용해야 한다.

```csharp
// -------------------------------------------------------
// 여러 비동기 작업 순차 실행 (단일 연결)
// -------------------------------------------------------

// 단일 연결에서는 순차 실행을 권장한다.
var p1001 = await service.GetPlayerByIdAsync(1001);
var p1002 = await service.GetPlayerByIdAsync(1002);

// 서로 다른 연결이 필요한 경우: 각각 별도 서비스 인스턴스를 생성
// var service1 = new PlayerService("game_analytics.duckdb");
// var service2 = new PlayerService("game_analytics.duckdb");
// var results = await Task.WhenAll(
//     service1.GetTopPlayersAsync(1),
//     service2.GetTopPlayersAsync(2)
// );
```

---
    

## 이 장의 핵심 정리
이 장에서 배운 내용을 정리하면 다음과 같다.

| 주제 | 핵심 내용 |
|------|-----------|
| **라이브러리 구조** | `DuckDB.NET.Data` 패키지가 ADO.NET 표준 인터페이스를 구현하며, 주요 클래스는 `DuckDBConnection`, `DuckDBCommand`, `DuckDBDataReader`이다. |
| **연결 관리** | `"DataSource=파일명.duckdb"`로 파일 DB, `"DataSource=:memory:"`로 인메모리 DB에 연결한다. `using` 패턴으로 자원 해제를 반드시 보장한다. |
| **DDL 실행** | `ExecuteNonQuery()`로 `CREATE TABLE`, `DROP TABLE` 등 스키마 변경 구문을 실행한다. `IF NOT EXISTS`, `IF EXISTS`로 안전하게 처리한다. |
| **DML 실행** | `INSERT`, `UPDATE`, `DELETE`도 `ExecuteNonQuery()`로 실행하며, 반환값은 영향받은 행 수이다. |
| **결과 매핑** | `ExecuteReader()`로 `DuckDBDataReader`를 얻어 `Read()` 루프로 행을 순회한다. 단일 값 조회는 `ExecuteScalar()`가 간결하다. NULL 처리는 `IsDBNull()`로 확인한다. |
| **파라미터 바인딩** | `$파라미터명` 형식의 named parameter와 `DuckDBParameter`를 사용한다. 문자열 직접 조합은 SQL 인젝션 위험이 있으므로 절대 사용하지 않는다. |
| **트랜잭션** | `BeginTransaction()` → 작업 → `Commit()` / `Rollback()` 패턴을 사용한다. 배치 삽입 시 트랜잭션으로 묶으면 성능이 크게 향상된다. |
| **비동기 패턴** | `OpenAsync()`, `ExecuteNonQueryAsync()`, `ExecuteReaderAsync()`, `ReadAsync()`를 활용한다. DuckDB.NET의 비동기는 내부적으로 동기 작업을 래핑하지만, UI/서버 환경에서 메인 스레드 블로킹 방지에 유용하다. |

---

## 다음 장 예고
이 장에서는 C#에서 DuckDB를 다루는 기본 패턴을 모두 익혔다. 연결 관리, DDL/DML, 결과 매핑, 파라미터 바인딩, 트랜잭션, 비동기 처리까지 실무에 필요한 핵심 기법을 코드로 직접 확인했다.

**제6장: 파일 데이터 읽기/쓰기**에서는 DuckDB의 강력한 파일 처리 능력을 집중적으로 다룬다. CSV, JSON, Parquet 등 다양한 형식의 파일을 SQL 한 줄로 읽어들이고, 분석 결과를 다시 파일로 저장하는 방법을 배운다. 특히 수백만 건의 게임 로그 파일을 DuckDB로 직접 쿼리하는 실전 예제를 통해 DuckDB가 왜 분석 데이터베이스로 각광받는지를 몸소 느낄 수 있을 것이다.   