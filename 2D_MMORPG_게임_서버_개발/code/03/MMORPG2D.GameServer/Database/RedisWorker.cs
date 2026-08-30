using System.Collections.Concurrent;
using CloudStructures;
using CloudStructures.Structures;

namespace MMORPG2D.GameServer.Database;

public sealed class RedisWorker : IDisposable
{
    private readonly BlockingCollection<Action> _jobs = new();
    private readonly PacketDispatchWorker _packetDispatcher;
    private readonly RedisConnection _redis;
    private readonly Thread _thread;

    public RedisWorker(string connectionString, PacketDispatchWorker packetDispatcher)
    {
        _packetDispatcher = packetDispatcher;
        _redis = new RedisConnection(new RedisConfig("game", connectionString));
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "redis-worker"
        };
        _thread.Start();
    }

    public bool Post(
        GameSession session,
        SessionAccountRequest request,
        Action<GameSession, SessionAccountResult> completed)
    {
        return Post(() =>
        {
            var result = Process(request);
            _packetDispatcher.Post(() => completed(session, result));
        });
    }

    private bool Post(Action job)
    {
        try
        {
            _jobs.Add(job);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private SessionAccountResult Process(SessionAccountRequest request)
    {
        try
        {
            var sessKey = new RedisString<long>(_redis, $"sess:{request.Token}", null);
            var value = sessKey.GetAsync().GetAwaiter().GetResult();
            return value.HasValue
                ? new SessionAccountResult(0, value.Value)
                : new SessionAccountResult(1);
        }
        catch (Exception ex)
        {
            Logger.Error("Redis session lookup failed", ex);
            return new SessionAccountResult(99);
        }
    }

    private void Run()
    {
        foreach (var job in _jobs.GetConsumingEnumerable())
        {
            try
            {
                job();
            }
            catch (Exception ex)
            {
                Logger.Error("Redis job failed", ex);
            }
        }
    }

    public void Dispose()
    {
        _jobs.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(3));
        (_redis as IDisposable)?.Dispose();
        _jobs.Dispose();
    }
}
