# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 25. 단위 테스트

단위 테스트는 개별 함수나 메서드의 동작을 검증하는 테스트다. 게임 서버에서는 카드 평가 로직, 베팅 계산, 플레이어 상태 관리 등 핵심 비즈니스 로직이 정확하게 작동하는지 확인해야 한다. Go의 `testing` 패키지는 강력하고 사용하기 쉬운 단위 테스트 프레임워크를 제공한다. 이 챕터에서는 게임 서버 개발 시 필요한 다양한 단위 테스트 기법을 다룬다.

## 25.1 testing 패키지

### 기본 테스트 작성

Go의 테스트 파일은 `_test.go` 확장자를 사용하며, `testing.T` 타입의 파라미터를 받는 `TestXxx` 형태의 함수다. 가장 간단한 예제부터 시작하자.

```go
// card.go
package poker

// 카드를 나타내는 타입
type Card struct {
    Rank int // 2-14 (14는 Ace)
    Suit int // 0: Spades, 1: Hearts, 2: Diamonds, 3: Clubs
}

// 카드 점수 계산 (간단한 버전)
func GetCardValue(card Card) int {
    if card.Rank == 14 { // Ace
        return 11
    }
    if card.Rank >= 10 {
        return 10
    }
    return card.Rank
}

// 두 카드 비교
func CompareCards(card1, card2 Card) int {
    if card1.Rank > card2.Rank {
        return 1  // card1이 더 강함
    }
    if card1.Rank < card2.Rank {
        return -1  // card2가 더 강함
    }
    return 0  // 같음
}
```

위의 `card.go` 파일에 대한 기본 단위 테스트를 작성하자.

```go
// card_test.go
package poker

import (
    "testing"
)

// GetCardValue 함수 테스트
func TestGetCardValue(t *testing.T) {
    // 테스트 케이스: Ace
    card := Card{Rank: 14, Suit: 0}
    value := GetCardValue(card)
    
    if value != 11 {
        t.Errorf("Ace의 값은 11이어야 하지만 %d를 얻었다", value)
    }
}

// 더 많은 케이스를 포함한 테스트
func TestGetCardValueWithKing(t *testing.T) {
    card := Card{Rank: 13, Suit: 1}
    value := GetCardValue(card)
    
    if value != 10 {
        t.Errorf("King의 값은 10이어야 하지만 %d를 얻었다", value)
    }
}

func TestGetCardValueWithNumber(t *testing.T) {
    card := Card{Rank: 5, Suit: 2}
    value := GetCardValue(card)
    
    if value != 5 {
        t.Errorf("숫자 5의 값은 5여야 하지만 %d를 얻었다", value)
    }
}

// CompareCards 함수 테스트
func TestCompareCards(t *testing.T) {
    ace := Card{Rank: 14, Suit: 0}
    king := Card{Rank: 13, Suit: 1}
    
    result := CompareCards(ace, king)
    
    if result != 1 {
        t.Errorf("Ace는 King보다 강해야 하므로 1을 반환해야 하지만 %d를 반환했다", result)
    }
}
```

테스트를 실행하려면 다음 명령어를 사용한다.

```bash
# 현재 디렉토리의 모든 테스트 실행
go test

# 자세한 출력과 함께 실행
go test -v

# 특정 테스트만 실행
go test -run TestGetCardValue
```

### 테스트 실패와 오류 처리

`testing.T`는 테스트 실패를 보고하기 위한 여러 메서드를 제공한다.

```go
// error_handling_test.go
package poker

import (
    "testing"
)

// Fail: 즉시 테스트를 실패로 표시
func TestFail(t *testing.T) {
    if 1 == 2 {
        t.Fail()  // 테스트 실패, 하지만 계속 실행
    }
}

// FailNow: 즉시 테스트를 중단하고 실패로 표시
func TestFailNow(t *testing.T) {
    card := Card{Rank: 0, Suit: 0}  // 유효하지 않은 카드
    
    if card.Rank < 2 || card.Rank > 14 {
        t.FailNow()  // 이 지점에서 즉시 종료
    }
    
    // 이 코드는 실행되지 않음
    t.Logf("이 메시지는 출력되지 않는다")
}

// Error: Logf와 Fail을 합친 것 (메시지와 함께 실패 표시)
func TestError(t *testing.T) {
    value := GetCardValue(Card{Rank: 5, Suit: 0})
    
    if value != 5 {
        t.Error("카드 값이 올바르지 않습니다")
    }
}

// Errorf: 포맷 문자열과 함께 실패 표시
func TestErrorf(t *testing.T) {
    value := GetCardValue(Card{Rank: 10, Suit: 0})
    
    if value != 10 {
        t.Errorf("예상 값: 10, 실제 값: %d", value)
    }
}

// Fatal: Error와 FailNow를 합친 것 (메시지와 함께 즉시 중단)
func TestFatal(t *testing.T) {
    card := Card{Rank: 15, Suit: 0}  // 유효하지 않은 카드
    
    if card.Rank > 14 {
        t.Fatal("카드의 Rank가 14를 초과할 수 없습니다")
    }
    
    // 이 코드는 실행되지 않음
    t.Log("이 메시지는 출력되지 않습니다")
}

// Fatalf: 포맷 문자열과 함께 즉시 중단
func TestFatalf(t *testing.T) {
    suits := []int{0, 1, 2, 3}
    suitIndex := 5
    
    if suitIndex >= len(suits) {
        t.Fatalf("유효하지 않은 슈트 인덱스: %d (최대: %d)", suitIndex, len(suits)-1)
    }
}

// Log와 Logf: 정보 메시지 출력 (테스트는 계속 진행)
func TestLog(t *testing.T) {
    t.Log("이것은 정보 메시지입니다")
    t.Logf("카드 값: %d", GetCardValue(Card{Rank: 10, Suit: 0}))
}

// Skip: 테스트 건너뛰기
func TestSkip(t *testing.T) {
    t.Skip("이 테스트는 현재 구현되지 않았습니다")
    
    // 이 코드는 실행되지 않음
    t.Error("이 오류는 나타나지 않습니다")
}

// Skipf: 포맷 문자열과 함께 테스트 건너뛰기
func TestSkipf(t *testing.T) {
    version := "1.0"
    t.Skipf("이 기능은 버전 %s에서는 지원되지 않습니다", version)
}
```

## 25.2 테이블 기반 테스트

여러 입력값에 대한 테스트를 반복적으로 작성하는 것은 비효율적이다. 테이블 기반 테스트는 하나의 테스트 함수에서 여러 케이스를 처리하는 우아한 방법이다.

### 기본 테이블 기반 테스트

```go
// table_test.go
package poker

import (
    "testing"
)

// GetCardValue에 대한 테이블 기반 테스트
func TestGetCardValueTable(t *testing.T) {
    tests := []struct {
        name     string
        card     Card
        expected int
    }{
        {
            name:     "Ace는 11",
            card:     Card{Rank: 14, Suit: 0},
            expected: 11,
        },
        {
            name:     "King은 10",
            card:     Card{Rank: 13, Suit: 1},
            expected: 10,
        },
        {
            name:     "Queen은 10",
            card:     Card{Rank: 12, Suit: 2},
            expected: 10,
        },
        {
            name:     "Jack은 10",
            card:     Card{Rank: 11, Suit: 3},
            expected: 10,
        },
        {
            name:     "10은 10",
            card:     Card{Rank: 10, Suit: 0},
            expected: 10,
        },
        {
            name:     "5는 5",
            card:     Card{Rank: 5, Suit: 0},
            expected: 5,
        },
        {
            name:     "2는 2",
            card:     Card{Rank: 2, Suit: 0},
            expected: 2,
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            result := GetCardValue(tt.card)
            if result != tt.expected {
                t.Errorf("기댓값 %d, 실제 %d", tt.expected, result)
            }
        })
    }
}

// CompareCards에 대한 테이블 기반 테스트
func TestCompareCardsTable(t *testing.T) {
    tests := []struct {
        name     string
        card1    Card
        card2    Card
        expected int
    }{
        {
            name:     "Ace > King",
            card1:    Card{Rank: 14, Suit: 0},
            card2:    Card{Rank: 13, Suit: 0},
            expected: 1,
        },
        {
            name:     "King < Ace",
            card1:    Card{Rank: 13, Suit: 0},
            card2:    Card{Rank: 14, Suit: 0},
            expected: -1,
        },
        {
            name:     "같은 Rank는 같음",
            card1:    Card{Rank: 10, Suit: 0},
            card2:    Card{Rank: 10, Suit: 1},
            expected: 0,
        },
        {
            name:     "5 > 3",
            card1:    Card{Rank: 5, Suit: 0},
            card2:    Card{Rank: 3, Suit: 0},
            expected: 1,
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            result := CompareCards(tt.card1, tt.card2)
            if result != tt.expected {
                t.Errorf("기댓값 %d, 실제 %d", tt.expected, result)
            }
        })
    }
}
```

테이블 기반 테스트의 장점은 명확하다. 새로운 케이스를 추가하려면 테이블에 한 행만 추가하면 되고, 테스트 로직 자체는 변경할 필요가 없다. `-run` 플래그로 특정 서브테스트를 실행할 수도 있다.

```bash
# 테이블 기반 테스트 실행
go test -v -run TestGetCardValueTable

# 특정 서브테스트만 실행
go test -v -run TestGetCardValueTable/Ace
```

### 게임 서버 로직의 복잡한 테이블 기반 테스트

게임 서버의 더 복잡한 로직을 테이블 기반으로 테스트하는 예제다.

```go
// player.go
package poker

type Player struct {
    ID    int
    Name  string
    Chips int
}

// 베팅 가능한지 확인
func (p *Player) CanBet(amount int) bool {
    return p.Chips >= amount
}

// 베팅 수행
func (p *Player) Bet(amount int) error {
    if !p.CanBet(amount) {
        return ErrInsufficientChips
    }
    p.Chips -= amount
    return nil
}

// 칩 추가
func (p *Player) AddChips(amount int) {
    p.Chips += amount
}

var ErrInsufficientChips = struct {
    message string
}{
    message: "칩이 부족합니다",
}

// player_test.go
package poker

import (
    "testing"
)

func TestPlayerBetting(t *testing.T) {
    tests := []struct {
        name           string
        initialChips   int
        betAmount      int
        shouldSucceed  bool
        finalChips     int
    }{
        {
            name:           "충분한 칩으로 베팅 성공",
            initialChips:   1000,
            betAmount:      100,
            shouldSucceed:  true,
            finalChips:     900,
        },
        {
            name:           "정확한 칩만 남은 경우",
            initialChips:   100,
            betAmount:      100,
            shouldSucceed:  true,
            finalChips:     0,
        },
        {
            name:           "부족한 칩으로 베팅 실패",
            initialChips:   50,
            betAmount:      100,
            shouldSucceed:  false,
            finalChips:     50,  // 변경되지 않음
        },
        {
            name:           "0칩일 때 베팅 불가능",
            initialChips:   0,
            betAmount:      1,
            shouldSucceed:  false,
            finalChips:     0,
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            player := &Player{
                ID:    1,
                Name:  "Alice",
                Chips: tt.initialChips,
            }
            
            err := player.Bet(tt.betAmount)
            
            if tt.shouldSucceed && err != nil {
                t.Errorf("베팅이 성공해야 하는데 실패했습니다: %v", err)
            }
            
            if !tt.shouldSucceed && err == nil {
                t.Error("베팅이 실패해야 하는데 성공했습니다")
            }
            
            if player.Chips != tt.finalChips {
                t.Errorf("최종 칩 수 기댓값 %d, 실제 %d", tt.finalChips, player.Chips)
            }
        })
    }
}
```

## 25.3 Mock 객체 만들기

실제 게임 서버는 데이터베이스, 네트워크, 외부 서비스 등 많은 의존성을 가지고 있다. 단위 테스트에서는 이러한 의존성을 모의 객체(Mock)로 대체하여 테스트를 격리한다.

### 인터페이스 기반 Mock 설계

게임 서버의 메시지 전송을 테스트하는 예제를 보자.

```go
// message.go
package poker

// 메시지 전송 인터페이스
type MessageSender interface {
    SendMessage(playerID int, message string) error
    BroadcastMessage(message string) error
    Close() error
}

// 플레이어 매니저
type PlayerManager struct {
    players map[int]*Player
    sender  MessageSender
}

func NewPlayerManager(sender MessageSender) *PlayerManager {
    return &PlayerManager{
        players: make(map[int]*Player),
        sender:  sender,
    }
}

// 플레이어에게 게임 시작 알림
func (pm *PlayerManager) NotifyGameStart(playerID int) error {
    return pm.sender.SendMessage(playerID, "게임이 시작되었습니다")
}

// 모든 플레이어에게 알림
func (pm *PlayerManager) NotifyAllPlayers(message string) error {
    return pm.sender.BroadcastMessage(message)
}
```

이제 `MessageSender` 인터페이스의 Mock 구현을 만들자.

