# C#과 P2P 통신을 이용한 온라인 게임 만들기

저자: 최흥배, AI-Assisted   
  
------  
  
# 12장: 실시간 액션 게임 구현 사례

## 12.1 FPS 게임의 P2P 네트워킹
FPS(First Person Shooter) 게임에서 P2P 네트워킹은 특별한 도전과제를 제시합니다. 플레이어의 빠른 움직임, 즉각적인 반응성, 그리고 정확한 히트 검증이 모두 필요하기 때문입니다.

### 12.1.1 게임 상태 구조 설계
먼저 FPS 게임의 기본 상태 구조를 정의해보겠습니다.

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

public struct Vector3Serializable
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3Serializable(float x, float y, float z)
    {
        X = x; Y = y; Z = z;
    }

    public static implicit operator Vector3(Vector3Serializable v) => new(v.X, v.Y, v.Z);
    public static implicit operator Vector3Serializable(Vector3 v) => new(v.X, v.Y, v.Z);
}

public class PlayerState
{
    public uint PlayerId { get; set; }
    public Vector3Serializable Position { get; set; }
    public Vector3Serializable Rotation { get; set; }
    public Vector3Serializable Velocity { get; set; }
    public int Health { get; set; }
    public bool IsAlive { get; set; }
    public uint SequenceNumber { get; set; }
    public long Timestamp { get; set; }
    
    // 무기 상태
    public int CurrentWeapon { get; set; }
    public int Ammo { get; set; }
    public bool IsShooting { get; set; }
    public Vector3Serializable AimDirection { get; set; }
}

public class GameAction
{
    public uint PlayerId { get; set; }
    public ActionType Type { get; set; }
    public uint SequenceNumber { get; set; }
    public long Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
}

public enum ActionType
{
    Move,
    Shoot,
    Reload,
    ChangeWeapon,
    Jump,
    Crouch
}

public class ShootAction
{
    public Vector3Serializable Origin { get; set; }
    public Vector3Serializable Direction { get; set; }
    public int WeaponType { get; set; }
    public uint TargetPlayerId { get; set; }
    public bool IsHit { get; set; }
}
```

### 12.1.2 P2P 게임 세션 관리자

```csharp
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class P2PGameSession
{
    private readonly UdpClient _udpClient;
    private readonly Dictionary<uint, IPEndPoint> _peerEndpoints;
    private readonly Dictionary<uint, PlayerState> _playerStates;
    private readonly ConcurrentQueue<GameAction> _actionQueue;
    private readonly Timer _updateTimer;
    private readonly Timer _heartbeatTimer;
    
    private uint _localPlayerId;
    private uint _sequenceNumber;
    private bool _isRunning;
    
    // 네트워크 보상 시스템
    private readonly Dictionary<uint, Queue<PlayerState>> _stateHistory;
    private const int MAX_HISTORY_SIZE = 60; // 1초간의 히스토리 (60fps 기준)
    
    public event Action<uint, PlayerState>? PlayerStateUpdated;
    public event Action<uint, GameAction>? ActionReceived;
    
    public P2PGameSession(int localPort, uint playerId)
    {
        _localPlayerId = playerId;
        _udpClient = new UdpClient(localPort);
        _peerEndpoints = new Dictionary<uint, IPEndPoint>();
        _playerStates = new Dictionary<uint, PlayerState>();
        _actionQueue = new ConcurrentQueue<GameAction>();
        _stateHistory = new Dictionary<uint, Queue<PlayerState>>();
        
        // 60fps로 게임 상태 업데이트
        _updateTimer = new Timer(UpdateGame, null, 0, 16);
        
        // 1초마다 하트비트 전송
        _heartbeatTimer = new Timer(SendHeartbeat, null, 0, 1000);
        
        _isRunning = true;
        _ = Task.Run(ReceiveLoop);
    }
    
    public void AddPeer(uint playerId, IPEndPoint endpoint)
    {
        _peerEndpoints[playerId] = endpoint;
        _playerStates[playerId] = new PlayerState
        {
            PlayerId = playerId,
            Position = Vector3.Zero,
            Health = 100,
            IsAlive = true,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _stateHistory[playerId] = new Queue<PlayerState>();
    }
    
    public void SendAction(GameAction action)
    {
        action.SequenceNumber = ++_sequenceNumber;
        action.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var message = new NetworkMessage
        {
            Type = MessageType.Action,
            Data = JsonSerializer.Serialize(action)
        };
        
        BroadcastMessage(message);
    }
    
    public void UpdateLocalPlayerState(PlayerState state)
    {
        state.PlayerId = _localPlayerId;
        state.SequenceNumber = ++_sequenceNumber;
        state.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        _playerStates[_localPlayerId] = state;
        
        // 상태 히스토리 저장
        var history = _stateHistory[_localPlayerId];
        history.Enqueue(state);
        while (history.Count > MAX_HISTORY_SIZE)
            history.Dequeue();
        
        var message = new NetworkMessage
        {
            Type = MessageType.PlayerState,
            Data = JsonSerializer.Serialize(state)
        };
        
        BroadcastMessage(message);
    }
    
    private void UpdateGame(object? state)
    {
        // 액션 큐 처리
        while (_actionQueue.TryDequeue(out var action))
        {
            ProcessAction(action);
        }
        
        // 지연된 상태 보간
        InterpolatePlayerStates();
    }
    
    private void ProcessAction(GameAction action)
    {
        switch (action.Type)
        {
            case ActionType.Shoot:
                ProcessShootAction(action);
                break;
            case ActionType.Move:
                ProcessMoveAction(action);
                break;
            // 다른 액션 타입들...
        }
        
        ActionReceived?.Invoke(action.PlayerId, action);
    }
    
    private void ProcessShootAction(GameAction action)
    {
        var shootData = JsonSerializer.Deserialize<ShootAction>(action.Data);
        if (shootData == null) return;
        
        // 히트 검증 - 과거 상태 기반
        var hitValidated = ValidateHit(action.PlayerId, shootData, action.Timestamp);
        
        if (hitValidated && shootData.TargetPlayerId != 0)
        {
            // 데미지 적용
            if (_playerStates.TryGetValue(shootData.TargetPlayerId, out var targetState))
            {
                targetState.Health = Math.Max(0, targetState.Health - GetWeaponDamage(shootData.WeaponType));
                if (targetState.Health <= 0)
                {
                    targetState.IsAlive = false;
                }
                PlayerStateUpdated?.Invoke(shootData.TargetPlayerId, targetState);
            }
        }
    }
    
    private void ProcessMoveAction(GameAction action)
    {
        // 이동 액션 검증 및 처리
        // 치트 방지를 위한 이동 거리 검증 등
    }
    
    private bool ValidateHit(uint shooterId, ShootAction shootData, long timestamp)
    {
        // 히트 검증을 위해 과거 상태를 확인
        if (!_stateHistory.TryGetValue(shootData.TargetPlayerId, out var history))
            return false;
        
        // 네트워크 지연을 고려한 과거 상태 찾기
        var targetTime = timestamp - GetEstimatedLatency(shooterId);
        var targetState = FindStateAtTime(history, targetTime);
        
        if (targetState == null) return false;
        
        // 레이캐스팅을 통한 히트 검증
        return IsRayHit(shootData.Origin, shootData.Direction, targetState.Position);
    }
    
    private PlayerState? FindStateAtTime(Queue<PlayerState> history, long targetTime)
    {
        PlayerState? result = null;
        foreach (var state in history)
        {
            if (state.Timestamp <= targetTime)
                result = state;
            else
                break;
        }
        return result;
    }
    
    private bool IsRayHit(Vector3 origin, Vector3 direction, Vector3 targetPos)
    {
        // 간단한 구체 충돌 검사 (실제로는 더 정교한 충돌 검사 필요)
        const float playerRadius = 0.5f;
        var toTarget = targetPos - origin;
        var projection = Vector3.Dot(toTarget, Vector3.Normalize(direction));
        
        if (projection <= 0) return false;
        
        var projectedPoint = origin + Vector3.Normalize(direction) * projection;
        var distance = Vector3.Distance(projectedPoint, targetPos);
        
        return distance <= playerRadius;
    }
    
    private void InterpolatePlayerStates()
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        foreach (var kvp in _playerStates)
        {
            if (kvp.Key == _localPlayerId) continue; // 로컬 플레이어는 스킵
            
            var playerId = kvp.Key;
            var state = kvp.Value;
            
            // 지연 보상을 위한 상태 보간
            var interpolationTime = currentTime - 100; // 100ms 지연
            if (_stateHistory.TryGetValue(playerId, out var history) && history.Count >= 2)
            {
                var interpolatedState = InterpolateState(history, interpolationTime);
                if (interpolatedState != null)
                {
                    _playerStates[playerId] = interpolatedState;
                    PlayerStateUpdated?.Invoke(playerId, interpolatedState);
                }
            }
        }
    }
    
    private PlayerState? InterpolateState(Queue<PlayerState> history, long targetTime)
    {
        PlayerState? before = null;
        PlayerState? after = null;
        
        foreach (var state in history)
        {
            if (state.Timestamp <= targetTime)
                before = state;
            else
            {
                after = state;
                break;
            }
        }
        
        if (before == null || after == null) return before ?? after;
        
        var timeDiff = after.Timestamp - before.Timestamp;
        if (timeDiff <= 0) return before;
        
        var t = (float)(targetTime - before.Timestamp) / timeDiff;
        t = Math.Clamp(t, 0f, 1f);
        
        return new PlayerState
        {
            PlayerId = before.PlayerId,
            Position = Vector3.Lerp(before.Position, after.Position, t),
            Rotation = Vector3.Lerp(before.Rotation, after.Rotation, t),
            Velocity = Vector3.Lerp(before.Velocity, after.Velocity, t),
            Health = before.Health,
            IsAlive = before.IsAlive,
            SequenceNumber = before.SequenceNumber,
            Timestamp = targetTime
        };
    }
    
    private async Task ReceiveLoop()
    {
        while (_isRunning)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync();
                var message = JsonSerializer.Deserialize<NetworkMessage>(result.Buffer);
                
                if (message != null)
                {
                    await ProcessNetworkMessage(message, result.RemoteEndPoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"네트워크 수신 오류: {ex.Message}");
            }
        }
    }
    
    private async Task ProcessNetworkMessage(NetworkMessage message, IPEndPoint sender)
    {
        switch (message.Type)
        {
            case MessageType.PlayerState:
                var playerState = JsonSerializer.Deserialize<PlayerState>(message.Data);
                if (playerState != null && playerState.PlayerId != _localPlayerId)
                {
                    _playerStates[playerState.PlayerId] = playerState;
                    
                    // 상태 히스토리 저장
                    if (_stateHistory.TryGetValue(playerState.PlayerId, out var history))
                    {
                        history.Enqueue(playerState);
                        while (history.Count > MAX_HISTORY_SIZE)
                            history.Dequeue();
                    }
                    
                    PlayerStateUpdated?.Invoke(playerState.PlayerId, playerState);
                }
                break;
                
            case MessageType.Action:
                var action = JsonSerializer.Deserialize<GameAction>(message.Data);
                if (action != null && action.PlayerId != _localPlayerId)
                {
                    _actionQueue.Enqueue(action);
                }
                break;
        }
    }
    
    private void BroadcastMessage(NetworkMessage message)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(message);
        
        foreach (var endpoint in _peerEndpoints.Values)
        {
            try
            {
                _udpClient.Send(data, endpoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"메시지 전송 실패: {ex.Message}");
            }
        }
    }
    
    private void SendHeartbeat(object? state)
    {
        var heartbeat = new NetworkMessage
        {
            Type = MessageType.Heartbeat,
            Data = _localPlayerId.ToString()
        };
        
        BroadcastMessage(heartbeat);
    }
    
    private long GetEstimatedLatency(uint playerId)
    {
        // 실제로는 RTT 측정을 통해 구현
        return 50; // 기본 50ms
    }
    
    private int GetWeaponDamage(int weaponType)
    {
        return weaponType switch
        {
            1 => 25, // 라이플
            2 => 100, // 스나이퍼
            3 => 15, // 기관총
            _ => 10
        };
    }
    
    public void Dispose()
    {
        _isRunning = false;
        _updateTimer?.Dispose();
        _heartbeatTimer?.Dispose();
        _udpClient?.Dispose();
    }
}

public class NetworkMessage
{
    public MessageType Type { get; set; }
    public string Data { get; set; } = string.Empty;
}

public enum MessageType
{
    PlayerState,
    Action,
    Heartbeat,
    Synchronization
}
```

## 12.2 상태 동기화와 충돌 해결

P2P 게임에서 가장 중요한 문제 중 하나는 여러 클라이언트 간의 상태 불일치를 해결하는 것입니다.

### 12.2.1 충돌 해결 시스템

```csharp
public class ConflictResolver
{
    private readonly Dictionary<uint, uint> _lastProcessedSequence;
    private readonly Dictionary<uint, Queue<GameAction>> _pendingActions;
    
    public ConflictResolver()
    {
        _lastProcessedSequence = new Dictionary<uint, uint>();
        _pendingActions = new Dictionary<uint, Queue<GameAction>>();
    }
    
    public ConflictResolution ResolveConflict(List<GameAction> conflictingActions)
    {
        // 타임스탬프 기반 우선순위 결정
        var sortedActions = conflictingActions
            .OrderBy(a => a.Timestamp)
            .ThenBy(a => a.PlayerId) // 동일 시간일 경우 플레이어 ID로 결정
            .ToList();
        
        var primaryAction = sortedActions.First();
        var conflictedActions = sortedActions.Skip(1).ToList();
        
        return new ConflictResolution
        {
            PrimaryAction = primaryAction,
            ConflictedActions = conflictedActions,
            ResolutionMethod = ResolutionMethod.TimestampPriority
        };
    }
    
    public bool ShouldProcessAction(GameAction action)
    {
        if (!_lastProcessedSequence.TryGetValue(action.PlayerId, out var lastSeq))
        {
            _lastProcessedSequence[action.PlayerId] = action.SequenceNumber;
            return true;
        }
        
        // 순서가 맞지 않는 패킷 처리
        if (action.SequenceNumber <= lastSeq)
        {
            return false; // 이미 처리된 액션
        }
        
        if (action.SequenceNumber > lastSeq + 1)
        {
            // 패킷 손실 감지 - 대기 큐에 추가
            if (!_pendingActions.ContainsKey(action.PlayerId))
                _pendingActions[action.PlayerId] = new Queue<GameAction>();
            
            _pendingActions[action.PlayerId].Enqueue(action);
            return false;
        }
        
        _lastProcessedSequence[action.PlayerId] = action.SequenceNumber;
        return true;
    }
    
    public List<GameAction> GetProcessableActions(uint playerId)
    {
        var result = new List<GameAction>();
        
        if (!_pendingActions.TryGetValue(playerId, out var pending))
            return result;
        
        var expectedSeq = _lastProcessedSequence[playerId] + 1;
        
        while (pending.Count > 0 && pending.Peek().SequenceNumber == expectedSeq)
        {
            var action = pending.Dequeue();
            result.Add(action);
            _lastProcessedSequence[playerId] = expectedSeq++;
        }
        
        return result;
    }
}

public class ConflictResolution
{
    public GameAction PrimaryAction { get; set; } = new();
    public List<GameAction> ConflictedActions { get; set; } = new();
    public ResolutionMethod ResolutionMethod { get; set; }
}

public enum ResolutionMethod
{
    TimestampPriority,
    PlayerIdPriority,
    HealthPriority,
    RollbackRequired
}
```

### 12.2.2 상태 동기화 시스템

```csharp
public class StateSynchronizer
{
    private readonly Dictionary<uint, PlayerState> _authorativeStates;
    private readonly Dictionary<uint, List<StateSnapshot>> _stateSnapshots;
    private readonly object _lockObject = new();
    
    public event Action<uint, PlayerState>? StateDesynchronized;
    
    public StateSynchronizer()
    {
        _authorativeStates = new Dictionary<uint, PlayerState>();
        _stateSnapshots = new Dictionary<uint, List<StateSnapshot>>();
    }
    
    public void AddStateSnapshot(uint playerId, PlayerState state)
    {
        lock (_lockObject)
        {
            if (!_stateSnapshots.ContainsKey(playerId))
                _stateSnapshots[playerId] = new List<StateSnapshot>();
            
            var snapshot = new StateSnapshot
            {
                State = state,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Confirmed = false
            };
            
            _stateSnapshots[playerId].Add(snapshot);
            
            // 오래된 스냅샷 정리 (5초 이상)
            var cutoffTime = snapshot.Timestamp - 5000;
            _stateSnapshots[playerId].RemoveAll(s => s.Timestamp < cutoffTime);
        }
    }
    
    public bool ValidateStateTransition(uint playerId, PlayerState fromState, PlayerState toState)
    {
        var timeDelta = (toState.Timestamp - fromState.Timestamp) / 1000.0f; // 초 단위
        
        if (timeDelta <= 0) return false;
        
        // 최대 이동 속도 검증
        var distance = Vector3.Distance(fromState.Position, toState.Position);
        var maxSpeed = GetMaxSpeed(playerId);
        
        if (distance / timeDelta > maxSpeed * 1.1f) // 10% 여유분
        {
            Console.WriteLine($"플레이어 {playerId} 속도 위반: {distance / timeDelta:F2} > {maxSpeed}");
            return false;
        }
        
        // 물리적 제약 검증 (점프, 중력 등)
        if (!ValidatePhysicsConstraints(fromState, toState, timeDelta))
        {
            Console.WriteLine($"플레이어 {playerId} 물리 법칙 위반");
            return false;
        }
        
        return true;
    }
    
    private bool ValidatePhysicsConstraints(PlayerState from, PlayerState to, float deltaTime)
    {
        // 중력 검증
        const float gravity = 9.81f;
        var expectedY = from.Position.Y + from.Velocity.Y * deltaTime - 0.5f * gravity * deltaTime * deltaTime;
        var actualY = to.Position.Y;
        
        // 점프나 다른 특수 상황을 고려한 허용 오차
        var tolerance = 2.0f;
        
        return Math.Abs(actualY - expectedY) <= tolerance;
    }
    
    public SyncResult SynchronizeStates(Dictionary<uint, PlayerState> receivedStates)
    {
        var conflicts = new List<StateConflict>();
        var corrections = new Dictionary<uint, PlayerState>();
        
        lock (_lockObject)
        {
            foreach (var kvp in receivedStates)
            {
                var playerId = kvp.Key;
                var receivedState = kvp.Value;
                
                if (_authorativeStates.TryGetValue(playerId, out var currentState))
                {
                    var conflict = DetectConflict(currentState, receivedState);
                    if (conflict.HasConflict)
                    {
                        conflicts.Add(new StateConflict
                        {
                            PlayerId = playerId,
                            LocalState = currentState,
                            RemoteState = receivedState,
                            ConflictType = conflict.Type,
                            Severity = conflict.Severity
                        });
                        
                        // 충돌 해결
                        var resolvedState = ResolveStateConflict(currentState, receivedState, conflict);
                        corrections[playerId] = resolvedState;
                        _authorativeStates[playerId] = resolvedState;
                    }
                }
                else
                {
                    _authorativeStates[playerId] = receivedState;
                }
            }
        }
        
        return new SyncResult
        {
            Conflicts = conflicts,
            Corrections = corrections,
            Success = conflicts.Count == 0
        };
    }
    
    private ConflictDetectionResult DetectConflict(PlayerState local, PlayerState remote)
    {
        var positionDiff = Vector3.Distance(local.Position, remote.Position);
        var timeDiff = Math.Abs(local.Timestamp - remote.Timestamp);
        
        // 위치 불일치 검사
        if (positionDiff > 1.0f && timeDiff < 100) // 100ms 이내에 1m 이상 차이
        {
            return new ConflictDetectionResult
            {
                HasConflict = true,
                Type = ConflictType.PositionMismatch,
                Severity = positionDiff > 5.0f ? ConflictSeverity.High : ConflictSeverity.Medium
            };
        }
        
        // 체력 불일치 검사
        if (local.Health != remote.Health)
        {
            return new ConflictDetectionResult
            {
                HasConflict = true,
                Type = ConflictType.HealthMismatch,
                Severity = ConflictSeverity.High
            };
        }
        
        // 생존 상태 불일치 검사
        if (local.IsAlive != remote.IsAlive)
        {
            return new ConflictDetectionResult
            {
                HasConflict = true,
                Type = ConflictType.AliveStatusMismatch,
                Severity = ConflictSeverity.Critical
            };
        }
        
        return new ConflictDetectionResult { HasConflict = false };
    }
    
    private PlayerState ResolveStateConflict(PlayerState local, PlayerState remote, ConflictDetectionResult conflict)
    {
        return conflict.Type switch
        {
            ConflictType.PositionMismatch => ResolvePositionConflict(local, remote),
            ConflictType.HealthMismatch => ResolveHealthConflict(local, remote),
            ConflictType.AliveStatusMismatch => ResolveAliveStatusConflict(local, remote),
            _ => local
        };
    }
    
    private PlayerState ResolvePositionConflict(PlayerState local, PlayerState remote)
    {
        // 더 최신 타임스탬프를 가진 상태를 우선시
        if (remote.Timestamp > local.Timestamp)
        {
            // 보간을 통한 부드러운 위치 조정
            var result = remote;
            result.Position = Vector3.Lerp(local.Position, remote.Position, 0.8f);
            return result;
        }
        return local;
    }
    
    private PlayerState ResolveHealthConflict(PlayerState local, PlayerState remote)
    {
        // 체력은 더 낮은 값을 채택 (보수적 접근)
        var result = local.Timestamp > remote.Timestamp ? local : remote;
        result.Health = Math.Min(local.Health, remote.Health);
        return result;
    }
    
    private PlayerState ResolveAliveStatusConflict(PlayerState local, PlayerState remote)
    {
        // 죽음 상태는 되돌릴 수 없음
        var result = local.Timestamp > remote.Timestamp ? local : remote;
        result.IsAlive = local.IsAlive && remote.IsAlive;
        if (!result.IsAlive)
        {
            result.Health = 0;
        }
        return result;
    }
    
    private float GetMaxSpeed(uint playerId)
    {
        // 플레이어별 최대 속도 (미터/초)
        return 10.0f; // 기본값
    }
}

public class StateSnapshot
{
    public PlayerState State { get; set; } = new();
    public long Timestamp { get; set; }
    public bool Confirmed { get; set; }
}

public class StateConflict
{
    public uint PlayerId { get; set; }
    public PlayerState LocalState { get; set; } = new();
    public PlayerState RemoteState { get; set; } = new();
    public ConflictType ConflictType { get; set; }
    public ConflictSeverity Severity { get; set; }
}

public class ConflictDetectionResult
{
    public bool HasConflict { get; set; }
    public ConflictType Type { get; set; }
    public ConflictSeverity Severity { get; set; }
}

public class SyncResult
{
    public List<StateConflict> Conflicts { get; set; } = new();
    public Dictionary<uint, PlayerState> Corrections { get; set; } = new();
    public bool Success { get; set; }
}

public enum ConflictType
{
    PositionMismatch,
    HealthMismatch,
    AliveStatusMismatch,
    SequenceError
}

public enum ConflictSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

## 12.3 지연 보상과 롤백 시스템

네트워크 지연을 보상하고 공정한 게임플레이를 제공하기 위한 롤백 시스템을 구현해보겠습니다.

### 12.3.1 롤백 시스템 구현

```csharp
public class RollbackSystem
{
    private readonly Dictionary<uint, CircularBuffer<GameSnapshot>> _gameHistory;
    private readonly Dictionary<uint, PredictionBuffer> _predictions;
    private readonly ConflictResolver _conflictResolver;
    
    private const int HISTORY_SIZE = 120; // 2초간의 히스토리 (60fps 기준)
    private const int ROLLBACK_LIMIT = 60; // 최대 1초 롤백
    
    public event Action<RollbackEvent>? RollbackOccurred;
    
    public RollbackSystem()
    {
        _gameHistory = new Dictionary<uint, CircularBuffer<GameSnapshot>>();
        _predictions = new Dictionary<uint, PredictionBuffer>();
        _conflictResolver = new ConflictResolver();
    }
    
    public void SaveGameSnapshot(Dictionary<uint, PlayerState> playerStates, List<GameAction> actions, long timestamp)
    {
        var snapshot = new GameSnapshot
        {
            Timestamp = timestamp,
            PlayerStates = new Dictionary<uint, PlayerState>(playerStates),
            Actions = new List<GameAction>(actions),
            FrameNumber = GetFrameNumber(timestamp)
        };
        
        foreach (var playerId in playerStates.Keys)
        {
            if (!_gameHistory.ContainsKey(playerId))
                _gameHistory[playerId] = new CircularBuffer<GameSnapshot>(HISTORY_SIZE);
            
            _gameHistory[playerId].Add(snapshot);
        }
    }
    
    public RollbackResult ProcessDelayedAction(GameAction delayedAction, long currentTime)
    {
        var rollbackTime = delayedAction.Timestamp;
        var rollbackFrame = GetFrameNumber(rollbackTime);
        var currentFrame = GetFrameNumber(currentTime);
        
        // 롤백 제한 검사
        if (currentFrame - rollbackFrame > ROLLBACK_LIMIT)
        {
            return new RollbackResult
            {
                Success = false,
                Reason = "롤백 시간 제한 초과",
                RollbackFrames = 0
            };
        }
        
        // 해당 프레임으로 롤백
        var rollbackSnapshot = FindSnapshotAtTime(delayedAction.PlayerId, rollbackTime);
        if (rollbackSnapshot == null)
        {
            return new RollbackResult
            {
                Success = false,
                Reason = "롤백 스냅샷을 찾을 수 없음",
                RollbackFrames = 0
            };
        }
        
        // 액션 재시뮬레이션
        var resimulationResult = ResimulateFromSnapshot(rollbackSnapshot, delayedAction, currentTime);
        
        // 예측과 실제 결과 비교
        var predictionAccuracy = ValidatePredictions(delayedAction.PlayerId, rollbackTime, resimulationResult);
        
        RollbackOccurred?.Invoke(new RollbackEvent
        {
            PlayerId = delayedAction.PlayerId,
            RollbackTime = rollbackTime,
            CurrentTime = currentTime,
            Action = delayedAction,
            PredictionAccuracy = predictionAccuracy
        });
        
        return new RollbackResult
        {
            Success = true,
            RollbackFrames = currentFrame - rollbackFrame,
            NewStates = resimulationResult.FinalStates,
            AffectedPlayers = resimulationResult.AffectedPlayers
        };
    }
    
    private GameSnapshot? FindSnapshotAtTime(uint playerId, long targetTime)
    {
        if (!_gameHistory.TryGetValue(playerId, out var history))
            return null;
        
        GameSnapshot? closestSnapshot = null;
        var minTimeDiff = long.MaxValue;
        
        foreach (var snapshot in history)
        {
            var timeDiff = Math.Abs(snapshot.Timestamp - targetTime);
            if (timeDiff < minTimeDiff)
            {
                minTimeDiff = timeDiff;
                closestSnapshot = snapshot;
            }
        }
        
        return closestSnapshot;
    }
    
    private ResimulationResult ResimulateFromSnapshot(GameSnapshot snapshot, GameAction newAction, long targetTime)
    {
        var currentStates = new Dictionary<uint, PlayerState>(snapshot.PlayerStates);
        var affectedPlayers = new HashSet<uint> { newAction.PlayerId };
        
        // 새로운 액션을 액션 리스트에 삽입
        var allActions = new List<GameAction>(snapshot.Actions) { newAction };
        allActions.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        
        // 스냅샷 시점부터 현재까지 재시뮬레이션
        var currentTime = snapshot.Timestamp;
        const float timeStep = 1000f / 60f; // 60fps (16.67ms)
        
        while (currentTime < targetTime)
        {
            // 현재 시간에 해당하는 액션들 처리
            var actionsAtTime = allActions.Where(a => 
                a.Timestamp >= currentTime && a.Timestamp < currentTime + timeStep).ToList();
            
            foreach (var action in actionsAtTime)
            {
                ProcessActionInSimulation(action, currentStates, affectedPlayers);
            }
            
            // 물리 업데이트
            UpdatePhysics(currentStates, timeStep / 1000f);
            
            currentTime += (long)timeStep;
        }
        
        return new ResimulationResult
        {
            FinalStates = currentStates,
            AffectedPlayers = affectedPlayers.ToList()
        };
    }
    
    private void ProcessActionInSimulation(GameAction action, Dictionary<uint, PlayerState> states, HashSet<uint> affectedPlayers)
    {
        switch (action.Type)
        {
            case ActionType.Shoot:
                ProcessShootInSimulation(action, states, affectedPlayers);
                break;
            case ActionType.Move:
                ProcessMoveInSimulation(action, states, affectedPlayers);
                break;
            // 다른 액션 타입들...
        }
    }
    
    private void ProcessShootInSimulation(GameAction action, Dictionary<uint, PlayerState> states, HashSet<uint> affectedPlayers)
    {
        var shootData = JsonSerializer.Deserialize<ShootAction>(action.Data);
        if (shootData == null) return;
        
        // 히트 검증
        if (shootData.TargetPlayerId != 0 && states.TryGetValue(shootData.TargetPlayerId, out var target))
        {
            if (IsRayHitInSimulation(shootData.Origin, shootData.Direction, target.Position))
            {
                var damage = GetWeaponDamage(shootData.WeaponType);
                target.Health = Math.Max(0, target.Health - damage);
                if (target.Health <= 0)
                {
                    target.IsAlive = false;
                }
                affectedPlayers.Add(shootData.TargetPlayerId);
            }
        }
    }
    
    private void ProcessMoveInSimulation(GameAction action, Dictionary<uint, PlayerState> states, HashSet<uint> affectedPlayers)
    {
        // 이동 액션 처리
        if (states.TryGetValue(action.PlayerId, out var playerState))
        {
            // 간단한 이동 처리 (실제로는 더 복잡한 로직 필요)
            affectedPlayers.Add(action.PlayerId);
        }
    }
    
    private void UpdatePhysics(Dictionary<uint, PlayerState> states, float deltaTime)
    {
        foreach (var state in states.Values)
        {
            if (!state.IsAlive) continue;
            
            // 중력 적용
            state.Velocity = new Vector3Serializable(
                state.Velocity.X,
                state.Velocity.Y - 9.81f * deltaTime,
                state.Velocity.Z
            );
            
            // 위치 업데이트
            state.Position = new Vector3Serializable(
                state.Position.X + state.Velocity.X * deltaTime,
                Math.Max(0, state.Position.Y + state.Velocity.Y * deltaTime), // 지면 충돌
                state.Position.Z + state.Velocity.Z * deltaTime
            );
            
            // 지면 충돌 시 수직 속도 리셋
            if (state.Position.Y <= 0)
            {
                state.Velocity = new Vector3Serializable(state.Velocity.X, 0, state.Velocity.Z);
            }
        }
    }
    
    public void StorePrediction(uint playerId, GameAction action, PlayerState predictedResult)
    {
        if (!_predictions.ContainsKey(playerId))
            _predictions[playerId] = new PredictionBuffer();
        
        _predictions[playerId].AddPrediction(new Prediction
        {
            Action = action,
            PredictedState = predictedResult,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }
    
    private float ValidatePredictions(uint playerId, long rollbackTime, ResimulationResult result)
    {
        if (!_predictions.TryGetValue(playerId, out var predictionBuffer))
            return 1.0f; // 예측이 없으면 100% 정확도로 간주
        
        var predictions = predictionBuffer.GetPredictionsAfterTime(rollbackTime);
        if (predictions.Count == 0) return 1.0f;
        
        float totalAccuracy = 0f;
        int validPredictions = 0;
        
        foreach (var prediction in predictions)
        {
            if (result.FinalStates.TryGetValue(playerId, out var actualState))
            {
                var positionError = Vector3.Distance(prediction.PredictedState.Position, actualState.Position);
                var accuracy = Math.Max(0f, 1f - (positionError / 10f)); // 10m 오차를 기준으로 정확도 계산
                totalAccuracy += accuracy;
                validPredictions++;
            }
        }
        
        return validPredictions > 0 ? totalAccuracy / validPredictions : 1.0f;
    }
    
    private bool IsRayHitInSimulation(Vector3 origin, Vector3 direction, Vector3 targetPos)
    {
        // 시뮬레이션에서의 히트 검사 (실제 게임과 동일한 로직)
        const float playerRadius = 0.5f;
        var toTarget = targetPos - origin;
        var projection = Vector3.Dot(toTarget, Vector3.Normalize(direction));
        
        if (projection <= 0) return false;
        
        var projectedPoint = origin + Vector3.Normalize(direction) * projection;
        var distance = Vector3.Distance(projectedPoint, targetPos);
        
        return distance <= playerRadius;
    }
    
    private int GetWeaponDamage(int weaponType)
    {
        return weaponType switch
        {
            1 => 25,  // 라이플
            2 => 100, // 스나이퍼
            3 => 15,  // 기관총
            _ => 10
        };
    }
    
    private long GetFrameNumber(long timestamp)
    {
        return timestamp / 16; // 60fps 기준 (16.67ms per frame)
    }
}

// 원형 버퍼 구현
public class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _count;
    private readonly int _capacity;
    
    public CircularBuffer(int capacity)
    {
        _capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _count = 0;
    }
    
    public void Add(T item)
    {
        _buffer[_head] = item;
        _head = (_head + 1) % _capacity;
        
        if (_count < _capacity)
            _count++;
    }
    
    public IEnumerable<T> GetItems()
    {
        for (int i = 0; i < _count; i++)
        {
            var index = (_head - _count + i + _capacity) % _capacity;
            yield return _buffer[index];
        }
    }
}

public class GameSnapshot
{
    public long Timestamp { get; set; }
    public long FrameNumber { get; set; }
    public Dictionary<uint, PlayerState> PlayerStates { get; set; } = new();
    public List<GameAction> Actions { get; set; } = new();
}

public class Prediction
{
    public GameAction Action { get; set; } = new();
    public PlayerState PredictedState { get; set; } = new();
    public long Timestamp { get; set; }
}

public class PredictionBuffer
{
    private readonly Queue<Prediction> _predictions = new();
    private const int MAX_PREDICTIONS = 60;
    
    public void AddPrediction(Prediction prediction)
    {
        _predictions.Enqueue(prediction);
        while (_predictions.Count > MAX_PREDICTIONS)
            _predictions.Dequeue();
    }
    
    public List<Prediction> GetPredictionsAfterTime(long timestamp)
    {
        return _predictions.Where(p => p.Timestamp >= timestamp).ToList();
    }
}

public class RollbackEvent
{
    public uint PlayerId { get; set; }
    public long RollbackTime { get; set; }
    public long CurrentTime { get; set; }
    public GameAction Action { get; set; } = new();
    public float PredictionAccuracy { get; set; }
}

public class RollbackResult
{
    public bool Success { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RollbackFrames { get; set; }
    public Dictionary<uint, PlayerState> NewStates { get; set; } = new();
    public List<uint> AffectedPlayers { get; set; } = new();
}

public class ResimulationResult
{
    public Dictionary<uint, PlayerState> FinalStates { get; set; } = new();
    public List<uint> AffectedPlayers { get; set; } = new();
}
```

