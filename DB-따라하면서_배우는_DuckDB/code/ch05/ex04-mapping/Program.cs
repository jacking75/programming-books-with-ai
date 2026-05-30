// code/ch05/ex04-mapping/Program.cs
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

// 테스트 데이터 삽입 (이미 있으면 무시)
command.CommandText = @"
    INSERT OR IGNORE INTO player (player_id, player_name, level, experience, gold, server_id)
    VALUES
        (1001, '홍길동',   51, 130000, 31000, 1),
        (1002, '이순신',   75, 280000, 56000, 1),
        (1004, '세종대왕', 99, 9999999, 101000, 1)";
command.ExecuteNonQuery();

// -------------------------------------------------------
// 쿼리 결과를 Player 리스트로 매핑
// -------------------------------------------------------

command.CommandText = @"
    SELECT player_id, player_name, level, experience, gold, server_id,
           created_at, last_login_at
    FROM player
    WHERE server_id = 1
    ORDER BY level DESC";

using var reader = command.ExecuteReader();

var players = new List<Player>();
while (reader.Read())
{
    var player = new Player(
        PlayerId:    reader.GetInt64(0),
        PlayerName:  reader.GetString(1),
        Level:       reader.GetInt32(2),
        Experience:  reader.GetInt64(3),
        Gold:        reader.GetInt64(4),
        ServerId:    reader.GetInt32(5),
        CreatedAt:   reader.GetDateTime(6),
        // NULL 가능한 컬럼은 IsDBNull로 확인 후 처리
        LastLoginAt: reader.IsDBNull(7) ? null : reader.GetDateTime(7)
    );
    players.Add(player);
}

Console.WriteLine($"조회된 플레이어: {players.Count}명");
foreach (var p in players)
{
    Console.WriteLine($"  [{p.PlayerId}] {p.PlayerName} | Lv.{p.Level} | 골드: {p.Gold:N0}");
}

// -------------------------------------------------------
// ExecuteScalar() — 단일 값 조회
// -------------------------------------------------------

// 전체 플레이어 수 조회
command.CommandText = "SELECT COUNT(*) FROM player";
long totalPlayers = (long)command.ExecuteScalar()!;
Console.WriteLine($"\n전체 플레이어: {totalPlayers}명");

// 최고 레벨 플레이어의 레벨 조회
command.CommandText = "SELECT MAX(level) FROM player";
int maxLevel = (int)command.ExecuteScalar()!;
Console.WriteLine($"최고 레벨: {maxLevel}");

// 서버 1의 평균 골드 조회
command.CommandText = "SELECT AVG(gold) FROM player WHERE server_id = 1";
double avgGold = (double)command.ExecuteScalar()!;
Console.WriteLine($"서버 1 평균 골드: {avgGold:N0}");

// -------------------------------------------------------
// 확장 메서드를 이용한 매핑 예제
// -------------------------------------------------------

command.CommandText = @"
    SELECT player_id, player_name, level, experience, gold, server_id,
           created_at, last_login_at
    FROM player
    ORDER BY player_id";

using var reader2 = command.ExecuteReader();
var allPlayers = reader2.ToList(r => r.ToPlayer());

Console.WriteLine($"\n확장 메서드로 조회된 플레이어: {allPlayers.Count}명");
foreach (var p in allPlayers)
{
    Console.WriteLine($"  [{p.PlayerId}] {p.PlayerName} | Lv.{p.Level}");
}