```go
// message_test.go
package poker

import (
    "testing"
)

// Mock 구현: 전송된 메시지를 기록하는 MessageSender
type MockMessageSender struct {
    sentMessages  map[int][]string  // playerID -> messages
    broadcasts    []string
    sendError     error
    broadcastError error
    closed        bool
}

func NewMockMessageSender() *MockMessageSender {
    return &MockMessageSender{
        sentMessages: make(map[int][]string),
        broadcasts:   make([]string, 0),
    }
}

// 메시지 전송 (Mock 구현)
func (m *MockMessageSender) SendMessage(playerID int, message string) error {
    if m.sendError != nil {
        return m.sendError
    }
    
    m.sentMessages[playerID] = append(m.sentMessages[playerID], message)
    return nil
}

// 브로드캐스트 (Mock 구현)
func (m *MockMessageSender) BroadcastMessage(message string) error {
    if m.broadcastError != nil {
        return m.broadcastError
    }
    
    m.broadcasts = append(m.broadcasts, message)
    return nil
}

// 닫기 (Mock 구현)
func (m *MockMessageSender) Close() error {
    m.closed = true
    return nil
}

// Mock 객체를 사용한 테스트
func TestPlayerManagerNotifyGameStart(t *testing.T) {
    // Mock 생성
    mockSender := NewMockMessageSender()
    manager := NewPlayerManager(mockSender)
    
    // 게임 시작 알림
    err := manager.NotifyGameStart(1)
    
    if err != nil {
        t.Fatalf("게임 시작 알림이 실패했습니다: %v", err)
    }
    
    // 메시지가 올바르게 전송되었는지 확인
    messages, exists := mockSender.sentMessages[1]
    if !exists {
        t.Error("플레이어 1에게 메시지가 전송되지 않았습니다")
    }
    
    if len(messages) != 1 {
        t.Errorf("메시지 개수 기댓값 1, 실제 %d", len(messages))
    }
    
    if messages[0] != "게임이 시작되었습니다" {
        t.Errorf("메시지 내용이 올바르지 않습니다: %s", messages[0])
    }
}

func TestPlayerManagerBroadcast(t *testing.T) {
    mockSender := NewMockMessageSender()
    manager := NewPlayerManager(mockSender)
    
    // 브로드캐스트 메시지 전송
    err := manager.NotifyAllPlayers("모든 플레이어를 위한 알림")
    
    if err != nil {
        t.Fatalf("브로드캐스트 실패: %v", err)
    }
    
    if len(mockSender.broadcasts) != 1 {
        t.Errorf("브로드캐스트 개수 기댓값 1, 실제 %d", len(mockSender.broadcasts))
    }
}
```

### 복잡한 Mock 객체

더 복잡한 시나리오를 위한 Mock을 만들어보자.

```go
// room.go
package poker

import (
    "errors"
)

type Room interface {
    AddPlayer(player *Player) error
    RemovePlayer(playerID int) error
    GetPlayers() []*Player
    Start() error
    Close() error
}

// 게임 룸 관리자
type RoomManager struct {
    rooms map[int]Room
    room  Room
}

func NewRoomManager(room Room) *RoomManager {
    return &RoomManager{
        rooms: make(map[int]Room),
        room:  room,
    }
}

// 플레이어를 룸에 추가
func (rm *RoomManager) JoinRoom(player *Player) error {
    if player.Chips < 100 {
        return errors.New("최소 100칩이 필요합니다")
    }
    return rm.room.AddPlayer(player)
}

// room_test.go
package poker

import (
    "testing"
)

// Mock Room 구현
type MockRoom struct {
    players     []*Player
    addError    error
    removeError error
    startError  error
    started     bool
}

func NewMockRoom() *MockRoom {
    return &MockRoom{
        players: make([]*Player, 0),
    }
}

func (m *MockRoom) AddPlayer(player *Player) error {
    if m.addError != nil {
        return m.addError
    }
    m.players = append(m.players, player)
    return nil
}

func (m *MockRoom) RemovePlayer(playerID int) error {
    if m.removeError != nil {
        return m.removeError
    }
    
    for i, p := range m.players {
        if p.ID == playerID {
            m.players = append(m.players[:i], m.players[i+1:]...)
            return nil
        }
    }
    return errors.New("플레이어를 찾을 수 없습니다")
}

func (m *MockRoom) GetPlayers() []*Player {
    return m.players
}

func (m *MockRoom) Start() error {
    if m.startError != nil {
        return m.startError
    }
    m.started = true
    return nil
}

func (m *MockRoom) Close() error {
    return nil
}

// Mock Room을 사용한 테스트
func TestRoomManagerJoinRoom(t *testing.T) {
    tests := []struct {
        name          string
        chips         int
        shouldSucceed bool
        addError      error
    }{
        {
            name:          "충분한 칩으로 입장 성공",
            chips:         1000,
            shouldSucceed: true,
            addError:      nil,
        },
        {
            name:          "부족한 칩으로 입장 실패",
            chips:         50,
            shouldSucceed: false,
            addError:      nil,
        },
        {
            name:          "룸이 가득 찬 경우",
            chips:         1000,
            shouldSucceed: false,
            addError:      errors.New("룸이 가득 찼습니다"),
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            mockRoom := NewMockRoom()
            mockRoom.addError = tt.addError
            
            manager := NewRoomManager(mockRoom)
            
            player := &Player{
                ID:    1,
                Name:  "Alice",
                Chips: tt.chips,
            }
            
            err := manager.JoinRoom(player)
            
            if tt.shouldSucceed && err != nil {
                t.Errorf("입장이 성공해야 하는데 실패했습니다: %v", err)
            }
            
            if !tt.shouldSucceed && err == nil {
                t.Error("입장이 실패해야 하는데 성공했습니다")
            }
        })
    }
}
```

## 25.4 테스트 커버리지

테스트 커버리지는 코드 중 테스트되는 부분의 비율을 나타낸다. 높은 커버리지는 더 많은 버그를 사전에 잡을 수 있음을 의미한다.

### 커버리지 측정

```bash
# 현재 디렉토리의 커버리지 측정
go test -cover

# 커버리지 통계 자세히 보기
go test -cover -v

# 커버리지 HTML 리포트 생성
go test -coverprofile=coverage.out
go tool cover -html=coverage.out
```

커버리지 리포트를 분석하는 코드를 보자.

```go
// eval.go
package poker

// 손 등급 정의
const (
    HighCard     = 1
    OnePair      = 2
    TwoPair      = 3
    ThreeOfAKind = 4
    Straight     = 5
    Flush        = 6
    FullHouse    = 7
    FourOfAKind  = 8
    StraightFlush = 9
)

// Hand 평가 로직
type Hand struct {
    Cards []Card
}

// 손에 있는 카드 개수 확인
func (h *Hand) CountRank(rank int) int {
    count := 0
    for _, card := range h.Cards {
        if card.Rank == rank {
            count++
        }
    }
    return count
}

// 페어 찾기
func (h *Hand) HasPair() bool {
    for rank := 2; rank <= 14; rank++ {
        if h.CountRank(rank) >= 2 {
            return true
        }
    }
    return false
}

// 플러시 확인
func (h *Hand) HasFlush() bool {
    suitCount := make(map[int]int)
    for _, card := range h.Cards {
        suitCount[card.Suit]++
        if suitCount[card.Suit] >= 5 {
            return true
        }
    }
    return false
}

// eval_test.go
package poker

import (
    "testing"
)

func TestCountRank(t *testing.T) {
    hand := &Hand{
        Cards: []Card{
            {Rank: 10, Suit: 0},
            {Rank: 10, Suit: 1},
            {Rank: 10, Suit: 2},
            {Rank: 5, Suit: 3},
            {Rank: 3, Suit: 0},
        },
    }
    
    // Rank 10의 개수 확인
    if count := hand.CountRank(10); count != 3 {
        t.Errorf("Rank 10의 개수 기댓값 3, 실제 %d", count)
    }
    
    // 없는 Rank 확인
    if count := hand.CountRank(14); count != 0 {
        t.Errorf("Rank 14의 개수는 0이어야 하는데 %d", count)
    }
}

func TestHasPair(t *testing.T) {
    tests := []struct {
        name      string
        cards     []Card
        hasPair   bool
    }{
        {
            name: "페어 있음",
            cards: []Card{
                {Rank: 10, Suit: 0},
                {Rank: 10, Suit: 1},
                {Rank: 5, Suit: 2},
                {Rank: 3, Suit: 3},
                {Rank: 2, Suit: 0},
            },
            hasPair: true,
        },
        {
            name: "페어 없음",
            cards: []Card{
                {Rank: 14, Suit: 0},
                {Rank: 12, Suit: 1},
                {Rank: 10, Suit: 2},
                {Rank: 8, Suit: 3},
                {Rank: 6, Suit: 0},
            },
            hasPair: false,
        },
        {
            name: "스리 오브 어 카인드",
            cards: []Card{
                {Rank: 10, Suit: 0},
                {Rank: 10, Suit: 1},
                {Rank: 10, Suit: 2},
                {Rank: 3, Suit: 3},
                {Rank: 2, Suit: 0},
            },
            hasPair: true,
        },
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            hand := &Hand{Cards: tt.cards}
            if hand.HasPair() != tt.hasPair {
                t.Errorf("기댓값 %v, 실제 %v", tt.hasPair, hand.HasPair())
            }
        })
    }
}

func TestHasFlush(t *testing.T) {
    // 모두 스페이드
    flushHand := &Hand{
        Cards: []Card{
            {Rank: 14, Suit: 0},
            {Rank: 12, Suit: 0},
            {Rank: 10, Suit: 0},
            {Rank: 8, Suit: 0},
            {Rank: 6, Suit: 0},
        },
    }
    
    if !flushHand.HasFlush() {
        t.Error("플러시를 감지해야 합니다")
    }
    
    // 플러시가 아님
    noFlushHand := &Hand{
        Cards: []Card{
            {Rank: 14, Suit: 0},
            {Rank: 12, Suit: 1},
            {Rank: 10, Suit: 2},
            {Rank: 8, Suit: 3},
            {Rank: 6, Suit: 0},
        },
    }
    
    if noFlushHand.HasFlush() {
        t.Error("플러시를 감지하면 안 됩니다")
    }
}
```

커버리지 프로필을 생성하고 분석하자.

```bash
# 커버리지 프로필 생성
go test -coverprofile=coverage.out ./...

# 커버리지 통계 출력
go tool cover -func=coverage.out

# HTML 리포트 생성 및 브라우저에서 보기
go tool cover -html=coverage.out -o coverage.html
```

커버리지를 높이기 위해 테스트해야 할 엣지 케이스를 찾아 추가 테스트를 작성한다.

```go
// edge_case_test.go
package poker

import (
    "testing"
)

// 엣지 케이스: 빈 손
func TestCountRankEmptyHand(t *testing.T) {
    hand := &Hand{Cards: []Card{}}
    if count := hand.CountRank(10); count != 0 {
        t.Error("빈 손에서 카드를 찾으면 안 됩니다")
    }
}

// 엣지 케이스: 1장의 카드
func TestHasPairSingleCard(t *testing.T) {
    hand := &Hand{
        Cards: []Card{{Rank: 10, Suit: 0}},
    }
    if hand.HasPair() {
        t.Error("1장의 카드로는 페어를 만들 수 없습니다")
    }
}

// 엣지 케이스: 모든 카드가 같은 Rank
func TestHasFlushAllSameSuit(t *testing.T) {
    hand := &Hand{
        Cards: []Card{
            {Rank: 2, Suit: 0},
            {Rank: 3, Suit: 0},
            {Rank: 4, Suit: 0},
            {Rank: 5, Suit: 0},
            {Rank: 6, Suit: 0},
        },
    }
    if !hand.HasFlush() {
        t.Error("모든 카드가 같은 슈트일 때 플러시를 감지해야 합니다")
    }
}
```

## 25.5 벤치마크 작성

벤치마크는 함수의 성능을 측정하고 최적화의 효과를 검증한다.

### 기본 벤치마크

```go
// bench_test.go
package poker

import (
    "testing"
)

// CountRank의 성능 측정
func BenchmarkCountRank(b *testing.B) {
    hand := &Hand{
        Cards: []Card{
            {Rank: 10, Suit: 0},
            {Rank: 10, Suit: 1},
            {Rank: 10, Suit: 2},
            {Rank: 5, Suit: 3},
            {Rank: 3, Suit: 0},
        },
    }
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        hand.CountRank(10)
    }
}

// HasPair의 성능 측정
func BenchmarkHasPair(b *testing.B) {
    hand := &Hand{
        Cards: []Card{
            {Rank: 10, Suit: 0},
            {Rank: 10, Suit: 1},
            {Rank: 5, Suit: 2},
            {Rank: 3, Suit: 3},
            {Rank: 2, Suit: 0},
        },
    }
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        hand.HasPair()
    }
}

// HasFlush의 성능 측정
func BenchmarkHasFlush(b *testing.B) {
    hand := &Hand{
        Cards: []Card{
            {Rank: 14, Suit: 0},
            {Rank: 12, Suit: 0},
            {Rank: 10, Suit: 0},
            {Rank: 8, Suit: 0},
            {Rank: 6, Suit: 0},
        },
    }
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        hand.HasFlush()
    }
}
```

벤치마크 실행:

```bash
# 기본 벤치마크 실행
go test -bench=. -benchmem

# 특정 벤치마크만 실행
go test -bench=BenchmarkCountRank

# 더 오래 실행 (기본: 1초)
go test -bench=. -benchtime=3s

# 비교를 위해 여러 번 실행
go test -bench=. -count=5
```

### 복잡한 벤치마크

여러 입력 크기에 대한 벤치마크를 작성하자.

