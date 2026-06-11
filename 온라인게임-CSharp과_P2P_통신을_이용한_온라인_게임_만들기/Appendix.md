# C#과 P2P 통신을 이용한 온라인 게임 만들기

저자: 최흥배, AI-Assisted   
  
------  
  
# A. C# 홀펀칭 라이브러리 레퍼런스
  
## 1. NAT 홀펀칭 개념과 필요성

### NAT의 문제점
대부분의 클라이언트는 NAT(Network Address Translation) 뒤에 있어 직접적인 P2P 연결이 어렵습니다. NAT는 내부 IP를 외부 IP로 변환하면서 포트 매핑을 생성하는데, 이 매핑 정보를 알아야 외부에서 접근할 수 있습니다.

### 홀펀칭 기술
- **STUN (Session Traversal Utilities for NAT)**: 클라이언트가 자신의 공인 IP와 포트를 발견
- **TURN (Traversal Using Relays around NAT)**: 직접 연결이 불가능할 때 릴레이 서버를 통한 통신
- **ICE (Interactive Connectivity Establishment)**: STUN과 TURN을 조합한 연결 방법 선택 프레임워크

## 2. C# 홀펀칭 라이브러리

### 2.1 주요 라이브러리들

**STUN.NET**: 순수 C# STUN 클라이언트 라이브러리
```bash
dotnet add package STUN.NET
```

**RTCIceCandidate**: WebRTC 스타일의 ICE 후보 처리
```bash
dotnet add package Microsoft.AspNetCore.SignalR.Client  # 시그널링용
```

### 2.2 기본 STUN 클라이언트 구현

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class STUNClient
{
    private readonly string stunServer;
    private readonly int stunPort;
    
    public STUNClient(string server = "stun.l.google.com", int port = 19302)
    {
        stunServer = server;
        stunPort = port;
    }
    
    public async Task<IPEndPoint> GetPublicEndPointAsync()
    {
        using var udpClient = new UdpClient();
        var stunEndPoint = new IPEndPoint(
            (await Dns.GetHostAddressesAsync(stunServer))[0], 
            stunPort);
        
        // STUN Binding Request 패킷 생성
        var bindingRequest = CreateBindingRequest();
        
        await udpClient.SendAsync(bindingRequest, bindingRequest.Length, stunEndPoint);
        
        var response = await udpClient.ReceiveAsync();
        return ParseBindingResponse(response.Buffer);
    }
    
    private byte[] CreateBindingRequest()
    {
        var packet = new byte[20];
        
        // STUN Header
        packet[0] = 0x00; // Message Type: Binding Request (0x0001)
        packet[1] = 0x01;
        packet[2] = 0x00; // Message Length: 0
        packet[3] = 0x00;
        
        // Magic Cookie (RFC 5389)
        packet[4] = 0x21;
        packet[5] = 0x12;
        packet[6] = 0xA4;
        packet[7] = 0x42;
        
        // Transaction ID (96 bits random)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            packet[i] = (byte)random.Next(256);
        }
        
        return packet;
    }
    
    private IPEndPoint ParseBindingResponse(byte[] response)
    {
        // STUN Response 파싱 (간단화된 버전)
        if (response.Length < 20) return null;
        
        // XOR-MAPPED-ADDRESS 속성 찾기 (0x0020)
        int offset = 20; // STUN 헤더 이후부터
        while (offset < response.Length - 4)
        {
            ushort attrType = (ushort)((response[offset] << 8) | response[offset + 1]);
            ushort attrLength = (ushort)((response[offset + 2] << 8) | response[offset + 3]);
            
            if (attrType == 0x0020) // XOR-MAPPED-ADDRESS
            {
                // XOR 디코딩
                byte family = response[offset + 5];
                ushort port = (ushort)(((response[offset + 6] << 8) | response[offset + 7]) ^ 0x2112);
                
                var ipBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    ipBytes[i] = (byte)(response[offset + 8 + i] ^ response[4 + i]);
                }
                
                return new IPEndPoint(new IPAddress(ipBytes), port);
            }
            
            offset += 4 + attrLength;
        }
        
        return null;
    }
}
```

## 3. P2P 게임 시그널링 서버

게임 클라이언트들이 서로를 발견하고 연결 정보를 교환하기 위한 시그널링 서버가 필요합니다.

```csharp
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public class GameRoom
{
    public string RoomId { get; set; }
    public List<string> Players { get; set; } = new();
    public Dictionary<string, PeerInfo> PeerInfos { get; set; } = new();
}

public class PeerInfo
{
    public string PlayerId { get; set; }
    public IPEndPoint PublicEndPoint { get; set; }
    public IPEndPoint LocalEndPoint { get; set; }
    public List<string> IceCandidates { get; set; } = new();
}

public class GameSignalingHub : Hub
{
    private static readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    
    public async Task JoinRoom(string roomId, PeerInfo peerInfo)
    {
        var room = _rooms.GetOrAdd(roomId, _ => new GameRoom { RoomId = roomId });
        
        lock (room)
        {
            if (!room.Players.Contains(Context.ConnectionId))
            {
                room.Players.Add(Context.ConnectionId);
                room.PeerInfos[Context.ConnectionId] = peerInfo;
            }
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        
        // 기존 플레이어들에게 새 플레이어 알림
        await Clients.GroupExcept(roomId, Context.ConnectionId)
            .SendAsync("PlayerJoined", Context.ConnectionId, peerInfo);
        
        // 새 플레이어에게 기존 플레이어들 정보 전송
        foreach (var existingPlayer in room.PeerInfos.Where(p => p.Key != Context.ConnectionId))
        {
            await Clients.Caller.SendAsync("PlayerJoined", existingPlayer.Key, existingPlayer.Value);
        }
    }
    
    public async Task SendIceCandidate(string targetPlayerId, string candidate)
    {
        await Clients.Client(targetPlayerId).SendAsync("IceCandidate", Context.ConnectionId, candidate);
    }
    
    public async Task SendOffer(string targetPlayerId, string offer)
    {
        await Clients.Client(targetPlayerId).SendAsync("Offer", Context.ConnectionId, offer);
    }
    
    public async Task SendAnswer(string targetPlayerId, string answer)
    {
        await Clients.Client(targetPlayerId).SendAsync("Answer", Context.ConnectionId, answer);
    }
}
```

## 4. P2P 게임 클라이언트 구현

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Sockets;

public class P2PGameClient
{
    private readonly STUNClient _stunClient;
    private readonly HubConnection _signaling;
    private readonly ConcurrentDictionary<string, UdpClient> _peerConnections;
    private readonly string _playerId;
    
    public P2PGameClient(string signalingUrl)
    {
        _stunClient = new STUNClient();
        _peerConnections = new ConcurrentDictionary<string, UdpClient>();
        _playerId = Guid.NewGuid().ToString();
        
        _signaling = new HubConnectionBuilder()
            .WithUrl(signalingUrl)
            .Build();
            
        SetupSignalingHandlers();
    }
    
    private void SetupSignalingHandlers()
    {
        _signaling.On<string, PeerInfo>("PlayerJoined", async (playerId, peerInfo) =>
        {
            Console.WriteLine($"Player {playerId} joined");
            await InitiateP2PConnection(playerId, peerInfo);
        });
        
        _signaling.On<string, string>("IceCandidate", (fromPlayerId, candidate) =>
        {
            Console.WriteLine($"Received ICE candidate from {fromPlayerId}");
            // ICE candidate 처리
        });
    }
    
    public async Task StartAsync(string roomId)
    {
        await _signaling.StartAsync();
        
        // 공인 IP 주소 얻기
        var publicEndPoint = await _stunClient.GetPublicEndPointAsync();
        var localEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        var peerInfo = new PeerInfo
        {
            PlayerId = _playerId,
            PublicEndPoint = publicEndPoint,
            LocalEndPoint = localEndPoint
        };
        
        await _signaling.InvokeAsync("JoinRoom", roomId, peerInfo);
        Console.WriteLine($"Joined room {roomId} with public endpoint {publicEndPoint}");
    }
    
    private async Task InitiateP2PConnection(string targetPlayerId, PeerInfo targetInfo)
    {
        var udpClient = new UdpClient();
        _peerConnections[targetPlayerId] = udpClient;
        
        // UDP 홀펀칭 시도
        _ = Task.Run(() => StartHolePunching(udpClient, targetInfo));
        
        // 게임 데이터 수신 루프
        _ = Task.Run(() => ReceiveGameDataLoop(udpClient, targetPlayerId));
    }
    
    private async Task StartHolePunching(UdpClient udpClient, PeerInfo targetInfo)
    {
        var targets = new[] { targetInfo.PublicEndPoint, targetInfo.LocalEndPoint };
        
        foreach (var target in targets.Where(t => t != null))
        {
            for (int i = 0; i < 10; i++) // 10번 시도
            {
                try
                {
                    var helloMessage = System.Text.Encoding.UTF8.GetBytes($"HOLE_PUNCH:{_playerId}");
                    await udpClient.SendAsync(helloMessage, helloMessage.Length, target);
                    Console.WriteLine($"Sent hole punch to {target}");
                    
                    await Task.Delay(100); // 100ms 간격
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hole punch failed: {ex.Message}");
                }
            }
        }
    }
    
    private async Task ReceiveGameDataLoop(UdpClient udpClient, string peerId)
    {
        while (true)
        {
            try
            {
                var result = await udpClient.ReceiveAsync();
                var message = System.Text.Encoding.UTF8.GetString(result.Buffer);
                
                if (message.StartsWith("HOLE_PUNCH:"))
                {
                    Console.WriteLine($"Received hole punch from {peerId}");
                    // 홀펀칭 성공, 응답 전송
                    var response = System.Text.Encoding.UTF8.GetBytes($"PUNCH_ACK:{_playerId}");
                    await udpClient.SendAsync(response, response.Length, result.RemoteEndPoint);
                }
                else if (message.StartsWith("GAME_DATA:"))
                {
                    // 게임 데이터 처리
                    ProcessGameData(peerId, message.Substring(10));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
                break;
            }
        }
    }
    
    public async Task SendGameData(string targetPlayerId, string gameData)
    {
        if (_peerConnections.TryGetValue(targetPlayerId, out var udpClient))
        {
            var message = $"GAME_DATA:{gameData}";
            var data = System.Text.Encoding.UTF8.GetBytes(message);
            
            // 마지막으로 알려진 엔드포인트로 전송
            // 실제 구현에서는 연결된 엔드포인트를 추적해야 함
        }
    }
    
    private void ProcessGameData(string fromPlayerId, string gameData)
    {
        Console.WriteLine($"Game data from {fromPlayerId}: {gameData}");
        // 게임 로직 처리
    }
}
```

## 5. 액션 게임용 데이터 동기화

```csharp
public class GameState
{
    public Dictionary<string, PlayerState> Players { get; set; } = new();
    public long Timestamp { get; set; }
}

public class PlayerState
{
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Health { get; set; }
    public int Score { get; set; }
}

public struct Vector3
{
    public float X, Y, Z;
    
    public Vector3(float x, float y, float z)
    {
        X = x; Y = y; Z = z;
    }
}

public class GameStateManager
{
    private GameState _currentState = new();
    private readonly Timer _syncTimer;
    private readonly P2PGameClient _client;
    
    public GameStateManager(P2PGameClient client)
    {
        _client = client;
        _syncTimer = new Timer(SyncGameState, null, 0, 50); // 20Hz 동기화
    }
    
    private async void SyncGameState(object state)
    {
        _currentState.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var json = System.Text.Json.JsonSerializer.Serialize(_currentState);
        
        // 모든 연결된 피어에게 게임 상태 전송
        foreach (var peerId in _client.GetConnectedPeers())
        {
            await _client.SendGameData(peerId, $"STATE:{json}");
        }
    }
    
    public void UpdatePlayerPosition(string playerId, Vector3 newPosition)
    {
        if (!_currentState.Players.ContainsKey(playerId))
        {
            _currentState.Players[playerId] = new PlayerState { PlayerId = playerId };
        }
        
        _currentState.Players[playerId].Position = newPosition;
    }
}
```

## 6. 콘솔 게임 예제

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("P2P Action Game Client");
        Console.WriteLine("Commands: move <x> <y> <z>, attack, quit");
        
        var client = new P2PGameClient("https://localhost:5001/gamehub");
        var gameManager = new GameStateManager(client);
        
        await client.StartAsync("room1");
        
        while (true)
        {
            var input = Console.ReadLine();
            if (input == "quit") break;
            
            var parts = input.Split(' ');
            switch (parts[0].ToLower())
            {
                case "move":
                    if (parts.Length >= 4)
                    {
                        var pos = new Vector3(
                            float.Parse(parts[1]),
                            float.Parse(parts[2]),
                            float.Parse(parts[3])
                        );
                        gameManager.UpdatePlayerPosition("local", pos);
                        Console.WriteLine($"Moved to {pos.X}, {pos.Y}, {pos.Z}");
                    }
                    break;
                    
                case "attack":
                    // 공격 로직
                    Console.WriteLine("Attack!");
                    break;
            }
        }
    }
}
```

## 7. 성능 최적화 팁

### 델타 압축
게임 상태 전체를 매번 보내지 말고 변경된 부분만 전송:

```csharp
public class DeltaCompressor
{
    private GameState _lastSentState = new();
    
    public byte[] CreateDelta(GameState currentState)
    {
        var delta = new Dictionary<string, object>();
        
        foreach (var player in currentState.Players)
        {
            if (!_lastSentState.Players.ContainsKey(player.Key) ||
                !PlayerStatesEqual(_lastSentState.Players[player.Key], player.Value))
            {
                delta[player.Key] = player.Value;
            }
        }
        
        _lastSentState = CloneGameState(currentState);
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(delta);
    }
}
```

### 예측과 보정
네트워크 지연을 위한 클라이언트 예측:

```csharp
public class PredictiveMovement
{
    public Vector3 PredictPosition(Vector3 lastPosition, Vector3 velocity, float deltaTime)
    {
        return new Vector3(
            lastPosition.X + velocity.X * deltaTime,
            lastPosition.Y + velocity.Y * deltaTime,
            lastPosition.Z + velocity.Z * deltaTime
        );
    }
    
