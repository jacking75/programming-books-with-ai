# C#과 P2P 통신을 이용한 온라인 게임 만들기

저자: 최흥배, AI-Assisted   
  
------  
  
# 3장: STUN 프로토콜 심화
  
## 3.1 STUN의 동작 원리와 메시지 구조

### 3.1.1 STUN 프로토콜 개요
STUN(Session Traversal Utilities for NAT)은 RFC 5389에 정의된 프로토콜로, NAT 뒤에 있는 클라이언트가 자신의 공인 IP 주소와 포트를 발견할 수 있게 해줍니다. 또한 NAT의 특성을 파악하여 적절한 NAT 순회 전략을 결정하는 데 사용됩니다.

```csharp
public enum STUNMessageType : ushort
{
    BindingRequest = 0x0001,
    BindingResponse = 0x0101,
    BindingErrorResponse = 0x0111,
    SharedSecretRequest = 0x0002,
    SharedSecretResponse = 0x0102,
    SharedSecretErrorResponse = 0x0112
}

public enum STUNAttributeType : ushort
{
    MappedAddress = 0x0001,
    ResponseAddress = 0x0002,
    ChangeRequest = 0x0003,
    SourceAddress = 0x0004,
    ChangedAddress = 0x0005,
    Username = 0x0006,
    Password = 0x0007,
    MessageIntegrity = 0x0008,
    ErrorCode = 0x0009,
    UnknownAttributes = 0x000A,
    ReflectedFrom = 0x000B,
    XorMappedAddress = 0x0020,
    Software = 0x8022,
    AlternateServer = 0x8023,
    Fingerprint = 0x8028
}
```

### 3.1.2 STUN 메시지 구조
STUN 메시지는 20바이트의 고정 헤더와 가변 길이의 어트리뷰트들로 구성됩니다:

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct STUNMessageHeader
{
    public ushort MessageType;      // 메시지 타입
    public ushort MessageLength;    // 헤더를 제외한 메시지 길이
    public uint MagicCookie;        // 0x2112A442 (RFC 5389)
    public ulong TransactionId1;    // 트랜잭션 ID (96비트)
    public uint TransactionId2;
}

public class STUNMessage
{
    public const uint MAGIC_COOKIE = 0x2112A442;
    
    public STUNMessageType MessageType { get; set; }
    public byte[] TransactionId { get; set; }
    public List<STUNAttribute> Attributes { get; set; }
    
    public STUNMessage()
    {
        TransactionId = new byte[12];
        Attributes = new List<STUNAttribute>();
        
        // 트랜잭션 ID 생성 (96비트 랜덤)
        var random = new Random();
        random.NextBytes(TransactionId);
    }
    
    public byte[] ToByteArray()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        // 어트리뷰트들의 총 길이 계산
        var attributesLength = Attributes.Sum(attr => 4 + attr.GetPaddedLength());
        
        // 헤더 작성
        writer.Write(IPAddress.HostToNetworkOrder((short)MessageType));
        writer.Write(IPAddress.HostToNetworkOrder((short)attributesLength));
        writer.Write(IPAddress.HostToNetworkOrder((int)MAGIC_COOKIE));
        writer.Write(TransactionId);
        
        // 어트리뷰트들 작성
        foreach (var attribute in Attributes)
        {
            writer.Write(attribute.ToByteArray());
        }
        
        return ms.ToArray();
    }
    
    public static STUNMessage Parse(byte[] data)
    {
        if (data.Length < 20)
            throw new ArgumentException("STUN 메시지가 너무 짧습니다.");
        
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);
        
        var message = new STUNMessage();
        
        // 헤더 파싱
        message.MessageType = (STUNMessageType)IPAddress.NetworkToHostOrder(reader.ReadInt16());
        var messageLength = IPAddress.NetworkToHostOrder(reader.ReadInt16());
        var magicCookie = IPAddress.NetworkToHostOrder(reader.ReadInt32());
        
        if (magicCookie != MAGIC_COOKIE)
            throw new ArgumentException("잘못된 STUN 매직 쿠키입니다.");
        
        message.TransactionId = reader.ReadBytes(12);
        
        // 어트리뷰트 파싱
        var remainingLength = messageLength;
        while (remainingLength > 0 && ms.Position < ms.Length)
        {
            var attribute = STUNAttribute.Parse(reader);
            message.Attributes.Add(attribute);
            remainingLength -= (ushort)(4 + attribute.GetPaddedLength());
        }
        
        return message;
    }
}
```

### 3.1.3 STUN 어트리뷰트 구조
STUN 어트리뷰트는 TLV(Type-Length-Value) 형식을 따릅니다:

```csharp
public abstract class STUNAttribute
{
    public STUNAttributeType Type { get; set; }
    public abstract ushort Length { get; }
    public abstract byte[] Value { get; }
    
    public virtual byte[] ToByteArray()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        writer.Write(IPAddress.HostToNetworkOrder((short)Type));
        writer.Write(IPAddress.HostToNetworkOrder((short)Length));
        writer.Write(Value);
        
        // 4바이트 경계로 패딩
        var padding = (4 - (Length % 4)) % 4;
        for (int i = 0; i < padding; i++)
        {
            writer.Write((byte)0);
        }
        
        return ms.ToArray();
    }
    
    public int GetPaddedLength()
    {
        return ((Length + 3) / 4) * 4;
    }
    
    public static STUNAttribute Parse(BinaryReader reader)
    {
        var type = (STUNAttributeType)IPAddress.NetworkToHostOrder(reader.ReadInt16());
        var length = IPAddress.NetworkToHostOrder(reader.ReadInt16());
        var value = reader.ReadBytes(length);
        
        // 패딩 스킵
        var padding = (4 - (length % 4)) % 4;
        reader.ReadBytes(padding);
        
        return type switch
        {
            STUNAttributeType.MappedAddress => MappedAddressAttribute.FromBytes(value),
            STUNAttributeType.XorMappedAddress => XorMappedAddressAttribute.FromBytes(value),
            STUNAttributeType.ChangeRequest => ChangeRequestAttribute.FromBytes(value),
            STUNAttributeType.ErrorCode => ErrorCodeAttribute.FromBytes(value),
            _ => new UnknownAttribute(type, value)
        };
    }
}

public class MappedAddressAttribute : STUNAttribute
{
    public IPEndPoint MappedEndPoint { get; set; }
    
    public override ushort Length => 8;
    
    public override byte[] Value
    {
        get
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write((byte)0); // Reserved
            writer.Write((byte)0x01); // IPv4
            writer.Write(IPAddress.HostToNetworkOrder((short)MappedEndPoint.Port));
            writer.Write(MappedEndPoint.Address.GetAddressBytes());
            
            return ms.ToArray();
        }
    }
    
    public static MappedAddressAttribute FromBytes(byte[] data)
    {
        if (data.Length < 8)
            throw new ArgumentException("MappedAddress 어트리뷰트 데이터가 부족합니다.");
        
        var family = data[1];
        if (family != 0x01) // IPv4만 지원
            throw new NotSupportedException("IPv6는 아직 지원되지 않습니다.");
        
        var port = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
        var ipBytes = new byte[4];
        Array.Copy(data, 4, ipBytes, 0, 4);
        var ip = new IPAddress(ipBytes);
        
        return new MappedAddressAttribute
        {
            Type = STUNAttributeType.MappedAddress,
            MappedEndPoint = new IPEndPoint(ip, port)
        };
    }
}

public class XorMappedAddressAttribute : STUNAttribute
{
    public IPEndPoint MappedEndPoint { get; set; }
    public byte[] TransactionId { get; set; }
    
    public override ushort Length => 8;
    
    public override byte[] Value
    {
        get
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write((byte)0); // Reserved
            writer.Write((byte)0x01); // IPv4
            
            // 포트 XOR
            var xorPort = MappedEndPoint.Port ^ (STUNMessage.MAGIC_COOKIE >> 16);
            writer.Write(IPAddress.HostToNetworkOrder((short)xorPort));
            
            // IP 주소 XOR
            var ipBytes = MappedEndPoint.Address.GetAddressBytes();
            var magicBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)STUNMessage.MAGIC_COOKIE));
            
            for (int i = 0; i < 4; i++)
            {
                writer.Write((byte)(ipBytes[i] ^ magicBytes[i]));
            }
            
            return ms.ToArray();
        }
    }
    
    public static XorMappedAddressAttribute FromBytes(byte[] data, byte[] transactionId = null)
    {
        if (data.Length < 8)
            throw new ArgumentException("XorMappedAddress 어트리뷰트 데이터가 부족합니다.");
        
        var family = data[1];
        if (family != 0x01)
            throw new NotSupportedException("IPv6는 아직 지원되지 않습니다.");
        
        // 포트 XOR 해제
        var xorPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
        var port = xorPort ^ (STUNMessage.MAGIC_COOKIE >> 16);
        
        // IP 주소 XOR 해제
        var xorIpBytes = new byte[4];
        Array.Copy(data, 4, xorIpBytes, 0, 4);
        var magicBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)STUNMessage.MAGIC_COOKIE));
        
        var ipBytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            ipBytes[i] = (byte)(xorIpBytes[i] ^ magicBytes[i]);
        }
        
        return new XorMappedAddressAttribute
        {
            Type = STUNAttributeType.XorMappedAddress,
            MappedEndPoint = new IPEndPoint(new IPAddress(ipBytes), port),
            TransactionId = transactionId
        };
    }
}

public class ChangeRequestAttribute : STUNAttribute
{
    public bool ChangeIP { get; set; }
    public bool ChangePort { get; set; }
    
    public override ushort Length => 4;
    
    public override byte[] Value
    {
        get
        {
            var flags = 0u;
            if (ChangeIP) flags |= 0x04;
            if (ChangePort) flags |= 0x02;
            
            return BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)flags));
        }
    }
    
    public static ChangeRequestAttribute FromBytes(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("ChangeRequest 어트리뷰트 데이터가 부족합니다.");
        
        var flags = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
        
        return new ChangeRequestAttribute
        {
            Type = STUNAttributeType.ChangeRequest,
            ChangeIP = (flags & 0x04) != 0,
            ChangePort = (flags & 0x02) != 0
        };
    }
}
```
  

## 3.2 STUN 서버 구축과 운영

### 3.2.1 기본 STUN 서버 구현
STUN 서버는 클라이언트의 요청을 받아 클라이언트의 공인 IP 주소와 포트를 응답으로 돌려주는 역할을 합니다:

```csharp
public class STUNServer
{
    private UdpClient _primarySocket;
    private UdpClient _alternateSocket;
    private IPEndPoint _primaryEndPoint;
    private IPEndPoint _alternateEndPoint;
    private bool _isRunning;
    private readonly object _statsLock = new object();
    private ServerStatistics _statistics;
    
    public class ServerStatistics
    {
        public long TotalRequests { get; set; }
        public long SuccessfulResponses { get; set; }
        public long ErrorResponses { get; set; }
        public DateTime ServerStartTime { get; set; }
        public double RequestsPerSecond => TotalRequests / (DateTime.UtcNow - ServerStartTime).TotalSeconds;
    }
    
    public STUNServer(IPEndPoint primaryEndPoint, IPEndPoint alternateEndPoint = null)
    {
        _primaryEndPoint = primaryEndPoint;
        _alternateEndPoint = alternateEndPoint ?? new IPEndPoint(primaryEndPoint.Address, primaryEndPoint.Port + 1);
        _statistics = new ServerStatistics { ServerStartTime = DateTime.UtcNow };
    }
    
    public async Task StartAsync()
    {
        try
        {
            _primarySocket = new UdpClient(_primaryEndPoint);
            _alternateSocket = new UdpClient(_alternateEndPoint);
            
            _isRunning = true;
            
            Console.WriteLine($"STUN 서버 시작됨:");
            Console.WriteLine($"  Primary: {_primaryEndPoint}");
            Console.WriteLine($"  Alternate: {_alternateEndPoint}");
            
            // 두 소켓에서 동시에 수신 대기
            var primaryTask = HandleRequests(_primarySocket, _primaryEndPoint);
            var alternateTask = HandleRequests(_alternateSocket, _alternateEndPoint);
            
            await Task.WhenAll(primaryTask, alternateTask);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN 서버 시작 실패: {ex.Message}");
            throw;
        }
    }
    
    public void Stop()
    {
        _isRunning = false;
        _primarySocket?.Close();
        _alternateSocket?.Close();
        
        Console.WriteLine("STUN 서버가 중지되었습니다.");
        PrintStatistics();
    }
    
    private async Task HandleRequests(UdpClient socket, IPEndPoint localEndPoint)
    {
        while (_isRunning)
        {
            try
            {
                var result = await socket.ReceiveAsync();
                _ = Task.Run(() => ProcessRequest(socket, result, localEndPoint));
            }
            catch (ObjectDisposedException)
            {
                // 서버 종료 시 정상적인 예외
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"요청 수신 오류: {ex.Message}");
            }
        }
    }
    
    private async Task ProcessRequest(UdpClient socket, UdpReceiveResult result, IPEndPoint localEndPoint)
    {
        lock (_statsLock)
        {
            _statistics.TotalRequests++;
        }
        
        try
        {
            var request = STUNMessage.Parse(result.Buffer);
            
            if (request.MessageType == STUNMessageType.BindingRequest)
            {
                await ProcessBindingRequest(socket, request, result.RemoteEndPoint, localEndPoint);
            }
            else
            {
                await SendErrorResponse(socket, request, result.RemoteEndPoint, 400, "잘못된 요청");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"요청 처리 오류 ({result.RemoteEndPoint}): {ex.Message}");
            
            lock (_statsLock)
            {
                _statistics.ErrorResponses++;
            }
        }
    }
    
    private async Task ProcessBindingRequest(UdpClient socket, STUNMessage request, 
                                           IPEndPoint clientEndPoint, IPEndPoint localEndPoint)
    {
        var response = new STUNMessage
        {
            MessageType = STUNMessageType.BindingResponse,
            TransactionId = request.TransactionId
        };
        
        // MAPPED-ADDRESS 어트리뷰트 추가
        response.Attributes.Add(new MappedAddressAttribute
        {
            Type = STUNAttributeType.MappedAddress,
            MappedEndPoint = clientEndPoint
        });
        
        // XOR-MAPPED-ADDRESS 어트리뷰트 추가 (RFC 5389)
        response.Attributes.Add(new XorMappedAddressAttribute
        {
            Type = STUNAttributeType.XorMappedAddress,
            MappedEndPoint = clientEndPoint,
            TransactionId = request.TransactionId
        });
        
        // SOURCE-ADDRESS 어트리뷰트 추가
        response.Attributes.Add(new MappedAddressAttribute
        {
            Type = STUNAttributeType.SourceAddress,
            MappedEndPoint = localEndPoint
        });
        
        // CHANGE-REQUEST 처리
        var changeRequest = request.Attributes.OfType<ChangeRequestAttribute>().FirstOrDefault();
        if (changeRequest != null)
        {
            await HandleChangeRequest(changeRequest, response, clientEndPoint);
        }
        else
        {
            await SendResponse(socket, response, clientEndPoint);
        }
        
        lock (_statsLock)
        {
            _statistics.SuccessfulResponses++;
        }
        
        Console.WriteLine($"바인딩 응답 전송: {clientEndPoint} -> {clientEndPoint}");
    }
    
    private async Task HandleChangeRequest(ChangeRequestAttribute changeRequest, 
                                         STUNMessage response, IPEndPoint clientEndPoint)
    {
        UdpClient responseSocket;
        IPEndPoint responseEndPoint;
        
        if (changeRequest.ChangeIP && changeRequest.ChangePort)
        {
            // 다른 IP와 포트에서 응답
            responseSocket = _alternateSocket;
            responseEndPoint = _alternateEndPoint;
        }
        else if (changeRequest.ChangePort)
        {
            // 같은 IP, 다른 포트에서 응답
            responseSocket = _alternateSocket;
            responseEndPoint = new IPEndPoint(_primaryEndPoint.Address, _alternateEndPoint.Port);
        }
        else if (changeRequest.ChangeIP)
        {
            // 다른 IP, 같은 포트에서 응답 (멀티홈 환경에서만 가능)
            responseSocket = _primarySocket;
            responseEndPoint = _primaryEndPoint;
        }
        else
        {
            // 변경 없음
            responseSocket = _primarySocket;
            responseEndPoint = _primaryEndPoint;
        }
        
        await SendResponse(responseSocket, response, clientEndPoint);
    }
    
    private async Task SendResponse(UdpClient socket, STUNMessage response, IPEndPoint target)
    {
        var responseData = response.ToByteArray();
        await socket.SendAsync(responseData, responseData.Length, target);
    }
    
    private async Task SendErrorResponse(UdpClient socket, STUNMessage request, 
                                       IPEndPoint target, int errorCode, string reason)
    {
        var response = new STUNMessage
        {
            MessageType = STUNMessageType.BindingErrorResponse,
            TransactionId = request?.TransactionId ?? new byte[12]
        };
        
        response.Attributes.Add(new ErrorCodeAttribute
        {
            Type = STUNAttributeType.ErrorCode,
            ErrorCode = errorCode,
            ReasonPhrase = reason
        });
        
        await SendResponse(socket, response, target);
        
        lock (_statsLock)
        {
            _statistics.ErrorResponses++;
        }
    }
    
    private void PrintStatistics()
    {
        lock (_statsLock)
        {
            Console.WriteLine("\n=== STUN 서버 통계 ===");
            Console.WriteLine($"총 요청 수: {_statistics.TotalRequests:N0}");
            Console.WriteLine($"성공 응답: {_statistics.SuccessfulResponses:N0}");
            Console.WriteLine($"오류 응답: {_statistics.ErrorResponses:N0}");
            Console.WriteLine($"평균 요청/초: {_statistics.RequestsPerSecond:F2}");
            Console.WriteLine($"운영 시간: {DateTime.UtcNow - _statistics.ServerStartTime:hh\\:mm\\:ss}");
        }
    }
}

public class ErrorCodeAttribute : STUNAttribute
{
    public int ErrorCode { get; set; }
    public string ReasonPhrase { get; set; } = "";
    
    public override ushort Length => (ushort)(4 + System.Text.Encoding.UTF8.GetByteCount(ReasonPhrase));
    
    public override byte[] Value
    {
        get
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write((ushort)0); // Reserved
            writer.Write((byte)(ErrorCode / 100)); // Class
            writer.Write((byte)(ErrorCode % 100)); // Number
            writer.Write(System.Text.Encoding.UTF8.GetBytes(ReasonPhrase));
            
            return ms.ToArray();
        }
    }
    
    public static ErrorCodeAttribute FromBytes(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("ErrorCode 어트리뷰트 데이터가 부족합니다.");
        
        var errorClass = data[2];
        var errorNumber = data[3];
        var errorCode = errorClass * 100 + errorNumber;
        
        var reasonPhrase = "";
        if (data.Length > 4)
        {
            reasonPhrase = System.Text.Encoding.UTF8.GetString(data, 4, data.Length - 4);
        }
        
        return new ErrorCodeAttribute
        {
            Type = STUNAttributeType.ErrorCode,
            ErrorCode = errorCode,
            ReasonPhrase = reasonPhrase
        };
    }
}
```

### 3.2.2 고성능 STUN 서버 최적화
실제 운영 환경에서는 높은 처리량을 위한 최적화가 필요합니다:

```csharp
public class HighPerformanceSTUNServer
{
    private readonly Socket _primarySocket;
    private readonly Socket _alternateSocket;
    private readonly IPEndPoint _primaryEndPoint;
    private readonly IPEndPoint _alternateEndPoint;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ObjectPool<byte[]> _bufferPool;
    private ServerMetrics _metrics;
    
    private const int BUFFER_SIZE = 1500; // MTU 크기
    private const int MAX_CONCURRENT_CONNECTIONS = 10000;
    
    public class ServerMetrics
    {
        private long _totalRequests;
        private long _totalResponses;
        private long _totalErrors;
        private readonly object _lock = new object();
        
        public void IncrementRequests() => Interlocked.Increment(ref _totalRequests);
        public void IncrementResponses() => Interlocked.Increment(ref _totalResponses);
        public void IncrementErrors() => Interlocked.Increment(ref _totalErrors);
        
        public (long requests, long responses, long errors) GetCounters()
        {
            return (_totalRequests, _totalResponses, _totalErrors);
        }
    }
    
    public HighPerformanceSTUNServer(IPEndPoint primaryEndPoint, IPEndPoint alternateEndPoint = null)
    {
        _primaryEndPoint = primaryEndPoint;
        _alternateEndPoint = alternateEndPoint ?? new IPEndPoint(primaryEndPoint.Address, primaryEndPoint.Port + 1);
        _cancellationTokenSource = new CancellationTokenSource();
        _connectionSemaphore = new SemaphoreSlim(MAX_CONCURRENT_CONNECTIONS);
        _bufferPool = ObjectPool.Create<byte[]>(() => new byte[BUFFER_SIZE]);
        _metrics = new ServerMetrics();
        
        // 소켓 생성 및 최적화
        _primarySocket = CreateOptimizedSocket(_primaryEndPoint);
        _alternateSocket = CreateOptimizedSocket(_alternateEndPoint);
    }
    
