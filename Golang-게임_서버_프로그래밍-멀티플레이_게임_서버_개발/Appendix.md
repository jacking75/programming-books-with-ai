# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# 부록

## Appendix A. Go 1.25 주요 기능

### A.1 새로운 언어 기능

Go 1.25는 이전 버전의 안정성을 유지하면서도 몇 가지 중요한 언어 기능을 추가했다. 게임 서버 개발자 입장에서 특히 주목할 만한 변화들을 살펴본다.

#### 제네릭 성능 개선

Go 1.18에서 도입된 제네릭은 1.25에서 상당한 성능 개선을 이루었다. 컴파일러의 인라이닝 최적화가 강화되어 제네릭 함수 호출 오버헤드가 크게 감소했다.

```go
// 게임 서버에서 자주 사용하는 제네릭 풀 구현
type ObjectPool[T any] struct {
    pool chan T
    new  func() T
}

func NewObjectPool[T any](size int, newFunc func() T) *ObjectPool[T] {
    return &ObjectPool[T]{
        pool: make(chan T, size),
        new:  newFunc,
    }
}

func (p *ObjectPool[T]) Get() T {
    select {
    case obj := <-p.pool:
        return obj
    default:
        return p.new()
    }
}

func (p *ObjectPool[T]) Put(obj T) {
    select {
    case p.pool <- obj:
    default:
        // 풀이 가득 차면 버린다
    }
}

// 패킷 풀 예제
type Packet struct {
    Data   []byte
    Length int
}

var packetPool = NewObjectPool(100, func() *Packet {
    return &Packet{
        Data: make([]byte, 4096),
    }
})

func handleConnection(conn net.Conn) {
    packet := packetPool.Get()
    defer packetPool.Put(packet)
    
    // 패킷 처리
    n, err := conn.Read(packet.Data)
    if err != nil {
        return
    }
    packet.Length = n
    // ... 처리 로직
}
```

#### 개선된 에러 처리

Go 1.25는 에러 처리를 더 간결하게 만드는 새로운 패턴을 지원한다.

```go
// 다중 에러 반환 간소화
import "errors"

// 여러 리소스를 정리할 때 발생하는 에러를 모두 수집
func closeResources(resources ...io.Closer) error {
    var errs []error
    for _, r := range resources {
        if err := r.Close(); err != nil {
            errs = append(errs, err)
        }
    }
    return errors.Join(errs...)
}

// 게임 서버 종료 시 사용 예제
func (s *GameServer) Shutdown() error {
    return closeResources(
        s.listener,
        s.database,
        s.cache,
        s.logger,
    )
}

// 에러 체크를 더 명확하게
func processGameAction(action *GameAction) error {
    if err := validateAction(action); err != nil {
        return fmt.Errorf("액션 검증 실패: %w", err)
    }
    
    if err := applyAction(action); err != nil {
        return fmt.Errorf("액션 적용 실패: %w", err)
    }
    
    return nil
}
```

#### 향상된 구조체 태그 처리

구조체 태그를 런타임에 더 효율적으로 처리할 수 있게 되었다.

```go
// 게임 프로토콜 정의에 유용한 구조체 태그
type PlayerLoginRequest struct {
    UserID   string `packet:"id=1,required"`
    Password string `packet:"id=2,required,max=32"`
    Version  string `packet:"id=3,required"`
}

type PlayerInfo struct {
    ID       int64  `json:"id" db:"player_id"`
    Nickname string `json:"nickname" db:"nickname" validate:"min=2,max=16"`
    Level    int    `json:"level" db:"level" validate:"min=1,max=100"`
    Gold     int64  `json:"gold" db:"gold" validate:"min=0"`
}

// 태그 기반 자동 직렬화
func serializeStruct(v interface{}) ([]byte, error) {
    val := reflect.ValueOf(v)
    typ := reflect.TypeOf(v)
    
    var buf bytes.Buffer
    
    for i := 0; i < val.NumField(); i++ {
        field := val.Field(i)
        tag := typ.Field(i).Tag.Get("packet")
        
        if tag == "" {
            continue
        }
        
        // 태그 파싱 및 직렬화 로직
        // ...
    }
    
    return buf.Bytes(), nil
}
```

