// Program.cs
// 성능 벤치마크 실행 진입점

using System.Diagnostics;
using DuckDB.NET.Data;

Console.WriteLine("=================================================");
Console.WriteLine("  DuckDB .NET 성능 벤치마크 (제10장)");
Console.WriteLine("=================================================");
Console.WriteLine();

// 인메모리 데이터베이스 사용 (벤치마크용)
const string dbPath = ":memory:";

// ─────────────────────────────────────────────
// 벤치마크 1: INSERT 방식 비교
// ─────────────────────────────────────────────
Console.WriteLine("[ 벤치마크 1 ] INSERT 방식 비교 (10,000건)");
Console.WriteLine("-------------------------------------------------");

const int insertCount = 10_000;

// 단건 INSERT
using var conn1 = new DuckDBConnection($"DataSource={dbPath}");
conn1.Open();
var bench1 = new BatchInsertExample(conn1);

Console.Write($"  단건 INSERT {insertCount:N0}건 ... ");
long ms1 = bench1.InsertOneByOne(insertCount);
Console.WriteLine($"{ms1,8:N0} ms");

bench1.TruncateTable();

// 다중 VALUES
Console.Write($"  다중 VALUES {insertCount:N0}건 (1000건씩) ... ");
long ms2 = bench1.InsertBatchValues(insertCount, batchSize: 1000);
Console.WriteLine($"{ms2,8:N0} ms");

bench1.TruncateTable();

// DuckDBAppender
Console.Write($"  DuckDBAppender {insertCount:N0}건 ... ");
long ms3 = bench1.InsertWithAppender(insertCount);
Console.WriteLine($"{ms3,8:N0} ms");

Console.WriteLine();
Console.WriteLine("  ┌──────────────────────────────┬───────────┬─────────────┐");
Console.WriteLine("  │ 방법                         │ 소요 시간 │ 상대 속도   │");
Console.WriteLine("  ├──────────────────────────────┼───────────┼─────────────┤");
Console.WriteLine($"  │ 단건 INSERT                  │ {ms1,6:N0} ms │ {(double)ms1 / ms1,8:F1}x    │");
Console.WriteLine($"  │ 다중 VALUES (1000건씩)       │ {ms2,6:N0} ms │ {(double)ms1 / ms2,8:F1}x    │");
Console.WriteLine($"  │ DuckDBAppender               │ {ms3,6:N0} ms │ {(double)ms1 / ms3,8:F1}x    │");
Console.WriteLine("  └──────────────────────────────┴───────────┴─────────────┘");
Console.WriteLine();

// ─────────────────────────────────────────────
// 벤치마크 2: 커넥션 재사용 vs 매번 새로 열기
// ─────────────────────────────────────────────
Console.WriteLine("[ 벤치마크 2 ] 커넥션 재사용 vs 매번 새로 열기 (100회 쿼리)");
Console.WriteLine("-------------------------------------------------");

const int queryRepeat = 100;

// 매번 새 커넥션 열기
var swReopen = Stopwatch.StartNew();
for (int i = 0; i < queryRepeat; i++)
{
    using var tmpConn = new DuckDBConnection($"DataSource={dbPath}");
    tmpConn.Open();
    using var cmd = tmpConn.CreateCommand();
    cmd.CommandText = "SELECT 42";
    cmd.ExecuteScalar();
}
swReopen.Stop();

// 커넥션 재사용 (싱글턴)
using var sharedConn = new DuckDBConnection($"DataSource={dbPath}");
sharedConn.Open();
var swReuse = Stopwatch.StartNew();
for (int i = 0; i < queryRepeat; i++)
{
    using var cmd = sharedConn.CreateCommand();
    cmd.CommandText = "SELECT 42";
    cmd.ExecuteScalar();
}
swReuse.Stop();

Console.WriteLine($"  매번 새 커넥션 열기 ({queryRepeat}회): {swReopen.ElapsedMilliseconds,6:N0} ms");
Console.WriteLine($"  커넥션 재사용      ({queryRepeat}회): {swReuse.ElapsedMilliseconds,6:N0} ms");
if (swReuse.ElapsedMilliseconds > 0)
{
    Console.WriteLine($"  → 재사용이 {(double)swReopen.ElapsedMilliseconds / swReuse.ElapsedMilliseconds:F1}배 빠름");
}
Console.WriteLine();

// ─────────────────────────────────────────────
// 벤치마크 3: AnalyticsQueue 시연
// ─────────────────────────────────────────────
Console.WriteLine("[ 벤치마크 3 ] AnalyticsQueue - 백그라운드 쿼리 처리 시연");
Console.WriteLine("-------------------------------------------------");

await using var queue = new AnalyticsQueue(dbPath);

// 더미 로그 3배치 삽입
var logTypes = new[] { "LOGIN", "LOGOUT", "PURCHASE", "KILL", "LEVEL_UP" };
var rng = new Random(42);
var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

for (int batch = 0; batch < 3; batch++)
{
    int batchNum = batch;
    var logs = Enumerable.Range(0, 1000).Select(i => new GameLogEntry(
        LogTime: baseTime.AddSeconds(batchNum * 1000 + i),
        LogType: logTypes[rng.Next(logTypes.Length)],
        UserId: rng.NextInt64(1, 100_000),
        ServerId: rng.Next(1, 5),
        EventData: null
    )).ToList();

    await queue.EnqueueBatchInsert(logs);
    Console.WriteLine($"  배치 {batchNum + 1} 큐 등록 완료 (1,000건)");
}

// 집계 쿼리 등록
long totalRows = 0;
await queue.EnqueueQuery(
    "SELECT COUNT(*) FROM game_logs",
    reader =>
    {
        if (reader.Read())
            totalRows = reader.GetInt64(0);
    });

// 모든 작업 완료 대기
await queue.FlushAsync();

Console.WriteLine($"  백그라운드 처리 완료 - 총 {totalRows:N0}건 삽입 확인");
Console.WriteLine($"  AnalyticsQueue 처리 건수: {queue.ProcessedCount}건");
Console.WriteLine();

// ─────────────────────────────────────────────
// 마무리 요약
// ─────────────────────────────────────────────
Console.WriteLine("=================================================");
Console.WriteLine("  종합 결과 요약");
Console.WriteLine("=================================================");
Console.WriteLine($"  INSERT {insertCount:N0}건 기준:");
Console.WriteLine($"    단건 INSERT:     {ms1,6:N0} ms   ({insertCount * 1000.0 / ms1,10:N0} rows/s)");
Console.WriteLine($"    다중 VALUES:     {ms2,6:N0} ms   ({insertCount * 1000.0 / ms2,10:N0} rows/s)");
Console.WriteLine($"    DuckDBAppender:  {ms3,6:N0} ms   ({insertCount * 1000.0 / ms3,10:N0} rows/s)");
Console.WriteLine();
Console.WriteLine("  핵심 교훈:");
Console.WriteLine("    1. 대량 INSERT는 DuckDBAppender를 사용하라.");
Console.WriteLine("    2. 커넥션은 싱글턴으로 관리해 재사용하라.");
Console.WriteLine("    3. 동시 쿼리 대신 AnalyticsQueue로 순차 처리하라.");
Console.WriteLine("=================================================");
