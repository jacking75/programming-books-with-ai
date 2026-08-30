using CloudStructures;

namespace MMORPG2D.ApiServer.Infrastructure;

public class RedisProvider
{
    public RedisConnection Conn { get; }

    public RedisProvider(string connStr)
    {
        Conn = new RedisConnection(new RedisConfig("api", connStr));
    }
}