```go
// complex_bench_test.go
package poker

import (
    "testing"
)

// 다양한 손 크기에 대한 벤치마크
func BenchmarkCountRankDifferentHandSizes(b *testing.B) {
    sizes := []int{5, 10, 20, 50}
    
    for _, size := range sizes {
        b.Run(string(rune(size)), func(b *testing.B) {
            cards := make([]Card, size)
            for i := 0; i < size; i++ {
                cards[i] = Card{Rank: (i % 13) + 2, Suit: i % 4}
            }
            
            hand := &Hand{Cards: cards}
            
            b.ResetTimer()
            
            for i := 0; i < b.N; i++ {
                hand.CountRank(10)
            }
        })
    }
}

// 최적화 전후 비교
func countRankOptimized(hand *Hand, rank int) int {
    // 캐싱을 사용한 최적화
    cache := make(map[int]int)
    for _, card := range hand.Cards {
        cache[card.Rank]++
    }
    return cache[rank]
}

func BenchmarkCountRankOptimized(b *testing.B) {
    hand := &Hand{
        Cards: []Card{
            {Rank: 10, Suit: 0},
            {Rank: 10, Suit: 1},
            {Rank: 10, Suit: 2},
            {Rank: 5, Suit: 3},
            {Rank: 3, Suit: 0},
        },
    }
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        countRankOptimized(hand, 10)
    }
}
```

벤치마크 결과를 저장하고 비교하자.

```bash
# 벤치마크 결과를 파일로 저장
go test -bench=. -benchmem > benchmark_old.txt

# 코드 변경 후 다시 실행
go test -bench=. -benchmem > benchmark_new.txt

# 벤치마크 비교 도구 설치
go install golang.org/x/perf/cmd/benchstat@latest

# 결과 비교
benchstat benchmark_old.txt benchmark_new.txt
```

### 게임 서버 벤치마크 예제

실제 게임 서버의 핵심 로직을 벤치마크한다.

```go
// game_bench_test.go
package poker

import (
    "testing"
)

// 베팅 처리의 성능 측정
func BenchmarkPlayerBet(b *testing.B) {
    player := &Player{
        ID:    1,
        Name:  "Alice",
        Chips: 1000000,
    }
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        player.Bet(100)
        player.AddChips(100)  // 복구
    }
}

// Mock을 사용한 벤치마크
func BenchmarkNotifyGameStart(b *testing.B) {
    mockSender := NewMockMessageSender()
    manager := NewPlayerManager(mockSender)
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        manager.NotifyGameStart(1)
    }
}

// 룸 입장의 성능 측정
func BenchmarkJoinRoom(b *testing.B) {
    mockRoom := NewMockRoom()
    manager := NewRoomManager(mockRoom)
    
    player := &Player{
        ID:    1,
        Name:  "Alice",
        Chips: 1000,
    }
    
    b.ResetTimer()
    
    for i := 0; i < b.N; i++ {
        // 플레이어 칩 복구
        player.Chips = 1000
        manager.JoinRoom(player)
    }
}
```

---

## 요약

단위 테스트는 게임 서버의 핵심 로직이 정확하게 작동하는지 보장하는 필수 요소다.

Go의 `testing` 패키지는 간단하면서도 강력한 테스트 프레임워크를 제공한다. `testing.T`의 메서드들(`Error`, `Fatal`, `Log` 등)을 통해 테스트 결과를 명확하게 보고할 수 있다.

테이블 기반 테스트는 여러 입력값에 대한 테스트를 효율적으로 작성하고 유지보수할 수 있는 패턴이다. 각 케이스를 테이블의 행으로 추가하기만 하면 되므로 코드 중복을 줄이고 가독성을 높인다.

Mock 객체를 사용하면 외부 의존성을 제거하고 순수한 로직만 테스트할 수 있다. 게임 서버의 메시지 전송, 룸 관리 등 복잡한 시나리오를 Mock으로 격리하여 테스트한다.

테스트 커버리지를 측정하고 분석하면 테스트되지 않는 코드를 찾아 추가 테스트를 작성할 수 있다. 벤치마크는 함수의 성능을 측정하고 최적화의 효과를 정량적으로 검증한다.

이러한 기법들을 종합적으로 활용하면 안정적이고 신뢰할 수 있는 게임 서버를 구축할 수 있다.

  
# Chapter 26. 통합 테스트

통합 테스트는 여러 컴포넌트가 함께 작동하는 시나리오를 검증하는 테스트다. 단위 테스트가 개별 함수의 정확성을 확인한다면, 통합 테스트는 네트워크 통신, 게임 로직, 상태 관리가 함께 정상적으로 작동하는지 확인한다. 게임 서버에서는 플레이어 로그인부터 게임 종료까지의 전체 흐름을 통합 테스트로 검증해야 한다.

## 26.1 테스트 클라이언트 구현

### 간단한 테스트 클라이언트

게임 서버와 통신하는 테스트 클라이언트를 만들자. 실제 클라이언트 대신 자동화된 테스트 클라이언트로 서버의 동작을 검증할 수 있다.

```go
// server.go
package main

import (
    "bufio"
    "fmt"
    "net"
    "strings"
)

// 간단한 게임 서버
type GameServer struct {
    listener net.Listener
    done     chan bool
}

func NewGameServer(addr string) (*GameServer, error) {
    listener, err := net.Listen("tcp", addr)
    if err != nil {
        return nil, err
    }
    
    return &GameServer{
        listener: listener,
        done:     make(chan bool),
    }, nil
}

func (s *GameServer) Start() {
    go func() {
        for {
            conn, err := s.listener.Accept()
            if err != nil {
                return
            }
            go s.handleConnection(conn)
        }
    }()
}

func (s *GameServer) handleConnection(conn net.Conn) {
    defer conn.Close()
    
    reader := bufio.NewReader(conn)
    writer := bufio.NewWriter(conn)
    
    for {
        // 클라이언트로부터 메시지 수신
        message, err := reader.ReadString('\n')
        if err != nil {
            return
        }
        
        message = strings.TrimSpace(message)
        
        // 명령어 처리
        response := s.processCommand(message)
        
        // 응답 전송
        fmt.Fprintf(writer, "%s\n", response)
        writer.Flush()
    }
}

func (s *GameServer) processCommand(cmd string) string {
    parts := strings.Fields(cmd)
    
    if len(parts) == 0 {
        return "ERROR: 빈 명령어"
    }
    
    switch parts[0] {
    case "LOGIN":
        if len(parts) < 2 {
            return "ERROR: 플레이어명이 필요합니다"
        }
        return fmt.Sprintf("OK: %s가 로그인했습니다", parts[1])
    
    case "JOIN_ROOM":
        if len(parts) < 2 {
            return "ERROR: 방 ID가 필요합니다"
        }
        return fmt.Sprintf("OK: 방 %s에 입장했습니다", parts[1])
    
    case "START_GAME":
        return "OK: 게임이 시작되었습니다"
    
    case "BET":
        if len(parts) < 2 {
            return "ERROR: 베팅 금액이 필요합니다"
        }
        return fmt.Sprintf("OK: %s칩 베팅했습니다", parts[1])
    
    case "LOGOUT":
        return "OK: 로그아웃했습니다"
    
    default:
        return "ERROR: 알 수 없는 명령어"
    }
}

func (s *GameServer) Close() error {
    return s.listener.Close()
}
```

이제 위 서버와 통신하는 테스트 클라이언트를 만들자.

```go
// client.go
package main

import (
    "bufio"
    "fmt"
    "net"
    "strings"
)

// 테스트용 게임 클라이언트
type GameClient struct {
    conn   net.Conn
    reader *bufio.Reader
    writer *bufio.Writer
}

func NewGameClient(addr string) (*GameClient, error) {
    conn, err := net.Dial("tcp", addr)
    if err != nil {
        return nil, err
    }
    
    return &GameClient{
        conn:   conn,
        reader: bufio.NewReader(conn),
        writer: bufio.NewWriter(conn),
    }, nil
}

// 명령어 전송 및 응답 받기
func (gc *GameClient) SendCommand(cmd string) (string, error) {
    // 명령어 전송
    fmt.Fprintf(gc.writer, "%s\n", cmd)
    if err := gc.writer.Flush(); err != nil {
        return "", err
    }
    
    // 응답 수신
    response, err := gc.reader.ReadString('\n')
    if err != nil {
        return "", err
    }
    
    return strings.TrimSpace(response), nil
}

func (gc *GameClient) Close() error {
    return gc.conn.Close()
}
```

테스트 클라이언트를 사용한 통합 테스트를 작성하자.

```go
// integration_test.go
package main

import (
    "testing"
)

// 기본 로그인 테스트
func TestBasicLogin(t *testing.T) {
    // 테스트 서버 시작
    server, err := NewGameServer("127.0.0.1:0")  // 포트 0 = 자동 할당
    if err != nil {
        t.Fatalf("서버 시작 실패: %v", err)
    }
    defer server.Close()
    
    server.Start()
    
    // 테스트 클라이언트 연결
    client, err := NewGameClient(server.listener.Addr().String())
    if err != nil {
        t.Fatalf("클라이언트 연결 실패: %v", err)
    }
    defer client.Close()
    
    // 로그인 명령어 전송
    response, err := client.SendCommand("LOGIN Alice")
    if err != nil {
        t.Fatalf("명령어 전송 실패: %v", err)
    }
    
    // 응답 검증
    if !strings.Contains(response, "OK") {
        t.Errorf("로그인 실패: %s", response)
    }
}

// 에러 처리 테스트
func TestLoginWithoutPlayerName(t *testing.T) {
    server, err := NewGameServer("127.0.0.1:0")
    if err != nil {
        t.Fatalf("서버 시작 실패: %v", err)
    }
    defer server.Close()
    
    server.Start()
    
    client, err := NewGameClient(server.listener.Addr().String())
    if err != nil {
        t.Fatalf("클라이언트 연결 실패: %v", err)
    }
    defer client.Close()
    
    // 플레이어명 없이 로그인 시도
    response, err := client.SendCommand("LOGIN")
    if err != nil {
        t.Fatalf("명령어 전송 실패: %v", err)
    }
    
    // 에러 응답 검증
    if !strings.Contains(response, "ERROR") {
        t.Errorf("에러를 기대했는데 성공 응답: %s", response)
    }
}
```

### 테스트 서버 헬퍼

테스트마다 서버를 생성하고 정리하는 것을 자동화하는 헬퍼를 만들자.

```go
// test_helper.go
package main

import (
    "net"
    "testing"
)

// 테스트 서버 래퍼
type TestServerWrapper struct {
    server *GameServer
    addr   string
    t      *testing.T
}

// 테스트 서버 생성
func StartTestServer(t *testing.T) *TestServerWrapper {
    server, err := NewGameServer("127.0.0.1:0")
    if err != nil {
        t.Fatalf("테스트 서버 시작 실패: %v", err)
    }
    
    server.Start()
    
    return &TestServerWrapper{
        server: server,
        addr:   server.listener.Addr().String(),
        t:      t,
    }
}

// 테스트 클라이언트 생성
func (tw *TestServerWrapper) CreateClient() *GameClient {
    client, err := NewGameClient(tw.addr)
    if err != nil {
        tw.t.Fatalf("테스트 클라이언트 생성 실패: %v", err)
    }
    return client
}

// 정리
func (tw *TestServerWrapper) Close() {
    if err := tw.server.Close(); err != nil {
        tw.t.Errorf("서버 종료 실패: %v", err)
    }
}

// 헬퍼를 사용한 테스트
func TestWithHelper(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    client := ts.CreateClient()
    defer client.Close()
    
    response, err := client.SendCommand("LOGIN Bob")
    if err != nil {
        t.Fatalf("명령어 전송 실패: %v", err)
    }
    
    if !strings.Contains(response, "OK") {
        t.Errorf("예상과 다른 응답: %s", response)
    }
}
```

## 26.2 시나리오 테스트

### 시나리오 기반 테스트

실제 게임 플레이 시나리오를 재현하는 테스트를 작성하자.

```go
// game_logic.go
package main

import (
    "errors"
    "sync"
)

// 게임 서버의 상태를 관리하는 구조체
type GameServerWithState struct {
    listener net.Listener
    players  map[string]*Player
    rooms    map[string]*Room
    mu       sync.RWMutex
    done     chan bool
}

type Player struct {
    ID    string
    Name  string
    Chips int
    Room  string
}

type Room struct {
    ID      string
    Players map[string]*Player
    Status  string  // "waiting", "playing", "finished"
}

func NewGameServerWithState(addr string) (*GameServerWithState, error) {
    listener, err := net.Listen("tcp", addr)
    if err != nil {
        return nil, err
    }
    
    return &GameServerWithState{
        listener: listener,
        players:  make(map[string]*Player),
        rooms:    make(map[string]*Room),
        done:     make(chan bool),
    }, nil
}

// 플레이어 추가
func (gs *GameServerWithState) AddPlayer(playerID, name string, chips int) error {
    gs.mu.Lock()
    defer gs.mu.Unlock()
    
    if _, exists := gs.players[playerID]; exists {
        return errors.New("이미 존재하는 플레이어")
    }
    
    gs.players[playerID] = &Player{
        ID:    playerID,
        Name:  name,
        Chips: chips,
    }
    
    return nil
}

// 방 생성
func (gs *GameServerWithState) CreateRoom(roomID string) error {
    gs.mu.Lock()
    defer gs.mu.Unlock()
    
    if _, exists := gs.rooms[roomID]; exists {
        return errors.New("이미 존재하는 방")
    }
    
    gs.rooms[roomID] = &Room{
        ID:      roomID,
        Players: make(map[string]*Player),
        Status:  "waiting",
    }
    
    return nil
}

// 방에 플레이어 추가
func (gs *GameServerWithState) JoinRoom(playerID, roomID string) error {
    gs.mu.Lock()
    defer gs.mu.Unlock()
    
    player, exists := gs.players[playerID]
    if !exists {
        return errors.New("플레이어를 찾을 수 없음")
    }
    
    room, exists := gs.rooms[roomID]
    if !exists {
        return errors.New("방을 찾을 수 없음")
    }
    
    room.Players[playerID] = player
    player.Room = roomID
    
    return nil
}

// 베팅 처리
func (gs *GameServerWithState) PlaceBet(playerID string, amount int) error {
    gs.mu.Lock()
    defer gs.mu.Unlock()
    
    player, exists := gs.players[playerID]
    if !exists {
        return errors.New("플레이어를 찾을 수 없음")
    }
    
    if player.Chips < amount {
        return errors.New("칩이 부족함")
    }
    
    player.Chips -= amount
    return nil
}

// 플레이어 칩 조회
func (gs *GameServerWithState) GetPlayerChips(playerID string) (int, error) {
    gs.mu.RLock()
    defer gs.mu.RUnlock()
    
    player, exists := gs.players[playerID]
    if !exists {
        return 0, errors.New("플레이어를 찾을 수 없음")
    }
    
    return player.Chips, nil
}

// 방의 상태 변경
func (gs *GameServerWithState) StartGame(roomID string) error {
    gs.mu.Lock()
    defer gs.mu.Unlock()
    
    room, exists := gs.rooms[roomID]
    if !exists {
        return errors.New("방을 찾을 수 없음")
    }
    
    room.Status = "playing"
    return nil
}
```

