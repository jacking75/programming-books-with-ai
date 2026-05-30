// code/ch07/LogPipeline/Program.cs
using GameLogPipeline;

// --- 데모용 샘플 JSONL 파일 생성 ---
// 실제 환경에서는 JsonLogWriter 프로젝트가 생성한 파일을 사용한다
Console.WriteLine("=== DuckDB 로그 적재 파이프라인 ===");
Console.WriteLine();

DateOnly targetDate = DateOnly.FromDateTime(DateTime.UtcNow);
string dateStr = targetDate.ToString("yyyy-MM-dd");
string logDir  = Path.Combine("logs", dateStr);
Directory.CreateDirectory(logDir);

// 샘플 JSONL 데이터 생성
Console.WriteLine($"[Setup] 샘플 JSONL 파일 생성: {logDir}");
await CreateSampleJsonlFilesAsync(logDir, dateStr);

// --- 파이프라인 실행 ---
var pipeline = new LogIngestionPipeline(
    duckDbPath:       "data/gamelogs.duckdb",
    logDirectory:     "logs",
    archiveDirectory: "archive"
);

Console.WriteLine();
Console.WriteLine($"[Pipeline] {dateStr} 날짜 로그 처리 시작...");
await pipeline.RunAsync(targetDate);

Console.WriteLine();
Console.WriteLine("=== 파이프라인 완료 ===");
Console.WriteLine();
Console.WriteLine("생성된 파일:");
Console.WriteLine($"  데이터베이스: data/gamelogs.duckdb");
Console.WriteLine($"  Parquet 아카이브: archive/{dateStr}/");

// 파이프라인 완료 후 Parquet 파일 목록 출력
string archiveDir = Path.Combine("archive", dateStr);
if (Directory.Exists(archiveDir))
{
    foreach (string file in Directory.GetFiles(archiveDir, "*.parquet"))
    {
        var info = new FileInfo(file);
        Console.WriteLine($"    {Path.GetFileName(file)} ({info.Length:N0} bytes)");
    }
}

// ─── 샘플 데이터 생성 헬퍼 ───────────────────────────────────────────────────
static async Task CreateSampleJsonlFilesAsync(string logDir, string dateStr)
{
    // 접속 로그
    string sessionPath = Path.Combine(logDir, $"session-{dateStr}.jsonl");
    await File.WriteAllLinesAsync(sessionPath, new[]
    {
        $$"""{"log_type":"session","event_type":"login","player_id":"P001234","server_id":"S-KR-01","ip":"203.0.113.42","client_version":"2.5.1","timestamp":"{{dateStr}}T09:00:00.000Z"}""",
        $$"""{"log_type":"session","event_type":"login","player_id":"P005678","server_id":"S-KR-01","ip":"203.0.113.55","client_version":"2.5.1","timestamp":"{{dateStr}}T09:05:00.000Z"}""",
        $$"""{"log_type":"session","event_type":"logout","player_id":"P001234","server_id":"S-KR-01","ip":"203.0.113.42","client_version":"2.5.1","timestamp":"{{dateStr}}T10:30:00.000Z"}""",
    });
    Console.WriteLine($"  생성: {sessionPath}");

    // 전투 로그
    string combatPath = Path.Combine(logDir, $"combat-{dateStr}.jsonl");
    var combatLines = Enumerable.Range(1, 20).Select(i =>
        $$"""{"log_type":"combat","player_id":"P{{(i % 3 + 1):D6}}","skill_id":{{100 + i % 5}},"target_id":"MON_{{i:D4}}","damage":{{Random.Shared.Next(500, 9999)}},"is_critical":{{(i % 4 == 0 ? "true" : "false")}},"timestamp":"{{dateStr}}T09:{{i:D2}}:{{(i * 3 % 60):D2}}.000Z"}""");
    await File.WriteAllLinesAsync(combatPath, combatLines);
    Console.WriteLine($"  생성: {combatPath}");

    // 아이템 로그
    string itemPath = Path.Combine(logDir, $"item-{dateStr}.jsonl");
    await File.WriteAllLinesAsync(itemPath, new[]
    {
        $$"""{"log_type":"item","player_id":"P001234","item_id":10045,"item_name":"강화석 +7","action":"acquire","quantity":1,"timestamp":"{{dateStr}}T09:18:05.200Z"}""",
        $$"""{"log_type":"item","player_id":"P005678","item_id":20001,"item_name":"회복 포션","action":"consume","quantity":3,"timestamp":"{{dateStr}}T09:25:10.000Z"}""",
        $$"""{"log_type":"item","player_id":"P001234","item_id":30010,"item_name":"전설 검","action":"trade","quantity":1,"timestamp":"{{dateStr}}T10:00:00.000Z"}""",
    });
    Console.WriteLine($"  생성: {itemPath}");

    // 결제 로그
    string paymentPath = Path.Combine(logDir, $"payment-{dateStr}.jsonl");
    await File.WriteAllLinesAsync(paymentPath, new[]
    {
        $$"""{"log_type":"payment","player_id":"P001234","product_id":"PACK_DIAMOND_100","amount":11000,"currency":"KRW","payment_method":"mobile","timestamp":"{{dateStr}}T10:00:00.000Z"}""",
        $$"""{"log_type":"payment","player_id":"P005678","product_id":"PACK_GOLD_50","amount":5500,"currency":"KRW","payment_method":"card","timestamp":"{{dateStr}}T10:15:30.000Z"}""",
    });
    Console.WriteLine($"  생성: {paymentPath}");
}
