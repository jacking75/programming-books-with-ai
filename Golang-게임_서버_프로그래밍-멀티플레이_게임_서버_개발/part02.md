# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 5. 고루틴 (Goroutine)

## 5.1 고루틴의 개념과 생성

고루틴은 Go 언어의 가장 강력한 기능 중 하나다. 고루틴은 매우 가벼운 스레드로, 수천 개 또는 수만 개를 동시에 실행할 수 있다. 전통적인 스레드는 생성 비용이 크고 메모리를 많이 사용하지만, 고루틴은 그렇지 않다.

고루틴을 생성하는 것은 매우 간단하다. 함수 호출 앞에 `go` 키워드를 붙이면 된다.

```go
package main

import (
	"fmt"
	"time"
)

func sayHello(name string) {
	fmt.Printf("안녕하세요, %s\n", name)
}

func main() {
	// 일반적인 함수 호출 - 동기적으로 실행된다
	sayHello("Alice")
	sayHello("Bob")

	// 고루틴으로 실행 - 비동기적으로 실행된다
	go sayHello("Charlie")
	go sayHello("Diana")

	// 메인 고루틴이 너무 빨리 종료되지 않도록 대기한다
	time.Sleep(1 * time.Second)
}
```

이 예제를 실행하면 처음 두 호출은 순서대로 실행되고, 다음 두 호출은 백그라운드에서 실행된다. `time.Sleep`을 사용한 이유는 메인 함수가 종료되면 실행 중인 모든 고루틴도 함께 종료되기 때문이다.

게임 서버에서 고루틴은 클라이언트 연결을 처리하는 데 사용된다. 각 클라이언트를 위해 하나의 고루틴을 만들고, 그 고루틴이 클라이언트와 통신하는 동안 다른 클라이언트를 처리하는 고루틴들도 독립적으로 실행된다.

```go
package main

import (
	"fmt"
	"time"
)

func handleClient(clientID int) {
	fmt.Printf("[%d] 클라이언트 연결됨\n", clientID)

	// 클라이언트 처리 작업을 시뮬레이션한다
	time.Sleep(2 * time.Second)

	fmt.Printf("[%d] 클라이언트 연결 종료\n", clientID)
}

func main() {
	// 5명의 클라이언트를 처리한다
	for i := 1; i <= 5; i++ {
		go handleClient(i)
	}

	// 모든 고루틴이 완료될 때까지 기다린다
	time.Sleep(3 * time.Second)
	fmt.Println("서버 종료")
}
```

위 예제는 문제가 있다. 각 클라이언트 처리가 2초 걸리지만, 메인 함수에서는 3초를 기다린다. 이 방식은 정확하지 않다. 더 좋은 방법은 다음 장에서 배울 채널을 사용하는 것이다.

고루틴은 메모리를 매우 적게 사용한다. 일반적인 OS 스레드는 약 1MB의 메모리를 사용하지만, 고루틴은 수 KB 정도만 사용한다. 따라서 많은 동시 연결을 처리할 수 있다.

```go
package main

import (
	"fmt"
)

func worker(id int, done chan bool) {
	fmt.Printf("워커 %d 시작\n", id)
	done <- true // 완료 신호를 보낸다
}

func main() {
	numWorkers := 1000
	done := make(chan bool, numWorkers)

	// 1000개의 고루틴을 생성한다
	for i := 1; i <= numWorkers; i++ {
		go worker(i, done)
	}

	// 모든 워커가 완료될 때까지 기다린다
	for i := 1; i <= numWorkers; i++ {
		<-done
	}

	fmt.Printf("%d개의 워커 모두 완료\n", numWorkers)
}
```

이 예제에서는 1000개의 고루틴을 생성했다. OS 스레드로 이렇게 많은 스레드를 생성하면 시스템이 응답하지 않을 것이다. 하지만 고루틴은 쉽게 생성할 수 있다.

## 5.2 고루틴 vs 스레드

고루틴과 OS 스레드의 차이를 이해하면 Go의 동시성 모델을 잘 이해할 수 있다.

OS 스레드는 운영 체제에 의해 관리된다. 각 스레드는 독립적인 스택을 가지고 있고, 운영 체제는 여러 스레드를 번갈아가며 실행한다. 스레드를 생성하고 전환하는 비용이 크다.

고루틴은 Go 런타임에 의해 관리된다. 고루틴은 더 가벼운 추상화이며, 여러 고루틴이 하나의 OS 스레드에서 실행될 수 있다. Go 런타임은 고루틴의 스케줄링을 담당한다.

```
┌─────────────────────────────────────────────────────────┐
│                   Go 프로그램                           │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ Goroutine 1 │  │ Goroutine 2 │  │ Goroutine 3 │ ... │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
│         │                 │                 │           │
│         └─────────────────┼─────────────────┘           │
│                           │                             │
│                  Go Scheduler                           │
│                           │                             │
│         ┌─────────────────┼─────────────────┐           │
│         │                 │                 │           │
│  ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐    │
│  │ OS Thread 1 │  │ OS Thread 2 │  │ OS Thread 3 │    │
│  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────┘
```

고루틴의 장점을 정리하면 다음과 같다. 첫째, 생성 비용이 매우 낮다. 둘째, 메모리 사용량이 적다. 셋째, 컨텍스트 스위칭이 빠르다. 넷째, 코드 작성이 간단하다.

```go
package main

import (
	"fmt"
	"runtime"
	"sync"
)

func main() {
	// 현재 활성 OS 스레드 개수 출력
	fmt.Printf("활성 OS 스레드: %d\n", runtime.NumGoroutine())

	var wg sync.WaitGroup
	numGoroutines := 100

	wg.Add(numGoroutines)

	for i := 1; i <= numGoroutines; i++ {
		go func(id int) {
			defer wg.Done()
			// 각 고루틴은 약간의 작업을 수행한다
			sum := 0
			for j := 0; j < 1000000; j++ {
				sum += j
			}
		}(i)
	}

	wg.Wait()
	fmt.Printf("완료된 고루틴: 100개\n")
}
```

위 예제에서는 `sync.WaitGroup`을 사용하여 모든 고루틴이 완료될 때까지 기다린다. 이는 5장의 마지막 부분에서 자세히 설명할 것이다.

## 5.3 고루틴 스케줄링 이해하기

Go 런타임은 모든 고루틴을 관리하고 스케줄링한다. Go 1.5 이후로는 GOMAXPROCS 값에 따라 여러 OS 스레드를 사용한다.

고루틴은 두 가지 상황에서 CPU를 양보한다. 첫 번째는 채널에서 데이터를 기다릴 때다. 두 번째는 뮤텍스를 기다릴 때다. 이외에도 명시적으로 `runtime.Gosched()`를 호출하여 CPU를 양보할 수 있다.

```go
package main

import (
	"fmt"
	"runtime"
)

func printNumbers(id int, count int) {
	for i := 1; i <= count; i++ {
		fmt.Printf("[고루틴 %d] 숫자 %d\n", id, i)

		// CPU를 다른 고루틴에게 양보한다
		if i%2 == 0 {
			runtime.Gosched()
		}
	}
}

func main() {
	go printNumbers(1, 5)
	go printNumbers(2, 5)

	// 모든 고루틴이 완료될 때까지 기다린다
	<-make(chan struct{})
}
```

다만 `runtime.Gosched()`는 거의 사용하지 않는다. 대신 채널이나 뮤텍스를 통해 자연스럽게 CPU를 양보하는 것이 더 좋다.

GOMAXPROCS는 동시에 실행될 수 있는 OS 스레드의 최대 개수를 설정한다. Go 1.5 이후로는 기본값이 CPU 코어 수와 같다.

```go
package main

import (
	"fmt"
	"runtime"
)

func main() {
	// CPU 코어 수 확인
	numCPU := runtime.NumCPU()
	fmt.Printf("CPU 코어 수: %d\n", numCPU)

	// GOMAXPROCS 값 확인 (기본값은 CPU 코어 수)
	gomaxprocs := runtime.GOMAXPROCS(-1)
	fmt.Printf("GOMAXPROCS: %d\n", gomaxprocs)

	// GOMAXPROCS 값을 변경할 수 있다
	runtime.GOMAXPROCS(4)
	fmt.Printf("변경된 GOMAXPROCS: %d\n", runtime.GOMAXPROCS(-1))
}
```

고루틴 스케줄링은 비선점형(non-preemptive)이다. 이는 고루틴이 명시적으로 CPU를 양보할 때까지 계속 실행된다는 의미다. 따라서 한 고루틴이 오래 실행되면 다른 고루틴이 기아 상태(starvation)에 빠질 수 있다.

```go
package main

import (
	"fmt"
	"time"
)

func busyWork(id int) {
	start := time.Now()
	// CPU를 많이 사용하는 작업
	sum := 0
	for i := 0; i < 2000000000; i++ {
		sum += i
	}
	elapsed := time.Since(start)
	fmt.Printf("[고루틴 %d] 완료 (소요 시간: %v)\n", id, elapsed)
}

func main() {
	go busyWork(1)
	go busyWork(2)

	time.Sleep(15 * time.Second)
}
```

이 예제에서는 두 고루틴이 CPU를 많이 사용하는 작업을 수행한다. 시스템에 여러 코어가 있으면 두 고루틴이 동시에 실행되고, 하나의 코어만 있으면 순서대로 실행된다.

## 5.4 고루틴 누수 방지하기

고루틴 누수는 게임 서버에서 심각한 문제가 될 수 있다. 고루틴 누수는 시간이 지나면서 메모리가 점점 증가하고, 결국 서버가 다운된다.

고루틴 누수의 가장 흔한 원인은 고루틴이 채널을 기다리다가 영원히 블로킹되는 경우다.

```go
package main

import (
	"fmt"
	"time"
)

func leakyGoroutine(id int) {
	ch := make(chan string)
	// 채널에서 데이터를 기다린다
	// 하지만 누군가 데이터를 보내지 않으면 영원히 대기한다
	msg := <-ch
	fmt.Printf("[%d] 메시지: %s\n", id, msg)
}

func main() {
	// 100개의 누수된 고루틴을 생성한다
	for i := 1; i <= 100; i++ {
		go leakyGoroutine(i)
	}

	// 고루틴이 완료되지 않으므로 이 줄에 도달하지 않는다
	time.Sleep(1 * time.Second)
	fmt.Println("프로그램 종료")
}
```

이 예제에서는 100개의 고루틴이 생성되지만, 누군가 채널에 데이터를 보내지 않으므로 모두 블로킹된다. 고루틴이 절대 종료되지 않으므로 메모리 누수가 발생한다.

고루틴 누수를 방지하는 방법은 여러 가지가 있다. 가장 간단한 방법은 타임아웃을 설정하는 것이다.

```go
package main

import (
	"fmt"
	"time"
)

func workerWithTimeout(id int, timeout time.Duration) {
	ch := make(chan string)

	go func() {
		// 실제 작업은 여기에 있다고 가정
		msg := <-ch
		fmt.Printf("[%d] 메시지: %s\n", id, msg)
	}()

	// 타임아웃을 기다린다
	select {
	case <-time.After(timeout):
		fmt.Printf("[%d] 타임아웃\n", id)
		// 타임아웃이 발생하면 고루틴은 결국 종료된다
	}
}

func main() {
	for i := 1; i <= 5; i++ {
		go workerWithTimeout(i, 1*time.Second)
	}

	time.Sleep(3 * time.Second)
	fmt.Println("프로그램 종료")
}
```

더 나은 방법은 고루틴이 종료되어야 할 때를 명시적으로 신호하는 것이다. 이는 컨텍스트(Context)를 사용하여 구현한다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func worker(ctx context.Context, id int) {
	for {
		select {
		case <-ctx.Done():
			fmt.Printf("[%d] 종료 신호를 받았습니다\n", id)
			return
		default:
			fmt.Printf("[%d] 작업 중...\n", id)
			time.Sleep(500 * time.Millisecond)
		}
	}
}

func main() {
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel()

	// 5개의 워커를 생성한다
	for i := 1; i <= 5; i++ {
		go worker(ctx, i)
	}

	// 타임아웃이 발생할 때까지 기다린다
	<-ctx.Done()
	fmt.Println("모든 워커가 종료되었습니다")
	time.Sleep(1 * time.Second)
}
```

위 예제에서 `context.WithTimeout`을 사용하여 2초 후에 자동으로 타임아웃되도록 설정했다. 각 워커는 `ctx.Done()` 채널을 확인하여 종료 신호를 받으면 반환한다.

게임 서버에서는 클라이언트 연결이 끊어지면 해당 고루틴이 반드시 종료되어야 한다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func handleClientConnection(ctx context.Context, clientID int, done chan<- bool) {
	defer func() {
		fmt.Printf("[클라이언트 %d] 연결 종료\n", clientID)
		done <- true
	}()

	for {
		select {
		case <-ctx.Done():
			fmt.Printf("[클라이언트 %d] 종료 신호를 받았습니다\n", clientID)
			return
		default:
			// 클라이언트 데이터 처리
			fmt.Printf("[클라이언트 %d] 데이터 처리 중\n", clientID)
			time.Sleep(500 * time.Millisecond)
		}
	}
}

func main() {
	numClients := 3
	done := make(chan bool, numClients)

	for i := 1; i <= numClients; i++ {
		// 각 클라이언트마다 독립적인 컨텍스트를 생성할 수 있다
		ctx, cancel := context.WithCancel(context.Background())

		go handleClientConnection(ctx, i, done)

		// 2초 후에 클라이언트 연결을 종료한다
		go func(c context.CancelFunc) {
			time.Sleep(2 * time.Second)
			c()
		}(cancel)
	}

	// 모든 클라이언트가 종료될 때까지 기다린다
	for i := 0; i < numClients; i++ {
		<-done
	}

	fmt.Println("서버 종료")
}
```

