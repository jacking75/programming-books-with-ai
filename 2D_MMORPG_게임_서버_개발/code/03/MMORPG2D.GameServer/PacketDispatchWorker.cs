using System.Collections.Concurrent;

namespace MMORPG2D.GameServer;

public sealed class PacketDispatchWorker : IDisposable
{
    private readonly PacketHandler _handler;
    private readonly BlockingCollection<Action> _jobs = new();
    private readonly Thread _thread;

    public PacketDispatchWorker(PacketHandler handler)
    {
        _handler = handler;
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "packet-dispatch-worker"
        };
        _thread.Start();
    }

    public bool Post(GameSession session, GameRequestInfo request)
        => Post(() => _handler.Dispatch(session, request));

    public bool Post(Action job)
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
                Logger.Error("packet dispatch job failed", ex);
            }
        }
    }

    public void Dispose()
    {
        _jobs.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(3));
        _jobs.Dispose();
    }
}