## 12.4 스펙테이터 모드 구현

스펙테이터 모드는 게임에 참여하지 않고 다른 플레이어의 게임을 관전할 수 있는 기능입니다.

### 12.4.1 스펙테이터 시스템

```csharp
public class SpectatorSystem
{
    private readonly Dictionary<uint, SpectatorClient> _spectators;
    private readonly Dictionary<uint, PlayerState> _playerStates;
    private readonly Queue<GameEvent> _eventHistory;
    private readonly Timer _broadcastTimer;
    
    private const int MAX_EVENT_HISTORY = 1000;
    private const int SPECTATOR_UPDATE_INTERVAL = 33; // 30fps
    
    public event Action<uint>? SpectatorJoined;
    public event Action<uint>? SpectatorLeft;
    
    public SpectatorSystem()
    {
        _spectators = new Dictionary<uint, SpectatorClient>();
        _playerStates = new Dictionary<uint, PlayerState>();
        _eventHistory = new Queue<GameEvent>();
        
        _broadcastTimer = new Timer(BroadcastToSpectators, null, 0, SPECTATOR_UPDATE_INTERVAL);
    }
    
    public bool AddSpectator(uint spectatorId, IPEndPoint endpoint, SpectatorMode mode)
    {
        if (_spectators.ContainsKey(spectatorId))
            return false;
        
        var spectator = new SpectatorClient
        {
            Id = spectatorId,
            Endpoint = endpoint,
            Mode = mode,
            JoinTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LastUpdateTime = 0,
            FollowingPlayerId = 0,
            IsActive = true
        };
        
        _spectators[spectatorId] = spectator;
        
        // 초기 게임 상태 전송
        SendInitialStateToSpectator(spectator);
        
        SpectatorJoined?.Invoke(spectatorId);
        return true;
    }
    
    public void RemoveSpectator(uint spectatorId)
    {
        if (_spectators.Remove(spectatorId))
        {
            SpectatorLeft?.Invoke(spectatorId);
        }
    }
    
    public void UpdatePlayerState(uint playerId, PlayerState state)
    {
        _playerStates[playerId] = state;
        
        // 게임 이벤트 기록
        RecordGameEvent(new GameEvent
        {
            Type = GameEventType.PlayerStateUpdate,
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Data = JsonSerializer.Serialize(state)
        });
    }
    
    public void RecordGameAction(GameAction action)
    {
        RecordGameEvent(new GameEvent
        {
            Type = GameEventType.PlayerAction,
            PlayerId = action.PlayerId,
            Timestamp = action.Timestamp,
            Data = JsonSerializer.Serialize(action)
        });
    }
    
    public void RecordPlayerKill(uint killerId, uint victimId, int weaponType)
    {
        var killEvent = new KillEvent
        {
            KillerId = killerId,
            VictimId = victimId,
            WeaponType = weaponType,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        RecordGameEvent(new GameEvent
        {
            Type = GameEventType.PlayerKill,
            PlayerId = killerId,
            Timestamp = killEvent.Timestamp,
            Data = JsonSerializer.Serialize(killEvent)
        });
    }
    
    private void RecordGameEvent(GameEvent gameEvent)
    {
        _eventHistory.Enqueue(gameEvent);
        
        // 히스토리 크기 제한
        while (_eventHistory.Count > MAX_EVENT_HISTORY)
            _eventHistory.Dequeue();
    }
    
    private void SendInitialStateToSpectator(SpectatorClient spectator)
    {
        var initialData = new SpectatorInitialData
        {
            PlayerStates = new Dictionary<uint, PlayerState>(_playerStates),
            RecentEvents = _eventHistory.TakeLast(100).ToList(),
            GameTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        var message = new SpectatorMessage
        {
            Type = SpectatorMessageType.InitialData,
            Data = JsonSerializer.Serialize(initialData)
        };
        
        SendToSpectator(spectator, message);
    }
    
    private void BroadcastToSpectators(object? state)
    {
        if (_spectators.Count == 0) return;
        
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        foreach (var spectator in _spectators.Values)
        {
            if (!spectator.IsActive) continue;
            
            try
            {
                switch (spectator.Mode)
                {
                    case SpectatorMode.FreeCamera:
                        SendFreeCameraUpdate(spectator, currentTime);
                        break;
                    case SpectatorMode.FollowPlayer:
                        SendFollowPlayerUpdate(spectator, currentTime);
                        break;
                    case SpectatorMode.Overview:
                        SendOverviewUpdate(spectator, currentTime);
                        break;
                }
                
                spectator.LastUpdateTime = currentTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"스펙테이터 {spectator.Id} 업데이트 실패: {ex.Message}");
                spectator.IsActive = false;
            }
        }
    }
    
    private void SendFreeCameraUpdate(SpectatorClient spectator, long currentTime)
    {
        // 자유 카메라 모드 - 모든 플레이어 상태 전송
        var update = new SpectatorUpdate
        {
            PlayerStates = new Dictionary<uint, PlayerState>(_playerStates),
            RecentEvents = GetRecentEvents(spectator.LastUpdateTime),
            Timestamp = currentTime
        };
        
        var message = new SpectatorMessage
        {
            Type = SpectatorMessageType.GameUpdate,
            Data = JsonSerializer.Serialize(update)
        };
        
        SendToSpectator(spectator, message);
    }
    
    private void SendFollowPlayerUpdate(SpectatorClient spectator, long currentTime)
    {
        // 특정 플레이어 추적 모드
        if (spectator.FollowingPlayerId == 0 || !_playerStates.ContainsKey(spectator.FollowingPlayerId))
        {
            // 추적할 플레이어가 없으면 임의의 살아있는 플레이어 선택
            var alivePlayers = _playerStates.Where(kvp => kvp.Value.IsAlive).ToList();
            if (alivePlayers.Count > 0)
            {
                spectator.FollowingPlayerId = alivePlayers.First().Key;
            }
        }
        
        if (spectator.FollowingPlayerId != 0)
        {
            var followUpdate = new FollowPlayerUpdate
            {
                TargetPlayer = _playerStates[spectator.FollowingPlayerId],
                NearbyPlayers = GetNearbyPlayers(spectator.FollowingPlayerId, 50.0f),
                RecentEvents = GetRecentEvents(spectator.LastUpdateTime),
                Timestamp = currentTime
            };
            
            var message = new SpectatorMessage
            {
                Type = SpectatorMessageType.FollowUpdate,
                Data = JsonSerializer.Serialize(followUpdate)
            };
            
            SendToSpectator(spectator, message);
        }
    }
    
    private void SendOverviewUpdate(SpectatorClient spectator, long currentTime)
    {
        // 전체 맵 개요 모드 - 간소화된 정보만 전송
        var overviewUpdate = new OverviewUpdate
        {
            PlayerPositions = _playerStates.ToDictionary(
                kvp => kvp.Key,
                kvp => new PlayerOverview
                {
                    PlayerId = kvp.Key,
                    Position = kvp.Value.Position,
                    Health = kvp.Value.Health,
                    IsAlive = kvp.Value.IsAlive
                }
            ),
            ImportantEvents = GetImportantEvents(spectator.LastUpdateTime),
            Timestamp = currentTime
        };
        
        var message = new SpectatorMessage
        {
            Type = SpectatorMessageType.OverviewUpdate,
            Data = JsonSerializer.Serialize(overviewUpdate)
        };
        
        SendToSpectator(spectator, message);
    }
    
    private Dictionary<uint, PlayerState> GetNearbyPlayers(uint targetPlayerId, float radius)
    {
        if (!_playerStates.TryGetValue(targetPlayerId, out var targetPlayer))
            return new Dictionary<uint, PlayerState>();
        
        return _playerStates
            .Where(kvp => kvp.Key != targetPlayerId)
            .Where(kvp => Vector3.Distance(kvp.Value.Position, targetPlayer.Position) <= radius)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
    
    private List<GameEvent> GetRecentEvents(long sinceTime)
    {
        return _eventHistory
            .Where(e => e.Timestamp > sinceTime)
            .ToList();
    }
    
    private List<GameEvent> GetImportantEvents(long sinceTime)
    {
        return _eventHistory
            .Where(e => e.Timestamp > sinceTime)
            .Where(e => e.Type == GameEventType.PlayerKill || e.Type == GameEventType.PlayerDeath)
            .ToList();
    }
    
    private void SendToSpectator(SpectatorClient spectator, SpectatorMessage message)
    {
        // UDP를 통한 메시지 전송 (실제 구현에서는 P2P 연결 사용)
        var data = JsonSerializer.SerializeToUtf8Bytes(message);
        // 실제 전송 로직 구현 필요
    }
    
    public void SetSpectatorMode(uint spectatorId, SpectatorMode mode, uint followPlayerId = 0)
    {
        if (_spectators.TryGetValue(spectatorId, out var spectator))
        {
            spectator.Mode = mode;
            spectator.FollowingPlayerId = followPlayerId;
        }
    }
    
    public SpectatorInfo GetSpectatorInfo(uint spectatorId)
    {
        if (_spectators.TryGetValue(spectatorId, out var spectator))
        {
            return new SpectatorInfo
            {
                Id = spectator.Id,
                Mode = spectator.Mode,
                JoinTime = spectator.JoinTime,
                FollowingPlayerId = spectator.FollowingPlayerId,
                IsActive = spectator.IsActive
            };
        }
        
        return new SpectatorInfo();
    }
    
    public List<SpectatorInfo> GetAllSpectators()
    {
        return _spectators.Values
            .Where(s => s.IsActive)
            .Select(s => new SpectatorInfo
            {
                Id = s.Id,
                Mode = s.Mode,
                JoinTime = s.JoinTime,
                FollowingPlayerId = s.FollowingPlayerId,
                IsActive = s.IsActive
            })
            .ToList();
    }
    
    public void Dispose()
    {
        _broadcastTimer?.Dispose();
    }
}

public class SpectatorClient
{
    public uint Id { get; set; }
    public IPEndPoint Endpoint { get; set; } = new(IPAddress.Any, 0);
    public SpectatorMode Mode { get; set; }
    public long JoinTime { get; set; }
    public long LastUpdateTime { get; set; }
    public uint FollowingPlayerId { get; set; }
    public bool IsActive { get; set; }
}

public class GameEvent
{
    public GameEventType Type { get; set; }
    public uint PlayerId { get; set; }
    public long Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class KillEvent
{
    public uint KillerId { get; set; }
    public uint VictimId { get; set; }
    public int WeaponType { get; set; }
    public long Timestamp { get; set; }
}

public class SpectatorMessage
{
    public SpectatorMessageType Type { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class SpectatorInitialData
{
    public Dictionary<uint, PlayerState> PlayerStates { get; set; } = new();
    public List<GameEvent> RecentEvents { get; set; } = new();
    public long GameTime { get; set; }
}

public class SpectatorUpdate
{
    public Dictionary<uint, PlayerState> PlayerStates { get; set; } = new();
    public List<GameEvent> RecentEvents { get; set; } = new();
    public long Timestamp { get; set; }
}

public class FollowPlayerUpdate
{
    public PlayerState TargetPlayer { get; set; } = new();
    public Dictionary<uint, PlayerState> NearbyPlayers { get; set; } = new();
    public List<GameEvent> RecentEvents { get; set; } = new();
    public long Timestamp { get; set; }
}

public class PlayerOverview
{
    public uint PlayerId { get; set; }
    public Vector3Serializable Position { get; set; }
    public int Health { get; set; }
    public bool IsAlive { get; set; }
}

public class OverviewUpdate
{
    public Dictionary<uint, PlayerOverview> PlayerPositions { get; set; } = new();
    public List<GameEvent> ImportantEvents { get; set; } = new();
    public long Timestamp { get; set; }
}

public class SpectatorInfo
{
    public uint Id { get; set; }
    public SpectatorMode Mode { get; set; }
    public long JoinTime { get; set; }
    public uint FollowingPlayerId { get; set; }
    public bool IsActive { get; set; }
}

public enum SpectatorMode
{
    FreeCamera,    // 자유 카메라
    FollowPlayer,  // 특정 플레이어 추적
    Overview       // 전체 맵 개요
}

public enum GameEventType
{
    PlayerStateUpdate,
    PlayerAction,
    PlayerKill,
    PlayerDeath,
    WeaponPickup,
    HealthPickup
}

public enum SpectatorMessageType
{
    InitialData,
    GameUpdate,
    FollowUpdate,
    OverviewUpdate
}
```

### 12.4.2 콘솔 기반 게임 시연 프로그램

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== P2P FPS 게임 시연 ===");
        Console.WriteLine("1. 플레이어로 참여");
        Console.WriteLine("2. 스펙테이터로 참여");
        Console.Write("선택: ");
        
        var choice = Console.ReadLine();
        
        if (choice == "1")
        {
            await RunAsPlayer();
        }
        else if (choice == "2")
        {
            await RunAsSpectator();
        }
    }
    
    static async Task RunAsPlayer()
    {
        Console.Write("플레이어 ID 입력: ");
        var playerId = uint.Parse(Console.ReadLine() ?? "1");
        
        Console.Write("로컬 포트 입력: ");
        var port = int.Parse(Console.ReadLine() ?? "12345");
        
        var gameSession = new P2PGameSession(port, playerId);
        var rollbackSystem = new RollbackSystem();
        var spectatorSystem = new SpectatorSystem();
        
        // 이벤트 핸들러 등록
        gameSession.PlayerStateUpdated += (id, state) =>
        {
            Console.WriteLine($"플레이어 {id} 상태 업데이트: 위치({state.Position.X:F1}, {state.Position.Y:F1}, {state.Position.Z:F1}) 체력:{state.Health}");
            spectatorSystem.UpdatePlayerState(id, state);
        };
        
        gameSession.ActionReceived += (id, action) =>
        {
            Console.WriteLine($"플레이어 {id} 액션: {action.Type}");
            spectatorSystem.RecordGameAction(action);
        };
        
        // 피어 추가 (예제)
        Console.WriteLine("피어 추가를 원하면 'IP:Port' 형식으로 입력하세요:");
        var peerInput = Console.ReadLine();
        if (!string.IsNullOrEmpty(peerInput))
        {
            var parts = peerInput.Split(':');
            if (parts.Length == 2)
            {
                var endpoint = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
                gameSession.AddPeer(2, endpoint); // 예제 플레이어 ID 2
            }
        }
        
        // 게임 루프
        var random = new Random();
        var position = Vector3.Zero;
        
        Console.WriteLine("게임 시작! 'q'를 입력하면 종료됩니다.");
        
        _ = Task.Run(async () =>
        {
            while (true)
            {
                // 랜덤 이동 시뮬레이션
                position += new Vector3(
                    (float)(random.NextDouble() - 0.5) * 2,
                    0,
                    (float)(random.NextDouble() - 0.5) * 2
                );
                
                var playerState = new PlayerState
                {
                    PlayerId = playerId,
                    Position = position,
                    Health = 100,
                    IsAlive = true,
                    Velocity = Vector3.Zero
                };
                
                gameSession.UpdateLocalPlayerState(playerState);
                
                // 가끔 사격 액션
                if (random.NextDouble() < 0.1)
                {
                    var shootAction = new GameAction
                    {
                        PlayerId = playerId,
                        Type = ActionType.Shoot,
                        Data = JsonSerializer.Serialize(new ShootAction
                        {
                            Origin = position,
                            Direction = new Vector3(1, 0, 0),
                            WeaponType = 1
                        })
                    };
                    
                    gameSession.SendAction(shootAction);
                }
                
                await Task.Delay(100); // 10fps로 상태 업데이트
            }
        });
        
        while (Console.ReadLine()?.ToLower() != "q")
        {
            // 사용자 입력 대기
        }
        
        gameSession.Dispose();
        spectatorSystem.Dispose();
    }
    
    static async Task RunAsSpectator()
    {
        Console.Write("스펙테이터 ID 입력: ");
        var spectatorId = uint.Parse(Console.ReadLine() ?? "100");
        
        Console.WriteLine("스펙테이터 모드 선택:");
        Console.WriteLine("1. 자유 카메라");
        Console.WriteLine("2. 플레이어 추적");
        Console.WriteLine("3. 전체 개요");
        Console.Write("선택: ");
        
        var modeChoice = Console.ReadLine();
        var mode = modeChoice switch
        {
            "1" => SpectatorMode.FreeCamera,
            "2" => SpectatorMode.FollowPlayer,
            "3" => SpectatorMode.Overview,
            _ => SpectatorMode.FreeCamera
        };
        
        var spectatorSystem = new SpectatorSystem();
        
        spectatorSystem.SpectatorJoined += (id) =>
        {
            Console.WriteLine($"스펙테이터 {id} 참여");
        };
        
        // 스펙테이터 등록 (예제 - 실제로는 네트워크를 통해)
        var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
        spectatorSystem.AddSpectator(spectatorId, endpoint, mode);
        
        Console.WriteLine($"스펙테이터 모드로 연결됨 (모드: {mode})");
        Console.WriteLine("게임 상태를 관전 중... 'q'를 입력하면 종료됩니다.");
        
        // 가상 게임 데이터 시뮬레이션
        _ = Task.Run(async () =>
        {
            var random = new Random();
            uint playerId = 1;
            var position = Vector3.Zero;
            
            while (true)
            {
                // 가상 플레이어 상태 업데이트
                position += new Vector3(
                    (float)(random.NextDouble() - 0.5) * 1,
                    0,
                    (float)(random.NextDouble() - 0.5) * 1
                );
                
                var playerState = new PlayerState
                {
                    PlayerId = playerId,
                    Position = position,
                    Health = random.Next(50, 100),
                    IsAlive = true
                };
                
                spectatorSystem.UpdatePlayerState(playerId, playerState);
                
                await Task.Delay(200); // 5fps로 업데이트
            }
        });
        
        while (Console.ReadLine()?.ToLower() != "q")
        {
            // 사용자 입력 대기
        }
        
        spectatorSystem.Dispose();
    }
}
```

이 12장에서는 실시간 액션 게임에서 P2P 네트워킹의 핵심 구현 사례들을 다뤘습니다. FPS 게임의 특성상 요구되는 낮은 지연시간, 정확한 히트 검증, 그리고 공정한 게임플레이를 위한 롤백 시스템과 스펙테이터 모드까지 포괄적으로 구현했습니다.

핵심 특징들:
- **지연 보상**: 네트워크 지연을 고려한 히트 검증 시스템
- **롤백 시스템**: 지연된 액션을 처리하기 위한 게임 상태 롤백
- **충돌 해결**: 여러 클라이언트 간 상태 불일치 해결
- **스펙테이터 지원**: 다양한 관전 모드 제공

이러한 시스템들은 실제 상용 게임에서도 사용되는 검증된 기법들로, P2P 액션 게임의 품질과 공정성을 크게 향상시킵니다.
  

# 13장: 모바일 게임에서의 홀펀칭

## 13.1 모바일 NAT의 특성과 대응
모바일 환경에서의 NAT는 일반적인 가정용 라우터와는 다른 특성을 가지고 있습니다. 특히 캐리어 NAT(Carrier-grade NAT, CGN)와 대칭 NAT의 빈도가 높아 특별한 대응이 필요합니다.

### 13.1.1 모바일 NAT 분석 시스템

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class MobileNATAnalyzer
{
    private readonly List<STUNServer> _stunServers;
    private readonly Dictionary<string, NATTestResult> _testResults;
    private readonly object _lockObject = new();
    
    public event Action<NATAnalysisResult>? AnalysisCompleted;
    
    public MobileNATAnalyzer()
    {
        _stunServers = new List<STUNServer>
        {
            new() { Host = "stun.l.google.com", Port = 19302 },
            new() { Host = "stun1.l.google.com", Port = 19302 },
            new() { Host = "stun2.l.google.com", Port = 19302 },
            new() { Host = "stun.cloudflare.com", Port = 3478 },
            new() { Host = "stun.nextcloud.com", Port = 3478 }
        };
        _testResults = new Dictionary<string, NATTestResult>();
    }
    
    public async Task<MobileNATProfile> AnalyzeMobileNAT(NetworkType networkType, string carrierInfo)
    {
        Console.WriteLine($"모바일 NAT 분석 시작 - 네트워크: {networkType}, 캐리어: {carrierInfo}");
        
        var profile = new MobileNATProfile
        {
            NetworkType = networkType,
            CarrierInfo = carrierInfo,
            AnalysisTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        // 1. 기본 NAT 타입 검출
        profile.NATType = await DetectNATType();
        
        // 2. 포트 매핑 동작 분석
        profile.PortMappingBehavior = await AnalyzePortMappingBehavior();
        
        // 3. 타임아웃 특성 분석
        profile.MappingTimeouts = await AnalyzeMappingTimeouts();
        
        // 4. 동시 연결 제한 분석
        profile.MaxConcurrentMappings = await TestConcurrentMappings();
        
        // 5. 캐리어급 NAT 검출
        profile.HasCarrierGradeNAT = await DetectCarrierGradeNAT();
        
        // 6. IPv6 지원 여부
        profile.IPv6Support = await TestIPv6Support();
        
        return profile;
    }
    
    private async Task<NATType> DetectNATType()
    {
        var tasks = _stunServers.Select(server => TestNATWithServer(server)).ToArray();
        var results = await Task.WhenAll(tasks);
        
        return AnalyzeNATTypeFromResults(results);
    }
    
    private async Task<NATTestResult> TestNATWithServer(STUNServer server)
    {
        try
        {
            using var udpClient = new UdpClient();
            var localEndpoint = (IPEndPoint)udpClient.Client.LocalEndPoint!;
            
            // STUN 바인딩 요청
            var request = CreateSTUNBindingRequest();
            var serverEndpoint = new IPEndPoint(
                (await Dns.GetHostAddressesAsync(server.Host))[0], 
                server.Port
            );
            
            await udpClient.SendAsync(request, serverEndpoint);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await udpClient.ReceiveAsync();
            
            var response = ParseSTUNResponse(result.Buffer);
            
            return new NATTestResult
            {
                ServerEndpoint = serverEndpoint,
                LocalEndpoint = localEndpoint,
                MappedEndpoint = response.MappedAddress,
                SourceEndpoint = response.SourceAddress,
                Success = true,
                ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STUN 서버 {server.Host} 테스트 실패: {ex.Message}");
            return new NATTestResult { Success = false };
        }
    }
    
    private NATType AnalyzeNATTypeFromResults(NATTestResult[] results)
    {
        var successfulResults = results.Where(r => r.Success).ToArray();
        if (successfulResults.Length == 0)
        {
            return NATType.Unknown;
        }
        
        // 첫 번째 성공한 결과 분석
        var firstResult = successfulResults[0];
        
        // 로컬 주소와 매핑된 주소가 같으면 NAT 없음
        if (firstResult.LocalEndpoint.Address.Equals(firstResult.MappedEndpoint.Address))
        {
            return NATType.None;
        }
        
        // 여러 서버에서의 매핑 주소 비교
        var mappedAddresses = successfulResults.Select(r => r.MappedEndpoint).ToArray();
        var uniqueAddresses = mappedAddresses.GroupBy(ep => ep.Address).Count();
        var uniquePorts = mappedAddresses.GroupBy(ep => ep.Port).Count();
        
        if (uniqueAddresses > 1)
        {
            return NATType.Symmetric; // IP에 따라 다른 주소 할당
        }
        
        if (uniquePorts > 1)
        {
            return NATType.Symmetric; // 포트에 따라 다른 포트 할당
        }
        
        // 추가 테스트로 정확한 타입 결정
        return await DetermineExactNATType(firstResult);
    }
    
    private async Task<NATType> DetermineExactNATType(NATTestResult baseResult)
    {
        try
        {
            // 다른 포트로 같은 서버에 요청
            using var udpClient = new UdpClient();
            var alternatePort = baseResult.ServerEndpoint.Port + 1;
            var alternateEndpoint = new IPEndPoint(baseResult.ServerEndpoint.Address, alternatePort);
            
            var request = CreateSTUNBindingRequest();
            await udpClient.SendAsync(request, alternateEndpoint);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var result = await udpClient.ReceiveAsync();
            var response = ParseSTUNResponse(result.Buffer);
            
            // 포트가 달라지면 포트 제한적 NAT
            if (response.MappedAddress.Port != baseResult.MappedEndpoint.Port)
            {
                return NATType.PortRestricted;
            }
            
            return NATType.FullCone;
        }
        catch
        {
            return NATType.PortRestricted; // 타임아웃은 제한적 NAT의 증거
        }
    }
    
    private async Task<PortMappingBehavior> AnalyzePortMappingBehavior()
    {
        var behavior = new PortMappingBehavior();
        
        try
        {
            using var udpClient = new UdpClient();
            var localPort = ((IPEndPoint)udpClient.Client.LocalEndPoint!).Port;
            
            // 같은 로컬 포트로 여러 대상에 연결 테스트
            var targets = new[]
            {
                new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53),
                new IPEndPoint(IPAddress.Parse("8.8.4.4"), 53),
                new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53)
            };
            
            var mappedPorts = new List<int>();
            
            foreach (var target in targets)
            {
                try
                {
                    var request = CreateSTUNBindingRequest();
                    await udpClient.SendAsync(request, target);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    var result = await udpClient.ReceiveAsync();
                    var response = ParseSTUNResponse(result.Buffer);
                    
                    mappedPorts.Add(response.MappedAddress.Port);
                }
                catch
                {
                    // 응답 없음은 정상 (DNS 서버이므로)
                }
            }
            
            behavior.IsConsistent = mappedPorts.Distinct().Count() <= 1;
            behavior.PortIncrement = CalculatePortIncrement(mappedPorts);
            behavior.PreservesLocalPort = mappedPorts.Any(p => p == localPort);
            
            return behavior;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"포트 매핑 동작 분석 실패: {ex.Message}");
            return behavior;
        }
    }
    
    private async Task<MappingTimeouts> AnalyzeMappingTimeouts()
    {
        var timeouts = new MappingTimeouts();
        
        try
        {
            using var udpClient = new UdpClient();
            var stunServer = _stunServers[0];
            var serverEndpoint = new IPEndPoint(
                (await Dns.GetHostAddressesAsync(stunServer.Host))[0], 
                stunServer.Port
            );
            
            // 초기 매핑 생성
            var request = CreateSTUNBindingRequest();
            await udpClient.SendAsync(request, serverEndpoint);
            var response = await udpClient.ReceiveAsync();
            var initialMapping = ParseSTUNResponse(response.Buffer);
            
            // 다양한 간격으로 매핑 유지 테스트
            var testIntervals = new[] { 30, 60, 120, 300, 600 }; // 초
            
            foreach (var interval in testIntervals)
            {
                await Task.Delay(interval * 1000);
                
                try
                {
                    await udpClient.SendAsync(request, serverEndpoint);
                    var testResponse = await udpClient.ReceiveAsync();
                    var testMapping = ParseSTUNResponse(testResponse.Buffer);
                    
                    if (!testMapping.MappedAddress.Equals(initialMapping.MappedAddress))
                    {
                        timeouts.UDPMappingTimeout = interval;
                        break;
                    }
                }
                catch
                {
                    timeouts.UDPMappingTimeout = interval;
                    break;
                }
            }
            
            return timeouts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"매핑 타임아웃 분석 실패: {ex.Message}");
            return timeouts;
        }
    }
    
    private async Task<int> TestConcurrentMappings()
    {
        var maxConcurrent = 0;
        var clients = new List<UdpClient>();
        
        try
        {
            var stunServer = _stunServers[0];
            var serverEndpoint = new IPEndPoint(
                (await Dns.GetHostAddressesAsync(stunServer.Host))[0], 
                stunServer.Port
            );
            
            // 점진적으로 연결 수 증가
            for (int i = 1; i <= 100; i++)
            {
                try
                {
                    var client = new UdpClient();
                    clients.Add(client);
                    
                    var request = CreateSTUNBindingRequest();
                    await client.SendAsync(request, serverEndpoint);
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var response = await client.ReceiveAsync();
                    
                    maxConcurrent = i;
                }
                catch
                {
                    break; // 연결 실패 시 중단
                }
                
                if (i % 10 == 0)
                {
                    await Task.Delay(100); // 과부하 방지
                }
            }
        }
        finally
        {
            foreach (var client in clients)
            {
                client?.Dispose();
            }
        }
        
        return maxConcurrent;
    }
    
    private async Task<bool> DetectCarrierGradeNAT()
    {
        try
        {
            // RFC 6598 주소 범위 체크 (100.64.0.0/10)
            var localAddresses = await GetLocalIPAddresses();
            
            foreach (var addr in localAddresses)
            {
                var bytes = addr.GetAddressBytes();
                if (bytes[0] == 100 && (bytes[1] & 0xC0) == 0x40)
                {
                    return true; // CGN 주소 범위
                }
            }
            
            // 추가적으로 STUN을 통한 외부 IP 체크
            var results = await Task.WhenAll(_stunServers.Take(3).Select(TestNATWithServer));
            var successfulResults = results.Where(r => r.Success).ToArray();
            
            if (successfulResults.Length > 0)
            {
                var externalIP = successfulResults[0].MappedEndpoint.Address;
                var isPrivate = IsPrivateIPAddress(externalIP);
                return isPrivate; // 외부 IP가 프라이빗이면 CGN
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> TestIPv6Support()
    {
        try
        {
            using var udpClient = new UdpClient(AddressFamily.InterNetworkV6);
            var ipv6STUNServer = new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), 53);
            
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            await udpClient.SendAsync(testData, ipv6STUNServer);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await udpClient.ReceiveAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private byte[] CreateSTUNBindingRequest()
    {
        var request = new byte[20];
        // STUN 바인딩 요청 메시지 타입 (0x0001)
        request[0] = 0x00;
        request[1] = 0x01;
        // 메시지 길이 (0)
        request[2] = 0x00;
        request[3] = 0x00;
        // 매직 쿠키
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xA4;
        request[7] = 0x42;
        // 트랜잭션 ID (12바이트 랜덤)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            request[i] = (byte)random.Next(256);
        }
        
        return request;
    }
    
    private STUNResponse ParseSTUNResponse(byte[] data)
    {
        // 간단한 STUN 응답 파싱 (실제로는 더 정교한 파싱 필요)
        var response = new STUNResponse();
        
        if (data.Length >= 20)
        {
            // XOR-MAPPED-ADDRESS 속성 찾기 (0x0020)
            for (int i = 20; i < data.Length - 8; i++)
            {
                if (data[i] == 0x00 && data[i + 1] == 0x20)
                {
                    var port = (data[i + 6] << 8) | data[i + 7];
                    var ipBytes = new byte[4];
                    Array.Copy(data, i + 8, ipBytes, 0, 4);
                    
                    // XOR 디코딩
                    port ^= 0x2112;
                    for (int j = 0; j < 4; j++)
                    {
                        ipBytes[j] ^= new byte[] { 0x21, 0x12, 0xA4, 0x42 }[j];
                    }
                    
                    response.MappedAddress = new IPEndPoint(new IPAddress(ipBytes), port);
                    break;
                }
            }
        }
        
        return response;
    }
    
    private async Task<List<IPAddress>> GetLocalIPAddresses()
    {
        var addresses = new List<IPAddress>();
        
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
            foreach (var addr in hostEntry.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    addresses.Add(addr);
                }
            }
        }
        catch { }
        
        return addresses;
    }
    
    private bool IsPrivateIPAddress(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        
        return bytes[0] == 10 ||
               (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
               (bytes[0] == 192 && bytes[1] == 168) ||
               (bytes[0] == 100 && (bytes[1] & 0xC0) == 0x40); // CGN
    }
    
    private int CalculatePortIncrement(List<int> ports)
    {
        if (ports.Count < 2) return 0;
        
        var increments = new List<int>();
        for (int i = 1; i < ports.Count; i++)
        {
            increments.Add(ports[i] - ports[i - 1]);
        }
        
        return increments.Count > 0 ? (int)increments.Average() : 0;
    }
}

public class MobileNATProfile
{
    public NetworkType NetworkType { get; set; }
    public string CarrierInfo { get; set; } = string.Empty;
    public NATType NATType { get; set; }
    public PortMappingBehavior PortMappingBehavior { get; set; } = new();
    public MappingTimeouts MappingTimeouts { get; set; } = new();
    public int MaxConcurrentMappings { get; set; }
    public bool HasCarrierGradeNAT { get; set; }
    public bool IPv6Support { get; set; }
    public long AnalysisTime { get; set; }
}

public class PortMappingBehavior
{
    public bool IsConsistent { get; set; }
    public int PortIncrement { get; set; }
    public bool PreservesLocalPort { get; set; }
}

public class MappingTimeouts
{
    public int UDPMappingTimeout { get; set; } = 300; // 기본 5분
    public int TCPMappingTimeout { get; set; } = 1800; // 기본 30분
}

public class NATTestResult
{
    public IPEndPoint ServerEndpoint { get; set; } = new(IPAddress.Any, 0);
    public IPEndPoint LocalEndpoint { get; set; } = new(IPAddress.Any, 0);
    public IPEndPoint MappedEndpoint { get; set; } = new(IPAddress.Any, 0);
    public IPEndPoint SourceEndpoint { get; set; } = new(IPAddress.Any, 0);
    public bool Success { get; set; }
    public long ResponseTime { get; set; }
}

public class STUNServer
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}

public class STUNResponse
{
    public IPEndPoint MappedAddress { get; set; } = new(IPAddress.Any, 0);
    public IPEndPoint SourceAddress { get; set; } = new(IPAddress.Any, 0);
}

public class NATAnalysisResult
{
    public MobileNATProfile Profile { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public float SuccessProbability { get; set; }
}

public enum NetworkType
{
    WiFi,
    Cellular4G,
    Cellular5G,
    Ethernet,
    Unknown
}

public enum NATType
{
    None,
    FullCone,
    Restricted,
    PortRestricted,
    Symmetric,
    Unknown
}
```