위의 게임 로직을 기반으로 시나리오 테스트를 작성하자.

```go
// scenario_test.go
package main

import (
    "testing"
)

// 시나리오 1: 플레이어 로그인 및 방 입장
func TestScenarioLoginAndJoinRoom(t *testing.T) {
    // 게임 서버 초기화
    server, err := NewGameServerWithState("127.0.0.1:0")
    if err != nil {
        t.Fatalf("서버 생성 실패: %v", err)
    }
    defer server.listener.Close()
    
    // 시나리오:
    // 1. Alice와 Bob이 로그인
    // 2. 방 1을 생성
    // 3. 두 플레이어가 방에 입장
    
    // Step 1: 플레이어 생성
    if err := server.AddPlayer("player1", "Alice", 1000); err != nil {
        t.Fatalf("Alice 추가 실패: %v", err)
    }
    
    if err := server.AddPlayer("player2", "Bob", 1000); err != nil {
        t.Fatalf("Bob 추가 실패: %v", err)
    }
    
    // Step 2: 방 생성
    if err := server.CreateRoom("room1"); err != nil {
        t.Fatalf("방 생성 실패: %v", err)
    }
    
    // Step 3: 방에 입장
    if err := server.JoinRoom("player1", "room1"); err != nil {
        t.Fatalf("Alice 입장 실패: %v", err)
    }
    
    if err := server.JoinRoom("player2", "room1"); err != nil {
        t.Fatalf("Bob 입장 실패: %v", err)
    }
    
    // 검증: 두 플레이어가 방에 있는지 확인
    if len(server.rooms["room1"].Players) != 2 {
        t.Errorf("방에 있는 플레이어 수 기댓값 2, 실제 %d", 
            len(server.rooms["room1"].Players))
    }
}

// 시나리오 2: 베팅 및 칩 관리
func TestScenarioBettingAndChips(t *testing.T) {
    server, err := NewGameServerWithState("127.0.0.1:0")
    if err != nil {
        t.Fatalf("서버 생성 실패: %v", err)
    }
    defer server.listener.Close()
    
    // Step 1: 플레이어 생성
    if err := server.AddPlayer("player1", "Alice", 1000); err != nil {
        t.Fatalf("Alice 추가 실패: %v", err)
    }
    
    // Step 2: 베팅 - 첫 번째 베팅
    if err := server.PlaceBet("player1", 100); err != nil {
        t.Fatalf("첫 번째 베팅 실패: %v", err)
    }
    
    chips, err := server.GetPlayerChips("player1")
    if err != nil {
        t.Fatalf("칩 조회 실패: %v", err)
    }
    
    if chips != 900 {
        t.Errorf("칩 기댓값 900, 실제 %d", chips)
    }
    
    // Step 3: 베팅 - 두 번째 베팅
    if err := server.PlaceBet("player1", 200); err != nil {
        t.Fatalf("두 번째 베팅 실패: %v", err)
    }
    
    chips, err = server.GetPlayerChips("player1")
    if err != nil {
        t.Fatalf("칩 조회 실패: %v", err)
    }
    
    if chips != 700 {
        t.Errorf("칩 기댓값 700, 실제 %d", chips)
    }
    
    // Step 4: 부족한 칩으로 베팅 시도
    err = server.PlaceBet("player1", 1000)
    if err == nil {
        t.Error("부족한 칩으로 베팅이 성공하면 안 됨")
    }
}

// 시나리오 3: 게임 시작
func TestScenarioGameFlow(t *testing.T) {
    server, err := NewGameServerWithState("127.0.0.1:0")
    if err != nil {
        t.Fatalf("서버 생성 실패: %v", err)
    }
    defer server.listener.Close()
    
    // Step 1: 플레이어 생성
    if err := server.AddPlayer("player1", "Alice", 1000); err != nil {
        t.Fatalf("Alice 추가 실패: %v", err)
    }
    
    if err := server.AddPlayer("player2", "Bob", 1000); err != nil {
        t.Fatalf("Bob 추가 실패: %v", err)
    }
    
    // Step 2: 방 생성 및 입장
    if err := server.CreateRoom("room1"); err != nil {
        t.Fatalf("방 생성 실패: %v", err)
    }
    
    if err := server.JoinRoom("player1", "room1"); err != nil {
        t.Fatalf("Alice 입장 실패: %v", err)
    }
    
    if err := server.JoinRoom("player2", "room1"); err != nil {
        t.Fatalf("Bob 입장 실패: %v", err)
    }
    
    // Step 3: 게임 시작
    if err := server.StartGame("room1"); err != nil {
        t.Fatalf("게임 시작 실패: %v", err)
    }
    
    // Step 4: 베팅
    if err := server.PlaceBet("player1", 100); err != nil {
        t.Fatalf("Alice 베팅 실패: %v", err)
    }
    
    if err := server.PlaceBet("player2", 100); err != nil {
        t.Fatalf("Bob 베팅 실패: %v", err)
    }
    
    // 검증: 베팅 후 칩 확인
    aliceChips, _ := server.GetPlayerChips("player1")
    bobChips, _ := server.GetPlayerChips("player2")
    
    if aliceChips != 900 {
        t.Errorf("Alice의 칩 기댓값 900, 실제 %d", aliceChips)
    }
    
    if bobChips != 900 {
        t.Errorf("Bob의 칩 기댓값 900, 실제 %d", bobChips)
    }
}
```

## 26.3 부하 테스트 도구 작성

### 기본 부하 테스트

여러 클라이언트가 동시에 접속하는 상황을 시뮬레이션하는 부하 테스트 도구를 만들자.

```go
// load_test.go
package main

import (
    "fmt"
    "sync"
    "testing"
    "time"
)

// 부하 테스트 통계
type LoadTestStats struct {
    mu                sync.Mutex
    successCount      int
    failureCount      int
    totalResponseTime time.Duration
    minResponseTime   time.Duration
    maxResponseTime   time.Duration
    startTime         time.Time
}

func NewLoadTestStats() *LoadTestStats {
    return &LoadTestStats{
        minResponseTime: time.Hour,
        startTime:       time.Now(),
    }
}

// 성공 기록
func (lts *LoadTestStats) RecordSuccess(responseTime time.Duration) {
    lts.mu.Lock()
    defer lts.mu.Unlock()
    
    lts.successCount++
    lts.totalResponseTime += responseTime
    
    if responseTime < lts.minResponseTime {
        lts.minResponseTime = responseTime
    }
    if responseTime > lts.maxResponseTime {
        lts.maxResponseTime = responseTime
    }
}

// 실패 기록
func (lts *LoadTestStats) RecordFailure() {
    lts.mu.Lock()
    defer lts.mu.Unlock()
    
    lts.failureCount++
}

// 평균 응답 시간 계산
func (lts *LoadTestStats) GetAverageResponseTime() time.Duration {
    lts.mu.Lock()
    defer lts.mu.Unlock()
    
    if lts.successCount == 0 {
        return 0
    }
    
    return lts.totalResponseTime / time.Duration(lts.successCount)
}

// 통계 출력
func (lts *LoadTestStats) PrintStats() {
    lts.mu.Lock()
    defer lts.mu.Unlock()
    
    totalTime := time.Since(lts.startTime)
    totalRequests := lts.successCount + lts.failureCount
    successRate := float64(lts.successCount) / float64(totalRequests) * 100
    
    fmt.Println("=== 부하 테스트 결과 ===")
    fmt.Printf("총 요청 수: %d\n", totalRequests)
    fmt.Printf("성공: %d, 실패: %d\n", lts.successCount, lts.failureCount)
    fmt.Printf("성공률: %.2f%%\n", successRate)
    fmt.Printf("평균 응답 시간: %v\n", lts.GetAverageResponseTime())
    fmt.Printf("최소 응답 시간: %v\n", lts.minResponseTime)
    fmt.Printf("최대 응답 시간: %v\n", lts.maxResponseTime)
    fmt.Printf("총 실행 시간: %v\n", totalTime)
    fmt.Printf("초당 처리량: %.2f req/sec\n", 
        float64(totalRequests)/totalTime.Seconds())
}

// 부하 테스트: 단일 스레드에서 순차 요청
func TestLoadTestSequential(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    stats := NewLoadTestStats()
    clientCount := 100
    
    for i := 0; i < clientCount; i++ {
        client := ts.CreateClient()
        
        start := time.Now()
        response, err := client.SendCommand("LOGIN Player")
        elapsed := time.Since(start)
        
        if err != nil || !contains(response, "OK") {
            stats.RecordFailure()
        } else {
            stats.RecordSuccess(elapsed)
        }
        
        client.Close()
    }
    
    stats.PrintStats()
}

// 부하 테스트: 다중 스레드에서 동시 요청
func TestLoadTestConcurrent(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    stats := NewLoadTestStats()
    clientCount := 50
    concurrency := 10
    
    var wg sync.WaitGroup
    semaphore := make(chan struct{}, concurrency)
    
    for i := 0; i < clientCount; i++ {
        wg.Add(1)
        go func() {
            defer wg.Done()
            
            semaphore <- struct{}{}  // 세마포어 획득
            defer func() { <-semaphore }()
            
            client := ts.CreateClient()
            defer client.Close()
            
            start := time.Now()
            response, err := client.SendCommand("LOGIN Player")
            elapsed := time.Since(start)
            
            if err != nil || !contains(response, "OK") {
                stats.RecordFailure()
            } else {
                stats.RecordSuccess(elapsed)
            }
        }()
    }
    
    wg.Wait()
    stats.PrintStats()
}

// 헬퍼 함수
func contains(s, substr string) bool {
    return strings.Contains(s, substr)
}
```

## 26.4 동시 접속 테스트

### 동시 연결 유지 및 통신 테스트

실제 게임 서버처럼 여러 클라이언트가 동시에 연결을 유지하면서 통신하는 상황을 테스트하자.

```go
// concurrent_test.go
package main

import (
    "fmt"
    "sync"
    "sync/atomic"
    "testing"
    "time"
)

// 동시 접속 테스트를 위한 클라이언트 시뮬레이터
type ConcurrentClientSimulator struct {
    client       *GameClient
    playerID     string
    commandCount int32
    lastError    error
    mu           sync.Mutex
}

func NewConcurrentClientSimulator(addr, playerID string) (*ConcurrentClientSimulator, error) {
    client, err := NewGameClient(addr)
    if err != nil {
        return nil, err
    }
    
    return &ConcurrentClientSimulator{
        client:   client,
        playerID: playerID,
    }, nil
}

// 주기적으로 명령어 전송
func (ccs *ConcurrentClientSimulator) SendCommandsPeriodically(
    duration time.Duration, 
    interval time.Duration, 
    commands []string) {
    
    ticker := time.NewTicker(interval)
    defer ticker.Stop()
    
    deadline := time.Now().Add(duration)
    commandIdx := 0
    
    for range ticker.C {
        if time.Now().After(deadline) {
            break
        }
        
        cmd := commands[commandIdx%len(commands)]
        _, err := ccs.client.SendCommand(cmd)
        
        ccs.mu.Lock()
        if err != nil {
            ccs.lastError = err
        } else {
            atomic.AddInt32(&ccs.commandCount, 1)
        }
        ccs.mu.Unlock()
        
        commandIdx++
    }
}

func (ccs *ConcurrentClientSimulator) GetCommandCount() int32 {
    return atomic.LoadInt32(&ccs.commandCount)
}

func (ccs *ConcurrentClientSimulator) GetLastError() error {
    ccs.mu.Lock()
    defer ccs.mu.Unlock()
    return ccs.lastError
}

func (ccs *ConcurrentClientSimulator) Close() {
    ccs.client.Close()
}

// 테스트: 10개의 동시 클라이언트가 각각 명령어를 주기적으로 전송
func TestConcurrentConnections(t *testing.T) {
    // 테스트 서버 시작
    ts := StartTestServer(t)
    defer ts.Close()
    
    clientCount := 10
    duration := 5 * time.Second
    interval := 100 * time.Millisecond
    
    commands := []string{
        "LOGIN Alice",
        "JOIN_ROOM room1",
        "BET 100",
    }
    
    simulators := make([]*ConcurrentClientSimulator, clientCount)
    var wg sync.WaitGroup
    
    // 클라이언트 생성 및 시작
    for i := 0; i < clientCount; i++ {
        playerID := fmt.Sprintf("player%d", i)
        
        sim, err := NewConcurrentClientSimulator(ts.addr, playerID)
        if err != nil {
            t.Fatalf("클라이언트 생성 실패: %v", err)
        }
        
        simulators[i] = sim
        
        // 각 클라이언트가 명령어 주기적으로 전송
        wg.Add(1)
        go func(s *ConcurrentClientSimulator) {
            defer wg.Done()
            s.SendCommandsPeriodically(duration, interval, commands)
        }(sim)
    }
    
    wg.Wait()
    
    // 결과 검증
    totalCommands := int32(0)
    for i, sim := range simulators {
        sim.Close()
        
        count := sim.GetCommandCount()
        totalCommands += count
        
        err := sim.GetLastError()
        if err != nil {
            t.Errorf("클라이언트 %d 에러: %v", i, err)
        }
        
        // 각 클라이언트가 대략 50개 명령어를 전송했는지 확인
        // (5초 / 100ms = 50개, 약간의 오차 허용)
        if count < 40 || count > 60 {
            t.Logf("클라이언트 %d 명령어 수: %d (기댓값: ~50)", i, count)
        }
    }
    
    fmt.Printf("총 전송된 명령어: %d\n", totalCommands)
    fmt.Printf("평균 명령어/클라이언트: %d\n", totalCommands/int32(clientCount))
}

// 테스트: 스트레스 테스트 (많은 수의 동시 연결)
func TestStressTest(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    clientCount := 100
    
    var wg sync.WaitGroup
    successCount := int32(0)
    failureCount := int32(0)
    
    for i := 0; i < clientCount; i++ {
        wg.Add(1)
        go func(index int) {
            defer wg.Done()
            
            client := ts.CreateClient()
            defer client.Close()
            
            // 여러 명령어 실행
            commands := []string{
                fmt.Sprintf("LOGIN Player%d", index),
                fmt.Sprintf("JOIN_ROOM room%d", index%5),
                fmt.Sprintf("BET 100"),
            }
            
            for _, cmd := range commands {
                response, err := client.SendCommand(cmd)
                if err != nil || !contains(response, "OK") {
                    atomic.AddInt32(&failureCount, 1)
                    return
                }
            }
            
            atomic.AddInt32(&successCount, 1)
        }(i)
    }
    
    wg.Wait()
    
    fmt.Printf("성공: %d, 실패: %d\n", successCount, failureCount)
    
    if failureCount > int32(clientCount)*10/100 {  // 10% 이상 실패
        t.Errorf("실패율이 높습니다: %d/%d", failureCount, clientCount)
    }
}
```

