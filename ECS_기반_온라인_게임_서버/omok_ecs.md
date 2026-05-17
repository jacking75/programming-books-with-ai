# 오목 게임 서버
  
ECS로 구성한 오목 게임 서버  
```
// ======================== COMPONENTS ========================

// 네트워크 관련 컴포넌트
public class NetworkSessionComponent : IComponent
{
    public int SessionId { get; set; }
    public string IPAddress { get; set; }
    public DateTime ConnectedTime { get; set; }
    public bool IsConnected { get; set; }
}

public class PlayerComponent : IComponent
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Rating { get; set; }
    public PlayerState State { get; set; } // Idle, InLobby, InGame, Spectating
}

// 게임룸 관련 컴포넌트
public class GameRoomComponent : IComponent
{
    public string RoomId { get; set; }
    public string RoomName { get; set; }
    public int MaxPlayers { get; set; }
    public RoomState State { get; set; } // Waiting, Playing, Finished
    public GameMode Mode { get; set; } // Normal, Ranked, Tournament
}

public class GameRoomMemberComponent : IComponent
{
    public string RoomId { get; set; }
    public List<int> PlayerEntityIds { get; set; }
    public List<int> SpectatorEntityIds { get; set; }
}

// 게임 보드 관련 컴포넌트
public class GameBoardComponent : IComponent
{
    public int BoardSize { get; set; } = 15; // 15x15 오목판
    public StoneType[,] Board { get; set; }
    public string GameId { get; set; }
}

public class GameStateComponent : IComponent
{
    public GamePhase Phase { get; set; } // Waiting, Playing, Finished
    public int CurrentPlayerEntityId { get; set; }
    public int BlackPlayerEntityId { get; set; }
    public int WhitePlayerEntityId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastMoveTime { get; set; }
    public int MoveCount { get; set; }
}

// 바둑돌 관련 컴포넌트
public class StoneComponent : IComponent
{
    public int X { get; set; }
    public int Y { get; set; }
    public StoneType Type { get; set; } // Black, White
    public int PlayerEntityId { get; set; }
    public DateTime PlacedTime { get; set; }
}

// 게임 규칙 관련 컴포넌트
public class GameRuleComponent : IComponent
{
    public bool AllowRenju { get; set; } = false; // 렌주룰 허용 여부
    public int TimeLimit { get; set; } = 300; // 초 단위
    public bool IsRanked { get; set; }
}

// 매칭 관련 컴포넌트
public class MatchmakingComponent : IComponent
{
    public int Rating { get; set; }
    public DateTime QueueTime { get; set; }
    public MatchmakingType Type { get; set; } // Quick, Ranked
    public int PreferredOpponentRating { get; set; }
}

// 히스토리 관련 컴포넌트
public class MoveHistoryComponent : IComponent
{
    public List<MoveRecord> Moves { get; set; }
    public string GameId { get; set; }
}

public class MoveRecord
{
    public int X { get; set; }
    public int Y { get; set; }
    public StoneType StoneType { get; set; }
    public int PlayerEntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public int MoveNumber { get; set; }
}

// 타이머 관련 컴포넌트
public class GameTimerComponent : IComponent
{
    public int BlackPlayerTimeLeft { get; set; }
    public int WhitePlayerTimeLeft { get; set; }
    public DateTime LastTurnStart { get; set; }
    public bool IsBlackTurn { get; set; }
}

// 통계 관련 컴포넌트
public class PlayerStatisticsComponent : IComponent
{
    public int TotalGames { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public double WinRate => TotalGames > 0 ? (double)Wins / TotalGames : 0;
}

// ======================== SYSTEMS ========================

// 네트워크 관련 시스템
public class NetworkMessageSystem : ISystem
{
    // 클라이언트로부터 받은 메시지 처리
    // 패킷 직렬화/역직렬화
    // 연결 상태 관리
}

public class SessionManagementSystem : ISystem
{
    // 세션 생성/삭제
    // 연결 끊김 감지
    // 하트비트 처리
}

// 매칭 관련 시스템
public class MatchmakingSystem : ISystem
{
    // 플레이어 매칭 로직
    // 레이팅 기반 상대 찾기
    // 매칭 큐 관리
}

public class GameRoomSystem : ISystem
{
    // 게임룸 생성/삭제
    // 플레이어 입장/퇴장
    // 관전자 관리
}

// 게임 로직 시스템
public class GameLogicSystem : ISystem
{
    // 바둑돌 놓기 유효성 검사
    // 승부 판정 (5목 체크)
    // 렌주룰 적용
}

public class TurnManagementSystem : ISystem
{
    // 턴 순서 관리
    // 턴 타임아웃 처리
    // 다음 플레이어로 턴 넘기기
}

public class WinConditionSystem : ISystem
{
    // 승리 조건 체크 (5목, 시간 초과 등)
    // 게임 종료 처리
    // 결과 기록
}

// 게임 보드 관련 시스템
public class BoardUpdateSystem : ISystem
{
    // 보드 상태 업데이트
    // 바둑돌 배치
    // 보드 검증
}

public class MoveValidationSystem : ISystem
{
    // 수 놓기 유효성 검사
    // 이미 놓인 자리 체크
    // 금수 체크 (렌주룰)
}

// 타이머 관련 시스템
public class GameTimerSystem : ISystem
{
    // 게임 시간 관리
    // 턴별 시간 제한
    // 시간 초과 처리
}

// 히스토리 관련 시스템
public class MoveHistorySystem : ISystem
{
    // 수순 기록
    // 무르기 처리
    // 기보 저장
}

// 통계 관련 시스템
public class StatisticsSystem : ISystem
{
    // 플레이어 통계 업데이트
    // 레이팅 계산
    // 랭킹 관리
}

// 데이터베이스 관련 시스템
public class DatabaseSystem : ISystem
{
    // 플레이어 정보 저장/로드
    // 게임 결과 저장
    // 통계 데이터 영속화
}

// ======================== ENUMS ========================

public enum PlayerState
{
    Offline,
    Online,
    InLobby,
    InMatchmaking,
    InGame,
    Spectating
}

public enum RoomState
{
    Waiting,
    Playing,
    Finished,
    Abandoned
}

public enum GameMode
{
    Normal,
    Ranked,
    Tournament,
    Custom
}

public enum StoneType
{
    Empty,
    Black,
    White
}

public enum GamePhase
{
    Waiting,
    Playing,
    Finished,
    Paused
}

public enum MatchmakingType
{
    Quick,
    Ranked,
    Custom
}

// ======================== MAIN ARCHITECTURE ========================

public class OmokGameServer
{
    private ECSWorld _world;
    private NetworkServer _networkServer;
    private Dictionary<int, int> _sessionToPlayerEntity; // SessionId -> EntityId
    
    public void Initialize()
    {
        _world = new ECSWorld();
        _networkServer = new NetworkServer();
        
        // 시스템 등록
        RegisterSystems();
        
        // 네트워크 이벤트 핸들러 등록
        _networkServer.OnClientConnected += OnClientConnected;
        _networkServer.OnClientDisconnected += OnClientDisconnected;
        _networkServer.OnMessageReceived += OnMessageReceived;
    }
    
    private void RegisterSystems()
    {
        // 네트워크 시스템
        _world.AddSystem(new NetworkMessageSystem());
        _world.AddSystem(new SessionManagementSystem());
        
        // 매칭 시스템
        _world.AddSystem(new MatchmakingSystem());
        _world.AddSystem(new GameRoomSystem());
        
        // 게임 로직 시스템
        _world.AddSystem(new MoveValidationSystem());
        _world.AddSystem(new GameLogicSystem());
        _world.AddSystem(new TurnManagementSystem());
        _world.AddSystem(new WinConditionSystem());
        
        // 보드 관련 시스템
        _world.AddSystem(new BoardUpdateSystem());
        
        // 기타 시스템
        _world.AddSystem(new GameTimerSystem());
        _world.AddSystem(new MoveHistorySystem());
        _world.AddSystem(new StatisticsSystem());
        _world.AddSystem(new DatabaseSystem());
    }
    
    public void Update(float deltaTime)
    {
        _world.Update(deltaTime);
    }
    
    private void OnClientConnected(int sessionId, string ipAddress)
    {
        // 새 플레이어 엔티티 생성
        var playerEntity = _world.CreateEntity();
        
        _world.AddComponent(playerEntity, new NetworkSessionComponent
        {
            SessionId = sessionId,
            IPAddress = ipAddress,
            ConnectedTime = DateTime.Now,
            IsConnected = true
        });
        
        _world.AddComponent(playerEntity, new PlayerComponent
        {
            State = PlayerState.Online
        });
        
        _sessionToPlayerEntity[sessionId] = playerEntity;
    }
    
    private void OnClientDisconnected(int sessionId)
    {
        if (_sessionToPlayerEntity.TryGetValue(sessionId, out var entityId))
        {
            // 연결 해제 처리
            var session = _world.GetComponent<NetworkSessionComponent>(entityId);
            if (session != null)
            {
                session.IsConnected = false;
            }
            
            // 게임 중이었다면 게임 중단 처리
            var player = _world.GetComponent<PlayerComponent>(entityId);
            if (player != null && player.State == PlayerState.InGame)
            {
                // 게임 중단 로직 실행
            }
        }
    }
    
    private void OnMessageReceived(int sessionId, byte[] data)
    {
        // 메시지 타입에 따른 처리를 NetworkMessageSystem에 위임
    }
}

// ======================== PACKET DEFINITIONS ========================

public abstract class GamePacket
{
    public PacketType Type { get; set; }
    public int SessionId { get; set; }
}

public enum PacketType
{
    // 로그인/로그아웃
    LoginRequest,
    LoginResponse,
    LogoutRequest,
    
    // 매칭
    MatchmakingRequest,
    MatchmakingCancel,
    MatchFound,
    
    // 게임룸
    JoinRoomRequest,
    LeaveRoomRequest,
    RoomStateUpdate,
    
    // 게임 플레이
    PlaceStoneRequest,
    PlaceStoneResponse,
    GameStateUpdate,
    GameResult,
    
    // 채팅
    ChatMessage,
    
    // 기타
    Heartbeat,
    Error
}

public class PlaceStoneRequest : GamePacket
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class GameStateUpdate : GamePacket
{
    public int[,] BoardState { get; set; }
    public int CurrentPlayerEntityId { get; set; }
    public int BlackTimeLeft { get; set; }
    public int WhiteTimeLeft { get; set; }
}
```