### A.2 표준 라이브러리 변경사항

#### net 패키지 개선

네트워크 패키지에 게임 서버 개발에 유용한 기능들이 추가되었다.

```go
import (
    "context"
    "net"
    "time"
)

// 향상된 Deadline 제어
func handleClientWithTimeout(conn net.Conn) {
    // 전체 연결에 대한 타임아웃
    ctx, cancel := context.WithTimeout(context.Background(), 5*time.Minute)
    defer cancel()
    
    // 읽기/쓰기별 세밀한 타임아웃 제어
    conn.SetReadDeadline(time.Now().Add(30 * time.Second))
    
    buffer := make([]byte, 1024)
    n, err := conn.Read(buffer)
    if err != nil {
        if netErr, ok := err.(net.Error); ok && netErr.Timeout() {
            // 타임아웃 처리
            return
        }
        return
    }
    
    // 응답 전송 타임아웃
    conn.SetWriteDeadline(time.Now().Add(10 * time.Second))
    _, err = conn.Write(buffer[:n])
}

// 개선된 Keep-Alive 설정
func setupTCPConnection(conn *net.TCPConn) error {
    // Keep-Alive 활성화
    if err := conn.SetKeepAlive(true); err != nil {
        return err
    }
    
    // Keep-Alive 주기 설정 (Go 1.25에서 더 세밀한 제어 가능)
    if err := conn.SetKeepAlivePeriod(30 * time.Second); err != nil {
        return err
    }
    
    // TCP 버퍼 크기 조정
    if err := conn.SetReadBuffer(65536); err != nil {
        return err
    }
    
    if err := conn.SetWriteBuffer(65536); err != nil {
        return err
    }
    
    return nil
}
```

#### sync 패키지 확장

동시성 제어를 위한 새로운 도구들이 추가되었다.

```go
import (
    "sync"
    "sync/atomic"
)

// 개선된 sync.Map 성능
type SessionManager struct {
    sessions sync.Map // map[string]*Session
    count    atomic.Int64
}

func (sm *SessionManager) Add(sessionID string, session *Session) {
    sm.sessions.Store(sessionID, session)
    sm.count.Add(1)
}

func (sm *SessionManager) Remove(sessionID string) {
    if _, loaded := sm.sessions.LoadAndDelete(sessionID); loaded {
        sm.count.Add(-1)
    }
}

func (sm *SessionManager) Get(sessionID string) (*Session, bool) {
    value, ok := sm.sessions.Load(sessionID)
    if !ok {
        return nil, false
    }
    return value.(*Session), true
}

func (sm *SessionManager) Count() int64 {
    return sm.count.Load()
}

// 모든 세션에 브로드캐스트
func (sm *SessionManager) Broadcast(message []byte) {
    sm.sessions.Range(func(key, value interface{}) bool {
        session := value.(*Session)
        session.Send(message)
        return true // continue iteration
    })
}

// 새로운 Once 패턴 - 에러 반환 지원
type OnceValue[T any] struct {
    once  sync.Once
    value T
    err   error
}

func (o *OnceValue[T]) Do(f func() (T, error)) (T, error) {
    o.once.Do(func() {
        o.value, o.err = f()
    })
    return o.value, o.err
}

// 게임 서버 설정 로딩 예제
var gameConfig OnceValue[*GameConfig]

func GetGameConfig() (*GameConfig, error) {
    return gameConfig.Do(func() (*GameConfig, error) {
        // 설정 파일 로딩 - 한 번만 실행됨
        return loadConfigFromFile("game_config.json")
    })
}
```

#### context 패키지 강화

컨텍스트 관리가 더욱 편리해졌다.

