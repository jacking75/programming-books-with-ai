# 부록

---

## 부록 A. DuckDB SQL 함수 레퍼런스 (게임 분석용 핵심 함수)

### A.1 문자열 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `concat(s1, s2, ...)` | 여러 문자열을 이어붙임 | `concat(server_id, '_', player_id)` — 고유 키 생성 |
| `length(s)` | 문자열 길이 반환 | `length(player_name)` — 닉네임 길이 검증 |
| `substring(s, start, len)` | 문자열 일부 추출 | `substring(event_type, 1, 4)` — 이벤트 분류 코드 추출 |
| `replace(s, from, to)` | 문자열 치환 | `replace(item_name, '_', ' ')` — 아이템 이름 정제 |
| `trim(s)` | 양쪽 공백 제거 | `trim(player_name)` — 입력값 정제 |
| `upper(s)` | 대문자로 변환 | `upper(region_code)` — 지역 코드 정규화 |
| `lower(s)` | 소문자로 변환 | `lower(email)` — 이메일 정규화 |
| `s LIKE pattern` | 패턴 매칭 (% 와일드카드) | `event_type LIKE 'BATTLE_%'` — 전투 이벤트 필터 |
| `regexp_matches(s, pattern)` | 정규식 매칭 여부 반환 | `regexp_matches(player_name, '^[a-zA-Z0-9]+$')` — 닉네임 유효성 검사 |
| `string_split(s, sep)` | 구분자로 분리하여 리스트 반환 | `string_split(item_tags, ',')` — 아이템 태그 파싱 |

```sql
-- 서버+플레이어 복합 키로 중복 접속 확인
SELECT
    concat(server_id, '_', CAST(player_id AS VARCHAR)) AS composite_key,
    COUNT(*) AS login_count
FROM login_logs
GROUP BY composite_key
HAVING login_count > 1;

-- 이벤트 타입 접두어별 통계
SELECT
    substring(event_type, 1, 6) AS event_prefix,
    COUNT(*) AS event_count
FROM game_events
GROUP BY event_prefix
ORDER BY event_count DESC;
```

---

### A.2 날짜/시간 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `now()` | 현재 타임스탬프 반환 | `now()` — 리포트 생성 시각 기록 |
| `current_date` | 현재 날짜(DATE) 반환 | `current_date - INTERVAL 7 DAY` — 7일 전 날짜 |
| `date_trunc(part, ts)` | 지정 단위로 잘라냄 | `date_trunc('hour', event_time)` — 시간대별 집계 |
| `date_diff(part, start, end)` | 두 날짜의 차이 계산 | `date_diff('day', first_login, last_login)` — 플레이어 활동 기간 |
| `date_add(date, interval)` | 날짜에 기간 추가 | `date_add(join_date, INTERVAL 30 DAY)` — 30일 후 리텐션 체크 날짜 |
| `extract(part FROM ts)` | 날짜/시간 특정 부분 추출 | `extract('hour' FROM event_time)` — 시간대 추출 |
| `strftime(format, ts)` | 날짜를 문자열로 포맷 | `strftime('%Y-%m-%d', event_time)` — 날짜 문자열 변환 |
| `age(ts1, ts2)` | 두 타임스탬프 차이를 interval로 반환 | `age(now(), first_login)` — 가입 경과 기간 |

```sql
-- 시간대별 동시 접속자 분석 (피크 타임 파악)
SELECT
    extract('hour' FROM login_time) AS hour_of_day,
    COUNT(DISTINCT player_id) AS concurrent_players
FROM login_logs
WHERE login_time >= current_date - INTERVAL 7 DAY
GROUP BY hour_of_day
ORDER BY hour_of_day;

-- 플레이어 생애 주기 단계 분류
SELECT
    player_id,
    date_diff('day', first_login, last_login) AS active_days,
    CASE
        WHEN date_diff('day', last_login, current_date) <= 1 THEN '활성'
        WHEN date_diff('day', last_login, current_date) <= 7 THEN '7일 이탈 위험'
        WHEN date_diff('day', last_login, current_date) <= 30 THEN '30일 이탈'
        ELSE '이탈'
    END AS lifecycle_stage
FROM player_summary;
```

---

### A.3 수학 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `abs(x)` | 절댓값 | `abs(balance_change)` — 재화 변동량 |
| `ceil(x)` | 올림 | `ceil(damage / 100.0)` — 피해량 백 단위 올림 |
| `floor(x)` | 내림 | `floor(exp / 1000.0)` — 경험치 레벨 계산 |
| `round(x, n)` | 반올림 (n: 소수점 자리) | `round(win_rate * 100, 2)` — 승률 퍼센트 표시 |
| `sqrt(x)` | 제곱근 | `sqrt(variance)` — 표준편차 계산 |
| `pow(x, y)` | x의 y제곱 | `pow(2, level)` — 레벨별 경험치 요구량 계산 |
| `ln(x)` | 자연로그 | `ln(revenue)` — 매출 로그 변환 |
| `log(x)` | 상용로그 | `log(damage)` — 피해량 스케일 조정 |
| `random()` | 0~1 난수 | `random()` — 샘플링 |
| `range(start, stop, step)` | 숫자 시퀀스 생성 | `range(1, 31, 1)` — 1~30일 날짜 시퀀스 생성 |

```sql
-- 승률 계산 및 통계
SELECT
    player_id,
    round(SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2) AS win_rate_pct,
    COUNT(*) AS total_battles
FROM battle_logs
GROUP BY player_id
HAVING total_battles >= 10
ORDER BY win_rate_pct DESC;

-- 랜덤 샘플링 (전체 로그의 1%)
SELECT *
FROM game_events
WHERE random() < 0.01;
```

---

