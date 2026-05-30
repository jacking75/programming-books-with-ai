// code/ch05/ex01-connection/Program.cs
using DuckDB.NET.Data;

// -------------------------------------------------------
// 파일 DB 연결 예제
// - DataSource에 파일 경로를 지정하면 파일 DB 모드로 동작한다.
// - 파일이 없으면 자동으로 생성된다.
// -------------------------------------------------------

// using 블록을 사용하면 블록이 끝날 때 자동으로 연결이 닫히고 자원이 해제된다.
using var connection = new DuckDBConnection("DataSource=game_analytics.duckdb");

// 연결을 열기 전에는 쿼리를 실행할 수 없다.
connection.Open();

Console.WriteLine($"연결 상태: {connection.State}");  // Open
Console.WriteLine($"데이터베이스: {connection.Database}");

// 간단한 쿼리로 연결 정상 동작 확인
using var command = connection.CreateCommand();
command.CommandText = "SELECT current_date AS today";

using var reader = command.ExecuteReader();
if (reader.Read())
{
    Console.WriteLine($"오늘 날짜: {reader.GetValue(0)}");
}

// using 블록 종료 시 connection.Dispose()가 자동 호출되어 연결이 닫힌다.