고루틴 누수를 피하는 일반적인 패턴을 정리하면 다음과 같다.

```
고루틴 시작할 때:
  ├─► 고루틴이 종료되는 조건을 명확히 한다
  ├─► Context를 사용하여 취소 신호를 전달한다
  └─► Defer를 사용하여 정리 작업을 보장한다

고루틴 대기할 때:
  ├─► 채널 닫기를 확인한다
  ├─► 타임아웃을 설정한다
  └─► Select를 사용하여 여러 신호를 처리한다
```

---

## Chapter 5 정리 다이어그램

고루틴의 생명주기를 시각화하면 다음과 같다.

```
┌──────────────────────────────────────────────────────────┐
│                   고루틴 생명주기                         │
└──────────────────────────────────────────────────────────┘

    생성
      │
      ▼
  ┌─────────────────┐
  │   실행 가능      │  ◄─── CPU 대기 중
  │  (Runnable)     │
  └────────┬────────┘
           │
           ▼
  ┌─────────────────┐
  │   실행 중        │
  │  (Running)      │
  └────────┬────────┘
           │
      ┌────┴──────────────────┐
      │                       │
      ▼                       ▼
  ┌─────────────┐      ┌──────────────────┐
  │  블로킹      │      │  CPU 양보        │
  │(채널, 뮤텍스)│      │(시간 슬라이스)   │
  └─────────────┘      └──────────────────┘
      │                       │
      │     실행 가능으로 전환  │
      └───────────────┬───────┘
                      │
                      ▼
           취소 신호 또는 반환
                      │
                      ▼
                  ┌─────────┐
                  │ 종료     │
                  │(Dead)   │
                  └─────────┘
```

고루틴과 OS 스레드의 비교를 요약하면 다음과 같다.

```
┌─────────────────────────────────────────────────────────┐
│              고루틴 vs OS 스레드                         │
├──────────────────┬──────────────┬──────────────────────┤
│   특성           │  고루틴       │   OS 스레드          │
├──────────────────┼──────────────┼──────────────────────┤
│ 생성 비용        │ 매우 낮음     │ 높음                 │
│ 메모리 사용      │ KB 수준       │ MB 수준              │
│ 개수 제한        │ 수백만 개     │ 수천 개              │
│ 관리             │ Go 런타임     │ 운영 체제             │
│ 스케줄링         │ M:N 모델      │ 1:1 모델             │
│ 컨텍스트 스위칭  │ 빠름          │ 느림                 │
└──────────────────┴──────────────┴──────────────────────┘
```

고루틴 누수 방지 체크리스트다.

```
고루틴 누수 확인 목록:
  □ 모든 고루틴에 종료 조건이 있는가?
  □ Context를 사용하여 취소 신호를 전달하는가?
  □ 채널 수신 시 타임아웃을 설정했는가?
  □ Defer를 사용하여 정리 작업을 수행하는가?
  □ 테스트에서 고루틴 개수를 확인했는가?
```

---

이제 고루틴의 기본을 이해했다. 다음 장에서는 고루틴 간의 통신을 위한 채널(Channel)에 대해 배운다. 채널은 고루틴을 연결하고 안전하게 데이터를 주고받는 메커니즘이다.

   
# Chapter 6. 채널 (Channel)

## 6.1 채널의 기본 사용법

채널은 고루틴 간의 안전한 통신을 위한 Go의 핵심 메커니즘이다. 채널을 통해 고루틴들은 데이터를 주고받을 수 있으며, Go 런타임은 동시성 문제를 자동으로 처리한다.

채널은 `make` 함수로 생성한다. 채널을 생성할 때는 채널이 전달할 데이터의 타입을 지정해야 한다.

```go
package main

import (
	"fmt"
)

func main() {
	// 정수를 전달하는 채널 생성
	ch := make(chan int)

	// 채널로 데이터를 보내는 고루틴
	go func() {
		ch <- 42 // 42를 채널에 보낸다
		fmt.Println("데이터를 보냈습니다")
	}()

	// 채널에서 데이터를 받는다
	value := <-ch
	fmt.Printf("받은 데이터: %d\n", value)
}
```

이 예제에서는 간단한 채널 통신을 보여준다. 화살표 방향이 중요하다. `ch <- value`는 채널에 데이터를 보내고, `value := <-ch`는 채널에서 데이터를 받는다.

채널 통신은 기본적으로 블로킹된다. 데이터를 받는 쪽에서 기다리고 있으면, 데이터를 보내는 쪽에서 데이터를 보낼 때까지 기다린다. 반대로 데이터를 보내는 쪽에서 대기 중이면, 받는 쪽에서 데이터를 받을 때까지 기다린다.

```go
package main

import (
	"fmt"
	"time"
)

func sendData(ch chan string) {
	fmt.Println("데이터를 보내기 전...")
	time.Sleep(2 * time.Second)
	ch <- "Hello"
	fmt.Println("데이터를 보냈습니다")
}

func main() {
	ch := make(chan string)

	go sendData(ch)

	fmt.Println("데이터를 받기 위해 대기 중...")
	message := <-ch
	fmt.Printf("받은 메시지: %s\n", message)
}
```

이 예제에서는 수신자가 먼저 대기하고, 송신자가 2초 후에 데이터를 보낸다. 메인 함수는 메시지를 받을 때까지 블로킹된다.

게임 서버에서는 채널을 사용하여 클라이언트로부터 받은 데이터를 처리하거나, 게임 상태 변화를 다른 고루틴에 알린다.

```go
package main

import (
	"fmt"
	"time"
)

type PlayerAction struct {
	PlayerID int
	Action   string
}

func gameLogic(actions chan PlayerAction) {
	for {
		action := <-actions
		fmt.Printf("플레이어 %d의 액션: %s\n", action.PlayerID, action.Action)
	}
}

func main() {
	actions := make(chan PlayerAction)

	// 게임 로직을 실행하는 고루틴
	go gameLogic(actions)

	// 플레이어 액션을 시뮬레이션한다
	actions <- PlayerAction{PlayerID: 1, Action: "fold"}
	time.Sleep(100 * time.Millisecond)

	actions <- PlayerAction{PlayerID: 2, Action: "raise"}
	time.Sleep(100 * time.Millisecond)

	actions <- PlayerAction{PlayerID: 3, Action: "call"}
	time.Sleep(100 * time.Millisecond)
}
```

위 예제에서 `gameLogic` 고루틴은 무한 루프에서 채널로부터 플레이어 액션을 받아 처리한다. 메인 함수는 액션을 채널에 보낸다.

## 6.2 버퍼드 채널 vs 언버퍼드 채널

채널에는 두 가지 종류가 있다. 언버퍼드 채널은 한 번에 하나의 데이터만 보관할 수 있고, 버퍼드 채널은 여러 개의 데이터를 보관할 수 있다.

언버퍼드 채널에서는 데이터를 보내는 고루틴과 받는 고루틴이 동시에 준비되어야 한다. 한쪽이 준비되지 않으면 다른 쪽이 블로킹된다.

```go
package main

import (
	"fmt"
)

func main() {
	// 언버퍼드 채널
	ch := make(chan int)

	// 송신 고루틴
	go func() {
		for i := 1; i <= 3; i++ {
			fmt.Printf("송신: %d\n", i)
			ch <- i
		}
	}()

	// 수신
	for i := 1; i <= 3; i++ {
		fmt.Printf("수신: %d\n", <-ch)
	}
}
```

이 예제에서는 송신과 수신이 엄격하게 동기화된다. 각 데이터를 보낸 후에 받을 때까지 대기한다.

버퍼드 채널은 지정된 크기만큼의 버퍼를 가진다. 버퍼가 가득 차기 전까지는 송신자가 블로킹되지 않는다.

```go
package main

import (
	"fmt"
)

func main() {
	// 크기 3인 버퍼드 채널
	ch := make(chan int, 3)

	// 버퍼가 가득 찰 때까지 송신자는 블로킹되지 않는다
	ch <- 1
	ch <- 2
	ch <- 3

	fmt.Println("3개의 데이터를 모두 보냈습니다")

	// 이제 수신한다
	fmt.Printf("수신: %d\n", <-ch)
	fmt.Printf("수신: %d\n", <-ch)
	fmt.Printf("수신: %d\n", <-ch)
}
```

버퍼드 채널은 생산자와 소비자의 속도 차이를 완충할 수 있다. 예를 들어 빠른 생산자와 느린 소비자가 있을 때, 생산자가 버퍼에 데이터를 저장하고 계속 진행할 수 있다.

```go
package main

import (
	"fmt"
	"time"
)

func producer(ch chan int) {
	for i := 1; i <= 5; i++ {
		fmt.Printf("생산: %d\n", i)
		ch <- i
	}
}

func consumer(ch chan int) {
	for {
		value := <-ch
		fmt.Printf("소비: %d (처리 중...)\n", value)
		time.Sleep(1 * time.Second)
	}
}

func main() {
	// 버퍼 크기 2로 생산자와 소비자 간의 속도 차이를 완충한다
	ch := make(chan int, 2)

	go producer(ch)
	go consumer(ch)

	time.Sleep(10 * time.Second)
}
```

이 예제에서 생산자는 5개의 데이터를 빠르게 보내고, 소비자는 각 데이터를 1초씩 처리한다. 버퍼 크기가 2이므로 생산자가 2개까지 미리 보낼 수 있다.

게임 서버에서는 버퍼드 채널을 사용하여 네트워크 메시지를 처리한다. 네트워크 I/O는 느릴 수 있으므로, 게임 로직이 메시지를 빠르게 처리하고 버퍼에 저장할 수 있다.

```go
package main

import (
	"fmt"
	"time"
)

type Message struct {
	ClientID int
	Data     string
}

func networkHandler(messages chan Message) {
	// 네트워크에서 메시지를 받는 것을 시뮬레이션한다
	for i := 1; i <= 5; i++ {
		fmt.Printf("네트워크에서 메시지 수신: %d\n", i)
		messages <- Message{ClientID: i, Data: fmt.Sprintf("message %d", i)}
		time.Sleep(100 * time.Millisecond)
	}
}

func gameLogic(messages chan Message) {
	for {
		msg := <-messages
		fmt.Printf("게임 로직 처리: 클라이언트 %d의 메시지 처리 중\n", msg.ClientID)
		time.Sleep(500 * time.Millisecond)
	}
}

func main() {
	// 버퍼 크기 5로 네트워크 메시지를 버퍼링한다
	messages := make(chan Message, 5)

	go networkHandler(messages)
	go gameLogic(messages)

	time.Sleep(5 * time.Second)
}
```

## 6.3 채널의 방향성

채널은 송신 전용 또는 수신 전용으로 선언할 수 있다. 이는 함수의 의도를 명확하게 하고, 실수를 방지한다.

```go
package main

import (
	"fmt"
)

// 송신 전용 채널
func send(ch chan<- int) {
	ch <- 42
	// <-ch // 컴파일 에러: 수신할 수 없다
}

// 수신 전용 채널
func receive(ch <-chan int) {
	value := <-ch
	fmt.Println(value)
	// ch <- 100 // 컴파일 에러: 송신할 수 없다
}

// 양방향 채널
func bidirectional(ch chan int) {
	ch <- 10
	value := <-ch
	fmt.Println(value)
}

func main() {
	ch := make(chan int)

	go send(ch)
	receive(ch)
}
```

송신 전용 채널은 `chan<-` 형태이고, 수신 전용 채널은 `<-chan` 형태다. 양방향 채널은 `chan` 형태다.

일반적으로 함수를 호출할 때 양방향 채널을 전달하면, Go는 자동으로 필요한 방향의 채널로 변환한다.

```go
package main

import (
	"fmt"
)

func worker(id int, jobs <-chan int, results chan<- string) {
	for job := range jobs {
		fmt.Printf("워커 %d가 작업 %d를 처리 중\n", id, job)
		results <- fmt.Sprintf("워커 %d이 작업 %d를 완료했습니다", id, job)
	}
}

func main() {
	jobs := make(chan int, 5)
	results := make(chan string)

	// 2개의 워커를 시작한다
	go worker(1, jobs, results)
	go worker(2, jobs, results)

	// 작업을 채널에 보낸다
	for i := 1; i <= 5; i++ {
		jobs <- i
	}

	// 결과를 받는다
	for i := 1; i <= 5; i++ {
		fmt.Println(<-results)
	}
}
```

이 예제에서 `worker` 함수는 `jobs` 채널에서 작업을 받고 (수신 전용), `results` 채널에 결과를 보낸다 (송신 전용). 이렇게 하면 함수의 의도가 명확하고, 실수로 잘못 사용하는 것을 방지할 수 있다.

## 6.4 select 문을 이용한 다중 채널 처리

`select` 문은 여러 채널을 동시에 처리할 수 있게 해준다. 준비된 채널 중 하나를 선택하여 실행한다.

