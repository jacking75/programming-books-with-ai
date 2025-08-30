using System;
using System.Collections.Generic;

using FreeNetLite;
using System.Collections.Concurrent;

namespace EchoServerIOThreadPacketProcess;

class IoThreadPacketDispatcher : IPacketDispatcher
{
    // 헤더는 패킷 전체 크기(2바이트)와 ID(2바이트)로 구성됩니다.
    int _packetHeaderSize = 4;
    ConcurrentDictionary<UInt64, GameUser> _userList = new();


    // 여기에서는 사용하지 않지만, 인터페이스 구현을 위해 필요합니다.
    public Queue<Packet> DispatchAll() { return null; }

    public void Init(UInt16 headerSize) 
    {
        _packetHeaderSize = headerSize;
    }

    // DispatchPacket이 ReadOnlyMemory<byte>를 받도록 수정합니다.
    public void DispatchPacket(Session session, ReadOnlyMemory<byte> buffer)
    {
        var remainBuffer = buffer;

        while (remainBuffer.Length >= _packetHeaderSize)
        {
            var headerSpan = remainBuffer.Span;
            var packetSize = FastBinaryRead.UInt16(headerSpan);

            if (packetSize > remainBuffer.Length)
            {
                break;
            }

            var packetId = FastBinaryRead.UInt16(headerSpan.Slice(2));

            var bodySize = packetSize - _packetHeaderSize;
            ReadOnlyMemory<byte>? bodyData = null;
            if (bodySize > 0)
            {
                bodyData = remainBuffer.Slice(_packetHeaderSize, bodySize);
            }

            var packet = new Packet(session, packetId, bodyData);
            IncomingPacket(false, session, packet);

            remainBuffer = remainBuffer.Slice(packetSize);
        }
    }

    public void IncomingPacket(bool IsSystem, Session user, Packet packet)
    {
        if (IsSystem == false && (packet.Id <= NetworkDefine.SysNtfMax))
        {
            // 해킹 의심
            return;
        }

        var protocol = (ProtocolId)packet.Id;

        switch (protocol)
        {
            case ProtocolId.Echo:
                {
                    if (packet.BodyData.HasValue)
                    {
                        var bodySpan = packet.BodyData.Value.Span;

                        var requestPkt = new EchoPacket();
                        requestPkt.Decode(bodySpan);
                        Console.WriteLine($"text {requestPkt.Data}");

                        var responsePkt = new EchoPacket();
                        // ToPacket 메서드는 byte[]를 인자로 받으므로 ToArray()로 변환합니다.
                        var packetData = responsePkt.ToPacket(ProtocolId.Echo, bodySpan.ToArray());
                        packet.Owner.Send(new ArraySegment<byte>(packetData));
                    }
                }
                break;
            default:
                if (OnSystemPacket(packet) == false)
                {
                    Console.WriteLine("Unknown protocol id " + protocol);
                }
                break;
        }
    }

    bool OnSystemPacket(Packet packet)
    {
        var session = packet.Owner;

        switch (packet.Id)
        {
            case NetworkDefine.SysNtfConnected:
                Console.WriteLine("SYS_NTF_CONNECTED : " + session.UniqueId);
                var user = new GameUser(session);
                _userList.TryAdd(session.UniqueId, user);
                return true;

            case NetworkDefine.SysNtfClosed:
                Console.WriteLine("SYS_NTF_CLOSED : " + session.UniqueId);
                _userList.TryRemove(session.UniqueId, out _);
                return true;
        }
        return false;
    }
}