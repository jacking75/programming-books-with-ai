# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 9. TCP 소켓 프로그래밍 기초

게임 서버의 핵심은 네트워크 통신이다. 클라이언트와 서버 간의 안정적인 데이터 전송을 위해 TCP(Transmission Control Protocol) 프로토콜을 사용한다. 이 장에서는 Go의 `net` 패키지를 이용하여 TCP 소켓 프로그래밍의 기초를 학습한다.

## 9.1 net 패키지 소개

### Go의 네트워크 프로그래밍 철학

Go는 네트워크 프로그래밍을 매우 간단하게 만들었다. C나 Java와 달리 소켓을 직접 다루지 않고, 고수준의 인터페이스를 제공한다. Go의 `net` 패키지는 다음의 철학을 따른다.

단순성: 복잡한 설정 없이 기본적인 네트워크 통신이 가능하다. 안전성: 에러 처리와 리소스 관리가 자동으로 이루어진다. 동시성: 고루틴을 활용하여 여러 클라이언트를 효율적으로 처리할 수 있다.

### net 패키지의 주요 인터페이스

Go의 `net` 패키지에서는 네트워크 연결을 추상화된 인터페이스로 제공한다.

`Listener`: 들어오는 연결을 대기하는 객체이다. `Accept()` 메서드로 새로운 연결을 받아들인다.

`Conn`: 양방향 네트워크 연결을 나타낸다. `Read()` 메서드로 데이터를 읽고, `Write()` 메서드로 데이터를 쓴다.

`Addr`: 네트워크 주소를 나타낸다. IP 주소와 포트 정보를 포함한다.

```
┌─────────────────────────────────────────────────────────┐
│                   net 패키지                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Listener                                               │
│  ├─ Accept() -> Conn                                    │
│  └─ Close()                                             │
│                                                         │
│  Conn (인터페이스)                                       │
│  ├─ Read(b []byte) (n int, err error)                  │
│  ├─ Write(b []byte) (n int, err error)                 │
│  ├─ Close()                                             │
│  ├─ LocalAddr() Addr                                    │
│  └─ RemoteAddr() Addr                                   │
│                                                         │
│  Addr (인터페이스)                                       │
│  ├─ Network() string                                    │
│  └─ String() string                                     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### net 패키지의 주요 함수들

Go는 네트워크 작업을 위한 여러 함수를 제공한다.

`net.Listen(network, address)`: TCP 리스너를 생성한다. 서버에서 클라이언트의 연결을 대기할 때 사용한다.

`net.Dial(network, address)`: 원격 주소에 연결한다. 클라이언트에서 서버에 연결할 때 사용한다.

`net.ListenTCP(laddr)`: TCP 리스너를 생성하며, 더 세밀한 제어가 가능하다.

`net.DialTCP(laddr, raddr)`: TCP 연결을 생성한다.

```go
package main

import (
	"fmt"
	"net"
)

func main() {
	// TCP 리스너 생성
	listener, err := net.Listen("tcp", ":8080")
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Printf("서버가 %s에서 대기 중입니다\n", listener.Addr())

	// 리스너 정보 출력
	fmt.Printf("네트워크 종류: %s\n", listener.Addr().Network())
	fmt.Printf("주소: %s\n", listener.Addr().String())
}
```

이 코드를 실행하면 서버가 포트 8080에서 클라이언트의 연결을 대기할 준비를 한다. `Listen()` 함수의 인자를 설명하면, 첫 번째 인자 `"tcp"`는 TCP 프로토콜을 사용함을 의미하고, 두 번째 인자 `":8080"`은 모든 인터페이스(0.0.0.0)의 8080 포트를 의미한다.

## 9.2 TCP 서버 만들기

### 기본 TCP 서버 구현

TCP 서버의 기본 구조는 다음과 같다.

리스너를 생성한다. 무한 루프에서 `Accept()` 메서드로 새로운 연결을 대기한다. 각 연결마다 고루틴을 시작하여 클라이언트를 처리한다. 클라이언트로부터 데이터를 읽고 처리한다. 응답을 클라이언트에게 전송한다.

```go
package main

import (
	"fmt"
	"net"
)

func handleConnection(conn net.Conn) {
	defer conn.Close()

	fmt.Printf("클라이언트 연결됨: %s\n", conn.RemoteAddr())

	// 클라이언트로부터 데이터를 읽는다 (최대 1024 바이트)
	buffer := make([]byte, 1024)
	n, err := conn.Read(buffer)
	if err != nil {
		fmt.Printf("읽기 에러: %v\n", err)
		return
	}

	// 받은 데이터를 출력한다
	message := string(buffer[:n])
	fmt.Printf("수신한 메시지: %s\n", message)

	// 클라이언트에게 응답을 보낸다
	response := fmt.Sprintf("서버가 수신했습니다: %s", message)
	_, err = conn.Write([]byte(response))
	if err != nil {
		fmt.Printf("쓰기 에러: %v\n", err)
		return
	}

	fmt.Printf("응답 전송 완료\n")
}

func main() {
	listener, err := net.Listen("tcp", ":8080")
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Println("서버 시작: localhost:8080")

	for {
		// 새로운 클라이언트 연결을 대기한다
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		// 각 클라이언트를 별도의 고루틴에서 처리한다
		go handleConnection(conn)
	}
}
```

이 예제의 흐름을 설명한다.

`Listen("tcp", ":8080")`은 포트 8080에 리스너를 생성한다. `listener.Accept()`는 클라이언트의 연결을 기다린다. 새로운 연결이 들어오면 `handleConnection()` 함수를 별도의 고루틴에서 실행하여 동시에 여러 클라이언트를 처리한다. `conn.Read()`는 클라이언트로부터 최대 1024 바이트의 데이터를 읽는다. `conn.Write()`는 클라이언트에게 응답을 전송한다.

### 여러 클라이언트 동시 처리

게임 서버는 동시에 많은 클라이언트를 처리해야 한다. Go의 고루틴은 이를 매우 효율적으로 만든다.

```go
package main

import (
	"fmt"
	"net"
	"sync"
	"sync/atomic"
	"time"
)

type Server struct {
	mu              sync.Mutex
	connectedCount  int32
	totalConnections int32
}

func (s *Server) handleConnection(conn net.Conn) {
	defer func() {
		conn.Close()
		atomic.AddInt32(&s.connectedCount, -1)
		fmt.Printf("클라이언트 종료: %s (남은 연결: %d)\n", 
			conn.RemoteAddr(), atomic.LoadInt32(&s.connectedCount))
	}()

	// 연결 카운트 증가
	atomic.AddInt32(&s.connectedCount, 1)
	atomic.AddInt32(&s.totalConnections, 1)

	fmt.Printf("클라이언트 연결됨: %s (총 연결: %d)\n", 
		conn.RemoteAddr(), atomic.LoadInt32(&s.connectedCount))

	// 클라이언트와 대화한다
	buffer := make([]byte, 1024)
	for {
		n, err := conn.Read(buffer)
		if err != nil {
			// 클라이언트가 연결을 종료한 경우
			if err.Error() != "EOF" {
				fmt.Printf("읽기 에러: %v\n", err)
			}
			return
		}

		message := string(buffer[:n])
		fmt.Printf("[%s] %s\n", conn.RemoteAddr(), message)

		// 에코 응답 (받은 메시지를 그대로 돌려보낸다)
		_, err = conn.Write(buffer[:n])
		if err != nil {
			fmt.Printf("쓰기 에러: %v\n", err)
			return
		}
	}
}

func (s *Server) Start(port string) {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Printf("서버 시작: localhost:%s\n", port)

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		go s.handleConnection(conn)
	}
}

func main() {
	server := &Server{}

	// 서버를 별도의 고루틴에서 실행한다
	go server.Start("8080")

	// 메인 고루틴은 상태를 주기적으로 출력한다
	ticker := time.NewTicker(3 * time.Second)
	defer ticker.Stop()

	for range ticker.C {
		connected := atomic.LoadInt32(&server.connectedCount)
		total := atomic.LoadInt32(&server.totalConnections)
		fmt.Printf("상태: 현재 연결=%d, 누적 연결=%d\n", connected, total)
	}
}
```

이 예제에서 중요한 패턴들을 설명한다.

`defer`를 사용하여 연결 종료 시 연결 카운트를 감소시킨다. 이는 비정상 종료 경우도 처리한다. `atomic`을 사용하여 동시에 여러 고루틴에서 카운트를 수정할 때의 경쟁 조건을 방지한다. 각 클라이언트마다 별도의 고루틴에서 처리하므로, 한 클라이언트의 느린 처리가 다른 클라이언트에게 영향을 주지 않는다.

## 9.3 TCP 클라이언트 만들기

### 기본 TCP 클라이언트 구현

TCP 클라이언트는 서버에 연결하고 데이터를 주고받는다. 기본 구현은 다음과 같다.

```go
package main

import (
	"bufio"
	"fmt"
	"net"
	"os"
)

func main() {
	// 서버에 연결한다
	conn, err := net.Dial("tcp", "localhost:8080")
	if err != nil {
		fmt.Printf("연결 실패: %v\n", err)
		return
	}
	defer conn.Close()

	fmt.Println("서버에 연결되었습니다")
	fmt.Println("메시지를 입력하고 엔터를 누르세요 (quit 입력 시 종료):")

	// 표준 입력에서 메시지를 읽는다
	scanner := bufio.NewScanner(os.Stdin)

	for scanner.Scan() {
		message := scanner.Text()

		// quit을 입력하면 종료한다
		if message == "quit" {
			fmt.Println("연결을 종료합니다")
			break
		}

		// 서버에 메시지를 전송한다
		_, err := conn.Write([]byte(message))
		if err != nil {
			fmt.Printf("전송 실패: %v\n", err)
			break
		}

		// 서버로부터 응답을 받는다
		buffer := make([]byte, 1024)
		n, err := conn.Read(buffer)
		if err != nil {
			fmt.Printf("수신 실패: %v\n", err)
			break
		}

		response := string(buffer[:n])
		fmt.Printf("서버 응답: %s\n", response)
	}
}
```

이 클라이언트 코드의 특징을 설명한다.

`net.Dial()`을 사용하여 서버에 연결한다. `bufio.Scanner`를 사용하여 사용자의 입력을 라인 단위로 읽는다. 사용자가 "quit"을 입력하면 연결을 종료한다. 서버에 메시지를 전송하고 응답을 받는 과정을 반복한다.

### 비동기 클라이언트 구현

게임 클라이언트는 동시에 여러 일을 해야 한다. 사용자 입력을 받으면서 동시에 서버로부터 메시지를 받아야 한다. 이를 구현하려면 고루틴을 사용해야 한다.

```go
package main

import (
	"bufio"
	"fmt"
	"net"
	"os"
	"sync"
)

type GameClient struct {
	conn      net.Conn
	inputChan chan string
	done      chan struct{}
	wg        sync.WaitGroup
}

func NewGameClient(address string) (*GameClient, error) {
	conn, err := net.Dial("tcp", address)
	if err != nil {
		return nil, err
	}

	return &GameClient{
		conn:      conn,
		inputChan: make(chan string, 10),
		done:      make(chan struct{}),
	}, nil
}

// 사용자 입력을 읽는 고루틴
func (c *GameClient) readUserInput() {
	defer c.wg.Done()

	scanner := bufio.NewScanner(os.Stdin)
	fmt.Println("메시지를 입력하세요 (quit 입력 시 종료):")

	for scanner.Scan() {
		message := scanner.Text()
		select {
		case <-c.done:
			return
		case c.inputChan <- message:
			if message == "quit" {
				close(c.done)
				return
			}
		}
	}
}

// 사용자 입력을 서버로 전송하는 고루틴
func (c *GameClient) sendToServer() {
	defer c.wg.Done()

	for {
		select {
		case <-c.done:
			return
		case message := <-c.inputChan:
			if message == "quit" {
				return
			}

			_, err := c.conn.Write([]byte(message))
			if err != nil {
				fmt.Printf("전송 실패: %v\n", err)
				close(c.done)
				return
			}
			fmt.Printf("전송: %s\n", message)
		}
	}
}

// 서버로부터 메시지를 받는 고루틴
func (c *GameClient) receiveFromServer() {
	defer c.wg.Done()

	buffer := make([]byte, 1024)
	for {
		select {
		case <-c.done:
			return
		default:
		}

		n, err := c.conn.Read(buffer)
		if err != nil {
			fmt.Printf("수신 실패: %v\n", err)
			close(c.done)
			return
		}

		message := string(buffer[:n])
		fmt.Printf("수신: %s\n", message)
	}
}

func (c *GameClient) Start() {
	c.wg.Add(3)
	go c.readUserInput()
	go c.sendToServer()
	go c.receiveFromServer()
}

func (c *GameClient) Wait() {
	c.wg.Wait()
	c.conn.Close()
	fmt.Println("연결 종료")
}

func main() {
	client, err := NewGameClient("localhost:8080")
	if err != nil {
		fmt.Printf("클라이언트 생성 실패: %v\n", err)
		return
	}

	client.Start()
	client.Wait()
}
```

이 비동기 클라이언트의 구조를 설명한다.

3개의 고루틴이 동시에 작동한다. `readUserInput()`는 사용자로부터 입력을 받아 채널에 보낸다. `sendToServer()`는 입력 채널에서 메시지를 받아 서버로 전송한다. `receiveFromServer()`는 서버로부터 메시지를 받아 출력한다. `done` 채널을 사용하여 모든 고루틴에 종료 신호를 전파한다.

## 9.4 연결 관리와 에러 처리

### 연결 상태 추적

안정적인 게임 서버를 만들기 위해서는 연결의 상태를 정확하게 추적해야 한다.

```go
package main

import (
	"fmt"
	"net"
	"sync"
	"time"
)

type ConnectionState int

const (
	StateConnecting ConnectionState = iota
	StateConnected
	StateDisconnecting
	StateDisconnected
)

func (s ConnectionState) String() string {
	states := map[ConnectionState]string{
		StateConnecting:    "연결 중",
		StateConnected:     "연결됨",
		StateDisconnecting: "종료 중",
		StateDisconnected:  "종료됨",
	}
	return states[s]
}

type ManagedConnection struct {
	conn      net.Conn
	state     ConnectionState
	mu        sync.RWMutex
	startTime time.Time
	errorChan chan error
}

func (mc *ManagedConnection) GetState() ConnectionState {
	mc.mu.RLock()
	defer mc.mu.RUnlock()
	return mc.state
}

func (mc *ManagedConnection) SetState(state ConnectionState) {
	mc.mu.Lock()
	defer mc.mu.Unlock()
	fmt.Printf("상태 변경: %s -> %s\n", mc.state, state)
	mc.state = state
}

func (mc *ManagedConnection) GetDuration() time.Duration {
	mc.mu.RLock()
	defer mc.mu.RUnlock()
	return time.Since(mc.startTime)
}

func (mc *ManagedConnection) Read(buffer []byte) (int, error) {
	n, err := mc.conn.Read(buffer)
	if err != nil {
		mc.SetState(StateDisconnected)
		mc.errorChan <- fmt.Errorf("읽기 실패: %w", err)
		return 0, err
	}
	return n, nil
}

func (mc *ManagedConnection) Write(data []byte) (int, error) {
	n, err := mc.conn.Write(data)
	if err != nil {
		mc.SetState(StateDisconnected)
		mc.errorChan <- fmt.Errorf("쓰기 실패: %w", err)
		return 0, err
	}
	return n, nil
}

func (mc *ManagedConnection) Close() error {
	mc.SetState(StateDisconnecting)
	err := mc.conn.Close()
	mc.SetState(StateDisconnected)
	fmt.Printf("연결 종료 (지속 시간: %v)\n", mc.GetDuration())
	return err
}

func NewManagedConnection(conn net.Conn) *ManagedConnection {
	return &ManagedConnection{
		conn:      conn,
		state:     StateConnected,
		startTime: time.Now(),
		errorChan: make(chan error, 10),
	}
}

func main() {
	listener, err := net.Listen("tcp", ":8080")
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Println("서버 시작: localhost:8080")

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		go handleManagedConnection(conn)
	}
}

func handleManagedConnection(conn net.Conn) {
	mc := NewManagedConnection(conn)
	defer mc.Close()

	fmt.Printf("클라이언트 연결: %s (상태: %s)\n", 
		conn.RemoteAddr(), mc.GetState())

	buffer := make([]byte, 1024)
	for {
		select {
		case err := <-mc.errorChan:
			fmt.Printf("에러 발생: %v\n", err)
		default:
		}

		n, err := mc.Read(buffer)
		if err != nil {
			fmt.Printf("읽기 실패: %v\n", err)
			return
		}

		message := string(buffer[:n])
		fmt.Printf("수신: %s\n", message)

		_, err = mc.Write(buffer[:n])
		if err != nil {
			fmt.Printf("쓰기 실패: %v\n", err)
			return
		}
	}
}
```

이 구현에서 중요한 패턴들을 설명한다.

연결 상태를 열거형으로 정의하여 명확하게 추적한다. `RWMutex`를 사용하여 상태 읽기는 병렬로, 상태 변경은 직렬로 처리한다. 연결이 얼마나 오래 유지되었는지 추적한다. 에러가 발생하면 상태를 자동으로 `StateDisconnected`로 변경한다.

### 고급 에러 처리

게임 서버에서는 네트워크 에러가 빈번하게 발생한다. 이를 적절하게 처리해야 한다.

```go
package main

import (
	"errors"
	"fmt"
	"io"
	"net"
	"syscall"
)

// 커스텀 에러 타입
type NetworkError struct {
	Operation string
	Err       error
	Timestamp int64
}

func (ne *NetworkError) Error() string {
	return fmt.Sprintf("[%s] %v", ne.Operation, ne.Err)
}

func (ne *NetworkError) Unwrap() error {
	return ne.Err
}