### A.4 집계 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `count(*)` | 전체 행 수 | `count(*)` — 이벤트 발생 건수 |
| `count(DISTINCT col)` | 고유 값 수 | `count(DISTINCT player_id)` — DAU 계산 |
| `sum(col)` | 합계 | `sum(purchase_amount)` — 일별 매출 합계 |
| `avg(col)` | 평균 | `avg(session_duration)` — 평균 세션 시간 |
| `min(col)` | 최솟값 | `min(login_time)` — 첫 접속 시각 |
| `max(col)` | 최댓값 | `max(level)` — 최고 레벨 플레이어 |
| `median(col)` | 중앙값 | `median(damage)` — 피해량 중앙값 |
| `stddev(col)` | 표준편차 | `stddev(play_time)` — 플레이 시간 편차 |
| `variance(col)` | 분산 | `variance(revenue)` — 매출 분산 |
| `percentile_cont(p) WITHIN GROUP (ORDER BY col)` | p분위수 | `percentile_cont(0.95) WITHIN GROUP (ORDER BY latency)` — 95th 퍼센타일 지연 |
| `mode() WITHIN GROUP (ORDER BY col)` | 최빈값 | `mode() WITHIN GROUP (ORDER BY item_id)` — 가장 많이 구매된 아이템 |
| `list(col)` | 값들을 리스트로 집계 | `list(item_id)` — 플레이어 보유 아이템 목록 |
| `string_agg(col, sep)` | 문자열을 구분자로 연결 | `string_agg(skill_name, ', ')` — 보유 스킬 목록 |

```sql
-- DAU/MAU 및 스티키니스 계산
SELECT
    date_trunc('month', login_date) AS month,
    COUNT(DISTINCT player_id) AS mau,
    AVG(daily_au) AS avg_dau,
    ROUND(AVG(daily_au) * 100.0 / COUNT(DISTINCT player_id), 2) AS stickiness_pct
FROM (
    SELECT
        login_date,
        COUNT(DISTINCT player_id) AS daily_au
    FROM login_logs
    GROUP BY login_date
) daily
JOIN login_logs USING (login_date)
GROUP BY month;

-- 95th 퍼센타일 응답 지연 (서버 성능 모니터링)
SELECT
    server_id,
    ROUND(AVG(response_ms), 2) AS avg_latency_ms,
    percentile_cont(0.95) WITHIN GROUP (ORDER BY response_ms) AS p95_latency_ms,
    percentile_cont(0.99) WITHIN GROUP (ORDER BY response_ms) AS p99_latency_ms
FROM server_metrics
GROUP BY server_id;
```

---

### A.5 윈도우 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `row_number() OVER (...)` | 각 행에 고유 번호 부여 | `row_number() OVER (ORDER BY score DESC)` — 전체 순위 |
| `rank() OVER (...)` | 동점 허용 순위 (공동 순위 후 건너뜀) | `rank() OVER (PARTITION BY server_id ORDER BY score DESC)` — 서버별 순위 |
| `dense_rank() OVER (...)` | 동점 허용 순위 (건너뜀 없음) | `dense_rank() OVER (ORDER BY level DESC)` — 레벨 순위 |
| `lag(col, n) OVER (...)` | n행 이전 값 | `lag(daily_revenue, 1) OVER (ORDER BY date)` — 전일 매출 |
| `lead(col, n) OVER (...)` | n행 이후 값 | `lead(login_time, 1) OVER (PARTITION BY player_id ORDER BY login_time)` — 다음 접속 시각 |
| `sum(col) OVER (...)` | 누적/이동 합계 | `sum(revenue) OVER (ORDER BY date ROWS UNBOUNDED PRECEDING)` — 누적 매출 |
| `avg(col) OVER (...)` | 이동 평균 | `avg(dau) OVER (ORDER BY date ROWS 6 PRECEDING)` — 7일 이동 평균 |
| `first_value(col) OVER (...)` | 파티션 내 첫 번째 값 | `first_value(login_time) OVER (PARTITION BY player_id ORDER BY login_time)` — 첫 로그인 |
| `last_value(col) OVER (...)` | 파티션 내 마지막 값 | `last_value(level) OVER (PARTITION BY player_id ORDER BY login_time ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING)` — 최근 레벨 |

```sql
-- 서버별 상위 10명 플레이어 순위
SELECT *
FROM (
    SELECT
        server_id,
        player_id,
        total_score,
        rank() OVER (PARTITION BY server_id ORDER BY total_score DESC) AS server_rank
    FROM player_scores
) ranked
WHERE server_rank <= 10;

-- 일별 매출과 전일 대비 증감률
SELECT
    date,
    daily_revenue,
    lag(daily_revenue, 1) OVER (ORDER BY date) AS prev_revenue,
    ROUND(
        (daily_revenue - lag(daily_revenue, 1) OVER (ORDER BY date))
        * 100.0 / lag(daily_revenue, 1) OVER (ORDER BY date),
        2
    ) AS growth_rate_pct
FROM daily_revenue_summary
ORDER BY date;

-- 7일 이동 평균 DAU (추세 파악)
SELECT
    date,
    dau,
    ROUND(avg(dau) OVER (ORDER BY date ROWS BETWEEN 6 PRECEDING AND CURRENT ROW), 0) AS dau_7day_avg
FROM daily_active_users
ORDER BY date;
```

---

### A.6 JSON 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `json_extract(json, path)` | JSON에서 값 추출 (JSON 타입 반환) | `json_extract(event_data, '$.item_id')` — 이벤트 데이터에서 아이템 ID 추출 |
| `json_extract_string(json, path)` | JSON에서 문자열 추출 | `json_extract_string(event_data, '$.player_name')` — 플레이어 이름 추출 |
| `json_array_length(json)` | JSON 배열 길이 | `json_array_length(inventory_json)` — 인벤토리 아이템 수 |
| `json_keys(json)` | JSON 객체의 키 목록 | `json_keys(stats_json)` — 통계 필드 목록 확인 |
| `to_json(value)` | 값을 JSON 문자열로 변환 | `to_json(item_list)` — 리스트를 JSON으로 직렬화 |
| `json_object(k, v, ...)` | 키-값으로 JSON 객체 생성 | `json_object('player_id', player_id, 'score', score)` — JSON 응답 생성 |

