// code/ch05/ex07-async/PlayerService.cs
using DuckDB.NET.Data;

/// <summary>
/// 플레이어 데이터 접근 서비스 — 비동기 패턴 적용
/// </summary>
public class PlayerService : IDisposable
{
    private readonly DuckDBConnection _connection;

    public PlayerService(string dataSource)
    {
        _connection = new DuckDBConnection($"DataSource={dataSource}");
    }

    // 서비스 초기화 (비동기 연결 열기)
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();

        // 테이블이 없으면 생성
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS player (
                player_id       BIGINT       PRIMARY KEY,
                player_name     VARCHAR(50)  NOT NULL,
                level           INTEGER      NOT NULL DEFAULT 1,
                experience      BIGINT       NOT NULL DEFAULT 0,
                gold            BIGINT       NOT NULL DEFAULT 0,
                server_id       INTEGER      NOT NULL DEFAULT 1,
                created_at      TIMESTAMP    NOT NULL DEFAULT current_timestamp,
                last_login_at   TIMESTAMP
            )";
        await cmd.ExecuteNonQueryAsync();

        // 테스트 데이터 삽입
        cmd.CommandText = @"
            INSERT OR IGNORE INTO player (player_id, player_name, level, experience, gold, server_id)
            VALUES
                (1001, '홍길동',   51, 130000, 31000, 1),
                (1002, '이순신',   75, 280000, 56000, 1),
                (1004, '세종대왕', 99, 9999999, 101000, 1)";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 플레이어 ID로 단일 플레이어 조회 (비동기)
    /// </summary>
    public async Task<Player?> GetPlayerByIdAsync(long playerId)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT player_id, player_name, level, experience, gold, server_id,
                   created_at, last_login_at
            FROM player
            WHERE player_id = $playerId";
        command.Parameters.Add(new DuckDBParameter("playerId", playerId));

        using var reader = (DuckDBDataReader)await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Player(
                PlayerId:    reader.GetInt64(0),
                PlayerName:  reader.GetString(1),
                Level:       reader.GetInt32(2),
                Experience:  reader.GetInt64(3),
                Gold:        reader.GetInt64(4),
                ServerId:    reader.GetInt32(5),
                CreatedAt:   reader.GetDateTime(6),
                LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            );
        }
        return null;  // 플레이어가 없으면 null 반환
    }

    /// <summary>
    /// 서버 내 상위 N명의 플레이어 조회 (비동기)
    /// </summary>
    public async Task<List<Player>> GetTopPlayersAsync(int serverId, int topN = 10)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT player_id, player_name, level, experience, gold, server_id,
                   created_at, last_login_at
            FROM player
            WHERE server_id = $serverId
            ORDER BY level DESC, experience DESC
            LIMIT $topN";
        command.Parameters.Add(new DuckDBParameter("serverId", serverId));
        command.Parameters.Add(new DuckDBParameter("topN",     topN));

        var players = new List<Player>();
        using var reader = (DuckDBDataReader)await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            players.Add(new Player(
                PlayerId:    reader.GetInt64(0),
                PlayerName:  reader.GetString(1),
                Level:       reader.GetInt32(2),
                Experience:  reader.GetInt64(3),
                Gold:        reader.GetInt64(4),
                ServerId:    reader.GetInt32(5),
                CreatedAt:   reader.GetDateTime(6),
                LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            ));
        }
        return players;
    }

    /// <summary>
    /// 플레이어 골드 업데이트 (비동기)
    /// </summary>
    public async Task<bool> UpdatePlayerGoldAsync(long playerId, long goldDelta)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE player
            SET gold = gold + $delta
            WHERE player_id = $playerId AND (gold + $delta) >= 0";
        command.Parameters.Add(new DuckDBParameter("playerId", playerId));
        command.Parameters.Add(new DuckDBParameter("delta",    goldDelta));

        int rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