// 에러 분류
func classifyError(err error) string {
	if err == nil {
		return "NoError"
	}

	// EOF: 연결이 정상적으로 종료됨
	if errors.Is(err, io.EOF) {
		return "ConnectionClosed"
	}

	// 네트워크 에러인지 확인
	var netErr net.Error
	if errors.As(err, &netErr) {
		if netErr.Timeout() {
			return "Timeout"
		}
		if netErr.Temporary() {
			return "TemporaryError"
		}
		return "NetworkError"
	}

	// 시스템 에러 확인
	var syscallErr syscall.Errno
	if errors.As(err, &syscallErr) {
		switch syscallErr {
		case syscall.ECONNRESET:
			return "ConnectionReset"
		case syscall.EPIPE:
			return "BrokenPipe"
		case syscall.ECONNREFUSED:
			return "ConnectionRefused"
		}
	}

	return "UnknownError"
}

// 에러 처리기
func handleError(operation string, err error) bool {
	if err == nil {
		return true
	}

	classification := classifyError(err)
	fmt.Printf("에러 [%s] %s: %v\n", operation, classification, err)

	// 재시도 가능 여부 판단
	switch classification {
	case "ConnectionClosed":
		fmt.Println("→ 클라이언트가 연결을 종료했습니다")
		return false
	case "Timeout":
		fmt.Println("→ 타임아웃 (재시도 권장)")
		return true
	case "TemporaryError":
		fmt.Println("→ 일시적 에러 (재시도 권장)")
		return true
	case "ConnectionReset", "BrokenPipe":
		fmt.Println("→ 연결 오류 (종료 권장)")
		return false
	default:
		return false
	}
}

func main() {
	listener, err := net.Listen("tcp", ":8080")
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Println("서버 시작: localhost:8080")

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		go handleClientWithErrorRecovery(conn)
	}
}

func handleClientWithErrorRecovery(conn net.Conn) {
	defer conn.Close()

	buffer := make([]byte, 1024)
	retryCount := 0
	maxRetries := 3

	for {
		n, err := conn.Read(buffer)

		// 에러 처리
		if !handleError("Read", err) {
			break
		}

		// 일시적 에러는 재시도
		if err != nil {
			retryCount++
			if retryCount >= maxRetries {
				fmt.Println("재시도 횟수 초과")
				break
			}
			continue
		}

		retryCount = 0 // 성공 시 재시도 카운트 초기화

		message := string(buffer[:n])
		fmt.Printf("수신: %s\n", message)

		_, err = conn.Write(buffer[:n])
		if !handleError("Write", err) {
			break
		}
	}

	fmt.Println("클라이언트 연결 종료")
}
```

이 에러 처리 방식의 특징을 설명한다.

`errors.Is()`와 `errors.As()`를 사용하여 에러의 타입을 정확하게 판단한다. 에러를 분류하여 각각에 맞는 처리를 한다. 일시적 에러는 재시도하고, 치명적 에러는 연결을 종료한다. 재시도 로직을 구현하여 일시적 문제로 인한 연결 끊김을 방지한다.

## 9.5 Keep-Alive와 타임아웃 설정

### TCP Keep-Alive 구현

게임 서버에서는 좀비 연결(클라이언트가 종료되었지만 서버는 여전히 연결 상태로 인식하는 경우)을 방지해야 한다. Keep-Alive 메커니즘을 사용하면 이를 해결할 수 있다.

```go
package main

import (
	"fmt"
	"net"
	"time"
)

type ServerWithKeepAlive struct {
	keepAliveInterval time.Duration
	keepAliveTimeout  time.Duration
}

func NewServerWithKeepAlive(interval, timeout time.Duration) *ServerWithKeepAlive {
	return &ServerWithKeepAlive{
		keepAliveInterval: interval,
		keepAliveTimeout:  timeout,
	}
}

func (s *ServerWithKeepAlive) handleConnection(conn net.Conn) {
	defer conn.Close()

	// TCP Keep-Alive 활성화
	if tcpConn, ok := conn.(*net.TCPConn); ok {
		// Keep-Alive 활성화
		err := tcpConn.SetKeepAlive(true)
		if err != nil {
			fmt.Printf("Keep-Alive 설정 실패: %v\n", err)
		}

		// Keep-Alive 주기 설정 (Go 1.13+)
		err = tcpConn.SetKeepAlivePeriod(s.keepAliveInterval)
		if err != nil {
			fmt.Printf("Keep-Alive 주기 설정 실패: %v\n", err)
		}
	}

	fmt.Printf("클라이언트 연결: %s\n", conn.RemoteAddr())

	// 응용 프로그램 계층의 Keep-Alive도 구현한다
	heartbeatTicker := time.NewTicker(s.keepAliveInterval)
	defer heartbeatTicker.Stop()

	dataChan := make(chan []byte, 1)
	errorChan := make(chan error, 1)

	// 클라이언트로부터 데이터를 읽는 고루틴
	go func() {
		buffer := make([]byte, 1024)
		for {
			n, err := conn.Read(buffer)
			if err != nil {
				errorChan <- err
				return
			}
			dataChan <- buffer[:n]
		}
	}()

	// 메인 이벤트 루프
	for {
		select {
		case data := <-dataChan:
			fmt.Printf("수신: %s\n", string(data))
			// 데이터 수신 시 타임아웃 타이머를 리셋한다
			conn.Write([]byte("pong"))

		case err := <-errorChan:
			fmt.Printf("에러: %v\n", err)
			return

		case <-heartbeatTicker.C:
			// Keep-Alive 핸드셰이크 (선택사항)
			fmt.Printf("Keep-Alive 검사: %s\n", conn.RemoteAddr())
			conn.Write([]byte("ping"))
		}
	}
}

func (s *ServerWithKeepAlive) Start(port string) {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Printf("서버 시작: localhost:%s\n", port)

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		go s.handleConnection(conn)
	}
}

func main() {
	// Keep-Alive 간격: 30초, 타임아웃: 5분
	server := NewServerWithKeepAlive(30*time.Second, 5*time.Minute)
	server.Start("8080")
}
```

이 구현에서 중요한 점들을 설명한다.

`SetKeepAlive(true)`를 호출하여 OS 레벨의 TCP Keep-Alive를 활성화한다. `SetKeepAlivePeriod()`로 Keep-Alive 주기를 설정한다(Go 1.13 이상 필요). 응용 프로그램 계층에서도 별도로 Keep-Alive를 구현하여, 더 정확한 제어가 가능하다.

### 읽기/쓰기 타임아웃 설정

게임 서버의 각 작업은 합리적인 시간 내에 완료되어야 한다.

```go
package main

import (
	"fmt"
	"net"
	"time"
)

type ConnectionWithTimeouts struct {
	conn         net.Conn
	readTimeout  time.Duration
	writeTimeout time.Duration
}

func NewConnectionWithTimeouts(conn net.Conn, read, write time.Duration) *ConnectionWithTimeouts {
	return &ConnectionWithTimeouts{
		conn:         conn,
		readTimeout:  read,
		writeTimeout: write,
	}
}

func (c *ConnectionWithTimeouts) ReadMessage() (string, error) {
	// 읽기 타임아웃 설정
	c.conn.SetReadDeadline(time.Now().Add(c.readTimeout))

	buffer := make([]byte, 1024)
	n, err := c.conn.Read(buffer)
	if err != nil {
		// 타임아웃 에러인지 확인
		if netErr, ok := err.(net.Error); ok && netErr.Timeout() {
			return "", fmt.Errorf("읽기 타임아웃 (%v 초과)", c.readTimeout)
		}
		return "", err
	}

	return string(buffer[:n]), nil
}

func (c *ConnectionWithTimeouts) WriteMessage(message string) error {
	// 쓰기 타임아웃 설정
	c.conn.SetWriteDeadline(time.Now().Add(c.writeTimeout))

	_, err := c.conn.Write([]byte(message))
	if err != nil {
		// 타임아웃 에러인지 확인
		if netErr, ok := err.(net.Error); ok && netErr.Timeout() {
			return fmt.Errorf("쓰기 타임아웃 (%v 초과)", c.writeTimeout)
		}
		return err
	}

	return nil
}

func (c *ConnectionWithTimeouts) Close() error {
	return c.conn.Close()
}

type GameServerWithTimeouts struct {
	readTimeout  time.Duration
	writeTimeout time.Duration
}

func NewGameServerWithTimeouts(read, write time.Duration) *GameServerWithTimeouts {
	return &GameServerWithTimeouts{
		readTimeout:  read,
		writeTimeout: write,
	}
}

func (s *GameServerWithTimeouts) handleGameConnection(conn net.Conn) {
	defer conn.Close()

	fmt.Printf("플레이어 연결: %s\n", conn.RemoteAddr())

	gameConn := NewConnectionWithTimeouts(
		conn,
		s.readTimeout,
		s.writeTimeout,
	)

	// 게임 루프
	for {
		// 플레이어로부터 명령을 읽는다
		command, err := gameConn.ReadMessage()
		if err != nil {
			fmt.Printf("에러: %v\n", err)
			break
		}

		fmt.Printf("명령 수신: %s\n", command)

		// 명령을 처리한다
		response := fmt.Sprintf("명령 처리됨: %s", command)

		// 응답을 보낸다
		err = gameConn.WriteMessage(response)
		if err != nil {
			fmt.Printf("응답 전송 실패: %v\n", err)
			break
		}

		fmt.Printf("응답 전송: %s\n", response)
	}

	fmt.Printf("플레이어 종료: %s\n", conn.RemoteAddr())
}

func (s *GameServerWithTimeouts) Start(port string) {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Printf("게임 서버 시작: localhost:%s\n", port)
	fmt.Printf("읽기 타임아웃: %v, 쓰기 타임아웃: %v\n", 
		s.readTimeout, s.writeTimeout)

	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		go s.handleGameConnection(conn)
	}
}

func main() {
	// 읽기 타임아웃: 10초, 쓰기 타임아웃: 5초
	server := NewGameServerWithTimeouts(
		10*time.Second,
		5*time.Second,
	)
	server.Start("8080")
}
```

이 타임아웃 구현의 특징을 설명한다.

`SetReadDeadline()`과 `SetWriteDeadline()`을 사용하여 각 작업의 타임아웃을 설정한다. 타임아웃 에러를 명시적으로 처리하여 다른 에러와 구분한다. 각 읽기/쓰기 작업마다 타임아웃을 새로 설정하여, 데이터가 도착하면 타이머가 리셋된다.

### 통합: Keep-Alive와 타임아웃이 있는 실제 게임 세션

```go
package main

import (
	"fmt"
	"net"
	"time"
)

type GameSession struct {
	conn            net.Conn
	playerID        string
	readTimeout     time.Duration
	writeTimeout    time.Duration
	idleTimeout     time.Duration
	lastActivityTime time.Time
	closed          bool
}

func NewGameSession(conn net.Conn, playerID string) *GameSession {
	return &GameSession{
		conn:            conn,
		playerID:        playerID,
		readTimeout:     10 * time.Second,
		writeTimeout:    5 * time.Second,
		idleTimeout:     2 * time.Minute,
		lastActivityTime: time.Now(),
	}
}

func (gs *GameSession) UpdateActivity() {
	gs.lastActivityTime = time.Now()
}

func (gs *GameSession) IsIdle() bool {
	return time.Since(gs.lastActivityTime) > gs.idleTimeout
}

func (gs *GameSession) ReadCommand() (string, error) {
	gs.conn.SetReadDeadline(time.Now().Add(gs.readTimeout))

	buffer := make([]byte, 1024)
	n, err := gs.conn.Read(buffer)
	if err != nil {
		return "", err
	}

	gs.UpdateActivity()
	return string(buffer[:n]), nil
}

func (gs *GameSession) SendResponse(response string) error {
	gs.conn.SetWriteDeadline(time.Now().Add(gs.writeTimeout))

	_, err := gs.conn.Write([]byte(response))
	if err != nil {
		return err
	}

	gs.UpdateActivity()
	return nil
}

func (gs *GameSession) Close() error {
	gs.closed = true
	return gs.conn.Close()
}

type RobustGameServer struct {
	sessions map[string]*GameSession
	readTimeout time.Duration
	writeTimeout time.Duration
}

func NewRobustGameServer() *RobustGameServer {
	return &RobustGameServer{
		sessions:     make(map[string]*GameSession),
		readTimeout:  10 * time.Second,
		writeTimeout: 5 * time.Second,
	}
}

func (rgs *RobustGameServer) handleGameSession(conn net.Conn, playerID string) {
	session := NewGameSession(conn, playerID)
	rgs.sessions[playerID] = session
	defer func() {
		session.Close()
		delete(rgs.sessions, playerID)
		fmt.Printf("플레이어 %s 세션 종료\n", playerID)
	}()

	fmt.Printf("플레이어 %s 접속 (%s)\n", playerID, conn.RemoteAddr())

	// 타임아웃 체크 고루틴
	timeoutTicker := time.NewTicker(30 * time.Second)
	defer timeoutTicker.Stop()

	go func() {
		for range timeoutTicker.C {
			if session.IsIdle() {
				fmt.Printf("플레이어 %s 유휴 타임아웃\n", playerID)
				session.SendResponse("timeout:idle")
				session.Close()
			}
		}
	}()

	// 게임 루프
	for !session.closed {
		command, err := session.ReadCommand()
		if err != nil {
			if netErr, ok := err.(net.Error); ok && netErr.Timeout() {
				fmt.Printf("플레이어 %s 읽기 타임아웃\n", playerID)
			} else {
				fmt.Printf("플레이어 %s 읽기 에러: %v\n", playerID, err)
			}
			break
		}

		fmt.Printf("플레이어 %s 명령: %s\n", playerID, command)

		// 명령 처리
		response := fmt.Sprintf("ACK:%s", command)
		err = session.SendResponse(response)
		if err != nil {
			fmt.Printf("플레이어 %s 응답 전송 실패: %v\n", playerID, err)
			break
		}
	}
}

func (rgs *RobustGameServer) Start(port string) {
	listener, err := net.Listen("tcp", ":"+port)
	if err != nil {
		fmt.Printf("리스너 생성 실패: %v\n", err)
		return
	}
	defer listener.Close()

	fmt.Printf("게임 서버 시작: localhost:%s\n", port)

	playerCounter := 0
	for {
		conn, err := listener.Accept()
		if err != nil {
			fmt.Printf("연결 수락 실패: %v\n", err)
			continue
		}

		playerCounter++
		playerID := fmt.Sprintf("player-%d", playerCounter)
		go rgs.handleGameSession(conn, playerID)
	}
}

func main() {
	server := NewRobustGameServer()
	server.Start("8080")
}
```

이 최종 구현에서는 다음의 특징들을 포함한다.

각 세션이 마지막 활동 시간을 추적하여 유휴 타임아웃을 감지한다. 읽기/쓰기 타임아웃을 각각 설정하여 네트워크 문제를 조기에 감지한다. 활동이 있을 때마다 시간을 업데이트하여, 계속 활동하는 연결은 타임아웃되지 않는다. 별도의 타임아웃 체크 고루틴을 실행하여, 장시간 유휴 상태인 세션을 정리한다.

---

## 정리

TCP 소켓 프로그래밍은 게임 서버의 기초이다. 다음은 이 장에서 학습한 주요 개념들을 정리한 표이다.

```
┌─────────────────────────┬──────────────────────────────────────┐
│ 개념                    │ 설명                                │
├─────────────────────────┼──────────────────────────────────────┤
│ Listener                │ 들어오는 연결을 대기하는 객체         │
│ Conn                    │ 양방향 네트워크 연결                 │
│ Addr                    │ 네트워크 주소 (IP:Port)             │
├─────────────────────────┼──────────────────────────────────────┤
│ net.Listen()            │ TCP 리스너 생성                      │
│ listener.Accept()       │ 새로운 연결 대기                     │
│ net.Dial()              │ 서버에 연결                          │
├─────────────────────────┼──────────────────────────────────────┤
│ conn.Read()             │ 데이터 읽기                          │
│ conn.Write()            │ 데이터 쓰기                          │
│ conn.Close()            │ 연결 종료                            │
├─────────────────────────┼──────────────────────────────────────┤
│ SetReadDeadline()       │ 읽기 타임아웃 설정                   │
│ SetWriteDeadline()      │ 쓰기 타임아웃 설정                   │
│ SetKeepAlive()          │ TCP Keep-Alive 활성화                │
│ SetKeepAlivePeriod()    │ Keep-Alive 주기 설정                 │
└─────────────────────────┴──────────────────────────────────────┘
```

게임 서버 개발 시 TCP 소켓 프로그래밍의 핵심 원칙을 정리하면 다음과 같다.

첫째, 항상 `defer`를 사용하여 연결을 정리한다. 비정상 종료 시에도 리소스가 누수되지 않도록 해야 한다.

둘째, 각 클라이언트를 별도의 고루틴에서 처리하여 동시성을 확보한다. Go의 고루틴은 매우 가볍고 효율적이므로 부담 없이 사용할 수 있다.

셋째, 적절한 타임아웃을 설정하여 좀비 연결이나 행(hang) 상태를 방지한다.

넷째, Keep-Alive 메커니즘을 구현하여 네트워크 상태를 주기적으로 확인한다.

다섯째, 에러를 분류하여 각각에 맞는 처리를 한다. 일시적 에러는 재시도하고, 치명적 에러는 연결을 종료한다.

이러한 원칙들을 따르면, 안정적이고 확장 가능한 게임 서버를 구축할 수 있다. 다음 장에서는 이 TCP 연결 위에서 게임 프로토콜을 어떻게 설계하고 구현하는지 학습하게 된다.

  
# Chapter 10. 바이너리 프로토콜 설계

## 10.1 텍스트 vs 바이너리 프로토콜

게임 서버 개발에서 클라이언트와 서버 간의 통신을 위해서는 명확한 프로토콜이 필요하다. 프로토콜은 크게 텍스트 기반과 바이너리 기반으로 나뉜다.

텍스트 프로토콜은 JSON이나 XML 같은 형식을 사용하여 사람이 읽을 수 있는 형태로 데이터를 주고받는다. 개발과 디버깅이 쉽고 네트워크 통신 내용을 직관적으로 파악할 수 있다는 장점이 있다. 하지만 데이터 크기가 크고 직렬화/역직렬화 비용이 상대적으로 높다는 단점이 있다.

바이너리 프로토콜은 데이터를 바이트 형태로 직접 인코딩하여 전송한다. 데이터 크기가 작고 인코딩/디코딩이 빠르며 네트워크 대역폭을 효율적으로 사용할 수 있다. 특히 실시간 게임 서버처럼 높은 빈도로 메시지를 주고받아야 하는 환경에서는 바이너리 프로토콜이 필수다. 단점으로는 바이트 단위의 세밀한 제어가 필요하고 바이트 순서(Endianness) 같은 플랫폼 특성을 고려해야 한다.

게임 서버에서는 일반적으로 바이너리 프로토콜을 사용한다. 초당 수십에서 수백 건의 메시지 처리가 필요한 환경에서는 데이터 크기와 처리 속도가 매우 중요하기 때문이다.

```
텍스트 프로토콜 예시 (JSON)
{"type": "move", "playerId": 1, "x": 100, "y": 200}
크기: 약 50바이트