```sql
-- JSONL 로그에서 이벤트 타입별 집계
SELECT
    json_extract_string(raw_log, '$.event_type') AS event_type,
    json_extract_string(raw_log, '$.server_id') AS server_id,
    COUNT(*) AS event_count
FROM raw_event_logs
GROUP BY event_type, server_id
ORDER BY event_count DESC;

-- 전투 결과 JSON에서 데미지 통계 추출
SELECT
    player_id,
    AVG(CAST(json_extract_string(battle_result, '$.damage_dealt') AS DOUBLE)) AS avg_damage,
    SUM(CAST(json_extract_string(battle_result, '$.kills') AS INTEGER)) AS total_kills
FROM battle_logs
WHERE json_extract_string(battle_result, '$.result') = 'WIN'
GROUP BY player_id;
```

---

### A.7 리스트/배열 함수

| 함수명 | 설명 | 게임 분석 예시 |
|--------|------|----------------|
| `list_aggregate(list, func)` | 리스트에 집계 함수 적용 | `list_aggregate(damage_list, 'sum')` — 데미지 리스트 합산 |
| `list_filter(list, lambda)` | 조건에 맞는 요소만 필터 | `list_filter(item_ids, x -> x > 1000)` — 고급 아이템만 필터 |
| `list_transform(list, lambda)` | 리스트 각 요소 변환 | `list_transform(prices, x -> x * 1.1)` — 가격 10% 인상 |
| `list_contains(list, val)` | 리스트에 특정 값 포함 여부 | `list_contains(skill_ids, 99)` — 특정 스킬 보유 여부 |
| `list_sort(list)` | 리스트 정렬 | `list_sort(level_history)` — 레벨 이력 정렬 |
| `unnest(list)` | 리스트를 행으로 펼침 | `unnest(purchased_items)` — 구매 아이템 목록 전개 |

```sql
-- 플레이어별 구매 아이템 목록 집계 후 고급 아이템 필터
SELECT
    player_id,
    list_filter(
        list(item_id),
        x -> x >= 5000  -- 5000번 이상 = 레어 아이템
    ) AS rare_items
FROM purchase_logs
GROUP BY player_id;

-- 리스트를 행으로 펼쳐 아이템별 통계
SELECT
    item_id,
    COUNT(*) AS owner_count
FROM (
    SELECT player_id, unnest(owned_items) AS item_id
    FROM player_inventory
)
GROUP BY item_id
ORDER BY owner_count DESC
LIMIT 20;
```

---

### A.8 타입 변환

| 함수/연산자 | 설명 | 게임 분석 예시 |
|-------------|------|----------------|
| `CAST(val AS type)` | 명시적 타입 변환 (실패 시 오류) | `CAST(player_id AS BIGINT)` — 문자열 ID를 정수로 변환 |
| `TRY_CAST(val AS type)` | 타입 변환 시도 (실패 시 NULL 반환) | `TRY_CAST(amount AS DECIMAL(18,2))` — 잘못된 금액 데이터 NULL 처리 |
| `typeof(val)` | 값의 DuckDB 타입명 반환 | `typeof(event_data)` — 컬럼 타입 확인 |
| `val::type` | `::` 연산자로 타입 변환 (PostgreSQL 방언) | `'2024-01-01'::DATE` — 날짜 리터럴 변환 |

```sql
-- 외부 데이터 임포트 시 타입 안전 변환
SELECT
    TRY_CAST(raw_player_id AS BIGINT) AS player_id,
    TRY_CAST(raw_amount AS DECIMAL(18, 2)) AS purchase_amount,
    TRY_CAST(raw_time AS TIMESTAMP) AS event_time
FROM raw_import_table
WHERE TRY_CAST(raw_player_id AS BIGINT) IS NOT NULL;  -- 변환 실패 행 제외

-- :: 연산자 활용 (간결한 표현)
SELECT *
FROM game_events
WHERE event_time BETWEEN '2024-01-01'::TIMESTAMP AND '2024-01-31'::TIMESTAMP;
```

---

## 부록 B. DuckDB.NET API 레퍼런스

### B.1 주요 클래스 목록

| 클래스명 | 네임스페이스 | 설명 |
|----------|-------------|------|
| `DuckDBConnection` | `DuckDB.NET.Data` | DuckDB 데이터베이스 연결 객체 |
| `DuckDBCommand` | `DuckDB.NET.Data` | SQL 명령 실행 객체 |
| `DuckDBDataReader` | `DuckDB.NET.Data` | 쿼리 결과 읽기 객체 |
| `DuckDBTransaction` | `DuckDB.NET.Data` | 트랜잭션 관리 객체 |
| `DuckDBAppender` | `DuckDB.NET.Data` | 고성능 대량 데이터 적재 객체 |

---

### B.2 DuckDBConnection 주요 멤버

| 멤버 | 종류 | 설명 |
|------|------|------|
| `DuckDBConnection(connectionString)` | 생성자 | 연결 문자열로 인스턴스 생성 |
| `Open()` | 메서드 | 데이터베이스 연결 열기 |
| `OpenAsync()` | 메서드 | 비동기 연결 열기 |
| `Close()` | 메서드 | 연결 닫기 |
| `CreateCommand()` | 메서드 | `DuckDBCommand` 인스턴스 생성 |
| `BeginTransaction()` | 메서드 | 트랜잭션 시작 |
| `BeginTransactionAsync()` | 메서드 | 비동기 트랜잭션 시작 |
| `CreateAppender(table)` | 메서드 | 지정 테이블에 `DuckDBAppender` 생성 |
| `State` | 프로퍼티 | 현재 연결 상태 (`ConnectionState`) |
| `ConnectionString` | 프로퍼티 | 연결 문자열 |
| `Database` | 프로퍼티 | 연결된 데이터베이스 이름 |

---