## 13.2 배터리 최적화와 백그라운드 처리

모바일 기기에서는 배터리 수명이 중요한 고려사항입니다. 특히 P2P 통신은 지속적인 네트워크 활동을 필요로 하므로 효율적인 관리가 필수입니다.

### 13.2.1 배터리 최적화 시스템

```csharp
public class MobileBatteryOptimizer
{
    private readonly Dictionary<string, AdaptiveTimer> _timers;
    private readonly NetworkActivityMonitor _networkMonitor;
    private readonly PowerStateTracker _powerTracker;
    private BatteryOptimizationMode _currentMode;
    
    public event Action<BatteryOptimizationMode>? ModeChanged;
    public event Action<PowerState>? PowerStateChanged;
    
    public MobileBatteryOptimizer()
    {
        _timers = new Dictionary<string, AdaptiveTimer>();
        _networkMonitor = new NetworkActivityMonitor();
        _powerTracker = new PowerStateTracker();
        _currentMode = BatteryOptimizationMode.Balanced;
        
        _powerTracker.PowerStateChanged += OnPowerStateChanged;
        _networkMonitor.ActivityChanged += OnNetworkActivityChanged;
    }
    
    public void SetOptimizationMode(BatteryOptimizationMode mode)
    {
        if (_currentMode == mode) return;
        
        _currentMode = mode;
        ApplyOptimizationSettings(mode);
        ModeChanged?.Invoke(mode);
        
        Console.WriteLine($"배터리 최적화 모드 변경: {mode}");
    }
    
    private void ApplyOptimizationSettings(BatteryOptimizationMode mode)
    {
        var settings = GetOptimizationSettings(mode);
        
        // 하트비트 간격 조정
        AdjustHeartbeatInterval("primary", settings.HeartbeatInterval);
        
        // 상태 동기화 간격 조정
        AdjustTimerInterval("state_sync", settings.StateSyncInterval);
        
        // NAT 매핑 유지 간격 조정
        AdjustTimerInterval("nat_keepalive", settings.NATKeepAliveInterval);
        
        // 네트워크 품질 측정 간격 조정
        AdjustTimerInterval("quality_check", settings.QualityCheckInterval);
    }
    
    private OptimizationSettings GetOptimizationSettings(BatteryOptimizationMode mode)
    {
        return mode switch
        {
            BatteryOptimizationMode.Performance => new OptimizationSettings
            {
                HeartbeatInterval = 1000,    // 1초
                StateSyncInterval = 100,     // 100ms
                NATKeepAliveInterval = 15000, // 15초
                QualityCheckInterval = 5000,  // 5초
                MaxConcurrentConnections = 8,
                PacketBatchSize = 1
            },
            BatteryOptimizationMode.Balanced => new OptimizationSettings
            {
                HeartbeatInterval = 3000,    // 3초
                StateSyncInterval = 200,     // 200ms
                NATKeepAliveInterval = 30000, // 30초
                QualityCheckInterval = 15000, // 15초
                MaxConcurrentConnections = 4,
                PacketBatchSize = 3
            },
            BatteryOptimizationMode.PowerSaver => new OptimizationSettings
            {
                HeartbeatInterval = 10000,   // 10초
                StateSyncInterval = 500,     // 500ms
                NATKeepAliveInterval = 60000, // 60초
                QualityCheckInterval = 60000, // 60초
                MaxConcurrentConnections = 2,
                PacketBatchSize = 10
            },
            _ => GetOptimizationSettings(BatteryOptimizationMode.Balanced)
        };
    }
    
    private void AdjustHeartbeatInterval(string connectionId, int interval)
    {
        if (!_timers.TryGetValue($"heartbeat_{connectionId}", out var timer))
        {
            timer = new AdaptiveTimer(interval);
            _timers[$"heartbeat_{connectionId}"] = timer;
        }
        
        timer.UpdateInterval(interval);
    }
    
    private void AdjustTimerInterval(string timerId, int interval)
    {
        if (!_timers.TryGetValue(timerId, out var timer))
        {
            timer = new AdaptiveTimer(interval);
            _timers[timerId] = timer;
        }
        
        timer.UpdateInterval(interval);
    }
    
    private void OnPowerStateChanged(PowerState state)
    {
        switch (state)
        {
            case PowerState.Charging:
                SetOptimizationMode(BatteryOptimizationMode.Performance);
                break;
            case PowerState.LowBattery:
                SetOptimizationMode(BatteryOptimizationMode.PowerSaver);
                break;
            case PowerState.Normal:
                SetOptimizationMode(BatteryOptimizationMode.Balanced);
                break;
        }
        
        PowerStateChanged?.Invoke(state);
    }
    
    private void OnNetworkActivityChanged(NetworkActivity activity)
    {
        // 네트워크 활동에 따른 동적 조정
        if (activity.BytesPerSecond < 1024) // 1KB/s 미만
        {
            // 낮은 활동 시 간격 증가
            foreach (var timer in _timers.Values)
            {
                timer.IncreaseInterval(1.5f);
            }
        }
        else if (activity.BytesPerSecond > 10240) // 10KB/s 초과
        {
            // 높은 활동 시 간격 감소
            foreach (var timer in _timers.Values)
            {
                timer.DecreaseInterval(0.8f);
            }
        }
    }
    
    public async Task<bool> ShouldSkipOperation(string operationType)
    {
        // 배터리 수준에 따른 작업 스킵 결정
        var batteryLevel = await _powerTracker.GetBatteryLevel();
        
        return operationType switch
        {
            "quality_measurement" when batteryLevel < 20 => true,
            "full_state_sync" when batteryLevel < 15 => true,
            "peer_discovery" when batteryLevel < 10 => true,
            _ => false
        };
    }
    
    public PacketBatchProcessor CreateBatchProcessor()
    {
        var settings = GetOptimizationSettings(_currentMode);
        return new PacketBatchProcessor(settings.PacketBatchSize);
    }
}

public class AdaptiveTimer
{
    private Timer? _timer;
    private int _currentInterval;
    private readonly int _baseInterval;
    private readonly int _minInterval;
    private readonly int _maxInterval;
    private readonly object _lock = new();
    
    public event Action? Elapsed;
    
    public AdaptiveTimer(int baseInterval)
    {
        _baseInterval = baseInterval;
        _currentInterval = baseInterval;
        _minInterval = Math.Max(100, baseInterval / 10);
        _maxInterval = baseInterval * 10;
        
        _timer = new Timer(OnElapsed, null, _currentInterval, _currentInterval);
    }
    
    public void UpdateInterval(int newInterval)
    {
        lock (_lock)
        {
            _currentInterval = Math.Clamp(newInterval, _minInterval, _maxInterval);
            _timer?.Change(_currentInterval, _currentInterval);
        }
    }
    
    public void IncreaseInterval(float multiplier)
    {
        lock (_lock)
        {
            var newInterval = (int)(_currentInterval * multiplier);
            UpdateInterval(newInterval);
        }
    }
    
    public void DecreaseInterval(float multiplier)
    {
        lock (_lock)
        {
            var newInterval = (int)(_currentInterval * multiplier);
            UpdateInterval(newInterval);
        }
    }
    
    private void OnElapsed(object? state)
    {
        Elapsed?.Invoke();
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}

public class NetworkActivityMonitor
{
    private long _lastBytesSent;
    private long _lastBytesReceived;
    private long _lastMeasureTime;
    private readonly Timer _measureTimer;
    
    public event Action<NetworkActivity>? ActivityChanged;
    
    public NetworkActivityMonitor()
    {
        _lastMeasureTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _measureTimer = new Timer(MeasureActivity, null, 0, 5000); // 5초마다 측정
    }
    
    private void MeasureActivity(object? state)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var timeDelta = currentTime - _lastMeasureTime;
        
        if (timeDelta <= 0) return;
        
        // 실제 환경에서는 NetworkInterface.GetAllNetworkInterfaces() 사용
        var currentBytesSent = GetNetworkBytesSent();
        var currentBytesReceived = GetNetworkBytesReceived();
        
        var bytesSentDelta = currentBytesSent - _lastBytesSent;
        var bytesReceivedDelta = currentBytesReceived - _lastBytesReceived;
        
        var activity = new NetworkActivity
        {
            BytesPerSecond = (bytesSentDelta + bytesReceivedDelta) * 1000 / timeDelta,
            PacketsPerSecond = EstimatePacketsPerSecond(bytesSentDelta + bytesReceivedDelta, timeDelta),
            Timestamp = currentTime
        };
        
        _lastBytesSent = currentBytesSent;
        _lastBytesReceived = currentBytesReceived;
        _lastMeasureTime = currentTime;
        
        ActivityChanged?.Invoke(activity);
    }
    
    private long GetNetworkBytesSent()
    {
        // 실제 구현에서는 시스템 네트워크 통계 사용
        return Environment.TickCount64; // 시뮬레이션
    }
    
    private long GetNetworkBytesReceived()
    {
        // 실제 구현에서는 시스템 네트워크 통계 사용
        return Environment.TickCount64; // 시뮬레이션
    }
    
    private long EstimatePacketsPerSecond(long bytes, long timeMs)
    {
        const int averagePacketSize = 1024; // 평균 패킷 크기 가정
        return (bytes / averagePacketSize) * 1000 / timeMs;
    }
    
    public void Dispose()
    {
        _measureTimer?.Dispose();
    }
}

public class PowerStateTracker
{
    private PowerState _currentState;
    private readonly Timer _checkTimer;
    
    public event Action<PowerState>? PowerStateChanged;
    
    public PowerStateTracker()
    {
        _currentState = PowerState.Normal;
        _checkTimer = new Timer(CheckPowerState, null, 0, 10000); // 10초마다 확인
    }
    
    private void CheckPowerState(object? state)
    {
        var newState = GetCurrentPowerState();
        
        if (newState != _currentState)
        {
            _currentState = newState;
            PowerStateChanged?.Invoke(newState);
        }
    }
    
    private PowerState GetCurrentPowerState()
    {
        // 실제 구현에서는 플랫폼별 배터리 API 사용
        // Windows: SystemInformation.PowerStatus
        // 여기서는 시뮬레이션
        
        var batteryLevel = new Random().Next(0, 100);
        var isCharging = new Random().Next(0, 2) == 1;
        
        if (isCharging)
            return PowerState.Charging;
        
        if (batteryLevel < 20)
            return PowerState.LowBattery;
        
        return PowerState.Normal;
    }
    
    public async Task<int> GetBatteryLevel()
    {
        // 실제 구현에서는 플랫폼별 배터리 수준 API 사용
        await Task.Delay(10); // 시뮬레이션
        return new Random().Next(0, 100);
    }
    
    public void Dispose()
    {
        _checkTimer?.Dispose();
    }
}

public class PacketBatchProcessor
{
    private readonly Queue<NetworkPacket> _packetQueue;
    private readonly Timer _flushTimer;
    private readonly int _batchSize;
    private readonly object _lock = new();
    
    public event Action<List<NetworkPacket>>? BatchReady;
    
    public PacketBatchProcessor(int batchSize)
    {
        _batchSize = batchSize;
        _packetQueue = new Queue<NetworkPacket>();
        _flushTimer = new Timer(FlushBatch, null, 100, 100); // 100ms마다 플러시
    }
    
    public void AddPacket(NetworkPacket packet)
    {
        lock (_lock)
        {
            _packetQueue.Enqueue(packet);
            
            if (_packetQueue.Count >= _batchSize)
            {
                FlushBatch(null);
            }
        }
    }
    
    private void FlushBatch(object? state)
    {
        List<NetworkPacket> batch;
        
        lock (_lock)
        {
            if (_packetQueue.Count == 0)
                return;
            
            batch = new List<NetworkPacket>();
            var count = Math.Min(_batchSize, _packetQueue.Count);
            
            for (int i = 0; i < count; i++)
            {
                batch.Add(_packetQueue.Dequeue());
            }
        }
        
        BatchReady?.Invoke(batch);
    }
    
    public void Dispose()
    {
        _flushTimer?.Dispose();
    }
}

public class NetworkPacket
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public IPEndPoint Destination { get; set; } = new(IPAddress.Any, 0);
    public PacketPriority Priority { get; set; }
    public long Timestamp { get; set; }
}

public class OptimizationSettings
{
    public int HeartbeatInterval { get; set; }
    public int StateSyncInterval { get; set; }
    public int NATKeepAliveInterval { get; set; }
    public int QualityCheckInterval { get; set; }
    public int MaxConcurrentConnections { get; set; }
    public int PacketBatchSize { get; set; }
}

public class NetworkActivity
{
    public long BytesPerSecond { get; set; }
    public long PacketsPerSecond { get; set; }
    public long Timestamp { get; set; }
}

public enum BatteryOptimizationMode
{
    Performance,
    Balanced,
    PowerSaver
}

public enum PowerState
{
    Normal,
    LowBattery,
    Charging
}

public enum PacketPriority
{
    Low,
    Normal,
    High,
    Critical
}
```

## 13.3 네트워크 전환 처리 (WiFi-셀룰러)

모바일 기기는 WiFi와 셀룰러 네트워크 간 전환이 빈번하게 발생합니다. P2P 연결을 유지하면서 이런 전환을 처리하는 시스템이 필요합니다.

### 13.3.1 네트워크 전환 관리 시스템

```csharp
public class MobileNetworkTransitionManager
{
    private readonly Dictionary<string, P2PConnection> _activeConnections;
    private readonly NetworkMonitor _networkMonitor;
    private readonly ConnectionRecoveryManager _recoveryManager;
    private NetworkInfo _currentNetwork;
    private readonly object _lockObject = new();
    
    public event Action<NetworkTransitionEvent>? NetworkTransition;
    public event Action<string, ConnectionState>? ConnectionStateChanged;
    
    public MobileNetworkTransitionManager()
    {
        _activeConnections = new Dictionary<string, P2PConnection>();
        _networkMonitor = new NetworkMonitor();
        _recoveryManager = new ConnectionRecoveryManager();
        _currentNetwork = new NetworkInfo();
        
        _networkMonitor.NetworkChanged += OnNetworkChanged;
        _recoveryManager.RecoveryCompleted += OnConnectionRecovered;
    }
    
    public async Task<bool> InitializeConnection(string peerId, IPEndPoint initialEndpoint)
    {
        try
        {
            var connection = new P2PConnection(peerId, initialEndpoint);
            
            lock (_lockObject)
            {
                _activeConnections[peerId] = connection;
            }
            
            connection.StateChanged += (state) => 
            {
                ConnectionStateChanged?.Invoke(peerId, state);
                
                if (state == ConnectionState.Disconnected)
                {
                    _ = Task.Run(() => HandleConnectionLoss(peerId));
                }
            };
            
            await connection.EstablishConnection();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"연결 초기화 실패 {peerId}: {ex.Message}");
            return false;
        }
    }
    
    private async void OnNetworkChanged(NetworkChangeInfo changeInfo)
    {
        Console.WriteLine($"네트워크 변경 감지: {changeInfo.OldNetwork?.Type} -> {changeInfo.NewNetwork?.Type}");
        
        var transitionEvent = new NetworkTransitionEvent
        {
            OldNetwork = _currentNetwork,
            NewNetwork = changeInfo.NewNetwork ?? new NetworkInfo(),
            TransitionTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Reason = changeInfo.Reason
        };
        
        _currentNetwork = transitionEvent.NewNetwork;
        
        // 모든 활성 연결에 대해 전환 처리
        await HandleNetworkTransition(transitionEvent);
        
        NetworkTransition?.Invoke(transitionEvent);
    }
    
    private async Task HandleNetworkTransition(NetworkTransitionEvent transitionEvent)
    {
        var connections = new Dictionary<string, P2PConnection>();
        
        lock (_lockObject)
        {
            foreach (var kvp in _activeConnections)
            {
                connections[kvp.Key] = kvp.Value;
            }
        }
        
        var transitionTasks = connections.Select(async kvp =>
        {
            var peerId = kvp.Key;
            var connection = kvp.Value;
            
            try
            {
                await ProcessConnectionTransition(peerId, connection, transitionEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 전환 처리 실패 {peerId}: {ex.Message}");
            }
        });
        
        await Task.WhenAll(transitionTasks);
    }
    
    private async Task ProcessConnectionTransition(string peerId, P2PConnection connection, NetworkTransitionEvent transitionEvent)
    {
        // 1. 기존 연결 상태 저장
        var oldState = connection.SaveState();
        
        // 2. 새 네트워크에서 NAT 타입 재분석
        var natAnalyzer = new MobileNATAnalyzer();
        var newNATProfile = await natAnalyzer.AnalyzeMobileNAT(
            transitionEvent.NewNetwork.Type, 
            transitionEvent.NewNetwork.CarrierInfo
        );
        
        // 3. 연결 전환 전략 결정
        var strategy = DetermineTransitionStrategy(transitionEvent, newNATProfile);
        
        // 4. 전환 전략 실행
        switch (strategy)
        {
            case TransitionStrategy.QuickReconnect:
                await ExecuteQuickReconnect(peerId, connection, oldState);
                break;
            case TransitionStrategy.FullReestablish:
                await ExecuteFullReestablish(peerId, connection, oldState);
                break;
            case TransitionStrategy.ParallelConnect:
                await ExecuteParallelConnect(peerId, connection, oldState);
                break;
            case TransitionStrategy.GracefulMigrate:
                await ExecuteGracefulMigration(peerId, connection, oldState);
                break;
        }
    }
    
    private TransitionStrategy DetermineTransitionStrategy(NetworkTransitionEvent transition, MobileNATProfile natProfile)
    {
        // WiFi -> 셀룰러 전환
        if (transition.OldNetwork.Type == NetworkType.WiFi && 
            transition.NewNetwork.Type == NetworkType.Cellular4G)
        {
            return natProfile.NATType == NATType.Symmetric ? 
                TransitionStrategy.FullReestablish : 
                TransitionStrategy.QuickReconnect;
        }
        
        // 셀룰러 -> WiFi 전환
        if (transition.OldNetwork.Type == NetworkType.Cellular4G && 
            transition.NewNetwork.Type == NetworkType.WiFi)
        {
            return TransitionStrategy.ParallelConnect;
        }
        
        // 셀룰러 타워 변경
        if (transition.OldNetwork.Type == NetworkType.Cellular4G && 
            transition.NewNetwork.Type == NetworkType.Cellular4G)
        {
            return TransitionStrategy.GracefulMigrate;
        }
        
        return TransitionStrategy.QuickReconnect;
    }
    
    private async Task ExecuteQuickReconnect(string peerId, P2PConnection connection, ConnectionState oldState)
    {
        Console.WriteLine($"빠른 재연결 시도: {peerId}");
        
        try
        {
            // 기존 엔드포인트로 즉시 재연결 시도
            await connection.QuickReconnect();
            
            // 연결 성공 시 상태 복원
            if (connection.IsConnected)
            {
                await connection.RestoreState(oldState);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"빠른 재연결 실패: {ex.Message}");
            await ExecuteFullReestablish(peerId, connection, oldState);
        }
    }
    
    private async Task ExecuteFullReestablish(string peerId, P2PConnection connection, ConnectionState oldState)
    {
        Console.WriteLine($"전체 재설정 시도: {peerId}");
        
        try
        {
            // 기존 연결 완전 종료
            await connection.Disconnect();
            
            // NAT 홀펀칭부터 다시 시작
            await connection.ReestablishFromScratch();
            
            // 상태 복원
            if (connection.IsConnected)
            {
                await connection.RestoreState(oldState);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"전체 재설정 실패: {ex.Message}");
            _recoveryManager.ScheduleRecovery(peerId, connection, oldState);
        }
    }
    
    private async Task ExecuteParallelConnect(string peerId, P2PConnection connection, ConnectionState oldState)
    {
        Console.WriteLine($"병렬 연결 시도: {peerId}");
        
        try
        {
            // 새 네트워크로 병렬 연결 시도
            var newConnection = new P2PConnection(peerId, connection.RemoteEndpoint);
            var connectTask = newConnection.EstablishConnection();
            
            // 기존 연결도 유지하면서 대기
            var timeoutTask = Task.Delay(5000); // 5초 타임아웃
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == connectTask && newConnection.IsConnected)
            {
                // 새 연결 성공 시 기존 연결 교체
                await connection.Disconnect();
                lock (_lockObject)
                {
                    _activeConnections[peerId] = newConnection;
                }
                await newConnection.RestoreState(oldState);
            }
            else
            {
                // 새 연결 실패 시 기존 연결 유지 시도
                newConnection.Dispose();
                await ExecuteQuickReconnect(peerId, connection, oldState);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"병렬 연결 실패: {ex.Message}");
            await ExecuteQuickReconnect(peerId, connection, oldState);
        }
    }
    
    private async Task ExecuteGracefulMigration(string peerId, P2PConnection connection, ConnectionState oldState)
    {
        Console.WriteLine($"순차적 마이그레이션 시도: {peerId}");
        
        try
        {
            // 1. 피어에게 마이그레이션 시작 알림
            await connection.SendMigrationStart();
            
            // 2. 새 네트워크 정보 획득
            var newLocalEndpoint = await GetNewLocalEndpoint();
            
            // 3. 피어에게 새 엔드포인트 정보 전송
            await connection.SendNewEndpoint(newLocalEndpoint);
            
            // 4. 새 엔드포인트로 연결 확인
            await connection.ConfirmNewConnection();
            
            // 5. 기존 연결 종료
            await connection.CloseOldConnection();
            
            Console.WriteLine($"순차적 마이그레이션 완료: {peerId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"순차적 마이그레이션 실패: {ex.Message}");
            await ExecuteQuickReconnect(peerId, connection, oldState);
        }
    }
    
    private async Task HandleConnectionLoss(string peerId)
    {
        if (!_activeConnections.TryGetValue(peerId, out var connection))
            return;
        
        Console.WriteLine($"연결 손실 감지: {peerId}");
        
        // 연결 복구 시도
        _recoveryManager.ScheduleRecovery(peerId, connection, connection.SaveState());
    }
    
    private void OnConnectionRecovered(string peerId, P2PConnection newConnection)
    {
        lock (_lockObject)
        {
            if (_activeConnections.TryGetValue(peerId, out var oldConnection))
            {
                oldConnection.Dispose();
            }
            _activeConnections[peerId] = newConnection;
        }
        
        Console.WriteLine($"연결 복구 완료: {peerId}");
        ConnectionStateChanged?.Invoke(peerId, ConnectionState.Connected);
    }
    
    private async Task<IPEndPoint> GetNewLocalEndpoint()
    {
        // 새 네트워크에서의 로컬 엔드포인트 획득
        // 실제로는 STUN을 통해 외부 엔드포인트 확인
        await Task.Delay(100); // 시뮬레이션
        return new IPEndPoint(IPAddress.Any, 0);
    }
    
    public async Task<NetworkQuality> MeasureNetworkQuality()
    {
        var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        try
        {
            // 간단한 핑 테스트
            using var client = new UdpClient();
            var testServer = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            
            await client.SendAsync(testData, testServer);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var result = await client.ReceiveAsync();
            
            var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime;
            
            return new NetworkQuality
            {
                Latency = (int)latency,
                PacketLoss = 0,
                Bandwidth = EstimateBandwidth(),
                Stability = CalculateStability(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
        catch
        {
            return new NetworkQuality
            {
                Latency = 999,
                PacketLoss = 100,
                Bandwidth = 0,
                Stability = 0,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
    
    private long EstimateBandwidth()
    {
        // 대략적인 대역폭 추정
        return _currentNetwork.Type switch
        {
            NetworkType.WiFi => 50_000_000, // 50Mbps
            NetworkType.Cellular5G => 100_000_000, // 100Mbps
            NetworkType.Cellular4G => 20_000_000, // 20Mbps
            _ => 1_000_000 // 1Mbps
        };
    }
    
    private float CalculateStability()
    {
        // 네트워크 안정성 계산 (0.0 ~ 1.0)
        return _currentNetwork.Type switch
        {
            NetworkType.WiFi => 0.9f,
            NetworkType.Cellular5G => 0.8f,
            NetworkType.Cellular4G => 0.7f,
            _ => 0.5f
        };
    }
    
    public void Dispose()
    {
        _networkMonitor?.Dispose();
        _recoveryManager?.Dispose();
        
        lock (_lockObject)
        {
            foreach (var connection in _activeConnections.Values)
            {
                connection?.Dispose();
            }
            _activeConnections.Clear();
        }
    }
}

public class NetworkMonitor
{
    private NetworkInfo _currentNetwork;
    private readonly Timer _monitorTimer;
    
    public event Action<NetworkChangeInfo>? NetworkChanged;
    
    public NetworkMonitor()
    {
        _currentNetwork = GetCurrentNetworkInfo();
        _monitorTimer = new Timer(CheckNetworkChanges, null, 0, 2000); // 2초마다 확인
    }
    
    private void CheckNetworkChanges(object? state)
    {
        var newNetwork = GetCurrentNetworkInfo();
        
        if (!NetworkInfoEquals(_currentNetwork, newNetwork))
        {
            var changeInfo = new NetworkChangeInfo
            {
                OldNetwork = _currentNetwork,
                NewNetwork = newNetwork,
                ChangeTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Reason = DetermineChangeReason(_currentNetwork, newNetwork)
            };
            
            _currentNetwork = newNetwork;
            NetworkChanged?.Invoke(changeInfo);
        }
    }
    
    private NetworkInfo GetCurrentNetworkInfo()
    {
        // 실제 구현에서는 플랫폼별 네트워크 API 사용
        // 시뮬레이션 데이터
        var random = new Random();
        var types = Enum.GetValues<NetworkType>();
        var selectedType = types[random.Next(types.Length)];
        
        return new NetworkInfo
        {
            Type = selectedType,
            SSID = selectedType == NetworkType.WiFi ? "TestWiFi" : null,
            CarrierInfo = selectedType != NetworkType.WiFi ? "TestCarrier" : string.Empty,
            SignalStrength = random.Next(-100, -30),
            IsMetered = selectedType != NetworkType.WiFi
        };
    }
    
    private bool NetworkInfoEquals(NetworkInfo network1, NetworkInfo network2)
    {
        return network1.Type == network2.Type &&
               network1.SSID == network2.SSID &&
               network1.CarrierInfo == network2.CarrierInfo;
    }
    
    private NetworkChangeReason DetermineChangeReason(NetworkInfo oldNetwork, NetworkInfo newNetwork)
    {
        if (oldNetwork.Type != newNetwork.Type)
        {
            return NetworkChangeReason.NetworkTypeChanged;
        }
        
        if (oldNetwork.SSID != newNetwork.SSID)
        {
            return NetworkChangeReason.WiFiNetworkChanged;
        }
        
        if (oldNetwork.CarrierInfo != newNetwork.CarrierInfo)
        {
            return NetworkChangeReason.CellularTowerChanged;
        }
        
        return NetworkChangeReason.Other;
    }
    
    public void Dispose()
    {
        _monitorTimer?.Dispose();
    }
}

public class ConnectionRecoveryManager
{
    private readonly Dictionary<string, RecoveryAttempt> _recoveryAttempts;
    private readonly Timer _recoveryTimer;
    private readonly object _lockObject = new();
    
    public event Action<string, P2PConnection>? RecoveryCompleted;
    
    public ConnectionRecoveryManager()
    {
        _recoveryAttempts = new Dictionary<string, RecoveryAttempt>();
        _recoveryTimer = new Timer(ProcessRecoveries, null, 0, 1000); // 1초마다 처리
    }
    
    public void ScheduleRecovery(string peerId, P2PConnection connection, ConnectionState state)
    {
        lock (_lockObject)
        {
            _recoveryAttempts[peerId] = new RecoveryAttempt
            {
                PeerId = peerId,
                Connection = connection,
                SavedState = state,
                AttemptCount = 0,
                NextAttemptTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1000,
                MaxAttempts = 5
            };
        }
        
        Console.WriteLine($"연결 복구 예약: {peerId}");
    }
    
    private async void ProcessRecoveries(object? state)
    {
        var attempts = new Dictionary<string, RecoveryAttempt>();
        
        lock (_lockObject)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            foreach (var kvp in _recoveryAttempts.ToList())
            {
                if (kvp.Value.NextAttemptTime <= currentTime)
                {
                    attempts[kvp.Key] = kvp.Value;
                }
            }
        }
        
        foreach (var attempt in attempts.Values)
        {
            await ProcessSingleRecovery(attempt);
        }
    }
    
    private async Task ProcessSingleRecovery(RecoveryAttempt attempt)
    {
        try
        {
            Console.WriteLine($"연결 복구 시도 {attempt.AttemptCount + 1}/{attempt.MaxAttempts}: {attempt.PeerId}");
            
            attempt.AttemptCount++;
            
            // 연결 복구 시도
            var success = await AttemptRecovery(attempt);
            
            if (success)
            {
                lock (_lockObject)
                {
                    _recoveryAttempts.Remove(attempt.PeerId);
                }
                
                RecoveryCompleted?.Invoke(attempt.PeerId, attempt.Connection);
                return;
            }
            
            // 복구 실패 시 재시도 스케줄링
            if (attempt.AttemptCount < attempt.MaxAttempts)
            {
                var delay = Math.Min(30000, 1000 * (int)Math.Pow(2, attempt.AttemptCount)); // 지수 백오프
                attempt.NextAttemptTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + delay;
            }
            else
            {
                // 최대 시도 횟수 초과
                lock (_lockObject)
                {
                    _recoveryAttempts.Remove(attempt.PeerId);
                }
                
                Console.WriteLine($"연결 복구 포기: {attempt.PeerId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"연결 복구 처리 오류: {ex.Message}");
        }
    }
    
    private async Task<bool> AttemptRecovery(RecoveryAttempt attempt)
    {
        try
        {
            // 새로운 연결 시도
            var newConnection = new P2PConnection(attempt.PeerId, attempt.Connection.RemoteEndpoint);
            await newConnection.EstablishConnection();
            
            if (newConnection.IsConnected)
            {
                // 상태 복원
                await newConnection.RestoreState(attempt.SavedState);
                
                // 기존 연결 정리
                attempt.Connection.Dispose();
                attempt.Connection = newConnection;
                
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"복구 시도 실패: {ex.Message}");
        }
        
        return false;
    }
    
    public void Dispose()
    {
        _recoveryTimer?.Dispose();
        
        lock (_lockObject)
        {
            foreach (var attempt in _recoveryAttempts.Values)
            {
                attempt.Connection?.Dispose();
            }
            _recoveryAttempts.Clear();
        }
    }
}

// 모델 클래스들
public class NetworkInfo
{
    public NetworkType Type { get; set; }
    public string? SSID { get; set; }
    public string CarrierInfo { get; set; } = string.Empty;
    public int SignalStrength { get; set; }
    public bool IsMetered { get; set; }
}

public class NetworkChangeInfo
{
    public NetworkInfo? OldNetwork { get; set; }
    public NetworkInfo? NewNetwork { get; set; }
    public long ChangeTime { get; set; }
    public NetworkChangeReason Reason { get; set; }
}

public class NetworkTransitionEvent
{
    public NetworkInfo OldNetwork { get; set; } = new();
    public NetworkInfo NewNetwork { get; set; } = new();
    public long TransitionTime { get; set; }
    public NetworkChangeReason Reason { get; set; }
}

public class P2PConnection : IDisposable
{
    public string PeerId { get; }
    public IPEndPoint RemoteEndpoint { get; }
    public bool IsConnected { get; private set; }
    
    public event Action<ConnectionState>? StateChanged;
    
    public P2PConnection(string peerId, IPEndPoint remoteEndpoint)
    {
        PeerId = peerId;
        RemoteEndpoint = remoteEndpoint;
    }
    
    public async Task EstablishConnection()
    {
        await Task.Delay(1000); // 시뮬레이션
        IsConnected = true;
        StateChanged?.Invoke(ConnectionState.Connected);
    }
    
    public async Task QuickReconnect()
    {
        await Task.Delay(500); // 시뮬레이션
        IsConnected = true;
        StateChanged?.Invoke(ConnectionState.Connected);
    }
    
    public async Task ReestablishFromScratch()
    {
        await Task.Delay(2000); // 시뮬레이션
        IsConnected = true;
        StateChanged?.Invoke(ConnectionState.Connected);
    }
    
    public async Task Disconnect()
    {
        await Task.Delay(100); // 시뮬레이션
        IsConnected = false;
        StateChanged?.Invoke(ConnectionState.Disconnected);
    }
    
    public ConnectionState SaveState()
    {
        return new ConnectionState();
    }
    
    public async Task RestoreState(ConnectionState state)
    {
        await Task.Delay(100); // 시뮬레이션
    }
    
    public async Task SendMigrationStart()
    {
        await Task.Delay(50); // 시뮬레이션
    }
    
    public async Task SendNewEndpoint(IPEndPoint endpoint)
    {
        await Task.Delay(50); // 시뮬레이션
    }
    
    public async Task ConfirmNewConnection()
    {
        await Task.Delay(100); // 시뮬레이션
    }
    
    public async Task CloseOldConnection()
    {
        await Task.Delay(50); // 시뮬레이션
    }
    
    public void Dispose()
    {
        IsConnected = false;
    }
}

public class ConnectionState
{
    // 연결 상태 정보
}

public class RecoveryAttempt
{
    public string PeerId { get; set; } = string.Empty;
    public P2PConnection Connection { get; set; } = null!;
    public ConnectionState SavedState { get; set; } = new();
    public int AttemptCount { get; set; }
    public long NextAttemptTime { get; set; }
    public int MaxAttempts { get; set; }
}

public class NetworkQuality
{
    public int Latency { get; set; }
    public float PacketLoss { get; set; }
    public long Bandwidth { get; set; }
    public float Stability { get; set; }
    public long Timestamp { get; set; }
}

public enum TransitionStrategy
{
    QuickReconnect,
    FullReestablish,
    ParallelConnect,
    GracefulMigrate
}

public enum NetworkChangeReason
{
    NetworkTypeChanged,
    WiFiNetworkChanged,
    CellularTowerChanged,
    SignalLoss,
    Other
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting
}
```

