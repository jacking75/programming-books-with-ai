using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeNetLite;

public interface IPacketDispatcher
{
    /// <summary>
    /// 패킷 디스패처를 초기화합니다.
    /// </summary>
    /// <param name="headerSize">패킷 헤더의 크기입니다.</param>
    void Init(ushort headerSize);

    /// <summary>
    /// 수신된 버퍼를 파싱하여 완전한 패킷들을 처리합니다.
    /// </summary>
    /// <param name="session">패킷을 수신한 세션입니다.</param>
    /// <param name="buffer">수신된 데이터가 포함된 메모리 버퍼입니다.</param>
    void DispatchPacket(Session session, ReadOnlyMemory<byte> buffer);

    /// <summary>
    /// 시스템 또는 사용자로부터의 패킷을 수신 큐에 추가합니다.
    /// </summary>
    void IncomingPacket(bool isSystem, Session user, Packet packet);

    /// <summary>
    /// 처리할 모든 패킷을 큐에서 가져옵니다.
    /// </summary>
    /// <returns>처리 대기 중인 패킷들이 담긴 큐입니다.</returns>
    Queue<Packet> DispatchAll();
}
