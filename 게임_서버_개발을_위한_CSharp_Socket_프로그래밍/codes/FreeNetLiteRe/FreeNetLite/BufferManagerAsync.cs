using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FreeNetLite;

public class BufferManagerAsync
{
    byte[] _totalBuffer;
    ConcurrentBag<int> _freeIndexPool = new();
    int _takeBufferSize;


    public void Init(int bufferCount, int bufferSize)
    {
        int TotalBytes = bufferCount * bufferSize;
        _takeBufferSize = bufferSize;
        _totalBuffer = new byte[TotalBytes];

        var count = TotalBytes / _takeBufferSize;
        for (int i = 0; i < count; ++i)
        {
            _freeIndexPool.Add((i * _takeBufferSize));
        }
    }

    public bool SetBuffer(SocketAsyncEventArgs args)
    {
        if (_freeIndexPool.TryTake(out int index))
        {
            args.SetBuffer(_totalBuffer, index, _takeBufferSize);
            return true;
        }

        return false;
    }

   
}