## 13.4 iOS/Android 플랫폼별 고려사항

각 모바일 플랫폼은 고유한 제약사항과 특성을 가지고 있어 플랫폼별 최적화가 필요합니다.

### 13.4.1 플랫폼별 최적화 시스템

```csharp
public abstract class PlatformOptimizer
{
    protected readonly Dictionary<string, object> _platformSettings;
    protected readonly BackgroundTaskManager _backgroundTaskManager;
    
    public event Action<PlatformEvent>? PlatformEventOccurred;
    
    protected PlatformOptimizer()
    {
        _platformSettings = new Dictionary<string, object>();
        _backgroundTaskManager = new BackgroundTaskManager();
    }
    
    public abstract Task InitializePlatformOptimizations();
    public abstract Task HandleBackgroundTransition();
    public abstract Task HandleForegroundTransition();
    public abstract Task<bool> RequestNetworkPermissions();
    public abstract Task<bool> RequestBackgroundPermissions();
    public abstract void ConfigureDataUsagePolicy(bool isMetered);
    public abstract Task<PlatformCapabilities> GetPlatformCapabilities();
    
    protected void RaisePlatformEvent(PlatformEventType type, string message)
    {
        PlatformEventOccurred?.Invoke(new PlatformEvent
        {
            Type = type,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }
}

public class iOSOptimizer : PlatformOptimizer
{
    private readonly Timer _backgroundHeartbeat;
    private bool _isInBackground;
    private readonly Dictionary<string, P2PConnection> _connections;
    
    public iOSOptimizer()
    {
        _connections = new Dictionary<string, P2PConnection>();
        _backgroundHeartbeat = new Timer(HandleBackgroundHeartbeat, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public override async Task InitializePlatformOptimizations()
    {
        Console.WriteLine("iOS 플랫폼 최적화 초기화");
        
        // iOS 특화 설정
        _platformSettings["background_app_refresh"] = true;
        _platformSettings["low_data_mode"] = false;
        _platformSettings["background_processing"] = true;
        
        // Network.framework 시뮬레이션
        await ConfigureNetworkFramework();
        
        // CallKit 통합 (VoIP 앱의 경우)
        await ConfigureCallKitIntegration();
        
        RaisePlatformEvent(PlatformEventType.Initialization, "iOS 최적화 완료");
    }
    
    private async Task ConfigureNetworkFramework()
    {
        // iOS Network.framework의 시뮬레이션
        await Task.Delay(100);
        
        // Path monitoring 설정
        _platformSettings["path_monitoring"] = true;
        
        // Quality of Service 설정
        _platformSettings["qos_level"] = "utility"; // background에서 사용
        
        Console.WriteLine("Network.framework 구성 완료");
    }
    
    private async Task ConfigureCallKitIntegration()
    {
        // VoIP 앱을 위한 CallKit 통합
        await Task.Delay(100);
        
        if (IsVoIPApp())
        {
            _platformSettings["callkit_enabled"] = true;
            _platformSettings["voip_pushes"] = true;
            Console.WriteLine("CallKit 통합 활성화");
        }
    }
    
    public override async Task HandleBackgroundTransition()
    {
        Console.WriteLine("iOS 백그라운드 전환 처리");
        
        _isInBackground = true;
        
        // 1. 백그라운드 태스크 시작
        await _backgroundTaskManager.BeginBackgroundTask("P2P_Maintenance");
        
        // 2. 연결 최소화
        await MinimizeConnections();
        
        // 3. 백그라운드 하트비트 시작 (VoIP 앱의 경우)
        if (IsVoIPApp())
        {
            _backgroundHeartbeat.Change(0, 25000); // 25초마다 (30초 제한 고려)
        }
        
        // 4. 데이터 사용량 최소화
        ConfigureDataUsagePolicy(true);
        
        RaisePlatformEvent(PlatformEventType.BackgroundTransition, "백그라운드 최적화 적용");
    }
    
    public override async Task HandleForegroundTransition()
    {
        Console.WriteLine("iOS 포그라운드 전환 처리");
        
        _isInBackground = false;
        
        // 1. 백그라운드 하트비트 중지
        _backgroundHeartbeat.Change(Timeout.Infinite, Timeout.Infinite);
        
        // 2. 전체 연결 복원
        await RestoreConnections();
        
        // 3. 백그라운드 태스크 종료
        await _backgroundTaskManager.EndBackgroundTask();
        
        // 4. 정상 데이터 사용량 복원
        ConfigureDataUsagePolicy(false);
        
        RaisePlatformEvent(PlatformEventType.ForegroundTransition, "포그라운드 최적화 적용");
    }
    
    private async Task MinimizeConnections()
    {
        // 중요하지 않은 연결 일시 중단
        var nonCriticalConnections = _connections.Values
            .Where(c => !IsCriticalConnection(c))
            .ToList();
        
        foreach (var connection in nonCriticalConnections)
        {
            await connection.SuspendConnection();
        }
        
        Console.WriteLine($"{nonCriticalConnections.Count}개 연결 일시 중단");
    }
    
    private async Task RestoreConnections()
    {
        // 일시 중단된 연결 복원
        var suspendedConnections = _connections.Values
            .Where(c => c.IsSuspended)
            .ToList();
        
        foreach (var connection in suspendedConnections)
        {
            await connection.ResumeConnection();
        }
        
        Console.WriteLine($"{suspendedConnections.Count}개 연결 복원");
    }
    
    private void HandleBackgroundHeartbeat(object? state)
    {
        if (!_isInBackground) return;
        
        try
        {
            // VoIP 푸시 또는 백그라운드 네트워크 활동
            SendKeepAliveToAllConnections();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"백그라운드 하트비트 오류: {ex.Message}");
        }
    }
    
    private void SendKeepAliveToAllConnections()
    {
        var activationCount = 0;
        
        foreach (var connection in _connections.Values.Where(c => !c.IsSuspended))
        {
            try
            {
                connection.SendKeepAlive();
                activationCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Keep-alive 전송 실패: {ex.Message}");
            }
        }
        
        Console.WriteLine($"백그라운드 Keep-alive 전송: {activationCount}개 연결");
    }
    
    public override async Task<bool> RequestNetworkPermissions()
    {
        // iOS에서는 자동으로 허용되지만 시뮬레이션
        await Task.Delay(100);
        return true;
    }
    
    public override async Task<bool> RequestBackgroundPermissions()
    {
        // 백그라운드 앱 새로 고침 권한 요청 시뮬레이션
        await Task.Delay(500);
        
        var granted = new Random().NextDouble() > 0.3; // 70% 확률로 허용
        
        if (granted)
        {
            _platformSettings["background_permitted"] = true;
            Console.WriteLine("백그라운드 권한 허용됨");
        }
        else
        {
            Console.WriteLine("백그라운드 권한 거부됨");
        }
        
        return granted;
    }
    
    public override void ConfigureDataUsagePolicy(bool isMetered)
    {
        if (isMetered || _isInBackground)
        {
            // 데이터 사용량 최소화
            _platformSettings["max_packet_size"] = 512;
            _platformSettings["heartbeat_interval"] = 30000; // 30초
            _platformSettings["quality_check_disabled"] = true;
        }
        else
        {
            // 정상 데이터 사용량
            _platformSettings["max_packet_size"] = 1400;
            _platformSettings["heartbeat_interval"] = 5000; // 5초
            _platformSettings["quality_check_disabled"] = false;
        }
        
        Console.WriteLine($"데이터 사용 정책 설정: {(isMetered ? "제한됨" : "정상")}");
    }
    
    public override async Task<PlatformCapabilities> GetPlatformCapabilities()
    {
        await Task.Delay(100);
        
        return new PlatformCapabilities
        {
            Platform = MobilePlatform.iOS,
            SupportsBackgroundNetworking = IsVoIPApp(),
            SupportsCallKit = IsVoIPApp(),
            MaxBackgroundTime = IsVoIPApp() ? int.MaxValue : 30, // VoIP 앱은 무제한
            SupportsNetworkPath = true,
            SupportsMultipleNetworks = true,
            BackgroundAppRefreshAvailable = (bool)_platformSettings.GetValueOrDefault("background_permitted", false)
        };
    }
    
    private bool IsVoIPApp()
    {
        // VoIP 앱 여부 확인 (실제로는 앱 설정에서)
        return _platformSettings.GetValueOrDefault("app_type", "game").ToString() == "voip";
    }
    
    private bool IsCriticalConnection(P2PConnection connection)
    {
        // 중요한 연결 여부 판단 (예: 음성 통화 연결)
        return connection.Priority == ConnectionPriority.Critical;
    }
}

public class AndroidOptimizer : PlatformOptimizer
{
    private readonly WorkManagerSimulator _workManager;
    private readonly DozeOptimizer _dozeOptimizer;
    private bool _isDozing;
    
    public AndroidOptimizer()
    {
        _workManager = new WorkManagerSimulator();
        _dozeOptimizer = new DozeOptimizer();
        
        _dozeOptimizer.DozeStateChanged += OnDozeStateChanged;
    }
    
    public override async Task InitializePlatformOptimizations()
    {
        Console.WriteLine("Android 플랫폼 최적화 초기화");
        
        // Android 특화 설정
        _platformSettings["battery_optimization_exempt"] = false;
        _platformSettings["data_saver_enabled"] = false;
        _platformSettings["doze_mode_active"] = false;
        
        // WorkManager 초기화
        await _workManager.Initialize();
        
        // Doze 모드 최적화
        await _dozeOptimizer.Initialize();
        
        // 네트워크 보안 정책 설정
        await ConfigureNetworkSecurityPolicy();
        
        RaisePlatformEvent(PlatformEventType.Initialization, "Android 최적화 완료");
    }
    
    private async Task ConfigureNetworkSecurityPolicy()
    {
        // Android Network Security Config 시뮬레이션
        await Task.Delay(100);
        
        _platformSettings["clear_text_permitted"] = false;
        _platformSettings["certificate_pinning"] = true;
        
        Console.WriteLine("네트워크 보안 정책 구성 완료");
    }
    
    public override async Task HandleBackgroundTransition()
    {
        Console.WriteLine("Android 백그라운드 전환 처리");
        
        // 1. WorkManager를 통한 백그라운드 작업 스케줄링
        await _workManager.ScheduleNetworkMaintenance();
        
        // 2. Doze 모드 대응
        await _dozeOptimizer.PrepareForDoze();
        
        // 3. 배터리 최적화 확인
        if (!await IsBatteryOptimizationExempt())
        {
            await HandleBatteryOptimizationRestrictions();
        }
        
        RaisePlatformEvent(PlatformEventType.BackgroundTransition, "백그라운드 작업 스케줄링 완료");
    }
    
    public override async Task HandleForegroundTransition()
    {
        Console.WriteLine("Android 포그라운드 전환 처리");
        
        // 1. 백그라운드 작업 취소
        await _workManager.CancelNetworkMaintenance();
        
        // 2. Doze 모드 해제
        await _dozeOptimizer.ExitDoze();
        
        // 3. 전체 연결 상태 확인 및 복원
        await VerifyAndRestoreConnections();
        
        RaisePlatformEvent(PlatformEventType.ForegroundTransition, "포그라운드 복원 완료");
    }
    
    private async Task<bool> IsBatteryOptimizationExempt()
    {
        // 배터리 최적화 면제 여부 확인
        await Task.Delay(50);
        return (bool)_platformSettings.GetValueOrDefault("battery_optimization_exempt", false);
    }
    
    private async Task HandleBatteryOptimizationRestrictions()
    {
        // 배터리 최적화로 인한 제약 처리
        Console.WriteLine("배터리 최적화 제약 하에서 동작");
        
        // 연결 수 제한
        var maxConnections = 2;
        
        // 하트비트 간격 증가
        _platformSettings["restricted_heartbeat_interval"] = 60000; // 1분
        
        // FCM을 통한 푸시 알림 활용
        await SetupFCMFallback();
        
        await Task.Delay(100);
    }
    
    private async Task SetupFCMFallback()
    {
        // Firebase Cloud Messaging 백업 시스템 설정
        await Task.Delay(100);
        _platformSettings["fcm_fallback"] = true;
        Console.WriteLine("FCM 백업 시스템 활성화");
    }
    
    private void OnDozeStateChanged(bool isDozing)
    {
        _isDozing = isDozing;
        
        if (isDozing)
        {
            Console.WriteLine("Doze 모드 진입 - 네트워크 활동 최소화");
            // 중요한 연결만 유지
        }
        else
        {
            Console.WriteLine("Doze 모드 해제 - 정상 네트워크 활동 복원");
            // 전체 연결 복원
        }
        
        RaisePlatformEvent(PlatformEventType.DozeStateChanged, 
            isDozing ? "Doze 모드 진입" : "Doze 모드 해제");
    }
    
    private async Task VerifyAndRestoreConnections()
    {
        // 백그라운드에서 손실되었을 수 있는 연결 확인 및 복원
        await Task.Delay(200);
        Console.WriteLine("연결 상태 확인 및 복원 완료");
    }
    
    public override async Task<bool> RequestNetworkPermissions()
    {
        // Android 네트워크 권한 요청
        await Task.Delay(200);
        
        var permissions = new[]
        {
            "android.permission.INTERNET",
            "android.permission.ACCESS_NETWORK_STATE",
            "android.permission.CHANGE_NETWORK_STATE"
        };
        
        // 권한 요청 시뮬레이션
        foreach (var permission in permissions)
        {
            var granted = new Random().NextDouble() > 0.1; // 90% 확률로 허용
            if (!granted)
            {
                Console.WriteLine($"권한 거부: {permission}");
                return false;
            }
        }
        
        Console.WriteLine("모든 네트워크 권한 허용됨");
        return true;
    }
    
    public override async Task<bool> RequestBackgroundPermissions()
    {
        // 백그라운드 권한 요청
        await Task.Delay(300);
        
        // 배터리 최적화 면제 요청
        var batteryExempt = await RequestBatteryOptimizationExemption();
        
        // 백그라운드 활동 권한
        var backgroundActivity = await RequestBackgroundActivityPermission();
        
        return batteryExempt || backgroundActivity;
    }
    
    private async Task<bool> RequestBatteryOptimizationExemption()
    {
        await Task.Delay(200);
        
        var granted = new Random().NextDouble() > 0.5; // 50% 확률로 허용
        if (granted)
        {
            _platformSettings["battery_optimization_exempt"] = true;
            Console.WriteLine("배터리 최적화 면제 허용됨");
        }
        else
        {
            Console.WriteLine("배터리 최적화 면제 거부됨");
        }
        
        return granted;
    }
    
    private async Task<bool> RequestBackgroundActivityPermission()
    {
        await Task.Delay(150);
        
        var granted = new Random().NextDouble() > 0.3; // 70% 확률로 허용
        if (granted)
        {
            Console.WriteLine("백그라운드 활동 권한 허용됨");
        }
        else
        {
            Console.WriteLine("백그라운드 활동 권한 거부됨");
        }
        
        return granted;
    }
    
    public override void ConfigureDataUsagePolicy(bool isMetered)
    {
        var isDataSaverEnabled = (bool)_platformSettings.GetValueOrDefault("data_saver_enabled", false);
        
        if (isMetered || isDataSaverEnabled || _isDozing)
        {
            // 데이터 절약 모드
            _platformSettings["max_packet_size"] = 256;
            _platformSettings["batch_packets"] = true;
            _platformSettings["compress_data"] = true;
            _platformSettings["heartbeat_interval"] = 45000; // 45초
        }
        else
        {
            // 정상 모드
            _platformSettings["max_packet_size"] = 1400;
            _platformSettings["batch_packets"] = false;
            _platformSettings["compress_data"] = false;
            _platformSettings["heartbeat_interval"] = 10000; // 10초
        }
        
        Console.WriteLine($"Android 데이터 사용 정책: {(isMetered ? "제한됨" : "정상")}");
    }
    
    public override async Task<PlatformCapabilities> GetPlatformCapabilities()
    {
        await Task.Delay(150);
        
        return new PlatformCapabilities
        {
            Platform = MobilePlatform.Android,
            SupportsBackgroundNetworking = await IsBatteryOptimizationExempt(),
            SupportsCallKit = false,
            MaxBackgroundTime = await IsBatteryOptimizationExempt() ? int.MaxValue : 60,
            SupportsNetworkPath = true,
            SupportsMultipleNetworks = true,
            BackgroundAppRefreshAvailable = await IsBatteryOptimizationExempt(),
            SupportsFCM = true,
            SupportsWorkManager = true
        };
    }
}

// 보조 클래스들
public class WorkManagerSimulator
{
    private bool _isInitialized;
    private readonly Timer _workTimer;
    
    public WorkManagerSimulator()
    {
        _workTimer = new Timer(ExecuteBackgroundWork, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public async Task Initialize()
    {
        await Task.Delay(100);
        _isInitialized = true;
        Console.WriteLine("WorkManager 초기화 완료");
    }
    
    public async Task ScheduleNetworkMaintenance()
    {
        if (!_isInitialized) return;
        
        await Task.Delay(50);
        _workTimer.Change(0, 900000); // 15분마다 실행
        Console.WriteLine("네트워크 유지 작업 스케줄링됨");
    }
    
    public async Task CancelNetworkMaintenance()
    {
        await Task.Delay(50);
        _workTimer.Change(Timeout.Infinite, Timeout.Infinite);
        Console.WriteLine("네트워크 유지 작업 취소됨");
    }
    
    private void ExecuteBackgroundWork(object? state)
    {
        try
        {
            Console.WriteLine("백그라운드 네트워크 유지 작업 실행");
            // 실제로는 P2P 연결 상태 확인 및 복구
        }
        catch (Exception ex)
        {
            Console.WriteLine($"백그라운드 작업 오류: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        _workTimer?.Dispose();
    }
}

public class DozeOptimizer
{
    private bool _isDozing;
    private readonly Timer _dozeSimulator;
    
    public event Action<bool>? DozeStateChanged;
    
    public DozeOptimizer()
    {
        _dozeSimulator = new Timer(SimulateDozeChanges, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public async Task Initialize()
    {
        await Task.Delay(50);
        _dozeSimulator.Change(0, 120000); // 2분마다 상태 변경 시뮬레이션
        Console.WriteLine("Doze 최적화 초기화 완료");
    }
    
    public async Task PrepareForDoze()
    {
        await Task.Delay(100);
        Console.WriteLine("Doze 모드 준비 완료");
    }
    
    public async Task ExitDoze()
    {
        await Task.Delay(100);
        if (_isDozing)
        {
            _isDozing = false;
            DozeStateChanged?.Invoke(false);
        }
    }
    
    private void SimulateDozeChanges(object? state)
    {
        var newDozeState = new Random().NextDouble() > 0.7; // 30% 확률로 Doze
        
        if (newDozeState != _isDozing)
        {
            _isDozing = newDozeState;
            DozeStateChanged?.Invoke(_isDozing);
        }
    }
    
    public void Dispose()
    {
        _dozeSimulator?.Dispose();
    }
}

public class BackgroundTaskManager
{
    private readonly Dictionary<string, BackgroundTask> _activeTasks;
    
    public BackgroundTaskManager()
    {
        _activeTasks = new Dictionary<string, BackgroundTask>();
    }
    
    public async Task BeginBackgroundTask(string taskName)
    {
        await Task.Delay(50);
        
        var task = new BackgroundTask
        {
            Name = taskName,
            StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        
        _activeTasks[taskName] = task;
        Console.WriteLine($"백그라운드 태스크 시작: {taskName}");
    }
    
    public async Task EndBackgroundTask()
    {
        await Task.Delay(50);
        
        foreach (var task in _activeTasks.Values)
        {
            Console.WriteLine($"백그라운드 태스크 종료: {task.Name}");
        }
        
        _activeTasks.Clear();
    }
}

public class BackgroundTask
{
    public string Name { get; set; } = string.Empty;
    public long StartTime { get; set; }
}

// 확장된 P2P 연결 클래스
public partial class P2PConnection
{
    public bool IsSuspended { get; private set; }
    public ConnectionPriority Priority { get; set; } = ConnectionPriority.Normal;
    
    public async Task SuspendConnection()
    {
        await Task.Delay(100);
        IsSuspended = true;
        Console.WriteLine($"연결 일시 중단: {PeerId}");
    }
    
    public async Task ResumeConnection()
    {
        await Task.Delay(200);
        IsSuspended = false;
        Console.WriteLine($"연결 복원: {PeerId}");
    }
    
    public void SendKeepAlive()
    {
        // Keep-alive 패킷 전송
        Console.WriteLine($"Keep-alive 전송: {PeerId}");
    }
}

// 모델 클래스들
public class PlatformCapabilities
{
    public MobilePlatform Platform { get; set; }
    public bool SupportsBackgroundNetworking { get; set; }
    public bool SupportsCallKit { get; set; }
    public int MaxBackgroundTime { get; set; } // 초 단위
    public bool SupportsNetworkPath { get; set; }
    public bool SupportsMultipleNetworks { get; set; }
    public bool BackgroundAppRefreshAvailable { get; set; }
    public bool SupportsFCM { get; set; }
    public bool SupportsWorkManager { get; set; }
}

public class PlatformEvent
{
    public PlatformEventType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

public enum MobilePlatform
{
    iOS,
    Android,
    Unknown
}

public enum PlatformEventType
{
    Initialization,
    BackgroundTransition,
    ForegroundTransition,
    DozeStateChanged,
    PermissionGranted,
    PermissionDenied
}

public enum ConnectionPriority
{
    Low,
    Normal,
    High,
    Critical
}

// 콘솔 시연 프로그램
class MobilePlatformDemo
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 모바일 P2P 홀펀칭 시연 ===");
        Console.WriteLine("1. iOS 시뮬레이션");
        Console.WriteLine("2. Android 시뮬레이션");
        Console.WriteLine("3. 네트워크 전환 테스트");
        Console.WriteLine("4. 배터리 최적화 테스트");
        Console.Write("선택: ");
        
        var choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                await RuniOSSimulation();
                break;
            case "2":
                await RunAndroidSimulation();
                break;
            case "3":
                await RunNetworkTransitionTest();
                break;
            case "4":
                await RunBatteryOptimizationTest();
                break;
            default:
                Console.WriteLine("잘못된 선택입니다.");
                break;
        }
    }
    
    static async Task RuniOSSimulation()
    {
        Console.WriteLine("\n=== iOS 플랫폼 시뮬레이션 ===");
        
        var optimizer = new iOSOptimizer();
        
        optimizer.PlatformEventOccurred += (e) =>
        {
            Console.WriteLine($"[iOS 이벤트] {e.Type}: {e.Message}");
        };
        
        await optimizer.InitializePlatformOptimizations();
        
        var capabilities = await optimizer.GetPlatformCapabilities();
        Console.WriteLine($"백그라운드 네트워킹 지원: {capabilities.SupportsBackgroundNetworking}");
        Console.WriteLine($"최대 백그라운드 시간: {capabilities.MaxBackgroundTime}초");
        
        // 권한 요청 시뮬레이션
        await optimizer.RequestNetworkPermissions();
        await optimizer.RequestBackgroundPermissions();
        
        // 백그라운드/포그라운드 전환 시뮬레이션
        Console.WriteLine("\n백그라운드 전환 시뮬레이션...");
        await optimizer.HandleBackgroundTransition();
        
        await Task.Delay(3000);
        
        Console.WriteLine("\n포그라운드 전환 시뮬레이션...");
        await optimizer.HandleForegroundTransition();
        
        Console.WriteLine("\niOS 시뮬레이션 완료");
    }
    
    static async Task RunAndroidSimulation()
    {
        Console.WriteLine("\n=== Android 플랫폼 시뮬레이션 ===");
        
        var optimizer = new AndroidOptimizer();
        
        optimizer.PlatformEventOccurred += (e) =>
        {
            Console.WriteLine($"[Android 이벤트] {e.Type}: {e.Message}");
        };
        
        await optimizer.InitializePlatformOptimizations();
        
        var capabilities = await optimizer.GetPlatformCapabilities();
        Console.WriteLine($"WorkManager 지원: {capabilities.SupportsWorkManager}");
        Console.WriteLine($"FCM 지원: {capabilities.SupportsFCM}");
        
        // 권한 요청 시뮬레이션
        await optimizer.RequestNetworkPermissions();
        await optimizer.RequestBackgroundPermissions();
        
        // 백그라운드 처리 시뮬레이션
        Console.WriteLine("\nDoze 모드 시뮬레이션 시작...");
        await optimizer.HandleBackgroundTransition();
        
        await Task.Delay(5000);
        
        await optimizer.HandleForegroundTransition();
        
        Console.WriteLine("\nAndroid 시뮬레이션 완료");
    }
    
    static async Task RunNetworkTransitionTest()
    {
        Console.WriteLine("\n=== 네트워크 전환 테스트 ===");
        
        var transitionManager = new MobileNetworkTransitionManager();
        
        transitionManager.NetworkTransition += (e) =>
        {
            Console.WriteLine($"네트워크 전환: {e.OldNetwork.Type} -> {e.NewNetwork.Type}");
        };
        
        transitionManager.ConnectionStateChanged += (peerId, state) =>
        {
            Console.WriteLine($"연결 상태 변경 [{peerId}]: {state}");
        };
        
        // 가상 피어 연결
        var peer1Endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 12345);
        await transitionManager.InitializeConnection("peer1", peer1Endpoint);
        
        Console.WriteLine("네트워크 전환 시뮬레이션 중...");
        await Task.Delay(3000);
        
        // 네트워크 품질 측정
        var quality = await transitionManager.MeasureNetworkQuality();
        Console.WriteLine($"네트워크 품질 - 지연시간: {quality.Latency}ms, 안정성: {quality.Stability:F2}");
        
        transitionManager.Dispose();
        Console.WriteLine("네트워크 전환 테스트 완료");
    }
    
    static async Task RunBatteryOptimizationTest()
    {
        Console.WriteLine("\n=== 배터리 최적화 테스트 ===");
        
        var batteryOptimizer = new MobileBatteryOptimizer();
        
        batteryOptimizer.ModeChanged += (mode) =>
        {
            Console.WriteLine($"최적화 모드 변경: {mode}");
        };
        
        batteryOptimizer.PowerStateChanged += (state) =>
        {
            Console.WriteLine($"전원 상태 변경: {state}");
        };
        
        // 다양한 모드 테스트
        batteryOptimizer.SetOptimizationMode(BatteryOptimizationMode.Performance);
        await Task.Delay(2000);
        
        batteryOptimizer.SetOptimizationMode(BatteryOptimizationMode.Balanced);
        await Task.Delay(2000);
        
        batteryOptimizer.SetOptimizationMode(BatteryOptimizationMode.PowerSaver);
        await Task.Delay(2000);
        
        // 작업 스킵 테스트
        var shouldSkip = await batteryOptimizer.ShouldSkipOperation("quality_measurement");
        Console.WriteLine($"품질 측정 스킵 여부: {shouldSkip}");
        
        Console.WriteLine("배터리 최적화 테스트 완료");
    }
}
```

이 13장에서는 모바일 환경에서의 P2P 홀펀칭 구현의 핵심 사항들을 다뤘습니다:

**주요 특징:**
- **모바일 NAT 분석**: 캐리어급 NAT와 대칭 NAT 대응
- **배터리 최적화**: 적응형 타이머와 패킷 배치 처리
- **네트워크 전환**: WiFi-셀룰러 간 seamless 전환
- **플랫폼별 최적화**: iOS와 Android의 고유 제약사항 대응

모바일 P2P 게임의 성공을 위해서는 이러한 모바일 특화 최적화가 필수적이며, 특히 배터리 수명과 네트워크 안정성의 균형을 맞추는 것이 중요합니다.  

  

# 14장: 문제 해결과 디버깅

## 14.1 홀펀칭 실패 원인 분석

### 14.1.1 홀펀칭 실패 분류 체계

P2P 홀펀칭 실패는 여러 단계에서 발생할 수 있으며, 각 단계별로 다른 원인과 해결 방법이 필요하다.

```csharp
// HolePunchingDiagnostics.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

public enum HolePunchingFailureType
{
    STUNServerUnreachable,
    NATTypeDetectionFailure,
    SymmetricNATDetected,
    PortAllocationFailure,
    TimingIssue,
    FirewallBlocking,
    NetworkConnectivityLoss,
    PeerUnresponsive,
    InvalidEndpointInfo,
    TURNServerRequired,
    Unknown
}

public class HolePunchingFailureAnalyzer
{
    public class FailureAnalysisResult
    {
        public HolePunchingFailureType FailureType { get; set; }
        public string DetailedDescription { get; set; }
        public List<string> PossibleCauses { get; set; } = new();
        public List<string> RecommendedSolutions { get; set; } = new();
        public int SeverityLevel { get; set; } // 1-5 (5가 가장 심각)
        public Dictionary<string, object> DiagnosticData { get; set; } = new();
    }
    
    public async Task<FailureAnalysisResult> AnalyzeHolePunchingFailure(
        HolePunchingAttempt attempt,
        NetworkDiagnosticInfo networkInfo)
    {
        var result = new FailureAnalysisResult();
        
        // 1. STUN 서버 연결성 확인
        if (!attempt.STUNServerReachable)
        {
            return AnalyzeSTUNServerFailure(attempt, networkInfo);
        }
        
        // 2. NAT 타입 감지 문제 분석
        if (attempt.DetectedNATType == NATType.Unknown)
        {
            return AnalyzeNATDetectionFailure(attempt, networkInfo);
        }
        
        // 3. 대칭 NAT 문제 분석
        if (attempt.DetectedNATType == NATType.Symmetric)
        {
            return AnalyzeSymmetricNATIssue(attempt, networkInfo);
        }
        
        // 4. 포트 매핑 문제 분석
        if (attempt.PortMappingFailed)
        {
            return AnalyzePortMappingFailure(attempt, networkInfo);
        }
        
        // 5. 타이밍 관련 문제 분석
        if (attempt.TimingViolation)
        {
            return AnalyzeTimingIssue(attempt, networkInfo);
        }
        
        // 6. 피어 응답 문제 분석
        if (attempt.PeerUnresponsive)
        {
            return AnalyzePeerResponsiveness(attempt, networkInfo);
        }
        
        // 7. 일반적인 네트워크 문제 분석
        return await AnalyzeGeneralNetworkIssue(attempt, networkInfo);
    }
    
    private FailureAnalysisResult AnalyzeSTUNServerFailure(
        HolePunchingAttempt attempt, 
        NetworkDiagnosticInfo networkInfo)
    {
        return new FailureAnalysisResult
        {
            FailureType = HolePunchingFailureType.STUNServerUnreachable,
            DetailedDescription = "STUN 서버에 연결할 수 없습니다.",
            PossibleCauses = new List<string>
            {
                "인터넷 연결 문제",
                "방화벽에서 UDP 트래픽 차단",
                "STUN 서버 다운 또는 과부하",
                "DNS 해결 실패",
                "ISP에서 특정 포트 차단"
            },
            RecommendedSolutions = new List<string>
            {
                "대체 STUN 서버 사용",
                "네트워크 연결 상태 확인",
                "방화벽 설정 검토",
                "다른 포트 시도",
                "VPN 연결 테스트"
            },
            SeverityLevel = 4,
            DiagnosticData = new Dictionary<string, object>
            {
                ["STUNServers"] = attempt.TestedSTUNServers,
                ["LastError"] = attempt.LastSTUNError,
                ["NetworkInterface"] = networkInfo.ActiveInterface
            }
        };
    }
    
    private FailureAnalysisResult AnalyzeSymmetricNATIssue(
        HolePunchingAttempt attempt, 
        NetworkDiagnosticInfo networkInfo)
    {
        return new FailureAnalysisResult
        {
            FailureType = HolePunchingFailureType.SymmetricNATDetected,
            DetailedDescription = "대칭 NAT 환경에서 직접 P2P 연결이 불가능합니다.",
            PossibleCauses = new List<string>
            {
                "ISP에서 대칭 NAT 사용",
                "기업 네트워크의 보안 정책",
                "고급 방화벽 설정",
                "캐리어 그레이드 NAT(CGN) 환경"
            },
            RecommendedSolutions = new List<string>
            {
                "TURN 서버를 통한 릴레이 연결 사용",
                "UDP 홀펀칭 대신 TCP 홀펀칭 시도",
                "포트 예측 알고리즘 사용",
                "사용자에게 네트워크 설정 변경 안내"
            },
            SeverityLevel = 3,
            DiagnosticData = new Dictionary<string, object>
            {
                ["MappedPorts"] = attempt.MappedPorts,
                ["PortPrediction"] = attempt.PortPredictionAttempts,
                ["ISPInfo"] = networkInfo.ISPInformation
            }
        };
    }
    
    private FailureAnalysisResult AnalyzeTimingIssue(
        HolePunchingAttempt attempt, 
        NetworkDiagnosticInfo networkInfo)
    {
        return new FailureAnalysisResult
        {
            FailureType = HolePunchingFailureType.TimingIssue,
            DetailedDescription = "홀펀칭 타이밍이 부정확하여 연결에 실패했습니다.",
            PossibleCauses = new List<string>
            {
                "높은 네트워크 지연시간",
                "시계 동기화 문제",
                "NAT 매핑 수명 시간 초과",
                "패킷 순서 문제",
                "동시 홀펀칭 타이밍 불일치"
            },
            RecommendedSolutions = new List<string>
            {
                "NTP를 통한 시계 동기화",
                "홀펀칭 타이밍 알고리즘 조정",
                "재시도 간격 최적화",
                "킵얼라이브 패킷 빈도 증가",
                "적응적 타이밍 제어 구현"
            },
            SeverityLevel = 2,
            DiagnosticData = new Dictionary<string, object>
            {
                ["NetworkLatency"] = attempt.MeasuredLatency,
                ["TimingOffset"] = attempt.TimingOffset,
                ["RetryIntervals"] = attempt.RetryIntervals,
                ["MappingLifetime"] = attempt.NATMappingLifetime
            }
        };
    }
}

// 홀펀칭 시도 정보를 추적하는 클래스
public class HolePunchingAttempt
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public NATType DetectedNATType { get; set; }
    public bool STUNServerReachable { get; set; }
    public bool PortMappingFailed { get; set; }
    public bool TimingViolation { get; set; }
    public bool PeerUnresponsive { get; set; }
    public List<string> TestedSTUNServers { get; set; } = new();
    public string LastSTUNError { get; set; }
    public List<int> MappedPorts { get; set; } = new();
    public int PortPredictionAttempts { get; set; }
    public TimeSpan MeasuredLatency { get; set; }
    public TimeSpan TimingOffset { get; set; }
    public List<TimeSpan> RetryIntervals { get; set; } = new();
    public TimeSpan NATMappingLifetime { get; set; }
    public IPEndPoint LocalEndpoint { get; set; }
    public IPEndPoint MappedEndpoint { get; set; }
    public IPEndPoint PeerEndpoint { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

// 네트워크 진단 정보
public class NetworkDiagnosticInfo
{
    public string ActiveInterface { get; set; }
    public string ISPInformation { get; set; }
    public bool HasIPv6Support { get; set; }
    public bool HasUPnPSupport { get; set; }
    public List<IPAddress> DNSServers { get; set; } = new();
    public NetworkInterfaceType InterfaceType { get; set; }
    public long BandwidthBps { get; set; }
    public TimeSpan RTT { get; set; }
    public double PacketLoss { get; set; }
    public bool IsVPNActive { get; set; }
    public string FirewallStatus { get; set; }
}
```