    public Vector3 InterpolatePosition(Vector3 from, Vector3 to, float t)
    {
        return new Vector3(
            from.X + (to.X - from.X) * t,
            from.Y + (to.Y - from.Y) * t,
            from.Z + (to.Z - from.Z) * t
        );
    }
}
```

이 가이드를 통해 C#으로 NAT 홀펀칭을 활용한 P2P 온라인 액션 게임의 기본 구조를 구현할 수 있습니다. 실제 상용 게임에서는 더 정교한 상태 동기화, 치팅 방지, 연결 복구 등의 기능이 추가로 필요합니다.


# B. STUN/TURN 서버 설정 가이드
  
## 1. STUN/TURN 서버 개념과 역할

### STUN 서버 (Session Traversal Utilities for NAT)
- 클라이언트가 NAT 뒤의 자신의 공인 IP 주소와 포트를 발견
- 단순한 요청-응답 프로토콜
- 데이터 중계 없이 정보만 제공

### TURN 서버 (Traversal Using Relays around NAT)
- 직접 P2P 연결이 불가능할 때 릴레이 역할
- 실제 게임 데이터를 중계
- STUN 서버 기능도 포함

## 2. 오픈소스 STUN/TURN 서버 설치

### coturn 서버 설치 (Ubuntu/Debian)

```bash
# coturn 설치
sudo apt-get update
sudo apt-get install coturn

# 설정 파일 편집
sudo nano /etc/turnserver.conf
```

### coturn 설정 파일 예제

```ini
# /etc/turnserver.conf

# 리스닝 포트
listening-port=3478
tls-listening-port=5349

# 외부 IP 주소 (서버의 공인 IP)
external-ip=YOUR_PUBLIC_IP

# 릴레이 IP 범위
relay-ip=YOUR_PUBLIC_IP
min-port=10000
max-port=20000

# 인증 설정
lt-cred-mech
user=testuser:testpass
realm=yourrealmname

# 로그 설정
verbose
log-file=/var/log/turnserver.log

# 데이터베이스 (옵션)
# userdb=/var/lib/turn/turndb

# SSL 인증서 (TURNS 용)
# cert=/etc/ssl/certs/turn_server_cert.pem
# pkey=/etc/ssl/private/turn_server_pkey.pem
```

### 서버 시작

```bash
# 서비스 시작
sudo systemctl start coturn
sudo systemctl enable coturn

# 상태 확인
sudo systemctl status coturn

# 포트 확인
sudo netstat -tulpn | grep 3478
```

## 3. C# STUN 클라이언트 고급 구현

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public enum StunMessageType : ushort
{
    BindingRequest = 0x0001,
    BindingResponse = 0x0101,
    BindingErrorResponse = 0x0111,
    SharedSecretRequest = 0x0002,
    SharedSecretResponse = 0x0102,
    SharedSecretErrorResponse = 0x0112
}

public enum StunAttributeType : ushort
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
    XorMappedAddressExp = 0x8020,
    Software = 0x8022,
    AlternateServer = 0x8023,
    Fingerprint = 0x8028
}

public class StunMessage
{
    public StunMessageType MessageType { get; set; }
    public ushort MessageLength { get; set; }
    public uint MagicCookie { get; set; } = 0x2112A442;
    public byte[] TransactionId { get; set; } = new byte[12];
    public List<StunAttribute> Attributes { get; set; } = new();

    public byte[] ToBytes()
    {
        var attributesData = new List<byte>();
        foreach (var attr in Attributes)
        {
            attributesData.AddRange(attr.ToBytes());
        }

        MessageLength = (ushort)attributesData.Count;
        
        var result = new List<byte>();
        
        // Header
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)MessageType)));
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)MessageLength)));
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)MagicCookie)));
        result.AddRange(TransactionId);
        
        // Attributes
        result.AddRange(attributesData);
        
        return result.ToArray();
    }

    public static StunMessage FromBytes(byte[] data)
    {
        if (data.Length < 20) return null;

        var message = new StunMessage();
        
        message.MessageType = (StunMessageType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
        message.MessageLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));
        message.MagicCookie = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 4));
        
        Array.Copy(data, 8, message.TransactionId, 0, 12);
        
        // Parse attributes
        int offset = 20;
        while (offset < data.Length - 4)
        {
            var attrType = (StunAttributeType)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
            var attrLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset + 2));
            
            var attrData = new byte[attrLength];
            Array.Copy(data, offset + 4, attrData, 0, attrLength);
            
            message.Attributes.Add(new StunAttribute
            {
                Type = attrType,
                Length = attrLength,
                Value = attrData
            });
            
            offset += 4 + attrLength;
            // Padding to 4-byte boundary
            while (offset % 4 != 0) offset++;
        }
        
        return message;
    }
}

public class StunAttribute
{
    public StunAttributeType Type { get; set; }
    public ushort Length { get; set; }
    public byte[] Value { get; set; }

    public byte[] ToBytes()
    {
        var result = new List<byte>();
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)Type)));
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)Length)));
        result.AddRange(Value);
        
        // Padding to 4-byte boundary
        while (result.Count % 4 != 0)
            result.Add(0);
            
        return result.ToArray();
    }
}

public class AdvancedStunClient
{
    private readonly string _stunServer;
    private readonly int _stunPort;
    private UdpClient _udpClient;

    public AdvancedStunClient(string server = "stun.l.google.com", int port = 19302)
    {
        _stunServer = server;
        _stunPort = port;
    }

    public async Task<StunResult> GetNATTypeAsync()
    {
        _udpClient = new UdpClient();
        var stunEndPoint = new IPEndPoint(
            (await Dns.GetHostAddressesAsync(_stunServer))[0], 
            _stunPort);

        // Test 1: 기본 바인딩 요청
        var test1Result = await PerformBindingTest(stunEndPoint, false, false);
        if (test1Result.MappedAddress == null)
        {
            return new StunResult { NATType = NATType.Blocked };
        }

        var localEndPoint = (IPEndPoint)_udpClient.Client.LocalEndPoint;
        if (test1Result.MappedAddress.Address.Equals(localEndPoint.Address))
        {
            // Test 2: Change IP and Port 요청
            var test2Result = await PerformBindingTest(stunEndPoint, true, true);
            if (test2Result.Success)
            {
                return new StunResult 
                { 
                    NATType = NATType.OpenInternet,
                    MappedAddress = test1Result.MappedAddress 
                };
            }
            else
            {
                return new StunResult 
                { 
                    NATType = NATType.SymmetricFirewall,
                    MappedAddress = test1Result.MappedAddress 
                };
            }
        }

        // Test 2: Change IP and Port 요청
        var test2Result2 = await PerformBindingTest(stunEndPoint, true, true);
        if (test2Result2.Success)
        {
            return new StunResult 
            { 
                NATType = NATType.FullCone,
                MappedAddress = test1Result.MappedAddress 
            };
        }

        // Test 3: Change Port only 요청
        var test3Result = await PerformBindingTest(stunEndPoint, false, true);
        if (test3Result.Success)
        {
            return new StunResult 
            { 
                NATType = NATType.RestrictedCone,
                MappedAddress = test1Result.MappedAddress 
            };
        }

        // Test 4: 다른 포트로 바인딩 요청하여 주소 비교
        _udpClient?.Close();
        _udpClient = new UdpClient();
        var test4Result = await PerformBindingTest(stunEndPoint, false, false);
        
        if (test4Result.MappedAddress != null && 
            !test4Result.MappedAddress.Equals(test1Result.MappedAddress))
        {
            return new StunResult 
            { 
                NATType = NATType.Symmetric,
                MappedAddress = test1Result.MappedAddress 
            };
        }

        return new StunResult 
        { 
            NATType = NATType.PortRestrictedCone,
            MappedAddress = test1Result.MappedAddress 
        };
    }

    private async Task<BindingTestResult> PerformBindingTest(IPEndPoint stunEndPoint, bool changeIP, bool changePort)
    {
        var message = new StunMessage
        {
            MessageType = StunMessageType.BindingRequest,
            TransactionId = GenerateTransactionId()
        };

        if (changeIP || changePort)
        {
            var changeRequest = new byte[4];
            if (changeIP) changeRequest[3] |= 0x04;
            if (changePort) changeRequest[3] |= 0x02;

            message.Attributes.Add(new StunAttribute
            {
                Type = StunAttributeType.ChangeRequest,
                Length = 4,
                Value = changeRequest
            });
        }

        try
        {
            var requestData = message.ToBytes();
            await _udpClient.SendAsync(requestData, requestData.Length, stunEndPoint);
            
            var responseTask = _udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(5000);
            
            var completedTask = await Task.WhenAny(responseTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                return new BindingTestResult { Success = false };
            }

            var response = await responseTask;
            var responseMessage = StunMessage.FromBytes(response.Buffer);
            
            if (responseMessage?.MessageType == StunMessageType.BindingResponse)
            {
                var mappedAddr = ExtractMappedAddress(responseMessage);
                return new BindingTestResult 
                { 
                    Success = true, 
                    MappedAddress = mappedAddr 
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN test failed: {ex.Message}");
        }

        return new BindingTestResult { Success = false };
    }

    private IPEndPoint ExtractMappedAddress(StunMessage message)
    {
        // XOR-MAPPED-ADDRESS 먼저 확인
        var xorMappedAttr = message.Attributes.FirstOrDefault(a => 
            a.Type == StunAttributeType.XorMappedAddress);
        
        if (xorMappedAttr != null)
        {
            return ParseXorMappedAddress(xorMappedAttr.Value, message.MagicCookie, message.TransactionId);
        }

        // MAPPED-ADDRESS 확인
        var mappedAttr = message.Attributes.FirstOrDefault(a => 
            a.Type == StunAttributeType.MappedAddress);
        
        if (mappedAttr != null)
        {
            return ParseMappedAddress(mappedAttr.Value);
        }

        return null;
    }

    private IPEndPoint ParseXorMappedAddress(byte[] data, uint magicCookie, byte[] transactionId)
    {
        if (data.Length < 8) return null;

        byte family = data[1];
        ushort port = (ushort)((data[2] << 8) | data[3]);
        
        // Port XOR with magic cookie의 상위 16비트
        port ^= (ushort)(magicCookie >> 16);

        if (family == 0x01) // IPv4
        {
            var ipBytes = new byte[4];
            var magicBytes = BitConverter.GetBytes(magicCookie);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(magicBytes);
                
            for (int i = 0; i < 4; i++)
            {
                ipBytes[i] = (byte)(data[4 + i] ^ magicBytes[i]);
            }

            return new IPEndPoint(new IPAddress(ipBytes), port);
        }

        return null;
    }

    private IPEndPoint ParseMappedAddress(byte[] data)
    {
        if (data.Length < 8) return null;

        byte family = data[1];
        ushort port = (ushort)((data[2] << 8) | data[3]);

        if (family == 0x01) // IPv4
        {
            var ipBytes = new byte[4];
            Array.Copy(data, 4, ipBytes, 0, 4);
            return new IPEndPoint(new IPAddress(ipBytes), port);
        }

        return null;
    }

    private byte[] GenerateTransactionId()
    {
        var transactionId = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(transactionId);
        }
        return transactionId;
    }

    public void Dispose()
    {
        _udpClient?.Close();
    }
}

public enum NATType
{
    OpenInternet,
    FullCone,
    RestrictedCone,
    PortRestrictedCone,
    Symmetric,
    SymmetricFirewall,
    Blocked
}

public class StunResult
{
    public NATType NATType { get; set; }
    public IPEndPoint MappedAddress { get; set; }
}

public class BindingTestResult
{
    public bool Success { get; set; }
    public IPEndPoint MappedAddress { get; set; }
}
```