바이너리 프로토콜 예시 (같은 정보)
[패킷타입:1바이트][플레이어ID:4바이트][X:4바이트][Y:4바이트]
크기: 9바이트
```

## 10.2 패킷 구조 설계

바이너리 프로토콜을 설계할 때는 일관되고 효율적인 패킷 구조가 필수다. 모든 패킷은 기본적인 헤더 부분과 가변적인 페이로드 부분으로 이루어진다.

```
일반적인 게임 서버 패킷 구조

┌─────────────┬──────────┬────────────────────┬──────────┐
│   패킷 길이  │ 패킷 ID  │   프로토콜 ID      │  페이로드 │
│  (4바이트)   │(2바이트) │   (1바이트)        │  (가변)   │
└─────────────┴──────────┴────────────────────┴──────────┘
```

패킷 길이는 헤더를 포함한 전체 패킷의 바이트 크기를 나타낸다. 이를 통해 수신자는 다음 패킷의 경계를 정확히 파악할 수 있다.

패킷 ID는 요청과 응답을 매칭하기 위해 사용된다. 클라이언트가 특정 패킷을 보낼 때 ID를 포함시키고, 서버가 응답할 때도 같은 ID를 포함시킴으로써 비동기 통신에서도 순서를 보장한다.

프로토콜 ID는 패킷의 타입을 나타낸다. 로그인, 움직임, 공격 등 각각의 액션에 서로 다른 프로토콜 ID를 부여한다.

페이로드는 실제 게임 데이터를 담는 부분으로 프로토콜마다 구조가 다르다.

다음은 포커 게임 서버를 위한 프로토콜 ID 정의의 예시다.

```go
package protocol

// 프로토콜 ID 상수 정의
const (
	// 시스템 프로토콜
	CmdHeartbeat   = 1000
	CmdDisconnect  = 1001
	
	// 로그인/로그아웃
	CmdLogin       = 2000
	CmdLoginResult = 2001
	CmdLogout      = 2002
	
	// 방 관련
	CmdCreateRoom  = 3000
	CmdCreateRoomResult = 3001
	CmdJoinRoom    = 3002
	CmdJoinRoomResult = 3003
	CmdLeaveRoom   = 3004
	CmdRoomUpdated = 3005
	
	// 게임 진행
	CmdGameStart   = 4000
	CmdDealCards   = 4001
	CmdBetAction   = 4002
	CmdBetResult   = 4003
	CmdShowdown    = 4004
	CmdGameResult  = 4005
	CmdGameEnd     = 4006
)

// 프로토콜 구조체 - 모든 프로토콜의 기본 헤더
type PacketHeader struct {
	Length    uint32 // 전체 패킷 길이 (헤더 포함)
	PacketID  uint16 // 요청-응답 매칭용 ID
	ProtocolID uint16 // 프로토콜 타입
}

// 헤더 크기 상수
const HeaderSize = 8 // 4 + 2 + 2 = 8바이트
```

이 구조에서 PacketID는 선택적으로 사용될 수 있다. 서버에서 먼저 보내는 메시지(예: 게임 시작 알림)는 특정 ID가 필요 없지만, 클라이언트의 요청에 대한 응답은 같은 ID를 사용하여 클라이언트가 어느 요청에 대한 응답인지 구분할 수 있다.

## 10.3 encoding/binary 패키지

Go의 `encoding/binary` 패키지는 구조체를 바이트 슬라이스로 변환하거나 바이트를 구조체로 복원하는 작업을 수행한다. 이 패키지의 핵심 함수는 `Binary.Read()`와 `Binary.Write()`다.

```go
package main

import (
	"bytes"
	"encoding/binary"
	"fmt"
)

// 로그인 프로토콜
type LoginRequest struct {
	UserID   uint32
	NameLen  uint16
	Name     string // 가변 길이 데이터는 별도로 처리
	Password string
}

func EncodeLoginRequest(userID uint32, name string, password string) ([]byte, error) {
	// 버퍼 생성
	buf := new(bytes.Buffer)
	
	// 고정 길이 필드들을 바이너리로 인코딩
	// WriteUint32: uint32를 4바이트로 변환하여 버퍼에 쓴다
	if err := binary.Write(buf, binary.LittleEndian, userID); err != nil {
		return nil, err
	}
	
	// 문자열 길이를 uint16으로 인코딩
	nameLen := uint16(len(name))
	if err := binary.Write(buf, binary.LittleEndian, nameLen); err != nil {
		return nil, err
	}
	
	// 문자열 데이터를 바이트 슬라이스로 변환하여 쓴다
	if _, err := buf.Write([]byte(name)); err != nil {
		return nil, err
	}
	
	// 패스워드도 같은 방식으로 처리
	passwordLen := uint16(len(password))
	if err := binary.Write(buf, binary.LittleEndian, passwordLen); err != nil {
		return nil, err
	}
	
	if _, err := buf.Write([]byte(password)); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

func DecodeLoginRequest(data []byte) (*LoginRequest, error) {
	// bytes.NewReader는 바이트 슬라이스를 Reader 인터페이스로 감싸준다
	// Reader 인터페이스를 사용하면 sequential한 읽기가 가능하다
	reader := bytes.NewReader(data)
	
	login := &LoginRequest{}
	
	// ReadUint32: Reader에서 4바이트를 읽어 uint32로 변환
	if err := binary.Read(reader, binary.LittleEndian, &login.UserID); err != nil {
		return nil, err
	}
	
	// 이름 길이 읽기
	if err := binary.Read(reader, binary.LittleEndian, &login.NameLen); err != nil {
		return nil, err
	}
	
	// 이름 데이터 읽기 - NameLen만큼의 바이트를 읽는다
	nameBytes := make([]byte, login.NameLen)
	if _, err := reader.Read(nameBytes); err != nil {
		return nil, err
	}
	login.Name = string(nameBytes)
	
	// 패스워드 길이 읽기
	var passwordLen uint16
	if err := binary.Read(reader, binary.LittleEndian, &passwordLen); err != nil {
		return nil, err
	}
	
	// 패스워드 데이터 읽기
	passwordBytes := make([]byte, passwordLen)
	if _, err := reader.Read(passwordBytes); err != nil {
		return nil, err
	}
	login.Password = string(passwordBytes)
	
	return login, nil
}

func main() {
	// 인코딩 테스트
	encoded, err := EncodeLoginRequest(12345, "john", "secret123")
	if err != nil {
		fmt.Println("Encoding error:", err)
		return
	}
	
	fmt.Printf("Encoded bytes: %v\n", encoded)
	fmt.Printf("Length: %d bytes\n", len(encoded))
	
	// 디코딩 테스트
	decoded, err := DecodeLoginRequest(encoded)
	if err != nil {
		fmt.Println("Decoding error:", err)
		return
	}
	
	fmt.Printf("Decoded UserID: %d\n", decoded.UserID)
	fmt.Printf("Decoded Name: %s\n", decoded.Name)
	fmt.Printf("Decoded Password: %s\n", decoded.Password)
}
```

위 코드에서 `binary.LittleEndian`은 바이트 순서를 정의하는 부분인데, 다음 섹션에서 자세히 설명한다.

실제 프로토콜 구현에서는 좀 더 체계적으로 접근하는 것이 좋다. 모든 인코더/디코더를 고정 길이로 처리하면 메모리 낭비가 심하므로, 가변 길이 데이터를 효율적으로 처리하는 방법이 필요하다.

```go
package protocol

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"io"
)

// Packet 인터페이스 - 모든 프로토콜이 구현해야 한다
type Packet interface {
	Encode() ([]byte, error)
}

// PacketDecoder 인터페이스 - 각 프로토콜 타입별 디코더
type PacketDecoder interface {
	Decode(data []byte) error
}

// 고정 길이 필드를 포함한 기본 헤더 구조
type BasePacket struct {
	Length     uint32
	PacketID   uint16
	ProtocolID uint16
}

// 베팅 액션 요청 - 프로토콜 ID 4002
type BetActionRequest struct {
	BasePacket
	PlayerID uint32
	BetAmount uint32
}

// BetActionRequest를 바이너리로 인코딩한다
func (p *BetActionRequest) Encode() ([]byte, error) {
	buf := new(bytes.Buffer)
	
	// 프로토콜 ID 설정
	p.ProtocolID = 4002
	
	// 페이로드를 먼저 인코딩하여 크기를 계산한다
	payloadBuf := new(bytes.Buffer)
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.PlayerID); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.BetAmount); err != nil {
		return nil, err
	}
	
	payload := payloadBuf.Bytes()
	
	// 전체 길이 계산: 헤더(8바이트) + 페이로드
	p.Length = uint32(8 + len(payload))
	
	// 헤더 인코딩
	if err := binary.Write(buf, binary.LittleEndian, p.Length); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.PacketID); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.ProtocolID); err != nil {
		return nil, err
	}
	
	// 페이로드 추가
	if _, err := buf.Write(payload); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

// 바이너리 데이터를 BetActionRequest로 디코딩한다
func (p *BetActionRequest) Decode(data []byte) error {
	reader := bytes.NewReader(data)
	
	// 헤더 디코딩
	if err := binary.Read(reader, binary.LittleEndian, &p.Length); err != nil {
		return err
	}
	if err := binary.Read(reader, binary.LittleEndian, &p.PacketID); err != nil {
		return err
	}
	if err := binary.Read(reader, binary.LittleEndian, &p.ProtocolID); err != nil {
		return err
	}
	
	// 프로토콜 ID 검증
	if p.ProtocolID != 4002 {
		return fmt.Errorf("invalid protocol ID: expected 4002, got %d", p.ProtocolID)
	}
	
	// 페이로드 디코딩
	if err := binary.Read(reader, binary.LittleEndian, &p.PlayerID); err != nil {
		return err
	}
	if err := binary.Read(reader, binary.LittleEndian, &p.BetAmount); err != nil {
		return err
	}
	
	return nil
}

func main() {
	// 베팅 요청 생성
	betReq := &BetActionRequest{
		BasePacket: BasePacket{PacketID: 1001},
		PlayerID:   42,
		BetAmount:  5000,
	}
	
	// 인코딩
	encoded, err := betReq.Encode()
	if err != nil {
		fmt.Println("Encode error:", err)
		return
	}
	
	fmt.Printf("Encoded: %v\n", encoded)
	fmt.Printf("Packet length: %d bytes\n", len(encoded))
	
	// 디코딩
	decoded := &BetActionRequest{}
	if err := decoded.Decode(encoded); err != nil {
		fmt.Println("Decode error:", err)
		return
	}
	
	fmt.Printf("Decoded - PlayerID: %d, BetAmount: %d\n", 
		decoded.PlayerID, decoded.BetAmount)
}
```

## 10.4 Little Endian vs Big Endian

바이트 순서(Endianness)는 다중 바이트 정수를 바이트 배열로 변환할 때 어느 바이트가 먼저 오는지를 결정한다.

Little Endian은 가장 낮은 유효 바이트(Least Significant Byte)가 먼저 온다. 예를 들어 16진수 0x12345678을 Little Endian으로 저장하면 78 56 34 12 순서로 저장된다. 대부분의 PC 프로세서(x86, x64, ARM)가 Little Endian을 사용한다.

Big Endian은 가장 높은 유효 바이트(Most Significant Byte)가 먼저 온다. 같은 값 0x12345678을 Big Endian으로 저장하면 12 34 56 78 순서로 저장된다. 네트워크 통신이나 일부 임베디드 시스템에서 사용된다.

```
0x12345678을 바이트 배열로 변환

Little Endian (x86, x64, ARM)
┌──┬──┬──┬──┐
│78│56│34│12│
└──┴──┴──┴──┘

Big Endian (네트워크, 모토로라)
┌──┬──┬──┬──┐
│12│34│56│78│
└──┴──┴──┴──┘
```

게임 서버 개발에서는 일반적으로 클라이언트와 서버가 같은 플랫폼에서 실행되므로 바이트 순서 문제가 적게 발생한다. 하지만 모바일 클라이언트와 서버가 다른 플랫폼에서 실행될 수 있으므로 명확한 바이트 순서를 정의해야 한다. 대부분의 게임 서버는 일관성을 위해 Big Endian을 사용하거나, 최신 추세에 따라 Little Endian을 사용한다.

중요한 것은 클라이언트와 서버가 같은 바이트 순서를 사용해야 한다는 것이다. 이를 프로토콜 문서에 명시해야 한다.

```go
package main

import (
	"bytes"
	"encoding/binary"
	"fmt"
)

func demonstrateEndianness() {
	// 테스트 값
	value := uint32(0x12345678)
	
	// Little Endian 인코딩
	littleEndianBuf := new(bytes.Buffer)
	binary.Write(littleEndianBuf, binary.LittleEndian, value)
	littleEndianBytes := littleEndianBuf.Bytes()
	
	fmt.Print("Little Endian (0x12345678): ")
	for _, b := range littleEndianBytes {
		fmt.Printf("%02X ", b)
	}
	fmt.Println()
	
	// Big Endian 인코딩
	bigEndianBuf := new(bytes.Buffer)
	binary.Write(bigEndianBuf, binary.BigEndian, value)
	bigEndianBytes := bigEndianBuf.Bytes()
	
	fmt.Print("Big Endian (0x12345678):    ")
	for _, b := range bigEndianBytes {
		fmt.Printf("%02X ", b)
	}
	fmt.Println()
	
	// 역 변환으로 올바르게 복원되는지 확인
	var littleValue, bigValue uint32
	binary.Read(bytes.NewReader(littleEndianBytes), binary.LittleEndian, &littleValue)
	binary.Read(bytes.NewReader(bigEndianBytes), binary.BigEndian, &bigValue)
	
	fmt.Printf("\nDecoded Little Endian: 0x%X\n", littleValue)
	fmt.Printf("Decoded Big Endian: 0x%X\n", bigValue)
	
	// 호스트의 기본 바이트 순서 확인
	hostOrder := binary.LittleEndian
	if hostOrder == binary.BigEndian {
		fmt.Println("\nHost is Big Endian")
	} else {
		fmt.Println("\nHost is Little Endian")
	}
}

func main() {
	demonstrateEndianness()
}
```

출력 결과는 다음과 같다:

```
Little Endian (0x12345678): 78 56 34 12 
Big Endian (0x12345678):    12 34 56 78 

Decoded Little Endian: 0x12345678
Decoded Big Endian: 0x12345678

Host is Little Endian
```

실제 게임 프로토콜에서는 반드시 하나의 바이트 순서를 일관되게 사용해야 한다. 다음은 바이트 순서 추상화를 통해 변경을 쉽게 할 수 있도록 한 예시다.

```go
package protocol

import (
	"encoding/binary"
)

// 게임 서버 프로토콜에서 사용할 바이트 순서 정의
// 만약 Big Endian으로 변경하려면 이 한 곳만 수정하면 된다
var ByteOrder = binary.LittleEndian

// 모든 인코딩/디코딩에서는 protocol.ByteOrder를 사용한다
// 예: binary.Write(buf, protocol.ByteOrder, value)
// 이렇게 하면 나중에 바이트 순서를 변경할 때 코드 전체를 수정할 필요가 없다
```

## 10.5 가변 길이 데이터 처리

게임 서버의 프로토콜에서는 플레이어 이름, 채팅 메시지, 아이템 데이터 등 길이가 정해지지 않은 데이터를 처리해야 한다. 가변 길이 데이터를 효율적으로 인코딩하는 방식은 여러 가지가 있다.

가장 간단한 방식은 길이-값(Length-Value) 패턴으로, 데이터 앞에 길이를 명시하는 방법이다. 이전 예제에서 보았듯이 문자열 길이를 uint16으로 저장하고 그 뒤에 문자열을 저장하는 방식이다.

더 효율적인 방식은 가변 길이 인코딩(Variable-Length Encoding)으로, 데이터의 크기에 따라 길이 정보의 바이트 수를 조정한다. 이는 프로토콜 버퍼(Protocol Buffers)나 메시지팩(MessagePack)에서 사용하는 방식이다.

```
길이-값 패턴 (Length-Value Pattern)

짧은 문자열 "Hi"
┌────┬──┬──┐
│ 2  │H │I │
└────┴──┴──┘
2바이트 + 2바이트 = 4바이트

