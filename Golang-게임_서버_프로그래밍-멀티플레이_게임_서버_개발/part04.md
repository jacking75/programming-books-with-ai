# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 13. 게임 서버 설계 패턴

게임 서버를 설계할 때는 단순히 클라이언트의 요청을 처리하는 것이 아니라 동시에 접속한 수천 명의 플레이어를 관리하고, 실시간 상태 동기화를 유지하며, 높은 성능을 보장해야 한다. 이 장에서는 게임 서버의 다양한 설계 패턴과 아키텍처를 다룬다. 각 패턴은 서로 다른 게임의 특성과 요구사항에 맞도록 설계되었다.

## 13.1 게임 서버 아키텍처 개요

게임 서버의 아키텍처는 게임의 종류, 플레이어 수, 실시간성 요구도에 따라 다르게 설계된다. 기본적인 게임 서버 아키텍처는 다음과 같은 계층으로 구성된다.

```
게임 서버 아키텍처 계층

┌─────────────────────────────────────────────────┐
│  Presentation Layer (클라이언트 통신)           │
│  - 네트워크 I/O                                 │
│  - 패킷 송수신                                   │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Session Layer (세션 관리)                      │
│  - 플레이어 인증                                 │
│  - 세션 생명주기 관리                            │
│  - 연결 상태 추적                                │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Game Logic Layer (게임 로직)                   │
│  - 게임 규칙 구현                                │
│  - 상태 업데이트                                 │
│  - 검증 로직                                     │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Data Layer (데이터 관리)                       │
│  - 플레이어 상태 저장                            │
│  - 데이터베이스 통신                             │
│  - 캐싱                                         │
└─────────────────────────────────────────────────┘
```

게임 서버는 여러 핵심 컴포넌트로 구성된다:

첫째, 네트워크 계층은 TCP/UDP 연결을 관리하고 패킷을 송수신한다.

둘째, 세션 관리자는 연결된 클라이언트의 상태를 추적하고 관리한다.

셋째, 게임 로직 엔진은 게임의 규칙을 구현하고 상태를 업데이트한다.

넷째, 데이터 계층은 플레이어 정보와 게임 상태를 저장하고 검색한다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// 게임 서버의 기본 아키텍처를 보여주는 예제

// 게임 메시지 인터페이스
type Message interface {
	GetType() string
	GetPlayerID() uint32
}

// 로그인 메시지
type LoginMessage struct {
	PlayerID uint32
	Username string
}

func (lm *LoginMessage) GetType() string   { return "LOGIN" }
func (lm *LoginMessage) GetPlayerID() uint32 { return lm.PlayerID }

// 게임 액션 메시지
type GameActionMessage struct {
	PlayerID uint32
	Action   string
	Data     map[string]interface{}
}

func (gam *GameActionMessage) GetType() string   { return "GAME_ACTION" }
func (gam *GameActionMessage) GetPlayerID() uint32 { return gam.PlayerID }

// 플레이어 세션
type PlayerSession struct {
	PlayerID    uint32
	Username    string
	ConnectedAt time.Time
	LastActivity time.Time
	State       map[string]interface{} // 게임 상태
	mu          sync.RWMutex
}

func NewPlayerSession(playerID uint32, username string) *PlayerSession {
	return &PlayerSession{
		PlayerID:    playerID,
		Username:    username,
		ConnectedAt: time.Now(),
		LastActivity: time.Now(),
		State:       make(map[string]interface{}),
	}
}

func (ps *PlayerSession) UpdateActivity() {
	ps.mu.Lock()
	defer ps.mu.Unlock()
	ps.LastActivity = time.Now()
}

func (ps *PlayerSession) GetState(key string) interface{} {
	ps.mu.RLock()
	defer ps.mu.RUnlock()
	return ps.State[key]
}

func (ps *PlayerSession) SetState(key string, value interface{}) {
	ps.mu.Lock()
	defer ps.mu.Unlock()
	ps.State[key] = value
}

// 세션 관리자
type SessionManager struct {
	sessions map[uint32]*PlayerSession
	mu       sync.RWMutex
}

func NewSessionManager() *SessionManager {
	return &SessionManager{
		sessions: make(map[uint32]*PlayerSession),
	}
}

func (sm *SessionManager) CreateSession(playerID uint32, username string) *PlayerSession {
	sm.mu.Lock()
	defer sm.mu.Unlock()
	
	session := NewPlayerSession(playerID, username)
	sm.sessions[playerID] = session
	
	fmt.Printf("[SessionManager] Player %d (%s) connected\n", playerID, username)
	return session
}

func (sm *SessionManager) GetSession(playerID uint32) *PlayerSession {
	sm.mu.RLock()
	defer sm.mu.RUnlock()
	return sm.sessions[playerID]
}

func (sm *SessionManager) RemoveSession(playerID uint32) {
	sm.mu.Lock()
	defer sm.mu.Unlock()
	
	if session, ok := sm.sessions[playerID]; ok {
		delete(sm.sessions, playerID)
		fmt.Printf("[SessionManager] Player %d (%s) disconnected\n", playerID, session.Username)
	}
}

func (sm *SessionManager) GetAllSessions() []*PlayerSession {
	sm.mu.RLock()
	defer sm.mu.RUnlock()
	
	sessions := make([]*PlayerSession, 0, len(sm.sessions))
	for _, session := range sm.sessions {
		sessions = append(sessions, session)
	}
	return sessions
}

// 게임 로직 처리기
type GameLogicHandler struct {
	sessionManager *SessionManager
}

func NewGameLogicHandler(sm *SessionManager) *GameLogicHandler {
	return &GameLogicHandler{sessionManager: sm}
}

// 메시지를 처리한다
func (glh *GameLogicHandler) HandleMessage(msg Message) error {
	session := glh.sessionManager.GetSession(msg.GetPlayerID())
	if session == nil {
		return fmt.Errorf("session not found for player %d", msg.GetPlayerID())
	}
	
	session.UpdateActivity()
	
	switch m := msg.(type) {
	case *LoginMessage:
		return glh.handleLogin(m, session)
	case *GameActionMessage:
		return glh.handleGameAction(m, session)
	default:
		return fmt.Errorf("unknown message type")
	}
}

func (glh *GameLogicHandler) handleLogin(msg *LoginMessage, session *PlayerSession) error {
	fmt.Printf("[GameLogic] Player %d logged in\n", msg.PlayerID)
	session.SetState("logged_in", true)
	session.SetState("login_time", time.Now())
	return nil
}

func (glh *GameLogicHandler) handleGameAction(msg *GameActionMessage, session *PlayerSession) error {
	fmt.Printf("[GameLogic] Player %d action: %s\n", msg.PlayerID, msg.Action)
	// 게임 로직 검증 및 상태 업데이트
	return nil
}

// 게임 서버 메인 클래스
type GameServer struct {
	sessionManager     *SessionManager
	gameLogicHandler   *GameLogicHandler
	messageQueue       chan Message
	isRunning          bool
	mu                 sync.Mutex
}

func NewGameServer() *GameServer {
	sm := NewSessionManager()
	return &GameServer{
		sessionManager:     sm,
		gameLogicHandler:   NewGameLogicHandler(sm),
		messageQueue:       make(chan Message, 1000),
		isRunning:          false,
	}
}

// 서버를 시작한다
func (gs *GameServer) Start() {
	gs.mu.Lock()
	if gs.isRunning {
		gs.mu.Unlock()
		return
	}
	gs.isRunning = true
	gs.mu.Unlock()
	
	fmt.Println("[GameServer] Starting...")
	
	// 메시지 처리 루프
	go gs.messageProcessingLoop()
	
	// 세션 헬스 체크 루프
	go gs.sessionHealthCheckLoop()
}

// 메시지 처리 루프
func (gs *GameServer) messageProcessingLoop() {
	for msg := range gs.messageQueue {
		if err := gs.gameLogicHandler.HandleMessage(msg); err != nil {
			fmt.Printf("[Error] Failed to handle message: %v\n", err)
		}
	}
}

// 세션 헬스 체크 루프
func (gs *GameServer) sessionHealthCheckLoop() {
	ticker := time.NewTicker(10 * time.Second)
	defer ticker.Stop()
	
	for range ticker.C {
		sessions := gs.sessionManager.GetAllSessions()
		fmt.Printf("[HealthCheck] Active sessions: %d\n", len(sessions))
		
		// 타임아웃된 세션 제거 (예: 5분 이상 활동 없음)
		for _, session := range sessions {
			if time.Since(session.LastActivity) > 5*time.Minute {
				gs.sessionManager.RemoveSession(session.PlayerID)
			}
		}
	}
}

// 메시지를 큐에 추가한다
func (gs *GameServer) EnqueueMessage(msg Message) {
	select {
	case gs.messageQueue <- msg:
	default:
		fmt.Println("[Warning] Message queue full, dropping message")
	}
}

// 플레이어를 연결한다
func (gs *GameServer) ConnectPlayer(playerID uint32, username string) {
	session := gs.sessionManager.CreateSession(playerID, username)
	fmt.Printf("Player session created: %v\n", session.PlayerID)
}

// 플레이어를 연결 해제한다
func (gs *GameServer) DisconnectPlayer(playerID uint32) {
	gs.sessionManager.RemoveSession(playerID)
}

// 서버를 종료한다
func (gs *GameServer) Shutdown() {
	gs.mu.Lock()
	if !gs.isRunning {
		gs.mu.Unlock()
		return
	}
	gs.isRunning = false
	gs.mu.Unlock()
	
	close(gs.messageQueue)
	fmt.Println("[GameServer] Shutdown complete")
}

func demonstrateBasicArchitecture() {
	fmt.Println("=== Basic Game Server Architecture ===\n")
	
	// 서버 생성
	server := NewGameServer()
	server.Start()
	
	// 플레이어 연결
	server.ConnectPlayer(1, "Alice")
	server.ConnectPlayer(2, "Bob")
	
	// 메시지 처리
	server.EnqueueMessage(&LoginMessage{PlayerID: 1, Username: "Alice"})
	server.EnqueueMessage(&GameActionMessage{
		PlayerID: 1,
		Action:   "MOVE",
		Data: map[string]interface{}{
			"x": 100,
			"y": 200,
		},
	})
	
	// 처리 대기
	time.Sleep(1 * time.Second)
	
	// 정보 확인
	session := server.sessionManager.GetSession(1)
	if session != nil {
		fmt.Printf("\nSession Info:\n")
		fmt.Printf("  PlayerID: %d\n", session.PlayerID)
		fmt.Printf("  Username: %s\n", session.Username)
		fmt.Printf("  Connected: %v\n", session.ConnectedAt)
	}
	
	// 플레이어 연결 해제
	server.DisconnectPlayer(1)
	server.Shutdown()
}

func main() {
	demonstrateBasicArchitecture()
}
```

## 13.2 단일 서버 vs 분산 서버

게임 서버의 확장성은 중요한 설계 고려사항이다. 플레이어 수와 게임의 특성에 따라 단일 서버 또는 분산 서버 구조를 선택해야 한다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// 단일 서버 아키텍처
type SingleServerArchitecture struct {
	maxPlayers int
	players    map[uint32]*Player
	mu         sync.RWMutex
}

type Player struct {
	ID        uint32
	Name      string
	Gold      int
	Level     int
	Location  string
}

func NewSingleServerArchitecture(maxPlayers int) *SingleServerArchitecture {
	return &SingleServerArchitecture{
		maxPlayers: maxPlayers,
		players:    make(map[uint32]*Player),
	}
}

func (ssa *SingleServerArchitecture) AddPlayer(player *Player) bool {
	ssa.mu.Lock()
	defer ssa.mu.Unlock()
	
	if len(ssa.players) >= ssa.maxPlayers {
		return false // 서버 만석
	}
	
	ssa.players[player.ID] = player
	return true
}

func (ssa *SingleServerArchitecture) GetPlayerCount() int {
	ssa.mu.RLock()
	defer ssa.mu.RUnlock()
	return len(ssa.players)
}

// 장점: 간단한 구현, 낮은 레이턴시, 상태 동기화 용이
// 단점: 확장성 제한, 높은 부하 시 성능 저하, 단일 장애점(SPOF)

// 분산 서버 아키텍처
type DistributedServerArchitecture struct {
	// 게이트웨이: 플레이어 접속 관리
	gateway *Gateway
	
	// 게임 서버들: 실제 게임 로직 처리
	gameServers map[string]*GameServerNode
	
	// 상태 저장소: 플레이어 상태 동기화
	stateStore *StateStore
	
	mu sync.RWMutex
}

type Gateway struct {
	playerToServer map[uint32]string // playerID -> serverID
	mu             sync.RWMutex
}

type GameServerNode struct {
	ServerID string
	Players  map[uint32]*Player
	mu       sync.RWMutex
}

type StateStore struct {
	playerStates map[uint32]map[string]interface{}
	mu           sync.RWMutex
}

func NewDistributedServerArchitecture() *DistributedServerArchitecture {
	return &DistributedServerArchitecture{
		gateway: &Gateway{
			playerToServer: make(map[uint32]string),
		},
		gameServers: make(map[string]*GameServerNode),
		stateStore: &StateStore{
			playerStates: make(map[uint32]map[string]interface{}),
		},
	}
}

func (dsa *DistributedServerArchitecture) RegisterGameServer(serverID string) {
	dsa.mu.Lock()
	defer dsa.mu.Unlock()
	
	dsa.gameServers[serverID] = &GameServerNode{
		ServerID: serverID,
		Players:  make(map[uint32]*Player),
	}
	
	fmt.Printf("[Distributed] Game server registered: %s\n", serverID)
}

func (dsa *DistributedServerArchitecture) AssignPlayerToServer(playerID uint32, player *Player) string {
	// 로드 밸런싱: 가장 플레이어가 적은 서버에 할당
	dsa.mu.RLock()
	defer dsa.mu.RUnlock()
	
	var selectedServer string
	minPlayers := int(^uint32(0) >> 1) // 최대값
	
	for serverID, server := range dsa.gameServers {
		server.mu.RLock()
		playerCount := len(server.Players)
		server.mu.RUnlock()
		
		if playerCount < minPlayers {
			minPlayers = playerCount
			selectedServer = serverID
		}
	}
	
	if selectedServer != "" {
		dsa.gateway.mu.Lock()
		dsa.gateway.playerToServer[playerID] = selectedServer
		dsa.gateway.mu.Unlock()
		
		dsa.gameServers[selectedServer].mu.Lock()
		dsa.gameServers[selectedServer].Players[playerID] = player
		dsa.gameServers[selectedServer].mu.Unlock()
	}
	
	return selectedServer
}

// 장점: 확장성 우수, 높은 성능, 장애 격리, 유연한 구성
// 단점: 복잡한 구현, 상태 동기화 어려움, 높은 레이턴시 가능성, 개발 비용 증가

func compareArchitectures() {
	fmt.Println("=== Single Server vs Distributed Architecture ===\n")
	
	fmt.Println("[Single Server Architecture]")
	singleServer := NewSingleServerArchitecture(1000)
	
	// 플레이어 추가
	for i := uint32(1); i <= 100; i++ {
		player := &Player{
			ID:       i,
			Name:     fmt.Sprintf("Player%d", i),
			Gold:     1000,
			Level:    1,
			Location: "StartZone",
		}
		singleServer.AddPlayer(player)
	}
	
	fmt.Printf("Players: %d / %d\n", singleServer.GetPlayerCount(), singleServer.maxPlayers)
	fmt.Println("Characteristics:")
	fmt.Println("  + Simple implementation")
	fmt.Println("  + Low latency")
	fmt.Println("  + Easy state sync")
	fmt.Println("  - Limited scalability")
	fmt.Println("  - Single point of failure")
	
	fmt.Println("\n[Distributed Architecture]")
	distributed := NewDistributedServerArchitecture()
	
	// 게임 서버 등록
	distributed.RegisterGameServer("GameServer-1")
	distributed.RegisterGameServer("GameServer-2")
	distributed.RegisterGameServer("GameServer-3")
	
	// 플레이어 할당
	for i := uint32(1); i <= 100; i++ {
		player := &Player{
			ID:       i,
			Name:     fmt.Sprintf("Player%d", i),
			Gold:     1000,
			Level:    1,
			Location: "StartZone",
		}
		serverID := distributed.AssignPlayerToServer(i, player)
		if serverID != "" {
			fmt.Printf("Player %d assigned to %s\n", i, serverID)
		}
	}
	
	fmt.Println("\nCharacteristics:")
	fmt.Println("  + Excellent scalability")
	fmt.Println("  + High performance")
	fmt.Println("  + Fault isolation")
	fmt.Println("  - Complex implementation")
	fmt.Println("  - State sync challenges")
	fmt.Println("  - Higher latency potential")
}

func main() {
	compareArchitectures()
}
```

## 13.3 상태 관리 전략

게임 서버의 상태 관리는 매우 중요하다. 플레이어의 위치, 인벤토리, 통계 등의 상태를 어떻게 저장하고 동기화할지가 게임 경험과 성능에 큰 영향을 미친다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// 상태 관리 전략 1: 메모리 상태 + 데이터베이스 동기화
type GameState struct {
	PlayerID uint32
	Data     map[string]interface{}
	Version  int64 // 충돌 감지용
	mu       sync.RWMutex
}

func NewGameState(playerID uint32) *GameState {
	return &GameState{
		PlayerID: playerID,
		Data:     make(map[string]interface{}),
		Version:  0,
	}
}

func (gs *GameState) Get(key string) interface{} {
	gs.mu.RLock()
	defer gs.mu.RUnlock()
	return gs.Data[key]
}

func (gs *GameState) Set(key string, value interface{}) {
	gs.mu.Lock()
	defer gs.mu.Unlock()
	gs.Data[key] = value
	gs.Version++
}

// 데이터베이스 동기화
type DatabaseSynchronizer struct {
	states   map[uint32]*GameState
	mu       sync.RWMutex
	saveTicker *time.Ticker
}

func NewDatabaseSynchronizer() *DatabaseSynchronizer {
	synchronizer := &DatabaseSynchronizer{
		states: make(map[uint32]*GameState),
		saveTicker: time.NewTicker(30 * time.Second), // 30초마다 저장
	}
	
	// 백그라운드에서 주기적으로 데이터베이스에 저장
	go synchronizer.backgroundSave()
	
	return synchronizer
}

func (ds *DatabaseSynchronizer) RegisterState(state *GameState) {
	ds.mu.Lock()
	defer ds.mu.Unlock()
	ds.states[state.PlayerID] = state
}

func (ds *DatabaseSynchronizer) backgroundSave() {
	for range ds.saveTicker.C {
		ds.mu.RLock()
		states := make([]*GameState, 0, len(ds.states))
		for _, state := range ds.states {
			states = append(states, state)
		}
		ds.mu.RUnlock()
		
		for _, state := range states {
			// 데이터베이스에 저장 (시뮬레이션)
			fmt.Printf("[DB] Saving state for player %d (version: %d)\n", 
				state.PlayerID, state.Version)
		}
	}
}

// 상태 관리 전략 2: 이벤트 소싱
// 모든 상태 변화를 이벤트로 기록하고, 이벤트를 재생하여 현재 상태를 복원한다
type GameEvent struct {
	EventType string
	Timestamp time.Time
	Data      map[string]interface{}
}

type EventSourcedState struct {
	PlayerID uint32
	Events   []*GameEvent
	Current  map[string]interface{}
	mu       sync.RWMutex
}

func NewEventSourcedState(playerID uint32) *EventSourcedState {
	return &EventSourcedState{
		PlayerID: playerID,
		Events:   make([]*GameEvent, 0),
		Current:  make(map[string]interface{}),
	}
}