### 14.1.2 실시간 실패 원인 감지

```csharp
// RealTimeFailureDetector.cs
public class RealTimeHolePunchingFailureDetector
{
    private readonly Dictionary<string, FailurePattern> _knownPatterns = new();
    private readonly ConcurrentQueue<HolePunchingEvent> _eventHistory = new();
    
    public event EventHandler<FailureDetectedEventArgs> FailureDetected;
    
    public class FailurePattern
    {
        public string Name { get; set; }
        public Func<List<HolePunchingEvent>, bool> DetectionLogic { get; set; }
        public string Description { get; set; }
        public int Confidence { get; set; } // 0-100
    }
    
    public class HolePunchingEvent
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string ClientId { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    public class FailureDetectedEventArgs : EventArgs
    {
        public FailurePattern Pattern { get; set; }
        public List<HolePunchingEvent> TriggeringEvents { get; set; }
        public int Confidence { get; set; }
        public DateTime DetectionTime { get; set; }
    }
    
    public RealTimeHolePunchingFailureDetector()
    {
        InitializeFailurePatterns();
    }
    
    private void InitializeFailurePatterns()
    {
        // 1. STUN 서버 연결 실패 패턴
        _knownPatterns["STUN_SERVER_DOWN"] = new FailurePattern
        {
            Name = "STUN Server Connectivity Issue",
            Description = "다수의 클라이언트에서 동시에 STUN 서버 연결 실패",
            DetectionLogic = events =>
            {
                var recentEvents = events.Where(e => 
                    DateTime.UtcNow - e.Timestamp < TimeSpan.FromMinutes(5) &&
                    e.EventType == "STUN_TIMEOUT").ToList();
                
                var uniqueClients = recentEvents.Select(e => e.ClientId).Distinct().Count();
                return uniqueClients >= 5; // 5명 이상 동시 실패
            },
            Confidence = 85
        };
        
        // 2. 특정 ISP 홀펀칭 실패 패턴
        _knownPatterns["ISP_BLOCKING"] = new FailurePattern
        {
            Name = "ISP-specific Blocking",
            Description = "특정 ISP 사용자들의 홀펀칭 실패율 급증",
            DetectionLogic = events =>
            {
                var recentFailures = events.Where(e => 
                    DateTime.UtcNow - e.Timestamp < TimeSpan.FromMinutes(10) &&
                    e.EventType == "HOLE_PUNCHING_FAILED").ToList();
                
                if (recentFailures.Count < 10) return false;
                
                var ispGroups = recentFailures
                    .GroupBy(e => GetISPFromEvent(e))
                    .Where(g => g.Count() >= 5)
                    .ToList();
                
                return ispGroups.Any();
            },
            Confidence = 75
        };
        
        // 3. 시간대별 성능 저하 패턴
        _knownPatterns["TIME_BASED_DEGRADATION"] = new FailurePattern
        {
            Name = "Time-based Performance Degradation",
            Description = "특정 시간대에 홀펀칭 성능 저하",
            DetectionLogic = events =>
            {
                var currentHour = DateTime.UtcNow.Hour;
                var currentHourEvents = events.Where(e => 
                    e.Timestamp.Hour == currentHour &&
                    e.EventType == "HOLE_PUNCHING_SLOW").ToList();
                
                return currentHourEvents.Count >= 20;
            },
            Confidence = 60
        };
        
        // 4. 네트워크 지연 스파이크 패턴
        _knownPatterns["LATENCY_SPIKE"] = new FailurePattern
        {
            Name = "Network Latency Spike",
            Description = "네트워크 지연시간 급증으로 인한 홀펀칭 실패",
            DetectionLogic = events =>
            {
                var recentLatencyEvents = events.Where(e => 
                    DateTime.UtcNow - e.Timestamp < TimeSpan.FromMinutes(3) &&
                    e.EventType == "HIGH_LATENCY" &&
                    e.Properties.ContainsKey("Latency")).ToList();
                
                if (recentLatencyEvents.Count < 5) return false;
                
                var averageLatency = recentLatencyEvents
                    .Select(e => (double)e.Properties["Latency"])
                    .Average();
                
                return averageLatency > 500; // 500ms 이상
            },
            Confidence = 80
        };
    }
    
    public void RecordEvent(HolePunchingEvent holePunchingEvent)
    {
        _eventHistory.Enqueue(holePunchingEvent);
        
        // 오래된 이벤트 제거 (최근 1시간만 보관)
        while (_eventHistory.TryPeek(out var oldEvent) && 
               DateTime.UtcNow - oldEvent.Timestamp > TimeSpan.FromHours(1))
        {
            _eventHistory.TryDequeue(out _);
        }
        
        // 실시간 패턴 감지
        DetectPatterns();
    }
    
    private void DetectPatterns()
    {
        var currentEvents = _eventHistory.ToList();
        
        foreach (var pattern in _knownPatterns.Values)
        {
            try
            {
                if (pattern.DetectionLogic(currentEvents))
                {
                    var triggeringEvents = GetTriggeringEvents(currentEvents, pattern);
                    
                    FailureDetected?.Invoke(this, new FailureDetectedEventArgs
                    {
                        Pattern = pattern,
                        TriggeringEvents = triggeringEvents,
                        Confidence = pattern.Confidence,
                        DetectionTime = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"패턴 감지 오류 ({pattern.Name}): {ex.Message}");
            }
        }
    }
    
    private List<HolePunchingEvent> GetTriggeringEvents(
        List<HolePunchingEvent> events, 
        FailurePattern pattern)
    {
        // 패턴에 따라 관련 이벤트들을 추출
        var relevantTimespan = TimeSpan.FromMinutes(10);
        return events
            .Where(e => DateTime.UtcNow - e.Timestamp < relevantTimespan)
            .OrderByDescending(e => e.Timestamp)
            .Take(50)
            .ToList();
    }
    
    private string GetISPFromEvent(HolePunchingEvent holePunchingEvent)
    {
        // IP 주소를 기반으로 ISP 정보 추출
        if (holePunchingEvent.Properties.ContainsKey("ISP"))
        {
            return holePunchingEvent.Properties["ISP"].ToString();
        }
        
        // 간단한 IP 범위 기반 ISP 감지 (실제로는 더 정교한 로직 필요)
        var ip = holePunchingEvent.EndPoint?.Address?.ToString();
        return ExtractISPFromIP(ip) ?? "Unknown";
    }
    
    private string ExtractISPFromIP(string ip)
    {
        // 실제 구현에서는 GeoIP 데이터베이스나 외부 API 사용
        if (string.IsNullOrEmpty(ip)) return null;
        
        // 예시: 한국 주요 ISP IP 범위 (매우 간단한 예시)
        if (ip.StartsWith("211.") || ip.StartsWith("114."))
            return "KT";
        else if (ip.StartsWith("222.") || ip.StartsWith("121."))
            return "SKT";
        else if (ip.StartsWith("175.") || ip.StartsWith("112."))
            return "LGU+";
        
        return "Unknown";
    }
}
```

## 14.2 네트워크 진단 도구 활용

### 14.2.1 패킷 분석 도구 구현

```csharp
// PacketAnalyzer.cs
using System.Net.NetworkInformation;
using System.Threading;

public class P2PPacketAnalyzer
{
    private readonly RawSocket _rawSocket;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, PacketFlow> _flows = new();
    
    public event EventHandler<PacketCapturedEventArgs> PacketCaptured;
    public event EventHandler<FlowAnalysisEventArgs> FlowAnalysisComplete;
    
    public class PacketInfo
    {
        public DateTime Timestamp { get; set; }
        public IPAddress SourceIP { get; set; }
        public IPAddress DestinationIP { get; set; }
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public ProtocolType Protocol { get; set; }
        public int PacketSize { get; set; }
        public byte[] RawData { get; set; }
        public PacketDirection Direction { get; set; }
        public bool IsSTUNPacket { get; set; }
        public STUNMessageType? STUNType { get; set; }
    }
    
    public enum PacketDirection
    {
        Incoming,
        Outgoing,
        Unknown
    }
    
    public enum STUNMessageType
    {
        BindingRequest = 0x0001,
        BindingResponse = 0x0101,
        BindingErrorResponse = 0x0111
    }
    
    public class PacketFlow
    {
        public string FlowId { get; set; }
        public IPEndPoint LocalEndpoint { get; set; }
        public IPEndPoint RemoteEndpoint { get; set; }
        public List<PacketInfo> Packets { get; set; } = new();
        public DateTime FirstPacketTime { get; set; }
        public DateTime LastPacketTime { get; set; }
        public long TotalBytesOut { get; set; }
        public long TotalBytesIn { get; set; }
        public double AverageLatency { get; set; }
        public bool IsHolePunchingFlow { get; set; }
    }
    
    public void StartCapture(IPAddress localIP, int port)
    {
        Task.Run(async () =>
        {
            try
            {
                using var udpClient = new UdpClient(port);
                
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync();
                        var packet = AnalyzePacket(result.Buffer, result.RemoteEndPoint, localIP);
                        
                        if (packet != null)
                        {
                            ProcessPacket(packet);
                            PacketCaptured?.Invoke(this, new PacketCapturedEventArgs { Packet = packet });
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        // 타임아웃은 정상적인 상황
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"패킷 캡처 오류: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"패킷 분석기 오류: {ex.Message}");
            }
        }, _cancellationTokenSource.Token);
    }
    
    private PacketInfo AnalyzePacket(byte[] data, IPEndPoint remoteEndpoint, IPAddress localIP)
    {
        if (data == null || data.Length < 4) return null;
        
        var packet = new PacketInfo
        {
            Timestamp = DateTime.UtcNow,
            SourceIP = remoteEndpoint.Address,
            DestinationIP = localIP,
            SourcePort = remoteEndpoint.Port,
            Protocol = ProtocolType.Udp,
            PacketSize = data.Length,
            RawData = data,
            Direction = PacketDirection.Incoming
        };
        
        // STUN 패킷 감지
        if (IsSTUNPacket(data))
        {
            packet.IsSTUNPacket = true;
            packet.STUNType = GetSTUNMessageType(data);
        }
        
        return packet;
    }
    
    private bool IsSTUNPacket(byte[] data)
    {
        if (data.Length < 20) return false;
        
        // STUN 매직 쿠키 확인 (RFC 5389)
        return data[4] == 0x21 && data[5] == 0x12 && data[6] == 0xa4 && data[7] == 0x42;
    }
    
    private STUNMessageType GetSTUNMessageType(byte[] data)
    {
        if (data.Length < 2) return STUNMessageType.BindingRequest;
        
        var messageType = (ushort)((data[0] << 8) | data[1]);
        return (STUNMessageType)messageType;
    }
    
    private void ProcessPacket(PacketInfo packet)
    {
        var flowId = GenerateFlowId(packet);
        
        if (!_flows.TryGetValue(flowId, out var flow))
        {
            flow = new PacketFlow
            {
                FlowId = flowId,
                LocalEndpoint = new IPEndPoint(packet.DestinationIP, packet.DestinationPort),
                RemoteEndpoint = new IPEndPoint(packet.SourceIP, packet.SourcePort),
                FirstPacketTime = packet.Timestamp,
                IsHolePunchingFlow = packet.IsSTUNPacket
            };
            _flows[flowId] = flow;
        }
        
        flow.Packets.Add(packet);
        flow.LastPacketTime = packet.Timestamp;
        
        if (packet.Direction == PacketDirection.Incoming)
            flow.TotalBytesIn += packet.PacketSize;
        else
            flow.TotalBytesOut += packet.PacketSize;
        
        // 주기적으로 플로우 분석
        if (flow.Packets.Count % 10 == 0)
        {
            AnalyzeFlow(flow);
        }
    }
    
    private void AnalyzeFlow(PacketFlow flow)
    {
        // 지연시간 계산
        CalculateLatency(flow);
        
        // 홀펀칭 패턴 감지
        DetectHolePunchingPattern(flow);
        
        // 분석 결과 이벤트 발생
        FlowAnalysisComplete?.Invoke(this, new FlowAnalysisEventArgs { Flow = flow });
    }
    
    private void CalculateLatency(PacketFlow flow)
    {
        var requestResponsePairs = new List<(PacketInfo Request, PacketInfo Response)>();
        
        foreach (var packet in flow.Packets.Where(p => p.IsSTUNPacket))
        {
            if (packet.STUNType == STUNMessageType.BindingRequest)
            {
                // 해당 요청에 대한 응답 찾기
                var response = flow.Packets
                    .FirstOrDefault(p => p.STUNType == STUNMessageType.BindingResponse &&
                                        p.Timestamp > packet.Timestamp &&
                                        p.Timestamp - packet.Timestamp < TimeSpan.FromSeconds(5));
                
                if (response != null)
                {
                    requestResponsePairs.Add((packet, response));
                }
            }
        }
        
        if (requestResponsePairs.Any())
        {
            var latencies = requestResponsePairs
                .Select(pair => (pair.Response.Timestamp - pair.Request.Timestamp).TotalMilliseconds)
                .ToList();
            
            flow.AverageLatency = latencies.Average();
        }
    }
    
    private void DetectHolePunchingPattern(PacketFlow flow)
    {
        var stunPackets = flow.Packets.Where(p => p.IsSTUNPacket).ToList();
        
        if (stunPackets.Count >= 3)
        {
            // 연속적인 STUN 요청/응답 패턴 감지
            var hasBindingRequest = stunPackets.Any(p => p.STUNType == STUNMessageType.BindingRequest);
            var hasBindingResponse = stunPackets.Any(p => p.STUNType == STUNMessageType.BindingResponse);
            
            flow.IsHolePunchingFlow = hasBindingRequest && hasBindingResponse;
        }
    }
    
    private string GenerateFlowId(PacketInfo packet)
    {
        // 소스와 목적지를 정렬하여 양방향 플로우를 하나로 처리
        var endpoints = new[]
        {
            $"{packet.SourceIP}:{packet.SourcePort}",
            $"{packet.DestinationIP}:{packet.DestinationPort}"
        }.OrderBy(x => x);
        
        return string.Join("<->", endpoints);
    }
    
    public void StopCapture()
    {
        _cancellationTokenSource.Cancel();
    }
    
    public FlowStatistics GetFlowStatistics()
    {
        var stats = new FlowStatistics();
        var flows = _flows.Values.ToList();
        
        stats.TotalFlows = flows.Count;
        stats.HolePunchingFlows = flows.Count(f => f.IsHolePunchingFlow);
        stats.AverageLatency = flows.Where(f => f.AverageLatency > 0)
                                   .DefaultIfEmpty()
                                   .Average(f => f.AverageLatency);
        stats.TotalPackets = flows.Sum(f => f.Packets.Count);
        stats.TotalBytes = flows.Sum(f => f.TotalBytesIn + f.TotalBytesOut);
        
        return stats;
    }
}

public class FlowStatistics
{
    public int TotalFlows { get; set; }
    public int HolePunchingFlows { get; set; }
    public double AverageLatency { get; set; }
    public long TotalPackets { get; set; }
    public long TotalBytes { get; set; }
}

public class PacketCapturedEventArgs : EventArgs
{
    public P2PPacketAnalyzer.PacketInfo Packet { get; set; }
}

public class FlowAnalysisEventArgs : EventArgs
{
    public P2PPacketAnalyzer.PacketFlow Flow { get; set; }
}
```

### 14.2.2 네트워크 상태 진단 도구

```csharp
// NetworkDiagnosticTool.cs
public class ComprehensiveNetworkDiagnostic
{
    public class DiagnosticReport
    {
        public DateTime GeneratedAt { get; set; }
        public NetworkConnectivityResult Connectivity { get; set; }
        public NATDetectionResult NATInfo { get; set; }
        public PerformanceMetrics Performance { get; set; }
        public List<DiagnosticIssue> Issues { get; set; } = new();
        public List<DiagnosticRecommendation> Recommendations { get; set; } = new();
    }
    
    public class NetworkConnectivityResult
    {
        public bool InternetConnectivity { get; set; }
        public bool IPv4Connectivity { get; set; }
        public bool IPv6Connectivity { get; set; }
        public bool DNSResolution { get; set; }
        public List<string> ReachableSTUNServers { get; set; } = new();
        public List<string> UnreachableSTUNServers { get; set; } = new();
        public TimeSpan AverageResponseTime { get; set; }
    }
    
    public class NATDetectionResult
    {
        public NATType DetectedType { get; set; }
        public IPEndPoint LocalEndpoint { get; set; }
        public IPEndPoint MappedEndpoint { get; set; }
        public bool ConsistentMapping { get; set; }
        public TimeSpan MappingLifetime { get; set; }
        public bool SupportsHairpinning { get; set; }
        public Dictionary<string, IPEndPoint> STUNServerMappings { get; set; } = new();
    }
    
    public class PerformanceMetrics
    {
        public double Latency { get; set; }
        public double Jitter { get; set; }
        public double PacketLoss { get; set; }
        public long BandwidthUpload { get; set; }
        public long BandwidthDownload { get; set; }
        public int MTUSize { get; set; }
    }
    
    public class DiagnosticIssue
    {
        public string Category { get; set; }
        public string Description { get; set; }
        public SeverityLevel Severity { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }
    
    public class DiagnosticRecommendation
    {
        public string Issue { get; set; }
        public string Recommendation { get; set; }
        public int Priority { get; set; }
        public string ExpectedImprovement { get; set; }
    }
    
    public enum SeverityLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    public async Task<DiagnosticReport> RunComprehensiveDiagnostic()
    {
        var report = new DiagnosticReport
        {
            GeneratedAt = DateTime.UtcNow
        };
        
        Console.WriteLine("네트워크 진단을 시작합니다...");
        
        // 1. 기본 연결성 테스트
        Console.WriteLine("기본 연결성 테스트 중...");
        report.Connectivity = await TestNetworkConnectivity();
        
        // 2. NAT 감지 및 분석
        Console.WriteLine("NAT 환경 분석 중...");
        report.NATInfo = await AnalyzeNATEnvironment();
        
        // 3. 성능 측정
        Console.WriteLine("네트워크 성능 측정 중...");
        report.Performance = await MeasureNetworkPerformance();
        
        // 4. 문제점 분석
        AnalyzeIssues(report);
        
        // 5. 권장사항 생성
        GenerateRecommendations(report);
        
        Console.WriteLine("진단이 완료되었습니다.");
        return report;
    }
    
    private async Task<NetworkConnectivityResult> TestNetworkConnectivity()
    {
        var result = new NetworkConnectivityResult();
        var responseTimes = new List<double>();
        
        // 인터넷 연결 테스트
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 5000);
            result.InternetConnectivity = reply.Status == IPStatus.Success;
            result.IPv4Connectivity = reply.Status == IPStatus.Success;
            
            if (reply.Status == IPStatus.Success)
            {
                responseTimes.Add(reply.RoundtripTime);
            }
        }
        catch
        {
            result.InternetConnectivity = false;
            result.IPv4Connectivity = false;
        }
        
        // IPv6 연결 테스트
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("2001:4860:4860::8888", 5000);
            result.IPv6Connectivity = reply.Status == IPStatus.Success;
            
            if (reply.Status == IPStatus.Success)
            {
                responseTimes.Add(reply.RoundtripTime);
            }
        }
        catch
        {
            result.IPv6Connectivity = false;
        }
        
        // DNS 해결 테스트
        try
        {
            var addresses = await Dns.GetHostAddressesAsync("google.com");
            result.DNSResolution = addresses.Length > 0;
        }
        catch
        {
            result.DNSResolution = false;
        }
        
        // STUN 서버 연결 테스트
        var stunServers = new[]
        {
            "stun.l.google.com:19302",
            "stun1.l.google.com:19302",
            "stun.stunprotocol.org:3478",
            "stun.voiparound.com:3478"
        };
        
        foreach (var server in stunServers)
        {
            var reachable = await TestSTUNServerConnectivity(server);
            if (reachable)
                result.ReachableSTUNServers.Add(server);
            else
                result.UnreachableSTUNServers.Add(server);
        }
        
        if (responseTimes.Any())
        {
            result.AverageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average());
        }
        
        return result;
    }
    
    private async Task<bool> TestSTUNServerConnectivity(string serverEndpoint)
    {
        try
        {
            var parts = serverEndpoint.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);
            
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = 5000;
            
            var addresses = await Dns.GetHostAddressesAsync(host);
            var endpoint = new IPEndPoint(addresses[0], port);
            
            // 간단한 STUN Binding Request 전송
            var request = CreateSimpleSTUNRequest();
            await udpClient.SendAsync(request, request.Length, endpoint);
            
            // 응답 대기
            var response = await udpClient.ReceiveAsync();
            return response.Buffer.Length >= 20; // 최소 STUN 헤더 크기
        }
        catch
        {
            return false;
        }
    }
    
    private byte[] CreateSimpleSTUNRequest()
    {
        var request = new byte[20];
        
        // Message Type: Binding Request (0x0001)
        request[0] = 0x00;
        request[1] = 0x01;
        
        // Message Length: 0
        request[2] = 0x00;
        request[3] = 0x00;
        
        // Magic Cookie
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xa4;
        request[7] = 0x42;
        
        // Transaction ID (12 bytes)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            request[i] = (byte)random.Next(256);
        }
        
        return request;
    }
    
    private async Task<NATDetectionResult> AnalyzeNATEnvironment()
    {
        var result = new NATDetectionResult();
        
        try
        {
            using var udpClient = new UdpClient(0);
            result.LocalEndpoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            
            // 여러 STUN 서버를 통한 NAT 타입 감지
            var stunServers = new[]
            {
                "stun.l.google.com:19302",
                "stun1.l.google.com:19302",
                "stun.stunprotocol.org:3478"
            };
            
            var mappings = new List<IPEndPoint>();
            
            foreach (var server in stunServers)
            {
                try
                {
                    var mapping = await GetMappedEndpoint(udpClient, server);
                    if (mapping != null)
                    {
                        mappings.Add(mapping);
                        result.STUNServerMappings[server] = mapping;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"STUN 서버 {server} 테스트 실패: {ex.Message}");
                }
            }
            
            if (mappings.Any())
            {
                result.MappedEndpoint = mappings.First();
                result.ConsistentMapping = mappings.All(m => m.Equals(mappings.First()));
                result.DetectedType = DetermineNATType(mappings);
            }
            else
            {
                result.DetectedType = NATType.Unknown;
            }
            
            // NAT 매핑 수명 테스트
            result.MappingLifetime = await TestMappingLifetime(udpClient);
            
            // Hairpinning 테스트
            result.SupportsHairpinning = await TestHairpinning(udpClient, result.MappedEndpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NAT 분석 오류: {ex.Message}");
            result.DetectedType = NATType.Unknown;
        }
        
        return result;
    }
    
    private async Task<IPEndPoint> GetMappedEndpoint(UdpClient udpClient, string stunServer)
    {
        var parts = stunServer.Split(':');
        var host = parts[0];
        var port = int.Parse(parts[1]);
        
        var addresses = await Dns.GetHostAddressesAsync(host);
        var endpoint = new IPEndPoint(addresses[0], port);
        
        var request = CreateSTUNBindingRequest();
        await udpClient.SendAsync(request, request.Length, endpoint);
        
        udpClient.Client.ReceiveTimeout = 5000;
        var response = await udpClient.ReceiveAsync();
        
        return ParseMappedAddressFromSTUNResponse(response.Buffer);
    }
    
    private byte[] CreateSTUNBindingRequest()
    {
        // STUN Binding Request 생성 (완전한 구현)
        var message = new byte[20];
        
        // Message Type: Binding Request
        message[0] = 0x00;
        message[1] = 0x01;
        
        // Message Length: 0 (attributes 없음)
        message[2] = 0x00;
        message[3] = 0x00;
        
        // Magic Cookie
        message[4] = 0x21;
        message[5] = 0x12;
        message[6] = 0xa4;
        message[7] = 0x42;
        
        // Transaction ID (12 bytes, 랜덤)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            message[i] = (byte)random.Next(256);
        }
        
        return message;
    }
    
    private IPEndPoint ParseMappedAddressFromSTUNResponse(byte[] response)
    {
        // STUN 응답에서 XOR-MAPPED-ADDRESS 속성 파싱
        if (response.Length < 20) return null;
        
        // 간단한 파싱 (실제로는 더 정교한 파싱 필요)
        for (int i = 20; i < response.Length - 8; i++)
        {
            // XOR-MAPPED-ADDRESS 속성 타입 (0x0020)
            if (response[i] == 0x00 && response[i + 1] == 0x20)
            {
                var family = (response[i + 5] << 8) | response[i + 6];
                if (family == 0x01) // IPv4
                {
                    var port = ((response[i + 6] << 8) | response[i + 7]) ^ 0x2112;
                    var ip = new byte[4];
                    var magicCookie = new byte[] { 0x21, 0x12, 0xa4, 0x42 };
                    
                    for (int j = 0; j < 4; j++)
                    {
                        ip[j] = (byte)(response[i + 8 + j] ^ magicCookie[j]);
                    }
                    
                    return new IPEndPoint(new IPAddress(ip), port);
                }
            }
        }
        
        return null;
    }
    
    private NATType DetermineNATType(List<IPEndPoint> mappings)
    {
        if (!mappings.Any()) return NATType.Unknown;
        
        // 모든 매핑이 동일한 경우
        if (mappings.All(m => m.Equals(mappings.First())))
        {
            return NATType.FullCone; // 또는 Restricted Cone (추가 테스트 필요)
        }
        
        // 매핑이 다른 경우 Symmetric NAT
        return NATType.Symmetric;
    }
    
    private async Task<TimeSpan> TestMappingLifetime(UdpClient udpClient)
    {
        // NAT 매핑이 얼마나 오래 유지되는지 테스트
        var startTime = DateTime.UtcNow;
        
        try
        {
            // 주기적으로 킵얼라이브 패킷 전송하면서 매핑 유지 시간 측정
            var testDuration = TimeSpan.FromMinutes(2); // 테스트 시간 제한
            var interval = TimeSpan.FromSeconds(30);
            
            while (DateTime.UtcNow - startTime < testDuration)
            {
                await Task.Delay(interval);
                
                // 매핑이 여전히 유효한지 확인
                var isValid = await TestMappingValidity(udpClient);
                if (!isValid)
                {
                    return DateTime.UtcNow - startTime;
                }
            }
        }
        catch
        {
            // 오류 발생 시 현재까지의 시간 반환
        }
        
        return DateTime.UtcNow - startTime;
    }
    
    private async Task<bool> TestMappingValidity(UdpClient udpClient)
    {
        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
            var testData = Encoding.UTF8.GetBytes("test");
            
            await udpClient.SendAsync(testData, testData.Length, endpoint);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> TestHairpinning(UdpClient udpClient, IPEndPoint mappedEndpoint)
    {
        if (mappedEndpoint == null) return false;
        
        try
        {
            // 자신의 매핑된 주소로 패킷 전송 시도
            var testData = Encoding.UTF8.GetBytes("hairpin_test");
            await udpClient.SendAsync(testData, testData.Length, mappedEndpoint);
            
            udpClient.Client.ReceiveTimeout = 2000;
            var response = await udpClient.ReceiveAsync();
            
            return response.Buffer.SequenceEqual(testData);
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<PerformanceMetrics> MeasureNetworkPerformance()
    {
        var metrics = new PerformanceMetrics();
        
        // 지연시간 측정
        var latencies = new List<double>();
        using var ping = new Ping();
        
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var reply = await ping.SendPingAsync("8.8.8.8", 5000);
                if (reply.Status == IPStatus.Success)
                {
                    latencies.Add(reply.RoundtripTime);
                }
            }
            catch { }
            
            await Task.Delay(100);
        }
        
        if (latencies.Any())
        {
            metrics.Latency = latencies.Average();
            
            // 지터 계산 (지연시간의 표준편차)
            var avgLatency = latencies.Average();
            var variance = latencies.Sum(l => Math.Pow(l - avgLatency, 2)) / latencies.Count;
            metrics.Jitter = Math.Sqrt(variance);
        }
        
        // 패킷 손실률 계산
        metrics.PacketLoss = 1.0 - ((double)latencies.Count / 10);
        
        // MTU 크기 감지
        metrics.MTUSize = await DetectMTUSize();
        
        return metrics;
    }
    
    private async Task<int> DetectMTUSize()
    {
        // Binary search를 통한 MTU 크기 감지
        int low = 576; // IPv4 최소 MTU
        int high = 1500; // 이더넷 표준 MTU
        int mtu = low;
        
        using var ping = new Ping();
        var options = new PingOptions { DontFragment = true };
        
        while (low <= high)
        {
            int mid = (low + high) / 2;
            var buffer = new byte[mid - 28]; // IP + ICMP 헤더 제외
            
            try
            {
                var reply = await ping.SendPingAsync("8.8.8.8", 5000, buffer, options);
                
                if (reply.Status == IPStatus.Success)
                {
                    mtu = mid;
                    low = mid + 1;
                }
                else if (reply.Status == IPStatus.PacketTooBig)
                {
                    high = mid - 1;
                }
                else
                {
                    break;
                }
            }
            catch
            {
                high = mid - 1;
            }
        }
        
        return mtu;
    }
    
    private void AnalyzeIssues(DiagnosticReport report)
    {
        // 연결성 문제 분석
        if (!report.Connectivity.InternetConnectivity)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "연결성",
                Description = "인터넷 연결이 없습니다",
                Severity = SeverityLevel.Critical
            });
        }
        
        if (!report.Connectivity.DNSResolution)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "DNS",
                Description = "DNS 해결에 문제가 있습니다",
                Severity = SeverityLevel.Error
            });
        }
        
        if (report.Connectivity.ReachableSTUNServers.Count == 0)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "STUN",
                Description = "STUN 서버에 연결할 수 없습니다",
                Severity = SeverityLevel.Critical
            });
        }
        
        // NAT 관련 문제 분석
        if (report.NATInfo.DetectedType == NATType.Symmetric)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "NAT",
                Description = "대칭 NAT가 감지되었습니다. P2P 연결이 어려울 수 있습니다",
                Severity = SeverityLevel.Warning
            });
        }
        
        if (!report.NATInfo.ConsistentMapping)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "NAT",
                Description = "NAT 매핑이 일관되지 않습니다",
                Severity = SeverityLevel.Warning
            });
        }
        
        // 성능 문제 분석
        if (report.Performance.Latency > 200)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "성능",
                Description = $"높은 지연시간이 감지되었습니다 ({report.Performance.Latency:F0}ms)",
                Severity = SeverityLevel.Warning
            });
        }
        
        if (report.Performance.PacketLoss > 0.05)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "성능",
                Description = $"높은 패킷 손실률이 감지되었습니다 ({report.Performance.PacketLoss * 100:F1}%)",
                Severity = SeverityLevel.Error
            });
        }
        
        if (report.Performance.Jitter > 50)
        {
            report.Issues.Add(new DiagnosticIssue
            {
                Category = "성능",
                Description = $"높은 지터가 감지되었습니다 ({report.Performance.Jitter:F0}ms)",
                Severity = SeverityLevel.Warning
            });
        }
    }
    
    private void GenerateRecommendations(DiagnosticReport report)
    {
        foreach (var issue in report.Issues)
        {
            var recommendations = GetRecommendationsForIssue(issue);
            report.Recommendations.AddRange(recommendations);
        }
    }
    
    private List<DiagnosticRecommendation> GetRecommendationsForIssue(DiagnosticIssue issue)
    {
        var recommendations = new List<DiagnosticRecommendation>();
        
        switch (issue.Category.ToLower())
        {
            case "연결성":
                recommendations.Add(new DiagnosticRecommendation
                {
                    Issue = issue.Description,
                    Recommendation = "네트워크 케이블 및 Wi-Fi 연결을 확인하고, 라우터를 재시작해보세요",
                    Priority = 1,
                    ExpectedImprovement = "기본적인 네트워크 연결 복구"
                });
                break;
                
            case "nat":
                if (issue.Description.Contains("대칭"))
                {
                    recommendations.Add(new DiagnosticRecommendation
                    {
                        Issue = issue.Description,
                        Recommendation = "TURN 서버를 통한 릴레이 연결을 사용하거나, 라우터 설정에서 NAT 타입을 변경해보세요",
                        Priority = 2,
                        ExpectedImprovement = "P2P 연결 성공률 향상"
                    });
                }
                break;
                
            case "성능":
                if (issue.Description.Contains("지연시간"))
                {
                    recommendations.Add(new DiagnosticRecommendation
                    {
                        Issue = issue.Description,
                        Recommendation = "유선 연결 사용, QoS 설정 조정, 또는 더 가까운 서버 선택을 고려해보세요",
                        Priority = 2,
                        ExpectedImprovement = "20-50% 지연시간 감소"
                    });
                }
                break;
        }
        
        return recommendations;
    }
    
    public void PrintDiagnosticReport(DiagnosticReport report)
    {
        Console.WriteLine("\n=== 네트워크 진단 보고서 ===");
        Console.WriteLine($"생성 시간: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        
        // 연결성 정보
        Console.WriteLine("### 연결성 정보");
        Console.WriteLine($"인터넷 연결: {(report.Connectivity.InternetConnectivity ? "✓" : "✗")}");
        Console.WriteLine($"IPv4 지원: {(report.Connectivity.IPv4Connectivity ? "✓" : "✗")}");
        Console.WriteLine($"IPv6 지원: {(report.Connectivity.IPv6Connectivity ? "✓" : "✗")}");
        Console.WriteLine($"DNS 해결: {(report.Connectivity.DNSResolution ? "✓" : "✗")}");
        Console.WriteLine($"연결 가능한 STUN 서버: {report.Connectivity.ReachableSTUNServers.Count}");
        Console.WriteLine($"평균 응답 시간: {report.Connectivity.AverageResponseTime.TotalMilliseconds:F0}ms");
        Console.WriteLine();
        
        // NAT 정보
        Console.WriteLine("### NAT 정보");
        Console.WriteLine($"감지된 NAT 타입: {report.NATInfo.DetectedType}");
        Console.WriteLine($"로컬 엔드포인트: {report.NATInfo.LocalEndpoint}");
        Console.WriteLine($"매핑된 엔드포인트: {report.NATInfo.MappedEndpoint}");
        Console.WriteLine($"일관된 매핑: {(report.NATInfo.ConsistentMapping ? "✓" : "✗")}");
        Console.WriteLine($"Hairpinning 지원: {(report.NATInfo.SupportsHairpinning ? "✓" : "✗")}");
        Console.WriteLine($"매핑 수명: {report.NATInfo.MappingLifetime}");
        Console.WriteLine();
        
        // 성능 정보
        Console.WriteLine("### 성능 정보");
        Console.WriteLine($"지연시간: {report.Performance.Latency:F0}ms");
        Console.WriteLine($"지터: {report.Performance.Jitter:F0}ms");
        Console.WriteLine($"패킷 손실률: {report.Performance.PacketLoss * 100:F1}%");
        Console.WriteLine($"MTU 크기: {report.Performance.MTUSize} bytes");
        Console.WriteLine();
        
        // 문제점
        if (report.Issues.Any())
        {
            Console.WriteLine("### 발견된 문제점");
            foreach (var issue in report.Issues)
            {
                var severityIcon = issue.Severity switch
                {
                    SeverityLevel.Critical => "🔴",
                    SeverityLevel.Error => "🟠",
                    SeverityLevel.Warning => "🟡",
                    _ => "ℹ️"
                };
                Console.WriteLine($"{severityIcon} [{issue.Category}] {issue.Description}");
            }
            Console.WriteLine();
        }
        
        // 권장사항
        if (report.Recommendations.Any())
        {
            Console.WriteLine("### 권장사항");
            foreach (var rec in report.Recommendations.OrderBy(r => r.Priority))
            {
                Console.WriteLine($"[우선순위 {rec.Priority}] {rec.Issue}");
                Console.WriteLine($"  권장사항: {rec.Recommendation}");
                Console.WriteLine($"  예상 효과: {rec.ExpectedImprovement}");
                Console.WriteLine();
            }
        }
    }
}
```

