using DuckDB.NET.Data;

namespace GameLogAnalyzer.Pipeline;

/// <summary>
/// JSONL 파일을 DuckDB에 벌크 적재한다.
/// DuckDB의 read_json_auto() 함수를 활용하여 스키마 자동 추론 후 INSERT한다.
/// </summary>
public class BulkLoader
{
    private readonly DuckDBConnection _conn;
    private readonly DuplicateChecker _checker;

    public BulkLoader(DuckDBConnection conn, DuplicateChecker checker)
    {
        _conn = conn;
        _checker = checker;
        EnsureTables();
    }

    /// <summary>
    /// 네 가지 이벤트 테이블을 생성한다 (없으면).
    /// </summary>
    private void EnsureTables()
    {
        using var cmd = _conn.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS login_events (
                event_type              VARCHAR,
                player_id               VARCHAR,
                nickname                VARCHAR,
                timestamp               TIMESTAMP,
                date                    DATE,
                level                   INTEGER,
                region                  VARCHAR,
                platform                VARCHAR,
                session_duration_seconds INTEGER,
                is_first_login          BOOLEAN
            );

            CREATE TABLE IF NOT EXISTS battle_events (
                event_type      VARCHAR,
                player_id       VARCHAR,
                timestamp       TIMESTAMP,
                date            DATE,
                dungeon_id      VARCHAR,
                dungeon_name    VARCHAR,
                difficulty      VARCHAR,
                is_success      BOOLEAN,
                duration_seconds INTEGER,
                damage_dealt    INTEGER,
                damage_taken    INTEGER,
                gold_earned     INTEGER,
                exp_earned      INTEGER,
                player_level    INTEGER
            );

            CREATE TABLE IF NOT EXISTS item_events (
                event_type      VARCHAR,
                player_id       VARCHAR,
                timestamp       TIMESTAMP,
                date            DATE,
                action          VARCHAR,
                item_id         VARCHAR,
                item_name       VARCHAR,
                item_category   VARCHAR,
                quantity        INTEGER,
                gold_value      INTEGER,
                source          VARCHAR
            );

            CREATE TABLE IF NOT EXISTS payment_events (
                event_type      VARCHAR,
                player_id       VARCHAR,
                timestamp       TIMESTAMP,
                date            DATE,
                product_id      VARCHAR,
                product_name    VARCHAR,
                amount_krw      INTEGER,
                currency        VARCHAR,
                payment_method  VARCHAR,
                store           VARCHAR
            );
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 파일 목록을 순회하며 DuckDB에 적재한다.
    /// 이미 적재된 파일은 건너뛴다.
    /// </summary>
    public LoadResult LoadFiles(IEnumerable<FileInfo> files)
    {
        var result = new LoadResult();

        foreach (var file in files)
        {
            if (_checker.IsAlreadyLoaded(file.Name))
            {
                Console.WriteLine($"  [스킵] {file.Name} — 이미 적재됨");
                result.SkippedFiles++;
                continue;
            }

            Console.Write($"  [적재] {file.Name} ... ");
            long rows = LoadFile(file.FullName);
            _checker.MarkAsLoaded(file.Name, rows);

            Console.WriteLine($"{rows:N0}개 이벤트 적재 완료");
            result.LoadedFiles++;
            result.TotalRows += rows;
        }

        return result;
    }

    /// <summary>
    /// 단일 JSONL 파일을 이벤트 타입에 따라 분류하여 각 테이블에 INSERT한다.
    /// DuckDB의 read_json_auto()를 사용해 JSON 파싱 비용을 최소화한다.
    /// </summary>
    private long LoadFile(string filePath)
    {
        // 경로 내 역슬래시를 슬래시로 변환 (DuckDB 경로 호환)
        string safePath = filePath.Replace("\\", "/");
        long totalRows = 0;

        // 이벤트 타입별 INSERT — DuckDB가 JSON 컬럼을 자동 추론
        var insertQueries = new[]
        {
            (Table: "login_events", EventType: "login", Query: $"""
                INSERT INTO login_events
                SELECT
                    event_type,
                    player_id,
                    nickname,
                    timestamp::TIMESTAMP,
                    date::DATE,
                    level,
                    region,
                    platform,
                    session_duration_seconds,
                    is_first_login
                FROM read_json_auto('{safePath}')
                WHERE event_type = 'login'
                """),

            (Table: "battle_events", EventType: "battle", Query: $"""
                INSERT INTO battle_events
                SELECT
                    event_type,
                    player_id,
                    timestamp::TIMESTAMP,
                    date::DATE,
                    dungeon_id,
                    dungeon_name,
                    difficulty,
                    is_success,
                    duration_seconds,
                    damage_dealt,
                    damage_taken,
                    gold_earned,
                    exp_earned,
                    player_level
                FROM read_json_auto('{safePath}')
                WHERE event_type = 'battle'
                """),

            (Table: "item_events", EventType: "item", Query: $"""
                INSERT INTO item_events
                SELECT
                    event_type,
                    player_id,
                    timestamp::TIMESTAMP,
                    date::DATE,
                    action,
                    item_id,
                    item_name,
                    item_category,
                    quantity,
                    gold_value,
                    source
                FROM read_json_auto('{safePath}')
                WHERE event_type = 'item'
                """),

            (Table: "payment_events", EventType: "payment", Query: $"""
                INSERT INTO payment_events
                SELECT
                    event_type,
                    player_id,
                    timestamp::TIMESTAMP,
                    date::DATE,
                    product_id,
                    product_name,
                    amount_krw,
                    currency,
                    payment_method,
                    store
                FROM read_json_auto('{safePath}')
                WHERE event_type = 'payment'
                """)
        };

        foreach (var (table, eventType, query) in insertQueries)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = query;
            long affected = cmd.ExecuteNonQuery();
            totalRows += affected;
        }

        return totalRows;
    }
}

public record LoadResult
{
    public int LoadedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public long TotalRows { get; set; }
}