func (ess *EventSourcedState) ApplyEvent(eventType string, data map[string]interface{}) {
	ess.mu.Lock()
	defer ess.mu.Unlock()
	
	event := &GameEvent{
		EventType: eventType,
		Timestamp: time.Now(),
		Data:      data,
	}
	
	ess.Events = append(ess.Events, event)
	ess.applyToState(event)
	
	fmt.Printf("[Event] %s for player %d\n", eventType, ess.PlayerID)
}

func (ess *EventSourcedState) applyToState(event *GameEvent) {
	switch event.EventType {
	case "GAIN_GOLD":
		current := ess.Current["gold"].(int)
		amount := event.Data["amount"].(int)
		ess.Current["gold"] = current + amount
	case "LEVEL_UP":
		ess.Current["level"] = event.Data["level"]
	case "MOVE":
		ess.Current["x"] = event.Data["x"]
		ess.Current["y"] = event.Data["y"]
	}
}

func (ess *EventSourcedState) GetState() map[string]interface{} {
	ess.mu.RLock()
	defer ess.mu.RUnlock()
	
	// 현재 상태의 복사본을 반환
	stateCopy := make(map[string]interface{})
	for k, v := range ess.Current {
		stateCopy[k] = v
	}
	return stateCopy
}

func (ess *EventSourcedState) ReplayEvents() map[string]interface{} {
	ess.mu.RLock()
	defer ess.mu.RUnlock()
	
	// 모든 이벤트를 재생하여 현재 상태 복원
	state := make(map[string]interface{})
	state["gold"] = 0
	state["level"] = 1
	state["x"] = 0
	state["y"] = 0
	
	for _, event := range ess.Events {
		switch event.EventType {
		case "GAIN_GOLD":
			current := state["gold"].(int)
			amount := event.Data["amount"].(int)
			state["gold"] = current + amount
		case "LEVEL_UP":
			state["level"] = event.Data["level"]
		case "MOVE":
			state["x"] = event.Data["x"]
			state["y"] = event.Data["y"]
		}
	}
	
	return state
}

// 상태 관리 전략 3: 캐시 + 지연 쓰기
// 자주 접근하는 데이터는 메모리에 캐시하고, 일정 조건에서 데이터베이스에 저장
type CachedGameState struct {
	PlayerID      uint32
	Data          map[string]interface{}
	Dirty         bool // 데이터 변경 여부
	LastSaveTime  time.Time
	SaveInterval  time.Duration
	mu            sync.RWMutex
}

func NewCachedGameState(playerID uint32) *CachedGameState {
	return &CachedGameState{
		PlayerID:     playerID,
		Data:         make(map[string]interface{}),
		Dirty:        false,
		LastSaveTime: time.Now(),
		SaveInterval: 5 * time.Minute,
	}
}

func (cgs *CachedGameState) Get(key string) interface{} {
	cgs.mu.RLock()
	defer cgs.mu.RUnlock()
	return cgs.Data[key]
}

func (cgs *CachedGameState) Set(key string, value interface{}) {
	cgs.mu.Lock()
	defer cgs.mu.Unlock()
	cgs.Data[key] = value
	cgs.Dirty = true
}

func (cgs *CachedGameState) ShouldSave() bool {
	cgs.mu.RLock()
	defer cgs.mu.RUnlock()
	
	if !cgs.Dirty {
		return false
	}
	
	if time.Since(cgs.LastSaveTime) > cgs.SaveInterval {
		return true
	}
	
	return false
}

func (cgs *CachedGameState) SaveToDatabase() {
	cgs.mu.Lock()
	defer cgs.mu.Unlock()
	
	// 데이터베이스에 저장
	fmt.Printf("[Cache] Saving cached state for player %d\n", cgs.PlayerID)
	cgs.Dirty = false
	cgs.LastSaveTime = time.Now()
}

func demonstrateStateManagement() {
	fmt.Println("=== State Management Strategies ===\n")
	
	// 전략 1: 메모리 상태 + DB 동기화
	fmt.Println("[Strategy 1: Memory State + DB Sync]")
	state1 := NewGameState(1)
	state1.Set("gold", 1000)
	state1.Set("level", 10)
	state1.Set("location", "Forest")
	
	synchronizer := NewDatabaseSynchronizer()
	synchronizer.RegisterState(state1)
	
	fmt.Printf("State: gold=%v, level=%v, location=%v\n",
		state1.Get("gold"), state1.Get("level"), state1.Get("location"))
	
	// 전략 2: 이벤트 소싱
	fmt.Println("\n[Strategy 2: Event Sourcing]")
	eventState := NewEventSourcedState(2)
	eventState.ApplyEvent("GAIN_GOLD", map[string]interface{}{"amount": 500})
	eventState.ApplyEvent("LEVEL_UP", map[string]interface{}{"level": 11})
	eventState.ApplyEvent("MOVE", map[string]interface{}{"x": 100, "y": 200})
	
	currentState := eventState.GetState()
	fmt.Printf("Current state: %v\n", currentState)
	
	// 이벤트 재생
	replayedState := eventState.ReplayEvents()
	fmt.Printf("Replayed state: %v\n", replayedState)
	
	// 전략 3: 캐시 + 지연 쓰기
	fmt.Println("\n[Strategy 3: Cache + Lazy Write]")
	cachedState := NewCachedGameState(3)
	cachedState.Set("gold", 2000)
	cachedState.Set("level", 15)
	
	fmt.Printf("Cached - Dirty: %v, Should Save: %v\n", 
		cachedState.Dirty, cachedState.ShouldSave())
	
	// 시간 경과 시뮬레이션
	time.Sleep(100 * time.Millisecond)
	fmt.Printf("Should Save: %v\n", cachedState.ShouldSave())
	
	if cachedState.ShouldSave() {
		cachedState.SaveToDatabase()
	}
}

func main() {
	demonstrateStateManagement()
}
```

## 13.4 이벤트 기반 아키텍처

이벤트 기반 아키텍처는 게임 서버의 느슨한 결합을 실현한다. 각 컴포넌트가 이벤트를 발행하고 구독하는 방식으로 통신하므로, 새로운 기능을 쉽게 추가할 수 있다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// 이벤트 인터페이스
type Event interface {
	GetEventType() string
	GetTimestamp() time.Time
}

// 플레이어 관련 이벤트들
type PlayerJoinedEvent struct {
	PlayerID  uint32
	Username  string
	Timestamp time.Time
}

func (pje *PlayerJoinedEvent) GetEventType() string { return "PLAYER_JOINED" }
func (pje *PlayerJoinedEvent) GetTimestamp() time.Time { return pje.Timestamp }

type PlayerLevelUpEvent struct {
	PlayerID  uint32
	NewLevel  int
	Timestamp time.Time
}

func (plue *PlayerLevelUpEvent) GetEventType() string { return "PLAYER_LEVEL_UP" }
func (plue *PlayerLevelUpEvent) GetTimestamp() time.Time { return plue.Timestamp }

type ItemObtainedEvent struct {
	PlayerID  uint32
	ItemID    uint32
	ItemName  string
	Timestamp time.Time
}

func (ioe *ItemObtainedEvent) GetEventType() string { return "ITEM_OBTAINED" }
func (ioe *ItemObtainedEvent) GetTimestamp() time.Time { return ioe.Timestamp }

// 이벤트 핸들러
type EventHandler interface {
	Handle(event Event) error
}

// 로깅 핸들러
type LoggingHandler struct {
	name string
}

func (lh *LoggingHandler) Handle(event Event) error {
	fmt.Printf("[%s] Event: %s at %v\n", lh.name, event.GetEventType(), event.GetTimestamp())
	return nil
}

// 통계 수집 핸들러
type StatisticsHandler struct {
	eventCounts map[string]int
	mu          sync.Mutex
}

func NewStatisticsHandler() *StatisticsHandler {
	return &StatisticsHandler{
		eventCounts: make(map[string]int),
	}
}

func (sh *StatisticsHandler) Handle(event Event) error {
	sh.mu.Lock()
	defer sh.mu.Unlock()
	
	sh.eventCounts[event.GetEventType()]++
	fmt.Printf("[Statistics] %s count: %d\n", event.GetEventType(), sh.eventCounts[event.GetEventType()])
	return nil
}

// 알림 핸들러
type NotificationHandler struct{}

func (nh *NotificationHandler) Handle(event Event) error {
	switch e := event.(type) {
	case *PlayerJoinedEvent:
		fmt.Printf("[Notification] Welcome %s to the game!\n", e.Username)
	case *PlayerLevelUpEvent:
		fmt.Printf("[Notification] Player %d reached level %d!\n", e.PlayerID, e.NewLevel)
	case *ItemObtainedEvent:
		fmt.Printf("[Notification] Player %d obtained %s!\n", e.PlayerID, e.ItemName)
	}
	return nil
}

// 이벤트 버스
type EventBus struct {
	subscribers map[string][]EventHandler
	mu          sync.RWMutex
}

func NewEventBus() *EventBus {
	return &EventBus{
		subscribers: make(map[string][]EventHandler),
	}
}

// 핸들러를 구독시킨다
func (eb *EventBus) Subscribe(eventType string, handler EventHandler) {
	eb.mu.Lock()
	defer eb.mu.Unlock()
	
	eb.subscribers[eventType] = append(eb.subscribers[eventType], handler)
	fmt.Printf("[EventBus] Handler subscribed to %s\n", eventType)
}

// 이벤트를 발행한다
func (eb *EventBus) Publish(event Event) error {
	eb.mu.RLock()
	handlers := eb.subscribers[event.GetEventType()]
	eb.mu.RUnlock()
	
	// 각 핸들러에 이벤트를 전달한다
	for _, handler := range handlers {
		if err := handler.Handle(event); err != nil {
			return err
		}
	}
	
	return nil
}

func demonstrateEventDrivenArchitecture() {
	fmt.Println("=== Event-Driven Architecture ===\n")
	
	// 이벤트 버스 생성
	eventBus := NewEventBus()
	
	// 핸들러 생성
	logger := &LoggingHandler{name: "Logger"}
	stats := NewStatisticsHandler()
	notifier := &NotificationHandler{}
	
	// 핸들러 등록
	eventBus.Subscribe("PLAYER_JOINED", logger)
	eventBus.Subscribe("PLAYER_JOINED", stats)
	eventBus.Subscribe("PLAYER_JOINED", notifier)
	
	eventBus.Subscribe("PLAYER_LEVEL_UP", logger)
	eventBus.Subscribe("PLAYER_LEVEL_UP", stats)
	eventBus.Subscribe("PLAYER_LEVEL_UP", notifier)
	
	eventBus.Subscribe("ITEM_OBTAINED", logger)
	eventBus.Subscribe("ITEM_OBTAINED", stats)
	eventBus.Subscribe("ITEM_OBTAINED", notifier)
	
	// 이벤트 발행
	fmt.Println("Publishing PLAYER_JOINED event:")
	eventBus.Publish(&PlayerJoinedEvent{
		PlayerID:  1,
		Username:  "Alice",
		Timestamp: time.Now(),
	})
	
	fmt.Println("\nPublishing ITEM_OBTAINED event:")
	eventBus.Publish(&ItemObtainedEvent{
		PlayerID:  1,
		ItemID:    101,
		ItemName:  "Excalibur",
		Timestamp: time.Now(),
	})
	
	fmt.Println("\nPublishing PLAYER_LEVEL_UP event:")
	eventBus.Publish(&PlayerLevelUpEvent{
		PlayerID:  1,
		NewLevel:  42,
		Timestamp: time.Now(),
	})
}

func main() {
	demonstrateEventDrivenArchitecture()
}
```

## 13.5 액터 모델 패턴

액터 모델은 동시성 프로그래밍을 단순화하는 패턴이다. 각 게임 엔티티(플레이어, 방, 등)를 독립적인 액터로 모델링하고, 액터 간 통신은 메시지를 통해 이루어진다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// 액터에게 보낼 메시지
type ActorMessage interface {
	GetType() string
}

type MoveMessage struct {
	X, Y int
}

func (mm *MoveMessage) GetType() string { return "MOVE" }

type DamageMessage struct {
	Amount int
}

func (dm *DamageMessage) GetType() string { return "DAMAGE" }

type QueryStateMessage struct {
	ResponseChan chan map[string]interface{}
}

func (qsm *QueryStateMessage) GetType() string { return "QUERY_STATE" }

// 액터: 플레이어를 나타낸다
type PlayerActor struct {
	ID       uint32
	Name     string
	Health   int
	X, Y     int
	msgChan  chan ActorMessage
	stopChan chan struct{}
	mu       sync.RWMutex
}

func NewPlayerActor(id uint32, name string) *PlayerActor {
	return &PlayerActor{
		ID:       id,
		Name:     name,
		Health:   100,
		X:        0,
		Y:        0,
		msgChan:  make(chan ActorMessage, 10),
		stopChan: make(chan struct{}),
	}
}

// 액터의 메인 루프
func (pa *PlayerActor) Start() {
	go pa.messageLoop()
}

func (pa *PlayerActor) messageLoop() {
	for {
		select {
		case msg := <-pa.msgChan:
			pa.handleMessage(msg)
		case <-pa.stopChan:
			fmt.Printf("[PlayerActor] %s stopped\n", pa.Name)
			return
		}
	}
}

func (pa *PlayerActor) handleMessage(msg ActorMessage) {
	pa.mu.Lock()
	defer pa.mu.Unlock()
	
	switch m := msg.(type) {
	case *MoveMessage:
		pa.X = m.X
		pa.Y = m.Y
		fmt.Printf("[%s] Moved to (%d, %d)\n", pa.Name, pa.X, pa.Y)
		
	case *DamageMessage:
		pa.Health -= m.Amount
		if pa.Health < 0 {
			pa.Health = 0
		}
		fmt.Printf("[%s] Took %d damage, health: %d\n", pa.Name, m.Amount, pa.Health)
		
	case *QueryStateMessage:
		state := map[string]interface{}{
			"id":     pa.ID,
			"name":   pa.Name,
			"health": pa.Health,
			"x":      pa.X,
			"y":      pa.Y,
		}
		m.ResponseChan <- state
	}
}

// 액터에게 메시지를 전송한다
func (pa *PlayerActor) Send(msg ActorMessage) {
	select {
	case pa.msgChan <- msg:
	default:
		fmt.Printf("[Warning] Message queue full for %s\n", pa.Name)
	}
}

// 액터를 중지한다
func (pa *PlayerActor) Stop() {
	close(pa.stopChan)
}

// 상태를 쿼리한다
func (pa *PlayerActor) GetState() map[string]interface{} {
	responseChan := make(chan map[string]interface{})
	pa.Send(&QueryStateMessage{ResponseChan: responseChan})
	return <-responseChan
}

// 액터 관리자: 여러 액터를 관리한다
type ActorSystem struct {
	actors map[uint32]*PlayerActor
	mu     sync.RWMutex
}

func NewActorSystem() *ActorSystem {
	return &ActorSystem{
		actors: make(map[uint32]*PlayerActor),
	}
}

func (as *ActorSystem) CreateActor(id uint32, name string) *PlayerActor {
	as.mu.Lock()
	defer as.mu.Unlock()
	
	actor := NewPlayerActor(id, name)
	actor.Start()
	as.actors[id] = actor
	
	fmt.Printf("[ActorSystem] Created actor: %s\n", name)
	return actor
}

func (as *ActorSystem) GetActor(id uint32) *PlayerActor {
	as.mu.RLock()
	defer as.mu.RUnlock()
	return as.actors[id]
}

func (as *ActorSystem) SendMessage(toID uint32, msg ActorMessage) {
	actor := as.GetActor(toID)
	if actor != nil {
		actor.Send(msg)
	}
}

func (as *ActorSystem) Shutdown() {
	as.mu.RLock()
	actors := make([]*PlayerActor, 0, len(as.actors))
	for _, actor := range as.actors {
		actors = append(actors, actor)
	}
	as.mu.RUnlock()
	
	for _, actor := range actors {
		actor.Stop()
	}
	
	fmt.Println("[ActorSystem] Shutdown complete")
}

func demonstrateActorModel() {
	fmt.Println("=== Actor Model Pattern ===\n")
	
	// 액터 시스템 생성
	system := NewActorSystem()
	
	// 액터 생성
	alice := system.CreateActor(1, "Alice")
	bob := system.CreateActor(2, "Bob")
	
	// 메시지 전송
	fmt.Println("\nSending messages:")
	alice.Send(&MoveMessage{X: 100, Y: 200})
	bob.Send(&MoveMessage{X: 50, Y: 150})
	
	time.Sleep(100 * time.Millisecond)
	
	// 데미지 처리
	fmt.Println("\nApplying damage:")
	alice.Send(&DamageMessage{Amount: 30})
	bob.Send(&DamageMessage{Amount: 20})
	
	time.Sleep(100 * time.Millisecond)
	
	// 상태 조회
	fmt.Println("\nQuerying state:")
	aliceState := alice.GetState()
	bobState := bob.GetState()
	
	fmt.Printf("Alice: %v\n", aliceState)
	fmt.Printf("Bob: %v\n", bobState)
	
	// 시스템 종료
	system.Shutdown()
}

func main() {
	demonstrateActorModel()
}
```

액터 모델의 장점은 다음과 같다:

첫째, 동시성 처리가 자연스럽고 안전하다. 각 액터는 독립적으로 실행되고 메시지를 통해서만 통신한다.

둘째, 확장성이 우수하다. 새로운 액터를 쉽게 추가할 수 있고, 여러 머신에 분산시킬 수 있다.

셋째, 디버깅이 상대적으로 쉽다. 메시지 순서를 추적하면 문제를 진단할 수 있다.

게임 서버에서 액터 모델은 플레이어, 방, 몬스터 등 게임 월드의 주요 엔티티를 모델링하는 데 적합하다. Go의 고루틴과 채널을 활용하면 액터 모델을 효율적으로 구현할 수 있다.

---

이제 Chapter 13이 완성되었다. 이 장에서는 게임 서버의 기본 아키텍처부터 고급 패턴까지 다루었다. 게임 서버 설계는 게임의 특성과 요구사항에 따라 다르므로, 이 장에서 배운 패턴들을 프로젝트에 맞게 선택하고 조합하여 사용해야 한다. 다음 장에서는 세션 관리라는 게임 서버의 핵심 컴포넌트를 자세히 다룰 것이다.



# Chapter 14. 세션 관리

세션 관리는 소켓 기반 게임 서버의 핵심 구성 요소다. 클라이언트가 서버에 연결하는 순간부터 연결이 종료될 때까지, 각 연결의 상태와 데이터를 안전하게 관리해야 한다. 이 장에서는 동시성 환경에서 안전한 세션 관리 시스템을 구축하는 방법을 다룬다.

## 14.1 세션의 개념과 생명주기

세션(Session)은 클라이언트와 서버 간의 논리적 연결을 나타낸다. TCP 연결은 물리적 연결이지만, 세션은 이를 추상화하여 게임 서버가 이해할 수 있는 형태로 표현한다.

### 세션의 주요 역할

세션은 다음과 같은 역할을 수행한다:

- **연결 정보 보관**: TCP 소켓 연결, 클라이언트 IP/포트 등
- **사용자 상태 관리**: 로그인 상태, 인증 정보, 닉네임 등
- **게임 컨텍스트 유지**: 현재 참여 중인 방, 게임 상태 등
- **메시지 송수신 인터페이스**: 패킷을 주고받는 채널 제공
- **자원 관리**: 버퍼, 고루틴 등의 정리

### 세션 생명주기

```
┌─────────────┐
│   Created   │ ← 클라이언트 연결 수락
└──────┬──────┘
       │
       ▼
┌─────────────┐
│Authenticating│ ← 인증 진행 중
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Active    │ ← 정상 활동 중
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  Closing    │ ← 종료 절차 진행 중
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Closed    │ ← 완전히 종료됨
└─────────────┘
```

### 기본 세션 구조체

세션을 표현하는 기본 구조체를 설계한다:

```go
package session