## 14.3 로깅과 모니터링 시스템

### 14.3.1 구조화된 로깅 시스템

```csharp
// P2PLoggingSystem.cs
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class P2PStructuredLogger
{
    private readonly ILogger _logger;
    private readonly string _sessionId;
    
    public P2PStructuredLogger(ILoggerFactory loggerFactory, string sessionId = null)
    {
        _logger = loggerFactory.CreateLogger("P2P");
        _sessionId = sessionId ?? Guid.NewGuid().ToString("N")[..8];
    }
    
    public class P2PLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; }
        public string EventType { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public string ClientId { get; set; }
        public string PeerId { get; set; }
        public IPEndPoint LocalEndpoint { get; set; }
        public IPEndPoint RemoteEndpoint { get; set; }
        public NATType? NATType { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
    
    public void LogHolePunchingAttempt(string clientId, string peerId, 
        IPEndPoint localEndpoint, IPEndPoint remoteEndpoint)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "HolePunchingAttempt",
            Level = LogLevel.Information,
            Message = "홀펀칭 시도 시작",
            ClientId = clientId,
            PeerId = peerId,
            LocalEndpoint = localEndpoint,
            RemoteEndpoint = remoteEndpoint
        };
        
        LogEntry(entry);
    }
    
    public void LogHolePunchingSuccess(string clientId, string peerId, 
        TimeSpan duration, NATType natType)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "HolePunchingSuccess",
            Level = LogLevel.Information,
            Message = "홀펀칭 성공",
            ClientId = clientId,
            PeerId = peerId,
            Duration = duration,
            Success = true,
            NATType = natType
        };
        
        LogEntry(entry);
    }
    
    public void LogHolePunchingFailure(string clientId, string peerId, 
        TimeSpan duration, string errorCode, string errorMessage, 
        Dictionary<string, object> diagnosticData = null)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "HolePunchingFailure",
            Level = LogLevel.Error,
            Message = "홀펀칭 실패",
            ClientId = clientId,
            PeerId = peerId,
            Duration = duration,
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            Properties = diagnosticData ?? new()
        };
        
        LogEntry(entry);
    }
    
    public void LogSTUNInteraction(string stunServer, string messageType, 
        bool success, TimeSpan responseTime, string errorMessage = null)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "STUNInteraction",
            Level = success ? LogLevel.Debug : LogLevel.Warning,
            Message = $"STUN {messageType} - {stunServer}",
            Success = success,
            Duration = responseTime,
            ErrorMessage = errorMessage,
            Properties = new Dictionary<string, object>
            {
                ["STUNServer"] = stunServer,
                ["MessageType"] = messageType
            }
        };
        
        LogEntry(entry);
    }
    
    public void LogNATDetection(NATType detectedType, List<IPEndPoint> mappedEndpoints, 
        bool consistent, TimeSpan detectionTime)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "NATDetection",
            Level = LogLevel.Information,
            Message = $"NAT 타입 감지: {detectedType}",
            NATType = detectedType,
            Duration = detectionTime,
            Success = detectedType != NATType.Unknown,
            Properties = new Dictionary<string, object>
            {
                ["MappedEndpoints"] = mappedEndpoints.Select(e => e.ToString()).ToList(),
                ["ConsistentMapping"] = consistent,
                ["EndpointCount"] = mappedEndpoints.Count
            }
        };
        
        LogEntry(entry);
    }
    
    public void LogPacketTransmission(string packetType, int size, 
        IPEndPoint destination, bool success, string errorMessage = null)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "PacketTransmission",
            Level = success ? LogLevel.Trace : LogLevel.Warning,
            Message = $"{packetType} 패킷 전송 ({size} bytes)",
            RemoteEndpoint = destination,
            Success = success,
            ErrorMessage = errorMessage,
            Properties = new Dictionary<string, object>
            {
                ["PacketType"] = packetType,
                ["PacketSize"] = size
            }
        };
        
        LogEntry(entry);
    }
    
    public void LogConnectionStateChange(string clientId, string peerId, 
        string oldState, string newState, string reason = null)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "ConnectionStateChange",
            Level = LogLevel.Information,
            Message = $"연결 상태 변경: {oldState} → {newState}",
            ClientId = clientId,
            PeerId = peerId,
            Properties = new Dictionary<string, object>
            {
                ["OldState"] = oldState,
                ["NewState"] = newState,
                ["Reason"] = reason ?? "Unknown"
            }
        };
        
        LogEntry(entry);
    }
    
    public void LogPerformanceMetric(string metricName, double value, 
        string unit, Dictionary<string, object> context = null)
    {
        var entry = new P2PLogEntry
        {
            SessionId = _sessionId,
            EventType = "PerformanceMetric",
            Level = LogLevel.Debug,
            Message = $"{metricName}: {value} {unit}",
            Properties = new Dictionary<string, object>
            {
                ["MetricName"] = metricName,
                ["MetricValue"] = value,
                ["Unit"] = unit
            }
        };
        
        if (context != null)
        {
            foreach (var kvp in context)
            {
                entry.Properties[kvp.Key] = kvp.Value;
            }
        }
        
        LogEntry(entry);
    }
    
    private void LogEntry(P2PLogEntry entry)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var jsonString = JsonSerializer.Serialize(entry, jsonOptions);
        
        _logger.Log(entry.Level, jsonString);
        
        // 추가로 필요한 경우 외부 로깅 시스템으로 전송
        SendToExternalLoggingSystem(entry);
    }
    
    private void SendToExternalLoggingSystem(P2PLogEntry entry)
    {
        // 예: Elasticsearch, Splunk, Azure Application Insights 등으로 전송
        // 비동기로 처리하여 메인 스레드에 영향 주지 않음
        Task.Run(async () =>
        {
            try
            {
                // 외부 시스템 전송 로직
                await SendToElasticsearch(entry);
            }
            catch (Exception ex)
            {
                // 로깅 실패는 조용히 처리
                Console.WriteLine($"외부 로깅 시스템 전송 실패: {ex.Message}");
            }
        });
    }
    
    private async Task SendToElasticsearch(P2PLogEntry entry)
    {
        // Elasticsearch 전송 예시 (실제 구현은 Elasticsearch 클라이언트 사용)
        // 여기서는 HTTP POST로 간단히 구현
        
        var client = new HttpClient();
        var json = JsonSerializer.Serialize(entry);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Elasticsearch 인덱스에 문서 추가
        var indexName = $"p2p-logs-{DateTime.UtcNow:yyyy.MM.dd}";
        var url = $"http://elasticsearch:9200/{indexName}/_doc";
        
        var response = await client.PostAsync(url, content);
        // 응답 처리는 선택사항
    }
}

// 로깅 컨텍스트 관리
public class P2PLoggingContext : IDisposable
{
    private readonly P2PStructuredLogger _logger;
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly Dictionary<string, object> _context;
    
    public P2PLoggingContext(P2PStructuredLogger logger, string operationName, 
        Dictionary<string, object> context = null)
    {
        _logger = logger;
        _operationName = operationName;
        _context = context ?? new();
        _stopwatch = Stopwatch.StartNew();
        
        // 작업 시작 로그
        LogOperationStart();
    }
    
    private void LogOperationStart()
    {
        var entry = new P2PStructuredLogger.P2PLogEntry
        {
            EventType = "OperationStart",
            Level = LogLevel.Debug,
            Message = $"작업 시작: {_operationName}",
            Properties = new Dictionary<string, object>(_context)
            {
                ["OperationName"] = _operationName
            }
        };
        
        // 로거에 직접 전달
    }
    
    public void AddContextData(string key, object value)
    {
        _context[key] = value;
    }
    
    public void LogProgress(string message, Dictionary<string, object> progressData = null)
    {
        var properties = new Dictionary<string, object>(_context)
        {
            ["OperationName"] = _operationName,
            ["ElapsedMs"] = _stopwatch.ElapsedMilliseconds
        };
        
        if (progressData != null)
        {
            foreach (var kvp in progressData)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }
        
        var entry = new P2PStructuredLogger.P2PLogEntry
        {
            EventType = "OperationProgress",
            Level = LogLevel.Debug,
            Message = $"[{_operationName}] {message}",
            Properties = properties
        };
        
        // 로거에 전달
    }
    
    public void Dispose()
    {
        _stopwatch.Stop();
        
        var entry = new P2PStructuredLogger.P2PLogEntry
        {
            EventType = "OperationComplete",
            Level = LogLevel.Debug,
            Message = $"작업 완료: {_operationName}",
            Duration = _stopwatch.Elapsed,
            Properties = new Dictionary<string, object>(_context)
            {
                ["OperationName"] = _operationName
            }
        };
        
        // 로거에 전달
    }
}
```

### 14.3.2 실시간 모니터링 대시보드

```csharp
// RealtimeMonitoringDashboard.cs
public class P2PRealtimeMonitoringDashboard
{
    private readonly ConcurrentDictionary<string, ClientMetrics> _clientMetrics = new();
    private readonly ConcurrentQueue<SystemEvent> _recentEvents = new();
    private readonly Timer _metricsUpdateTimer;
    private readonly Timer _alertCheckTimer;
    
    public event EventHandler<AlertTriggeredEventArgs> AlertTriggered;
    public event EventHandler<MetricsUpdatedEventArgs> MetricsUpdated;
    
    public class ClientMetrics
    {
        public string ClientId { get; set; }
        public DateTime LastSeen { get; set; }
        public ConnectionStatus Status { get; set; }
        public NATType NATType { get; set; }
        public List<ConnectionAttempt> RecentAttempts { get; set; } = new();
        public PerformanceStats Performance { get; set; } = new();
        public int ActiveConnections { get; set; }
        public TimeSpan AverageConnectionTime { get; set; }
        public double SuccessRate { get; set; }
    }
    
    public class ConnectionAttempt
    {
        public DateTime Timestamp { get; set; }
        public string PeerId { get; set; }
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string FailureReason { get; set; }
    }
    
    public class PerformanceStats
    {
        public double AverageLatency { get; set; }
        public double PacketLossRate { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public int PacketsPerSecond { get; set; }
    }
    
    public class SystemEvent
    {
        public DateTime Timestamp { get; set; }
        public EventSeverity Severity { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
    
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Failed,
        Unknown
    }
    
    public enum EventSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
    
    public class DashboardSnapshot
    {
        public DateTime Timestamp { get; set; }
        public SystemOverview Overview { get; set; }
        public List<ClientMetrics> TopClients { get; set; }
        public List<SystemEvent> RecentEvents { get; set; }
        public PerformanceOverview Performance { get; set; }
        public List<ActiveAlert> ActiveAlerts { get; set; }
    }
    
    public class SystemOverview
    {
        public int TotalClients { get; set; }
        public int ActiveConnections { get; set; }
        public int ConnectionAttempts { get; set; }
        public double OverallSuccessRate { get; set; }
        public double AverageConnectionTime { get; set; }
        public Dictionary<NATType, int> NATTypeDistribution { get; set; } = new();
    }
    
    public class PerformanceOverview
    {
        public double AverageLatency { get; set; }
        public double AveragePacketLoss { get; set; }
        public long TotalBytesTransferred { get; set; }
        public int PeakConcurrentConnections { get; set; }
        public double SystemCpuUsage { get; set; }
        public long SystemMemoryUsage { get; set; }
    }
    
    public class ActiveAlert
    {
        public string Id { get; set; }
        public DateTime TriggeredAt { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Acknowledged { get; set; }
    }
    
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public P2PRealtimeMonitoringDashboard()
    {
        // 1초마다 메트릭 업데이트
        _metricsUpdateTimer = new Timer(UpdateMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        
        // 5초마다 알람 검사
        _alertCheckTimer = new Timer(CheckAlerts, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }
    
    public void UpdateClientMetrics(string clientId, Action<ClientMetrics> updateAction)
    {
        var metrics = _clientMetrics.GetOrAdd(clientId, _ => new ClientMetrics
        {
            ClientId = clientId,
            LastSeen = DateTime.UtcNow,
            Status = ConnectionStatus.Unknown
        });
        
        updateAction(metrics);
        metrics.LastSeen = DateTime.UtcNow;
    }
    
    public void RecordConnectionAttempt(string clientId, string peerId, 
        bool success, TimeSpan duration, string failureReason = null)
    {
        UpdateClientMetrics(clientId, metrics =>
        {
            var attempt = new ConnectionAttempt
            {
                Timestamp = DateTime.UtcNow,
                PeerId = peerId,
                Success = success,
                Duration = duration,
                FailureReason = failureReason
            };
            
            metrics.RecentAttempts.Add(attempt);
            
            // 최근 100개 시도만 보관
            if (metrics.RecentAttempts.Count > 100)
            {
                metrics.RecentAttempts.RemoveAt(0);
            }
            
            // 성공률 재계산
            var recentAttempts = metrics.RecentAttempts
                .Where(a => DateTime.UtcNow - a.Timestamp < TimeSpan.FromMinutes(10))
                .ToList();
            
            if (recentAttempts.Any())
            {
                metrics.SuccessRate = (double)recentAttempts.Count(a => a.Success) / recentAttempts.Count * 100;
                metrics.AverageConnectionTime = TimeSpan.FromMilliseconds(
                    recentAttempts.Where(a => a.Success).DefaultIfEmpty().Average(a => a.Duration.TotalMilliseconds));
            }
        });
        
        // 시스템 이벤트 기록
        var eventSeverity = success ? EventSeverity.Info : EventSeverity.Warning;
        var eventMessage = success 
            ? $"클라이언트 {clientId}가 {peerId}와 연결 성공 ({duration.TotalMilliseconds:F0}ms)"
            : $"클라이언트 {clientId}가 {peerId}와 연결 실패: {failureReason}";
        
        RecordSystemEvent(eventSeverity, "Connection", eventMessage, new Dictionary<string, object>
        {
            ["ClientId"] = clientId,
            ["PeerId"] = peerId,
            ["Success"] = success,
            ["Duration"] = duration.TotalMilliseconds,
            ["FailureReason"] = failureReason
        });
    }
    
    public void RecordSystemEvent(EventSeverity severity, string category, 
        string message, Dictionary<string, object> data = null)
    {
        var systemEvent = new SystemEvent
        {
            Timestamp = DateTime.UtcNow,
            Severity = severity,
            Category = category,
            Message = message,
            Data = data ?? new()
        };
        
        _recentEvents.Enqueue(systemEvent);
        
        // 최근 1000개 이벤트만 보관
        while (_recentEvents.Count > 1000)
        {
            _recentEvents.TryDequeue(out _);
        }
        
        // 높은 심각도 이벤트는 즉시 알람 확인
        if (severity >= EventSeverity.Error)
        {
            CheckCriticalAlerts();
        }
    }
    
    private void UpdateMetrics(object state)
    {
        try
        {
            // 메트릭 업데이트 및 정리
            CleanupOldMetrics();
            
            // 스냅샷 생성
            var snapshot = CreateDashboardSnapshot();
            
            // 메트릭 업데이트 이벤트 발생
            MetricsUpdated?.Invoke(this, new MetricsUpdatedEventArgs { Snapshot = snapshot });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"메트릭 업데이트 오류: {ex.Message}");
        }
    }
    
    private void CleanupOldMetrics()
    {
        var cutoffTime = DateTime.UtcNow - TimeSpan.FromMinutes(30);
        
        var oldClients = _clientMetrics
            .Where(kvp => kvp.Value.LastSeen < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var clientId in oldClients)
        {
            _clientMetrics.TryRemove(clientId, out _);
        }
    }
    
    private DashboardSnapshot CreateDashboardSnapshot()
    {
        var allClients = _clientMetrics.Values.ToList();
        var recentEvents = _recentEvents.TakeLast(50).ToList();
        
        // 시스템 개요 계산
        var overview = new SystemOverview
        {
            TotalClients = allClients.Count,
            ActiveConnections = allClients.Sum(c => c.ActiveConnections),
            ConnectionAttempts = allClients.Sum(c => c.RecentAttempts.Count),
            OverallSuccessRate = allClients.Any() ? allClients.Average(c => c.SuccessRate) : 0,
            AverageConnectionTime = allClients.Any() 
                ? TimeSpan.FromMilliseconds(allClients.Average(c => c.AverageConnectionTime.TotalMilliseconds))
                : TimeSpan.Zero,
            NATTypeDistribution = allClients
                .GroupBy(c => c.NATType)
                .ToDictionary(g => g.Key, g => g.Count())
        };
        
        // 성능 개요 계산
        var performance = new PerformanceOverview
        {
            AverageLatency = allClients.Any() ? allClients.Average(c => c.Performance.AverageLatency) : 0,
            AveragePacketLoss = allClients.Any() ? allClients.Average(c => c.Performance.PacketLossRate) : 0,
            TotalBytesTransferred = allClients.Sum(c => c.Performance.BytesSent + c.Performance.BytesReceived),
            PeakConcurrentConnections = allClients.Max(c => c.ActiveConnections),
            SystemCpuUsage = GetSystemCpuUsage(),
            SystemMemoryUsage = GC.GetTotalMemory(false)
        };
        
        return new DashboardSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Overview = overview,
            TopClients = allClients
                .OrderByDescending(c => c.SuccessRate)
                .Take(10)
                .ToList(),
            RecentEvents = recentEvents,
            Performance = performance,
            ActiveAlerts = GetActiveAlerts()
        };
    }
    
    private void CheckAlerts(object state)
    {
        try
        {
            CheckPerformanceAlerts();
            CheckConnectivityAlerts();
            CheckSystemResourceAlerts();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"알람 검사 오류: {ex.Message}");
        }
    }
    
    private void CheckCriticalAlerts()
    {
        // 즉시 확인이 필요한 중요 알람들
        var criticalEvents = _recentEvents
            .Where(e => e.Severity == EventSeverity.Critical && 
                       DateTime.UtcNow - e.Timestamp < TimeSpan.FromMinutes(1))
            .ToList();
        
        foreach (var evt in criticalEvents)
        {
            TriggerAlert(AlertSeverity.Critical, 
                $"Critical Event: {evt.Category}", 
                evt.Message);
        }
    }
    
    private void CheckPerformanceAlerts()
    {
        var allClients = _clientMetrics.Values.ToList();
        if (!allClients.Any()) return;
        
        // 평균 성공률 확인
        var overallSuccessRate = allClients.Average(c => c.SuccessRate);
        if (overallSuccessRate < 80)
        {
            TriggerAlert(AlertSeverity.High, 
                "Low Success Rate", 
                $"전체 연결 성공률이 {overallSuccessRate:F1}%로 낮습니다");
        }
        
        // 평균 지연시간 확인
        var averageLatency = allClients.Average(c => c.Performance.AverageLatency);
        if (averageLatency > 500)
        {
            TriggerAlert(AlertSeverity.Medium, 
                "High Latency", 
                $"평균 지연시간이 {averageLatency:F0}ms로 높습니다");
        }
        
        // 패킷 손실률 확인
        var averagePacketLoss = allClients.Average(c => c.Performance.PacketLossRate);
        if (averagePacketLoss > 0.05)
        {
            TriggerAlert(AlertSeverity.High, 
                "High Packet Loss", 
                $"평균 패킷 손실률이 {averagePacketLoss * 100:F1}%로 높습니다");
        }
    }
    
    private void CheckConnectivityAlerts()
    {
        var allClients = _clientMetrics.Values.ToList();
        
        // 대량 연결 실패 감지
        var recentFailures = allClients
            .SelectMany(c => c.RecentAttempts)
            .Where(a => !a.Success && DateTime.UtcNow - a.Timestamp < TimeSpan.FromMinutes(5))
            .ToList();
        
        if (recentFailures.Count > 20)
        {
            TriggerAlert(AlertSeverity.High, 
                "Mass Connection Failures", 
                $"최근 5분간 {recentFailures.Count}건의 연결 실패가 발생했습니다");
        }
        
        // 특정 NAT 타입 문제 감지
        var symmetricNATClients = allClients.Count(c => c.NATType == NATType.Symmetric);
        var totalClients = allClients.Count;
        
        if (totalClients > 0 && (double)symmetricNATClients / totalClients > 0.3)
        {
            TriggerAlert(AlertSeverity.Medium, 
                "High Symmetric NAT Ratio", 
                $"대칭 NAT 클라이언트 비율이 {(double)symmetricNATClients / totalClients * 100:F1}%로 높습니다");
        }
    }
    
    private void CheckSystemResourceAlerts()
    {
        // CPU 사용률 확인
        var cpuUsage = GetSystemCpuUsage();
        if (cpuUsage > 80)
        {
            TriggerAlert(AlertSeverity.High, 
                "High CPU Usage", 
                $"시스템 CPU 사용률이 {cpuUsage:F1}%입니다");
        }
        
        // 메모리 사용량 확인
        var memoryUsage = GC.GetTotalMemory(false);
        var memoryMB = memoryUsage / (1024 * 1024);
        if (memoryMB > 1000)
        {
            TriggerAlert(AlertSeverity.Medium, 
                "High Memory Usage", 
                $"메모리 사용량이 {memoryMB:F0}MB입니다");
        }
    }
    
    private void TriggerAlert(AlertSeverity severity, string title, string description)
    {
        var alert = new ActiveAlert
        {
            Id = Guid.NewGuid().ToString(),
            TriggeredAt = DateTime.UtcNow,
            Severity = severity,
            Title = title,
            Description = description,
            Acknowledged = false
        };
        
        AlertTriggered?.Invoke(this, new AlertTriggeredEventArgs { Alert = alert });
    }
    
    private double GetSystemCpuUsage()
    {
        // 간단한 CPU 사용률 측정 (실제로는 PerformanceCounter 사용)
        try
        {
            using var process = Process.GetCurrentProcess();
            return process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100;
        }
        catch
        {
            return 0;
        }
    }
    
    private List<ActiveAlert> GetActiveAlerts()
    {
        // 실제 구현에서는 알람 저장소에서 활성 알람 조회
        return new List<ActiveAlert>();
    }
    
    public void PrintDashboard()
    {
        var snapshot = CreateDashboardSnapshot();
        
        Console.Clear();
        Console.WriteLine("=== P2P 실시간 모니터링 대시보드 ===");
        Console.WriteLine($"업데이트 시간: {snapshot.Timestamp:HH:mm:ss}");
        Console.WriteLine();
        
        // 시스템 개요
        Console.WriteLine("### 시스템 개요");
        Console.WriteLine($"총 클라이언트: {snapshot.Overview.TotalClients}");
        Console.WriteLine($"활성 연결: {snapshot.Overview.ActiveConnections}");
        Console.WriteLine($"연결 시도: {snapshot.Overview.ConnectionAttempts}");
        Console.WriteLine($"전체 성공률: {snapshot.Overview.OverallSuccessRate:F1}%");
        Console.WriteLine($"평균 연결 시간: {snapshot.Overview.AverageConnectionTime.TotalMilliseconds:F0}ms");
        Console.WriteLine();
        
        // NAT 타입 분포
        Console.WriteLine("### NAT 타입 분포");
        foreach (var kvp in snapshot.Overview.NATTypeDistribution)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
        Console.WriteLine();
        
        // 성능 개요
        Console.WriteLine("### 성능 개요");
        Console.WriteLine($"평균 지연시간: {snapshot.Performance.AverageLatency:F0}ms");
        Console.WriteLine($"평균 패킷 손실: {snapshot.Performance.AveragePacketLoss * 100:F2}%");
        Console.WriteLine($"총 전송량: {snapshot.Performance.TotalBytesTransferred / (1024 * 1024):F1}MB");
        Console.WriteLine($"최대 동시 연결: {snapshot.Performance.PeakConcurrentConnections}");
        Console.WriteLine($"시스템 CPU: {snapshot.Performance.SystemCpuUsage:F1}%");
        Console.WriteLine($"시스템 메모리: {snapshot.Performance.SystemMemoryUsage / (1024 * 1024):F0}MB");
        Console.WriteLine();
        
        // 최근 이벤트
        Console.WriteLine("### 최근 이벤트 (최근 10개)");
        foreach (var evt in snapshot.RecentEvents.TakeLast(10))
        {
            var severityIcon = evt.Severity switch
            {
                EventSeverity.Critical => "🔴",
                EventSeverity.Error => "🟠",
                EventSeverity.Warning => "🟡",
                _ => "ℹ️"
            };
            Console.WriteLine($"{severityIcon} [{evt.Timestamp:HH:mm:ss}] [{evt.Category}] {evt.Message}");
        }
    }
    
    public void Dispose()
    {
        _metricsUpdateTimer?.Dispose();
        _alertCheckTimer?.Dispose();
    }
}

public class MetricsUpdatedEventArgs : EventArgs
{
    public P2PRealtimeMonitoringDashboard.DashboardSnapshot Snapshot { get; set; }
}

public class AlertTriggeredEventArgs : EventArgs
{
    public P2PRealtimeMonitoringDashboard.ActiveAlert Alert { get; set; }
}
```

## 14.4 운영 환경에서의 트러블슈팅

### 14.4.1 운영 환경 문제 해결 가이드

