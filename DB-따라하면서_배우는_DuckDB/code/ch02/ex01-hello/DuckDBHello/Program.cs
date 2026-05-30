using DuckDB.NET.Data;

// 인메모리 DuckDB 데이터베이스에 연결
using var connection = new DuckDBConnection("Data Source=:memory:");
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = "SELECT 'Hello, DuckDB!' AS message, 42 AS answer;";

using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"Message: {reader.GetString(0)}");
    Console.WriteLine($"Answer:  {reader.GetInt32(1)}");
}