import (
    "net"
    "sync"
    "sync/atomic"
    "time"
)

// SessionState는 세션의 현재 상태를 나타낸다
type SessionState int32

const (
    StateCreated SessionState = iota
    StateAuthenticating
    StateActive
    StateClosing
    StateClosed
)

// Session은 클라이언트 연결을 나타낸다
type Session struct {
    id            uint64        // 고유 세션 ID
    conn          net.Conn      // TCP 연결
    state         atomic.Int32  // 현재 상태 (동시성 안전)
    
    // 사용자 정보
    userID        uint64        // 사용자 ID (인증 후 설정)
    nickname      string        // 닉네임
    
    // 네트워크 관련
    sendChan      chan []byte   // 송신 채널
    recvBuffer    []byte        // 수신 버퍼
    
    // 타임스탬프
    createdAt     time.Time     // 생성 시각
    lastActivity  time.Time     // 마지막 활동 시각
    
    // 동기화
    mu            sync.RWMutex  // 데이터 보호용 뮤텍스
    closeOnce     sync.Once     // 한 번만 닫기
    
    // 컨텍스트
    roomID        uint64        // 현재 참여 중인 방 ID
    
    // 종료 신호
    done          chan struct{} // 종료 신호
}

// NewSession은 새로운 세션을 생성한다
func NewSession(id uint64, conn net.Conn) *Session {
    now := time.Now()
    s := &Session{
        id:           id,
        conn:         conn,
        sendChan:     make(chan []byte, 100), // 버퍼 크기 100
        recvBuffer:   make([]byte, 4096),     // 4KB 수신 버퍼
        createdAt:    now,
        lastActivity: now,
        done:         make(chan struct{}),
    }
    
    // 초기 상태 설정
    s.state.Store(int32(StateCreated))
    
    return s
}

// ID는 세션 ID를 반환한다
func (s *Session) ID() uint64 {
    return s.id
}

// GetState는 현재 세션 상태를 반환한다
func (s *Session) GetState() SessionState {
    return SessionState(s.state.Load())
}

// SetState는 세션 상태를 변경한다
func (s *Session) SetState(state SessionState) {
    s.state.Store(int32(state))
}

// IsActive는 세션이 활성 상태인지 확인한다
func (s *Session) IsActive() bool {
    return s.GetState() == StateActive
}

// UpdateActivity는 마지막 활동 시각을 갱신한다
func (s *Session) UpdateActivity() {
    s.mu.Lock()
    s.lastActivity = time.Now()
    s.mu.Unlock()
}

// GetLastActivity는 마지막 활동 시각을 반환한다
func (s *Session) GetLastActivity() time.Time {
    s.mu.RLock()
    defer s.mu.RUnlock()
    return s.lastActivity
}
```

위 코드에서 주목할 점:

**atomic.Int32 사용**: 세션 상태는 여러 고루틴에서 동시에 접근할 수 있으므로 atomic 타입을 사용하여 동시성 안전성을 보장한다.

**sync.Once**: 세션 종료는 한 번만 실행되어야 하므로 `closeOnce`를 사용한다.

**채널 버퍼 크기**: `sendChan`에 버퍼를 두어 일시적인 송신 지연을 흡수한다.

### 세션 송수신 메서드

```go
// Send는 데이터를 클라이언트로 전송한다
// 블로킹을 방지하기 위해 select 문을 사용한다
func (s *Session) Send(data []byte) error {
    // 닫힌 세션에는 전송하지 않는다
    if s.GetState() >= StateClosing {
        return ErrSessionClosed
    }
    
    select {
    case s.sendChan <- data:
        return nil
    case <-s.done:
        return ErrSessionClosed
    default:
        // 채널이 가득 찬 경우
        return ErrSendBufferFull
    }
}

// SendBlocking은 블로킹 방식으로 데이터를 전송한다
func (s *Session) SendBlocking(data []byte) error {
    if s.GetState() >= StateClosing {
        return ErrSessionClosed
    }
    
    select {
    case s.sendChan <- data:
        return nil
    case <-s.done:
        return ErrSessionClosed
    }
}

// RemoteAddr는 클라이언트의 원격 주소를 반환한다
func (s *Session) RemoteAddr() net.Addr {
    return s.conn.RemoteAddr()
}
```

### 사용자 정보 관리

```go
// SetUserInfo는 인증 완료 후 사용자 정보를 설정한다
func (s *Session) SetUserInfo(userID uint64, nickname string) {
    s.mu.Lock()
    defer s.mu.Unlock()
    
    s.userID = userID
    s.nickname = nickname
    s.SetState(StateActive)
}

// GetUserInfo는 사용자 정보를 반환한다
func (s *Session) GetUserInfo() (userID uint64, nickname string) {
    s.mu.RLock()
    defer s.mu.RUnlock()
    
    return s.userID, s.nickname
}

// GetUserID는 사용자 ID를 반환한다
func (s *Session) GetUserID() uint64 {
    s.mu.RLock()
    defer s.mu.RUnlock()
    return s.userID
}

// SetRoomID는 현재 참여 중인 방 ID를 설정한다
func (s *Session) SetRoomID(roomID uint64) {
    s.mu.Lock()
    s.roomID = roomID
    s.mu.Unlock()
}

// GetRoomID는 현재 참여 중인 방 ID를 반환한다
func (s *Session) GetRoomID() uint64 {
    s.mu.RLock()
    defer s.mu.RUnlock()
    return s.roomID
}
```

### 에러 타입 정의

```go
package session

import "errors"

var (
    ErrSessionClosed    = errors.New("session is closed")
    ErrSendBufferFull   = errors.New("send buffer is full")
    ErrInvalidState     = errors.New("invalid session state")
    ErrNotAuthenticated = errors.New("session is not authenticated")
)
```

## 14.2 세션 스토리지 구현

여러 세션을 효율적으로 관리하기 위한 스토리지를 구현한다. 세션 스토리지는 다음 기능을 제공한다:

- 세션 추가/조회/삭제
- 사용자 ID로 세션 찾기
- 전체 세션 순회
- 통계 정보 제공

### 세션 스토리지 구조체

```go
package session

import (
    "sync"
    "sync/atomic"
)

// SessionStorage는 모든 세션을 관리한다
type SessionStorage struct {
    sessions      sync.Map       // sessionID -> *Session
    userSessions  sync.Map       // userID -> *Session
    nextSessionID atomic.Uint64  // 다음 세션 ID
    count         atomic.Int64   // 현재 세션 수
}

// NewSessionStorage는 새로운 세션 스토리지를 생성한다
func NewSessionStorage() *SessionStorage {
    return &SessionStorage{}
}

// Add는 새로운 세션을 추가한다
func (ss *SessionStorage) Add(session *Session) {
    ss.sessions.Store(session.ID(), session)
    ss.count.Add(1)
}

// Remove는 세션을 제거한다
func (ss *SessionStorage) Remove(sessionID uint64) {
    if session, ok := ss.sessions.LoadAndDelete(sessionID); ok {
        ss.count.Add(-1)
        
        // 사용자 세션 매핑도 제거
        if s, ok := session.(*Session); ok {
            userID := s.GetUserID()
            if userID > 0 {
                ss.userSessions.Delete(userID)
            }
        }
    }
}

// Get은 세션 ID로 세션을 조회한다
func (ss *SessionStorage) Get(sessionID uint64) (*Session, bool) {
    if session, ok := ss.sessions.Load(sessionID); ok {
        return session.(*Session), true
    }
    return nil, false
}

// GetByUserID는 사용자 ID로 세션을 조회한다
func (ss *SessionStorage) GetByUserID(userID uint64) (*Session, bool) {
    if session, ok := ss.userSessions.Load(userID); ok {
        return session.(*Session), true
    }
    return nil, false
}

// BindUser는 사용자 ID와 세션을 연결한다
// 인증 완료 후 호출된다
func (ss *SessionStorage) BindUser(userID uint64, session *Session) {
    ss.userSessions.Store(userID, session)
}

// UnbindUser는 사용자 ID와 세션의 연결을 해제한다
func (ss *SessionStorage) UnbindUser(userID uint64) {
    ss.userSessions.Delete(userID)
}

// Count는 현재 세션 수를 반환한다
func (ss *SessionStorage) Count() int64 {
    return ss.count.Load()
}

// GenerateSessionID는 새로운 세션 ID를 생성한다
func (ss *SessionStorage) GenerateSessionID() uint64 {
    return ss.nextSessionID.Add(1)
}
```

### 세션 순회 기능

```go
// Range는 모든 세션을 순회한다
// fn이 false를 반환하면 순회를 중단한다
func (ss *SessionStorage) Range(fn func(session *Session) bool) {
    ss.sessions.Range(func(key, value interface{}) bool {
        session := value.(*Session)
        return fn(session)
    })
}

// FindSessions는 조건에 맞는 세션들을 찾는다
func (ss *SessionStorage) FindSessions(predicate func(*Session) bool) []*Session {
    var result []*Session
    
    ss.sessions.Range(func(key, value interface{}) bool {
        session := value.(*Session)
        if predicate(session) {
            result = append(result, session)
        }
        return true
    })
    
    return result
}

// GetActiveSessions는 활성 상태의 세션들을 반환한다
func (ss *SessionStorage) GetActiveSessions() []*Session {
    return ss.FindSessions(func(s *Session) bool {
        return s.IsActive()
    })
}

// GetSessionsByRoom은 특정 방에 있는 세션들을 반환한다
func (ss *SessionStorage) GetSessionsByRoom(roomID uint64) []*Session {
    return ss.FindSessions(func(s *Session) bool {
        return s.GetRoomID() == roomID
    })
}
```

### 통계 정보 제공

```go
// Statistics는 세션 통계 정보를 담는다
type Statistics struct {
    TotalSessions      int64
    ActiveSessions     int64
    AuthenticatingSessions int64
    ClosingSessions    int64
}

// GetStatistics는 세션 통계를 반환한다
func (ss *SessionStorage) GetStatistics() Statistics {
    stats := Statistics{
        TotalSessions: ss.Count(),
    }
    
    ss.sessions.Range(func(key, value interface{}) bool {
        session := value.(*Session)
        state := session.GetState()
        
        switch state {
        case StateActive:
            stats.ActiveSessions++
        case StateAuthenticating:
            stats.AuthenticatingSessions++
        case StateClosing:
            stats.ClosingSessions++
        }
        
        return true
    })
    
    return stats
}
```

## 14.3 동시성 안전한 세션 관리

게임 서버는 수천 개의 동시 연결을 처리해야 하므로, 세션 관리에서 동시성 안전성은 필수다.

### 읽기-쓰기 잠금 전략

세션 데이터 접근 패턴을 분석하면, 읽기 작업이 쓰기 작업보다 훨씬 많다는 것을 알 수 있다. 따라서 `sync.RWMutex`를 사용하여 여러 고루틴이 동시에 읽기 작업을 수행할 수 있도록 한다.

```go
// 읽기 전용 작업: RLock 사용
func (s *Session) GetNickname() string {
    s.mu.RLock()
    defer s.mu.RUnlock()
    return s.nickname
}

// 쓰기 작업: Lock 사용
func (s *Session) SetNickname(nickname string) {
    s.mu.Lock()
    defer s.mu.Unlock()
    s.nickname = nickname
}
```

### Atomic 연산 활용

자주 변경되는 단순 값에는 atomic 패키지를 사용하여 뮤텍스 오버헤드를 줄인다.

```go
// 세션 상태는 자주 확인되므로 atomic 사용
func (s *Session) GetState() SessionState {
    return SessionState(s.state.Load())
}

func (s *Session) SetState(state SessionState) {
    s.state.Store(int32(state))
}

// Compare-And-Swap을 이용한 안전한 상태 전이
func (s *Session) TransitionState(from, to SessionState) bool {
    return s.state.CompareAndSwap(int32(from), int32(to))
}
```

### 데드락 방지 패턴

여러 세션에 걸친 작업을 수행할 때 데드락을 방지하려면 일관된 잠금 순서를 유지해야 한다.

```go
// 나쁜 예: 데드락 가능성
func transferItem(fromSession, toSession *Session, itemID uint64) {
    fromSession.mu.Lock()
    defer fromSession.mu.Unlock()
    
    toSession.mu.Lock()
    defer toSession.mu.Unlock()
    
    // 아이템 전송 로직
}

// 좋은 예: 세션 ID로 정렬하여 잠금 순서 보장
func transferItemSafe(fromSession, toSession *Session, itemID uint64) {
    // ID가 작은 세션을 먼저 잠근다
    first, second := fromSession, toSession
    if first.ID() > second.ID() {
        first, second = second, first
    }
    
    first.mu.Lock()
    defer first.mu.Unlock()
    
    second.mu.Lock()
    defer second.mu.Unlock()
    
    // 아이템 전송 로직
}
```

### 채널을 이용한 동시성 제어

각 세션이 자신만의 고루틴을 가지고 메시지를 순차 처리하는 패턴을 사용할 수 있다.

```go
// SessionCommand는 세션에 대한 명령을 나타낸다
type SessionCommand struct {
    Type    CommandType
    Data    interface{}
    ResChan chan<- interface{} // 응답 채널 (옵션)
}

type CommandType int

const (
    CmdSetNickname CommandType = iota
    CmdJoinRoom
    CmdLeaveRoom
    CmdGetUserInfo
)

// RunCommandLoop는 세션 명령을 순차 처리한다
func (s *Session) RunCommandLoop() {
    cmdChan := make(chan SessionCommand, 50)
    s.cmdChan = cmdChan
    
    go func() {
        for {
            select {
            case cmd := <-cmdChan:
                s.handleCommand(cmd)
            case <-s.done:
                return
            }
        }
    }()
}

func (s *Session) handleCommand(cmd SessionCommand) {
    switch cmd.Type {
    case CmdSetNickname:
        nickname := cmd.Data.(string)
        s.mu.Lock()
        s.nickname = nickname
        s.mu.Unlock()
        
        if cmd.ResChan != nil {
            cmd.ResChan <- nil
        }
        
    case CmdJoinRoom:
        roomID := cmd.Data.(uint64)
        s.mu.Lock()
        s.roomID = roomID
        s.mu.Unlock()
        
        if cmd.ResChan != nil {
            cmd.ResChan <- nil
        }
        
    case CmdGetUserInfo:
        s.mu.RLock()
        info := map[string]interface{}{
            "userID":   s.userID,
            "nickname": s.nickname,
            "roomID":   s.roomID,
        }
        s.mu.RUnlock()
        
        if cmd.ResChan != nil {
            cmd.ResChan <- info
        }
    }
}

// SetNicknameAsync는 비동기로 닉네임을 설정한다
func (s *Session) SetNicknameAsync(nickname string) {
    s.cmdChan <- SessionCommand{
        Type: CmdSetNickname,
        Data: nickname,
    }
}

// GetUserInfoSync는 동기적으로 사용자 정보를 가져온다
func (s *Session) GetUserInfoSync() map[string]interface{} {
    resChan := make(chan interface{}, 1)
    
    s.cmdChan <- SessionCommand{
        Type:    CmdGetUserInfo,
        ResChan: resChan,
    }
    
    return (<-resChan).(map[string]interface{})
}
```

이 패턴의 장점:

- 세션 데이터에 대한 모든 변경이 단일 고루틴에서 처리되어 경쟁 조건이 없다
- 뮤텍스 없이도 동시성 안전성을 보장한다
- 명령 큐잉으로 일시적인 부하를 흡수한다

## 14.4 세션 타임아웃 처리

클라이언트가 비정상 종료하거나 네트워크가 끊어진 경우, 서버는 이를 감지하고 세션을 정리해야 한다.

### 하트비트 메커니즘

클라이언트가 주기적으로 하트비트(heartbeat) 패킷을 보내고, 서버는 일정 시간 동안 하트비트가 없으면 세션을 종료한다.

```go
package session

import (
    "context"
    "time"
)

const (
    // HeartbeatInterval은 하트비트 전송 주기
    HeartbeatInterval = 30 * time.Second
    
    // HeartbeatTimeout은 하트비트 타임아웃
    HeartbeatTimeout = 90 * time.Second
)

// StartHeartbeatMonitor는 하트비트 모니터링을 시작한다
func (s *Session) StartHeartbeatMonitor(ctx context.Context) {
    ticker := time.NewTicker(10 * time.Second) // 10초마다 체크
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            s.checkHeartbeatTimeout()
        case <-ctx.Done():
            return
        case <-s.done:
            return
        }
    }
}

func (s *Session) checkHeartbeatTimeout() {
    lastActivity := s.GetLastActivity()
    elapsed := time.Since(lastActivity)
    
    if elapsed > HeartbeatTimeout {
        // 타임아웃 발생
        s.Close(CloseReasonTimeout)
    }
}

// OnHeartbeat는 하트비트 패킷 수신 시 호출된다
func (s *Session) OnHeartbeat() {
    s.UpdateActivity()
}
```

### 유휴 세션 정리

일정 시간 동안 활동이 없는 세션을 자동으로 정리한다.

```go
// IdleTimeoutManager는 유휴 세션을 관리한다
type IdleTimeoutManager struct {
    storage       *SessionStorage
    idleTimeout   time.Duration
    checkInterval time.Duration
    stopChan      chan struct{}
}

// NewIdleTimeoutManager는 새로운 유휴 타임아웃 관리자를 생성한다
func NewIdleTimeoutManager(storage *SessionStorage, idleTimeout time.Duration) *IdleTimeoutManager {
    return &IdleTimeoutManager{
        storage:       storage,
        idleTimeout:   idleTimeout,
        checkInterval: 60 * time.Second,
        stopChan:      make(chan struct{}),
    }
}

// Start는 유휴 세션 검사를 시작한다
func (m *IdleTimeoutManager) Start() {
    go m.run()
}

// Stop은 유휴 세션 검사를 중지한다
func (m *IdleTimeoutManager) Stop() {
    close(m.stopChan)
}

func (m *IdleTimeoutManager) run() {
    ticker := time.NewTicker(m.checkInterval)
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            m.checkIdleSessions()
        case <-m.stopChan:
            return
        }
    }
}

func (m *IdleTimeoutManager) checkIdleSessions() {
    now := time.Now()
    var toClose []*Session
    
    // 타임아웃된 세션을 찾는다
    m.storage.Range(func(session *Session) bool {
        if session.GetState() != StateActive {
            return true // 활성 상태가 아니면 건너뛴다
        }
        
        lastActivity := session.GetLastActivity()
        if now.Sub(lastActivity) > m.idleTimeout {
            toClose = append(toClose, session)
        }
        
        return true
    })
    
    // 찾은 세션들을 종료한다
    for _, session := range toClose {
        session.Close(CloseReasonIdle)
    }
    
    if len(toClose) > 0 {
        // 로그 기록
        println("Closed", len(toClose), "idle sessions")
    }
}
```

### 타임아웃 설정 관리

다양한 타임아웃 설정을 관리하는 구조체를 만든다.

```go
// TimeoutConfig는 타임아웃 설정을 담는다
type TimeoutConfig struct {
    // 읽기 타임아웃: 데이터 수신 대기 시간
    ReadTimeout time.Duration
    
    // 쓰기 타임아웃: 데이터 전송 대기 시간
    WriteTimeout time.Duration
    
    // 하트비트 타임아웃: 하트비트 미수신 허용 시간
    HeartbeatTimeout time.Duration
    
    // 인증 타임아웃: 연결 후 인증 완료까지 허용 시간
    AuthTimeout time.Duration
    
    // 유휴 타임아웃: 활동 없이 유지 가능한 시간
    IdleTimeout time.Duration
}

// DefaultTimeoutConfig는 기본 타임아웃 설정을 반환한다
func DefaultTimeoutConfig() TimeoutConfig {
    return TimeoutConfig{
        ReadTimeout:      3 * time.Minute,
        WriteTimeout:     30 * time.Second,
        HeartbeatTimeout: 90 * time.Second,
        AuthTimeout:      30 * time.Second,
        IdleTimeout:      10 * time.Minute,
    }
}

