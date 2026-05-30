// ConnectionManager.cs
// DuckDB 싱글턴 커넥션 매니저

using DuckDB.NET.Data;

/// <summary>
/// DuckDB 커넥션 싱글턴 매니저.
/// DuckDB는 멀티스레드에서 단일 커넥션을 공유하여 사용한다.
/// (DuckDB의 단일 파일 DB는 동시 쓰기에 내부 잠금을 사용함)
/// </summary>
public sealed class DuckDbConnectionManager : IDisposable
{
    private static DuckDbConnectionManager? _instance;
    private static readonly Lock _lock = new();

    private readonly DuckDBConnection _connection;
    private bool _disposed;

    private DuckDbConnectionManager(string databasePath)
    {
        _connection = new DuckDBConnection($"DataSource={databasePath}");
        _connection.Open();
        ApplyOptimalSettings();
    }

    public static DuckDbConnectionManager GetInstance(string databasePath)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                _instance ??= new DuckDbConnectionManager(databasePath);
            }
        }
        return _instance;
    }

    /// <summary>
    /// 싱글턴 인스턴스를 초기화한다.
    /// 테스트나 데이터베이스 경로 변경이 필요할 때 호출한다.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    private void ApplyOptimalSettings()
    {
        using var cmd = _connection.CreateCommand();

        // 게임 서버 환경 최적 설정
        var settings = new[]
        {
            "SET memory_limit = '4GB'",
            "SET threads = 4",
        };

        foreach (var setting in settings)
        {
            cmd.CommandText = setting;
            cmd.ExecuteNonQuery();
        }
    }

    public DuckDBConnection Connection => _connection;

    public DuckDBCommand CreateCommand()
    {
        return _connection.CreateCommand();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }
}
