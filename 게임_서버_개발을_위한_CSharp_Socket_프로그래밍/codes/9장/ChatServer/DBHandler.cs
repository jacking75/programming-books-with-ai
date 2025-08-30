using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetServerLib;

namespace ChatServer;


public class DBHandler
{
    private readonly BlockingCollection<Action> _dbRequestQueue = new();
    private readonly PacketProcessor _packetProcessor;

    public Action<ISession, IPacket> EnqueueResult { get; }

    public DBHandler(PacketProcessor packetProcessor, Action<ISession, IPacket> enqueueResultCallback)
    {
        _packetProcessor = packetProcessor;
        EnqueueResult = enqueueResultCallback;
    }

    public void EnqueueRequest(Action request)
    {
        if (!_dbRequestQueue.IsAddingCompleted)
        {
            try
            {
                _dbRequestQueue.Add(request);
            }
            catch (InvalidOperationException) { /* Queue is closed */ }
        }
    }

    public void Process()
    {
        while (!_dbRequestQueue.IsCompleted)
        {
            try
            {
                var request = _dbRequestQueue.Take();
                request();
            }
            catch (InvalidOperationException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB processing error: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        _dbRequestQueue.CompleteAdding();
    }
}