    private Socket CreateOptimizedSocket(IPEndPoint endPoint)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        
        // 소켓 최적화 옵션들
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 8 * 1024 * 1024); // 8MB
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 8 * 1024 * 1024);    // 8MB
        
        // Windows 특정 최적화
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            socket.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.NoChecksum, true);
        }
        
        socket.Bind(endPoint);
        return socket;
    }
    
    public async Task StartAsync()
    {
        Console.WriteLine($"고성능 STUN 서버 시작:");
        Console.WriteLine($"  Primary: {_primaryEndPoint}");
        Console.WriteLine($"  Alternate: {_alternateEndPoint}");
        Console.WriteLine($"  최대 동시 연결: {MAX_CONCURRENT_CONNECTIONS:N0}");
        
        var tasks = new[]
        {
            HandleSocketAsync(_primarySocket, _cancellationTokenSource.Token),
            HandleSocketAsync(_alternateSocket, _cancellationTokenSource.Token),
            MetricsReportingTask(_cancellationTokenSource.Token)
        };
        
        await Task.WhenAll(tasks);
    }
    
    private async Task HandleSocketAsync(Socket socket, CancellationToken cancellationToken)
    {
        var endPoint = socket.LocalEndPoint as IPEndPoint;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var buffer = _bufferPool.Get();
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                
                try
                {
                    var result = await socket.ReceiveFromAsync(
                        new ArraySegment<byte>(buffer), 
                        SocketFlags.None, 
                        remoteEndPoint
                    );
                    
                    _metrics.IncrementRequests();
                    
                    // 연결 제한 확인
                    if (await _connectionSemaphore.WaitAsync(0))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessRequestAsync(socket, buffer, result.ReceivedBytes, 
                                                        result.RemoteEndPoint as IPEndPoint, endPoint);
                            }
                            finally
                            {
                                _connectionSemaphore.Release();
                                _bufferPool.Return(buffer);
                            }
                        });
                    }
                    else
                    {
                        _bufferPool.Return(buffer);
                        _metrics.IncrementErrors();
                        Console.WriteLine($"연결 제한 도달, 요청 거부: {remoteEndPoint}");
                    }
                }
                catch
                {
                    _bufferPool.Return(buffer);
                    throw;
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                _metrics.IncrementErrors();
                Console.WriteLine($"소켓 수신 오류: {ex.Message}");
                await Task.Delay(100, cancellationToken); // 과부하 방지
            }
        }
    }
    
    private async Task ProcessRequestAsync(Socket socket, byte[] buffer, int length, 
                                         IPEndPoint remoteEndPoint, IPEndPoint localEndPoint)
    {
        try
        {
            var requestData = new byte[length];
            Array.Copy(buffer, requestData, length);
            
            var request = STUNMessage.Parse(requestData);
            
            if (request.MessageType == STUNMessageType.BindingRequest)
            {
                var response = CreateBindingResponse(request, remoteEndPoint, localEndPoint);
                var responseData = response.ToByteArray();
                
                await socket.SendToAsync(
                    new ArraySegment<byte>(responseData), 
                    SocketFlags.None, 
                    remoteEndPoint
                );
                
                _metrics.IncrementResponses();
            }
            else
            {
                _metrics.IncrementErrors();
            }
        }
        catch (Exception ex)
        {
            _metrics.IncrementErrors();
            Console.WriteLine($"요청 처리 오류 ({remoteEndPoint}): {ex.Message}");
        }
    }
    
    private STUNMessage CreateBindingResponse(STUNMessage request, IPEndPoint clientEndPoint, IPEndPoint serverEndPoint)
    {
        var response = new STUNMessage
        {
            MessageType = STUNMessageType.BindingResponse,
            TransactionId = request.TransactionId
        };
        
        // XOR-MAPPED-ADDRESS (RFC 5389 권장)
        response.Attributes.Add(new XorMappedAddressAttribute
        {
            Type = STUNAttributeType.XorMappedAddress,
            MappedEndPoint = clientEndPoint,
            TransactionId = request.TransactionId
        });
        
        return response;
    }
    
    private async Task MetricsReportingTask(CancellationToken cancellationToken)
    {
        var lastRequests = 0L;
        var lastTime = DateTime.UtcNow;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(5000, cancellationToken); // 5초마다 리포트
            
            var (requests, responses, errors) = _metrics.GetCounters();
            var currentTime = DateTime.UtcNow;
            var rps = (requests - lastRequests) / (currentTime - lastTime).TotalSeconds;
            
            Console.WriteLine($"[{currentTime:HH:mm:ss}] 요청: {requests:N0}, 응답: {responses:N0}, " +
                            $"오류: {errors:N0}, RPS: {rps:F1}");
            
            lastRequests = requests;
            lastTime = currentTime;
        }
    }
    
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        _primarySocket?.Close();
        _alternateSocket?.Close();
        _connectionSemaphore?.Dispose();
        
        Console.WriteLine("고성능 STUN 서버가 중지되었습니다.");
    }
}
```
  
  
## 3.3 C#에서 STUN 클라이언트 구현

### 3.3.1 기본 STUN 클라이언트
STUN 클라이언트는 STUN 서버에 바인딩 요청을 보내고 자신의 공인 IP 주소를 얻습니다:

```csharp
public class STUNClient : IDisposable
{
    private UdpClient _udpClient;
    private readonly Random _random;
    private readonly int _timeout;
    private readonly int _retryCount;
    
    public STUNClient(int localPort = 0, int timeout = 3000, int retryCount = 3)
    {
        _udpClient = new UdpClient(localPort);
        _random = new Random();
        _timeout = timeout;
        _retryCount = retryCount;
    }
    
    public async Task<STUNBindingResult> GetPublicEndPointAsync(string stunServer, int stunPort = 3478)
    {
        var serverEndPoint = new IPEndPoint(IPAddress.Parse(stunServer), stunPort);
        
        for (int attempt = 1; attempt <= _retryCount; attempt++)
        {
            try
            {
                Console.WriteLine($"STUN 요청 시도 #{attempt} -> {serverEndPoint}");
                
                var request = CreateBindingRequest();
                var requestData = request.ToByteArray();
                
                // 요청 전송
                await _udpClient.SendAsync(requestData, requestData.Length, serverEndPoint);
                
                // 응답 대기
                var response = await ReceiveResponseAsync(request.TransactionId);
                
                if (response != null)
                {
                    return ParseBindingResponse(response);
                }
                
                Console.WriteLine($"시도 #{attempt} 타임아웃");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"시도 #{attempt} 실패: {ex.Message}");
            }
            
            if (attempt < _retryCount)
            {
                await Task.Delay(1000 * attempt); // 지수 백오프
            }
        }
        
        throw new TimeoutException($"STUN 서버 {stunServer}:{stunPort}에 연결할 수 없습니다.");
    }
    
    private STUNMessage CreateBindingRequest()
    {
        var request = new STUNMessage
        {
            MessageType = STUNMessageType.BindingRequest
        };
        
        // SOFTWARE 어트리뷰트 추가 (선택사항)
        request.Attributes.Add(new SoftwareAttribute
        {
            Type = STUNAttributeType.Software,
            Software = "P2P Game Client v1.0"
        });
        
        return request;
    }
    
    private async Task<STUNMessage> ReceiveResponseAsync(byte[] expectedTransactionId)
    {
        var timeoutTask = Task.Delay(_timeout);
        
        while (true)
        {
            var receiveTask = _udpClient.ReceiveAsync();
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                return null; // 타임아웃
            }
            
            var result = await receiveTask;
            
            try
            {
                var response = STUNMessage.Parse(result.Buffer);
                
                // 트랜잭션 ID 확인
                if (response.TransactionId.SequenceEqual(expectedTransactionId))
                {
                    return response;
                }
                
                Console.WriteLine("다른 트랜잭션 ID의 응답 무시");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"응답 파싱 오류: {ex.Message}");
            }
        }
    }
    
    private STUNBindingResult ParseBindingResponse(STUNMessage response)
    {
        var result = new STUNBindingResult();
        
        if (response.MessageType == STUNMessageType.BindingErrorResponse)
        {
            var errorAttr = response.Attributes.OfType<ErrorCodeAttribute>().FirstOrDefault();
            if (errorAttr != null)
            {
                result.IsSuccess = false;
                result.ErrorCode = errorAttr.ErrorCode;
                result.ErrorReason = errorAttr.ReasonPhrase;
                return result;
            }
        }
        
        // XOR-MAPPED-ADDRESS 우선 처리 (RFC 5389)
        var xorMappedAttr = response.Attributes.OfType<XorMappedAddressAttribute>().FirstOrDefault();
        if (xorMappedAttr != null)
        {
            result.PublicEndPoint = xorMappedAttr.MappedEndPoint;
            result.IsSuccess = true;
            return result;
        }
        
        // MAPPED-ADDRESS 처리
        var mappedAttr = response.Attributes.OfType<MappedAddressAttribute>().FirstOrDefault();
        if (mappedAttr != null)
        {
            result.PublicEndPoint = mappedAttr.MappedEndPoint;
            result.IsSuccess = true;
            return result;
        }
        
        result.IsSuccess = false;
        result.ErrorReason = "매핑된 주소를 찾을 수 없습니다.";
        return result;
    }
    
    public void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
    }
}

public class STUNBindingResult
{
    public bool IsSuccess { get; set; }
    public IPEndPoint PublicEndPoint { get; set; }
    public IPEndPoint LocalEndPoint { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorReason { get; set; }
    
    public override string ToString()
    {
        if (IsSuccess)
        {
            return $"성공: Public={PublicEndPoint}, Local={LocalEndPoint}";
        }
        else
        {
            return $"실패: Error={ErrorCode}, Reason={ErrorReason}";
        }
    }
}

public class SoftwareAttribute : STUNAttribute
{
    public string Software { get; set; } = "";
    
    public override ushort Length => (ushort)System.Text.Encoding.UTF8.GetByteCount(Software);
    
    public override byte[] Value => System.Text.Encoding.UTF8.GetBytes(Software);
    
    public static SoftwareAttribute FromBytes(byte[] data)
    {
        return new SoftwareAttribute
        {
            Type = STUNAttributeType.Software,
            Software = System.Text.Encoding.UTF8.GetString(data)
        };
    }
}
```

### 3.3.2 비동기 STUN 클라이언트 풀
여러 STUN 서버를 동시에 사용하여 안정성을 높이는 클라이언트 풀입니다:

```csharp
public class STUNClientPool : IDisposable
{
    private readonly List<string> _stunServers;
    private readonly SemaphoreSlim _semaphore;
    private readonly int _maxConcurrentRequests;
    private readonly int _timeout;
    
    public STUNClientPool(IEnumerable<string> stunServers, int maxConcurrentRequests = 5, int timeout = 3000)
    {
        _stunServers = stunServers.ToList();
        _maxConcurrentRequests = maxConcurrentRequests;
        _timeout = timeout;
        _semaphore = new SemaphoreSlim(_maxConcurrentRequests);
        
        if (!_stunServers.Any())
        {
            throw new ArgumentException("최소 하나의 STUN 서버가 필요합니다.");
        }
    }
    
    public async Task<STUNBindingResult> GetPublicEndPointAsync(int localPort = 0)
    {
        var tasks = _stunServers.Select(server => QueryServerAsync(server, localPort)).ToArray();
        
        try
        {
            // 가장 빠른 응답을 기다림
            var completedTask = await Task.WhenAny(tasks);
            var result = await completedTask;
            
            if (result.IsSuccess)
            {
                return result;
            }
            
            // 첫 번째 결과가 실패면 다른 결과들도 확인
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    var taskResult = await task;
                    if (taskResult.IsSuccess)
                    {
                        return taskResult;
                    }
                }
            }
            
            // 모든 완료된 작업이 실패했으면 나머지 대기
            var allResults = await Task.WhenAll(tasks);
            var successResult = allResults.FirstOrDefault(r => r.IsSuccess);
            
