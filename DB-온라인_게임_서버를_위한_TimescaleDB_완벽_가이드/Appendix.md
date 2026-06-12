# 온라인 게임 서버를 위한 TimescaleDB 완벽 가이드  

저자: 최흥배, Claude AI   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **.NET**: 9 이상
- **OS**: Windows 10 이상

-----  
  
# 부록

책의 본문에서는 개념과 예제를 통해 TimescaleDB를 배웠다. 이 부록에서는 실무에서 자주 참조하게 될 레퍼런스 자료를 정리했다. 코드를 작성하다가 "이 메서드의 정확한 문법이 뭐였지?" 또는 "이 함수의 옵션에는 어떤 것들이 있지?"라고 궁금할 때 빠르게 찾아볼 수 있도록 구성했다. 브라우저의 즐겨찾기처럼 자주 참조하게 될 섹션이다.

## Appendix A: SQLKata 레퍼런스

SQLKata는 이 책에서 C# 코드로 SQL 쿼리를 작성할 때 사용한 쿼리 빌더 라이브러리다. 타입 안전하고 가독성 높은 쿼리를 작성할 수 있게 해준다. 이 섹션에서는 자주 사용하는 패턴과 메서드를 빠르게 참조할 수 있도록 정리했다.

**기본 설정 및 초기화**

```csharp
// NuGet 패키지 설치
// Install-Package SqlKata
// Install-Package SqlKata.Execution
// Install-Package Npgsql

using SqlKata;
using SqlKata.Execution;
using SqlKata.Compilers;
using Npgsql;

// 연결 설정
var connectionString = "Host=localhost;Port=5432;Database=game_monitoring;Username=postgres;Password=password";
var connection = new NpgsqlConnection(connectionString);

// PostgreSQL 컴파일러 생성
var compiler = new PostgresCompiler();

// QueryFactory 생성 (권장 방법)
var db = new QueryFactory(connection, compiler);

// 또는 Query 객체 직접 사용
var query = new Query("table_name");
var sql = compiler.Compile(query);
```

**SELECT 쿼리**

```csharp
// 기본 SELECT
var query = new Query("server_metrics")
    .Select("timestamp", "server_id", "cpu_percent");

// 전체 컬럼 선택
var query = new Query("server_metrics").Select("*");

// 별칭(Alias) 사용
var query = new Query("server_metrics")
    .Select("cpu_percent as cpu")
    .Select("memory_percent as memory");

// 원시(Raw) SQL 사용
var query = new Query("server_metrics")
    .SelectRaw("COUNT(*) as total_count")
    .SelectRaw("AVG(cpu_percent) as avg_cpu");

// DISTINCT
var query = new Query("server_metrics")
    .Distinct()
    .Select("server_id");

// LIMIT과 OFFSET (페이징)
var query = new Query("server_metrics")
    .Select("*")
    .Limit(10)
    .Offset(20);

// 또는 Paginate 메서드 사용
var query = new Query("server_metrics")
    .Select("*")
    .ForPage(page: 3, perPage: 10); // 3페이지, 페이지당 10개
```

**WHERE 조건**

