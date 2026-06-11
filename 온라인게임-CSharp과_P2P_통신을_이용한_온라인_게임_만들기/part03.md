# C#과 P2P 통신을 이용한 온라인 게임 만들기

저자: 최흥배, AI-Assisted   
  
------  
  
# 6장: C# 홀펀칭 라이브러리 개발
  
## 6.1 .NET 9의 네트워킹 개선사항 활용
.NET 9에서는 네트워킹 성능과 관련된 여러 개선사항이 도입되었습니다. P2P 홀펀칭 구현에 특히 유용한 기능들을 살펴보겠습니다.

### 향상된 UDP 소켓 성능
.NET 9에서는 UDP 소켓의 성능이 크게 향상되었으며, 새로운 `UdpClient` API가 추가되었습니다. 이를 활용한 기본 UDP 클라이언트 클래스를 구현해보겠습니다.

```csharp
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

public class EnhancedUdpClient : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly ConcurrentQueue<(IPEndPoint endpoint, byte[] data)> _sendQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed = false;

    public event Action<IPEndPoint, byte[]>? DataReceived;
    public IPEndPoint? LocalEndPoint => _udpClient.Client.LocalEndPoint as IPEndPoint;

    public EnhancedUdpClient(int port = 0)
    {
        _udpClient = new UdpClient(port);
        _sendQueue = new ConcurrentQueue<(IPEndPoint, byte[])>();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // .NET 9의 향상된 소켓 옵션 설정
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, false);
        
        StartReceiving();
        StartSending();
    }

    private async void StartReceiving()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(_cancellationTokenSource.Token);
                DataReceived?.Invoke(result.RemoteEndPoint, result.Buffer);
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 종료
        }
        catch (Exception ex)
        {
            Console.WriteLine($"수신 오류: {ex.Message}");
        }
    }

    private async void StartSending()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_sendQueue.TryDequeue(out var item))
                {
                    await _udpClient.SendAsync(item.data, item.endpoint, _cancellationTokenSource.Token);
                }
                else
                {
                    await Task.Delay(1, _cancellationTokenSource.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 종료
        }
        catch (Exception ex)
        {
            Console.WriteLine($"송신 오류: {ex.Message}");
        }
    }

    public void SendAsync(IPEndPoint endpoint, byte[] data)
    {
        if (!_disposed)
        {
            _sendQueue.Enqueue((endpoint, data));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cancellationTokenSource.Cancel();
            _udpClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
```
  

## 6.2 비동기 소켓 프로그래밍과 홀펀칭
홀펀칭은 본질적으로 비동기 작업입니다. 여러 대상과 동시에 연결을 시도하고, 시간 제한을 두고 응답을 기다려야 합니다.

### STUN 클라이언트 구현
먼저 STUN 프로토콜을 구현하여 공인 IP와 포트를 발견하는 기능을 만들어보겠습니다.

```csharp
using System.Net;
using System.Security.Cryptography;

public class StunClient
{
    private const ushort STUN_BINDING_REQUEST = 0x0001;
    private const ushort STUN_BINDING_RESPONSE = 0x0101;
    private const ushort STUN_MAPPED_ADDRESS = 0x0001;
    private const ushort STUN_XOR_MAPPED_ADDRESS = 0x0020;
    
    private readonly EnhancedUdpClient _udpClient;
    private readonly List<IPEndPoint> _stunServers;

    public StunClient(EnhancedUdpClient udpClient)
    {
        _udpClient = udpClient;
        _stunServers = new List<IPEndPoint>
        {
            new IPEndPoint(IPAddress.Parse("64.233.165.127"), 19302), // Google STUN
            new IPEndPoint(IPAddress.Parse("23.21.150.121"), 3478),   // Amazon STUN
            new IPEndPoint(IPAddress.Parse("74.125.250.129"), 19302)  // Google STUN 2
        };
    }

    public async Task<IPEndPoint?> GetPublicEndPointAsync(TimeSpan timeout = default)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(5);

        var tasks = _stunServers.Select(server => QueryStunServerAsync(server, timeout)).ToArray();
        
        try
        {
            var completedTask = await Task.WhenAny(tasks);
            return await completedTask;
        }
        catch
        {
            return null;
        }
    }

    private async Task<IPEndPoint?> QueryStunServerAsync(IPEndPoint stunServer, TimeSpan timeout)
    {
        var transactionId = GenerateTransactionId();
        var request = CreateBindingRequest(transactionId);
        
        var tcs = new TaskCompletionSource<IPEndPoint?>();
        var cancellationToken = new CancellationTokenSource(timeout);
        
        cancellationToken.Token.Register(() => tcs.TrySetResult(null));

        void OnDataReceived(IPEndPoint sender, byte[] data)
        {
            if (sender.Equals(stunServer))
            {
                var endpoint = ParseBindingResponse(data, transactionId);
                if (endpoint != null)
                {
                    tcs.TrySetResult(endpoint);
                }
            }
        }

        _udpClient.DataReceived += OnDataReceived;
        
        try
        {
            _udpClient.SendAsync(stunServer, request);
            return await tcs.Task;
        }
        finally
        {
            _udpClient.DataReceived -= OnDataReceived;
            cancellationToken.Dispose();
        }
    }

    private byte[] CreateBindingRequest(byte[] transactionId)
    {
        var packet = new List<byte>();
        
        // STUN Header
        packet.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)STUN_BINDING_REQUEST)));
        packet.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)0))); // Length
        packet.AddRange(new byte[] { 0x21, 0x12, 0xA4, 0x42 }); // Magic Cookie
        packet.AddRange(transactionId);
        
        return packet.ToArray();
    }

    private IPEndPoint? ParseBindingResponse(byte[] data, byte[] expectedTransactionId)
    {
        if (data.Length < 20) return null;
        
        var messageType = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
        if (messageType != STUN_BINDING_RESPONSE) return null;
        
        // Transaction ID 확인
        var transactionId = data.Skip(8).Take(12).ToArray();
        if (!transactionId.SequenceEqual(expectedTransactionId)) return null;
        
        var messageLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
        var offset = 20;
        
        while (offset < data.Length)
        {
            if (offset + 4 > data.Length) break;
            
            var attributeType = IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(data, offset));
            var attributeLength = IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(data, offset + 2));
            offset += 4;
            
            if (attributeType == STUN_XOR_MAPPED_ADDRESS && attributeLength >= 8)
            {
                return ParseXorMappedAddress(data, offset);
            }
            else if (attributeType == STUN_MAPPED_ADDRESS && attributeLength >= 8)
            {
                return ParseMappedAddress(data, offset);
            }
            
            offset += attributeLength;
            offset = (offset + 3) & ~3; // 4바이트 정렬
        }
        
        return null;
    }

    private IPEndPoint? ParseXorMappedAddress(byte[] data, int offset)
    {
        if (offset + 8 > data.Length) return null;
        
        var family = IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(data, offset + 1));
        if (family != 0x01) return null; // IPv4만 지원
        
        var xorPort = IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(data, offset + 2));
        var port = xorPort ^ 0x2112;
        
        var xorAddress = BitConverter.ToUInt32(data, offset + 4);
        var address = xorAddress ^ 0x2112A442;
        
        return new IPEndPoint(new IPAddress(BitConverter.GetBytes(address)), port);
    }

    private IPEndPoint? ParseMappedAddress(byte[] data, int offset)
    {
        if (offset + 8 > data.Length) return null;
        
        var family = IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(data, offset + 1));
        if (family != 0x01) return null;
        
        var port = IPAddress.NetworkToHostOrder(BitConverter.ToUInt16(data, offset + 2));
        var address = BitConverter.ToUInt32(data, offset + 4);
        
        return new IPEndPoint(new IPAddress(BitConverter.GetBytes(address)), port);
    }

    private byte[] GenerateTransactionId()
    {
        var transactionId = new byte[12];
        RandomNumberGenerator.Fill(transactionId);
        return transactionId;
    }
}
```
  

## 6.3 홀펀칭 상태 머신 설계
홀펀칭 과정은 여러 단계를 거치므로 상태 머신으로 관리하는 것이 효율적입니다.

### 홀펀칭 상태 정의

```csharp
public enum HolePunchingState
{
    Idle,
    DiscoveringPublicEndpoint,
    RequestingIntroduction,
    WaitingForIntroduction,
    SendingPing,
    WaitingForPong,
    Connected,
    Failed,
    Timeout
}

public class HolePunchingSession
{
    public string SessionId { get; }
    public IPEndPoint? LocalEndPoint { get; set; }
    public IPEndPoint? PublicEndPoint { get; set; }
    public IPEndPoint? TargetPublicEndPoint { get; set; }
    public IPEndPoint? TargetLocalEndPoint { get; set; }
    public HolePunchingState State { get; set; }
    public DateTime LastStateChange { get; set; }
    public int RetryCount { get; set; }
    public string? PeerId { get; set; }

    public HolePunchingSession(string sessionId)
    {
        SessionId = sessionId;
        State = HolePunchingState.Idle;
        LastStateChange = DateTime.UtcNow;
        RetryCount = 0;
    }
}
```

### 홀펀칭 매니저 구현

```csharp
public class HolePunchingManager : IDisposable
{
    private readonly EnhancedUdpClient _udpClient;
    private readonly StunClient _stunClient;
    private readonly ConcurrentDictionary<string, HolePunchingSession> _sessions;
    private readonly Timer _timeoutTimer;
    private readonly IPEndPoint _coordinationServer;
    
    private const int MAX_RETRY_COUNT = 5;
    private const int PING_INTERVAL_MS = 500;
    private const int SESSION_TIMEOUT_MS = 30000;

    public event Action<string, IPEndPoint>? ConnectionEstablished;
    public event Action<string, string>? ConnectionFailed;

    public HolePunchingManager(EnhancedUdpClient udpClient, IPEndPoint coordinationServer)
    {
        _udpClient = udpClient;
        _stunClient = new StunClient(udpClient);
        _sessions = new ConcurrentDictionary<string, HolePunchingSession>();
        _coordinationServer = coordinationServer;
        
        _udpClient.DataReceived += OnDataReceived;
        
        _timeoutTimer = new Timer(CheckTimeouts, null, 1000, 1000);
    }

    public async Task<bool> ConnectToPeerAsync(string peerId)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new HolePunchingSession(sessionId)
        {
            PeerId = peerId,
            LocalEndPoint = _udpClient.LocalEndPoint
        };

        _sessions[sessionId] = session;

        try
        {
            // 1단계: 공인 엔드포인트 발견
            await TransitionToState(session, HolePunchingState.DiscoveringPublicEndpoint);
            session.PublicEndPoint = await _stunClient.GetPublicEndPointAsync();
            
            if (session.PublicEndPoint == null)
            {
                await TransitionToState(session, HolePunchingState.Failed);
                return false;
            }

            // 2단계: 코디네이션 서버에 연결 요청
            await TransitionToState(session, HolePunchingState.RequestingIntroduction);
            await RequestIntroduction(session);

            // 연결 결과 대기
            var timeout = TimeSpan.FromMilliseconds(SESSION_TIMEOUT_MS);
            var startTime = DateTime.UtcNow;
            
            while (session.State != HolePunchingState.Connected && 
                   session.State != HolePunchingState.Failed &&
                   DateTime.UtcNow - startTime < timeout)
            {
                await Task.Delay(100);
            }

            return session.State == HolePunchingState.Connected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"홀펀칭 오류: {ex.Message}");
            await TransitionToState(session, HolePunchingState.Failed);
            return false;
        }
        finally
        {
            _sessions.TryRemove(sessionId, out _);
        }
    }

    private async Task TransitionToState(HolePunchingSession session, HolePunchingState newState)
    {
        var oldState = session.State;
        session.State = newState;
        session.LastStateChange = DateTime.UtcNow;
        
        Console.WriteLine($"세션 {session.SessionId}: {oldState} -> {newState}");
        
        switch (newState)
        {
            case HolePunchingState.SendingPing:
                await StartPingProcess(session);
                break;
            case HolePunchingState.Failed:
                ConnectionFailed?.Invoke(session.SessionId, "연결 실패");
                break;
            case HolePunchingState.Connected:
                if (session.TargetPublicEndPoint != null)
                    ConnectionEstablished?.Invoke(session.SessionId, session.TargetPublicEndPoint);
                break;
        }
    }

    private async Task RequestIntroduction(HolePunchingSession session)
    {
        var message = new
        {
            Type = "IntroductionRequest",
            SessionId = session.SessionId,
            PeerId = session.PeerId,
            PublicEndPoint = session.PublicEndPoint?.ToString(),
            LocalEndPoint = session.LocalEndPoint?.ToString()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var data = System.Text.Encoding.UTF8.GetBytes(json);
        
        _udpClient.SendAsync(_coordinationServer, data);
        await TransitionToState(session, HolePunchingState.WaitingForIntroduction);
    }

    private async Task StartPingProcess(HolePunchingSession session)
    {
        if (session.TargetPublicEndPoint == null) return;

        var pingMessage = new
        {
            Type = "Ping",
            SessionId = session.SessionId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(pingMessage);
        var data = System.Text.Encoding.UTF8.GetBytes(json);

        // 여러 번 핑 전송 (포트 예측을 위해)
        for (int i = 0; i < 5; i++)
        {
            _udpClient.SendAsync(session.TargetPublicEndPoint, data);
            
            // 로컬 엔드포인트로도 시도 (같은 NAT 내부일 가능성)
            if (session.TargetLocalEndPoint != null)
            {
                _udpClient.SendAsync(session.TargetLocalEndPoint, data);
            }
            
            await Task.Delay(PING_INTERVAL_MS);
        }

        await TransitionToState(session, HolePunchingState.WaitingForPong);
    }

    private async void OnDataReceived(IPEndPoint sender, byte[] data)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            if (!message.TryGetValue("Type", out var typeObj)) return;
            var messageType = typeObj.ToString();

            switch (messageType)
            {
                case "IntroductionResponse":
                    await HandleIntroductionResponse(sender, message);
                    break;
                case "Ping":
                    await HandlePing(sender, message);
                    break;
                case "Pong":
                    await HandlePong(sender, message);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"메시지 처리 오류: {ex.Message}");
        }
    }

    private async Task HandleIntroductionResponse(IPEndPoint sender, Dictionary<string, object> message)
    {
        if (!message.TryGetValue("SessionId", out var sessionIdObj)) return;
        var sessionId = sessionIdObj.ToString();
        
        if (!_sessions.TryGetValue(sessionId!, out var session)) return;
        
        if (message.TryGetValue("TargetPublicEndPoint", out var targetPublicObj))
        {
            session.TargetPublicEndPoint = IPEndPoint.Parse(targetPublicObj.ToString()!);
        }
        
        if (message.TryGetValue("TargetLocalEndPoint", out var targetLocalObj))
        {
            session.TargetLocalEndPoint = IPEndPoint.Parse(targetLocalObj.ToString()!);
        }

        await TransitionToState(session, HolePunchingState.SendingPing);
    }

    private async Task HandlePing(IPEndPoint sender, Dictionary<string, object> message)
    {
        var pongMessage = new
        {
            Type = "Pong",
            SessionId = message.GetValueOrDefault("SessionId")?.ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(pongMessage);
        var data = System.Text.Encoding.UTF8.GetBytes(json);
        
        _udpClient.SendAsync(sender, data);
        
        // 새로운 연결로 간주
        var sessionId = Guid.NewGuid().ToString();
        ConnectionEstablished?.Invoke(sessionId, sender);
    }

    private async Task HandlePong(IPEndPoint sender, Dictionary<string, object> message)
    {
        if (!message.TryGetValue("SessionId", out var sessionIdObj)) return;
        var sessionId = sessionIdObj.ToString();
        
        if (!_sessions.TryGetValue(sessionId!, out var session)) return;
        
        session.TargetPublicEndPoint = sender;
        await TransitionToState(session, HolePunchingState.Connected);
    }

    private void CheckTimeouts(object? state)
    {
        var now = DateTime.UtcNow;
        var timeoutSessions = new List<HolePunchingSession>();

        foreach (var session in _sessions.Values)
        {
            var elapsed = now - session.LastStateChange;
            
            if (elapsed.TotalMilliseconds > SESSION_TIMEOUT_MS)
            {
                timeoutSessions.Add(session);
            }
        }

        foreach (var session in timeoutSessions)
        {
            _ = TransitionToState(session, HolePunchingState.Timeout);
            _sessions.TryRemove(session.SessionId, out _);
        }
    }

    public void Dispose()
    {
        _timeoutTimer?.Dispose();
        _udpClient.DataReceived -= OnDataReceived;
    }
}
```
  

