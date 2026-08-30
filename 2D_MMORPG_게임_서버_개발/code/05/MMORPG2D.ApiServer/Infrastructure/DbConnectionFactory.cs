using MySqlConnector;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace MMORPG2D.ApiServer.Infrastructure;

/// <summary>
/// 매 요청마다 새 MySqlConnection 을 만들고 SqlKata QueryFactory 로 감싸 돌려준다.
/// MySqlConnector 의 내장 풀이 실제 connection 재사용을 처리한다.
/// </summary>
public class DbConnectionFactory
{
    private readonly string _connStr;
    private readonly MySqlCompiler _compiler = new();

    public DbConnectionFactory(string connStr)
    {
        _connStr = connStr;
    }

    public QueryFactory Create()
    {
        var conn = new MySqlConnection(_connStr);
        return new QueryFactory(conn, _compiler);
    }
}