```csharp
// 기본 WHERE
var query = new Query("server_metrics")
    .Where("server_id", "game-server-01");

// 비교 연산자
var query = new Query("server_metrics")
    .Where("cpu_percent", ">", 80);

// 여러 조건 (AND)
var query = new Query("server_metrics")
    .Where("server_id", "game-server-01")
    .Where("cpu_percent", ">", 80);

// OR 조건
var query = new Query("server_metrics")
    .Where("server_id", "game-server-01")
    .OrWhere("server_id", "game-server-02");

// IN 조건
var serverIds = new[] { "server-01", "server-02", "server-03" };
var query = new Query("server_metrics")
    .WhereIn("server_id", serverIds);

// NOT IN
var query = new Query("server_metrics")
    .WhereNotIn("server_id", serverIds);

// BETWEEN
var query = new Query("server_metrics")
    .WhereBetween("cpu_percent", 50, 80);

// NULL 체크
var query = new Query("server_metrics")
    .WhereNull("error_message");

var query = new Query("server_metrics")
    .WhereNotNull("error_message");

// LIKE 패턴 매칭
var query = new Query("game_logs")
    .WhereLike("message", "%error%");

// 날짜/시간 범위
var startDate = DateTime.UtcNow.AddHours(-24);
var endDate = DateTime.UtcNow;
var query = new Query("server_metrics")
    .WhereBetween("timestamp", startDate, endDate);

// 또는 비교 연산자로
var query = new Query("server_metrics")
    .Where("timestamp", ">=", startDate)
    .Where("timestamp", "<=", endDate);

// 원시 WHERE (복잡한 조건)
var query = new Query("server_metrics")
    .WhereRaw("timestamp > NOW() - INTERVAL '1 hour'");

// 파라미터를 사용한 원시 WHERE (SQL Injection 방지)
var query = new Query("server_metrics")
    .WhereRaw("cpu_percent > ? AND memory_percent > ?", 80, 70);

// 중첩 WHERE (괄호 사용)
var query = new Query("server_metrics")
    .Where(q => q
        .Where("server_id", "server-01")
        .OrWhere("server_id", "server-02")
    )
    .Where("cpu_percent", ">", 80);
// 결과: WHERE (server_id = 'server-01' OR server_id = 'server-02') AND cpu_percent > 80
```

**JOIN**

```csharp
// INNER JOIN
var query = new Query("game_logs")
    .Join("players", "game_logs.player_id", "players.player_id")
    .Select("game_logs.*", "players.username");

// LEFT JOIN
var query = new Query("game_logs")
    .LeftJoin("players", "game_logs.player_id", "players.player_id");

// RIGHT JOIN
var query = new Query("game_logs")
    .RightJoin("players", "game_logs.player_id", "players.player_id");

// 여러 조건의 JOIN
var query = new Query("game_logs")
    .Join("players", j => j
        .On("game_logs.player_id", "players.player_id")
        .On("game_logs.server_id", "players.server_id")
    );

// 원시 JOIN
var query = new Query("game_logs")
    .JoinRaw("INNER JOIN players ON game_logs.player_id = players.player_id");

// 서브쿼리 JOIN
var subQuery = new Query("players")
    .Select("player_id", "username")
    .Where("level", ">", 50);

var query = new Query("game_logs")
    .Join(subQuery.As("high_level_players"), "game_logs.player_id", "high_level_players.player_id");
```

**ORDER BY**

```csharp
// 오름차순 정렬
var query = new Query("server_metrics")
    .OrderBy("timestamp");

// 내림차순 정렬
var query = new Query("server_metrics")
    .OrderByDesc("timestamp");

// 여러 컬럼 정렬
var query = new Query("server_metrics")
    .OrderBy("server_id")
    .OrderByDesc("timestamp");

// 원시 ORDER BY
var query = new Query("server_metrics")
    .OrderByRaw("cpu_percent DESC NULLS LAST");
```

**GROUP BY 및 집계**

```csharp
// 기본 GROUP BY
var query = new Query("server_metrics")
    .Select("server_id")
    .SelectRaw("AVG(cpu_percent) as avg_cpu")
    .GroupBy("server_id");

// 여러 컬럼 GROUP BY
var query = new Query("game_logs")
    .Select("server_id", "event_type")
    .SelectRaw("COUNT(*) as event_count")
    .GroupBy("server_id", "event_type");

// HAVING 조건
var query = new Query("server_metrics")
    .Select("server_id")
    .SelectRaw("AVG(cpu_percent) as avg_cpu")
    .GroupBy("server_id")
    .Having("avg_cpu", ">", 50);

// 또는 HavingRaw
var query = new Query("server_metrics")
    .Select("server_id")
    .SelectRaw("COUNT(*) as metric_count")
    .GroupBy("server_id")
    .HavingRaw("COUNT(*) > 1000");

// 집계 함수들
var query = new Query("server_metrics")
    .SelectRaw("COUNT(*) as total_count")
    .SelectRaw("AVG(cpu_percent) as avg_cpu")
    .SelectRaw("MAX(cpu_percent) as max_cpu")
    .SelectRaw("MIN(cpu_percent) as min_cpu")
    .SelectRaw("SUM(network_in_bytes) as total_network_in");
```

