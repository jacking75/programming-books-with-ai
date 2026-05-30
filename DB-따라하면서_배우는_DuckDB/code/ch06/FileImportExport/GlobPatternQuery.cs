using DuckDB.NET.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ch06FileIO
{
    public class MultiFileAnalyzer
    {
        private readonly string _dbPath;

        public MultiFileAnalyzer(string dbPath)
        {
            _dbPath = dbPath;
        }

        /// <summary>
        /// 글로브 패턴으로 여러 날짜의 전투 로그를 통합 집계
        /// </summary>
        public List<PlayerWeeklyStat> GetWeeklyPlayerStats(
            string logDirectory,
            DateTime weekStart)
        {
            // 글로브 패턴 경로 생성
            string globPattern = Path.Combine(logDirectory, "battle_log_*.csv")
                                     .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT
                    player_id,
                    player_name,
                    COUNT(*)                                            AS total_battles,
                    SUM(damage_dealt)                                   AS total_damage,
                    SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)    AS wins,
                    ROUND(
                        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)
                        * 100.0 / COUNT(*), 2
                    )                                                   AS win_rate,
                    MIN(timestamp::DATE)                                AS first_battle_date,
                    MAX(timestamp::DATE)                                AS last_battle_date
                FROM read_csv_auto('{globPattern}', filename = true)
                WHERE
                    timestamp::DATE BETWEEN '{weekStart:yyyy-MM-dd}'
                                        AND '{weekStart.AddDays(6):yyyy-MM-dd}'
                GROUP BY player_id, player_name
                ORDER BY total_damage DESC";

            var results = new List<PlayerWeeklyStat>();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new PlayerWeeklyStat
                {
                    PlayerId       = reader.GetInt32(0),
                    PlayerName     = reader.GetString(1),
                    TotalBattles   = reader.GetInt64(2),
                    TotalDamage    = reader.GetInt64(3),
                    Wins           = reader.GetInt64(4),
                    WinRate        = reader.GetDouble(5)
                });
            }

            return results;
        }

        /// <summary>
        /// 여러 파일의 행 수와 파일 출처를 확인
        /// </summary>
        public void PrintFileRowCounts(string globPattern)
        {
            string normalizedGlob = globPattern.Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT
                    filename,
                    COUNT(*) AS row_count
                FROM read_csv_auto(
                    '{normalizedGlob}',
                    filename = true
                )
                GROUP BY filename
                ORDER BY filename";

            using var reader = command.ExecuteReader();

            Console.WriteLine("\n--- 파일별 행 수 ---");
            Console.WriteLine($"{"파일명",-50} {"행 수",8}");
            Console.WriteLine(new string('-', 60));

            long totalRows = 0;
            while (reader.Read())
            {
                string fileName = Path.GetFileName(reader.GetString(0));
                long rowCount = reader.GetInt64(1);
                totalRows += rowCount;
                Console.WriteLine($"{fileName,-50} {rowCount,8:N0}");
            }
            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"{"합계",-50} {totalRows,8:N0}");
        }

        /// <summary>
        /// 여러 파일을 통합하여 Parquet으로 아카이빙
        /// </summary>
        public void ArchiveCsvToParquet(
            string sourceGlob,
            string outputParquetPath)
        {
            string normalizedGlob   = sourceGlob.Replace('\\', '/');
            string normalizedOutput = outputParquetPath.Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                COPY (
                    SELECT * FROM read_csv_auto('{normalizedGlob}')
                )
                TO '{normalizedOutput}'
                (FORMAT PARQUET, COMPRESSION ZSTD)";

            command.ExecuteNonQuery();

            long fileSize = new FileInfo(outputParquetPath).Length;
            Console.WriteLine(
                $"[아카이빙 완료] {Path.GetFileName(outputParquetPath)} " +
                $"({fileSize / 1024.0 / 1024.0:F2} MB)");
        }
    }

    public record PlayerWeeklyStat
    {
        public int    PlayerId     { get; init; }
        public string PlayerName   { get; init; } = string.Empty;
        public long   TotalBattles { get; init; }
        public long   TotalDamage  { get; init; }
        public long   Wins         { get; init; }
        public double WinRate      { get; init; }
    }
}