```csharp
// ProductionTroubleshootingGuide.cs
public class ProductionTroubleshootingGuide
{
    public class TroubleshootingStep
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<Task<StepResult>> ExecuteAction { get; set; }
        public List<string> RequiredTools { get; set; } = new();
        public TimeSpan EstimatedDuration { get; set; }
        public int Priority { get; set; } // 1=높음, 5=낮음
    }
    
    public class StepResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public List<string> NextSteps { get; set; } = new();
    }
    
    public class TroubleshootingSession
    {
        public string SessionId { get; set; }
        public DateTime StartTime { get; set; }
        public string ProblemDescription { get; set; }
        public List<TroubleshootingStep> ExecutedSteps { get; set; } = new();
        public Dictionary<string, object> CollectedData { get; set; } = new();
        public bool Resolved { get; set; }
        public string Resolution { get; set; }
    }
    
    // 일반적인 문제 시나리오별 가이드
    private readonly Dictionary<string, List<TroubleshootingStep>> _troubleshootingGuides = new();
    
    public ProductionTroubleshootingGuide()
    {
        InitializeTroubleshootingGuides();
    }
    
    private void InitializeTroubleshootingGuides()
    {
        // 1. 홀펀칭 성공률 급감 문제
        _troubleshootingGuides["low_success_rate"] = new List<TroubleshootingStep>
        {
            new()
            {
                Name = "기본 연결성 확인",
                Description = "STUN 서버 연결 상태 및 기본 네트워크 연결성 확인",
                ExecuteAction = CheckBasicConnectivity,
                RequiredTools = new() { "ping", "telnet", "nslookup" },
                EstimatedDuration = TimeSpan.FromMinutes(2),
                Priority = 1
            },
            new()
            {
                Name = "STUN 서버 상태 확인",
                Description = "모든 STUN 서버의 응답 상태 및 성능 확인",
                ExecuteAction = CheckSTUNServerStatus,
                RequiredTools = new() { "udp_test_tool" },
                EstimatedDuration = TimeSpan.FromMinutes(3),
                Priority = 1
            },
            new()
            {
                Name = "NAT 타입 분포 분석",
                Description = "클라이언트들의 NAT 타입 분포 변화 확인",
                ExecuteAction = AnalyzeNATTypeDistribution,
                RequiredTools = new() { "log_analyzer" },
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 2
            },
            new()
            {
                Name = "ISP별 성공률 분석",
                Description = "ISP별 홀펀칭 성공률 비교 분석",
                ExecuteAction = AnalyzeISPSuccessRates,
                RequiredTools = new() { "geoip_database", "log_analyzer" },
                EstimatedDuration = TimeSpan.FromMinutes(10),
                Priority = 2
            },
            new()
            {
                Name = "시간대별 패턴 분석",
                Description = "시간대별 성공률 변화 패턴 분석",
                ExecuteAction = AnalyzeTimeBasedPatterns,
                RequiredTools = new() { "time_series_analyzer" },
                EstimatedDuration = TimeSpan.FromMinutes(8),
                Priority = 3
            }
        };
        
        // 2. 높은 지연시간 문제
        _troubleshootingGuides["high_latency"] = new List<TroubleshootingStep>
        {
            new()
            {
                Name = "네트워크 지연시간 측정",
                Description = "다양한 목적지에 대한 네트워크 지연시간 측정",
                ExecuteAction = MeasureNetworkLatency,
                RequiredTools = new() { "ping", "traceroute", "mtr" },
                EstimatedDuration = TimeSpan.FromMinutes(5),
                Priority = 1
            },
            new()
            {
                Name = "STUN 서버 응답시간 분석",
                Description = "STUN 서버별 응답시간 상세 분석",
                ExecuteAction = AnalyzeSTUNResponseTimes,
                RequiredTools = new() { "stun_profiler" },
                EstimatedDuration = TimeSpan.FromMinutes(3),
                Priority = 1
            },
            new()
            {
                Name = "시스템 리소스 확인",
                Description = "CPU, 메모리, 네트워크 인터페이스 상태 확인",
                ExecuteAction = CheckSystemResources,
                RequiredTools = new() { "top", "iostat", "netstat" },
                EstimatedDuration = TimeSpan.FromMinutes(2),
                Priority = 1
            },
            new()
            {
                Name = "네트워크 경로 분석",
                Description = "패킷 경로 및 중간 홉들의 지연시간 분석",
                ExecuteAction = AnalyzeNetworkPath,
                RequiredTools = new() { "traceroute", "mtr" },
                EstimatedDuration = TimeSpan.FromMinutes(10),
                Priority = 2
            }
        };
        
        // 3. 메모리 누수 문제
        _troubleshootingGuides["memory_leak"] = new List<TroubleshootingStep>
        {
            new()
            {
                Name = "메모리 사용량 트렌드 분석",
                Description = "시간에 따른 메모리 사용량 증가 패턴 분석",
                ExecuteAction = AnalyzeMemoryTrend,
                RequiredTools = new() { "memory_profiler", "gc_analyzer" },
                EstimatedDuration = TimeSpan.FromMinutes(15),
                Priority = 1
            },
            new()
            {
                Name = "GC 동작 분석",
                Description = "가비지 컬렉션 빈도 및 성능 분석",
                ExecuteAction = AnalyzeGCBehavior,
                RequiredTools = new() { "gc_profiler", "dotnet_counters" },
                EstimatedDuration = TimeSpan.FromMinutes(10),
                Priority = 1
            },
            new()
            {
                Name = "객체 참조 추적",
                Description = "메모리에 남아있는 객체들의 참조 관계 분석",
                ExecuteAction = TraceObjectReferences,
                RequiredTools = new() { "heap_dump_analyzer", "memory_profiler" },
                EstimatedDuration = TimeSpan.FromMinutes(20),
                Priority = 2
            }
        };
    }
    
    public async Task<TroubleshootingSession> StartTroubleshootingSession(
        string problemType, 
        string problemDescription)
    {
        var session = new TroubleshootingSession
        {
            SessionId = Guid.NewGuid().ToString("N")[..8],
            StartTime = DateTime.UtcNow,
            ProblemDescription = problemDescription
        };
        
        Console.WriteLine($"=== 트러블슈팅 세션 시작 ===");
        Console.WriteLine($"세션 ID: {session.SessionId}");
        Console.WriteLine($"문제 유형: {problemType}");
        Console.WriteLine($"문제 설명: {problemDescription}");
        Console.WriteLine($"시작 시간: {session.StartTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();
        
        if (_troubleshootingGuides.TryGetValue(problemType, out var steps))
        {
            await ExecuteTroubleshootingSteps(session, steps);
        }
        else
        {
            Console.WriteLine($"알 수 없는 문제 유형: {problemType}");
            await ExecuteGenericTroubleshooting(session);
        }
        
        GenerateTroubleshootingReport(session);
        return session;
    }
    
    private async Task ExecuteTroubleshootingSteps(
        TroubleshootingSession session, 
        List<TroubleshootingStep> steps)
    {
        var sortedSteps = steps.OrderBy(s => s.Priority).ToList();
        
        foreach (var step in sortedSteps)
        {
            Console.WriteLine($"📋 실행 중: {step.Name}");
            Console.WriteLine($"   설명: {step.Description}");
            Console.WriteLine($"   예상 소요 시간: {step.EstimatedDuration}");
            
            var startTime = DateTime.UtcNow;
            
            try
            {
                var result = await step.ExecuteAction();
                var duration = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"   결과: {(result.Success ? "✅ 성공" : "❌ 실패")}");
                Console.WriteLine($"   메시지: {result.Message}");
                Console.WriteLine($"   실제 소요 시간: {duration}");
                
                // 수집된 데이터 저장
                foreach (var kvp in result.Data)
                {
                    session.CollectedData[$"{step.Name}_{kvp.Key}"] = kvp.Value;
                }
                
                session.ExecutedSteps.Add(step);
                
                // 다음 단계 제안이 있는 경우
                if (result.NextSteps.Any())
                {
                    Console.WriteLine("   권장 다음 단계:");
                    foreach (var nextStep in result.NextSteps)
                    {
                        Console.WriteLine($"     - {nextStep}");
                    }
                }
                
                Console.WriteLine();
                
                // 심각한 문제 발견 시 즉시 알림
                if (!result.Success && step.Priority == 1)
                {
                    Console.WriteLine("⚠️ 중요한 문제가 발견되었습니다. 즉시 조치가 필요합니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   오류 발생: {ex.Message}");
                session.CollectedData[$"{step.Name}_Error"] = ex.Message;
            }
            
            // 단계 간 잠시 대기
            await Task.Delay(1000);
        }
    }
    
    private async Task<StepResult> CheckBasicConnectivity()
    {
        var result = new StepResult();
        var connectivityIssues = new List<string>();
        
        // 인터넷 연결 확인
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("8.8.8.8", 5000);
            
            if (reply.Status == IPStatus.Success)
            {
                result.Data["InternetConnectivity"] = true;
                result.Data["InternetLatency"] = reply.RoundtripTime;
            }
            else
            {
                connectivityIssues.Add("인터넷 연결 실패");
                result.Data["InternetConnectivity"] = false;
            }
        }
        catch (Exception ex)
        {
            connectivityIssues.Add($"인터넷 연결 테스트 오류: {ex.Message}");
            result.Data["InternetConnectivity"] = false;
        }
        
        // DNS 해결 확인
        try
        {
            var addresses = await Dns.GetHostAddressesAsync("google.com");
            result.Data["DNSResolution"] = addresses.Length > 0;
            
            if (addresses.Length == 0)
            {
                connectivityIssues.Add("DNS 해결 실패");
            }
        }
        catch (Exception ex)
        {
            connectivityIssues.Add($"DNS 해결 오류: {ex.Message}");
            result.Data["DNSResolution"] = false;
        }
        
        result.Success = !connectivityIssues.Any();
        result.Message = result.Success 
            ? "기본 네트워크 연결성 정상" 
            : $"문제 발견: {string.Join(", ", connectivityIssues)}";
        
        if (!result.Success)
        {
            result.NextSteps.Add("네트워크 설정 확인");
            result.NextSteps.Add("방화벽 설정 검토");
            result.NextSteps.Add("ISP 문의");
        }
        
        return result;
    }
    
    private async Task<StepResult> CheckSTUNServerStatus()
    {
        var result = new StepResult();
        var stunServers = new[]
        {
            "stun.l.google.com:19302",
            "stun1.l.google.com:19302",
            "stun.stunprotocol.org:3478",
            "stun.voiparound.com:3478"
        };
        
        var serverResults = new Dictionary<string, object>();
        var failedServers = new List<string>();
        
        foreach (var server in stunServers)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var isReachable = await TestSTUNServer(server);
                stopwatch.Stop();
                
                serverResults[server] = new
                {
                    Reachable = isReachable,
                    ResponseTime = stopwatch.ElapsedMilliseconds
                };
                
                if (!isReachable)
                {
                    failedServers.Add(server);
                }
            }
            catch (Exception ex)
            {
                serverResults[server] = new
                {
                    Reachable = false,
                    Error = ex.Message
                };
                failedServers.Add(server);
            }
        }
        
        result.Data["STUNServerResults"] = serverResults;
        result.Data["FailedServers"] = failedServers;
        result.Data["SuccessRate"] = (double)(stunServers.Length - failedServers.Count) / stunServers.Length * 100;
        
        result.Success = failedServers.Count < stunServers.Length / 2; // 50% 이상 성공
        result.Message = result.Success
            ? $"STUN 서버 상태 양호 ({stunServers.Length - failedServers.Count}/{stunServers.Length} 서버 응답)"
            : $"STUN 서버 문제 ({failedServers.Count}/{stunServers.Length} 서버 실패)";
        
        if (!result.Success)
        {
            result.NextSteps.Add("대체 STUN 서버 추가");
            result.NextSteps.Add("STUN 서버 제공업체 문의");
            result.NextSteps.Add("방화벽에서 UDP 트래픽 허용 확인");
        }
        
        return result;
    }
    
    private async Task<bool> TestSTUNServer(string serverEndpoint)
    {
        try
        {
            var parts = serverEndpoint.Split(':');
            var host = parts[0];
            var port = int.Parse(parts[1]);
            
            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = 5000;
            
            var addresses = await Dns.GetHostAddressesAsync(host);
            var endpoint = new IPEndPoint(addresses[0], port);
            
            // STUN Binding Request 전송
            var request = CreateSTUNBindingRequest();
            await udpClient.SendAsync(request, request.Length, endpoint);
            
            // 응답 대기
            var response = await udpClient.ReceiveAsync();
            return response.Buffer.Length >= 20;
        }
        catch
        {
            return false;
        }
    }
    
    private byte[] CreateSTUNBindingRequest()
    {
        var request = new byte[20];
        
        // Message Type: Binding Request
        request[0] = 0x00;
        request[1] = 0x01;
        
        // Message Length: 0
        request[2] = 0x00;
        request[3] = 0x00;
        
        // Magic Cookie
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xa4;
        request[7] = 0x42;
        
        // Transaction ID (12 bytes)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            request[i] = (byte)random.Next(256);
        }
        
        return request;
    }
    
    // 추가 트러블슈팅 메서드들 (간략히 구현)
    private async Task<StepResult> AnalyzeNATTypeDistribution()
    {
        // NAT 타입 분포 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "NAT 타입 분포 분석 완료",
            Data = new()
            {
                ["FullCone"] = 30,
                ["RestrictedCone"] = 25,
                ["PortRestrictedCone"] = 20,
                ["Symmetric"] = 15,
                ["Unknown"] = 10
            }
        };
    }
    
    private async Task<StepResult> AnalyzeISPSuccessRates()
    {
        // ISP별 성공률 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "ISP별 성공률 분석 완료"
        };
    }
    
    private async Task<StepResult> AnalyzeTimeBasedPatterns()
    {
        // 시간대별 패턴 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "시간대별 패턴 분석 완료"
        };
    }
    
    private async Task<StepResult> MeasureNetworkLatency()
    {
        // 네트워크 지연시간 측정 로직
        return new StepResult
        {
            Success = true,
            Message = "네트워크 지연시간 측정 완료"
        };
    }
    
    private async Task<StepResult> AnalyzeSTUNResponseTimes()
    {
        // STUN 응답시간 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "STUN 응답시간 분석 완료"
        };
    }
    
    private async Task<StepResult> CheckSystemResources()
    {
        // 시스템 리소스 확인 로직
        return new StepResult
        {
            Success = true,
            Message = "시스템 리소스 확인 완료"
        };
    }
    
    private async Task<StepResult> AnalyzeNetworkPath()
    {
        // 네트워크 경로 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "네트워크 경로 분석 완료"
        };
    }
    
    private async Task<StepResult> AnalyzeMemoryTrend()
    {
        // 메모리 트렌드 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "메모리 트렌드 분석 완료"
        };
    }
    
    private async Task<StepResult> AnalyzeGCBehavior()
    {
        // GC 동작 분석 로직
        return new StepResult
        {
            Success = true,
            Message = "GC 동작 분석 완료"
        };
    }
    
    private async Task<StepResult> TraceObjectReferences()
    {
        // 객체 참조 추적 로직
        return new StepResult
        {
            Success = true,
            Message = "객체 참조 추적 완료"
        };
    }
    
    private async Task ExecuteGenericTroubleshooting(TroubleshootingSession session)
    {
        Console.WriteLine("일반적인 트러블슈팅 절차를 실행합니다...");
        
        // 기본 진단 단계들 실행
        var genericSteps = new[]
        {
            CheckBasicConnectivity,
            CheckSTUNServerStatus,
            CheckSystemResources
        };
        
        foreach (var step in genericSteps)
        {
            try
            {
                var result = await step();
                Console.WriteLine($"진단 결과: {result.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"진단 오류: {ex.Message}");
            }
        }
    }
    
    private void GenerateTroubleshootingReport(TroubleshootingSession session)
    {
        Console.WriteLine("\n=== 트러블슈팅 보고서 ===");
        Console.WriteLine($"세션 ID: {session.SessionId}");
        Console.WriteLine($"시작 시간: {session.StartTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"소요 시간: {DateTime.UtcNow - session.StartTime}");
        Console.WriteLine($"실행된 단계 수: {session.ExecutedSteps.Count}");
        Console.WriteLine($"해결 여부: {(session.Resolved ? "해결됨" : "미해결")}");
        
        if (!string.IsNullOrEmpty(session.Resolution))
        {
            Console.WriteLine($"해결 방법: {session.Resolution}");
        }
        
        Console.WriteLine("\n### 수집된 데이터");
        foreach (var kvp in session.CollectedData)
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
        
        Console.WriteLine("\n### 권장사항");
        Console.WriteLine("- 정기적인 네트워크 연결성 모니터링 구현");
        Console.WriteLine("- 다중 STUN 서버 구성으로 안정성 향상");
        Console.WriteLine("- 자동화된 알람 시스템 구축");
        Console.WriteLine("- 성능 메트릭 기반 임계값 설정");
    }
}

// 프로그램 실행 예제
class Program
{
    static async Task Main(string[] args)
    {
        // 트러블슈팅 가이드 사용 예제
        var troubleshootingGuide = new ProductionTroubleshootingGuide();
        
        Console.WriteLine("P2P 홀펀칭 트러블슈팅 도구");
        Console.WriteLine("사용 가능한 문제 유형:");
        Console.WriteLine("1. low_success_rate - 홀펀칭 성공률 급감");
        Console.WriteLine("2. high_latency - 높은 지연시간");
        Console.WriteLine("3. memory_leak - 메모리 누수");
        Console.WriteLine();
        
        Console.Write("문제 유형을 입력하세요: ");
        var problemType = Console.ReadLine();
        
        Console.Write("문제 상세 설명을 입력하세요: ");
        var problemDescription = Console.ReadLine();
        
        var session = await troubleshootingGuide.StartTroubleshootingSession(
            problemType, 
            problemDescription);
        
        Console.WriteLine("\n트러블슈팅이 완료되었습니다.");
        Console.ReadKey();
    }
}
```

이 14장에서는 P2P 홀펀칭 시스템에서 발생할 수 있는 다양한 문제들을 체계적으로 분석하고 해결하는 방법을 다뤘다. 실패 원인 분석, 네트워크 진단 도구, 구조화된 로깅 시스템, 실시간 모니터링, 그리고 운영 환경에서의 실용적인 트러블슈팅 가이드를 제공했다. 이러한 도구들을 활용하면 온라인 액션 게임의 P2P 연결 품질을 안정적으로 유지하고 문제 발생 시 신속하게 대응할 수 있다.   


# 15장: 성능 테스트와 최적화

## 15.1 홀펀칭 성능 테스트 방법론

### 15.1.1 테스트 목적과 범위

P2P 홀펀칭 성능 테스트는 다음과 같은 목적을 가진다:

1. **연결 성공률 측정**: 다양한 NAT 환경에서의 홀펀칭 성공률
2. **연결 시간 측정**: 홀펀칭 완료까지 소요되는 시간
3. **네트워크 대역폭 효율성**: STUN/TURN 서버 트래픽 분석
4. **동시 연결 처리 능력**: 다중 클라이언트 환경에서의 성능

### 15.1.2 테스트 환경 구성

```csharp
// PerformanceTestFramework.cs
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class HolePunchingPerformanceTest
{
    private readonly ConcurrentBag<TestResult> _results = new();
    private readonly Stopwatch _globalStopwatch = new();
    
    public class TestResult
    {
        public bool Success { get; set; }
        public TimeSpan ConnectionTime { get; set; }
        public string ErrorMessage { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public NATType DetectedNATType { get; set; }
    }
    
    public enum NATType
    {
        Open,
        FullCone,
        RestrictedCone,
        PortRestrictedCone,
        Symmetric,
        Unknown
    }
    
    // 대규모 홀펀칭 테스트 실행
    public async Task<TestSummary> RunBulkTest(int clientCount, TimeSpan timeout)
    {
        _globalStopwatch.Start();
        
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(50); // 동시 연결 제한
        
        for (int i = 0; i < clientCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await RunSingleTest(timeout);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        _globalStopwatch.Stop();
        
        return AnalyzeResults();
    }
    
    private async Task RunSingleTest(TimeSpan timeout)
    {
        var testResult = new TestResult();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var udpClient = new UdpClient();
            
            // NAT 타입 감지
            testResult.DetectedNATType = await DetectNATType(udpClient);
            testResult.LocalEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            
            // 홀펀칭 시도
            var success = await AttemptHolePunching(udpClient, timeout);
            
            testResult.Success = success;
            testResult.ConnectionTime = stopwatch.Elapsed;
        }
        catch (Exception ex)
        {
            testResult.Success = false;
            testResult.ErrorMessage = ex.Message;
            testResult.ConnectionTime = stopwatch.Elapsed;
        }
        
        _results.Add(testResult);
    }
}

// 테스트 결과 분석
public class TestSummary
{
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public double SuccessRate => TotalTests > 0 ? (double)SuccessfulTests / TotalTests * 100 : 0;
    public TimeSpan AverageConnectionTime { get; set; }
    public TimeSpan MedianConnectionTime { get; set; }
    public TimeSpan MaxConnectionTime { get; set; }
    public TimeSpan MinConnectionTime { get; set; }
    public Dictionary<NATType, int> NATTypeDistribution { get; set; } = new();
    public Dictionary<NATType, double> SuccessRateByNATType { get; set; } = new();
}
```

### 15.1.3 NAT 타입별 성능 측정

```csharp
// NATTypePerformanceAnalyzer.cs
public class NATTypePerformanceAnalyzer
{
    private readonly Dictionary<NATType, List<PerformanceMetric>> _metrics = new();
    
    public class PerformanceMetric
    {
        public TimeSpan HolePunchingTime { get; set; }
        public int AttemptCount { get; set; }
        public bool Success { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
    }
    
    public async Task<NATType> DetectNATType(UdpClient udpClient)
    {
        try
        {
            // STUN 서버를 이용한 NAT 타입 감지
            var stunServers = new[]
            {
                "stun.l.google.com:19302",
                "stun1.l.google.com:19302",
                "stun2.l.google.com:19302"
            };
            
            var results = new List<STUNResult>();
            
            foreach (var server in stunServers)
            {
                var result = await PerformSTUNTest(udpClient, server);
                if (result != null)
                    results.Add(result);
            }
            
            return AnalyzeSTUNResults(results);
        }
        catch
        {
            return NATType.Unknown;
        }
    }
    
    private async Task<STUNResult> PerformSTUNTest(UdpClient udpClient, string stunServer)
    {
        var parts = stunServer.Split(':');
        var host = parts[0];
        var port = int.Parse(parts[1]);
        
        var endpoint = new IPEndPoint(IPAddress.Parse(await ResolveHostname(host)), port);
        
        // STUN Binding Request 생성
        var request = CreateSTUNBindingRequest();
        
        var stopwatch = Stopwatch.StartNew();
        await udpClient.SendAsync(request, request.Length, endpoint);
        
        var response = await udpClient.ReceiveAsync();
        stopwatch.Stop();
        
        return ParseSTUNResponse(response.Buffer, stopwatch.Elapsed);
    }
    
    private byte[] CreateSTUNBindingRequest()
    {
        var message = new byte[20];
        
        // STUN Header
        message[0] = 0x00; // Message Type: Binding Request
        message[1] = 0x01;
        message[2] = 0x00; // Message Length
        message[3] = 0x00;
        
        // Magic Cookie
        message[4] = 0x21;
        message[5] = 0x12;
        message[6] = 0xa4;
        message[7] = 0x42;
        
        // Transaction ID (12 bytes)
        var random = new Random();
        for (int i = 8; i < 20; i++)
        {
            message[i] = (byte)random.Next(256);
        }
        
        return message;
    }
}

public class STUNResult
{
    public IPEndPoint MappedAddress { get; set; }
    public IPEndPoint SourceAddress { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool HasChangedAddress { get; set; }
}
```

## 15.2 부하 테스트와 스트레스 테스트

### 15.2.1 동시 연결 부하 테스트

```csharp
// LoadTestManager.cs
public class P2PLoadTestManager
{
    private readonly List<P2PTestClient> _clients = new();
    private readonly ConcurrentDictionary<string, ConnectionMetrics> _connectionMetrics = new();
    
    public class ConnectionMetrics
    {
        public DateTime StartTime { get; set; }
        public DateTime? ConnectedTime { get; set; }
        public long PacketsSent { get; set; }
        public long PacketsReceived { get; set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public double AverageLatency { get; set; }
        public double PacketLoss { get; set; }
        public List<double> LatencyHistory { get; set; } = new();
    }
    
    // 동시 연결 부하 테스트
    public async Task<LoadTestResult> RunConcurrentConnectionTest(
        int clientCount, 
        TimeSpan testDuration,
        int packetsPerSecond = 60)
    {
        var loadTestResult = new LoadTestResult
        {
            StartTime = DateTime.UtcNow,
            ClientCount = clientCount,
            TestDuration = testDuration
        };
        
        // 클라이언트 생성 및 초기화
        await CreateTestClients(clientCount);
        
        // 연결 단계별 측정
        await MeasureConnectionPhase(loadTestResult);
        
        // 데이터 전송 부하 테스트
        await MeasureDataTransferPhase(loadTestResult, testDuration, packetsPerSecond);
        
        // 결과 분석
        loadTestResult.EndTime = DateTime.UtcNow;
        AnalyzeLoadTestResults(loadTestResult);
        
        return loadTestResult;
    }
    
    private async Task CreateTestClients(int count)
    {
        var tasks = new List<Task>();
        
        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var client = new P2PTestClient($"Client_{i}");
                await client.Initialize();
                _clients.Add(client);
            }));
        }
        
        await Task.WhenAll(tasks);
    }
    
    private async Task MeasureConnectionPhase(LoadTestResult result)
    {
        var connectionTasks = new List<Task>();
        var connectionStopwatch = Stopwatch.StartNew();
        
        // 클라이언트들을 페어링하여 연결 시도
        for (int i = 0; i < _clients.Count; i += 2)
        {
            if (i + 1 < _clients.Count)
            {
                var client1 = _clients[i];
                var client2 = _clients[i + 1];
                
                connectionTasks.Add(Task.Run(async () =>
                {
                    var metrics = new ConnectionMetrics
                    {
                        StartTime = DateTime.UtcNow
                    };
                    
                    var connectionId = $"{client1.Id}-{client2.Id}";
                    _connectionMetrics[connectionId] = metrics;
                    
                    try
                    {
                        await EstablishP2PConnection(client1, client2);
                        metrics.ConnectedTime = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Connection failed for {connectionId}: {ex.Message}");
                    }
                }));
            }
        }
        
        await Task.WhenAll(connectionTasks);
        connectionStopwatch.Stop();
        
        result.ConnectionPhaseTime = connectionStopwatch.Elapsed;
        result.SuccessfulConnections = _connectionMetrics.Values.Count(m => m.ConnectedTime.HasValue);
    }
    
    private async Task MeasureDataTransferPhase(
        LoadTestResult result, 
        TimeSpan duration, 
        int packetsPerSecond)
    {
        var transferStopwatch = Stopwatch.StartNew();
        var cancellationToken = new CancellationTokenSource(duration).Token;
        
        var transferTasks = new List<Task>();
        
        foreach (var client in _clients.Where(c => c.IsConnected))
        {
            transferTasks.Add(Task.Run(async () =>
            {
                await SimulateGameTraffic(client, packetsPerSecond, cancellationToken);
            }, cancellationToken));
        }
        
        try
        {
            await Task.WhenAll(transferTasks);
        }
        catch (OperationCanceledException)
        {
            // 테스트 시간 종료
        }
        
        transferStopwatch.Stop();
        result.DataTransferPhaseTime = transferStopwatch.Elapsed;
    }
    
    private async Task SimulateGameTraffic(
        P2PTestClient client, 
        int packetsPerSecond, 
        CancellationToken cancellationToken)
    {
        var packetInterval = TimeSpan.FromMilliseconds(1000.0 / packetsPerSecond);
        var lastSendTime = DateTime.UtcNow;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // 게임 패킷 시뮬레이션 (플레이어 위치, 액션 등)
                var gamePacket = CreateGamePacket(client.Id);
                var sendTime = DateTime.UtcNow;
                
                await client.SendGamePacket(gamePacket);
                
                // 메트릭 업데이트
                var connectionId = GetConnectionId(client);
                if (_connectionMetrics.TryGetValue(connectionId, out var metrics))
                {
                    metrics.PacketsSent++;
                    metrics.BytesSent += gamePacket.Length;
                }
                
                // 다음 패킷까지 대기
                var nextSendTime = lastSendTime.Add(packetInterval);
                var delay = nextSendTime - DateTime.UtcNow;
                
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
                
                lastSendTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending packet for {client.Id}: {ex.Message}");
            }
        }
    }
}

public class LoadTestResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int ClientCount { get; set; }
    public TimeSpan TestDuration { get; set; }
    public TimeSpan ConnectionPhaseTime { get; set; }
    public TimeSpan DataTransferPhaseTime { get; set; }
    public int SuccessfulConnections { get; set; }
    public double ConnectionSuccessRate => ClientCount > 0 ? (double)SuccessfulConnections / (ClientCount / 2) * 100 : 0;
    public long TotalPacketsSent { get; set; }
    public long TotalPacketsReceived { get; set; }
    public double AverageLatency { get; set; }
    public double PacketLossRate { get; set; }
    public long PeakMemoryUsage { get; set; }
    public double PeakCpuUsage { get; set; }
}
```

### 15.2.2 스트레스 테스트 구현

```csharp
// StressTestRunner.cs
public class P2PStressTestRunner
{
    private readonly PerformanceCounter _cpuCounter = new("Processor", "% Processor Time", "_Total");
    private readonly PerformanceCounter _memoryCounter = new("Memory", "Available MBytes");
    
    public async Task<StressTestResult> RunStressTest(StressTestConfiguration config)
    {
        var result = new StressTestResult
        {
            Configuration = config,
            StartTime = DateTime.UtcNow
        };
        
        // 1. 점진적 부하 증가 테스트
        await RunGradualLoadIncrease(result, config);
        
        // 2. 갑작스런 부하 스파이크 테스트
        await RunSuddenLoadSpike(result, config);
        
        // 3. 장시간 안정성 테스트
        await RunLongTermStabilityTest(result, config);
        
        // 4. 극한 조건 테스트
        await RunExtremeConditionTest(result, config);
        
        result.EndTime = DateTime.UtcNow;
        return result;
    }
    
    private async Task RunGradualLoadIncrease(StressTestResult result, StressTestConfiguration config)
    {
        var phases = new List<LoadPhase>();
        var currentLoad = config.MinClients;
        
        while (currentLoad <= config.MaxClients)
        {
            var phase = new LoadPhase
            {
                ClientCount = currentLoad,
                StartTime = DateTime.UtcNow
            };
            
            Console.WriteLine($"Starting load phase: {currentLoad} clients");
            
            // 시스템 리소스 모니터링 시작
            var resourceMonitor = StartResourceMonitoring();
            
            try
            {
                // 해당 로드에서 테스트 실행
                var loadTestManager = new P2PLoadTestManager();
                var loadResult = await loadTestManager.RunConcurrentConnectionTest(
                    currentLoad, 
                    config.PhaseDuration,
                    config.PacketsPerSecond
                );
                
                phase.LoadTestResult = loadResult;
                phase.Success = loadResult.ConnectionSuccessRate > config.MinSuccessRate;
                
                // 리소스 사용량 기록
                phase.PeakCpuUsage = resourceMonitor.PeakCpuUsage;
                phase.PeakMemoryUsage = resourceMonitor.PeakMemoryUsage;
                phase.AverageResponseTime = loadResult.AverageLatency;
                
                if (!phase.Success)
                {
                    Console.WriteLine($"Load phase failed at {currentLoad} clients");
                    result.MaxSustainableLoad = Math.Max(0, currentLoad - config.LoadIncrement);
                    break;
                }
            }
            catch (Exception ex)
            {
                phase.Success = false;
                phase.ErrorMessage = ex.Message;
                Console.WriteLine($"Load phase crashed at {currentLoad} clients: {ex.Message}");
            }
            finally
            {
                resourceMonitor.Stop();
                phase.EndTime = DateTime.UtcNow;
                phases.Add(phase);
            }
            
            currentLoad += config.LoadIncrement;
            
            // 다음 단계 전 안정화 대기
            await Task.Delay(config.StabilizationDelay);
        }
        
        result.LoadPhases = phases;
        result.MaxSustainableLoad = currentLoad - config.LoadIncrement;
    }
    
    private async Task RunSuddenLoadSpike(StressTestResult result, StressTestConfiguration config)
    {
        Console.WriteLine("Starting sudden load spike test");
        
        var spikeResult = new LoadSpikeResult
        {
            StartTime = DateTime.UtcNow,
            BaselineLoad = config.MinClients,
            SpikeLoad = config.MaxClients
        };
        
        try
        {
            // 1. 기준 부하로 안정된 상태 구성
            var baselineManager = new P2PLoadTestManager();
            var baselineTask = baselineManager.RunConcurrentConnectionTest(
                config.MinClients,
                TimeSpan.FromMinutes(1),
                config.PacketsPerSecond
            );
            
            await Task.Delay(TimeSpan.FromSeconds(30)); // 안정화 대기
            
            // 2. 갑작스런 부하 스파이크 발생
            var spikeManager = new P2PLoadTestManager();
            var spikeTask = spikeManager.RunConcurrentConnectionTest(
                config.MaxClients - config.MinClients,
                config.SpikeTestDuration,
                config.PacketsPerSecond
            );
            
            var results = await Task.WhenAll(baselineTask, spikeTask);
            
            spikeResult.BaselineResult = results[0];
            spikeResult.SpikeResult = results[1];
            spikeResult.Success = results[1].ConnectionSuccessRate > config.MinSuccessRate;
        }
        catch (Exception ex)
        {
            spikeResult.Success = false;
            spikeResult.ErrorMessage = ex.Message;
        }
        
        spikeResult.EndTime = DateTime.UtcNow;
        result.LoadSpikeResult = spikeResult;
    }
    
    private ResourceMonitor StartResourceMonitoring()
    {
        return new ResourceMonitor(_cpuCounter, _memoryCounter);
    }
}

public class StressTestConfiguration
{
    public int MinClients { get; set; } = 10;
    public int MaxClients { get; set; } = 1000;
    public int LoadIncrement { get; set; } = 50;
    public TimeSpan PhaseD duration { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan StabilizationDelay { get; set; } = TimeSpan.FromSeconds(30);
    public int PacketsPerSecond { get; set; } = 60;
    public double MinSuccessRate { get; set; } = 95.0;
    public TimeSpan SpikeTestDuration { get; set; } = TimeSpan.FromMinutes(5);
}

public class ResourceMonitor
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    private readonly Timer _monitoringTimer;
    public double PeakCpuUsage { get; private set; }
    public long PeakMemoryUsage { get; private set; }
    
    public ResourceMonitor(PerformanceCounter cpuCounter, PerformanceCounter memoryCounter)
    {
        _cpuCounter = cpuCounter;
        _memoryCounter = memoryCounter;
        
        _monitoringTimer = new Timer(MonitorResources, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    private void MonitorResources(object state)
    {
        try
        {
            var currentCpu = _cpuCounter.NextValue();
            var currentMemory = GC.GetTotalMemory(false);
            
            PeakCpuUsage = Math.Max(PeakCpuUsage, currentCpu);
            PeakMemoryUsage = Math.Max(PeakMemoryUsage, currentMemory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Resource monitoring error: {ex.Message}");
        }
    }
    
    public void Stop()
    {
        _monitoringTimer?.Dispose();
    }
}
```

## 15.3 네트워크 시뮬레이션과 테스트 환경

### 15.3.1 네트워크 조건 시뮬레이션