**INSERT**

```csharp
// 단일 행 삽입
var data = new
{
    timestamp = DateTime.UtcNow,
    server_id = "game-server-01",
    cpu_percent = 75.5,
    memory_percent = 60.2
};

var id = await db.Query("server_metrics").InsertAsync(data);

// 여러 행 삽입
var dataList = new[]
{
    new { timestamp = DateTime.UtcNow, server_id = "server-01", cpu_percent = 75.5 },
    new { timestamp = DateTime.UtcNow, server_id = "server-02", cpu_percent = 80.3 }
};

var count = await db.Query("server_metrics").InsertAsync(dataList);

// Dictionary로 삽입
var data = new Dictionary<string, object>
{
    { "timestamp", DateTime.UtcNow },
    { "server_id", "game-server-01" },
    { "cpu_percent", 75.5 }
};

await db.Query("server_metrics").InsertAsync(data);

// INSERT ... RETURNING (PostgreSQL)
// SQLKata는 기본적으로 RETURNING을 지원하지 않으므로 원시 쿼리 사용
var sql = @"
    INSERT INTO server_metrics (timestamp, server_id, cpu_percent)
    VALUES (@timestamp, @server_id, @cpu_percent)
    RETURNING id";

var id = await db.ExecuteScalarAsync<int>(sql, new
{
    timestamp = DateTime.UtcNow,
    server_id = "game-server-01",
    cpu_percent = 75.5
});
```

**UPDATE**

```csharp
// 기본 UPDATE
var affected = await db.Query("server_metrics")
    .Where("server_id", "game-server-01")
    .UpdateAsync(new
    {
        cpu_percent = 85.5,
        updated_at = DateTime.UtcNow
    });

// 조건부 UPDATE
var affected = await db.Query("server_metrics")
    .Where("server_id", "game-server-01")
    .Where("timestamp", "<", DateTime.UtcNow.AddHours(-1))
    .UpdateAsync(new { is_processed = true });

// 증가/감소 (원시 쿼리 필요)
await db.Query("player_stats")
    .Where("player_id", "player123")
    .UpdateAsync(new Dictionary<string, object>
    {
        { "login_count", new UnsafeLiteral("login_count + 1") }
    });
```

**DELETE**

```csharp
// 기본 DELETE
var affected = await db.Query("server_metrics")
    .Where("server_id", "game-server-01")
    .DeleteAsync();

// 조건부 DELETE
var affected = await db.Query("server_metrics")
    .Where("timestamp", "<", DateTime.UtcNow.AddDays(-30))
    .DeleteAsync();

// 전체 DELETE (주의!)
var affected = await db.Query("temp_data").DeleteAsync();
```

**서브쿼리**

```csharp
// WHERE 절의 서브쿼리
var subQuery = new Query("players")
    .Select("player_id")
    .Where("level", ">", 50);

var query = new Query("game_logs")
    .WhereIn("player_id", subQuery);

// FROM 절의 서브쿼리
var subQuery = new Query("server_metrics")
    .SelectRaw("server_id, AVG(cpu_percent) as avg_cpu")
    .GroupBy("server_id");

var query = new Query(subQuery.As("avg_metrics"))
    .Select("*")
    .Where("avg_cpu", ">", 70);

// SELECT 절의 서브쿼리
var query = new Query("players")
    .Select("player_id", "username")
    .SelectRaw(@"(
        SELECT COUNT(*) 
        FROM game_logs 
        WHERE game_logs.player_id = players.player_id
    ) as total_logs");
```

**UNION**

```csharp
// UNION
var query1 = new Query("server_metrics")
    .Select("server_id", "cpu_percent")
    .Where("server_id", "server-01");

var query2 = new Query("server_metrics_archive")
    .Select("server_id", "cpu_percent")
    .Where("server_id", "server-01");

var unionQuery = query1.Union(query2);

// UNION ALL
var unionQuery = query1.UnionAll(query2);
```

**WITH (CTE - Common Table Expression)**