긴 문자열 "Hello, World!"
┌────┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐
│ 13 │H │e │l │l │o │, │  │W │o │r │l │d │! │
└────┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘
2바이트 + 13바이트 = 15바이트
```

길이-값 패턴은 구현이 간단하고 이해하기 쉽지만, 항상 길이에 고정 바이트(보통 2 또는 4)를 사용하므로 작은 데이터를 저장할 때 낭비가 생긴다.

가변 길이 인코딩은 작은 값은 적은 바이트로, 큰 값은 많은 바이트로 인코딩한다. 프로토콜 버퍼의 방식을 따르면 각 바이트의 최상위 비트(MSB)를 플래그로 사용한다. MSB가 1이면 다음 바이트가 더 있다는 뜻이고, 0이면 마지막 바이트라는 뜻이다.

```
가변 길이 인코딩 예시

값 127 (7비트 사용):
┌─┬──────┐
│0│ 1111111│ = 0x7F
└─┴──────┘
1바이트

값 128 (8비트 사용):
┌─┬──────┐┌─┬──────┐
│1│0000000││0│0000001│ = 0x80 0x01
└─┴──────┘└─┴──────┘
2바이트

값 16383 (14비트 사용):
┌─┬──────┐┌─┬──────┐
│1│1111111││0│1111111│ = 0xFF 0x7F
└─┴──────┘└─┴──────┘
2바이트
```

게임 서버에서 자주 사용되는 메시지(플레이어 목록, 방 정보)는 작은 크기가 많으므로 가변 길이 인코딩이 효율적이다. 다음은 가변 길이 인코딩을 구현한 예제다.

```go
package protocol

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"io"
)

// Varint - 가변 길이 정수 인코딩/디코딩
// 프로토콜 버퍼의 가변 길이 인코딩 방식을 따른다

// EncodeVarint는 uint64 값을 가변 길이로 인코딩한다
// 최대 10바이트까지 사용 가능하다
func EncodeVarint(value uint64) []byte {
	buf := make([]byte, 0, 10)
	
	for value > 127 {
		// 최하위 7비트를 추출하고 MSB를 1로 설정
		buf = append(buf, byte((value&0x7F)|0x80))
		value >>= 7
	}
	
	// 마지막 바이트는 MSB가 0
	buf = append(buf, byte(value&0x7F))
	
	return buf
}

// DecodeVarint는 가변 길이로 인코딩된 바이트로부터 uint64를 디코딩한다
func DecodeVarint(data []byte) (uint64, int, error) {
	var value uint64
	var shift uint
	
	for i, b := range data {
		// 하위 7비트를 결과에 추가
		value |= uint64(b&0x7F) << shift
		
		// MSB가 0이면 디코딩 완료
		if b&0x80 == 0 {
			return value, i + 1, nil
		}
		
		shift += 7
		
		// 너무 많은 바이트를 사용한 경우 오류
		if shift >= 64 {
			return 0, 0, fmt.Errorf("varint overflow")
		}
	}
	
	return 0, 0, fmt.Errorf("incomplete varint")
}

// 가변 길이 문자열 인코딩을 사용하는 메시지 구조체
type ChatMessage struct {
	PlayerID uint32
	Message  string
}

// ChatMessage를 효율적으로 인코딩한다
func (m *ChatMessage) Encode() ([]byte, error) {
	buf := new(bytes.Buffer)
	
	// 플레이어 ID (고정 4바이트)
	if err := binary.Write(buf, binary.LittleEndian, m.PlayerID); err != nil {
		return nil, err
	}
	
	// 메시지 길이를 가변 길이로 인코딩
	messageBytes := []byte(m.Message)
	lengthBytes := EncodeVarint(uint64(len(messageBytes)))
	if _, err := buf.Write(lengthBytes); err != nil {
		return nil, err
	}
	
	// 메시지 데이터
	if _, err := buf.Write(messageBytes); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

// 바이너리 데이터를 ChatMessage로 디코딩한다
func (m *ChatMessage) Decode(data []byte) error {
	reader := bytes.NewReader(data)
	
	// 플레이어 ID 읽기
	if err := binary.Read(reader, binary.LittleEndian, &m.PlayerID); err != nil {
		return err
	}
	
	// 남은 데이터 읽기
	remainingData, err := io.ReadAll(reader)
	if err != nil {
		return err
	}
	
	// 메시지 길이 디코딩
	msgLen, bytesRead, err := DecodeVarint(remainingData)
	if err != nil {
		return err
	}
	
	// 메시지 데이터 추출
	msgData := remainingData[bytesRead : bytesRead+int(msgLen)]
	m.Message = string(msgData)
	
	return nil
}

func demonstrateVarint() {
	testValues := []uint64{0, 127, 128, 255, 256, 16383, 16384, 2097151, 2097152}
	
	fmt.Println("Varint Encoding Demo:")
	fmt.Println("────────────────────────────────────")
	
	for _, val := range testValues {
		encoded := EncodeVarint(val)
		decoded, _, _ := DecodeVarint(encoded)
		
		fmt.Printf("Value: %7d | Bytes: %d | Encoded: ", val, len(encoded))
		for _, b := range encoded {
			fmt.Printf("%02X ", b)
		}
		fmt.Printf("| Decoded: %d\n", decoded)
	}
	
	fmt.Println("\n효율성 비교:")
	fmt.Println("────────────────────────────────────")
	
	// 고정 4바이트 vs 가변 길이
	for _, val := range []uint64{100, 1000, 100000} {
		varintBytes := EncodeVarint(val)
		fmt.Printf("Value %6d: Varint=%d bytes vs Fixed=4 bytes\n", val, len(varintBytes))
	}
}

func demonstrateChatMessage() {
	fmt.Println("\n\nChatMessage Encoding Demo:")
	fmt.Println("────────────────────────────────────")
	
	// 짧은 메시지
	shortMsg := &ChatMessage{
		PlayerID: 123,
		Message:  "Hi",
	}
	
	encoded, _ := shortMsg.Encode()
	fmt.Printf("Short message (PlayerID:123, 'Hi'): %d bytes\n", len(encoded))
	fmt.Printf("Bytes: %v\n", encoded)
	
	// 긴 메시지
	longMsg := &ChatMessage{
		PlayerID: 456,
		Message:  "This is a longer message for testing",
	}
	
	encoded, _ = longMsg.Encode()
	fmt.Printf("\nLong message (PlayerID:456, 'This is a longer message for testing'): %d bytes\n", len(encoded))
	
	// 디코딩 테스트
	decoded := &ChatMessage{}
	decoded.Decode(encoded)
	fmt.Printf("Decoded: PlayerID=%d, Message='%s'\n", decoded.PlayerID, decoded.Message)
}

func main() {
	demonstrateVarint()
	demonstrateChatMessage()
}
```

길이-값 패턴과 가변 길이 인코딩의 선택은 프로토콜 설계의 트레이드오프다. 길이-값 패턴은 구현이 간단하고 최대 크기 제한이 명확하지만, 가변 길이 인코딩은 네트워크 대역폭을 더 효율적으로 사용한다. 게임 서버는 대역폭이 중요하므로 가변 길이 인코딩을 추천한다.

## 10.6 프로토콜 버전 관리

게임 서버는 지속적으로 업데이트되고 기능이 추가된다. 새로운 기능을 추가할 때 기존 클라이언트와의 호환성을 유지해야 한다. 프로토콜 버전 관리는 이러한 문제를 해결하는 중요한 설계 원칙이다.

프로토콜 버전 관리 전략은 크게 두 가지가 있다.

첫째는 프로토콜 자체에 버전을 포함시키는 방식이다. 각 패킷의 헤더에 프로토콜 버전 정보를 포함시키고, 수신자는 이 정보를 바탕으로 어떻게 해석할지 결정한다.

둘째는 프로토콜 ID를 버전별로 구분하는 방식이다. 기존 명령에 기능을 추가하려면 새로운 프로토콜 ID를 할당하고, 서버는 필요에 따라 두 버전 모두를 처리할 수 있도록 구현한다.

```
프로토콜 버전 관리 전략 예시

전략 1: 헤더에 버전 포함
┌─────────┬──────────┬─────────┬────────────┬──────────┐
│ 길이    │패킷ID   │프로토콜ID│프로토콜Ver │페이로드  │
│(4바이트)│(2바이트)│(2바이트)│(1바이트)   │ (가변)   │
└─────────┴──────────┴─────────┴────────────┴──────────┘

전략 2: 프로토콜 ID로 버전 구분
CmdBetAction_V1    = 4002
CmdBetAction_V2    = 4003  // 기존 프로토콜에 새 필드 추가시 새 ID
```

포커 게임 서버의 예를 들면, 초기에는 게임 결과에 우승자 ID와 상금 정보만 전송했다고 가정하자. 나중에 통계 정보를 추가하려면 어떻게 해야 할까.

```go
package protocol

import (
	"bytes"
	"encoding/binary"
	"fmt"
)

const (
	GameResultV1 = 4005
	GameResultV2 = 4006 // 버전 2: 통계 정보 추가
)

// GameResult V1 - 우승자와 상금 정보만 포함
type GameResultV1 struct {
	BasePacket
	WinnerID  uint32
	PrizeAmount uint32
}

func (p *GameResultV1) Encode() ([]byte, error) {
	buf := new(bytes.Buffer)
	p.ProtocolID = GameResultV1
	
	payloadBuf := new(bytes.Buffer)
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.WinnerID); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.PrizeAmount); err != nil {
		return nil, err
	}
	
	payload := payloadBuf.Bytes()
	p.Length = uint32(8 + len(payload))
	
	if err := binary.Write(buf, binary.LittleEndian, p.Length); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.PacketID); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.ProtocolID); err != nil {
		return nil, err
	}
	if _, err := buf.Write(payload); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

// GameResult V2 - 우승자, 상금, 통계 정보 포함
type GameResultV2 struct {
	BasePacket
	WinnerID      uint32
	PrizeAmount   uint32
	HandType      uint8  // 우승 핸드 타입 (1=High Card, 2=Pair, ...)
	MaxBetAmount  uint32
	TotalRaises   uint16
}

func (p *GameResultV2) Encode() ([]byte, error) {
	buf := new(bytes.Buffer)
	p.ProtocolID = GameResultV2
	
	payloadBuf := new(bytes.Buffer)
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.WinnerID); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.PrizeAmount); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.HandType); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.MaxBetAmount); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.TotalRaises); err != nil {
		return nil, err
	}
	
	payload := payloadBuf.Bytes()
	p.Length = uint32(8 + len(payload))
	
	if err := binary.Write(buf, binary.LittleEndian, p.Length); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.PacketID); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.ProtocolID); err != nil {
		return nil, err
	}
	if _, err := buf.Write(payload); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

// 서버에서 두 버전 모두를 처리할 수 있도록 지원
type GameResultHandler struct{}

func (h *GameResultHandler) Handle(protocolID uint16, data []byte) error {
	switch protocolID {
	case GameResultV1:
		fmt.Println("Processing GameResult V1 (우승자와 상금 정보)")
		result := &GameResultV1{}
		// 디코딩 로직 생략
		fmt.Printf("Winner: %d, Prize: %d\n", result.WinnerID, result.PrizeAmount)
		
	case GameResultV2:
		fmt.Println("Processing GameResult V2 (우승자, 상금, 통계 정보)")
		result := &GameResultV2{}
		// 디코딩 로직 생략
		fmt.Printf("Winner: %d, Prize: %d, HandType: %d\n",
			result.WinnerID, result.PrizeAmount, result.HandType)
		
	default:
		return fmt.Errorf("unknown game result protocol: %d", protocolID)
	}
	
	return nil
}

func main() {
	// V1 패킷 생성 및 인코딩
	resultV1 := &GameResultV1{
		BasePacket: BasePacket{PacketID: 1},
		WinnerID:   42,
		PrizeAmount: 50000,
	}
	
	v1Data, _ := resultV1.Encode()
	fmt.Printf("GameResult V1: %d bytes\n", len(v1Data))
	
	// V2 패킷 생성 및 인코딩
	resultV2 := &GameResultV2{
		BasePacket: BasePacket{PacketID: 1},
		WinnerID:   42,
		PrizeAmount: 50000,
		HandType:   7, // Straight Flush
		MaxBetAmount: 10000,
		TotalRaises: 4,
	}
	
	v2Data, _ := resultV2.Encode()
	fmt.Printf("GameResult V2: %d bytes\n", len(v2Data))
	
	// 서버가 두 버전 모두 처리
	handler := &GameResultHandler{}
	fmt.Println("\n서버 처리:")
	handler.Handle(GameResultV1, v1Data)
	fmt.Println()
	handler.Handle(GameResultV2, v2Data)
}
```

또 다른 프로토콜 버전 관리 방식은 헤더에 명시적인 버전 정보를 포함시키는 것이다. 이 방식은 같은 프로토콜 ID를 사용하면서도 버전에 따라 다르게 해석할 수 있다.

```go
package protocol

import (
	"bytes"
	"encoding/binary"
	"fmt"
)

// 버전을 포함한 개선된 헤더 구조
type VersionedPacketHeader struct {
	Length      uint32
	PacketID    uint16
	ProtocolID  uint16
	Version     uint8  // 프로토콜 버전
	Reserved    uint8  // 향후 확장용 예약 필드
}

const HeaderSizeWithVersion = 10 // 기존 8 + 2

// 동일한 프로토콜 ID를 유지하면서 버전으로 차등 처리
const CmdGameResult = 4005

type VersionedGameResult struct {
	Header      VersionedPacketHeader
	WinnerID    uint32
	PrizeAmount uint32
	// V2 이상에서만 포함
	HandType    uint8
	MaxBetAmount uint32
	TotalRaises uint16
}

func (p *VersionedGameResult) Encode(version uint8) ([]byte, error) {
	buf := new(bytes.Buffer)
	p.Header.ProtocolID = CmdGameResult
	p.Header.Version = version
	p.Header.PacketID = 1
	
	payloadBuf := new(bytes.Buffer)
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.WinnerID); err != nil {
		return nil, err
	}
	if err := binary.Write(payloadBuf, binary.LittleEndian, p.PrizeAmount); err != nil {
		return nil, err
	}
	
	// 버전 2 이상에서만 추가 필드 인코딩
	if version >= 2 {
		if err := binary.Write(payloadBuf, binary.LittleEndian, p.HandType); err != nil {
			return nil, err
		}
		if err := binary.Write(payloadBuf, binary.LittleEndian, p.MaxBetAmount); err != nil {
			return nil, err
		}
		if err := binary.Write(payloadBuf, binary.LittleEndian, p.TotalRaises); err != nil {
			return nil, err
		}
	}
	
	payload := payloadBuf.Bytes()
	p.Header.Length = uint32(HeaderSizeWithVersion + len(payload))
	
	// 헤더 쓰기
	if err := binary.Write(buf, binary.LittleEndian, p.Header.Length); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.Header.PacketID); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.Header.ProtocolID); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.Header.Version); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.Header.Reserved); err != nil {
		return nil, err
	}
	
	if _, err := buf.Write(payload); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

func main() {
	result := &VersionedGameResult{
		WinnerID:     42,
		PrizeAmount:  50000,
		HandType:     7,
		MaxBetAmount: 10000,
		TotalRaises:  4,
	}
	
	// V1으로 인코딩 (추가 필드 없음)
	v1Data, _ := result.Encode(1)
	fmt.Printf("Versioned GameResult V1: %d bytes\n", len(v1Data))
	
	// V2로 인코딩 (추가 필드 포함)
	v2Data, _ := result.Encode(2)
	fmt.Printf("Versioned GameResult V2: %d bytes\n", len(v2Data))
	
	fmt.Printf("\n버전 기반 처리의 장점:\n")
	fmt.Println("- 같은 프로토콜 ID를 사용하여 프로토콜 레지스트리 단순화")
	fmt.Println("- 버전 정보가 명시적으로 패킷에 포함되어 있음")
	fmt.Println("- 하위 호환성이 더 명확함")
}
```

프로토콜 버전 관리에서 중요한 원칙은 다음과 같다:

첫째, 기존 필드의 의미를 바꾸지 않는다. 기존 필드를 삭제하거나 순서를 바꾸면 호환성이 깨진다.

둘째, 새 필드는 항상 마지막에 추가한다. 이렇게 하면 기존 클라이언트는 새 필드를 무시하고 계속 작동한다.

셋째, 필드의 타입을 변경할 때는 새로운 프로토콜 ID를 사용한다. uint16에서 uint32로 확장하는 경우 기존 클라이언트와의 호환성이 깨진다.

넷째, 프로토콜 문서에 버전 정보를 명시하고 각 버전의 변경 사항을 기록한다. 이는 나중에 프로토콜을 수정할 때 참고할 수 있는 중요한 자산이다.

실제 게임 서버 운영에서는 주요 업데이트 시에 구 버전 클라이언트를 수용하는 기간을 정하고, 기한이 지난 후에는 지원을 중단한다. 이를 통해 서버 코드의 복잡성을 관리할 수 있다.

---

이제 Chapter 10은 완성되었다. 이 장에서는 바이너리 프로토콜 설계의 기초부터 실무적인 버전 관리까지 다루었다. 다음 장에서는 이러한 바이너리 데이터를 효율적으로 직렬화하고 역직렬화하는 방법을 자세히 다룰 것이다.  
   

# Chapter 11. 직렬화/역직렬화

게임 서버에서는 클라이언트와 주고받는 데이터를 구조화된 형태로 표현해야 한다. 직렬화(Serialization)는 메모리에 있는 객체를 바이트 스트림으로 변환하는 과정이고, 역직렬화(Deserialization)는 바이트 스트림을 다시 객체로 복원하는 과정이다. 게임 서버는 높은 처리량과 낮은 지연시간이 중요하므로 효율적인 직렬화 방식 선택이 매우 중요하다.

## 11.1 JSON 인코딩/디코딩

JSON은 사람이 읽을 수 있는 텍스트 형식으로 데이터를 표현한다. 개발과 디버깅이 쉽고 언어와 플랫폼에 관계없이 사용할 수 있다는 장점이 있다. Go의 표준 라이브러리 `encoding/json`은 구조체와 JSON 간의 자동 변환을 지원한다.

가장 간단한 JSON 직렬화는 `json.Marshal()` 함수를 사용하는 것이다.

```go
package main