### B.3 DuckDBCommand 주요 멤버

| 멤버 | 종류 | 설명 |
|------|------|------|
| `DuckDBCommand(sql, connection)` | 생성자 | SQL과 연결 객체로 인스턴스 생성 |
| `CommandText` | 프로퍼티 | 실행할 SQL 문자열 |
| `Parameters` | 프로퍼티 | 파라미터 컬렉션 |
| `ExecuteNonQuery()` | 메서드 | INSERT/UPDATE/DELETE 등 결과 없는 SQL 실행 |
| `ExecuteNonQueryAsync()` | 메서드 | 비동기 ExecuteNonQuery |
| `ExecuteScalar()` | 메서드 | 단일 값 반환 쿼리 실행 |
| `ExecuteScalarAsync()` | 메서드 | 비동기 ExecuteScalar |
| `ExecuteReader()` | 메서드 | `DuckDBDataReader` 반환 |
| `ExecuteReaderAsync()` | 메서드 | 비동기 ExecuteReader |

---

### B.4 DuckDBDataReader 주요 멤버

| 멤버 | 종류 | 설명 |
|------|------|------|
| `Read()` | 메서드 | 다음 행으로 이동 (행이 있으면 `true`) |
| `ReadAsync()` | 메서드 | 비동기 Read |
| `GetInt32(ordinal)` | 메서드 | 지정 컬럼을 `int`로 읽기 |
| `GetInt64(ordinal)` | 메서드 | 지정 컬럼을 `long`으로 읽기 |
| `GetDouble(ordinal)` | 메서드 | 지정 컬럼을 `double`로 읽기 |
| `GetString(ordinal)` | 메서드 | 지정 컬럼을 `string`으로 읽기 |
| `GetDateTime(ordinal)` | 메서드 | 지정 컬럼을 `DateTime`으로 읽기 |
| `GetDecimal(ordinal)` | 메서드 | 지정 컬럼을 `decimal`로 읽기 |
| `GetBoolean(ordinal)` | 메서드 | 지정 컬럼을 `bool`로 읽기 |
| `IsDBNull(ordinal)` | 메서드 | NULL 여부 확인 |
| `GetOrdinal(name)` | 메서드 | 컬럼 이름으로 인덱스 조회 |
| `FieldCount` | 프로퍼티 | 결과 컬럼 수 |
| `HasRows` | 프로퍼티 | 결과 행 존재 여부 |

---

### B.5 DuckDBAppender 주요 멤버

| 멤버 | 종류 | 설명 |
|------|------|------|
| `AppendRow(params object[] values)` | 메서드 | 한 행의 값을 순서대로 추가 |
| `Flush()` | 메서드 | 버퍼에 쌓인 데이터를 DB에 반영 |
| `Close()` | 메서드 | Appender를 닫고 Flush 수행 |
| `Dispose()` | 메서드 | 리소스 해제 (Close 포함) |

---

### B.6 연결 문자열 옵션

| 옵션 | 설명 | 예시 |
|------|------|------|
| `Data Source` | DB 파일 경로. `:memory:` 지정 시 인메모리 | `Data Source=game.duckdb` |
| `Data Source=:memory:` | 인메모리 DB 사용 | `Data Source=:memory:` |
| `threads` | 사용할 스레드 수 | `threads=4` |
| `memory_limit` | 최대 메모리 사용량 | `memory_limit=4GB` |
| `read_only` | 읽기 전용 모드 | `read_only=true` |

```csharp
// 파일 기반 DB 연결
var connectionString = "Data Source=C:/GameLogs/analytics.duckdb";

// 인메모리 DB (테스트용)
var memConnectionString = "Data Source=:memory:";

// 읽기 전용으로 열기 (리포트 서버)
var readOnlyString = "Data Source=C:/GameLogs/analytics.duckdb;read_only=true";
```

---

### B.7 DuckDB 타입 ↔ C# 타입 매핑

| DuckDB 타입 | C# 타입 | 비고 |
|-------------|---------|------|
| `BOOLEAN` | `bool` | |
| `TINYINT` | `sbyte` | 8비트 정수 |
| `SMALLINT` | `short` | 16비트 정수 |
| `INTEGER` | `int` | 32비트 정수 |
| `BIGINT` | `long` | 64비트 정수 |
| `HUGEINT` | `System.Numerics.BigInteger` | 128비트 정수 |
| `FLOAT` | `float` | 단정밀도 부동소수점 |
| `DOUBLE` | `double` | 배정밀도 부동소수점 |
| `DECIMAL(p, s)` | `decimal` | 고정밀 소수 |
| `VARCHAR` | `string` | 가변 길이 문자열 |
| `DATE` | `DateOnly` | .NET 6+ |
| `TIME` | `TimeOnly` | .NET 6+ |
| `TIMESTAMP` | `DateTime` | |
| `TIMESTAMP WITH TIME ZONE` | `DateTimeOffset` | |
| `INTERVAL` | `TimeSpan` | |
| `BLOB` | `byte[]` | 이진 데이터 |
| `UUID` | `Guid` | |
| `JSON` | `string` | JSON 문자열로 반환 |
| `LIST` | `List<T>` | 원소 타입 T에 따라 다름 |

---

### B.8 자주 사용하는 코드 패턴

**패턴 1: 기본 쿼리 실행 및 결과 읽기**

```csharp
using DuckDB.NET.Data;

await using var connection = new DuckDBConnection("Data Source=game.duckdb");
await connection.OpenAsync();

await using var command = connection.CreateCommand();
command.CommandText = @"
    SELECT player_id, SUM(purchase_amount) AS total_spent
    FROM purchase_logs
    WHERE log_date >= current_date - INTERVAL 30 DAY
    GROUP BY player_id
    ORDER BY total_spent DESC
    LIMIT 100
";

await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    long playerId = reader.GetInt64(0);
    decimal totalSpent = reader.GetDecimal(1);
    Console.WriteLine($"Player {playerId}: {totalSpent:C}");
}
```