```csharp
// SQLKata는 CTE를 직접 지원하지 않으므로 원시 쿼리 사용
var sql = @"
    WITH high_cpu_servers AS (
        SELECT server_id, AVG(cpu_percent) as avg_cpu
        FROM server_metrics
        WHERE timestamp > NOW() - INTERVAL '1 hour'
        GROUP BY server_id
        HAVING AVG(cpu_percent) > 80
    )
    SELECT * FROM high_cpu_servers
    ORDER BY avg_cpu DESC";

var result = await db.SelectAsync<dynamic>(sql);
```

**쿼리 실행 메서드**

```csharp
// 여러 행 조회
var results = await db.Query("server_metrics")
    .Select("*")
    .GetAsync<ServerMetric>();

// 단일 행 조회
var result = await db.Query("server_metrics")
    .Where("id", 1)
    .FirstAsync<ServerMetric>();

// 단일 행 조회 (없으면 null)
var result = await db.Query("server_metrics")
    .Where("id", 1)
    .FirstOrDefaultAsync<ServerMetric>();

// 스칼라 값 조회 (COUNT, AVG 등)
var count = await db.Query("server_metrics")
    .CountAsync<int>();

var avgCpu = await db.Query("server_metrics")
    .Select("cpu_percent")
    .FirstOrDefaultAsync<double>();

// 원시 쿼리 실행
var sql = "SELECT * FROM server_metrics WHERE cpu_percent > @threshold";
var results = await db.SelectAsync<ServerMetric>(sql, new { threshold = 80 });

// 쿼리 문자열만 가져오기 (디버깅용)
var query = new Query("server_metrics")
    .Where("server_id", "server-01");

var sqlResult = compiler.Compile(query);
Console.WriteLine(sqlResult.Sql);        // SQL 쿼리
Console.WriteLine(sqlResult.Bindings);   // 파라미터 값들
```

**트랜잭션**

```csharp
// 트랜잭션 사용
using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

using var transaction = await connection.BeginTransactionAsync();
var db = new QueryFactory(connection, compiler);

try
{
    await db.Query("players").InsertAsync(new { /* ... */ });
    await db.Query("player_stats").InsertAsync(new { /* ... */ });
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

**동적 쿼리 구성**

```csharp
// 조건부로 WHERE 절 추가
var query = new Query("server_metrics").Select("*");

if (!string.IsNullOrEmpty(serverId))
{
    query = query.Where("server_id", serverId);
}

if (startDate.HasValue)
{
    query = query.Where("timestamp", ">=", startDate.Value);
}

if (endDate.HasValue)
{
    query = query.Where("timestamp", "<=", endDate.Value);
}

var results = await db.GetAsync<ServerMetric>(query);

// When 메서드 사용 (더 깔끔한 방법)
var query = new Query("server_metrics")
    .Select("*")
    .When(!string.IsNullOrEmpty(serverId), q => q.Where("server_id", serverId))
    .When(startDate.HasValue, q => q.Where("timestamp", ">=", startDate.Value))
    .When(endDate.HasValue, q => q.Where("timestamp", "<=", endDate.Value));
```

**자주 사용하는 패턴**

```csharp
// 페이징 + 필터링 + 정렬
public async Task<PagedResult<ServerMetric>> GetServerMetrics(
    string serverId = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    int page = 1,
    int pageSize = 20,
    string sortBy = "timestamp",
    string sortOrder = "desc")
{
    var query = new Query("server_metrics")
        .Select("*")
        .When(!string.IsNullOrEmpty(serverId), q => q.Where("server_id", serverId))
        .When(startDate.HasValue, q => q.Where("timestamp", ">=", startDate))
        .When(endDate.HasValue, q => q.Where("timestamp", "<=", endDate));

    // 총 개수 조회
    var totalCount = await db.Query("server_metrics")
        .When(!string.IsNullOrEmpty(serverId), q => q.Where("server_id", serverId))
        .When(startDate.HasValue, q => q.Where("timestamp", ">=", startDate))
        .When(endDate.HasValue, q => q.Where("timestamp", "<=", endDate))
        .CountAsync<int>();

    // 정렬 추가
    if (sortOrder.ToLower() == "desc")
        query = query.OrderByDesc(sortBy);
    else
        query = query.OrderBy(sortBy);

    // 페이징 적용
    query = query.ForPage(page, pageSize);

    var items = await db.GetAsync<ServerMetric>(query);

    return new PagedResult<ServerMetric>
    {
        Items = items.ToList(),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };
}
```

## Appendix B: TimescaleDB 함수 레퍼런스

TimescaleDB는 PostgreSQL을 확장하여 시계열 데이터에 특화된 함수들을 제공한다. 이 섹션에서는 자주 사용하는 TimescaleDB 전용 함수들을 정리했다.

**Hypertable 관리**

```sql
-- Hypertable 생성
SELECT create_hypertable(
    relation => 'table_name',
    time_column_name => 'timestamp_column',
    chunk_time_interval => INTERVAL '1 day',
    if_not_exists => TRUE
);