```go
package main

import (
	"fmt"
	"time"
)

func main() {
	ch1 := make(chan string)
	ch2 := make(chan string)

	go func() {
		time.Sleep(1 * time.Second)
		ch1 <- "결과 1"
	}()

	go func() {
		time.Sleep(2 * time.Second)
		ch2 <- "결과 2"
	}()

	// 먼저 준비된 채널을 선택한다
	select {
	case msg1 := <-ch1:
		fmt.Println("ch1에서 받음:", msg1)
	case msg2 := <-ch2:
		fmt.Println("ch2에서 받음:", msg2)
	}

	// 다른 채널의 결과도 받는다
	select {
	case msg1 := <-ch1:
		fmt.Println("ch1에서 받음:", msg1)
	case msg2 := <-ch2:
		fmt.Println("ch2에서 받음:", msg2)
	}
}
```

`select` 문은 어느 채널이 먼저 준비되었는지에 따라 해당 케이스를 실행한다. 여러 채널이 동시에 준비되면 무작위로 하나를 선택한다.

`select` 문은 게임 서버에서 여러 이벤트를 처리할 때 매우 유용하다.

```go
package main

import (
	"fmt"
	"time"
)

type PlayerMessage struct {
	PlayerID int
	Message  string
}

func main() {
	playerMessages := make(chan PlayerMessage)
	timerTick := make(chan time.Time)
	shutdown := make(chan bool)

	// 타이머를 시작한다
	ticker := time.NewTicker(1 * time.Second)
	go func() {
		for t := range ticker.C {
			timerTick <- t
		}
	}()

	// 플레이어 메시지를 시뮬레이션한다
	go func() {
		for i := 1; i <= 3; i++ {
			time.Sleep(500 * time.Millisecond)
			playerMessages <- PlayerMessage{PlayerID: i, Message: fmt.Sprintf("action %d", i)}
		}
	}()

	// 3초 후에 종료한다
	go func() {
		time.Sleep(3 * time.Second)
		shutdown <- true
	}()

	// 여러 이벤트를 처리한다
	for {
		select {
		case msg := <-playerMessages:
			fmt.Printf("플레이어 %d: %s\n", msg.PlayerID, msg.Message)
		case <-timerTick:
			fmt.Println("게임 로직 실행 (1초 간격)")
		case <-shutdown:
			fmt.Println("게임 종료")
			ticker.Stop()
			return
		}
	}
}
```

이 예제에서는 플레이어 메시지, 타이머, 종료 신호를 동시에 처리한다. `select` 문이 준비된 채널 중 하나를 선택하여 처리한다.

`default` 케이스를 사용하면 어느 채널도 준비되지 않았을 때 실행할 코드를 지정할 수 있다.

```go
package main

import (
	"fmt"
	"time"
)

func main() {
	ch := make(chan int)

	go func() {
		time.Sleep(2 * time.Second)
		ch <- 42
	}()

	// 폴링 구현
	for {
		select {
		case value := <-ch:
			fmt.Println("데이터 수신:", value)
			return
		default:
			fmt.Println("데이터를 기다리는 중...")
			time.Sleep(500 * time.Millisecond)
		}
	}
}
```

다만 `default` 케이스를 사용하면 채널이 준비될 때까지 계속 루프가 실행되므로 CPU를 많이 사용한다. 위의 예제에서는 `time.Sleep`으로 조절했지만, 더 효율적인 방법은 `default` 없이 채널이 준비될 때까지 대기하는 것이다.

## 6.5 채널 닫기와 range

채널을 닫으려면 `close` 함수를 사용한다. 채널을 닫은 후에 데이터를 받을 수 있지만, 보낼 수는 없다.

```go
package main

import (
	"fmt"
)

func main() {
	ch := make(chan int)

	go func() {
		for i := 1; i <= 3; i++ {
			ch <- i
		}
		close(ch) // 채널을 닫는다
	}()

	// 채널에서 데이터를 받는다
	for value := range ch {
		fmt.Println(value)
	}

	fmt.Println("채널이 닫혔습니다")
}
```

`range`를 사용하면 채널이 닫힐 때까지 데이터를 받을 수 있다. 채널이 닫혀서 더 이상 데이터가 없으면 루프가 종료된다.

채널을 받는 쪽에서 닫혀 있는지 확인할 수 있다.

```go
package main

import (
	"fmt"
)

func main() {
	ch := make(chan int)

	go func() {
		ch <- 1
		ch <- 2
		close(ch)
	}()

	// 두 가지 방법으로 닫혀 있는지 확인한다

	// 방법 1: ok 값으로 확인
	if value, ok := <-ch; ok {
		fmt.Println("받은 값:", value)
	}

	// 방법 2: range 사용 (더 권장)
	for value := range ch {
		fmt.Println("받은 값:", value)
	}
}
```

중요한 점은 송신자만 채널을 닫을 수 있다는 것이다. 수신자가 채널을 닫으려고 하면 패닉이 발생한다.

```go
package main

import (
	"fmt"
)

func main() {
	ch := make(chan int)

	go func() {
		<-ch
		close(ch) // 패닉: send on closed channel
	}()

	ch <- 1
}
```

게임 서버에서는 채널을 사용하여 작업을 분배하고 완료를 알린다.

```go
package main

import (
	"fmt"
	"sync"
)

func worker(id int, jobs <-chan int, wg *sync.WaitGroup) {
	defer wg.Done()

	for job := range jobs {
		fmt.Printf("워커 %d가 작업 %d를 처리합니다\n", id, job)
	}
	fmt.Printf("워커 %d가 종료됩니다\n", id)
}

func main() {
	jobs := make(chan int, 5)
	var wg sync.WaitGroup

	// 3개의 워커를 시작한다
	numWorkers := 3
	for i := 1; i <= numWorkers; i++ {
		wg.Add(1)
		go worker(i, jobs, &wg)
	}

	// 작업을 분배한다
	for i := 1; i <= 10; i++ {
		jobs <- i
	}

	// 더 이상 작업이 없음을 알린다
	close(jobs)

	// 모든 워커가 완료될 때까지 대기한다
	wg.Wait()
	fmt.Println("모든 작업이 완료되었습니다")
}
```

이 예제에서는 `jobs` 채널을 통해 작업을 분배하고, 채널을 닫아서 더 이상 작업이 없음을 알린다. 각 워커는 `range`를 사용하여 채널에서 작업을 받고, 채널이 닫혀서 더 이상 작업이 없으면 자동으로 루프를 종료한다.

---

## Chapter 6 정리 다이어그램

채널의 기본 동작을 시각화하면 다음과 같다.

```
언버퍼드 채널 (Unbuffered Channel)
    ┌────────────────┐
    │ 송신자 고루틴  │
    └────────┬───────┘
             │
             │ ch <- data
             │ (블로킹)
             │
         ┌───▼────┐
         │ 채널   │
         │(버퍼 없음)
         └───┬────┘
             │
             │ <-ch
             │ (해제)
             │
    ┌────────▼───────┐
    │ 수신자 고루틴  │
    └────────────────┘

버퍼드 채널 (Buffered Channel with size N)
    ┌────────────────┐
    │ 송신자 고루틴  │
    └────────┬───────┘
             │
             │ ch <- data
             │ (버퍼가 비어있으면 계속)
             │
    ┌────────▼──────────────────────┐
    │ 채널 (버퍼 크기: N)          │
    │ ┌────┬────┬────┬──┐          │
    │ │ 1  │ 2  │ 3  │..│          │
    │ └────┴────┴────┴──┘          │
    └────────┬──────────────────────┘
             │
             │ <-ch
             │
    ┌────────▼───────┐
    │ 수신자 고루틴  │
    └────────────────┘
```

채널 방향성의 요약이다.

```
┌────────────────────────────────────────────────────┐
│            채널 방향성 (Channel Direction)         │
├────────────────┬──────────┬───────────────────────┤
│  선언 형태     │ 송신     │ 수신                   │
├────────────────┼──────────┼───────────────────────┤
│ chan T         │ ✓        │ ✓                      │
│ chan<- T       │ ✓        │ ✗                      │
│ <-chan T       │ ✗        │ ✓                      │
└────────────────┴──────────┴───────────────────────┘
```

select 문의 동작 흐름이다.

```
┌─────────────────────────────────────┐
│     select 문 실행 시작             │
└────────────────┬────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
    ┌───▼────┐      ┌────▼────┐
    │ case 1 │      │ case 2   │
    └───┬────┘      └────┬─────┘
        │                │
    ┌───▼────┐      ┌────▼────┐
    │ 준비됨 │      │ 준비됨   │
    └────────┘      └─────────┘
        │                │
        └────────┬───────┘
                 │
           ┌─────▼──────┐
           │ 무작위 선택 │
           │ (동시에 준비│
           │  되면)     │
           └─────┬──────┘
                 │
         ┌───────▼────────┐
         │ 선택된 case    │
         │ 실행           │
         └────────────────┘

준비된 채널이 없으면:
  ├─► default가 있으면 → default 실행
  └─► default가 없으면 → 채널이 준비될 때까지 대기
```

채널 닫기와 수신의 패턴이다.

```
┌──────────────────────────────────────────────┐
│      채널의 생명주기                         │
├──────────────────────────────────────────────┤
│                                              │
│  생성: ch := make(chan Type)                │
│         │                                    │
│         ▼                                    │
│  송신/수신 중                                 │
│         │                                    │
│         ├─► 송신: ch <- data (OK)           │
│         ├─► 수신: <-ch (OK)                 │
│         │                                    │
│         ▼                                    │
│  닫기: close(ch) [송신자만 가능]            │
│         │                                    │
│         ├─► 송신: ch <- data (패닉)         │
│         ├─► 수신: <-ch (OK, 0 값)          │
│         ├─► range: for v := range ch       │
│         │                 (정상 종료)       │
│         │                                    │
│         ▼                                    │
│  종료                                        │
│                                              │
└──────────────────────────────────────────────┘
```

게임 서버에서의 채널 활용 패턴이다.

```
클라이언트 연결
    │
    ▼
┌─────────────────────────────────────┐
│   Session Handler (고루틴)          │
│   ├─ 네트워크 메시지 수신           │
│   └─ playerMessages 채널에 전송    │
└────────────┬────────────────────────┘
             │
             │ playerMessages
             │
             ▼
┌─────────────────────────────────────┐
│   Game Logic (고루틴)               │
│   ├─ playerMessages에서 수신        │
│   ├─ 게임 상태 업데이트             │
│   └─ 결과를 broadcastResults로 전송│
└────────────┬────────────────────────┘
             │
             │ broadcastResults
             │
             ▼
┌─────────────────────────────────────┐
│   Broadcasting Handler (고루틴)      │
│   ├─ 결과를 모든 클라이언트에 전송  │
│   └─ 네트워크로 데이터 전송         │
└─────────────────────────────────────┘
```

---

## 채널 사용 규칙 정리

채널을 안전하고 올바르게 사용하기 위한 규칙을 정리한다.

```
채널 사용 규칙:
  
  1. 송신 규칙
     ├─► 한 고루틴만 송신해야 한다 (권장)
     ├─► 채널이 닫혀 있으면 송신할 수 없다 (패닉)
     └─► 송신 후 바로 결과를 기대하면 안 된다

  2. 수신 규칙
     ├─► 여러 고루틴이 동시에 수신할 수 있다
     ├─► 닫힌 채널에서 수신하면 0 값을 받는다
     └─► range를 사용하면 자동으로 종료된다

  3. 닫기 규칙
     ├─► 송신자만 닫을 수 있다
     ├─► 닫은 후 송신하면 패닉이 발생한다
     └─► 이미 닫힌 채널을 다시 닫으면 패닉이 발생한다

  4. 일반적인 패턴
     ├─► 많은 송신자, 한 수신자 → sync.Mutex
     ├─► 한 송신자, 많은 수신자 → 채널 사용 가능
     ├─► 양쪽이 많으면 → 채널 방향성 명확히 하기
     └─► 종료 신호 → Context 또는 별도 채널 사용
```

이제 채널의 모든 기본을 이해했다. 다음 장에서는 여러 고루틴을 안전하게 동기화하는 방법인 동기화 패턴을 배운다. 뮤텍스, WaitGroup, Once, Pool 등의 도구를 사용하여 고루틴 간의 공유 자원을 보호한다.



# Chapter 7. 동기화 패턴

게임 서버에서 여러 고루틴이 동시에 실행되면서 공유 자원에 접근할 때 발생하는 경쟁 조건(race condition)을 방지하는 것은 매우 중요하다. 이 장에서는 Go에서 제공하는 동기화 메커니즘들을 살펴보고, 게임 서버의 특성에 맞는 동기화 패턴을 학습한다.

## 7.1 sync.Mutex와 sync.RWMutex

### Mutex의 개념과 필요성

Mutex(뮤튜얼 익스클루전, Mutual Exclusion)는 여러 고루틴이 공유 자원에 동시에 접근하는 것을 방지하는 잠금 메커니즘이다. Go에서 제공하는 `sync.Mutex`는 이진 잠금으로, 한 번에 하나의 고루틴만이 보호된 영역에 진입할 수 있도록 한다.

게임 서버의 경우를 생각해보자. 여러 클라이언트가 동시에 플레이어의 골드(gold) 정보를 변경하려고 할 때, 동기화 없이 진행하면 어떻게 될까?

```
클라이언트 A: 현재 골드 = 100
클라이언트 B: 현재 골드 = 100

[동시 실행]
A가 골드 50을 더함: 골드 = 100 + 50 = 150 (메모리에 쓰기 전)
B가 골드 30을 뺌: 골드 = 100 - 30 = 70 (메모리에 쓰기 전)

최종 결과:
A의 연산 결과 150이 먼저 저장됨 → 골드 = 150
B의 연산 결과 70이 나중에 저장됨 → 골드 = 70 (잘못된 결과!)

올바른 결과는 100 + 50 - 30 = 120이어야 함
```