## 6.4 멀티쓰레드 환경에서의 홀펀칭 처리
P2P 게임에서는 여러 피어와 동시에 홀펀칭을 수행해야 할 수 있습니다. 이를 위한 멀티쓰레드 안전한 구현을 살펴보겠습니다.

### 쓰레드 안전한 P2P 연결 매니저

```csharp
public class P2PConnectionManager : IDisposable
{
    private readonly EnhancedUdpClient _udpClient;
    private readonly HolePunchingManager _holePunchingManager;
    private readonly ConcurrentDictionary<string, P2PConnection> _connections;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public event Action<string, P2PConnection>? PeerConnected;
    public event Action<string>? PeerDisconnected;
    public event Action<string, byte[]>? DataReceived;

    public P2PConnectionManager(int localPort, IPEndPoint coordinationServer)
    {
        _udpClient = new EnhancedUdpClient(localPort);
        _holePunchingManager = new HolePunchingManager(_udpClient, coordinationServer);
        _connections = new ConcurrentDictionary<string, P2PConnection>();
        _connectionSemaphore = new SemaphoreSlim(10, 10); // 최대 10개 동시 연결
        _cancellationTokenSource = new CancellationTokenSource();
        
        _holePunchingManager.ConnectionEstablished += OnConnectionEstablished;
        _holePunchingManager.ConnectionFailed += OnConnectionFailed;
        _udpClient.DataReceived += OnUdpDataReceived;
        
        StartHeartbeatTask();
    }

    public async Task<bool> ConnectToPeerAsync(string peerId)
    {
        await _connectionSemaphore.WaitAsync();
        
        try
        {
            if (_connections.ContainsKey(peerId))
            {
                Console.WriteLine($"피어 {peerId}는 이미 연결되어 있습니다.");
                return true;
            }

            Console.WriteLine($"피어 {peerId}에 연결 시도 중...");
            return await _holePunchingManager.ConnectToPeerAsync(peerId);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    public async Task<bool> SendDataAsync(string peerId, byte[] data)
    {
        if (_connections.TryGetValue(peerId, out var connection))
        {
            return await connection.SendDataAsync(data);
        }
        
        Console.WriteLine($"피어 {peerId}에 연결되어 있지 않습니다.");
        return false;
    }

    public void DisconnectPeer(string peerId)
    {
        if (_connections.TryRemove(peerId, out var connection))
        {
            connection.Dispose();
            PeerDisconnected?.Invoke(peerId);
            Console.WriteLine($"피어 {peerId} 연결 해제됨");
        }
    }

    private async void OnConnectionEstablished(string sessionId, IPEndPoint remoteEndPoint)
    {
        var peerId = sessionId; // 실제로는 세션에서 피어ID 추출
        var connection = new P2PConnection(peerId, remoteEndPoint, _udpClient);
        
        connection.DataReceived += (data) => DataReceived?.Invoke(peerId, data);
        connection.Disconnected += () => DisconnectPeer(peerId);
        
        _connections[peerId] = connection;
        PeerConnected?.Invoke(peerId, connection);
        
        Console.WriteLine($"피어 {peerId} 연결 성공: {remoteEndPoint}");
    }

    private void OnConnectionFailed(string sessionId, string reason)
    {
        Console.WriteLine($"연결 실패 (세션 {sessionId}): {reason}");
    }

    private void OnUdpDataReceived(IPEndPoint sender, byte[] data)
    {
        // 기존 연결에서 온 데이터인지 확인
        var connection = _connections.Values.FirstOrDefault(c => c.RemoteEndPoint.Equals(sender));
        connection?.HandleReceivedData(data);
    }

    private async void StartHeartbeatTask()
    {
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(5000, _cancellationTokenSource.Token);
                
                var tasks = _connections.Values.Select(conn => conn.SendHeartbeatAsync()).ToArray();
                await Task.WhenAll(tasks);
                
                // 타임아웃된 연결 정리
                var timedOutConnections = _connections.Values
                    .Where(conn => conn.IsTimedOut)
                    .ToList();
                
                foreach (var conn in timedOutConnections)
                {
                    DisconnectPeer(conn.PeerId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 종료
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        
        foreach (var connection in _connections.Values)
        {
            connection.Dispose();
        }
        _connections.Clear();
        
        _holePunchingManager?.Dispose();
        _udpClient?.Dispose();
        _connectionSemaphore?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

public class P2PConnection : IDisposable
{
    public string PeerId { get; }
    public IPEndPoint RemoteEndPoint { get; }
    public bool IsConnected { get; private set; }
    public DateTime LastActivity { get; private set; }
    public bool IsTimedOut => DateTime.UtcNow - LastActivity > TimeSpan.FromSeconds(30);

    private readonly EnhancedUdpClient _udpClient;
    private readonly object _lockObject = new object();

    public event Action<byte[]>? DataReceived;
    public event Action? Disconnected;

    public P2PConnection(string peerId, IPEndPoint remoteEndPoint, EnhancedUdpClient udpClient)
    {
        PeerId = peerId;
        RemoteEndPoint = remoteEndPoint;
        _udpClient = udpClient;
        IsConnected = true;
        LastActivity = DateTime.UtcNow;
    }

    public async Task<bool> SendDataAsync(byte[] data)
    {
        lock (_lockObject)
        {
            if (!IsConnected) return false;
        }

        try
        {
            var packet = CreateDataPacket(data);
            _udpClient.SendAsync(RemoteEndPoint, packet);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"데이터 전송 실패: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendHeartbeatAsync()
    {
        var heartbeat = CreateHeartbeatPacket();
        return await SendDataAsync(heartbeat);
    }

    public void HandleReceivedData(byte[] data)
    {
        lock (_lockObject)
        {
            LastActivity = DateTime.UtcNow;
        }

        var packetType = GetPacketType(data);
        
        switch (packetType)
        {
            case PacketType.Data:
                var userData = ExtractUserData(data);
                DataReceived?.Invoke(userData);
                break;
            case PacketType.Heartbeat:
                // 하트비트 응답 (필요시)
                break;
            case PacketType.Disconnect:
                HandleDisconnection();
                break;
        }
    }

    private void HandleDisconnection()
    {
        lock (_lockObject)
        {
            IsConnected = false;
        }
        
        Disconnected?.Invoke();
    }

    private byte[] CreateDataPacket(byte[] data)
    {
        var packet = new List<byte>();
        packet.Add((byte)PacketType.Data);
        packet.AddRange(BitConverter.GetBytes(data.Length));
        packet.AddRange(data);
        return packet.ToArray();
    }

    private byte[] CreateHeartbeatPacket()
    {
        return new byte[] { (byte)PacketType.Heartbeat };
    }

    private PacketType GetPacketType(byte[] data)
    {
        return data.Length > 0 ? (PacketType)data[0] : PacketType.Unknown;
    }

    private byte[] ExtractUserData(byte[] packet)
    {
        if (packet.Length < 5) return Array.Empty<byte>();
        
        var length = BitConverter.ToInt32(packet, 1);
        if (packet.Length < 5 + length) return Array.Empty<byte>();
        
        return packet.Skip(5).Take(length).ToArray();
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            IsConnected = false;
        }
    }
}

public enum PacketType : byte
{
    Unknown = 0,
    Data = 1,
    Heartbeat = 2,
    Disconnect = 3
}
```

### 사용 예제

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("P2P 홀펀칭 테스트 시작");
        
        var coordinationServer = new IPEndPoint(IPAddress.Parse("your-server-ip"), 8080);
        var connectionManager = new P2PConnectionManager(0, coordinationServer);
        
        connectionManager.PeerConnected += (peerId, connection) =>
        {
            Console.WriteLine($"피어 연결됨: {peerId}");
        };
        
        connectionManager.DataReceived += (peerId, data) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine($"{peerId}로부터 메시지: {message}");
        };
        
        Console.WriteLine("연결할 피어 ID를 입력하세요:");
        var targetPeerId = Console.ReadLine();
        
        if (!string.IsNullOrEmpty(targetPeerId))
        {
            var success = await connectionManager.ConnectToPeerAsync(targetPeerId);
            if (success)
            {
                Console.WriteLine("연결 성공! 메시지를 입력하세요 (quit으로 종료):");
                
                string? input;
                while ((input = Console.ReadLine()) != "quit")
                {
                    if (!string.IsNullOrEmpty(input))
                    {
                        var data = System.Text.Encoding.UTF8.GetBytes(input);
                        await connectionManager.SendDataAsync(targetPeerId, data);
                    }
                }
            }
        }
        
        connectionManager.Dispose();
    }
}
```

이 장에서 구현한 홀펀칭 라이브러리는 .NET 9의 성능 개선사항을 활용하여 효율적인 P2P 연결을 제공합니다. 상태 머신을 통한 체계적인 연결 관리와 멀티쓰레드 환경에서의 안전한 동작을 보장하며, 실제 게임 개발에 바로 적용할 수 있는 수준의 완성도를 가지고 있습니다.

  

# 7장: 게임 특화 P2P 연결 관리
  
## 7.1 게임 세션과 P2P 연결 라이프사이클
게임에서 P2P 연결은 단순한 일대일 통신이 아닌 복잡한 멀티플레이어 세션의 일부입니다. 게임 세션의 전체 라이프사이클을 관리하는 시스템을 구현해보겠습니다.

### 게임 세션 모델 정의

```csharp
public class GameSession
{
    public string SessionId { get; }
    public string SessionName { get; set; }
    public GameSessionState State { get; set; }
    public string HostPlayerId { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; }
    public DateTime LastActivity { get; set; }
    
    private readonly ConcurrentDictionary<string, GamePlayer> _players;
    private readonly object _stateLock = new object();

    public IReadOnlyCollection<GamePlayer> Players => _players.Values;
    public int PlayerCount => _players.Count;
    public bool IsFull => PlayerCount >= MaxPlayers;

    public event Action<GamePlayer>? PlayerJoined;
    public event Action<GamePlayer>? PlayerLeft;
    public event Action<GameSessionState>? StateChanged;

    public GameSession(string sessionId, string sessionName, string hostPlayerId, int maxPlayers = 8)
    {
        SessionId = sessionId;
        SessionName = sessionName;
        HostPlayerId = hostPlayerId;
        MaxPlayers = maxPlayers;
        State = GameSessionState.Waiting;
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
        _players = new ConcurrentDictionary<string, GamePlayer>();
    }

    public bool TryAddPlayer(GamePlayer player)
    {
        if (IsFull || State != GameSessionState.Waiting)
            return false;

        if (_players.TryAdd(player.PlayerId, player))
        {
            LastActivity = DateTime.UtcNow;
            PlayerJoined?.Invoke(player);
            return true;
        }
        return false;
    }

    public bool TryRemovePlayer(string playerId)
    {
        if (_players.TryRemove(playerId, out var player))
        {
            LastActivity = DateTime.UtcNow;
            PlayerLeft?.Invoke(player);
            
            // 호스트가 떠난 경우 새 호스트 선택
            if (playerId == HostPlayerId && PlayerCount > 0)
            {
                HostPlayerId = _players.Keys.First();
                Console.WriteLine($"새 호스트: {HostPlayerId}");
            }
            
            return true;
        }
        return false;
    }

    public void ChangeState(GameSessionState newState)
    {
        lock (_stateLock)
        {
            if (State != newState)
            {
                var oldState = State;
                State = newState;
                LastActivity = DateTime.UtcNow;
                Console.WriteLine($"세션 {SessionId} 상태 변경: {oldState} -> {newState}");
                StateChanged?.Invoke(newState);
            }
        }
    }

    public GamePlayer? GetPlayer(string playerId)
    {
        _players.TryGetValue(playerId, out var player);
        return player;
    }
}

public enum GameSessionState
{
    Waiting,    // 플레이어 대기 중
    Starting,   // 게임 시작 중
    InProgress, // 게임 진행 중
    Paused,     // 일시 정지
    Ending,     // 게임 종료 중
    Ended       // 게임 종료됨
}

public class GamePlayer
{
    public string PlayerId { get; }
    public string PlayerName { get; set; }
    public IPEndPoint? PublicEndPoint { get; set; }
    public IPEndPoint? LocalEndPoint { get; set; }
    public PlayerState State { get; set; }
    public DateTime LastSeen { get; set; }
    public TimeSpan Ping { get; set; }
    public float PacketLoss { get; set; }

    public GamePlayer(string playerId, string playerName)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        State = PlayerState.Connecting;
        LastSeen = DateTime.UtcNow;
        Ping = TimeSpan.Zero;
        PacketLoss = 0f;
    }

    public bool IsConnected => State == PlayerState.Connected;
    public bool IsTimedOut => DateTime.UtcNow - LastSeen > TimeSpan.FromSeconds(30);
}

public enum PlayerState
{
    Connecting,
    Connected,
    Disconnecting,
    Disconnected,
    TimedOut
}
```

### 게임 세션 매니저

```csharp
public class GameSessionManager : IDisposable
{
    private readonly P2PConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, GameSession> _sessions;
    private readonly Timer _sessionMaintenanceTimer;
    private readonly string _localPlayerId;
    
    public event Action<GameSession>? SessionCreated;
    public event Action<string>? SessionEnded;
    public event Action<string, GamePlayer>? PlayerJoinedSession;
    public event Action<string, GamePlayer>? PlayerLeftSession;

    public IReadOnlyCollection<GameSession> Sessions => _sessions.Values;

    public GameSessionManager(P2PConnectionManager connectionManager, string localPlayerId)
    {
        _connectionManager = connectionManager;
        _localPlayerId = localPlayerId;
        _sessions = new ConcurrentDictionary<string, GameSession>();
        
        _connectionManager.PeerConnected += OnPeerConnected;
        _connectionManager.PeerDisconnected += OnPeerDisconnected;
        _connectionManager.DataReceived += OnDataReceived;
        
        _sessionMaintenanceTimer = new Timer(MaintenanceSessions, null, 5000, 5000);
    }

