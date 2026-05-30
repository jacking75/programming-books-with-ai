using DuckDB.NET.Data;
using System;
using System.IO;

namespace Ch06FileIO
{
    public class GameEventJsonImporter
    {
        private readonly string _dbPath;

        public GameEventJsonImporter(string dbPath)
        {
            _dbPath = dbPath;
        }

        /// <summary>
        /// JSON 파일을 DuckDB에 임포트 (중첩 구조 자동 처리)
        /// </summary>
        public void ImportJsonEvents(string jsonFilePath)
        {
            string normalizedPath = jsonFilePath.Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();

            // 중첩 JSON을 플랫 테이블로 변환하여 저장
            command.CommandText = $@"
                CREATE OR REPLACE TABLE item_events AS
                SELECT
                    event_id,
                    CAST(timestamp AS TIMESTAMP)   AS event_time,
                    player.id                       AS player_id,
                    player.name                     AS player_name,
                    player.level                    AS player_level,
                    player.class                    AS player_class,
                    item.id                         AS item_id,
                    item.name                       AS item_name,
                    item.grade                      AS item_grade,
                    location.zone_id                AS zone_id,
                    source
                FROM read_json_auto('{normalizedPath}')";

            command.ExecuteNonQuery();

            command.CommandText = "SELECT COUNT(*) FROM item_events";
            long count = (long)(command.ExecuteScalar() ?? 0L);

            Console.WriteLine($"[JSON 임포트] {Path.GetFileName(jsonFilePath)}: {count}건 처리 완료");
        }

        /// <summary>
        /// 임포트된 이벤트 통계 출력
        /// </summary>
        public void PrintEventStats()
        {
            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    item_grade,
                    source,
                    COUNT(*) AS count
                FROM item_events
                GROUP BY item_grade, source
                ORDER BY count DESC";

            using var reader = command.ExecuteReader();

            Console.WriteLine("\n--- 아이템 등급별 습득 현황 ---");
            Console.WriteLine($"{"등급",-12} {"출처",-16} {"건수",6}");
            Console.WriteLine(new string('-', 36));

            while (reader.Read())
            {
                Console.WriteLine(
                    $"{reader.GetString(0),-12} " +
                    $"{reader.GetString(1),-16} " +
                    $"{reader.GetInt64(2),6:N0}");
            }
        }
    }
}