```go
import (
    "context"
    "time"
)

// 여러 컨텍스트 병합
func handleGameSession(parentCtx context.Context, session *Session) {
    // 부모 컨텍스트와 타임아웃을 함께 사용
    ctx, cancel := context.WithTimeout(parentCtx, 10*time.Minute)
    defer cancel()
    
    // 값 추가
    ctx = context.WithValue(ctx, "sessionID", session.ID)
    ctx = context.WithValue(ctx, "playerID", session.PlayerID)
    
    // 게임 로직 실행
    if err := playGame(ctx, session); err != nil {
        // 컨텍스트 취소 원인 확인
        if ctx.Err() == context.DeadlineExceeded {
            session.Send([]byte("게임 시간이 초과되었습니다"))
        } else if ctx.Err() == context.Canceled {
            session.Send([]byte("게임이 취소되었습니다"))
        }
    }
}

// 컨텍스트 값 타입 안전성
type contextKey string

const (
    sessionIDKey contextKey = "sessionID"
    playerIDKey  contextKey = "playerID"
    roomIDKey    contextKey = "roomID"
)

func getSessionID(ctx context.Context) (string, bool) {
    id, ok := ctx.Value(sessionIDKey).(string)
    return id, ok
}

func withSessionID(ctx context.Context, sessionID string) context.Context {
    return context.WithValue(ctx, sessionIDKey, sessionID)
}
```

#### encoding/binary 패키지 개선

바이너리 데이터 처리 성능이 향상되었다.

```go
import (
    "encoding/binary"
    "io"
)

// 더 빠른 바이너리 읽기/쓰기
type PacketHeader struct {
    Length uint16
    Type   uint16
    SeqNum uint32
}

// 개선된 직렬화 성능
func (h *PacketHeader) WriteTo(w io.Writer) (int64, error) {
    // binary.Write보다 빠른 직접 쓰기
    var buf [8]byte
    binary.LittleEndian.PutUint16(buf[0:2], h.Length)
    binary.LittleEndian.PutUint16(buf[2:4], h.Type)
    binary.LittleEndian.PutUint32(buf[4:8], h.SeqNum)
    
    n, err := w.Write(buf[:])
    return int64(n), err
}

func (h *PacketHeader) ReadFrom(r io.Reader) (int64, error) {
    var buf [8]byte
    n, err := io.ReadFull(r, buf[:])
    if err != nil {
        return int64(n), err
    }
    
    h.Length = binary.LittleEndian.Uint16(buf[0:2])
    h.Type = binary.LittleEndian.Uint16(buf[2:4])
    h.SeqNum = binary.LittleEndian.Uint32(buf[4:8])
    
    return int64(n), nil
}

// 대용량 데이터 처리 최적화
func serializePlayerData(player *Player, w io.Writer) error {
    // 헤더 쓰기
    header := PacketHeader{
        Type: 100, // PlayerData
    }
    
    // 버퍼링을 통한 성능 향상
    var buf bytes.Buffer
    
    // 플레이어 데이터 직렬화
    binary.Write(&buf, binary.LittleEndian, player.ID)
    binary.Write(&buf, binary.LittleEndian, uint16(len(player.Nickname)))
    buf.WriteString(player.Nickname)
    binary.Write(&buf, binary.LittleEndian, player.Level)
    binary.Write(&buf, binary.LittleEndian, player.Gold)
    
    // 헤더에 길이 설정
    header.Length = uint16(buf.Len())
    
    // 헤더 쓰기
    if _, err := header.WriteTo(w); err != nil {
        return err
    }
    
    // 데이터 쓰기
    _, err := buf.WriteTo(w)
    return err
}
```

### A.3 성능 개선사항

#### 가비지 컬렉션 최적화

Go 1.25는 가비지 컬렉션 성능이 크게 개선되어 게임 서버의 지연시간(latency)이 감소했다.

