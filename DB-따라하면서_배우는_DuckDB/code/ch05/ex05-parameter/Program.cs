// code/ch05/ex05-parameter/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// 테이블 및 테스트 데이터 준비
command.CommandText = @"
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
command.ExecuteNonQuery();

command.CommandText = @"
    INSERT OR IGNORE INTO player (player_id, player_name, level, experience, gold, server_id)
    VALUES
        (1001, '홍길동',   51, 130000, 31000, 1),
        (1002, '이순신',   75, 280000, 56000, 1),
        (1004, '세종대왕', 99, 9999999, 101000, 1)";
command.ExecuteNonQuery();

// -------------------------------------------------------
// Named Parameter 방식 — 파라미터 바인딩
// -------------------------------------------------------

// SQL 문에서 파라미터는 $이름 형식으로 표시한다.
command.CommandText = @"
    SELECT player_id, player_name, level, gold
    FROM player
    WHERE server_id = $serverId
    AND level >= $minLevel
    ORDER BY level DESC
    LIMIT $pageSize";

// Parameters.Add()로 파라미터 값을 바인딩한다.
command.Parameters.Add(new DuckDBParameter("serverId", 1));
command.Parameters.Add(new DuckDBParameter("minLevel", 50));
command.Parameters.Add(new DuckDBParameter("pageSize", 10));

using var reader = command.ExecuteReader();
Console.WriteLine("=== 서버 1 레벨 50 이상 플레이어 ===");
while (reader.Read())
{
    Console.WriteLine($"[{reader.GetInt64(0)}] {reader.GetString(1)} " +
                      $"Lv.{reader.GetInt32(2)} | 골드: {reader.GetInt64(3):N0}");
}

// -------------------------------------------------------
// INSERT에 파라미터 바인딩 적용
// -------------------------------------------------------

// 파라미터를 사용하면 다양한 플레이어 데이터를 안전하게 삽입할 수 있다.
var newPlayers = new[]
{
    (Id: 2001L, Name: "신플레이어1", Level: 1, Gold: 100L, ServerId: 2),
    (Id: 2002L, Name: "신플레이어2", Level: 1, Gold: 100L, ServerId: 2),
    (Id: 2003L, Name: "신플레이어3", Level: 1, Gold: 100L, ServerId: 2),
};

command.CommandText = @"
    INSERT OR IGNORE INTO player (player_id, player_name, level, gold, server_id)
    VALUES ($playerId, $playerName, $level, $gold, $serverId)";

command.Parameters.Clear();

// 먼저 파라미터 슬롯을 생성해 두고
command.Parameters.Add(new DuckDBParameter("playerId",   0L));
command.Parameters.Add(new DuckDBParameter("playerName", ""));
command.Parameters.Add(new DuckDBParameter("level",      0));
command.Parameters.Add(new DuckDBParameter("gold",       0L));
command.Parameters.Add(new DuckDBParameter("serverId",   0));

foreach (var p in newPlayers)
{
    // 파라미터 값만 교체하여 반복 실행 (쿼리 파싱은 1번만 수행됨 — 성능상 유리)
    command.Parameters["playerId"].Value   = p.Id;
    command.Parameters["playerName"].Value = p.Name;
    command.Parameters["level"].Value      = p.Level;
    command.Parameters["gold"].Value       = p.Gold;
    command.Parameters["serverId"].Value   = p.ServerId;

    command.ExecuteNonQuery();
    Console.WriteLine($"플레이어 '{p.Name}' 등록 완료");
}

// -------------------------------------------------------
// 파라미터 타입 명시 예제
// -------------------------------------------------------

// 타입을 명시하는 경우
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS login_log (
        player_id BIGINT NOT NULL,
        logged_at TIMESTAMP NOT NULL
    )";
command.Parameters.Clear();
command.ExecuteNonQuery();

command.CommandText = "INSERT INTO login_log (player_id, logged_at) VALUES ($playerId, $loggedAt)";
command.Parameters.Add(new DuckDBParameter("playerId", 1001L));
command.Parameters.Add(new DuckDBParameter
{
    ParameterName = "loggedAt",
    Value         = DateTime.UtcNow,
    DbType        = System.Data.DbType.DateTime
});
command.ExecuteNonQuery();
Console.WriteLine("로그인 기록 저장 완료");