// ApplyToSession은 세션에 타임아웃 설정을 적용한다
func (tc TimeoutConfig) ApplyToSession(session *Session) {
    // TCP 연결에 타임아웃 설정
    if tc.ReadTimeout > 0 {
        session.conn.SetReadDeadline(time.Now().Add(tc.ReadTimeout))
    }
    if tc.WriteTimeout > 0 {
        session.conn.SetWriteDeadline(time.Now().Add(tc.WriteTimeout))
    }
}
```

## 14.5 재연결 처리

모바일 환경이나 불안정한 네트워크에서는 일시적인 연결 끊김이 빈번하다. 재연결 기능을 제공하여 사용자 경험을 개선한다.

### 재연결 토큰 생성

```go
package session

import (
    "crypto/rand"
    "encoding/base64"
    "sync"
    "time"
)

// ReconnectToken은 재연결 토큰 정보를 담는다
type ReconnectToken struct {
    Token      string
    UserID     uint64
    SessionID  uint64
    IssuedAt   time.Time
    ExpiresAt  time.Time
    State      interface{} // 복원할 상태 정보
}

// ReconnectManager는 재연결 토큰을 관리한다
type ReconnectManager struct {
    tokens    sync.Map // token -> *ReconnectToken
    ttl       time.Duration
}

// NewReconnectManager는 새로운 재연결 관리자를 생성한다
func NewReconnectManager(ttl time.Duration) *ReconnectManager {
    return &ReconnectManager{
        ttl: ttl,
    }
}

// GenerateToken은 재연결 토큰을 생성한다
func (rm *ReconnectManager) GenerateToken(session *Session) (string, error) {
    // 랜덤 토큰 생성
    tokenBytes := make([]byte, 32)
    if _, err := rand.Read(tokenBytes); err != nil {
        return "", err
    }
    tokenStr := base64.URLEncoding.EncodeToString(tokenBytes)
    
    // 토큰 정보 생성
    now := time.Now()
    token := &ReconnectToken{
        Token:     tokenStr,
        UserID:    session.GetUserID(),
        SessionID: session.ID(),
        IssuedAt:  now,
        ExpiresAt: now.Add(rm.ttl),
        State:     rm.captureSessionState(session),
    }
    
    rm.tokens.Store(tokenStr, token)
    return tokenStr, nil
}

// captureSessionState는 세션 상태를 캡처한다
func (rm *ReconnectManager) captureSessionState(session *Session) interface{} {
    return map[string]interface{}{
        "roomID":   session.GetRoomID(),
        "userID":   session.GetUserID(),
        "nickname": session.GetNickname(),
    }
}

// ValidateToken은 재연결 토큰을 검증한다
func (rm *ReconnectManager) ValidateToken(tokenStr string) (*ReconnectToken, bool) {
    value, ok := rm.tokens.Load(tokenStr)
    if !ok {
        return nil, false
    }
    
    token := value.(*ReconnectToken)
    
    // 만료 확인
    if time.Now().After(token.ExpiresAt) {
        rm.tokens.Delete(tokenStr)
        return nil, false
    }
    
    return token, true
}

// ConsumeToken은 토큰을 사용하고 삭제한다 (일회용)
func (rm *ReconnectManager) ConsumeToken(tokenStr string) (*ReconnectToken, bool) {
    value, ok := rm.tokens.LoadAndDelete(tokenStr)
    if !ok {
        return nil, false
    }
    
    token := value.(*ReconnectToken)
    
    // 만료 확인
    if time.Now().After(token.ExpiresAt) {
        return nil, false
    }
    
    return token, true
}

// CleanupExpired는 만료된 토큰을 정리한다
func (rm *ReconnectManager) CleanupExpired() {
    now := time.Now()
    var expired []string
    
    rm.tokens.Range(func(key, value interface{}) bool {
        token := value.(*ReconnectToken)
        if now.After(token.ExpiresAt) {
            expired = append(expired, key.(string))
        }
        return true
    })
    
    for _, tokenStr := range expired {
        rm.tokens.Delete(tokenStr)
    }
}

// StartCleanupRoutine은 주기적으로 만료된 토큰을 정리한다
func (rm *ReconnectManager) StartCleanupRoutine(interval time.Duration) chan struct{} {
    stopChan := make(chan struct{})
    
    go func() {
        ticker := time.NewTicker(interval)
        defer ticker.Stop()
        
        for {
            select {
            case <-ticker.C:
                rm.CleanupExpired()
            case <-stopChan:
                return
            }
        }
    }()
    
    return stopChan
}
```

### 재연결 처리 로직

```go
// HandleReconnect는 재연결 요청을 처리한다
func (ss *SessionStorage) HandleReconnect(
    newSession *Session,
    token string,
    reconnectMgr *ReconnectManager,
) error {
    // 토큰 검증 및 소비
    reconnectToken, ok := reconnectMgr.ConsumeToken(token)
    if !ok {
        return ErrInvalidReconnectToken
    }
    
    // 기존 세션 찾기
    oldSession, exists := ss.GetByUserID(reconnectToken.UserID)
    if exists {
        // 기존 세션 정리
        oldSession.Close(CloseReasonReconnected)
        ss.Remove(oldSession.ID())
    }
    
    // 새 세션에 상태 복원
    state := reconnectToken.State.(map[string]interface{})
    newSession.SetUserInfo(
        state["userID"].(uint64),
        state["nickname"].(string),
    )
    newSession.SetRoomID(state["roomID"].(uint64))
    
    // 새 세션 등록
    ss.Add(newSession)
    ss.BindUser(reconnectToken.UserID, newSession)
    
    return nil
}
```

### 클라이언트 재연결 흐름도

```
클라이언트                       서버
    │                             │
    │  1. 연결 끊김 감지           │
    │────────────────────────────▶│
    │                             │
    │  2. 재연결 시도              │
    │────────────────────────────▶│
    │                             │
    │  3. 재연결 토큰 전송         │
    │────────────────────────────▶│
    │                             │
    │                             │ 4. 토큰 검증
    │                             │
    │                             │ 5. 기존 세션 정리
    │                             │
    │                             │ 6. 새 세션 생성
    │                             │
    │                             │ 7. 상태 복원
    │                             │
    │  8. 재연결 성공 응답         │
    │◀────────────────────────────│
    │                             │
    │  9. 게임 재개                │
    │◀───────────────────────────▶│
    │                             │
```

### 세션 종료 이유 정의

```go
// CloseReason은 세션 종료 이유를 나타낸다
type CloseReason int

const (
    CloseReasonNormal CloseReason = iota
    CloseReasonTimeout
    CloseReasonIdle
    CloseReasonKicked
    CloseReasonReconnected
    CloseReasonError
)

// String은 종료 이유를 문자열로 반환한다
func (cr CloseReason) String() string {
    switch cr {
    case CloseReasonNormal:
        return "Normal"
    case CloseReasonTimeout:
        return "Timeout"
    case CloseReasonIdle:
        return "Idle"
    case CloseReasonKicked:
        return "Kicked"
    case CloseReasonReconnected:
        return "Reconnected"
    case CloseReasonError:
        return "Error"
    default:
        return "Unknown"
    }
}

// Close는 세션을 종료한다
func (s *Session) Close(reason CloseReason) error {
    var err error
    
    s.closeOnce.Do(func() {
        // 상태를 Closing으로 변경
        s.SetState(StateClosing)
        
        // 종료 신호 전송
        close(s.done)
        
        // TCP 연결 종료
        if s.conn != nil {
            err = s.conn.Close()
        }
        
        // 송신 채널 닫기
        close(s.sendChan)
        
        // 최종 상태로 변경
        s.SetState(StateClosed)
        
        // 로그 기록
        println("Session", s.ID(), "closed. Reason:", reason.String())
    })
    
    return err
}
```

### 완전한 세션 관리 예제

모든 기능을 통합한 완전한 예제다:

```go
package main

import (
    "context"
    "fmt"
    "net"
    "time"
    
    "yourproject/session"
)

func main() {
    // 세션 스토리지 생성
    storage := session.NewSessionStorage()
    
    // 재연결 관리자 생성 (토큰 유효기간: 5분)
    reconnectMgr := session.NewReconnectManager(5 * time.Minute)
    
    // 재연결 토큰 정리 루틴 시작 (1분마다)
    stopCleanup := reconnectMgr.StartCleanupRoutine(1 * time.Minute)
    defer close(stopCleanup)
    
    // 유휴 세션 관리자 생성 (유휴 시간: 10분)
    idleMgr := session.NewIdleTimeoutManager(storage, 10*time.Minute)
    idleMgr.Start()
    defer idleMgr.Stop()
    
    // TCP 리스너 시작
    listener, err := net.Listen("tcp", ":8080")
    if err != nil {
        panic(err)
    }
    defer listener.Close()
    
    fmt.Println("게임 서버 시작: :8080")
    
    ctx, cancel := context.WithCancel(context.Background())
    defer cancel()
    
    // 연결 수락 루프
    for {
        conn, err := listener.Accept()
        if err != nil {
            fmt.Println("Accept error:", err)
            continue
        }
        
        // 새 세션 생성
        sessionID := storage.GenerateSessionID()
        newSession := session.NewSession(sessionID, conn)
        
        // 세션 추가
        storage.Add(newSession)
        
        // 세션 처리 고루틴 시작
        go handleSession(ctx, newSession, storage, reconnectMgr)
    }
}

func handleSession(
    ctx context.Context,
    s *session.Session,
    storage *session.SessionStorage,
    reconnectMgr *session.ReconnectManager,
) {
    defer func() {
        s.Close(session.CloseReasonNormal)
        storage.Remove(s.ID())
    }()
    
    // 하트비트 모니터링 시작
    go s.StartHeartbeatMonitor(ctx)
    
    fmt.Printf("새 세션: %d, 주소: %s\n", s.ID(), s.RemoteAddr())
    
    // 세션 처리 로직
    // (실제로는 패킷 읽기/쓰기 로직이 들어간다)
    
    <-s.Done() // 세션 종료 대기
}
```

---

이 장에서는 게임 서버의 핵심인 세션 관리를 다루었다. 세션의 생명주기부터 동시성 안전한 관리, 타임아웃 처리, 재연결 기능까지 실전에서 필요한 모든 내용을 다루었다. 

다음 장에서는 여러 세션을 묶어 관리하는 방(Room) 시스템을 설계한다. 세션 관리에서 배운 동시성 패턴과 상태 관리 기법이 방 시스템에도 그대로 적용된다.



# Chapter 15. 방(Room) 시스템 설계

방(Room) 시스템은 멀티플레이어 게임 서버의 핵심 구성 요소다. 플레이어들이 모여서 게임을 진행하는 논리적 공간을 제공하며, 게임 상태를 관리하고 플레이어 간 상호작용을 중재한다. 이 장에서는 동시성 환경에서 안전하고 확장 가능한 방 시스템을 설계하고 구현한다.

## 15.1 방 생성/삭제/입장/퇴장

방 시스템의 기본 동작은 방의 생명주기 관리다. 방 생성부터 삭제까지의 전체 흐름을 이해하고 구현한다.

### 방의 생명주기

```
┌──────────────┐
│   Creating   │ ← 방 생성 요청
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Waiting    │ ← 플레이어 대기 중
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Playing    │ ← 게임 진행 중
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Ending     │ ← 게임 종료 처리 중
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Closed     │ ← 방 완전히 종료
└──────────────┘
```

### 기본 방 구조체

방의 핵심 정보를 담는 구조체를 설계한다:

```go
package room

import (
    "sync"
    "sync/atomic"
    "time"
    
    "yourproject/session"
)

// RoomState는 방의 현재 상태를 나타낸다
type RoomState int32

const (
    RoomStateWaiting RoomState = iota  // 대기 중
    RoomStatePlaying                    // 게임 진행 중
    RoomStateEnding                     // 종료 처리 중
    RoomStateClosed                     // 닫힘
)

// String은 방 상태를 문자열로 반환한다
func (rs RoomState) String() string {
    switch rs {
    case RoomStateWaiting:
        return "Waiting"
    case RoomStatePlaying:
        return "Playing"
    case RoomStateEnding:
        return "Ending"
    case RoomStateClosed:
        return "Closed"
    default:
        return "Unknown"
    }
}

// RoomConfig는 방 설정을 담는다
type RoomConfig struct {
    MaxPlayers    int           // 최대 플레이어 수
    MinPlayers    int           // 최소 플레이어 수 (게임 시작 조건)
    IsPrivate     bool          // 비공개 방 여부
    Password      string        // 비공개 방 비밀번호
    AutoStart     bool          // 자동 시작 여부
    WaitTimeout   time.Duration // 대기 타임아웃
}

// DefaultRoomConfig는 기본 방 설정을 반환한다
func DefaultRoomConfig() RoomConfig {
    return RoomConfig{
        MaxPlayers:  6,
        MinPlayers:  2,
        IsPrivate:   false,
        Password:    "",
        AutoStart:   true,
        WaitTimeout: 5 * time.Minute,
    }
}

// Room은 게임 방을 나타낸다
type Room struct {
    id         uint64                    // 방 고유 ID
    name       string                    // 방 이름
    config     RoomConfig                // 방 설정
    state      atomic.Int32              // 현재 상태
    
    // 플레이어 관리
    players    map[uint64]*session.Session  // userID -> Session
    playersMu  sync.RWMutex              // 플레이어 맵 보호
    maxPlayers int                       // 최대 플레이어 수
    
    // 방장 정보
    ownerID    uint64                    // 방장 사용자 ID
    
    // 타임스탬프
    createdAt  time.Time                 // 생성 시각
    startedAt  time.Time                 // 게임 시작 시각
    
    // 메시지 처리
    cmdChan    chan RoomCommand          // 명령 채널
    
    // 종료 관리
    done       chan struct{}             // 종료 신호
    closeOnce  sync.Once                 // 한 번만 닫기
    
    // 게임 상태 (인터페이스로 추상화)
    gameState  interface{}               // 게임별 상태 데이터
    gameStateMu sync.RWMutex             // 게임 상태 보호
}

// NewRoom은 새로운 방을 생성한다
func NewRoom(id uint64, name string, ownerID uint64, config RoomConfig) *Room {
    r := &Room{
        id:         id,
        name:       name,
        config:     config,
        players:    make(map[uint64]*session.Session),
        maxPlayers: config.MaxPlayers,
        ownerID:    ownerID,
        createdAt:  time.Now(),
        cmdChan:    make(chan RoomCommand, 100),
        done:       make(chan struct{}),
    }
    
    r.state.Store(int32(RoomStateWaiting))
    
    // 방 명령 처리 루프 시작
    go r.runCommandLoop()
    
    return r
}

// ID는 방 ID를 반환한다
func (r *Room) ID() uint64 {
    return r.id
}

// Name은 방 이름을 반환한다
func (r *Room) Name() string {
    return r.name
}

// GetState는 현재 방 상태를 반환한다
func (r *Room) GetState() RoomState {
    return RoomState(r.state.Load())
}

// SetState는 방 상태를 변경한다
func (r *Room) SetState(state RoomState) {
    r.state.Store(int32(state))
}

// IsWaiting은 대기 중인지 확인한다
func (r *Room) IsWaiting() bool {
    return r.GetState() == RoomStateWaiting
}

// IsPlaying은 게임 진행 중인지 확인한다
func (r *Room) IsPlaying() bool {
    return r.GetState() == RoomStatePlaying
}

// IsFull은 방이 가득 찼는지 확인한다
func (r *Room) IsFull() bool {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    return len(r.players) >= r.maxPlayers
}

// PlayerCount는 현재 플레이어 수를 반환한다
func (r *Room) PlayerCount() int {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    return len(r.players)
}
```

### 방 명령 시스템

방에 대한 모든 작업을 명령 패턴으로 처리하여 동시성 문제를 해결한다:

```go
// RoomCommandType은 방 명령 타입을 나타낸다
type RoomCommandType int

const (
    CmdJoin RoomCommandType = iota
    CmdLeave
    CmdStart
    CmdMessage
    CmdKick
    CmdClose
)

// RoomCommand는 방 명령을 나타낸다
type RoomCommand struct {
    Type     RoomCommandType
    Session  *session.Session
    Data     interface{}
    ResChan  chan<- interface{} // 응답 채널
}

// runCommandLoop는 방 명령을 순차적으로 처리한다
func (r *Room) runCommandLoop() {
    for {
        select {
        case cmd := <-r.cmdChan:
            r.handleCommand(cmd)
        case <-r.done:
            return
        }
    }
}

// handleCommand는 방 명령을 처리한다
func (r *Room) handleCommand(cmd RoomCommand) {
    switch cmd.Type {
    case CmdJoin:
        err := r.handleJoin(cmd.Session, cmd.Data)
        if cmd.ResChan != nil {
            cmd.ResChan <- err
        }
        
    case CmdLeave:
        err := r.handleLeave(cmd.Session)
        if cmd.ResChan != nil {
            cmd.ResChan <- err
        }
        
    case CmdStart:
        err := r.handleStart(cmd.Session)
        if cmd.ResChan != nil {
            cmd.ResChan <- err
        }
        
    case CmdMessage:
        r.handleMessage(cmd.Session, cmd.Data)
        
    case CmdKick:
        targetID := cmd.Data.(uint64)
        err := r.handleKick(cmd.Session, targetID)
        if cmd.ResChan != nil {
            cmd.ResChan <- err
        }
        
    case CmdClose:
        r.handleClose()
    }
}
```

### 입장 처리

플레이어가 방에 입장하는 로직을 구현한다:

```go
// Join은 플레이어를 방에 입장시킨다 (비동기)
func (r *Room) Join(s *session.Session, password string) error {
    // 응답 채널 생성
    resChan := make(chan interface{}, 1)
    
    // 명령 전송
    r.cmdChan <- RoomCommand{
        Type:    CmdJoin,
        Session: s,
        Data:    password,
        ResChan: resChan,
    }
    
    // 응답 대기
    res := <-resChan
    if err, ok := res.(error); ok {
        return err
    }
    return nil
}

// handleJoin은 입장 요청을 실제로 처리한다
func (r *Room) handleJoin(s *session.Session, data interface{}) error {
    // 상태 확인
    if r.GetState() == RoomStateClosed {
        return ErrRoomClosed
    }
    
    if r.GetState() == RoomStatePlaying && !r.config.AllowJoinInProgress {
        return ErrGameInProgress
    }
    
    // 인원 확인
    if r.IsFull() {
        return ErrRoomFull
    }
    
    // 비밀번호 확인 (비공개 방인 경우)
    if r.config.IsPrivate {
        password, ok := data.(string)
        if !ok || password != r.config.Password {
            return ErrInvalidPassword
        }
    }
    
    userID := s.GetUserID()
    
    // 이미 입장한 플레이어인지 확인
    r.playersMu.RLock()
    _, exists := r.players[userID]
    r.playersMu.RUnlock()
    
    if exists {
        return ErrAlreadyInRoom
    }
    
    // 플레이어 추가
    r.playersMu.Lock()
    r.players[userID] = s
    r.playersMu.Unlock()
    
    // 세션에 방 ID 설정
    s.SetRoomID(r.id)
    
    // 입장 알림 브로드캐스트
    r.broadcastJoin(s)
    
    // 자동 시작 조건 확인
    if r.config.AutoStart && r.PlayerCount() >= r.config.MinPlayers {
        go r.checkAutoStart()
    }
    
    return nil
}