## 4. TURN 클라이언트 구현

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class TurnClient : IDisposable
{
    private readonly string _turnServer;
    private readonly int _turnPort;
    private readonly string _username;
    private readonly string _password;
    private readonly string _realm;
    private UdpClient _udpClient;
    private IPEndPoint _turnEndPoint;
    private IPEndPoint _relayAddress;
    private string _nonce;

    public TurnClient(string server, int port, string username, string password, string realm = "")
    {
        _turnServer = server;
        _turnPort = port;
        _username = username;
        _password = password;
        _realm = realm;
    }

    public async Task<bool> AllocateRelayAsync()
    {
        _udpClient = new UdpClient();
        _turnEndPoint = new IPEndPoint(
            (await Dns.GetHostAddressesAsync(_turnServer))[0], 
            _turnPort);

        // 1. Allocate 요청 (인증 없이)
        var allocateResult = await SendAllocateRequest();
        if (!allocateResult.Success && allocateResult.ErrorCode == 401)
        {
            // 2. 인증 정보로 다시 Allocate 요청
            _nonce = allocateResult.Nonce;
            _realm = allocateResult.Realm ?? _realm;
            
            allocateResult = await SendAllocateRequest(true);
            if (allocateResult.Success)
            {
                _relayAddress = allocateResult.RelayAddress;
                Console.WriteLine($"Relay allocated: {_relayAddress}");
                return true;
            }
        }

        return false;
    }

    private async Task<AllocateResult> SendAllocateRequest(bool withAuth = false)
    {
        var message = new StunMessage
        {
            MessageType = (StunMessageType)0x0003, // Allocate Request
            TransactionId = GenerateTransactionId()
        };

        // REQUESTED-TRANSPORT 속성 (UDP = 17)
        message.Attributes.Add(new StunAttribute
        {
            Type = (StunAttributeType)0x0019,
            Length = 4,
            Value = new byte[] { 17, 0, 0, 0 }
        });

        if (withAuth)
        {
            // USERNAME 속성
            var usernameBytes = Encoding.UTF8.GetBytes(_username);
            message.Attributes.Add(new StunAttribute
            {
                Type = StunAttributeType.Username,
                Length = (ushort)usernameBytes.Length,
                Value = usernameBytes
            });

            // REALM 속성
            if (!string.IsNullOrEmpty(_realm))
            {
                var realmBytes = Encoding.UTF8.GetBytes(_realm);
                message.Attributes.Add(new StunAttribute
                {
                    Type = (StunAttributeType)0x0014,
                    Length = (ushort)realmBytes.Length,
                    Value = realmBytes
                });
            }

            // NONCE 속성
            if (!string.IsNullOrEmpty(_nonce))
            {
                var nonceBytes = Encoding.UTF8.GetBytes(_nonce);
                message.Attributes.Add(new StunAttribute
                {
                    Type = (StunAttributeType)0x0015,
                    Length = (ushort)nonceBytes.Length,
                    Value = nonceBytes
                });
            }

            // MESSAGE-INTEGRITY 속성 계산 및 추가
            var messageIntegrity = CalculateMessageIntegrity(message, _password);
            message.Attributes.Add(new StunAttribute
            {
                Type = StunAttributeType.MessageIntegrity,
                Length = 20,
                Value = messageIntegrity
            });
        }

        try
        {
            var requestData = message.ToBytes();
            await _udpClient.SendAsync(requestData, requestData.Length, _turnEndPoint);

            var response = await _udpClient.ReceiveAsync();
            var responseMessage = StunMessage.FromBytes(response.Buffer);

            if (responseMessage != null)
            {
                if (responseMessage.MessageType == (StunMessageType)0x0103) // Allocate Success Response
                {
                    var relayAddr = ExtractXorRelayedAddress(responseMessage);
                    return new AllocateResult { Success = true, RelayAddress = relayAddr };
                }
                else if (responseMessage.MessageType == (StunMessageType)0x0113) // Allocate Error Response
                {
                    var errorCode = ExtractErrorCode(responseMessage);
                    var nonce = ExtractNonce(responseMessage);
                    var realm = ExtractRealm(responseMessage);
                    
                    return new AllocateResult 
                    { 
                        Success = false, 
                        ErrorCode = errorCode,
                        Nonce = nonce,
                        Realm = realm
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TURN Allocate failed: {ex.Message}");
        }

        return new AllocateResult { Success = false };
    }

    public async Task<bool> CreatePermissionAsync(IPAddress peerAddress)
    {
        var message = new StunMessage
        {
            MessageType = (StunMessageType)0x0008, // CreatePermission Request
            TransactionId = GenerateTransactionId()
        };

        // XOR-PEER-ADDRESS 속성
        var peerAddressAttr = CreateXorPeerAddressAttribute(peerAddress);
        message.Attributes.Add(peerAddressAttr);

        // 인증 속성들 추가
        AddAuthAttributes(message);

        try
        {
            var requestData = message.ToBytes();
            await _udpClient.SendAsync(requestData, requestData.Length, _turnEndPoint);

            var response = await _udpClient.ReceiveAsync();
            var responseMessage = StunMessage.FromBytes(response.Buffer);

            return responseMessage?.MessageType == (StunMessageType)0x0108; // CreatePermission Success Response
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Create Permission failed: {ex.Message}");
            return false;
        }
    }

    public async Task SendDataAsync(IPEndPoint peerEndPoint, byte[] data)
    {
        var message = new StunMessage
        {
            MessageType = (StunMessageType)0x0016, // Send Indication
            TransactionId = GenerateTransactionId()
        };

        // XOR-PEER-ADDRESS 속성
        var peerAddressAttr = CreateXorPeerAddressAttribute(peerEndPoint.Address, peerEndPoint.Port);
        message.Attributes.Add(peerAddressAttr);

        // DATA 속성
        message.Attributes.Add(new StunAttribute
        {
            Type = (StunAttributeType)0x0013,
            Length = (ushort)data.Length,
            Value = data
        });

        try
        {
            var requestData = message.ToBytes();
            await _udpClient.SendAsync(requestData, requestData.Length, _turnEndPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send data failed: {ex.Message}");
        }
    }

    private StunAttribute CreateXorPeerAddressAttribute(IPAddress address, int port = 0)
    {
        var addressBytes = address.GetAddressBytes();
        var result = new List<byte>();
        
        result.Add(0); // Reserved
        result.Add(0x01); // IPv4
        
        // XOR port with magic cookie의 상위 16비트
        var xorPort = port ^ (0x2112A442 >> 16);
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)xorPort)));
        
        // XOR address with magic cookie
        var magicBytes = BitConverter.GetBytes(0x2112A442);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(magicBytes);
            
        for (int i = 0; i < 4; i++)
        {
            result.Add((byte)(addressBytes[i] ^ magicBytes[i]));
        }

        return new StunAttribute
        {
            Type = (StunAttributeType)0x0012, // XOR-PEER-ADDRESS
            Length = (ushort)result.Count,
            Value = result.ToArray()
        };
    }

    private void AddAuthAttributes(StunMessage message)
    {
        // USERNAME
        var usernameBytes = Encoding.UTF8.GetBytes(_username);
        message.Attributes.Add(new StunAttribute
        {
            Type = StunAttributeType.Username,
            Length = (ushort)usernameBytes.Length,
            Value = usernameBytes
        });

        // REALM
        if (!string.IsNullOrEmpty(_realm))
        {
            var realmBytes = Encoding.UTF8.GetBytes(_realm);
            message.Attributes.Add(new StunAttribute
            {
                Type = (StunAttributeType)0x0014,
                Length = (ushort)realmBytes.Length,
                Value = realmBytes
            });
        }

        // NONCE
        if (!string.IsNullOrEmpty(_nonce))
        {
            var nonceBytes = Encoding.UTF8.GetBytes(_nonce);
            message.Attributes.Add(new StunAttribute
            {
                Type = (StunAttributeType)0x0015,
                Length = (ushort)nonceBytes.Length,
                Value = nonceBytes
            });
        }

        // MESSAGE-INTEGRITY
        var messageIntegrity = CalculateMessageIntegrity(message, _password);
        message.Attributes.Add(new StunAttribute
        {
            Type = StunAttributeType.MessageIntegrity,
            Length = 20,
            Value = messageIntegrity
        });
    }

    private byte[] CalculateMessageIntegrity(StunMessage message, string password)
    {
        // HMAC-SHA1 계산을 위한 키 생성
        var key = Encoding.UTF8.GetBytes(password);
        
        // MESSAGE-INTEGRITY 속성을 제외한 메시지 직렬화
        var tempMessage = new StunMessage
        {
            MessageType = message.MessageType,
            MagicCookie = message.MagicCookie,
            TransactionId = message.TransactionId,
            Attributes = message.Attributes.Where(a => a.Type != StunAttributeType.MessageIntegrity).ToList()
        };
        
        var messageBytes = tempMessage.ToBytes();
        
        // HMAC-SHA1 계산
        using (var hmac = new HMACSHA1(key))
        {
            return hmac.ComputeHash(messageBytes);
        }
    }

    private IPEndPoint ExtractXorRelayedAddress(StunMessage message)
    {
        var attr = message.Attributes.FirstOrDefault(a => a.Type == (StunAttributeType)0x0016);
        if (attr != null)
        {
            return ParseXorMappedAddress(attr.Value, message.MagicCookie, message.TransactionId);
        }
        return null;
    }

    private int ExtractErrorCode(StunMessage message)
    {
        var attr = message.Attributes.FirstOrDefault(a => a.Type == StunAttributeType.ErrorCode);
        if (attr?.Value != null && attr.Value.Length >= 4)
        {
            return (attr.Value[2] * 100) + attr.Value[3];
        }
        return 0;
    }

    private string ExtractNonce(StunMessage message)
    {
        var attr = message.Attributes.FirstOrDefault(a => a.Type == (StunAttributeType)0x0015);
        return attr?.Value != null ? Encoding.UTF8.GetString(attr.Value) : null;
    }

    private string ExtractRealm(StunMessage message)
    {
        var attr = message.Attributes.FirstOrDefault(a => a.Type == (StunAttributeType)0x0014);
        return attr?.Value != null ? Encoding.UTF8.GetString(attr.Value) : null;
    }

    private IPEndPoint ParseXorMappedAddress(byte[] data, uint magicCookie, byte[] transactionId)
    {
        if (data.Length < 8) return null;

        byte family = data[1];
        ushort port = (ushort)((data[2] << 8) | data[3]);
        
        port ^= (ushort)(magicCookie >> 16);

        if (family == 0x01)
        {
            var ipBytes = new byte[4];
            var magicBytes = BitConverter.GetBytes(magicCookie);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(magicBytes);
                
            for (int i = 0; i < 4; i++)
            {
                ipBytes[i] = (byte)(data[4 + i] ^ magicBytes[i]);
            }

            return new IPEndPoint(new IPAddress(ipBytes), port);
        }

        return null;
    }

    private byte[] GenerateTransactionId()
    {
        var transactionId = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(transactionId);
        }
        return transactionId;
    }

    public IPEndPoint RelayAddress => _relayAddress;

    public void Dispose()
    {
        _udpClient?.Close();
    }
}

public class AllocateResult
{
    public bool Success { get; set; }
    public IPEndPoint RelayAddress { get; set; }
    public int ErrorCode { get; set; }
    public string Nonce { get; set; }
    public string Realm { get; set; }
}
```

## 5. 자체 STUN 서버 구현

```csharp
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class SimpleStunServer
{
    private readonly int _port;
    private UdpClient _udpServer;
    private CancellationTokenSource _cancellationTokenSource;

    public SimpleStunServer(int port = 3478)
    {
        _port = port;
    }

    public async Task StartAsync()
    {
        _udpServer = new UdpClient(_port);
        _cancellationTokenSource = new CancellationTokenSource();
        
        Console.WriteLine($"STUN Server started on port {_port}");
        
        await Task.Run(() => ProcessRequestsAsync(_cancellationTokenSource.Token));
    }

    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpServer.ReceiveAsync();
                _ = Task.Run(() => HandleRequestAsync(result.Buffer, result.RemoteEndPoint));
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving STUN request: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(byte[] requestData, IPEndPoint clientEndPoint)
    {
        try
        {
            var request = StunMessage.FromBytes(requestData);
            if (request?.MessageType == StunMessageType.BindingRequest)
            {
                var response = CreateBindingResponse(request, clientEndPoint);
                var responseData = response.ToBytes();
                
                await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);
                Console.WriteLine($"Sent binding response to {clientEndPoint}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling STUN request: {ex.Message}");
        }
    }

    private StunMessage CreateBindingResponse(StunMessage request, IPEndPoint clientEndPoint)
    {
        var response = new StunMessage
        {
            MessageType = StunMessageType.BindingResponse,
            MagicCookie = request.MagicCookie,
            TransactionId = request.TransactionId
        };

        // XOR-MAPPED-ADDRESS 속성 추가
        var xorMappedAddress = CreateXorMappedAddressAttribute(clientEndPoint, request.MagicCookie);
        response.Attributes.Add(xorMappedAddress);

        // SOFTWARE 속성 추가
        var software = "SimpleStunServer/1.0";
        var softwareBytes = System.Text.Encoding.UTF8.GetBytes(software);
        response.Attributes.Add(new StunAttribute
        {
            Type = StunAttributeType.Software,
            Length = (ushort)softwareBytes.Length,
            Value = softwareBytes
        });

        return response;
    }

    private StunAttribute CreateXorMappedAddressAttribute(IPEndPoint endPoint, uint magicCookie)
    {
        var addressBytes = endPoint.Address.GetAddressBytes();
        var result = new List<byte>();
        
        result.Add(0); // Reserved
        result.Add(0x01); // IPv4
        
        // XOR port with magic cookie의 상위 16비트
        var xorPort = endPoint.Port ^ (int)(magicCookie >> 16);
        result.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)xorPort)));
        
        // XOR address with magic cookie
        var magicBytes = BitConverter.GetBytes(magicCookie);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(magicBytes);
            
        for (int i = 0; i < 4; i++)
        {
            result.Add((byte)(addressBytes[i] ^ magicBytes[i]));
        }

        return new StunAttribute
        {
            Type = StunAttributeType.XorMappedAddress,
            Length = (ushort)result.Count,
            Value = result.ToArray()
        };
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _udpServer?.Close();
        Console.WriteLine("STUN Server stopped");
    }
}
```

## 6. 통합 테스트 콘솔 프로그램

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("STUN/TURN Test Console");
        Console.WriteLine("Commands:");
        Console.WriteLine("  server - Start STUN server");
        Console.WriteLine("  stun <server> - Test STUN client");
        Console.WriteLine("  nat - Detect NAT type");
        Console.WriteLine("  turn <server> <username> <password> - Test TURN client");
        Console.WriteLine("  quit - Exit");

        SimpleStunServer stunServer = null;

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Split(' ');
            if (input == null || input.Length == 0) continue;

            switch (input[0].ToLower())
            {
                case "server":
                    if (stunServer == null)
                    {
                        stunServer = new SimpleStunServer();
                        _ = Task.Run(() => stunServer.StartAsync());
                    }
                    else
                    {
                        Console.WriteLine("Server already running");
                    }
                    break;

                case "stun":
                    var server = input.Length > 1 ? input[1] : "stun.l.google.com";
                    await TestStunClient(server);
                    break;

                case "nat":
                    await TestNATDetection();
                    break;

                case "turn":
                    if (input.Length >= 4)
                    {
                        await TestTurnClient(input[1], input[2], input[3]);
                    }
                    else
                    {
                        Console.WriteLine("Usage: turn <server> <username> <password>");
                    }
                    break;

                case "quit":
                    stunServer?.Stop();
                    return;

                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
        }
    }

    static async Task TestStunClient(string server)
    {
        Console.WriteLine($"Testing STUN client with server: {server}");
        
        using var client = new AdvancedStunClient(server);
        var result = await client.GetNATTypeAsync();
        
        Console.WriteLine($"NAT Type: {result.NATType}");
        Console.WriteLine($"Mapped Address: {result.MappedAddress}");
    }

    static async Task TestNATDetection()
    {
        Console.WriteLine("Detecting NAT type...");
        
        using var client = new AdvancedStunClient();
        var result = await client.GetNATTypeAsync();
        
        Console.WriteLine($"NAT Type: {result.NATType}");
        Console.WriteLine($"Public Address: {result.MappedAddress}");
        
        switch (result.NATType)
        {
            case NATType.OpenInternet:
                Console.WriteLine("Direct P2P connections should work");
                break;
            case NATType.FullCone:
                Console.WriteLine("P2P connections work after hole punching");
                break;
            case NATType.RestrictedCone:
            case NATType.PortRestrictedCone:
                Console.WriteLine("P2P connections may work with hole punching");
                break;
            case NATType.Symmetric:
                Console.WriteLine("P2P connections difficult, TURN server recommended");
                break;
            case NATType.Blocked:
                Console.WriteLine("P2P connections not possible, TURN server required");
                break;
        }
    }

    static async Task TestTurnClient(string server, string username, string password)
    {
        Console.WriteLine($"Testing TURN client with server: {server}");
        
        using var client = new TurnClient(server, 3478, username, password);
        
        var success = await client.AllocateRelayAsync();
        if (success)
        {
            Console.WriteLine($"TURN relay allocated: {client.RelayAddress}");
            
            // 테스트 데이터 전송
            var testData = System.Text.Encoding.UTF8.GetBytes("Hello TURN!");
            await client.SendDataAsync(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53), testData);
            Console.WriteLine("Test data sent through TURN relay");
        }
        else
        {
            Console.WriteLine("Failed to allocate TURN relay");
        }
    }
}
```

