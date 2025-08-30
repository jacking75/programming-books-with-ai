using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNetLite;

/// <summary>
/// 두개의 큐를 교체해가며 활용한다.
/// IO스레드에서 입력큐에 막 쌓아놓고,
/// 로직스레드에서 큐를 뒤바꾼뒤(swap) 쌓아놓은 패킷을 가져가 처리한다.
/// 참고 : http://roadster.egloos.com/m/4199854
/// </summary>
class DoubleBufferingQueue
{
    Queue<Packet> _queue1;
    Queue<Packet> _queue2;

    Queue<Packet> _refInput;
    Queue<Packet> _refOutput;

    // SpinLock 대신 object lock을 사용합니다.
    private readonly Lock _lock = new Lock();


    public DoubleBufferingQueue()
    {
        _queue1 = new Queue<Packet>();
        _queue2 = new Queue<Packet>();
        _refInput = _queue1;
        _refOutput = _queue2;
    }

    public void Enqueue(Packet msg)
    {
        lock (_lock)
        {
            _refInput.Enqueue(msg);
        }
    }

    public Queue<Packet> TakeAll()
    {
        swap();
        return _refOutput;
    }

    void swap()
    {
        lock (_lock)
        {
            var temp = _refInput;
            _refInput = _refOutput;
            _refOutput = temp;
        }
    }
}
