using DuckDB.NET.Data;
using System;
using System.IO;

namespace Ch06FileIO
{
    /// <summary>
    /// DuckDB를 이용한 게임 로그 파일 임포트/익스포트 자동화
    /// </summary>
    public class GameLogFileProcessor
    {
        private readonly string _dbPath;
        private readonly string _dataDirectory;
        private readonly string _outputDirectory;

        public GameLogFileProcessor(
            string dbPath,
            string dataDirectory,
            string outputDirectory)
        {
            _dbPath = dbPath;
            _dataDirectory = dataDirectory;
            _outputDirectory = outputDirectory;

            Directory.CreateDirectory(outputDirectory);
        }

        /// <summary>
        /// CSV 파일을 DuckDB 테이블로 임포트
        /// </summary>
        public void ImportCsvToTable(string csvFileName, string tableName)
        {
            string csvPath = Path.Combine(_dataDirectory, csvFileName)
                                 .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();

            // 테이블이 이미 존재하면 삭제 후 재생성
            command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            command.ExecuteNonQuery();

            // CSV를 읽어 테이블 생성 (타입 자동 감지)
            command.CommandText = $@"
                CREATE TABLE {tableName} AS
                SELECT * FROM read_csv_auto('{csvPath}')";
            command.ExecuteNonQuery();

            // 임포트된 행 수 확인
            command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            long rowCount = (long)(command.ExecuteScalar() ?? 0L);

            Console.WriteLine($"[임포트 완료] {csvFileName} -> {tableName}: {rowCount:N0}행");
        }

        /// <summary>
        /// 테이블을 CSV 파일로 익스포트
        /// </summary>
        public void ExportTableToCsv(string tableName, string outputFileName)
        {
            string outputPath = Path.Combine(_outputDirectory, outputFileName)
                                    .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                COPY {tableName}
                TO '{outputPath}'
                (FORMAT CSV, HEADER TRUE)";
            command.ExecuteNonQuery();

            long fileSize = new FileInfo(outputPath.Replace('/', '\\')).Length;
            Console.WriteLine($"[익스포트 완료] {tableName} -> {outputFileName} ({fileSize:N0} bytes)");
        }

        /// <summary>
        /// 쿼리 결과를 Parquet 파일로 저장
        /// </summary>
        public void ExportQueryToParquet(string query, string outputFileName)
        {
            string outputPath = Path.Combine(_outputDirectory, outputFileName)
                                    .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                COPY ({query})
                TO '{outputPath}'
                (FORMAT PARQUET, COMPRESSION ZSTD)";
            command.ExecuteNonQuery();

            Console.WriteLine($"[Parquet 저장 완료] -> {outputFileName}");
        }

        /// <summary>
        /// 일별 게임 통계 리포트 생성
        /// </summary>
        public void GenerateDailyReport(DateTime reportDate)
        {
            string dateStr = reportDate.ToString("yyyy-MM-dd");
            Console.WriteLine($"\n===== {dateStr} 일별 게임 통계 리포트 생성 중 =====\n");

            // 1단계: 해당 날짜의 CSV 파일 임포트
            string csvFileName = $"battle_log_{dateStr.Replace("-", "")}.csv";
            ImportCsvToTable(csvFileName, "battle_log_daily");

            // 2단계: 플레이어별 통계 집계 후 Parquet 저장
            string playerStatsQuery = $@"
                SELECT
                    player_id,
                    player_name,
                    COUNT(*)                                              AS total_battles,
                    SUM(damage_dealt)                                     AS total_damage_dealt,
                    SUM(damage_taken)                                     AS total_damage_taken,
                    SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)      AS wins,
                    SUM(CASE WHEN result = 'LOSE' THEN 1 ELSE 0 END)     AS losses,
                    ROUND(
                        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)
                        * 100.0 / COUNT(*), 2
                    )                                                     AS win_rate
                FROM battle_log_daily
                GROUP BY player_id, player_name
                ORDER BY total_damage_dealt DESC";

            ExportQueryToParquet(
                playerStatsQuery,
                $"player_stats_{dateStr.Replace("-", "")}.parquet");

            // 3단계: 구역별 통계를 CSV로 내보내기 (BI 도구 연동용)
            string zoneStatsQuery = $@"
                SELECT
                    zone_id,
                    COUNT(DISTINCT player_id)                             AS unique_players,
                    COUNT(*)                                              AS total_battles,
                    AVG(damage_dealt)                                     AS avg_damage,
                    MAX(damage_dealt)                                     AS max_damage
                FROM battle_log_daily
                GROUP BY zone_id
                ORDER BY total_battles DESC";

            // 임시 뷰로 만든 후 CSV 내보내기
            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $"CREATE OR REPLACE VIEW zone_stats_view AS {zoneStatsQuery}";
            cmd.ExecuteNonQuery();

            ExportTableToCsv(
                "zone_stats_view",
                $"zone_stats_{dateStr.Replace("-", "")}.csv");

            Console.WriteLine($"\n[완료] {dateStr} 리포트 생성이 모두 끝났습니다.");
        }
    }
}
