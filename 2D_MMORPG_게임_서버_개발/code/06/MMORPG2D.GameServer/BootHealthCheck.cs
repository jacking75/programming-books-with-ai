using CloudStructures;
using CloudStructures.Structures;
using MySqlConnector;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace MMORPG2D.GameServer;

/// <summary>
/// 서버 시동 직후 DB / Redis 가 살아 있는지 확인하는 가벼운 헬스 체크.
/// 학습용으로 결과만 콘솔에 찍는다.
/// </summary>
public static class BootHealthCheck
{
    public static async Task RunAsync(string mysqlConnStr, string redisConnStr)
    {
        await CheckMySqlAsync(mysqlConnStr);
        await CheckRedisAsync(redisConnStr);
    }

    private static async Task CheckMySqlAsync(string connStr)
    {
        try
        {
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            using var db = new QueryFactory(conn, new MySqlCompiler());
            var rows = await db.SelectAsync<DateTime>("SELECT UTC_TIMESTAMP()");
            Logger.Info($"DB ok. server utc = {rows.First():yyyy-MM-dd HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Logger.Error("DB check failed", ex);
        }
    }

    private static async Task CheckRedisAsync(string connStr)
    {
        try
        {
            var redis = new RedisConnection(new RedisConfig("local", connStr));
            var key = new RedisString<string>(redis, "boot:hello", TimeSpan.FromMinutes(1));
            await key.SetAsync("server-on");
            var v = await key.GetAsync();
            Logger.Info($"Redis ok. boot:hello = {v.Value}");
        }
        catch (Exception ex)
        {
            Logger.Error("Redis check failed", ex);
        }
    }
}