import (
	"encoding/json"
	"fmt"
	"log"
)

// 플레이어 정보를 나타내는 구조체
type Player struct {
	ID       int    `json:"id"`
	Name     string `json:"name"`
	Level    int    `json:"level"`
	Gold     int    `json:"gold"`
	IsOnline bool   `json:"is_online"`
}

func basicJSONMarshal() {
	player := Player{
		ID:       12345,
		Name:     "Alice",
		Level:    42,
		Gold:     100000,
		IsOnline: true,
	}
	
	// 구조체를 JSON으로 변환한다
	// json.Marshal()은 바이트 슬라이스를 반환한다
	data, err := json.Marshal(player)
	if err != nil {
		log.Fatal(err)
	}
	
	// 바이트를 문자열로 변환하여 출력한다
	fmt.Println("Marshaled JSON:")
	fmt.Println(string(data))
	
	// 역변환: JSON을 다시 구조체로 변환한다
	var decodedPlayer Player
	err = json.Unmarshal(data, &decodedPlayer)
	if err != nil {
		log.Fatal(err)
	}
	
	fmt.Println("\nUnmarshaled Player:")
	fmt.Printf("ID: %d, Name: %s, Level: %d, Gold: %d, Online: %v\n",
		decodedPlayer.ID, decodedPlayer.Name, decodedPlayer.Level,
		decodedPlayer.Gold, decodedPlayer.IsOnline)
}

func main() {
	basicJSONMarshal()
}
```

위 코드에서 백틱(`)으로 감싼 부분을 구조체 태그(Tag)라고 부른다. `json:"id"`는 JSON으로 변환할 때 필드 이름을 "id"로 사용하라는 뜻이다. 태그가 없으면 필드의 원래 이름(첫 글자가 대문자)을 사용한다.

JSON 직렬화는 유연하지만 텍스트 형식이므로 데이터 크기가 크고 처리 속도가 상대적으로 느리다. 위 예제의 JSON 결과를 보면 필드 이름도 함께 저장되므로 같은 정보를 바이너리 형식보다 훨씬 많은 바이트로 저장한다.

게임 서버에서 실시간으로 많은 메시지를 주고받아야 하므로 JSON은 일반적으로 사용하지 않는다. 대신 설정 파일이나 클라이언트-서버 간의 초기 핸드셰이크(예: 로그인 요청) 같은 빈도가 낮은 통신에 사용된다.

더 나은 JSON 처리를 위해 `json.Encoder`와 `json.Decoder`를 사용할 수 있다. 이들은 스트림 기반 처리를 지원하므로 메모리 효율이 좋고 대용량 데이터 처리에 유리하다.

```go
package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"log"
)

type Player struct {
	ID       int    `json:"id"`
	Name     string `json:"name"`
	Level    int    `json:"level"`
	Gold     int    `json:"gold"`
}

// JSON을 파일이나 네트워크에 직접 쓸 때 사용한다
func streamingJSONEncoding() {
	players := []Player{
		{ID: 1, Name: "Alice", Level: 50, Gold: 100000},
		{ID: 2, Name: "Bob", Level: 45, Gold: 85000},
		{ID: 3, Name: "Charlie", Level: 48, Gold: 95000},
	}
	
	// 버퍼에 JSON을 쓴다
	buf := new(bytes.Buffer)
	encoder := json.NewEncoder(buf)
	
	// SetIndent()를 사용하면 들여쓰기가 추가된 보기 좋은 JSON이 된다
	// 첫 번째 인자는 각 줄의 앞 공백, 두 번째는 중첩 레벨마다 추가할 공백
	encoder.SetIndent("", "  ")
	
	for _, player := range players {
		if err := encoder.Encode(player); err != nil {
			log.Fatal(err)
		}
	}
	
	fmt.Println("Encoded Players:")
	fmt.Println(buf.String())
}

// JSON 문자열을 읽어서 디코딩할 때 사용한다
func streamingJSONDecoding() {
	jsonStr := `
	{"id":1,"name":"Alice","level":50,"gold":100000}
	{"id":2,"name":"Bob","level":45,"gold":85000}
	{"id":3,"name":"Charlie","level":48,"gold":95000}
	`
	
	// 문자열을 Reader로 변환한다
	decoder := json.NewDecoder(bytes.NewBufferString(jsonStr))
	
	fmt.Println("Decoded Players:")
	for decoder.More() {
		var player Player
		// Decode()는 다음 JSON 객체를 읽고 역직렬화한다
		if err := decoder.Decode(&player); err != nil {
			log.Fatal(err)
		}
		fmt.Printf("ID: %d, Name: %s, Level: %d\n", player.ID, player.Name, player.Level)
	}
}

func main() {
	streamingJSONEncoding()
	fmt.Println()
	streamingJSONDecoding()
}
```

## 11.2 구조체 태그 활용

구조체 태그는 필드의 직렬화 방식을 세밀하게 제어할 수 있는 방법이다. JSON뿐 아니라 다른 직렬화 형식에서도 태그를 사용할 수 있다.

```go
package main

import (
	"encoding/json"
	"fmt"
)

// 다양한 태그 옵션을 보여주는 예제
type GameRoom struct {
	// 기본 사용법: json 필드명 지정
	RoomID int `json:"room_id"`
	
	// omitempty: 필드 값이 빈 값(0, "", false, nil)이면 JSON에 포함시키지 않는다
	Password string `json:"password,omitempty"`
	
	// 필드가 항상 포함되도록 명시적으로 지정할 수도 있다
	// 단순히 필드명만 지정하면 된다
	Name string `json:"room_name"`
	
	// 대문자로 시작하지 않는 필드는 JSON에 포함되지 않는다 (내보내지 않음)
	internalState int
	
	// MaxPlayers는 JSON에서 "max_players"로 표현된다
	MaxPlayers int `json:"max_players"`
	
	// 대시(-)를 사용하면 이 필드는 JSON에 포함되지 않는다
	// 주로 임시 데이터나 계산 결과를 저장할 때 사용한다
	tempData string `json:"-"`
}

func demonstrateTagOptions() {
	// 비밀번호가 없는 방
	room1 := GameRoom{
		RoomID:     1,
		Name:       "Public Room",
		MaxPlayers: 6,
		tempData:   "this will not be included",
	}
	
	data1, _ := json.MarshalIndent(room1, "", "  ")
	fmt.Println("Room without password:")
	fmt.Println(string(data1))
	fmt.Println()
	
	// 비밀번호가 있는 방
	room2 := GameRoom{
		RoomID:     2,
		Password:   "secret123",
		Name:       "Private Room",
		MaxPlayers: 4,
	}
	
	data2, _ := json.MarshalIndent(room2, "", "  ")
	fmt.Println("Room with password:")
	fmt.Println(string(data2))
}

// 중첩된 구조체도 태그를 사용할 수 있다
type GameSession struct {
	SessionID string `json:"session_id"`
	Player    Player `json:"player"`
	Room      GameRoom `json:"room"`
}

type Player struct {
	ID   int    `json:"id"`
	Name string `json:"name"`
	// 게임 로직에서만 필요하고 클라이언트에는 보낼 필요 없는 필드
	InternalID string `json:"-"`
}

func demonstrateNestedStructs() {
	session := GameSession{
		SessionID: "sess_12345",
		Player: Player{
			ID:         1,
			Name:       "Alice",
			InternalID: "internal_12345",
		},
		Room: GameRoom{
			RoomID:     5,
			Name:       "Poker Room",
			MaxPlayers: 8,
		},
	}
	
	data, _ := json.MarshalIndent(session, "", "  ")
	fmt.Println("Game Session:")
	fmt.Println(string(data))
}

// 슬라이스와 맵도 JSON으로 변환할 수 있다
type RoomList struct {
	Rooms []GameRoom `json:"rooms"`
	// 맵도 JSON으로 변환되지만, 키는 문자열이어야 한다
	RoomCount map[string]int `json:"room_count"`
}

func demonstrateCollections() {
	roomList := RoomList{
		Rooms: []GameRoom{
			{RoomID: 1, Name: "Room 1", MaxPlayers: 6},
			{RoomID: 2, Name: "Room 2", MaxPlayers: 4},
		},
		RoomCount: map[string]int{
			"public":  10,
			"private": 5,
		},
	}
	
	data, _ := json.MarshalIndent(roomList, "", "  ")
	fmt.Println("Room List:")
	fmt.Println(string(data))
}

func main() {
	demonstrateTagOptions()
	fmt.Println()
	demonstrateNestedStructs()
	fmt.Println()
	demonstrateCollections()
}
```

커스텀 태그를 정의하여 다른 형식의 직렬화에도 사용할 수 있다. 이는 바이너리 형식이나 CSV 형식 같은 다양한 직렬화 방식을 같은 구조체에 적용할 때 유용하다.

```go
package main

import (
	"fmt"
	"reflect"
	"strconv"
	"strings"
)

// CSV 직렬화를 위한 커스텀 태그
type Player struct {
	ID    int    `csv:"id" json:"id"`
	Name  string `csv:"name" json:"name"`
	Level int    `csv:"level" json:"level"`
	Gold  int    `csv:"gold" json:"gold"`
}

// 커스텀 태그를 읽어서 CSV 형식으로 변환하는 함수
func toCSV(v interface{}) string {
	val := reflect.ValueOf(v)
	typ := reflect.TypeOf(v)
	
	var result []string
	
	for i := 0; i < val.NumField(); i++ {
		field := typ.Field(i)
		fieldVal := val.Field(i)
		
		// "csv" 태그를 확인한다
		if tag, ok := field.Tag.Lookup("csv"); ok && tag != "-" {
			result = append(result, fmt.Sprintf("%v", fieldVal.Interface()))
		}
	}
	
	return strings.Join(result, ",")
}

// 커스텀 태그 헤더를 생성하는 함수
func getCSVHeader(v interface{}) string {
	typ := reflect.TypeOf(v)
	
	var headers []string
	
	for i := 0; i < typ.NumField(); i++ {
		field := typ.Field(i)
		
		// "csv" 태그를 읽어서 헤더로 사용한다
		if tag, ok := field.Tag.Lookup("csv"); ok && tag != "-" {
			headers = append(headers, tag)
		}
	}
	
	return strings.Join(headers, ",")
}

func demonstrateCustomTags() {
	players := []Player{
		{ID: 1, Name: "Alice", Level: 50, Gold: 100000},
		{ID: 2, Name: "Bob", Level: 45, Gold: 85000},
		{ID: 3, Name: "Charlie", Level: 48, Gold: 95000},
	}
	
	fmt.Println(getCSVHeader(players[0]))
	for _, player := range players {
		fmt.Println(toCSV(player))
	}
}

func main() {
	demonstrateCustomTags()
}
```

## 11.3 커스텀 마샬러 구현

기본 JSON 인코딩이 원하는 형식을 제공하지 않을 때는 커스텀 마샬러를 구현할 수 있다. `json.Marshaler`와 `json.Unmarshaler` 인터페이스를 구현하면 된다.

```go
package main

import (
	"encoding/json"
	"fmt"
	"log"
	"time"
)

// 기본 시간 형식이 마음에 들지 않을 때 커스텀 마샬러를 사용한다
// 예를 들어, 게임에서는 타임스탬프를 특정 형식으로 보내야 할 수 있다

type GameEvent struct {
	EventID   int       `json:"event_id"`
	EventName string    `json:"event_name"`
	Timestamp CustomTime `json:"timestamp"`
}

// CustomTime은 특정 형식으로 시간을 직렬화한다
type CustomTime struct {
	time.Time
}

// MarshalJSON을 구현하면 JSON 인코딩 시 이 메서드가 호출된다
// 반환값은 JSON 형식의 바이트 슬라이스와 에러다
func (ct CustomTime) MarshalJSON() ([]byte, error) {
	// Unix 타임스탬프 형식으로 변환한다
	// 이는 게임 클라이언트가 해석하기 쉬운 숫자 형식이다
	timestamp := ct.Time.Unix()
	
	// JSON으로 변환한다
	return json.Marshal(timestamp)
}

// UnmarshalJSON을 구현하면 JSON 디코딩 시 이 메서드가 호출된다
func (ct *CustomTime) UnmarshalJSON(data []byte) error {
	var timestamp int64
	
	// JSON에서 int64를 추출한다
	if err := json.Unmarshal(data, &timestamp); err != nil {
		return err
	}
	
	// Unix 타임스탬프를 time.Time으로 변환한다
	ct.Time = time.Unix(timestamp, 0)
	return nil
}

func demonstrateCustomMarshaler() {
	event := GameEvent{
		EventID:   1001,
		EventName: "PlayerLogin",
		Timestamp: CustomTime{time.Now()},
	}
	
	data, err := json.MarshalIndent(event, "", "  ")
	if err != nil {
		log.Fatal(err)
	}
	
	fmt.Println("Event with custom timestamp:")
	fmt.Println(string(data))
	
	// 역직렬화 테스트
	jsonStr := `{
	  "event_id": 1002,
	  "event_name": "PlayerLogout",
	  "timestamp": 1704067200
	}`
	
	var decodedEvent GameEvent
	if err := json.Unmarshal([]byte(jsonStr), &decodedEvent); err != nil {
		log.Fatal(err)
	}
	
	fmt.Println("\nDecoded event:")
	fmt.Printf("Event ID: %d\n", decodedEvent.EventID)
	fmt.Printf("Event Name: %s\n", decodedEvent.EventName)
	fmt.Printf("Timestamp: %v\n", decodedEvent.Timestamp.Time)
}

// 더 복잡한 예: 게임 아이템을 직렬화할 때 타입에 따라 다르게 처리
type ItemType int

const (
	ItemSword ItemType = iota
	ItemShield
	ItemPotion
)

type GameItem struct {
	ID     int      `json:"id"`
	Type   ItemType `json:"type"`
	Name   string   `json:"name"`
	Rarity int      `json:"rarity"` // 1-5 등급
}

// MarshalJSON을 통해 아이템을 게임 클라이언트가 이해할 수 있는 형식으로 변환
func (item GameItem) MarshalJSON() ([]byte, error) {
	type Alias GameItem
	
	// 타입을 문자열로 변환한다
	typeStr := ""
	switch item.Type {
	case ItemSword:
		typeStr = "sword"
	case ItemShield:
		typeStr = "shield"
	case ItemPotion:
		typeStr = "potion"
	}
	
	// 익명 구조체를 사용하여 JSON 형식을 커스터마이징한다
	return json.Marshal(&struct {
		*Alias
		TypeName string `json:"type_name"`
		Quality  string `json:"quality"`
	}{
		Alias:    (*Alias)(&item),
		TypeName: typeStr,
		Quality:  getRarityName(item.Rarity),
	})
}

func getRarityName(rarity int) string {
	rarities := []string{"", "Common", "Uncommon", "Rare", "Epic", "Legendary"}
	if rarity >= 1 && rarity <= 5 {
		return rarities[rarity]
	}
	return "Unknown"
}

func demonstrateComplexMarshaler() {
	item := GameItem{
		ID:     1,
		Type:   ItemSword,
		Name:   "Excalibur",
		Rarity: 5,
	}
	
	data, _ := json.MarshalIndent(item, "", "  ")
	fmt.Println("Item with custom marshaler:")
	fmt.Println(string(data))
}

func main() {
	demonstrateCustomMarshaler()
	fmt.Println()
	demonstrateComplexMarshaler()
}
```

## 11.4 바이너리 직렬화 구현

게임 서버에서 높은 성능이 필요한 경우 바이너리 직렬화를 사용해야 한다. 이전 Chapter 10에서 배운 `encoding/binary`를 활용하여 직렬화 구조를 만들 수 있다.

