// code/ch05/ex01-connection/InMemoryExample.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// 인메모리 DB 연결 예제
// - DataSource=:memory: 로 지정하면 인메모리 모드로 동작한다.
// - 빠른 테스트, 임시 집계 작업에 적합하다.
// -------------------------------------------------------

// 이 파일의 코드를 실행하려면 Program.cs의 코드를 이 내용으로 교체하거나,
// 별도 메서드로 호출하도록 수정한다.

// using var connection = new DuckDBConnection("DataSource=:memory:");
// connection.Open();
//
// using var command = connection.CreateCommand();
//
// // 인메모리 DB에 테이블 생성
// command.CommandText = @"
//     CREATE TABLE player (
//         player_id   BIGINT PRIMARY KEY,
//         player_name VARCHAR NOT NULL,
//         level       INTEGER DEFAULT 1,
//         created_at  TIMESTAMP DEFAULT current_timestamp
//     )";
// command.ExecuteNonQuery();
//
// Console.WriteLine("인메모리 DB에 player 테이블 생성 완료");
//
// // 데이터 삽입 및 조회 확인
// command.CommandText = "INSERT INTO player (player_id, player_name, level) VALUES (1001, '홍길동', 50)";
// command.ExecuteNonQuery();
//
// command.CommandText = "SELECT player_id, player_name, level FROM player";
// using var reader = command.ExecuteReader();
// while (reader.Read())
// {
//     Console.WriteLine($"[{reader.GetInt64(0)}] {reader.GetString(1)} (레벨: {reader.GetInt32(2)})");
// }

public static class InMemoryExample
{
    public static void Run()
    {
        using var connection = new DuckDBConnection("DataSource=:memory:");
        connection.Open();

        using var command = connection.CreateCommand();

        // 인메모리 DB에 테이블 생성
        command.CommandText = @"
            CREATE TABLE player (
                player_id   BIGINT PRIMARY KEY,
                player_name VARCHAR NOT NULL,
                level       INTEGER DEFAULT 1,
                created_at  TIMESTAMP DEFAULT current_timestamp
            )";
        command.ExecuteNonQuery();

        Console.WriteLine("인메모리 DB에 player 테이블 생성 완료");

        // 데이터 삽입 및 조회 확인
        command.CommandText = "INSERT INTO player (player_id, player_name, level) VALUES (1001, '홍길동', 50)";
        command.ExecuteNonQuery();

        command.CommandText = "SELECT player_id, player_name, level FROM player";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"[{reader.GetInt64(0)}] {reader.GetString(1)} (레벨: {reader.GetInt32(2)})");
        }
    }
}