-- 예제
SELECT create_hypertable(
    'server_metrics',
    'timestamp',
    chunk_time_interval => INTERVAL '1 day'
);

-- 공간(Space) 파티셔닝 추가
SELECT add_dimension(
    'server_metrics',
    'server_id',
    number_partitions => 4
);

-- Hypertable 정보 조회
SELECT * FROM timescaledb_information.hypertables;

-- 특정 Hypertable의 청크 정보
SELECT * FROM timescaledb_information.chunks
WHERE hypertable_name = 'server_metrics';

-- 청크 크기 확인
SELECT
    chunk_name,
    pg_size_pretty(total_bytes) AS total_size,
    pg_size_pretty(index_bytes) AS index_size,
    pg_size_pretty(toast_bytes) AS toast_size,
    pg_size_pretty(table_bytes) AS table_size
FROM timescaledb_information.chunks
WHERE hypertable_name = 'server_metrics'
ORDER BY range_start DESC;
```

**시간 버킷 함수 (time_bucket)**

```sql
-- 기본 time_bucket
SELECT
    time_bucket('5 minutes', timestamp) AS bucket,
    AVG(cpu_percent) AS avg_cpu
FROM server_metrics
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY bucket
ORDER BY bucket;

-- 다양한 시간 간격
time_bucket('1 minute', timestamp)     -- 1분
time_bucket('5 minutes', timestamp)    -- 5분
time_bucket('15 minutes', timestamp)   -- 15분
time_bucket('1 hour', timestamp)       -- 1시간
time_bucket('1 day', timestamp)        -- 1일
time_bucket('1 week', timestamp)       -- 1주
time_bucket('1 month', timestamp)      -- 1개월

-- 오프셋을 사용한 time_bucket (특정 시간부터 시작)
SELECT
    time_bucket('1 day', timestamp, TIMESTAMPTZ '2024-01-01 09:00:00') AS bucket,
    COUNT(*) AS count
FROM game_logs
GROUP BY bucket;

-- 갭 채우기 (time_bucket_gapfill)
SELECT
    time_bucket_gapfill('5 minutes', timestamp) AS bucket,
    server_id,
    AVG(cpu_percent) AS avg_cpu
FROM server_metrics
WHERE timestamp > NOW() - INTERVAL '1 hour'
  AND server_id = 'game-server-01'
GROUP BY bucket, server_id
ORDER BY bucket;

-- locf (Last Observation Carried Forward) - 이전 값으로 채우기
SELECT
    time_bucket_gapfill('5 minutes', timestamp) AS bucket,
    server_id,
    locf(AVG(cpu_percent)) AS avg_cpu
FROM server_metrics
WHERE timestamp BETWEEN NOW() - INTERVAL '1 hour' AND NOW()
  AND server_id = 'game-server-01'
GROUP BY bucket, server_id
ORDER BY bucket;

-- interpolate - 선형 보간으로 채우기
SELECT
    time_bucket_gapfill('5 minutes', timestamp) AS bucket,
    server_id,
    interpolate(AVG(cpu_percent)) AS avg_cpu
FROM server_metrics
WHERE timestamp BETWEEN NOW() - INTERVAL '1 hour' AND NOW()
  AND server_id = 'game-server-01'
