using DuckDB.NET.Data;
using GameLogAnalyzer.Analysis;
using GameLogAnalyzer.Pipeline;
using GameLogAnalyzer.Report;
using GameLogAnalyzer.Simulator;

// ─────────────────────────────────────────────
// 설정
// ─────────────────────────────────────────────
const string DbPath   = "game_logs.db";
const string DataDir  = "logs";
var SimStartDate      = new DateTime(2025, 1, 1);
const int SimDays     = 7;
const int PlayerCount = 100;

ConsoleReporter.PrintHeader("Game Log Analyzer — DuckDB 기반 게임 로그 분석 시스템");

// ─────────────────────────────────────────────
// Phase 1: 로그 시뮬레이션
// ─────────────────────────────────────────────
ConsoleReporter.PrintSection("Phase 1 — 게임 로그 시뮬레이션");

Console.WriteLine($"  플레이어 {PlayerCount}명 생성 중...");
var players = PlayerFactory.CreatePlayers(PlayerCount);
Console.WriteLine($"  {players.Count}명의 플레이어를 생성했습니다.");

var generator = new EventGenerator(seed: 99999);
var writer    = new JsonlWriter(DataDir);

Console.WriteLine($"  {SimDays}일치 이벤트를 시뮬레이션합니다 ({SimStartDate:yyyy-MM-dd} ~ {SimStartDate.AddDays(SimDays - 1):yyyy-MM-dd})");

for (int day = 0; day < SimDays; day++)
{
    var date = SimStartDate.AddDays(day);
    Console.WriteLine($"\n  [{date:yyyy-MM-dd}] 이벤트 생성 중...");

    var allEvents = new List<object>();
    foreach (var player in players)
    {
        var events = generator.GenerateEventsForDay(player, date);
        allEvents.AddRange(events);
    }

    // 타임스탬프 순으로 정렬 후 저장
    allEvents.Sort((a, b) =>
    {
        var ta = (DateTime)a.GetType().GetProperty("Timestamp")!.GetValue(a)!;
        var tb = (DateTime)b.GetType().GetProperty("Timestamp")!.GetValue(b)!;
        return ta.CompareTo(tb);
    });

    await writer.WriteAsync(allEvents, date);
}

Console.WriteLine("\n  시뮬레이션 완료!");

// ─────────────────────────────────────────────
// Phase 2: DuckDB 적재
// ─────────────────────────────────────────────
ConsoleReporter.PrintSection("Phase 2 — DuckDB 적재 파이프라인");

using var conn = new DuckDBConnection($"Data Source={DbPath}");
conn.Open();
Console.WriteLine($"  DuckDB 연결 완료: {DbPath}");

var checker = new DuplicateChecker(conn);
var loader  = new BulkLoader(conn, checker);
var scanner = new FileScanner(DataDir);

var files = scanner.ScanLogFiles();
Console.WriteLine($"\n  발견된 JSONL 파일: {files.Count}개");

var loadResult = loader.LoadFiles(files);

Console.WriteLine($"""

  ── 적재 결과 ──────────────────────
  신규 적재: {loadResult.LoadedFiles}개 파일
  스킵:      {loadResult.SkippedFiles}개 파일
  총 이벤트: {loadResult.TotalRows:N0}건
""");

// ─────────────────────────────────────────────
// Phase 3: 분석 리포트
// ─────────────────────────────────────────────
ConsoleReporter.PrintSection("Phase 3 — 분석 리포트 생성");

var engine = new AnalysisEngine(conn);

// 데이터 요약
var (loginCnt, battleCnt, itemCnt, paymentCnt) = engine.GetDataSummary();
ConsoleReporter.PrintSummary(loginCnt, battleCnt, itemCnt, paymentCnt);

// 1. DAU 리포트
Console.WriteLine("\n  DAU 분석 실행 중...");
var dauRows = engine.GetDailyActiveUsers();
ConsoleReporter.PrintDauReport(dauRows);

// 2. 전투 균형 리포트
Console.WriteLine("\n  전투 균형 분석 실행 중...");
var battleRows = engine.GetBattleBalanceReport();
ConsoleReporter.PrintBattleBalanceReport(battleRows);

// 3. 아이템 경제 리포트
Console.WriteLine("\n  아이템 경제 분석 실행 중...");
var itemRows = engine.GetItemEconomyReport();
ConsoleReporter.PrintItemEconomyReport(itemRows);

// 4. 매출 리포트
Console.WriteLine("\n  매출 분석 실행 중...");
var revenueReport = await engine.GetRevenueReportAsync();
ConsoleReporter.PrintRevenueReport(revenueReport.Rows);

// 5. 이상 탐지
Console.WriteLine("\n  이상 탐지 분석 실행 중...");
var anomalyRows = await engine.GetAnomalyDetectionReportAsync();
ConsoleReporter.PrintAnomalyReport(anomalyRows);

ConsoleReporter.PrintFooter();
