// code/ch05/ex02-ddl/Program.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// DDL 실행 예제 — 게임 관련 테이블 생성
// -------------------------------------------------------

using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
connection.Open();

using var command = connection.CreateCommand();

// 1. 플레이어 테이블 생성
// IF NOT EXISTS: 이미 테이블이 있으면 오류 없이 건너뛴다.
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
int result = command.ExecuteNonQuery();
Console.WriteLine("player 테이블 생성 완료");

// 2. 전투 로그 테이블 생성
// DuckDB의 SEQUENCE 타입을 사용해 자동 증가 ID를 구현한다.
command.CommandText = @"
    CREATE SEQUENCE IF NOT EXISTS seq_battle_log_id START 1";
command.ExecuteNonQuery();

command.CommandText = @"
    CREATE TABLE IF NOT EXISTS battle_log (
        log_id          BIGINT       PRIMARY KEY DEFAULT nextval('seq_battle_log_id'),
        player_id       BIGINT       NOT NULL,
        monster_id      INTEGER      NOT NULL,
        damage_dealt    INTEGER      NOT NULL DEFAULT 0,
        damage_taken    INTEGER      NOT NULL DEFAULT 0,
        is_victory      BOOLEAN      NOT NULL DEFAULT false,
        battle_duration DOUBLE,              -- 전투 시간 (초)
        logged_at       TIMESTAMP    NOT NULL DEFAULT current_timestamp
    )";
command.ExecuteNonQuery();
Console.WriteLine("battle_log 테이블 생성 완료");

// 3. 아이템 테이블 생성
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS item (
        item_id         INTEGER      PRIMARY KEY,
        item_name       VARCHAR(100) NOT NULL,
        item_type       VARCHAR(20)  NOT NULL,   -- 'weapon', 'armor', 'potion', ...
        rarity          VARCHAR(10)  NOT NULL DEFAULT 'common', -- 'common','rare','epic','legendary'
        base_power      INTEGER      NOT NULL DEFAULT 0
    )";
command.ExecuteNonQuery();
Console.WriteLine("item 테이블 생성 완료");

// 4. 플레이어-아이템 보유 현황 테이블
command.CommandText = @"
    CREATE TABLE IF NOT EXISTS player_item (
        player_id       BIGINT       NOT NULL,
        item_id         INTEGER      NOT NULL,
        quantity        INTEGER      NOT NULL DEFAULT 1,
        obtained_at     TIMESTAMP    NOT NULL DEFAULT current_timestamp,
        PRIMARY KEY (player_id, item_id)
    )";
command.ExecuteNonQuery();
Console.WriteLine("player_item 테이블 생성 완료");

// DuckDB의 정보 스키마(information_schema)를 사용해 테이블 목록 조회
command.CommandText = @"
    SELECT table_name, table_type
    FROM information_schema.tables
    WHERE table_schema = 'main'
    ORDER BY table_name";

using var reader = command.ExecuteReader();
Console.WriteLine("\n=== 현재 테이블 목록 ===");
while (reader.Read())
{
    Console.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
}