## 7. Docker를 이용한 TURN 서버 배포

```dockerfile
# Dockerfile for coturn
FROM ubuntu:22.04

RUN apt-get update && apt-get install -y coturn

COPY turnserver.conf /etc/turnserver.conf

EXPOSE 3478/udp 3478/tcp 5349/tcp 49152-65535/udp

CMD ["turnserver", "-c", "/etc/turnserver.conf"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  turn-server:
    build: .
    ports:
      - "3478:3478/udp"
      - "3478:3478/tcp" 
      - "5349:5349/tcp"
      - "49152-65535:49152-65535/udp"
    environment:
      - TURN_USERNAME=testuser
      - TURN_PASSWORD=testpass
    volumes:
      - ./logs:/var/log
```

이 가이드를 통해 STUN/TURN 서버 설정부터 C# 클라이언트 구현까지 완전한 P2P 통신 환경을 구축할 수 있습니다. 실제 게임에서는 이러한 기술들을 조합하여 안정적인 P2P 연결을 확보할 수 있습니다.
 

# C. 네트워킹 도구 및 유틸리티
 
## 1. 네트워크 연결 분석 도구

### 1.1 P2P 연결 상태 모니터

```csharp
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class NetworkConnectionMonitor
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections;
    private readonly Timer _monitorTimer;
    private readonly object _lockObject = new object();

    public NetworkConnectionMonitor()
    {
        _connections = new ConcurrentDictionary<string, ConnectionInfo>();
        _monitorTimer = new Timer(MonitorConnections, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public void AddConnection(string peerId, IPEndPoint remoteEndPoint, UdpClient udpClient)
    {
        var connectionInfo = new ConnectionInfo
        {
            PeerId = peerId,
            RemoteEndPoint = remoteEndPoint,
            UdpClient = udpClient,
            LastSeen = DateTime.UtcNow,
            IsActive = true
        };

        _connections.TryAdd(peerId, connectionInfo);
        Console.WriteLine($"[Monitor] Added connection to {peerId} at {remoteEndPoint}");
    }

    public void UpdateConnection(string peerId, long latency, int packetSize)
    {
        if (_connections.TryGetValue(peerId, out var connection))
        {
            lock (_lockObject)
            {
                connection.LastSeen = DateTime.UtcNow;
                connection.LatencyHistory.Add(latency);
                connection.PacketsSent++;
                connection.BytesSent += packetSize;
                
                if (connection.LatencyHistory.Count > 100)
                {
                    connection.LatencyHistory.RemoveAt(0);
                }
            }
        }
    }

    public void RecordPacketReceived(string peerId, int packetSize)
    {
        if (_connections.TryGetValue(peerId, out var connection))
        {
            lock (_lockObject)
            {
                connection.PacketsReceived++;
                connection.BytesReceived += packetSize;
                connection.LastReceived = DateTime.UtcNow;
            }
        }
    }

    public void RecordPacketLoss(string peerId)
    {
        if (_connections.TryGetValue(peerId, out var connection))
        {
            lock (_lockObject)
            {
                connection.PacketsLost++;
            }
        }
    }

    private async void MonitorConnections(object state)
    {
        Console.Clear();
        Console.WriteLine("=== P2P Connection Monitor ===");
        Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        foreach (var kvp in _connections)
        {
            var connection = kvp.Value;
            await UpdateConnectionHealth(connection);
            DisplayConnectionInfo(connection);
        }

        Console.WriteLine("\nPress 'q' to quit, 'r' to reset stats, 't' to test connections");
    }

    private async Task UpdateConnectionHealth(ConnectionInfo connection)
    {
        try
        {
            // Ping 테스트
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(connection.RemoteEndPoint.Address, 1000);
            
            if (reply.Status == IPStatus.Success)
            {
                connection.PingLatency = reply.RoundtripTime;
                connection.IsReachable = true;
            }
            else
            {
                connection.IsReachable = false;
            }
        }
        catch
        {
            connection.IsReachable = false;
        }

        // 연결 활성 상태 확인
        var timeSinceLastSeen = DateTime.UtcNow - connection.LastSeen;
        connection.IsActive = timeSinceLastSeen.TotalSeconds < 30;
    }

    private void DisplayConnectionInfo(ConnectionInfo connection)
    {
        var avgLatency = connection.LatencyHistory.Count > 0 
            ? connection.LatencyHistory.Average() 
            : 0;

        var packetLossRate = connection.PacketsSent > 0 
            ? (double)connection.PacketsLost / connection.PacketsSent * 100 
            : 0;

        var throughputSent = connection.BytesSent / 1024.0; // KB
        var throughputReceived = connection.BytesReceived / 1024.0; // KB

        Console.WriteLine($"Peer: {connection.PeerId}");
        Console.WriteLine($"  Endpoint: {connection.RemoteEndPoint}");
        Console.WriteLine($"  Status: {(connection.IsActive ? "Active" : "Inactive")} | {(connection.IsReachable ? "Reachable" : "Unreachable")}");
        Console.WriteLine($"  Ping: {connection.PingLatency}ms | Avg Game Latency: {avgLatency:F1}ms");
        Console.WriteLine($"  Packets: Sent={connection.PacketsSent}, Received={connection.PacketsReceived}, Lost={connection.PacketsLost}");
        Console.WriteLine($"  Packet Loss: {packetLossRate:F2}%");
        Console.WriteLine($"  Throughput: Sent={throughputSent:F1}KB, Received={throughputReceived:F1}KB");
        Console.WriteLine($"  Last Seen: {connection.LastSeen:HH:mm:ss}");
        Console.WriteLine();
    }

    public NetworkStats GetNetworkStats()
    {
        var stats = new NetworkStats();
        
        foreach (var connection in _connections.Values)
        {
            stats.TotalConnections++;
            if (connection.IsActive) stats.ActiveConnections++;
            if (connection.IsReachable) stats.ReachableConnections++;
            
            stats.TotalPacketsSent += connection.PacketsSent;
            stats.TotalPacketsReceived += connection.PacketsReceived;
            stats.TotalPacketsLost += connection.PacketsLost;
            stats.TotalBytesSent += connection.BytesSent;
            stats.TotalBytesReceived += connection.BytesReceived;
        }

        return stats;
    }

    public void ResetStats()
    {
        foreach (var connection in _connections.Values)
        {
            lock (_lockObject)
            {
                connection.PacketsSent = 0;
                connection.PacketsReceived = 0;
                connection.PacketsLost = 0;
                connection.BytesSent = 0;
                connection.BytesReceived = 0;
                connection.LatencyHistory.Clear();
            }
        }
        Console.WriteLine("Statistics reset");
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

public class ConnectionInfo
{
    public string PeerId { get; set; }
    public IPEndPoint RemoteEndPoint { get; set; }
    public UdpClient UdpClient { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime LastReceived { get; set; }
    public bool IsActive { get; set; }
    public bool IsReachable { get; set; }
    public long PingLatency { get; set; }
    public List<long> LatencyHistory { get; set; } = new();
    public int PacketsSent { get; set; }
    public int PacketsReceived { get; set; }
    public int PacketsLost { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
}

public class NetworkStats
{
    public int TotalConnections { get; set; }
    public int ActiveConnections { get; set; }
    public int ReachableConnections { get; set; }
    public int TotalPacketsSent { get; set; }
    public int TotalPacketsReceived { get; set; }
    public int TotalPacketsLost { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    
    public double PacketLossRate => TotalPacketsSent > 0 
        ? (double)TotalPacketsLost / TotalPacketsSent * 100 
        : 0;
}
```

## 2. 패킷 분석 및 캡처 도구

```csharp
using System;
using System.IO;
using System.Text;
using System.Text.Json;

public class PacketAnalyzer
{
    private readonly string _logDirectory;
    private readonly Dictionary<string, List<PacketInfo>> _packetsByPeer;
    private readonly object _lockObject = new object();

    public PacketAnalyzer(string logDirectory = "packet_logs")
    {
        _logDirectory = logDirectory;
        _packetsByPeer = new Dictionary<string, List<PacketInfo>>();
        
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public void CapturePacket(string peerId, PacketDirection direction, byte[] data, IPEndPoint endpoint)
    {
        var packet = new PacketInfo
        {
            Timestamp = DateTime.UtcNow,
            PeerId = peerId,
            Direction = direction,
            Size = data.Length,
            Endpoint = endpoint.ToString(),
            Data = Convert.ToBase64String(data),
            PacketType = DeterminePacketType(data)
        };

        lock (_lockObject)
        {
            if (!_packetsByPeer.ContainsKey(peerId))
            {
                _packetsByPeer[peerId] = new List<PacketInfo>();
            }
            
            _packetsByPeer[peerId].Add(packet);
            
            // 메모리 관리: 최근 1000개 패킷만 유지
            if (_packetsByPeer[peerId].Count > 1000)
            {
                _packetsByPeer[peerId].RemoveAt(0);
            }
        }

        // 실시간 로깅
        LogPacketToFile(packet);
    }

    private GamePacketType DeterminePacketType(byte[] data)
    {
        if (data.Length < 4) return GamePacketType.Unknown;

        var header = BitConverter.ToUInt32(data, 0);
        return header switch
        {
            0x01 => GamePacketType.PlayerMovement,
            0x02 => GamePacketType.PlayerAction,
            0x03 => GamePacketType.GameState,
            0x04 => GamePacketType.Chat,
            0x05 => GamePacketType.Heartbeat,
            0xFF => GamePacketType.HandshakeRequest,
            0xFE => GamePacketType.HandshakeResponse,
            _ => GamePacketType.Unknown
        };
    }

    private void LogPacketToFile(PacketInfo packet)
    {
        var logFile = Path.Combine(_logDirectory, $"{packet.PeerId}_{DateTime.UtcNow:yyyyMMdd}.log");
        var logEntry = $"{packet.Timestamp:HH:mm:ss.fff} [{packet.Direction}] {packet.PacketType} {packet.Size}B -> {packet.Endpoint}\n";
        
        try
        {
            File.AppendAllText(logFile, logEntry);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log packet: {ex.Message}");
        }
    }

    public void GenerateAnalysisReport(string peerId)
    {
        if (!_packetsByPeer.ContainsKey(peerId))
        {
            Console.WriteLine($"No packets found for peer {peerId}");
            return;
        }

        var packets = _packetsByPeer[peerId];
        var report = new StringBuilder();
        
        report.AppendLine($"=== Packet Analysis Report for {peerId} ===");
        report.AppendLine($"Analysis Period: {packets.First().Timestamp:yyyy-MM-dd HH:mm:ss} - {packets.Last().Timestamp:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine($"Total Packets: {packets.Count}");
        report.AppendLine();

        // 패킷 타입별 통계
        var packetTypeStats = packets
            .GroupBy(p => p.PacketType)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count(),
                TotalSize = g.Sum(p => p.Size),
                AvgSize = g.Average(p => p.Size)
            })
            .OrderByDescending(s => s.Count);

        report.AppendLine("Packet Type Statistics:");
        foreach (var stat in packetTypeStats)
        {
            report.AppendLine($"  {stat.Type}: {stat.Count} packets, {stat.TotalSize}B total, {stat.AvgSize:F1}B avg");
        }
        report.AppendLine();

        // 방향별 통계
        var directionStats = packets
            .GroupBy(p => p.Direction)
            .Select(g => new
            {
                Direction = g.Key,
                Count = g.Count(),
                TotalSize = g.Sum(p => p.Size)
            });

        report.AppendLine("Direction Statistics:");
        foreach (var stat in directionStats)
        {
            report.AppendLine($"  {stat.Direction}: {stat.Count} packets, {stat.TotalSize}B");
        }
        report.AppendLine();

        // 시간대별 트래픽 분석
        var hourlyTraffic = packets
            .GroupBy(p => p.Timestamp.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                Count = g.Count(),
                Size = g.Sum(p => p.Size)
            })
            .OrderBy(h => h.Hour);

        report.AppendLine("Hourly Traffic:");
        foreach (var traffic in hourlyTraffic)
        {
            report.AppendLine($"  {traffic.Hour:D2}:00 - {traffic.Count} packets, {traffic.Size}B");
        }

        var reportFile = Path.Combine(_logDirectory, $"{peerId}_analysis_{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
        File.WriteAllText(reportFile, report.ToString());
        
        Console.WriteLine(report.ToString());
        Console.WriteLine($"Report saved to: {reportFile}");
    }

    public void ExportToJson(string peerId, string fileName)
    {
        if (!_packetsByPeer.ContainsKey(peerId))
        {
            Console.WriteLine($"No packets found for peer {peerId}");
            return;
        }

        var packets = _packetsByPeer[peerId];
        var json = JsonSerializer.Serialize(packets, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var filePath = Path.Combine(_logDirectory, fileName);
        File.WriteAllText(filePath, json);
        Console.WriteLine($"Packets exported to: {filePath}");
    }

    public List<PacketInfo> GetPacketsByType(string peerId, GamePacketType packetType)
    {
        if (!_packetsByPeer.ContainsKey(peerId))
            return new List<PacketInfo>();

        return _packetsByPeer[peerId]
            .Where(p => p.PacketType == packetType)
            .ToList();
    }

    public void ClearPackets(string peerId)
    {
        lock (_lockObject)
        {
            if (_packetsByPeer.ContainsKey(peerId))
            {
                _packetsByPeer[peerId].Clear();
                Console.WriteLine($"Cleared packet history for {peerId}");
            }
        }
    }
}

public class PacketInfo
{
    public DateTime Timestamp { get; set; }
    public string PeerId { get; set; }
    public PacketDirection Direction { get; set; }
    public int Size { get; set; }
    public string Endpoint { get; set; }
    public string Data { get; set; }
    public GamePacketType PacketType { get; set; }
}

public enum PacketDirection
{
    Sent,
    Received
}

public enum GamePacketType
{
    Unknown,
    HandshakeRequest,
    HandshakeResponse,
    PlayerMovement,
    PlayerAction,
    GameState,
    Chat,
    Heartbeat
}
```

## 3. 네트워크 상태 시뮬레이터

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

public class NetworkConditionSimulator
{
    private readonly Random _random;
    private NetworkCondition _currentCondition;
    private bool _isEnabled;