## 26.5 엣지 케이스 테스트

### 예상치 못한 상황 처리

엣지 케이스는 정상적인 사용 패턴을 벗어난 상황을 말한다. 게임 서버는 이러한 엣지 케이스를 우아하게 처리해야 한다.

```go
// edge_case_test.go
package main

import (
    "net"
    "strings"
    "testing"
    "time"
)

// 엣지 케이스 1: 빠른 연결/종료
func TestRapidConnectDisconnect(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    for i := 0; i < 20; i++ {
        client := ts.CreateClient()
        client.Close()
    }
    
    // 여전히 정상 작동하는지 확인
    client := ts.CreateClient()
    defer client.Close()
    
    response, err := client.SendCommand("LOGIN Test")
    if err != nil || !contains(response, "OK") {
        t.Error("빠른 연결/종료 후 정상 작동 실패")
    }
}

// 엣지 케이스 2: 매우 긴 명령어
func TestVeryLongCommand(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    client := ts.CreateClient()
    defer client.Close()
    
    // 매우 긴 명령어 생성
    longCommand := "LOGIN " + strings.Repeat("A", 10000)
    
    response, err := client.SendCommand(longCommand)
    
    // 에러가 발생하거나 정상 처리해야 함
    if err != nil && !contains(response, "ERROR") {
        t.Logf("긴 명령어 처리: %v", err)
    }
}

// 엣지 케이스 3: 빈 명령어
func TestEmptyCommand(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    client := ts.CreateClient()
    defer client.Close()
    
    response, err := client.SendCommand("")
    
    if err != nil || !contains(response, "ERROR") {
        t.Errorf("빈 명령어 처리 실패: %s", response)
    }
}

// 엣지 케이스 4: 잘못된 명령어 형식
func TestMalformedCommand(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    client := ts.CreateClient()
    defer client.Close()
    
    commands := []string{
        "\x00\x01\x02",  // 바이너리 데이터
        "\n\n\n",        // 개행 문자만
        "   ",           // 공백만
    }
    
    for _, cmd := range commands {
        response, err := client.SendCommand(cmd)
        
        // 에러나 예상 가능한 응답이어야 함
        if err == nil && !contains(response, "ERROR") {
            t.Logf("예상치 못한 응답: %s", response)
        }
    }
}

// 엣지 케이스 5: 연결 후 기다리기 (타임아웃 테스트)
func TestConnectionTimeout(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    client := ts.CreateClient()
    defer client.Close()
    
    // 5초 대기 후 명령어 전송
    time.Sleep(5 * time.Second)
    
    response, err := client.SendCommand("LOGIN Test")
    if err != nil || !contains(response, "OK") {
        t.Error("타임아웃 후 명령어 실패")
    }
}

// 엣지 케이스 6: 동시에 같은 작업 수행
func TestConcurrentSameOperation(t *testing.T) {
    server, err := NewGameServerWithState("127.0.0.1:0")
    if err != nil {
        t.Fatalf("서버 생성 실패: %v", err)
    }
    defer server.listener.Close()
    
    // 동시에 같은 플레이어를 여러 번 추가하려고 시도
    var wg sync.WaitGroup
    for i := 0; i < 5; i++ {
        wg.Add(1)
        go func() {
            defer wg.Done()
            // 같은 ID로 플레이어 추가 시도
            server.AddPlayer("player1", "Alice", 1000)
        }()
    }
    
    wg.Wait()
    
    // 1명의 플레이어만 추가되어야 함
    if len(server.players) > 1 {
        t.Errorf("중복 플레이어 추가됨: %d명", len(server.players))
    }
}

// 엣지 케이스 7: 비정상 종료
func TestAbruptConnectionClose(t *testing.T) {
    ts := StartTestServer(t)
    defer ts.Close()
    
    conn, err := net.Dial("tcp", ts.addr)
    if err != nil {
        t.Fatalf("연결 실패: %v", err)
    }
    
    // 명령어를 보내지 않고 바로 종료
    conn.Close()
    
    // 서버가 여전히 작동하는지 확인
    client := ts.CreateClient()
    defer client.Close()
    
    response, err := client.SendCommand("LOGIN Test")
    if err != nil || !contains(response, "OK") {
        t.Error("비정상 종료 후 서버 작동 실패")
    }
}

// 엣지 케이스 8: 연결 유지하면서 오래된 상태 접근
func TestStateConsistency(t *testing.T) {
    server, err := NewGameServerWithState("127.0.0.1:0")
    if err != nil {
        t.Fatalf("서버 생성 실패: %v", err)
    }
    defer server.listener.Close()
    
    // 플레이어 생성
    server.AddPlayer("player1", "Alice", 1000)
    
    // 초기 칩 확인
    chips1, _ := server.GetPlayerChips("player1")
    
    // 베팅
    server.PlaceBet("player1", 100)
    
    // 베팅 후 칩 확인
    chips2, _ := server.GetPlayerChips("player1")
    
    // 초기 칩과 베팅 후 칩이 다른지 확인
    if chips1 == chips2 {
        t.Error("베팅이 반영되지 않음")
    }
    
    if chips2 != 900 {
        t.Errorf("베팅 후 칩 기댓값 900, 실제 %d", chips2)
    }
}
```

### 데이터 경쟁 감지

동시성 문제를 감지하기 위해 race detector와 함께 테스트를 실행하자.

```bash
# race detector와 함께 통합 테스트 실행
go test -race ./...

# 특정 테스트만 race detector로 실행
go test -race -run TestConcurrentConnections
```

---

## 요약

통합 테스트는 여러 컴포넌트가 함께 작동하는 복잡한 시나리오를 검증한다. 게임 서버의 경우 네트워크 통신, 게임 로직, 상태 관리가 모두 정상적으로 작동해야 한다.

테스트 클라이언트를 구현하면 실제 클라이언트 없이도 서버의 동작을 자동화된 방식으로 검증할 수 있다. 테스트 서버 헬퍼는 테스트 코드를 더 간결하고 유지보수하기 쉽게 만든다.

시나리오 테스트는 실제 사용자가 경험하는 게임 플레이 흐름을 재현하여 비즈니스 로직의 정확성을 검증한다. 부하 테스트와 동시 접속 테스트는 서버의 확장성과 안정성을 평가한다.

엣지 케이스 테스트는 정상적인 사용 패턴을 벗어난 상황에서도 서버가 우아하게 동작하도록 보장한다. race detector를 활용하면 동시성 문제를 조기에 발견하고 수정할 수 있다.

이러한 기법들을 종합적으로 활용하면 복잡한 게임 서버가 다양한 상황에서 안정적으로 작동할 수 있다.


# Chapter 27. 디버깅과 모니터링

게임 서버 개발에서 버그를 찾고 성능을 측정하는 것은 안정적인 서비스를 제공하기 위해 매우 중요하다. 이 장에서는 Go 언어에서 제공하는 강력한 디버깅과 모니터링 도구들을 실전 예제와 함께 살펴본다.

## 27.1 VSCode 디버거 활용

VSCode는 Go 개발을 위한 훌륭한 디버깅 환경을 제공한다. Delve 디버거를 통해 브레이크포인트 설정, 변수 검사, 스택 추적 등의 기능을 사용할 수 있다.

### VSCode 설정 및 필수 확장 설치

먼저 VSCode에서 Go 확장을 설치해야 한다. Extensions 탭에서 "Go" 확장을 검색하여 설치하면, 자동으로 Delve와 필요한 도구들이 설정된다.

VSCode의 설정 파일 `.vscode/launch.json`을 다음과 같이 구성한다.

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Connect to server",
            "type": "go",
            "request": "launch",
            "mode": "debug",
            "program": "${workspaceFolder}",
            "env": {},
            "args": [],
            "showLog": true
        },
        {
            "name": "Attach to running process",
            "type": "go",
            "request": "attach",
            "mode": "local",
            "processId": "${command:pickProcess}"
        }
    ]
}
```

이 설정에서 "Connect to server" 구성은 프로그램을 디버그 모드로 시작하고, "Attach to running process" 구성은 이미 실행 중인 프로세스에 디버거를 연결한다.

### 실전 예제: 게임 서버 디버깅

다음은 간단한 게임 서버 코드로, VSCode 디버거를 활용하여 문제를 찾는 방법을 보여준다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// Player는 게임 플레이어를 나타낸다.
type Player struct {
	ID       int
	Name     string
	Score    int
	LastPing time.Time
}

// GameRoom은 게임 방을 관리한다.
type GameRoom struct {
	RoomID   int
	Players  map[int]*Player
	mu       sync.RWMutex
	MaxCount int
}

// NewGameRoom은 새로운 게임 방을 생성한다.
func NewGameRoom(roomID, maxCount int) *GameRoom {
	return &GameRoom{
		RoomID:   roomID,
		Players:  make(map[int]*Player),
		MaxCount: maxCount,
	}
}

// AddPlayer는 방에 플레이어를 추가한다.
// 이 함수에 버그가 있다고 가정하자.
func (gr *GameRoom) AddPlayer(player *Player) error {
	gr.mu.Lock()
	defer gr.mu.Unlock()

	if len(gr.Players) >= gr.MaxCount {
		return fmt.Errorf("room is full")
	}

	gr.Players[player.ID] = player
	fmt.Printf("Player %s added to room %d\n", player.Name, gr.RoomID)
	return nil
}

// UpdatePlayerScore는 플레이어의 점수를 업데이트한다.
func (gr *GameRoom) UpdatePlayerScore(playerID, scoreIncrease int) {
	gr.mu.Lock()
	defer gr.mu.Unlock()

	if player, exists := gr.Players[playerID]; exists {
		player.Score += scoreIncrease
		player.LastPing = time.Now()
	}
}

// GetTotalScore는 방의 총 점수를 반환한다.
func (gr *GameRoom) GetTotalScore() int {
	gr.mu.RLock()
	defer gr.mu.RUnlock()

	total := 0
	for _, player := range gr.Players {
		total += player.Score
	}
	return total
}

// CheckInactivePlayers는 일정 시간 동안 활동이 없는 플레이어를 검사한다.
func (gr *GameRoom) CheckInactivePlayers(timeout time.Duration) []int {
	gr.mu.RLock()
	defer gr.mu.RUnlock()

	now := time.Now()
	var inactivePlayers []int

	for id, player := range gr.Players {
		if now.Sub(player.LastPing) > timeout {
			inactivePlayers = append(inactivePlayers, id)
		}
	}

	return inactivePlayers
}

func main() {
	// 게임 방 생성
	room := NewGameRoom(1, 4)

	// 플레이어 추가
	player1 := &Player{
		ID:       1,
		Name:     "Alice",
		Score:    0,
		LastPing: time.Now(),
	}

	player2 := &Player{
		ID:       2,
		Name:     "Bob",
		Score:    0,
		LastPing: time.Now(),
	}

	if err := room.AddPlayer(player1); err != nil {
		fmt.Printf("Error adding player1: %v\n", err)
	}

	if err := room.AddPlayer(player2); err != nil {
		fmt.Printf("Error adding player2: %v\n", err)
	}

	// 점수 업데이트
	room.UpdatePlayerScore(1, 100)
	room.UpdatePlayerScore(2, 50)

	// 총 점수 출력
	totalScore := room.GetTotalScore()
	fmt.Printf("Total score in room: %d\n", totalScore)

	// 비활성 플레이어 검사
	time.Sleep(2 * time.Second)
	inactivePlayers := room.CheckInactivePlayers(1 * time.Second)
	fmt.Printf("Inactive players: %v\n", inactivePlayers)
}
```