GROUP BY bucket, server_id
ORDER BY bucket;
```

**연속 집계 (Continuous Aggregates)**

```sql
-- 연속 집계 생성
CREATE MATERIALIZED VIEW server_metrics_5min
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('5 minutes', timestamp) AS bucket,
    server_id,
    AVG(cpu_percent) AS avg_cpu,
    MAX(cpu_percent) AS max_cpu,
    MIN(cpu_percent) AS min_cpu,
    AVG(memory_percent) AS avg_memory
FROM server_metrics
GROUP BY bucket, server_id;

-- 리프레시 정책 추가
SELECT add_continuous_aggregate_policy(
    'server_metrics_5min',
    start_offset => INTERVAL '1 month',
    end_offset => INTERVAL '1 minute',
    schedule_interval => INTERVAL '5 minutes'
);

-- 수동 리프레시
CALL refresh_continuous_aggregate('server_metrics_5min', NULL, NULL);

-- 특정 시간 범위 리프레시
CALL refresh_continuous_aggregate(
    'server_metrics_5min',
    '2024-01-01 00:00:00',
    '2024-01-02 00:00:00'
);

-- 연속 집계 정보 조회
SELECT * FROM timescaledb_information.continuous_aggregates;

-- 리프레시 정책 조회
SELECT * FROM timescaledb_information.jobs
WHERE proc_name = 'policy_refresh_continuous_aggregate';

-- 리프레시 정책 제거
SELECT remove_continuous_aggregate_policy('server_metrics_5min');

-- 연속 집계 삭제
DROP MATERIALIZED VIEW server_metrics_5min;
```

**압축 (Compression)**

```sql
-- 압축 활성화
ALTER TABLE server_metrics SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'server_id',
    timescaledb.compress_orderby = 'timestamp DESC'
);

-- 압축 정책 추가 (7일 이상 된 청크 자동 압축)
SELECT add_compression_policy(
    'server_metrics',
    INTERVAL '7 days'
);

-- 수동 압축 (특정 청크)
SELECT compress_chunk('_timescaledb_internal._hyper_1_1_chunk');

-- 모든 압축 가능한 청크 압축
SELECT compress_chunk(i.schema_name || '.' || i.table_name)
FROM timescaledb_information.chunks i
WHERE i.hypertable_name = 'server_metrics'
  AND i.is_compressed = FALSE
  AND i.range_end < NOW() - INTERVAL '7 days';

-- 청크 압축 해제
SELECT decompress_chunk('_timescaledb_internal._hyper_1_1_chunk');

-- 압축 통계 조회
SELECT
    hypertable_name,
    pg_size_pretty(before_compression_total_bytes) AS before_compression,
    pg_size_pretty(after_compression_total_bytes) AS after_compression,
    compression_ratio
FROM timescaledb_information.compression_settings
WHERE hypertable_name = 'server_metrics';

-- 압축 정책 제거
SELECT remove_compression_policy('server_metrics');
```

**데이터 보관 정책 (Retention Policy)**

```sql
-- 보관 정책 추가 (30일 이상 된 데이터 자동 삭제)
SELECT add_retention_policy(
    'server_metrics',
    INTERVAL '30 days'
);

-- 보관 정책 조회
SELECT * FROM timescaledb_information.jobs
WHERE proc_name = 'policy_retention';

-- 보관 정책 제거
SELECT remove_retention_policy('server_metrics');

-- 수동으로 오래된 청크 삭제
SELECT drop_chunks(
    'server_metrics',
    older_than => INTERVAL '30 days'
);

-- 특정 시간 이전의 청크 삭제
SELECT drop_chunks(
    'server_metrics',
    older_than => TIMESTAMPTZ '2024-01-01'
);

-- 삭제 예정인 청크 미리 보기 (실제로 삭제하지 않음)
SELECT show_chunks(
    'server_metrics',
    older_than => INTERVAL '30 days'
);
```

**시간 함수**

```sql
-- 현재 시간 (UTC)
SELECT NOW();
SELECT CURRENT_TIMESTAMP;

-- 타임존 변환
SELECT timestamp AT TIME ZONE 'Asia/Seoul' AS kst_time
FROM server_metrics;