// broadcastJoin은 플레이어 입장을 모든 플레이어에게 알린다
func (r *Room) broadcastJoin(s *session.Session) {
    userID, nickname := s.GetUserInfo()
    
    // 입장 메시지 생성
    joinMsg := map[string]interface{}{
        "type":     "player_joined",
        "userID":   userID,
        "nickname": nickname,
        "count":    r.PlayerCount(),
    }
    
    // 직렬화 (실제로는 프로토콜에 맞게 직렬화)
    data := serializeMessage(joinMsg)
    
    // 브로드캐스트
    r.Broadcast(data, 0) // 0은 제외할 사용자 없음
}
```

### 퇴장 처리

플레이어가 방에서 나가는 로직을 구현한다:

```go
// Leave는 플레이어를 방에서 퇴장시킨다
func (r *Room) Leave(s *session.Session) error {
    resChan := make(chan interface{}, 1)
    
    r.cmdChan <- RoomCommand{
        Type:    CmdLeave,
        Session: s,
        ResChan: resChan,
    }
    
    res := <-resChan
    if err, ok := res.(error); ok {
        return err
    }
    return nil
}

// handleLeave는 퇴장 요청을 실제로 처리한다
func (r *Room) handleLeave(s *session.Session) error {
    userID := s.GetUserID()
    
    // 플레이어 존재 확인
    r.playersMu.Lock()
    _, exists := r.players[userID]
    if !exists {
        r.playersMu.Unlock()
        return ErrNotInRoom
    }
    
    // 플레이어 제거
    delete(r.players, userID)
    playerCount := len(r.players)
    r.playersMu.Unlock()
    
    // 세션의 방 ID 초기화
    s.SetRoomID(0)
    
    // 퇴장 알림 브로드캐스트
    r.broadcastLeave(userID)
    
    // 방장이 나간 경우 처리
    if userID == r.ownerID {
        r.handleOwnerLeft(playerCount)
    }
    
    // 방이 비었으면 자동 삭제
    if playerCount == 0 {
        r.Close()
    }
    
    return nil
}

// handleOwnerLeft는 방장이 퇴장한 경우를 처리한다
func (r *Room) handleOwnerLeft(remainingCount int) {
    if remainingCount == 0 {
        return
    }
    
    // 새 방장 선정 (가장 먼저 입장한 플레이어)
    r.playersMu.Lock()
    defer r.playersMu.Unlock()
    
    for userID := range r.players {
        r.ownerID = userID
        break
    }
    
    // 방장 변경 알림
    r.broadcastOwnerChanged(r.ownerID)
}

// broadcastLeave는 플레이어 퇴장을 알린다
func (r *Room) broadcastLeave(userID uint64) {
    leaveMsg := map[string]interface{}{
        "type":   "player_left",
        "userID": userID,
        "count":  r.PlayerCount(),
    }
    
    data := serializeMessage(leaveMsg)
    r.Broadcast(data, 0)
}

// broadcastOwnerChanged는 방장 변경을 알린다
func (r *Room) broadcastOwnerChanged(newOwnerID uint64) {
    msg := map[string]interface{}{
        "type":    "owner_changed",
        "ownerID": newOwnerID,
    }
    
    data := serializeMessage(msg)
    r.Broadcast(data, 0)
}
```

### 방 삭제

방을 안전하게 삭제하는 로직을 구현한다:

```go
// Close는 방을 닫는다
func (r *Room) Close() {
    r.closeOnce.Do(func() {
        // 상태 변경
        r.SetState(RoomStateClosed)
        
        // 모든 플레이어를 퇴장시킨다
        r.playersMu.Lock()
        players := make([]*session.Session, 0, len(r.players))
        for _, s := range r.players {
            players = append(players, s)
        }
        r.playersMu.Unlock()
        
        // 각 플레이어에게 방 닫힘 알림
        closeMsg := map[string]interface{}{
            "type": "room_closed",
        }
        data := serializeMessage(closeMsg)
        
        for _, s := range players {
            s.Send(data)
            s.SetRoomID(0)
        }
        
        // 플레이어 맵 비우기
        r.playersMu.Lock()
        r.players = make(map[uint64]*session.Session)
        r.playersMu.Unlock()
        
        // 명령 처리 루프 종료
        close(r.done)
    })
}

// handleClose는 방 닫기 명령을 처리한다
func (r *Room) handleClose() {
    r.Close()
}
```

### 에러 타입 정의

```go
package room

import "errors"

var (
    ErrRoomFull          = errors.New("room is full")
    ErrRoomClosed        = errors.New("room is closed")
    ErrGameInProgress    = errors.New("game is in progress")
    ErrInvalidPassword   = errors.New("invalid password")
    ErrAlreadyInRoom     = errors.New("already in room")
    ErrNotInRoom         = errors.New("not in room")
    ErrNotOwner          = errors.New("not room owner")
    ErrNotEnoughPlayers  = errors.New("not enough players")
    ErrInvalidState      = errors.New("invalid room state")
)
```

## 15.2 방 상태 관리

방의 상태는 게임 진행에 따라 변경된다. 상태 전이를 안전하게 관리하고, 각 상태에서 허용되는 동작을 제한한다.

### 상태 전이 다이어그램

```
                    ┌──────────────┐
                    │   Waiting    │
                    └───────┬──────┘
                            │
              MinPlayers 이상 입장 &
                   Start 명령
                            │
                            ▼
                    ┌──────────────┐
                    │   Playing    │
                    └───────┬──────┘
                            │
                   게임 종료 조건
                            │
                            ▼
                    ┌──────────────┐
                    │    Ending    │
                    └───────┬──────┘
                            │
                     결과 처리 완료
                            │
                            ▼
                    ┌──────────────┐
                    │    Closed    │
                    └──────────────┘
```

### 상태 전이 검증

```go
// TransitionState는 상태 전이가 유효한지 확인하고 변경한다
func (r *Room) TransitionState(from, to RoomState) error {
    current := r.GetState()
    
    // 현재 상태 확인
    if current != from {
        return ErrInvalidState
    }
    
    // 유효한 상태 전이인지 확인
    if !r.isValidTransition(from, to) {
        return ErrInvalidState
    }
    
    // 상태 변경
    r.SetState(to)
    
    // 상태 변경 이벤트 발생
    r.onStateChanged(from, to)
    
    return nil
}

// isValidTransition은 유효한 상태 전이인지 확인한다
func (r *Room) isValidTransition(from, to RoomState) bool {
    validTransitions := map[RoomState][]RoomState{
        RoomStateWaiting: {RoomStatePlaying, RoomStateClosed},
        RoomStatePlaying: {RoomStateEnding, RoomStateClosed},
        RoomStateEnding:  {RoomStateWaiting, RoomStateClosed}, // 재시작 가능
        RoomStateClosed:  {}, // 닫힌 후에는 전이 불가
    }
    
    allowed, exists := validTransitions[from]
    if !exists {
        return false
    }
    
    for _, state := range allowed {
        if state == to {
            return true
        }
    }
    
    return false
}

// onStateChanged는 상태 변경 시 호출된다
func (r *Room) onStateChanged(from, to RoomState) {
    // 상태 변경 로그
    println("Room", r.id, "state changed:", from.String(), "->", to.String())
    
    // 상태별 처리
    switch to {
    case RoomStatePlaying:
        r.startedAt = time.Now()
        r.broadcastGameStart()
        
    case RoomStateEnding:
        r.broadcastGameEnd()
        
    case RoomStateClosed:
        // 방 닫기는 이미 Close()에서 처리됨
    }
}
```

### 게임 시작 처리

```go
// Start는 게임을 시작한다
func (r *Room) Start(s *session.Session) error {
    resChan := make(chan interface{}, 1)
    
    r.cmdChan <- RoomCommand{
        Type:    CmdStart,
        Session: s,
        ResChan: resChan,
    }
    
    res := <-resChan
    if err, ok := res.(error); ok {
        return err
    }
    return nil
}

// handleStart는 게임 시작 요청을 처리한다
func (r *Room) handleStart(s *session.Session) error {
    // 방장 확인
    if s.GetUserID() != r.ownerID {
        return ErrNotOwner
    }
    
    // 상태 확인
    if r.GetState() != RoomStateWaiting {
        return ErrInvalidState
    }
    
    // 최소 인원 확인
    if r.PlayerCount() < r.config.MinPlayers {
        return ErrNotEnoughPlayers
    }
    
    // 상태 전이
    if err := r.TransitionState(RoomStateWaiting, RoomStatePlaying); err != nil {
        return err
    }
    
    // 게임 상태 초기화
    r.initGameState()
    
    return nil
}

// initGameState는 게임 상태를 초기화한다
func (r *Room) initGameState() {
    r.gameStateMu.Lock()
    defer r.gameStateMu.Unlock()
    
    // 게임별 상태 초기화 로직
    // 포커 게임의 경우: 카드 섞기, 칩 분배 등
    r.gameState = r.createInitialGameState()
}

// createInitialGameState는 초기 게임 상태를 생성한다
// (게임 종류에 따라 구현이 달라진다)
func (r *Room) createInitialGameState() interface{} {
    // 포커 게임 예시
    return map[string]interface{}{
        "deck":         shuffleDeck(),
        "pot":          0,
        "currentBet":   0,
        "dealerIndex":  0,
        "currentTurn":  0,
    }
}

// broadcastGameStart는 게임 시작을 알린다
func (r *Room) broadcastGameStart() {
    msg := map[string]interface{}{
        "type": "game_start",
        "time": r.startedAt.Unix(),
    }
    
    data := serializeMessage(msg)
    r.Broadcast(data, 0)
}

// broadcastGameEnd는 게임 종료를 알린다
func (r *Room) broadcastGameEnd() {
    msg := map[string]interface{}{
        "type": "game_end",
    }
    
    data := serializeMessage(msg)
    r.Broadcast(data, 0)
}
```

### 자동 시작 기능

```go
// checkAutoStart는 자동 시작 조건을 확인한다
func (r *Room) checkAutoStart() {
    // 약간의 지연을 두어 연속 입장 처리
    time.Sleep(2 * time.Second)
    
    // 상태 재확인
    if r.GetState() != RoomStateWaiting {
        return
    }
    
    if r.PlayerCount() >= r.config.MinPlayers {
        // 자동 시작 (방장 권한으로)
        r.playersMu.RLock()
        ownerSession := r.players[r.ownerID]
        r.playersMu.RUnlock()
        
        if ownerSession != nil {
            r.Start(ownerSession)
        }
    }
}
```

## 15.3 방 내 브로드캐스팅

방 안의 모든 플레이어 또는 특정 플레이어에게 메시지를 전송하는 기능을 구현한다.

### 기본 브로드캐스트

```go
// Broadcast는 방 안의 모든 플레이어에게 메시지를 전송한다
func (r *Room) Broadcast(data []byte, excludeUserID uint64) {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    for userID, s := range r.players {
        // 제외 대상이면 건너뛴다
        if userID == excludeUserID {
            continue
        }
        
        // 비동기 전송 (블로킹 방지)
        go func(session *session.Session) {
            if err := session.Send(data); err != nil {
                // 전송 실패 로그 (세션이 이미 닫혔을 수 있음)
                println("Broadcast failed to user", session.GetUserID(), ":", err.Error())
            }
        }(s)
    }
}

// BroadcastExcept는 특정 플레이어를 제외하고 브로드캐스트한다
func (r *Room) BroadcastExcept(data []byte, excludeUserIDs ...uint64) {
    excludeMap := make(map[uint64]bool)
    for _, id := range excludeUserIDs {
        excludeMap[id] = true
    }
    
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    for userID, s := range r.players {
        if excludeMap[userID] {
            continue
        }
        
        go func(session *session.Session) {
            session.Send(data)
        }(s)
    }
}

// SendTo는 특정 플레이어에게만 메시지를 전송한다
func (r *Room) SendTo(userID uint64, data []byte) error {
    r.playersMu.RLock()
    s, exists := r.players[userID]
    r.playersMu.RUnlock()
    
    if !exists {
        return ErrNotInRoom
    }
    
    return s.Send(data)
}

// SendToMultiple은 여러 플레이어에게 메시지를 전송한다
func (r *Room) SendToMultiple(userIDs []uint64, data []byte) {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    for _, userID := range userIDs {
        if s, exists := r.players[userID]; exists {
            go func(session *session.Session) {
                session.Send(data)
            }(s)
        }
    }
}
```

### 조건부 브로드캐스트

특정 조건을 만족하는 플레이어에게만 메시지를 전송한다:

```go
// BroadcastIf는 조건을 만족하는 플레이어에게만 브로드캐스트한다
func (r *Room) BroadcastIf(data []byte, predicate func(*session.Session) bool) {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    for _, s := range r.players {
        if predicate(s) {
            go func(session *session.Session) {
                session.Send(data)
            }(s)
        }
    }
}

// 사용 예시: 특정 팀에만 브로드캐스트
func (r *Room) BroadcastToTeam(data []byte, teamID int) {
    r.BroadcastIf(data, func(s *session.Session) bool {
        // 세션에서 팀 정보를 가져온다 (게임 상태에 저장되어 있다고 가정)
        playerTeam := r.getPlayerTeam(s.GetUserID())
        return playerTeam == teamID
    })
}

// getPlayerTeam은 플레이어의 팀을 반환한다 (예시)
func (r *Room) getPlayerTeam(userID uint64) int {
    r.gameStateMu.RLock()
    defer r.gameStateMu.RUnlock()
    
    if r.gameState == nil {
        return -1
    }
    
    state := r.gameState.(map[string]interface{})
    teams := state["teams"].(map[uint64]int)
    return teams[userID]
}
```

### 순차 브로드캐스트

메시지 순서가 중요한 경우 사용한다:

```go
// BroadcastSequential은 순차적으로 브로드캐스트한다
// 모든 전송이 완료될 때까지 기다린다
func (r *Room) BroadcastSequential(data []byte, excludeUserID uint64) error {
    r.playersMu.RLock()
    players := make([]*session.Session, 0, len(r.players))
    for userID, s := range r.players {
        if userID != excludeUserID {
            players = append(players, s)
        }
    }
    r.playersMu.RUnlock()
    
    // 순차적으로 전송
    for _, s := range players {
        if err := s.SendBlocking(data); err != nil {
            // 에러가 발생해도 계속 진행
            println("Sequential broadcast error:", err.Error())
        }
    }
    
    return nil
}
```

### 브로드캐스트 성능 최적화

대규모 방에서 브로드캐스트 성능을 개선한다:

```go
// BroadcastOptimized는 최적화된 브로드캐스트를 수행한다
func (r *Room) BroadcastOptimized(data []byte, excludeUserID uint64) {
    // 데이터 복사본을 미리 만들지 않고 원본을 공유한다
    // (데이터가 불변이라고 가정)
    
    r.playersMu.RLock()
    playerCount := len(r.players)
    
    if playerCount == 0 {
        r.playersMu.RUnlock()
        return
    }
    
    // WaitGroup을 사용하여 모든 전송 완료를 추적할 수 있다
    // (필요한 경우)
    var wg sync.WaitGroup
    wg.Add(playerCount)
    
    for userID, s := range r.players {
        if userID == excludeUserID {
            wg.Done()
            continue
        }
        
        go func(session *session.Session) {
            defer wg.Done()
            session.Send(data)
        }(s)
    }
    r.playersMu.RUnlock()
    
    // 필요하면 대기
    // wg.Wait()
}

// BroadcastWithPool은 고루틴 풀을 사용하여 브로드캐스트한다
type BroadcastTask struct {
    Session *session.Session
    Data    []byte
}

// 전역 브로드캐스트 워커 풀
var broadcastWorkerPool = make(chan BroadcastTask, 1000)

func init() {
    // 브로드캐스트 워커 시작
    for i := 0; i < 10; i++ {
        go broadcastWorker()
    }
}

func broadcastWorker() {
    for task := range broadcastWorkerPool {
        task.Session.Send(task.Data)
    }
}

// BroadcastWithPool은 워커 풀을 사용하여 브로드캐스트한다
func (r *Room) BroadcastWithPool(data []byte, excludeUserID uint64) {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    for userID, s := range r.players {
        if userID == excludeUserID {
            continue
        }
        
        select {
        case broadcastWorkerPool <- BroadcastTask{
            Session: s,
            Data:    data,
        }:
        default:
            // 풀이 가득 찬 경우 직접 전송
            go s.Send(data)
        }
    }
}
```

## 15.4 동시성 제어

여러 고루틴이 동시에 방에 접근할 때 발생하는 경쟁 조건을 방지한다.

### 명령 기반 동시성 제어

앞서 구현한 명령 채널 패턴은 가장 안전한 동시성 제어 방법이다:

```go
// 모든 방 변경 작업은 명령 채널을 통해 순차 처리된다
// 이로 인해 경쟁 조건이 원천적으로 차단된다

// 예: 동시에 여러 플레이어가 입장 시도
func concurrentJoinExample() {
    room := NewRoom(1, "TestRoom", 100, DefaultRoomConfig())
    
    // 100명이 동시에 입장 시도
    var wg sync.WaitGroup
    for i := 0; i < 100; i++ {
        wg.Add(1)
        go func(userID uint64) {
            defer wg.Done()
            
            // 가상의 세션 생성
            session := createMockSession(userID)
            
            // 입장 시도 (내부적으로 명령 채널 사용)
            err := room.Join(session, "")
            if err != nil {
                println("Join failed:", err.Error())
            }
        }(uint64(i))
    }
    
    wg.Wait()
    
    // 결과 확인
    println("Final player count:", room.PlayerCount())
}
```

### 읽기 작업 최적화

읽기 작업은 RWMutex를 사용하여 동시 접근을 허용한다:

```go
// GetPlayers는 현재 플레이어 목록을 반환한다
func (r *Room) GetPlayers() []uint64 {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    players := make([]uint64, 0, len(r.players))
    for userID := range r.players {
        players = append(players, userID)
    }
    
    return players
}

// GetPlayerInfo는 플레이어 정보를 반환한다
func (r *Room) GetPlayerInfo(userID uint64) (map[string]interface{}, error) {
    r.playersMu.RLock()
    s, exists := r.players[userID]
    r.playersMu.RUnlock()
    
    if !exists {
        return nil, ErrNotInRoom
    }
    
    uid, nickname := s.GetUserInfo()
    return map[string]interface{}{
        "userID":   uid,
        "nickname": nickname,
    }, nil
}

// GetAllPlayerInfo는 모든 플레이어 정보를 반환한다
func (r *Room) GetAllPlayerInfo() []map[string]interface{} {
    r.playersMu.RLock()
    defer r.playersMu.RUnlock()
    
    infos := make([]map[string]interface{}, 0, len(r.players))
    
    for _, s := range r.players {
        userID, nickname := s.GetUserInfo()
        infos = append(infos, map[string]interface{}{
            "userID":   userID,
            "nickname": nickname,
        })
    }
    
    return infos
}
```

### 게임 상태 동시성 제어

게임 상태는 별도의 뮤텍스로 보호한다:

```go
// UpdateGameState는 게임 상태를 안전하게 업데이트한다
func (r *Room) UpdateGameState(updater func(state interface{}) interface{}) {
    r.gameStateMu.Lock()
    defer r.gameStateMu.Unlock()
    
    r.gameState = updater(r.gameState)
}

// GetGameState는 게임 상태의 복사본을 반환한다
func (r *Room) GetGameState() interface{} {
    r.gameStateMu.RLock()
    defer r.gameStateMu.RUnlock()
    
    // 깊은 복사를 수행해야 한다 (상태에 따라 다름)
    return deepCopy(r.gameState)
}

// 사용 예시: 포커 게임에서 팟 증가
func (r *Room) AddToPot(amount int) {
    r.UpdateGameState(func(state interface{}) interface{} {
        s := state.(map[string]interface{})
        currentPot := s["pot"].(int)
        s["pot"] = currentPot + amount
        return s
    })
}
```

### 데드락 방지

여러 잠금을 동시에 사용할 때 일관된 순서를 유지한다:

```go
// 잠금 순서: playersMu -> gameStateMu
// 항상 이 순서를 지켜야 데드락을 방지할 수 있다