```go
package main

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"io"
	"log"
)

// 바이너리 직렬화를 위한 인터페이스 정의
type BinaryMarshaler interface {
	MarshalBinary() ([]byte, error)
}

type BinaryUnmarshaler interface {
	UnmarshalBinary(data []byte) error
}

// 플레이어 정보를 바이너리로 직렬화한다
type Player struct {
	ID    uint32
	Name  string
	Level uint16
	Gold  uint32
}

// MarshalBinary를 구현하면 binary 패키지의 Write()에서 자동으로 사용된다
func (p Player) MarshalBinary() ([]byte, error) {
	buf := new(bytes.Buffer)
	
	// ID와 Level은 고정 길이 필드
	if err := binary.Write(buf, binary.LittleEndian, p.ID); err != nil {
		return nil, err
	}
	
	// 문자열은 길이-값 형식으로 저장한다
	// 먼저 이름의 길이를 uint16으로 저장
	nameLen := uint16(len(p.Name))
	if err := binary.Write(buf, binary.LittleEndian, nameLen); err != nil {
		return nil, err
	}
	
	// 이름 데이터 저장
	if _, err := buf.Write([]byte(p.Name)); err != nil {
		return nil, err
	}
	
	// Level과 Gold 저장
	if err := binary.Write(buf, binary.LittleEndian, p.Level); err != nil {
		return nil, err
	}
	if err := binary.Write(buf, binary.LittleEndian, p.Gold); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

// UnmarshalBinary를 구현하여 역직렬화한다
func (p *Player) UnmarshalBinary(data []byte) error {
	reader := bytes.NewReader(data)
	
	// ID 읽기
	if err := binary.Read(reader, binary.LittleEndian, &p.ID); err != nil {
		return err
	}
	
	// 이름 길이 읽기
	var nameLen uint16
	if err := binary.Read(reader, binary.LittleEndian, &nameLen); err != nil {
		return err
	}
	
	// 이름 데이터 읽기
	nameBytes := make([]byte, nameLen)
	if _, err := reader.Read(nameBytes); err != nil {
		return err
	}
	p.Name = string(nameBytes)
	
	// Level과 Gold 읽기
	if err := binary.Read(reader, binary.LittleEndian, &p.Level); err != nil {
		return err
	}
	if err := binary.Read(reader, binary.LittleEndian, &p.Gold); err != nil {
		return err
	}
	
	return nil
}

func demonstrateBasicBinaryMarshaling() {
	player := Player{
		ID:    12345,
		Name:  "Alice",
		Level: 42,
		Gold:  100000,
	}
	
	// MarshalBinary() 호출
	data, err := player.MarshalBinary()
	if err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Binary serialized size: %d bytes\n", len(data))
	fmt.Printf("Data: %v\n", data)
	
	// 역직렬화
	var decoded Player
	if err := decoded.UnmarshalBinary(data); err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("\nDeserialized:\n")
	fmt.Printf("ID: %d, Name: %s, Level: %d, Gold: %d\n",
		decoded.ID, decoded.Name, decoded.Level, decoded.Gold)
}

// 더 체계적인 접근: Reader/Writer 인터페이스 사용
type BetAction struct {
	PlayerID  uint32
	BetAmount uint32
	Action    uint8 // 1=Fold, 2=Check, 3=Call, 4=Raise
}

// 바이너리로 인코딩한다
func (ba *BetAction) EncodeTo(w io.Writer) error {
	return binary.Write(w, binary.LittleEndian, ba)
}

// 바이너리에서 디코딩한다
func (ba *BetAction) DecodeFrom(r io.Reader) error {
	return binary.Read(r, binary.LittleEndian, ba)
}

func demonstrateReaderWriterApproach() {
	action := BetAction{
		PlayerID:  42,
		BetAmount: 5000,
		Action:    4, // Raise
	}
	
	// Writer를 사용한 인코딩
	buf := new(bytes.Buffer)
	if err := action.EncodeTo(buf); err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Encoded BetAction: %v\n", buf.Bytes())
	
	// Reader를 사용한 디코딩
	decoded := &BetAction{}
	if err := decoded.DecodeFrom(bytes.NewReader(buf.Bytes())); err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Decoded: PlayerID=%d, BetAmount=%d, Action=%d\n",
		decoded.PlayerID, decoded.BetAmount, decoded.Action)
}

// 복합 구조체의 바이너리 직렬화
type GameResult struct {
	GameID    uint64
	WinnerID  uint32
	Prize     uint32
	Players   [4]uint32 // 최대 4명의 플레이어
	PlayerCount uint8
}

func (gr *GameResult) EncodeTo(w io.Writer) error {
	if err := binary.Write(w, binary.LittleEndian, gr.GameID); err != nil {
		return err
	}
	if err := binary.Write(w, binary.LittleEndian, gr.WinnerID); err != nil {
		return err
	}
	if err := binary.Write(w, binary.LittleEndian, gr.Prize); err != nil {
		return err
	}
	
	// 배열의 모든 요소를 저장한다
	if err := binary.Write(w, binary.LittleEndian, gr.Players); err != nil {
		return err
	}
	
	if err := binary.Write(w, binary.LittleEndian, gr.PlayerCount); err != nil {
		return err
	}
	
	return nil
}

func (gr *GameResult) DecodeFrom(r io.Reader) error {
	if err := binary.Read(r, binary.LittleEndian, &gr.GameID); err != nil {
		return err
	}
	if err := binary.Read(r, binary.LittleEndian, &gr.WinnerID); err != nil {
		return err
	}
	if err := binary.Read(r, binary.LittleEndian, &gr.Prize); err != nil {
		return err
	}
	if err := binary.Read(r, binary.LittleEndian, &gr.Players); err != nil {
		return err
	}
	if err := binary.Read(r, binary.LittleEndian, &gr.PlayerCount); err != nil {
		return err
	}
	
	return nil
}

func demonstrateComplexBinaryMarshaling() {
	result := GameResult{
		GameID:      1001,
		WinnerID:    42,
		Prize:       50000,
		Players:     [4]uint32{42, 15, 28, 67},
		PlayerCount: 4,
	}
	
	buf := new(bytes.Buffer)
	if err := result.EncodeTo(buf); err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Complex GameResult size: %d bytes\n", buf.Len())
	
	decoded := &GameResult{}
	if err := decoded.DecodeFrom(bytes.NewReader(buf.Bytes())); err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Decoded: GameID=%d, Winner=%d, Prize=%d\n",
		decoded.GameID, decoded.WinnerID, decoded.Prize)
}

func main() {
	fmt.Println("=== Basic Binary Marshaling ===")
	demonstrateBasicBinaryMarshaling()
	
	fmt.Println("\n=== Reader/Writer Approach ===")
	demonstrateReaderWriterApproach()
	
	fmt.Println("\n=== Complex Binary Marshaling ===")
	demonstrateComplexBinaryMarshaling()
}
```

## 11.5 성능 비교 및 최적화

게임 서버의 선택에 가장 큰 영향을 미치는 것은 성능이다. JSON과 바이너리 형식의 성능을 비교하고 최적화 방법을 살펴보자.

```go
package main

import (
	"bytes"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"testing"
	"time"
)

// 성능 비교를 위한 데이터 구조체
type BenchmarkPlayer struct {
	ID       uint32 `json:"id"`
	Name     string `json:"name"`
	Level    uint16 `json:"level"`
	Gold     uint32 `json:"gold"`
	IsOnline bool   `json:"is_online"`
}

// JSON 직렬화
func jsonMarshal(player BenchmarkPlayer) ([]byte, error) {
	return json.Marshal(player)
}

// JSON 역직렬화
func jsonUnmarshal(data []byte) (BenchmarkPlayer, error) {
	var player BenchmarkPlayer
	err := json.Unmarshal(data, &player)
	return player, err
}

// 바이너리 직렬화
func binaryMarshal(player BenchmarkPlayer) ([]byte, error) {
	buf := new(bytes.Buffer)
	
	if err := binary.Write(buf, binary.LittleEndian, player.ID); err != nil {
		return nil, err
	}
	
	nameLen := uint16(len(player.Name))
	if err := binary.Write(buf, binary.LittleEndian, nameLen); err != nil {
		return nil, err
	}
	
	if _, err := buf.Write([]byte(player.Name)); err != nil {
		return nil, err
	}
	
	if err := binary.Write(buf, binary.LittleEndian, player.Level); err != nil {
		return nil, err
	}
	
	if err := binary.Write(buf, binary.LittleEndian, player.Gold); err != nil {
		return nil, err
	}
	
	// bool은 1바이트로 저장
	isOnline := uint8(0)
	if player.IsOnline {
		isOnline = 1
	}
	if err := binary.Write(buf, binary.LittleEndian, isOnline); err != nil {
		return nil, err
	}
	
	return buf.Bytes(), nil
}

// 바이너리 역직렬화
func binaryUnmarshal(data []byte) (BenchmarkPlayer, error) {
	reader := bytes.NewReader(data)
	player := BenchmarkPlayer{}
	
	if err := binary.Read(reader, binary.LittleEndian, &player.ID); err != nil {
		return player, err
	}
	
	var nameLen uint16
	if err := binary.Read(reader, binary.LittleEndian, &nameLen); err != nil {
		return player, err
	}
	
	nameBytes := make([]byte, nameLen)
	if _, err := reader.Read(nameBytes); err != nil {
		return player, err
	}
	player.Name = string(nameBytes)
	
	if err := binary.Read(reader, binary.LittleEndian, &player.Level); err != nil {
		return player, err
	}
	
	if err := binary.Read(reader, binary.LittleEndian, &player.Gold); err != nil {
		return player, err
	}
	
	var isOnline uint8
	if err := binary.Read(reader, binary.LittleEndian, &isOnline); err != nil {
		return player, err
	}
	player.IsOnline = isOnline != 0
	
	return player, nil
}

// 성능 비교를 위한 벤치마크
func performanceComparison() {
	player := BenchmarkPlayer{
		ID:       12345,
		Name:     "AliceLongNameForTesting",
		Level:    42,
		Gold:     1000000,
		IsOnline: true,
	}
	
	// JSON 인코딩
	jsonData, _ := jsonMarshal(player)
	fmt.Printf("JSON size: %d bytes\n", len(jsonData))
	fmt.Printf("JSON data: %s\n", string(jsonData))
	
	// 바이너리 인코딩
	binData, _ := binaryMarshal(player)
	fmt.Printf("\nBinary size: %d bytes\n", len(binData))
	
	// 성능 테스트: 인코딩 속도
	numIterations := 100000
	
	// JSON 인코딩 성능
	startTime := time.Now()
	for i := 0; i < numIterations; i++ {
		jsonMarshal(player)
	}
	jsonEncodeTime := time.Since(startTime)
	
	// 바이너리 인코딩 성능
	startTime = time.Now()
	for i := 0; i < numIterations; i++ {
		binaryMarshal(player)
	}
	binEncodeTime := time.Since(startTime)
	
	// JSON 디코딩 성능
	startTime = time.Now()
	for i := 0; i < numIterations; i++ {
		jsonUnmarshal(jsonData)
	}
	jsonDecodeTime := time.Since(startTime)
	
	// 바이너리 디코딩 성능
	startTime = time.Now()
	for i := 0; i < numIterations; i++ {
		binaryUnmarshal(binData)
	}
	binDecodeTime := time.Since(startTime)
	
	fmt.Printf("\n=== Performance Comparison (%d iterations) ===\n", numIterations)
	fmt.Printf("JSON Encoding:  %v\n", jsonEncodeTime)
	fmt.Printf("Binary Encoding: %v (%.2fx faster)\n", binEncodeTime, 
		float64(jsonEncodeTime)/float64(binEncodeTime))
	
	fmt.Printf("JSON Decoding:  %v\n", jsonDecodeTime)
	fmt.Printf("Binary Decoding: %v (%.2fx faster)\n", binDecodeTime,
		float64(jsonDecodeTime)/float64(binDecodeTime))
	
	// 데이터 크기 효율성
	fmt.Printf("\n=== Data Size Efficiency ===\n")
	fmt.Printf("JSON: %d bytes\n", len(jsonData))
	fmt.Printf("Binary: %d bytes\n", len(binData))
	fmt.Printf("Binary is %.1f%% smaller\n", 
		100.0*(1.0-float64(len(binData))/float64(len(jsonData))))
}

// 최적화 기법: 버퍼 재사용
type OptimizedBinaryEncoder struct {
	buffer *bytes.Buffer
	data   []byte
}

func NewOptimizedBinaryEncoder() *OptimizedBinaryEncoder {
	return &OptimizedBinaryEncoder{
		buffer: new(bytes.Buffer),
		data:   make([]byte, 1024), // 사전 할당
	}
}

// 버퍼를 재사용하여 메모리 할당을 줄인다
func (obe *OptimizedBinaryEncoder) Encode(player BenchmarkPlayer) []byte {
	obe.buffer.Reset()
	
	binary.Write(obe.buffer, binary.LittleEndian, player.ID)
	
	nameLen := uint16(len(player.Name))
	binary.Write(obe.buffer, binary.LittleEndian, nameLen)
	obe.buffer.Write([]byte(player.Name))
	
	binary.Write(obe.buffer, binary.LittleEndian, player.Level)
	binary.Write(obe.buffer, binary.LittleEndian, player.Gold)
	
	isOnline := uint8(0)
	if player.IsOnline {
		isOnline = 1
	}
	binary.Write(obe.buffer, binary.LittleEndian, isOnline)
	
	// 결과를 복사하여 반환
	result := make([]byte, obe.buffer.Len())
	copy(result, obe.buffer.Bytes())
	return result
}

func demonstrateOptimization() {
	player := BenchmarkPlayer{
		ID:       12345,
		Name:     "TestPlayer",
		Level:    50,
		Gold:     500000,
		IsOnline: true,
	}
	
	numIterations := 100000
	
	// 일반적인 방식
	startTime := time.Now()
	for i := 0; i < numIterations; i++ {
		binaryMarshal(player)
	}
	normalTime := time.Since(startTime)
	
	// 최적화된 방식 (버퍼 재사용)
	encoder := NewOptimizedBinaryEncoder()
	startTime = time.Now()
	for i := 0; i < numIterations; i++ {
		encoder.Encode(player)
	}
	optimizedTime := time.Since(startTime)
	
	fmt.Printf("\n=== Buffer Reuse Optimization ===\n")
	fmt.Printf("Normal approach:     %v\n", normalTime)
	fmt.Printf("Optimized approach:  %v\n", optimizedTime)
	fmt.Printf("Improvement: %.2fx faster\n", float64(normalTime)/float64(optimizedTime))
}

func main() {
	performanceComparison()
	demonstrateOptimization()
}
```

실행 결과는 다음과 비슷할 것이다:

```
JSON size: 63 bytes
JSON data: {"id":12345,"name":"AliceLongNameForTesting","level":42,"gold":1000000,"is_online":true}

Binary size: 36 bytes

=== Performance Comparison (100000 iterations) ===
JSON Encoding:  2.5s
Binary Encoding: 0.3s (8.33x faster)
JSON Decoding:  3.2s
Binary Decoding: 0.4s (8.00x faster)

=== Data Size Efficiency ===
JSON: 63 bytes
Binary: 36 bytes
Binary is 42.9% smaller

=== Buffer Reuse Optimization ===
Normal approach:     250ms
Optimized approach:  180ms
Improvement: 1.39x faster
```

성능 비교에서 보이듯이 바이너리 직렬화는 JSON보다 훨씬 빠르고 데이터 크기도 작다. 게임 서버에서 실시간으로 많은 메시지를 처리해야 하므로 바이너리 직렬화를 사용하는 것이 필수다.

게임 서버에서 직렬화 성능을 최적화하기 위한 추가 팁은 다음과 같다:

첫째, 자주 사용되는 메시지는 고정 크기로 설계하여 동적 메모리 할당을 줄인다.

둘째, 버퍼를 미리 할당하고 재사용하여 가비지 컬렉션의 부담을 줄인다.

셋째, 큰 메시지는 청크 단위로 나누어 처리하여 메모리 사용량을 제한한다.

넷째, 핫패스(자주 실행되는 경로)에서는 json.Marshal 대신 커스텀 마샬러나 바이너리 직렬화를 사용한다.

다섯째, 프로파일링 도구를 사용하여 실제 병목 지점을 찾아 최적화한다.

---

이제 Chapter 11이 완성되었다. 이 장에서는 JSON과 바이너리 직렬화의 개념부터 실제 게임 서버에서 필요한 최적화 기법까지 다루었다. 직렬화는 게임 서버 개발의 핵심 부분이므로 이 내용을 충분히 이해하고 있어야 한다. 다음 장에서는 직렬화된 데이터를 효율적으로 읽고 쓰기 위한 버퍼링과 I/O 최적화를 다룰 것이다.



# Chapter 12. 버퍼링과 I/O

네트워크 통신에서 데이터를 읽고 쓸 때 직접 I/O 작업을 수행하면 매번 시스템 콜이 발생하여 성능이 저하된다. 버퍼링은 메모리에 데이터를 모았다가 한 번에 전송하거나 수신하는 기법으로, 게임 서버에서 높은 성능을 달성하기 위해 필수적이다. 이 장에서는 Go의 bufio 패키지를 활용한 효율적인 버퍼링 전략과 I/O 최적화를 다룬다.

## 12.1 bufio 패키지

bufio 패키지는 io.Reader와 io.Writer를 래핑하여 버퍼링된 읽기/쓰기를 제공한다. TCP 소켓 같은 일반 I/O 작업에 사용되면 효율성을 크게 향상시킨다.

```go
package main

import (
	"bufio"
	"bytes"
	"fmt"
	"log"
	"strings"
)

// bufio.Reader의 기본 사용법
func demonstrateBufferedReading() {
	// 테스트용 데이터: 여러 줄의 게임 패킷을 시뮬레이션한다
	data := `PLAYER_LOGIN:1001