**패턴 2: 파라미터 바인딩으로 안전한 쿼리**

```csharp
await using var command = connection.CreateCommand();
command.CommandText = @"
    SELECT *
    FROM game_events
    WHERE player_id = $playerId
      AND event_time >= $startDate
      AND event_time <  $endDate
";

command.Parameters.Add(new DuckDBParameter("playerId", playerId));
command.Parameters.Add(new DuckDBParameter("startDate", startDate));
command.Parameters.Add(new DuckDBParameter("endDate", endDate));

await using var reader = await command.ExecuteReaderAsync();
```

**패턴 3: Appender를 이용한 고성능 대량 적재**

```csharp
using DuckDB.NET.Data;

await using var connection = new DuckDBConnection("Data Source=game.duckdb");
await connection.OpenAsync();

// 테이블 생성
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS battle_logs (
            log_id      BIGINT,
            player_id   BIGINT,
            event_time  TIMESTAMP,
            damage      INTEGER,
            result      VARCHAR
        )
    ";
    await cmd.ExecuteNonQueryAsync();
}

// Appender로 대량 삽입
using var appender = connection.CreateAppender("battle_logs");
foreach (var log in battleLogs)  // battleLogs: IEnumerable<BattleLog>
{
    appender.AppendRow(
        log.LogId,
        log.PlayerId,
        log.EventTime,
        log.Damage,
        log.Result
    );
}
appender.Close();  // 내부적으로 Flush 후 Close
```

**패턴 4: 트랜잭션 처리**

```csharp
await using var connection = new DuckDBConnection("Data Source=game.duckdb");
await connection.OpenAsync();

await using var transaction = await connection.BeginTransactionAsync();
try
{
    await using var cmd = connection.CreateCommand();

    cmd.CommandText = "UPDATE player_wallet SET balance = balance - $amount WHERE player_id = $pid";
    cmd.Parameters.Add(new DuckDBParameter("amount", purchaseAmount));
    cmd.Parameters.Add(new DuckDBParameter("pid", playerId));
    await cmd.ExecuteNonQueryAsync();

    cmd.CommandText = "INSERT INTO purchase_logs (player_id, item_id, amount) VALUES ($pid, $itemId, $amount)";
    cmd.Parameters.Add(new DuckDBParameter("itemId", itemId));
    await cmd.ExecuteNonQueryAsync();

    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    Console.Error.WriteLine($"트랜잭션 실패: {ex.Message}");
    throw;
}
```

**패턴 5: Parquet 파일 직접 쿼리 및 결과를 C# 컬렉션으로 변환**

```csharp
await using var connection = new DuckDBConnection("Data Source=:memory:");
await connection.OpenAsync();

await using var command = connection.CreateCommand();
command.CommandText = @"
    SELECT
        date_trunc('day', event_time) AS log_date,
        COUNT(DISTINCT player_id) AS dau
    FROM read_parquet('C:/GameLogs/2024/**/*.parquet')
    GROUP BY log_date
    ORDER BY log_date
";

var results = new List<(DateOnly Date, long Dau)>();
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var date = DateOnly.FromDateTime(reader.GetDateTime(0));
    var dau = reader.GetInt64(1);
    results.Add((date, dau));
}

return results;
```

---

## 부록 C. 로그 스키마 설계 체크리스트

### C.1 공통 필드 체크리스트

모든 게임 로그 테이블에 반드시 포함해야 할 필드 목록입니다.

| 필드명 | 타입 | 필수 여부 | 설명 |
|--------|------|-----------|------|
| `log_id` | `BIGINT` | 권장 | 로그 고유 식별자 (자동 증가 또는 UUID) |
| `event_time` | `TIMESTAMP` | **필수** | 이벤트 발생 시각 (UTC 기준) |
| `server_time` | `TIMESTAMP` | 권장 | 서버 수신 시각 (event_time과의 차이로 지연 감지) |
| `player_id` | `BIGINT` | **필수** | 플레이어 고유 ID |
| `session_id` | `VARCHAR(36)` | 권장 | 세션 식별자 (UUID 형식) |
| `server_id` | `VARCHAR(32)` | **필수** | 서버 식별자 |
| `channel_id` | `VARCHAR(32)` | 권장 | 채널/존 식별자 |
| `event_type` | `VARCHAR(64)` | **필수** | 이벤트 종류 (열거형 문자열) |
| `client_version` | `VARCHAR(16)` | 권장 | 클라이언트 버전 |
| `platform` | `VARCHAR(16)` | 권장 | 플랫폼 (PC, Mobile, Console 등) |
| `country_code` | `CHAR(2)` | 권장 | ISO 3166-1 alpha-2 국가 코드 |

**체크 항목:**
- [ ] `event_time`은 UTC로 저장되는가?
- [ ] `player_id`는 BIGINT(64비트)로 충분한가? (32비트 INT는 약 21억 명 한계)
- [ ] `server_id`는 일관된 명명 규칙을 따르는가?
- [ ] `event_type`은 열거형으로 관리되어 오타가 방지되는가?
- [ ] 비로그인 상태(게스트)의 `player_id`는 어떻게 처리하는가?

---

### C.2 이벤트 타입별 권장 필드 목록

#### 접속 이벤트 (LOGIN / LOGOUT)

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `event_type` | `VARCHAR` | `LOGIN` 또는 `LOGOUT` |
| `ip_address` | `VARCHAR(45)` | IPv4/IPv6 주소 |
| `device_id` | `VARCHAR(64)` | 기기 고유 ID |
| `os_version` | `VARCHAR(32)` | OS 버전 |
| `network_type` | `VARCHAR(16)` | WiFi / LTE / 5G 등 |
| `login_type` | `VARCHAR(32)` | 소셜 로그인 종류 (Google, Apple 등) |
| `session_duration_sec` | `INTEGER` | (LOGOUT 시) 세션 지속 시간 (초) |
| `play_time_today_sec` | `INTEGER` | (LOGOUT 시) 당일 누적 플레이 시간 (초) |

