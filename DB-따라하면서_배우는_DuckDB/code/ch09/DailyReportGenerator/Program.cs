// code/ch09/DailyReportGenerator/Program.cs
// 9장 실전 예제: DuckDB 통계 분석 결과를 콘솔과 HTML 리포트로 출력한다.
//
// 실행 방법:
//   dotnet run                              -- 오늘 날짜로 report.html 생성
//   dotnet run -- 2025-06-30               -- 특정 날짜로 실행
//   dotnet run -- 2025-06-30 output.html   -- 출력 파일명 지정

using DuckDB.NET.Data;
using DailyReportGenerator;

// ─────────────────────────────────────────────────────────────────────────────
// 실행 인수 처리
// ─────────────────────────────────────────────────────────────────────────────
var reportDate = args.Length > 0 ? args[0] : DateTime.Today.ToString("yyyy-MM-dd");
var outputPath = args.Length > 1 ? args[1] : "report.html";

Console.WriteLine("===========================================");
Console.WriteLine($"  DuckDB 일일 통계 리포트 생성기");
Console.WriteLine($"  기준일: {reportDate}");
Console.WriteLine("===========================================");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 인메모리 DuckDB 연결 (파일 없이 메모리에서 실행)
// ─────────────────────────────────────────────────────────────────────────────
using var connection = new DuckDBConnection("DataSource=:memory:");
await connection.OpenAsync();

// ─────────────────────────────────────────────────────────────────────────────
// Step 1: 30일치 샘플 데이터 생성
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("[1/6] 샘플 데이터 생성 중...");
await ExecuteNonQueryAsync(connection, ReportQuery.CreateSampleData());
Console.WriteLine("      players / login_logs / battle_logs / payments 테이블 생성 완료");
Console.WriteLine();

// HTML 빌더 초기화
var htmlBuilder = new HtmlReportBuilder(reportDate);

// ─────────────────────────────────────────────────────────────────────────────
// Step 2: 일별 DAU 추이 (최근 7일)
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("[2/6] 일별 DAU 추이 (최근 7일)");
Console.WriteLine($"  {"날짜",-12} {"DAU",6}");
Console.WriteLine($"  {new string('-', 20)}");

var dauRows = new List<(string Date, int Dau)>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = ReportQuery.DailyDauTrend();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var date = reader.GetDateTime(0).ToString("yyyy-MM-dd");
        var dau  = reader.GetInt32(1);
        dauRows.Add((date, dau));
        Console.WriteLine($"  {date,-12} {dau,6:N0}");
    }
}
Console.WriteLine();
htmlBuilder.AddDauSection(dauRows);

// ─────────────────────────────────────────────────────────────────────────────
// Step 3: 직업별 전투 통계 (윈도우 함수 활용)
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("[3/6] 직업별 전투 통계");
Console.WriteLine($"  {"직업",-8} {"전투수",8} {"평균점수",10} {"중앙값",10} {"P99점수",10} {"순위",6}");
Console.WriteLine($"  {new string('-', 56)}");

var jobStatRows = new List<JobStatRow>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = ReportQuery.JobBattleStats();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var row = new JobStatRow(
            JobClass:       reader.IsDBNull(0) ? "Unknown" : reader.GetString(0),
            BattleCount:    reader.GetInt32(1),
            AvgScore:       reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
            MedianScore:    reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
            StddevScore:    reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
            P99Score:       reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
            AvgDuration:    reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
            MedianDuration: reader.IsDBNull(7) ? 0 : reader.GetDouble(7),
            RankByScore:    reader.GetInt32(8),
            RankByDuration: reader.GetInt32(9)
        );
        jobStatRows.Add(row);
        Console.WriteLine($"  {row.JobClass,-8} {row.BattleCount,8:N0} {row.AvgScore,10:F1} {row.MedianScore,10:F1} {row.P99Score,10:F1} {row.RankByScore,6}");
    }
}
Console.WriteLine();
htmlBuilder.AddJobStatsSection(jobStatRows);

// ─────────────────────────────────────────────────────────────────────────────
// Step 4: 시간대별 접속 패턴
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("[4/6] 시간대별 접속 패턴");
Console.WriteLine($"  {"시간",6} {"접속수",8} {"고유유저",10} {"비중",8}");
Console.WriteLine($"  {new string('-', 36)}");