-- 시간 추출
SELECT
    EXTRACT(YEAR FROM timestamp) AS year,
    EXTRACT(MONTH FROM timestamp) AS month,
    EXTRACT(DAY FROM timestamp) AS day,
    EXTRACT(HOUR FROM timestamp) AS hour,
    EXTRACT(MINUTE FROM timestamp) AS minute,
    EXTRACT(DOW FROM timestamp) AS day_of_week  -- 0=일요일, 6=토요일
FROM server_metrics;

-- date_trunc (시간 단위로 절삭)
SELECT
    date_trunc('hour', timestamp) AS hour,
    COUNT(*) AS count
FROM server_metrics
GROUP BY hour;

-- 가능한 date_trunc 단위
date_trunc('microseconds', timestamp)
date_trunc('milliseconds', timestamp)
date_trunc('second', timestamp)
date_trunc('minute', timestamp)
date_trunc('hour', timestamp)
date_trunc('day', timestamp)
date_trunc('week', timestamp)
date_trunc('month', timestamp)
date_trunc('quarter', timestamp)
date_trunc('year', timestamp)
date_trunc('decade', timestamp)
date_trunc('century', timestamp)

-- 시간 연산
SELECT
    timestamp + INTERVAL '1 hour' AS one_hour_later,
    timestamp - INTERVAL '1 day' AS one_day_ago,
    timestamp + INTERVAL '5 minutes' AS five_minutes_later
FROM server_metrics;

-- 두 시간 사이의 차이
SELECT
    timestamp,
    LAG(timestamp) OVER (ORDER BY timestamp) AS previous_timestamp,
    timestamp - LAG(timestamp) OVER (ORDER BY timestamp) AS time_diff
FROM server_metrics;
```

**집계 함수 (Hyperfunctions)**

```sql
-- 백분위수 (Percentile)
SELECT
    server_id,
    percentile_cont(0.50) WITHIN GROUP (ORDER BY cpu_percent) AS p50_cpu,
    percentile_cont(0.95) WITHIN GROUP (ORDER BY cpu_percent) AS p95_cpu,
    percentile_cont(0.99) WITHIN GROUP (ORDER BY cpu_percent) AS p99_cpu
FROM server_metrics
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY server_id;

-- 표준 편차
SELECT
    server_id,
    stddev(cpu_percent) AS cpu_stddev,
    variance(cpu_percent) AS cpu_variance
FROM server_metrics
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY server_id;

-- 히스토그램
SELECT
    width_bucket(cpu_percent, 0, 100, 10) AS bucket,
    COUNT(*) AS frequency
FROM server_metrics
WHERE timestamp > NOW() - INTERVAL '1 hour'
GROUP BY bucket
ORDER BY bucket;

-- 이동 평균 (윈도우 함수)
SELECT
    timestamp,
    server_id,
    cpu_percent,
    AVG(cpu_percent) OVER (
        PARTITION BY server_id
        ORDER BY timestamp
        ROWS BETWEEN 4 PRECEDING AND CURRENT ROW
    ) AS moving_avg_5
FROM server_metrics
ORDER BY server_id, timestamp;

-- 누적 합계
SELECT
    timestamp,
    server_id,
    network_in_bytes,
    SUM(network_in_bytes) OVER (
        PARTITION BY server_id
        ORDER BY timestamp
    ) AS cumulative_network_in
FROM server_metrics
ORDER BY server_id, timestamp;

-- 델타 계산 (이전 값과의 차이)
SELECT
    timestamp,
    server_id,
    cpu_percent,
    cpu_percent - LAG(cpu_percent) OVER (
        PARTITION BY server_id
        ORDER BY timestamp
    ) AS cpu_delta
FROM server_metrics
ORDER BY server_id, timestamp;
```

**메타데이터 조회**

```sql
-- 모든 Hypertable 목록
SELECT
    hypertable_schema,
    hypertable_name,
    num_dimensions,
    num_chunks,
    compression_enabled,
    replication_factor
FROM timescaledb_information.hypertables;

-- Hypertable 크기
SELECT
    hypertable_name,
    pg_size_pretty(hypertable_size(format('%I.%I', hypertable_schema, hypertable_name)::regclass)) AS total_size,
    pg_size_pretty(indexes_size(format('%I.%I', hypertable_schema, hypertable_name)::regclass)) AS indexes_size
