// code/ch07/LogPipeline/LogPipeline.cs
using DuckDB.NET.Data;

namespace GameLogPipeline;

public class LogIngestionPipeline
{
    private readonly string _duckDbPath;
    private readonly string _logDirectory;
    private readonly string _archiveDirectory;

    public LogIngestionPipeline(string duckDbPath, string logDirectory, string archiveDirectory)
    {
        _duckDbPath       = duckDbPath;
        _logDirectory     = logDirectory;
        _archiveDirectory = archiveDirectory;
    }

    /// <summary>지정된 날짜의 JSONL 파일을 모두 DuckDB에 적재한다.</summary>
    public async Task RunAsync(DateOnly targetDate)
    {
        string dateStr = targetDate.ToString("yyyy-MM-dd");
        string dateDir = Path.Combine(_logDirectory, dateStr);

        if (!Directory.Exists(dateDir))
        {
            Console.WriteLine($"[Pipeline] 로그 디렉터리 없음: {dateDir}");
            return;
        }

        string[] jsonlFiles = Directory.GetFiles(dateDir, "*.jsonl");
        if (jsonlFiles.Length == 0)
        {
            Console.WriteLine($"[Pipeline] JSONL 파일 없음: {dateDir}");
            return;
        }

        Console.WriteLine($"[Pipeline] {jsonlFiles.Length}개 파일 발견. 적재 시작...");

        // DuckDB 데이터베이스 파일이 있는 디렉터리 생성
        string? dbDir = Path.GetDirectoryName(_duckDbPath);
        if (!string.IsNullOrEmpty(dbDir))
            Directory.CreateDirectory(dbDir);

        await using var conn = new DuckDBConnection($"Data Source={_duckDbPath}");
        await conn.OpenAsync();

        // 1단계: 테이블 초기화
        await InitializeTablesAsync(conn);

        // 2단계: 스테이징 테이블에 전체 JSONL 적재
        await LoadToStagingAsync(conn, dateDir);

        // 3단계: 로그 타입별 분류 적재
        await DistributeByLogTypeAsync(conn, dateStr);

        // 4단계: Parquet 아카이브 저장
        await ExportToParquetAsync(conn, dateStr);

        Console.WriteLine($"[Pipeline] {dateStr} 적재 완료.");
    }

    private static async Task InitializeTablesAsync(DuckDBConnection conn)
    {
        string sql = """
            -- 스테이징 테이블 (JSON 원본 보관)
            CREATE TABLE IF NOT EXISTS staging_raw (
                raw_json  VARCHAR,
                loaded_at TIMESTAMP DEFAULT now()
            );

            -- 접속 로그 테이블
            CREATE TABLE IF NOT EXISTS session_logs (
                event_type     VARCHAR,
                player_id      VARCHAR,
                server_id      VARCHAR,
                ip             VARCHAR,
                client_version VARCHAR,
                timestamp      TIMESTAMP
            );

            -- 전투 로그 테이블
            CREATE TABLE IF NOT EXISTS combat_logs (
                player_id   VARCHAR,
                skill_id    INTEGER,
                target_id   VARCHAR,
                damage      INTEGER,
                is_critical BOOLEAN,
                timestamp   TIMESTAMP
            );

            -- 아이템 로그 테이블
            CREATE TABLE IF NOT EXISTS item_logs (
                player_id VARCHAR,
                item_id   INTEGER,
                item_name VARCHAR,
                action    VARCHAR,
                quantity  INTEGER,
                timestamp TIMESTAMP
            );

            -- 결제 로그 테이블
            CREATE TABLE IF NOT EXISTS payment_logs (
                player_id      VARCHAR,
                product_id     VARCHAR,
                amount         DECIMAL(18,2),
                currency       VARCHAR,
                payment_method VARCHAR,
                timestamp      TIMESTAMP
            );
            """;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine("[Pipeline] 테이블 초기화 완료.");
    }

    private static async Task LoadToStagingAsync(DuckDBConnection conn, string dateDir)
    {
        // 디렉터리 내 모든 JSONL 파일을 한 번에 읽기
        // DuckDB의 glob 패턴을 활용한다
        string pattern = Path.Combine(dateDir, "*.jsonl").Replace("\\", "/");

        string sql = $"""
            -- 스테이징 테이블을 비우고 새로 적재 (일별 실행 기준)
            DELETE FROM staging_raw;

            INSERT INTO staging_raw (raw_json)
            SELECT json::VARCHAR
            FROM read_ndjson_auto('{pattern}', ignore_errors = true);
            """;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();

        // 적재된 건수 확인
        cmd.CommandText = "SELECT COUNT(*) FROM staging_raw;";
        object? count = await cmd.ExecuteScalarAsync();
        Console.WriteLine($"[Pipeline] 스테이징 적재 완료: {count:N0}건");
    }