**체크 항목:**
- [ ] 비정상 종료(CRASH)와 정상 로그아웃(LOGOUT)을 구분하는가?
- [ ] 멀티 기기 동시 접속 정책이 로그에 반영되는가?

#### 전투 이벤트 (BATTLE_START / BATTLE_END)

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `battle_id` | `BIGINT` | 전투 고유 ID |
| `battle_type` | `VARCHAR(32)` | PvP / PvE / Boss 등 |
| `map_id` | `INTEGER` | 맵 식별자 |
| `result` | `VARCHAR(8)` | WIN / LOSE / DRAW |
| `duration_sec` | `INTEGER` | 전투 지속 시간 (초) |
| `damage_dealt` | `INTEGER` | 가한 총 피해량 |
| `damage_taken` | `INTEGER` | 받은 총 피해량 |
| `kills` | `SMALLINT` | 처치 수 |
| `deaths` | `SMALLINT` | 사망 수 |
| `exp_gained` | `INTEGER` | 획득 경험치 |
| `gold_gained` | `INTEGER` | 획득 골드 |
| `opponent_id` | `BIGINT` | (PvP) 상대 플레이어 ID |

**체크 항목:**
- [ ] 전투 시작(START)과 종료(END) 이벤트가 쌍으로 존재하는가?
- [ ] `battle_id`로 START-END 매칭이 가능한가?

#### 아이템 이벤트 (ITEM_ACQUIRE / ITEM_USE / ITEM_SELL)

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `item_id` | `INTEGER` | 아이템 종류 ID |
| `item_uid` | `BIGINT` | 아이템 인스턴스 고유 ID |
| `item_count` | `INTEGER` | 수량 |
| `item_grade` | `TINYINT` | 등급 (1=일반, 2=희귀, 3=영웅, 4=전설) |
| `acquire_type` | `VARCHAR(32)` | 획득 경로 (MONSTER_DROP, CRAFT, PURCHASE 등) |
| `before_count` | `INTEGER` | 처리 전 보유 수량 |
| `after_count` | `INTEGER` | 처리 후 보유 수량 |

**체크 항목:**
- [ ] `before_count`와 `after_count`의 차이가 `item_count`와 일치하는가?
- [ ] 아이템 소멸 경로(판매, 분해, 소비 등)가 명확히 구분되는가?

#### 결제 이벤트 (PURCHASE)

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `transaction_id` | `VARCHAR(64)` | 결제 플랫폼 트랜잭션 ID |
| `product_id` | `VARCHAR(64)` | 상품 식별자 |
| `product_name` | `VARCHAR(128)` | 상품명 |
| `amount_local` | `DECIMAL(18,2)` | 현지 통화 금액 |
| `currency_code` | `CHAR(3)` | ISO 4217 통화 코드 (USD, KRW 등) |
| `amount_usd` | `DECIMAL(18,4)` | USD 환산 금액 |
| `platform` | `VARCHAR(16)` | 결제 플랫폼 (Apple, Google, Steam 등) |
| `is_first_purchase` | `BOOLEAN` | 첫 결제 여부 |
| `cumulative_spending` | `DECIMAL(18,2)` | 누적 결제 금액 (USD) |

**체크 항목:**
- [ ] `transaction_id`로 결제 플랫폼 원장과 대조 가능한가?
- [ ] 환불 처리 시 별도 REFUND 이벤트가 존재하는가?
- [ ] 통화 코드와 환율 환산 기준이 명확히 정의되는가?

#### 오류 이벤트 (ERROR / EXCEPTION)

| 필드명 | 타입 | 설명 |
|--------|------|------|
| `error_code` | `VARCHAR(32)` | 오류 코드 |
| `error_message` | `VARCHAR(512)` | 오류 메시지 |
| `stack_trace` | `TEXT` | 스택 트레이스 (선택) |
| `severity` | `VARCHAR(8)` | INFO / WARN / ERROR / FATAL |
| `context_data` | `JSON` | 오류 발생 당시 컨텍스트 정보 |

---

### C.3 데이터 타입 선택 가이드라인

| 데이터 종류 | 권장 타입 | 이유 |
|-------------|-----------|------|
| 플레이어 ID, 길드 ID | `BIGINT` | 32비트 INT는 약 21억 한계, 미래 확장성 확보 |
| 아이템 ID, 맵 ID | `INTEGER` | 일반적으로 32비트 범위 충분 |
| 등급, 레벨 (소규모) | `TINYINT` (1바이트) 또는 `SMALLINT` | 범위가 작을 때 스토리지 절약 |
| 금액, 결제액 | `DECIMAL(18,2)` | 부동소수점 오차 방지 (금융 데이터) |
| 좌표 (위치) | `FLOAT` 또는 `DOUBLE` | 게임 좌표는 FLOAT로 충분 |
| 승률, 확률 | `DOUBLE` | 계산 정밀도 필요 시 DOUBLE |
| 이벤트 타입, 국가 코드 | `VARCHAR` (고정 or 최대 길이 지정) | 열거형 코드는 길이 상한 지정 권장 |
| 타임스탬프 | `TIMESTAMP` (UTC) | 항상 UTC 기준, 타임존 정보 별도 저장 |
| 플래그, 불린 | `BOOLEAN` | True/False 명확한 의미 |
| JSON 구조체 | `JSON` 또는 `VARCHAR` | 반정형 데이터, 쿼리 빈도 낮을 때 |
| UUID | `UUID` 또는 `VARCHAR(36)` | DuckDB UUID 타입 활용 권장 |

---

### C.4 파일 포맷별 권장 사항