var hourlyRows = new List<(int Hour, int LoginCount, int UniqueUsers, double Pct)>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = ReportQuery.HourlyLoginPattern();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var hour        = reader.GetInt32(0);
        var loginCount  = reader.GetInt32(1);
        var uniqueUsers = reader.GetInt32(2);
        var pct         = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
        hourlyRows.Add((hour, loginCount, uniqueUsers, pct));
        Console.WriteLine($"  {hour,4}시 {loginCount,8:N0} {uniqueUsers,10:N0} {pct,7:F2}%");
    }
}
Console.WriteLine();
htmlBuilder.AddHourlyPatternSection(hourlyRows);

// ─────────────────────────────────────────────────────────────────────────────
// Step 5: 코호트 분석 (가입 주차별 리텐션)
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("[5/6] 코호트 분석 (가입 주차별 리텐션)");
Console.WriteLine($"  {"코호트 주차",-14} {"초기유저",8} {"경과주차",8} {"잔존유저",8} {"리텐션",8}");
Console.WriteLine($"  {new string('-', 52)}");

var cohortRows = new List<CohortRow>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = ReportQuery.CohortRetention();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var row = new CohortRow(
            CohortWeek:   reader.GetDateTime(0).ToString("yyyy-MM-dd"),
            CohortUsers:  reader.GetInt32(1),
            WeekNum:      reader.GetInt32(2),
            ActiveUsers:  reader.GetInt32(3),
            RetentionPct: reader.IsDBNull(4) ? 0 : reader.GetDouble(4)
        );
        cohortRows.Add(row);
        Console.WriteLine($"  {row.CohortWeek,-14} {row.CohortUsers,8} {row.WeekNum,8} {row.ActiveUsers,8} {row.RetentionPct,7:F1}%");
    }
}
Console.WriteLine();
htmlBuilder.AddCohortSection(cohortRows);

// ─────────────────────────────────────────────────────────────────────────────
// Step 6: 결제 퍼널 분석
// ─────────────────────────────────────────────────────────────────────────────
Console.WriteLine("[6/6] 결제 퍼널 분석");
Console.WriteLine($"  {"단계",4} {"퍼널 스텝",-18} {"유저수",8} {"전체CVR",10} {"직전CVR",10} {"이탈수",8}");
Console.WriteLine($"  {new string('-', 64)}");

var funnelRows = new List<FunnelRow>();
await using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = ReportQuery.PaymentFunnel();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        // step_order 이 NULL 일 수 있으므로 방어
        if (reader.IsDBNull(0)) continue;
        var row = new FunnelRow(
            StepOrder:       reader.GetInt32(0),
            FunnelStep:      reader.IsDBNull(1) ? "" : reader.GetString(1),
            Users:           reader.GetInt32(2),
            TotalCvrPct:     reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
            PrevStepCvrPct:  reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
            DropOff:         reader.GetInt32(5)
        );
        funnelRows.Add(row);
        Console.WriteLine($"  {row.StepOrder,4} {row.FunnelStep,-18} {row.Users,8:N0} {row.TotalCvrPct,9:F1}% {row.PrevStepCvrPct,9:F1}% {row.DropOff,8:N0}");
    }
}
Console.WriteLine();
htmlBuilder.AddFunnelSection(funnelRows);

// ─────────────────────────────────────────────────────────────────────────────
// HTML 리포트 저장
// ─────────────────────────────────────────────────────────────────────────────
await htmlBuilder.SaveAsync(outputPath);
Console.WriteLine("===========================================");
Console.WriteLine($"  HTML 리포트 저장 완료: {Path.GetFullPath(outputPath)}");
Console.WriteLine("===========================================");

// ─────────────────────────────────────────────────────────────────────────────
// 헬퍼: DDL / DML 실행 (결과 없음)
// ─────────────────────────────────────────────────────────────────────────────
static async Task ExecuteNonQueryAsync(DuckDBConnection conn, string sql)
{
    // 세미콜론으로 구분된 여러 문장을 순서대로 실행
    var statements = sql
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(s => s.Length > 0);

    foreach (var stmt in statements)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = stmt;
        await cmd.ExecuteNonQueryAsync();
    }
}