            return successResult ?? allResults.First();
        }
        finally
        {
            // 진행 중인 작업들 정리
            foreach (var task in tasks)
            {
                if (!task.IsCompleted)
                {
                    // 실제로는 CancellationToken을 사용해 작업을 취소해야 함
                }
            }
        }
    }
    
    private async Task<STUNBindingResult> QueryServerAsync(string server, int localPort)
    {
        await _semaphore.WaitAsync();
        
        try
        {
            Console.WriteLine($"STUN 서버 조회 시작: {server}");
            
            using var client = new STUNClient(localPort, _timeout, retryCount: 1);
            var result = await client.GetPublicEndPointAsync(server);
            
            Console.WriteLine($"STUN 서버 {server}: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN 서버 {server} 오류: {ex.Message}");
            return new STUNBindingResult
            {
                IsSuccess = false,
                ErrorReason = ex.Message
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<List<STUNBindingResult>> GetAllResultsAsync(int localPort = 0)
    {
        var tasks = _stunServers.Select(server => QueryServerAsync(server, localPort));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
    
    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}

// 사용 예제
public class STUNClientExample
{
    public static async Task Main(string[] args)
    {
        var stunServers = new[]
        {
            "stun.l.google.com",
            "stun1.l.google.com",
            "stun.ekiga.net",
            "stun.ideasip.com",
            "stun.voiparound.com"
        };
        
        using var stunPool = new STUNClientPool(stunServers);
        
        try
        {
            var result = await stunPool.GetPublicEndPointAsync();
            Console.WriteLine($"공인 주소: {result.PublicEndPoint}");
            
            if (result.IsSuccess)
            {
                // 홀펀칭 또는 P2P 연결 시도
                await AttemptP2PConnection(result.PublicEndPoint);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN 조회 실패: {ex.Message}");
        }
    }
    
    private static async Task AttemptP2PConnection(IPEndPoint publicEndPoint)
    {
        Console.WriteLine($"P2P 연결 시도: {publicEndPoint}");
        // 실제 P2P 연결 로직 구현
    }
}
```
  


## 3.4 STUN 바인딩과 NAT 타입 탐지

### 3.4.1 RFC 3489 기반 NAT 타입 탐지
NAT 타입을 정확히 판별하기 위해서는 여러 단계의 STUN 테스트가 필요합니다:

```csharp
public class NATTypeDetector
{
    private readonly List<string> _stunServers;
    private readonly int _timeout;
    
    public NATTypeDetector(IEnumerable<string> stunServers, int timeout = 5000)
    {
        _stunServers = stunServers.ToList();
        _timeout = timeout;
    }
    
    public async Task<NATDetectionResult> DetectNATTypeAsync(int localPort = 0)
    {
        var result = new NATDetectionResult();
        
        try
        {
            Console.WriteLine("NAT 타입 탐지 시작...");
            
            // Test 1: 기본 바인딩 테스트
            var test1Result = await PerformTest1(localPort);
            result.Test1 = test1Result;
            
            if (!test1Result.IsSuccess)
            {
                result.NATType = NATType.Blocked;
                result.CanReceiveFromInternet = false;
                return result;
            }
            
            result.PublicEndPoint = test1Result.MappedEndPoint;
            result.LocalEndPoint = test1Result.LocalEndPoint;
            
            // Test 2: 다른 IP, 다른 포트에서 수신 테스트
            var test2Result = await PerformTest2(localPort);
            result.Test2 = test2Result;
            
            if (test2Result.IsSuccess)
            {
                result.NATType = NATType.FullCone;
                result.CanReceiveFromInternet = true;
                return result;
            }
            
            // Test 3: 같은 IP, 다른 포트에서 수신 테스트
            var test3Result = await PerformTest3(localPort, test1Result.ChangedAddress);
            result.Test3 = test3Result;
            
            if (test3Result.IsSuccess)
            {
                result.NATType = NATType.RestrictedCone;
                result.CanReceiveFromInternet = false;
                return result;
            }
            
            // Test 4: 포트 제한 확인
            var test4Result = await PerformTest4(localPort);
            result.Test4 = test4Result;
            
            if (test1Result.MappedEndPoint.Equals(test4Result.MappedEndPoint))
            {
                result.NATType = NATType.PortRestricted;
            }
            else
            {
                result.NATType = NATType.Symmetric;
            }
            
            result.CanReceiveFromInternet = false;
            
            Console.WriteLine($"NAT 타입 탐지 완료: {result.NATType}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NAT 탐지 오류: {ex.Message}");
            result.NATType = NATType.Unknown;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
    
    // Test 1: 기본 바인딩 요청
    private async Task<STUNTestResult> PerformTest1(int localPort)
    {
        Console.WriteLine("Test 1: 기본 바인딩 테스트");
        
        using var client = new STUNClient(localPort, _timeout);
        var stunServer = _stunServers.First();
        
        try
        {
            var bindingResult = await client.GetPublicEndPointAsync(stunServer);
            
            if (bindingResult.IsSuccess)
            {
                return new STUNTestResult
                {
                    IsSuccess = true,
                    MappedEndPoint = bindingResult.PublicEndPoint,
                    LocalEndPoint = bindingResult.LocalEndPoint,
                    // ChangedAddress는 응답에서 파싱해야 함
                    ChangedAddress = new IPEndPoint(IPAddress.Parse(stunServer), 3479)
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test 1 실패: {ex.Message}");
        }
        
        return new STUNTestResult { IsSuccess = false };
    }
    
    // Test 2: CHANGE-REQUEST로 다른 IP/포트에서 응답 요청
    private async Task<STUNTestResult> PerformTest2(int localPort)
    {
        Console.WriteLine("Test 2: Change IP and Port 테스트");
        
        using var udpClient = new UdpClient(localPort);
        var stunServer = IPEndPoint.Parse(_stunServers.First() + ":3478");
        
        try
        {
            var request = new STUNMessage
            {
                MessageType = STUNMessageType.BindingRequest
            };
            
            // CHANGE-REQUEST: IP와 포트 모두 변경 요청
            request.Attributes.Add(new ChangeRequestAttribute
            {
                Type = STUNAttributeType.ChangeRequest,
                ChangeIP = true,
                ChangePort = true
            });
            
            var requestData = request.ToByteArray();
            await udpClient.SendAsync(requestData, requestData.Length, stunServer);
            
            // 5초 대기
            var timeoutTask = Task.Delay(_timeout);
            var receiveTask = udpClient.ReceiveAsync();
            
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            
            if (completedTask == receiveTask)
            {
                var result = await receiveTask;
                var response = STUNMessage.Parse(result.Buffer);
                
                if (response.TransactionId.SequenceEqual(request.TransactionId))
                {
                    return new STUNTestResult { IsSuccess = true };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test 2 오류: {ex.Message}");
        }
        
        return new STUNTestResult { IsSuccess = false };
    }
    
    // Test 3: 같은 IP, 다른 포트에서 응답 요청
    private async Task<STUNTestResult> PerformTest3(int localPort, IPEndPoint changedAddress)
    {
        Console.WriteLine("Test 3: Change Port 테스트");
        
        using var udpClient = new UdpClient(localPort);
        
        try
        {
            var request = new STUNMessage
            {
                MessageType = STUNMessageType.BindingRequest
            };
            
            // CHANGE-REQUEST: 포트만 변경 요청
            request.Attributes.Add(new ChangeRequestAttribute
            {
                Type = STUNAttributeType.ChangeRequest,
                ChangeIP = false,
                ChangePort = true
            });
            
            var requestData = request.ToByteArray();
            await udpClient.SendAsync(requestData, requestData.Length, changedAddress);
            
            var timeoutTask = Task.Delay(_timeout);
            var receiveTask = udpClient.ReceiveAsync();
            
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            
            if (completedTask == receiveTask)
            {
                return new STUNTestResult { IsSuccess = true };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test 3 오류: {ex.Message}");
        }
        
        return new STUNTestResult { IsSuccess = false };
    }
    
    // Test 4: 다른 STUN 서버로 바인딩 테스트 (Symmetric NAT 탐지용)
    private async Task<STUNTestResult> PerformTest4(int localPort)
    {
        Console.WriteLine("Test 4: 다른 서버 바인딩 테스트");
        
        if (_stunServers.Count < 2)
        {
            return new STUNTestResult { IsSuccess = false };
        }
        
        using var client = new STUNClient(localPort, _timeout);
        var alternateServer = _stunServers[1];
        
        try
        {
            var bindingResult = await client.GetPublicEndPointAsync(alternateServer);
            
            if (bindingResult.IsSuccess)
            {
                return new STUNTestResult
                {
                    IsSuccess = true,
                    MappedEndPoint = bindingResult.PublicEndPoint
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test 4 실패: {ex.Message}");
        }
        
        return new STUNTestResult { IsSuccess = false };
    }
}

public class NATDetectionResult
{
    public NATType NATType { get; set; } = NATType.Unknown;
    public IPEndPoint PublicEndPoint { get; set; }
    public IPEndPoint LocalEndPoint { get; set; }
    public bool CanReceiveFromInternet { get; set; }
    public string ErrorMessage { get; set; }
    
    public STUNTestResult Test1 { get; set; }
    public STUNTestResult Test2 { get; set; }
    public STUNTestResult Test3 { get; set; }
    public STUNTestResult Test4 { get; set; }
    
    public double GetHolePunchingSuccessRate()
    {
        return NATType switch
        {
            NATType.FullCone => 0.95,
            NATType.RestrictedCone => 0.90,
            NATType.PortRestricted => 0.85,
            NATType.Symmetric => 0.60,
            NATType.Blocked => 0.0,
            _ => 0.50
        };
    }
    
    public string GetRecommendedStrategy()
    {
        return NATType switch
        {
            NATType.FullCone => "직접 연결 가능, UDP 홀펀칭 권장",
            NATType.RestrictedCone => "UDP 홀펀칭 권장",
            NATType.PortRestricted => "UDP 홀펀칭 시도, 실패 시 TURN 릴레이",
            NATType.Symmetric => "TURN 릴레이 우선, UDP 홀펀칭 병행 시도",
            NATType.Blocked => "TURN 릴레이 또는 HTTP 터널링 필요",
            _ => "추가 테스트 필요"
        };
    }
}

public class STUNTestResult
{
    public bool IsSuccess { get; set; }
    public IPEndPoint MappedEndPoint { get; set; }
    public IPEndPoint LocalEndPoint { get; set; }
    public IPEndPoint ChangedAddress { get; set; }
    public string ErrorMessage { get; set; }
}

public enum NATType
{
    Unknown,
    FullCone,           // 가장 관대한 NAT
    RestrictedCone,     // IP 제한
    PortRestricted,     // IP + 포트 제한
    Symmetric,          // 가장 제한적인 NAT
    Blocked             // 차단됨
}
```

### 3.4.2 NAT 타입별 홀펀칭 전략
각 NAT 타입에 맞는 최적의 홀펀칭 전략을 구현합니다:

```csharp
public class NATTraversalStrategy
{
    public static async Task<bool> AttemptConnection(NATDetectionResult localNAT, 
                                                   NATDetectionResult remoteNAT,
                                                   IPEndPoint remotePublicEndPoint)
    {
        Console.WriteLine($"NAT 순회 시도: Local={localNAT.NATType}, Remote={remoteNAT.NATType}");
        
        var strategy = DetermineOptimalStrategy(localNAT.NATType, remoteNAT.NATType);
        
        foreach (var method in strategy)
        {
            Console.WriteLine($"시도 중: {method}");
            
            var success = await method switch
            {
                ConnectionMethod.DirectUDP => AttemptDirectUDPConnection(remotePublicEndPoint),
                ConnectionMethod.UDPHolePunching => AttemptUDPHolePunching(localNAT, remotePublicEndPoint),
                ConnectionMethod.TCPHolePunching => AttemptTCPHolePunching(localNAT, remotePublicEndPoint),
                ConnectionMethod.TURNRelay => AttemptTURNRelay(remotePublicEndPoint),
                ConnectionMethod.HTTPTunneling => AttemptHTTPTunneling(remotePublicEndPoint),
                _ => false
            };
            
            if (success)
            {
                Console.WriteLine($"연결 성공: {method}");
                return true;
            }
            
            Console.WriteLine($"연결 실패: {method}");
        }
        
        Console.WriteLine("모든 연결 방법이 실패했습니다.");
        return false;
    }
    
    private static List<ConnectionMethod> DetermineOptimalStrategy(NATType localType, NATType remoteType)
    {
        // Full Cone NAT의 경우 직접 연결 가능
        if (localType == NATType.FullCone || remoteType == NATType.FullCone)
        {
            return new List<ConnectionMethod>
            {
                ConnectionMethod.DirectUDP,
                ConnectionMethod.UDPHolePunching,
                ConnectionMethod.TURNRelay
            };
        }
        
        // Symmetric NAT이 포함된 경우 TURN 우선
        if (localType == NATType.Symmetric || remoteType == NATType.Symmetric)
        {
            return new List<ConnectionMethod>
            {
                ConnectionMethod.TURNRelay,
                ConnectionMethod.UDPHolePunching,
                ConnectionMethod.TCPHolePunching,
                ConnectionMethod.HTTPTunneling
            };
        }
        
        // 일반적인 경우 UDP 홀펀칭 우선
        return new List<ConnectionMethod>
        {
            ConnectionMethod.UDPHolePunching,
            ConnectionMethod.TCPHolePunching,
            ConnectionMethod.TURNRelay,
            ConnectionMethod.HTTPTunneling
        };
    }
    
    private static async Task<bool> AttemptDirectUDPConnection(IPEndPoint target)
    {
        try
        {
            using var udpClient = new UdpClient();
            var testMessage = System.Text.Encoding.UTF8.GetBytes("DIRECT_TEST");
            
            await udpClient.SendAsync(testMessage, testMessage.Length, target);
            
            var timeoutTask = Task.Delay(2000);
            var receiveTask = udpClient.ReceiveAsync();
            
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            return completedTask == receiveTask;
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task<bool> AttemptUDPHolePunching(NATDetectionResult localNAT, IPEndPoint target)
    {
        try
        {
            var holePuncher = new UdpHolePunching();
            return await holePuncher.ConnectToPeer(localNAT.LocalEndPoint, target);
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task<bool> AttemptTCPHolePunching(NATDetectionResult localNAT, IPEndPoint target)
    {
        try
        {
            var holePuncher = new TcpHolePunching();
            return await holePuncher.ConnectToPeer(localNAT.LocalEndPoint, target);
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task<bool> AttemptTURNRelay(IPEndPoint target)
    {
        try
        {
            // TURN 릴레이 구현
            await Task.Delay(100); // 시뮬레이션
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task<bool> AttemptHTTPTunneling(IPEndPoint target)
    {
        try
        {
            // HTTP 터널링 구현
            await Task.Delay(100); // 시뮬레이션
            return false; // 일반적으로 게임에는 부적합
        }
        catch
        {
            return false;
        }
    }
}

public enum ConnectionMethod
{
    DirectUDP,
    UDPHolePunching,
    TCPHolePunching,
    TURNRelay,
    HTTPTunneling
}

// 사용 예제
public class NATTraversalExample
{
    public static async Task Main(string[] args)
    {
        // NAT 타입 탐지
        var stunServers = new[] { "stun.l.google.com", "stun1.l.google.com" };
        var detector = new NATTypeDetector(stunServers);
        
        var localNAT = await detector.DetectNATTypeAsync();
        Console.WriteLine($"로컬 NAT: {localNAT.NATType}");
        Console.WriteLine($"공인 주소: {localNAT.PublicEndPoint}");
        Console.WriteLine($"권장 전략: {localNAT.GetRecommendedStrategy()}");
        Console.WriteLine($"홀펀칭 성공률: {localNAT.GetHolePunchingSuccessRate():P1}");
        
        // 실제 P2P 연결에서는 시그널링 서버를 통해 상대방 정보를 얻음
        var remotePublicEndPoint = new IPEndPoint(IPAddress.Parse("203.0.113.100"), 12345);
        var remoteNAT = new NATDetectionResult { NATType = NATType.RestrictedCone };
        
        // NAT 순회 시도
        var success = await NATTraversalStrategy.AttemptConnection(localNAT, remoteNAT, remotePublicEndPoint);
        
        if (success)
        {
            Console.WriteLine("P2P 연결 성공!");
        }
        else
        {
            Console.WriteLine("P2P 연결 실패, 서버 기반 통신 필요");
        }
    }
}
```

이제 3장에서 STUN 프로토콜의 심화 내용을 다뤘습니다. STUN 메시지 구조부터 실제 서버 구현, 클라이언트 개발, 그리고 NAT 타입 탐지까지 포괄적으로 살펴봤습니다. 다음 장에서는 TURN 프로토콜과 릴레이 서버 구현에 대해 알아보겠습니다.
  
  

# 4장: TURN 프로토콜과 릴레이 서버
  
## 4.1 TURN의 개념과 STUN과의 차이점

### 4.1.1 TURN 프로토콜 개요
TURN(Traversal Using Relays around NAT)은 RFC 5766에 정의된 프로토콜로, NAT 순회가 불가능한 경우 릴레이 서버를 통해 통신할 수 있게 해주는 프로토콜입니다. STUN이 직접 P2P 연결을 시도하는 반면, TURN은 중계 서버를 통한 간접 연결을 제공합니다.

```csharp
public enum TURNMessageType : ushort
{
    // STUN 메시지 타입들 (상속)
    BindingRequest = 0x0001,
    BindingResponse = 0x0101,
    BindingErrorResponse = 0x0111,
    
    // TURN 전용 메시지 타입들
    AllocateRequest = 0x0003,
    AllocateResponse = 0x0103,
    AllocateErrorResponse = 0x0113,
    
    RefreshRequest = 0x0004,
    RefreshResponse = 0x0104,
    RefreshErrorResponse = 0x0114,
    
    SendIndication = 0x0016,
    DataIndication = 0x0017,
    
    CreatePermissionRequest = 0x0008,
    CreatePermissionResponse = 0x0108,
    CreatePermissionErrorResponse = 0x0118,
    
    ChannelBindRequest = 0x0009,
    ChannelBindResponse = 0x0109,
    ChannelBindErrorResponse = 0x0119
}

public enum TURNAttributeType : ushort
{
    // STUN 어트리뷰트들 (상속)
    MappedAddress = 0x0001,
    Username = 0x0006,
    MessageIntegrity = 0x0008,
    ErrorCode = 0x0009,
    Realm = 0x0014,
    Nonce = 0x0015,
    XorMappedAddress = 0x0020,
    
    // TURN 전용 어트리뷰트들
    ChannelNumber = 0x000C,
    Lifetime = 0x000D,
    XorPeerAddress = 0x0012,
    Data = 0x0013,
    XorRelayedAddress = 0x0016,
    EvenPort = 0x0018,
    RequestedTransport = 0x0019,
    DontFragment = 0x001A,
    ReservationToken = 0x0022,
    Software = 0x8022,
    AlternateServer = 0x8023
}
```

### 4.1.2 TURN vs STUN 비교
TURN과 STUN의 주요 차이점을 코드로 비교해보겠습니다:

```csharp
public abstract class NATTraversalMethod
{
    public abstract Task<bool> EstablishConnection(IPEndPoint localEndPoint, IPEndPoint peerEndPoint);
    public abstract Task<bool> SendData(byte[] data, IPEndPoint target);
    public abstract bool RequiresServer { get; }
    public abstract double GetCostFactor(); // 상대적 비용 (1.0 기준)
}

public class STUNMethod : NATTraversalMethod
{
    private UdpClient _udpClient;
    
    public override bool RequiresServer => false; // P2P 직접 연결
    
    public override double GetCostFactor() => 1.0; // 기준
    
    public override async Task<bool> EstablishConnection(IPEndPoint localEndPoint, IPEndPoint peerEndPoint)
    {
        Console.WriteLine("STUN: 직접 P2P 연결 시도");
        
        _udpClient = new UdpClient(localEndPoint);
        
        // 홀펀칭 시도
        var success = await PerformHolePunching(peerEndPoint);
        
        if (success)
        {
            Console.WriteLine("STUN: P2P 연결 성공 - 낮은 지연시간, 높은 대역폭");
            return true;
        }
        
        Console.WriteLine("STUN: P2P 연결 실패 - NAT 타입 비호환");
        return false;
    }
    
    public override async Task<bool> SendData(byte[] data, IPEndPoint target)
    {
        if (_udpClient != null)
        {
            await _udpClient.SendAsync(data, data.Length, target);
            return true;
        }
        return false;
    }
    
    private async Task<bool> PerformHolePunching(IPEndPoint target)
    {
        // 홀펀칭 로직 (이전 장에서 구현)
        return true; // 시뮬레이션
    }
}

public class TURNMethod : NATTraversalMethod
{
    private TURNClient _turnClient;
    private IPEndPoint _relayedAddress;
    
    public override bool RequiresServer => true; // 릴레이 서버 필요
    
    public override double GetCostFactor() => 2.5; // STUN 대비 2.5배 비용
    
    public override async Task<bool> EstablishConnection(IPEndPoint localEndPoint, IPEndPoint peerEndPoint)
    {
        Console.WriteLine("TURN: 릴레이 서버를 통한 연결 시도");
        
        _turnClient = new TURNClient("turn.example.com", "username", "password");
        
        // TURN 서버에 릴레이 주소 할당
        _relayedAddress = await _turnClient.AllocateRelayAsync();
        
        if (_relayedAddress != null)
        {
            // 피어에 대한 권한 생성
            await _turnClient.CreatePermissionAsync(peerEndPoint.Address);
            
            Console.WriteLine($"TURN: 릴레이 연결 성공 - 릴레이 주소: {_relayedAddress}");
            Console.WriteLine("TURN: 안정적 연결, 약간의 지연시간 증가");
            return true;
        }
        
        Console.WriteLine("TURN: 릴레이 연결 실패");
        return false;
    }
    
    public override async Task<bool> SendData(byte[] data, IPEndPoint target)
    {
        if (_turnClient != null)
        {
            await _turnClient.SendDataAsync(data, target);
            return true;
        }
        return false;
    }
}

// 연결 방법 선택 전략
public class ConnectionStrategy
{
    public static async Task<NATTraversalMethod> SelectOptimalMethod(
        NATType localNAT, NATType remoteNAT, bool costSensitive = false)
    {
        var methods = new List<(NATTraversalMethod method, double successRate)>
        {
            (new STUNMethod(), GetSTUNSuccessRate(localNAT, remoteNAT)),
            (new TURNMethod(), 0.99) // TURN은 거의 항상 성공
        };
        
        if (costSensitive)
        {
            // 비용 고려하여 정렬
            methods = methods.OrderBy(m => m.method.GetCostFactor() / m.successRate).ToList();
        }
        else
        {
            // 성공률 우선 정렬
            methods = methods.OrderByDescending(m => m.successRate).ToList();
        }
        
        foreach (var (method, rate) in methods)
        {
            Console.WriteLine($"시도: {method.GetType().Name} (성공률: {rate:P1}, 비용: {method.GetCostFactor():F1}x)");
            
            // 실제로는 테스트 연결을 시도해야 함
            if (rate > 0.8) // 80% 이상 성공률이면 시도
            {
                return method;
            }
        }
        
        return new TURNMethod(); // 최후의 수단
    }
    
    private static double GetSTUNSuccessRate(NATType local, NATType remote)
    {
        return (local, remote) switch
        {
            (NATType.FullCone, _) or (_, NATType.FullCone) => 0.95,
            (NATType.RestrictedCone, NATType.RestrictedCone) => 0.90,
            (NATType.PortRestricted, NATType.PortRestricted) => 0.85,
            (NATType.Symmetric, _) or (_, NATType.Symmetric) => 0.60,
            _ => 0.70
        };
    }
}
```

### 4.1.3 TURN 작동 시나리오

TURN의 전체적인 작동 과정을 보여주는 예제입니다:

```csharp
public class TURNConnectionFlow
{
    public static async Task DemonstrateFlow()
    {
        Console.WriteLine("=== TURN 연결 흐름 시연 ===\n");
        
        // 1단계: 클라이언트들이 TURN 서버에 연결
        var clientA = new TURNClient("turn.gameserver.com", "playerA", "passwordA");
        var clientB = new TURNClient("turn.gameserver.com", "playerB", "passwordB");
        
        Console.WriteLine("1. 클라이언트들이 TURN 서버에 인증");
        await clientA.AuthenticateAsync();
        await clientB.AuthenticateAsync();
        
        // 2단계: 릴레이 주소 할당
        Console.WriteLine("\n2. 릴레이 주소 할당");
        var relayA = await clientA.AllocateRelayAsync();
        var relayB = await clientB.AllocateRelayAsync();
        
        Console.WriteLine($"   클라이언트 A 릴레이: {relayA}");
        Console.WriteLine($"   클라이언트 B 릴레이: {relayB}");
        
        // 3단계: 피어 권한 설정
        Console.WriteLine("\n3. 피어 간 권한 설정");
        await clientA.CreatePermissionAsync(relayB.Address);
        await clientB.CreatePermissionAsync(relayA.Address);
        
        // 4단계: 채널 바인딩 (선택사항, 성능 향상)
        Console.WriteLine("\n4. 채널 바인딩으로 성능 최적화");
        var channelA = await clientA.BindChannelAsync(relayB);
        var channelB = await clientB.BindChannelAsync(relayA);
        
        // 5단계: 게임 데이터 교환
        Console.WriteLine("\n5. 게임 데이터 교환 시작");
        await SimulateGameDataExchange(clientA, clientB, channelA, channelB);
        
        Console.WriteLine("\n=== TURN 연결 완료 ===");
    }
    
    private static async Task SimulateGameDataExchange(
        TURNClient clientA, TURNClient clientB, 
        ushort channelA, ushort channelB)
    {
        // 플레이어 A의 이동 데이터
        var playerAMove = new PlayerMoveData
        {
            PlayerId = "A",
            Position = new Vector3(10, 0, 5),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        // 플레이어 B의 이동 데이터
        var playerBMove = new PlayerMoveData
        {
            PlayerId = "B", 
            Position = new Vector3(-5, 0, 10),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        // 채널을 통한 고속 데이터 전송
        await clientA.SendChannelDataAsync(channelA, SerializeMove(playerAMove));
        await clientB.SendChannelDataAsync(channelB, SerializeMove(playerBMove));
        
        Console.WriteLine($"   A -> B: {playerAMove.Position} (채널 {channelA})");
        Console.WriteLine($"   B -> A: {playerBMove.Position} (채널 {channelB})");
    }
    
    private static byte[] SerializeMove(PlayerMoveData move)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        writer.Write(move.PlayerId);
        writer.Write(move.Position.X);
        writer.Write(move.Position.Y);
        writer.Write(move.Position.Z);
        writer.Write(move.Timestamp);
        
        return ms.ToArray();
    }
}

public class PlayerMoveData
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public long Timestamp { get; set; }
}
```
  

## 4.2 TURN 서버 구축 및 설정

### 4.2.1 기본 TURN 서버 구현
TURN 서버는 클라이언트들 간의 트래픽을 중계하는 역할을 합니다:

```csharp
public class TURNServer
{
    private readonly Dictionary<string, TURNAllocation> _allocations = new();
    private readonly Dictionary<IPEndPoint, TURNClient> _clients = new();
    private readonly object _lock = new object();
    private UdpClient _udpServer;
    private TcpListener _tcpServer;
    private bool _isRunning;
    private readonly TURNServerConfig _config;
    
    public class TURNServerConfig
    {
        public IPEndPoint UdpEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 3478);
        public IPEndPoint TcpEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 3478);
        public IPEndPoint RelayStartPoint { get; set; } = new IPEndPoint(IPAddress.Any, 49152);
        public int MaxAllocations { get; set; } = 1000;
        public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromMinutes(10);
        public long MaxBandwidthPerClient { get; set; } = 1024 * 1024; // 1 MB/s
        public string Realm { get; set; } = "turn.gameserver.com";
        public Dictionary<string, string> Users { get; set; } = new();
    }
    
    public class TURNAllocation
    {
        public string AllocationId { get; set; }
        public IPEndPoint ClientEndPoint { get; set; }
        public IPEndPoint RelayedAddress { get; set; }
        public UdpClient RelaySocket { get; set; }
        public DateTime ExpiryTime { get; set; }
        public HashSet<IPAddress> Permissions { get; set; } = new();
        public Dictionary<ushort, IPEndPoint> Channels { get; set; } = new();
        public BandwidthLimiter BandwidthLimiter { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }
    
    public TURNServer(TURNServerConfig config = null)
    {
        _config = config ?? new TURNServerConfig();
        
        // 기본 사용자 추가
        if (!_config.Users.Any())
        {
            _config.Users["testuser"] = "testpass";
            _config.Users["player1"] = "secret123";
            _config.Users["player2"] = "secret456";
        }
    }
    
    public async Task StartAsync()
    {
        _udpServer = new UdpClient(_config.UdpEndPoint);
        _tcpServer = new TcpListener(_config.TcpEndPoint);
        _tcpServer.Start();
        
        _isRunning = true;
        
        Console.WriteLine($"TURN 서버 시작됨:");
        Console.WriteLine($"  UDP: {_config.UdpEndPoint}");
        Console.WriteLine($"  TCP: {_config.TcpEndPoint}");
        Console.WriteLine($"  릴레이 범위: {_config.RelayStartPoint}+");
        Console.WriteLine($"  최대 할당: {_config.MaxAllocations}");
        
        var tasks = new[]
        {
            HandleUdpRequests(),
            HandleTcpRequests(),
            CleanupExpiredAllocations()
        };
        
        await Task.WhenAll(tasks);
    }
    
    private async Task HandleUdpRequests()
    {
        while (_isRunning)
        {
            try
            {
                var result = await _udpServer.ReceiveAsync();
                _ = Task.Run(() => ProcessUdpMessage(result));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP 수신 오류: {ex.Message}");
            }
        }
    }
    
    private async Task ProcessUdpMessage(UdpReceiveResult result)
    {
        try
        {
            // 채널 데이터인지 확인 (첫 2바이트가 채널 번호)
            if (result.Buffer.Length >= 4 && 
                (result.Buffer[0] & 0xC0) == 0x40) // 채널 데이터 마커
            {
                await ProcessChannelData(result);
                return;
            }
            
            // STUN/TURN 메시지 처리
            var message = STUNMessage.Parse(result.Buffer);
            await ProcessTurnMessage(message, result.RemoteEndPoint, TransportType.UDP);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP 메시지 처리 오류: {ex.Message}");
        }
    }
    
    private async Task ProcessChannelData(UdpReceiveResult result)
    {
        var buffer = result.Buffer;
        var channelNumber = (ushort)((buffer[0] << 8) | buffer[1]);
        var length = (ushort)((buffer[2] << 8) | buffer[3]);
        
        if (buffer.Length < 4 + length)
            return;
        
        var data = new byte[length];
        Array.Copy(buffer, 4, data, 0, length);
        
        // 채널을 통한 데이터 중계
        await RelayChannelData(channelNumber, data, result.RemoteEndPoint);
    }
    
    private async Task ProcessTurnMessage(STUNMessage message, IPEndPoint clientEndPoint, TransportType transport)
    {
        switch (message.MessageType)
        {
            case TURNMessageType.AllocateRequest:
                await ProcessAllocateRequest(message, clientEndPoint, transport);
                break;
                
            case TURNMessageType.RefreshRequest:
                await ProcessRefreshRequest(message, clientEndPoint);
                break;
                
            case TURNMessageType.CreatePermissionRequest:
                await ProcessCreatePermissionRequest(message, clientEndPoint);
                break;
                
            case TURNMessageType.ChannelBindRequest:
                await ProcessChannelBindRequest(message, clientEndPoint);
                break;
                
            case TURNMessageType.SendIndication:
                await ProcessSendIndication(message, clientEndPoint);
                break;
                
            default:
                await SendErrorResponse(message, clientEndPoint, 400, "지원하지 않는 메시지 타입");
                break;
        }
    }
    
    private async Task ProcessAllocateRequest(STUNMessage request, IPEndPoint clientEndPoint, TransportType transport)
    {
        // 인증 확인
        if (!await AuthenticateRequest(request, clientEndPoint))
        {
            await SendErrorResponse(request, clientEndPoint, 401, "인증 필요");
            return;
        }
        
        // 기존 할당 확인
        var existingAllocation = GetAllocationByClient(clientEndPoint);
        if (existingAllocation != null)
        {
            await SendErrorResponse(request, clientEndPoint, 437, "할당이 이미 존재함");
            return;
        }
        
        // 새 릴레이 주소 할당
        var relayAddress = await AllocateRelayAddress();
        if (relayAddress == null)
        {
            await SendErrorResponse(request, clientEndPoint, 508, "서버 용량 부족");
            return;
        }
        
        // 할당 생성
        var allocation = new TURNAllocation
        {
            AllocationId = Guid.NewGuid().ToString(),
            ClientEndPoint = clientEndPoint,
            RelayedAddress = relayAddress,
            RelaySocket = new UdpClient(relayAddress),
            ExpiryTime = DateTime.UtcNow.Add(_config.DefaultLifetime),
            BandwidthLimiter = new BandwidthLimiter(_config.MaxBandwidthPerClient)
        };
        
        lock (_lock)
        {
            _allocations[allocation.AllocationId] = allocation;
        }
        
        // 릴레이 소켓에서 데이터 수신 대기
        _ = Task.Run(() => HandleRelayData(allocation));
        
        // 성공 응답 전송
        await SendAllocateSuccessResponse(request, clientEndPoint, allocation);
        
        Console.WriteLine($"할당 생성: {clientEndPoint} -> {relayAddress}");
    }
    
    private async Task<IPEndPoint> AllocateRelayAddress()
    {
        lock (_lock)
        {
            if (_allocations.Count >= _config.MaxAllocations)
                return null;
        }
        
        // 사용 가능한 포트 찾기
        for (int port = _config.RelayStartPoint.Port; port < 65536; port++)
        {
            try
            {
                var candidate = new IPEndPoint(_config.RelayStartPoint.Address, port);
                
                // 이미 사용 중인지 확인
                lock (_lock)
                {
                    if (_allocations.Values.Any(a => a.RelayedAddress.Port == port))
                        continue;
                }
                
                // 포트 사용 가능성 테스트
                using var testSocket = new UdpClient(candidate);
                return candidate;
            }
            catch
            {
                continue;
            }
        }
        
        return null;
    }
    
    private async Task HandleRelayData(TURNAllocation allocation)
    {
        while (_isRunning && DateTime.UtcNow < allocation.ExpiryTime)
        {
            try
            {
                var result = await allocation.RelaySocket.ReceiveAsync();
                
                // 권한 확인
                if (!allocation.Permissions.Contains(result.RemoteEndPoint.Address))
                {
                    Console.WriteLine($"권한 없는 주소에서 데이터 수신: {result.RemoteEndPoint}");
                    continue;
                }
                
                // 대역폭 제한 확인
                if (!await allocation.BandwidthLimiter.CanSend(result.Buffer.Length))
                {
                    Console.WriteLine($"대역폭 제한으로 데이터 드롭: {allocation.ClientEndPoint}");
                    continue;
                }
                
                // 클라이언트에게 데이터 전달
                await SendDataIndication(allocation.ClientEndPoint, result.RemoteEndPoint, result.Buffer);
                
                allocation.BytesReceived += result.Buffer.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"릴레이 데이터 처리 오류: {ex.Message}");
                break;
            }
        }
        
        // 할당 정리
        lock (_lock)
        {
            _allocations.Remove(allocation.AllocationId);
        }
        
        allocation.RelaySocket?.Close();
        Console.WriteLine($"할당 만료: {allocation.RelayedAddress}");
    }
    
    private async Task SendDataIndication(IPEndPoint clientEndPoint, IPEndPoint peerEndPoint, byte[] data)
    {
        var indication = new STUNMessage
        {
            MessageType = TURNMessageType.DataIndication
        };
        
        indication.Attributes.Add(new XorPeerAddressAttribute
        {
            Type = TURNAttributeType.XorPeerAddress,
            PeerEndPoint = peerEndPoint
        });
        
        indication.Attributes.Add(new DataAttribute
        {
            Type = TURNAttributeType.Data,
            Data = data
        });
        
        var responseData = indication.ToByteArray();
        await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
    }
    
    private async Task<bool> AuthenticateRequest(STUNMessage request, IPEndPoint clientEndPoint)
    {
        var usernameAttr = request.Attributes.OfType<UsernameAttribute>().FirstOrDefault();
        var integrityAttr = request.Attributes.OfType<MessageIntegrityAttribute>().FirstOrDefault();
        
        if (usernameAttr == null || integrityAttr == null)
            return false;
        
        if (!_config.Users.TryGetValue(usernameAttr.Username, out var password))
            return false;
        
        // HMAC-SHA1 검증 (실제 구현에서는 proper HMAC 계산 필요)
        return true; // 시뮬레이션
    }
    
    private TURNAllocation GetAllocationByClient(IPEndPoint clientEndPoint)
    {
        lock (_lock)
        {
            return _allocations.Values.FirstOrDefault(a => a.ClientEndPoint.Equals(clientEndPoint));
        }
    }
    
    private async Task SendAllocateSuccessResponse(STUNMessage request, IPEndPoint clientEndPoint, TURNAllocation allocation)
    {
        var response = new STUNMessage
        {
            MessageType = TURNMessageType.AllocateResponse,
            TransactionId = request.TransactionId
        };
        
        response.Attributes.Add(new XorRelayedAddressAttribute
        {
            Type = TURNAttributeType.XorRelayedAddress,
            RelayedAddress = allocation.RelayedAddress
        });
        
        response.Attributes.Add(new LifetimeAttribute
        {
            Type = TURNAttributeType.Lifetime,
            Lifetime = (uint)_config.DefaultLifetime.TotalSeconds
        });
        
        var responseData = response.ToByteArray();
        await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
    }
    
    private async Task SendErrorResponse(STUNMessage request, IPEndPoint clientEndPoint, int errorCode, string reason)
    {
        var response = new STUNMessage
        {
            MessageType = TURNMessageType.AllocateErrorResponse,
            TransactionId = request?.TransactionId ?? new byte[12]
        };
        
        response.Attributes.Add(new ErrorCodeAttribute
        {
            Type = TURNAttributeType.ErrorCode,
            ErrorCode = errorCode,
            ReasonPhrase = reason
        });
        
        var responseData = response.ToByteArray();
        await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
    }
    
    private async Task CleanupExpiredAllocations()
    {
        while (_isRunning)
        {
            var now = DateTime.UtcNow;
            var expiredAllocations = new List<TURNAllocation>();
            
            lock (_lock)
            {
                var expired = _allocations.Values.Where(a => a.ExpiryTime <= now).ToList();
                foreach (var allocation in expired)
                {
                    _allocations.Remove(allocation.AllocationId);
                    expiredAllocations.Add(allocation);
                }
            }
            
            foreach (var allocation in expiredAllocations)
            {
                allocation.RelaySocket?.Close();
                Console.WriteLine($"만료된 할당 정리: {allocation.RelayedAddress}");
            }
            
            await Task.Delay(60000); // 1분마다 정리
        }
    }
    
    public void Stop()
    {
        _isRunning = false;
        _udpServer?.Close();
        _tcpServer?.Stop();
        
        lock (_lock)
        {
            foreach (var allocation in _allocations.Values)
            {
                allocation.RelaySocket?.Close();
            }
            _allocations.Clear();
        }
        
        Console.WriteLine("TURN 서버가 중지되었습니다.");
    }
}

public enum TransportType
{
    UDP,
    TCP
}
```

### 4.2.2 TURN 전용 어트리뷰트 구현
TURN 프로토콜에서 사용하는 특별한 어트리뷰트들을 구현합니다:

```csharp
public class XorRelayedAddressAttribute : STUNAttribute
{
    public IPEndPoint RelayedAddress { get; set; }
    
    public override ushort Length => 8;
    
    public override byte[] Value
    {
        get
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write((byte)0); // Reserved
            writer.Write((byte)0x01); // IPv4
            
            // 포트 XOR
            var xorPort = RelayedAddress.Port ^ (STUNMessage.MAGIC_COOKIE >> 16);
            writer.Write(IPAddress.HostToNetworkOrder((short)xorPort));
            
            // IP 주소 XOR
            var ipBytes = RelayedAddress.Address.GetAddressBytes();
            var magicBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)STUNMessage.MAGIC_COOKIE));
            
            for (int i = 0; i < 4; i++)
            {
                writer.Write((byte)(ipBytes[i] ^ magicBytes[i]));
            }
            
            return ms.ToArray();
        }
    }
}

public class XorPeerAddressAttribute : STUNAttribute
{
    public IPEndPoint PeerEndPoint { get; set; }
    
    public override ushort Length => 8;
    
    public override byte[] Value
    {
        get
        {
            // XorRelayedAddressAttribute와 동일한 구조
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            writer.Write((byte)0);
            writer.Write((byte)0x01);
            
            var xorPort = PeerEndPoint.Port ^ (STUNMessage.MAGIC_COOKIE >> 16);
            writer.Write(IPAddress.HostToNetworkOrder((short)xorPort));
            
            var ipBytes = PeerEndPoint.Address.GetAddressBytes();
            var magicBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)STUNMessage.MAGIC_COOKIE));
            
            for (int i = 0; i < 4; i++)
            {
                writer.Write((byte)(ipBytes[i] ^ magicBytes[i]));
            }
            
            return ms.ToArray();
        }
    }
}

public class DataAttribute : STUNAttribute
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    
    public override ushort Length => (ushort)Data.Length;
    
    public override byte[] Value => Data;
    
    public static DataAttribute FromBytes(byte[] data)
    {
        return new DataAttribute
        {
            Type = TURNAttributeType.Data,
            Data = data
        };
    }
}

public class LifetimeAttribute : STUNAttribute
{
    public uint Lifetime { get; set; } // 초 단위
    
    public override ushort Length => 4;
    
    public override byte[] Value => BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Lifetime));
    
    public static LifetimeAttribute FromBytes(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("Lifetime 어트리뷰트 데이터가 부족합니다.");
        
        var lifetime = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
        
        return new LifetimeAttribute
        {
            Type = TURNAttributeType.Lifetime,
            Lifetime = (uint)lifetime
        };
    }
}

public class RequestedTransportAttribute : STUNAttribute
{
    public byte Protocol { get; set; } = 17; // UDP = 17, TCP = 6
    
    public override ushort Length => 4;
    
    public override byte[] Value
    {
        get
        {
            var data = new byte[4];
            data[0] = Protocol;
            // 나머지 3바이트는 Reserved (0)
            return data;
        }
    }
    
    public static RequestedTransportAttribute FromBytes(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("RequestedTransport 어트리뷰트 데이터가 부족합니다.");
        
        return new RequestedTransportAttribute
        {
            Type = TURNAttributeType.RequestedTransport,
            Protocol = data[0]
        };
    }
}

public class ChannelNumberAttribute : STUNAttribute
{
    public ushort ChannelNumber { get; set; }
    
    public override ushort Length => 4;
    
    public override byte[] Value
    {
        get
        {
            var data = new byte[4];
            var channelBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)ChannelNumber));
            Array.Copy(channelBytes, data, 2);
            // 나머지 2바이트는 Reserved (0)
            return data;
        }
    }
    
    public static ChannelNumberAttribute FromBytes(byte[] data)
    {
        if (data.Length < 4)
            throw new ArgumentException("ChannelNumber 어트리뷰트 데이터가 부족합니다.");
        
        var channelNumber = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
        
        return new ChannelNumberAttribute
        {
            Type = TURNAttributeType.ChannelNumber,
            ChannelNumber = (ushort)channelNumber
        };
    }
}

public class UsernameAttribute : STUNAttribute
{
    public string Username { get; set; } = "";
    
    public override ushort Length => (ushort)System.Text.Encoding.UTF8.GetByteCount(Username);
    
    public override byte[] Value => System.Text.Encoding.UTF8.GetBytes(Username);
    
    public static UsernameAttribute FromBytes(byte[] data)
    {
        return new UsernameAttribute
        {
            Type = TURNAttributeType.Username,
            Username = System.Text.Encoding.UTF8.GetString(data)
        };
    }
}

public class MessageIntegrityAttribute : STUNAttribute
{
    public byte[] HMAC { get; set; } = new byte[20]; // SHA-1 HMAC
    
    public override ushort Length => 20;
    
    public override byte[] Value => HMAC;
    
    public static MessageIntegrityAttribute FromBytes(byte[] data)
    {
        if (data.Length != 20)
            throw new ArgumentException("MessageIntegrity는 정확히 20바이트여야 합니다.");
        
        return new MessageIntegrityAttribute
        {
            Type = TURNAttributeType.MessageIntegrity,
            HMAC = data
        };
    }
}
```
  

## 4.3 C#에서 TURN 클라이언트 구현

### 4.3.1 기본 TURN 클라이언트
TURN 클라이언트는 서버와 통신하여 릴레이 기능을 사용합니다:

```csharp
public class TURNClient : IDisposable
{
    private UdpClient _udpClient;
    private readonly string _serverHost;
    private readonly int _serverPort;
    private readonly string _username;
    private readonly string _password;
    private IPEndPoint _serverEndPoint;
    private IPEndPoint _relayedAddress;
    private readonly Dictionary<ushort, IPEndPoint> _channels = new();
    private readonly Random _random = new();
    private bool _isAllocated;
    private Timer _refreshTimer;
    
    public IPEndPoint RelayedAddress => _relayedAddress;
    public bool IsAllocated => _isAllocated;
    
    public TURNClient(string serverHost, string username, string password, int serverPort = 3478)
    {
        _serverHost = serverHost;
        _serverPort = serverPort;
        _username = username;
        _password = password;
    }
    
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverHost), _serverPort);
            _udpClient = new UdpClient();
            
            Console.WriteLine($"TURN 서버에 인증 중: {_serverEndPoint}");
            
            // 첫 번째 할당 요청 (인증 없이)
            var request = CreateAllocateRequest();
            await SendRequest(request);
            
            // 401 Unauthorized 응답 대기
            var response = await ReceiveResponse(request.TransactionId);
            
            if (response?.MessageType == TURNMessageType.AllocateErrorResponse)
            {
                var errorAttr = response.Attributes.OfType<ErrorCodeAttribute>().FirstOrDefault();
                if (errorAttr?.ErrorCode == 401)
                {
                    Console.WriteLine("인증 필요 - 자격 증명으로 재시도");
                    return true; // 인증 프로세스 시작
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"인증 오류: {ex.Message}");
            return false;
        }
    }
    
    public async Task<IPEndPoint> AllocateRelayAsync()
    {
        if (_udpClient == null)
        {
            throw new InvalidOperationException("먼저 AuthenticateAsync()를 호출하세요.");
        }
        
        try
        {
            Console.WriteLine("릴레이 주소 할당 요청...");
            
            var request = CreateAuthenticatedAllocateRequest();
            await SendRequest(request);
            
            var response = await ReceiveResponse(request.TransactionId);
            
            if (response?.MessageType == TURNMessageType.AllocateResponse)
            {
                var relayAttr = response.Attributes.OfType<XorRelayedAddressAttribute>().FirstOrDefault();
                var lifetimeAttr = response.Attributes.OfType<LifetimeAttribute>().FirstOrDefault();
                
                if (relayAttr != null)
                {
                    _relayedAddress = relayAttr.RelayedAddress;
                    _isAllocated = true;
                    
                    // 주기적 갱신 타이머 설정
                    var lifetime = lifetimeAttr?.Lifetime ?? 600; // 기본 10분
                    var refreshInterval = TimeSpan.FromSeconds(lifetime * 0.8); // 80% 지점에서 갱신
                    _refreshTimer = new Timer(RefreshAllocation, null, refreshInterval, refreshInterval);
                    
                    Console.WriteLine($"릴레이 할당 성공: {_relayedAddress} (수명: {lifetime}초)");
                    return _relayedAddress;
                }
            }
            else if (response?.MessageType == TURNMessageType.AllocateErrorResponse)
            {
                var errorAttr = response.Attributes.OfType<ErrorCodeAttribute>().FirstOrDefault();
                Console.WriteLine($"할당 실패: {errorAttr?.ErrorCode} - {errorAttr?.ReasonPhrase}");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"할당 오류: {ex.Message}");
            return null;
        }
    }
    
    public async Task<bool> CreatePermissionAsync(IPAddress peerAddress)
    {
        if (!_isAllocated)
        {
            throw new InvalidOperationException("릴레이가 할당되지 않았습니다.");
        }
        
        try
        {
            Console.WriteLine($"권한 생성: {peerAddress}");
            
            var request = new STUNMessage
            {
                MessageType = TURNMessageType.CreatePermissionRequest
            };
            
            request.Attributes.Add(new XorPeerAddressAttribute
            {
                Type = TURNAttributeType.XorPeerAddress,
                PeerEndPoint = new IPEndPoint(peerAddress, 0) // 포트는 0으로 설정
            });
            
            AddAuthenticationAttributes(request);
            
            await SendRequest(request);
            var response = await ReceiveResponse(request.TransactionId);
            
            if (response?.MessageType == TURNMessageType.CreatePermissionResponse)
            {
                Console.WriteLine($"권한 생성 성공: {peerAddress}");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"권한 생성 오류: {ex.Message}");
            return false;
        }
    }
    
    public async Task<ushort> BindChannelAsync(IPEndPoint peerEndPoint)
    {
        if (!_isAllocated)
        {
            throw new InvalidOperationException("릴레이가 할당되지 않았습니다.");
        }
        
        try
        {
            // 사용 가능한 채널 번호 생성 (0x4000-0x7FFF 범위)
            ushort channelNumber;
            do
            {
                channelNumber = (ushort)_random.Next(0x4000, 0x8000);
            } while (_channels.ContainsKey(channelNumber));
            
            Console.WriteLine($"채널 바인딩: {peerEndPoint} -> 채널 {channelNumber:X4}");
            
            var request = new STUNMessage
            {
                MessageType = TURNMessageType.ChannelBindRequest
            };
            
            request.Attributes.Add(new ChannelNumberAttribute
            {
                Type = TURNAttributeType.ChannelNumber,
                ChannelNumber = channelNumber
            });
            
            request.Attributes.Add(new XorPeerAddressAttribute
            {
                Type = TURNAttributeType.XorPeerAddress,
                PeerEndPoint = peerEndPoint
            });
            
            AddAuthenticationAttributes(request);
            
            await SendRequest(request);
            var response = await ReceiveResponse(request.TransactionId);
            
            if (response?.MessageType == TURNMessageType.ChannelBindResponse)
            {
                _channels[channelNumber] = peerEndPoint;
                Console.WriteLine($"채널 바인딩 성공: 채널 {channelNumber:X4}");
                return channelNumber;
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"채널 바인딩 오류: {ex.Message}");
            return 0;
        }
    }
    
    public async Task SendDataAsync(byte[] data, IPEndPoint targetEndPoint)
    {
        if (!_isAllocated)
        {
            throw new InvalidOperationException("릴레이가 할당되지 않았습니다.");
        }
        
        try
        {
            var indication = new STUNMessage
            {
                MessageType = TURNMessageType.SendIndication
            };
            
            indication.Attributes.Add(new XorPeerAddressAttribute
            {
                Type = TURNAttributeType.XorPeerAddress,
                PeerEndPoint = targetEndPoint
            });
            
            indication.Attributes.Add(new DataAttribute
            {
                Type = TURNAttributeType.Data,
                Data = data
            });
            
            await SendRequest(indication);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"데이터 전송 오류: {ex.Message}");
        }
    }
    
    public async Task SendChannelDataAsync(ushort channelNumber, byte[] data)
    {
        if (!_isAllocated)
        {
            throw new InvalidOperationException("릴레이가 할당되지 않았습니다.");
        }
        
        if (!_channels.ContainsKey(channelNumber))
        {
            throw new ArgumentException("바인딩되지 않은 채널입니다.");
        }
        
        try
        {
            // 채널 데이터 형식: [Channel Number (2)] [Length (2)] [Data (variable)]
            var packet = new byte[4 + data.Length];
            
            // 채널 번호
            packet[0] = (byte)(channelNumber >> 8);
            packet[1] = (byte)(channelNumber & 0xFF);
            
            // 데이터 길이
            packet[2] = (byte)(data.Length >> 8);
            packet[3] = (byte)(data.Length & 0xFF);
            
            // 데이터
            Array.Copy(data, 0, packet, 4, data.Length);
            
            await _udpClient.SendAsync(packet, packet.Length, _serverEndPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"채널 데이터 전송 오류: {ex.Message}");
        }
    }
    
    private STUNMessage CreateAllocateRequest()
    {
        var request = new STUNMessage
        {
            MessageType = TURNMessageType.AllocateRequest
        };
        
        request.Attributes.Add(new RequestedTransportAttribute
        {
            Type = TURNAttributeType.RequestedTransport,
            Protocol = 17 // UDP
        });
        
        return request;
    }
    
    private STUNMessage CreateAuthenticatedAllocateRequest()
    {
        var request = CreateAllocateRequest();
        AddAuthenticationAttributes(request);
        return request;
    }
    
    private void AddAuthenticationAttributes(STUNMessage message)
    {
        message.Attributes.Add(new UsernameAttribute
        {
            Type = TURNAttributeType.Username,
            Username = _username
        });
        
        // 실제 구현에서는 proper HMAC-SHA1 계산 필요
        message.Attributes.Add(new MessageIntegrityAttribute
        {
            Type = TURNAttributeType.MessageIntegrity,
            HMAC = new byte[20] // 시뮬레이션
        });
    }
    
    private async Task SendRequest(STUNMessage message)
    {
        var data = message.ToByteArray();
        await _udpClient.SendAsync(data, data.Length, _serverEndPoint);
    }
    
    private async Task<STUNMessage> ReceiveResponse(byte[] expectedTransactionId, int timeout = 5000)
    {
        var timeoutTask = Task.Delay(timeout);
        
        while (true)
        {
            var receiveTask = _udpClient.ReceiveAsync();
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                return null; // 타임아웃
            }
            
            var result = await receiveTask;
            
            try
            {
                var response = STUNMessage.Parse(result.Buffer);
                
                if (response.TransactionId.SequenceEqual(expectedTransactionId))
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"응답 파싱 오류: {ex.Message}");
            }
        }
    }
    
    private async void RefreshAllocation(object state)
    {
        try
        {
            Console.WriteLine("할당 갱신 중...");
            
            var request = new STUNMessage
            {
                MessageType = TURNMessageType.RefreshRequest
            };
            
            AddAuthenticationAttributes(request);
            
            await SendRequest(request);
            var response = await ReceiveResponse(request.TransactionId);
            
            if (response?.MessageType == TURNMessageType.RefreshResponse)
            {
                Console.WriteLine("할당 갱신 성공");
            }
            else
            {
                Console.WriteLine("할당 갱신 실패");
                _isAllocated = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"할당 갱신 오류: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _udpClient?.Close();
        _udpClient?.Dispose();
        _isAllocated = false;
    }
}
```

### 4.3.2 고급 TURN 클라이언트 기능

실제 게임에서 사용할 수 있는 고급 기능들을 포함한 클라이언트입니다:

```csharp
public class AdvancedTURNClient : IDisposable
{
    private readonly TURNClientPool _clientPool;
    private readonly ConnectionQualityMonitor _qualityMonitor;
    private readonly RetryPolicy _retryPolicy;
    
    public class TURNClientPool
    {
        private readonly List<TURNClient> _clients = new();
        private readonly SemaphoreSlim _semaphore;
        private int _currentIndex = 0;
        
        public TURNClientPool(int maxClients = 3)
        {
            _semaphore = new SemaphoreSlim(maxClients);
        }
        
        public async Task<TURNClient> GetClientAsync(string server, string username, string password)
        {
            await _semaphore.WaitAsync();
            
            try
            {
                // 기존 클라이언트 재사용
                var existingClient = _clients.FirstOrDefault(c => 
                    !c.IsAllocated && c.ServerHost == server);
                
                if (existingClient != null)
                {
                    return existingClient;
                }
                
                // 새 클라이언트 생성
                var newClient = new TURNClient(server, username, password);
                _clients.Add(newClient);
                
                return newClient;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public async Task<TURNClient> GetBestClientAsync()
        {
            // 라운드 로빈으로 클라이언트 선택
            var availableClients = _clients.Where(c => c.IsAllocated).ToList();
            
            if (availableClients.Any())
            {
                var client = availableClients[_currentIndex % availableClients.Count];
                _currentIndex++;
                return client;
            }
            
            return null;
        }
        
        public void Dispose()
        {
            foreach (var client in _clients)
            {
                client.Dispose();
            }
            _clients.Clear();
            _semaphore?.Dispose();
        }
    }
    
    public class ConnectionQualityMonitor
    {
        private readonly Dictionary<string, QualityMetrics> _metrics = new();
        
        public class QualityMetrics
        {
            public double AverageLatency { get; set; }
            public double PacketLoss { get; set; }
            public double Jitter { get; set; }
            public long BytesSent { get; set; }
            public long BytesReceived { get; set; }
            public DateTime LastUpdate { get; set; }
        }
        
        public async Task MonitorConnection(TURNClient client, string connectionId)
        {
            var metrics = new QualityMetrics();
            _metrics[connectionId] = metrics;
            
            while (client.IsAllocated)
            {
                var latency = await MeasureLatency(client);
                var packetLoss = await MeasurePacketLoss(client);
                
                metrics.AverageLatency = (metrics.AverageLatency + latency.TotalMilliseconds) / 2;
                metrics.PacketLoss = packetLoss;
                metrics.LastUpdate = DateTime.UtcNow;
                
                // 품질이 임계값 이하로 떨어지면 경고
                if (metrics.PacketLoss > 0.05 || metrics.AverageLatency > 200)
                {
                    Console.WriteLine($"연결 품질 저하 감지: {connectionId} " +
                                    $"(지연: {metrics.AverageLatency:F1}ms, 손실: {metrics.PacketLoss:P1})");
                }
                
                await Task.Delay(5000); // 5초마다 모니터링
            }
        }
        
        private async Task<TimeSpan> MeasureLatency(TURNClient client)
        {
            var startTime = DateTime.UtcNow;
            
            // TURN 서버에 핑 전송 (BINDING REQUEST 사용)
            try
            {
                var pingData = System.Text.Encoding.UTF8.GetBytes("PING");
                await client.SendDataAsync(pingData, client.RelayedAddress);
                
                // 실제로는 pong 응답을 기다려야 함
                await Task.Delay(50); // 시뮬레이션
                
                return DateTime.UtcNow - startTime;
            }
            catch
            {
                return TimeSpan.FromMilliseconds(1000); // 실패 시 높은 값
            }
        }
        
        private async Task<double> MeasurePacketLoss(TURNClient client)
        {
            // 패킷 손실률 측정 로직
            return 0.01; // 시뮬레이션: 1% 손실률
        }
        
        public QualityMetrics GetMetrics(string connectionId)
        {
            return _metrics.GetValueOrDefault(connectionId);
        }
    }
    
    public class RetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
        public double BackoffMultiplier { get; set; } = 2.0;
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        
        public async Task<T> ExecuteWithRetry<T>(Func<Task<T>> operation, string operationName)
        {
            var delay = InitialDelay;
            
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var result = await operation();
                    
                    if (attempt > 1)
                    {
                        Console.WriteLine($"{operationName} 성공 (시도 {attempt}/{MaxRetries})");
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{operationName} 시도 {attempt}/{MaxRetries} 실패: {ex.Message}");
                    
                    if (attempt == MaxRetries)
                    {
                        throw; // 마지막 시도에서는 예외를 재throw
                    }
                    
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(Math.Min(
                        delay.TotalMilliseconds * BackoffMultiplier, 
                        MaxDelay.TotalMilliseconds));
                }
            }
            
            return default(T);
        }
    }
    
    public AdvancedTURNClient()
    {
        _clientPool = new TURNClientPool();
        _qualityMonitor = new ConnectionQualityMonitor();
        _retryPolicy = new RetryPolicy();
    }
    
    public async Task<bool> EstablishGameConnection(string[] turnServers, string username, string password)
    {
        Console.WriteLine("고급 TURN 연결 설정 시작...");
        
        // 여러 TURN 서버에 동시 연결 시도
        var connectionTasks = turnServers.Select(server => 
            ConnectToTurnServer(server, username, password)).ToArray();
        
        try
        {
            // 가장 빠른 연결 대기
            var firstSuccessful = await Task.WhenAny(connectionTasks);
            var client = await firstSuccessful;
            
            if (client != null)
            {
                Console.WriteLine($"TURN 연결 성공: {client.ServerHost}");
                
                // 연결 품질 모니터링 시작
                _ = Task.Run(() => _qualityMonitor.MonitorConnection(client, client.ServerHost));
                
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TURN 연결 실패: {ex.Message}");
        }
        
        return false;
    }
    
    private async Task<TURNClient> ConnectToTurnServer(string server, string username, string password)
    {
        return await _retryPolicy.ExecuteWithRetry(async () =>
        {
            var client = await _clientPool.GetClientAsync(server, username, password);
            
            var authenticated = await client.AuthenticateAsync();
            if (!authenticated)
            {
                throw new Exception("TURN 인증 실패");
            }
            
            var relayAddress = await client.AllocateRelayAsync();
            if (relayAddress == null)
            {
                throw new Exception("릴레이 할당 실패");
            }
            
            return client;
        }, $"TURN 서버 연결 ({server})");
    }
    
    public async Task<bool> SendGameDataOptimized(byte[] data, IPEndPoint target)
    {
        var client = await _clientPool.GetBestClientAsync();
        if (client == null)
        {
            return false;
        }
        
        return await _retryPolicy.ExecuteWithRetry(async () =>
        {
            await client.SendDataAsync(data, target);
            return true;
        }, "게임 데이터 전송");
    }
    
    public void Dispose()
    {
        _clientPool?.Dispose();
    }
}

// 사용 예제
public class TURNClientExample
{
    public static async Task Main(string[] args)
    {
        var turnServers = new[]
        {
            "turn1.gameserver.com",
            "turn2.gameserver.com", 
            "turn3.gameserver.com"
        };
        
        using var advancedClient = new AdvancedTURNClient();
        
        var success = await advancedClient.EstablishGameConnection(
            turnServers, "gameuser", "gamepass123");
        
        if (success)
        {
            Console.WriteLine("게임 세션 준비 완료!");
            
            // 게임 데이터 전송 시뮬레이션
            for (int i = 0; i < 100; i++)
            {
                var gameData = CreateGameData(i);
                var targetEndPoint = new IPEndPoint(IPAddress.Parse("203.0.113.100"), 12345);
                
                await advancedClient.SendGameDataOptimized(gameData, targetEndPoint);
                await Task.Delay(16); // 60 FPS
            }
        }
    }
    
    private static byte[] CreateGameData(int frameNumber)
    {
        // 게임 프레임 데이터 생성
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        writer.Write(frameNumber);
        writer.Write(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        writer.Write(100.0f + frameNumber); // X 좌표
        writer.Write(50.0f); // Y 좌표
        writer.Write(200.0f + frameNumber * 0.5f); // Z 좌표
        
        return ms.ToArray();
    }
}
```
  

## 4.4 TURN 릴레이 최적화 전략

### 4.4.1 대역폭 및 연결 최적화
TURN 릴레이의 성능을 최대화하기 위한 다양한 최적화 기법들입니다:

```csharp
public class TURNOptimizationManager
{
    private readonly BandwidthOptimizer _bandwidthOptimizer;
    private readonly ConnectionPoolManager _connectionPool;
    private readonly DataCompressionManager _compressionManager;
    private readonly LoadBalancer _loadBalancer;
    
    public class BandwidthOptimizer
    {
        private readonly Dictionary<string, BandwidthTracker> _trackers = new();
        
        public class BandwidthTracker
        {
            private readonly Queue<(DateTime timestamp, int bytes)> _samples = new();
            private readonly TimeSpan _windowSize = TimeSpan.FromSeconds(10);
            
            public void RecordTransfer(int bytes)
            {
                var now = DateTime.UtcNow;
                _samples.Enqueue((now, bytes));
                
                // 오래된 샘플 제거
                while (_samples.Count > 0 && now - _samples.Peek().timestamp > _windowSize)
                {
                    _samples.Dequeue();
                }
            }
            
            public double GetCurrentBandwidth()
            {
                if (_samples.Count < 2) return 0;
                
                var totalBytes = _samples.Sum(s => s.bytes);
                var timeSpan = DateTime.UtcNow - _samples.First().timestamp;
                
                return totalBytes / timeSpan.TotalSeconds; // bytes/sec
            }
            
            public bool CanSend(int bytes, double maxBandwidth)
            {
                var currentBandwidth = GetCurrentBandwidth();
                var projectedBandwidth = (currentBandwidth * _samples.Count + bytes) / (_samples.Count + 1);
                
                return projectedBandwidth <= maxBandwidth;
            }
        }
        
        public async Task<bool> RequestBandwidth(string clientId, int bytes, double maxBandwidth)
        {
            if (!_trackers.TryGetValue(clientId, out var tracker))
            {
                tracker = new BandwidthTracker();
                _trackers[clientId] = tracker;
            }
            
            if (tracker.CanSend(bytes, maxBandwidth))
            {
                tracker.RecordTransfer(bytes);
                return true;
            }
            
            // 대역폭 초과 시 대기 또는 거부
            var currentBandwidth = tracker.GetCurrentBandwidth();
            var waitTime = (int)((bytes / maxBandwidth) * 1000); // ms
            
            if (waitTime < 1000) // 1초 미만이면 대기
            {
                await Task.Delay(waitTime);
                tracker.RecordTransfer(bytes);
                return true;
            }
            
            return false; // 대기 시간이 너무 길면 거부
        }
        
        public void OptimizeBandwidthUsage(List<(string clientId, int bytes, int priority)> requests)
        {
            // 우선순위와 대역폭 사용량을 고려한 스케줄링
            var sortedRequests = requests
                .OrderByDescending(r => r.priority)
                .ThenBy(r => r.bytes)
                .ToList();
            
            foreach (var (clientId, bytes, priority) in sortedRequests)
            {
                // 우선순위 기반 대역폭 할당 로직
                Console.WriteLine($"대역폭 할당: {clientId} - {bytes} bytes (우선순위: {priority})");
            }
        }
    }
    
    public class ConnectionPoolManager
    {
        private readonly Dictionary<string, Queue<TURNConnection>> _connectionPools = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxConnectionsPerPool;
        
        public class TURNConnection
        {
            public TURNClient Client { get; set; }
            public DateTime LastUsed { get; set; }
            public bool IsInUse { get; set; }
            public int UsageCount { get; set; }
        }
        
        public ConnectionPoolManager(int maxConnectionsPerPool = 10)
        {
            _maxConnectionsPerPool = maxConnectionsPerPool;
            _semaphore = new SemaphoreSlim(maxConnectionsPerPool * 10); // 전체 제한
        }
        
        public async Task<TURNConnection> GetConnectionAsync(string serverId)
        {
            await _semaphore.WaitAsync();
            
            try
            {
                if (!_connectionPools.TryGetValue(serverId, out var pool))
                {
                    pool = new Queue<TURNConnection>();
                    _connectionPools[serverId] = pool;
                }
                
                // 사용 가능한 연결 찾기
                while (pool.Count > 0)
                {
                    var connection = pool.Dequeue();
                    if (!connection.IsInUse && connection.Client.IsAllocated)
                    {
                        connection.IsInUse = true;
                        connection.LastUsed = DateTime.UtcNow;
                        connection.UsageCount++;
                        return connection;
                    }
                }
                
                // 새 연결 생성
                if (pool.Count < _maxConnectionsPerPool)
                {
                    var newClient = new TURNClient(serverId, "pooluser", "poolpass");
                    
                    if (await newClient.AuthenticateAsync() && 
                        await newClient.AllocateRelayAsync() != null)
                    {
                        return new TURNConnection
                        {
                            Client = newClient,
                            LastUsed = DateTime.UtcNow,
                            IsInUse = true,
                            UsageCount = 1
                        };
                    }
                }
                
                return null; // 연결 실패
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public void ReturnConnection(string serverId, TURNConnection connection)
        {
            if (connection == null) return;
            
            connection.IsInUse = false;
            
            if (_connectionPools.TryGetValue(serverId, out var pool))
            {
                // 너무 많이 사용된 연결은 폐기
                if (connection.UsageCount < 100)
                {
                    pool.Enqueue(connection);
                }
                else
                {
                    connection.Client.Dispose();
                }
            }
        }
        
        public void CleanupIdleConnections()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-5); // 5분 이상 비활성
            
            foreach (var pool in _connectionPools.Values)
            {
                var connectionsToRemove = new List<TURNConnection>();
                var connectionsToKeep = new List<TURNConnection>();
                
                while (pool.Count > 0)
                {
                    var connection = pool.Dequeue();
                    
                    if (!connection.IsInUse && connection.LastUsed < cutoffTime)
                    {
                        connectionsToRemove.Add(connection);
                    }
                    else
                    {
                        connectionsToKeep.Add(connection);
                    }
                }
                
                // 정리
                foreach (var connection in connectionsToRemove)
                {
                    connection.Client.Dispose();
                }
                
                // 유지할 연결들 다시 큐에 추가
                foreach (var connection in connectionsToKeep)
                {
                    pool.Enqueue(connection);
                }
            }
        }
    }
    
    public class DataCompressionManager
    {
        private readonly Dictionary<string, CompressionStats> _stats = new();
        
        public class CompressionStats
        {
            public long OriginalBytes { get; set; }
            public long CompressedBytes { get; set; }
            public double CompressionRatio => CompressedBytes / (double)OriginalBytes;
            public int CompressionTime { get; set; } // ms
        }
        
        public byte[] CompressData(byte[] data, string clientId, CompressionLevel level = CompressionLevel.Optimal)
        {
            var startTime = DateTime.UtcNow;
            
            using var input = new MemoryStream(data);
            using var output = new MemoryStream();
            
            using (var gzip = new System.IO.Compression.GZipStream(output, level))
            {
                input.CopyTo(gzip);
            }
            
            var compressed = output.ToArray();
            var compressionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            // 통계 업데이트
            if (!_stats.TryGetValue(clientId, out var stats))
            {
                stats = new CompressionStats();
                _stats[clientId] = stats;
            }
            
            stats.OriginalBytes += data.Length;
            stats.CompressedBytes += compressed.Length;
            stats.CompressionTime = compressionTime;
            
            // 압축 효과가 없으면 원본 반환
            if (compressed.Length >= data.Length * 0.9)
            {
                return data;
            }
            
            Console.WriteLine($"압축 완료: {data.Length} -> {compressed.Length} bytes " +
                            $"({stats.CompressionRatio:P1}, {compressionTime}ms)");
            
            return compressed;
        }
        
        public byte[] DecompressData(byte[] compressedData)
        {
            try
            {
                using var input = new MemoryStream(compressedData);
                using var output = new MemoryStream();
                using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
                
                gzip.CopyTo(output);
                return output.ToArray();
            }
            catch
            {
                // 압축되지 않은 데이터일 수 있음
                return compressedData;
            }
        }
        
        public CompressionStats GetCompressionStats(string clientId)
        {
            return _stats.GetValueOrDefault(clientId);
        }
    }
    
    public class LoadBalancer
    {
        private readonly List<TURNServerInfo> _servers = new();
        private int _currentIndex = 0;
        
        public class TURNServerInfo
        {
            public string ServerAddress { get; set; }
            public int CurrentConnections { get; set; }
            public int MaxConnections { get; set; }
            public double CpuUsage { get; set; }
            public double NetworkUsage { get; set; }
            public TimeSpan AverageLatency { get; set; }
            public bool IsHealthy { get; set; } = true;
            
            public double GetLoadScore()
            {
                var connectionRatio = CurrentConnections / (double)MaxConnections;
                var resourceUsage = (CpuUsage + NetworkUsage) / 2.0;
                var latencyPenalty = AverageLatency.TotalMilliseconds / 1000.0;
                
                return connectionRatio * 0.4 + resourceUsage * 0.4 + latencyPenalty * 0.2;
            }
        }
        
        public void AddServer(string serverAddress, int maxConnections = 1000)
        {
            _servers.Add(new TURNServerInfo
            {
                ServerAddress = serverAddress,
                MaxConnections = maxConnections
            });
        }
        
        public string SelectOptimalServer(LoadBalancingStrategy strategy = LoadBalancingStrategy.LeastLoaded)
        {
            var healthyServers = _servers.Where(s => s.IsHealthy).ToList();
            
            if (!healthyServers.Any())
            {
                throw new InvalidOperationException("사용 가능한 서버가 없습니다.");
            }
            
            return strategy switch
            {
                LoadBalancingStrategy.RoundRobin => SelectRoundRobin(healthyServers),
                LoadBalancingStrategy.LeastLoaded => SelectLeastLoaded(healthyServers),
                LoadBalancingStrategy.LeastLatency => SelectLeastLatency(healthyServers),
                LoadBalancingStrategy.Weighted => SelectWeighted(healthyServers),
                _ => healthyServers.First().ServerAddress
            };
        }
        
        private string SelectRoundRobin(List<TURNServerInfo> servers)
        {
            var server = servers[_currentIndex % servers.Count];
            _currentIndex++;
            return server.ServerAddress;
        }
        
        private string SelectLeastLoaded(List<TURNServerInfo> servers)
        {
            return servers.OrderBy(s => s.GetLoadScore()).First().ServerAddress;
        }
        
        private string SelectLeastLatency(List<TURNServerInfo> servers)
        {
            return servers.OrderBy(s => s.AverageLatency).First().ServerAddress;
        }
        
        private string SelectWeighted(List<TURNServerInfo> servers)
        {
            // 가중치 기반 선택 (부하가 낮을수록 높은 가중치)
            var weightedServers = servers.Select(s => new
            {
                Server = s,
                Weight = 1.0 / Math.Max(s.GetLoadScore(), 0.1)
            }).ToList();
            
            var totalWeight = weightedServers.Sum(ws => ws.Weight);
            var random = new Random().NextDouble() * totalWeight;
            
            double currentWeight = 0;
            foreach (var ws in weightedServers)
            {
                currentWeight += ws.Weight;
                if (random <= currentWeight)
                {
                    return ws.Server.ServerAddress;
                }
            }
            
            return servers.First().ServerAddress;
        }
        
        public async Task MonitorServerHealth()
        {
            while (true)
            {
                foreach (var server in _servers)
                {
                    try
                    {
                        // 서버 상태 확인
                        var latency = await MeasureServerLatency(server.ServerAddress);
                        server.AverageLatency = latency;
                        server.IsHealthy = latency.TotalMilliseconds < 1000; // 1초 이상이면 비정상
                        
                        Console.WriteLine($"서버 상태: {server.ServerAddress} - " +
                                        $"지연: {latency.TotalMilliseconds:F0}ms, " +
                                        $"부하: {server.GetLoadScore():F2}");
                    }
                    catch (Exception ex)
                    {
                        server.IsHealthy = false;
                        Console.WriteLine($"서버 상태 확인 실패: {server.ServerAddress} - {ex.Message}");
                    }
                }
                
                await Task.Delay(30000); // 30초마다 확인
            }
        }
        
        private async Task<TimeSpan> MeasureServerLatency(string serverAddress)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                using var client = new TURNClient(serverAddress, "healthcheck", "healthcheck");
                await client.AuthenticateAsync();
                
                return DateTime.UtcNow - startTime;
            }
            catch
            {
                return TimeSpan.FromMilliseconds(10000); // 실패 시 높은 값
            }
        }
    }
    
    public enum LoadBalancingStrategy
    {
        RoundRobin,
        LeastLoaded,
        LeastLatency,
        Weighted
    }
    
    public TURNOptimizationManager()
    {
        _bandwidthOptimizer = new BandwidthOptimizer();
        _connectionPool = new ConnectionPoolManager();
        _compressionManager = new DataCompressionManager();
        _loadBalancer = new LoadBalancer();
        
        // 기본 서버들 추가
        _loadBalancer.AddServer("turn1.gameserver.com");
        _loadBalancer.AddServer("turn2.gameserver.com");
        _loadBalancer.AddServer("turn3.gameserver.com");
        
        // 서버 상태 모니터링 시작
        _ = Task.Run(_loadBalancer.MonitorServerHealth);
    }
    
    public async Task<bool> SendOptimizedData(string clientId, byte[] data, IPEndPoint target, int priority = 1)
    {
        try
        {
            // 1. 대역폭 확인
            var maxBandwidth = 1024 * 1024; // 1 MB/s
            if (!await _bandwidthOptimizer.RequestBandwidth(clientId, data.Length, maxBandwidth))
            {
                Console.WriteLine("대역폭 제한으로 데이터 전송 거부");
                return false;
            }
            
            // 2. 데이터 압축
            var compressedData = _compressionManager.CompressData(data, clientId);
            
            // 3. 최적 서버 선택
            var serverAddress = _loadBalancer.SelectOptimalServer(LoadBalancingStrategy.LeastLoaded);
            
            // 4. 연결 풀에서 연결 가져오기
            var connection = await _connectionPool.GetConnectionAsync(serverAddress);
            
            if (connection != null)
            {
                try
                {
                    await connection.Client.SendDataAsync(compressedData, target);
                    return true;
                }
                finally
                {
                    _connectionPool.ReturnConnection(serverAddress, connection);
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"최적화된 데이터 전송 실패: {ex.Message}");
            return false;
        }
    }
}

// 사용 예제
public class TURNOptimizationExample
{
    public static async Task Main(string[] args)
    {
        var optimizer = new TURNOptimizationManager();
        
        // 게임 세션 시뮬레이션
        var players = new[] { "player1", "player2", "player3" };
        var targetEndPoint = new IPEndPoint(IPAddress.Parse("203.0.113.100"), 12345);
        
        for (int frame = 0; frame < 1000; frame++)
        {
            foreach (var player in players)
            {
                var gameData = CreateGameFrameData(player, frame);
                var priority = player == "player1" ? 3 : 1; // 특정 플레이어 우선순위
                
                await optimizer.SendOptimizedData(player, gameData, targetEndPoint, priority);
            }
            
            await Task.Delay(16); // 60 FPS
        }
    }
    
    private static byte[] CreateGameFrameData(string playerId, int frame)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        
        writer.Write(playerId);
        writer.Write(frame);
        writer.Write(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        writer.Write(100.0f + frame % 100); // X
        writer.Write(50.0f); // Y
        writer.Write(200.0f + frame % 200); // Z
        
        return ms.ToArray();
    }
}
```

이제 4장에서 TURN 프로토콜과 릴레이 서버에 대해 포괄적으로 다뤘습니다. TURN의 개념부터 서버 구축, 클라이언트 구현, 그리고 성능 최적화까지 실제 게임 개발에서 활용할 수 있는 모든 내용을 포함했습니다. 다음 장에서는 실제 P2P 게임 아키텍처 설계에 대해 알아보겠습니다.    

  

# 5장: ICE(Interactive Connectivity Establishment) 프레임워크
  
## 5.1 ICE의 개념과 후보자 수집

### 5.1.1 ICE 프레임워크 개요
ICE(Interactive Connectivity Establishment)는 RFC 8445에 정의된 프로토콜로, 두 피어 간의 최적 연결 경로를 찾기 위한 종합적인 NAT 순회 프레임워크입니다. ICE는 STUN과 TURN을 활용하여 여러 연결 후보자를 수집하고, 실제 연결성을 테스트하여 가장 좋은 경로를 선택합니다.

```csharp
public enum ICECandidateType
{
    Host,           // 로컬 인터페이스 주소
    ServerReflexive, // STUN을 통해 얻은 공인 주소
    PeerReflexive,   // 피어로부터 학습한 주소
    Relay           // TURN 릴레이 주소
}

public enum ICEConnectionState
{
    New,            // 초기 상태
    Gathering,      // 후보자 수집 중
    Checking,       // 연결성 검사 중
    Connected,      // 연결 성공
    Completed,      // 최적화 완료
    Failed,         // 연결 실패
    Disconnected,   // 연결 끊어짐
    Closed          // ICE 종료
}

public class ICECandidate
{
    public ICECandidateType Type { get; set; }
    public IPEndPoint EndPoint { get; set; }
    public IPEndPoint BaseEndPoint { get; set; } // 기본 주소 (Host candidate의 경우)
    public string Foundation { get; set; }        // 후보자 그룹 식별자
    public uint Priority { get; set; }            // 우선순위 (높을수록 우선)
    public string ComponentId { get; set; }       // 컴포넌트 ID (RTP=1, RTCP=2 등)
    public TransportProtocol Transport { get; set; } = TransportProtocol.UDP;
    
    public override string ToString()
    {
        return $"{Type}:{EndPoint} (Priority: {Priority}, Foundation: {Foundation})";
    }
    
    public string ToSdpString()
    {
        // SDP candidate 형식으로 변환
        return $"candidate:{Foundation} {ComponentId} {Transport.ToString().ToUpper()} " +
               $"{Priority} {EndPoint.Address} {EndPoint.Port} typ {Type.ToString().ToLower()}";
    }
    
    public static ICECandidate FromSdpString(string sdpCandidate)
    {
        // SDP candidate 문자열 파싱
        var parts = sdpCandidate.Split(' ');
        if (parts.Length < 8) throw new ArgumentException("잘못된 SDP candidate 형식");
        
        return new ICECandidate
        {
            Foundation = parts[1],
            ComponentId = parts[2],
            Transport = Enum.Parse<TransportProtocol>(parts[3], true),
            Priority = uint.Parse(parts[4]),
            EndPoint = new IPEndPoint(IPAddress.Parse(parts[5]), int.Parse(parts[6])),
            Type = Enum.Parse<ICECandidateType>(parts[8], true)
        };
    }
}

public enum TransportProtocol
{
    UDP,
    TCP
}
```

### 5.1.2 후보자 수집 엔진
ICE 후보자를 체계적으로 수집하는 엔진을 구현합니다:

```csharp
public class ICECandidateGatherer
{
    private readonly List<ICECandidate> _candidates = new();
    private readonly List<string> _stunServers;
    private readonly List<TURNServerConfig> _turnServers;
    private readonly object _candidatesLock = new object();
    
    public event Action<ICECandidate> OnCandidateGathered;
    public event Action OnGatheringComplete;
    
    public class TURNServerConfig
    {
        public string Host { get; set; }
        public int Port { get; set; } = 3478;
        public string Username { get; set; }
        public string Password { get; set; }
    }
    
    public ICECandidateGatherer(IEnumerable<string> stunServers = null, 
                               IEnumerable<TURNServerConfig> turnServers = null)
    {
        _stunServers = stunServers?.ToList() ?? new List<string>
        {
            "stun.l.google.com:19302",
            "stun1.l.google.com:19302",
            "stun.ekiga.net:3478"
        };
        
        _turnServers = turnServers?.ToList() ?? new List<TURNServerConfig>();
    }
    
    public async Task<List<ICECandidate>> GatherCandidatesAsync(string componentId = "1")
    {
        Console.WriteLine("ICE 후보자 수집 시작...");
        
        var gatheringTasks = new List<Task>
        {
            GatherHostCandidates(componentId),
            GatherServerReflexiveCandidates(componentId),
            GatherRelayCandidates(componentId)
        };
        
        await Task.WhenAll(gatheringTasks);
        
        lock (_candidatesLock)
        {
            var sortedCandidates = _candidates.OrderByDescending(c => c.Priority).ToList();
            
            Console.WriteLine($"후보자 수집 완료: {sortedCandidates.Count}개");
            foreach (var candidate in sortedCandidates)
            {
                Console.WriteLine($"  {candidate}");
            }
            
            OnGatheringComplete?.Invoke();
            return new List<ICECandidate>(sortedCandidates);
        }
    }
    
    private async Task GatherHostCandidates(string componentId)
    {
        Console.WriteLine("Host 후보자 수집 중...");
        
        try
        {
            var hostInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
                .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                .Where(addr => !IPAddress.IsLoopback(addr.Address))
                .Select(addr => addr.Address)
                .Distinct();
            
            foreach (var address in hostInterfaces)
            {
                // 사용 가능한 포트 찾기
                var port = FindAvailablePort(address);
                if (port > 0)
                {
                    var candidate = new ICECandidate
                    {
                        Type = ICECandidateType.Host,
                        EndPoint = new IPEndPoint(address, port),
                        BaseEndPoint = new IPEndPoint(address, port),
                        Foundation = GenerateFoundation(ICECandidateType.Host, address),
                        ComponentId = componentId,
                        Priority = CalculatePriority(ICECandidateType.Host, 65535, 255)
                    };
                    
                    AddCandidate(candidate);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host 후보자 수집 오류: {ex.Message}");
        }
    }
    
    private async Task GatherServerReflexiveCandidates(string componentId)
    {
        Console.WriteLine("Server Reflexive 후보자 수집 중...");
        
        var tasks = _stunServers.Select(server => GatherFromStunServer(server, componentId));
        await Task.WhenAll(tasks);
    }
    
    private async Task GatherFromStunServer(string stunServer, string componentId)
    {
        try
        {
            var parts = stunServer.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : 3478;
            
            using var stunClient = new STUNClient(0, 3000, 1);
            var result = await stunClient.GetPublicEndPointAsync(host, port);
            
            if (result.IsSuccess)
            {
                var candidate = new ICECandidate
                {
                    Type = ICECandidateType.ServerReflexive,
                    EndPoint = result.PublicEndPoint,
                    BaseEndPoint = result.LocalEndPoint,
                    Foundation = GenerateFoundation(ICECandidateType.ServerReflexive, result.PublicEndPoint.Address),
                    ComponentId = componentId,
                    Priority = CalculatePriority(ICECandidateType.ServerReflexive, 65535, 255)
                };
                
                AddCandidate(candidate);
                Console.WriteLine($"STUN 서버 {stunServer}에서 후보자 획득: {candidate.EndPoint}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN 서버 {stunServer} 오류: {ex.Message}");
        }
    }
    
    private async Task GatherRelayCandidates(string componentId)
    {
        if (!_turnServers.Any())
        {
            Console.WriteLine("설정된 TURN 서버가 없습니다.");
            return;
        }
        
        Console.WriteLine("Relay 후보자 수집 중...");
        
        var tasks = _turnServers.Select(server => GatherFromTurnServer(server, componentId));
        await Task.WhenAll(tasks);
    }
    
    private async Task GatherFromTurnServer(TURNServerConfig serverConfig, string componentId)
    {
        try
        {
            using var turnClient = new TURNClient(serverConfig.Host, serverConfig.Username, 
                                                 serverConfig.Password, serverConfig.Port);
            
            if (await turnClient.AuthenticateAsync())
            {
                var relayAddress = await turnClient.AllocateRelayAsync();
                if (relayAddress != null)
                {
                    var candidate = new ICECandidate
                    {
                        Type = ICECandidateType.Relay,
                        EndPoint = relayAddress,
                        BaseEndPoint = relayAddress,
                        Foundation = GenerateFoundation(ICECandidateType.Relay, relayAddress.Address),
                        ComponentId = componentId,
                        Priority = CalculatePriority(ICECandidateType.Relay, 65535, 255)
                    };
                    
                    AddCandidate(candidate);
                    Console.WriteLine($"TURN 서버 {serverConfig.Host}에서 릴레이 후보자 획득: {relayAddress}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TURN 서버 {serverConfig.Host} 오류: {ex.Message}");
        }
    }
    
    private void AddCandidate(ICECandidate candidate)
    {
        lock (_candidatesLock)
        {
            // 중복 후보자 제거
            if (!_candidates.Any(c => c.EndPoint.Equals(candidate.EndPoint) && c.Type == candidate.Type))
            {
                _candidates.Add(candidate);
                OnCandidateGathered?.Invoke(candidate);
            }
        }
    }
    
    private string GenerateFoundation(ICECandidateType type, IPAddress address)
    {
        // Foundation은 같은 타입과 베이스 주소를 가진 후보자들을 그룹화
        var input = $"{type}:{address}";
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..8]; // 첫 8자리만 사용
    }
    
    private uint CalculatePriority(ICECandidateType type, ushort localPreference, byte componentId)
    {
        // RFC 8445의 우선순위 계산 공식
        var typePreference = type switch
        {
            ICECandidateType.Host => 126,
            ICECandidateType.PeerReflexive => 110,
            ICECandidateType.ServerReflexive => 100,
            ICECandidateType.Relay => 0,
            _ => 0
        };
        
        return (uint)((typePreference << 24) | (localPreference << 8) | componentId);
    }
    
    private int FindAvailablePort(IPAddress address, int startPort = 49152)
    {
        for (int port = startPort; port < 65536; port++)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(address, port));
                return port;
            }
            catch
            {
                continue;
            }
        }
        return -1;
    }
    
    public List<ICECandidate> GetCandidates()
    {
        lock (_candidatesLock)
        {
            return new List<ICECandidate>(_candidates);
        }
    }
}
```

### 5.1.3 트리클 ICE 구현
실시간으로 후보자를 교환하는 트리클 ICE를 구현합니다:

```csharp
public class TrickleICE
{
    private readonly ICECandidateGatherer _gatherer;
    private readonly List<ICECandidate> _remoteCandidates = new();
    private readonly object _remoteCandidatesLock = new object();
    
    public event Action<ICECandidate> OnLocalCandidateReady;
    public event Action<ICECandidate> OnRemoteCandidateAdded;
    public event Action OnAllCandidatesGathered;
    
    private bool _gatheringComplete = false;
    
    public TrickleICE(ICECandidateGatherer gatherer)
    {
        _gatherer = gatherer;
        _gatherer.OnCandidateGathered += OnCandidateGathered;
        _gatherer.OnGatheringComplete += OnGatheringComplete;
    }
    
    public async Task StartGatheringAsync(string componentId = "1")
    {
        Console.WriteLine("트리클 ICE 시작...");
        
        // 백그라운드에서 후보자 수집 시작
        _ = Task.Run(async () =>
        {
            await _gatherer.GatherCandidatesAsync(componentId);
        });
        
        // 즉시 반환하여 다른 작업들이 병렬로 진행될 수 있게 함
    }
    
    private void OnCandidateGathered(ICECandidate candidate)
    {
        Console.WriteLine($"새 로컬 후보자: {candidate}");
        OnLocalCandidateReady?.Invoke(candidate);
    }
    
    private void OnGatheringComplete()
    {
        _gatheringComplete = true;
        Console.WriteLine("로컬 후보자 수집 완료");
        OnAllCandidatesGathered?.Invoke();
    }
    
    public void AddRemoteCandidate(ICECandidate candidate)
    {
        lock (_remoteCandidatesLock)
        {
            if (!_remoteCandidates.Any(c => c.EndPoint.Equals(candidate.EndPoint)))
            {
                _remoteCandidates.Add(candidate);
                Console.WriteLine($"새 원격 후보자: {candidate}");
                OnRemoteCandidateAdded?.Invoke(candidate);
            }
        }
    }
    
    public void AddRemoteCandidateFromSdp(string sdpCandidate)
    {
        try
        {
            var candidate = ICECandidate.FromSdpString(sdpCandidate);
            AddRemoteCandidate(candidate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SDP 후보자 파싱 오류: {ex.Message}");
        }
    }
    
    public List<ICECandidate> GetLocalCandidates()
    {
        return _gatherer.GetCandidates();
    }
    
    public List<ICECandidate> GetRemoteCandidates()
    {
        lock (_remoteCandidatesLock)
        {
            return new List<ICECandidate>(_remoteCandidates);
        }
    }
    
    public bool IsGatheringComplete => _gatheringComplete;
    
    public List<string> GetLocalCandidatesSdp()
    {
        return GetLocalCandidates().Select(c => c.ToSdpString()).ToList();
    }
}

// 트리클 ICE 시뮬레이션 예제
public class TrickleICEDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("=== 트리클 ICE 데모 ===\n");
        
        // 두 개의 ICE 에이전트 생성
        var agentA = CreateICEAgent("Agent A");
        var agentB = CreateICEAgent("Agent B");
        
        // 상호 후보자 교환 설정
        agentA.OnLocalCandidateReady += candidate =>
        {
            Console.WriteLine($"A -> B: {candidate.ToSdpString()}");
            agentB.AddRemoteCandidate(candidate);
        };
        
        agentB.OnLocalCandidateReady += candidate =>
        {
            Console.WriteLine($"B -> A: {candidate.ToSdpString()}");
            agentA.AddRemoteCandidate(candidate);
        };
        
        // 동시에 후보자 수집 시작
        Console.WriteLine("양방향 후보자 수집 시작...\n");
        var taskA = agentA.StartGatheringAsync();
        var taskB = agentB.StartGatheringAsync();
        
        // 수집 완료 대기
        await Task.WhenAll(taskA, taskB);
        
        // 잠시 대기하여 모든 후보자가 교환되도록 함
        await Task.Delay(2000);
        
        Console.WriteLine("\n=== 최종 후보자 목록 ===");
        Console.WriteLine($"Agent A 로컬: {agentA.GetLocalCandidates().Count}개");
        Console.WriteLine($"Agent A 원격: {agentA.GetRemoteCandidates().Count}개");
        Console.WriteLine($"Agent B 로컬: {agentB.GetLocalCandidates().Count}개");
        Console.WriteLine($"Agent B 원격: {agentB.GetRemoteCandidates().Count}개");
    }
    
    private static TrickleICE CreateICEAgent(string name)
    {
        var gatherer = new ICECandidateGatherer();
        var trickleICE = new TrickleICE(gatherer);
        
        trickleICE.OnAllCandidatesGathered += () =>
        {
            Console.WriteLine($"{name} 후보자 수집 완료");
        };
        
        return trickleICE;
    }
}
```
  

## 5.2 후보자 우선순위와 페어링

### 5.2.1 후보자 페어 생성
로컬과 원격 후보자를 조합하여 연결 가능한 페어를 생성합니다:

```csharp
public class ICECandidatePair
{
    public ICECandidate LocalCandidate { get; set; }
    public ICECandidate RemoteCandidate { get; set; }
    public uint Priority { get; set; }
    public ICEPairState State { get; set; } = ICEPairState.Waiting;
    public bool IsNominated { get; set; }
    public DateTime LastCheckTime { get; set; }
    public int CheckCount { get; set; }
    public TimeSpan? RoundTripTime { get; set; }
    
    public string PairId => $"{LocalCandidate.Foundation}:{RemoteCandidate.Foundation}";
    
    public override string ToString()
    {
        var rttStr = RoundTripTime?.TotalMilliseconds.ToString("F1") + "ms" ?? "N/A";
        return $"{LocalCandidate.EndPoint} <-> {RemoteCandidate.EndPoint} " +
               $"({State}, RTT: {rttStr}, Priority: {Priority})";
    }
}

public enum ICEPairState
{
    Waiting,     // 검사 대기 중
    InProgress,  // 검사 진행 중
    Succeeded,   // 검사 성공
    Failed,      // 검사 실패
    Frozen       // 동일 foundation의 다른 페어 검사 대기 중
}

public class ICECandidatePairGenerator
{
    public List<ICECandidatePair> GeneratePairs(List<ICECandidate> localCandidates, 
                                               List<ICECandidate> remoteCandidates,
                                               bool isControlling = true)
    {
        var pairs = new List<ICECandidatePair>();
        
        foreach (var localCandidate in localCandidates)
        {
            foreach (var remoteCandidate in remoteCandidates)
            {
                // 호환성 검사
                if (AreCompatible(localCandidate, remoteCandidate))
                {
                    var pair = new ICECandidatePair
                    {
                        LocalCandidate = localCandidate,
                        RemoteCandidate = remoteCandidate,
                        Priority = CalculatePairPriority(localCandidate, remoteCandidate, isControlling)
                    };
                    
                    pairs.Add(pair);
                }
            }
        }
        
        // 우선순위에 따라 정렬
        pairs.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        
        // Foundation 기반 프리징 설정
        ApplyFoundationFreezing(pairs);
        
        Console.WriteLine($"생성된 후보자 페어: {pairs.Count}개");
        foreach (var pair in pairs.Take(5)) // 상위 5개만 출력
        {
            Console.WriteLine($"  {pair}");
        }
        
        return pairs;
    }
    
    private bool AreCompatible(ICECandidate local, ICECandidate remote)
    {
        // 주소 패밀리 확인 (IPv4 vs IPv6)
        if (local.EndPoint.AddressFamily != remote.EndPoint.AddressFamily)
            return false;
        
        // 전송 프로토콜 확인
        if (local.Transport != remote.Transport)
            return false;
        
        // 컴포넌트 ID 확인
        if (local.ComponentId != remote.ComponentId)
            return false;
        
        return true;
    }
    
    private uint CalculatePairPriority(ICECandidate local, ICECandidate remote, bool isControlling)
    {
        // RFC 8445의 페어 우선순위 계산
        var controllingPriority = isControlling ? local.Priority : remote.Priority;
        var controlledPriority = isControlling ? remote.Priority : local.Priority;
        
        return (uint)((Math.Min(controllingPriority, controlledPriority) << 32) |
                     (Math.Max(controllingPriority, controlledPriority) << 1) |
                     (controllingPriority > controlledPriority ? 1u : 0u));
    }
    
    private void ApplyFoundationFreezing(List<ICECandidatePair> pairs)
    {
        var foundationGroups = pairs.GroupBy(p => p.PairId).ToList();
        
        foreach (var group in foundationGroups)
        {
            var groupPairs = group.OrderByDescending(p => p.Priority).ToList();
            
            // 각 foundation 그룹에서 가장 높은 우선순위만 활성화
            for (int i = 0; i < groupPairs.Count; i++)
            {
                if (i == 0)
                {
                    groupPairs[i].State = ICEPairState.Waiting;
                }
                else
                {
                    groupPairs[i].State = ICEPairState.Frozen;
                }
            }
        }
    }
    
    public void UnfreezePairs(List<ICECandidatePair> pairs, string succeededFoundation)
    {
        // 성공한 foundation과 같은 그룹의 frozen 페어들을 해제
        foreach (var pair in pairs.Where(p => p.State == ICEPairState.Frozen))
        {
            if (pair.PairId == succeededFoundation)
            {
                pair.State = ICEPairState.Waiting;
                Console.WriteLine($"페어 해제: {pair}");
            }
        }
    }
}
```

### 5.2.2 동적 우선순위 조정
네트워크 상황에 따라 동적으로 우선순위를 조정하는 시스템입니다:

```csharp
public class DynamicPriorityManager
{
    private readonly Dictionary<string, PairMetrics> _pairMetrics = new();
    private readonly Timer _adjustmentTimer;
    
    public class PairMetrics
    {
        public List<long> RTTHistory { get; set; } = new();
        public List<DateTime> SuccessHistory { get; set; } = new();
        public List<DateTime> FailureHistory { get; set; } = new();
        public double PacketLossRate { get; set; }
        public double Jitter { get; set; }
        public int BandwidthEstimate { get; set; } // bytes/sec
        
        public double GetQualityScore()
        {
            if (!RTTHistory.Any()) return 0;
            
            var avgRTT = RTTHistory.Average();
            var successRate = CalculateSuccessRate();
            var stabilityScore = CalculateStabilityScore();
            
            // 품질 점수 계산 (0-100)
            var rttScore = Math.Max(0, 100 - (avgRTT / 10)); // 1000ms = 0점
            var reliabilityScore = successRate * 100;
            var stabilityBonus = stabilityScore * 20;
            
            return (rttScore * 0.4 + reliabilityScore * 0.4 + stabilityBonus * 0.2);
        }
        
        private double CalculateSuccessRate()
        {
            var totalAttempts = SuccessHistory.Count + FailureHistory.Count;
            return totalAttempts > 0 ? SuccessHistory.Count / (double)totalAttempts : 0;
        }
        
        private double CalculateStabilityScore()
        {
            if (RTTHistory.Count < 3) return 0;
            
            var recent = RTTHistory.TakeLast(10).ToList();
            var mean = recent.Average();
            var variance = recent.Sum(x => Math.Pow(x - mean, 2)) / recent.Count;
            var stdDev = Math.Sqrt(variance);
            
            // 표준편차가 낮을수록 안정적
            return Math.Max(0, 1 - (stdDev / mean));
        }
    }
    
    public DynamicPriorityManager()
    {
        _adjustmentTimer = new Timer(AdjustPriorities, null, 
                                   TimeSpan.FromSeconds(30), 
                                   TimeSpan.FromSeconds(30));
    }
    
    public void RecordCheckResult(ICECandidatePair pair, bool success, long rttMs = 0)
    {
        var pairId = pair.PairId;
        
        if (!_pairMetrics.TryGetValue(pairId, out var metrics))
        {
            metrics = new PairMetrics();
            _pairMetrics[pairId] = metrics;
        }
        
        if (success)
        {
            metrics.SuccessHistory.Add(DateTime.UtcNow);
            if (rttMs > 0)
            {
                metrics.RTTHistory.Add(rttMs);
                
                // 최근 50개 샘플만 유지
                if (metrics.RTTHistory.Count > 50)
                {
                    metrics.RTTHistory.RemoveAt(0);
                }
            }
        }
        else
        {
            metrics.FailureHistory.Add(DateTime.UtcNow);
        }
        
        // 오래된 기록 정리 (24시간 이상)
        var cutoff = DateTime.UtcNow.AddHours(-24);
        metrics.SuccessHistory.RemoveAll(t => t < cutoff);
        metrics.FailureHistory.RemoveAll(t => t < cutoff);
    }
    
    public List<ICECandidatePair> ReorderPairsByQuality(List<ICECandidatePair> pairs)
    {
        var reorderedPairs = pairs.Select(pair =>
        {
            var metrics = _pairMetrics.GetValueOrDefault(pair.PairId);
            var qualityScore = metrics?.GetQualityScore() ?? 0;
            
            return new
            {
                Pair = pair,
                QualityScore = qualityScore,
                OriginalPriority = pair.Priority
            };
        }).ToList();
        
        // 품질 점수와 원래 우선순위를 조합하여 재정렬
        reorderedPairs.Sort((a, b) =>
        {
            var qualityComparison = b.QualityScore.CompareTo(a.QualityScore);
            if (Math.Abs(qualityComparison) < 0.1) // 품질이 비슷하면 원래 우선순위 사용
            {
                return b.OriginalPriority.CompareTo(a.OriginalPriority);
            }
            return qualityComparison;
        });
        
        return reorderedPairs.Select(x => x.Pair).ToList();
    }
    
    private void AdjustPriorities(object state)
    {
        Console.WriteLine("동적 우선순위 조정 실행...");
        
        foreach (var (pairId, metrics) in _pairMetrics)
        {
            var qualityScore = metrics.GetQualityScore();
            Console.WriteLine($"페어 {pairId}: 품질 점수 {qualityScore:F1}");
            
            if (qualityScore < 30) // 품질이 너무 낮은 페어
            {
                Console.WriteLine($"  -> 품질 저하 감지, 재검사 권장");
            }
        }
    }
    
    public PairMetrics GetMetrics(string pairId)
    {
        return _pairMetrics.GetValueOrDefault(pairId);
    }
    
    public void Dispose()
    {
        _adjustmentTimer?.Dispose();
    }
}

// 우선순위 기반 페어 스케줄러
public class PairScheduler
{
    private readonly Queue<ICECandidatePair> _activeQueue = new();
    private readonly Queue<ICECandidatePair> _waitingQueue = new();
    private readonly DynamicPriorityManager _priorityManager;
    private readonly int _maxConcurrentChecks;
    
    public PairScheduler(DynamicPriorityManager priorityManager, int maxConcurrentChecks = 3)
    {
        _priorityManager = priorityManager;
        _maxConcurrentChecks = maxConcurrentChecks;
    }
    
    public void AddPairs(List<ICECandidatePair> pairs)
    {
        // 동적 우선순위 적용하여 재정렬
        var reorderedPairs = _priorityManager.ReorderPairsByQuality(pairs);
        
        foreach (var pair in reorderedPairs)
        {
            if (pair.State == ICEPairState.Waiting)
            {
                _waitingQueue.Enqueue(pair);
            }
        }
        
        Console.WriteLine($"스케줄러에 {pairs.Count}개 페어 추가됨");
    }
    
    public ICECandidatePair GetNextPairToCheck()
    {
        // 활성 검사가 최대 수에 도달했으면 null 반환
        if (_activeQueue.Count >= _maxConcurrentChecks)
            return null;
        
        // 대기 중인 페어가 있으면 활성 큐로 이동
        if (_waitingQueue.Count > 0)
        {
            var pair = _waitingQueue.Dequeue();
            pair.State = ICEPairState.InProgress;
            _activeQueue.Enqueue(pair);
            return pair;
        }
        
        return null;
    }
    
    public void CompletePairCheck(ICECandidatePair pair, bool success, long rttMs = 0)
    {
        // 활성 큐에서 제거
        var tempQueue = new Queue<ICECandidatePair>();
        while (_activeQueue.Count > 0)
        {
            var p = _activeQueue.Dequeue();
            if (p != pair)
            {
                tempQueue.Enqueue(p);
            }
        }
        
        while (tempQueue.Count > 0)
        {
            _activeQueue.Enqueue(tempQueue.Dequeue());
        }
        
        // 상태 업데이트
        pair.State = success ? ICEPairState.Succeeded : ICEPairState.Failed;
        pair.LastCheckTime = DateTime.UtcNow;
        pair.CheckCount++;
        
        if (success && rttMs > 0)
        {
            pair.RoundTripTime = TimeSpan.FromMilliseconds(rttMs);
        }
        
        // 메트릭 기록
        _priorityManager.RecordCheckResult(pair, success, rttMs);
        
        Console.WriteLine($"페어 검사 완료: {pair} -> {(success ? "성공" : "실패")}");
    }
    
    public int GetActiveCheckCount() => _activeQueue.Count;
    public int GetWaitingCheckCount() => _waitingQueue.Count;
    
    public void ReprioritizeWaitingPairs()
    {
        var waitingPairs = new List<ICECandidatePair>();
        
        while (_waitingQueue.Count > 0)
        {
            waitingPairs.Add(_waitingQueue.Dequeue());
        }
        
        // 재우선순위 적용
        var reordered = _priorityManager.ReorderPairsByQuality(waitingPairs);
        
        foreach (var pair in reordered)
        {
            _waitingQueue.Enqueue(pair);
        }
        
        Console.WriteLine($"대기 중인 {waitingPairs.Count}개 페어 재우선순위 적용");
    }
}
```
  

## 5.3 연결성 검사 및 최적 경로 선택

### 5.3.1 STUN 기반 연결성 검사
실제 연결성을 테스트하는 검사 엔진을 구현합니다:

```csharp
public class ICEConnectivityChecker
{
    private readonly Dictionary<string, UdpClient> _sockets = new();
    private readonly PairScheduler _scheduler;
    private readonly object _socketLock = new object();
    private bool _isRunning;
    
    public event Action<ICECandidatePair, bool, long> OnCheckComplete;
    public event Action<ICECandidatePair> OnPairNominated;
    
    public ICEConnectivityChecker(PairScheduler scheduler)
    {
        _scheduler = scheduler;
    }
    
    public async Task StartConnectivityChecks(List<ICECandidatePair> pairs, 
                                            string localUsername, 
                                            string localPassword,
                                            string remoteUsername,
                                            string remotePassword)
    {
        Console.WriteLine("연결성 검사 시작...");
        
        _isRunning = true;
        _scheduler.AddPairs(pairs);
        
        // 소켓 초기화
        await InitializeSockets(pairs);
        
        // 검사 루프 시작
        var checkTasks = new List<Task>();
        
        while (_isRunning && (_scheduler.GetActiveCheckCount() > 0 || _scheduler.GetWaitingCheckCount() > 0))
        {
            var pairToCheck = _scheduler.GetNextPairToCheck();
            
            if (pairToCheck != null)
            {
                var checkTask = PerformConnectivityCheck(pairToCheck, 
                                                       localUsername, localPassword,
                                                       remoteUsername, remotePassword);
                checkTasks.Add(checkTask);
            }
            else
            {
                // 잠시 대기 후 다시 시도
                await Task.Delay(100);
            }
            
            // 완료된 작업들 정리
            checkTasks.RemoveAll(t => t.IsCompleted);
        }
        
        // 모든 검사 완료 대기
        await Task.WhenAll(checkTasks);
        
        Console.WriteLine("연결성 검사 완료");
        CleanupSockets();
    }
    
    private async Task InitializeSockets(List<ICECandidatePair> pairs)
    {
        var localEndPoints = pairs.Select(p => p.LocalCandidate.EndPoint).Distinct();
        
        foreach (var endPoint in localEndPoints)
        {
            try
            {
                var socket = new UdpClient(endPoint);
                
                lock (_socketLock)
                {
                    _sockets[endPoint.ToString()] = socket;
                }
                
                // 수신 대기 시작
                _ = Task.Run(() => HandleIncomingData(socket, endPoint));
                
                Console.WriteLine($"소켓 초기화: {endPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"소켓 초기화 실패 {endPoint}: {ex.Message}");
            }
        }
    }
    
    private async Task HandleIncomingData(UdpClient socket, IPEndPoint localEndPoint)
    {
        while (_isRunning)
        {
            try
            {
                var result = await socket.ReceiveAsync();
                
                // STUN 응답 처리
                if (IsSTUNMessage(result.Buffer))
                {
                    await ProcessSTUNResponse(result.Buffer, result.RemoteEndPoint, localEndPoint);
                }
                else
                {
                    // 일반 데이터 처리
                    Console.WriteLine($"데이터 수신: {result.RemoteEndPoint} -> {localEndPoint} " +
                                    $"({result.Buffer.Length} bytes)");
                }
            }
            catch (ObjectDisposedException)
            {
                break; // 소켓이 닫힘
            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터 수신 오류 {localEndPoint}: {ex.Message}");
            }
        }
    }
    
    private async Task PerformConnectivityCheck(ICECandidatePair pair,
                                              string localUsername, string localPassword,
                                              string remoteUsername, string remotePassword)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            Console.WriteLine($"연결성 검사 시작: {pair}");
            
            // STUN Binding Request 생성
            var bindingRequest = CreateSTUNBindingRequest(localUsername, remoteUsername, localPassword);
            
            // 소켓 가져오기
            UdpClient socket;
            lock (_socketLock)
            {
                var socketKey = pair.LocalCandidate.EndPoint.ToString();
                if (!_sockets.TryGetValue(socketKey, out socket))
                {
                    throw new InvalidOperationException($"소켓을 찾을 수 없습니다: {socketKey}");
                }
            }
            
            // 요청 전송
            var requestData = bindingRequest.ToByteArray();
            await socket.SendAsync(requestData, requestData.Length, pair.RemoteCandidate.EndPoint);
            
            // 응답 대기 (타임아웃: 500ms)
            var success = await WaitForSTUNResponse(bindingRequest.TransactionId, 
                                                  pair.RemoteCandidate.EndPoint, 500);
            
            var rtt = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            
            _scheduler.CompletePairCheck(pair, success, success ? rtt : 0);
            OnCheckComplete?.Invoke(pair, success, success ? rtt : 0);
            
            if (success)
            {
                Console.WriteLine($"연결성 검사 성공: {pair} (RTT: {rtt}ms)");
                
                // 첫 번째 성공한 페어를 지명
                if (!pair.IsNominated)
                {
                    pair.IsNominated = true;
                    OnPairNominated?.Invoke(pair);
                    Console.WriteLine($"페어 지명: {pair}");
                }
            }
            else
            {
                Console.WriteLine($"연결성 검사 실패: {pair}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"연결성 검사 오류: {pair} - {ex.Message}");
            _scheduler.CompletePairCheck(pair, false);
            OnCheckComplete?.Invoke(pair, false, 0);
        }
    }
    
    private STUNMessage CreateSTUNBindingRequest(string localUsername, 
                                               string remoteUsername, 
                                               string password)
    {
        var request = new STUNMessage
        {
            MessageType = STUNMessageType.BindingRequest
        };
        
        // USERNAME 어트리뷰트 (remote:local 형식)
        request.Attributes.Add(new UsernameAttribute
        {
            Type = STUNAttributeType.Username,
            Username = $"{remoteUsername}:{localUsername}"
        });
        
        // PRIORITY 어트리뷰트 (ICE에서 사용)
        request.Attributes.Add(new PriorityAttribute
        {
            Type = STUNAttributeType.Priority,
            Priority = 2113667326 // 기본값
        });
        
        // USE-CANDIDATE 어트리뷰트 (controlling agent에서 사용)
        request.Attributes.Add(new UseCandidateAttribute
        {
            Type = STUNAttributeType.UseCandidate
        });
        
        // MESSAGE-INTEGRITY 계산 및 추가
        var integrity = CalculateMessageIntegrity(request, password);
        request.Attributes.Add(new MessageIntegrityAttribute
        {
            Type = STUNAttributeType.MessageIntegrity,
            HMAC = integrity
        });
        
        return request;
    }
    
    private byte[] CalculateMessageIntegrity(STUNMessage message, string password)
    {
        // 실제 HMAC-SHA1 계산 (간단화된 버전)
        using var hmac = new System.Security.Cryptography.HMACSHA1(
            System.Text.Encoding.UTF8.GetBytes(password));
        
        var messageBytes = message.ToByteArray();
        return hmac.ComputeHash(messageBytes);
    }
    
    private async Task<bool> WaitForSTUNResponse(byte[] transactionId, 
                                               IPEndPoint remoteEndPoint, 
                                               int timeoutMs)
    {
        // 실제로는 응답 추적 시스템이 필요
        // 여기서는 간단화하여 시뮬레이션
        await Task.Delay(timeoutMs / 2);
        return Random.Shared.NextDouble() > 0.3; // 70% 성공률
    }
    
    private bool IsSTUNMessage(byte[] data)
    {
        if (data.Length < 20) return false;
        
        // STUN 매직 쿠키 확인
        var magicCookie = BitConverter.ToUInt32(data, 4);
        return magicCookie == STUNMessage.MAGIC_COOKIE;
    }
    
    private async Task ProcessSTUNResponse(byte[] data, IPEndPoint remoteEndPoint, IPEndPoint localEndPoint)
    {
        try
        {
            var response = STUNMessage.Parse(data);
            Console.WriteLine($"STUN 응답 수신: {response.MessageType} from {remoteEndPoint}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN 응답 파싱 오류: {ex.Message}");
        }
    }
    
    private void CleanupSockets()
    {
        lock (_socketLock)
        {
            foreach (var socket in _sockets.Values)
            {
                socket?.Close();
                socket?.Dispose();
            }
            _sockets.Clear();
        }
    }
    
    public void Stop()
    {
        _isRunning = false;
        CleanupSockets();
    }
}

// ICE 전용 STUN 어트리뷰트들
public class PriorityAttribute : STUNAttribute
{
    public uint Priority { get; set; }
    
    public override ushort Length => 4;
    
    public override byte[] Value => BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)Priority));
}

public class UseCandidateAttribute : STUNAttribute
{
    public override ushort Length => 0; // 빈 어트리뷰트
    
    public override byte[] Value => Array.Empty<byte>();
}

public class ICEControllingAttribute : STUNAttribute
{
    public ulong TieBreaker { get; set; }
    
    public override ushort Length => 8;
    
    public override byte[] Value
    {
        get
        {
            var bytes = BitConverter.GetBytes(TieBreaker);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}

public class ICEControlledAttribute : STUNAttribute
{
    public ulong TieBreaker { get; set; }
    
    public override ushort Length => 8;
    
    public override byte[] Value
    {
        get
        {
            var bytes = BitConverter.GetBytes(TieBreaker);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}
```

### 5.3.2 최적 경로 선택 및 지명
연결성 검사 결과를 바탕으로 최적의 경로를 선택합니다:

```csharp
public class ICEPathSelector
{
    private readonly List<ICECandidatePair> _validPairs = new();
    private ICECandidatePair _selectedPair;
    private readonly object _pairsLock = new object();
    
    public event Action<ICECandidatePair> OnPathSelected;
    public event Action<ICECandidatePair, ICECandidatePair> OnPathChanged;
    
    public class PathSelectionCriteria
    {
        public double RTTWeight { get; set; } = 0.4;
        public double ReliabilityWeight { get; set; } = 0.3;
        public double TypePreferenceWeight { get; set; } = 0.2;
        public double BandwidthWeight { get; set; } = 0.1;
        
        public double MaxAcceptableRTT { get; set; } = 200; // ms
        public double MinReliability { get; set; } = 0.8;   // 80%
    }
    
    private readonly PathSelectionCriteria _criteria;
    private readonly Timer _optimizationTimer;
    
    public ICEPathSelector(PathSelectionCriteria criteria = null)
    {
        _criteria = criteria ?? new PathSelectionCriteria();
        
        // 주기적 경로 최적화
        _optimizationTimer = new Timer(OptimizePath, null, 
                                     TimeSpan.FromSeconds(30), 
                                     TimeSpan.FromSeconds(30));
    }
    
    public void AddValidPair(ICECandidatePair pair)
    {
        lock (_pairsLock)
        {
            if (!_validPairs.Any(p => p.PairId == pair.PairId))
            {
                _validPairs.Add(pair);
                Console.WriteLine($"유효한 페어 추가: {pair}");
                
                // 첫 번째 유효한 페어 또는 더 좋은 페어가 있으면 선택
                if (_selectedPair == null || IsBetterPair(pair, _selectedPair))
                {
                    SelectPath(pair);
                }
            }
        }
    }
    
    public void SelectPath(ICECandidatePair pair)
    {
        var previousPair = _selectedPair;
        _selectedPair = pair;
        
        Console.WriteLine($"경로 선택: {pair}");
        OnPathSelected?.Invoke(pair);
        
        if (previousPair != null && previousPair != pair)
        {
            Console.WriteLine($"경로 변경: {previousPair} -> {pair}");
            OnPathChanged?.Invoke(previousPair, pair);
        }
    }
    
    public bool IsBetterPair(ICECandidatePair candidate, ICECandidatePair current)
    {
        var candidateScore = CalculatePathScore(candidate);
        var currentScore = CalculatePathScore(current);
        
        return candidateScore > currentScore;
    }
    
    public double CalculatePathScore(ICECandidatePair pair)
    {
        double score = 0;
        
        // RTT 점수 (낮을수록 좋음)
        if (pair.RoundTripTime.HasValue)
        {
            var rtt = pair.RoundTripTime.Value.TotalMilliseconds;
            var rttScore = Math.Max(0, (1 - rtt / _criteria.MaxAcceptableRTT)) * 100;
            score += rttScore * _criteria.RTTWeight;
        }
        
        // 타입 선호도 점수
        var typeScore = GetTypePreferenceScore(pair);
        score += typeScore * _criteria.TypePreferenceWeight;
        
        // 신뢰성 점수 (성공률 기반)
        var reliabilityScore = GetReliabilityScore(pair);
        score += reliabilityScore * _criteria.ReliabilityWeight;
        
        // 대역폭 점수 (추정값 기반)
        var bandwidthScore = GetBandwidthScore(pair);
        score += bandwidthScore * _criteria.BandwidthWeight;
        
        return score;
    }
    
    private double GetTypePreferenceScore(ICECandidatePair pair)
    {
        // 연결 타입별 선호도 점수
        var localTypeScore = pair.LocalCandidate.Type switch
        {
            ICECandidateType.Host => 100,
            ICECandidateType.ServerReflexive => 80,
            ICECandidateType.PeerReflexive => 70,
            ICECandidateType.Relay => 50,
            _ => 0
        };
        
        var remoteTypeScore = pair.RemoteCandidate.Type switch
        {
            ICECandidateType.Host => 100,
            ICECandidateType.ServerReflexive => 80,
            ICECandidateType.PeerReflexive => 70,
            ICECandidateType.Relay => 50,
            _ => 0
        };
        
        return (localTypeScore + remoteTypeScore) / 2.0;
    }
    
    private double GetReliabilityScore(ICECandidatePair pair)
    {
        // 연결 검사 성공률 기반 신뢰성 계산
        if (pair.CheckCount == 0) return 50; // 기본값
        
        var successCount = pair.State == ICEPairState.Succeeded ? 1 : 0;
        var successRate = successCount / (double)pair.CheckCount;
        
        return successRate * 100;
    }
    
    private double GetBandwidthScore(ICECandidatePair pair)
    {
        // 연결 타입별 예상 대역폭 점수
        return (pair.LocalCandidate.Type, pair.RemoteCandidate.Type) switch
        {
            (ICECandidateType.Host, ICECandidateType.Host) => 100,    // 직접 연결
            (ICECandidateType.ServerReflexive, ICECandidateType.ServerReflexive) => 90,
            (ICECandidateType.Host, ICECandidateType.ServerReflexive) => 85,
            (ICECandidateType.ServerReflexive, ICECandidateType.Host) => 85,
            _ when pair.LocalCandidate.Type == ICECandidateType.Relay || 
                   pair.RemoteCandidate.Type == ICECandidateType.Relay => 60, // 릴레이 연결
            _ => 70
        };
    }
    
    private void OptimizePath(object state)
    {
        lock (_pairsLock)
        {
            if (_validPairs.Count <= 1) return;
            
            Console.WriteLine("경로 최적화 실행...");
            
            // 모든 유효한 페어의 점수 재계산
            var scoredPairs = _validPairs.Select(pair => new
            {
                Pair = pair,
                Score = CalculatePathScore(pair)
            }).OrderByDescending(x => x.Score).ToList();
            
            foreach (var item in scoredPairs.Take(3))
            {
                Console.WriteLine($"  {item.Pair} - 점수: {item.Score:F1}");
            }
            
            var bestPair = scoredPairs.First().Pair;
            
            // 현재 선택된 페어보다 상당히 좋은 페어가 있으면 변경
            if (_selectedPair != bestPair && 
                scoredPairs.First().Score > CalculatePathScore(_selectedPair) + 10)
            {
                Console.WriteLine("더 좋은 경로 발견, 변경 실행");
                SelectPath(bestPair);
            }
        }
    }
    
    public ICECandidatePair GetSelectedPair()
    {
        return _selectedPair;
    }
    
    public List<ICECandidatePair> GetValidPairs()
    {
        lock (_pairsLock)
        {
            return new List<ICECandidatePair>(_validPairs);
        }
    }
    
    public void RemoveInvalidPair(ICECandidatePair pair)
    {
        lock (_pairsLock)
        {
            _validPairs.Remove(pair);
            
            if (_selectedPair == pair)
            {
                // 새로운 최적 경로 선택
                var bestAlternative = _validPairs.OrderByDescending(CalculatePathScore).FirstOrDefault();
                if (bestAlternative != null)
                {
                    SelectPath(bestAlternative);
                }
                else
                {
                    _selectedPair = null;
                    Console.WriteLine("사용 가능한 경로가 없습니다.");
                }
            }
        }
    }
    
    public void Dispose()
    {
        _optimizationTimer?.Dispose();
    }
}

// 경로 품질 모니터링
public class PathQualityMonitor
{
    private readonly ICEPathSelector _pathSelector;
    private readonly Timer _monitoringTimer;
    private readonly Dictionary<string, PathQualityMetrics> _pathMetrics = new();
    
    public class PathQualityMetrics
    {
        public Queue<long> RTTSamples { get; set; } = new(capacity: 100);
        public Queue<DateTime> PacketTimes { get; set; } = new(capacity: 100);
        public int PacketsSent { get; set; }
        public int PacketsReceived { get; set; }
        public DateTime LastPacketTime { get; set; }
        
        public double GetAverageRTT()
        {
            return RTTSamples.Any() ? RTTSamples.Average() : 0;
        }
        
        public double GetPacketLossRate()
        {
            return PacketsSent > 0 ? 1.0 - (PacketsReceived / (double)PacketsSent) : 0;
        }
        
        public double GetJitter()
        {
            if (RTTSamples.Count < 2) return 0;
            
            var rttArray = RTTSamples.ToArray();
            var differences = new List<double>();
            
            for (int i = 1; i < rttArray.Length; i++)
            {
                differences.Add(Math.Abs(rttArray[i] - rttArray[i - 1]));
            }
            
            return differences.Average();
        }
    }
    
    public PathQualityMonitor(ICEPathSelector pathSelector)
    {
        _pathSelector = pathSelector;
        _monitoringTimer = new Timer(MonitorQuality, null, 
                                   TimeSpan.FromSeconds(5), 
                                   TimeSpan.FromSeconds(5));
    }
    
    public void RecordPacket(ICECandidatePair pair, bool sent, long rttMs = 0)
    {
        var pairId = pair.PairId;
        
        if (!_pathMetrics.TryGetValue(pairId, out var metrics))
        {
            metrics = new PathQualityMetrics();
            _pathMetrics[pairId] = metrics;
        }
        
        if (sent)
        {
            metrics.PacketsSent++;
        }
        else
        {
            metrics.PacketsReceived++;
            metrics.LastPacketTime = DateTime.UtcNow;
            
            if (rttMs > 0)
            {
                metrics.RTTSamples.Enqueue(rttMs);
                
                // 최근 100개 샘플만 유지
                if (metrics.RTTSamples.Count > 100)
                {
                    metrics.RTTSamples.Dequeue();
                }
            }
        }
    }
    
    private void MonitorQuality(object state)
    {
        var selectedPair = _pathSelector.GetSelectedPair();
        if (selectedPair == null) return;
        
        var metrics = _pathMetrics.GetValueOrDefault(selectedPair.PairId);
        if (metrics == null) return;
        
        var avgRTT = metrics.GetAverageRTT();
        var packetLoss = metrics.GetPacketLossRate();
        var jitter = metrics.GetJitter();
        
        Console.WriteLine($"경로 품질: RTT={avgRTT:F1}ms, 손실={packetLoss:P1}, 지터={jitter:F1}ms");
        
        // 품질 저하 감지
        if (avgRTT > 300 || packetLoss > 0.1) // 300ms 이상 또는 10% 이상 손실
        {
            Console.WriteLine("경로 품질 저하 감지 - 대안 경로 탐색 권장");
            
            // 다른 유효한 경로가 있는지 확인
            var validPairs = _pathSelector.GetValidPairs();
            var alternatives = validPairs.Where(p => p != selectedPair).ToList();
            
            if (alternatives.Any())
            {
                var bestAlternative = alternatives.OrderByDescending(_pathSelector.CalculatePathScore).First();
                Console.WriteLine($"대안 경로 발견: {bestAlternative}");
                _pathSelector.SelectPath(bestAlternative);
            }
        }
    }
    
    public PathQualityMetrics GetMetrics(ICECandidatePair pair)
    {
        return _pathMetrics.GetValueOrDefault(pair.PairId);
    }
    
    public void Dispose()
    {
        _monitoringTimer?.Dispose();
    }
}
```
  

## 5.4 C#에서 ICE 구현하기

### 5.4.1 통합 ICE 에이전트
모든 ICE 기능을 통합한 완전한 ICE 에이전트를 구현합니다:

```csharp
public class ICEAgent : IDisposable
{
    private readonly ICECandidateGatherer _gatherer;
    private readonly TrickleICE _trickleICE;
    private readonly ICECandidatePairGenerator _pairGenerator;
    private readonly DynamicPriorityManager _priorityManager;
    private readonly PairScheduler _scheduler;
    private readonly ICEConnectivityChecker _connectivityChecker;
    private readonly ICEPathSelector _pathSelector;
    private readonly PathQualityMonitor _qualityMonitor;
    
    private ICEConnectionState _state = ICEConnectionState.New;
    private readonly string _localUsername;
    private readonly string _localPassword;
    private string _remoteUsername;
    private string _remotePassword;
    private readonly bool _isControlling;
    
    public event Action<ICEConnectionState> OnStateChanged;
    public event Action<ICECandidate> OnLocalCandidateReady;
    public event Action<ICECandidatePair> OnConnectionEstablished;
    public event Action<byte[], IPEndPoint> OnDataReceived;
    
    public ICEConnectionState State => _state;
    public ICECandidatePair SelectedPair => _pathSelector.GetSelectedPair();
    
    public ICEAgent(bool isControlling = true, 
                   IEnumerable<string> stunServers = null,
                   IEnumerable<ICECandidateGatherer.TURNServerConfig> turnServers = null)
    {
        _isControlling = isControlling;
        _localUsername = GenerateUsername();
        _localPassword = GeneratePassword();
        
        // 컴포넌트들 초기화
        _gatherer = new ICECandidateGatherer(stunServers, turnServers);
        _trickleICE = new TrickleICE(_gatherer);
        _pairGenerator = new ICECandidatePairGenerator();
        _priorityManager = new DynamicPriorityManager();
        _scheduler = new PairScheduler(_priorityManager);
        _connectivityChecker = new ICEConnectivityChecker(_scheduler);
        _pathSelector = new ICEPathSelector();
        _qualityMonitor = new PathQualityMonitor(_pathSelector);
        
        SetupEventHandlers();
        
        Console.WriteLine($"ICE 에이전트 생성됨 (Role: {(_isControlling ? "Controlling" : "Controlled")})");
    }
    
    private void SetupEventHandlers()
    {
        _trickleICE.OnLocalCandidateReady += candidate =>
        {
            OnLocalCandidateReady?.Invoke(candidate);
        };
        
        _trickleICE.OnRemoteCandidateAdded += candidate =>
        {
            // 새 원격 후보자가 추가되면 페어를 생성하고 검사 시작
            if (_state == ICEConnectionState.Checking)
            {
                StartPairChecking();
            }
        };
        
        _connectivityChecker.OnCheckComplete += (pair, success, rtt) =>
        {
            if (success)
            {
                _pathSelector.AddValidPair(pair);
            }
        };
        
        _connectivityChecker.OnPairNominated += pair =>
        {
            if (_state != ICEConnectionState.Connected)
            {
                SetState(ICEConnectionState.Connected);
                OnConnectionEstablished?.Invoke(pair);
            }
        };
        
        _pathSelector.OnPathSelected += pair =>
        {
            Console.WriteLine($"최적 경로 선택됨: {pair}");
        };
    }
    
    public async Task<bool> StartAsync()
    {
        Console.WriteLine("ICE 에이전트 시작...");
        
        SetState(ICEConnectionState.Gathering);
        
        // 후보자 수집 시작
        await _trickleICE.StartGatheringAsync();
        
        return true;
    }
    
    public void SetRemoteCredentials(string username, string password)
    {
        _remoteUsername = username;
        _remotePassword = password;
        
        Console.WriteLine($"원격 자격 증명 설정: {username}");
        
        // 자격 증명이 설정되고 후보자가 있으면 연결성 검사 시작
        if (_trickleICE.GetRemoteCandidates().Any())
        {
            StartPairChecking();
        }
    }
    
    public void AddRemoteCandidate(ICECandidate candidate)
    {
        _trickleICE.AddRemoteCandidate(candidate);
    }
    
    public void AddRemoteCandidateFromSdp(string sdpCandidate)
    {
        _trickleICE.AddRemoteCandidateFromSdp(sdpCandidate);
    }
    
    private async void StartPairChecking()
    {
        if (_state != ICEConnectionState.Gathering && _state != ICEConnectionState.Checking)
            return;
        
        if (string.IsNullOrEmpty(_remoteUsername) || string.IsNullOrEmpty(_remotePassword))
        {
            Console.WriteLine("원격 자격 증명이 없어 연결성 검사를 연기합니다.");
            return;
        }
        
        SetState(ICEConnectionState.Checking);
        
        var localCandidates = _trickleICE.GetLocalCandidates();
        var remoteCandidates = _trickleICE.GetRemoteCandidates();
        
        if (!localCandidates.Any() || !remoteCandidates.Any())
        {
            Console.WriteLine("후보자가 부족하여 연결성 검사를 연기합니다.");
            return;
        }
        
        Console.WriteLine($"연결성 검사 시작: 로컬 {localCandidates.Count}개, 원격 {remoteCandidates.Count}개");
        
        var pairs = _pairGenerator.GeneratePairs(localCandidates, remoteCandidates, _isControlling);
        
        // 연결성 검사 시작
        _ = Task.Run(async () =>
        {
            await _connectivityChecker.StartConnectivityChecks(pairs, 
                                                             _localUsername, _localPassword,
                                                             _remoteUsername, _remotePassword);
        });
    }
    
    public async Task<bool> SendDataAsync(byte[] data)
    {
        var selectedPair = _pathSelector.GetSelectedPair();
        if (selectedPair == null || _state != ICEConnectionState.Connected)
        {
            Console.WriteLine("연결이 설정되지 않아 데이터를 보낼 수 없습니다.");
            return false;
        }
        
        try
        {
            // 실제 구현에서는 선택된 페어의 소켓을 통해 데이터 전송
            Console.WriteLine($"데이터 전송: {data.Length} bytes -> {selectedPair.RemoteCandidate.EndPoint}");
            
            // 품질 모니터링을 위한 패킷 기록
            _qualityMonitor.RecordPacket(selectedPair, sent: true);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"데이터 전송 오류: {ex.Message}");
            return false;
        }
    }
    
    public string GetLocalUsername() => _localUsername;
    public string GetLocalPassword() => _localPassword;
    
    public List<string> GetLocalCandidatesSdp()
    {
        return _trickleICE.GetLocalCandidatesSdp();
    }
    
    private void SetState(ICEConnectionState newState)
    {
        if (_state != newState)
        {
            var oldState = _state;
            _state = newState;
            
            Console.WriteLine($"ICE 상태 변경: {oldState} -> {newState}");
            OnStateChanged?.Invoke(newState);
        }
    }
    
    private string GenerateUsername()
    {
        return $"ice_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Random.Shared.Next(1000, 9999)}";
    }
    
    private string GeneratePassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 24)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    
    public void Dispose()
    {
        _connectivityChecker?.Stop();
        _priorityManager?.Dispose();
        _pathSelector?.Dispose();
        _qualityMonitor?.Dispose();
        
        SetState(ICEConnectionState.Closed);
    }
}
```

### 5.4.2 ICE 게임 세션 관리자
실제 게임에서 ICE를 활용하는 세션 관리자를 구현합니다:

```csharp
public class ICEGameSessionManager
{
    private readonly Dictionary<string, ICEAgent> _agents = new();
    private readonly Dictionary<string, GamePeer> _peers = new();
    private readonly SignalingChannel _signalingChannel;
    
    public class GamePeer
    {
        public string PeerId { get; set; }
        public ICEAgent Agent { get; set; }
        public ICEConnectionState ConnectionState { get; set; }
        public DateTime LastActivity { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }
    
    public class SignalingChannel
    {
        public event Action<string, string> OnSignalingMessage;
        
        public async Task SendSignalingMessage(string peerId, string message)
        {
            // 실제로는 시그널링 서버를 통해 전송
            Console.WriteLine($"시그널링 전송 -> {peerId}: {message[..Math.Min(message.Length, 50)]}...");
            
            // 시뮬레이션을 위한 지연
            await Task.Delay(100);
        }
        
        public void SimulateIncomingMessage(string fromPeerId, string message)
        {
            OnSignalingMessage?.Invoke(fromPeerId, message);
        }
    }
    
    public event Action<string, ICEConnectionState> OnPeerConnectionStateChanged;
    public event Action<string, byte[]> OnGameDataReceived;
    
    public ICEGameSessionManager()
    {
        _signalingChannel = new SignalingChannel();
        _signalingChannel.OnSignalingMessage += HandleSignalingMessage;
    }
    
    public async Task<bool> ConnectToPeerAsync(string peerId, bool asController = true)
    {
        Console.WriteLine($"피어 연결 시작: {peerId} (Role: {(asController ? "Controller" : "Controlled")})");
        
        // ICE 에이전트 생성
        var agent = new ICEAgent(asController);
        
        agent.OnStateChanged += state =>
        {
            UpdatePeerConnectionState(peerId, state);
        };
        
        agent.OnLocalCandidateReady += async candidate =>
        {
            // 로컬 후보자를 시그널링을 통해 피어에게 전송
            var candidateMessage = CreateCandidateMessage(candidate);
            await _signalingChannel.SendSignalingMessage(peerId, candidateMessage);
        };
        
        agent.OnConnectionEstablished += pair =>
        {
            Console.WriteLine($"피어 {peerId}와 연결 설정됨: {pair}");
        };
        
        // 피어 정보 저장
        _agents[peerId] = agent;
        _peers[peerId] = new GamePeer
        {
            PeerId = peerId,
            Agent = agent,
            ConnectionState = ICEConnectionState.New,
            LastActivity = DateTime.UtcNow
        };
        
        // ICE 프로세스 시작
        await agent.StartAsync();
        
        // 자격 증명 교환
        var credentialsMessage = CreateCredentialsMessage(agent.GetLocalUsername(), agent.GetLocalPassword());
        await _signalingChannel.SendSignalingMessage(peerId, credentialsMessage);
        
        return true;
    }
    
    private void HandleSignalingMessage(string fromPeerId, string message)
    {
        Console.WriteLine($"시그널링 수신 <- {fromPeerId}: {message[..Math.Min(message.Length, 50)]}...");
        
        if (!_agents.TryGetValue(fromPeerId, out var agent))
        {
            Console.WriteLine($"알 수 없는 피어: {fromPeerId}");
            return;
        }
        
        try
        {
            var messageObj = System.Text.Json.JsonSerializer.Deserialize<SignalingMessage>(message);
            
            switch (messageObj.Type)
            {
                case "credentials":
                    var creds = System.Text.Json.JsonSerializer.Deserialize<CredentialsData>(messageObj.Data);
                    agent.SetRemoteCredentials(creds.Username, creds.Password);
                    break;
                
                case "candidate":
                    var candidateData = System.Text.Json.JsonSerializer.Deserialize<CandidateData>(messageObj.Data);
                    agent.AddRemoteCandidateFromSdp(candidateData.Candidate);
                    break;
                
                case "gamedata":
                    var gameData = Convert.FromBase64String(messageObj.Data);
                    HandleGameData(fromPeerId, gameData);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"시그널링 메시지 처리 오류: {ex.Message}");
        }
    }
    
    private void UpdatePeerConnectionState(string peerId, ICEConnectionState state)
    {
        if (_peers.TryGetValue(peerId, out var peer))
        {
            peer.ConnectionState = state;
            peer.LastActivity = DateTime.UtcNow;
            
            OnPeerConnectionStateChanged?.Invoke(peerId, state);
        }
    }
    
    private void HandleGameData(string fromPeerId, byte[] data)
    {
        if (_peers.TryGetValue(fromPeerId, out var peer))
        {
            peer.BytesReceived += data.Length;
            peer.LastActivity = DateTime.UtcNow;
            
            OnGameDataReceived?.Invoke(fromPeerId, data);
        }
    }
    
    public async Task<bool> SendGameDataAsync(string peerId, byte[] data)
    {
        if (!_agents.TryGetValue(peerId, out var agent))
        {
            Console.WriteLine($"피어를 찾을 수 없습니다: {peerId}");
            return false;
        }
        
        if (agent.State == ICEConnectionState.Connected)
        {
            var success = await agent.SendDataAsync(data);
            
            if (success && _peers.TryGetValue(peerId, out var peer))
            {
                peer.BytesSent += data.Length;
                peer.LastActivity = DateTime.UtcNow;
            }
            
            return success;
        }
        else
        {
            // 연결이 안 되어 있으면 시그널링을 통해 전송
            var gameDataMessage = CreateGameDataMessage(data);
            await _signalingChannel.SendSignalingMessage(peerId, gameDataMessage);
            return true;
        }
    }
    
    public async Task BroadcastGameDataAsync(byte[] data, params string[] excludePeerIds)
    {
        var excludeSet = new HashSet<string>(excludePeerIds ?? Array.Empty<string>());
        var sendTasks = new List<Task>();
        
        foreach (var peerId in _peers.Keys)
        {
            if (!excludeSet.Contains(peerId))
            {
                sendTasks.Add(SendGameDataAsync(peerId, data));
            }
        }
        
        await Task.WhenAll(sendTasks);
    }
    
    public List<GamePeer> GetConnectedPeers()
    {
        return _peers.Values
            .Where(p => p.ConnectionState == ICEConnectionState.Connected)
            .ToList();
    }
    
    public void DisconnectPeer(string peerId)
    {
        if (_agents.TryGetValue(peerId, out var agent))
        {
            agent.Dispose();
            _agents.Remove(peerId);
        }
        
        _peers.Remove(peerId);
        Console.WriteLine($"피어 연결 해제: {peerId}");
    }
    
    private string CreateCredentialsMessage(string username, string password)
    {
        var message = new SignalingMessage
        {
            Type = "credentials",
            Data = System.Text.Json.JsonSerializer.Serialize(new CredentialsData
            {
                Username = username,
                Password = password
            })
        };
        
        return System.Text.Json.JsonSerializer.Serialize(message);
    }
    
    private string CreateCandidateMessage(ICECandidate candidate)
    {
        var message = new SignalingMessage
        {
            Type = "candidate",
            Data = System.Text.Json.JsonSerializer.Serialize(new CandidateData
            {
                Candidate = candidate.ToSdpString()
            })
        };
        
        return System.Text.Json.JsonSerializer.Serialize(message);
    }
    
    private string CreateGameDataMessage(byte[] data)
    {
        var message = new SignalingMessage
        {
            Type = "gamedata",
            Data = Convert.ToBase64String(data)
        };
        
        return System.Text.Json.JsonSerializer.Serialize(message);
    }
    
    public void Dispose()
    {
        foreach (var agent in _agents.Values)
        {
            agent.Dispose();
        }
        
        _agents.Clear();
        _peers.Clear();
    }
    
    // 시그널링 메시지 구조체들
    private class SignalingMessage
    {
        public string Type { get; set; }
        public string Data { get; set; }
    }
    
    private class CredentialsData
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    
    private class CandidateData
    {
        public string Candidate { get; set; }
    }
}

// ICE 게임 세션 사용 예제
public class ICEGameSessionDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("=== ICE 게임 세션 데모 ===\n");
        
        // 두 명의 플레이어 시뮬레이션
        var playerA = new ICEGameSessionManager();
        var playerB = new ICEGameSessionManager();
        
        // 이벤트 핸들러 설정
        playerA.OnPeerConnectionStateChanged += (peerId, state) =>
        {
            Console.WriteLine($"Player A: {peerId} 연결 상태 = {state}");
        };
        
        playerB.OnPeerConnectionStateChanged += (peerId, state) =>
        {
            Console.WriteLine($"Player B: {peerId} 연결 상태 = {state}");
        };
        
        playerA.OnGameDataReceived += (peerId, data) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine($"Player A 수신: {message}");
        };
        
        playerB.OnGameDataReceived += (peerId, data) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(data);
            Console.WriteLine($"Player B 수신: {message}");
        };
        
        // 교차 시그널링 설정 (실제로는 중앙 시그널링 서버를 통함)
        SetupCrossSignaling(playerA, playerB);
        
        // 연결 시작
        Console.WriteLine("P2P 연결 시작...\n");
        await playerA.ConnectToPeerAsync("PlayerB", asController: true);
        await playerB.ConnectToPeerAsync("PlayerA", asController: false);
        
        // 연결 설정 대기
        await Task.Delay(5000);
        
        // 게임 데이터 교환
        Console.WriteLine("\n게임 데이터 교환 시작...");
        for (int i = 0; i < 5; i++)
        {
            var messageA = $"Player A 메시지 #{i}";
            var messageB = $"Player B 메시지 #{i}";
            
            await playerA.SendGameDataAsync("PlayerB", System.Text.Encoding.UTF8.GetBytes(messageA));
            await playerB.SendGameDataAsync("PlayerA", System.Text.Encoding.UTF8.GetBytes(messageB));
            
            await Task.Delay(1000);
        }
        
        Console.WriteLine("\n=== 데모 완료 ===");
        
        playerA.Dispose();
        playerB.Dispose();
    }
    
    private static void SetupCrossSignaling(ICEGameSessionManager playerA, ICEGameSessionManager playerB)
    {
        // Player A의 시그널링 메시지를 Player B에게 전달
        var signalingFieldA = typeof(ICEGameSessionManager)
            .GetField("_signalingChannel", BindingFlags.NonPublic | BindingFlags.Instance);
        var channelA = signalingFieldA.GetValue(playerA) as ICEGameSessionManager.SignalingChannel;
        
        var signalingFieldB = typeof(ICEGameSessionManager)
            .GetField("_signalingChannel", BindingFlags.NonPublic | BindingFlags.Instance);
        var channelB = signalingFieldB.GetValue(playerB) as ICEGameSessionManager.SignalingChannel;
        
        // 메시지 릴레이 설정
        var originalSendA = channelA.GetType().GetMethod("SendSignalingMessage");
        var originalSendB = channelB.GetType().GetMethod("SendSignalingMessage");
        
        // 실제로는 리플렉션 대신 인터페이스를 통해 구현해야 함
        // 여기서는 시뮬레이션을 위한 간단한 구현
    }
}
```

이제 5장에서 ICE 프레임워크에 대해 포괄적으로 다뤘습니다. ICE의 개념과 후보자 수집부터 연결성 검사, 최적 경로 선택, 그리고 실제 게임에서의 통합 구현까지 실용적인 코드와 함께 설명했습니다. ICE는 현대 P2P 게임에서 가장 중요한 기술 중 하나이므로, 이러한 구현을 통해 안정적이고 효율적인 P2P 연결을 구축할 수 있습니다.  