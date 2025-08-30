// See https://aka.ms/new-console-template for more information

using ChatServer.Packets;
using NetServerLib;
using System.Collections.Concurrent;

IPacketProcessor _packetProcessor = new PacketProcessor(); 
ConcurrentDictionary<Guid, PlayerInfo> _players = new ConcurrentDictionary<Guid, PlayerInfo>();

Console.Title = "Chat Server";
            

// 최대 연결 1000개, 버퍼 크기 4096으로 서버 초기화
INetworkServer _server = new TcpNetworkServer(1000, 4096, _packetProcessor, OnPacketReceived);

_server.SessionConnected += OnSessionConnected;
_server.SessionDisconnected += OnSessionDisconnected;


RegisterPacketsAndHandlers();


_server.Start(7777);

Console.WriteLine("Type 'exit' to stop the server.");
while (Console.ReadLine()?.ToLower() != "exit")
{
    // 서버 실행 중
}

_server.Stop();


/// <summary>
/// 패킷 타입과 핸들러를 등록합니다.
/// </summary>
void RegisterPacketsAndHandlers()
{
    _packetProcessor.RegisterPacket<LoginRequestPacket>();
    _packetProcessor.RegisterPacket<ChatMessagePacket>();

    _packetProcessor.RegisterPacketHandler<LoginRequestPacket>(OnLoginRequest);
    _packetProcessor.RegisterPacketHandler<ChatMessagePacket>(OnChatMessage);
}

/// <summary>
/// 모든 세션으로부터 오는 패킷 데이터를 처리하는 중앙 콜백입니다.
/// </summary>
void OnPacketReceived(ISession session, byte[] data)
{
    _packetProcessor.HandlePacket(data, session);
}

void OnSessionConnected(ISession session)
{
    Console.WriteLine($"[System] Client connected: {session.RemoteEndPoint}");
}

void OnSessionDisconnected(ISession session)
{
    if (_players.TryRemove(session.Id, out var player))
    {
        Console.WriteLine($"[System] {player.Username} has disconnected.");
        var notification = new ChatMessagePacket
        {
            Username = "System",
            Message = $"{player.Username} has left the chat."
        };
        Broadcast(notification, null);
    }
}

/// <summary>
/// 로그인 요청을 처리합니다.
/// </summary>
void OnLoginRequest(LoginRequestPacket packet, ISession session)
{
    Console.WriteLine($"[Login] {packet.Username} attempts to login from {session.RemoteEndPoint}");
    var player = new PlayerInfo { SessionId = session.Id, Username = packet.Username };

    if (_players.TryAdd(session.Id, player))
    {
        var notification = new ChatMessagePacket
        {
            Username = "System",
            Message = $"{player.Username} has joined the chat."
        };
        Broadcast(notification, session.Id);

        var welcomeMessage = new ChatMessagePacket
        {
            Username = "System",
            Message = $"Welcome, {player.Username}!"
        };
        session.Send(welcomeMessage);
    }
}

/// <summary>
/// 채팅 메시지를 처리하고 브로드캐스트합니다.
/// </summary>
void OnChatMessage(ChatMessagePacket packet, ISession session)
{
    if (_players.TryGetValue(session.Id, out var player))
    {
        Console.WriteLine($"[{player.Username}]: {packet.Message}");
        var broadcastPacket = new ChatMessagePacket
        {
            Username = player.Username,
            Message = packet.Message
        };
        Broadcast(broadcastPacket, session.Id);
    }
}

/// <summary>
/// 특정 세션을 제외한 모든 클라이언트에게 패킷을 전송합니다.
/// </summary>
void Broadcast(IPacket packet, Guid? excludeSessionId)
{
    foreach (var session in _server.GetSessions())
    {
        if (session.IsConnected && session.Id != excludeSessionId)
        {
            session.Send(packet);
        }
    }
}




public class PlayerInfo
{
    public Guid SessionId { get; set; }
    public string Username { get; set; }
}