### VSCode에서 디버깅하기

위 코드를 디버깅하기 위해 다음 단계를 따른다.

1. **브레이크포인트 설정**: AddPlayer 함수의 첫 번째 줄 왼쪽을 클릭하여 빨간 점을 표시한다. 이렇게 하면 프로그램이 해당 줄에서 멈춘다.

2. **디버그 시작**: F5를 누르거나 Run 메뉴에서 "Start Debugging"을 선택한다.

3. **변수 검사**: 프로그램이 브레이크포인트에서 멈추면, VSCode 왼쪽 사이드바의 Variables 섹션에서 player, gr, 기타 로컬 변수의 값을 확인할 수 있다.

4. **단계 실행**: F10을 눌러 한 줄씩 실행하거나 (Step Over), F11을 눌러 함수 내부로 이동할 수 있다 (Step Into).

5. **조건부 브레이크포인트**: 브레이크포인트를 마우스 오른쪽으로 클릭하여 "Edit Breakpoint"를 선택하고, `player.ID == 1`과 같은 조건을 입력하여 특정 조건일 때만 멈추도록 설정할 수 있다.

6. **콜 스택 확인**: Call Stack 섹션에서 함수 호출 경로를 추적할 수 있다.

### 디버그 콘솔 활용

VSCode의 디버그 콘솔에서 직접 표현식을 평가할 수 있다. 프로그램이 멈춘 상태에서 디버그 콘솔 하단의 입력창에 다음과 같이 입력한다.

```
print(room.GetTotalScore())
print(len(room.Players))
print(player1.Score)
```

이렇게 하면 현재 프로그램 상태에서 변수 값이나 함수 호출 결과를 즉시 확인할 수 있다.

---

## 27.2 pprof를 이용한 프로파일링

Go 언어는 표준 라이브러리에 `runtime/pprof` 패키지를 포함하고 있어, 성능 분석을 위한 강력한 프로파일링 기능을 제공한다. CPU 프로파일, 메모리 프로파일, 고루틴 프로파일, 뮤텍스 프로파일 등을 수집할 수 있다.

### pprof 기본 설정

게임 서버에 pprof를 통합하는 방법을 보여준다.

```go
package main

import (
	"fmt"
	"log"
	"net"
	"net/http"
	_ "net/http/pprof"
	"runtime"
	"sync"
	"time"
)

// GameLogic은 게임 로직을 처리한다.
type GameLogic struct {
	calculations int
	mu           sync.Mutex
}

// ProcessGameTick은 게임 틱마다 호출된다.
// 의도적으로 CPU 비용이 많이 드는 작업을 수행한다.
func (gl *GameLogic) ProcessGameTick() {
	gl.mu.Lock()
	defer gl.mu.Unlock()

	// 복잡한 계산 수행
	for i := 0; i < 1000000; i++ {
		_ = fibonacci(20)
	}
	gl.calculations++
}

// fibonacci는 재귀적으로 피보나치 수를 계산한다.
func fibonacci(n int) int {
	if n <= 1 {
		return n
	}
	return fibonacci(n-1) + fibonacci(n-2)
}

// GameServer는 게임 서버를 나타낸다.
type GameServer struct {
	port      int
	gameLogic *GameLogic
	running   bool
}

// NewGameServer는 새로운 게임 서버를 생성한다.
func NewGameServer(port int) *GameServer {
	return &GameServer{
		port:      port,
		gameLogic: &GameLogic{},
		running:   false,
	}
}

// Start는 게임 서버를 시작한다.
func (gs *GameServer) Start() error {
	gs.running = true

	// pprof HTTP 핸들러 등록
	go func() {
		pprofAddr := fmt.Sprintf(":%d", gs.port+1)
		log.Printf("pprof server starting on %s\n", pprofAddr)
		if err := http.ListenAndServe(pprofAddr, nil); err != nil {
			log.Printf("pprof server error: %v\n", err)
		}
	}()

	// 게임 루프 시작
	go gs.gameLoop()

	return nil
}

// gameLoop는 게임 루프를 실행한다.
func (gs *GameServer) gameLoop() {
	ticker := time.NewTicker(16 * time.Millisecond)
	defer ticker.Stop()

	for gs.running {
		select {
		case <-ticker.C:
			gs.gameLogic.ProcessGameTick()
		}
	}
}

// Stop은 게임 서버를 중지한다.
func (gs *GameServer) Stop() {
	gs.running = false
}

// PrintStats는 서버 통계를 출력한다.
func (gs *GameServer) PrintStats() {
	var m runtime.MemStats
	runtime.ReadMemStats(&m)

	fmt.Printf("Memory Stats:\n")
	fmt.Printf("  Alloc: %v MB\n", m.Alloc/1024/1024)
	fmt.Printf("  TotalAlloc: %v MB\n", m.TotalAlloc/1024/1024)
	fmt.Printf("  Sys: %v MB\n", m.Sys/1024/1024)
	fmt.Printf("  NumGC: %v\n", m.NumGC)

	gs.gameLogic.mu.Lock()
	fmt.Printf("Game Logic Stats:\n")
	fmt.Printf("  Calculations: %d\n", gs.gameLogic.calculations)
	gs.gameLogic.mu.Unlock()
}

func main() {
	server := NewGameServer(8080)
	if err := server.Start(); err != nil {
		log.Fatalf("Failed to start server: %v", err)
	}

	fmt.Println("Game server started")
	fmt.Println("pprof available at http://localhost:8081/debug/pprof/")

	// 30초 동안 실행
	for i := 0; i < 30; i++ {
		time.Sleep(1 * time.Second)
		if i%10 == 0 {
			server.PrintStats()
		}
	}

	server.Stop()
	fmt.Println("Game server stopped")
}
```

이 코드에서 `_ "net/http/pprof"`를 import하면 자동으로 `/debug/pprof/` 엔드포인트가 등록된다.

### pprof를 통한 프로파일링 실행

프로그램을 실행한 상태에서 다음 명령어를 터미널에서 실행한다.

```bash
# CPU 프로파일 수집 (30초)
go tool pprof http://localhost:8081/debug/pprof/profile?seconds=30

# 힙 메모리 프로파일
go tool pprof http://localhost:8081/debug/pprof/heap

# 고루틴 프로파일
go tool pprof http://localhost:8081/debug/pprof/goroutine

# 뮤텍스 경합 프로파일
go tool pprof http://localhost:8081/debug/pprof/mutex
```

pprof 대화형 셸에 진입하면 다양한 명령어를 사용할 수 있다.

```
(pprof) top          # 가장 CPU를 많이 쓰는 함수 표시
(pprof) list fibonacci  # fibonacci 함수의 상세 분석
(pprof) web          # 그래프를 브라우저에서 시각화
(pprof) quit         # 종료
```

### 프로그래매틱 프로파일링

프로그램 내에서 직접 프로파일링을 제어하고 싶다면 `runtime/pprof` 패키지를 사용할 수 있다.

```go
package main

import (
	"fmt"
	"log"
	"os"
	"runtime"
	"runtime/pprof"
	"runtime/trace"
	"time"
)

// PerformanceAnalyzer는 성능 분석을 수행한다.
type PerformanceAnalyzer struct {
	cpuProfile  *os.File
	memProfile  *os.File
	traceFile   *os.File
}

// StartCPUProfile은 CPU 프로파일을 시작한다.
func (pa *PerformanceAnalyzer) StartCPUProfile(filename string) error {
	file, err := os.Create(filename)
	if err != nil {
		return fmt.Errorf("could not create CPU profile: %v", err)
	}

	pa.cpuProfile = file
	if err := pprof.StartCPUProfile(file); err != nil {
		file.Close()
		return fmt.Errorf("could not start CPU profile: %v", err)
	}

	return nil
}

// StopCPUProfile은 CPU 프로파일을 종료한다.
func (pa *PerformanceAnalyzer) StopCPUProfile() error {
	pprof.StopCPUProfile()
	if pa.cpuProfile != nil {
		pa.cpuProfile.Close()
	}
	return nil
}

// WriteMemProfile은 메모리 프로파일을 파일에 쓴다.
func (pa *PerformanceAnalyzer) WriteMemProfile(filename string) error {
	file, err := os.Create(filename)
	if err != nil {
		return fmt.Errorf("could not create memory profile: %v", err)
	}
	defer file.Close()

	runtime.GC()
	if err := pprof.WriteHeapProfile(file); err != nil {
		return fmt.Errorf("could not write memory profile: %v", err)
	}

	return nil
}

// StartTrace는 execution trace를 시작한다.
func (pa *PerformanceAnalyzer) StartTrace(filename string) error {
	file, err := os.Create(filename)
	if err != nil {
		return fmt.Errorf("could not create trace file: %v", err)
	}

	pa.traceFile = file
	if err := trace.Start(file); err != nil {
		file.Close()
		return fmt.Errorf("could not start trace: %v", err)
	}

	return nil
}

// StopTrace는 execution trace를 종료한다.
func (pa *PerformanceAnalyzer) StopTrace() error {
	trace.Stop()
	if pa.traceFile != nil {
		pa.traceFile.Close()
	}
	return nil
}

// HeavyComputation은 무거운 계산을 수행한다.
func HeavyComputation(iterations int) int {
	result := 0
	for i := 0; i < iterations; i++ {
		for j := 0; j < 1000; j++ {
			result += j % 7
		}
	}
	return result
}

func main() {
	analyzer := &PerformanceAnalyzer{}

	// CPU 프로파일 시작
	if err := analyzer.StartCPUProfile("cpu.prof"); err != nil {
		log.Fatal(err)
	}
	defer analyzer.StopCPUProfile()

	// Trace 시작
	if err := analyzer.StartTrace("trace.out"); err != nil {
		log.Fatal(err)
	}
	defer analyzer.StopTrace()

	// 무거운 작업 수행
	fmt.Println("Running heavy computation...")
	for i := 0; i < 10; i++ {
		result := HeavyComputation(1000)
		_ = result
		time.Sleep(100 * time.Millisecond)
	}

	// 메모리 프로파일 작성
	if err := analyzer.WriteMemProfile("mem.prof"); err != nil {
		log.Fatal(err)
	}

	fmt.Println("Profiling completed")
	fmt.Println("Run: go tool pprof cpu.prof")
	fmt.Println("Run: go tool trace trace.out")
}
```

프로파일링 파일이 생성되면 다음 명령어로 분석할 수 있다.

```bash
go tool pprof cpu.prof
go tool trace trace.out
```

---

## 27.3 race detector 사용하기

Go의 race detector는 데이터 경쟁(race condition)을 자동으로 감지한다. 멀티스레드 프로그래밍에서 발생할 수 있는 미묘한 버그를 찾는 데 매우 유용하다.

### Race Detector 기본 사용법

race detector는 `-race` 플래그를 사용하여 활성화한다.

```bash
# 실행 시 race detector 활성화
go run -race main.go

# 테스트 시 race detector 활성화
go test -race ./...

# 빌드 시 race detector 포함
go build -race -o gameserver main.go
```

### Race Condition 예제

다음 코드는 의도적으로 race condition을 포함하고 있다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// PlayerScoreBoard는 플레이어 점수를 관리한다.
type PlayerScoreBoard struct {
	scores map[int]int
	// 의도적으로 뮤텍스를 생략했다 - 이것이 race condition을 만든다
}

// UpdateScore는 플레이어 점수를 업데이트한다.
func (psb *PlayerScoreBoard) UpdateScore(playerID, points int) {
	// Race condition: 동시에 접근할 때 동기화가 없다
	psb.scores[playerID] += points
}

// GetScore는 플레이어 점수를 반환한다.
func (psb *PlayerScoreBoard) GetScore(playerID int) int {
	// Race condition: 동시에 접근할 때 동기화가 없다
	return psb.scores[playerID]
}

func main() {
	board := &PlayerScoreBoard{
		scores: make(map[int]int),
	}

	// 10개의 고루틴이 동시에 점수를 업데이트
	var wg sync.WaitGroup
	for i := 0; i < 10; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 1000; j++ {
				board.UpdateScore(1, 1)
			}
		}(i)
	}

	wg.Wait()
	fmt.Printf("Final score: %d (expected 10000)\n", board.GetScore(1))
}
```

이 코드를 race detector와 함께 실행하면 문제를 감지한다.

```bash
go run -race race_example.go
```

Race detector는 다음과 같은 메시지를 출력한다.

```
==================
WARNING: DATA RACE
Write at 0x... by goroutine 5:
    main.(*PlayerScoreBoard).UpdateScore()
        race_example.go:16 +0x54

Previous read at 0x... by goroutine 4:
    main.(*PlayerScoreBoard).UpdateScore()
        race_example.go:16 +0x4c

Goroutine 5 (running) created at:
    main.main()
        race_example.go:35 +0x9c