| 포맷 | 권장 용도 | 장점 | 주의 사항 |
|------|-----------|------|-----------|
| **Parquet** | 장기 보관, 분석 쿼리 | 높은 압축률, 컬럼 지향, DuckDB 최적 지원 | 실시간 쓰기 불가, 배치 변환 필요 |
| **JSONL** | 실시간 수집, 로그 스트리밍 | 스키마 유연, 사람이 읽기 쉬움 | 파일 크기 큼, 쿼리 속도 느림 |
| **CSV** | 간단한 데이터 교환 | 범용성 높음 | 타입 정보 없음, 특수문자 이스케이프 필요 |
| **DuckDB 파일** | 실시간 분석 DB | 빠른 쿼리, 인덱스 지원 | 단일 프로세스 쓰기 제한 |

**파일 포맷 선택 체크 항목:**
- [ ] 실시간 수집은 JSONL → 배치 변환 → Parquet 파이프라인을 구성했는가?
- [ ] Parquet 파일의 파티션 전략(날짜/서버별)이 쿼리 패턴과 일치하는가?
- [ ] CSV 사용 시 인코딩(UTF-8)과 구분자 규칙이 문서화되었는가?

---

### C.5 로테이션 정책 체크리스트

- [ ] 일별 로테이션: 파일명에 날짜 포함 (`game_events_20240101.parquet`)
- [ ] 월별 아카이브: 30일 이상 데이터는 콜드 스토리지로 이동
- [ ] 압축 정책: Parquet은 Snappy 또는 ZSTD 압축 적용
- [ ] 보존 기간: 원본 로그는 최소 1년, 집계 데이터는 3년 이상 보존
- [ ] 파티션 구조: `logs/YYYY/MM/DD/server_id/` 형식 권장
- [ ] 파일 크기: 단일 Parquet 파일은 128MB~1GB 범위 유지 (너무 작으면 메타데이터 오버헤드)
- [ ] 백업: 중요 집계 데이터는 별도 위치에 백업

---

### C.6 DuckDB 적재 최적화 체크리스트

- [ ] **Appender 사용**: 대량 INSERT 시 `DuckDBAppender` 사용 (최소 10배 이상 빠름)
- [ ] **트랜잭션 배치**: 다수의 INSERT를 단일 트랜잭션으로 묶기
- [ ] **Parquet 직접 쿼리**: ETL 없이 `read_parquet()` 활용 검토
- [ ] **컬럼 순서 최적화**: 자주 필터링하는 컬럼을 앞쪽에 배치
- [ ] **파티션 정렬**: `ORDER BY` 기준 컬럼(보통 event_time)으로 물리 정렬
- [ ] **스레드 설정**: `SET threads = N` 으로 병렬 처리 코어 수 설정
- [ ] **메모리 제한**: `SET memory_limit = 'XGB'` 으로 OOM 방지
- [ ] **통계 업데이트**: 대량 적재 후 `ANALYZE` 실행하여 쿼리 플랜 최적화

---

### C.7 실수하기 쉬운 함정 목록

| 함정 | 증상 | 해결책 |
|------|------|--------|
| **Timestamp 타임존 혼용** | 서버별로 시각이 달라 집계 오류 | 모든 타임스탬프를 UTC로 통일. 클라이언트 현지 시간은 별도 컬럼에 저장 |
| **player_id INT32 오버플로우** | 21억 명 이상에서 ID 중복/음수 발생 | 초기 설계부터 BIGINT(64비트) 사용 |
| **CSV 특수문자 이스케이프 누락** | 플레이어 이름의 쉼표, 따옴표로 파싱 오류 | RFC 4180 준수, 모든 CSV 필드 따옴표로 감싸기 |
| **NULL과 빈 문자열 혼용** | 집계 함수에서 예기치 않은 결과 | NULL 정책 명확히 정의 (미입력=NULL, 의도적 없음=빈 문자열) |
| **부동소수점 금액 저장** | 1.1 + 2.2 = 3.3000000000000003 오류 | 금액은 반드시 `DECIMAL(18,2)` 사용 |
| **이벤트 타입 대소문자 혼용** | `Login`, `LOGIN`, `login` 이 별개 집계됨 | 이벤트 타입은 대문자 상수로 표준화 |
| **로그 중복 수집** | 네트워크 재전송으로 동일 이벤트 2회 기록 | `log_id` 또는 `transaction_id` 기반 중복 제거 로직 구현 |
| **배치 적재 시 트랜잭션 미사용** | 적재 중단 시 절반만 들어간 데이터 발생 | 배치 단위로 트랜잭션 적용 |
| **Parquet 파일 너무 잦은 소규모 생성** | 수천 개의 작은 파일로 메타데이터 쿼리 느려짐 | 파일 병합(Compaction) 주기적으로 실행 |
| **player_id 없는 이벤트 로그** | 비로그인 이벤트에서 player_id = 0 또는 NULL | 게스트 ID 정책 명확히 정의, 별도 임시 ID 부여 |
| **서버 시각과 클라이언트 시각 혼용** | 클라이언트 시각 조작으로 통계 오염 | `event_time`(클라이언트)과 `server_time`(서버) 구분, 분석엔 `server_time` 기준 |

---

## 부록 D. 참고 자료 및 공식 문서 링크

### D.1 DuckDB 공식 문서

| 항목 | URL |
|------|-----|
| DuckDB 공식 홈페이지 | https://duckdb.org |
| DuckDB 공식 문서 (최신) | https://duckdb.org/docs/stable/ |
| SQL 함수 레퍼런스 | https://duckdb.org/docs/stable/sql/functions/overview |
| 데이터 임포트/익스포트 | https://duckdb.org/docs/stable/data/overview |
| Parquet 지원 | https://duckdb.org/docs/stable/data/parquet/overview |
| JSON 지원 | https://duckdb.org/docs/stable/data/json/overview |
| 윈도우 함수 | https://duckdb.org/docs/stable/sql/window_functions |
| DuckDB GitHub 저장소 | https://github.com/duckdb/duckdb |
| DuckDB 릴리즈 노트 | https://github.com/duckdb/duckdb/releases |
| DuckDB 블로그 | https://duckdb.org/news/ |

### D.2 DuckDB.NET 관련

