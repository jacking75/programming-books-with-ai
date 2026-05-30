using DuckDB.NET.Data;

namespace GameLogAnalyzer.Pipeline;

/// <summary>
/// loaded_files 테이블을 활용하여 이미 적재된 파일을 추적한다.
/// 멱등성 보장: 같은 파일을 두 번 적재하지 않는다.
/// </summary>
public class DuplicateChecker
{
    private readonly DuckDBConnection _conn;

    public DuplicateChecker(DuckDBConnection conn)
    {
        _conn = conn;
        EnsureTable();
    }

    private void EnsureTable()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS loaded_files (
                file_name   VARCHAR PRIMARY KEY,
                loaded_at   TIMESTAMP DEFAULT now(),
                row_count   BIGINT
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public bool IsAlreadyLoaded(string fileName)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM loaded_files WHERE file_name = $name";
        cmd.Parameters.Add(new DuckDBParameter("name", fileName));
        var result = cmd.ExecuteScalar();
        return Convert.ToInt64(result) > 0;
    }

    public void MarkAsLoaded(string fileName, long rowCount)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO loaded_files (file_name, row_count)
            VALUES ($name, $rows)
            """;
        cmd.Parameters.Add(new DuckDBParameter("name", fileName));
        cmd.Parameters.Add(new DuckDBParameter("rows", rowCount));
        cmd.ExecuteNonQuery();
    }
}
