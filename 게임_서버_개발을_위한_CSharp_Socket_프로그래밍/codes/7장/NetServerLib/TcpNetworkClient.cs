using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetServerLib;


public class TcpNetworkClient : INetworkClient
{
    private readonly Func<IMessageFramer> _messageFramerFactory;
    private readonly IPacketProcessor _packetProcessor;
    private TcpClient _tcpClient;
    private TcpSession _session;

    public bool IsConnected => _session?.IsConnected ?? false;

    public event Action Connected;
    public event Action Disconnected;
    public event Action<IPacket> PacketReceived;

    public TcpNetworkClient(Func<IMessageFramer> messageFramerFactory, IPacketProcessor packetProcessor)
    {
        _messageFramerFactory = messageFramerFactory;
        _packetProcessor = packetProcessor;
    }

    public void Connect(string host, int port)
    {
        if (IsConnected)
        {
            throw new InvalidOperationException("Already connected.");
        }

        try
        {
            _tcpClient = new TcpClient();
            // ConnectAsync 대신 동기 Connect 사용
            _tcpClient.Connect(host, port);

            // TcpSession 생성 시, 받은 패킷을 PacketReceived 이벤트로 전달하는 람다를 제공합니다.
            // 서버와 달리 클라이언트는 보통 패킷 처리용 중앙 큐가 필요 없습니다.
            _session = new TcpSession(_tcpClient, _messageFramerFactory(), _packetProcessor, (session, data) =>
            {
                var packet = _packetProcessor.ProcessPacket(data, session);
                PacketReceived?.Invoke(packet);
            });

            _session.Disconnected += () =>
            {
                _session = null;
                _tcpClient = null;
                Disconnected?.Invoke();
            };

            // 세션의 수신 루프를 별도의 스레드에서 시작합니다.
            _session.Start();
            Connected?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to {host}:{port}. Error: {ex.Message}");
            _session = null;
            _tcpClient?.Close();
            _tcpClient = null;
        }
    }

    public void Disconnect()
    {
        if (_session != null)
        {
            // 동기 Disconnect 호출
            _session.Disconnect();
        }
    }

    public void Send(IPacket packet)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to the server.");
        }
        // 동기 Send 호출
        _session.Send(packet);
    }
}
