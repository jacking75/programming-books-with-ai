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
    ISession GetSession(Guid sessionId);
    IReadOnlyCollection<ISession> GetSessions();
    event Action<ISession> SessionConnected;
    event Action<ISession> SessionDisconnected;
}

// 세션 인터페이스
public interface ISession
{
    Guid Id { get; }
    bool IsConnected { get; }
    EndPoint RemoteEndPoint { get; }
    void Send(IPacket packet);
    void Disconnect();
}

// 패킷 인터페이스
public interface IPacket
{
    ushort Id { get; }
    void Serialize(System.IO.BinaryWriter writer);
    void Deserialize(System.IO.BinaryReader reader);
}

// 패킷 프로세서 인터페이스
public interface IPacketProcessor
{
    void RegisterPacket<T>() where T : IPacket, new();
    void RegisterPacketHandler<T>(Action<T, ISession> handler) where T : IPacket;
    void HandlePacket(byte[] data, ISession session);
    byte[] SerializePacket(IPacket packet);
}

// 메시지 프레이머 인터페이스
public interface IMessageFramer
{
    event Action<byte[]> MessageReceived;
    void ProcessReceivedData(ArraySegment<byte> data);
    byte[] FrameMessage(byte[] message);
}