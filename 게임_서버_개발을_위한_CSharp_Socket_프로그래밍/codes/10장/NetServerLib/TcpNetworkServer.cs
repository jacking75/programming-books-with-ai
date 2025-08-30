using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace NetServerLib;


public class TcpNetworkServer : INetworkServer
{
    private readonly int _bufferSize;
    private readonly int _maxConnections;
    private Socket _listenSocket;

    // SocketAsyncEventArgs 객체들을 재사용하기 위한 풀
    private readonly Stack<SocketAsyncEventArgs> _acceptEventArgsPool;
    private readonly Stack<SocketAsyncEventArgs> _receiveEventArgsPool;

    private readonly ConcurrentDictionary<Guid, ISession> _sessions = new ConcurrentDictionary<Guid, ISession>();
    private readonly Action<ISession, byte[]> _onPacketReceived;
    private readonly IPacketProcessor _packetProcessor;

    public event Action<ISession> SessionConnected;
    public event Action<ISession> SessionDisconnected;

    public TcpNetworkServer(int maxConnections, int bufferSize, IPacketProcessor packetProcessor, Action<ISession, byte[]> onPacketReceived)
    {
        _maxConnections = maxConnections;
        _bufferSize = bufferSize;
        _packetProcessor = packetProcessor;
        _onPacketReceived = onPacketReceived;

        _acceptEventArgsPool = new Stack<SocketAsyncEventArgs>(maxConnections);
        _receiveEventArgsPool = new Stack<SocketAsyncEventArgs>(maxConnections);

        // SAEA 풀 초기화
        for (int i = 0; i < _maxConnections; i++)
        {
            var acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            _acceptEventArgsPool.Push(acceptEventArg);

            var receiveEventArg = new SocketAsyncEventArgs();
            receiveEventArg.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            receiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            _receiveEventArgsPool.Push(receiveEventArg);
        }
    }
    
    public void Start(int port)
    {
        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
        _listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(localEndPoint);
        _listenSocket.Listen(100);

        Console.WriteLine($"Server started on port {port}. Listening for connections...");
        StartAccept(null);
    }

    private void StartAccept(SocketAsyncEventArgs acceptEventArg)
    {
        if (acceptEventArg == null)
        {
            // 풀에서 Accept SAEA를 가져옴
            if (!_acceptEventArgsPool.TryPop(out acceptEventArg))
            {
                // 모든 SAEA가 사용 중이면 잠시 후 다시 시도 (실제로는 이런 일이 드물어야 함)
                    Console.WriteLine("Warning: No available SocketAsyncEventArgs for accept operation.");
                return;
            }
        }
        else
        {
            // 재사용을 위해 이전 소켓 정보 초기화
            acceptEventArg.AcceptSocket = null;
        }

        try
        {
            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }
        catch (ObjectDisposedException) { /* 서버가 멈추는 중 */ }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting accept: {ex.Message}");
        }
    }
    
    private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
    {
        ProcessAccept(e);
    }

    private void ProcessAccept(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            Socket clientSocket = e.AcceptSocket;
            
            // 새 클라이언트를 처리
            var session = new TcpSession(clientSocket, OnSessionDisconnected, _onPacketReceived, _packetProcessor);
            
            if (_sessions.TryAdd(session.Id, session))
            {
                SessionConnected?.Invoke(session);
                Console.WriteLine($"Session connected: {session.Id} | Remote: {session.RemoteEndPoint}");

                // 이 세션을 위한 Receive SAEA를 풀에서 가져옴
                if (_receiveEventArgsPool.TryPop(out SocketAsyncEventArgs receiveEventArgs))
                {
                    receiveEventArgs.UserToken = session;
                    StartReceive(receiveEventArgs);
                }
                else
                {
                        Console.WriteLine($"Warning: No available SocketAsyncEventArgs for receive on session {session.Id}. Disconnecting.");
                    session.Disconnect();
                }
            }
            else
            {
                Console.WriteLine("Failed to add session to dictionary. Disconnecting client.");
                clientSocket.Close();
            }
        }
        else
        {
            Console.WriteLine($"Accept error: {e.SocketError}");
        }

        // 다음 연결을 받기 위해 다시 Accept 시작
        StartAccept(e);
    }

    private void StartReceive(SocketAsyncEventArgs e)
    {
        var session = (TcpSession)e.UserToken;

        try
        {
            bool willRaiseEvent = session.IsConnected && session.RemoteEndPoint != null && ((Socket)session.GetType().GetField("_socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(session)).ReceiveAsync(e);
            if (!willRaiseEvent)
            {
                ProcessReceive(e);
            }
        }
        catch (ObjectDisposedException)
        {
                // 세션이 이미 닫혔을 수 있음
                ReturnReceiveEventArgs(e);
        }
        catch (Exception ex)
        {
                Console.WriteLine($"Error starting receive for session {session.Id}: {ex.Message}");
                session.Close();
                ReturnReceiveEventArgs(e);
        }
    }
    
    private void IO_Completed(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                ProcessReceive(e);
                break;
            default:
                throw new ArgumentException("The last operation completed on the socket was not a receive");
        }
    }

    private void ProcessReceive(SocketAsyncEventArgs e)
    {
        var session = (TcpSession)e.UserToken;
        if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            session.ProcessReceive(e.Buffer, e.Offset, e.BytesTransferred);
            StartReceive(e); // 다음 데이터 수신 시작
        }
        else
        {
            // 연결 종료
            session.Close();
            ReturnReceiveEventArgs(e);
        }
    }
    
    private void OnSessionDisconnected(TcpSession session)
    {
        if (_sessions.TryRemove(session.Id, out _))
        {
            SessionDisconnected?.Invoke(session);
            Console.WriteLine($"Session disconnected: {session.Id} | Remote: {session.RemoteEndPoint}");
        }
    }
    
    private void ReturnReceiveEventArgs(SocketAsyncEventArgs e)
    {
        if (e == null) return;
        e.UserToken = null;
        _receiveEventArgsPool.Push(e);
    }

    public void Stop()
    {
        Console.WriteLine("Stopping server...");
        _listenSocket?.Close();
        _listenSocket = null;

        foreach (var session in _sessions.Values.ToList())
        {
            session.Disconnect();
        }
        _sessions.Clear();
        
        // SAEA 풀 정리
        while(_acceptEventArgsPool.TryPop(out var args)) args.Dispose();
        while(_receiveEventArgsPool.TryPop(out var args)) args.Dispose();
        
        Console.WriteLine("Server stopped.");
    }

    public ISession GetSession(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public IReadOnlyCollection<ISession> GetSessions()
    {
        return _sessions.Values.ToList().AsReadOnly();
    }
}