```go
// GC 튜닝 예제
import (
    "runtime"
    "runtime/debug"
)

func optimizeGCForGameServer() {
    // 메모리 제한 설정 (소프트 리밋)
    debug.SetMemoryLimit(8 << 30) // 8GB
    
    // GC 목표 퍼센티지 조정
    // 기본값 100에서 200으로 증가하면 GC 빈도가 줄어들지만 메모리 사용량 증가
    debug.SetGCPercent(150)
    
    // CPU 코어 수에 따른 GOMAXPROCS 설정
    numCPU := runtime.NumCPU()
    runtime.GOMAXPROCS(numCPU)
    
    // 메모리 통계 모니터링
    var m runtime.MemStats
    runtime.ReadMemStats(&m)
    
    log.Printf("메모리 할당: %d MB", m.Alloc/1024/1024)
    log.Printf("전체 할당: %d MB", m.TotalAlloc/1024/1024)
    log.Printf("시스템 메모리: %d MB", m.Sys/1024/1024)
    log.Printf("GC 실행 횟수: %d", m.NumGC)
}

// 객체 풀링으로 GC 부담 감소
type MessageBuffer struct {
    data []byte
}

var messageBufferPool = sync.Pool{
    New: func() interface{} {
        return &MessageBuffer{
            data: make([]byte, 0, 4096),
        }
    },
}

func processMessage(msg []byte) {
    buf := messageBufferPool.Get().(*MessageBuffer)
    defer func() {
        buf.data = buf.data[:0] // 슬라이스 재사용을 위해 길이만 0으로
        messageBufferPool.Put(buf)
    }()
    
    buf.data = append(buf.data, msg...)
    // 메시지 처리
}
```

#### 컴파일러 최적화

```go
// 인라이닝 힌트 활용
//go:inline
func fastAdd(a, b int32) int32 {
    return a + b
}

// 경계 체크 제거 최적화
func processPacketData(data []byte) {
    // 길이 체크를 미리 하면 컴파일러가 이후 인덱싱의 경계 체크를 생략
    if len(data) < 8 {
        return
    }
    
    // 이제 data[0:8] 접근 시 경계 체크가 생략됨
    header := binary.LittleEndian.Uint64(data[0:8])
    _ = header
}

// 루프 최적화
func sumArray(arr []int) int {
    sum := 0
    // 범위 기반 for 루프는 자동으로 최적화됨
    for _, v := range arr {
        sum += v
    }
    return sum
}
```

#### 메모리 할당 최적화

```go
// 사전 할당으로 성능 향상
func createPlayerList(count int) []*Player {
    // 용량을 미리 지정하여 재할당 방지
    players := make([]*Player, 0, count)
    
    for i := 0; i < count; i++ {
        players = append(players, &Player{
            ID: int64(i),
        })
    }
    
    return players
}

// 스트링 빌더 사용
func buildMessage(parts []string) string {
    var builder strings.Builder
    
    // 예상 크기 사전 할당
    totalLen := 0
    for _, p := range parts {
        totalLen += len(p)
    }
    builder.Grow(totalLen)
    
    for _, p := range parts {
        builder.WriteString(p)
    }
    
    return builder.String()
}

// 맵 사전 할당
func createSessionMap(expectedSize int) map[string]*Session {
    // 예상 크기를 지정하여 리해싱 방지
    return make(map[string]*Session, expectedSize)
}
```

#### 네트워크 I/O 성능

```go
// 버퍼 재사용으로 성능 향상
type Connection struct {
    conn   net.Conn
    reader *bufio.Reader
    writer *bufio.Writer
}

func NewConnection(conn net.Conn) *Connection {
    return &Connection{
        conn:   conn,
        reader: bufio.NewReaderSize(conn, 32*1024), // 32KB 읽기 버퍼
        writer: bufio.NewWriterSize(conn, 32*1024), // 32KB 쓰기 버퍼
    }
}

func (c *Connection) ReadPacket() ([]byte, error) {
    // 헤더 읽기
    var header PacketHeader
    if _, err := header.ReadFrom(c.reader); err != nil {
        return nil, err
    }
    
    // 데이터 읽기
    data := make([]byte, header.Length)
    if _, err := io.ReadFull(c.reader, data); err != nil {
        return nil, err
    }
    
    return data, nil
}

func (c *Connection) WritePacket(data []byte) error {
    header := PacketHeader{
        Length: uint16(len(data)),
    }
    
    if _, err := header.WriteTo(c.writer); err != nil {
        return err
    }
    
    if _, err := c.writer.Write(data); err != nil {
        return err
    }
    
    // 주기적으로 플러시 (모든 쓰기마다 하지 않음)
    return c.writer.Flush()
}
```

