// code/ch05/ex03-dml/Program.cs
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// 테이블이 없으면 먼저 생성한다.
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
    CREATE TABLE IF NOT EXISTS item (
        item_id         INTEGER      PRIMARY KEY,
        item_name       VARCHAR(100) NOT NULL,
        item_type       VARCHAR(20)  NOT NULL,
        rarity          VARCHAR(10)  NOT NULL DEFAULT 'common',
        base_power      INTEGER      NOT NULL DEFAULT 0
    )";
command.ExecuteNonQuery();

command.CommandText = @"
    CREATE TABLE IF NOT EXISTS player_item (
        player_id       BIGINT       NOT NULL,
        item_id         INTEGER      NOT NULL,
        quantity        INTEGER      NOT NULL DEFAULT 1,
        obtained_at     TIMESTAMP    NOT NULL DEFAULT current_timestamp,
        PRIMARY KEY (player_id, item_id)
    )";
command.ExecuteNonQuery();

// -------------------------------------------------------
// INSERT 예제
// -------------------------------------------------------

// 단건 삽입
command.CommandText = @"
    INSERT INTO player (player_id, player_name, level, experience, gold, server_id)
    VALUES (1001, '홍길동', 50, 125000, 30000, 1)";
int rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"삽입된 행 수: {rowsAffected}");  // 1

// 여러 행 한 번에 삽입 (VALUES 절 나열)
command.CommandText = @"
    INSERT INTO player (player_id, player_name, level, experience, gold, server_id)
    VALUES
        (1002, '이순신', 75, 280000, 55000, 1),
        (1003, '강감찬', 33, 42000, 18000, 2),
        (1004, '세종대왕', 99, 9999999, 100000, 1)";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"삽입된 행 수: {rowsAffected}");  // 3

// 아이템 데이터 삽입
command.CommandText = @"
    INSERT INTO item (item_id, item_name, item_type, rarity, base_power)
    VALUES
        (101, '불꽃의 검', 'weapon', 'epic', 350),
        (102, '강철 갑옷', 'armor', 'rare', 220),
        (103, '체력 물약', 'potion', 'common', 0),
        (104, '용의 심장', 'weapon', 'legendary', 999)";
command.ExecuteNonQuery();
Console.WriteLine("아이템 데이터 삽입 완료");

// -------------------------------------------------------
// UPDATE 예제
// -------------------------------------------------------

// 플레이어 레벨업 처리
command.CommandText = @"
    UPDATE player
    SET level = level + 1,
        experience = experience + 5000
    WHERE player_id = 1001";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"레벨업 처리된 플레이어 수: {rowsAffected}");

// 서버 1의 모든 플레이어에게 골드 지급 (이벤트)
command.CommandText = @"
    UPDATE player
    SET gold = gold + 1000
    WHERE server_id = 1";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"골드 지급된 플레이어 수: {rowsAffected}");

// -------------------------------------------------------
// DELETE 예제
// -------------------------------------------------------

// 특정 플레이어 삭제 (탈퇴 처리)
command.CommandText = "DELETE FROM player WHERE player_id = 1003";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"삭제된 플레이어 수: {rowsAffected}");

// 레벨 30 미만의 비활성 플레이어 정리
command.CommandText = @"
    DELETE FROM player
    WHERE level < 30
    AND last_login_at < current_timestamp - INTERVAL 90 DAY";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"비활성 플레이어 정리: {rowsAffected}명 삭제");

// -------------------------------------------------------
// UPSERT 예제 (INSERT OR REPLACE)
// -------------------------------------------------------

// player_item 테이블에서: 이미 보유한 아이템이면 수량 증가, 없으면 새로 삽입
command.CommandText = @"
    INSERT INTO player_item (player_id, item_id, quantity)
    VALUES (1001, 103, 5)
    ON CONFLICT (player_id, item_id)
    DO UPDATE SET quantity = player_item.quantity + excluded.quantity";
rowsAffected = command.ExecuteNonQuery();
Console.WriteLine($"아이템 지급 처리: {rowsAffected}행 영향");