MOVE:2000:100
ATTACK:2001:2000
CHAT:1002:Hello world
DISCONNECT:1001`
	
	// strings.NewReader는 문자열을 io.Reader로 변환한다
	// 실제로는 네트워크 연결(net.Conn)에서 읽게 된다
	reader := strings.NewReader(data)
	
	// bufio.Reader를 생성한다
	// 기본 버퍼 크기는 4096바이트이다
	bufferedReader := bufio.NewReader(reader)
	
	fmt.Println("Reading lines with bufio.Reader:")
	for {
		// ReadLine()은 개행 문자까지 한 줄을 읽는다
		// 반환값: 줄 데이터(개행 제외), 불완전 줄 여부, 에러
		line, isPrefix, err := bufferedReader.ReadLine()
		
		if err != nil {
			if err.Error() == "EOF" {
				break
			}
			log.Fatal(err)
		}
		
		// isPrefix가 true면 다음 ReadLine()과 연결되어야 한다는 뜻이다
		// 매우 긴 줄(버퍼보다 큰)을 처리할 때 발생한다
		fmt.Printf("Line: %s (Prefix: %v)\n", string(line), isPrefix)
	}
}

// 더 실용적인 텍스트 읽기: ReadString()
func demonstrateReadString() {
	data := "PlayerID:1001\nGold:50000\nLevel:42\n"
	reader := strings.NewReader(data)
	bufferedReader := bufio.NewReader(reader)
	
	fmt.Println("\nReading strings with ReadString:")
	for {
		// ReadString('\n')은 지정한 구분자('\n')까지 모든 문자를 읽는다
		// 구분자도 반환된 문자열에 포함된다
		line, err := bufferedReader.ReadString('\n')
		
		if err != nil {
			if err.Error() == "EOF" {
				// 파일 끝에 도달했지만 아직 읽을 데이터가 있을 수 있다
				if len(line) > 0 {
					fmt.Println("Final line: " + line)
				}
				break
			}
			log.Fatal(err)
		}
		
		// strings.TrimSpace()로 개행 문자를 제거한다
		fmt.Println("Data: " + strings.TrimSpace(line))
	}
}

// 버퍼에서 바이트 읽기: ReadBytes()
func demonstrateReadBytes() {
	data := "PACKET1|DATA:100|PACKET2|DATA:200|"
	reader := strings.NewReader(data)
	bufferedReader := bufio.NewReader(reader)
	
	fmt.Println("\nReading with ReadBytes:")
	for {
		// ReadBytes('|')는 구분자까지 모든 바이트를 읽는다
		// 바이너리 데이터에서 자주 사용된다
		packet, err := bufferedReader.ReadBytes('|')
		
		if err != nil {
			if err.Error() == "EOF" {
				if len(packet) > 0 {
					fmt.Printf("Final packet: %v\n", packet)
				}
				break
			}
			log.Fatal(err)
		}
		
		fmt.Printf("Packet: %v (length: %d)\n", packet, len(packet))
	}
}

// bufio.Writer의 기본 사용법
func demonstrateBufferedWriting() {
	// bytes.Buffer에 쓰기를 시뮬레이션한다
	// 실제로는 네트워크 연결에 쓰게 된다
	buf := new(bytes.Buffer)
	
	// bufio.Writer를 생성한다
	// 버퍼 크기는 기본 4096바이트다
	bufferedWriter := bufio.NewWriter(buf)
	
	fmt.Println("Writing with bufio.Writer:")
	
	// WriteString()으로 문자열을 쓴다
	// 실제로는 내부 버퍼에만 쓰여지고 아직 buf에는 전송되지 않는다
	bufferedWriter.WriteString("PLAYER_JOINED:1001\n")
	bufferedWriter.WriteString("GAME_STARTED\n")
	bufferedWriter.WriteString("MOVE_RECEIVED:1001:100:200\n")
	
	fmt.Printf("Buffer size before flush: %d bytes\n", buf.Len())
	fmt.Println("Buffer contents: (empty, data is in bufio.Writer)")
	
	// Flush()를 호출해야 bufio.Writer의 버퍼 데이터가 실제 Writer(buf)로 전송된다
	// 이는 실제 I/O를 수행하는 중요한 단계다
	bufferedWriter.Flush()
	
	fmt.Printf("Buffer size after flush: %d bytes\n", buf.Len())
	fmt.Printf("Buffer contents:\n%s\n", buf.String())
}

// 바이트 단위 쓰기: WriteByte(), WriteRune()
func demonstrateByteWriting() {
	buf := new(bytes.Buffer)
	bufferedWriter := bufio.NewWriter(buf)
	
	fmt.Println("\nWriting individual bytes:")
	
	// WriteByte()로 한 바이트씩 쓴다
	bufferedWriter.WriteByte('[')
	bufferedWriter.WriteString("PLAYER")
	bufferedWriter.WriteByte(']')
	
	// WriteRune()으로 문자를 쓴다
	bufferedWriter.WriteRune(':')
	bufferedWriter.WriteString("1001")
	
	bufferedWriter.Flush()
	fmt.Printf("Output: %s\n", buf.String())
}

func main() {
	demonstrateBufferedReading()
	demonstrateReadString()
	demonstrateReadBytes()
	demonstrateBufferedWriting()
	demonstrateByteWriting()
}
```

bufio 패키지의 핵심은 버퍼링을 통해 시스템 콜의 횟수를 줄인다는 것이다. 버퍼 없이 매번 읽기/쓰기를 하면 매우 느리지만, 버퍼에 데이터를 모은 후 한 번에 전송하면 훨씬 빠르다.

```
버퍼링이 없을 경우 vs 있을 경우

버퍼링 없음:
App -> Read() -> Kernel -> Network -> App (1000번 반복)
└─────────────┬─────────────┘
   매번 시스템 콜 발생

버퍼링 있음:
App -> bufio.Reader -> Buffer -> Network -> App
└─────┬─────┘
      버퍼에서 여러 바이트를 한 번에 읽음
      시스템 콜 횟수 대폭 감소
```

## 12.2 버퍼링 전략

게임 서버에서는 데이터의 특성과 사용 패턴에 따라 버퍼링 전략을 다르게 적용해야 한다.

```go
package main

import (
	"bufio"
	"bytes"
	"fmt"
	"io"
	"log"
	"net"
	"time"
)

// 전략 1: 고정 크기 버퍼 (작은 메시지가 자주 올 때)
type FixedSizeBufferedConnection struct {
	conn   net.Conn
	reader *bufio.Reader
	writer *bufio.Writer
	
	// 버퍼 크기를 작게 설정하여 메모리 사용을 줄인다
	// 게임 메시지는 보통 수백 바이트 이하다
	bufferSize int
}

func NewFixedSizeBufferedConnection(conn net.Conn, size int) *FixedSizeBufferedConnection {
	return &FixedSizeBufferedConnection{
		conn:       conn,
		reader:     bufio.NewReaderSize(conn, size),
		writer:     bufio.NewWriterSize(conn, size),
		bufferSize: size,
	}
}

// 고정 크기 버퍼는 메모리 효율이 좋지만, 버퍼를 자주 플러시해야 할 수 있다
func (fbc *FixedSizeBufferedConnection) SendMessage(msg string) error {
	_, err := fbc.writer.WriteString(msg)
	if err != nil {
		return err
	}
	// 매 메시지마다 플러시하면 버퍼링의 이점이 감소한다
	// 대신 작은 버퍼로 메모리 사용량을 줄인다
	return fbc.writer.Flush()
}

// 전략 2: 적응형 버퍼 (가변 크기 메시지)
type AdaptiveBufferedConnection struct {
	conn net.Conn
	
	// 읽기 버퍼는 큰 크기로 설정
	reader *bufio.Reader
	
	// 쓰기 버퍼도 충분히 큼
	writer *bufio.Writer
	
	// 플러시 카운터: 일정 횟수 후 강제 플러시
	writeCount int
	flushEvery int
}

func NewAdaptiveBufferedConnection(conn net.Conn) *AdaptiveBufferedConnection {
	return &AdaptiveBufferedConnection{
		conn:       conn,
		reader:     bufio.NewReaderSize(conn, 65536),  // 64KB 읽기 버퍼
		writer:     bufio.NewWriterSize(conn, 65536),  // 64KB 쓰기 버퍼
		flushEvery: 100, // 100개 메시지마다 플러시
	}
}

// 적응형 버퍼는 여러 메시지를 모아서 한 번에 플러시한다
func (abc *AdaptiveBufferedConnection) SendMessages(msgs []string) error {
	for _, msg := range msgs {
		if _, err := abc.writer.WriteString(msg); err != nil {
			return err
		}
		
		abc.writeCount++
		
		// 일정 개수에 도달했거나 명시적으로 플러시할 때까지 버퍼에만 유지한다
		if abc.writeCount >= abc.flushEvery {
			if err := abc.writer.Flush(); err != nil {
				return err
			}
			abc.writeCount = 0
		}
	}
	
	return nil
}

func (abc *AdaptiveBufferedConnection) Flush() error {
	return abc.writer.Flush()
}

// 전략 3: 라인 기반 버퍼링 (프로토콜이 라인 단위일 때)
type LineBasedBufferedConnection struct {
	reader *bufio.Reader
	writer *bufio.Writer
	conn   net.Conn
}

func NewLineBasedBufferedConnection(conn net.Conn) *LineBasedBufferedConnection {
	return &LineBasedBufferedConnection{
		reader: bufio.NewReader(conn),
		writer: bufio.NewWriter(conn),
		conn:   conn,
	}
}

// ReadPacket은 한 줄(패킷)을 읽는다
func (lbc *LineBasedBufferedConnection) ReadPacket() (string, error) {
	// ReadString('\n')은 개행까지 읽는다
	// 게임 프로토콜이 라인 기반일 때 효율적이다
	line, err := lbc.reader.ReadString('\n')
	if err != nil {
		return "", err
	}
	
	// 개행 문자 제거
	if len(line) > 0 && line[len(line)-1] == '\n' {
		line = line[:len(line)-1]
	}
	
	return line, nil
}

// WritePacket은 패킷을 쓰고 개행을 추가한다
func (lbc *LineBasedBufferedConnection) WritePacket(packet string) error {
	_, err := lbc.writer.WriteString(packet + "\n")
	return err
}

// 전략 4: 비동기 버퍼 (고성능이 필요할 때)
type AsyncBufferedConnection struct {
	reader    *bufio.Reader
	writer    *bufio.Writer
	conn      net.Conn
	writeChan chan string
	
	// 플러시 타이머: 일정 시간 후 자동으로 플러시
	flushTicker *time.Ticker
}

func NewAsyncBufferedConnection(conn net.Conn, flushInterval time.Duration) *AsyncBufferedConnection {
	abc := &AsyncBufferedConnection{
		reader:      bufio.NewReaderSize(conn, 65536),
		writer:      bufio.NewWriterSize(conn, 65536),
		conn:        conn,
		writeChan:   make(chan string, 1000), // 1000 메시지 버퍼
		flushTicker: time.NewTicker(flushInterval),
	}
	
	// 비동기 쓰기 고루틴 시작
	go abc.asyncWriteLoop()
	
	return abc
}

// 비동기 쓰기 루프: 채널에서 메시지를 받아 버퍼에 쓴다
func (abc *AsyncBufferedConnection) asyncWriteLoop() {
	for {
		select {
		case msg, ok := <-abc.writeChan:
			if !ok {
				// 채널이 닫혔으면 플러시하고 종료
				abc.writer.Flush()
				return
			}
			
			// 채널에서 받은 메시지를 버퍼에 쓴다
			abc.writer.WriteString(msg)
			
		case <-abc.flushTicker.C:
			// 주기적으로 플러시한다
			// 타임아웃이 발생하지 않도록 해주며 레이턴시를 제한한다
			abc.writer.Flush()
		}
	}
}

// 메시지를 비동기로 전송한다
func (abc *AsyncBufferedConnection) SendMessageAsync(msg string) error {
	select {
	case abc.writeChan <- msg:
		return nil
	default:
		return fmt.Errorf("write channel full")
	}
}

// 버퍼 전략 비교
func compareBufferingStrategies() {
	fmt.Println("=== Buffering Strategies Comparison ===\n")
	
	strategies := []string{
		"Fixed Size Buffer",
		"Adaptive Buffer",
		"Line-based Buffer",
		"Async Buffer",
	}
	
	characteristics := map[string][]string{
		"Fixed Size Buffer": {
			"메모리: 낮음 (작은 버퍼)",
			"처리량: 중간 (자주 플러시)",
			"레이턴시: 낮음",
			"용도: 소규모 게임, 메모리 제약",
		},
		"Adaptive Buffer": {
			"메모리: 중간 (큰 버퍼)",
			"처리량: 높음 (배치 처리)",
			"레이턴시: 중간",
			"용도: 일반적인 게임 서버",
		},
		"Line-based Buffer": {
			"메모리: 중간",
			"처리량: 높음 (라인 단위)",
			"레이턴시: 낮음",
			"용도: 텍스트 기반 프로토콜",
		},
		"Async Buffer": {
			"메모리: 높음 (채널 버퍼)",
			"처리량: 매우 높음 (비동기)",
			"레이턴시: 낮음",
			"용도: 고성능 서버",
		},
	}
	
	for _, strategy := range strategies {
		fmt.Printf("[%s]\n", strategy)
		for _, char := range characteristics[strategy] {
			fmt.Printf("  - %s\n", char)
		}
		fmt.Println()
	}
}

func main() {
	compareBufferingStrategies()
}
```

## 12.3 io.Reader와 io.Writer 인터페이스

Go의 I/O 시스템은 `io.Reader`와 `io.Writer` 인터페이스를 중심으로 설계되어 있다. 이 인터페이스를 이해하면 다양한 I/O 작업을 유연하게 처리할 수 있다.

```go
package main

import (
	"bytes"
	"fmt"
	"io"
	"strings"
)

// io.Reader 인터페이스는 단 하나의 메서드만 가지고 있다
// type Reader interface {
//     Read(p []byte) (n int, err error)
// }

// io.Reader를 구현하는 커스텀 타입
type GamePacketReader struct {
	data   string
	offset int
}

func NewGamePacketReader(data string) *GamePacketReader {
	return &GamePacketReader{data: data, offset: 0}
}

// Read() 메서드를 구현하여 io.Reader가 된다
func (gpr *GamePacketReader) Read(p []byte) (int, error) {
	// 모든 데이터를 읽었으면 EOF 반환
	if gpr.offset >= len(gpr.data) {
		return 0, io.EOF
	}
	
	// 최대한 p에 데이터를 복사한다
	n := copy(p, gpr.data[gpr.offset:])
	gpr.offset += n
	
	return n, nil
}

// io.Writer 인터페이스는 다음과 같다
// type Writer interface {
//     Write(p []byte) (n int, err error)
// }

// io.Writer를 구현하는 커스텀 타입
type GamePacketWriter struct {
	buffer bytes.Buffer
}

func NewGamePacketWriter() *GamePacketWriter {
	return &GamePacketWriter{}
}

// Write() 메서드를 구현하여 io.Writer가 된다
func (gpw *GamePacketWriter) Write(p []byte) (int, error) {
	return gpw.buffer.Write(p)
}

// 버퍼 내용을 반환한다
func (gpw *GamePacketWriter) String() string {
	return gpw.buffer.String()
}

// io.Reader와 io.Writer를 사용한 패킷 변환
func demonstrateReaderWriter() {
	fmt.Println("=== io.Reader and io.Writer ===\n")
	
	// GamePacketReader는 io.Reader다
	reader := NewGamePacketReader("PLAYER:1001|GOLD:50000|LEVEL:42")
	
	// io.Reader를 받는 함수들과 함께 사용할 수 있다
	buffer := make([]byte, 10)
	
	for {
		n, err := reader.Read(buffer)
		if err == io.EOF {
			break
		}
		
		if err != nil {
			panic(err)
		}
		
		fmt.Printf("Read %d bytes: %s\n", n, string(buffer[:n]))
	}
	
	fmt.Println()
}

// io.MultiReader: 여러 Reader를 순서대로 읽는다
func demonstrateMultiReader() {
	fmt.Println("=== io.MultiReader ===\n")
	
	reader1 := strings.NewReader("First packet: ")
	reader2 := strings.NewReader("Second packet")
	
	// 두 reader를 합쳐서 순서대로 읽는다
	multiReader := io.MultiReader(reader1, reader2)
	
	data := make([]byte, 100)
	n, _ := io.ReadFull(multiReader, data)
	
	fmt.Printf("Combined data: %s\n\n", string(data[:n]))
}

// io.MultiWriter: 여러 Writer에 동시에 쓴다
func demonstrateMultiWriter() {
	fmt.Println("=== io.MultiWriter ===\n")
	
	writer1 := new(bytes.Buffer)
	writer2 := new(bytes.Buffer)
	
	// 두 writer에 동시에 쓴다
	multiWriter := io.MultiWriter(writer1, writer2)
	
	data := []byte("Broadcast message to all writers")
	multiWriter.Write(data)
	
	fmt.Printf("Writer1: %s\n", writer1.String())
	fmt.Printf("Writer2: %s\n\n", writer2.String())
}

// io.Pipe: 두 고루틴 간의 동기 I/O 파이프
func demonstratePipe() {
	fmt.Println("=== io.Pipe ===\n")
	
	// PipeReader와 PipeWriter는 한 쌍을 이룬다
	reader, writer := io.Pipe()
	
	// 고루틴에서 데이터를 쓴다
	go func() {
		fmt.Fprint(writer, "First message\n")
		fmt.Fprint(writer, "Second message\n")
		writer.Close()
	}()
	
	// 메인 고루틴에서 데이터를 읽는다
	data := make([]byte, 100)
	n, _ := reader.Read(data)
	
	fmt.Printf("Piped data (first read): %s", string(data[:n]))
	
	n, _ = reader.Read(data)
	fmt.Printf("Piped data (second read): %s\n", string(data[:n]))
}

// io.LimitedReader: 읽을 수 있는 최대 바이트를 제한한다
func demonstrateLimitedReader() {
	fmt.Println("=== io.LimitedReader ===\n")
	
	source := strings.NewReader("This is a very long message that we want to limit")
	
	// 최대 20바이트만 읽도록 제한한다
	limitedReader := io.LimitReader(source, 20)
	
	data := make([]byte, 100)
	n, _ := io.ReadFull(limitedReader, data)
	
	fmt.Printf("Limited data (max 20 bytes): %s\n\n", string(data[:n]))
}

// io.TeeReader: 읽으면서 동시에 다른 Writer에 복사한다
func demonstrateTeeReader() {
	fmt.Println("=== io.TeeReader ===\n")
	
	source := strings.NewReader("Data being teed")
	logBuffer := new(bytes.Buffer)
	
	// 읽으면서 동시에 logBuffer에 복사한다
	// 이를 통해 로깅이나 감시 기능을 쉽게 추가할 수 있다
	teeReader := io.TeeReader(source, logBuffer)
	
	data := make([]byte, 100)
	n, _ := io.ReadFull(teeReader, data)
	
	fmt.Printf("Read data: %s\n", string(data[:n]))
	fmt.Printf("Logged data: %s\n\n", logBuffer.String())
}

// ReadCloser와 WriteCloser 인터페이스
// type ReadCloser interface {
//     Reader
//     Close() error
// }
// type WriteCloser interface {
//     Writer
//     Close() error
// }

type NetworkConnection struct {
	buffer bytes.Buffer
	closed bool
}

func NewNetworkConnection() *NetworkConnection {
	return &NetworkConnection{}
}

func (nc *NetworkConnection) Read(p []byte) (int, error) {
	if nc.closed {
		return 0, io.EOF
	}
	return nc.buffer.Read(p)
}

func (nc *NetworkConnection) Write(p []byte) (int, error) {
	if nc.closed {
		return 0, fmt.Errorf("connection closed")
	}
	return nc.buffer.Write(p)
}

func (nc *NetworkConnection) Close() error {
	nc.closed = true
	return nil
}

func demonstrateReadWriteCloser() {
	fmt.Println("=== ReadCloser and WriteCloser ===\n")
	
	conn := NewNetworkConnection()
	
	// WriteCloser로 사용
	data := []byte("Connection test data")
	conn.Write(data)
	
	// ReadCloser로 사용
	readData := make([]byte, 20)
	n, _ := conn.Read(readData)
	fmt.Printf("Data: %s\n\n", string(readData[:n]))
}

func main() {
	demonstrateReaderWriter()
	demonstrateMultiReader()
	demonstrateMultiWriter()
	demonstratePipe()
	demonstrateLimitedReader()
	demonstrateTeeReader()
	demonstrateReadWriteCloser()
}
```

