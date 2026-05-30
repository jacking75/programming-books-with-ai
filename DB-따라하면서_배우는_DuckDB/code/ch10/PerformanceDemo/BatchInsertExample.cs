// BatchInsertExample.cs
// 단건 INSERT vs 다중 VALUES vs DuckDBAppender 성능 비교

using System.Diagnostics;
using DuckDB.NET.Data;

public record GameLogEntry(
    DateTime LogTime,
    string LogType,
    long UserId,
    int ServerId,
    string? EventData
);

public class BatchInsertExample
{
    private readonly DuckDBConnection _connection;

    public BatchInsertExample(DuckDBConnection connection)
    {
        _connection = connection;
        EnsureTableExists();
    }

    private void EnsureTableExists()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS game_logs (
                log_time    TIMESTAMP NOT NULL,
                log_type    VARCHAR   NOT NULL,
                user_id     BIGINT    NOT NULL,
                server_id   INTEGER   NOT NULL,
                event_data  VARCHAR
            )";
        cmd.ExecuteNonQuery();
    }

    public void TruncateTable()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM game_logs";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 단건 INSERT를 count번 반복한다. (나쁜 예)
    /// 매번 SQL 파싱, 실행 계획 생성, 트랜잭션 커밋이 발생한다.
    /// </summary>
    /// <param name="count">삽입할 행 수</param>
    /// <returns>소요 시간 (ms)</returns>
    public long InsertOneByOne(int count)
    {
        var logs = GenerateLogs(count);
        var sw = Stopwatch.StartNew();

        foreach (var log in logs)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO game_logs (log_time, log_type, user_id, server_id, event_data)
                VALUES ($log_time, $log_type, $user_id, $server_id, $event_data)";

            cmd.Parameters.Add(new DuckDBParameter("log_time", log.LogTime));
            cmd.Parameters.Add(new DuckDBParameter("log_type", log.LogType));
            cmd.Parameters.Add(new DuckDBParameter("user_id", log.UserId));
            cmd.Parameters.Add(new DuckDBParameter("server_id", log.ServerId));
            cmd.Parameters.Add(new DuckDBParameter("event_data", log.EventData ?? string.Empty));

            cmd.ExecuteNonQuery();
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// 다중 VALUES로 한번에 INSERT한다. (중간 방법)
    /// batchSize 단위로 묶어서 INSERT SQL을 실행한다.
    /// </summary>
    /// <param name="count">삽입할 행 수</param>
    /// <param name="batchSize">한 번에 묶을 행 수 (기본 1000)</param>
    /// <returns>소요 시간 (ms)</returns>
    public long InsertBatchValues(int count, int batchSize = 1000)
    {
        var logs = GenerateLogs(count);
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < logs.Count; i += batchSize)
        {
            var batch = logs.Skip(i).Take(batchSize).ToList();
            var values = string.Join(",\n",
                batch.Select((_, idx) =>
                    $"($t{idx}, $y{idx}, $u{idx}, $s{idx}, $e{idx})"));

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO game_logs
                    (log_time, log_type, user_id, server_id, event_data)
                VALUES {values}";

            for (int j = 0; j < batch.Count; j++)
            {
                cmd.Parameters.Add(new DuckDBParameter($"t{j}", batch[j].LogTime));
                cmd.Parameters.Add(new DuckDBParameter($"y{j}", batch[j].LogType));
                cmd.Parameters.Add(new DuckDBParameter($"u{j}", batch[j].UserId));
                cmd.Parameters.Add(new DuckDBParameter($"s{j}", batch[j].ServerId));
                cmd.Parameters.Add(new DuckDBParameter($"e{j}", batch[j].EventData ?? string.Empty));
            }

            cmd.ExecuteNonQuery();
        }

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// DuckDBAppender를 사용해 대량 INSERT한다. (권장 방법)
    /// 내부적으로 컬럼 지향 배치 버퍼에 데이터를 모아서 한 번에 기록한다.
    /// </summary>
    /// <param name="count">삽입할 행 수</param>
    /// <returns>소요 시간 (ms)</returns>
    public long InsertWithAppender(int count)
    {
        var logs = GenerateLogs(count);
        var sw = Stopwatch.StartNew();

        using var appender = _connection.CreateAppender("game_logs");

        foreach (var log in logs)
        {
            appender.BeginRow();
            appender.AppendValue(log.LogTime);
            appender.AppendValue(log.LogType);
            appender.AppendValue(log.UserId);
            appender.AppendValue(log.ServerId);
            appender.AppendValue(log.EventData);
            appender.EndRow();
        }
        // Dispose 시 자동으로 Flush

        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    /// <summary>
    /// 테스트용 더미 로그 데이터를 생성한다.
    /// </summary>
    private static List<GameLogEntry> GenerateLogs(int count)
    {
        var logTypes = new[] { "LOGIN", "LOGOUT", "PURCHASE", "KILL", "LEVEL_UP" };
        var rng = new Random(42);
        var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var logs = new List<GameLogEntry>(count);

        for (int i = 0; i < count; i++)
        {
            var logType = logTypes[rng.Next(logTypes.Length)];
            logs.Add(new GameLogEntry(
                LogTime: baseTime.AddSeconds(i),
                LogType: logType,
                UserId: rng.NextInt64(1, 1_000_000),
                ServerId: rng.Next(1, 10),
                EventData: logType == "PURCHASE"
                    ? $"{{\"item_id\":{rng.Next(1, 500)},\"amount\":{rng.Next(100, 10000)}}}"
                    : null
            ));
        }

        return logs;
    }
}