// 좋은 예
func (r *Room) safeOperation() {
    r.playersMu.Lock()
    defer r.playersMu.Unlock()
    
    r.gameStateMu.Lock()
    defer r.gameStateMu.Unlock()
    
    // 작업 수행
}

// 나쁜 예 - 데드락 가능
func (r *Room) unsafeOperation() {
    r.gameStateMu.Lock()  // 잘못된 순서!
    defer r.gameStateMu.Unlock()
    
    r.playersMu.Lock()
    defer r.playersMu.Unlock()
    
    // 작업 수행
}
```

## 15.5 매치메이킹 기초

플레이어를 자동으로 방에 배치하는 매치메이킹 시스템의 기초를 구현한다.

### 방 관리자 구조체

```go
package room

import (
    "sync"
    "sync/atomic"
    
    "yourproject/session"
)

// RoomManager는 모든 방을 관리한다
type RoomManager struct {
    rooms        sync.Map       // roomID -> *Room
    nextRoomID   atomic.Uint64  // 다음 방 ID
    roomCount    atomic.Int64   // 현재 방 수
    
    // 매치메이킹용
    waitingRooms sync.Map       // 대기 중인 방들 (config hash -> []*Room)
}

// NewRoomManager는 새로운 방 관리자를 생성한다
func NewRoomManager() *RoomManager {
    return &RoomManager{}
}

// CreateRoom은 새로운 방을 생성한다
func (rm *RoomManager) CreateRoom(name string, ownerID uint64, config RoomConfig) *Room {
    roomID := rm.nextRoomID.Add(1)
    room := NewRoom(roomID, name, ownerID, config)
    
    rm.rooms.Store(roomID, room)
    rm.roomCount.Add(1)
    
    // 공개 방이고 대기 중이면 매치메이킹 풀에 추가
    if !config.IsPrivate && room.IsWaiting() {
        rm.addToWaitingPool(room, config)
    }
    
    return room
}

// GetRoom은 방 ID로 방을 조회한다
func (rm *RoomManager) GetRoom(roomID uint64) (*Room, bool) {
    if room, ok := rm.rooms.Load(roomID); ok {
        return room.(*Room), true
    }
    return nil, false
}

// RemoveRoom은 방을 제거한다
func (rm *RoomManager) RemoveRoom(roomID uint64) {
    if room, ok := rm.rooms.LoadAndDelete(roomID); ok {
        rm.roomCount.Add(-1)
        
        // 대기 풀에서도 제거
        r := room.(*Room)
        rm.removeFromWaitingPool(r)
        
        // 방 닫기
        r.Close()
    }
}

// RoomCount는 현재 방 개수를 반환한다
func (rm *RoomManager) RoomCount() int64 {
    return rm.roomCount.Load()
}
```

### 매치메이킹 구현

```go
// configHash는 방 설정의 해시를 생성한다
func configHash(config RoomConfig) string {
    return fmt.Sprintf("%d_%d_%v", config.MaxPlayers, config.MinPlayers, config.AutoStart)
}

// addToWaitingPool은 방을 대기 풀에 추가한다
func (rm *RoomManager) addToWaitingPool(room *Room, config RoomConfig) {
    hash := configHash(config)
    
    // 기존 리스트 가져오기
    var rooms []*Room
    if value, ok := rm.waitingRooms.Load(hash); ok {
        rooms = value.([]*Room)
    }
    
    // 방 추가
    rooms = append(rooms, room)
    rm.waitingRooms.Store(hash, rooms)
}

// removeFromWaitingPool은 대기 풀에서 방을 제거한다
func (rm *RoomManager) removeFromWaitingPool(room *Room) {
    hash := configHash(room.config)
    
    if value, ok := rm.waitingRooms.Load(hash); ok {
        rooms := value.([]*Room)
        
        // 방 제거
        filtered := make([]*Room, 0, len(rooms))
        for _, r := range rooms {
            if r.ID() != room.ID() {
                filtered = append(filtered, r)
            }
        }
        
        if len(filtered) > 0 {
            rm.waitingRooms.Store(hash, filtered)
        } else {
            rm.waitingRooms.Delete(hash)
        }
    }
}

// FindMatchingRoom은 매치메이킹을 위한 적합한 방을 찾는다
func (rm *RoomManager) FindMatchingRoom(config RoomConfig) *Room {
    hash := configHash(config)
    
    if value, ok := rm.waitingRooms.Load(hash); ok {
        rooms := value.([]*Room)
        
        // 가장 많은 플레이어가 있으면서 가득 차지 않은 방 찾기
        var bestRoom *Room
        maxPlayers := 0
        
        for _, room := range rooms {
            if room.IsFull() || !room.IsWaiting() {
                continue
            }
            
            playerCount := room.PlayerCount()
            if playerCount > maxPlayers {
                maxPlayers = playerCount
                bestRoom = room
            }
        }
        
        return bestRoom
    }
    
    return nil
}

// QuickMatch는 빠른 매칭을 시도한다
func (rm *RoomManager) QuickMatch(s *session.Session, config RoomConfig) (*Room, error) {
    // 1. 기존 방 찾기
    room := rm.FindMatchingRoom(config)
    
    if room != nil {
        // 기존 방에 입장 시도
        err := room.Join(s, "")
        if err == nil {
            return room, nil
        }
        // 입장 실패 시 새 방 생성으로 이어진다
    }
    
    // 2. 새 방 생성
    userID, nickname := s.GetUserInfo()
    roomName := fmt.Sprintf("%s's Room", nickname)
    newRoom := rm.CreateRoom(roomName, userID, config)
    
    // 3. 방에 입장
    err := newRoom.Join(s, "")
    if err != nil {
        rm.RemoveRoom(newRoom.ID())
        return nil, err
    }
    
    return newRoom, nil
}
```

### 방 목록 조회

```go
// RoomListFilter는 방 목록 필터 조건을 나타낸다
type RoomListFilter struct {
    IncludeFull    bool
    IncludePlaying bool
    IncludePrivate bool
    MaxResults     int
}

// GetRoomList는 조건에 맞는 방 목록을 반환한다
func (rm *RoomManager) GetRoomList(filter RoomListFilter) []RoomInfo {
    var result []RoomInfo
    
    rm.rooms.Range(func(key, value interface{}) bool {
        room := value.(*Room)
        
        // 필터 적용
        if !filter.IncludeFull && room.IsFull() {
            return true
        }
        
        if !filter.IncludePlaying && room.IsPlaying() {
            return true
        }
        
        if !filter.IncludePrivate && room.config.IsPrivate {
            return true
        }
        
        // 방 정보 추가
        result = append(result, RoomInfo{
            ID:            room.ID(),
            Name:          room.Name(),
            PlayerCount:   room.PlayerCount(),
            MaxPlayers:    room.config.MaxPlayers,
            State:         room.GetState(),
            IsPrivate:     room.config.IsPrivate,
        })
        
        // 최대 결과 수 확인
        if filter.MaxResults > 0 && len(result) >= filter.MaxResults {
            return false // 순회 중단
        }
        
        return true
    })
    
    return result
}

// RoomInfo는 방 정보를 나타낸다
type RoomInfo struct {
    ID          uint64
    Name        string
    PlayerCount int
    MaxPlayers  int
    State       RoomState
    IsPrivate   bool
}
```

### 매치메이킹 큐 시스템

더 정교한 매치메이킹을 위한 큐 시스템을 구현한다:

```go
// MatchmakingQueue는 매치메이킹 큐를 나타낸다
type MatchmakingQueue struct {
    mu       sync.Mutex
    queue    []*MatchRequest
    roomMgr  *RoomManager
    stopChan chan struct{}
}

// MatchRequest는 매치 요청을 나타낸다
type MatchRequest struct {
    Session    *session.Session
    Config     RoomConfig
    EnqueuedAt time.Time
    ResChan    chan *Room
}

// NewMatchmakingQueue는 새로운 매치메이킹 큐를 생성한다
func NewMatchmakingQueue(roomMgr *RoomManager) *MatchmakingQueue {
    mq := &MatchmakingQueue{
        queue:    make([]*MatchRequest, 0),
        roomMgr:  roomMgr,
        stopChan: make(chan struct{}),
    }
    
    // 매치메이킹 루프 시작
    go mq.runMatchmakingLoop()
    
    return mq
}

// Enqueue는 매치 요청을 큐에 추가한다
func (mq *MatchmakingQueue) Enqueue(s *session.Session, config RoomConfig) <-chan *Room {
    resChan := make(chan *Room, 1)
    
    req := &MatchRequest{
        Session:    s,
        Config:     config,
        EnqueuedAt: time.Now(),
        ResChan:    resChan,
    }
    
    mq.mu.Lock()
    mq.queue = append(mq.queue, req)
    mq.mu.Unlock()
    
    return resChan
}

// runMatchmakingLoop는 매치메이킹 루프를 실행한다
func (mq *MatchmakingQueue) runMatchmakingLoop() {
    ticker := time.NewTicker(1 * time.Second)
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            mq.processQueue()
        case <-mq.stopChan:
            return
        }
    }
}

// processQueue는 큐를 처리한다
func (mq *MatchmakingQueue) processQueue() {
    mq.mu.Lock()
    if len(mq.queue) == 0 {
        mq.mu.Unlock()
        return
    }
    
    // 큐에서 요청 가져오기
    requests := mq.queue
    mq.queue = make([]*MatchRequest, 0)
    mq.mu.Unlock()
    
    // 설정별로 그룹화
    groups := make(map[string][]*MatchRequest)
    for _, req := range requests {
        hash := configHash(req.Config)
        groups[hash] = append(groups[hash], req)
    }
    
    // 각 그룹 처리
    for _, group := range groups {
        mq.matchGroup(group)
    }
}

// matchGroup은 같은 설정을 가진 요청들을 매칭한다
func (mq *MatchmakingQueue) matchGroup(requests []*MatchRequest) {
    if len(requests) == 0 {
        return
    }
    
    config := requests[0].Config
    
    // 기존 방 찾기
    room := mq.roomMgr.FindMatchingRoom(config)
    
    for _, req := range requests {
        // 방이 없거나 가득 찼으면 새 방 생성
        if room == nil || room.IsFull() {
            userID, nickname := req.Session.GetUserInfo()
            roomName := fmt.Sprintf("%s's Room", nickname)
            room = mq.roomMgr.CreateRoom(roomName, userID, config)
        }
        
        // 방에 입장
        err := room.Join(req.Session, "")
        if err == nil {
            req.ResChan <- room
        } else {
            req.ResChan <- nil
        }
        
        close(req.ResChan)
    }
}

// Stop은 매치메이킹 큐를 중지한다
func (mq *MatchmakingQueue) Stop() {
    close(mq.stopChan)
}
```

### 통합 예제

모든 기능을 통합한 완전한 예제다:

```go
package main

import (
    "fmt"
    "time"
    
    "yourproject/room"
    "yourproject/session"
)

func main() {
    // 방 관리자 생성
    roomMgr := room.NewRoomManager()
    
    // 매치메이킹 큐 생성
    matchQueue := room.NewMatchmakingQueue(roomMgr)
    defer matchQueue.Stop()
    
    // 기본 방 설정
    config := room.DefaultRoomConfig()
    config.MaxPlayers = 4
    config.MinPlayers = 2
    
    // 시뮬레이션: 10명의 플레이어가 동시에 매치메이킹 요청
    for i := 0; i < 10; i++ {
        go func(playerID uint64) {
            // 가상 세션 생성
            s := createMockSession(playerID)
            
            // 매치메이킹 요청
            roomChan := matchQueue.Enqueue(s, config)
            
            // 결과 대기
            matchedRoom := <-roomChan
            if matchedRoom != nil {
                fmt.Printf("Player %d matched to room %d\n", 
                    playerID, matchedRoom.ID())
            } else {
                fmt.Printf("Player %d matching failed\n", playerID)
            }
        }(uint64(i))
    }
    
    // 결과 확인을 위한 대기
    time.Sleep(5 * time.Second)
    
    // 방 목록 조회
    filter := room.RoomListFilter{
        IncludeFull:    true,
        IncludePlaying: true,
        IncludePrivate: false,
        MaxResults:     10,
    }
    
    roomList := roomMgr.GetRoomList(filter)
    fmt.Println("\n=== Room List ===")
    for _, info := range roomList {
        fmt.Printf("Room %d: %s (%d/%d) - %s\n",
            info.ID, info.Name, info.PlayerCount, 
            info.MaxPlayers, info.State.String())
    }
}

// createMockSession은 테스트용 세션을 생성한다
func createMockSession(userID uint64) *session.Session {
    // 실제로는 TCP 연결을 사용하지만, 여기서는 간단히 구현
    // ...
    return nil
}

// serializeMessage는 메시지를 직렬화한다 (예시)
func serializeMessage(msg map[string]interface{}) []byte {
    // 실제로는 프로토콜에 맞게 직렬화
    return []byte(fmt.Sprintf("%v", msg))
}

// deepCopy는 깊은 복사를 수행한다 (예시)
func deepCopy(src interface{}) interface{} {
    // 실제로는 제대로 된 깊은 복사 구현 필요
    return src
}

// shuffleDeck은 카드 덱을 섞는다 (예시)
func shuffleDeck() interface{} {
    // 포커 게임용 카드 덱 생성 및 셔플
    return nil
}
```

---

이 장에서는 게임 서버의 핵심인 방 시스템을 설계하고 구현했다. 방의 생명주기 관리, 플레이어 입퇴장, 브로드캐스팅, 동시성 제어, 그리고 기본적인 매치메이킹까지 다루었다.

중요한 설계 원칙:

1. **명령 채널 패턴**: 모든 방 변경 작업을 단일 고루틴에서 순차 처리하여 경쟁 조건을 방지한다.

2. **읽기-쓰기 분리**: 읽기 작업에는 RWMutex를 사용하여 동시성을 높인다.

3. **상태 기계**: 명확한 상태 전이 규칙으로 예측 가능한 동작을 보장한다.

4. **비동기 브로드캐스트**: 블로킹을 방지하기 위해 비동기로 메시지를 전송한다.

다음 장에서는 메시지 큐와 이벤트 처리 시스템을 다룬다. 방 시스템에서 발생하는 다양한 이벤트를 효율적으로 처리하는 방법을 배운다.



# Chapter 16. 메시지 큐와 이벤트 처리

게임 서버에서 메시지와 이벤트를 효율적으로 처리하는 것은 성능과 안정성의 핵심이다. 이 장에서는 메시지 큐 패턴을 활용하여 순차 처리를 보장하고, 우선순위 기반 처리를 구현하며, 메시지 손실을 방지하는 방법을 다룬다.

## 16.1 메시지 큐 패턴

메시지 큐는 비동기 통신의 기본 패턴이다. 송신자와 수신자를 분리하여 시스템의 결합도를 낮추고, 부하를 분산시킨다.

### 메시지 큐의 필요성

게임 서버에서 메시지 큐가 필요한 이유는 다음과 같다:

- **순차 처리 보장**: 같은 플레이어의 명령은 순서대로 처리되어야 한다
- **부하 평준화**: 일시적인 트래픽 폭증을 흡수한다
- **비동기 처리**: 네트워크 I/O와 게임 로직을 분리한다
- **백프레셔 제어**: 처리 속도를 조절하여 시스템 과부하를 방지한다

### 기본 메시지 큐 구조

```go
package messagequeue

import (
    "context"
    "sync"
    "time"
)

// Message는 큐에 저장되는 메시지를 나타낸다
type Message struct {
    ID        uint64      // 메시지 ID
    Type      MessageType // 메시지 타입
    Sender    uint64      // 발신자 ID
    Receiver  uint64      // 수신자 ID
    Data      interface{} // 메시지 데이터
    Timestamp time.Time   // 생성 시각
    
    // 응답 처리용 (옵션)
    ResChan   chan<- interface{} // 응답 채널
}

// MessageType은 메시지 타입을 나타낸다
type MessageType int

const (
    MsgTypeLogin MessageType = iota
    MsgTypeLogout
    MsgTypeChat
    MsgTypeGameAction
    MsgTypeRoomJoin
    MsgTypeRoomLeave
)

// String은 메시지 타입을 문자열로 반환한다
func (mt MessageType) String() string {
    types := []string{
        "Login", "Logout", "Chat", "GameAction", 
        "RoomJoin", "RoomLeave",
    }
    
    if int(mt) < len(types) {
        return types[mt]
    }
    return "Unknown"
}

// MessageQueue는 기본 메시지 큐를 나타낸다
type MessageQueue struct {
    queue    chan *Message     // 메시지 채널
    handlers map[MessageType]MessageHandler
    mu       sync.RWMutex      // 핸들러 맵 보호
    
    // 워커 관리
    workerCount int
    workers     []*Worker
    
    // 통계
    stats       QueueStats
    statsMu     sync.Mutex
    
    // 종료 관리
    ctx         context.Context
    cancel      context.CancelFunc
    wg          sync.WaitGroup
}

// MessageHandler는 메시지 처리 함수 타입이다
type MessageHandler func(*Message) error

// QueueStats는 큐 통계를 나타낸다
type QueueStats struct {
    Enqueued    uint64 // 큐에 들어간 메시지 수
    Processed   uint64 // 처리된 메시지 수
    Failed      uint64 // 실패한 메시지 수
    Dropped     uint64 // 버려진 메시지 수
}

// NewMessageQueue는 새로운 메시지 큐를 생성한다
func NewMessageQueue(bufferSize, workerCount int) *MessageQueue {
    ctx, cancel := context.WithCancel(context.Background())
    
    mq := &MessageQueue{
        queue:       make(chan *Message, bufferSize),
        handlers:    make(map[MessageType]MessageHandler),
        workerCount: workerCount,
        workers:     make([]*Worker, workerCount),
        ctx:         ctx,
        cancel:      cancel,
    }
    
    // 워커 시작
    for i := 0; i < workerCount; i++ {
        mq.workers[i] = NewWorker(i, mq)
        mq.workers[i].Start()
    }
    
    return mq
}

// RegisterHandler는 메시지 핸들러를 등록한다
func (mq *MessageQueue) RegisterHandler(msgType MessageType, handler MessageHandler) {
    mq.mu.Lock()
    defer mq.mu.Unlock()
    
    mq.handlers[msgType] = handler
}

// Enqueue는 메시지를 큐에 추가한다 (논블로킹)
func (mq *MessageQueue) Enqueue(msg *Message) error {
    select {
    case mq.queue <- msg:
        mq.incrementStat("enqueued")
        return nil
    default:
        mq.incrementStat("dropped")
        return ErrQueueFull
    }
}

// EnqueueBlocking은 메시지를 큐에 추가한다 (블로킹)
func (mq *MessageQueue) EnqueueBlocking(msg *Message) error {
    select {
    case mq.queue <- msg:
        mq.incrementStat("enqueued")
        return nil
    case <-mq.ctx.Done():
        return ErrQueueClosed
    }
}

// EnqueueWithTimeout은 타임아웃과 함께 메시지를 큐에 추가한다
func (mq *MessageQueue) EnqueueWithTimeout(msg *Message, timeout time.Duration) error {
    timer := time.NewTimer(timeout)
    defer timer.Stop()
    
    select {
    case mq.queue <- msg:
        mq.incrementStat("enqueued")
        return nil
    case <-timer.C:
        mq.incrementStat("dropped")
        return ErrEnqueueTimeout
    case <-mq.ctx.Done():
        return ErrQueueClosed
    }
}

// incrementStat는 통계를 증가시킨다
func (mq *MessageQueue) incrementStat(name string) {
    mq.statsMu.Lock()
    defer mq.statsMu.Unlock()
    
    switch name {
    case "enqueued":
        mq.stats.Enqueued++
    case "processed":
        mq.stats.Processed++
    case "failed":
        mq.stats.Failed++
    case "dropped":
        mq.stats.Dropped++
    }
}