    public async Task<GameSession?> CreateSessionAsync(string sessionName, int maxPlayers = 8)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new GameSession(sessionId, sessionName, _localPlayerId, maxPlayers);
        
        // 호스트 플레이어 추가
        var hostPlayer = new GamePlayer(_localPlayerId, "Host")
        {
            State = PlayerState.Connected,
            PublicEndPoint = _connectionManager.LocalEndPoint
        };
        
        if (!session.TryAddPlayer(hostPlayer))
        {
            return null;
        }

        if (_sessions.TryAdd(sessionId, session))
        {
            session.PlayerJoined += (player) => PlayerJoinedSession?.Invoke(sessionId, player);
            session.PlayerLeft += (player) => PlayerLeftSession?.Invoke(sessionId, player);
            
            SessionCreated?.Invoke(session);
            Console.WriteLine($"게임 세션 생성됨: {sessionName} ({sessionId})");
            return session;
        }

        return null;
    }

    public async Task<bool> JoinSessionAsync(string sessionId, string playerName)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            Console.WriteLine($"세션을 찾을 수 없음: {sessionId}");
            return false;
        }

        if (session.IsFull)
        {
            Console.WriteLine("세션이 가득 참");
            return false;
        }

        // 호스트에게 연결
        var hostPlayer = session.GetPlayer(session.HostPlayerId);
        if (hostPlayer?.PublicEndPoint == null)
        {
            Console.WriteLine("호스트 정보를 찾을 수 없음");
            return false;
        }

        var connected = await _connectionManager.ConnectToPeerAsync(session.HostPlayerId);
        if (!connected)
        {
            Console.WriteLine("호스트에 연결 실패");
            return false;
        }

        // 참가 요청 전송
        var joinRequest = new GameMessage
        {
            Type = GameMessageType.JoinRequest,
            SenderId = _localPlayerId,
            Data = new JoinRequestData
            {
                SessionId = sessionId,
                PlayerName = playerName
            }
        };

        return await SendGameMessage(session.HostPlayerId, joinRequest);
    }

    public async Task<bool> LeaveSessionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        var player = session.GetPlayer(_localPlayerId);
        if (player == null)
            return false;

        // 다른 플레이어들에게 떠나는 것을 알림
        var leaveMessage = new GameMessage
        {
            Type = GameMessageType.PlayerLeave,
            SenderId = _localPlayerId,
            Data = new PlayerLeaveData
            {
                SessionId = sessionId,
                PlayerId = _localPlayerId
            }
        };

        await BroadcastToSession(sessionId, leaveMessage, _localPlayerId);
        
        // 세션에서 제거
        session.TryRemovePlayer(_localPlayerId);
        
        // 호스트인 경우 세션 종료
        if (session.HostPlayerId == _localPlayerId)
        {
            await EndSessionAsync(sessionId);
        }

        return true;
    }

    private async void OnPeerConnected(string peerId, P2PConnection connection)
    {
        Console.WriteLine($"피어 연결됨: {peerId}");
        
        // 연결된 피어에게 현재 세션 정보 전송 (필요시)
        foreach (var session in _sessions.Values)
        {
            var player = session.GetPlayer(peerId);
            if (player != null)
            {
                player.State = PlayerState.Connected;
                player.LastSeen = DateTime.UtcNow;
            }
        }
    }

    private void OnPeerDisconnected(string peerId)
    {
        Console.WriteLine($"피어 연결 해제됨: {peerId}");
        
        // 모든 세션에서 플레이어 제거
        foreach (var session in _sessions.Values)
        {
            var player = session.GetPlayer(peerId);
            if (player != null)
            {
                player.State = PlayerState.Disconnected;
                session.TryRemovePlayer(peerId);
            }
        }
    }

    private async void OnDataReceived(string senderId, byte[] data)
    {
        try
        {
            var message = GameMessage.Deserialize(data);
            await HandleGameMessage(senderId, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"게임 메시지 처리 오류: {ex.Message}");
        }
    }

    private async Task HandleGameMessage(string senderId, GameMessage message)
    {
        switch (message.Type)
        {
            case GameMessageType.JoinRequest:
                await HandleJoinRequest(senderId, message);
                break;
            case GameMessageType.JoinResponse:
                await HandleJoinResponse(senderId, message);
                break;
            case GameMessageType.PlayerJoin:
                await HandlePlayerJoin(senderId, message);
                break;
            case GameMessageType.PlayerLeave:
                await HandlePlayerLeave(senderId, message);
                break;
            case GameMessageType.SessionUpdate:
                await HandleSessionUpdate(senderId, message);
                break;
        }
    }

    private async Task HandleJoinRequest(string senderId, GameMessage message)
    {
        var joinData = message.GetData<JoinRequestData>();
        if (joinData == null) return;

        var session = _sessions.GetValueOrDefault(joinData.SessionId);
        if (session == null || session.HostPlayerId != _localPlayerId)
        {
            // 거부 응답
            var rejectResponse = new GameMessage
            {
                Type = GameMessageType.JoinResponse,
                SenderId = _localPlayerId,
                Data = new JoinResponseData
                {
                    Accepted = false,
                    Reason = "세션을 찾을 수 없거나 권한이 없음"
                }
            };
            await SendGameMessage(senderId, rejectResponse);
            return;
        }

        // 새 플레이어 추가
        var newPlayer = new GamePlayer(senderId, joinData.PlayerName)
        {
            State = PlayerState.Connected,
            PublicEndPoint = _connectionManager.GetPeerEndPoint(senderId)
        };

        if (session.TryAddPlayer(newPlayer))
        {
            // 승인 응답
            var acceptResponse = new GameMessage
            {
                Type = GameMessageType.JoinResponse,
                SenderId = _localPlayerId,
                Data = new JoinResponseData
                {
                    Accepted = true,
                    SessionInfo = new SessionInfo
                    {
                        SessionId = session.SessionId,
                        SessionName = session.SessionName,
                        HostPlayerId = session.HostPlayerId,
                        Players = session.Players.Select(p => new PlayerInfo
                        {
                            PlayerId = p.PlayerId,
                            PlayerName = p.PlayerName,
                            PublicEndPoint = p.PublicEndPoint?.ToString()
                        }).ToList()
                    }
                }
            };
            await SendGameMessage(senderId, acceptResponse);

            // 다른 플레이어들에게 새 플레이어 알림
            var playerJoinMessage = new GameMessage
            {
                Type = GameMessageType.PlayerJoin,
                SenderId = _localPlayerId,
                Data = new PlayerJoinData
                {
                    SessionId = session.SessionId,
                    Player = new PlayerInfo
                    {
                        PlayerId = newPlayer.PlayerId,
                        PlayerName = newPlayer.PlayerName,
                        PublicEndPoint = newPlayer.PublicEndPoint?.ToString()
                    }
                }
            };
            await BroadcastToSession(session.SessionId, playerJoinMessage, senderId);
        }
        else
        {
            // 거부 응답
            var rejectResponse = new GameMessage
            {
                Type = GameMessageType.JoinResponse,
                SenderId = _localPlayerId,
                Data = new JoinResponseData
                {
                    Accepted = false,
                    Reason = "세션이 가득 참"
                }
            };
            await SendGameMessage(senderId, rejectResponse);
        }
    }

    private async Task HandleJoinResponse(string senderId, GameMessage message)
    {
        var responseData = message.GetData<JoinResponseData>();
        if (responseData == null) return;

        if (responseData.Accepted && responseData.SessionInfo != null)
        {
            Console.WriteLine($"세션 참가 승인됨: {responseData.SessionInfo.SessionName}");
            
            // 다른 플레이어들과 연결
            foreach (var playerInfo in responseData.SessionInfo.Players)
            {
                if (playerInfo.PlayerId != _localPlayerId && 
                    !string.IsNullOrEmpty(playerInfo.PublicEndPoint))
                {
                    _ = Task.Run(async () => 
                    {
                        await _connectionManager.ConnectToPeerAsync(playerInfo.PlayerId);
                    });
                }
            }
        }
        else
        {
            Console.WriteLine($"세션 참가 거부됨: {responseData.Reason}");
        }
    }

    private async Task HandlePlayerJoin(string senderId, GameMessage message)
    {
        var joinData = message.GetData<PlayerJoinData>();
        if (joinData == null) return;

        var session = _sessions.GetValueOrDefault(joinData.SessionId);
        if (session == null) return;

        // 새 플레이어와 연결 시도
        if (joinData.Player.PlayerId != _localPlayerId)
        {
            _ = Task.Run(async () => 
            {
                await _connectionManager.ConnectToPeerAsync(joinData.Player.PlayerId);
            });
        }
    }

    private async Task HandlePlayerLeave(string senderId, GameMessage message)
    {
        var leaveData = message.GetData<PlayerLeaveData>();
        if (leaveData == null) return;

        var session = _sessions.GetValueOrDefault(leaveData.SessionId);
        if (session == null) return;

        session.TryRemovePlayer(leaveData.PlayerId);
        _connectionManager.DisconnectPeer(leaveData.PlayerId);
    }

    private async Task HandleSessionUpdate(string senderId, GameMessage message)
    {
        // 세션 상태 업데이트 처리
    }

    private async Task<bool> SendGameMessage(string peerId, GameMessage message)
    {
        var data = message.Serialize();
        return await _connectionManager.SendDataAsync(peerId, data);
    }

    private async Task BroadcastToSession(string sessionId, GameMessage message, string excludePlayerId = "")
    {
        var session = _sessions.GetValueOrDefault(sessionId);
        if (session == null) return;

        var tasks = new List<Task>();
        foreach (var player in session.Players)
        {
            if (player.PlayerId != excludePlayerId && player.PlayerId != _localPlayerId)
            {
                tasks.Add(SendGameMessage(player.PlayerId, message));
            }
        }

        await Task.WhenAll(tasks);
    }

    private async Task EndSessionAsync(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.ChangeState(GameSessionState.Ended);
            
            // 모든 플레이어 연결 해제
            foreach (var player in session.Players)
            {
                if (player.PlayerId != _localPlayerId)
                {
                    _connectionManager.DisconnectPeer(player.PlayerId);
                }
            }
            
            SessionEnded?.Invoke(sessionId);
            Console.WriteLine($"세션 종료됨: {sessionId}");
        }
    }

    private void MaintenanceSessions(object? state)
    {
        var expiredSessions = new List<string>();
        
        foreach (var session in _sessions.Values)
        {
            // 타임아웃된 플레이어 제거
            var timedOutPlayers = session.Players
                .Where(p => p.IsTimedOut)
                .ToList();
            
            foreach (var player in timedOutPlayers)
            {
                session.TryRemovePlayer(player.PlayerId);
                _connectionManager.DisconnectPeer(player.PlayerId);
            }
            
            // 빈 세션이나 오래된 세션 정리
            if (session.PlayerCount == 0 || 
                DateTime.UtcNow - session.LastActivity > TimeSpan.FromMinutes(30))
            {
                expiredSessions.Add(session.SessionId);
            }
        }
        
        foreach (var sessionId in expiredSessions)
        {
            _ = Task.Run(() => EndSessionAsync(sessionId));
        }
    }

    public void Dispose()
    {
        _sessionMaintenanceTimer?.Dispose();
        
        // 모든 세션 종료
        var sessionIds = _sessions.Keys.ToList();
        foreach (var sessionId in sessionIds)
        {
            _ = EndSessionAsync(sessionId);
        }
    }
}
```
 

## 7.2 동적 P2P 연결 관리 (플레이어 Join/Leave)
게임 중에 플레이어가 동적으로 참가하거나 떠날 때의 P2P 연결을 효율적으로 관리하는 시스템을 구현합니다.

### 동적 연결 관리자

```csharp
public class DynamicConnectionManager
{
    private readonly GameSessionManager _sessionManager;
    private readonly P2PConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, PlayerConnectionState> _connectionStates;
    private readonly SemaphoreSlim _connectionSemaphore;

    public event Action<string, string>? PlayerConnectionEstablished;
    public event Action<string, string>? PlayerConnectionLost;

    public DynamicConnectionManager(GameSessionManager sessionManager, P2PConnectionManager connectionManager)
    {
        _sessionManager = sessionManager;
        _connectionManager = connectionManager;
        _connectionStates = new ConcurrentDictionary<string, PlayerConnectionState>();
        _connectionSemaphore = new SemaphoreSlim(5, 5); // 최대 5개 동시 연결 시도

        _sessionManager.PlayerJoinedSession += OnPlayerJoinedSession;
        _sessionManager.PlayerLeftSession += OnPlayerLeftSession;
        _connectionManager.PeerConnected += OnPeerConnected;
        _connectionManager.PeerDisconnected += OnPeerDisconnected;
    }

    private async void OnPlayerJoinedSession(string sessionId, GamePlayer player)
    {
        if (player.PlayerId == _connectionManager.LocalPlayerId)
            return;

        await EstablishConnectionToPlayer(sessionId, player);
    }

    private async void OnPlayerLeftSession(string sessionId, GamePlayer player)
    {
        await CleanupPlayerConnection(sessionId, player.PlayerId);
    }