이러한 문제를 해결하기 위해 Mutex를 사용한다.

### Mutex 사용 예제

다음은 플레이어의 인벤토리를 관리하는 간단한 예제이다.

```go
package main

import (
	"fmt"
	"sync"
)

type Inventory struct {
	mu    sync.Mutex
	items map[string]int
}

func (inv *Inventory) AddItem(itemName string, quantity int) {
	inv.mu.Lock()
	defer inv.mu.Unlock()

	// 잠금 상태에서만 items 맵을 수정한다.
	if _, exists := inv.items[itemName]; !exists {
		inv.items[itemName] = 0
	}
	inv.items[itemName] += quantity
	fmt.Printf("%s를 %d개 추가했습니다. 총 개수: %d\n", 
		itemName, quantity, inv.items[itemName])
}

func (inv *Inventory) RemoveItem(itemName string, quantity int) bool {
	inv.mu.Lock()
	defer inv.mu.Unlock()

	if count, exists := inv.items[itemName]; exists && count >= quantity {
		inv.items[itemName] -= quantity
		fmt.Printf("%s를 %d개 제거했습니다. 남은 개수: %d\n", 
			itemName, quantity, inv.items[itemName])
		return true
	}
	fmt.Printf("%s는 %d개를 제거할 수 없습니다.\n", itemName, quantity)
	return false
}

func (inv *Inventory) GetItemCount(itemName string) int {
	inv.mu.Lock()
	defer inv.mu.Unlock()

	return inv.items[itemName]
}

func main() {
	inv := &Inventory{
		items: make(map[string]int),
	}

	var wg sync.WaitGroup

	// 10개의 고루틴이 동시에 아이템을 추가한다.
	for i := 0; i < 10; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			inv.AddItem("sword", 1)
		}(i)
	}

	wg.Wait()
	fmt.Printf("최종 sword 개수: %d\n", inv.GetItemCount("sword"))
}
```

이 코드에서 중요한 점들을 설명한다.

`Lock()` 메서드는 뮤텍스를 획득하려고 시도한다. 이미 다른 고루틴이 잠금을 가지고 있다면, 현재 고루틴은 대기한다. `defer inv.mu.Unlock()`은 함수 종료 시점에 반드시 잠금을 해제하도록 보장한다. 이는 중간에 함수가 반환되거나 패닉이 발생하더라도 데드락을 방지한다.

### RWMutex를 사용한 최적화

Mutex는 강력한 보호 메커니즘이지만, 읽기 작업이 많은 상황에서는 성능 저하가 발생한다. 예를 들어, 많은 클라이언트가 플레이어의 정보를 읽기만 하는데, 모두 동일한 잠금을 대기해야 한다.

이러한 상황에 대응하기 위해 `sync.RWMutex`(읽기-쓰기 뮤텍스)를 사용한다. RWMutex는 동시에 여러 고루틴이 읽기를 수행하도록 허용하지만, 쓰기는 여전히 독점적으로 처리한다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

type PlayerStats struct {
	mu    sync.RWMutex
	level int
	exp   int
	hp    int
}

func (ps *PlayerStats) GetLevel() int {
	ps.mu.RLock()
	defer ps.mu.RUnlock()
	return ps.level
}

func (ps *PlayerStats) GetExp() int {
	ps.mu.RLock()
	defer ps.mu.RUnlock()
	return ps.exp
}

func (ps *PlayerStats) GetHP() int {
	ps.mu.RLock()
	defer ps.mu.RUnlock()
	return ps.hp
}

func (ps *PlayerStats) AddExp(amount int) {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	ps.exp += amount
	if ps.exp >= 100 {
		ps.level++
		ps.exp = 0
		fmt.Printf("레벨 업! 현재 레벨: %d\n", ps.level)
	}
}

func (ps *PlayerStats) TakeDamage(damage int) {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	ps.hp -= damage
	if ps.hp < 0 {
		ps.hp = 0
	}
	fmt.Printf("데미지: %d, 남은 HP: %d\n", damage, ps.hp)
}

func main() {
	stats := &PlayerStats{
		level: 1,
		exp:   0,
		hp:    100,
	}

	var wg sync.WaitGroup

	// 5개의 고루틴이 경험치를 추가한다 (쓰기 작업)
	for i := 0; i < 5; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 10; j++ {
				stats.AddExp(20)
				time.Sleep(10 * time.Millisecond)
			}
		}(i)
	}

	// 10개의 고루틴이 플레이어 정보를 읽는다 (읽기 작업)
	for i := 0; i < 10; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 20; j++ {
				level := stats.GetLevel()
				exp := stats.GetExp()
				hp := stats.GetHP()
				fmt.Printf("[고루틴 %d] 레벨: %d, 경험치: %d, HP: %d\n", 
					id, level, exp, hp)
				time.Sleep(5 * time.Millisecond)
			}
		}(i)
	}

	wg.Wait()
	fmt.Printf("최종 상태 - 레벨: %d, 경험치: %d, HP: %d\n", 
		stats.level, stats.exp, stats.hp)
}
```

`RLock()`과 `RUnlock()`은 읽기 잠금(read lock)을 획득하고 해제한다. 여러 고루틴이 동시에 읽기 잠금을 가질 수 있으므로, 읽기만 빈번한 상황에서 훨씬 나은 성능을 제공한다. 반면 쓰기가 필요하면 `Lock()`을 사용하여 다른 모든 접근을 차단한다.

### Mutex와 RWMutex 선택 기준

Mutex를 사용해야 하는 경우는 읽기와 쓰기의 비율이 비슷하거나 쓰기 작업이 많을 때이다. RWMutex는 읽기가 쓰기보다 훨씬 빈번할 때(일반적으로 10:1 이상) 유리하다. 게임 서버에서는 플레이어 정보 조회(읽기)가 매우 빈번하므로, 자주 조회되는 데이터는 RWMutex로 보호하는 것이 좋다.

## 7.2 sync.WaitGroup

### WaitGroup의 개념

`sync.WaitGroup`은 여러 고루틴이 모두 완료될 때까지 대기하는 메커니즘을 제공한다. 고루틴의 완료를 추적할 때 매우 유용하며, 게임 서버에서는 초기화 단계나 종료 단계에서 자주 사용된다.

WaitGroup은 내부적으로 카운터를 유지한다. `Add(n)`으로 카운터를 증가시키고, `Done()`으로 감소시킨다. `Wait()`는 카운터가 0이 될 때까지 대기한다.

### WaitGroup 사용 예제

다음은 게임 서버 시작 시 여러 모듈을 초기화하는 예제이다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

type GameModule interface {
	Initialize() error
	GetName() string
}

type DatabaseModule struct{}

func (d *DatabaseModule) Initialize() error {
	fmt.Println("데이터베이스 모듈 초기화 중...")
	time.Sleep(1 * time.Second)
	fmt.Println("데이터베이스 모듈 초기화 완료")
	return nil
}

func (d *DatabaseModule) GetName() string {
	return "Database"
}

type NetworkModule struct{}

func (n *NetworkModule) Initialize() error {
	fmt.Println("네트워크 모듈 초기화 중...")
	time.Sleep(800 * time.Millisecond)
	fmt.Println("네트워크 모듈 초기화 완료")
	return nil
}

func (n *NetworkModule) GetName() string {
	return "Network"
}

type GameLogicModule struct{}

func (g *GameLogicModule) Initialize() error {
	fmt.Println("게임 로직 모듈 초기화 중...")
	time.Sleep(600 * time.Millisecond)
	fmt.Println("게임 로직 모듈 초기화 완료")
	return nil
}

func (g *GameLogicModule) GetName() string {
	return "GameLogic"
}

func main() {
	modules := []GameModule{
		&DatabaseModule{},
		&NetworkModule{},
		&GameLogicModule{},
	}

	var wg sync.WaitGroup
	errors := make(chan error, len(modules))

	// 각 모듈을 초기화한다
	for _, module := range modules {
		wg.Add(1) // 카운터를 1씩 증가시킨다

		go func(m GameModule) {
			defer wg.Done() // 함수 종료 시 카운터를 1씩 감소시킨다

			if err := m.Initialize(); err != nil {
				errors <- fmt.Errorf("%s 모듈 초기화 실패: %w", m.GetName(), err)
			}
		}(module)
	}

	// 모든 모듈이 초기화될 때까지 대기한다
	wg.Wait()

	// 에러 채널을 닫는다 (어떤 고루틴도 더 이상 에러를 보내지 않으므로)
	close(errors)

	// 에러가 있었는지 확인한다
	hasError := false
	for err := range errors {
		fmt.Printf("에러: %v\n", err)
		hasError = true
	}

	if !hasError {
		fmt.Println("\n모든 모듈 초기화 완료! 게임 서버 시작.")
	}
}
```

이 예제에서 WaitGroup의 사용 패턴을 보자. 각 모듈을 초기화하는 고루틴을 시작하기 전에 `wg.Add(1)`로 카운터를 증가시킨다. 그리고 고루틴 시작 시 `defer wg.Done()`으로 완료를 보장한다. 마지막으로 `wg.Wait()`에서 모든 고루틴이 완료될 때까지 메인 고루틴이 대기한다.

### WaitGroup과 Error 처리

게임 서버에서는 초기화 과정에서 에러가 발생할 수 있다. 위 예제는 각 고루틴의 에러를 채널을 통해 수집한다. `make(chan error, len(modules))`로 버퍼드 채널을 만들어, 모든 고루틴이 `wg.Wait()`를 실행하지 않아도 에러를 보낼 수 있도록 한다.

### WaitGroup의 주의사항

WaitGroup을 사용할 때 주의할 점이 있다. `Add()`의 호출 시점과 고루틴 시작 시점의 순서가 중요하다. 만약 고루틴을 시작한 후에 `Add()`를 호출하면, `Wait()`가 먼저 카운터가 0이 되어 반환될 수 있다. 또한 `Add()`에 음수를 전달하거나, `Done()`을 `Add()`의 횟수보다 많이 호출하면 패닉이 발생한다.

```go
// 잘못된 사용 예
var wg sync.WaitGroup

go func() {
	// 고루틴이 시작된 후에
	wg.Add(1) // Add() 호출 - 위험!
	defer wg.Done()
	// ...
}()

wg.Wait() // 고루틴이 Add()를 호출하기 전에 반환될 수 있음
```

따라서 항상 고루틴을 시작하기 전에 `Add()`를 호출해야 한다.

## 7.3 sync.Once

### Once의 개념

`sync.Once`는 정확히 한 번만 실행되어야 하는 코드를 보호하는 메커니즘이다. 싱글톤 패턴이나 초기화 코드가 여러 번 실행되지 않도록 보장할 때 사용한다.

Once는 Do() 메서드를 제공하며, 같은 Once 객체에 대해 여러 번 `Do()`를 호출해도 전달된 함수는 정확히 한 번만 실행된다.

### Once 사용 예제

게임 서버에서 데이터베이스 연결이 싱글톤이어야 한다고 가정하자.

```go
package main

import (
	"fmt"
	"sync"
)

type DatabaseConnection struct {
	connectionString string
}

var (
	dbInstance *DatabaseConnection
	once       sync.Once
)

func GetDatabaseConnection() *DatabaseConnection {
	once.Do(func() {
		fmt.Println("데이터베이스 연결을 초기화합니다...")
		// 실제로는 여기서 데이터베이스에 연결한다
		dbInstance = &DatabaseConnection{
			connectionString: "server=localhost;port=5432",
		}
		fmt.Println("데이터베이스 연결 완료")
	})
	return dbInstance
}

func main() {
	var wg sync.WaitGroup

	// 10개의 고루틴이 동시에 데이터베이스 연결을 요청한다
	for i := 0; i < 10; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			db := GetDatabaseConnection()
			fmt.Printf("고루틴 %d: 데이터베이스 연결 획득 (%s)\n", 
				id, db.connectionString)
		}(i)
	}

	wg.Wait()
}
```

이 코드를 실행하면 "데이터베이스 연결을 초기화합니다..."와 "데이터베이스 연결 완료"가 단 한 번만 출력된다. 10개의 고루틴이 모두 `GetDatabaseConnection()`을 호출했지만, 초기화는 한 번만 이루어진다.

### Once와 Race Condition

Once를 사용하지 않으면 다음과 같은 문제가 발생할 수 있다.

```go
// 위험한 싱글톤 구현
var dbInstance *DatabaseConnection

func GetDatabaseConnectionUnsafe() *DatabaseConnection {
	if dbInstance == nil { // 체크
		dbInstance = &DatabaseConnection{} // 초기화
	}
	return dbInstance
}
```

위 코드에서 여러 고루틴이 동시에 `if dbInstance == nil` 조건을 확인할 수 있다. 모두 nil을 확인하고 동시에 초기화를 시도하면, 서로 다른 인스턴스가 생성될 수 있다. 이를 "Double-Checked Locking 문제"라고 한다.

Once는 이러한 문제를 해결하기 위해 내부적으로 Mutex와 플래그를 사용하여, 함수의 실행을 정확히 한 번으로 보장한다.

### Once의 제한사항

Once는 매우 단순한 목적을 위해 설계되었다. 초기화가 실패했을 때 재시도할 수 없다. 만약 초기화 함수가 에러를 반환해야 한다면, 다른 방식을 사용해야 한다.

```go
// Once를 사용한 에러 처리 패턴
type SafeInit struct {
	once sync.Once
	err  error
	val  *DatabaseConnection
}

func (si *SafeInit) Get() (*DatabaseConnection, error) {
	si.once.Do(func() {
		// 초기화 로직
		si.val = &DatabaseConnection{}
		// si.err = ... 에러 처리
	})
	return si.val, si.err
}
```

