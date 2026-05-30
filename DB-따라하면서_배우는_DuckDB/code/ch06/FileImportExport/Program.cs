using Ch06FileIO;
using System;
using System.IO;

// 프로젝트 루트 기준 경로 설정
// 실행 파일이 FileImportExport/bin/Debug/net9.0/ 에 위치하므로
// 상위 4단계로 올라가면 code/ch06/ 디렉토리를 기준으로 삼는다
string baseDir = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

string dataDir   = Path.Combine(baseDir, "code", "ch06", "data");
string logsDir   = Path.Combine(dataDir, "logs");
string outputDir = Path.Combine(baseDir, "code", "ch06", "output");
string dbPath    = Path.Combine(outputDir, "game_analytics.db");

Directory.CreateDirectory(outputDir);

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("=================================================");
Console.WriteLine("  6장: DuckDB 파일 임포트/익스포트 예제");
Console.WriteLine("=================================================\n");

// ──────────────────────────────────────────────
// 데모 1: CSV 파일 임포트 및 테이블 익스포트
// ──────────────────────────────────────────────
Console.WriteLine("【데모 1】 CSV 임포트 및 익스포트");
Console.WriteLine(new string('-', 48));

var processor = new GameLogFileProcessor(
    dbPath: dbPath,
    dataDirectory: dataDir,
    outputDirectory: outputDir
);

// battle_log.csv 임포트
processor.ImportCsvToTable("battle_log.csv", "battle_log");

// 테이블 전체를 CSV로 내보내기
processor.ExportTableToCsv("battle_log", "battle_log_export.csv");

// 플레이어 통계 쿼리를 Parquet으로 저장
string playerStatsQuery = @"
    SELECT
        player_id,
        player_name,
        COUNT(*)                                           AS total_battles,
        SUM(damage_dealt)                                  AS total_damage_dealt,
        SUM(damage_taken)                                  AS total_damage_taken,
        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)   AS wins,
        SUM(CASE WHEN result = 'LOSE' THEN 1 ELSE 0 END)  AS losses,
        ROUND(
            SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)
            * 100.0 / COUNT(*), 2
        )                                                  AS win_rate
    FROM battle_log
    GROUP BY player_id, player_name
    ORDER BY total_damage_dealt DESC";

processor.ExportQueryToParquet(playerStatsQuery, "player_stats_all.parquet");

// ──────────────────────────────────────────────
// 데모 2: JSON 파일 임포트
// ──────────────────────────────────────────────
Console.WriteLine("\n【데모 2】 JSON 임포트 및 통계 출력");
Console.WriteLine(new string('-', 48));

var jsonImporter = new GameEventJsonImporter(dbPath);
jsonImporter.ImportJsonEvents(
    Path.Combine(dataDir, "item_acquisition.json"));
jsonImporter.PrintEventStats();

// ──────────────────────────────────────────────
// 데모 3: 글로브 패턴으로 다중 CSV 파일 처리
// ──────────────────────────────────────────────
Console.WriteLine("\n【데모 3】 글로브 패턴으로 다중 파일 처리");
Console.WriteLine(new string('-', 48));

var analyzer = new MultiFileAnalyzer(dbPath);

// 파일별 행 수 출력
string globPattern = Path.Combine(logsDir, "battle_log_*.csv");
analyzer.PrintFileRowCounts(globPattern);

// 주간 플레이어 통계 (3월 26일 기준 3일치 데이터)
DateTime weekStart = new DateTime(2026, 3, 26);
var weeklyStats = analyzer.GetWeeklyPlayerStats(logsDir, weekStart);

Console.WriteLine($"\n--- {weekStart:yyyy-MM-dd} 기준 3일 통합 플레이어 랭킹 ---");
Console.WriteLine($"{"순위",4} {"플레이어",-16} {"전투수",6} {"총 데미지",12} {"승리수",6} {"승률",8}");
Console.WriteLine(new string('-', 58));

int rank = 1;
foreach (var stat in weeklyStats)
{
    Console.WriteLine(
        $"{rank++,4} {stat.PlayerName,-16} {stat.TotalBattles,6:N0} " +
        $"{stat.TotalDamage,12:N0} {stat.Wins,6:N0} {stat.WinRate,7:F1}%");
}

// 다중 CSV를 Parquet으로 아카이빙
string archiveOutput = Path.Combine(outputDir, "battle_log_archive.parquet");
analyzer.ArchiveCsvToParquet(globPattern, archiveOutput);

// ──────────────────────────────────────────────
// 데모 4: 날짜별 일간 리포트 생성
// ──────────────────────────────────────────────
Console.WriteLine("\n【데모 4】 일간 리포트 자동 생성 (최신 날짜)");
Console.WriteLine(new string('-', 48));

var dailyProcessor = new GameLogFileProcessor(
    dbPath: dbPath,
    dataDirectory: logsDir,
    outputDirectory: outputDir
);

dailyProcessor.GenerateDailyReport(new DateTime(2026, 3, 28));

Console.WriteLine("\n=================================================");
Console.WriteLine("  모든 데모 완료. output/ 디렉토리를 확인하세요.");
Console.WriteLine("=================================================");