## 12.4 효율적인 데이터 읽기/쓰기

게임 서버에서 네트워크로부터 데이터를 읽고 쓸 때 고려해야 할 점들이 있다. 특히 패킷 경계를 정확하게 처리하고 부분 읽기/쓰기를 올바르게 처리하는 것이 중요하다.

```go
package main

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"fmt"
	"io"
	"log"
)

// 게임 패킷 구조
type GamePacket struct {
	Length   uint32 // 패킷 전체 크기 (헤더 포함)
	PacketID uint16 // 패킷 ID
	Data     []byte // 페이로드
}

// 효율적인 패킷 읽기: 길이 기반
type PacketReader struct {
	reader *bufio.Reader
	buf    []byte // 재사용 가능한 버퍼
}

func NewPacketReader(r io.Reader) *PacketReader {
	return &PacketReader{
		reader: bufio.NewReaderSize(r, 65536),
		buf:    make([]byte, 1024), // 초기 크기
	}
}

// ReadPacket은 패킷 하나를 읽는다
// 패킷 구조: [Length:4][PacketID:2][Data:Length-6]
func (pr *PacketReader) ReadPacket() (*GamePacket, error) {
	// 먼저 헤더(6바이트)를 읽는다
	headerBuf := make([]byte, 6)
	if _, err := io.ReadFull(pr.reader, headerBuf); err != nil {
		return nil, err
	}
	
	// 길이와 ID를 파싱한다
	length := binary.LittleEndian.Uint32(headerBuf[0:4])
	packetID := binary.LittleEndian.Uint16(headerBuf[4:6])
	
	// 길이 검증
	if length < 6 {
		return nil, fmt.Errorf("invalid packet length: %d", length)
	}
	
	// 패이로드 길이
	payloadLen := length - 6
	
	// 버퍼 크기가 부족하면 확장한다
	if payloadLen > uint32(len(pr.buf)) {
		pr.buf = make([]byte, payloadLen)
	}
	
	// 페이로드를 읽는다
	// io.ReadFull은 정확히 요청한 바이트 수를 읽거나 에러를 반환한다
	// 이는 부분 읽기를 자동으로 처리해준다
	if _, err := io.ReadFull(pr.reader, pr.buf[:payloadLen]); err != nil {
		return nil, err
	}
	
	// 데이터를 복사하여 반환한다
	// 버퍼 재사용으로 인한 문제를 피하기 위해 복사한다
	data := make([]byte, payloadLen)
	copy(data, pr.buf[:payloadLen])
	
	return &GamePacket{
		Length:   length,
		PacketID: packetID,
		Data:     data,
	}, nil
}

// 효율적인 패킷 쓰기
type PacketWriter struct {
	writer *bufio.Writer
	buf    *bytes.Buffer // 임시 버퍼
}

func NewPacketWriter(w io.Writer) *PacketWriter {
	return &PacketWriter{
		writer: bufio.NewWriterSize(w, 65536),
		buf:    new(bytes.Buffer),
	}
}

// WritePacket은 패킷을 쓴다
func (pw *PacketWriter) WritePacket(packetID uint16, data []byte) error {
	// 임시 버퍼를 리셋한다
	pw.buf.Reset()
	
	// 패킷 길이를 계산한다: 헤더(6) + 데이터
	length := uint32(6 + len(data))
	
	// 길이를 쓴다
	if err := binary.Write(pw.buf, binary.LittleEndian, length); err != nil {
		return err
	}
	
	// 패킷 ID를 쓴다
	if err := binary.Write(pw.buf, binary.LittleEndian, packetID); err != nil {
		return err
	}
	
	// 데이터를 쓴다
	if _, err := pw.buf.Write(data); err != nil {
		return err
	}
	
	// 전체 패킷을 bufio.Writer에 쓴다
	_, err := pw.writer.Write(pw.buf.Bytes())
	return err
}

// Flush는 버퍼를 실제 Writer로 전송한다
func (pw *PacketWriter) Flush() error {
	return pw.writer.Flush()
}

// 부분 읽기/쓰기 처리 데모
func demonstratePartialIO() {
	fmt.Println("=== Partial I/O Handling ===\n")
	
	// 부분 읽기 시뮬레이션: 한 번에 몇 바이트만 읽을 수 있는 Reader
	type PartialReader struct {
		data      []byte
		offset    int
		readSize  int // 한 번에 읽을 바이트 수
	}
	
	pr := &PartialReader{
		data:     []byte("This is test data that will be read partially"),
		offset:   0,
		readSize: 5, // 한 번에 5바이트씩만 읽음
	}
	
	pr.Read = func(p []byte) (int, error) {
		if pr.offset >= len(pr.data) {
			return 0, io.EOF
		}
		
		// 최대 readSize 바이트만 읽는다
		n := len(p)
		if n > pr.readSize {
			n = pr.readSize
		}
		
		n = copy(p, pr.data[pr.offset:])
		pr.offset += n
		
		return n, nil
	}
	
	// io.ReadFull을 사용하면 부분 읽기를 자동으로 처리한다
	buf := make([]byte, 20)
	n, err := io.ReadFull(pr, buf)
	
	fmt.Printf("ReadFull with partial reader:\n")
	fmt.Printf("Requested: 20 bytes\n")
	fmt.Printf("Got: %d bytes\n", n)
	fmt.Printf("Data: %s\n\n", string(buf[:n]))
}

// 실제 패킷 읽기/쓰기 데모
func demonstratePacketIO() {
	fmt.Println("=== Packet I/O ===\n")
	
	// 메모리 버퍼에 패킷을 쓴다
	buf := new(bytes.Buffer)
	writer := NewPacketWriter(buf)
	
	// 여러 패킷을 쓴다
	packets := []struct {
		id   uint16
		data []byte
	}{
		{1001, []byte("LOGIN:player123")},
		{2001, []byte("MOVE:100:200:300")},
		{3001, []byte("ATTACK:enemy42")},
	}
	
	for _, p := range packets {
		if err := writer.WritePacket(p.id, p.data); err != nil {
			log.Fatal(err)
		}
	}
	
	if err := writer.Flush(); err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Written %d bytes\n", buf.Len())
	
	// 패킷을 읽는다
	reader := NewPacketReader(buf)
	
	fmt.Println("\nReading packets:")
	for {
		packet, err := reader.ReadPacket()
		if err == io.EOF {
			break
		}
		if err != nil {
			log.Fatal(err)
		}
		
		fmt.Printf("PacketID: %d, Data: %s\n", packet.PacketID, string(packet.Data))
	}
}

// Copy를 사용한 효율적인 대용량 데이터 전송
func demonstrateCopy() {
	fmt.Println("=== io.Copy for Efficient Data Transfer ===\n")
	
	// 소스와 목적지 생성
	source := bytes.NewBufferString("Large amount of data to be transferred between connections")
	destination := new(bytes.Buffer)
	
	// io.Copy는 버퍼링을 자동으로 처리하면서 한 스트림에서 다른 스트림으로 데이터를 복사한다
	// 이는 메모리 효율적이면서도 빠르다
	written, err := io.Copy(destination, source)
	
	if err != nil {
		log.Fatal(err)
	}
	
	fmt.Printf("Copied %d bytes\n", written)
	fmt.Printf("Destination: %s\n", destination.String())
}

func main() {
	demonstratePartialIO()
	demonstratePacketIO()
	demonstrateCopy()
}
```

## 12.5 메모리 풀을 이용한 최적화

게임 서버에서는 매초 많은 패킷을 처리하므로 메모리 할당과 해제가 빈번하게 발생한다. `sync.Pool`을 사용하여 메모리 할당을 재사용하면 가비지 컬렉션의 부담을 줄이고 성능을 향상시킬 수 있다.

```go
package main

import (
	"bytes"
	"encoding/binary"
	"fmt"
	"sync"
	"time"
)

// 메모리 풀을 사용하지 않은 일반적인 패킷 처리
type SimplePacketProcessor struct {
	processedCount int
}

func (spp *SimplePacketProcessor) ProcessPacket(data []byte) []byte {
	// 매번 새로운 버퍼를 할당한다
	buf := new(bytes.Buffer)
	
	// 헤더 쓰기
	binary.Write(buf, binary.LittleEndian, uint32(len(data)+6))
	binary.Write(buf, binary.LittleEndian, uint16(1001))
	
	// 데이터 쓰기
	buf.Write(data)
	
	spp.processedCount++
	
	return buf.Bytes()
}

// 메모리 풀을 사용한 최적화된 패킷 처리
type PooledPacketProcessor struct {
	bufferPool *sync.Pool
	processed  int64
	mu         sync.Mutex
}

func NewPooledPacketProcessor() *PooledPacketProcessor {
	return &PooledPacketProcessor{
		bufferPool: &sync.Pool{
			New: func() interface{} {
				// 새로운 버퍼가 필요할 때 생성한다
				// 대부분의 패킷이 1KB 이하이므로 처음부터 충분히 큰 크기로 할당한다
				return bytes.NewBuffer(make([]byte, 0, 2048))
			},
		},
	}
}

func (ppp *PooledPacketProcessor) ProcessPacket(data []byte) []byte {
	// 풀에서 버퍼를 가져온다
	buf := ppp.bufferPool.Get().(*bytes.Buffer)
	
	// 버퍼를 초기화한다
	buf.Reset()
	
	// 헤더 쓰기
	binary.Write(buf, binary.LittleEndian, uint32(len(data)+6))
	binary.Write(buf, binary.LittleEndian, uint16(1001))
	
	// 데이터 쓰기
	buf.Write(data)
	
	// 결과를 복사한다 (풀에 반환하기 전에)
	result := make([]byte, buf.Len())
	copy(result, buf.Bytes())
	
	// 버퍼를 풀에 반환한다
	ppp.bufferPool.Put(buf)
	
	ppp.mu.Lock()
	ppp.processed++
	ppp.mu.Unlock()
	
	return result
}

// 구조체 풀을 사용하는 예제
type PlayerAction struct {
	PlayerID  uint32
	ActionID  uint16
	Timestamp int64
	Data      []byte
}

type PlayerActionPool struct {
	pool *sync.Pool
}

func NewPlayerActionPool() *PlayerActionPool {
	return &PlayerActionPool{
		pool: &sync.Pool{
			New: func() interface{} {
				return &PlayerAction{
					Data: make([]byte, 0, 256), // 기본 크기 할당
				}
			},
		},
	}
}

// Get은 풀에서 구조체를 가져온다
func (pap *PlayerActionPool) Get() *PlayerAction {
	return pap.pool.Get().(*PlayerAction)
}

// Put은 구조체를 풀에 반환한다
func (pap *PlayerActionPool) Put(action *PlayerAction) {
	// 재사용을 위해 상태를 초기화한다
	action.PlayerID = 0
	action.ActionID = 0
	action.Timestamp = 0
	action.Data = action.Data[:0] // 슬라이스 재사용
	
	pap.pool.Put(action)
}

// 성능 비교를 위한 벤치마크 함수
func benchmarkPacketProcessing() {
	fmt.Println("=== Memory Pool Performance Comparison ===\n")
	
	// 테스트 데이터
	testData := []byte("PlayerID:1001|Gold:50000|Level:42|Equipment:Sword,Shield,Armor")
	numPackets := 100000
	
	// 1. 메모리 풀 없이 처리
	fmt.Printf("Processing %d packets without memory pool...\n", numPackets)
	simpleProcessor := &SimplePacketProcessor{}
	
	start := time.Now()
	for i := 0; i < numPackets; i++ {
		simpleProcessor.ProcessPacket(testData)
	}
	simpleTime := time.Since(start)
	
	fmt.Printf("Time: %v\n", simpleTime)
	
	// 2. 메모리 풀을 사용하여 처리
	fmt.Printf("\nProcessing %d packets with memory pool...\n", numPackets)
	pooledProcessor := NewPooledPacketProcessor()
	
	start = time.Now()
	for i := 0; i < numPackets; i++ {
		pooledProcessor.ProcessPacket(testData)
	}
	pooledTime := time.Since(start)
	
	fmt.Printf("Time: %v\n", pooledTime)
	
	fmt.Printf("\nImprovement: %.2fx faster\n", float64(simpleTime)/float64(pooledTime))
}

// 구조체 풀 사용 예제
func demonstrateStructPool() {
	fmt.Println("\n=== Struct Pool Usage ===\n")
	
	pool := NewPlayerActionPool()
	
	// 풀에서 구조체를 가져온다
	action := pool.Get()
	
	action.PlayerID = 123
	action.ActionID = 1001
	action.Timestamp = time.Now().Unix()
	action.Data = append(action.Data, []byte("Move to coordinates")...)
	
	fmt.Printf("Action - PlayerID: %d, ActionID: %d, Data: %s\n",
		action.PlayerID, action.ActionID, string(action.Data))
	
	// 사용을 마치고 풀에 반환한다
	pool.Put(action)
	
	// 다시 풀에서 가져오면 재사용 가능한 상태다
	action2 := pool.Get()
	fmt.Printf("Reused action - PlayerID: %d (should be 0)\n", action2.PlayerID)
	
	// 새로운 데이터로 사용한다
	action2.PlayerID = 456
	action2.ActionID = 2001
	action2.Data = append(action2.Data, []byte("Attack enemy")...)
	
	fmt.Printf("New action - PlayerID: %d, ActionID: %d, Data: %s\n",
		action2.PlayerID, action2.ActionID, string(action2.Data))
}

// 다중 풀 사용: 다양한 크기의 버퍼 풀
type BufferPoolManager struct {
	smallPool  *sync.Pool  // 256 바이트 이하
	mediumPool *sync.Pool  // 1KB 이하
	largePool  *sync.Pool  // 4KB 이하
}

func NewBufferPoolManager() *BufferPoolManager {
	return &BufferPoolManager{
		smallPool: &sync.Pool{
			New: func() interface{} {
				return bytes.NewBuffer(make([]byte, 0, 256))
			},
		},
		mediumPool: &sync.Pool{
			New: func() interface{} {
				return bytes.NewBuffer(make([]byte, 0, 1024))
			},
		},
		largePool: &sync.Pool{
			New: func() interface{} {
				return bytes.NewBuffer(make([]byte, 0, 4096))
			},
		},
	}
}

// GetBuffer는 필요한 크기에 맞는 버퍼를 반환한다
func (bpm *BufferPoolManager) GetBuffer(estimatedSize int) *bytes.Buffer {
	switch {
	case estimatedSize <= 256:
		return bpm.smallPool.Get().(*bytes.Buffer)
	case estimatedSize <= 1024:
		return bpm.mediumPool.Get().(*bytes.Buffer)
	default:
		return bpm.largePool.Get().(*bytes.Buffer)
	}
}

// PutBuffer는 버퍼를 적절한 풀에 반환한다
func (bpm *BufferPoolManager) PutBuffer(buf *bytes.Buffer, size int) {
	buf.Reset()
	switch {
	case size <= 256:
		bpm.smallPool.Put(buf)
	case size <= 1024:
		bpm.mediumPool.Put(buf)
	default:
		bpm.largePool.Put(buf)
	}
}

func demonstrateMultiPoolManager() {
	fmt.Println("\n=== Multi-tier Buffer Pool Manager ===\n")
	
	manager := NewBufferPoolManager()
	
	// 작은 버퍼 사용
	smallBuf := manager.GetBuffer(128)
	smallBuf.WriteString("Small packet data")
	fmt.Printf("Small buffer: %s\n", smallBuf.String())
	manager.PutBuffer(smallBuf, 128)
	
	// 중간 버퍼 사용
	mediumBuf := manager.GetBuffer(512)
	mediumBuf.WriteString("Medium sized packet with more data")
	fmt.Printf("Medium buffer: %s\n", mediumBuf.String())
	manager.PutBuffer(mediumBuf, 512)
	
	// 큰 버퍼 사용
	largeBuf := manager.GetBuffer(2048)
	largeBuf.WriteString("Large packet containing lots of game state data")
	fmt.Printf("Large buffer: %s\n", largeBuf.String())
	manager.PutBuffer(largeBuf, 2048)
}

func main() {
	benchmarkPacketProcessing()
	demonstrateStructPool()
	demonstrateMultiPoolManager()
}
```

메모리 풀을 사용할 때 주의할 점은 다음과 같다:

첫째, 풀에 반환하기 전에 반드시 데이터를 초기화하거나 복사해야 한다. 그렇지 않으면 다른 고루틴이 이전 데이터를 볼 수 있다.

둘째, 풀의 크기를 너무 크게 설정하면 메모리를 낭비할 수 있으므로 실제 사용 패턴에 맞게 조정해야 한다.

셋째, 동시 접근이 많을 때는 lock-free 구조나 여러 개의 작은 풀을 사용하는 것이 더 효율적일 수 있다.

---

이제 Chapter 12가 완성되었다. 이 장에서는 bufio 패키지를 통한 버퍼링의 기초부터 메모리 풀을 활용한 고급 최적화까지 다루었다. 게임 서버의 성능은 I/O 처리의 효율성에 크게 의존하므로 이 내용들을 충분히 이해하고 실제 프로젝트에 적용하는 것이 중요하다. 다음 장에서는 이러한 네트워크 기초 위에 게임 서버의 전체적인 설계 패턴을 다룰 것이다.  