## 7.4 sync.Pool

### Pool의 개념과 목적

`sync.Pool`은 임시 객체를 재사용하여 메모리 할당을 줄이는 메커니즘이다. 게임 서버에서는 패킷을 주고받을 때 매번 새로운 버퍼를 생성하는데, 이는 가비지 컬렉션(GC) 부담을 증가시킨다. Pool을 사용하면 사용한 객체를 저장했다가, 다음에 필요할 때 재사용할 수 있다.

Pool은 스레드 안전하고(고루틴 안전), 별도의 잠금 없이 효율적으로 객체를 재사용한다.

### Pool 사용 예제

게임 서버에서 패킷을 처리할 때 버퍼를 재사용하는 예제이다.

```go
package main

import (
	"fmt"
	"sync"
)

const PACKET_BUFFER_SIZE = 4096

type PacketBuffer struct {
	data [PACKET_BUFFER_SIZE]byte
	size int
}

// 패킷 버퍼를 재사용하는 풀
var bufferPool = sync.Pool{
	New: func() interface{} {
		fmt.Println("새로운 패킷 버퍼를 생성합니다")
		return &PacketBuffer{}
	},
}

func GetBuffer() *PacketBuffer {
	buf := bufferPool.Get().(*PacketBuffer)
	buf.size = 0 // 버퍼 초기화
	return buf
}

func PutBuffer(buf *PacketBuffer) {
	buf.size = 0 // 데이터 초기화
	bufferPool.Put(buf)
}

func ProcessPacket(packetData []byte) {
	// 버퍼 풀에서 버퍼를 가져온다
	buf := GetBuffer()
	defer PutBuffer(buf)

	// 패킷 데이터를 버퍼에 복사한다
	copy(buf.data[:], packetData)
	buf.size = len(packetData)

	fmt.Printf("패킷 처리: %d 바이트\n", buf.size)
}

func main() {
	var wg sync.WaitGroup

	// 1000개의 패킷을 처리한다
	for i := 0; i < 1000; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()

			// 각 패킷은 다양한 크기를 가진다
			packetSize := (id % 100) + 1
			packet := make([]byte, packetSize)
			for j := 0; j < packetSize; j++ {
				packet[j] = byte(id % 256)
			}

			ProcessPacket(packet)
		}(i)
	}

	wg.Wait()
	fmt.Println("모든 패킷 처리 완료")
}
```

이 예제에서 중요한 점을 설명한다.

`sync.Pool`의 New 필드에 객체를 생성하는 함수를 지정한다. Pool이 비어있을 때 `Get()`을 호출하면 이 함수가 새로운 객체를 생성한다. 

`Get()`으로 객체를 얻은 후 사용을 마치면 `Put()`으로 되돌린다. 이렇게 함으로써 메모리 할당의 반복을 줄일 수 있다.

### Pool 사용 시 주의사항

Pool에는 중요한 특징이 있다. 첫째, GC(가비지 컬렉션) 실행 시 Pool의 모든 객체가 비워진다. 따라서 Pool은 임시 객체를 위한 것이지, 장기간 객체를 저장하는 용도가 아니다.

둘째, Pool에서 얻은 객체의 상태를 보장하지 않는다. 위 예제에서 `buf.size = 0`으로 명시적으로 초기화하는 이유가 이것이다.

### Pool의 성능 특성

다음은 Pool을 사용하지 않은 경우와 비교하는 예제이다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

type Buffer struct {
	data [4096]byte
}

var pool = sync.Pool{
	New: func() interface{} {
		return &Buffer{}
	},
}

func benchmarkWithPool(iterations int) time.Duration {
	start := time.Now()
	var wg sync.WaitGroup

	for i := 0; i < iterations; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			buf := pool.Get().(*Buffer)
			defer pool.Put(buf)

			// 버퍼 사용
			copy(buf.data[:], []byte("test"))
		}()
	}

	wg.Wait()
	return time.Since(start)
}

func benchmarkWithoutPool(iterations int) time.Duration {
	start := time.Now()
	var wg sync.WaitGroup

	for i := 0; i < iterations; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			buf := &Buffer{}

			// 버퍼 사용
			copy(buf.data[:], []byte("test"))
		}()
	}

	wg.Wait()
	return time.Since(start)
}

func main() {
	iterations := 10000

	dur1 := benchmarkWithPool(iterations)
	fmt.Printf("Pool 사용: %v\n", dur1)

	dur2 := benchmarkWithoutPool(iterations)
	fmt.Printf("Pool 미사용: %v\n", dur2)

	fmt.Printf("개선율: %.2f%%\n", 
		float64(dur2-dur1)/float64(dur2)*100)
}
```

고부하 상황에서 Pool을 사용하면 메모리 할당과 GC 부담이 크게 감소한다.

## 7.5 atomic 패키지

### Atomic 연산의 개념

`atomic` 패키지는 원자적(atomic) 연산을 제공한다. 원자적 연산은 더 이상 나눌 수 없는 단위의 연산으로, 중간에 다른 고루틴의 개입이 불가능하다. 

Mutex와의 차이는, Mutex는 여러 명령어를 포함한 코드 블록을 보호하는 반면, atomic 연산은 단일 메모리 연산(읽기, 쓰기, 증가 등)만을 지원한다. 따라서 Mutex보다 가볍고 빠르다.

### Atomic 연산의 종류

`atomic` 패키지가 지원하는 주요 연산들을 나열하면 다음과 같다.

정수형 연산: `LoadInt64()`, `StoreInt64()`, `AddInt64()`, `SwapInt64()`, `CompareAndSwapInt64()`

포인터 연산: `LoadPointer()`, `StorePointer()`, `CompareAndSwapPointer()`

불린형 연산: `LoadBool()`, `StoreBool()`, `CompareAndSwapBool()`

### Atomic을 사용한 카운터 예제

게임 서버에서 현재 접속한 플레이어 수를 추적하는 예제이다.

```go
package main

import (
	"fmt"
	"sync"
	"sync/atomic"
	"time"
)

type Server struct {
	playerCount int64
	roomCount   int64
}

func (s *Server) PlayerLogin() {
	atomic.AddInt64(&s.playerCount, 1)
}

func (s *Server) PlayerLogout() {
	atomic.AddInt64(&s.playerCount, -1)
}

func (s *Server) GetPlayerCount() int64 {
	return atomic.LoadInt64(&s.playerCount)
}

func (s *Server) CreateRoom() {
	atomic.AddInt64(&s.roomCount, 1)
}

func (s *Server) DeleteRoom() {
	atomic.AddInt64(&s.roomCount, -1)
}

func (s *Server) GetRoomCount() int64 {
	return atomic.LoadInt64(&s.roomCount)
}

func main() {
	server := &Server{}

	var wg sync.WaitGroup

	// 플레이어 로그인 시뮬레이션
	for i := 0; i < 50; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			server.PlayerLogin()
			fmt.Printf("플레이어 %d 로그인. 현재 인원: %d\n", 
				id, server.GetPlayerCount())

			time.Sleep(100 * time.Millisecond)

			server.PlayerLogout()
			fmt.Printf("플레이어 %d 로그아웃. 현재 인원: %d\n", 
				id, server.GetPlayerCount())
		}(i)
	}

	wg.Wait()
	fmt.Printf("최종 플레이어 수: %d\n", server.GetPlayerCount())
}
```

`atomic.AddInt64(&s.playerCount, 1)`은 playerCount를 1 증가시킨다. 이 연산은 원자적으로 수행되므로, 동시에 여러 고루틴이 호출해도 카운트가 정확하게 유지된다.

### Atomic vs Mutex 비교

Atomic이 더 빠르지만 제한사항이 있다. 단일 변수의 원자적 연산만 지원하므로, 여러 변수의 상태를 함께 변경해야 한다면 Mutex를 사용해야 한다.

예를 들어, 플레이어가 방에 입장할 때 플레이어 수와 방의 상태를 함께 변경해야 한다면, 이 두 변경이 함께 이루어지도록 Mutex로 보호해야 한다.

### CompareAndSwap을 이용한 최적화적 잠금

Atomic 패키지의 `CompareAndSwap` 함수는 특별한 패턴을 구현할 때 유용하다. 이 함수는 기대값과 현재값이 같을 때만 새 값으로 변경한다.

```go
package main

import (
	"fmt"
	"sync"
	"sync/atomic"
)

type GameState int32

const (
	StateInitializing GameState = 0
	StateRunning      GameState = 1
	StateStopping     GameState = 2
	StateStopped      GameState = 3
)

type GameServer struct {
	state int32
}

func (gs *GameServer) Start() bool {
	// StateInitializing에서만 StateRunning으로 변경할 수 있다
	return atomic.CompareAndSwapInt32(
		(*int32)(&gs.state),
		int32(StateInitializing),
		int32(StateRunning),
	)
}

func (gs *GameServer) Stop() bool {
	// StateRunning에서만 StateStopping으로 변경할 수 있다
	return atomic.CompareAndSwapInt32(
		(*int32)(&gs.state),
		int32(StateRunning),
		int32(StateStopping),
	)
}

func (gs *GameServer) GetState() GameState {
	return GameState(atomic.LoadInt32((*int32)(&gs.state)))
}

func main() {
	server := &GameServer{
		state: int32(StateInitializing),
	}

	var wg sync.WaitGroup

	// 여러 고루틴이 동시에 서버를 시작하려고 한다
	for i := 0; i < 5; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			if server.Start() {
				fmt.Printf("고루틴 %d: 서버 시작 성공\n", id)
			} else {
				fmt.Printf("고루틴 %d: 서버 시작 실패 (현재 상태: %v)\n", 
					id, server.GetState())
			}
		}(i)
	}

	wg.Wait()

	fmt.Printf("최종 서버 상태: %v\n", server.GetState())
}
```

이 예제에서 `CompareAndSwap`은 서버 상태 머신을 구현한다. 여러 고루틴이 동시에 서버를 시작하려고 해도, 정확히 하나의 고루틴만 성공한다.

## 7.6 게임 서버에서의 동시성 패턴

### 방(Room) 관리에서의 동시성

게임 서버의 포커 게임에서 방은 공유 자원이다. 여러 플레이어가 동시에 방에 접근할 수 있다. 다음은 방을 동시성 안전하게 관리하는 예제이다.

```go
package main

import (
	"fmt"
	"sync"
)

type Player struct {
	ID   string
	Gold int
}

type Room struct {
	mu       sync.RWMutex
	ID       string
	Players  map[string]*Player
	MaxCount int
	Status   string
}

func (r *Room) GetPlayerList() []string {
	r.mu.RLock()
	defer r.mu.RUnlock()

	players := make([]string, 0, len(r.Players))
	for _, player := range r.Players {
		players = append(players, player.ID)
	}
	return players
}

func (r *Room) AddPlayer(player *Player) bool {
	r.mu.Lock()
	defer r.mu.Unlock()

	if len(r.Players) >= r.MaxCount {
		return false
	}

	if _, exists := r.Players[player.ID]; exists {
		return false
	}

	r.Players[player.ID] = player
	fmt.Printf("플레이어 %s이 방 %s에 입장했습니다. (현재: %d명)\n", 
		player.ID, r.ID, len(r.Players))
	return true
}

func (r *Room) RemovePlayer(playerID string) bool {
	r.mu.Lock()
	defer r.mu.Unlock()

	if _, exists := r.Players[playerID]; !exists {
		return false
	}

	delete(r.Players, playerID)
	fmt.Printf("플레이어 %s이 방 %s에서 퇴장했습니다. (현재: %d명)\n", 
		playerID, r.ID, len(r.Players))
	return true
}

func (r *Room) GetPlayerCount() int {
	r.mu.RLock()
	defer r.mu.RUnlock()
	return len(r.Players)
}

func (r *Room) StartGame() bool {
	r.mu.Lock()
	defer r.mu.Unlock()

	if len(r.Players) < 2 {
		return false
	}

	r.Status = "playing"
	return true
}

func main() {
	room := &Room{
		ID:       "room1",
		Players:  make(map[string]*Player),
		MaxCount: 6,
		Status:   "waiting",
	}

	var wg sync.WaitGroup

	// 10명의 플레이어가 방에 입장을 시도한다
	for i := 1; i <= 10; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			player := &Player{
				ID:   fmt.Sprintf("player%d", id),
				Gold: 1000,
			}
			room.AddPlayer(player)
		}(i)
	}

	wg.Wait()

	fmt.Printf("\n방 %s에 입장한 플레이어: %v\n", room.ID, room.GetPlayerList())
	fmt.Printf("현재 플레이어 수: %d\n", room.GetPlayerCount())

	if room.StartGame() {
		fmt.Printf("게임이 시작되었습니다. 현재 상태: %s\n", room.Status)
	}
}
```

이 패턴에서 주목할 점은 다음과 같다.

플레이어를 추가하거나 제거할 때(쓰기)는 Lock을 사용한다. 플레이어 목록을 조회하거나 인원 수를 확인할 때(읽기)는 RLock을 사용한다. 이렇게 하면 여러 플레이어가 동시에 방의 정보를 조회할 수 있으면서도, 플레이어 추가/제거 시에는 일관성을 보장한다.

### 플레이어 상태 관리의 동시성

플레이어의 상태(골드, 경험치, 아이템 등)도 동시에 여러 요청에서 접근될 수 있다. 다음은 플레이어 상태를 원자적으로 관리하는 예제이다.

```go
package main