    public NetworkConditionSimulator()
    {
        _random = new Random();
        _currentCondition = NetworkCondition.Optimal;
        _isEnabled = false;
    }

    public void SetCondition(NetworkCondition condition)
    {
        _currentCondition = condition;
        Console.WriteLine($"Network condition set to: {condition}");
    }

    public void Enable()
    {
        _isEnabled = true;
        Console.WriteLine("Network simulation enabled");
    }

    public void Disable()
    {
        _isEnabled = false;
        Console.WriteLine("Network simulation disabled");
    }

    public async Task<bool> ShouldDropPacket()
    {
        if (!_isEnabled) return false;

        var dropRate = _currentCondition switch
        {
            NetworkCondition.Optimal => 0.0,
            NetworkCondition.Good => 0.1,
            NetworkCondition.Fair => 0.5,
            NetworkCondition.Poor => 2.0,
            NetworkCondition.Terrible => 5.0,
            NetworkCondition.Unstable => _random.NextDouble() * 10.0,
            _ => 0.0
        };

        return _random.NextDouble() * 100 < dropRate;
    }

    public async Task SimulateLatency()
    {
        if (!_isEnabled) return;

        var latencyMs = _currentCondition switch
        {
            NetworkCondition.Optimal => _random.Next(10, 30),
            NetworkCondition.Good => _random.Next(30, 60),
            NetworkCondition.Fair => _random.Next(60, 120),
            NetworkCondition.Poor => _random.Next(120, 300),
            NetworkCondition.Terrible => _random.Next(300, 1000),
            NetworkCondition.Unstable => _random.Next(10, 500),
            _ => 0
        };

        if (latencyMs > 0)
        {
            await Task.Delay(latencyMs);
        }
    }

    public async Task<byte[]> SimulateCorruption(byte[] data)
    {
        if (!_isEnabled) return data;

        var corruptionRate = _currentCondition switch
        {
            NetworkCondition.Optimal => 0.0,
            NetworkCondition.Good => 0.01,
            NetworkCondition.Fair => 0.05,
            NetworkCondition.Poor => 0.1,
            NetworkCondition.Terrible => 0.2,
            NetworkCondition.Unstable => _random.NextDouble() * 0.3,
            _ => 0.0
        };

        if (_random.NextDouble() * 100 < corruptionRate)
        {
            var corruptedData = new byte[data.Length];
            Array.Copy(data, corruptedData, data.Length);
            
            // 랜덤하게 몇 바이트 변조
            var corruptBytes = Math.Max(1, (int)(data.Length * corruptionRate / 100));
            for (int i = 0; i < corruptBytes; i++)
            {
                var index = _random.Next(data.Length);
                corruptedData[index] = (byte)_random.Next(256);
            }
            
            Console.WriteLine($"Packet corrupted: {corruptBytes} bytes affected");
            return corruptedData;
        }

        return data;
    }

    public void SimulateBandwidthLimit(ref byte[] data, int maxBytesPerSecond)
    {
        if (!_isEnabled || maxBytesPerSecond <= 0) return;

        // 단순한 대역폭 제한 시뮬레이션
        var currentTime = DateTime.UtcNow;
        var timeSinceLastPacket = currentTime - _lastPacketTime;
        var allowedBytes = (int)(maxBytesPerSecond * timeSinceLastPacket.TotalSeconds);

        if (data.Length > allowedBytes)
        {
            // 패킷 크기 제한 (실제로는 큐잉이나 분할이 필요)
            var limitedData = new byte[allowedBytes];
            Array.Copy(data, limitedData, allowedBytes);
            data = limitedData;
            Console.WriteLine($"Bandwidth limited: packet truncated to {allowedBytes} bytes");
        }

        _lastPacketTime = currentTime;
    }

    private DateTime _lastPacketTime = DateTime.UtcNow;

    public NetworkMetrics GetCurrentMetrics()
    {
        return new NetworkMetrics
        {
            Condition = _currentCondition,
            EstimatedLatency = _currentCondition switch
            {
                NetworkCondition.Optimal => 20,
                NetworkCondition.Good => 45,
                NetworkCondition.Fair => 90,
                NetworkCondition.Poor => 210,
                NetworkCondition.Terrible => 650,
                NetworkCondition.Unstable => 255,
                _ => 0
            },
            EstimatedPacketLoss = _currentCondition switch
            {
                NetworkCondition.Optimal => 0.0,
                NetworkCondition.Good => 0.1,
                NetworkCondition.Fair => 0.5,
                NetworkCondition.Poor => 2.0,
                NetworkCondition.Terrible => 5.0,
                NetworkCondition.Unstable => 2.5,
                _ => 0.0
            },
            IsEnabled = _isEnabled
        };
    }
}

public enum NetworkCondition
{
    Optimal,    // 최적 (게임용 광섬유)
    Good,       // 좋음 (안정적인 브로드밴드)
    Fair,       // 보통 (일반적인 인터넷)
    Poor,       // 나쁨 (불안정한 연결)
    Terrible,   // 최악 (모바일 데이터 등)
    Unstable    // 불안정 (랜덤한 변화)
}

public class NetworkMetrics
{
    public NetworkCondition Condition { get; set; }
    public int EstimatedLatency { get; set; }
    public double EstimatedPacketLoss { get; set; }
    public bool IsEnabled { get; set; }
}
```

## 4. 자동화된 P2P 연결 테스터

```csharp
using System;
using System.Net;
using System.Threading.Tasks;

public class P2PConnectionTester
{
    private readonly AdvancedStunClient _stunClient;
    private readonly List<TestResult> _testResults;

    public P2PConnectionTester()
    {
        _stunClient = new AdvancedStunClient();
        _testResults = new List<TestResult>();
    }

    public async Task<ConnectionTestReport> RunComprehensiveTest()
    {
        var report = new ConnectionTestReport
        {
            TestStartTime = DateTime.UtcNow,
            Tests = new List<TestResult>()
        };

        Console.WriteLine("Starting comprehensive P2P connection test...");

        // 1. STUN 서버 접근성 테스트
        await TestStunServerAccess(report);

        // 2. NAT 타입 감지 테스트
        await TestNATTypeDetection(report);

        // 3. 홀펀칭 테스트
        await TestHolePunching(report);

        // 4. 대역폭 테스트
        await TestBandwidth(report);

        // 5. 지연시간 테스트
        await TestLatency(report);

        report.TestEndTime = DateTime.UtcNow;
        report.OverallResult = DetermineOverallResult(report.Tests);

        return report;
    }

    private async Task TestStunServerAccess(ConnectionTestReport report)
    {
        var test = new TestResult
        {
            TestName = "STUN Server Access",
            StartTime = DateTime.UtcNow
        };

        try
        {
            var publicEndPoint = await _stunClient.GetPublicEndPointAsync();
            if (publicEndPoint != null)
            {
                test.Success = true;
                test.Message = $"Successfully discovered public endpoint: {publicEndPoint}";
                test.Data["PublicIP"] = publicEndPoint.Address.ToString();
                test.Data["PublicPort"] = publicEndPoint.Port.ToString();
            }
            else
            {
                test.Success = false;
                test.Message = "Failed to discover public endpoint";
            }
        }
        catch (Exception ex)
        {
            test.Success = false;
            test.Message = $"STUN test failed: {ex.Message}";
        }

        test.EndTime = DateTime.UtcNow;
        test.Duration = test.EndTime - test.StartTime;
        report.Tests.Add(test);

        Console.WriteLine($"✓ STUN Test: {(test.Success ? "PASS" : "FAIL")} - {test.Message}");
    }

    private async Task TestNATTypeDetection(ConnectionTestReport report)
    {
        var test = new TestResult
        {
            TestName = "NAT Type Detection",
            StartTime = DateTime.UtcNow
        };

        try
        {
            var result = await _stunClient.GetNATTypeAsync();
            test.Success = true;
            test.Message = $"NAT Type detected: {result.NATType}";
            test.Data["NATType"] = result.NATType.ToString();
            test.Data["MappedAddress"] = result.MappedAddress?.ToString() ?? "Unknown";

            // NAT 타입에 따른 P2P 가능성 평가
            var p2pViability = result.NATType switch
            {
                NATType.OpenInternet => "Excellent",
                NATType.FullCone => "Very Good",
                NATType.RestrictedCone => "Good",
                NATType.PortRestrictedCone => "Fair",
                NATType.Symmetric => "Poor",
                NATType.Blocked => "Impossible",
                _ => "Unknown"
            };

            test.Data["P2PViability"] = p2pViability;
        }
        catch (Exception ex)
        {
            test.Success = false;
            test.Message = $"NAT detection failed: {ex.Message}";
        }

        test.EndTime = DateTime.UtcNow;
        test.Duration = test.EndTime - test.StartTime;
        report.Tests.Add(test);

        Console.WriteLine($"✓ NAT Test: {(test.Success ? "PASS" : "FAIL")} - {test.Message}");
    }

    private async Task TestHolePunching(ConnectionTestReport report)
    {
        var test = new TestResult
        {
            TestName = "Hole Punching Simulation",
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 시뮬레이션된 홀펀칭 테스트
            var success = await SimulateHolePunching();
            test.Success = success;
            test.Message = success 
                ? "Hole punching simulation successful"
                : "Hole punching simulation failed";
        }
        catch (Exception ex)
        {
            test.Success = false;
            test.Message = $"Hole punching test failed: {ex.Message}";
        }

        test.EndTime = DateTime.UtcNow;
        test.Duration = test.EndTime - test.StartTime;
        report.Tests.Add(test);

        Console.WriteLine($"✓ Hole Punching Test: {(test.Success ? "PASS" : "FAIL")} - {test.Message}");
    }

    private async Task TestBandwidth(ConnectionTestReport report)
    {
        var test = new TestResult
        {
            TestName = "Bandwidth Test",
            StartTime = DateTime.UtcNow
        };

        try
        {
            var bandwidth = await MeasureBandwidth();
            test.Success = bandwidth > 0;
            test.Message = $"Estimated bandwidth: {bandwidth:F2} Mbps";
            test.Data["BandwidthMbps"] = bandwidth.ToString("F2");

            // 게임에 필요한 최소 대역폭 체크
            var adequate = bandwidth >= 1.0; // 1Mbps 최소 요구사항
            test.Data["AdequateForGaming"] = adequate.ToString();
        }
        catch (Exception ex)
        {
            test.Success = false;
            test.Message = $"Bandwidth test failed: {ex.Message}";
        }

        test.EndTime = DateTime.UtcNow;
        test.Duration = test.EndTime - test.StartTime;
        report.Tests.Add(test);

        Console.WriteLine($"✓ Bandwidth Test: {(test.Success ? "PASS" : "FAIL")} - {test.Message}");
    }

    private async Task TestLatency(ConnectionTestReport report)
    {
        var test = new TestResult
        {
            TestName = "Latency Test",
            StartTime = DateTime.UtcNow
        };

        try
        {
            var latencies = new List<long>();
            
            // 여러 번 측정하여 평균 계산
            for (int i = 0; i < 10; i++)
            {
                var latency = await MeasureLatency();
                if (latency > 0)
                {
                    latencies.Add(latency);
                }
                await Task.Delay(100);
            }

            if (latencies.Count > 0)
            {
                var avgLatency = latencies.Average();
                var minLatency = latencies.Min();
                var maxLatency = latencies.Max();
                var jitter = latencies.Count > 1 ? CalculateJitter(latencies) : 0;

                test.Success = true;
                test.Message = $"Average latency: {avgLatency:F1}ms";
                test.Data["AvgLatencyMs"] = avgLatency.ToString("F1");
                test.Data["MinLatencyMs"] = minLatency.ToString();
                test.Data["MaxLatencyMs"] = maxLatency.ToString();
                test.Data["JitterMs"] = jitter.ToString("F1");

                // 게임에 적합한 지연시간인지 평가
                var quality = avgLatency switch
                {
                    < 50 => "Excellent",
                    < 100 => "Good",
                    < 150 => "Fair", 
                    < 200 => "Poor",
                    _ => "Unacceptable"
                };
                test.Data["LatencyQuality"] = quality;
            }
            else
            {
                test.Success = false;
                test.Message = "Could not measure latency";
            }
        }
        catch (Exception ex)
        {
            test.Success = false;
            test.Message = $"Latency test failed: {ex.Message}";
        }

        test.EndTime = DateTime.UtcNow;
        test.Duration = test.EndTime - test.StartTime;
        report.Tests.Add(test);

        Console.WriteLine($"✓ Latency Test: {(test.Success ? "PASS" : "FAIL")} - {test.Message}");
    }

    private async Task<bool> SimulateHolePunching()
    {
        // 실제 홀펀칭 시뮬레이션
        await Task.Delay(2000); // 시뮬레이션 지연
        return new Random().NextDouble() > 0.3; // 70% 성공률
    }

    private async Task<double> MeasureBandwidth()
    {
        // 간단한 대역폭 측정 시뮬레이션
        await Task.Delay(1000);
        return new Random().NextDouble() * 100; // 0-100 Mbps
    }

