using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Network;



// 네트워크 매니저 - 서버의 네트워크 작업 관리
public class NetworkManager
{
    private INetworkListener _listener;
    private Dictionary<string, IConnection> _connections;
    private Dictionary<int, IPacketHandler> _packetHandlers;

    public NetworkManager(INetworkListener listener)
    {
        _listener = listener;
        _connections = new Dictionary<string, IConnection>();
        _packetHandlers = new Dictionary<int, IPacketHandler>();

        // 이벤트 등록
        _listener.OnClientConnected += OnClientConnected;
        _listener.OnDataReceived += OnDataReceived;
        _listener.OnClientDisconnected += OnClientDisconnected;
    }

    public async Task StartAsync(int port)
    {
        await _listener.StartAsync(port);
        Console.WriteLine($"서버가 포트 {port}에서 시작되었습니다.");
    }

    public void RegisterPacketHandler(int packetId, IPacketHandler handler)
    {
        _packetHandlers[packetId] = handler;
    }

    private void OnClientConnected(IConnection connection)
    {
        _connections[connection.ConnectionId] = connection;
        Console.WriteLine($"클라이언트 연결: {connection.ConnectionId}");
    }

    private void OnDataReceived(IConnection connection, byte[] data)
    {
        // 실제 구현에서는 패킷 헤더에서 packetId를 추출해야 함
        // 간단한 예시를 위해 첫 번째 바이트를 packetId로 가정
        if (data.Length > 0)
        {
            int packetId = data[0];
            if (_packetHandlers.TryGetValue(packetId, out var handler))
            {
                handler.HandlePacket(connection, data);
            }
        }
    }

    private void OnClientDisconnected(IConnection connection)
    {
        _connections.Remove(connection.ConnectionId);
        Console.WriteLine($"클라이언트 연결 해제: {connection.ConnectionId}");
    }

    public async Task BroadcastAsync(byte[] data)
    {
        foreach (var connection in _connections.Values)
        {
            if (connection.IsConnected)
            {
                await connection.SendAsync(data);
            }
        }
    }
}