## 핵심 구조 설명

### **1. 컴포넌트 분류**
- **네트워크 계층**: `NetworkSessionComponent`, `PlayerComponent`
- **게임룸 관리**: `GameRoomComponent`, `GameRoomMemberComponent`  
- **게임 로직**: `GameBoardComponent`, `GameStateComponent`, `StoneComponent`
- **시간 관리**: `GameTimerComponent`
- **매칭 시스템**: `MatchmakingComponent`
- **데이터 관리**: `MoveHistoryComponent`, `PlayerStatisticsComponent`

### **2. 시스템 분류**
- **네트워크**: 패킷 처리, 세션 관리
- **매칭**: 플레이어 매칭, 게임룸 관리
- **게임 로직**: 수 검증, 승부 판정, 턴 관리
- **데이터**: 히스토리 기록, 통계 업데이트, DB 연동

### **3. 주요 게임 플로우**

```
클라이언트 접속 → 플레이어 엔티티 생성 → 매칭 대기 → 
게임룸 생성 → 게임 시작 → 턴별 수 놓기 → 승부 판정 → 
결과 저장 → 통계 업데이트
```

## 실제 구현 시 고려사항

### **성능 최적화**
- 게임 보드는 15x15 고정 크기이므로 배열 캐싱 활용
- 승부 판정 시 전체 보드 스캔 대신 마지막 수 주변만 체크
- 매칭 큐는 레이팅 구간별로 분할하여 검색 최적화