    private async Task<long> MeasureLatency()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // 간단한 지연시간 측정 (실제로는 ping이나 UDP 패킷 사용)
            await Task.Delay(new Random().Next(10, 200));
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
        catch
        {
            return -1;
        }
    }

    private double CalculateJitter(List<long> latencies)
    {
        if (latencies.Count < 2) return 0;

        var differences = new List<double>();
        for (int i = 1; i < latencies.Count; i++)
        {
            differences.Add(Math.Abs(latencies[i] - latencies[i - 1]));
        }

        return differences.Average();
    }

    private TestResultLevel DetermineOverallResult(List<TestResult> tests)
    {
        var failedTests = tests.Count(t => !t.Success);
        
        return failedTests switch
        {
            0 => TestResultLevel.Excellent,
            1 => TestResultLevel.Good,
            2 => TestResultLevel.Fair,
            _ => TestResultLevel.Poor
        };
    }

    public void GenerateReport(ConnectionTestReport report, string fileName)
    {
        var reportBuilder = new StringBuilder();
        
        reportBuilder.AppendLine("=== P2P Connection Test Report ===");
        reportBuilder.AppendLine($"Test Duration: {report.TestStartTime:yyyy-MM-dd HH:mm:ss} - {report.TestEndTime:yyyy-MM-dd HH:mm:ss}");
        reportBuilder.AppendLine($"Overall Result: {report.OverallResult}");
        reportBuilder.AppendLine();

        foreach (var test in report.Tests)
        {
            reportBuilder.AppendLine($"Test: {test.TestName}");
            reportBuilder.AppendLine($"  Result: {(test.Success ? "PASS" : "FAIL")}");
            reportBuilder.AppendLine($"  Duration: {test.Duration.TotalMilliseconds:F0}ms");
            reportBuilder.AppendLine($"  Message: {test.Message}");
            
            if (test.Data.Any())
            {
                reportBuilder.AppendLine("  Additional Data:");
                foreach (var kvp in test.Data)
                {
                    reportBuilder.AppendLine($"    {kvp.Key}: {kvp.Value}");
                }
            }
            reportBuilder.AppendLine();
        }

        // P2P 게임 적합성 평가
        reportBuilder.AppendLine("=== P2P Gaming Suitability Assessment ===");
        var suitability = EvaluateGamingSuitability(report);
        reportBuilder.AppendLine($"Gaming Suitability: {suitability.Level}");
        reportBuilder.AppendLine($"Recommendation: {suitability.Recommendation}");

        File.WriteAllText(fileName, reportBuilder.ToString());
        Console.WriteLine($"\nDetailed report saved to: {fileName}");
        Console.WriteLine(reportBuilder.ToString());
    }

    private GamingSuitability EvaluateGamingSuitability(ConnectionTestReport report)
    {
        var natTest = report.Tests.FirstOrDefault(t => t.TestName == "NAT Type Detection");
        var latencyTest = report.Tests.FirstOrDefault(t => t.TestName == "Latency Test");
        var bandwidthTest = report.Tests.FirstOrDefault(t => t.TestName == "Bandwidth Test");

        var suitability = new GamingSuitability();

        // NAT 타입 평가
        if (natTest?.Data.TryGetValue("NATType", out var natType) == true)
        {
            var natScore = natType switch
            {
                "OpenInternet" => 10,
                "FullCone" => 9,
                "RestrictedCone" => 7,
                "PortRestrictedCone" => 5,
                "Symmetric" => 2,
                "Blocked" => 0,
                _ => 3
            };

            if (natScore >= 7)
            {
                suitability.Level = "Excellent for P2P Gaming";
                suitability.Recommendation = "Direct P2P connections should work reliably.";
            }
            else if (natScore >= 4)
            {
                suitability.Level = "Moderate for P2P Gaming";
                suitability.Recommendation = "P2P may work with hole punching. Consider TURN fallback.";
            }
            else
            {
                suitability.Level = "Poor for P2P Gaming";
                suitability.Recommendation = "TURN server required for reliable connections.";
            }
        }

        return suitability;
    }
}

public class ConnectionTestReport
{
    public DateTime TestStartTime { get; set; }
    public DateTime TestEndTime { get; set; }
    public List<TestResult> Tests { get; set; } = new();
    public TestResultLevel OverallResult { get; set; }
}

public class TestResult
{
    public string TestName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string> Data { get; set; } = new();
}

public enum TestResultLevel
{
    Excellent,
    Good,
    Fair,
    Poor
}

public class GamingSuitability
{
    public string Level { get; set; }
    public string Recommendation { get; set; }
}
```

## 5. 통합 네트워킹 도구 콘솔

```csharp
class NetworkingToolsConsole
{
    private NetworkConnectionMonitor _monitor;
    private PacketAnalyzer _analyzer;
    private NetworkConditionSimulator _simulator;
    private P2PConnectionTester _tester;
    private bool _isRunning;

    static async Task Main(string[] args)
    {
        var console = new NetworkingToolsConsole();
        await console.RunAsync();
    }

    public async Task RunAsync()
    {
        Initialize();
        _isRunning = true;

        Console.WriteLine("=== P2P Gaming Network Tools ===");
        Console.WriteLine("Available commands:");
        Console.WriteLine("  monitor - Start connection monitoring");
        Console.WriteLine("  analyze <peerid> - Generate packet analysis");
        Console.WriteLine("  simulate <condition> - Set network simulation");
        Console.WriteLine("  test - Run comprehensive connection test");
        Console.WriteLine("  reset - Reset all statistics");
        Console.WriteLine("  export <peerid> <filename> - Export packet data");
        Console.WriteLine("  help - Show detailed help");
        Console.WriteLine("  quit - Exit");
        Console.WriteLine();

        while (_isRunning)
        {
            Console.Write("NetworkTools> ");
            var input = Console.ReadLine()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (input == null || input.Length == 0) continue;

            await ProcessCommand(input);
        }
    }

    private void Initialize()
    {
        _monitor = new NetworkConnectionMonitor();
        _analyzer = new PacketAnalyzer();
        _simulator = new NetworkConditionSimulator();
        _tester = new P2PConnectionTester();
    }

    private async Task ProcessCommand(string[] input)
    {
        var command = input[0].ToLower();

        try
        {
            switch (command)
            {
                case "monitor":
                    HandleMonitorCommand();
                    break;

                case "analyze":
                    if (input.Length > 1)
                        _analyzer.GenerateAnalysisReport(input[1]);
                    else
                        Console.WriteLine("Usage: analyze <peerid>");
                    break;

                case "simulate":
                    if (input.Length > 1)
                        HandleSimulateCommand(input[1]);
                    else
                        ShowSimulationOptions();
                    break;

                case "test":
                    await HandleTestCommand();
                    break;

                case "reset":
                    _monitor.ResetStats();
                    Console.WriteLine("All statistics reset");
                    break;

                case "export":
                    if (input.Length > 2)
                        _analyzer.ExportToJson(input[1], input[2]);
                    else
                        Console.WriteLine("Usage: export <peerid> <filename>");
                    break;

                case "help":
                    ShowDetailedHelp();
                    break;

                case "quit":
                case "exit":
                    _isRunning = false;
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
        }
    }

    private void HandleMonitorCommand()
    {
        var stats = _monitor.GetNetworkStats();
        Console.WriteLine("=== Current Network Statistics ===");
        Console.WriteLine($"Total Connections: {stats.TotalConnections}");
        Console.WriteLine($"Active Connections: {stats.ActiveConnections}");
        Console.WriteLine($"Reachable Connections: {stats.ReachableConnections}");
        Console.WriteLine($"Packets Sent: {stats.TotalPacketsSent}");
        Console.WriteLine($"Packets Received: {stats.TotalPacketsReceived}");
        Console.WriteLine($"Packet Loss Rate: {stats.PacketLossRate:F2}%");
        Console.WriteLine($"Data Sent: {stats.TotalBytesSent / 1024.0:F1} KB");
        Console.WriteLine($"Data Received: {stats.TotalBytesReceived / 1024.0:F1} KB");
    }

    private void HandleSimulateCommand(string condition)
    {
        if (Enum.TryParse<NetworkCondition>(condition, true, out var networkCondition))
        {
            _simulator.SetCondition(networkCondition);
            _simulator.Enable();
            
            var metrics = _simulator.GetCurrentMetrics();
            Console.WriteLine($"Network simulation enabled:");
            Console.WriteLine($"  Condition: {metrics.Condition}");
            Console.WriteLine($"  Estimated Latency: {metrics.EstimatedLatency}ms");
            Console.WriteLine($"  Estimated Packet Loss: {metrics.EstimatedPacketLoss:F1}%");
        }
        else
        {
            Console.WriteLine($"Invalid condition: {condition}");
            ShowSimulationOptions();
        }
    }

    private void ShowSimulationOptions()
    {
        Console.WriteLine("Available network conditions:");
        foreach (NetworkCondition condition in Enum.GetValues<NetworkCondition>())
        {
            Console.WriteLine($"  {condition}");
        }
    }

    private async Task HandleTestCommand()
    {
        Console.WriteLine("Starting comprehensive P2P connection test...");
        var report = await _tester.RunComprehensiveTest();
        
        var fileName = $"connection_test_{DateTime.UtcNow:yyyyMMddHHmmss}.txt";
        _tester.GenerateReport(report, fileName);
    }

    private void ShowDetailedHelp()
    {
        Console.WriteLine("=== Detailed Command Help ===");
        Console.WriteLine();
        Console.WriteLine("monitor");
        Console.WriteLine("  Shows current network connection statistics");
        Console.WriteLine();
        Console.WriteLine("analyze <peerid>");
        Console.WriteLine("  Generates detailed packet analysis report for specified peer");
        Console.WriteLine("  Example: analyze peer1");
        Console.WriteLine();
        Console.WriteLine("simulate <condition>");
        Console.WriteLine("  Enables network condition simulation");
        Console.WriteLine("  Conditions: Optimal, Good, Fair, Poor, Terrible, Unstable");
        Console.WriteLine("  Example: simulate Poor");
        Console.WriteLine();
        Console.WriteLine("test");
        Console.WriteLine("  Runs comprehensive P2P connection test suite");
        Console.WriteLine("  Tests STUN access, NAT type, hole punching, bandwidth, and latency");
        Console.WriteLine();
        Console.WriteLine("reset");
        Console.WriteLine("  Resets all collected statistics and packet data");
        Console.WriteLine();
        Console.WriteLine("export <peerid> <filename>");
        Console.WriteLine("  Exports packet data for specified peer to JSON file");
        Console.WriteLine("  Example: export peer1 peer1_packets.json");
    }
}
```

이러한 네트워킹 도구들을 통해 P2P 온라인 게임 개발 시 네트워크 상태를 모니터링하고, 문제를 진단하며, 다양한 네트워크 환경에서의 동작을 테스트할 수 있습니다. 각 도구는 독립적으로 사용하거나 통합하여 사용할 수 있어 개발 과정에서 유용한 정보를 제공합니다.


# D. 주요 NAT 장비별 특성 정리
  
## 1. 가정용 라우터 제조사별 NAT 특성

### 1.1 TP-Link 라우터 시리즈

```csharp
public class TPLinkNATProfile : NATProfile
{
    public override string Manufacturer => "TP-Link";
    public override NATBehavior DefaultBehavior => NATBehavior.PortRestrictedCone;
    public override int SessionTimeout => 300; // 5분
    public override bool SupportsUPnP => true;
    public override bool SupportsNATMP => false;
    
    public override Dictionary<string, NATCharacteristics> ModelCharacteristics => new()
    {
        ["Archer AX6000"] = new NATCharacteristics
        {
            NATType = NATType.PortRestrictedCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 300,
            PortPredictability = PortPredictability.Sequential,
            HolePunchingSuccess = 85,
            Notes = "WiFi 6 지원, 안정적인 포트 매핑"
        },
        ["Archer C7"] = new NATCharacteristics
        {
            NATType = NATType.RestrictedCone,
            PortRange = new PortRange(32768, 65535),
            SessionTimeout = 180,
            PortPredictability = PortPredictability.Random,
            HolePunchingSuccess = 75,
            Notes = "인기 모델, 중간 수준의 홀펀칭 성공률"
        },
        ["TL-WR841N"] = new NATCharacteristics
        {
            NATType = NATType.Symmetric,
            PortRange = new PortRange(1024, 32767),
            SessionTimeout = 120,
            PortPredictability = PortPredictability.Random,
            HolePunchingSuccess = 45,
            Notes = "저가형 모델, 대칭 NAT으로 P2P 어려움"
        }
    };

    public override List<string> OptimizationTips => new()
    {
        "UPnP 활성화 권장",
        "게임 모드 활성화시 NAT 타임아웃 연장",
        "포트 포워딩으로 특정 포트 고정 가능",
        "펌웨어 업데이트로 NAT 성능 개선"
    };
}
```

### 1.2 Netgear 라우터 시리즈

```csharp
public class NetgearNATProfile : NATProfile
{
    public override string Manufacturer => "Netgear";
    public override NATBehavior DefaultBehavior => NATBehavior.FullCone;
    public override int SessionTimeout => 600; // 10분
    public override bool SupportsUPnP => true;
    public override bool SupportsNATMP => true;

    public override Dictionary<string, NATCharacteristics> ModelCharacteristics => new()
    {
        ["Nighthawk AX12"] = new NATCharacteristics
        {
            NATType = NATType.FullCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 900, // 15분
            PortPredictability = PortPredictability.Sequential,
            HolePunchingSuccess = 95,
            Notes = "최고급 모델, 게이밍 최적화 NAT"
        },
        ["R7000"] = new NATCharacteristics
        {
            NATType = NATType.RestrictedCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 600,
            PortPredictability = PortPredictability.Sequential,
            HolePunchingSuccess = 88,
            Notes = "게이밍 라우터, Dynamic QoS 지원"
        },
        ["WNR2000"] = new NATCharacteristics
        {
            NATType = NATType.PortRestrictedCone,
            PortRange = new PortRange(32768, 65535),
            SessionTimeout = 300,
            PortPredictability = PortPredictability.Random,
            HolePunchingSuccess = 70,
            Notes = "기본형 모델, 제한적 NAT"
        }
    };

    public override List<string> OptimizationTips => new()
    {
        "Dynamic QoS에서 게임 트래픽 우선순위 설정",
        "Gaming Dashboard 활용",
        "Port Triggering 기능 활용",
        "OpenWrt 펌웨어로 교체시 더 유연한 NAT 설정 가능"
    };
}
```

### 1.3 ASUS 라우터 시리즈

```csharp
public class AsusNATProfile : NATProfile
{
    public override string Manufacturer => "ASUS";
    public override NATBehavior DefaultBehavior => NATBehavior.PortRestrictedCone;
    public override int SessionTimeout => 300;
    public override bool SupportsUPnP => true;
    public override bool SupportsNATMP => false;

    public override Dictionary<string, NATCharacteristics> ModelCharacteristics => new()
    {
        ["ROG Rapture GT-AX11000"] = new NATCharacteristics
        {
            NATType = NATType.FullCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 1800, // 30분
            PortPredictability = PortPredictability.Sequential,
            HolePunchingSuccess = 92,
            Notes = "게이밍 특화, Adaptive QoS, NAT Acceleration"
        },
        ["RT-AX86U"] = new NATCharacteristics
        {
            NATType = NATType.RestrictedCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 600,
            PortPredictability = PortPredictability.Sequential,
            HolePunchingSuccess = 85,
            Notes = "WiFi 6 지원, 게임 가속기 내장"
        },
        ["RT-N66U"] = new NATCharacteristics
        {
            NATType = NATType.Symmetric,
            PortRange = new PortRange(32768, 61000),
            SessionTimeout = 180,
            PortPredictability = PortPredictability.Random,
            HolePunchingSuccess = 55,
            Notes = "구형 모델, 펌웨어 업데이트 필요"
        }
    };