==================
```

### Race Condition 해결

이 문제를 해결하려면 뮤텍스를 추가해야 한다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// PlayerScoreBoard는 플레이어 점수를 관리한다.
type PlayerScoreBoard struct {
	scores map[int]int
	mu     sync.RWMutex
}

// NewPlayerScoreBoard는 새로운 스코어보드를 생성한다.
func NewPlayerScoreBoard() *PlayerScoreBoard {
	return &PlayerScoreBoard{
		scores: make(map[int]int),
	}
}

// UpdateScore는 플레이어 점수를 업데이트한다.
func (psb *PlayerScoreBoard) UpdateScore(playerID, points int) {
	psb.mu.Lock()
	defer psb.mu.Unlock()
	psb.scores[playerID] += points
}

// GetScore는 플레이어 점수를 반환한다.
func (psb *PlayerScoreBoard) GetScore(playerID int) int {
	psb.mu.RLock()
	defer psb.mu.RUnlock()
	return psb.scores[playerID]
}

// GetAllScores는 모든 플레이어의 점수를 반환한다.
func (psb *PlayerScoreBoard) GetAllScores() map[int]int {
	psb.mu.RLock()
	defer psb.mu.RUnlock()

	// 맵의 복사본을 반환하여 외부에서 수정할 수 없도록 한다
	result := make(map[int]int)
	for k, v := range psb.scores {
		result[k] = v
	}
	return result
}

// ResetScore는 플레이어 점수를 리셋한다.
func (psb *PlayerScoreBoard) ResetScore(playerID int) {
	psb.mu.Lock()
	defer psb.mu.Unlock()
	psb.scores[playerID] = 0
}

// RankPlayers는 플레이어를 점수 순서로 정렬한다.
func (psb *PlayerScoreBoard) RankPlayers() []struct {
	PlayerID int
	Score    int
} {
	psb.mu.RLock()
	defer psb.mu.RUnlock()

	var ranking []struct {
		PlayerID int
		Score    int
	}

	for id, score := range psb.scores {
		ranking = append(ranking, struct {
			PlayerID int
			Score    int
		}{id, score})
	}

	// 간단한 정렬 (실제로는 sort.Slice 사용)
	for i := 0; i < len(ranking)-1; i++ {
		for j := i + 1; j < len(ranking); j++ {
			if ranking[i].Score < ranking[j].Score {
				ranking[i], ranking[j] = ranking[j], ranking[i]
			}
		}
	}

	return ranking
}

func main() {
	board := NewPlayerScoreBoard()

	// 10개의 고루틴이 동시에 점수를 업데이트
	var wg sync.WaitGroup
	for i := 0; i < 10; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 1000; j++ {
				board.UpdateScore(1, 1)
				// 가끔 다른 플레이어의 점수도 업데이트
				if j%3 == 0 {
					board.UpdateScore(2, 1)
				}
			}
		}(i)
	}

	// 동시에 점수를 읽는 고루틴도 시작
	for i := 0; i < 5; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 500; j++ {
				_ = board.GetScore(1)
				time.Sleep(1 * time.Millisecond)
			}
		}(i)
	}

	wg.Wait()
	fmt.Printf("Final score for player 1: %d (expected 10000)\n", board.GetScore(1))
	fmt.Printf("Final score for player 2: %d\n", board.GetScore(2))

	rankings := board.RankPlayers()
	fmt.Println("Rankings:")
	for _, r := range rankings {
		fmt.Printf("  Player %d: %d points\n", r.PlayerID, r.Score)
	}
}
```

이제 race detector로 실행하면 더 이상 경고가 없다.

```bash
go run -race race_fixed.go
```

### Race Detector의 성능 영향

Race detector를 사용하면 프로그램 실행 속도가 느려지고 메모리 사용량이 증가한다. 따라서 다음과 같이 사용한다.

1. **개발 및 테스트 중**: 항상 `-race` 플래그와 함께 테스트를 실행한다.
2. **CI/CD 파이프라인**: 자동 테스트에서 race detector를 활성화한다.
3. **프로덕션 빌드**: 성능상의 이유로 race detector를 포함하지 않는다.

### 일반적인 Race Condition 패턴

```go
package main

import (
	"sync"
)

// BadExample은 몇 가지 흔한 race condition 패턴을 보여준다.
type BadExample struct {
	value int
	mu    sync.Mutex
}

// 패턴 1: Check-then-act race condition
func (be *BadExample) Pattern1BadCheckThenAct() {
	// 위험: 확인과 수정 사이에 다른 고루틴이 개입할 수 있다
	if be.value < 100 {
		// 이 사이에 다른 고루틴이 value를 변경할 수 있다
		be.value = be.value + 1
	}
}

// 패턴 1 수정: 확인과 수정을 원자적으로 수행
func (be *BadExample) Pattern1FixCheckThenAct() {
	be.mu.Lock()
	defer be.mu.Unlock()

	if be.value < 100 {
		be.value = be.value + 1
	}
}

// 패턴 2: 읽기 후 쓰기 경합
func (be *BadExample) Pattern2BadReadModifyWrite(increment int) {
	// 위험: 읽기와 쓰기 사이에 다른 고루틴의 수정이 손실될 수 있다
	temp := be.value
	temp += increment
	be.value = temp
}

// 패턴 2 수정: 읽기와 쓰기를 원자적으로 수행
func (be *BadExample) Pattern2FixReadModifyWrite(increment int) {
	be.mu.Lock()
	defer be.mu.Unlock()

	be.value += increment
}

// 패턴 3: 구조체 포인터의 복사
type Node struct {
	value int
	next  *Node
	mu    sync.Mutex
}

// BadExample은 노드 포인터를 재할당하면서 동기화하지 않는다.
func (n *Node) Pattern3BadPointerRacassign(newNext *Node) {
	// 위험: 다른 고루틴이 n.next를 동시에 읽을 수 있다
	n.next = newNext
}

// 패턴 3 수정: 뮤텍스로 보호
func (n *Node) Pattern3FixPointerReassign(newNext *Node) {
	n.mu.Lock()
	defer n.mu.Unlock()

	n.next = newNext
}
```

---

## 27.4 로그 레벨 관리

게임 서버에서 효과적인 로깅은 문제 해결과 모니터링의 핵심이다. 로그 레벨을 적절히 관리하면 필요한 정보를 얻으면서도 성능 저하를 최소화할 수 있다.

### 로깅 아키텍처 설계

다음은 게임 서버를 위한 구조화된 로깅 시스템이다.

```go
package main

import (
	"fmt"
	"io"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// LogLevel은 로그 레벨을 나타낸다.
type LogLevel int

const (
	DEBUG LogLevel = iota
	INFO
	WARN
	ERROR
	FATAL
)

// String은 로그 레벨을 문자열로 반환한다.
func (l LogLevel) String() string {
	switch l {
	case DEBUG:
		return "DEBUG"
	case INFO:
		return "INFO"
	case WARN:
		return "WARN"
	case ERROR:
		return "ERROR"
	case FATAL:
		return "FATAL"
	default:
		return "UNKNOWN"
	}
}

// LogEntry는 단일 로그 항목을 나타낸다.
type LogEntry struct {
	Level     LogLevel
	Timestamp time.Time
	Message   string
	Context   map[string]interface{}
	Caller    string
}

// Logger는 게임 서버의 로거이다.
type Logger struct {
	level      LogLevel
	outputs    []io.Writer
	mu         sync.Mutex
	buffered   bool
	buffer     []*LogEntry
	bufferSize int
	flushCh    chan struct{}
	stopCh     chan struct{}
	running    bool
}

// NewLogger는 새로운 로거를 생성한다.
func NewLogger(level LogLevel) *Logger {
	logger := &Logger{
		level:      level,
		outputs:    []io.Writer{os.Stdout},
		buffered:   true,
		bufferSize: 100,
		buffer:     make([]*LogEntry, 0, 100),
		flushCh:    make(chan struct{}, 1),
		stopCh:     make(chan struct{}),
		running:    true,
	}

	// 백그라운드에서 버퍼 플러시
	go logger.flushLoop()

	return logger
}

// AddOutput은 새로운 출력 대상을 추가한다.
func (l *Logger) AddOutput(w io.Writer) {
	l.mu.Lock()
	defer l.mu.Unlock()
	l.outputs = append(l.outputs, w)
}

// SetLevel은 로그 레벨을 설정한다.
func (l *Logger) SetLevel(level LogLevel) {
	l.mu.Lock()
	defer l.mu.Unlock()
	l.level = level
}

// Debug는 디버그 레벨 로그를 작성한다.
func (l *Logger) Debug(message string, context ...map[string]interface{}) {
	l.log(DEBUG, message, context)
}

// Info는 정보 레벨 로그를 작성한다.
func (l *Logger) Info(message string, context ...map[string]interface{}) {
	l.log(INFO, message, context)
}

// Warn은 경고 레벨 로그를 작성한다.
func (l *Logger) Warn(message string, context ...map[string]interface{}) {
	l.log(WARN, message, context)
}

// Error는 에러 레벨 로그를 작성한다.
func (l *Logger) Error(message string, context ...map[string]interface{}) {
	l.log(ERROR, message, context)
}

// Fatal은 치명적 레벨 로그를 작성하고 프로그램을 종료한다.
func (l *Logger) Fatal(message string, context ...map[string]interface{}) {
	l.log(FATAL, message, context)
	l.Flush()
	os.Exit(1)
}

// log는 내부 로깅 함수이다.
func (l *Logger) log(level LogLevel, message string, contextSlice []map[string]interface{}) {
	if level < l.level {
		return
	}

	var ctx map[string]interface{}
	if len(contextSlice) > 0 {
		ctx = contextSlice[0]
	} else {
		ctx = make(map[string]interface{})
	}

	entry := &LogEntry{
		Level:     level,
		Timestamp: time.Now(),
		Message:   message,
		Context:   ctx,
	}

	l.mu.Lock()
	l.buffer = append(l.buffer, entry)

	// 버퍼가 가득 차면 플러시
	if len(l.buffer) >= l.bufferSize {
		l.flushUnsafe()
	}
	l.mu.Unlock()

	// 비블로킹으로 플러시 신호 전송
	select {
	case l.flushCh <- struct{}{}:
	default:
	}
}

// flushLoop는 백그라운드에서 주기적으로 버퍼를 플러시한다.
func (l *Logger) flushLoop() {
	ticker := time.NewTicker(5 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-ticker.C:
			l.Flush()
		case <-l.flushCh:
			l.Flush()
		case <-l.stopCh:
			return
		}
	}
}

// Flush는 버퍼의 모든 항목을 출력한다.
func (l *Logger) Flush() {
	l.mu.Lock()
	l.flushUnsafe()
	l.mu.Unlock()
}

// flushUnsafe는 동기화 없이 버퍼를 플러시한다.
func (l *Logger) flushUnsafe() {
	if len(l.buffer) == 0 {
		return
	}

	for _, entry := range l.buffer {
		l.writeEntry(entry)
	}

	l.buffer = l.buffer[:0]
}

// writeEntry는 단일 항목을 모든 출력에 작성한다.
func (l *Logger) writeEntry(entry *LogEntry) {
	output := l.formatEntry(entry)

	for _, w := range l.outputs {
		w.Write([]byte(output))
	}
}

// formatEntry는 항목을 문자열로 포맷한다.
func (l *Logger) formatEntry(entry *LogEntry) string {
	output := fmt.Sprintf("[%s] %s - %s",
		entry.Timestamp.Format("2006-01-02 15:04:05.000"),
		entry.Level.String(),
		entry.Message,
	)

	if len(entry.Context) > 0 {
		output += " {"
		first := true
		for k, v := range entry.Context {
			if !first {
				output += ", "
			}
			output += fmt.Sprintf("%s=%v", k, v)
			first = false
		}
		output += "}"
	}

	output += "\n"
	return output
}

// Close는 로거를 종료한다.
func (l *Logger) Close() {
	l.Flush()
	l.running = false
	close(l.stopCh)
}

// ContextLogger는 컨텍스트를 유지하는 로거이다.
type ContextLogger struct {
	logger  *Logger
	context map[string]interface{}
	mu      sync.RWMutex
}

// NewContextLogger는 새로운 컨텍스트 로거를 생성한다.
func NewContextLogger(logger *Logger) *ContextLogger {
	return &ContextLogger{
		logger:  logger,
		context: make(map[string]interface{}),
	}
}

// WithContext는 컨텍스트를 추가한다.
func (cl *ContextLogger) WithContext(key string, value interface{}) *ContextLogger {
	cl.mu.Lock()
	defer cl.mu.Unlock()
	cl.context[key] = value
	return cl
}

// Debug는 컨텍스트와 함께 디버그 로그를 작성한다.
func (cl *ContextLogger) Debug(message string) {
	cl.mu.RLock()
	defer cl.mu.RUnlock()
	cl.logger.Debug(message, cl.context)
}

// Info는 컨텍스트와 함께 정보 로그를 작성한다.
func (cl *ContextLogger) Info(message string) {
	cl.mu.RLock()
	defer cl.mu.RUnlock()
	cl.logger.Info(message, cl.context)
}

// Warn은 컨텍스트와 함께 경고 로그를 작성한다.
func (cl *ContextLogger) Warn(message string) {
	cl.mu.RLock()
	defer cl.mu.RUnlock()
	cl.logger.Warn(message, cl.context)
}

// Error는 컨텍스트와 함께 에러 로그를 작성한다.
func (cl *ContextLogger) Error(message string) {
	cl.mu.RLock()
	defer cl.mu.RUnlock()
	cl.logger.Error(message, cl.context)
}

func main() {
	// 로거 생성
	logger := NewLogger(INFO)

	// 파일에도 로깅하도록 설정
	logFile, err := os.OpenFile("game.log", os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err == nil {
		logger.AddOutput(logFile)
		defer logFile.Close()
	}

	// 기본 로깅
	logger.Info("Game server starting")
	logger.Debug("This debug message won't appear because log level is INFO")

	// 컨텍스트와 함께 로깅
	ctx := logger
	logger.Info("Player connected", map[string]interface{}{
		"player_id": 123,
		"ip":        "192.168.1.100",
		"port":      5555,
	})

	// 컨텍스트 로거 사용
	contextLogger := NewContextLogger(logger)
	contextLogger.WithContext("room_id", 42).
		WithContext("player_count", 4)
	contextLogger.Info("Game started")

	// 로그 레벨 변경
	logger.SetLevel(DEBUG)
	logger.Debug("Debug logging now enabled")

	// 일부 작업 시뮬레이션
	for i := 0; i < 5; i++ {
		logger.Info("Processing game tick", map[string]interface{}{
			"tick": i,
			"fps":  60,
		})
		time.Sleep(100 * time.Millisecond)
	}

	// 마지막 로그가 모두 기록되도록 플러시
	logger.Close()
	fmt.Println("Logging completed")
}
```