    private async Task EstablishConnectionToPlayer(string sessionId, GamePlayer player)
    {
        var connectionKey = $"{sessionId}:{player.PlayerId}";
        
        // 중복 연결 시도 방지
        if (_connectionStates.ContainsKey(connectionKey))
            return;

        var connectionState = new PlayerConnectionState
        {
            SessionId = sessionId,
            PlayerId = player.PlayerId,
            State = ConnectionAttemptState.Attempting,
            AttemptCount = 0,
            LastAttempt = DateTime.UtcNow
        };

        _connectionStates[connectionKey] = connectionState;

        await _connectionSemaphore.WaitAsync();
        try
        {
            var success = await AttemptConnectionWithRetry(player.PlayerId, 3);
            
            connectionState.State = success ? 
                ConnectionAttemptState.Established : 
                ConnectionAttemptState.Failed;
            
            if (success)
            {
                PlayerConnectionEstablished?.Invoke(sessionId, player.PlayerId);
                Console.WriteLine($"플레이어 {player.PlayerName}와 연결 성공");
            }
            else
            {
                Console.WriteLine($"플레이어 {player.PlayerName}와 연결 실패");
                _connectionStates.TryRemove(connectionKey, out _);
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task<bool> AttemptConnectionWithRetry(string playerId, int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Console.WriteLine($"플레이어 {playerId} 연결 시도 {attempt}/{maxRetries}");
            
            var success = await _connectionManager.ConnectToPeerAsync(playerId);
            if (success)
                return true;

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 지수 백오프
                await Task.Delay(delay);
            }
        }

        return false;
    }

    private async Task CleanupPlayerConnection(string sessionId, string playerId)
    {
        var connectionKey = $"{sessionId}:{playerId}";
        
        if (_connectionStates.TryRemove(connectionKey, out var connectionState))
        {
            _connectionManager.DisconnectPeer(playerId);
            PlayerConnectionLost?.Invoke(sessionId, playerId);
            Console.WriteLine($"플레이어 {playerId} 연결 정리됨");
        }
    }

    private void OnPeerConnected(string peerId, P2PConnection connection)
    {
        // 연결 상태 업데이트
        var connectionKey = _connectionStates.Keys
            .FirstOrDefault(key => key.EndsWith($":{peerId}"));
        
        if (connectionKey != null && _connectionStates.TryGetValue(connectionKey, out var state))
        {
            state.State = ConnectionAttemptState.Established;
            state.Connection = connection;
        }
    }

    private void OnPeerDisconnected(string peerId)
    {
        // 연결 손실 처리
        var lostConnections = _connectionStates
            .Where(kvp => kvp.Key.EndsWith($":{peerId}"))
            .ToList();

        foreach (var kvp in lostConnections)
        {
            var sessionId = kvp.Value.SessionId;
            _connectionStates.TryRemove(kvp.Key, out _);
            PlayerConnectionLost?.Invoke(sessionId, peerId);
        }
    }

    public async Task<bool> ReconnectToPlayer(string sessionId, string playerId)
    {
        var connectionKey = $"{sessionId}:{playerId}";
        
        if (_connectionStates.TryGetValue(connectionKey, out var state))
        {
            state.State = ConnectionAttemptState.Attempting;
            state.AttemptCount++;
            state.LastAttempt = DateTime.UtcNow;
            
            return await AttemptConnectionWithRetry(playerId, 2);
        }

        return false;
    }

    public Dictionary<string, ConnectionInfo> GetConnectionInfo(string sessionId)
    {
        var result = new Dictionary<string, ConnectionInfo>();
        
        foreach (var kvp in _connectionStates)
        {
            if (kvp.Value.SessionId == sessionId)
            {
                result[kvp.Value.PlayerId] = new ConnectionInfo
                {
                    State = kvp.Value.State,
                    AttemptCount = kvp.Value.AttemptCount,
                    LastAttempt = kvp.Value.LastAttempt,
                    Ping = kvp.Value.Connection?.Ping ?? TimeSpan.Zero,
                    PacketLoss = kvp.Value.Connection?.PacketLoss ?? 0f
                };
            }
        }
        
        return result;
    }
}

public class PlayerConnectionState
{
    public string SessionId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public ConnectionAttemptState State { get; set; }
    public int AttemptCount { get; set; }
    public DateTime LastAttempt { get; set; }
    public P2PConnection? Connection { get; set; }
}

public enum ConnectionAttemptState
{
    Attempting,
    Established,
    Failed,
    Reconnecting
}

public class ConnectionInfo
{
    public ConnectionAttemptState State { get; set; }
    public int AttemptCount { get; set; }
    public DateTime LastAttempt { get; set; }
    public TimeSpan Ping { get; set; }
    public float PacketLoss { get; set; }
}
```
  

## 7.3 연결 품질 모니터링과 자동 복구
네트워크 연결의 품질을 실시간으로 모니터링하고 문제를 감지하여 자동으로 복구하는 시스템을 구현합니다.

### 연결 품질 모니터

```csharp
public class ConnectionQualityMonitor : IDisposable
{
    private readonly P2PConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, ConnectionMetrics> _metrics;
    private readonly Timer _monitoringTimer;
    private readonly Timer _recoveryTimer;
    
    // 품질 임계값
    private const int MAX_PING_MS = 500;
    private const float MAX_PACKET_LOSS = 0.1f; // 10%
    private const int CONSECUTIVE_FAILURES_THRESHOLD = 3;

    public event Action<string, ConnectionQuality>? QualityChanged;
    public event Action<string, string>? ConnectionIssueDetected;
    public event Action<string>? RecoveryAttempted;

    public ConnectionQualityMonitor(P2PConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _metrics = new ConcurrentDictionary<string, ConnectionMetrics>();
        
        _connectionManager.PeerConnected += OnPeerConnected;
        _connectionManager.PeerDisconnected += OnPeerDisconnected;
        _connectionManager.DataReceived += OnDataReceived;
        
        _monitoringTimer = new Timer(MonitorConnections, null, 1000, 1000);
        _recoveryTimer = new Timer(AttemptRecovery, null, 5000, 5000);
    }

    private void OnPeerConnected(string peerId, P2PConnection connection)
    {
        var metrics = new ConnectionMetrics(peerId);
        _metrics[peerId] = metrics;
        
        // 첫 핑 테스트 예약
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            await SendPingTest(peerId);
        });
    }

    private void OnPeerDisconnected(string peerId)
    {
        if (_metrics.TryRemove(peerId, out var metrics))
        {
            metrics.Dispose();
        }
    }

    private async void OnDataReceived(string senderId, byte[] data)
    {
        if (_metrics.TryGetValue(senderId, out var metrics))
        {
            metrics.OnDataReceived(data.Length);
            
            // 핑 응답 처리
            if (IsPingResponse(data))
            {
                HandlePingResponse(senderId, data);
            }
        }
    }

    private async Task SendPingTest(string peerId)
    {
        if (!_metrics.TryGetValue(peerId, out var metrics))
            return;

        var pingData = CreatePingData();
        metrics.OnPingSent(pingData.Timestamp);
        
        var success = await _connectionManager.SendDataAsync(peerId, pingData.Data);
        if (!success)
        {
            metrics.OnPingFailed();
        }
    }

    private void HandlePingResponse(string peerId, byte[] data)
    {
        if (!_metrics.TryGetValue(peerId, out var metrics))
            return;

        var timestamp = ExtractPingTimestamp(data);
        if (timestamp > 0)
        {
            var ping = DateTime.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            metrics.OnPingReceived(ping);
        }
    }

    private async void MonitorConnections(object? state)
    {
        var currentTime = DateTime.UtcNow;
        
        foreach (var kvp in _metrics)
        {
            var peerId = kvp.Key;
            var metrics = kvp.Value;
            
            // 주기적으로 핑 테스트 실행
            if (currentTime - metrics.LastPingTime > TimeSpan.FromSeconds(5))
            {
                _ = Task.Run(() => SendPingTest(peerId));
            }
            
            // 품질 분석
            var quality = AnalyzeConnectionQuality(metrics);
            if (quality != metrics.LastReportedQuality)
            {
                metrics.LastReportedQuality = quality;
                QualityChanged?.Invoke(peerId, quality);
                
                if (quality == ConnectionQuality.Poor || quality == ConnectionQuality.Critical)
                {
                    var issue = GetQualityIssueDescription(metrics);
                    ConnectionIssueDetected?.Invoke(peerId, issue);
                }
            }
            
            // 임계값 확인
            CheckThresholds(peerId, metrics);
        }
    }

    private ConnectionQuality AnalyzeConnectionQuality(ConnectionMetrics metrics)
    {
        var avgPing = metrics.AveragePing;
        var packetLoss = metrics.PacketLossRate;
        var consecutiveFailures = metrics.ConsecutiveFailures;
        
        if (consecutiveFailures >= CONSECUTIVE_FAILURES_THRESHOLD || packetLoss > MAX_PACKET_LOSS * 2)
        {
            return ConnectionQuality.Critical;
        }
        
        if (avgPing > TimeSpan.FromMilliseconds(MAX_PING_MS) || packetLoss > MAX_PACKET_LOSS)
        {
            return ConnectionQuality.Poor;
        }
        
        if (avgPing > TimeSpan.FromMilliseconds(MAX_PING_MS * 0.7) || packetLoss > MAX_PACKET_LOSS * 0.5)
        {
            return ConnectionQuality.Fair;
        }
        
        return ConnectionQuality.Good;
    }

    private void CheckThresholds(string peerId, ConnectionMetrics metrics)
    {
        if (metrics.ConsecutiveFailures >= CONSECUTIVE_FAILURES_THRESHOLD)
        {
            metrics.RequiresRecovery = true;
            Console.WriteLine($"피어 {peerId}: 연속 실패 임계값 초과, 복구 필요");
        }
        
        if (metrics.PacketLossRate > MAX_PACKET_LOSS)
        {
            Console.WriteLine($"피어 {peerId}: 패킷 손실률 임계값 초과 ({metrics.PacketLossRate:P1})");
        }
        
        if (metrics.AveragePing > TimeSpan.FromMilliseconds(MAX_PING_MS))
        {
            Console.WriteLine($"피어 {peerId}: 핑 임계값 초과 ({metrics.AveragePing.TotalMilliseconds:F0}ms)");
        }
    }

    private async void AttemptRecovery(object? state)
    {
        var peersNeedingRecovery = _metrics
            .Where(kvp => kvp.Value.RequiresRecovery)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var peerId in peersNeedingRecovery)
        {
            if (_metrics.TryGetValue(peerId, out var metrics))
            {
                await AttemptConnectionRecovery(peerId, metrics);
            }
        }
    }

    private async Task AttemptConnectionRecovery(string peerId, ConnectionMetrics metrics)
    {
        Console.WriteLine($"피어 {peerId} 연결 복구 시도 중...");
        
        // 복구 시도 횟수 제한
        if (metrics.RecoveryAttempts >= 3)
        {
            Console.WriteLine($"피어 {peerId}: 최대 복구 시도 횟수 초과, 연결 포기");
            _connectionManager.DisconnectPeer(peerId);
            return;
        }
        
        metrics.RecoveryAttempts++;
        metrics.RequiresRecovery = false;
        
        // 현재 연결 종료 후 재연결
        _connectionManager.DisconnectPeer(peerId);
        await Task.Delay(2000); // 잠시 대기
        
        var success = await _connectionManager.ConnectToPeerAsync(peerId);
        if (success)
        {
            Console.WriteLine($"피어 {peerId} 연결 복구 성공");
            metrics.ResetFailureCount();
            RecoveryAttempted?.Invoke(peerId);
        }
        else
        {
            Console.WriteLine($"피어 {peerId} 연결 복구 실패");
            metrics.RequiresRecovery = true;
        }
    }

    private string GetQualityIssueDescription(ConnectionMetrics metrics)
    {
        var issues = new List<string>();
        
        if (metrics.AveragePing > TimeSpan.FromMilliseconds(MAX_PING_MS))
        {
            issues.Add($"높은 핑 ({metrics.AveragePing.TotalMilliseconds:F0}ms)");
        }
        
        if (metrics.PacketLossRate > MAX_PACKET_LOSS)
        {
            issues.Add($"패킷 손실 ({metrics.PacketLossRate:P1})");
        }
        
        if (metrics.ConsecutiveFailures > 0)
        {
            issues.Add($"연속 실패 ({metrics.ConsecutiveFailures}회)");
        }
        
        return string.Join(", ", issues);
    }

    // 핑 데이터 생성 및 처리 헬퍼 메서드들
    private (byte[] Data, long Timestamp) CreatePingData()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var data = new List<byte>();
        data.Add(0xFF); // 핑 마커
        data.AddRange(BitConverter.GetBytes(timestamp));
        return (data.ToArray(), timestamp);
    }

    private bool IsPingResponse(byte[] data)
    {
        return data.Length > 0 && data[0] == 0xFF;
    }

    private long ExtractPingTimestamp(byte[] data)
    {
        if (data.Length >= 9)
        {
            return BitConverter.ToInt64(data, 1);
        }
        return 0;
    }

    public Dictionary<string, ConnectionQualityInfo> GetQualityReport()
    {
        var report = new Dictionary<string, ConnectionQualityInfo>();
        
        foreach (var kvp in _metrics)
        {
            var metrics = kvp.Value;
            report[kvp.Key] = new ConnectionQualityInfo
            {
                Quality = metrics.LastReportedQuality,
                AveragePing = metrics.AveragePing,
                PacketLossRate = metrics.PacketLossRate,
                BytesSent = metrics.BytesSent,
                BytesReceived = metrics.BytesReceived,
                ConsecutiveFailures = metrics.ConsecutiveFailures,
                RecoveryAttempts = metrics.RecoveryAttempts
            };
        }
        
        return report;
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        _recoveryTimer?.Dispose();
        
        foreach (var metrics in _metrics.Values)
        {
            metrics.Dispose();
        }
        _metrics.Clear();
    }
}

public class ConnectionMetrics : IDisposable
{
    public string PeerId { get; }
    public DateTime LastPingTime { get; set; }
    public TimeSpan AveragePing { get; private set; }
    public float PacketLossRate { get; private set; }
    public long BytesSent { get; private set; }
    public long BytesReceived { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public int RecoveryAttempts { get; set; }
    public bool RequiresRecovery { get; set; }
    public ConnectionQuality LastReportedQuality { get; set; }

    private readonly Queue<TimeSpan> _pingHistory;
    private readonly Queue<bool> _packetHistory;
    private readonly Dictionary<long, DateTime> _pendingPings;
    private readonly object _lockObject = new object();

    private const int HISTORY_SIZE = 20;

    public ConnectionMetrics(string peerId)
    {
        PeerId = peerId;
        _pingHistory = new Queue<TimeSpan>();
        _packetHistory = new Queue<bool>();
        _pendingPings = new Dictionary<long, DateTime>();
        LastReportedQuality = ConnectionQuality.Unknown;
        LastPingTime = DateTime.UtcNow;
    }

    public void OnPingSent(long timestamp)
    {
        lock (_lockObject)
        {
            _pendingPings[timestamp] = DateTime.UtcNow;
            LastPingTime = DateTime.UtcNow;
        }
    }

    public void OnPingReceived(TimeSpan ping)
    {
        lock (_lockObject)
        {
            _pingHistory.Enqueue(ping);
            _packetHistory.Enqueue(true);
            
            if (_pingHistory.Count > HISTORY_SIZE)
            {
                _pingHistory.Dequeue();
                _packetHistory.Dequeue();
            }
            
            UpdateAveragePing();
            UpdatePacketLossRate();
            ConsecutiveFailures = 0;
        }
    }

    public void OnPingFailed()
    {
        lock (_lockObject)
        {
            _packetHistory.Enqueue(false);
            
            if (_packetHistory.Count > HISTORY_SIZE)
            {
                _packetHistory.Dequeue();
            }
            
            UpdatePacketLossRate();
            ConsecutiveFailures++;
        }
    }

    public void OnDataReceived(int bytes)
    {
        Interlocked.Add(ref BytesReceived, bytes);
    }

    public void OnDataSent(int bytes)
    {
        Interlocked.Add(ref BytesSent, bytes);
    }

    public void ResetFailureCount()
    {
        lock (_lockObject)
        {
            ConsecutiveFailures = 0;
            RecoveryAttempts = 0;
        }
    }

    private void UpdateAveragePing()
    {
        if (_pingHistory.Count > 0)
        {
            var total = _pingHistory.Aggregate(TimeSpan.Zero, (sum, ping) => sum + ping);
            AveragePing = TimeSpan.FromTicks(total.Ticks / _pingHistory.Count);
        }
    }

    private void UpdatePacketLossRate()
    {
        if (_packetHistory.Count > 0)
        {
            var failed = _packetHistory.Count(success => !success);
            PacketLossRate = (float)failed / _packetHistory.Count;
        }
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            _pingHistory.Clear();
            _packetHistory.Clear();
            _pendingPings.Clear();
        }
    }
}

public enum ConnectionQuality
{
    Unknown,
    Critical,
    Poor,
    Fair,
    Good,
    Excellent
}