```csharp
// NetworkSimulator.cs
public class NetworkConditionSimulator
{
    private readonly Random _random = new();
    
    public class NetworkCondition
    {
        public int LatencyMs { get; set; }
        public int JitterMs { get; set; }
        public double PacketLossRate { get; set; } // 0.0 ~ 1.0
        public int BandwidthKbps { get; set; }
        public bool IsUnstable { get; set; }
    }
    
    // 다양한 네트워크 환경 프리셋
    public static readonly Dictionary<string, NetworkCondition> NetworkPresets = new()
    {
        ["Excellent"] = new() { LatencyMs = 10, JitterMs = 2, PacketLossRate = 0.001, BandwidthKbps = 100000 },
        ["Good"] = new() { LatencyMs = 30, JitterMs = 5, PacketLossRate = 0.01, BandwidthKbps = 50000 },
        ["Average"] = new() { LatencyMs = 80, JitterMs = 15, PacketLossRate = 0.02, BandwidthKbps = 10000 },
        ["Poor"] = new() { LatencyMs = 150, JitterMs = 30, PacketLossRate = 0.05, BandwidthKbps = 5000 },
        ["Mobile3G"] = new() { LatencyMs = 200, JitterMs = 50, PacketLossRate = 0.03, BandwidthKbps = 1000 },
        ["Mobile4G"] = new() { LatencyMs = 50, JitterMs = 20, PacketLossRate = 0.015, BandwidthKbps = 20000 },
        ["Satellite"] = new() { LatencyMs = 600, JitterMs = 100, PacketLossRate = 0.02, BandwidthKbps = 15000 },
        ["Unstable"] = new() { LatencyMs = 100, JitterMs = 200, PacketLossRate = 0.08, BandwidthKbps = 8000, IsUnstable = true }
    };
    
    public async Task<bool> SimulatePacketTransmission(
        byte[] packet, 
        NetworkCondition condition, 
        Func<byte[], Task> deliveryCallback)
    {
        // 패킷 손실 시뮬레이션
        if (_random.NextDouble() < condition.PacketLossRate)
        {
            return false; // 패킷 손실
        }
        
        // 대역폭 제한 시뮬레이션
        var transmissionTime = CalculateTransmissionTime(packet.Length, condition.BandwidthKbps);
        
        // 레이턴시 및 지터 시뮬레이션
        var actualLatency = condition.LatencyMs;
        if (condition.JitterMs > 0)
        {
            var jitterVariation = _random.Next(-condition.JitterMs, condition.JitterMs + 1);
            actualLatency = Math.Max(0, actualLatency + jitterVariation);
        }
        
        // 불안정한 네트워크 조건 시뮬레이션
        if (condition.IsUnstable)
        {
            actualLatency = SimulateNetworkInstability(actualLatency);
        }
        
        // 전송 지연 후 패킷 전달
        await Task.Delay(actualLatency + transmissionTime);
        await deliveryCallback(packet);
        
        return true;
    }
    
    private TimeSpan CalculateTransmissionTime(int packetSize, int bandwidthKbps)
    {
        // 패킷 크기(바이트)를 비트로 변환하고 대역폭으로 나누어 전송 시간 계산
        var packetBits = packetSize * 8;
        var bandwidthBps = bandwidthKbps * 1000;
        var transmissionTimeMs = (double)packetBits / bandwidthBps * 1000;
        
        return TimeSpan.FromMilliseconds(transmissionTimeMs);
    }
    
    private int SimulateNetworkInstability(int baseLatency)
    {
        // 불안정한 네트워크에서 간헐적 지연 스파이크 시뮬레이션
        if (_random.NextDouble() < 0.1) // 10% 확률로 지연 스파이크
        {
            return baseLatency + _random.Next(200, 1000);
        }
        
        return baseLatency;
    }
}

// 다양한 NAT 환경 시뮬레이션
public class NATEnvironmentSimulator
{
    public class NATSimulationConfig
    {
        public NATType NATType { get; set; }
        public int MappingLifetimeSeconds { get; set; } = 120;
        public bool AllowHairpinning { get; set; } = false;
        public int MaxSimultaneousConnections { get; set; } = 100;
        public bool RandomizeExternalPorts { get; set; } = true;
    }
    
    private readonly Dictionary<NATType, NATSimulationConfig> _natConfigs = new()
    {
        [NATType.FullCone] = new() 
        { 
            NATType = NATType.FullCone, 
            AllowHairpinning = true,
            RandomizeExternalPorts = false 
        },
        [NATType.RestrictedCone] = new() 
        { 
            NATType = NATType.RestrictedCone,
            RandomizeExternalPorts = false 
        },
        [NATType.PortRestrictedCone] = new() 
        { 
            NATType = NATType.PortRestrictedCone,
            RandomizeExternalPorts = false 
        },
        [NATType.Symmetric] = new() 
        { 
            NATType = NATType.Symmetric,
            RandomizeExternalPorts = true,
            MaxSimultaneousConnections = 50 
        }
    };
    
    public async Task<HolePunchingTestResult> SimulateHolePunching(
        NATType client1NAT,
        NATType client2NAT,
        NetworkCondition networkCondition)
    {
        var result = new HolePunchingTestResult
        {
            Client1NATType = client1NAT,
            Client2NATType = client2NAT,
            NetworkCondition = networkCondition,
            StartTime = DateTime.UtcNow
        };
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // 1. STUN 단계 시뮬레이션
            var stunResult = await SimulateSTUNDiscovery(client1NAT, client2NAT, networkCondition);
            result.STUNPhaseTime = stunResult.Duration;
            result.STUNSuccess = stunResult.Success;
            
            if (!stunResult.Success)
            {
                result.Success = false;
                result.FailureReason = "STUN discovery failed";
                return result;
            }
            
            // 2. 홀펀칭 시도 시뮬레이션
            var punchingResult = await SimulateHolePunchingAttempt(
                client1NAT, 
                client2NAT, 
                networkCondition,
                stunResult.MappedEndpoints);
                
            result.HolePunchingPhaseTime = punchingResult.Duration;
            result.HolePunchingAttempts = punchingResult.Attempts;
            result.Success = punchingResult.Success;
            
            if (!punchingResult.Success)
            {
                result.FailureReason = punchingResult.FailureReason;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FailureReason = ex.Message;
        }
        
        stopwatch.Stop();
        result.TotalTime = stopwatch.Elapsed;
        result.EndTime = DateTime.UtcNow;
        
        return result;
    }
    
    private async Task<STUNSimulationResult> SimulateSTUNDiscovery(
        NATType client1NAT, 
        NATType client2NAT, 
        NetworkCondition networkCondition)
    {
        var simulator = new NetworkConditionSimulator();
        var result = new STUNSimulationResult { StartTime = DateTime.UtcNow };
        
        var stunStopwatch = Stopwatch.StartNew();
        
        // STUN 요청/응답 시뮬레이션
        var stunRequest = new byte[20]; // 간단한 STUN 요청
        
        var client1Success = await simulator.SimulatePacketTransmission(
            stunRequest,
            networkCondition,
            async (packet) => await Task.CompletedTask
        );
        
        var client2Success = await simulator.SimulatePacketTransmission(
            stunRequest,
            networkCondition,
            async (packet) => await Task.CompletedTask
        );
        
        stunStopwatch.Stop();
        
        result.Success = client1Success && client2Success;
        result.Duration = stunStopwatch.Elapsed;
        
        if (result.Success)
        {
            result.MappedEndpoints = new Dictionary<string, IPEndPoint>
            {
                ["Client1"] = GenerateMappedEndpoint(client1NAT),
                ["Client2"] = GenerateMappedEndpoint(client2NAT)
            };
        }
        
        return result;
    }
    
    private IPEndPoint GenerateMappedEndpoint(NATType natType)
    {
        var random = new Random();
        var basePort = 10000 + random.Next(20000);
        
        // NAT 타입에 따른 포트 매핑 동작 시뮬레이션
        var port = natType switch
        {
            NATType.FullCone or NATType.RestrictedCone or NATType.PortRestrictedCone => basePort,
            NATType.Symmetric => basePort + random.Next(100), // 대상에 따라 다른 포트
            _ => basePort
        };
        
        return new IPEndPoint(IPAddress.Parse("203.0.113.1"), port);
    }
}

public class HolePunchingTestResult
{
    public NATType Client1NATType { get; set; }
    public NATType Client2NATType { get; set; }
    public NetworkCondition NetworkCondition { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalTime { get; set; }
    public bool Success { get; set; }
    public string FailureReason { get; set; }
    public TimeSpan STUNPhaseTime { get; set; }
    public TimeSpan HolePunchingPhaseTime { get; set; }
    public bool STUNSuccess { get; set; }
    public int HolePunchingAttempts { get; set; }
}
```

### 15.3.2 자동화된 테스트 환경

```csharp
// AutomatedTestEnvironment.cs
public class AutomatedP2PTestEnvironment
{
    private readonly List<VirtualTestClient> _virtualClients = new();
    private readonly TestOrchestrator _orchestrator = new();
    
    public async Task<ComprehensiveTestReport> RunComprehensiveTestSuite()
    {
        var report = new ComprehensiveTestReport
        {
            StartTime = DateTime.UtcNow,
            TestEnvironment = Environment.MachineName,
            FrameworkVersion = Environment.Version.ToString()
        };
        
        // 1. 기본 기능 테스트
        Console.WriteLine("Running basic functionality tests...");
        report.BasicFunctionalityResults = await RunBasicFunctionalityTests();
        
        // 2. NAT 조합 매트릭스 테스트
        Console.WriteLine("Running NAT combination matrix tests...");
        report.NATMatrixResults = await RunNATCombinationTests();
        
        // 3. 네트워크 조건별 테스트
        Console.WriteLine("Running network condition tests...");
        report.NetworkConditionResults = await RunNetworkConditionTests();
        
        // 4. 확장성 테스트
        Console.WriteLine("Running scalability tests...");
        report.ScalabilityResults = await RunScalabilityTests();
        
        // 5. 안정성 테스트
        Console.WriteLine("Running reliability tests...");
        report.ReliabilityResults = await RunReliabilityTests();
        
        report.EndTime = DateTime.UtcNow;
        report.TotalTestDuration = report.EndTime - report.StartTime;
        
        // 테스트 보고서 생성
        await GenerateTestReport(report);
        
        return report;
    }
    
    private async Task<List<BasicFunctionalityResult>> RunBasicFunctionalityTests()
    {
        var tests = new List<(string Name, Func<Task<BasicFunctionalityResult>> Test)>
        {
            ("STUN Discovery", TestSTUNDiscovery),
            ("Simple Hole Punching", TestSimpleHolePunching),
            ("Bidirectional Communication", TestBidirectionalCommunication),
            ("Connection Keepalive", TestConnectionKeepalive),
            ("Graceful Disconnection", TestGracefulDisconnection)
        };
        
        var results = new List<BasicFunctionalityResult>();
        
        foreach (var (name, test) in tests)
        {
            Console.WriteLine($"  Running: {name}");
            try
            {
                var result = await test();
                result.TestName = name;
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(new BasicFunctionalityResult
                {
                    TestName = name,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
        
        return results;
    }
    
    private async Task<List<NATCombinationResult>> RunNATCombinationTests()
    {
        var natTypes = Enum.GetValues<NATType>().Where(n => n != NATType.Unknown).ToArray();
        var results = new List<NATCombinationResult>();
        
        for (int i = 0; i < natTypes.Length; i++)
        {
            for (int j = i; j < natTypes.Length; j++)
            {
                var nat1 = natTypes[i];
                var nat2 = natTypes[j];
                
                Console.WriteLine($"  Testing NAT combination: {nat1} <-> {nat2}");
                
                var simulator = new NATEnvironmentSimulator();
                var networkCondition = NetworkConditionSimulator.NetworkPresets["Good"];
                
                var result = await simulator.SimulateHolePunching(nat1, nat2, networkCondition);
                
                results.Add(new NATCombinationResult
                {
                    NAT1Type = nat1,
                    NAT2Type = nat2,
                    HolePunchingResult = result,
                    ExpectedSuccess = DetermineExpectedSuccess(nat1, nat2)
                });
            }
        }
        
        return results;
    }
    
    private bool DetermineExpectedSuccess(NATType nat1, NATType nat2)
    {
        // NAT 조합별 홀펀칭 성공 가능성 예측
        return (nat1, nat2) switch
        {
            (NATType.Open, _) or (_, NATType.Open) => true,
            (NATType.FullCone, NATType.FullCone) => true,
            (NATType.FullCone, NATType.RestrictedCone) => true,
            (NATType.FullCone, NATType.PortRestrictedCone) => true,
            (NATType.RestrictedCone, NATType.RestrictedCone) => true,
            (NATType.RestrictedCone, NATType.PortRestrictedCone) => true,
            (NATType.PortRestrictedCone, NATType.PortRestrictedCone) => true,
            (NATType.Symmetric, NATType.Symmetric) => false,
            _ when nat1 == NATType.Symmetric || nat2 == NATType.Symmetric => false,
            _ => true
        };
    }
    
    private async Task GenerateTestReport(ComprehensiveTestReport report)
    {
        var reportBuilder = new StringBuilder();
        
        reportBuilder.AppendLine("# P2P 홀펀칭 종합 테스트 보고서");
        reportBuilder.AppendLine($"테스트 일시: {report.StartTime:yyyy-MM-dd HH:mm:ss} ~ {report.EndTime:yyyy-MM-dd HH:mm:ss}");
        reportBuilder.AppendLine($"총 소요 시간: {report.TotalTestDuration}");
        reportBuilder.AppendLine($"테스트 환경: {report.TestEnvironment}");
        reportBuilder.AppendLine();
        
        // 기본 기능 테스트 결과
        reportBuilder.AppendLine("## 기본 기능 테스트 결과");
        var basicSuccess = report.BasicFunctionalityResults.Count(r => r.Success);
        var basicTotal = report.BasicFunctionalityResults.Count;
        reportBuilder.AppendLine($"성공률: {basicSuccess}/{basicTotal} ({(double)basicSuccess/basicTotal*100:F1}%)");
        reportBuilder.AppendLine();
        
        foreach (var result in report.BasicFunctionalityResults)
        {
            reportBuilder.AppendLine($"- {result.TestName}: {(result.Success ? "✓" : "✗")}");
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                reportBuilder.AppendLine($"  오류: {result.ErrorMessage}");
            }
        }
        reportBuilder.AppendLine();
        
        // NAT 조합 테스트 결과
        reportBuilder.AppendLine("## NAT 조합 테스트 결과");
        var natSuccess = report.NATMatrixResults.Count(r => r.HolePunchingResult.Success);
        var natTotal = report.NATMatrixResults.Count;
        reportBuilder.AppendLine($"전체 성공률: {natSuccess}/{natTotal} ({(double)natSuccess/natTotal*100:F1}%)");
        reportBuilder.AppendLine();
        
        // NAT 조합별 결과 매트릭스
        var natTypes = Enum.GetValues<NATType>().Where(n => n != NATType.Unknown).ToArray();
        reportBuilder.AppendLine("### NAT 조합 매트릭스");
        reportBuilder.Append("| NAT Type |");
        foreach (var nat in natTypes)
        {
            reportBuilder.Append($" {nat} |");
        }
        reportBuilder.AppendLine();
        
        reportBuilder.Append("|----------|");
        foreach (var nat in natTypes)
        {
            reportBuilder.Append("------|");
        }
        reportBuilder.AppendLine();
        
        foreach (var nat1 in natTypes)
        {
            reportBuilder.Append($"| {nat1} |");
            foreach (var nat2 in natTypes)
            {
                var result = report.NATMatrixResults
                    .FirstOrDefault(r => (r.NAT1Type == nat1 && r.NAT2Type == nat2) ||
                                        (r.NAT1Type == nat2 && r.NAT2Type == nat1));
                
                var symbol = result?.HolePunchingResult.Success == true ? "✓" : "✗";
                reportBuilder.Append($" {symbol} |");
            }
            reportBuilder.AppendLine();
        }
        
        // 파일로 저장
        var reportPath = $"P2P_Test_Report_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        await File.WriteAllTextAsync(reportPath, reportBuilder.ToString());
        
        Console.WriteLine($"테스트 보고서가 생성되었습니다: {reportPath}");
    }
}

public class ComprehensiveTestReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalTestDuration { get; set; }
    public string TestEnvironment { get; set; }
    public string FrameworkVersion { get; set; }
    public List<BasicFunctionalityResult> BasicFunctionalityResults { get; set; } = new();
    public List<NATCombinationResult> NATMatrixResults { get; set; } = new();
    public List<NetworkConditionResult> NetworkConditionResults { get; set; } = new();
    public List<ScalabilityResult> ScalabilityResults { get; set; } = new();
    public List<ReliabilityResult> ReliabilityResults { get; set; } = new();
}
```

## 15.4 성능 지표 측정과 분석

### 15.4.1 핵심 성능 지표 정의

```csharp
// PerformanceMetrics.cs
public class P2PPerformanceMetrics
{
    // 연결 관련 지표
    public class ConnectionMetrics
    {
        public TimeSpan HolePunchingTime { get; set; }
        public int HolePunchingAttempts { get; set; }
        public bool ConnectionSuccess { get; set; }
        public TimeSpan TimeToFirstPacket { get; set; }
        public string FailureReason { get; set; }
    }
    
    // 통신 품질 지표
    public class CommunicationQualityMetrics
    {
        public double AverageLatency { get; set; }
        public double MedianLatency { get; set; }
        public double LatencyStandardDeviation { get; set; }
        public double PacketLossRate { get; set; }
        public double Jitter { get; set; }
        public long ThroughputBytesPerSecond { get; set; }
        public int PacketsPerSecond { get; set; }
    }
    
    // 시스템 리소스 지표
    public class ResourceUtilizationMetrics
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long NetworkBytesSent { get; set; }
        public long NetworkBytesReceived { get; set; }
        public int ActiveConnections { get; set; }
        public int SocketsInUse { get; set; }
    }
    
    // 안정성 지표
    public class ReliabilityMetrics
    {
        public TimeSpan ConnectionUptime { get; set; }
        public int ReconnectionAttempts { get; set; }
        public int UnexpectedDisconnections { get; set; }
        public double ConnectionStabilityScore { get; set; }
        public TimeSpan MeanTimeBetweenFailures { get; set; }
        public TimeSpan MeanTimeToRecovery { get; set; }
    }
}

// 실시간 성능 모니터링
public class RealTimePerformanceMonitor
{
    private readonly Timer _monitoringTimer;
    private readonly ConcurrentQueue<PerformanceSample> _samples = new();
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _networkSentCounter;
    private readonly PerformanceCounter _networkReceivedCounter;
    
    public event EventHandler<PerformanceAlert> PerformanceAlertRaised;
    
    public class PerformanceSample
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public double AverageLatency { get; set; }
        public double PacketLossRate { get; set; }
        public int ActiveConnections { get; set; }
        public long NetworkThroughput { get; set; }
    }
    
    public class PerformanceAlert
    {
        public DateTime Timestamp { get; set; }
        public AlertLevel Level { get; set; }
        public string MetricName { get; set; }
        public double CurrentValue { get; set; }
        public double ThresholdValue { get; set; }
        public string Message { get; set; }
    }
    
    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }
    
    public RealTimePerformanceMonitor()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", GetNetworkInterfaceName());
        _networkReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", GetNetworkInterfaceName());
        
        _monitoringTimer = new Timer(CollectPerformanceSample, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    private void CollectPerformanceSample(object state)
    {
        try
        {
            var sample = new PerformanceSample
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = _cpuCounter.NextValue(),
                MemoryUsage = GC.GetTotalMemory(false),
                NetworkThroughput = (long)(_networkSentCounter.NextValue() + _networkReceivedCounter.NextValue())
            };
            
            // 최근 통계 계산
            CalculateRecentStatistics(sample);
            
            _samples.Enqueue(sample);
            
            // 오래된 샘플 제거 (최근 1시간만 보관)
            while (_samples.TryPeek(out var oldSample) && 
                   DateTime.UtcNow - oldSample.Timestamp > TimeSpan.FromHours(1))
            {
                _samples.TryDequeue(out _);
            }
            
            // 성능 임계값 확인
            CheckPerformanceThresholds(sample);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Performance monitoring error: {ex.Message}");
        }
    }
    
    private void CalculateRecentStatistics(PerformanceSample sample)
    {
        // 최근 P2P 연결 통계 계산
        var recentConnections = GetRecentConnectionMetrics();
        
        if (recentConnections.Any())
        {
            sample.AverageLatency = recentConnections.Average(c => c.AverageLatency);
            sample.PacketLossRate = recentConnections.Average(c => c.PacketLossRate);
            sample.ActiveConnections = recentConnections.Count(c => c.IsActive);
        }
    }
    
    private void CheckPerformanceThresholds(PerformanceSample sample)
    {
        // CPU 사용률 확인
        if (sample.CpuUsage > 80)
        {
            RaiseAlert(AlertLevel.Warning, "CPU Usage", sample.CpuUsage, 80, 
                "높은 CPU 사용률이 감지되었습니다.");
        }
        else if (sample.CpuUsage > 95)
        {
            RaiseAlert(AlertLevel.Critical, "CPU Usage", sample.CpuUsage, 95, 
                "매우 높은 CPU 사용률이 감지되었습니다.");
        }
        
        // 메모리 사용량 확인
        var memoryMB = sample.MemoryUsage / (1024 * 1024);
        if (memoryMB > 1000)
        {
            RaiseAlert(AlertLevel.Warning, "Memory Usage", memoryMB, 1000, 
                "높은 메모리 사용량이 감지되었습니다.");
        }
        
        // 평균 지연시간 확인
        if (sample.AverageLatency > 200)
        {
            RaiseAlert(AlertLevel.Warning, "Average Latency", sample.AverageLatency, 200, 
                "높은 평균 지연시간이 감지되었습니다.");
        }
        
        // 패킷 손실률 확인
        if (sample.PacketLossRate > 0.05)
        {
            RaiseAlert(AlertLevel.Critical, "Packet Loss Rate", sample.PacketLossRate * 100, 5, 
                "높은 패킷 손실률이 감지되었습니다.");
        }
    }
    
    private void RaiseAlert(AlertLevel level, string metricName, double currentValue, 
        double threshold, string message)
    {
        var alert = new PerformanceAlert
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            MetricName = metricName,
            CurrentValue = currentValue,
            ThresholdValue = threshold,
            Message = message
        };
        
        PerformanceAlertRaised?.Invoke(this, alert);
    }
    
    public PerformanceReport GenerateReport(TimeSpan period)
    {
        var cutoffTime = DateTime.UtcNow - period;
        var relevantSamples = _samples.Where(s => s.Timestamp >= cutoffTime).ToList();
        
        if (!relevantSamples.Any())
            return new PerformanceReport { Message = "해당 기간의 데이터가 없습니다." };
        
        return new PerformanceReport
        {
            Period = period,
            SampleCount = relevantSamples.Count,
            AverageCpuUsage = relevantSamples.Average(s => s.CpuUsage),
            PeakCpuUsage = relevantSamples.Max(s => s.CpuUsage),
            AverageMemoryUsage = relevantSamples.Average(s => s.MemoryUsage),
            PeakMemoryUsage = relevantSamples.Max(s => s.MemoryUsage),
            AverageLatency = relevantSamples.Where(s => s.AverageLatency > 0).DefaultIfEmpty().Average(s => s.AverageLatency),
            AveragePacketLossRate = relevantSamples.Where(s => s.PacketLossRate > 0).DefaultIfEmpty().Average(s => s.PacketLossRate),
            PeakActiveConnections = relevantSamples.Max(s => s.ActiveConnections),
            TotalNetworkThroughput = relevantSamples.Sum(s => s.NetworkThroughput)
        };
    }
}

// 성능 분석 및 최적화 권장사항
public class PerformanceAnalyzer
{
    public class OptimizationRecommendation
    {
        public string Category { get; set; }
        public string Issue { get; set; }
        public string Recommendation { get; set; }
        public int Priority { get; set; } // 1=높음, 2=중간, 3=낮음
        public string ExpectedImprovement { get; set; }
    }
    
    public List<OptimizationRecommendation> AnalyzePerformance(PerformanceReport report)
    {
        var recommendations = new List<OptimizationRecommendation>();
        
        // CPU 사용률 분석
        if (report.AverageCpuUsage > 70)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "CPU 최적화",
                Issue = $"평균 CPU 사용률이 높습니다 ({report.AverageCpuUsage:F1}%)",
                Recommendation = "비동기 처리 최적화, 불필요한 계산 줄이기, 스레드 풀 튜닝 검토",
                Priority = 1,
                ExpectedImprovement = "15-25% CPU 사용률 감소"
            });
        }
        
        // 메모리 사용량 분석
        if (report.PeakMemoryUsage > 1024 * 1024 * 1024) // 1GB
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "메모리 최적화",
                Issue = $"메모리 사용량이 높습니다 ({report.PeakMemoryUsage / (1024*1024):F0}MB)",
                Recommendation = "메모리 풀링 구현, 불필요한 객체 생성 줄이기, GC 압력 감소",
                Priority = 2,
                ExpectedImprovement = "20-30% 메모리 사용량 감소"
            });
        }
        
        // 지연시간 분석
        if (report.AverageLatency > 100)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "네트워크 지연시간",
                Issue = $"평균 지연시간이 높습니다 ({report.AverageLatency:F1}ms)",
                Recommendation = "UDP 소켓 버퍼 크기 최적화, 예측 알고리즘 구현, 홀펀칭 알고리즘 개선",
                Priority = 1,
                ExpectedImprovement = "30-50% 지연시간 단축"
            });
        }
        
        // 패킷 손실률 분석
        if (report.AveragePacketLossRate > 0.02)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = "패킷 신뢰성",
                Issue = $"패킷 손실률이 높습니다 ({report.AveragePacketLossRate*100:F1}%)",
                Recommendation = "재전송 메커니즘 개선, 적응적 전송 속도 조절, FEC(Forward Error Correction) 적용",
                Priority = 1,
                ExpectedImprovement = "50-70% 패킷 손실률 감소"
            });
        }
        
        return recommendations.OrderBy(r => r.Priority).ToList();
    }
    
    public void PrintRecommendations(List<OptimizationRecommendation> recommendations)
    {
        Console.WriteLine("\n=== 성능 최적화 권장사항 ===");
        
        foreach (var rec in recommendations)
        {
            var priorityText = rec.Priority switch
            {
                1 => "높음",
                2 => "중간",
                3 => "낮음",
                _ => "미지정"
            };
            
            Console.WriteLine($"\n[우선순위: {priorityText}] {rec.Category}");
            Console.WriteLine($"문제: {rec.Issue}");
            Console.WriteLine($"권장사항: {rec.Recommendation}");
            Console.WriteLine($"예상 개선 효과: {rec.ExpectedImprovement}");
        }
        
        if (!recommendations.Any())
        {
            Console.WriteLine("현재 성능이 양호합니다. 특별한 최적화가 필요하지 않습니다.");
        }
    }
}

public class PerformanceReport
{
    public TimeSpan Period { get; set; }
    public int SampleCount { get; set; }
    public double AverageCpuUsage { get; set; }
    public double PeakCpuUsage { get; set; }
    public double AverageMemoryUsage { get; set; }
    public double PeakMemoryUsage { get; set; }
    public double AverageLatency { get; set; }
    public double AveragePacketLossRate { get; set; }
    public int PeakActiveConnections { get; set; }
    public long TotalNetworkThroughput { get; set; }
    public string Message { get; set; }
}
```

### 15.4.2 종합 성능 테스트 실행 예제

```csharp
// Program.cs - 종합 성능 테스트 실행
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("P2P 홀펀칭 성능 테스트 시작");
        
        // 1. 실시간 모니터링 시작
        var monitor = new RealTimePerformanceMonitor();
        monitor.PerformanceAlertRaised += (sender, alert) =>
        {
            Console.WriteLine($"[{alert.Level}] {alert.MetricName}: {alert.CurrentValue:F1} (임계값: {alert.ThresholdValue})");
            Console.WriteLine($"  {alert.Message}");
        };
        
        // 2. 기본 성능 테스트
        await RunBasicPerformanceTest();
        
        // 3. 부하 테스트
        await RunLoadTest();
        
        // 4. 스트레스 테스트
        await RunStressTest();
        
        // 5. 네트워크 시뮬레이션 테스트
        await RunNetworkSimulationTest();
        
        // 6. 자동화된 종합 테스트
        await RunComprehensiveTest();
        
        // 7. 성능 보고서 생성
        var report = monitor.GenerateReport(TimeSpan.FromMinutes(30));
        PrintPerformanceReport(report);
        
        // 8. 최적화 권장사항 분석
        var analyzer = new PerformanceAnalyzer();
        var recommendations = analyzer.AnalyzePerformance(report);
        analyzer.PrintRecommendations(recommendations);
        
        Console.WriteLine("\n모든 테스트가 완료되었습니다.");
        Console.ReadKey();
    }
    
    static async Task RunBasicPerformanceTest()
    {
        Console.WriteLine("\n=== 기본 성능 테스트 ===");
        
        var perfTest = new HolePunchingPerformanceTest();
        var result = await perfTest.RunBulkTest(100, TimeSpan.FromMinutes(5));
        
        Console.WriteLine($"총 테스트: {result.TotalTests}");
        Console.WriteLine($"성공률: {result.SuccessRate:F1}%");
        Console.WriteLine($"평균 연결 시간: {result.AverageConnectionTime.TotalMilliseconds:F0}ms");
        Console.WriteLine($"최대 연결 시간: {result.MaxConnectionTime.TotalMilliseconds:F0}ms");
    }
    
    static async Task RunLoadTest()
    {
        Console.WriteLine("\n=== 부하 테스트 ===");
        
        var loadTestManager = new P2PLoadTestManager();
        var loadResult = await loadTestManager.RunConcurrentConnectionTest(
            clientCount: 500,
            testDuration: TimeSpan.FromMinutes(10),
            packetsPerSecond: 60
        );
        
        Console.WriteLine($"동시 클라이언트: {loadResult.ClientCount}");
        Console.WriteLine($"연결 성공률: {loadResult.ConnectionSuccessRate:F1}%");
        Console.WriteLine($"총 패킷 전송: {loadResult.TotalPacketsSent:N0}");
        Console.WriteLine($"패킷 손실률: {loadResult.PacketLossRate:F3}%");
        Console.WriteLine($"평균 지연시간: {loadResult.AverageLatency:F1}ms");
    }
    
    static async Task RunStressTest()
    {
        Console.WriteLine("\n=== 스트레스 테스트 ===");
        
        var stressConfig = new StressTestConfiguration
        {
            MinClients = 100,
            MaxClients = 2000,
            LoadIncrement = 100,
            PhaseD duration = TimeSpan.FromMinutes(3),
            PacketsPerSecond = 120,
            MinSuccessRate = 90.0
        };
        
        var stressRunner = new P2PStressTestRunner();
        var stressResult = await stressRunner.RunStressTest(stressConfig);
        
        Console.WriteLine($"최대 지원 부하: {stressResult.MaxSustainableLoad} 클라이언트");
        Console.WriteLine($"부하 단계 수: {stressResult.LoadPhases?.Count ?? 0}");
        
        if (stressResult.LoadSpikeResult != null)
        {
            Console.WriteLine($"부하 스파이크 테스트: {(stressResult.LoadSpikeResult.Success ? "성공" : "실패")}");
        }
    }
    
    static async Task RunNetworkSimulationTest()
    {
        Console.WriteLine("\n=== 네트워크 시뮬레이션 테스트 ===");
        
        var simulator = new NATEnvironmentSimulator();
        var natTypes = new[] { NATType.FullCone, NATType.RestrictedCone, NATType.PortRestrictedCone, NATType.Symmetric };
        var networkConditions = new[] { "Excellent", "Good", "Average", "Poor", "Mobile4G" };
        
        foreach (var condition in networkConditions)
        {
            Console.WriteLine($"\n네트워크 조건: {condition}");
            var networkCondition = NetworkConditionSimulator.NetworkPresets[condition];
            
            var successCount = 0;
            var totalTests = 0;
            var totalTime = TimeSpan.Zero;
            
            foreach (var nat1 in natTypes)
            {
                foreach (var nat2 in natTypes)
                {
                    var result = await simulator.SimulateHolePunching(nat1, nat2, networkCondition);
                    totalTests++;
                    
                    if (result.Success)
                    {
                        successCount++;
                        totalTime = totalTime.Add(result.TotalTime);
                    }
                }
            }
            
            Console.WriteLine($"  성공률: {successCount}/{totalTests} ({(double)successCount/totalTests*100:F1}%)");
            if (successCount > 0)
            {
                Console.WriteLine($"  평균 연결 시간: {totalTime.TotalMilliseconds/successCount:F0}ms");
            }
        }
    }
    
    static async Task RunComprehensiveTest()
    {
        Console.WriteLine("\n=== 자동화된 종합 테스트 ===");
        
        var testEnvironment = new AutomatedP2PTestEnvironment();
        var comprehensiveReport = await testEnvironment.RunComprehensiveTestSuite();
        
        Console.WriteLine($"테스트 소요 시간: {comprehensiveReport.TotalTestDuration}");
        Console.WriteLine($"기본 기능 테스트: {comprehensiveReport.BasicFunctionalityResults?.Count(r => r.Success) ?? 0}/{comprehensiveReport.BasicFunctionalityResults?.Count ?? 0}");
        Console.WriteLine($"NAT 조합 테스트: {comprehensiveReport.NATMatrixResults?.Count(r => r.HolePunchingResult.Success) ?? 0}/{comprehensiveReport.NATMatrixResults?.Count ?? 0}");
    }
    
    static void PrintPerformanceReport(PerformanceReport report)
    {
        Console.WriteLine("\n=== 성능 보고서 ===");
        Console.WriteLine($"분석 기간: {report.Period}");
        Console.WriteLine($"샘플 수: {report.SampleCount:N0}");
        Console.WriteLine($"평균 CPU 사용률: {report.AverageCpuUsage:F1}%");
        Console.WriteLine($"최대 CPU 사용률: {report.PeakCpuUsage:F1}%");
        Console.WriteLine($"평균 메모리 사용량: {report.AverageMemoryUsage/(1024*1024):F0}MB");
        Console.WriteLine($"최대 메모리 사용량: {report.PeakMemoryUsage/(1024*1024):F0}MB");
        Console.WriteLine($"평균 지연시간: {report.AverageLatency:F1}ms");
        Console.WriteLine($"평균 패킷 손실률: {report.AveragePacketLossRate*100:F2}%");
        Console.WriteLine($"최대 동시 연결: {report.PeakActiveConnections:N0}");
        Console.WriteLine($"총 네트워크 처리량: {report.TotalNetworkThroughput/(1024*1024):F0}MB");
    }
}
```

이 15장에서는 P2P 홀펀칭 시스템의 성능 테스트와 최적화에 대한 포괄적인 내용을 다뤘다. 체계적인 테스트 방법론, 부하 및 스트레스 테스트, 네트워크 시뮬레이션, 그리고 성능 지표 측정과 분석을 통해 안정적이고 고성능의 P2P 시스템을 구축할 수 있는 기반을 제공한다.  