// GetStats는 큐 통계를 반환한다
func (mq *MessageQueue) GetStats() QueueStats {
    mq.statsMu.Lock()
    defer mq.statsMu.Unlock()
    return mq.stats
}

// Size는 현재 큐 크기를 반환한다
func (mq *MessageQueue) Size() int {
    return len(mq.queue)
}

// Close는 메시지 큐를 종료한다
func (mq *MessageQueue) Close() {
    mq.cancel()
    
    // 워커들이 종료될 때까지 대기
    mq.wg.Wait()
    
    // 남은 메시지 처리 (옵션)
    close(mq.queue)
    for msg := range mq.queue {
        mq.processMessage(msg)
    }
}
```

### 워커 구현

메시지를 실제로 처리하는 워커를 구현한다:

```go
// Worker는 메시지를 처리하는 워커다
type Worker struct {
    id    int
    queue *MessageQueue
}

// NewWorker는 새로운 워커를 생성한다
func NewWorker(id int, queue *MessageQueue) *Worker {
    return &Worker{
        id:    id,
        queue: queue,
    }
}

// Start는 워커를 시작한다
func (w *Worker) Start() {
    w.queue.wg.Add(1)
    
    go func() {
        defer w.queue.wg.Done()
        w.run()
    }()
}

// run은 워커 루프를 실행한다
func (w *Worker) run() {
    for {
        select {
        case msg, ok := <-w.queue.queue:
            if !ok {
                // 큐가 닫힘
                return
            }
            
            w.queue.processMessage(msg)
            
        case <-w.queue.ctx.Done():
            return
        }
    }
}

// processMessage는 메시지를 처리한다
func (mq *MessageQueue) processMessage(msg *Message) {
    // 핸들러 조회
    mq.mu.RLock()
    handler, exists := mq.handlers[msg.Type]
    mq.mu.RUnlock()
    
    if !exists {
        println("No handler for message type:", msg.Type.String())
        mq.incrementStat("failed")
        return
    }
    
    // 핸들러 실행
    if err := handler(msg); err != nil {
        println("Message processing failed:", err.Error())
        mq.incrementStat("failed")
        
        // 응답 채널이 있으면 에러 전송
        if msg.ResChan != nil {
            msg.ResChan <- err
        }
    } else {
        mq.incrementStat("processed")
        
        // 응답 채널이 있으면 성공 전송
        if msg.ResChan != nil {
            msg.ResChan <- nil
        }
    }
}
```

### 사용 예제

```go
package main

import (
    "fmt"
    "time"
    
    "yourproject/messagequeue"
)

func main() {
    // 메시지 큐 생성 (버퍼: 1000, 워커: 4)
    mq := messagequeue.NewMessageQueue(1000, 4)
    defer mq.Close()
    
    // 핸들러 등록
    mq.RegisterHandler(messagequeue.MsgTypeChat, handleChat)
    mq.RegisterHandler(messagequeue.MsgTypeGameAction, handleGameAction)
    
    // 메시지 전송
    for i := 0; i < 100; i++ {
        msg := &messagequeue.Message{
            ID:        uint64(i),
            Type:      messagequeue.MsgTypeChat,
            Sender:    1,
            Data:      fmt.Sprintf("Message %d", i),
            Timestamp: time.Now(),
        }
        
        if err := mq.Enqueue(msg); err != nil {
            fmt.Println("Enqueue error:", err)
        }
    }
    
    // 처리 대기
    time.Sleep(2 * time.Second)
    
    // 통계 출력
    stats := mq.GetStats()
    fmt.Printf("Stats: Enqueued=%d, Processed=%d, Failed=%d, Dropped=%d\n",
        stats.Enqueued, stats.Processed, stats.Failed, stats.Dropped)
}

func handleChat(msg *messagequeue.Message) error {
    fmt.Printf("Chat from %d: %v\n", msg.Sender, msg.Data)
    return nil
}

func handleGameAction(msg *messagequeue.Message) error {
    fmt.Printf("Game action from %d: %v\n", msg.Sender, msg.Data)
    return nil
}
```

### 에러 타입 정의

```go
package messagequeue

import "errors"

var (
    ErrQueueFull       = errors.New("message queue is full")
    ErrQueueClosed     = errors.New("message queue is closed")
    ErrEnqueueTimeout  = errors.New("enqueue timeout")
    ErrInvalidMessage  = errors.New("invalid message")
    ErrNoHandler       = errors.New("no handler registered")
)
```

## 16.2 순차 처리 보장

게임 서버에서 특정 엔티티(플레이어, 방 등)의 메시지는 순서대로 처리되어야 한다. 순차 처리를 보장하는 여러 방법을 살펴본다.

### 엔티티별 큐 분리

각 엔티티가 자신만의 큐를 가지는 방식이다:

```go
package messagequeue

import (
    "sync"
)

// EntityMessageQueue는 엔티티별 메시지 큐를 관리한다
type EntityMessageQueue struct {
    queues  sync.Map // entityID -> chan *Message
    mu      sync.RWMutex
    
    // 설정
    bufferSize int
    
    // 종료 관리
    closing bool
    wg      sync.WaitGroup
}

// NewEntityMessageQueue는 새로운 엔티티 메시지 큐를 생성한다
func NewEntityMessageQueue(bufferSize int) *EntityMessageQueue {
    return &EntityMessageQueue{
        bufferSize: bufferSize,
    }
}

// Enqueue는 특정 엔티티의 큐에 메시지를 추가한다
func (emq *EntityMessageQueue) Enqueue(entityID uint64, msg *Message) error {
    if emq.closing {
        return ErrQueueClosed
    }
    
    // 엔티티 큐 가져오기 또는 생성
    queueInterface, _ := emq.queues.LoadOrStore(entityID, make(chan *Message, emq.bufferSize))
    queue := queueInterface.(chan *Message)
    
    select {
    case queue <- msg:
        return nil
    default:
        return ErrQueueFull
    }
}

// StartProcessor는 특정 엔티티의 메시지 처리를 시작한다
func (emq *EntityMessageQueue) StartProcessor(
    entityID uint64,
    handler MessageHandler,
) {
    queueInterface, ok := emq.queues.Load(entityID)
    if !ok {
        return
    }
    
    queue := queueInterface.(chan *Message)
    emq.wg.Add(1)
    
    go func() {
        defer emq.wg.Done()
        
        for msg := range queue {
            handler(msg)
        }
    }()
}

// StopProcessor는 특정 엔티티의 메시지 처리를 중지한다
func (emq *EntityMessageQueue) StopProcessor(entityID uint64) {
    if queueInterface, ok := emq.queues.LoadAndDelete(entityID); ok {
        queue := queueInterface.(chan *Message)
        close(queue)
    }
}

// Close는 모든 엔티티 큐를 종료한다
func (emq *EntityMessageQueue) Close() {
    emq.closing = true
    
    emq.queues.Range(func(key, value interface{}) bool {
        queue := value.(chan *Message)
        close(queue)
        emq.queues.Delete(key)
        return true
    })
    
    emq.wg.Wait()
}
```

### 해시 기반 라우팅

엔티티 ID를 해시하여 고정된 수의 큐에 분배하는 방식이다:

```go
// PartitionedQueue는 파티션된 메시지 큐다
type PartitionedQueue struct {
    partitions  []*MessageQueue
    partitionCount int
}

// NewPartitionedQueue는 새로운 파티션된 큐를 생성한다
func NewPartitionedQueue(partitionCount, bufferSize, workersPerPartition int) *PartitionedQueue {
    pq := &PartitionedQueue{
        partitions:     make([]*MessageQueue, partitionCount),
        partitionCount: partitionCount,
    }
    
    for i := 0; i < partitionCount; i++ {
        pq.partitions[i] = NewMessageQueue(bufferSize, workersPerPartition)
    }
    
    return pq
}

// getPartition은 엔티티 ID로 파티션을 결정한다
func (pq *PartitionedQueue) getPartition(entityID uint64) int {
    return int(entityID % uint64(pq.partitionCount))
}

// Enqueue는 엔티티 ID에 따라 적절한 파티션에 메시지를 추가한다
func (pq *PartitionedQueue) Enqueue(entityID uint64, msg *Message) error {
    partition := pq.getPartition(entityID)
    return pq.partitions[partition].Enqueue(msg)
}

// RegisterHandler는 모든 파티션에 핸들러를 등록한다
func (pq *PartitionedQueue) RegisterHandler(msgType MessageType, handler MessageHandler) {
    for _, partition := range pq.partitions {
        partition.RegisterHandler(msgType, handler)
    }
}

// GetStats는 모든 파티션의 통합 통계를 반환한다
func (pq *PartitionedQueue) GetStats() QueueStats {
    var total QueueStats
    
    for _, partition := range pq.partitions {
        stats := partition.GetStats()
        total.Enqueued += stats.Enqueued
        total.Processed += stats.Processed
        total.Failed += stats.Failed
        total.Dropped += stats.Dropped
    }
    
    return total
}

// Close는 모든 파티션을 종료한다
func (pq *PartitionedQueue) Close() {
    for _, partition := range pq.partitions {
        partition.Close()
    }
}
```

### 시퀀스 번호를 이용한 순서 보장

메시지에 시퀀스 번호를 부여하여 순서를 보장한다:

```go
// SequencedMessage는 시퀀스 번호가 있는 메시지다
type SequencedMessage struct {
    *Message
    Sequence uint64 // 시퀀스 번호
}

// SequenceChecker는 메시지 순서를 검증한다
type SequenceChecker struct {
    lastSequence sync.Map // entityID -> uint64
}

// NewSequenceChecker는 새로운 시퀀스 검사기를 생성한다
func NewSequenceChecker() *SequenceChecker {
    return &SequenceChecker{}
}

// CheckSequence는 메시지 시퀀스를 검증한다
func (sc *SequenceChecker) CheckSequence(entityID uint64, seq uint64) bool {
    lastSeqInterface, _ := sc.lastSequence.LoadOrStore(entityID, uint64(0))
    lastSeq := lastSeqInterface.(uint64)
    
    // 시퀀스가 순차적인지 확인
    if seq != lastSeq+1 {
        return false
    }
    
    // 시퀀스 업데이트
    sc.lastSequence.Store(entityID, seq)
    return true
}

// Reset은 엔티티의 시퀀스를 초기화한다
func (sc *SequenceChecker) Reset(entityID uint64) {
    sc.lastSequence.Delete(entityID)
}

// SequencedQueueWrapper는 시퀀스 검증을 추가한 큐 래퍼다
type SequencedQueueWrapper struct {
    queue   *MessageQueue
    checker *SequenceChecker
}

// NewSequencedQueueWrapper는 새로운 시퀀스 큐 래퍼를 생성한다
func NewSequencedQueueWrapper(queue *MessageQueue) *SequencedQueueWrapper {
    return &SequencedQueueWrapper{
        queue:   queue,
        checker: NewSequenceChecker(),
    }
}

// Enqueue는 시퀀스를 검증하고 메시지를 큐에 추가한다
func (sqw *SequencedQueueWrapper) Enqueue(entityID uint64, msg *SequencedMessage) error {
    if !sqw.checker.CheckSequence(entityID, msg.Sequence) {
        return ErrInvalidSequence
    }
    
    return sqw.queue.Enqueue(msg.Message)
}
```

### 순차 처리 시각화

```
플레이어 A의 메시지 흐름:

네트워크 → [A1] → [A2] → [A3] → Queue A → Worker → 순차 처리
                                     ↓
                                  [A1, A2, A3]

플레이어 B의 메시지 흐름:

네트워크 → [B1] → [B2] → Queue B → Worker → 순차 처리
                            ↓
                         [B1, B2]

각 플레이어는 독립된 큐를 가지므로 순서가 보장된다.
```

## 16.3 우선순위 큐 구현

중요한 메시지를 먼저 처리하기 위해 우선순위 큐를 구현한다.

### 우선순위 정의

```go
package messagequeue

// Priority는 메시지 우선순위를 나타낸다
type Priority int

const (
    PriorityLow Priority = iota
    PriorityNormal
    PriorityHigh
    PriorityCritical
)

// String은 우선순위를 문자열로 반환한다
func (p Priority) String() string {
    priorities := []string{"Low", "Normal", "High", "Critical"}
    if int(p) < len(priorities) {
        return priorities[p]
    }
    return "Unknown"
}

// PriorityMessage는 우선순위가 있는 메시지다
type PriorityMessage struct {
    *Message
    Priority Priority
}
```

### 힙 기반 우선순위 큐

Go의 `container/heap` 패키지를 사용하여 구현한다:

```go
package messagequeue

import (
    "container/heap"
    "sync"
)

// priorityQueueHeap은 힙 인터페이스를 구현한다
type priorityQueueHeap []*PriorityMessage

func (pq priorityQueueHeap) Len() int { return len(pq) }

func (pq priorityQueueHeap) Less(i, j int) bool {
    // 우선순위가 높을수록 먼저 처리 (내림차순)
    if pq[i].Priority != pq[j].Priority {
        return pq[i].Priority > pq[j].Priority
    }
    // 우선순위가 같으면 타임스탬프로 비교 (오름차순)
    return pq[i].Timestamp.Before(pq[j].Timestamp)
}

func (pq priorityQueueHeap) Swap(i, j int) {
    pq[i], pq[j] = pq[j], pq[i]
}

func (pq *priorityQueueHeap) Push(x interface{}) {
    *pq = append(*pq, x.(*PriorityMessage))
}

func (pq *priorityQueueHeap) Pop() interface{} {
    old := *pq
    n := len(old)
    item := old[n-1]
    old[n-1] = nil // 메모리 누수 방지
    *pq = old[0 : n-1]
    return item
}

// PriorityMessageQueue는 우선순위 메시지 큐다
type PriorityMessageQueue struct {
    heap     priorityQueueHeap
    mu       sync.Mutex
    cond     *sync.Cond
    
    // 워커 관리
    workers  []*PriorityWorker
    handlers map[MessageType]MessageHandler
    
    // 통계
    stats    QueueStats
    statsMu  sync.Mutex
    
    // 종료 관리
    closing  bool
    wg       sync.WaitGroup
}

// NewPriorityMessageQueue는 새로운 우선순위 큐를 생성한다
func NewPriorityMessageQueue(workerCount int) *PriorityMessageQueue {
    pmq := &PriorityMessageQueue{
        heap:     make(priorityQueueHeap, 0),
        workers:  make([]*PriorityWorker, workerCount),
        handlers: make(map[MessageType]MessageHandler),
    }
    
    heap.Init(&pmq.heap)
    pmq.cond = sync.NewCond(&pmq.mu)
    
    // 워커 시작
    for i := 0; i < workerCount; i++ {
        pmq.workers[i] = NewPriorityWorker(i, pmq)
        pmq.workers[i].Start()
    }
    
    return pmq
}

// Enqueue는 우선순위 메시지를 큐에 추가한다
func (pmq *PriorityMessageQueue) Enqueue(msg *PriorityMessage) error {
    pmq.mu.Lock()
    defer pmq.mu.Unlock()
    
    if pmq.closing {
        return ErrQueueClosed
    }
    
    heap.Push(&pmq.heap, msg)
    
    pmq.incrementStat("enqueued")
    
    // 대기 중인 워커에 신호
    pmq.cond.Signal()
    
    return nil
}

// Dequeue는 우선순위가 가장 높은 메시지를 꺼낸다
func (pmq *PriorityMessageQueue) Dequeue() *PriorityMessage {
    pmq.mu.Lock()
    defer pmq.mu.Unlock()
    
    for pmq.heap.Len() == 0 && !pmq.closing {
        pmq.cond.Wait()
    }
    
    if pmq.closing && pmq.heap.Len() == 0 {
        return nil
    }
    
    return heap.Pop(&pmq.heap).(*PriorityMessage)
}

// RegisterHandler는 메시지 핸들러를 등록한다
func (pmq *PriorityMessageQueue) RegisterHandler(msgType MessageType, handler MessageHandler) {
    pmq.mu.Lock()
    defer pmq.mu.Unlock()
    
    pmq.handlers[msgType] = handler
}

// Size는 현재 큐 크기를 반환한다
func (pmq *PriorityMessageQueue) Size() int {
    pmq.mu.Lock()
    defer pmq.mu.Unlock()
    return pmq.heap.Len()
}

// incrementStat는 통계를 증가시킨다
func (pmq *PriorityMessageQueue) incrementStat(name string) {
    pmq.statsMu.Lock()
    defer pmq.statsMu.Unlock()
    
    switch name {
    case "enqueued":
        pmq.stats.Enqueued++
    case "processed":
        pmq.stats.Processed++
    case "failed":
        pmq.stats.Failed++
    }
}

// GetStats는 큐 통계를 반환한다
func (pmq *PriorityMessageQueue) GetStats() QueueStats {
    pmq.statsMu.Lock()
    defer pmq.statsMu.Unlock()
    return pmq.stats
}

// Close는 우선순위 큐를 종료한다
func (pmq *PriorityMessageQueue) Close() {
    pmq.mu.Lock()
    pmq.closing = true
    pmq.cond.Broadcast() // 모든 워커 깨우기
    pmq.mu.Unlock()
    
    pmq.wg.Wait()
}
```

### 우선순위 워커

```go
// PriorityWorker는 우선순위 큐의 워커다
type PriorityWorker struct {
    id    int
    queue *PriorityMessageQueue
}

// NewPriorityWorker는 새로운 우선순위 워커를 생성한다
func NewPriorityWorker(id int, queue *PriorityMessageQueue) *PriorityWorker {
    return &PriorityWorker{
        id:    id,
        queue: queue,
    }
}

// Start는 워커를 시작한다
func (pw *PriorityWorker) Start() {
    pw.queue.wg.Add(1)
    
    go func() {
        defer pw.queue.wg.Done()
        pw.run()
    }()
}

// run은 워커 루프를 실행한다
func (pw *PriorityWorker) run() {
    for {
        msg := pw.queue.Dequeue()
        if msg == nil {
            // 큐가 닫힘
            return
        }
        
        pw.processMessage(msg)
    }
}

// processMessage는 메시지를 처리한다
func (pw *PriorityWorker) processMessage(msg *PriorityMessage) {
    pw.queue.mu.Lock()
    handler, exists := pw.queue.handlers[msg.Type]
    pw.queue.mu.Unlock()
    
    if !exists {
        println("No handler for message type:", msg.Type.String())
        pw.queue.incrementStat("failed")
        return
    }
    
    if err := handler(msg.Message); err != nil {
        println("Message processing failed:", err.Error())
        pw.queue.incrementStat("failed")
    } else {
        pw.queue.incrementStat("processed")
    }
}
```

### 우선순위 큐 사용 예제

```go
package main

import (
    "fmt"
    "time"
    
    "yourproject/messagequeue"
)

func main() {
    // 우선순위 큐 생성
    pmq := messagequeue.NewPriorityMessageQueue(2)
    defer pmq.Close()
    
    // 핸들러 등록
    pmq.RegisterHandler(messagequeue.MsgTypeGameAction, handleGameAction)
    
    // 다양한 우선순위의 메시지 추가
    messages := []*messagequeue.PriorityMessage{
        {
            Message: &messagequeue.Message{
                Type: messagequeue.MsgTypeGameAction,
                Data: "Low priority task",
            },
            Priority: messagequeue.PriorityLow,
        },
        {
            Message: &messagequeue.Message{
                Type: messagequeue.MsgTypeGameAction,
                Data: "Critical task",
            },
            Priority: messagequeue.PriorityCritical,
        },
        {
            Message: &messagequeue.Message{
                Type: messagequeue.MsgTypeGameAction,
                Data: "Normal task",
            },
            Priority: messagequeue.PriorityNormal,
        },
        {
            Message: &messagequeue.Message{
                Type: messagequeue.MsgTypeGameAction,
                Data: "High priority task",
            },
            Priority: messagequeue.PriorityHigh,
        },
    }
    
    // 메시지 추가 (순서 무작위)
    for _, msg := range messages {
        msg.Timestamp = time.Now()
        pmq.Enqueue(msg)
    }
    
    // 처리 대기
    time.Sleep(2 * time.Second)
    
    // 통계 출력
    stats := pmq.GetStats()
    fmt.Printf("Processed: %d, Failed: %d\n", stats.Processed, stats.Failed)
}

