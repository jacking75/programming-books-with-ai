using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetServerLib;


// 서버 인터페이스
public interface INetworkServer
{
    void Start(int port);
    void Stop();
    IReadOnlyCollection<ISession> GetSessions();
    event Action<ISession> SessionConnected;
    event Action<ISession> SessionDisconnected;
}

// 클라이언트 인터페이스
public interface INetworkClient
{
    bool IsConnected { get; }
    void Connect(string host, int port);
    void Disconnect();
    void Send(IPacket packet);
    event Action Connected;
    event Action Disconnected;
    event Action<IPacket> PacketReceived;
}

// 세션 인터페이스
public interface ISession
{
    Guid Id { get; }
    bool IsConnected { get; }
    EndPoint RemoteEndPoint { get; }
    void Send(IPacket packet);
    void Disconnect();
    event Action<IPacket> PacketReceived;
    event Action Disconnected;
}

// 패킷 인터페이스
public interface IPacket
{
    ushort Id { get; }

    /// <summary>
    /// 패킷 데이터를 BinaryWriter를 사용해 직렬화합니다. (Id 제외)
    /// </summary>
    void Serialize(BinaryWriter writer);

    /// <summary>
    /// BinaryReader를 사용해 패킷 데이터를 역직렬화합니다. (Id 제외)
    /// </summary>
    void Deserialize(BinaryReader reader);
}

// 패킷 프로세서 인터페이스
public interface IPacketProcessor
{
    void RegisterPacket<T>() where T : IPacket, new();
    void RegisterPacketHandler<T>(Action<T, ISession> handler) where T : IPacket;
    void UnregisterPacketHandler<T>() where T : IPacket;
    IPacket ProcessPacket(byte[] data, ISession session);
    byte[] SerializePacket(IPacket packet);
}

// 메시지 프레이머 인터페이스
public interface IMessageFramer
{
    event Action<byte[]> MessageReceived;
    void ProcessReceivedData(ArraySegment<byte> data);
    byte[] FrameMessage(byte[] message);
}