### **네트워크 처리**
- 패킷 직렬화는 MessagePack이나 Protobuf 사용 권장
- 턴 기반 게임이므로 UDP보다는 TCP 안정성 중시
- 관전자가 많을 경우 브로드캐스트 최적화 필요

### **확장성 고려**
- 게임룸별로 독립적인 처리 가능하도록 설계
- 매칭 서버와 게임 서버 분리 가능한 구조
- 데이터베이스 샤딩을 위한 플레이어 ID 기반 분산

이 구조는 오목 게임의 특성에 맞춰 턴 기반 처리와 실시간 상태 동기화를 효율적으로 처리할 수 있도록 설계했습니다.  
  

[아키텍처 그림](https://claude.ai/artifacts/799cd54e-0f32-40c5-8602-7f51e9434014 )

이 다이어그램에서 확인할 수 있는 핵심 구조는 다음과 같습니다:

## **계층별 역할 분담**

**1. 클라이언트 계층**: 다수의 오목 클라이언트들이 서버에 연결
**2. 네트워크 계층**: TCP 소켓 통신과 패킷 처리 담당  
**3. ECS World**: 게임의 핵심 로직이 동작하는 중앙 시스템
**4. 데이터베이스 계층**: 영속적 데이터 저장 및 관리

## **ECS 구조의 특징**

### **컴포넌트 분류**
- **네트워크**: 세션, 플레이어, 통계 관리
- **게임**: 보드, 상태, 바둑돌, 타이머 관리  
- **룸**: 게임룸, 매칭, 히스토리 관리

### **시스템 배치**
12개의 핵심 시스템이 각각 독립적으로 동작하면서 서로 상호작용합니다. 이를 통해 네트워크 처리부터 게임 로직, 데이터 저장까지 모든 기능을 모듈화해서 관리할 수 있습니다.

## **실제 구현 시 이점**

**모듈화**: 각 시스템이 독립적이어서 기능 추가/수정이 용이합니다
**확장성**: 새로운 게임 모드나 기능 추가 시 기존 코드 영향 최소화
**성능**: 컴포넌트별 데이터 지역성으로 캐시 효율성 확보
**재사용성**: 컴포넌트 조합으로 다양한 엔티티 타입 구성 가능

이런 구조로 구현하면 오목 게임 서버의 복잡한 로직들을 체계적으로 관리할 수 있습니다.


# NetworkMessageSystem의 핵심 구현
  
```
// ======================== MESSAGE QUEUE SYSTEM ========================

public class MessageQueue
{
    private readonly Queue<ReceivedMessage> _messages = new Queue<ReceivedMessage>();
    private readonly object _lock = new object();
    
    public void EnqueueMessage(int sessionId, byte[] data)
    {
        lock (_lock)
        {
            _messages.Enqueue(new ReceivedMessage
            {
                SessionId = sessionId,
                Data = data,
                Timestamp = DateTime.Now
            });
        }
    }
    
    public bool TryDequeueMessage(out ReceivedMessage message)
    {
        lock (_lock)
        {
            return _messages.TryDequeue(out message);
        }
    }
}

public struct ReceivedMessage
{
    public int SessionId { get; set; }
    public byte[] Data { get; set; }
    public DateTime Timestamp { get; set; }
}

// ======================== MAIN SERVER INTEGRATION ========================

public class OmokGameServer
{
    private ECSWorld _world;
    private NetworkServer _networkServer;
    private MessageQueue _messageQueue;
    private Dictionary<int, int> _sessionToPlayerEntity; // SessionId -> EntityId
    
    // NetworkMessageSystem에서 사용할 패킷 송신 인터페이스
    public interface IPacketSender
    {
        void SendPacket(int sessionId, GamePacket packet);
        void BroadcastToRoom(string roomId, GamePacket packet, int excludeSessionId = -1);
    }
    
    private class PacketSender : IPacketSender
    {
        private readonly NetworkServer _networkServer;
        private readonly ECSWorld _world;
        
        public PacketSender(NetworkServer networkServer, ECSWorld world)
        {
            _networkServer = networkServer;
            _world = world;
        }
        
        public void SendPacket(int sessionId, GamePacket packet)
        {
            var data = PacketSerializer.Serialize(packet);
            _networkServer.SendToClient(sessionId, data);
        }
        
        public void BroadcastToRoom(string roomId, GamePacket packet, int excludeSessionId = -1)
        {
            // 룸의 모든 멤버에게 브로드캐스트
            var roomEntities = _world.GetEntitiesWithComponent<GameRoomComponent>()
                .Where(e => _world.GetComponent<GameRoomComponent>(e).RoomId == roomId);
            
            foreach (var roomEntity in roomEntities)
            {
                var roomMember = _world.GetComponent<GameRoomMemberComponent>(roomEntity);
                if (roomMember != null)
                {
                    foreach (var playerEntityId in roomMember.PlayerEntityIds)
                    {
                        var session = _world.GetComponent<NetworkSessionComponent>(playerEntityId);
                        if (session != null && session.SessionId != excludeSessionId)
                        {
                            SendPacket(session.SessionId, packet);
                        }
                    }
                }
            }
        }
    }
    
    public void Initialize()
    {
        _world = new ECSWorld();
        _networkServer = new NetworkServer();
        _messageQueue = new MessageQueue();
        _sessionToPlayerEntity = new Dictionary<int, int>();
        
        // PacketSender 생성 및 NetworkMessageSystem에 주입
        var packetSender = new PacketSender(_networkServer, _world);
        var networkMessageSystem = new NetworkMessageSystem(_messageQueue, packetSender, _world);
        
        // 시스템 등록
        _world.AddSystem(networkMessageSystem);
        RegisterOtherSystems();
        
        // 네트워크 이벤트 핸들러 등록
        _networkServer.OnClientConnected += OnClientConnected;
        _networkServer.OnClientDisconnected += OnClientDisconnected;
        _networkServer.OnMessageReceived += OnMessageReceived;
    }
    
    private void OnMessageReceived(int sessionId, byte[] data)
    {
        // 메시지를 큐에 넣어서 NetworkMessageSystem에서 처리하도록 위임
        _messageQueue.EnqueueMessage(sessionId, data);
    }
    
    // ... 기타 코드들
}

// ======================== NETWORK MESSAGE SYSTEM ========================

public class NetworkMessageSystem : ISystem
{
    private readonly MessageQueue _messageQueue;
    private readonly IPacketSender _packetSender;
    private readonly ECSWorld _world;
    
    // 패킷 타입별 핸들러 매핑
    private readonly Dictionary<PacketType, Func<int, GamePacket, bool>> _packetHandlers;
    
    public NetworkMessageSystem(MessageQueue messageQueue, IPacketSender packetSender, ECSWorld world)
    {
        _messageQueue = messageQueue;
        _packetSender = packetSender;
        _world = world;
        
        // 패킷 핸들러 등록
        _packetHandlers = new Dictionary<PacketType, Func<int, GamePacket, bool>>
        {
            { PacketType.LoginRequest, HandleLoginRequest },
            { PacketType.LogoutRequest, HandleLogoutRequest },
            { PacketType.JoinRoomRequest, HandleJoinRoomRequest },
            { PacketType.LeaveRoomRequest, HandleLeaveRoomRequest },
            { PacketType.PlaceStoneRequest, HandlePlaceStoneRequest },
            { PacketType.MatchmakingRequest, HandleMatchmakingRequest },
            { PacketType.ChatMessage, HandleChatMessage },
            { PacketType.Heartbeat, HandleHeartbeat }
        };
    }
    
    public void Update(float deltaTime)
    {
        // 메시지 큐에서 패킷들을 처리
        while (_messageQueue.TryDequeueMessage(out var message))
        {
            ProcessMessage(message);
        }
    }
    
    private void ProcessMessage(ReceivedMessage message)
    {
        try
        {
            // 패킷 역직렬화
            var packet = PacketSerializer.Deserialize(message.Data);
            if (packet == null)
            {
                SendErrorResponse(message.SessionId, "Invalid packet format");
                return;
            }
            
            // 세션 ID 설정
            packet.SessionId = message.SessionId;
            
            // 핸들러 실행
            if (_packetHandlers.TryGetValue(packet.Type, out var handler))
            {
                bool success = handler(message.SessionId, packet);
                if (!success)
                {
                    SendErrorResponse(message.SessionId, $"Failed to process {packet.Type}");
                }
            }
            else
            {
                SendErrorResponse(message.SessionId, $"Unknown packet type: {packet.Type}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message from session {message.SessionId}: {ex.Message}");
            SendErrorResponse(message.SessionId, "Internal server error");
        }
    }
    
    // ======================== LOGIN HANDLER ========================
    
    private bool HandleLoginRequest(int sessionId, GamePacket packet)
    {
        var loginPacket = packet as LoginRequest;
        if (loginPacket == null) return false;
        
        // 세션에 해당하는 플레이어 엔티티 찾기
        var playerEntity = GetPlayerEntityBySession(sessionId);
        if (playerEntity == -1)
        {
            SendErrorResponse(sessionId, "Player entity not found");
            return false;
        }
        
        // 플레이어 정보 검증 (실제로는 데이터베이스에서 확인)
        if (!ValidatePlayerCredentials(loginPacket.PlayerId, loginPacket.Password))
        {
            _packetSender.SendPacket(sessionId, new LoginResponse
            {
                Success = false,
                ErrorMessage = "Invalid credentials"
            });
            return false;
        }
        
        // 플레이어 컴포넌트 업데이트
        var playerComponent = _world.GetComponent<PlayerComponent>(playerEntity);
        if (playerComponent != null)
        {
            playerComponent.PlayerId = loginPacket.PlayerId;
            playerComponent.PlayerName = loginPacket.PlayerName;
            playerComponent.State = PlayerState.Online;
        }
        
        // 플레이어 통계 컴포넌트 추가 (데이터베이스에서 로드)
        var stats = LoadPlayerStatistics(loginPacket.PlayerId);
        _world.AddComponent(playerEntity, new PlayerStatisticsComponent
        {
            TotalGames = stats.TotalGames,
            Wins = stats.Wins,
            Losses = stats.Losses,
            Draws = stats.Draws
        });
        
        // 로그인 성공 응답
        _packetSender.SendPacket(sessionId, new LoginResponse
        {
            Success = true,
            PlayerId = loginPacket.PlayerId,
            PlayerName = loginPacket.PlayerName,
            Rating = stats.Rating,
            TotalGames = stats.TotalGames,
            Wins = stats.Wins,
            Losses = stats.Losses
        });
        
        Console.WriteLine($"Player {loginPacket.PlayerId} logged in successfully");
        return true;
    }
    
    // ======================== JOIN ROOM HANDLER ========================
    
    private bool HandleJoinRoomRequest(int sessionId, GamePacket packet)
    {
        var joinPacket = packet as JoinRoomRequest;
        if (joinPacket == null) return false;
        
        var playerEntity = GetPlayerEntityBySession(sessionId);
        if (playerEntity == -1) return false;
        
        var playerComponent = _world.GetComponent<PlayerComponent>(playerEntity);
        if (playerComponent == null || playerComponent.State != PlayerState.Online)
        {
            _packetSender.SendPacket(sessionId, new JoinRoomResponse
            {
                Success = false,
                ErrorMessage = "Player not in valid state for room joining"
            });
            return false;
        }
        
        // 룸 찾기 또는 생성
        int roomEntity = FindOrCreateRoom(joinPacket.RoomId, joinPacket.RoomName, playerEntity);
        if (roomEntity == -1)
        {
            _packetSender.SendPacket(sessionId, new JoinRoomResponse
            {
                Success = false,
                ErrorMessage = "Failed to join or create room"
            });
            return false;
        }
        
        var roomComponent = _world.GetComponent<GameRoomComponent>(roomEntity);
        var roomMemberComponent = _world.GetComponent<GameRoomMemberComponent>(roomEntity);
        
        // 룸이 가득 찬 경우
        if (roomMemberComponent.PlayerEntityIds.Count >= roomComponent.MaxPlayers)
        {
            _packetSender.SendPacket(sessionId, new JoinRoomResponse
            {
                Success = false,
                ErrorMessage = "Room is full"
            });
            return false;
        }
        
        // 플레이어를 룸에 추가
        roomMemberComponent.PlayerEntityIds.Add(playerEntity);
        playerComponent.State = PlayerState.InLobby;
        
        // 입장 성공 응답
        _packetSender.SendPacket(sessionId, new JoinRoomResponse
        {
            Success = true,
            RoomId = roomComponent.RoomId,
            RoomName = roomComponent.RoomName,
            PlayerCount = roomMemberComponent.PlayerEntityIds.Count,
            MaxPlayers = roomComponent.MaxPlayers
        });
        
        // 룸의 다른 플레이어들에게 새 플레이어 입장 알림
        var playerJoinedNotification = new PlayerJoinedRoom
        {
            PlayerId = playerComponent.PlayerId,
            PlayerName = playerComponent.PlayerName,
            PlayerCount = roomMemberComponent.PlayerEntityIds.Count
        };
        
        _packetSender.BroadcastToRoom(roomComponent.RoomId, playerJoinedNotification, sessionId);
        
        // 게임 시작 조건 체크 (2명이 모이면 게임 시작)
        if (roomMemberComponent.PlayerEntityIds.Count == 2 && roomComponent.State == RoomState.Waiting)
        {
            StartGame(roomEntity);
        }
        
        Console.WriteLine($"Player {playerComponent.PlayerId} joined room {roomComponent.RoomId}");
        return true;
    }
    
    // ======================== HELPER METHODS ========================
    
    private int GetPlayerEntityBySession(int sessionId)
    {
        var entities = _world.GetEntitiesWithComponent<NetworkSessionComponent>();
        foreach (var entity in entities)
        {
            var session = _world.GetComponent<NetworkSessionComponent>(entity);
            if (session != null && session.SessionId == sessionId)
            {
                return entity;
            }
        }
        return -1;
    }
    
    private bool ValidatePlayerCredentials(string playerId, string password)
    {
        // 실제로는 데이터베이스에서 검증
        // 여기서는 간단한 예시
        return !string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(password);
    }
    
    private PlayerStatistics LoadPlayerStatistics(string playerId)
    {
        // 실제로는 데이터베이스에서 로드
        return new PlayerStatistics
        {
            Rating = 1000,
            TotalGames = 0,
            Wins = 0,
            Losses = 0,
            Draws = 0
        };
    }
    
    private int FindOrCreateRoom(string roomId, string roomName, int playerEntity)
    {
        // 기존 룸 찾기
        if (!string.IsNullOrEmpty(roomId))
        {
            var existingRooms = _world.GetEntitiesWithComponent<GameRoomComponent>();
            foreach (var roomEntity in existingRooms)
            {
                var room = _world.GetComponent<GameRoomComponent>(roomEntity);
                if (room != null && room.RoomId == roomId)
                {
                    return roomEntity;
                }
            }
        }
        
        // 새 룸 생성
        var newRoomEntity = _world.CreateEntity();
        var newRoomId = string.IsNullOrEmpty(roomId) ? Guid.NewGuid().ToString() : roomId;
        
        _world.AddComponent(newRoomEntity, new GameRoomComponent
        {
            RoomId = newRoomId,
            RoomName = roomName ?? $"Room_{newRoomId[..8]}",
            MaxPlayers = 2,
            State = RoomState.Waiting,
            Mode = GameMode.Normal
        });
        
        _world.AddComponent(newRoomEntity, new GameRoomMemberComponent
        {
            RoomId = newRoomId,
            PlayerEntityIds = new List<int>(),
            SpectatorEntityIds = new List<int>()
        });
        
        return newRoomEntity;
    }
    
    private void StartGame(int roomEntity)
    {
        var roomComponent = _world.GetComponent<GameRoomComponent>(roomEntity);
        var roomMemberComponent = _world.GetComponent<GameRoomMemberComponent>(roomEntity);
        
        if (roomMemberComponent.PlayerEntityIds.Count != 2)
            return;
        
        // 룸 상태를 게임 중으로 변경
        roomComponent.State = RoomState.Playing;
        
        // 게임 보드 엔티티 생성
        var gameEntity = _world.CreateEntity();
        var gameId = Guid.NewGuid().ToString();
        
        _world.AddComponent(gameEntity, new GameBoardComponent
        {
            BoardSize = 15,
            Board = new StoneType[15, 15],
            GameId = gameId
        });
        
        var player1Entity = roomMemberComponent.PlayerEntityIds[0];
        var player2Entity = roomMemberComponent.PlayerEntityIds[1];
        
        _world.AddComponent(gameEntity, new GameStateComponent
        {
            Phase = GamePhase.Playing,
            CurrentPlayerEntityId = player1Entity, // 첫 번째 플레이어가 흑돌
            BlackPlayerEntityId = player1Entity,
            WhitePlayerEntityId = player2Entity,
            StartTime = DateTime.Now,
            LastMoveTime = DateTime.Now,
            MoveCount = 0
        });
        
        _world.AddComponent(gameEntity, new GameTimerComponent
        {
            BlackPlayerTimeLeft = 300, // 5분
            WhitePlayerTimeLeft = 300,
            LastTurnStart = DateTime.Now,
            IsBlackTurn = true
        });
        
        _world.AddComponent(gameEntity, new MoveHistoryComponent
        {
            Moves = new List<MoveRecord>(),
            GameId = gameId
        });
        
        // 플레이어 상태 업데이트
        var player1 = _world.GetComponent<PlayerComponent>(player1Entity);
        var player2 = _world.GetComponent<PlayerComponent>(player2Entity);
        
        if (player1 != null) player1.State = PlayerState.InGame;
        if (player2 != null) player2.State = PlayerState.InGame;
        
        // 게임 시작 알림
        var gameStartPacket = new GameStarted
        {
            GameId = gameId,
            BlackPlayerId = player1?.PlayerId ?? "",
            WhitePlayerId = player2?.PlayerId ?? "",
            BoardSize = 15,
            TimeLimit = 300
        };
        
        _packetSender.BroadcastToRoom(roomComponent.RoomId, gameStartPacket);
        
        Console.WriteLine($"Game started in room {roomComponent.RoomId}");
    }
    
    private void SendErrorResponse(int sessionId, string errorMessage)
    {
        _packetSender.SendPacket(sessionId, new ErrorResponse
        {
            ErrorMessage = errorMessage
        });
    }
    
    // 다른 핸들러들 (간단한 스텁)
    private bool HandleLogoutRequest(int sessionId, GamePacket packet) => true;
    private bool HandleLeaveRoomRequest(int sessionId, GamePacket packet) => true;
    private bool HandlePlaceStoneRequest(int sessionId, GamePacket packet) => true;
    private bool HandleMatchmakingRequest(int sessionId, GamePacket packet) => true;
    private bool HandleChatMessage(int sessionId, GamePacket packet) => true;
    private bool HandleHeartbeat(int sessionId, GamePacket packet) => true;
}

// ======================== PACKET DEFINITIONS ========================

public class LoginRequest : GamePacket
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public string Password { get; set; }
}

public class LoginResponse : GamePacket
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Rating { get; set; }
    public int TotalGames { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}

public class JoinRoomRequest : GamePacket
{
    public string RoomId { get; set; } // 빈 문자열이면 새 룸 생성
    public string RoomName { get; set; }
}

public class JoinRoomResponse : GamePacket
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public string RoomId { get; set; }
    public string RoomName { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
}

public class PlayerJoinedRoom : GamePacket
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int PlayerCount { get; set; }
}

public class GameStarted : GamePacket
{
    public string GameId { get; set; }
    public string BlackPlayerId { get; set; }
    public string WhitePlayerId { get; set; }
    public int BoardSize { get; set; }
    public int TimeLimit { get; set; }
}

public class ErrorResponse : GamePacket
{
    public string ErrorMessage { get; set; }
}

// ======================== UTILITY CLASSES ========================

public class PlayerStatistics
{
    public int Rating { get; set; }
    public int TotalGames { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
}

public static class PacketSerializer
{
    public static byte[] Serialize(GamePacket packet)
    {
        // 실제로는 MessagePack, Protobuf 등 사용
        // 여기서는 간단한 예시
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(packet);
    }
    
    public static GamePacket Deserialize(byte[] data)
    {
        // 실제로는 패킷 타입을 먼저 읽어서 적절한 타입으로 역직렬화
        // 여기서는 간단한 예시
        var json = System.Text.Encoding.UTF8.GetString(data);
        return System.Text.Json.JsonSerializer.Deserialize<GamePacket>(json);
    }
}
```    
   
## **핵심 구조 설명**

### **1. 메시지 처리 흐름**
```
OnMessageReceived → MessageQueue → NetworkMessageSystem.Update → 패킷 핸들러
```

**비동기 처리**: 네트워크 스레드에서 바로 처리하지 않고 메시지 큐에 넣어서 메인 게임 루프에서 순차 처리합니다. 이를 통해 스레드 안전성을 보장합니다.

### **2. 패킷 핸들러 매핑**
```csharp
_packetHandlers = new Dictionary<PacketType, Func<int, GamePacket, bool>>
{
    { PacketType.LoginRequest, HandleLoginRequest },
    { PacketType.JoinRoomRequest, HandleJoinRoomRequest },
    // ... 다른 핸들러들
};
```

**확장 가능한 구조**: 새로운 패킷 타입 추가 시 핸들러만 등록하면 됩니다.

### **3. 로그인 처리 핵심**

1. **플레이어 엔티티 검색**: 세션 ID로 해당 플레이어 엔티티를 찾습니다
2. **인증 처리**: 플레이어 ID/패스워드 검증을 수행합니다
3. **컴포넌트 업데이트**: 로그인 성공 시 PlayerComponent와 PlayerStatisticsComponent를 업데이트합니다
4. **응답 전송**: 성공/실패 결과를 클라이언트에 전송합니다

### **4. 방 입장 처리 핵심**

1. **룸 찾기/생성**: 기존 룸이 있으면 찾고, 없으면 새로 생성합니다
2. **입장 가능 여부 체크**: 룸 정원 확인 및 플레이어 상태 검증을 수행합니다
3. **플레이어 추가**: 룸에 플레이어를 추가하고 상태를 업데이트합니다
4. **자동 게임 시작**: 2명이 모이면 자동으로 게임을 시작합니다

## **ECS 패턴 활용 특징**

### **컴포넌트 기반 데이터 관리**
- 각 엔티티의 상태를 컴포넌트로 분리해서 관리
- 네트워크 세션, 플레이어 정보, 게임 상태 등을 독립적으로 처리

### **시스템 간 의존성 최소화**
- IPacketSender 인터페이스를 통한 의존성 주입
- 각 핸들러가 독립적으로 동작하도록 설계

### **확장성 고려**
- 새로운 패킷 타입 추가가 용이
- 핸들러별로 독립적인 로직 구현 가능

## **실제 구현 시 장점**

**스레드 안전성**: 메시지 큐를 통한 순차 처리로 동시성 문제 해결
**성능**: 패킷 타입별 핸들러 매핑으로 빠른 처리
**유지보수성**: 기능별로 분리된 핸들러로 코드 관리가 용이
**테스트 가능성**: 인터페이스 기반 의존성 주입으로 단위 테스트 용이

이런 구조로 구현하면 온라인 게임 서버의 복잡한 네트워크 처리를 체계적이고 안정적으로 관리할 수 있습니다.