func handleGameAction(msg *messagequeue.Message) error {
    fmt.Printf("Processing: %v\n", msg.Data)
    time.Sleep(100 * time.Millisecond)
    return nil
}
```

출력 결과 (우선순위 순서대로 처리됨):
```
Processing: Critical task
Processing: High priority task
Processing: Normal task
Processing: Low priority task
```

## 16.4 이벤트 디스패처 설계

게임 서버에서 발생하는 다양한 이벤트를 효율적으로 전파하는 이벤트 디스패처를 구현한다.

### 이벤트 시스템 구조

```go
package event

import (
    "sync"
)

// EventType은 이벤트 타입을 나타낸다
type EventType string

const (
    EventPlayerJoin      EventType = "player.join"
    EventPlayerLeave     EventType = "player.leave"
    EventRoomCreated     EventType = "room.created"
    EventRoomClosed      EventType = "room.closed"
    EventGameStart       EventType = "game.start"
    EventGameEnd         EventType = "game.end"
    EventChatMessage     EventType = "chat.message"
)

// Event는 이벤트를 나타낸다
type Event struct {
    Type      EventType
    Source    interface{} // 이벤트 발생원
    Data      interface{} // 이벤트 데이터
    Timestamp int64       // 타임스탬프
}

// EventHandler는 이벤트 핸들러 함수 타입이다
type EventHandler func(*Event)

// EventDispatcher는 이벤트 디스패처다
type EventDispatcher struct {
    handlers  map[EventType][]EventHandler
    mu        sync.RWMutex
    
    // 비동기 처리용
    eventChan chan *Event
    workers   int
    wg        sync.WaitGroup
    closing   bool
}

// NewEventDispatcher는 새로운 이벤트 디스패처를 생성한다
func NewEventDispatcher(bufferSize, workers int) *EventDispatcher {
    ed := &EventDispatcher{
        handlers:  make(map[EventType][]EventHandler),
        eventChan: make(chan *Event, bufferSize),
        workers:   workers,
    }
    
    // 워커 시작
    for i := 0; i < workers; i++ {
        ed.wg.Add(1)
        go ed.worker()
    }
    
    return ed
}

// Subscribe는 이벤트 핸들러를 등록한다
func (ed *EventDispatcher) Subscribe(eventType EventType, handler EventHandler) {
    ed.mu.Lock()
    defer ed.mu.Unlock()
    
    ed.handlers[eventType] = append(ed.handlers[eventType], handler)
}

// Unsubscribe는 특정 이벤트 타입의 모든 핸들러를 제거한다
func (ed *EventDispatcher) Unsubscribe(eventType EventType) {
    ed.mu.Lock()
    defer ed.mu.Unlock()
    
    delete(ed.handlers, eventType)
}

// Dispatch는 이벤트를 동기적으로 발송한다
func (ed *EventDispatcher) Dispatch(event *Event) {
    ed.mu.RLock()
    handlers, exists := ed.handlers[event.Type]
    ed.mu.RUnlock()
    
    if !exists {
        return
    }
    
    // 모든 핸들러 실행
    for _, handler := range handlers {
        handler(event)
    }
}

// DispatchAsync는 이벤트를 비동기적으로 발송한다
func (ed *EventDispatcher) DispatchAsync(event *Event) error {
    if ed.closing {
        return ErrDispatcherClosed
    }
    
    select {
    case ed.eventChan <- event:
        return nil
    default:
        return ErrEventChannelFull
    }
}

// worker는 이벤트를 처리하는 워커다
func (ed *EventDispatcher) worker() {
    defer ed.wg.Done()
    
    for event := range ed.eventChan {
        ed.Dispatch(event)
    }
}

// Close는 이벤트 디스패처를 종료한다
func (ed *EventDispatcher) Close() {
    ed.closing = true
    close(ed.eventChan)
    ed.wg.Wait()
}
```

### 타입 안전 이벤트 시스템

제네릭을 사용하여 타입 안전한 이벤트 시스템을 만든다:

```go
// TypedEventHandler는 타입 안전한 이벤트 핸들러다
type TypedEventHandler[T any] func(T)

// TypedEventBus는 타입 안전한 이벤트 버스다
type TypedEventBus[T any] struct {
    handlers []TypedEventHandler[T]
    mu       sync.RWMutex
}

// NewTypedEventBus는 새로운 타입 안전 이벤트 버스를 생성한다
func NewTypedEventBus[T any]() *TypedEventBus[T] {
    return &TypedEventBus[T]{
        handlers: make([]TypedEventHandler[T], 0),
    }
}

// Subscribe는 핸들러를 등록한다
func (teb *TypedEventBus[T]) Subscribe(handler TypedEventHandler[T]) {
    teb.mu.Lock()
    defer teb.mu.Unlock()
    
    teb.handlers = append(teb.handlers, handler)
}

// Publish는 이벤트를 발행한다
func (teb *TypedEventBus[T]) Publish(event T) {
    teb.mu.RLock()
    handlers := make([]TypedEventHandler[T], len(teb.handlers))
    copy(handlers, teb.handlers)
    teb.mu.RUnlock()
    
    for _, handler := range handlers {
        handler(event)
    }
}

// PublishAsync는 이벤트를 비동기로 발행한다
func (teb *TypedEventBus[T]) PublishAsync(event T) {
    go teb.Publish(event)
}

// 사용 예시: 플레이어 입장 이벤트
type PlayerJoinEvent struct {
    PlayerID uint64
    RoomID   uint64
    Nickname string
}

func exampleTypedEventBus() {
    bus := NewTypedEventBus[PlayerJoinEvent]()
    
    // 핸들러 등록
    bus.Subscribe(func(event PlayerJoinEvent) {
        println("Player joined:", event.Nickname)
    })
    
    // 이벤트 발행
    bus.Publish(PlayerJoinEvent{
        PlayerID: 123,
        RoomID:   456,
        Nickname: "Player1",
    })
}
```

### 이벤트 필터링

특정 조건을 만족하는 이벤트만 처리한다:

```go
// FilteredEventHandler는 필터가 있는 이벤트 핸들러다
type FilteredEventHandler struct {
    Filter  func(*Event) bool
    Handler EventHandler
}

// FilteredEventDispatcher는 필터링을 지원하는 디스패처다
type FilteredEventDispatcher struct {
    handlers map[EventType][]*FilteredEventHandler
    mu       sync.RWMutex
}

// NewFilteredEventDispatcher는 새로운 필터링 디스패처를 생성한다
func NewFilteredEventDispatcher() *FilteredEventDispatcher {
    return &FilteredEventDispatcher{
        handlers: make(map[EventType][]*FilteredEventHandler),
    }
}

// Subscribe는 필터와 함께 핸들러를 등록한다
func (fed *FilteredEventDispatcher) Subscribe(
    eventType EventType,
    filter func(*Event) bool,
    handler EventHandler,
) {
    fed.mu.Lock()
    defer fed.mu.Unlock()
    
    fh := &FilteredEventHandler{
        Filter:  filter,
        Handler: handler,
    }
    
    fed.handlers[eventType] = append(fed.handlers[eventType], fh)
}

// Dispatch는 필터를 적용하여 이벤트를 발송한다
func (fed *FilteredEventDispatcher) Dispatch(event *Event) {
    fed.mu.RLock()
    handlers, exists := fed.handlers[event.Type]
    fed.mu.RUnlock()
    
    if !exists {
        return
    }
    
    for _, fh := range handlers {
        // 필터가 없거나 필터를 통과하면 실행
        if fh.Filter == nil || fh.Filter(event) {
            fh.Handler(event)
        }
    }
}

// 사용 예시
func exampleFilteredDispatcher() {
    fed := NewFilteredEventDispatcher()
    
    // 특정 방의 이벤트만 처리
    fed.Subscribe(
        EventPlayerJoin,
        func(event *Event) bool {
            data := event.Data.(map[string]interface{})
            roomID := data["roomID"].(uint64)
            return roomID == 123 // 방 123의 이벤트만
        },
        func(event *Event) {
            println("Player joined room 123")
        },
    )
}
```

## 16.5 메시지 손실 방지

네트워크 장애나 서버 재시작 시에도 중요한 메시지를 잃지 않도록 하는 메커니즘을 구현한다.

### 확인 응답(ACK) 메커니즘

```go
package messagequeue

import (
    "sync"
    "time"
)

// AckMessage는 확인 응답이 필요한 메시지다
type AckMessage struct {
    *Message
    AckTimeout time.Duration
    RetryCount int
    ackChan    chan bool
}

// NewAckMessage는 새로운 ACK 메시지를 생성한다
func NewAckMessage(msg *Message, timeout time.Duration, retry int) *AckMessage {
    return &AckMessage{
        Message:    msg,
        AckTimeout: timeout,
        RetryCount: retry,
        ackChan:    make(chan bool, 1),
    }
}

// WaitForAck는 ACK를 기다린다
func (am *AckMessage) WaitForAck() bool {
    timer := time.NewTimer(am.AckTimeout)
    defer timer.Stop()
    
    select {
    case ack := <-am.ackChan:
        return ack
    case <-timer.C:
        return false
    }
}

// Ack는 ACK를 전송한다
func (am *AckMessage) Ack() {
    select {
    case am.ackChan <- true:
    default:
    }
}

// ReliableQueue는 메시지 손실을 방지하는 큐다
type ReliableQueue struct {
    queue        *MessageQueue
    pendingMsgs  sync.Map // messageID -> *AckMessage
    retryQueue   chan *AckMessage
    
    wg           sync.WaitGroup
    closing      bool
}

// NewReliableQueue는 새로운 신뢰성 큐를 생성한다
func NewReliableQueue(bufferSize, workers int) *ReliableQueue {
    rq := &ReliableQueue{
        queue:      NewMessageQueue(bufferSize, workers),
        retryQueue: make(chan *AckMessage, 100),
    }
    
    // 재시도 워커 시작
    rq.wg.Add(1)
    go rq.retryWorker()
    
    return rq
}

// EnqueueReliable은 신뢰성 있게 메시지를 큐에 추가한다
func (rq *ReliableQueue) EnqueueReliable(msg *AckMessage) error {
    // 대기 중인 메시지로 등록
    rq.pendingMsgs.Store(msg.ID, msg)
    
    // 큐에 추가
    if err := rq.queue.Enqueue(msg.Message); err != nil {
        rq.pendingMsgs.Delete(msg.ID)
        return err
    }
    
    // ACK 대기
    go rq.waitForAck(msg)
    
    return nil
}

// waitForAck는 ACK를 기다리고 재시도를 처리한다
func (rq *ReliableQueue) waitForAck(msg *AckMessage) {
    if msg.WaitForAck() {
        // ACK 받음
        rq.pendingMsgs.Delete(msg.ID)
        return
    }
    
    // 타임아웃: 재시도 필요
    if msg.RetryCount > 0 {
        msg.RetryCount--
        
        select {
        case rq.retryQueue <- msg:
        default:
            // 재시도 큐가 가득 참
            println("Retry queue full, message lost:", msg.ID)
            rq.pendingMsgs.Delete(msg.ID)
        }
    } else {
        // 재시도 횟수 초과
        println("Max retries exceeded, message lost:", msg.ID)
        rq.pendingMsgs.Delete(msg.ID)
    }
}

// retryWorker는 재시도가 필요한 메시지를 처리한다
func (rq *ReliableQueue) retryWorker() {
    defer rq.wg.Done()
    
    for msg := range rq.retryQueue {
        if rq.closing {
            return
        }
        
        // 재시도
        println("Retrying message:", msg.ID)
        rq.queue.Enqueue(msg.Message)
        
        // ACK 재대기
        go rq.waitForAck(msg)
    }
}

// Ack는 메시지 확인 응답을 처리한다
func (rq *ReliableQueue) Ack(messageID uint64) {
    if msgInterface, ok := rq.pendingMsgs.Load(messageID); ok {
        msg := msgInterface.(*AckMessage)
        msg.Ack()
    }
}

// Close는 신뢰성 큐를 종료한다
func (rq *ReliableQueue) Close() {
    rq.closing = true
    close(rq.retryQueue)
    rq.wg.Wait()
    rq.queue.Close()
}
```

### 영속화 큐

중요한 메시지를 디스크에 저장하여 서버 재시작 후에도 복구한다:

```go
package messagequeue

import (
    "encoding/json"
    "os"
    "sync"
)

// PersistentQueue는 영속화되는 메시지 큐다
type PersistentQueue struct {
    queue      *MessageQueue
    filePath   string
    mu         sync.Mutex
    
    // 영속화 설정
    flushInterval time.Duration
    batchSize     int
    buffer        []*Message
    
    wg         sync.WaitGroup
    closing    bool
}

// NewPersistentQueue는 새로운 영속화 큐를 생성한다
func NewPersistentQueue(filePath string, bufferSize, workers int) (*PersistentQueue, error) {
    pq := &PersistentQueue{
        queue:         NewMessageQueue(bufferSize, workers),
        filePath:      filePath,
        flushInterval: 5 * time.Second,
        batchSize:     100,
        buffer:        make([]*Message, 0, 100),
    }
    
    // 기존 메시지 복구
    if err := pq.recover(); err != nil {
        return nil, err
    }
    
    // 주기적 플러시 시작
    pq.wg.Add(1)
    go pq.periodicFlush()
    
    return pq, nil
}

// Enqueue는 메시지를 큐에 추가하고 영속화한다
func (pq *PersistentQueue) Enqueue(msg *Message) error {
    // 메모리 큐에 추가
    if err := pq.queue.Enqueue(msg); err != nil {
        return err
    }
    
    // 버퍼에 추가
    pq.mu.Lock()
    pq.buffer = append(pq.buffer, msg)
    shouldFlush := len(pq.buffer) >= pq.batchSize
    pq.mu.Unlock()
    
    // 배치 크기에 도달하면 즉시 플러시
    if shouldFlush {
        pq.flush()
    }
    
    return nil
}

// flush는 버퍼의 메시지를 디스크에 쓴다
func (pq *PersistentQueue) flush() error {
    pq.mu.Lock()
    if len(pq.buffer) == 0 {
        pq.mu.Unlock()
        return nil
    }
    
    toFlush := pq.buffer
    pq.buffer = make([]*Message, 0, pq.batchSize)
    pq.mu.Unlock()
    
    // JSON으로 직렬화
    data, err := json.Marshal(toFlush)
    if err != nil {
        return err
    }
    
    // 파일에 추가
    file, err := os.OpenFile(pq.filePath, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0644)
    if err != nil {
        return err
    }
    defer file.Close()
    
    _, err = file.Write(append(data, '\n'))
    return err
}

// periodicFlush는 주기적으로 플러시한다
func (pq *PersistentQueue) periodicFlush() {
    defer pq.wg.Done()
    
    ticker := time.NewTicker(pq.flushInterval)
    defer ticker.Stop()
    
    for {
        select {
        case <-ticker.C:
            pq.flush()
        case <-time.After(1 * time.Second):
            if pq.closing {
                pq.flush() // 마지막 플러시
                return
            }
        }
    }
}

// recover는 디스크에서 메시지를 복구한다
func (pq *PersistentQueue) recover() error {
    file, err := os.Open(pq.filePath)
    if os.IsNotExist(err) {
        return nil // 파일이 없으면 정상
    }
    if err != nil {
        return err
    }
    defer file.Close()
    
    decoder := json.NewDecoder(file)
    
    for decoder.More() {
        var messages []*Message
        if err := decoder.Decode(&messages); err != nil {
            continue // 손상된 데이터는 건너뛴다
        }
        
        // 복구된 메시지를 큐에 추가
        for _, msg := range messages {
            pq.queue.Enqueue(msg)
        }
    }
    
    // 복구 완료 후 파일 삭제 (또는 백업)
    os.Remove(pq.filePath)
    
    return nil
}

// Close는 영속화 큐를 종료한다
func (pq *PersistentQueue) Close() {
    pq.closing = true
    pq.wg.Wait()
    pq.queue.Close()
}
```

### 중복 제거

같은 메시지가 여러 번 처리되는 것을 방지한다:

```go
// DeduplicationQueue는 중복 제거 기능이 있는 큐다
type DeduplicationQueue struct {
    queue       *MessageQueue
    processed   sync.Map // messageID -> bool
    ttl         time.Duration
    
    cleanupTicker *time.Ticker
    wg            sync.WaitGroup
    closing       bool
}

// NewDeduplicationQueue는 새로운 중복 제거 큐를 생성한다
func NewDeduplicationQueue(bufferSize, workers int, ttl time.Duration) *DeduplicationQueue {
    dq := &DeduplicationQueue{
        queue:         NewMessageQueue(bufferSize, workers),
        ttl:           ttl,
        cleanupTicker: time.NewTicker(ttl),
    }
    
    // 주기적 정리
    dq.wg.Add(1)
    go dq.cleanup()
    
    return dq
}

// Enqueue는 중복을 확인하고 메시지를 큐에 추가한다
func (dq *DeduplicationQueue) Enqueue(msg *Message) error {
    // 이미 처리된 메시지인지 확인
    if _, loaded := dq.processed.LoadOrStore(msg.ID, time.Now()); loaded {
        return ErrDuplicateMessage
    }
    
    return dq.queue.Enqueue(msg)
}

// cleanup은 주기적으로 오래된 항목을 정리한다
func (dq *DeduplicationQueue) cleanup() {
    defer dq.wg.Done()
    
    for {
        select {
        case <-dq.cleanupTicker.C:
            now := time.Now()
            
            dq.processed.Range(func(key, value interface{}) bool {
                timestamp := value.(time.Time)
                if now.Sub(timestamp) > dq.ttl {
                    dq.processed.Delete(key)
                }
                return true
            })
            
        case <-time.After(1 * time.Second):
            if dq.closing {
                return
            }
        }
    }
}

// Close는 중복 제거 큐를 종료한다
func (dq *DeduplicationQueue) Close() {
    dq.closing = true
    dq.cleanupTicker.Stop()
    dq.wg.Wait()
    dq.queue.Close()
}
```

### 에러 타입 추가

```go
var (
    ErrDispatcherClosed  = errors.New("event dispatcher is closed")
    ErrEventChannelFull  = errors.New("event channel is full")
    ErrInvalidSequence   = errors.New("invalid message sequence")
    ErrDuplicateMessage  = errors.New("duplicate message")
)
```

---

이 장에서는 게임 서버의 메시지 처리 핵심인 메시지 큐와 이벤트 시스템을 다루었다. 기본 메시지 큐부터 순차 처리 보장, 우선순위 큐, 이벤트 디스패처, 그리고 메시지 손실 방지까지 실전에서 필요한 모든 패턴을 구현했다.

핵심 설계 원칙:

1. **채널 기반 큐**: Go의 채널을 활용하여 동시성 안전한 큐를 구현한다.

2. **워커 풀 패턴**: 고정된 수의 워커로 메시지를 병렬 처리한다.

3. **순차 처리**: 엔티티별 큐 분리나 파티셔닝으로 순서를 보장한다.

4. **우선순위 처리**: 힙 자료구조로 중요한 메시지를 먼저 처리한다.

5. **신뢰성**: ACK, 영속화, 중복 제거로 메시지 손실을 방지한다.

다음 장에서는 Go 모듈 시스템의 고급 기능을 다룬다. 특히 로컬 모듈 참조를 통해 네트워크 라이브러리와 게임 로직을 분리하는 방법을 배운다.  