## Appendix B. 유용한 도구와 라이브러리

### B.1 개발 도구

게임 서버 개발을 더욱 효율적으로 만들어주는 필수 도구들을 소개한다.

#### VSCode 확장 프로그램

**Go 확장 (공식)**
- 기능: 코드 자동완성, 디버깅, 테스트 실행, 포매팅
- 설치: VSCode 마켓플레이스에서 "Go" 검색
- 필수 설정:

```json
// settings.json
{
    "go.useLanguageServer": true,
    "go.lintTool": "golangci-lint",
    "go.lintOnSave": "workspace",
    "go.formatTool": "goimports",
    "go.autocompleteUnimportedPackages": true,
    "go.gocodeAutoBuild": true,
    "go.buildOnSave": "workspace",
    "go.testOnSave": false,
    "go.coverOnSave": false,
    "go.toolsManagement.autoUpdate": true,
    "[go]": {
        "editor.formatOnSave": true,
        "editor.codeActionsOnSave": {
            "source.organizeImports": true
        }
    },
    "gopls": {
        "ui.semanticTokens": true,
        "ui.completion.usePlaceholders": true,
        "analyses": {
            "unusedparams": true,
            "shadow": true
        }
    }
}
```

**Error Lens**
- 기능: 에러와 경고를 코드 라인에 직접 표시
- 게임 서버 개발 시 네트워크 에러나 동시성 문제를 즉시 확인 가능

**REST Client**
- 기능: HTTP 요청 테스트
- 관리 API 테스트에 유용

```http
### 서버 상태 확인
GET http://localhost:8080/admin/status

### 플레이어 목록 조회
GET http://localhost:8080/admin/players
Authorization: Bearer your-admin-token

### 방 생성
POST http://localhost:8080/admin/rooms
Content-Type: application/json

{
    "name": "테스트방",
    "maxPlayers": 6
}
```

#### 명령줄 도구

**golangci-lint**
정적 분석 도구로 코드 품질을 향상시킨다.

```bash
# 설치
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest

# 사용
golangci-lint run

# 설정 파일 생성
golangci-lint config init
```

설정 파일 예제 (`.golangci.yml`):

```yaml
linters:
  enable:
    - gofmt
    - govet
    - errcheck
    - staticcheck
    - unused
    - gosimple
    - structcheck
    - varcheck
    - ineffassign
    - deadcode
    - typecheck
    - gosec
    - gocyclo
    - dupl
    - misspell
    - lll
    - unparam
    - nakedret
    - prealloc
    - gocritic

linters-settings:
  gocyclo:
    min-complexity: 15
  gocritic:
    enabled-tags:
      - diagnostic
      - performance
      - style
  lll:
    line-length: 120

issues:
  exclude-rules:
    - path: _test\.go
      linters:
        - gocyclo
        - dupl
```

**air**
핫 리로드 도구로 개발 생산성을 높인다.

```bash
# 설치
go install github.com/cosmtrek/air@latest

# 실행
air
```

설정 파일 (`.air.toml`):

```toml
root = "."
tmp_dir = "tmp"

[build]
  cmd = "go build -o ./tmp/main ."
  bin = "tmp/main"
  full_bin = "./tmp/main"
  include_ext = ["go", "tpl", "tmpl", "html"]
  exclude_dir = ["assets", "tmp", "vendor", "frontend"]
  include_dir = []
  exclude_file = []
  delay = 1000
  stop_on_error = true
  send_interrupt = false
  kill_delay = 500

[log]
  time = true

[color]
  main = "magenta"
  watcher = "cyan"
  build = "yellow"
  runner = "green"

[misc]
  clean_on_exit = true
```

**delve**
강력한 Go 디버거다.

```bash
# 설치
go install github.com/go-delve/delve/cmd/dlv@latest

# 사용
dlv debug main.go

# 브레이크포인트 설정
(dlv) break main.handleConnection
(dlv) continue

# 변수 검사
(dlv) print session
(dlv) print player.Gold
```

### B.2 테스트 도구

#### Testify
테스트 작성을 쉽게 만드는 어설션 라이브러리다.

