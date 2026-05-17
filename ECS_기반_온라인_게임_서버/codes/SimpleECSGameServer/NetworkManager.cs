using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;


// 네트워크 인터페이스 (간단한 구현)
public interface INetworkManager
{
    void SendMessageToClient(uint clientId, string message);
    void BroadcastMessage(string message);
    event Action<uint, string> OnMessageReceived;
}

// 간단한 네트워크 매니저 구현
public class SimpleNetworkManager : INetworkManager
{
    public void SendMessageToClient(uint clientId, string message)
    {
        Console.WriteLine($"[서버 -> 클라이언트 {clientId}] {message}");
    }

    public void BroadcastMessage(string message)
    {
        Console.WriteLine($"[서버 -> 모든 클라이언트] {message}");
    }

    public event Action<uint, string> OnMessageReceived;

    // 클라이언트 메시지 시뮬레이션
    public void SimulateClientMessage(uint clientId, string message)
    {
        OnMessageReceived?.Invoke(clientId, message);
    }
}