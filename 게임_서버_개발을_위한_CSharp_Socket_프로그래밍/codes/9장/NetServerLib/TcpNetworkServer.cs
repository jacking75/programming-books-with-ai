using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace NetServerLib;


public class TcpNetworkServer : INetworkServer
{
    private readonly Func<IMessageFramer> _messageFramerFactory;
    private readonly IPacketProcessor _packetProcessor;
    private readonly ConcurrentDictionary<Guid, ISession> _sessions = new();
    private TcpListener _listener;
    private Thread _acceptThread;
    private bool _isRunning = false;

    // 패킷 처리를 외부로 위임하기 위한 콜백 추가
    private readonly Action<ISession, byte[]> _onPacketReceived;

    public event Action<ISession> SessionConnected;
    public event Action<ISession> SessionDisconnected;

    public TcpNetworkServer(Func<IMessageFramer> messageFramerFactory, IPacketProcessor packetProcessor, Action<ISession, byte[]> onPacketReceived)
    {
        _messageFramerFactory = messageFramerFactory;
        _packetProcessor = packetProcessor;
        _onPacketReceived = onPacketReceived; // 콜백 저장
    }

    public void Start(int port)
    {
        if (_listener != null)
        {
            throw new InvalidOperationException("Server is already running.");
        }

        _listener = new TcpListener(IPAddress.Any, port);
        _isRunning = true;
        _listener.Start();
        
        _acceptThread = new Thread(AcceptLoop);
        _acceptThread.Start();
        
        Console.WriteLine($"Server started on port {port}.");
    }

    private void AcceptLoop()
    {
        try
        {
            while (_isRunning)
            {
                var tcpClient = _listener.AcceptTcpClient();
                Console.WriteLine("Client connected.");
                // 클라이언트 처리를 위한 새 스레드 시작
                var clientThread = new Thread(() => HandleNewClient(tcpClient));
                clientThread.Start();
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
        {
            // 정상적인 종료
            Console.WriteLine("Server stopping gracefully.");
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine($"Server error in AcceptLoop: {ex.Message}");
            }
        }
    }

    private void HandleNewClient(TcpClient tcpClient)
    {
        try
        {
            var messageFramer = _messageFramerFactory();
            // TcpSession 생성 시 콜백 전달
            var session = new TcpSession(tcpClient, messageFramer, _packetProcessor, _onPacketReceived);

            if (!_sessions.TryAdd(session.Id, session))
            {
                session.Disconnect();
                return;
            }

            session.Disconnected += () =>
            {
                if (_sessions.TryRemove(session.Id, out _))
                {
                    SessionDisconnected?.Invoke(session);
                    Console.WriteLine($"Session disconnected: {session.Id} | Remote: {session.RemoteEndPoint}");
                }
            };

            SessionConnected?.Invoke(session);
            Console.WriteLine($"Session connected: {session.Id} | Remote: {session.RemoteEndPoint}");

            // 세션의 수신 루프를 시작 (내부적으로 스레드 생성)
            session.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling new client: {ex.Message}");
            tcpClient?.Close();
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _listener.Stop();
        
        _acceptThread?.Join(); // Accept 스레드가 끝날 때까지 대기

        var disconnectTasks = _sessions.Values.ToList();
        foreach (var session in disconnectTasks)
        {
            session.Disconnect();
        }

        _sessions.Clear();
        _listener = null;
        Console.WriteLine("Server stopped.");
    }

    public IReadOnlyCollection<ISession> GetSessions()
    {
        return _sessions.Values.ToList().AsReadOnly();
    }
}