```bash
go get github.com/stretchr/testify
```

```go
import (
    "testing"
    "github.com/stretchr/testify/assert"
    "github.com/stretchr/testify/require"
    "github.com/stretchr/testify/mock"
)

// 기본 어설션
func TestPlayerCreation(t *testing.T) {
    player := NewPlayer(1, "테스터")
    
    assert.Equal(t, int64(1), player.ID)
    assert.Equal(t, "테스터", player.Nickname)
    assert.Greater(t, player.Gold, int64(0))
}

// 요구사항 어설션 (실패 시 즉시 중단)
func TestDatabaseConnection(t *testing.T) {
    db, err := ConnectDatabase()
    require.NoError(t, err, "데이터베이스 연결 실패")
    defer db.Close()
    
    // db가 nil이 아님을 보장하고 계속 진행
    assert.NotNil(t, db)
}

// Mock 객체 사용
type MockSessionManager struct {
    mock.Mock
}

func (m *MockSessionManager) GetSession(id string) (*Session, bool) {
    args := m.Called(id)
    if args.Get(0) == nil {
        return nil, args.Bool(1)
    }
    return args.Get(0).(*Session), args.Bool(1)
}

func TestGameLogic(t *testing.T) {
    mockSM := new(MockSessionManager)
    
    // 예상 호출 설정
    session := &Session{ID: "test-session"}
    mockSM.On("GetSession", "test-session").Return(session, true)
    
    // 테스트 실행
    result, found := mockSM.GetSession("test-session")
    
    // 검증
    assert.True(t, found)
    assert.Equal(t, session, result)
    mockSM.AssertExpectations(t)
}
```

#### gomock
인터페이스 기반 목 생성 도구다.

```bash
go install github.com/golang/mock/mockgen@latest
```

```go
// 인터페이스 정의
type PlayerRepository interface {
    GetPlayer(id int64) (*Player, error)
    SavePlayer(player *Player) error
}

// Mock 생성 명령
//go:generate mockgen -destination=mocks/mock_player_repository.go -package=mocks . PlayerRepository

// 테스트에서 사용
func TestGameService(t *testing.T) {
    ctrl := gomock.NewController(t)
    defer ctrl.Finish()
    
    mockRepo := mocks.NewMockPlayerRepository(ctrl)
    
    // 예상 동작 설정
    player := &Player{ID: 1, Nickname: "테스터"}
    mockRepo.EXPECT().
        GetPlayer(int64(1)).
        Return(player, nil).
        Times(1)
    
    // 서비스 테스트
    service := NewGameService(mockRepo)
    result, err := service.GetPlayerInfo(1)
    
    assert.NoError(t, err)
    assert.Equal(t, player, result)
}
```

### B.3 프로파일링 도구

#### pprof (표준 라이브러리)

CPU와 메모리 프로파일링을 위한 기본 도구다.

```go
import (
    "net/http"
    _ "net/http/pprof"
    "runtime"
    "runtime/pprof"
)

// HTTP 서버에 pprof 엔드포인트 추가
func startProfileServer() {
    go func() {
        log.Println(http.ListenAndServe("localhost:6060", nil))
    }()
}

// 프로그래밍 방식으로 CPU 프로파일링
func profileCPU() {
    f, err := os.Create("cpu.prof")
    if err != nil {
        log.Fatal(err)
    }
    defer f.Close()
    
    pprof.StartCPUProfile(f)
    defer pprof.StopCPUProfile()
    
    // 프로파일링할 코드 실행
    runGameServer()
}

// 메모리 프로파일링
func profileMemory() {
    runtime.GC() // GC 실행
    
    f, err := os.Create("mem.prof")
    if err != nil {
        log.Fatal(err)
    }
    defer f.Close()
    
    if err := pprof.WriteHeapProfile(f); err != nil {
        log.Fatal(err)
    }
}
```

프로파일 분석:

```bash
# CPU 프로파일 분석
go tool pprof cpu.prof
(pprof) top10
(pprof) list functionName
(pprof) web

# 메모리 프로파일 분석
go tool pprof mem.prof
(pprof) top10
(pprof) list functionName

# HTTP를 통한 실시간 프로파일링
go tool pprof http://localhost:6060/debug/pprof/profile?seconds=30
go tool pprof http://localhost:6060/debug/pprof/heap
```

