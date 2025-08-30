using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreeNetLite;

/// <summary>
/// 패킷을 수신하여 처리 큐에 추가하는 기본 디스패처 구현체입니다.
/// </summary>
public class DefaultPacketDispatcher : IPacketDispatcher
{
    private ushort _headerSize;
    private readonly DoubleBufferingQueue _messageQueue = new();


    public void Init(ushort headerSize)
    {
        _headerSize = headerSize;
    }

    public void DispatchPacket(Session session, ReadOnlyMemory<byte> buffer)
    {
        var remainBuffer = buffer;

        while (remainBuffer.Length >= _headerSize)
        {
            var headerSpan = remainBuffer.Span;
            var packetSize = FastBinaryRead.UInt16(headerSpan);

            if (packetSize > remainBuffer.Length)
            {
                break; // 완전한 패킷이 도착하지 않았습니다.
            }

            var packetId = FastBinaryRead.UInt16(headerSpan[2..]);

            var bodySize = packetSize - _headerSize;
            ReadOnlyMemory<byte>? bodyData = bodySize > 0
                ? remainBuffer.Slice(_headerSize, bodySize)
                : null;

            var packet = new Packet(session, packetId, bodyData);
            IncomingPacket(false, session, packet);

            remainBuffer = remainBuffer[packetSize..];
        }
    }

    public void IncomingPacket(bool isSystem, Session user, Packet packet)
    {
        if (!isSystem && packet.Id <= NetworkDefine.SysNtfMax)
        {
            // TODO: Log hacking attempt
            user.Close(); // 비정상적인 접근 시 연결을 종료합니다.
            return;
        }
        _messageQueue.Enqueue(packet);
    }

    public Queue<Packet> DispatchAll() => _messageQueue.TakeAll();
}