### 로그 로테이션 구현

파일 기반 로깅에서는 로그 파일의 크기를 관리해야 한다.

```go
package main

import (
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// RotatingFileWriter는 자동으로 로그 파일을 로테이션한다.
type RotatingFileWriter struct {
	logDir       string
	filename     string
	maxFileSize  int64
	maxFiles     int
	currentFile  *os.File
	currentSize  int64
	mu           sync.Mutex
}

// NewRotatingFileWriter는 새로운 로테이팅 파일 라이터를 생성한다.
func NewRotatingFileWriter(logDir, filename string, maxFileSize int64, maxFiles int) (*RotatingFileWriter, error) {
	if err := os.MkdirAll(logDir, 0755); err != nil {
		return nil, err
	}

	rfw := &RotatingFileWriter{
		logDir:      logDir,
		filename:    filename,
		maxFileSize: maxFileSize,
		maxFiles:    maxFiles,
	}

	if err := rfw.openNewFile(); err != nil {
		return nil, err
	}

	return rfw, nil
}

// Write는 데이터를 파일에 쓴다.
func (rfw *RotatingFileWriter) Write(p []byte) (int, error) {
	rfw.mu.Lock()
	defer rfw.mu.Unlock()

	// 파일 크기를 초과하면 로테이션
	if rfw.currentSize+int64(len(p)) > rfw.maxFileSize {
		if err := rfw.rotateFile(); err != nil {
			return 0, err
		}
	}

	n, err := rfw.currentFile.Write(p)
	rfw.currentSize += int64(n)
	return n, err
}

// openNewFile은 새로운 로그 파일을 연다.
func (rfw *RotatingFileWriter) openNewFile() error {
	now := time.Now()
	filename := filepath.Join(rfw.logDir,
		fmt.Sprintf("%s-%04d%02d%02d-%02d%02d%02d.log",
			rfw.filename,
			now.Year(), now.Month(), now.Day(),
			now.Hour(), now.Minute(), now.Second()))

	file, err := os.OpenFile(filename, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err != nil {
		return err
	}

	if rfw.currentFile != nil {
		rfw.currentFile.Close()
	}

	rfw.currentFile = file
	rfw.currentSize = 0

	return nil
}

// rotateFile는 현재 파일을 닫고 새로운 파일을 연다.
func (rfw *RotatingFileWriter) rotateFile() error {
	rfw.currentFile.Close()

	// 이전 파일 삭제 (maxFiles 초과 시)
	files, err := filepath.Glob(filepath.Join(rfw.logDir, rfw.filename+"*.log"))
	if err == nil && len(files) > rfw.maxFiles {
		// 파일을 수정 시간으로 정렬하고 오래된 파일 삭제
		// (간단히 구현하기 위해 생략)
	}

	return rfw.openNewFile()
}

// Close는 파일을 닫는다.
func (rfw *RotatingFileWriter) Close() error {
	rfw.mu.Lock()
	defer rfw.mu.Unlock()

	if rfw.currentFile != nil {
		return rfw.currentFile.Close()
	}
	return nil
}

func main() {
	// 로테이팅 파일 라이터 생성
	rotatingWriter, err := NewRotatingFileWriter("./logs", "gameserver", 1024*1024, 10)
	if err != nil {
		fmt.Println("Error creating rotating writer:", err)
		return
	}
	defer rotatingWriter.Close()

	// 로그 작성 시뮬레이션
	for i := 0; i < 100; i++ {
		message := fmt.Sprintf("[%d] This is a log message\n", i)
		rotatingWriter.Write([]byte(message))
	}

	fmt.Println("Logging completed")
}
```

---

## 27.5 메트릭 수집과 모니터링

게임 서버의 건강 상태를 실시간으로 파악하려면 메트릭을 수집하고 모니터링해야 한다.

### 메트릭 수집 시스템 구현

```go
package main

import (
	"fmt"
	"runtime"
	"sync"
	"sync/atomic"
	"time"
)

// MetricType은 메트릭의 종류를 나타낸다.
type MetricType int

const (
	Counter MetricType = iota
	Gauge
	Histogram
)

// Metric은 단일 메트릭을 나타낸다.
type Metric struct {
	Name  string
	Type  MetricType
	Value interface{}
}

// Counter는 증가만 하는 메트릭이다.
type CounterMetric struct {
	value int64
}

// Increment는 카운터를 증가시킨다.
func (c *CounterMetric) Increment(delta int64) {
	atomic.AddInt64(&c.value, delta)
}

// Value는 카운터 값을 반환한다.
func (c *CounterMetric) Value() int64 {
	return atomic.LoadInt64(&c.value)
}

// Gauge는 임의로 증감하는 메트릭이다.
type GaugeMetric struct {
	value int64
	mu    sync.RWMutex
}

// Set은 게이지 값을 설정한다.
func (g *GaugeMetric) Set(value int64) {
	g.mu.Lock()
	defer g.mu.Unlock()
	g.value = value
}

// Value는 게이지 값을 반환한다.
func (g *GaugeMetric) Value() int64 {
	g.mu.RLock()
	defer g.mu.RUnlock()
	return g.value
}

// MetricsCollector는 메트릭을 수집한다.
type MetricsCollector struct {
	counters   map[string]*CounterMetric
	gauges     map[string]*GaugeMetric
	mu         sync.RWMutex
	startTime  time.Time
	lastUpdate time.Time
}

// NewMetricsCollector는 새로운 메트릭 수집기를 생성한다.
func NewMetricsCollector() *MetricsCollector {
	return &MetricsCollector{
		counters:  make(map[string]*CounterMetric),
		gauges:    make(map[string]*GaugeMetric),
		startTime: time.Now(),
	}
}

// RegisterCounter는 카운터를 등록한다.
func (mc *MetricsCollector) RegisterCounter(name string) *CounterMetric {
	mc.mu.Lock()
	defer mc.mu.Unlock()

	if counter, exists := mc.counters[name]; exists {
		return counter
	}

	counter := &CounterMetric{}
	mc.counters[name] = counter
	return counter
}

// RegisterGauge는 게이지를 등록한다.
func (mc *MetricsCollector) RegisterGauge(name string) *GaugeMetric {
	mc.mu.Lock()
	defer mc.mu.Unlock()

	if gauge, exists := mc.gauges[name]; exists {
		return gauge
	}

	gauge := &GaugeMetric{}
	mc.gauges[name] = gauge
	return gauge
}

// GetMetrics는 모든 메트릭을 반환한다.
func (mc *MetricsCollector) GetMetrics() map[string]interface{} {
	mc.mu.RLock()
	defer mc.mu.RUnlock()

	metrics := make(map[string]interface{})

	for name, counter := range mc.counters {
		metrics[name] = counter.Value()
	}

	for name, gauge := range mc.gauges {
		metrics[name] = gauge.Value()
	}

	return metrics
}

// GameServerMetrics는 게임 서버의 메트릭을 관리한다.
type GameServerMetrics struct {
	collector *MetricsCollector

	// 네트워크 메트릭
	PacketsReceived *CounterMetric
	PacketsSent     *CounterMetric
	BytesReceived   *CounterMetric
	BytesSent       *CounterMetric

	// 플레이어 메트릭
	ActivePlayers *GaugeMetric
	TotalLogins   *CounterMetric
	TotalLogouts  *CounterMetric

	// 게임 메트릭
	GamesStarted   *CounterMetric
	GamesCompleted *CounterMetric
	GamesFailed    *CounterMetric

	// 시스템 메트릭
	MemoryUsage    *GaugeMetric
	GoroutineCount *GaugeMetric
	GCCount        *CounterMetric
}

// NewGameServerMetrics는 새로운 게임 서버 메트릭을 생성한다.
func NewGameServerMetrics() *GameServerMetrics {
	collector := NewMetricsCollector()

	return &GameServerMetrics{
		collector:      collector,
		PacketsReceived: collector.RegisterCounter("packets_received"),
		PacketsSent:     collector.RegisterCounter("packets_sent"),
		BytesReceived:   collector.RegisterCounter("bytes_received"),
		BytesSent:       collector.RegisterCounter("bytes_sent"),
		ActivePlayers:   collector.RegisterGauge("active_players"),
		TotalLogins:     collector.RegisterCounter("total_logins"),
		TotalLogouts:    collector.RegisterCounter("total_logouts"),
		GamesStarted:    collector.RegisterCounter("games_started"),
		GamesCompleted:  collector.RegisterCounter("games_completed"),
		GamesFailed:     collector.RegisterCounter("games_failed"),
		MemoryUsage:     collector.RegisterGauge("memory_usage_mb"),
		GoroutineCount:  collector.RegisterGauge("goroutine_count"),
		GCCount:         collector.RegisterCounter("gc_count"),
	}
}

// UpdateSystemMetrics는 시스템 메트릭을 업데이트한다.
func (gsm *GameServerMetrics) UpdateSystemMetrics() {
	var m runtime.MemStats
	runtime.ReadMemStats(&m)

	gsm.MemoryUsage.Set(int64(m.Alloc / 1024 / 1024))
	gsm.GoroutineCount.Set(int64(runtime.NumGoroutine()))
	gsm.GCCount.Increment(int64(m.NumGC))
}

// PrintMetrics는 메트릭을 출력한다.
func (gsm *GameServerMetrics) PrintMetrics() {
	gsm.UpdateSystemMetrics()
	metrics := gsm.collector.GetMetrics()

	fmt.Println("\n=== Game Server Metrics ===")
	fmt.Println("Network:")
	fmt.Printf("  Packets Received: %v\n", metrics["packets_received"])
	fmt.Printf("  Packets Sent: %v\n", metrics["packets_sent"])
	fmt.Printf("  Bytes Received: %v\n", metrics["bytes_received"])
	fmt.Printf("  Bytes Sent: %v\n", metrics["bytes_sent"])

	fmt.Println("Players:")
	fmt.Printf("  Active Players: %v\n", metrics["active_players"])
	fmt.Printf("  Total Logins: %v\n", metrics["total_logins"])
	fmt.Printf("  Total Logouts: %v\n", metrics["total_logouts"])

	fmt.Println("Games:")
	fmt.Printf("  Games Started: %v\n", metrics["games_started"])
	fmt.Printf("  Games Completed: %v\n", metrics["games_completed"])
	fmt.Printf("  Games Failed: %v\n", metrics["games_failed"])

	fmt.Println("System:")
	fmt.Printf("  Memory Usage: %v MB\n", metrics["memory_usage_mb"])
	fmt.Printf("  Goroutine Count: %v\n", metrics["goroutine_count"])
	fmt.Printf("  GC Count: %v\n", metrics["gc_count"])
	fmt.Println("============================")
}

// HealthChecker는 서버의 건강 상태를 검사한다.
type HealthChecker struct {
	metrics *GameServerMetrics
	mu      sync.RWMutex
	status  string
}

// NewHealthChecker는 새로운 건강 검사기를 생성한다.
func NewHealthChecker(metrics *GameServerMetrics) *HealthChecker {
	return &HealthChecker{
		metrics: metrics,
		status:  "HEALTHY",
	}
}

// Check는 서버 건강 상태를 검사한다.
func (hc *HealthChecker) Check() {
	hc.mu.Lock()
	defer hc.mu.Unlock()

	metrics := hc.metrics.collector.GetMetrics()

	// 메모리 사용량 검사
	memUsage := metrics["memory_usage_mb"].(int64)
	if memUsage > 1024 {
		hc.status = "WARNING"
		return
	}

	// 고루틴 수 검사
	goroutines := metrics["goroutine_count"].(int64)
	if goroutines > 10000 {
		hc.status = "WARNING"
		return
	}

	hc.status = "HEALTHY"
}

// GetStatus는 현재 상태를 반환한다.
func (hc *HealthChecker) GetStatus() string {
	hc.mu.RLock()
	defer hc.mu.RUnlock()
	return hc.status
}

func main() {
	metrics := NewGameServerMetrics()

	// 게임 이벤트 시뮬레이션
	go func() {
		for i := 0; i < 100; i++ {
			metrics.PacketsReceived.Increment(1)
			metrics.BytesReceived.Increment(512)
			metrics.ActivePlayers.Set(int64(i % 50))
			metrics.GamesStarted.Increment(1)
			time.Sleep(100 * time.Millisecond)
		}
	}()

	// 건강 검사기
	healthChecker := NewHealthChecker(metrics)

	// 메트릭 출력 및 건강 검사
	for i := 0; i < 5; i++ {
		time.Sleep(500 * time.Millisecond)
		metrics.PrintMetrics()
		healthChecker.Check()
		fmt.Printf("Server Status: %s\n", healthChecker.GetStatus())
	}
}
```

이 장에서 다루는 디버깅과 모니터링 기술들은 게임 서버의 안정성과 성능을 보장하기 위해 필수적이다. VSCode 디버거로 로직 오류를 찾고, pprof로 성능 병목을 식별하고, race detector로 동시성 문제를 해결하고, 로깅과 메트릭으로 운영 중인 서버의 상태를 파악할 수 있다.

실전 개발에서는 이 도구들을 적절히 조합하여 개발 생산성과 서비스 안정성을 함께 확보하는 것이 중요하다.  