#### trace

실행 추적 도구로 고루틴과 스케줄링을 분석한다.

```go
import (
    "os"
    "runtime/trace"
)

func main() {
    f, err := os.Create("trace.out")
    if err != nil {
        panic(err)
    }
    defer f.Close()
    
    if err := trace.Start(f); err != nil {
        panic(err)
    }
    defer trace.Stop()
    
    // 추적할 코드
    runGameServer()
}
```

분석:

```bash
go tool trace trace.out
```

### B.4 추천 서드파티 라이브러리

#### 로깅

**zerolog**
고성능 JSON 로깅 라이브러리다.

```bash
go get github.com/rs/zerolog/log
```

```go
import (
    "github.com/rs/zerolog"
    "github.com/rs/zerolog/log"
)

func setupLogger() {
    // 개발 환경: 사람이 읽기 쉬운 형식
    zerolog.TimeFieldFormat = zerolog.TimeFormatUnix
    log.Logger = log.Output(zerolog.ConsoleWriter{Out: os.Stderr})
    
    // 프로덕션: JSON 형식
    // log.Logger = zerolog.New(os.Stderr).With().Timestamp().Logger()
}

func logGameEvent(playerID int64, event string) {
    log.Info().
        Int64("player_id", playerID).
        Str("event", event).
        Msg("게임 이벤트 발생")
}

func logError(err error, context string) {
    log.Error().
        Err(err).
        Str("context", context).
        Msg("에러 발생")
}
```

**zap**
Uber가 만든 초고속 로깅 라이브러리다.

```bash
go get go.uber.org/zap
```

```go
import "go.uber.org/zap"

func setupZapLogger() *zap.Logger {
    // 프로덕션 설정
    logger, _ := zap.NewProduction()
    
    // 개발 설정
    // logger, _ := zap.NewDevelopment()
    
    return logger
}

func useLogger() {
    logger := setupZapLogger()
    defer logger.Sync()
    
    logger.Info("서버 시작",
        zap.String("address", ":8080"),
        zap.Int("max_connections", 1000),
    )
    
    logger.Error("연결 실패",
        zap.Error(err),
        zap.String("client_ip", "192.168.1.100"),
    )
}
```

#### 설정 관리

**viper**
다양한 형식의 설정 파일을 읽는 라이브러리다.

```bash
go get github.com/spf13/viper
```

```go
import "github.com/spf13/viper"

type Config struct {
    Server struct {
        Port           int
        MaxConnections int
        ReadTimeout    int
        WriteTimeout   int
    }
    Game struct {
        MaxRooms      int
        MaxPlayers    int
        RoundTimeout  int
    }
    Database struct {
        Host     string
        Port     int
        Username string
        Password string
        DBName   string
    }
}

func loadConfig() (*Config, error) {
    viper.SetConfigName("config")
    viper.SetConfigType("yaml")
    viper.AddConfigPath(".")
    viper.AddConfigPath("./config")
    
    // 환경변수 지원
    viper.AutomaticEnv()
    viper.SetEnvPrefix("GAME")
    
    if err := viper.ReadInConfig(); err != nil {
        return nil, err
    }
    
    var config Config
    if err := viper.Unmarshal(&config); err != nil {
        return nil, err
    }
    
    return &config, nil
}
```

설정 파일 예제 (`config.yaml`):

```yaml
server:
  port: 8080
  maxConnections: 1000
  readTimeout: 30
  writeTimeout: 30

game:
  maxRooms: 100
  maxPlayers: 6
  roundTimeout: 300

database:
  host: localhost
  port: 5432
  username: gameserver
  password: ${DB_PASSWORD}
  dbName: poker_game
```

#### 데이터 검증

**validator**
구조체 필드 검증 라이브러리다.

```bash
go get github.com/go-playground/validator/v10
```