public class ConnectionQualityInfo
{
    public ConnectionQuality Quality { get; set; }
    public TimeSpan AveragePing { get; set; }
    public float PacketLossRate { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int RecoveryAttempts { get; set; }
}
```
  

## 7.4 NAT 리바인딩과 연결 유지
NAT 장치는 일정 시간 후 포트 매핑을 변경할 수 있습니다. 이를 감지하고 대응하는 시스템을 구현합니다.

### NAT 리바인딩 감지 및 대응

```csharp
public class NATRebindingDetector : IDisposable
{
    private readonly EnhancedUdpClient _udpClient;
    private readonly StunClient _stunClient;
    private readonly Timer _rebindingCheckTimer;
    private readonly ConcurrentDictionary<string, NATBinding> _bindings;
    
    private IPEndPoint? _lastKnownPublicEndPoint;
    private DateTime _lastBindingCheck;

    public event Action<IPEndPoint, IPEndPoint>? RebindingDetected;
    public event Action<string>? BindingExpired;

    public NATRebindingDetector(EnhancedUdpClient udpClient)
    {
        _udpClient = udpClient;
        _stunClient = new StunClient(udpClient);
        _bindings = new ConcurrentDictionary<string, NATBinding>();
        _lastBindingCheck = DateTime.UtcNow;
        
        // 주기적으로 NAT 바인딩 상태 확인 (2분마다)
        _rebindingCheckTimer = new Timer(CheckNATBinding, null, 
            TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
    }

    public async Task<IPEndPoint?> InitializeBinding()
    {
        _lastKnownPublicEndPoint = await _stunClient.GetPublicEndPointAsync();
        _lastBindingCheck = DateTime.UtcNow;
        
        if (_lastKnownPublicEndPoint != null)
        {
            Console.WriteLine($"초기 NAT 바인딩: {_lastKnownPublicEndPoint}");
        }
        
        return _lastKnownPublicEndPoint;
    }

    public void RegisterPeerBinding(string peerId, IPEndPoint peerEndPoint)
    {
        var binding = new NATBinding
        {
            PeerId = peerId,
            PeerEndPoint = peerEndPoint,
            LastSeen = DateTime.UtcNow,
            KeepAliveInterval = TimeSpan.FromSeconds(30),
            IsActive = true
        };
        
        _bindings[peerId] = binding;
        
        // Keep-alive 시작
        _ = Task.Run(() => StartKeepAlive(binding));
    }

    public void UnregisterPeerBinding(string peerId)
    {
        if (_bindings.TryRemove(peerId, out var binding))
        {
            binding.IsActive = false;
            Console.WriteLine($"피어 {peerId} 바인딩 제거됨");
        }
    }

    private async void CheckNATBinding(object? state)
    {
        try
        {
            var currentPublicEndPoint = await _stunClient.GetPublicEndPointAsync();
            
            if (currentPublicEndPoint != null && _lastKnownPublicEndPoint != null)
            {
                if (!currentPublicEndPoint.Equals(_lastKnownPublicEndPoint))
                {
                    Console.WriteLine($"NAT 리바인딩 감지: {_lastKnownPublicEndPoint} -> {currentPublicEndPoint}");
                    
                    var oldEndPoint = _lastKnownPublicEndPoint;
                    _lastKnownPublicEndPoint = currentPublicEndPoint;
                    
                    RebindingDetected?.Invoke(oldEndPoint, currentPublicEndPoint);
                    
                    // 모든 피어 연결에 대해 재바인딩 처리
                    await HandleRebinding();
                }
            }
            
            _lastBindingCheck = DateTime.UtcNow;
            
            // 만료된 바인딩 정리
            CleanupExpiredBindings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NAT 바인딩 확인 오류: {ex.Message}");
        }
    }

    private async Task HandleRebinding()
    {
        var rebindingTasks = new List<Task>();
        
        foreach (var binding in _bindings.Values)
        {
            if (binding.IsActive)
            {
                rebindingTasks.Add(RefreshPeerBinding(binding));
            }
        }
        
        await Task.WhenAll(rebindingTasks);
    }

    private async Task RefreshPeerBinding(NATBinding binding)
    {
        try
        {
            // 새로운 바인딩을 위해 패킷 전송
            var refreshPacket = CreateRefreshPacket();
            _udpClient.SendAsync(binding.PeerEndPoint, refreshPacket);
            
            Console.WriteLine($"피어 {binding.PeerId} 바인딩 갱신 중...");
            
            // 응답 대기
            var timeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;
            var responded = false;
            
            while (DateTime.UtcNow - startTime < timeout && !responded)
            {
                await Task.Delay(100);
                
                // 최근에 해당 피어로부터 패킷을 받았는지 확인
                if (DateTime.UtcNow - binding.LastSeen < TimeSpan.FromSeconds(5))
                {
                    responded = true;
                    binding.RebindingCount++;
                    Console.WriteLine($"피어 {binding.PeerId} 바인딩 갱신 성공");
                }
            }
            
            if (!responded)
            {
                Console.WriteLine($"피어 {binding.PeerId} 바인딩 갱신 실패");
                binding.FailedRebindings++;
                
                if (binding.FailedRebindings >= 3)
                {
                    binding.IsActive = false;
                    BindingExpired?.Invoke(binding.PeerId);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"피어 {binding.PeerId} 바인딩 갱신 오류: {ex.Message}");
        }
    }

    private async void StartKeepAlive(NATBinding binding)
    {
        while (binding.IsActive)
        {
            try
            {
                var keepAlivePacket = CreateKeepAlivePacket();
                _udpClient.SendAsync(binding.PeerEndPoint, keepAlivePacket);
                
                binding.LastKeepAlive = DateTime.UtcNow;
                
                await Task.Delay(binding.KeepAliveInterval);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Keep-alive 오류 (피어 {binding.PeerId}): {ex.Message}");
                break;
            }
        }
    }

    public void OnPacketReceived(IPEndPoint sender, byte[] data)
    {
        // 해당 피어의 바인딩 정보 업데이트
        var binding = _bindings.Values
            .FirstOrDefault(b => b.PeerEndPoint.Equals(sender));
        
        if (binding != null)
        {
            binding.LastSeen = DateTime.UtcNow;
            
            // Keep-alive 응답 처리
            if (IsKeepAliveResponse(data))
            {
                binding.LastKeepAliveResponse = DateTime.UtcNow;
            }
        }
    }

    private void CleanupExpiredBindings()
    {
        var expiredBindings = new List<string>();
        var now = DateTime.UtcNow;
        
        foreach (var kvp in _bindings)
        {
            var binding = kvp.Value;
            
            // 30초 이상 응답이 없으면 만료로 간주
            if (now - binding.LastSeen > TimeSpan.FromSeconds(30))
            {
                expiredBindings.Add(kvp.Key);
            }
        }
        
        foreach (var peerId in expiredBindings)
        {
            if (_bindings.TryRemove(peerId, out var binding))
            {
                binding.IsActive = false;
                BindingExpired?.Invoke(peerId);
                Console.WriteLine($"피어 {peerId} 바인딩 만료됨");
            }
        }
    }

    private byte[] CreateKeepAlivePacket()
    {
        var packet = new List<byte>();
        packet.Add(0xFE); // Keep-alive 마커
        packet.AddRange(BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        return packet.ToArray();
    }

    private byte[] CreateRefreshPacket()
    {
        var packet = new List<byte>();
        packet.Add(0xFD); // Refresh 마커
        packet.AddRange(BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        return packet.ToArray();
    }

    private bool IsKeepAliveResponse(byte[] data)
    {
        return data.Length > 0 && data[0] == 0xFE;
    }

    public NATBindingInfo GetBindingInfo()
    {
        return new NATBindingInfo
        {
            CurrentPublicEndPoint = _lastKnownPublicEndPoint,
            LastBindingCheck = _lastBindingCheck,
            ActiveBindings = _bindings.Count,
            BindingDetails = _bindings.Values.Select(b => new PeerBindingInfo
            {
                PeerId = b.PeerId,
                PeerEndPoint = b.PeerEndPoint,
                LastSeen = b.LastSeen,
                RebindingCount = b.RebindingCount,
                FailedRebindings = b.FailedRebindings,
                IsActive = b.IsActive
            }).ToList()
        };
    }

    public void Dispose()
    {
        _rebindingCheckTimer?.Dispose();
        
        // 모든 바인딩 비활성화
        foreach (var binding in _bindings.Values)
        {
            binding.IsActive = false;
        }
        
        _bindings.Clear();
    }
}

public class NATBinding
{
    public string PeerId { get; set; } = "";
    public IPEndPoint PeerEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
    public DateTime LastSeen { get; set; }
    public DateTime LastKeepAlive { get; set; }
    public DateTime LastKeepAliveResponse { get; set; }
    public TimeSpan KeepAliveInterval { get; set; }
    public int RebindingCount { get; set; }
    public int FailedRebindings { get; set; }
    public bool IsActive { get; set; }
}

public class NATBindingInfo
{
    public IPEndPoint? CurrentPublicEndPoint { get; set; }
    public DateTime LastBindingCheck { get; set; }
    public int ActiveBindings { get; set; }
    public List<PeerBindingInfo> BindingDetails { get; set; } = new();
}

public class PeerBindingInfo
{
    public string PeerId { get; set; } = "";
    public IPEndPoint PeerEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
    public DateTime LastSeen { get; set; }
    public int RebindingCount { get; set; }
    public int FailedRebindings { get; set; }
    public bool IsActive { get; set; }
}
```

### 통합 사용 예제

```csharp
public class GameNetworkManager : IDisposable
{
    private readonly P2PConnectionManager _connectionManager;
    private readonly GameSessionManager _sessionManager;
    private readonly DynamicConnectionManager _dynamicManager;
    private readonly ConnectionQualityMonitor _qualityMonitor;
    private readonly NATRebindingDetector _rebindingDetector;

    public GameNetworkManager(int localPort, IPEndPoint coordinationServer, string playerId)
    {
        _connectionManager = new P2PConnectionManager(localPort, coordinationServer);
        _sessionManager = new GameSessionManager(_connectionManager, playerId);
        _dynamicManager = new DynamicConnectionManager(_sessionManager, _connectionManager);
        _qualityMonitor = new ConnectionQualityMonitor(_connectionManager);
        _rebindingDetector = new NATRebindingDetector(_connectionManager.UdpClient);

        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        _qualityMonitor.QualityChanged += (peerId, quality) =>
        {
            Console.WriteLine($"피어 {peerId} 연결 품질 변경: {quality}");
        };

        _rebindingDetector.RebindingDetected += async (oldEndPoint, newEndPoint) =>
        {
            Console.WriteLine($"NAT 리바인딩 감지, 모든 연결 갱신 중...");
            await RefreshAllConnections();
        };

        _dynamicManager.PlayerConnectionEstablished += (sessionId, playerId) =>
        {
            _rebindingDetector.RegisterPeerBinding(playerId, 
                _connectionManager.GetPeerEndPoint(playerId));
        };

        _dynamicManager.PlayerConnectionLost += (sessionId, playerId) =>
        {
            _rebindingDetector.UnregisterPeerBinding(playerId);
        };
    }

    public async Task<GameSession?> CreateGameAsync(string gameName, int maxPlayers = 8)
    {
        await _rebindingDetector.InitializeBinding();
        return await _sessionManager.CreateSessionAsync(gameName, maxPlayers);
    }

    public async Task<bool> JoinGameAsync(string sessionId, string playerName)
    {
        await _rebindingDetector.InitializeBinding();
        return await _sessionManager.JoinSessionAsync(sessionId, playerName);
    }

    private async Task RefreshAllConnections()
    {
        // 구현: 모든 연결을 새로고침하는 로직
        foreach (var session in _sessionManager.Sessions)
        {
            foreach (var player in session.Players)
            {
                if (player.PlayerId != _sessionManager.LocalPlayerId)
                {
                    await _dynamicManager.ReconnectToPlayer(session.SessionId, player.PlayerId);
                }
            }
        }
    }

    public void PrintNetworkStatus()
    {
        Console.WriteLine("=== 네트워크 상태 ===");
        
        var qualityReport = _qualityMonitor.GetQualityReport();
        foreach (var kvp in qualityReport)
        {
            var info = kvp.Value;
            Console.WriteLine($"피어 {kvp.Key}: {info.Quality}, " +
                            $"핑 {info.AveragePing.TotalMilliseconds:F0}ms, " +
                            $"손실률 {info.PacketLossRate:P1}");
        }

        var bindingInfo = _rebindingDetector.GetBindingInfo();
        Console.WriteLine($"NAT 바인딩: {bindingInfo.CurrentPublicEndPoint}, " +
                         $"활성 연결 {bindingInfo.ActiveBindings}개");
    }

    public void Dispose()
    {
        _rebindingDetector?.Dispose();
        _qualityMonitor?.Dispose();
        _dynamicManager?.Dispose();
        _sessionManager?.Dispose();
        _connectionManager?.Dispose();
    }
}

// 사용 예제
public class Program
{
    public static async Task Main(string[] args)
    {
        var coordinationServer = new IPEndPoint(IPAddress.Parse("your-server-ip"), 8080);
        var playerId = Environment.MachineName + "_" + Environment.UserName;
        
        using var networkManager = new GameNetworkManager(0, coordinationServer, playerId);
        
        Console.WriteLine("1: 게임 생성, 2: 게임 참가");
        var choice = Console.ReadLine();
        
        if (choice == "1")
        {
            var session = await networkManager.CreateGameAsync("Test Game", 4);
            if (session != null)
            {
                Console.WriteLine($"게임 생성됨: {session.SessionId}");
                Console.WriteLine("플레이어 대기 중... (s: 상태 확인, q: 종료)");
                
                string? input;
                while ((input = Console.ReadLine()) != "q")
                {
                    if (input == "s")
                    {
                        networkManager.PrintNetworkStatus();
                    }
                }
            }
        }
        else if (choice == "2")
        {
            Console.Write("세션 ID: ");
            var sessionId = Console.ReadLine();
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                var success = await networkManager.JoinGameAsync(sessionId, "Player");
                if (success)
                {
                    Console.WriteLine("게임 참가 성공!");
                    Console.WriteLine("s: 상태 확인, q: 종료");
                    
                    string? input;
                    while ((input = Console.ReadLine()) != "q")
                    {
                        if (input == "s")
                        {
                            networkManager.PrintNetworkStatus();
                        }
                    }
                }
            }
        }
    }
}
```

이 장에서는 게임에 특화된 P2P 연결 관리 시스템을 구현했습니다. 동적인 플레이어 참가/탈퇴, 연결 품질 모니터링, NAT 리바인딩 대응 등 실제 게임 개발에서 마주할 수 있는 복잡한 상황들을 체계적으로 처리할 수 있는 완전한 솔루션을 제공합니다.
  

# 8장: 시그널링 서버 구현

## 8.1 시그널링 서버의 역할과 설계
시그널링 서버는 P2P 연결 설정을 위한 핵심 인프라입니다. 직접적인 게임 데이터를 전송하지는 않지만, 피어들이 서로를 찾고 연결할 수 있도록 중계 역할을 수행합니다.

### 시그널링 서버 아키텍처

```csharp
public class SignalingServerArchitecture
{
    // 서버 구성 요소들
    public interface ISignalingComponent
    {
        Task StartAsync();
        Task StopAsync();
        bool IsRunning { get; }
    }

    // 1. 연결 관리자 - 클라이언트 연결 상태 관리
    public interface IConnectionManager : ISignalingComponent
    {
        event Action<string, ClientConnection>? ClientConnected;
        event Action<string>? ClientDisconnected;
        
        Task<bool> RegisterClientAsync(ClientConnection client);
        Task<bool> UnregisterClientAsync(string clientId);
        ClientConnection? GetClient(string clientId);
        IReadOnlyCollection<ClientConnection> GetAllClients();
    }

    // 2. 메시지 라우터 - 메시지 전달 및 라우팅
    public interface IMessageRouter : ISignalingComponent
    {
        Task RouteMessageAsync(string fromClientId, string toClientId, SignalingMessage message);
        Task BroadcastMessageAsync(string fromClientId, SignalingMessage message, string[]? targetClients = null);
        Task<bool> SendDirectMessageAsync(string clientId, SignalingMessage message);
    }

    // 3. 로비 관리자 - 게임 세션 및 매칭 관리
    public interface ILobbyManager : ISignalingComponent
    {
        Task<GameLobby?> CreateLobbyAsync(string hostClientId, LobbySettings settings);
        Task<bool> JoinLobbyAsync(string clientId, string lobbyId);
        Task<bool> LeaveLobbyAsync(string clientId, string lobbyId);
        Task<bool> CloseLobbyAsync(string lobbyId, string hostClientId);
        GameLobby? GetLobby(string lobbyId);
        IReadOnlyCollection<GameLobby> GetPublicLobbies();
    }

    // 4. 홀펀칭 조정자 - NAT 홀펀칭 지원
    public interface IHolePunchingCoordinator : ISignalingComponent
    {
        Task<bool> InitiateHolePunchingAsync(string clientA, string clientB);
        Task<bool> ExchangeConnectionInfoAsync(string clientA, string clientB, ConnectionInfo info);
        Task<STUNResponse?> HandleSTUNRequestAsync(STUNRequest request, IPEndPoint clientEndPoint);
    }
}

// 기본 데이터 모델들
public class ClientConnection
{
    public string ClientId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public IPEndPoint PublicEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
    public IPEndPoint LocalEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
    public Socket? TcpSocket { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
    public ClientState State { get; set; }
    public string? CurrentLobbyId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}

public enum ClientState
{
    Connecting,
    Connected,
    InLobby,
    InGame,
    Disconnecting,
    Disconnected
}

public class GameLobby
{
    public string LobbyId { get; set; } = "";
    public string LobbyName { get; set; } = "";
    public string HostClientId { get; set; } = "";
    public LobbySettings Settings { get; set; } = new();
    public List<string> ClientIds { get; set; } = new();
    public LobbyState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    
    public int PlayerCount => ClientIds.Count;
    public bool IsFull => PlayerCount >= Settings.MaxPlayers;
    public bool IsPublic => Settings.IsPublic;
}

public class LobbySettings
{
    public int MaxPlayers { get; set; } = 8;
    public bool IsPublic { get; set; } = true;
    public string GameMode { get; set; } = "";
    public string Password { get; set; } = "";
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public enum LobbyState
{
    Waiting,
    Starting,
    InProgress,
    Finished
}
```

### 통합 시그널링 서버

```csharp
public class SignalingServer : IDisposable
{
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageRouter _messageRouter;
    private readonly ILobbyManager _lobbyManager;
    private readonly IHolePunchingCoordinator _holePunchingCoordinator;
    
    private readonly TcpListener _tcpListener;
    private readonly UdpClient _udpListener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger _logger;
    
    public bool IsRunning { get; private set; }
    public int Port { get; }
    public IPAddress ListenAddress { get; }

    public SignalingServer(IPAddress listenAddress, int port, ILogger logger)
    {
        ListenAddress = listenAddress;
        Port = port;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();
        
        _tcpListener = new TcpListener(listenAddress, port);
        _udpListener = new UdpClient(port);
        
        // 구성 요소 초기화
        _connectionManager = new ConnectionManager(logger);
        _messageRouter = new MessageRouter(_connectionManager, logger);
        _lobbyManager = new LobbyManager(_connectionManager, _messageRouter, logger);
        _holePunchingCoordinator = new HolePunchingCoordinator(_connectionManager, _messageRouter, logger);
        
        SetupEventHandlers();
    }

    public async Task StartAsync()
    {
        if (IsRunning) return;

        try
        {
            // 모든 구성 요소 시작
            await _connectionManager.StartAsync();
            await _messageRouter.StartAsync();
            await _lobbyManager.StartAsync();
            await _holePunchingCoordinator.StartAsync();
            
            // TCP 리스너 시작
            _tcpListener.Start();
            _ = Task.Run(AcceptTcpClientsAsync);
            
            // UDP 리스너 시작
            _ = Task.Run(ProcessUdpMessagesAsync);
            
            IsRunning = true;
            _logger.LogInformation($"시그널링 서버 시작됨: {ListenAddress}:{Port}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"서버 시작 실패: {ex.Message}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        try
        {
            IsRunning = false;
            _cancellationTokenSource.Cancel();
            
            _tcpListener.Stop();
            _udpListener?.Close();
            
            // 모든 구성 요소 정지
            await _holePunchingCoordinator.StopAsync();
            await _lobbyManager.StopAsync();
            await _messageRouter.StopAsync();
            await _connectionManager.StopAsync();
            
            _logger.LogInformation("시그널링 서버 정지됨");
        }
        catch (Exception ex)
        {
            _logger.LogError($"서버 정지 중 오류: {ex.Message}");
        }
    }

    private async Task AcceptTcpClientsAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleTcpClientAsync(tcpClient));
            }
            catch (ObjectDisposedException)
            {
                break; // 서버 종료
            }
            catch (Exception ex)
            {
                _logger.LogError($"TCP 클라이언트 수락 오류: {ex.Message}");
            }
        }
    }

    private async Task HandleTcpClientAsync(TcpClient tcpClient)
    {
        var remoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint!;
        var clientId = "";
        
        try
        {
            using var networkStream = tcpClient.GetStream();
            var buffer = new byte[4096];
            
            while (tcpClient.Connected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var bytesRead = await networkStream.ReadAsync(buffer, _cancellationTokenSource.Token);
                if (bytesRead == 0) break;
                
                var message = SignalingMessage.Deserialize(buffer.AsSpan(0, bytesRead));
                if (message != null)
                {
                    clientId = await ProcessTcpMessage(message, tcpClient, remoteEndPoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"TCP 클라이언트 처리 오류 ({remoteEndPoint}): {ex.Message}");
        }
        finally
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                await _connectionManager.UnregisterClientAsync(clientId);
            }
            tcpClient.Close();
        }
    }

    private async Task<string> ProcessTcpMessage(SignalingMessage message, TcpClient tcpClient, IPEndPoint remoteEndPoint)
    {
        switch (message.Type)
        {
            case SignalingMessageType.Register:
                return await HandleClientRegistration(message, tcpClient, remoteEndPoint);
            
            case SignalingMessageType.CreateLobby:
                await HandleCreateLobby(message);
                break;
            
            case SignalingMessageType.JoinLobby:
                await HandleJoinLobby(message);
                break;
            
            case SignalingMessageType.LeaveLobby:
                await HandleLeaveLobby(message);
                break;
                
            case SignalingMessageType.DirectMessage:
                await HandleDirectMessage(message);
                break;
        }
        
        return message.SenderId;
    }

    private async Task ProcessUdpMessagesAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _udpListener.ReceiveAsync();
                _ = Task.Run(() => HandleUdpMessage(result.Buffer, result.RemoteEndPoint));
            }
            catch (ObjectDisposedException)
            {
                break; // 서버 종료
            }
            catch (Exception ex)
            {
                _logger.LogError($"UDP 메시지 처리 오류: {ex.Message}");
            }
        }
    }