| 항목 | URL |
|------|-----|
| DuckDB.NET GitHub 저장소 | https://github.com/Giorgi/DuckDB.NET |
| DuckDB.NET NuGet 패키지 | https://www.nuget.org/packages/DuckDB.NET.Data |
| DuckDB.NET 문서 및 예제 | https://github.com/Giorgi/DuckDB.NET/wiki |

### D.3 관련 생태계 및 도구

| 항목 | URL |
|------|-----|
| Apache Parquet 공식 사이트 | https://parquet.apache.org |
| Apache Arrow 공식 사이트 | https://arrow.apache.org |
| .NET 9 공식 문서 | https://learn.microsoft.com/dotnet/ |
| MotherDuck (DuckDB 클라우드) | https://motherduck.com |

---

### D.4 주요 개념 용어 사전

| 용어 | 설명 |
|------|------|
| **OLAP** (Online Analytical Processing) | 대량의 데이터를 집계·분석하기 위한 처리 방식. 읽기 위주의 복잡한 쿼리에 최적화되어 있다. DuckDB는 OLAP 엔진이다. |
| **OLTP** (Online Transaction Processing) | 소량의 데이터를 빠르게 읽고 쓰는 트랜잭션 처리 방식. 게임 서버의 실시간 처리에 사용된다. MySQL, PostgreSQL, SQLite 등이 대표적이다. |
| **컬럼 지향 (Column-Oriented)** | 데이터를 행(Row) 단위가 아닌 열(Column) 단위로 저장하는 방식. 특정 컬럼만 읽는 분석 쿼리에서 I/O를 크게 줄인다. DuckDB, Parquet가 이 방식을 사용한다. |
| **벡터화 실행 (Vectorized Execution)** | 한 번에 하나의 행이 아닌, 수천 개의 행(벡터)을 묶어 처리하는 실행 방식. CPU 캐시 활용률을 높여 분석 쿼리 속도를 대폭 향상시킨다. |
| **WAL** (Write-Ahead Log) | 데이터 변경 내용을 실제 파일에 쓰기 전에 로그에 먼저 기록하는 기법. 장애 발생 시 데이터 일관성을 복구하는 데 사용된다. |
| **Parquet** | Apache에서 만든 컬럼 지향 바이너리 파일 포맷. 높은 압축률과 빠른 분석 쿼리를 지원하며, DuckDB와 궁합이 좋다. |
| **Arrow** | Apache에서 만든 인메모리 컬럼 지향 데이터 포맷. 언어/시스템 간 데이터 교환 시 직렬화 오버헤드를 없애는 것이 목적이다. DuckDB는 Arrow 포맷을 네이티브로 지원한다. |
| **JSONL** (JSON Lines) | 한 줄에 하나의 JSON 객체를 기록하는 텍스트 포맷. 스트리밍 로그 수집에 널리 쓰인다. |
| **DAU** (Daily Active Users) | 일별 활성 사용자 수. 특정 날짜에 1회 이상 접속한 고유 플레이어 수. |
| **MAU** (Monthly Active Users) | 월별 활성 사용자 수. 특정 월에 1회 이상 접속한 고유 플레이어 수. |
| **스티키니스 (Stickiness)** | DAU/MAU 비율(%). 월간 유저 중 매일 돌아오는 유저의 비율로, 게임 충성도를 나타낸다. |
| **ARPU** (Average Revenue Per User) | 사용자 1인당 평균 매출. `총 매출 / 활성 사용자 수`로 계산. |
| **ARPPU** (Average Revenue Per Paying User) | 결제 사용자 1인당 평균 매출. `총 매출 / 결제 사용자 수`로 계산. ARPU보다 높으며, 과금 유저의 지불 의향을 나타낸다. |
| **리텐션 (Retention)** | 특정 시점 이후 사용자가 얼마나 남아있는지를 나타내는 지표. D1(1일 후), D7(7일 후), D30(30일 후) 리텐션을 주로 측정한다. |
| **코호트 (Cohort)** | 특정 기간에 게임을 처음 시작한 사용자 그룹. 코호트별로 리텐션, 매출 등을 비교 분석한다. |
| **퍼널 (Funnel)** | 사용자가 특정 목표(튜토리얼 완료, 첫 결제 등)에 도달하기까지의 단계별 이탈률 분석. 각 단계의 전환율을 시각화한다. |
| **윈도우 함수 (Window Function)** | 현재 행과 관련된 행들의 집합(윈도우)에 대해 집계 계산을 수행하는 SQL 함수. `OVER (PARTITION BY ... ORDER BY ...)` 절과 함께 사용하며, 기존 행을 유지하면서 계산 결과를 추가한다. |
| **파티셔닝 (Partitioning)** | 대량의 데이터를 날짜, 서버 ID 등 특정 기준으로 물리적으로 분할 저장하는 기법. 쿼리 시 해당 파티션만 읽어 성능을 향상시킨다. |
| **컴팩션 (Compaction)** | 작은 파일 다수를 큰 파일 소수로 병합하는 작업. Parquet 파일 관리에서 파일 수가 너무 많아지면 성능 저하가 발생하므로 주기적으로 수행한다. |
| **Appender** | DuckDB.NET에서 제공하는 고성능 대량 데이터 삽입 인터페이스. 파라미터 바인딩 오버헤드 없이 버퍼를 통해 직접 적재하여 일반 INSERT 대비 수배~수십 배 빠르다. |
| **인메모리 DB** | 모든 데이터를 디스크가 아닌 RAM에 저장하는 데이터베이스 모드. DuckDB에서 `Data Source=:memory:` 로 사용하며, 테스트 또는 임시 분석에 활용된다. |
| **P95 / P99 (퍼센타일)** | 전체 데이터 중 95% 또는 99%가 해당 값 이하임을 의미. 서버 응답 지연(latency) 측정 시 극단값의 영향을 배제하고 "대부분 사용자의 경험"을 나타내는 데 사용한다. |