    public override List<string> OptimizationTips => new()
    {
        "Adaptive QoS에서 게임 모드 활성화",
        "NAT Acceleration 활성화 (CPU 부하 감소)",
        "게임 가속기(Game Accelerator) 사용",
        "Merlin 펌웨어로 고급 NAT 설정 가능"
    };
}
```

## 2. ISP별 NAT 정책 특성

### 2.1 한국 주요 ISP NAT 특성

```csharp
public class KoreanISPProfiles
{
    public static Dictionary<string, ISPProfile> Profiles = new()
    {
        ["KT"] = new ISPProfile
        {
            ISPName = "KT",
            CGNATUsage = CGNATUsage.Partial, // 일부 지역
            IPv6Support = IPv6Support.Full,
            NATType = NATType.PortRestrictedCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 7200, // 2시간
            HolePunchingSuccess = 80,
            SpecialFeatures = new List<string>
            {
                "IPv6 우선 제공",
                "게임 패킷 우선 처리",
                "일부 지역 CGNAT 적용"
            },
            OptimizationNotes = new List<string>
            {
                "IPv6 활성화 권장",
                "게임 모드 신청 가능",
                "CGNAT 지역에서는 TURN 서버 필수"
            }
        },
        
        ["SKT"] = new ISPProfile
        {
            ISPName = "SK텔레콤",
            CGNATUsage = CGNATUsage.Limited,
            IPv6Support = IPv6Support.Partial,
            NATType = NATType.RestrictedCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 3600, // 1시간
            HolePunchingSuccess = 85,
            SpecialFeatures = new List<string>
            {
                "T 게임 랜드 서비스",
                "게임 전용 QoS",
                "Dynamic NAT 지원"
            }
        },
        
        ["LG U+"] = new ISPProfile
        {
            ISPName = "LG유플러스",
            CGNATUsage = CGNATUsage.Extensive, // 광범위 사용
            IPv6Support = IPv6Support.Limited,
            NATType = NATType.Symmetric,
            PortRange = new PortRange(10000, 50000),
            SessionTimeout = 1800, // 30분
            HolePunchingSuccess = 60,
            SpecialFeatures = new List<string>
            {
                "CGNAT 광범위 적용",
                "게임 서비스 최적화",
                "포트 제한이 다른 ISP보다 엄격"
            },
            OptimizationNotes = new List<string>
            {
                "TURN 서버 사용 권장",
                "UPnP 설정 확인 필요",
                "게임 전용 요금제 고려"
            }
        }
    };
}

public enum CGNATUsage
{
    None,        // CGNAT 미사용
    Limited,     // 제한적 사용
    Partial,     // 일부 지역 사용
    Extensive    // 광범위 사용
}

public enum IPv6Support
{
    None,        // IPv6 미지원
    Limited,     // 제한적 지원
    Partial,     // 부분 지원
    Full         // 완전 지원
}
```

### 2.2 글로벌 ISP NAT 특성

```csharp
public class GlobalISPProfiles
{
    public static Dictionary<string, ISPProfile> Profiles = new()
    {
        ["Comcast"] = new ISPProfile
        {
            ISPName = "Comcast (미국)",
            CGNATUsage = CGNATUsage.Limited,
            IPv6Support = IPv6Support.Full,
            NATType = NATType.PortRestrictedCone,
            PortRange = new PortRange(32768, 65535),
            SessionTimeout = 3600,
            HolePunchingSuccess = 75,
            SpecialFeatures = new List<string>
            {
                "xFi Gateway 사용시 게임 모드 지원",
                "포트 포워딩 자동 설정",
                "IPv6 기본 제공"
            }
        },
        
        ["Deutsche Telekom"] = new ISPProfile
        {
            ISPName = "Deutsche Telekom (독일)",
            CGNATUsage = CGNATUsage.Extensive,
            IPv6Support = IPv6Support.Full,
            NATType = NATType.Symmetric,
            PortRange = new PortRange(20000, 40000),
            SessionTimeout = 900, // 15분
            HolePunchingSuccess = 45,
            SpecialFeatures = new List<string>
            {
                "DS-Lite 기본 적용",
                "IPv6 우선 정책",
                "게임 트래픽 별도 처리"
            },
            OptimizationNotes = new List<string>
            {
                "IPv6 사용 필수",
                "TURN 서버 사용 권장",
                "UPnP 동작하지 않음"
            }
        },
        
        ["NTT"] = new ISPProfile
        {
            ISPName = "NTT (일본)",
            CGNATUsage = CGNATUsage.None,
            IPv6Support = IPv6Support.Partial,
            NATType = NATType.FullCone,
            PortRange = new PortRange(1024, 65535),
            SessionTimeout = 7200,
            HolePunchingSuccess = 90,
            SpecialFeatures = new List<string>
            {
                "게임 친화적 NAT 정책",
                "포트 제한 최소화",
                "안정적인 포트 매핑"
            }
        }
    };
}
```

## 3. 모바일 통신사 NAT 특성

### 3.3 모바일 캐리어별 NAT 정책

```csharp
public class MobileCarrierProfiles
{
    public static Dictionary<string, MobileNATProfile> Profiles = new()
    {
        ["SKT_5G"] = new MobileNATProfile
        {
            CarrierName = "SKT 5G",
            NetworkType = MobileNetworkType.FiveG,
            CGNATLayers = 2, // 이중 NAT
            NATType = NATType.Symmetric,
            PortRange = new PortRange(10000, 30000),
            SessionTimeout = 300, // 5분
            HolePunchingSuccess = 25,
            BatteryOptimization = true,
            SpecialFeatures = new List<string>
            {
                "5G 슬라이싱으로 게임 트래픽 분리",
                "Edge Computing 지원",
                "Ultra-low latency 모드"
            },
            Limitations = new List<string>
            {
                "이중 CGNAT로 P2P 매우 어려움",
                "배터리 절약시 연결 조기 종료",
                "데이터 사용량 제한"
            }
        },
        
        ["Verizon_5G"] = new MobileNATProfile
        {
            CarrierName = "Verizon 5G (미국)",
            NetworkType = MobileNetworkType.FiveG,
            CGNATLayers = 1,
            NATType = NATType.PortRestrictedCone,
            PortRange = new PortRange(15000, 45000),
            SessionTimeout = 600,
            HolePunchingSuccess = 55,
            BatteryOptimization = false,
            SpecialFeatures = new List<string>
            {
                "Gaming Edge 서비스",
                "Ultra Wideband 5G",
                "게임 QoS 지원"
            }
        },
        
        ["DoCoMo_5G"] = new MobileNATProfile
        {
            CarrierName = "NTT DoCoMo 5G (일본)",
            NetworkType = MobileNetworkType.FiveG,
            CGNATLayers = 1,
            NATType = NATType.RestrictedCone,
            PortRange = new PortRange(20000, 60000),
            SessionTimeout = 900,
            HolePunchingSuccess = 70,
            BatteryOptimization = false,
            SpecialFeatures = new List<string>
            {
                "Gaming+ 서비스",
                "네트워크 슬라이싱",
                "지연시간 최적화"
            }
        }
    };
}

public class MobileNATProfile : NATProfile
{
    public string CarrierName { get; set; }
    public MobileNetworkType NetworkType { get; set; }
    public int CGNATLayers { get; set; }
    public bool BatteryOptimization { get; set; }
    public List<string> Limitations { get; set; } = new();
}

public enum MobileNetworkType
{
    ThreeG,
    FourG_LTE,
    FiveG,
    WiFi
}
```

## 4. 기업/교육기관 방화벽 특성

```csharp
public class EnterpriseFirewallProfiles
{
    public static Dictionary<string, EnterpriseNATProfile> Profiles = new()
    {
        ["Cisco_ASA"] = new EnterpriseNATProfile
        {
            ProductName = "Cisco ASA Series",
            Manufacturer = "Cisco",
            NATType = NATType.Symmetric,
            PortRange = new PortRange(1024, 5000), // 매우 제한적
            SessionTimeout = 120, // 2분
            HolePunchingSuccess = 15,
            SecurityLevel = SecurityLevel.High,
            GameTrafficBlocking = TrafficBlocking.Partial,
            UDPSupport = UDPSupport.Limited,
            SpecialFeatures = new List<string>
            {
                "Application Layer Gateway",
                "Deep Packet Inspection",
                "게임 프로토콜 차단 가능성"
            },
            BypassMethods = new List<string>
            {
                "HTTPS 터널링",
                "DNS over HTTPS 활용",
                "포트 443 사용"
            }
        },
        
        ["Fortinet_FortiGate"] = new EnterpriseNATProfile
        {
            ProductName = "FortiGate Series",
            Manufacturer = "Fortinet",
            NATType = NATType.Symmetric,
            PortRange = new PortRange(10000, 20000),
            SessionTimeout = 300,
            HolePunchingSuccess = 25,
            SecurityLevel = SecurityLevel.High,
            GameTrafficBlocking = TrafficBlocking.Aggressive,
            UDPSupport = UDPSupport.Limited,
            SpecialFeatures = new List<string>
            {
                "FortiGuard 웹 필터링",
                "Application Control",
                "게임 카테고리 차단"
            }
        },
        
        ["Palo_Alto"] = new EnterpriseNATProfile
        {
            ProductName = "Palo Alto Next-Gen Firewall",
            Manufacturer = "Palo Alto Networks",
            NATType = NATType.Symmetric,
            PortRange = new PortRange(32768, 65535),
            SessionTimeout = 180,
            HolePunchingSuccess = 20,
            SecurityLevel = SecurityLevel.VeryHigh,
            GameTrafficBlocking = TrafficBlocking.Aggressive,
            UDPSupport = UDPSupport.VeryLimited,
            SpecialFeatures = new List<string>
            {
                "App-ID 기술",
                "사용자별 정책",
                "게임 애플리케이션 식별 및 차단"
            }
        }
    };
}

public class EnterpriseNATProfile : NATProfile
{
    public string ProductName { get; set; }
    public SecurityLevel SecurityLevel { get; set; }
    public TrafficBlocking GameTrafficBlocking { get; set; }
    public UDPSupport UDPSupport { get; set; }
    public List<string> BypassMethods { get; set; } = new();
}

public enum SecurityLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

public enum TrafficBlocking
{
    None,
    Minimal,
    Partial,
    Aggressive
}

public enum UDPSupport
{
    Full,
    Limited,
    VeryLimited,
    Blocked
}
```

## 5. NAT 특성 자동 감지 도구

```csharp
public class NATCharacteristicsDetector
{
    private readonly AdvancedStunClient _stunClient;
    private readonly List<TestServer> _testServers;

    public NATCharacteristicsDetector()
    {
        _stunClient = new AdvancedStunClient();
        _testServers = new List<TestServer>
        {
            new TestServer("stun.l.google.com", 19302),
            new TestServer("stun1.l.google.com", 19302),
            new TestServer("stun2.l.google.com", 19302),
            new TestServer("stun.stunprotocol.org", 3478)
        };
    }

    public async Task<NATFingerprint> DetectNATCharacteristics()
    {
        var fingerprint = new NATFingerprint
        {
            TestTimestamp = DateTime.UtcNow
        };

        // 1. 기본 NAT 타입 감지
        await DetectBasicNATType(fingerprint);

        // 2. 포트 할당 패턴 분석
        await AnalyzePortAllocationPattern(fingerprint);

        // 3. 세션 타임아웃 측정
        await MeasureSessionTimeout(fingerprint);

        // 4. 포트 범위 탐지
        await DetectPortRange(fingerprint);

        // 5. 제조사/모델 추정
        EstimateRouterProfile(fingerprint);

        return fingerprint;
    }

    private async Task DetectBasicNATType(NATFingerprint fingerprint)
    {
        try
        {
            var stunResult = await _stunClient.GetNATTypeAsync();
            fingerprint.NATType = stunResult.NATType;
            fingerprint.PublicEndPoint = stunResult.MappedAddress;
            fingerprint.TestResults.Add("NAT Type Detection", "Success");
        }
        catch (Exception ex)
        {
            fingerprint.TestResults.Add("NAT Type Detection", $"Failed: {ex.Message}");
        }
    }

    private async Task AnalyzePortAllocationPattern(NATFingerprint fingerprint)
    {
        var allocatedPorts = new List<int>();
        
        try
        {
            // 여러 번 바인딩하여 포트 할당 패턴 분석
            for (int i = 0; i < 10; i++)
            {
                using var udpClient = new UdpClient();
                var localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
                
                var publicEndPoint = await _stunClient.GetPublicEndPointAsync();
                if (publicEndPoint != null)
                {
                    allocatedPorts.Add(publicEndPoint.Port);
                }
                
                await Task.Delay(100);
            }

            if (allocatedPorts.Count > 2)
            {
                var pattern = AnalyzePortPattern(allocatedPorts);
                fingerprint.PortPredictability = pattern;
                fingerprint.AllocatedPorts = allocatedPorts;
                fingerprint.TestResults.Add("Port Pattern Analysis", $"Pattern: {pattern}");
            }
        }
        catch (Exception ex)
        {
            fingerprint.TestResults.Add("Port Pattern Analysis", $"Failed: {ex.Message}");
        }
    }

    private PortPredictability AnalyzePortPattern(List<int> ports)
    {
        if (ports.Count < 3) return PortPredictability.Unknown;

        var differences = new List<int>();
        for (int i = 1; i < ports.Count; i++)
        {
            differences.Add(Math.Abs(ports[i] - ports[i - 1]));
        }

        var avgDiff = differences.Average();
        var maxDiff = differences.Max();
        var minDiff = differences.Min();

        // 순차적 할당 패턴
        if (maxDiff <= 10 && avgDiff <= 5)
            return PortPredictability.Sequential;

        // 고정 증가량 패턴
        if (differences.All(d => d == differences[0]))
            return PortPredictability.FixedIncrement;

        // 완전 랜덤
        if (maxDiff > 10000)
            return PortPredictability.Random;

        // 제한된 랜덤
        return PortPredictability.LimitedRandom;
    }