    private async Task HandleUdpMessage(byte[] data, IPEndPoint remoteEndPoint)
    {
        try
        {
            // STUN 메시지인지 확인
            if (IsSTUNMessage(data))
            {
                var stunRequest = STUNRequest.Parse(data);
                var stunResponse = await _holePunchingCoordinator.HandleSTUNRequestAsync(stunRequest, remoteEndPoint);
                
                if (stunResponse != null)
                {
                    var responseData = stunResponse.Serialize();
                    await _udpListener.SendAsync(responseData, remoteEndPoint);
                }
            }
            else
            {
                // 일반 시그널링 메시지
                var message = SignalingMessage.Deserialize(data);
                if (message != null)
                {
                    await ProcessSignalingMessage(message, remoteEndPoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"UDP 메시지 처리 오류 ({remoteEndPoint}): {ex.Message}");
        }
    }

    private void SetupEventHandlers()
    {
        _connectionManager.ClientConnected += (clientId, client) =>
        {
            _logger.LogInformation($"클라이언트 연결됨: {clientId} ({client.PublicEndPoint})");
        };
        
        _connectionManager.ClientDisconnected += (clientId) =>
        {
            _logger.LogInformation($"클라이언트 연결 해제됨: {clientId}");
        };
    }

    // 메시지 핸들러들...
    private async Task<string> HandleClientRegistration(SignalingMessage message, TcpClient tcpClient, IPEndPoint remoteEndPoint)
    {
        var registerData = message.GetData<ClientRegistrationData>();
        if (registerData == null) return "";

        var client = new ClientConnection
        {
            ClientId = registerData.ClientId,
            DisplayName = registerData.DisplayName,
            PublicEndPoint = remoteEndPoint,
            LocalEndPoint = IPEndPoint.Parse(registerData.LocalEndPoint),
            TcpSocket = tcpClient.Client,
            ConnectedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            State = ClientState.Connected
        };

        await _connectionManager.RegisterClientAsync(client);
        
        var response = new SignalingMessage
        {
            Type = SignalingMessageType.RegisterResponse,
            SenderId = "server",
            TargetId = client.ClientId,
            Data = new ClientRegistrationResponse
            {
                Success = true,
                AssignedClientId = client.ClientId,
                ServerTime = DateTime.UtcNow,
                YourPublicEndPoint = remoteEndPoint.ToString()
            }
        };

        await SendTcpMessageAsync(tcpClient, response);
        return client.ClientId;
    }

    private async Task SendTcpMessageAsync(TcpClient tcpClient, SignalingMessage message)
    {
        try
        {
            var data = message.Serialize();
            var stream = tcpClient.GetStream();
            await stream.WriteAsync(data);
            await stream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"TCP 메시지 전송 오류: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _tcpListener?.Stop();
        _udpListener?.Close();
        _cancellationTokenSource?.Dispose();
    }
}
```
  

## 8.2 Socket을 이용한 실시간 시그널링
실시간 게임에서는 낮은 지연시간과 높은 안정성이 중요합니다. TCP와 UDP를 적절히 조합하여 최적의 성능을 달성하는 시그널링 시스템을 구현합니다.

### 메시지 라우터 구현

```csharp
public class MessageRouter : IMessageRouter
{
    private readonly IConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<string, MessageQueue> _messageQueues;
    private readonly Timer _messageProcessingTimer;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _routingSemaphore;
    
    private bool _isRunning;

    public MessageRouter(IConnectionManager connectionManager, ILogger logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _messageQueues = new ConcurrentDictionary<string, MessageQueue>();
        _routingSemaphore = new SemaphoreSlim(10, 10); // 최대 10개 동시 라우팅
        
        _messageProcessingTimer = new Timer(ProcessMessageQueues, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync()
    {
        _isRunning = true;
        _messageProcessingTimer.Change(0, 10); // 10ms마다 처리
        _logger.LogInformation("메시지 라우터 시작됨");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _messageProcessingTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        // 대기 중인 메시지 정리
        foreach (var queue in _messageQueues.Values)
        {
            queue.Dispose();
        }
        _messageQueues.Clear();
        
        _logger.LogInformation("메시지 라우터 정지됨");
        return Task.CompletedTask;
    }

    public bool IsRunning => _isRunning;

    public async Task RouteMessageAsync(string fromClientId, string toClientId, SignalingMessage message)
    {
        if (!_isRunning) return;

        await _routingSemaphore.WaitAsync();
        try
        {
            var targetClient = _connectionManager.GetClient(toClientId);
            if (targetClient == null)
            {
                _logger.LogWarning($"대상 클라이언트를 찾을 수 없음: {toClientId}");
                return;
            }

            message.SenderId = fromClientId;
            message.TargetId = toClientId;
            message.Timestamp = DateTime.UtcNow;

            // 메시지 우선순위에 따라 처리 방식 결정
            if (IsHighPriorityMessage(message))
            {
                // 고우선순위 메시지는 즉시 전송
                await SendMessageDirectlyAsync(targetClient, message);
            }
            else
            {
                // 일반 메시지는 큐에 추가
                EnqueueMessage(toClientId, message);
            }
        }
        finally
        {
            _routingSemaphore.Release();
        }
    }

    public async Task BroadcastMessageAsync(string fromClientId, SignalingMessage message, string[]? targetClients = null)
    {
        if (!_isRunning) return;

        var clients = targetClients != null 
            ? targetClients.Select(_connectionManager.GetClient).Where(c => c != null)
            : _connectionManager.GetAllClients().Where(c => c.ClientId != fromClientId);

        var tasks = clients.Select(client => 
            RouteMessageAsync(fromClientId, client!.ClientId, message.Clone())).ToArray();
        
        await Task.WhenAll(tasks);
    }

    public async Task<bool> SendDirectMessageAsync(string clientId, SignalingMessage message)
    {
        var client = _connectionManager.GetClient(clientId);
        if (client == null) return false;

        return await SendMessageDirectlyAsync(client, message);
    }

    private async Task<bool> SendMessageDirectlyAsync(ClientConnection client, SignalingMessage message)
    {
        try
        {
            var data = message.Serialize();
            
            // TCP 소켓을 통해 전송
            if (client.TcpSocket != null && client.TcpSocket.Connected)
            {
                await client.TcpSocket.SendAsync(data, SocketFlags.None);
                client.LastActivity = DateTime.UtcNow;
                return true;
            }
            
            _logger.LogWarning($"클라이언트 {client.ClientId} TCP 연결이 유효하지 않음");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"메시지 전송 실패 (클라이언트 {client.ClientId}): {ex.Message}");
            return false;
        }
    }

    private void EnqueueMessage(string clientId, SignalingMessage message)
    {
        var queue = _messageQueues.GetOrAdd(clientId, _ => new MessageQueue(clientId));
        queue.Enqueue(message);
    }

    private async void ProcessMessageQueues(object? state)
    {
        if (!_isRunning) return;

        var tasks = new List<Task>();
        
        foreach (var queue in _messageQueues.Values)
        {
            if (queue.HasMessages)
            {
                tasks.Add(ProcessClientQueue(queue));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
        
        // 빈 큐 정리
        CleanupEmptyQueues();
    }

    private async Task ProcessClientQueue(MessageQueue queue)
    {
        const int maxBatchSize = 10;
        var processedCount = 0;
        
        while (queue.HasMessages && processedCount < maxBatchSize)
        {
            if (queue.TryDequeue(out var message))
            {
                var client = _connectionManager.GetClient(queue.ClientId);
                if (client != null)
                {
                    await SendMessageDirectlyAsync(client, message);
                    processedCount++;
                }
                else
                {
                    // 클라이언트가 없으면 큐 정리
                    queue.Clear();
                    break;
                }
            }
        }
    }

    private void CleanupEmptyQueues()
    {
        var emptyQueues = _messageQueues
            .Where(kvp => !kvp.Value.HasMessages && 
                         DateTime.UtcNow - kvp.Value.LastActivity > TimeSpan.FromMinutes(5))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var clientId in emptyQueues)
        {
            if (_messageQueues.TryRemove(clientId, out var queue))
            {
                queue.Dispose();
            }
        }
    }

    private bool IsHighPriorityMessage(SignalingMessage message)
    {
        return message.Type == SignalingMessageType.HolePunchingRequest ||
               message.Type == SignalingMessageType.HolePunchingResponse ||
               message.Type == SignalingMessageType.ConnectionInfo ||
               message.Type == SignalingMessageType.Heartbeat;
    }
}

public class MessageQueue : IDisposable
{
    private readonly Queue<SignalingMessage> _messages;
    private readonly object _lockObject = new object();
    
    public string ClientId { get; }
    public DateTime LastActivity { get; private set; }
    public bool HasMessages { get; private set; }

    public MessageQueue(string clientId)
    {
        ClientId = clientId;
        _messages = new Queue<SignalingMessage>();
        LastActivity = DateTime.UtcNow;
    }

    public void Enqueue(SignalingMessage message)
    {
        lock (_lockObject)
        {
            _messages.Enqueue(message);
            HasMessages = _messages.Count > 0;
            LastActivity = DateTime.UtcNow;
        }
    }

    public bool TryDequeue(out SignalingMessage? message)
    {
        lock (_lockObject)
        {
            if (_messages.Count > 0)
            {
                message = _messages.Dequeue();
                HasMessages = _messages.Count > 0;
                LastActivity = DateTime.UtcNow;
                return true;
            }
            
            message = null;
            return false;
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            _messages.Clear();
            HasMessages = false;
            LastActivity = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        Clear();
    }
}
```

### 연결 관리자 구현

```csharp
public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, ClientConnection> _clients;
    private readonly Timer _healthCheckTimer;
    private readonly ILogger _logger;
    private bool _isRunning;

    public event Action<string, ClientConnection>? ClientConnected;
    public event Action<string>? ClientDisconnected;

    public ConnectionManager(ILogger logger)
    {
        _logger = logger;
        _clients = new ConcurrentDictionary<string, ClientConnection>();
        _healthCheckTimer = new Timer(PerformHealthCheck, null, Timeout.Infinite, Timeout.Infinite);
    }

    public Task StartAsync()
    {
        _isRunning = true;
        _healthCheckTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        _logger.LogInformation("연결 관리자 시작됨");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        // 모든 클라이언트 연결 해제
        var clientIds = _clients.Keys.ToList();
        foreach (var clientId in clientIds)
        {
            _ = UnregisterClientAsync(clientId);
        }
        
        _logger.LogInformation("연결 관리자 정지됨");
        return Task.CompletedTask;
    }

    public bool IsRunning => _isRunning;

    public Task<bool> RegisterClientAsync(ClientConnection client)
    {
        if (!_isRunning) return Task.FromResult(false);

        if (_clients.TryAdd(client.ClientId, client))
        {
            client.State = ClientState.Connected;
            client.LastActivity = DateTime.UtcNow;
            
            ClientConnected?.Invoke(client.ClientId, client);
            _logger.LogInformation($"클라이언트 등록됨: {client.ClientId} ({client.DisplayName})");
            
            return Task.FromResult(true);
        }
        
        _logger.LogWarning($"클라이언트 등록 실패 (중복 ID): {client.ClientId}");
        return Task.FromResult(false);
    }

    public Task<bool> UnregisterClientAsync(string clientId)
    {
        if (_clients.TryRemove(clientId, out var client))
        {
            try
            {
                client.State = ClientState.Disconnected;
                client.TcpSocket?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"클라이언트 연결 해제 중 오류 ({clientId}): {ex.Message}");
            }
            
            ClientDisconnected?.Invoke(clientId);
            _logger.LogInformation($"클라이언트 등록 해제됨: {clientId}");
            
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public ClientConnection? GetClient(string clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    public IReadOnlyCollection<ClientConnection> GetAllClients()
    {
        return _clients.Values.ToList().AsReadOnly();
    }

    private async void PerformHealthCheck(object? state)
    {
        if (!_isRunning) return;

        var now = DateTime.UtcNow;
        var timeoutThreshold = TimeSpan.FromMinutes(5);
        var timedOutClients = new List<string>();

        foreach (var kvp in _clients)
        {
            var client = kvp.Value;
            
            // 마지막 활동으로부터 타임아웃 확인
            if (now - client.LastActivity > timeoutThreshold)
            {
                timedOutClients.Add(client.ClientId);
                continue;
            }
            
            // TCP 연결 상태 확인
            if (client.TcpSocket != null && !IsSocketConnected(client.TcpSocket))
            {
                timedOutClients.Add(client.ClientId);
                continue;
            }
        }

        // 타임아웃된 클라이언트들 정리
        foreach (var clientId in timedOutClients)
        {
            await UnregisterClientAsync(clientId);
        }

        if (timedOutClients.Count > 0)
        {
            _logger.LogInformation($"헬스 체크로 {timedOutClients.Count}개 클라이언트 정리됨");
        }
    }

    private bool IsSocketConnected(Socket socket)
    {
        try
        {
            return socket.Connected && !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
        }
        catch
        {
            return false;
        }
    }

    public ConnectionStatistics GetStatistics()
    {
        var stats = new ConnectionStatistics
        {
            TotalConnections = _clients.Count,
            ConnectedClients = _clients.Values.Count(c => c.State == ClientState.Connected),
            ClientsInLobby = _clients.Values.Count(c => c.State == ClientState.InLobby),
            ClientsInGame = _clients.Values.Count(c => c.State == ClientState.InGame)
        };

        return stats;
    }
}

public class ConnectionStatistics
{
    public int TotalConnections { get; set; }
    public int ConnectedClients { get; set; }
    public int ClientsInLobby { get; set; }
    public int ClientsInGame { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```
  

## 8.3 Socket을 활용한 게임 로비 시스템
게임 로비는 플레이어들이 만나고 게임을 시작하기 전에 대기하는 공간입니다. 효율적인 매칭과 세션 관리 기능을 제공합니다.

### 로비 관리자 구현

```csharp
public class LobbyManager : ILobbyManager
{
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageRouter _messageRouter;
    private readonly ConcurrentDictionary<string, GameLobby> _lobbies;
    private readonly Timer _lobbyMaintenanceTimer;
    private readonly ILogger _logger;
    private bool _isRunning;

    public event Action<GameLobby>? LobbyCreated;
    public event Action<string>? LobbyClosed;
    public event Action<string, string>? PlayerJoinedLobby;
    public event Action<string, string>? PlayerLeftLobby;

    public LobbyManager(IConnectionManager connectionManager, IMessageRouter messageRouter, ILogger logger)
    {
        _connectionManager = connectionManager;
        _messageRouter = messageRouter;
        _logger = logger;
        _lobbies = new ConcurrentDictionary<string, GameLobby>();
        
        _lobbyMaintenanceTimer = new Timer(MaintainLobbies, null, Timeout.Infinite, Timeout.Infinite);
        
        // 클라이언트 연결 해제 시 로비에서 제거
        _connectionManager.ClientDisconnected += OnClientDisconnected;
    }

    public Task StartAsync()
    {
        _isRunning = true;
        _lobbyMaintenanceTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        _logger.LogInformation("로비 관리자 시작됨");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _lobbyMaintenanceTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        // 모든 로비 정리
        var lobbyIds = _lobbies.Keys.ToList();
        foreach (var lobbyId in lobbyIds)
        {
            _ = CloseLobbyAsync(lobbyId, "");
        }
        
        _logger.LogInformation("로비 관리자 정지됨");
        return Task.CompletedTask;
    }

    public bool IsRunning => _isRunning;

    public async Task<GameLobby?> CreateLobbyAsync(string hostClientId, LobbySettings settings)
    {
        if (!_isRunning) return null;

        var hostClient = _connectionManager.GetClient(hostClientId);
        if (hostClient == null)
        {
            _logger.LogWarning($"로비 생성 실패: 호스트 클라이언트를 찾을 수 없음 ({hostClientId})");
            return null;
        }

        var lobbyId = GenerateLobbyId();
        var lobby = new GameLobby
        {
            LobbyId = lobbyId,
            LobbyName = $"{hostClient.DisplayName}의 게임",
            HostClientId = hostClientId,
            Settings = settings,
            State = LobbyState.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        lobby.ClientIds.Add(hostClientId);

        if (_lobbies.TryAdd(lobbyId, lobby))
        {
            hostClient.State = ClientState.InLobby;
            hostClient.CurrentLobbyId = lobbyId;
            
            LobbyCreated?.Invoke(lobby);
            _logger.LogInformation($"로비 생성됨: {lobbyId} (호스트: {hostClientId})");
            
            // 호스트에게 생성 완료 알림
            var createResponse = new SignalingMessage
            {
                Type = SignalingMessageType.LobbyCreated,
                SenderId = "server",
                TargetId = hostClientId,
                Data = new LobbyCreatedData
                {
                    LobbyId = lobbyId,
                    Lobby = CreateLobbyInfo(lobby)
                }
            };
            
            await _messageRouter.SendDirectMessageAsync(hostClientId, createResponse);
            return lobby;
        }

        return null;
    }

    public async Task<bool> JoinLobbyAsync(string clientId, string lobbyId)
    {
        if (!_isRunning) return false;

        var client = _connectionManager.GetClient(clientId);
        var lobby = GetLobby(lobbyId);
        
        if (client == null || lobby == null)
        {
            _logger.LogWarning($"로비 참가 실패: 클라이언트 또는 로비를 찾을 수 없음 ({clientId}, {lobbyId})");
            return false;
        }

        if (lobby.IsFull)
        {
            _logger.LogWarning($"로비 참가 실패: 로비가 가득 참 ({lobbyId})");
            await SendJoinResponse(clientId, false, "로비가 가득 참");
            return false;
        }

        if (lobby.State != LobbyState.Waiting)
        {
            _logger.LogWarning($"로비 참가 실패: 로비가 대기 상태가 아님 ({lobbyId})");
            await SendJoinResponse(clientId, false, "게임이 이미 시작됨");
            return false;
        }

        // 비밀번호 확인 (필요시)
        if (!string.IsNullOrEmpty(lobby.Settings.Password))
        {
            // TODO: 비밀번호 검증 로직
        }

        // 로비에 참가
        lobby.ClientIds.Add(clientId);
        client.State = ClientState.InLobby;
        client.CurrentLobbyId = lobbyId;

        PlayerJoinedLobby?.Invoke(lobbyId, clientId);
        _logger.LogInformation($"플레이어가 로비에 참가함: {clientId} -> {lobbyId}");

        // 참가자에게 성공 응답
        await SendJoinResponse(clientId, true, "로비 참가 성공");

        // 로비의 모든 참가자에게 새 플레이어 알림
        var playerJoinedMessage = new SignalingMessage
        {
            Type = SignalingMessageType.PlayerJoinedLobby,
            SenderId = "server",
            Data = new PlayerJoinedLobbyData
            {
                LobbyId = lobbyId,
                PlayerId = clientId,
                PlayerName = client.DisplayName,
                UpdatedLobby = CreateLobbyInfo(lobby)
            }
        };

        await BroadcastToLobby(lobby, playerJoinedMessage, clientId);

        return true;
    }

    public async Task<bool> LeaveLobbyAsync(string clientId, string lobbyId)
    {
        if (!_isRunning) return false;

        var client = _connectionManager.GetClient(clientId);
        var lobby = GetLobby(lobbyId);
        
        if (client == null || lobby == null) return false;

        // 로비에서 제거
        if (!lobby.ClientIds.Remove(clientId)) return false;

        client.State = ClientState.Connected;
        client.CurrentLobbyId = null;

        PlayerLeftLobby?.Invoke(lobbyId, clientId);
        _logger.LogInformation($"플레이어가 로비를 떠남: {clientId} <- {lobbyId}");

        // 로비가 비었거나 호스트가 떠난 경우
        if (lobby.ClientIds.Count == 0 || clientId == lobby.HostClientId)
        {
            await CloseLobbyAsync(lobbyId, clientId);
        }
        else
        {
            // 남은 플레이어들에게 알림
            var playerLeftMessage = new SignalingMessage
            {
                Type = SignalingMessageType.PlayerLeftLobby,
                SenderId = "server",
                Data = new PlayerLeftLobbyData
                {
                    LobbyId = lobbyId,
                    PlayerId = clientId,
                    UpdatedLobby = CreateLobbyInfo(lobby)
                }
            };

            await BroadcastToLobby(lobby, playerLeftMessage);
        }

        return true;
    }

    public async Task<bool> CloseLobbyAsync(string lobbyId, string requesterId)
    {
        if (!_lobbies.TryRemove(lobbyId, out var lobby)) return false;

        // 권한 확인 (호스트 또는 서버만 로비 종료 가능)
        if (!string.IsNullOrEmpty(requesterId) && requesterId != lobby.HostClientId)
        {
            _logger.LogWarning($"로비 종료 권한 없음: {requesterId} (호스트: {lobby.HostClientId})");
            return false;
        }

        // 모든 플레이어에게 로비 종료 알림
        var lobbyClosedMessage = new SignalingMessage
        {
            Type = SignalingMessageType.LobbyClosed,
            SenderId = "server",
            Data = new LobbyClosedData
            {
                LobbyId = lobbyId,
                Reason = "호스트가 로비를 종료함"
            }
        };

        await BroadcastToLobby(lobby, lobbyClosedMessage);

        // 모든 플레이어 상태 변경
        foreach (var clientId in lobby.ClientIds)
        {
            var client = _connectionManager.GetClient(clientId);
            if (client != null)
            {
                client.State = ClientState.Connected;
                client.CurrentLobbyId = null;
            }
        }

        LobbyClosed?.Invoke(lobbyId);
        _logger.LogInformation($"로비 종료됨: {lobbyId}");

        return true;
    }

    public GameLobby? GetLobby(string lobbyId)
    {
        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

    public IReadOnlyCollection<GameLobby> GetPublicLobbies()
    {
        return _lobbies.Values
            .Where(lobby => lobby.IsPublic && lobby.State == LobbyState.Waiting)
            .ToList()
            .AsReadOnly();
    }

    private async void OnClientDisconnected(string clientId)
    {
        // 클라이언트가 참가한 로비에서 자동 제거
        var client = _connectionManager.GetClient(clientId);
        if (client?.CurrentLobbyId != null)
        {
            await LeaveLobbyAsync(clientId, client.CurrentLobbyId);
        }
    }

    private async void MaintainLobbies(object? state)
    {
        if (!_isRunning) return;

        var now = DateTime.UtcNow;
        var oldLobbies = new List<string>();

        // 오래된 로비나 비어있는 로비 정리
        foreach (var kvp in _lobbies)
        {
            var lobby = kvp.Value;
            
            // 30분 이상 된 대기 중인 로비 또는 비어있는 로비
            if ((now - lobby.CreatedAt > TimeSpan.FromMinutes(30) && lobby.State == LobbyState.Waiting) ||
                lobby.ClientIds.Count == 0)
            {
                oldLobbies.Add(lobby.LobbyId);
            }
        }

        foreach (var lobbyId in oldLobbies)
        {
            await CloseLobbyAsync(lobbyId, "");
        }

        if (oldLobbies.Count > 0)
        {
            _logger.LogInformation($"로비 정리: {oldLobbies.Count}개 로비 제거됨");
        }
    }

    private async Task SendJoinResponse(string clientId, bool success, string message)
    {
        var response = new SignalingMessage
        {
            Type = SignalingMessageType.JoinLobbyResponse,
            SenderId = "server",
            TargetId = clientId,
            Data = new JoinLobbyResponse
            {
                Success = success,
                Message = message
            }
        };

        await _messageRouter.SendDirectMessageAsync(clientId, response);
    }

    private async Task BroadcastToLobby(GameLobby lobby, SignalingMessage message, string? excludeClientId = null)
    {
        var targetClients = lobby.ClientIds
            .Where(id => id != excludeClientId)
            .ToArray();

        if (targetClients.Length > 0)
        {
            await _messageRouter.BroadcastMessageAsync("server", message, targetClients);
        }
    }

    private LobbyInfo CreateLobbyInfo(GameLobby lobby)
    {
        var players = lobby.ClientIds
            .Select(_connectionManager.GetClient)
            .Where(client => client != null)
            .Select(client => new LobbyPlayerInfo
            {
                PlayerId = client!.ClientId,
                PlayerName = client.DisplayName,
                IsHost = client.ClientId == lobby.HostClientId
            })
            .ToList();

        return new LobbyInfo
        {
            LobbyId = lobby.LobbyId,
            LobbyName = lobby.LobbyName,
            HostPlayerId = lobby.HostClientId,
            Settings = lobby.Settings,
            State = lobby.State,
            Players = players,
            CreatedAt = lobby.CreatedAt
        };
    }

    private string GenerateLobbyId()
    {
        return "lobby_" + Guid.NewGuid().ToString("N")[..8];
    }

    public LobbyStatistics GetStatistics()
    {
        return new LobbyStatistics
        {
            TotalLobbies = _lobbies.Count,
            WaitingLobbies = _lobbies.Values.Count(l => l.State == LobbyState.Waiting),
            ActiveLobbies = _lobbies.Values.Count(l => l.State == LobbyState.InProgress),
            TotalPlayersInLobbies = _lobbies.Values.Sum(l => l.ClientIds.Count)
        };
    }
}

// 데이터 모델들
public class LobbyInfo
{
    public string LobbyId { get; set; } = "";
    public string LobbyName { get; set; } = "";
    public string HostPlayerId { get; set; } = "";
    public LobbySettings Settings { get; set; } = new();
    public LobbyState State { get; set; }
    public List<LobbyPlayerInfo> Players { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class LobbyPlayerInfo
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public bool IsHost { get; set; }
}

public class LobbyStatistics
{
    public int TotalLobbies { get; set; }
    public int WaitingLobbies { get; set; }
    public int ActiveLobbies { get; set; }
    public int TotalPlayersInLobbies { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```
  

## 8.4 시그널링 메시지 최적화
대규모 멀티플레이어 환경에서는 메시지 처리 효율성이 중요합니다. 메시지 압축, 배치 처리, 우선순위 관리 등을 구현합니다.

### 메시지 최적화 시스템

```csharp
public class MessageOptimizer
{
    private readonly Dictionary<SignalingMessageType, MessageCompressionStrategy> _compressionStrategies;
    private readonly MessageBatcher _batcher;
    private readonly MessagePriorityManager _priorityManager;

    public MessageOptimizer()
    {
        _compressionStrategies = InitializeCompressionStrategies();
        _batcher = new MessageBatcher();
        _priorityManager = new MessagePriorityManager();
    }

    public OptimizedMessage OptimizeMessage(SignalingMessage message)
    {
        // 1. 우선순위 설정
        var priority = _priorityManager.GetPriority(message);
        
        // 2. 압축 적용
        var compressedData = CompressMessage(message);
        
        // 3. 최적화된 메시지 생성
        return new OptimizedMessage
        {
            OriginalMessage = message,
            CompressedData = compressedData,
            Priority = priority,
            CompressionRatio = (float)compressedData.Length / message.Serialize().Length,
            OptimizedAt = DateTime.UtcNow
        };
    }

    public List<OptimizedMessage> BatchMessages(IEnumerable<OptimizedMessage> messages)
    {
        return _batcher.BatchMessages(messages);
    }

    private byte[] CompressMessage(SignalingMessage message)
    {
        var strategy = _compressionStrategies.GetValueOrDefault(message.Type, 
            _compressionStrategies[SignalingMessageType.Default]);
        
        return strategy.Compress(message);
    }

    private Dictionary<SignalingMessageType, MessageCompressionStrategy> InitializeCompressionStrategies()
    {
        return new Dictionary<SignalingMessageType, MessageCompressionStrategy>
        {
            [SignalingMessageType.Default] = new GZipCompressionStrategy(),
            [SignalingMessageType.Heartbeat] = new NoCompressionStrategy(),
            [SignalingMessageType.HolePunchingRequest] = new LZ4CompressionStrategy(),
            [SignalingMessageType.HolePunchingResponse] = new LZ4CompressionStrategy(),
            [SignalingMessageType.LobbyUpdate] = new GZipCompressionStrategy(),
            [SignalingMessageType.PlayerList] = new DeltaCompressionStrategy()
        };
    }
}

public abstract class MessageCompressionStrategy
{
    public abstract byte[] Compress(SignalingMessage message);
    public abstract SignalingMessage Decompress(byte[] compressedData);
}

public class GZipCompressionStrategy : MessageCompressionStrategy
{
    public override byte[] Compress(SignalingMessage message)
    {
        var originalData = message.Serialize();
        
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            gzip.Write(originalData);
        }
        
        return output.ToArray();
    }

    public override SignalingMessage Decompress(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        
        gzip.CopyTo(output);
        return SignalingMessage.Deserialize(output.ToArray())!;
    }
}

public class LZ4CompressionStrategy : MessageCompressionStrategy
{
    public override byte[] Compress(SignalingMessage message)
    {
        // LZ4 압축 구현 (실제로는 LZ4 라이브러리 사용)
        var originalData = message.Serialize();
        
        // 단순 구현 - 실제로는 K4os.Compression.LZ4 등 사용
        return SimpleCompress(originalData);
    }

    public override SignalingMessage Decompress(byte[] compressedData)
    {
        var decompressedData = SimpleDecompress(compressedData);
        return SignalingMessage.Deserialize(decompressedData)!;
    }

    private byte[] SimpleCompress(byte[] data)
    {
        // 간단한 RLE 압축
        var compressed = new List<byte>();
        
        for (int i = 0; i < data.Length; i++)
        {
            byte current = data[i];
            byte count = 1;
            
            while (i + 1 < data.Length && data[i + 1] == current && count < 255)
            {
                count++;
                i++;
            }
            
            compressed.Add(count);
            compressed.Add(current);
        }
        
        return compressed.ToArray();
    }

    private byte[] SimpleDecompress(byte[] compressedData)
    {
        var decompressed = new List<byte>();
        
        for (int i = 0; i < compressedData.Length; i += 2)
        {
            byte count = compressedData[i];
            byte value = compressedData[i + 1];
            
            for (int j = 0; j < count; j++)
            {
                decompressed.Add(value);
            }
        }
        
        return decompressed.ToArray();
    }
}

public class NoCompressionStrategy : MessageCompressionStrategy
{
    public override byte[] Compress(SignalingMessage message)
    {
        return message.Serialize();
    }

    public override SignalingMessage Decompress(byte[] compressedData)
    {
        return SignalingMessage.Deserialize(compressedData)!;
    }
}

public class DeltaCompressionStrategy : MessageCompressionStrategy
{
    private static readonly ConcurrentDictionary<string, SignalingMessage> _lastMessages = new();

    public override byte[] Compress(SignalingMessage message)
    {
        var key = $"{message.SenderId}_{message.Type}";
        
        if (_lastMessages.TryGetValue(key, out var lastMessage))
        {
            var delta = CreateDelta(lastMessage, message);
            _lastMessages[key] = message;
            return SerializeDelta(delta);
        }
        else
        {
            _lastMessages[key] = message;
            return message.Serialize();
        }
    }

    public override SignalingMessage Decompress(byte[] compressedData)
    {
        // 델타 압축 해제 구현
        return SignalingMessage.Deserialize(compressedData)!;
    }

    private MessageDelta CreateDelta(SignalingMessage oldMessage, SignalingMessage newMessage)
    {
        // 메시지 간 차이점 계산
        return new MessageDelta
        {
            Type = newMessage.Type,
            Changes = CalculateChanges(oldMessage, newMessage)
        };
    }

    private Dictionary<string, object> CalculateChanges(SignalingMessage oldMessage, SignalingMessage newMessage)
    {
        // 실제 구현에서는 JSON 속성 비교 등을 수행
        return new Dictionary<string, object>();
    }

    private byte[] SerializeDelta(MessageDelta delta)
    {
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(delta);
    }
}

public class MessageBatcher
{
    private const int MAX_BATCH_SIZE = 10;
    private const int MAX_BATCH_DELAY_MS = 50;

    public List<OptimizedMessage> BatchMessages(IEnumerable<OptimizedMessage> messages)
    {
        var messagesList = messages.ToList();
        var batches = new List<OptimizedMessage>();
        
        // 우선순위별로 그룹화
        var priorityGroups = messagesList.GroupBy(m => m.Priority);
        
        foreach (var group in priorityGroups.OrderByDescending(g => g.Key))
        {
            var groupMessages = group.ToList();
            
            // 배치 크기에 따라 분할
            for (int i = 0; i < groupMessages.Count; i += MAX_BATCH_SIZE)
            {
                var batch = groupMessages.Skip(i).Take(MAX_BATCH_SIZE).ToList();
                
                if (batch.Count == 1)
                {
                    // 단일 메시지는 배치하지 않음
                    batches.Add(batch[0]);
                }
                else
                {
                    // 여러 메시지를 하나의 배치로 결합
                    var batchedMessage = CreateBatchMessage(batch);
                    batches.Add(batchedMessage);
                }
            }
        }
        
        return batches;
    }

    private OptimizedMessage CreateBatchMessage(List<OptimizedMessage> messages)
    {
        var batchData = new MessageBatch
        {
            Messages = messages.Select(m => m.OriginalMessage).ToList(),
            BatchId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var batchMessage = new SignalingMessage
        {
            Type = SignalingMessageType.Batch,
            SenderId = "server",
            Data = batchData
        };

        return new OptimizedMessage
        {
            OriginalMessage = batchMessage,
            CompressedData = batchMessage.Serialize(),
            Priority = messages.Max(m => m.Priority),
            OptimizedAt = DateTime.UtcNow
        };
    }
}

public class MessagePriorityManager
{
    private readonly Dictionary<SignalingMessageType, int> _priorities;

    public MessagePriorityManager()
    {
        _priorities = new Dictionary<SignalingMessageType, int>
        {
            [SignalingMessageType.Heartbeat] = 10,
            [SignalingMessageType.HolePunchingRequest] = 9,
            [SignalingMessageType.HolePunchingResponse] = 9,
            [SignalingMessageType.ConnectionInfo] = 8,
            [SignalingMessageType.JoinLobbyResponse] = 7,
            [SignalingMessageType.LobbyCreated] = 6,
            [SignalingMessageType.PlayerJoinedLobby] = 5,
            [SignalingMessageType.PlayerLeftLobby] = 5,
            [SignalingMessageType.LobbyUpdate] = 4,
            [SignalingMessageType.PlayerList] = 3,
            [SignalingMessageType.DirectMessage] = 2,
            [SignalingMessageType.Batch] = 1
        };
    }

    public int GetPriority(SignalingMessage message)
    {
        return _priorities.GetValueOrDefault(message.Type, 1);
    }
}

// 데이터 모델들
public class OptimizedMessage
{
    public SignalingMessage OriginalMessage { get; set; } = new();
    public byte[] CompressedData { get; set; } = Array.Empty<byte>();
    public int Priority { get; set; }
    public float CompressionRatio { get; set; }
    public DateTime OptimizedAt { get; set; }
}

public class MessageDelta
{
    public SignalingMessageType Type { get; set; }
    public Dictionary<string, object> Changes { get; set; } = new();
}

public class MessageBatch
{
    public List<SignalingMessage> Messages { get; set; } = new();
    public string BatchId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

// 사용 예제
public class OptimizedSignalingServer : SignalingServer
{
    private readonly MessageOptimizer _messageOptimizer;
    private readonly Timer _batchProcessingTimer;
    private readonly ConcurrentQueue<OptimizedMessage> _pendingMessages;

    public OptimizedSignalingServer(IPAddress listenAddress, int port, ILogger logger) 
        : base(listenAddress, port, logger)
    {
        _messageOptimizer = new MessageOptimizer();
        _pendingMessages = new ConcurrentQueue<OptimizedMessage>();
        _batchProcessingTimer = new Timer(ProcessMessageBatches, null, 10, 10);
    }

    protected override async Task SendMessageAsync(ClientConnection client, SignalingMessage message)
    {
        // 메시지 최적화
        var optimizedMessage = _messageOptimizer.OptimizeMessage(message);
        
        if (optimizedMessage.Priority >= 8)
        {
            // 고우선순위 메시지는 즉시 전송
            await base.SendMessageAsync(client, optimizedMessage.OriginalMessage);
        }
        else
        {
            // 일반 메시지는 배치 처리 대기열에 추가
            _pendingMessages.Enqueue(optimizedMessage);
        }
    }

    private async void ProcessMessageBatches(object? state)
    {
        if (_pendingMessages.IsEmpty) return;

        var messages = new List<OptimizedMessage>();
        
        // 대기열에서 메시지 수집
        while (_pendingMessages.TryDequeue(out var message) && messages.Count < 50)
        {
            messages.Add(message);
        }

        if (messages.Count > 0)
        {
            // 메시지 배치 처리
            var batches = _messageOptimizer.BatchMessages(messages);
            
            foreach (var batch in batches)
            {
                var targetClient = GetClientConnection(batch.OriginalMessage.TargetId);
                if (targetClient != null)
                {
                    await base.SendMessageAsync(targetClient, batch.OriginalMessage);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _batchProcessingTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

이 장에서 구현한 시그널링 서버는 P2P 게임을 위한 완전한 인프라를 제공합니다. 실시간 메시지 라우팅, 효율적인 로비 관리, 그리고 최적화된 메시지 처리를 통해 대규모 멀티플레이어 게임을 지원할 수 있는 견고한 기반을 마련했습니다.   