```go
import "github.com/go-playground/validator/v10"

type PlayerRegisterRequest struct {
    Nickname string `validate:"required,min=2,max=16,alphanum"`
    Email    string `validate:"required,email"`
    Password string `validate:"required,min=8,max=32"`
    Age      int    `validate:"required,gte=18,lte=100"`
}

var validate = validator.New()

func validateRequest(req *PlayerRegisterRequest) error {
    return validate.Struct(req)
}

// 사용 예제
func handleRegister(data []byte) error {
    var req PlayerRegisterRequest
    if err := json.Unmarshal(data, &req); err != nil {
        return err
    }
    
    if err := validateRequest(&req); err != nil {
        // 검증 에러 처리
        for _, err := range err.(validator.ValidationErrors) {
            log.Printf("필드 %s: %s", err.Field(), err.Tag())
        }
        return err
    }
    
    // 유효한 데이터 처리
    return nil
}
```

#### UUID 생성

**google/uuid**
고유 식별자 생성 라이브러리다.

```bash
go get github.com/google/uuid
```

```go
import "github.com/google/uuid"

// 세션 ID 생성
func generateSessionID() string {
    return uuid.New().String()
}

// 방 ID 생성
func generateRoomID() string {
    return uuid.NewString()
}

// UUID 파싱
func parseSessionID(id string) (uuid.UUID, error) {
    return uuid.Parse(id)
}
```

#### 데이터베이스

**GORM**
ORM 라이브러리다.

```bash
go get gorm.io/gorm
go get gorm.io/driver/postgres
```

```go
import (
    "gorm.io/gorm"
    "gorm.io/driver/postgres"
)

type Player struct {
    ID        int64     `gorm:"primaryKey"`
    Nickname  string    `gorm:"unique;not null"`
    Email     string    `gorm:"unique;not null"`
    Gold      int64     `gorm:"default:10000"`
    Level     int       `gorm:"default:1"`
    CreatedAt time.Time
    UpdatedAt time.Time
}

func connectDB() (*gorm.DB, error) {
    dsn := "host=localhost user=gameserver password=secret dbname=poker_game port=5432"
    return gorm.Open(postgres.Open(dsn), &gorm.Config{})
}

func createPlayer(db *gorm.DB, nickname, email string) (*Player, error) {
    player := &Player{
        Nickname: nickname,
        Email:    email,
    }
    
    result := db.Create(player)
    return player, result.Error
}
```

#### 메트릭 수집

**prometheus/client_golang**
Prometheus 메트릭 라이브러리다.

```bash
go get github.com/prometheus/client_golang/prometheus
go get github.com/prometheus/client_golang/prometheus/promhttp
```

```go
import (
    "github.com/prometheus/client_golang/prometheus"
    "github.com/prometheus/client_golang/prometheus/promauto"
    "github.com/prometheus/client_golang/prometheus/promhttp"
)

var (
    activeConnections = promauto.NewGauge(prometheus.GaugeOpts{
        Name: "game_server_active_connections",
        Help: "현재 활성 연결 수",
    })
    
    totalRequests = promauto.NewCounterVec(
        prometheus.CounterOpts{
            Name: "game_server_requests_total",
            Help: "총 요청 수",
        },
        []string{"type"},
    )
    
    requestDuration = promauto.NewHistogramVec(
        prometheus.HistogramOpts{
            Name:    "game_server_request_duration_seconds",
            Help:    "요청 처리 시간",
            Buckets: prometheus.DefBuckets,
        },
        []string{"type"},
    )
)

func setupMetrics() {
    http.Handle("/metrics", promhttp.Handler())
    go http.ListenAndServe(":9090", nil)
}

func recordConnection(delta float64) {
    activeConnections.Add(delta)
}

func recordRequest(requestType string, duration time.Duration) {
    totalRequests.WithLabelValues(requestType).Inc()
    requestDuration.WithLabelValues(requestType).Observe(duration.Seconds())
}
```

이러한 도구와 라이브러리들을 적절히 활용하면 게임 서버 개발의 생산성과 품질을 크게 향상시킬 수 있다. 각 도구의 공식 문서를 참고하여 프로젝트에 맞게 커스터마이징하는 것을 권장한다.   