    private static async Task DistributeByLogTypeAsync(DuckDBConnection conn, string dateStr)
    {
        // JSON 필드를 파싱하여 각 타입별 테이블로 분류 삽입
        string sql = $"""
            -- 접속 로그
            INSERT INTO session_logs
            SELECT
                json_extract_string(raw_json, '$.event_type')     AS event_type,
                json_extract_string(raw_json, '$.player_id')      AS player_id,
                json_extract_string(raw_json, '$.server_id')      AS server_id,
                json_extract_string(raw_json, '$.ip')             AS ip,
                json_extract_string(raw_json, '$.client_version') AS client_version,
                json_extract_string(raw_json, '$.timestamp')::TIMESTAMP AS timestamp
            FROM staging_raw
            WHERE json_extract_string(raw_json, '$.log_type') = 'session';

            -- 전투 로그
            INSERT INTO combat_logs
            SELECT
                json_extract_string(raw_json, '$.player_id')       AS player_id,
                json_extract(raw_json, '$.skill_id')::INTEGER       AS skill_id,
                json_extract_string(raw_json, '$.target_id')        AS target_id,
                json_extract(raw_json, '$.damage')::INTEGER         AS damage,
                json_extract(raw_json, '$.is_critical')::BOOLEAN    AS is_critical,
                json_extract_string(raw_json, '$.timestamp')::TIMESTAMP AS timestamp
            FROM staging_raw
            WHERE json_extract_string(raw_json, '$.log_type') = 'combat';

            -- 아이템 로그
            INSERT INTO item_logs
            SELECT
                json_extract_string(raw_json, '$.player_id') AS player_id,
                json_extract(raw_json, '$.item_id')::INTEGER AS item_id,
                json_extract_string(raw_json, '$.item_name') AS item_name,
                json_extract_string(raw_json, '$.action')    AS action,
                json_extract(raw_json, '$.quantity')::INTEGER AS quantity,
                json_extract_string(raw_json, '$.timestamp')::TIMESTAMP AS timestamp
            FROM staging_raw
            WHERE json_extract_string(raw_json, '$.log_type') = 'item';

            -- 결제 로그
            INSERT INTO payment_logs
            SELECT
                json_extract_string(raw_json, '$.player_id')      AS player_id,
                json_extract_string(raw_json, '$.product_id')     AS product_id,
                json_extract(raw_json, '$.amount')::DECIMAL(18,2) AS amount,
                json_extract_string(raw_json, '$.currency')       AS currency,
                json_extract_string(raw_json, '$.payment_method') AS payment_method,
                json_extract_string(raw_json, '$.timestamp')::TIMESTAMP AS timestamp
            FROM staging_raw
            WHERE json_extract_string(raw_json, '$.log_type') = 'payment';
            """;

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();

        // 분류 결과 출력
        foreach (string table in new[] { "session_logs", "combat_logs", "item_logs", "payment_logs" })
        {
            cmd.CommandText = $"SELECT COUNT(*) FROM {table};";
            object? count = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"  [{table}] {count:N0}건");
        }
    }

    private async Task ExportToParquetAsync(DuckDBConnection conn, string dateStr)
    {
        string archivePath = Path.Combine(_archiveDirectory, dateStr).Replace("\\", "/");
        Directory.CreateDirectory(Path.Combine(_archiveDirectory, dateStr));

        var tables = new[]
        {
            ("session_logs",  "session"),
            ("combat_logs",   "combat"),
            ("item_logs",     "item"),
            ("payment_logs",  "payment")
        };

        await using var cmd = conn.CreateCommand();

        foreach (var (table, prefix) in tables)
        {
            string parquetPath = $"{archivePath}/{prefix}-{dateStr}.parquet";
            cmd.CommandText = $"""
                COPY (SELECT * FROM {table})
                TO '{parquetPath}'
                (FORMAT PARQUET, COMPRESSION ZSTD, ROW_GROUP_SIZE 100000);
                """;
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"[Pipeline] Parquet 저장: {parquetPath}");
        }
    }
}