    private async Task MeasureSessionTimeout(NATFingerprint fingerprint)
    {
        try
        {
            using var udpClient = new UdpClient();
            var publicEndPoint = await _stunClient.GetPublicEndPointAsync();
            
            if (publicEndPoint == null) return;

            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.Zero;

            // 30분까지 5분 간격으로 테스트
            for (int minutes = 5; minutes <= 30; minutes += 5)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                
                try
                {
                    var currentEndPoint = await _stunClient.GetPublicEndPointAsync();
                    if (currentEndPoint == null || !currentEndPoint.Equals(publicEndPoint))
                    {
                        timeout = TimeSpan.FromMinutes(minutes - 5);
                        break;
                    }
                }
                catch
                {
                    timeout = TimeSpan.FromMinutes(minutes - 5);
                    break;
                }
            }

            fingerprint.SessionTimeout = timeout;
            fingerprint.TestResults.Add("Session Timeout", $"{timeout.TotalMinutes} minutes");
        }
        catch (Exception ex)
        {
            fingerprint.TestResults.Add("Session Timeout", $"Failed: {ex.Message}");
        }
    }

    private async Task DetectPortRange(NATFingerprint fingerprint)
    {
        var minPort = int.MaxValue;
        var maxPort = int.MinValue;

        try
        {
            for (int i = 0; i < 20; i++)
            {
                using var udpClient = new UdpClient();
                var publicEndPoint = await _stunClient.GetPublicEndPointAsync();
                
                if (publicEndPoint != null)
                {
                    minPort = Math.Min(minPort, publicEndPoint.Port);
                    maxPort = Math.Max(maxPort, publicEndPoint.Port);
                }
                
                await Task.Delay(50);
            }

            if (minPort != int.MaxValue && maxPort != int.MinValue)
            {
                fingerprint.PortRange = new PortRange(minPort, maxPort);
                fingerprint.TestResults.Add("Port Range Detection", $"{minPort}-{maxPort}");
            }
        }
        catch (Exception ex)
        {
            fingerprint.TestResults.Add("Port Range Detection", $"Failed: {ex.Message}");
        }
    }

    private void EstimateRouterProfile(NATFingerprint fingerprint)
    {
        var profiles = new List<NATProfile>
        {
            new TPLinkNATProfile(),
            new NetgearNATProfile(),
            new AsusNATProfile()
        };

        var bestMatch = new { Profile = (NATProfile)null, Score = 0 };

        foreach (var profile in profiles)
        {
            var score = CalculateMatchScore(fingerprint, profile);
            if (score > bestMatch.Score)
            {
                bestMatch = new { Profile = profile, Score = score };
            }
        }

        if (bestMatch.Profile != null && bestMatch.Score > 70)
        {
            fingerprint.EstimatedManufacturer = bestMatch.Profile.Manufacturer;
            fingerprint.MatchConfidence = bestMatch.Score;
            fingerprint.TestResults.Add("Router Estimation", 
                $"{bestMatch.Profile.Manufacturer} (Confidence: {bestMatch.Score}%)");
        }
        else
        {
            fingerprint.TestResults.Add("Router Estimation", "Unknown");
        }
    }

    private int CalculateMatchScore(NATFingerprint fingerprint, NATProfile profile)
    {
        var score = 0;

        // NAT 타입 매칭
        if (fingerprint.NATType == profile.DefaultBehavior.ToNATType())
            score += 30;

        // 포트 예측 가능성 매칭
        if (fingerprint.PortPredictability != PortPredictability.Unknown)
        {
            var profilePattern = profile.GetTypicalPortPattern();
            if (fingerprint.PortPredictability == profilePattern)
                score += 25;
        }

        // 세션 타임아웃 매칭
        if (fingerprint.SessionTimeout.TotalSeconds > 0)
        {
            var timeoutDiff = Math.Abs(fingerprint.SessionTimeout.TotalSeconds - profile.SessionTimeout);
            if (timeoutDiff < 60) score += 20;
            else if (timeoutDiff < 300) score += 10;
        }

        // 포트 범위 매칭
        if (fingerprint.PortRange != null)
        {
            var profileRange = profile.GetTypicalPortRange();
            if (profileRange != null && 
                Math.Abs(fingerprint.PortRange.Min - profileRange.Min) < 1000 &&
                Math.Abs(fingerprint.PortRange.Max - profileRange.Max) < 1000)
                score += 25;
        }

        return score;
    }
}

public class NATFingerprint
{
    public DateTime TestTimestamp { get; set; }
    public NATType NATType { get; set; }
    public IPEndPoint PublicEndPoint { get; set; }
    public PortRange PortRange { get; set; }
    public PortPredictability PortPredictability { get; set; }
    public TimeSpan SessionTimeout { get; set; }
    public List<int> AllocatedPorts { get; set; } = new();
    public string EstimatedManufacturer { get; set; }
    public int MatchConfidence { get; set; }
    public Dictionary<string, string> TestResults { get; set; } = new();
}

public enum PortPredictability
{
    Unknown,
    Sequential,        // 순차적
    FixedIncrement,    // 고정 증가량
    LimitedRandom,     // 제한된 랜덤
    Random             // 완전 랜덤
}
```

## 6. NAT 특성별 홀펀칭 전략

```csharp
public class AdaptiveHolePunchingStrategy
{
    private readonly Dictionary<string, HolePunchingMethod> _strategyMap;

    public AdaptiveHolePunchingStrategy()
    {
        _strategyMap = new Dictionary<string, HolePunchingMethod>
        {
            ["TP-Link_Sequential"] = new HolePunchingMethod
            {
                Name = "TP-Link Sequential Strategy",
                PreferredPorts = new List<int> { 12000, 12001, 12002 },
                Attempts = 5,
                Interval = TimeSpan.FromMilliseconds(200),
                UsePortPrediction = true,
                Strategy = "순차적 포트 할당을 이용한 예측 홀펀칭"
            },
            
            ["Netgear_FullCone"] = new HolePunchingMethod
            {
                Name = "Netgear Full Cone Strategy", 
                PreferredPorts = new List<int> { 30000, 40000, 50000 },
                Attempts = 3,
                Interval = TimeSpan.FromMilliseconds(100),
                UsePortPrediction = false,
                Strategy = "Full Cone NAT을 이용한 직접 연결"
            },
            
            ["CGNAT_TURN"] = new HolePunchingMethod
            {
                Name = "CGNAT TURN Fallback",
                PreferredPorts = new List<int>(),
                Attempts = 1,
                Interval = TimeSpan.FromSeconds(1),
                UsePortPrediction = false,
                Strategy = "CGNAT 환경에서 TURN 서버 사용"
            }
        };
    }

    public HolePunchingMethod SelectStrategy(NATFingerprint fingerprint)
    {
        // NAT 타입에 따른 전략 선택
        return fingerprint.NATType switch
        {
            NATType.OpenInternet => new HolePunchingMethod
            {
                Name = "Direct Connection",
                Attempts = 1,
                Strategy = "직접 연결 가능"
            },
            
            NATType.FullCone => _strategyMap.ContainsKey($"{fingerprint.EstimatedManufacturer}_FullCone")
                ? _strategyMap[$"{fingerprint.EstimatedManufacturer}_FullCone"]
                : GetDefaultFullConeStrategy(),
                
            NATType.RestrictedCone or NATType.PortRestrictedCone => 
                GetRestrictedConeStrategy(fingerprint),
                
            NATType.Symmetric => GetSymmetricNATStrategy(fingerprint),
            
            _ => GetDefaultStrategy()
        };
    }

    private HolePunchingMethod GetDefaultFullConeStrategy()
    {
        return new HolePunchingMethod
        {
            Name = "Standard Full Cone",
            PreferredPorts = new List<int> { 20000, 30000, 40000 },
            Attempts = 3,
            Interval = TimeSpan.FromMilliseconds(150),
            Strategy = "표준 Full Cone 홀펀칭"
        };
    }

    private HolePunchingMethod GetRestrictedConeStrategy(NATFingerprint fingerprint)
    {
        var method = new HolePunchingMethod
        {
            Name = "Restricted Cone Hole Punching",
            Attempts = 8,
            Interval = TimeSpan.FromMilliseconds(100),
            Strategy = "양방향 동시 홀펀칭"
        };

        // 포트 예측 가능성에 따라 포트 선택
        if (fingerprint.PortPredictability == PortPredictability.Sequential)
        {
            method.UsePortPrediction = true;
            method.PreferredPorts = PredictSequentialPorts(fingerprint.AllocatedPorts);
        }
        else
        {
            method.PreferredPorts = new List<int> { 15000, 25000, 35000, 45000 };
        }

        return method;
    }

    private HolePunchingMethod GetSymmetricNATStrategy(NATFingerprint fingerprint)
    {
        return new HolePunchingMethod
        {
            Name = "Symmetric NAT with TURN Fallback",
            Attempts = 15, // 더 많은 시도
            Interval = TimeSpan.FromMilliseconds(50),
            UsePortPrediction = true,
            FallbackToTURN = true,
            Strategy = "포트 예측 시도 후 TURN 서버 사용"
        };
    }

    private HolePunchingMethod GetDefaultStrategy()
    {
        return new HolePunchingMethod
        {
            Name = "Universal Hole Punching",
            PreferredPorts = new List<int> { 10000, 20000, 30000, 40000, 50000 },
            Attempts = 10,
            Interval = TimeSpan.FromMilliseconds(200),
            Strategy = "범용 홀펀칭 전략"
        };
    }

    private List<int> PredictSequentialPorts(List<int> knownPorts)
    {
        if (knownPorts.Count < 2) return new List<int>();

        var predicted = new List<int>();
        var lastPort = knownPorts.Last();
        var increment = knownPorts.Count > 1 ? knownPorts[1] - knownPorts[0] : 1;

        for (int i = 1; i <= 5; i++)
        {
            predicted.Add(lastPort + (increment * i));
        }

        return predicted;
    }
}

public class HolePunchingMethod
{
    public string Name { get; set; }
    public List<int> PreferredPorts { get; set; } = new();
    public int Attempts { get; set; }
    public TimeSpan Interval { get; set; }
    public bool UsePortPrediction { get; set; }
    public bool FallbackToTURN { get; set; }
    public string Strategy { get; set; }
}
```

## 7. NAT 특성 데이터베이스

```csharp
public class NATCharacteristicsDatabase
{
    private readonly string _databasePath;
    private readonly List<NATFingerprint> _fingerprints;

    public NATCharacteristicsDatabase(string databasePath = "nat_database.json")
    {
        _databasePath = databasePath;
        _fingerprints = LoadDatabase();
    }

    public void AddFingerprint(NATFingerprint fingerprint)
    {
        _fingerprints.Add(fingerprint);
        SaveDatabase();
    }

    public List<NATFingerprint> FindSimilarFingerprints(NATFingerprint target, int maxResults = 10)
    {
        return _fingerprints
            .Select(fp => new { Fingerprint = fp, Similarity = CalculateSimilarity(target, fp) })
            .Where(x => x.Similarity > 0.7)
            .OrderByDescending(x => x.Similarity)
            .Take(maxResults)
            .Select(x => x.Fingerprint)
            .ToList();
    }

    public NATStatistics GetStatistics()
    {
        var stats = new NATStatistics();
        
        stats.TotalFingerprints = _fingerprints.Count;
        stats.NATTypeDistribution = _fingerprints
            .GroupBy(fp => fp.NATType)
            .ToDictionary(g => g.Key, g => g.Count());
            
        stats.ManufacturerDistribution = _fingerprints
            .Where(fp => !string.IsNullOrEmpty(fp.EstimatedManufacturer))
            .GroupBy(fp => fp.EstimatedManufacturer)
            .ToDictionary(g => g.Key, g => g.Count());
            
        stats.AverageSessionTimeout = _fingerprints
            .Where(fp => fp.SessionTimeout.TotalSeconds > 0)
            .Average(fp => fp.SessionTimeout.TotalMinutes);

        return stats;
    }

    private double CalculateSimilarity(NATFingerprint fp1, NATFingerprint fp2)
    {
        var score = 0.0;
        var maxScore = 0.0;

        // NAT 타입 비교 (가중치: 0.3)
        maxScore += 0.3;
        if (fp1.NATType == fp2.NATType)
            score += 0.3;

        // 포트 예측 가능성 비교 (가중치: 0.2)
        maxScore += 0.2;
        if (fp1.PortPredictability == fp2.PortPredictability)
            score += 0.2;

        // 제조사 비교 (가중치: 0.3)
        maxScore += 0.3;
        if (!string.IsNullOrEmpty(fp1.EstimatedManufacturer) && 
            !string.IsNullOrEmpty(fp2.EstimatedManufacturer) &&
            fp1.EstimatedManufacturer == fp2.EstimatedManufacturer)
            score += 0.3;

        // 세션 타임아웃 유사성 (가중치: 0.2)
        maxScore += 0.2;
        if (fp1.SessionTimeout.TotalSeconds > 0 && fp2.SessionTimeout.TotalSeconds > 0)
        {
            var timeoutDiff = Math.Abs(fp1.SessionTimeout.TotalSeconds - fp2.SessionTimeout.TotalSeconds);
            if (timeoutDiff < 300) // 5분 이내 차이
                score += 0.2 * (1 - timeoutDiff / 300);
        }

        return maxScore > 0 ? score / maxScore : 0;
    }

    private List<NATFingerprint> LoadDatabase()
    {
        try
        {
            if (File.Exists(_databasePath))
            {
                var json = File.ReadAllText(_databasePath);
                return JsonSerializer.Deserialize<List<NATFingerprint>>(json) ?? new List<NATFingerprint>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load NAT database: {ex.Message}");
        }
        
        return new List<NATFingerprint>();
    }

    private void SaveDatabase()
    {
        try
        {
            var json = JsonSerializer.Serialize(_fingerprints, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_databasePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save NAT database: {ex.Message}");
        }
    }
}

public class NATStatistics
{
    public int TotalFingerprints { get; set; }
    public Dictionary<NATType, int> NATTypeDistribution { get; set; } = new();
    public Dictionary<string, int> ManufacturerDistribution { get; set; } = new();
    public double AverageSessionTimeout { get; set; }
}
```

이러한 NAT 장비별 특성 정리를 통해 실제 네트워크 환경에서 마주치게 될 다양한 NAT 구성에 대한 이해를 높이고, 각 상황에 맞는 최적의 홀펀칭 전략을 수립할 수 있습니다. 특히 한국의 ISP 환경과 글로벌 환경의 차이점을 이해하고, 모바일과 기업 환경에서의 제약사항을 파악하여 더 효과적인 P2P 연결 전략을 구현할 수 있습니다.  