import (
	"fmt"
	"sync"
	"sync/atomic"
)

type PlayerState struct {
	ID        string
	Gold      int64
	Exp       int64
	Level     int32
	mu        sync.Mutex
	Inventory map[string]int
}

func (ps *PlayerState) AddGold(amount int64) {
	atomic.AddInt64(&ps.Gold, amount)
}

func (ps *PlayerState) GetGold() int64 {
	return atomic.LoadInt64(&ps.Gold)
}

func (ps *PlayerState) AddExp(amount int64) int32 {
	newExp := atomic.AddInt64(&ps.Exp, amount)
	
	// 경험치 기반으로 레벨 계산 (경험치가 100마다 1레벨 상승)
	expectedLevel := int32(newExp / 100)
	currentLevel := atomic.LoadInt32(&ps.Level)
	
	if expectedLevel > currentLevel {
		atomic.StoreInt32(&ps.Level, expectedLevel)
		fmt.Printf("플레이어 %s이 레벨 %d로 상승했습니다!\n", 
			ps.ID, expectedLevel)
	}
	
	return atomic.LoadInt32(&ps.Level)
}

func (ps *PlayerState) AddItem(itemName string, quantity int) {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	if _, exists := ps.Inventory[itemName]; !exists {
		ps.Inventory[itemName] = 0
	}
	ps.Inventory[itemName] += quantity
}

func (ps *PlayerState) RemoveItem(itemName string, quantity int) bool {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	if count, exists := ps.Inventory[itemName]; exists && count >= quantity {
		ps.Inventory[itemName] -= quantity
		return true
	}
	return false
}

func (ps *PlayerState) GetInventory() map[string]int {
	ps.mu.Lock()
	defer ps.mu.Unlock()

	// 복사본을 반환하여 외부에서의 수정을 방지한다
	inventory := make(map[string]int)
	for k, v := range ps.Inventory {
		inventory[k] = v
	}
	return inventory
}

func main() {
	player := &PlayerState{
		ID:        "player1",
		Gold:      1000,
		Exp:       0,
		Level:     1,
		Inventory: make(map[string]int),
	}

	var wg sync.WaitGroup

	// 5개의 고루틴이 골드를 더한다 (atomic 연산)
	for i := 0; i < 5; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 100; j++ {
				player.AddGold(10)
			}
		}(i)
	}

	// 3개의 고루틴이 경험치를 더한다 (atomic 연산)
	for i := 0; i < 3; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 200; j++ {
				player.AddExp(5)
			}
		}(i)
	}

	// 2개의 고루틴이 아이템을 추가한다 (mutex 연산)
	for i := 0; i < 2; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			for j := 0; j < 50; j++ {
				player.AddItem("sword", 1)
				player.AddItem("shield", 1)
			}
		}(i)
	}

	wg.Wait()

	fmt.Printf("최종 상태:\n")
	fmt.Printf("  골드: %d\n", player.GetGold())
	fmt.Printf("  경험치: %d\n", atomic.LoadInt64(&player.Exp))
	fmt.Printf("  레벨: %d\n", atomic.LoadInt32(&player.Level))
	fmt.Printf("  인벤토리: %v\n", player.GetInventory())
}
```

이 패턴에서 중요한 설계 결정을 설명한다.

골드와 경험치는 atomic 연산으로 관리한다. 이들은 단순한 정수 값이므로, 원자적 연산만으로 충분하다.

인벤토리는 맵이므로, 여러 아이템의 상태를 함께 관리해야 한다. 따라서 Mutex로 보호한다.

단순 조회와 수정을 분리한다. 단순 조회(읽기)는 Lock을 사용하지 않거나 가벼운 방식으로 처리한다. 상태 수정(쓰기)은 명시적으로 Lock으로 보호한다.

### 높은 처리량이 필요한 시나리오에서의 패턴

게임 서버가 매초 수천 개의 메시지를 처리해야 할 때, 효율적인 동시성 패턴이 필요하다. 다음은 샤딩(sharding) 패턴을 사용하여 경쟁을 줄이는 예제이다.

```go
package main

import (
	"fmt"
	"hash/fnv"
	"sync"
	"sync/atomic"
)

type GameStateShard struct {
	mu    sync.RWMutex
	stats map[string]int64 // 플레이어별 통계
}

type GameStateShardedManager struct {
	shards    []*GameStateShard
	shardMask uint32
}

func NewGameStateShardedManager(shardCount int) *GameStateShardedManager {
	shards := make([]*GameStateShard, shardCount)
	for i := 0; i < shardCount; i++ {
		shards[i] = &GameStateShard{
			stats: make(map[string]int64),
		}
	}
	return &GameStateShardedManager{
		shards:    shards,
		shardMask: uint32(shardCount - 1),
	}
}

func (g *GameStateShardedManager) getShard(playerID string) *GameStateShard {
	// 플레이어 ID의 해시값을 기반으로 샤드를 선택한다
	hash := fnv.New32a()
	hash.Write([]byte(playerID))
	index := hash.Sum32() & g.shardMask
	return g.shards[index]
}

func (g *GameStateShardedManager) AddKill(playerID string) {
	shard := g.getShard(playerID)
	shard.mu.Lock()
	defer shard.mu.Unlock()

	shard.stats[playerID+"_kills"]++
}

func (g *GameStateShardedManager) GetKillCount(playerID string) int64 {
	shard := g.getShard(playerID)
	shard.mu.RLock()
	defer shard.mu.RUnlock()

	return shard.stats[playerID+"_kills"]
}

func main() {
	manager := NewGameStateShardedManager(16)

	var wg sync.WaitGroup
	var totalOperations int64

	// 1000명의 플레이어가 동시에 킬을 기록한다
	for i := 0; i < 1000; i++ {
		wg.Add(1)
		go func(id int) {
			defer wg.Done()
			playerID := fmt.Sprintf("player%d", id)
			
			// 각 플레이어가 100번 킬을 기록한다
			for j := 0; j < 100; j++ {
				manager.AddKill(playerID)
				atomic.AddInt64(&totalOperations, 1)
			}
		}(i)
	}

	wg.Wait()

	fmt.Printf("총 연산 수: %d\n", atomic.LoadInt64(&totalOperations))
	fmt.Printf("샤드 수: %d\n", len(manager.shards))
	
	// 일부 플레이어의 킬 수를 확인한다
	for i := 0; i < 10; i++ {
		playerID := fmt.Sprintf("player%d", i)
		kills := manager.GetKillCount(playerID)
		fmt.Printf("%s: %d킬\n", playerID, kills)
	}
}
```

이 샤딩 패턴은 다음과 같은 이점을 제공한다.

전체 데이터를 하나의 Lock으로 보호하는 대신, 여러 개의 작은 Lock으로 분산한다. 이렇게 하면 여러 고루틴이 다른 플레이어의 데이터를 수정할 때 서로 대기할 필요가 없다.

플레이어 ID를 해시하여 어느 샤드에 저장할지 결정한다. 같은 플레이어의 데이터는 항상 같은 샤드에 저장되므로 일관성을 보장한다.

이 패턴은 높은 처리량이 필요한 게임 서버에서 매우 효과적이다.

---

## 정리

동기화 패턴은 게임 서버의 안정성과 성능을 좌우하는 중요한 요소이다. 다음은 각 도구의 사용 시점을 정리한 표이다.

```
┌─────────────────┬──────────────────────────────────────────────────┐
│ 도구            │ 사용 시점                                        │
├─────────────────┼──────────────────────────────────────────────────┤
│ Mutex           │ 여러 명령어의 코드 블록을 보호할 때              │
│                 │ 읽기와 쓰기 비율이 비슷할 때                      │
│                 │ 간단한 보호가 필요할 때                           │
├─────────────────┼──────────────────────────────────────────────────┤
│ RWMutex         │ 읽기가 쓰기보다 훨씬 많을 때 (10:1 이상)        │
│                 │ 조회 성능이 중요할 때                            │
├─────────────────┼──────────────────────────────────────────────────┤
│ WaitGroup       │ 여러 고루틴의 완료를 기다려야 할 때              │
│                 │ 초기화, 종료 단계에서                             │
├─────────────────┼──────────────────────────────────────────────────┤
│ Once            │ 정확히 한 번만 실행되어야 할 때                   │
│                 │ 싱글톤 초기화 시                                  │
├─────────────────┼──────────────────────────────────────────────────┤
│ Pool            │ 임시 객체를 빈번하게 생성할 때                   │
│                 │ GC 부담을 줄이고 싶을 때                          │
│                 │ 버퍼, 패킷 등 재사용 가능한 객체                 │
├─────────────────┼──────────────────────────────────────────────────┤
│ atomic          │ 단일 정수 값의 원자적 연산이 필요할 때           │
│                 │ 카운터, 플래그 등 간단한 값                      │
│                 │ Mutex보다 높은 성능이 필요할 때                   │
├─────────────────┼──────────────────────────────────────────────────┤
│ Sharding        │ 높은 처리량이 필요할 때 (초당 수천 연산)         │
│                 │ 경쟁을 줄이고 싶을 때                            │
└─────────────────┴──────────────────────────────────────────────────┘
```

게임 서버 개발 시에는 먼저 정확성을 확보한 후, 성능 분석을 통해 필요한 부분에만 최적화를 적용해야 한다. 과도한 동기화는 성능을 해치고, 불충분한 동기화는 데이터 손상을 초래한다. 이 장에서 학습한 도구들을 적절히 조합하면, 안정적이고 효율적인 게임 서버를 구축할 수 있다.



# Chapter 8. Context 패키지

게임 서버에서 여러 고루틴이 독립적으로 작동할 때, 특정 상황에서 이들을 조율하거나 제어해야 한다. 예를 들어, 클라이언트가 연결을 끊을 때 그 클라이언트와 관련된 모든 고루틴을 중지해야 하거나, 일정 시간 내에 작업을 완료하지 못하면 자동으로 취소해야 한다. 이러한 상황들을 우아하게 처리하기 위해 Go는 `context` 패키지를 제공한다.

## 8.1 Context의 필요성

### Context가 해결하는 문제

일반적인 서버 프로그래밍에서 다음과 같은 상황들이 발생한다.

클라이언트가 연결을 종료할 때, 그 클라이언트를 처리하던 고루틴들도 함께 종료되어야 한다. 어떤 작업이 너무 오래 걸리면 타임아웃을 발생시켜야 한다. 데이터베이스 쿼리가 진행 중일 때 클라이언트가 연결을 끊으면, 데이터베이스 쿼리도 중단되어야 한다. 요청에 연관된 메타데이터(사용자 ID, 요청 ID 등)를 여러 함수에 전파해야 한다.

이러한 문제들을 해결하기 위해 Context가 등장했다.

Context는 다음의 책임을 진다.

고루틴 간의 신호 전달(취소, 타임아웃 등)을 담당한다. 요청의 생명주기를 추적한다. 데드라인을 설정하고 관리한다. 요청 범위의 값(request-scoped values)을 저장하고 전파한다.

### Context의 기본 개념

Context는 불변(immutable) 객체이다. 한번 생성되면 변경되지 않는다. Context에서 파생된 새로운 Context는 기존 Context의 특성을 상속하면서 추가 특성을 가진다. 이를 "Context 계층"이라고 부른다.

```
Background Context (루트)
    │
    ├─ WithCancel
    │   └─ 고루틴 A
    │   └─ 고루틴 B
    │
    └─ WithTimeout
        └─ 고루틴 C
        └─ 고루틴 D
```

위 다이어그램은 하나의 부모 Context에서 여러 자식 Context가 파생되는 모습을 보여준다. 부모 Context가 취소되면, 모든 자식 Context도 자동으로 취소된다.

### Context의 문제점과 대안

Context는 강력하지만, 남용하면 안 된다. 일반적인 함수 파라미터로 Context를 전달하는 것이 관례이다. 그러나 과도하게 사용하면 코드가 복잡해진다. 또한 Context는 동기식으로 작동하므로, 진정한 의미의 강제 종료(goroutine kill)는 제공하지 않는다. Context를 통해 취소 신호를 보내면, 해당 고루틴이 이를 확인하고 스스로 종료해야 한다.

## 8.2 Context 생성과 전파

### 루트 Context 생성

모든 Context는 루트 Context에서 시작된다. Go는 두 가지 루트 Context를 제공한다.

`context.Background()`: 배경 Context이다. 일반적인 서버 프로그래밍에서 모든 요청의 최상위 Context가 된다. 어떤 취소 신호, 데드라인, 값도 없다.

`context.TODO()`: TODO Context이다. Context를 어떻게 사용해야 할지 명확하지 않을 때 임시로 사용한다. 기능상 Background와 동일하지만, 의도를 명시하는 도구로 사용된다.

```go
package main

import (
	"context"
	"fmt"
)

