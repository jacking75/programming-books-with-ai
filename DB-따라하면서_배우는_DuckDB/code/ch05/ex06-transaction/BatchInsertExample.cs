// code/ch05/ex06-transaction/BatchInsertExample.cs
using DuckDB.NET.Data;

public static class BatchInsertExample
{
    public static void Run(DuckDBConnection connection)
    {
        // -------------------------------------------------------
        // 배치 삽입 트랜잭션 — 전투 로그 대량 삽입
        // -------------------------------------------------------

        // 테스트용 전투 로그 1,000건 생성
        var random = new Random(42);
        var battleLogs = Enumerable.Range(1, 1000).Select(i => new
        {
            PlayerId       = (long)(1001 + random.Next(2)),   // 2명의 플레이어 중 랜덤
            MonsterId      = random.Next(1, 50),
            DamageDealt    = random.Next(100, 5000),
            DamageTaken    = random.Next(0, 2000),
            IsVictory      = random.NextDouble() > 0.3,        // 70% 승률
            BattleDuration = 10.0 + random.NextDouble() * 290
        }).ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var transaction = connection.BeginTransaction();
        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO battle_log
                    (player_id, monster_id, damage_dealt, damage_taken, is_victory, battle_duration)
                VALUES
                    ($playerId, $monsterId, $damageDealt, $damageTaken, $isVictory, $battleDuration)";

            // 파라미터 슬롯 사전 생성
            command.Parameters.Add(new DuckDBParameter("playerId",       0L));
            command.Parameters.Add(new DuckDBParameter("monsterId",      0));
            command.Parameters.Add(new DuckDBParameter("damageDealt",    0));
            command.Parameters.Add(new DuckDBParameter("damageTaken",    0));
            command.Parameters.Add(new DuckDBParameter("isVictory",      false));
            command.Parameters.Add(new DuckDBParameter("battleDuration", 0.0));

            foreach (var log in battleLogs)
            {
                command.Parameters["playerId"].Value       = log.PlayerId;
                command.Parameters["monsterId"].Value      = log.MonsterId;
                command.Parameters["damageDealt"].Value    = log.DamageDealt;
                command.Parameters["damageTaken"].Value    = log.DamageTaken;
                command.Parameters["isVictory"].Value      = log.IsVictory;
                command.Parameters["battleDuration"].Value = log.BattleDuration;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            stopwatch.Stop();
            Console.WriteLine($"전투 로그 {battleLogs.Count}건 삽입 완료 ({stopwatch.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"배치 삽입 실패: {ex.Message}");
        }
    }
}