FROM timescaledb_information.hypertables;

-- 청크 정보
SELECT
    hypertable_name,
    chunk_name,
    range_start,
    range_end,
    is_compressed,
    pg_size_pretty(total_bytes) AS chunk_size
FROM timescaledb_information.chunks
ORDER BY range_start DESC;

-- 실행 중인 작업 (압축, 리프레시 등)
SELECT
    job_id,
    application_name,
    proc_schema,
    proc_name,
    scheduled,
    config,
    next_start
FROM timescaledb_information.jobs
ORDER BY next_start;

-- 작업 실행 통계
SELECT
    job_id,
    last_run_started_at,
    last_run_status,
    total_runs,
    total_successes,
    total_failures
FROM timescaledb_information.job_stats
ORDER BY last_run_started_at DESC;
```

**유용한 쿼리 패턴**

```csharp
// C#에서 TimescaleDB 함수 사용 예제

// 1. 5분 단위 집계
var query = new Query("server_metrics")
    .SelectRaw("time_bucket('5 minutes', timestamp) AS bucket")
    .Select("server_id")
    .SelectRaw("AVG(cpu_percent) AS avg_cpu")
    .WhereRaw("timestamp > NOW() - INTERVAL '1 hour'")
    .GroupByRaw("bucket, server_id")
    .OrderBy("bucket");

// 2. 갭 채우기
var sql = @"
    SELECT
        time_bucket_gapfill('5 minutes', timestamp) AS bucket,
        server_id,
        locf(AVG(cpu_percent)) AS avg_cpu
    FROM server_metrics
    WHERE timestamp BETWEEN @start AND @end
      AND server_id = @serverId
    GROUP BY bucket, server_id
    ORDER BY bucket";

var result = await db.SelectAsync<MetricData>(sql, new
{
    start = DateTime.UtcNow.AddHours(-1),
    end = DateTime.UtcNow,
    serverId = "game-server-01"
});

// 3. 시간대별 집계 (한국 시간 기준)
var query = new Query("game_logs")
    .SelectRaw("EXTRACT(HOUR FROM timestamp AT TIME ZONE 'Asia/Seoul') AS hour")
    .SelectRaw("COUNT(*) AS count")
    .WhereRaw("timestamp > NOW() - INTERVAL '7 days'")
    .GroupByRaw("hour")
    .OrderBy("hour");

// 4. 이동 평균
var sql = @"
    SELECT
        timestamp,
        server_id,
        cpu_percent,
        AVG(cpu_percent) OVER (
            PARTITION BY server_id
            ORDER BY timestamp
            ROWS BETWEEN 4 PRECEDING AND CURRENT ROW
        ) AS moving_avg_5
    FROM server_metrics
    WHERE timestamp > NOW() - INTERVAL '1 hour'
    ORDER BY server_id, timestamp";

var result = await db.SelectAsync<MovingAverageData>(sql);

// 5. 백분위수 계산
var sql = @"
    SELECT
        server_id,
        percentile_cont(0.50) WITHIN GROUP (ORDER BY cpu_percent) AS p50,
        percentile_cont(0.95) WITHIN GROUP (ORDER BY cpu_percent) AS p95,
        percentile_cont(0.99) WITHIN GROUP (ORDER BY cpu_percent) AS p99
    FROM server_metrics
    WHERE timestamp > NOW() - INTERVAL '1 hour'
    GROUP BY server_id";

var result = await db.SelectAsync<PercentileData>(sql);
```

**정리**

이 부록은 책상 옆에 두고 필요할 때마다 참조할 수 있는 레퍼런스로 활용한다. SQLKata의 메서드 체이닝 패턴과 TimescaleDB의 시계열 함수들을 조합하면 강력하면서도 읽기 쉬운 코드를 작성할 수 있다. 처음에는 모든 함수를 외울 필요 없이, 이 부록을 참고하면서 자주 사용하는 패턴부터 익숙해지면 된다. 시간이 지나면서 자연스럽게 손에 익게 될 것이다.

온라인 게임 서버의 모니터링과 로그 분석이라는 실무 맥락에서 이 레퍼런스를 활용하여 당신만의 강력한 시스템을 구축하기 바란다.  