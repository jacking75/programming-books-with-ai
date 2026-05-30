// code/ch05/ex02-ddl/DropTableExample.cs
using DuckDB.NET.Data;

// 이 파일은 테이블 삭제 예제를 담고 있다.
// Program.cs와 별도로 실행하거나, 필요 시 Program.cs에서 호출한다.

public static class DropTableExample
{
    public static void Run()
    {
        using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");
        connection.Open();

        using var command = connection.CreateCommand();

        // IF EXISTS: 테이블이 없어도 오류 없이 건너뛴다.
        // 삭제 순서 주의: 외래 키 의존성이 있는 경우 자식 테이블을 먼저 삭제해야 한다.
        command.CommandText = "DROP TABLE IF EXISTS player_item";
        command.ExecuteNonQuery();
        Console.WriteLine("player_item 테이블 삭제 완료");

        command.CommandText = "DROP TABLE IF EXISTS battle_log";
        command.ExecuteNonQuery();
        Console.WriteLine("battle_log 테이블 삭제 완료");
    }
}