func main() {
	// Background Context는 영구적으로 존재한다
	bgCtx := context.Background()
	fmt.Println("Background Context:", bgCtx)

	// TODO Context도 동일한 특성을 가진다
	todoCtx := context.TODO()
	fmt.Println("TODO Context:", todoCtx)

	// 두 Context 모두 <nil> 값을 가진다 (에러 없음)
	if err := bgCtx.Err(); err == nil {
		fmt.Println("Background Context는 취소되지 않음")
	}

	if err := todoCtx.Err(); err == nil {
		fmt.Println("TODO Context는 취소되지 않음")
	}
}
```

### Context 파생시키기: WithCancel

`context.WithCancel()`은 부모 Context에서 취소 가능한 자식 Context를 생성한다. 반환되는 `cancel` 함수를 호출하면 해당 Context와 그 자식들이 취소된다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func worker(ctx context.Context, name string) {
	fmt.Printf("[%s] 시작\n", name)

	for {
		select {
		case <-ctx.Done():
			fmt.Printf("[%s] 취소됨: %v\n", name, ctx.Err())
			return
		default:
			fmt.Printf("[%s] 작업 중...\n", name)
			time.Sleep(500 * time.Millisecond)
		}
	}
}

func main() {
	// 취소 가능한 Context를 생성한다
	ctx, cancel := context.WithCancel(context.Background())

	// 3개의 고루틴을 시작한다
	go worker(ctx, "worker1")
	go worker(ctx, "worker2")
	go worker(ctx, "worker3")

	// 2초 후에 모든 작업을 취소한다
	time.Sleep(2 * time.Second)
	fmt.Println("\n[메인] 모든 작업 취소 요청")
	cancel()

	// 고루틴들이 종료될 시간을 준다
	time.Sleep(1 * time.Second)
	fmt.Println("[메인] 완료")
}
```

이 코드에서 중요한 패턴을 설명한다.

`cancel` 함수는 한 번만 호출되어야 한다. 이미 취소된 Context를 다시 취소해도 에러가 발생하지 않지만, 불필요한 호출은 피해야 한다.

고루틴이 취소 신호를 확인하려면 반드시 `ctx.Done()` 채널을 모니터링해야 한다. `ctx.Done()`은 Context가 취소되면 닫히는 채널을 반환한다.

`ctx.Err()`은 Context가 취소된 이유를 반환한다. 취소된 경우 `context.Canceled` 에러를 반환한다.

### Context 파생시키기: WithTimeout

`context.WithTimeout()`은 지정된 시간이 경과하면 자동으로 취소되는 Context를 생성한다. 게임 서버에서 클라이언트 요청이 너무 오래 걸리지 않도록 보장할 때 유용하다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func fetchPlayerData(ctx context.Context, playerID string) (string, error) {
	// 데이터베이스 조회를 시뮬레이션한다 (3초 소요)
	fmt.Printf("[데이터베이스] %s 조회 시작\n", playerID)

	select {
	case <-time.After(3 * time.Second):
		fmt.Printf("[데이터베이스] %s 조회 완료\n", playerID)
		return fmt.Sprintf("Player %s Data", playerID), nil
	case <-ctx.Done():
		fmt.Printf("[데이터베이스] %s 조회 취소: %v\n", playerID, ctx.Err())
		return "", ctx.Err()
	}
}

func main() {
	// 2초의 타임아웃을 가진 Context를 생성한다
	ctx, cancel := context.WithTimeout(context.Background(), 2*time.Second)
	defer cancel() // 함수 종료 시 Context를 정리한다

	fmt.Println("player123 데이터를 2초 내에 조회합니다")
	data, err := fetchPlayerData(ctx, "player123")

	if err != nil {
		fmt.Printf("에러: %v\n", err)
	} else {
		fmt.Printf("결과: %s\n", data)
	}

	// 타임아웃이 만료될 때까지 대기
	time.Sleep(2 * time.Second)
}
```

이 예제에서 타임아웃의 작동 방식을 이해해야 한다.

`context.WithTimeout(parent, duration)`은 지정된 시간 후에 자동으로 Context를 취소한다. 따라서 `cancel()` 함수를 명시적으로 호출하지 않아도 된다. 그러나 좋은 관행은 `defer cancel()`을 사용하여 필요한 경우 조기에 Context를 취소할 수 있도록 하는 것이다.

`ctx.Err()`은 `context.DeadlineExceeded` 에러를 반환한다. 이는 타임아웃으로 인한 취소와 명시적 취소를 구분할 수 있게 한다.

### Context 파생시키기: WithDeadline

`context.WithDeadline()`은 WithTimeout과 유사하지만, 상대적인 시간(duration) 대신 절대적인 시간(deadline)을 지정한다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func requestWithDeadline(ctx context.Context, name string) {
	fmt.Printf("[%s] 시작\n", name)

	select {
	case <-time.After(5 * time.Second):
		fmt.Printf("[%s] 작업 완료\n", name)
	case <-ctx.Done():
		fmt.Printf("[%s] 데드라인 초과: %v\n", name, ctx.Err())
	}
}

func main() {
	// 3초 후에 만료되는 데드라인을 설정한다
	deadline := time.Now().Add(3 * time.Second)
	ctx, cancel := context.WithDeadline(context.Background(), deadline)
	defer cancel()

	fmt.Printf("데드라인: %v\n", deadline)
	fmt.Printf("현재 시간: %v\n", time.Now())

	requestWithDeadline(ctx, "request1")
}
```

WithDeadline과 WithTimeout의 선택 기준은 다음과 같다.

요청의 완료 시간이 명확할 때(예: API 호출이 정오까지 완료되어야 함) WithDeadline을 사용한다. 상대적인 시간 제한이 필요할 때(예: 지금부터 5초 이내) WithTimeout을 사용한다.

### Context 값 저장: WithValue

Context에 데이터를 저장하고 전파할 수 있다. 이는 요청 ID, 사용자 정보 등 요청 범위의 데이터를 여러 함수에 전달할 때 유용하다.

```go
package main

import (
	"context"
	"fmt"
)

type UserInfo struct {
	ID   string
	Name string
}

func processRequest(ctx context.Context) {
	// Context에서 값을 추출한다
	userID := ctx.Value("userID")
	requestID := ctx.Value("requestID")

	fmt.Printf("사용자 ID: %v\n", userID)
	fmt.Printf("요청 ID: %v\n", requestID)

	// 타입 assertion을 사용하여 타입을 확인한다
	if user, ok := ctx.Value("user").(*UserInfo); ok {
		fmt.Printf("사용자명: %s\n", user.Name)
	}
}

func main() {
	// 기본 Context에서 시작한다
	ctx := context.Background()

	// 값을 추가한다 (순차적으로)
	ctx = context.WithValue(ctx, "userID", "player123")
	ctx = context.WithValue(ctx, "requestID", "req-001")
	ctx = context.WithValue(ctx, "user", &UserInfo{
		ID:   "player123",
		Name: "Alice",
	})

	processRequest(ctx)
}
```

WithValue 사용 시 주의사항이 있다.

값은 문자열이 아닌 고유한 타입을 키로 사용해야 한다. 충돌을 방지하기 위해 패키지별 고유한 타입을 정의한다. WithValue는 O(n) 복잡도를 가지므로(n은 저장된 값의 개수), 과도하게 사용하면 성능이 저하된다. 따라서 필수적인 데이터만 저장해야 한다.

```go
// 올바른 키 정의 방식
package game

type contextKey string

const (
	playerIDKey contextKey = "playerID"
	roomIDKey   contextKey = "roomID"
)

// 올바른 사용
ctx = context.WithValue(ctx, playerIDKey, "player123")

// Context에서 추출
if playerID, ok := ctx.Value(playerIDKey).(string); ok {
	fmt.Println(playerID)
}
```

## 8.3 취소, 타임아웃, 데드라인

### 게임 서버에서의 취소 패턴

게임 서버에서 클라이언트 요청을 처리할 때, 취소 메커니즘이 매우 중요하다. 다음은 플레이어 매칭을 처리하는 예제이다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

type MatchRequest struct {
	PlayerID string
	Timeout  time.Duration
}

type MatchResult struct {
	Status string
	RoomID string
	Error  error
}

func findMatch(ctx context.Context, req MatchRequest) MatchResult {
	fmt.Printf("[매칭] %s 플레이어 매칭 시작\n", req.PlayerID)

	// 매칭 로직을 시뮬레이션한다
	for i := 0; i < 10; i++ {
		select {
		case <-ctx.Done():
			// Context가 취소되면 함수를 종료한다
			fmt.Printf("[매칭] %s 플레이어 매칭 취소\n", req.PlayerID)
			return MatchResult{
				Status: "canceled",
				Error:  ctx.Err(),
			}
		case <-time.After(500 * time.Millisecond):
			// 매칭 대기 (각 단계는 500ms 소요)
			fmt.Printf("[매칭] %s 플레이어 검색 중... (%d/10)\n", 
				req.PlayerID, i+1)

			if i == 6 {
				// 7번째 시도에서 매칭 성공
				fmt.Printf("[매칭] %s 플레이어 매칭 성공!\n", req.PlayerID)
				return MatchResult{
					Status: "matched",
					RoomID: "room123",
				}
			}
		}
	}

	return MatchResult{
		Status: "timeout",
		Error:  context.DeadlineExceeded,
	}
}

func main() {
	// 3초의 타임아웃이 있는 Context를 생성한다
	ctx, cancel := context.WithTimeout(
		context.Background(),
		3*time.Second,
	)
	defer cancel()

	req := MatchRequest{
		PlayerID: "player123",
		Timeout:  3 * time.Second,
	}

	result := findMatch(ctx, req)
	fmt.Printf("\n매칭 결과:\n")
	fmt.Printf("  상태: %s\n", result.Status)
	if result.Error != nil {
		fmt.Printf("  에러: %v\n", result.Error)
	} else {
		fmt.Printf("  방 ID: %s\n", result.RoomID)
	}
}
```

이 예제에서 중요한 패턴을 설명한다.

`select`문을 사용하여 Context 취소를 모니터링한다. 이렇게 하면 Context가 취소되면 즉시 반응할 수 있다. 데드라인이 만료되기 전에 작업이 완료되면, 자신의 작업을 정리하고 반환한다. 남은 시간이 있더라도 작업이 끝나면 더 이상 대기할 이유가 없다.

### 다중 타임아웃 관리

복잡한 게임 서버에서는 여러 단계의 작업이 있을 수 있다. 각 단계마다 타임아웃을 설정하면서, 전체 타임아웃도 유지해야 한다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func validateInput(ctx context.Context, data string) (bool, error) {
	fmt.Println("[검증] 입력 검증 시작")

	select {
	case <-time.After(500 * time.Millisecond):
		fmt.Println("[검증] 검증 완료")
		return true, nil
	case <-ctx.Done():
		return false, ctx.Err()
	}
}

func queryDatabase(ctx context.Context, query string) (string, error) {
	fmt.Println("[데이터베이스] 쿼리 실행")

	select {
	case <-time.After(1500 * time.Millisecond):
		fmt.Println("[데이터베이스] 쿼리 완료")
		return "result", nil
	case <-ctx.Done():
		return "", ctx.Err()
	}
}

func processRequest(ctx context.Context, data string) (string, error) {
	fmt.Printf("요청 처리 시작, 남은 시간: %v\n", 
		time.Until(getDeadline(ctx)))

	// 1단계: 입력 검증
	valid, err := validateInput(ctx, data)
	if err != nil {
		return "", fmt.Errorf("입력 검증 실패: %w", err)
	}
	if !valid {
		return "", fmt.Errorf("입력이 유효하지 않음")
	}

	fmt.Printf("검증 완료, 남은 시간: %v\n", 
		time.Until(getDeadline(ctx)))

	// 2단계: 데이터베이스 쿼리
	result, err := queryDatabase(ctx, "SELECT ...")
	if err != nil {
		return "", fmt.Errorf("데이터베이스 쿼리 실패: %w", err)
	}

	fmt.Printf("요청 처리 완료, 남은 시간: %v\n", 
		time.Until(getDeadline(ctx)))

	return result, nil
}

func getDeadline(ctx context.Context) time.Time {
	deadline, ok := ctx.Deadline()
	if !ok {
		return time.Time{}
	}
	return deadline
}

func main() {
	// 전체 작업을 위해 3초의 타임아웃을 설정한다
	ctx, cancel := context.WithTimeout(
		context.Background(),
		3*time.Second,
	)
	defer cancel()

	result, err := processRequest(ctx, "input_data")

	fmt.Printf("\n최종 결과:\n")
	if err != nil {
		fmt.Printf("에러: %v\n", err)
	} else {
		fmt.Printf("결과: %s\n", result)
	}
}
```

### Context 취소와 고루틴 정리

Context를 사용할 때 가장 중요한 것은 고루틴의 정리이다. Context가 취소되면, 그 Context를 사용하는 모든 고루틴이 종료되도록 보장해야 한다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

func backgroundTask(ctx context.Context, taskID string, cleanup chan<- string) {
	defer func() {
		cleanup <- fmt.Sprintf("task-%s", taskID)
	}()

	fmt.Printf("[작업 %s] 시작\n", taskID)

	ticker := time.NewTicker(1 * time.Second)
	defer ticker.Stop()

	for {
		select {
		case <-ctx.Done():
			fmt.Printf("[작업 %s] 취소됨: %v\n", taskID, ctx.Err())
			return
		case <-ticker.C:
			fmt.Printf("[작업 %s] 실행 중...\n", taskID)
		}
	}
}

func main() {
	ctx, cancel := context.WithCancel(context.Background())

	cleanup := make(chan string, 10)
	taskCount := 5

	// 여러 개의 백그라운드 작업을 시작한다
	for i := 1; i <= taskCount; i++ {
		go backgroundTask(ctx, fmt.Sprintf("%d", i), cleanup)
	}

	// 3초 후에 모든 작업을 취소한다
	time.Sleep(3 * time.Second)
	fmt.Println("\n[메인] 모든 작업 취소 요청")
	cancel()

	// 모든 작업이 정리될 때까지 대기한다
	fmt.Println("[메인] 작업 정리 대기 중...")
	cleanedTasks := 0
	for cleanedTasks < taskCount {
		cleanedTask := <-cleanup
		fmt.Printf("[메인] 정리 완료: %s\n", cleanedTask)
		cleanedTasks++
	}

	fmt.Println("[메인] 모든 작업 정리 완료")
}
```

이 패턴은 다음을 보장한다.

Context가 취소되면 모든 고루틴이 즉시 반응한다. defer를 사용하여 정리 코드를 항상 실행한다. 정리 채널을 사용하여 모든 고루틴이 정리될 때까지 메인 함수가 기다린다.

## 8.4 Context를 이용한 고루틴 관리

### 요청-응답 패턴

게임 서버의 핵심은 요청을 받고 응답하는 패턴이다. Context를 사용하면 각 요청의 생명주기를 효율적으로 관리할 수 있다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

type Request struct {
	ID   string
	Data string
}

type Response struct {
	RequestID string
	Result    string
	Error     error
}

type RequestHandler struct {
	name string
}

func (h *RequestHandler) Handle(ctx context.Context, req Request) Response {
	fmt.Printf("[%s] 요청 %s 처리 시작\n", h.name, req.ID)

	// 요청 처리를 시뮬레이션한다
	select {
	case <-time.After(2 * time.Second):
		result := fmt.Sprintf("Processed: %s", req.Data)
		fmt.Printf("[%s] 요청 %s 처리 완료\n", h.name, req.ID)
		return Response{
			RequestID: req.ID,
			Result:    result,
		}
	case <-ctx.Done():
		fmt.Printf("[%s] 요청 %s 처리 취소\n", h.name, req.ID)
		return Response{
			RequestID: req.ID,
			Error:     ctx.Err(),
		}
	}
}

func main() {
	handler := &RequestHandler{name: "MainHandler"}

	// 요청 1: 정상 완료
	fmt.Println("=== 요청 1 ===")
	ctx1, cancel1 := context.WithTimeout(
		context.Background(),
		3*time.Second,
	)
	resp1 := handler.Handle(ctx1, Request{ID: "req-1", Data: "test"})
	fmt.Printf("응답: ID=%s, Result=%s, Error=%v\n\n", 
		resp1.RequestID, resp1.Result, resp1.Error)
	cancel1()

	// 요청 2: 타임아웃으로 취소
	fmt.Println("=== 요청 2 ===")
	ctx2, cancel2 := context.WithTimeout(
		context.Background(),
		1*time.Second,
	)
	resp2 := handler.Handle(ctx2, Request{ID: "req-2", Data: "test2"})
	fmt.Printf("응답: ID=%s, Result=%s, Error=%v\n", 
		resp2.RequestID, resp2.Result, resp2.Error)
	cancel2()
}
```

### 게임 서버의 세션 관리

게임 서버에서 각 클라이언트 세션은 독립적인 Context를 가져야 한다. 클라이언트가 연결을 끊으면 해당 Context가 취소되어, 그 클라이언트와 관련된 모든 작업이 중단된다.

```go
package main

import (
	"context"
	"fmt"
	"sync"
	"time"
)

type Session struct {
	ID       string
	ctx      context.Context
	cancel   context.CancelFunc
	messages chan string
}

func NewSession(sessionID string) *Session {
	ctx, cancel := context.WithCancel(context.Background())
	return &Session{
		ID:       sessionID,
		ctx:      ctx,
		cancel:   cancel,
		messages: make(chan string, 100),
	}
}

func (s *Session) HandleConnection(wg *sync.WaitGroup) {
	defer wg.Done()

	fmt.Printf("[세션 %s] 연결 수락\n", s.ID)

	// 메시지 처리 고루틴
	go s.messageProcessor()

	// 클라이언트 시뮬레이션
	go s.simulateClient()

	// Context가 취소될 때까지 대기한다
	<-s.ctx.Done()
	fmt.Printf("[세션 %s] 종료\n", s.ID)
	close(s.messages)
}

func (s *Session) messageProcessor() {
	for {
		select {
		case <-s.ctx.Done():
			fmt.Printf("[세션 %s] 메시지 프로세서 종료\n", s.ID)
			return
		case msg := <-s.messages:
			fmt.Printf("[세션 %s] 수신: %s\n", s.ID, msg)
		}
	}
}

func (s *Session) simulateClient() {
	for i := 0; i < 5; i++ {
		select {
		case <-s.ctx.Done():
			fmt.Printf("[세션 %s] 클라이언트 시뮬레이션 종료\n", s.ID)
			return
		case <-time.After(1 * time.Second):
			msg := fmt.Sprintf("Message %d", i+1)
			s.messages <- msg
		}
	}

	// 5초 후 연결을 종료한다
	fmt.Printf("[세션 %s] 클라이언트 연결 종료\n", s.ID)
	s.Close()
}

func (s *Session) Close() {
	fmt.Printf("[세션 %s] 종료 요청\n", s.ID)
	s.cancel()
}

func main() {
	var wg sync.WaitGroup

	sessions := make([]*Session, 3)
	for i := 0; i < 3; i++ {
		sessions[i] = NewSession(fmt.Sprintf("client-%d", i+1))
		wg.Add(1)
		go sessions[i].HandleConnection(&wg)
	}

	wg.Wait()
	fmt.Println("\n모든 세션 종료 완료")
}
```

이 패턴에서 중요한 점들을 설명한다.

각 세션은 고유한 Context를 가진다. 클라이언트가 연결을 끊으면 Context의 cancel 함수를 호출하여, 그 세션과 관련된 모든 고루틴이 종료되도록 한다. defer를 사용하여 고루틴이 항상 정리되도록 보장한다.

### Context를 사용한 포커 게임 방 관리

게임 서버의 최종 목표인 포커 게임 구현을 염두에 두고, Context를 사용한 방 관리 예제를 만들어본다.

```go
package main

import (
	"context"
	"fmt"
	"sync"
	"time"
)

type GameRoom struct {
	ID       string
	ctx      context.Context
	cancel   context.CancelFunc
	mu       sync.RWMutex
	players  []string
	status   string
	gameOver chan struct{}
}

func NewGameRoom(roomID string) *GameRoom {
	ctx, cancel := context.WithCancel(context.Background())
	return &GameRoom{
		ID:       roomID,
		ctx:      ctx,
		cancel:   cancel,
		players:  make([]string, 0),
		status:   "waiting",
		gameOver: make(chan struct{}),
	}
}

func (r *GameRoom) AddPlayer(playerID string) bool {
	r.mu.Lock()
	defer r.mu.Unlock()

	if len(r.players) >= 6 {
		return false
	}

	r.players = append(r.players, playerID)
	fmt.Printf("[방 %s] %s 플레이어 입장 (인원: %d)\n", 
		r.ID, playerID, len(r.players))
	return true
}

func (r *GameRoom) GetPlayerCount() int {
	r.mu.RLock()
	defer r.mu.RUnlock()
	return len(r.players)
}

func (r *GameRoom) Start() bool {
	r.mu.Lock()
	defer r.mu.Unlock()

	if len(r.players) < 2 {
		return false
	}

	r.status = "playing"
	fmt.Printf("[방 %s] 게임 시작 (플레이어: %d명)\n", 
		r.ID, len(r.players))
	return true
}

func (r *GameRoom) PlayGame() {
	fmt.Printf("[방 %s] 게임 플레이 시작\n", r.ID)

	gameTimer := time.NewTimer(5 * time.Second)
	defer gameTimer.Stop()

	select {
	case <-gameTimer.C:
		fmt.Printf("[방 %s] 게임 완료\n", r.ID)
	case <-r.ctx.Done():
		fmt.Printf("[방 %s] 게임 중단: %v\n", r.ID, r.ctx.Err())
		return
	}

	close(r.gameOver)
}

func (r *GameRoom) Close() {
	fmt.Printf("[방 %s] 종료 요청\n", r.ID)
	r.cancel()
}

func main() {
	var wg sync.WaitGroup

	// 게임 방을 생성한다
	room := NewGameRoom("poker-room-1")

	// 플레이어들이 입장한다
	players := []string{"player1", "player2", "player3"}
	for _, playerID := range players {
		room.AddPlayer(playerID)
	}

	// 게임을 시작한다
	if room.Start() {
		wg.Add(1)
		go func() {
			defer wg.Done()
			room.PlayGame()
		}()
	}

	// 3초 후에 방을 강제 종료한다
	time.Sleep(3 * time.Second)
	fmt.Println("\n[메인] 게임 방 종료 요청")
	room.Close()

	wg.Wait()
	fmt.Println("\n게임 방 종료 완료")
}
```

### Context 타임아웃을 활용한 플레이어 턴 관리

포커 게임에서 각 플레이어는 제한된 시간 내에 결정을 내려야 한다. Context의 타임아웃을 활용하면 이를 구현할 수 있다.

```go
package main

import (
	"context"
	"fmt"
	"time"
)

type TurnDecision int

const (
	DecisionFold TurnDecision = iota
	DecisionCall
	DecisionRaise
)

type PlayerTurn struct {
	PlayerID string
	Turn     int
	Decision TurnDecision
	Error    error
}

func (pt *PlayerTurn) String() string {
	decisions := map[TurnDecision]string{
		DecisionFold:  "Fold",
		DecisionCall:  "Call",
		DecisionRaise: "Raise",
	}

	if pt.Error != nil {
		return fmt.Sprintf("턴 %d: 에러 (%v)", pt.Turn, pt.Error)
	}

	return fmt.Sprintf("턴 %d: %s", pt.Turn, decisions[pt.Decision])
}

func waitForPlayerDecision(ctx context.Context, playerID string, turnNum int) PlayerTurn {
	fmt.Printf("[%s] 턴 %d 결정 대기 중 (제한 시간: %v)\n", 
		playerID, turnNum, getTimeRemaining(ctx))

	// 플레이어의 결정을 기다린다 (실제로는 네트워크 메시지 수신)
	decisionChan := make(chan TurnDecision, 1)

	// 플레이어의 결정을 처리하는 고루틴
	go func() {
		// 1.5초 후에 결정을 반환한다고 가정
		time.Sleep(1500 * time.Millisecond)
		decisionChan <- DecisionCall
	}()

	select {
	case decision := <-decisionChan:
		fmt.Printf("[%s] 턴 %d 결정: %v\n", playerID, turnNum, decision)
		return PlayerTurn{
			PlayerID: playerID,
			Turn:     turnNum,
			Decision: decision,
		}
	case <-ctx.Done():
		fmt.Printf("[%s] 턴 %d 타임아웃\n", playerID, turnNum)
		return PlayerTurn{
			PlayerID: playerID,
			Turn:     turnNum,
			Decision: DecisionFold, // 타임아웃 시 fold
			Error:    ctx.Err(),
		}
	}
}

func getTimeRemaining(ctx context.Context) time.Duration {
	deadline, ok := ctx.Deadline()
	if !ok {
		return 0
	}
	return time.Until(deadline)
}

func main() {
	// 각 플레이어에게 3초씩 결정 시간을 준다
	playerID := "player1"
	turnNum := 1

	ctx, cancel := context.WithTimeout(
		context.Background(),
		3*time.Second,
	)
	defer cancel()

	decision := waitForPlayerDecision(ctx, playerID, turnNum)
	fmt.Printf("\n결과: %s\n", decision.String())
}
```

---

## 정리

Context 패키지는 Go에서 동시 프로그래밍의 필수 요소이다. 다음은 Context의 주요 개념을 정리한 표이다.

```
┌─────────────────────┬──────────────────────────────────────────────┐
│ Context 타입        │ 사용 시점                                    │
├─────────────────────┼──────────────────────────────────────────────┤
│ Background()        │ 루트 Context로 사용                          │
│ TODO()              │ Context 사용이 불명확할 때 임시 사용         │
├─────────────────────┼──────────────────────────────────────────────┤
│ WithCancel()        │ 명시적 취소가 필요할 때                      │
│ WithTimeout()       │ 상대적인 시간 제한이 필요할 때               │
│ WithDeadline()      │ 절대적인 시간 지정이 필요할 때               │
│ WithValue()         │ 요청 범위의 값을 전파할 때                   │
├─────────────────────┼──────────────────────────────────────────────┤
│ Done()              │ Context 취소를 감지                          │
│ Err()               │ 취소 이유 확인 (Canceled 또는 DeadlineExceeded) │
│ Deadline()          │ Context의 데드라인 확인                      │
│ Value()             │ 저장된 값 조회                               │
└─────────────────────┴──────────────────────────────────────────────┘
```

게임 서버 개발에서 Context를 활용할 때의 핵심 원칙을 정리하면 다음과 같다.

첫째, 모든 I/O 작업(네트워크, 데이터베이스)의 첫 번째 파라미터로 Context를 전달한다. 이는 Go의 관례이며, 작업을 취소할 수 있는 통로를 제공한다.

둘째, 각 클라이언트 세션은 고유한 Context를 가져야 한다. 클라이언트가 연결을 끊으면 해당 Context가 취소되어, 관련된 모든 작업이 자동으로 중단된다.

셋째, Context를 변경해서는 안 된다. Context는 불변 객체이므로, 새로운 값을 추가하려면 새로운 Context를 파생시켜야 한다.

넷째, 타임아웃은 정확하게 설정해야 한다. 너무 짧으면 정상적인 작업도 중단될 수 있고, 너무 길면 응답 시간이 길어진다.

이러한 원칙들을 따르면, 게임 서버에서 고루틴을 효율적으로 관리하고, 사용자의 경험을 향상시킬 수 있다.  