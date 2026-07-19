# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 17. Go 모듈 고급

게임 서버 프로젝트를 개발할 때 네트워크 라이브러리와 게임 로직을 별도의 모듈로 분리하는 것은 코드 관리와 재사용성 측면에서 매우 중요하다. 이 장에서는 Go의 모듈 시스템을 깊이 있게 학습하고, 로컬 환경에서 여러 모듈을 효율적으로 관리하는 방법을 다룬다.

## 17.1 go.mod와 go.sum 이해하기

Go 모듈은 `go.mod` 파일을 통해 프로젝트의 의존성을 관리한다. Go 1.11부터 도입된 모듈 시스템은 기존의 GOPATH 방식을 대체하여 프로젝트 단위의 독립적인 의존성 관리를 가능하게 했다.

### go.mod 파일의 구조

`go.mod` 파일은 다음과 같은 섹션으로 구성된다. module 지시문은 프로젝트의 모듈 경로를 정의하며, 이는 Go 패키지의 고유 식별자 역할을 한다. go 지시문은 프로젝트가 요구하는 최소 Go 버전을 명시한다. require 지시문은 직접 의존하는 외부 모듈들을 나열하고, indirect 주석으로 표시된 것들은 의존 모듈의 의존성을 의미한다. exclude와 replace 지시문으로는 특정 버전을 제외하거나 다른 버전으로 대체할 수 있다.

게임 서버 프로젝트의 `go.mod` 파일 예시를 살펴보자.

```
module gameserver.example.com/gameserver

go 1.25

require (
    gameserver.example.com/network v1.0.0
    gameserver.example.com/gamelogic v1.0.0
)
```

### go.sum 파일의 역할

`go.sum` 파일은 모듈 의존성의 암호화 해시값을 저장하여 의존성의 무결성을 보장한다. 같은 모듈을 다운로드할 때마다 해시값을 검증함으로써 악의적인 수정이나 손상을 감지할 수 있다.

```
gameserver.example.com/network v1.0.0 h1:abcd1234...
gameserver.example.com/network v1.0.0/go.mod h1:efgh5678...
gameserver.example.com/gamelogic v1.0.0 h1:ijkl9012...
gameserver.example.com/gamelogic v1.0.0/go.mod h1:mnop3456...
```

각 모듈마다 두 개의 해시가 생성된다. 첫 번째는 모듈 전체에 대한 해시이고, 두 번째는 `go.mod` 파일만의 해시다. 이렇게 분리하는 이유는 모듈의 `go.mod` 파일만 변경되었을 때도 감지할 수 있기 때문이다.

### go.mod 파일 검증 및 정리

프로젝트 개발 중에 불필요한 의존성이 누적될 수 있다. 다음 명령어들로 `go.mod`를 정리할 수 있다.

```bash
go mod tidy
```

이 명령어는 사용되지 않는 의존성을 제거하고, 누락된 의존성을 추가하며, 버전 정보를 최신으로 업데이트한다.

```bash
go mod verify
```

이 명령어는 현재 저장된 모듈들의 해시값이 유효한지 검증한다. 만약 해시값이 일치하지 않으면 의존성이 변조되었음을 의미한다.

```bash
go mod graph
```

이 명령어는 의존성 그래프를 출력하여 각 모듈 간의 의존 관계를 시각화할 수 있다.

다음은 게임 서버 프로젝트의 의존성 구조를 나타내는 다이어그램이다.

```
gameserver (main application)
    ├── network (로컬 모듈)
    │   └── golang.org/x/sys (외부 의존성)
    └── gamelogic (로컬 모듈)
        └── network (공통 의존성)
```

## 17.2 로컬 모듈 참조 (replace 지시문)

게임 서버 개발에서 네트워크 라이브러리와 게임 로직을 로컬 디렉토리에서 관리할 때, `replace` 지시문을 사용하여 원격 저장소 대신 로컬 경로를 참조할 수 있다. 이는 개발 중에 여러 모듈을 동시에 수정해야 할 때 매우 유용하다.

### replace 지시문의 기본 사용법

로컬 모듈을 참조하려면 `go.mod` 파일에 `replace` 지시문을 추가한다.

```
module gameserver.example.com/gameserver

go 1.25

require (
    gameserver.example.com/network v1.0.0
    gameserver.example.com/gamelogic v1.0.0
)

replace (
    gameserver.example.com/network => ../network
    gameserver.example.com/gamelogic => ../gamelogic
)
```

`replace` 지시문의 왼쪽 화살표 앞은 모듈 이름과 버전(생략 가능)이고, 오른쪽은 실제 경로다. 상대 경로를 사용할 때는 `../` 형식으로 표현하며, 절대 경로도 가능하다. 버전을 명시하지 않으면 require 지시문의 버전이 사용된다.

### 프로젝트 구조 설계

실제 개발 환경을 예시로 살펴보자.

```
workspace/
├── gameserver/
│   ├── go.mod
│   ├── go.sum
│   ├── main.go
│   └── cmd/
│       └── server/
│           └── main.go
├── network/
│   ├── go.mod
│   ├── go.sum
│   ├── listener.go
│   ├── session.go
│   └── handler.go
└── gamelogic/
    ├── go.mod
    ├── go.sum
    ├── poker.go
    ├── player.go
    └── room.go
```

이러한 구조에서 `gameserver` 디렉토리의 `go.mod` 파일은 다음과 같이 작성된다.

```
module gameserver.example.com/gameserver

go 1.25

require (
    gameserver.example.com/network v1.0.0
    gameserver.example.com/gamelogic v1.0.0
)

replace (
    gameserver.example.com/network => ../network
    gameserver.example.com/gamelogic => ../gamelogic
)
```

각 로컬 모듈도 자신의 `go.mod` 파일을 가져야 한다. `network/go.mod`의 예시다.

```
module gameserver.example.com/network

go 1.25

require golang.org/x/sys v0.20.0
```

### replace 지시문의 고급 활용

여러 버전을 테스트할 때 replace 지시문으로 동시에 여러 경로를 참조할 수 있다. 예를 들어 네트워크 라이브러리의 실험적 버전을 테스트하고 싶다면 다음과 같이 작성한다.

```
replace gameserver.example.com/network v1.0.0 => ../network-v1
replace gameserver.example.com/network v2.0.0-beta => ../network-v2-experimental
```

또한 외부 모듈을 로컬로 수정할 때도 replace를 사용할 수 있다. 예를 들어 타사 라이브러리에 버그가 있어서 자신의 환경에서만 패치를 적용해야 한다면 다음과 같이 할 수 있다.

```
require github.com/third-party/lib v1.2.3

replace github.com/third-party/lib v1.2.3 => ./patches/third-party-lib
```

### replace 지시문의 주의사항

`replace` 지시문은 개발 환경에서만 사용하는 것이 권장된다. 라이브러리를 다른 프로젝트에서 사용할 때는 replace 지시문을 제거하거나 주석 처리해야 한다. 왜냐하면 다른 프로젝트가 나의 로컬 경로를 참조할 수 없기 때문이다.

라이브러리를 배포할 때 체크리스트다.

- `replace` 지시문을 모두 제거했는가
- `go.mod`와 `go.sum`이 일관성 있게 업데이트되었는가
- 모든 의존성이 공개 저장소에서 다운로드 가능한가

다음 예제 코드는 로컬 네트워크 라이브러리를 사용하는 게임 서버의 메인 함수다.

```go
package main

import (
	"fmt"
	"gameserver.example.com/gamelogic"
	"gameserver.example.com/network"
	"log"
)

func main() {
	// 네트워크 리스너 생성 (로컬 모듈 사용)
	listener, err := network.NewListener(":8080")
	if err != nil {
		log.Fatalf("Failed to create listener: %v", err)
	}
	defer listener.Close()

	// 게임 로직 초기화 (로컬 모듈 사용)
	gameManager := gamelogic.NewGameManager()
	
	fmt.Println("Game server started on port 8080")
	fmt.Printf("Network module version: %s\n", network.Version)
	fmt.Printf("GameLogic module version: %s\n", gamelogic.Version)

	// 네트워크 이벤트와 게임 로직을 연결
	listener.RegisterHandler(func(sessionID string, packet []byte) {
		gameManager.ProcessPacket(sessionID, packet)
	})

	// 서버 실행
	if err := listener.Listen(); err != nil {
		log.Fatalf("Server error: %v", err)
	}
}
```

`network/version.go` 파일에서 버전을 정의한다.

```go
package network

const Version = "1.0.0"
```

`gamelogic/version.go` 파일에서도 마찬가지로 정의한다.

```go
package gamelogic

const Version = "1.0.0"
```

이렇게 구성하면 develop 브랜치에서 네트워크 라이브러리와 게임 로직을 동시에 수정하면서도 메인 게임 서버에서 최신 변경사항을 자동으로 반영할 수 있다.

## 17.3 모듈 버전 관리

Go 모듈은 semantic versioning(유의적 버전)을 따른다. 버전은 `MAJOR.MINOR.PATCH` 형식으로 표현된다. MAJOR는 호환되지 않는 변경사항, MINOR는 하위 호환되는 기능 추가, PATCH는 버그 수정을 의미한다.

### 버전 관리 전략

네트워크 라이브러리의 개발 단계를 예시로 설명한다. 초기 개발 단계에서는 v0.1.0부터 시작하고, 기능이 충분히 안정화되면 v1.0.0을 릴리스한다.

```
v0.1.0 - 초기 Alpha 버전 (기본 TCP 리스너)
v0.2.0 - Beta 버전 (세션 관리 추가)
v0.3.0 - 안정화 (에러 처리 개선)
v1.0.0 - 첫 번째 안정 릴리스
v1.1.0 - 하트비트 기능 추가
v1.1.1 - 메모리 누수 버그 수정
v2.0.0 - 주요 API 변경
```

### 주요 버전 변경 (Major Version)

주요 버전이 변경되면 하위 호환성이 보장되지 않는다. Go에서는 주요 버전이 변경되면 모듈 경로 뒤에 버전을 붙인다.

v1 API 예시:

```go
package network

type Listener struct {
	addr string
}

func NewListener(addr string) *Listener {
	return &Listener{addr: addr}
}

func (l *Listener) Listen() error {
	// 구현
	return nil
}
```

v2로 업그레이드하면서 API를 큰 폭으로 변경했다면 `go.mod`는 다음과 같이 수정된다.

```
module gameserver.example.com/network/v2

go 1.25

require golang.org/x/sys v0.20.0
```

사용자 입장에서는 두 버전을 동시에 사용할 수도 있다.

```go
import (
	netv1 "gameserver.example.com/network"
	netv2 "gameserver.example.com/network/v2"
)

func main() {
	// v1 사용
	listener1 := netv1.NewListener(":8080")
	
	// v2 사용 (다른 API)
	listener2 := netv2.NewListener(":8081")
}
```

### 의존성 업그레이드

특정 모듈을 최신 버전으로 업그레이드하려면 다음 명령어를 사용한다.

```bash
go get gameserver.example.com/network@latest
```

특정 버전으로 다운그레이드할 수도 있다.

```bash
go get gameserver.example.com/network@v1.1.0
```

모든 의존성을 한 번에 업그레이드하려면 다음과 같이 한다.

```bash
go get -u ./...
```

### 버전 관리 워크플로우

실제 게임 서버 프로젝트에서의 버전 관리 과정을 보여주는 시나리오다.

네트워크 라이브러리에서 v1.0.0을 릴리스한 상태다.

```go
// network/listener.go v1.0.0
package network

type Listener struct {
	addr string
}

func (l *Listener) Listen() error {
	// TCP 리스닝
	return nil
}
```

새로운 기능인 TLS 지원이 추가되어 v1.1.0을 릴리스했다.

```go
// network/listener.go v1.1.0
package network

type Listener struct {
	addr string
	tlsConfig *tls.Config
}

func (l *Listener) SetTLSConfig(config *tls.Config) {
	l.tlsConfig = config
}

func (l *Listener) Listen() error {
	// TLS 지원 추가
	return nil
}
```

게임 서버에서 이를 업그레이드한다.

```bash
cd gameserver
go get gameserver.example.com/network@v1.1.0
```

`go.mod` 파일이 자동으로 업데이트된다.

```
require gameserver.example.com/network v1.1.0
```

게임 서버 코드에서 새로운 기능을 사용할 수 있다.

```go
package main

import (
	"crypto/tls"
	"gameserver.example.com/network"
)

func main() {
	listener := network.NewListener(":8080")
	
	// v1.1.0에서 새로 추가된 기능 사용
	tlsConfig := &tls.Config{
		// TLS 설정
	}
	listener.SetTLSConfig(tlsConfig)
	
	listener.Listen()
}
```

## 17.4 프라이빗 모듈 구성

게임 회사의 내부 네트워크 라이브러리와 같이 공개하지 않고 회사 내에서만 사용하는 모듈을 프라이빗 모듈이라고 한다. Go는 프라이빗 모듈을 사용할 수 있도록 여러 메커니즘을 제공한다.

### GOPRIVATE 환경변수 설정

프라이빗 모듈의 경로를 지정하려면 GOPRIVATE 환경변수를 설정한다.

Windows 환경:

```bash
set GOPRIVATE=gameserver.example.com/network,gameserver.example.com/gamelogic
```

Linux/macOS 환경:

```bash
export GOPRIVATE=gameserver.example.com/network,gameserver.example.com/gamelogic
```

GOPRIVATE 환경변수를 설정하면 Go는 프라이빗 모듈에 대해 다음과 같이 동작한다. 공개 모듈 저장소(proxy.golang.org 등)를 조회하지 않으며, 모듈의 체크섬을 sum.golang.org에서 검증하지 않는다. 대신 Git 저장소 또는 로컬 경로에서 직접 다운로드한다.

### 프라이빗 Git 저장소에서 모듈 다운로드

회사의 Git 서버에 호스팅된 프라이빗 모듈을 사용할 때는 SSH 인증 설정이 필요하다.

`~/.gitconfig` 파일을 설정한다.

```
[url "ssh://git@gitserver.example.com/"]
    insteadOf = https://gitserver.example.com/
```

SSH 키를 설정한다.

```bash
ssh-keygen -t rsa -C "your_email@example.com"
ssh-add ~/.ssh/id_rsa
```

그 후 `go.mod` 파일에서 프라이빗 모듈을 참조한다.

```
module gameserver.example.com/gameserver

go 1.25

require gameserver.example.com/network v1.0.0

replace gameserver.example.com/network v1.0.0 => git::ssh://git@gitserver.example.com/gameserver/network.git#v1.0.0
```

또는 더 간단하게 로컬 경로를 참조할 수 있다.

```
replace gameserver.example.com/network => /internal/network
```

### 회사 내부 모듈 저장소 구성

여러 프로젝트에서 공유할 수 있는 프라이빗 모듈을 관리하려면 다음과 같은 디렉토리 구조를 추천한다.

```
company-modules/
├── network/
│   ├── go.mod
│   ├── go.sum
│   ├── listener.go
│   └── session.go
├── gamelogic/
│   ├── go.mod
│   ├── go.sum
│   ├── poker.go
│   └── room.go
└── utils/
    ├── go.mod
    ├── go.sum
    ├── logger.go
    └── config.go
```

각 모듈의 `go.mod` 파일에서 회사의 도메인을 모듈 경로로 사용한다.

```
module internal.company.com/network

go 1.25
```

```
module internal.company.com/gamelogic

go 1.25

require internal.company.com/network v1.0.0
```

### 프라이빗 모듈 사용 예제

게임 서버 프로젝트에서 프라이빗 모듈을 사용하는 전체 예시를 보여준다.

먼저 Windows 10에서 환경변수를 설정한다. PowerShell을 관리자 권한으로 실행하여 다음을 입력한다.

```powershell
[Environment]::SetEnvironmentVariable("GOPRIVATE", "internal.company.com/*", "User")
```

프로젝트 구조다.

```
D:\gameserver\
├── server\
│   ├── go.mod
│   ├── main.go
│   └── server.go
└── modules\
    ├── network\
    │   ├── go.mod
    │   ├── listener.go
    │   └── handler.go
    └── gamelogic\
        ├── go.mod
        ├── poker.go
        └── room.go
```

`server/go.mod`:

```
module internal.company.com/gameserver

go 1.25

require (
	internal.company.com/network v1.0.0
	internal.company.com/gamelogic v1.0.0
)

replace (
	internal.company.com/network => ../modules/network
	internal.company.com/gamelogic => ../modules/gamelogic
)
```

`modules/network/go.mod`:

```
module internal.company.com/network

go 1.25
```

`modules/network/listener.go`:

```go
package network

import (
	"fmt"
	"net"
)

type Listener struct {
	addr string
	ln   net.Listener
}

func NewListener(addr string) *Listener {
	return &Listener{addr: addr}
}

func (l *Listener) Listen() error {
	var err error
	l.ln, err = net.Listen("tcp", l.addr)
	if err != nil {
		return err
	}
	
	fmt.Printf("Listening on %s\n", l.addr)
	return nil
}

func (l *Listener) Accept() (net.Conn, error) {
	return l.ln.Accept()
}

func (l *Listener) Close() error {
	if l.ln != nil {
		return l.ln.Close()
	}
	return nil
}
```

`modules/gamelogic/go.mod`:

```
module internal.company.com/gamelogic

go 1.25

require internal.company.com/network v1.0.0
```

`modules/gamelogic/poker.go`:

```go
package gamelogic

import (
	"internal.company.com/network"
)

type PokerGame struct {
	listener *network.Listener
}

func NewPokerGame(listener *network.Listener) *PokerGame {
	return &PokerGame{listener: listener}
}

func (pg *PokerGame) Start() {
	// 게임 로직 시작
}
```

`server/main.go`:

```go
package main

import (
	"internal.company.com/gamelogic"
	"internal.company.com/network"
	"log"
)

func main() {
	listener := network.NewListener(":8080")
	if err := listener.Listen(); err != nil {
		log.Fatalf("Failed to listen: %v", err)
	}
	defer listener.Close()

	game := gamelogic.NewPokerGame(listener)
	game.Start()

	// 클라이언트 연결 수락
	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Printf("Accept error: %v", err)
			continue
		}
		go handleConnection(conn)
	}
}

func handleConnection(conn net.Conn) {
	defer conn.Close()
	// 연결 처리
}
```

## 17.5 멀티 모듈 워크스페이스 (go.work)

Go 1.18부터 도입된 워크스페이스 기능(`go.work`)을 사용하면 여러 모듈을 하나의 워크스페이스로 관리할 수 있다. 이는 로컬 개발 중에 여러 모듈을 동시에 수정할 때 매우 편리하다.

### go.work 파일의 개념

`go.work` 파일은 `go.mod` 파일과 유사하게 워크스페이스의 설정을 정의한다. `go.work` 파일이 있는 디렉토리 내의 모든 모듈들이 하나의 워크스페이스를 형성한다.

```
workspace (root)/
├── go.work
├── gameserver/
│   ├── go.mod
│   └── main.go
├── network/
│   ├── go.mod
│   └── listener.go
└── gamelogic/
    ├── go.mod
    └── poker.go
```

### go.work 파일 생성

워크스페이스를 초기화한다. 워크스페이스의 루트 디렉토리에서 다음 명령어를 실행한다.

```bash
go work init
```

이 명령어는 빈 `go.work` 파일을 생성한다.

```
go 1.25
```

다음으로 각 모듈을 워크스페이스에 추가한다.

```bash
go work use ./gameserver ./network ./gamelogic
```

이 명령어는 `go.work` 파일을 다음과 같이 업데이트한다.

```
go 1.25

use (
	./gameserver
	./network
	./gamelogic
)
```

### go.work 워크플로우

멀티 모듈 워크스페이스에서 개발할 때의 워크플로우를 설명한다.

처음 상태에서 `gameserver/go.mod` 파일은 다음과 같다.

```
module gameserver.example.com/gameserver

go 1.25

require (
	gameserver.example.com/network v1.0.0
	gameserver.example.com/gamelogic v1.0.0
)

replace (
	gameserver.example.com/network => ../network
	gameserver.example.com/gamelogic => ../gamelogic
)
```

`go.work` 파일이 있으면 `go.work`의 설정이 `go.mod`의 replace 지시문을 오버라이드한다. 즉, replace 지시문을 명시하지 않아도 워크스페이스의 모듈들이 자동으로 로컬 버전으로 사용된다.

따라서 `go.mod` 파일을 다음과 같이 간단하게 유지할 수 있다.

```
module gameserver.example.com/gameserver

go 1.25

require (
	gameserver.example.com/network v1.0.0
	gameserver.example.com/gamelogic v1.0.0
)
```

replace 지시문을 제거해도 워크스페이스 내에서는 로컬 모듈을 사용한다.

### go.work 파일의 고급 기능

특정 모듈을 임시로 워크스페이스에서 제외할 수 있다. 예를 들어 네트워크 라이브러리를 원격 버전으로 테스트하고 싶다면 다음과 같이 `go.work` 파일을 수정한다.

```
go 1.25

use (
	./gameserver
	./gamelogic
	// ./network를 제외하고 원격 버전 사용
)
```

이제 gameserver는 원격에서 v1.0.0 버전의 network를 다운로드하여 사용한다.

go.work 파일에서 특정 버전을 명시적으로 사용하는 것도 가능하다.

```
go 1.25

use (
	./gameserver
	./network
	./gamelogic
)

replace gameserver.example.com/network v1.0.0 => ./network-patch
```

### 워크스페이스 내 의존성 해결

워크스페이스 내에서 build, test, run 명령어를 실행할 때 Go는 자동으로 워크스페이스의 모듈들을 사용한다. 어느 모듈 디렉토리에서 명령어를 실행하든 같은 결과를 얻는다.

gameserver 디렉토리에서 빌드한다.

```bash
cd gameserver
go build ./cmd/server
```

network 디렉토리에서 테스트한다.

```bash
cd network
go test ./...
```

워크스페이스의 모든 모듈을 한 번에 빌드할 수도 있다.

```bash
go build ./...
```

### 실전 예제: 포커 게임 서버 워크스페이스

게임 서버 프로젝트의 전체 구조를 보여주는 실전 예제다.

워크스페이스 디렉토리 구조:

```
poker-game-workspace/
├── go.work
├── go.sum
├── gameserver/
│   ├── go.mod
│   ├── main.go
│   ├── server/
│   │   └── server.go
│   └── cmd/
│       └── main/
│           └── main.go
├── network/
│   ├── go.mod
│   ├── listener.go
│   ├── session.go
│   ├── handler.go
│   └── packet.go
└── gamelogic/
    ├── go.mod
    ├── poker/
    │   ├── game.go
    │   ├── player.go
    │   └── card.go
    └── room/
        ├── manager.go
        └── room.go
```

`go.work` 파일:

```
go 1.25

use (
	./gameserver
	./network
	./gamelogic
)
```

`gameserver/go.mod`:

```
module gameserver.example.com/gameserver

go 1.25

require (
	gameserver.example.com/network v1.0.0
	gameserver.example.com/gamelogic v1.0.0
)
```

`network/go.mod`:

```
module gameserver.example.com/network

go 1.25

require golang.org/x/sys v0.20.0
```

`gamelogic/go.mod`:

```
module gameserver.example.com/gamelogic

go 1.25

require gameserver.example.com/network v1.0.0
```

`network/listener.go`:

```go
package network

import (
	"fmt"
	"net"
	"sync"
)

type Listener struct {
	addr     string
	ln       net.Listener
	sessions map[string]*Session
	mu       sync.RWMutex
}

func NewListener(addr string) *Listener {
	return &Listener{
		addr:     addr,
		sessions: make(map[string]*Session),
	}
}

func (l *Listener) Listen() error {
	var err error
	l.ln, err = net.Listen("tcp", l.addr)
	if err != nil {
		return err
	}

	fmt.Printf("Network listener started on %s\n", l.addr)
	return nil
}

func (l *Listener) Accept() (net.Conn, error) {
	return l.ln.Accept()
}

func (l *Listener) RegisterSession(id string, session *Session) {
	l.mu.Lock()
	defer l.mu.Unlock()
	l.sessions[id] = session
}

func (l *Listener) UnregisterSession(id string) {
	l.mu.Lock()
	defer l.mu.Unlock()
	delete(l.sessions, id)
}

func (l *Listener) Close() error {
	if l.ln != nil {
		return l.ln.Close()
	}
	return nil
}

func (l *Listener) SessionCount() int {
	l.mu.RLock()
	defer l.mu.RUnlock()
	return len(l.sessions)
}
```

`network/session.go`:

```go
package network

import (
	"net"
	"sync"
)

type Session struct {
	ID       string
	Conn     net.Conn
	WriteCh  chan []byte
	ClosedCh chan struct{}
	mu       sync.Mutex
}

func NewSession(id string, conn net.Conn) *Session {
	return &Session{
		ID:       id,
		Conn:     conn,
		WriteCh:  make(chan []byte, 100),
		ClosedCh: make(chan struct{}),
	}
}

func (s *Session) Write(data []byte) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	_, err := s.Conn.Write(data)
	return err
}

func (s *Session) Close() error {
	return s.Conn.Close()
}
```

`gamelogic/go.mod`:

```
module gameserver.example.com/gamelogic

go 1.25

require gameserver.example.com/network v1.0.0
```

`gamelogic/poker/game.go`:

```go
package poker

import (
	"fmt"
	"gameserver.example.com/network"
	"sync"
)

type PokerGame struct {
	id      string
	players map[string]*Player
	mu      sync.RWMutex
	state   GameState
}

type GameState string

const (
	StateWaiting GameState = "waiting"
	StateRunning GameState = "running"
	StateEnded   GameState = "ended"
)

type Player struct {
	ID      string
	Session *network.Session
	Hand    []Card
	Chips   int
	Status  PlayerStatus
}

type PlayerStatus string

const (
	PlayerActive PlayerStatus = "active"
	PlayerFolded PlayerStatus = "folded"
	PlayerLeft   PlayerStatus = "left"
)

type Card struct {
	Suit   string
	Rank   string
	Value  int
}

func NewPokerGame(id string) *PokerGame {
	return &PokerGame{
		id:      id,
		players: make(map[string]*Player),
		state:   StateWaiting,
	}
}

func (pg *PokerGame) AddPlayer(id string, session *network.Session) error {
	pg.mu.Lock()
	defer pg.mu.Unlock()

	if len(pg.players) >= 6 {
		return fmt.Errorf("game is full")
	}

	pg.players[id] = &Player{
		ID:      id,
		Session: session,
		Chips:   1000,
		Status:  PlayerActive,
	}

	return nil
}

func (pg *PokerGame) RemovePlayer(id string) {
	pg.mu.Lock()
	defer pg.mu.Unlock()

	delete(pg.players, id)
}

func (pg *PokerGame) GetPlayerCount() int {
	pg.mu.RLock()
	defer pg.mu.RUnlock()
	return len(pg.players)
}

func (pg *PokerGame) Start() error {
	pg.mu.Lock()
	defer pg.mu.Unlock()

	if len(pg.players) < 2 {
		return fmt.Errorf("not enough players to start")
	}

	pg.state = StateRunning
	fmt.Printf("Poker game %s started with %d players\n", pg.id, len(pg.players))
	return nil
}

func (pg *PokerGame) BroadcastMessage(msg string) {
	pg.mu.RLock()
	defer pg.mu.RUnlock()

	for _, player := range pg.players {
		if player.Status != PlayerLeft {
			player.Session.Write([]byte(msg))
		}
	}
}
```

`gamelogic/room/manager.go`:

```go
package room

import (
	"fmt"
	"gameserver.example.com/gamelogic/poker"
	"gameserver.example.com/network"
	"sync"
)

type RoomManager struct {
	rooms map[string]*Room
	mu    sync.RWMutex
}

type Room struct {
	ID   string
	Game *poker.PokerGame
}

func NewRoomManager() *RoomManager {
	return &RoomManager{
		rooms: make(map[string]*Room),
	}
}

func (rm *RoomManager) CreateRoom(id string) *Room {
	rm.mu.Lock()
	defer rm.mu.Unlock()

	room := &Room{
		ID:   id,
		Game: poker.NewPokerGame(id),
	}
	rm.rooms[id] = room
	fmt.Printf("Room %s created\n", id)
	return room
}

func (rm *RoomManager) GetRoom(id string) (*Room, error) {
	rm.mu.RLock()
	defer rm.mu.RUnlock()

	room, exists := rm.rooms[id]
	if !exists {
		return nil, fmt.Errorf("room not found")
	}
	return room, nil
}

func (rm *RoomManager) DeleteRoom(id string) {
	rm.mu.Lock()
	defer rm.mu.Unlock()

	delete(rm.rooms, id)
	fmt.Printf("Room %s deleted\n", id)
}

func (rm *RoomManager) JoinRoom(roomID string, playerID string, session *network.Session) error {
	room, err := rm.GetRoom(roomID)
	if err != nil {
		return err
	}

	return room.Game.AddPlayer(playerID, session)
}

func (rm *RoomManager) LeaveRoom(roomID string, playerID string) error {
	room, err := rm.GetRoom(roomID)
	if err != nil {
		return err
	}

	room.Game.RemovePlayer(playerID)
	
	if room.Game.GetPlayerCount() == 0 {
		rm.DeleteRoom(roomID)
	}

	return nil
}

func (rm *RoomManager) RoomCount() int {
	rm.mu.RLock()
	defer rm.mu.RUnlock()
	return len(rm.rooms)
}
```

`gameserver/main.go`:

```go
package main

import (
	"fmt"
	"gameserver.example.com/gamelogic/room"
	"gameserver.example.com/network"
	"log"
	"sync"
)

var (
	listener      *network.Listener
	roomManager   *room.RoomManager
	sessionMutex  sync.RWMutex
	sessionMap    map[string]*network.Session
)

func init() {
	roomManager = room.NewRoomManager()
	sessionMap = make(map[string]*network.Session)
}

func main() {
	var err error
	listener, err = network.NewListener(":8080")
	if err != nil {
		log.Fatalf("Failed to create listener: %v", err)
	}

	if err := listener.Listen(); err != nil {
		log.Fatalf("Failed to listen: %v", err)
	}
	defer listener.Close()

	fmt.Println("=== Poker Game Server Started ===")
	fmt.Println("Network module: gameserver.example.com/network")
	fmt.Println("GameLogic module: gameserver.example.com/gamelogic")
	fmt.Println("Listening on port 8080")
	fmt.Println("===================================")

	// 클라이언트 연결 수락
	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Printf("Accept error: %v", err)
			continue
		}
		go handleConnection(conn)
	}
}

func handleConnection(conn net.Conn) {
	sessionID := generateSessionID()
	session := network.NewSession(sessionID, conn)
	
	sessionMutex.Lock()
	sessionMap[sessionID] = session
	sessionMutex.Unlock()
	
	listener.RegisterSession(sessionID, session)

	fmt.Printf("Player connected: %s (Total: %d)\n", sessionID, listener.SessionCount())

	defer func() {
		session.Close()
		listener.UnregisterSession(sessionID)
		
		sessionMutex.Lock()
		delete(sessionMap, sessionID)
		sessionMutex.Unlock()

		fmt.Printf("Player disconnected: %s\n", sessionID)
	}()

	// 클라이언트로부터 메시지 수신
	buffer := make([]byte, 1024)
	for {
		n, err := conn.Read(buffer)
		if err != nil {
			break
		}

		processMessage(sessionID, buffer[:n])
	}
}

func processMessage(sessionID string, data []byte) {
	// 메시지 처리 로직
	fmt.Printf("Message from %s: %s\n", sessionID, string(data))
}

func generateSessionID() string {
	return fmt.Sprintf("player_%d", len(sessionMap)+1)
}
```

이 예제에서 워크스페이스는 세 개의 모듈을 관리한다. 워크스페이스의 `go.work` 파일이 각 모듈의 require 지시문을 자동으로 해결해준다. 개발자는 어느 디렉토리에서든 `go build ./...`를 실행하면 전체 프로젝트가 빌드된다.

워크스페이스의 가장 큰 장점은 여러 모듈을 동시에 개발할 때 변경사항이 즉시 반영된다는 것이다. 예를 들어 network 모듈의 Listener 인터페이스를 수정하면 gameserver와 gamelogic에서 즉시 그 변경사항을 사용할 수 있다.

워크스페이스 내에서 모듈들 간의 의존성 순환이 발생하지 않도록 주의해야 한다. 위의 예제에서 gamelogic이 network에 의존하지만, network는 gamelogic에 의존하지 않는다. 이러한 단방향 의존성 구조를 유지하는 것이 중요하다.

Go 모듈 시스템의 고급 기능들을 제대로 이해하고 활용하면 대규모 게임 서버 프로젝트를 체계적으로 관리할 수 있다. 다음 장에서는 이러한 모듈 구조 위에 실제 네트워크 라이브러리를 구현하는 방법을 배운다.



# Chapter 18. 네트워크 라이브러리 설계

게임 서버 개발에서 네트워크 라이브러리는 재사용 가능하고 확장 가능한 형태로 설계되어야 한다. 이 장에서는 소켓 기반 게임 서버를 위한 네트워크 라이브러리의 전체 아키텍처를 설계하고, 각 컴포넌트의 역할과 인터페이스를 정의한다.

## 18.1 라이브러리 프로젝트 구조

네트워크 라이브러리는 게임 로직과 완전히 분리되어 독립적인 모듈로 관리된다. 이를 통해 다른 게임 프로젝트에서도 재사용할 수 있다.

### 디렉토리 구조

```
C:\GameProjects\
├── network-lib\              # 네트워크 라이브러리 프로젝트
│   ├── go.mod
│   ├── session\              # 세션 관리
│   │   ├── session.go
│   │   └── manager.go
│   ├── packet\               # 패킷 처리
│   │   ├── handler.go
│   │   ├── encoder.go
│   │   └── decoder.go
│   ├── protocol\             # 프로토콜 정의
│   │   └── protocol.go
│   ├── logger\               # 로깅
│   │   └── logger.go
│   └── server\               # TCP 서버
│       ├── server.go
│       └── config.go
└── poker-game\               # 게임 서버 프로젝트
    ├── go.mod
    ├── go.work               # 워크스페이스 설정
    └── main.go
```

### 네트워크 라이브러리 모듈 초기화

먼저 `network-lib` 디렉토리에서 Go 모듈을 초기화한다.

```bash
cd C:\GameProjects\network-lib
go mod init github.com/yourusername/network-lib
```

실제로는 GitHub에 올리지 않더라도, 모듈 경로는 도메인 형식으로 작성하는 것이 관례다. 로컬에서만 사용할 경우에도 이런 형식을 유지한다.

### 라이브러리 아키텍처 개요

```
┌─────────────────────────────────────────────────┐
│              Game Server Application            │
│  (poker-game: 비즈니스 로직, 게임 규칙)         │
└────────────────┬────────────────────────────────┘
                 │ 사용
                 ▼
┌─────────────────────────────────────────────────┐
│         Network Library (network-lib)           │
├─────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐      │
│  │  Server  │  │ Session  │  │  Packet  │      │
│  │  Manager │  │ Manager  │  │ Handler  │      │
│  └──────────┘  └──────────┘  └──────────┘      │
│       │             │              │            │
│       └─────────────┴──────────────┘            │
│                     │                           │
│  ┌──────────────────▼─────────────────────┐    │
│  │     Protocol & Serialization           │    │
│  └────────────────────────────────────────┘    │
│                     │                           │
│  ┌──────────────────▼─────────────────────┐    │
│  │         TCP Socket Layer               │    │
│  └────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
```

네트워크 라이브러리는 크게 다섯 가지 계층으로 구성된다.

1. **TCP Socket Layer**: 저수준 네트워크 통신을 담당한다.
2. **Protocol Layer**: 패킷 형식과 직렬화/역직렬화를 처리한다.
3. **Session Manager**: 클라이언트 연결을 관리한다.
4. **Packet Handler**: 수신한 패킷을 처리하고 라우팅한다.
5. **Server Manager**: 전체 서버의 생명주기를 관리한다.

## 18.2 패킷 핸들러 인터페이스

패킷 핸들러는 게임 로직과 네트워크 라이브러리를 연결하는 핵심 인터페이스다. 이를 통해 네트워크 라이브러리는 게임 로직의 구체적인 내용을 알 필요 없이 패킷을 전달할 수 있다.

### 패킷 핸들러 인터페이스 설계

`network-lib/packet/handler.go` 파일을 생성한다.

```go
package packet

import "context"

// PacketID는 패킷의 고유 식별자 타입이다.
type PacketID uint16

// Packet은 모든 패킷이 구현해야 하는 기본 인터페이스다.
type Packet interface {
    // GetID는 패킷의 고유 ID를 반환한다.
    GetID() PacketID
    
    // Serialize는 패킷을 바이트 슬라이스로 직렬화한다.
    Serialize() ([]byte, error)
    
    // Deserialize는 바이트 슬라이스를 패킷 구조체로 역직렬화한다.
    Deserialize(data []byte) error
}

// Session은 클라이언트 연결 세션을 나타내는 인터페이스다.
// 이 인터페이스를 통해 패킷 핸들러가 세션에 응답을 보낼 수 있다.
type Session interface {
    // GetID는 세션의 고유 ID를 반환한다.
    GetID() uint64
    
    // Send는 패킷을 클라이언트로 전송한다.
    Send(packet Packet) error
    
    // Close는 세션을 종료한다.
    Close() error
    
    // GetUserData는 세션에 저장된 사용자 정의 데이터를 반환한다.
    GetUserData(key string) (interface{}, bool)
    
    // SetUserData는 세션에 사용자 정의 데이터를 저장한다.
    SetUserData(key string, value interface{})
}

// Handler는 특정 패킷을 처리하는 핸들러 함수 타입이다.
// ctx는 요청의 컨텍스트, session은 클라이언트 세션, packet은 수신한 패킷이다.
type Handler func(ctx context.Context, session Session, packet Packet) error

// Router는 패킷 ID에 따라 적절한 핸들러로 라우팅하는 인터페이스다.
type Router interface {
    // Register는 특정 패킷 ID에 대한 핸들러를 등록한다.
    Register(packetID PacketID, handler Handler)
    
    // Handle은 수신한 패킷을 처리한다.
    Handle(ctx context.Context, session Session, packet Packet) error
    
    // Unregister는 특정 패킷 ID의 핸들러를 제거한다.
    Unregister(packetID PacketID)
}
```

이 인터페이스 설계의 핵심은 다음과 같다.

**Packet 인터페이스**: 모든 패킷 타입이 구현해야 하는 메서드를 정의한다. 이를 통해 네트워크 라이브러리는 구체적인 패킷 타입을 몰라도 패킷을 처리할 수 있다.

**Session 인터페이스**: 게임 로직이 클라이언트와 통신할 수 있는 추상화 계층을 제공한다. 세션 상태 정보를 저장하고 조회할 수 있는 `GetUserData`/`SetUserData` 메서드도 포함한다.

**Handler 함수 타입**: 실제 패킷 처리 로직을 구현하는 함수의 시그니처를 정의한다. `context.Context`를 받아 타임아웃이나 취소를 지원한다.

**Router 인터페이스**: 패킷 ID와 핸들러 함수를 매핑하고, 수신한 패킷을 적절한 핸들러로 디스패치한다.

### 기본 Router 구현

`network-lib/packet/router.go` 파일을 생성한다.

```go
package packet

import (
    "context"
    "fmt"
    "sync"
)

// DefaultRouter는 Router 인터페이스의 기본 구현이다.
type DefaultRouter struct {
    handlers map[PacketID]Handler
    mu       sync.RWMutex
}

// NewDefaultRouter는 새로운 DefaultRouter 인스턴스를 생성한다.
func NewDefaultRouter() *DefaultRouter {
    return &DefaultRouter{
        handlers: make(map[PacketID]Handler),
    }
}

// Register는 패킷 ID에 핸들러를 등록한다.
// 이미 등록된 패킷 ID에 핸들러를 등록하면 기존 핸들러를 덮어쓴다.
func (r *DefaultRouter) Register(packetID PacketID, handler Handler) {
    r.mu.Lock()
    defer r.mu.Unlock()
    r.handlers[packetID] = handler
}

// Unregister는 패킷 ID의 핸들러를 제거한다.
func (r *DefaultRouter) Unregister(packetID PacketID) {
    r.mu.Lock()
    defer r.mu.Unlock()
    delete(r.handlers, packetID)
}

// Handle은 수신한 패킷을 적절한 핸들러로 라우팅한다.
// 등록되지 않은 패킷 ID의 경우 에러를 반환한다.
func (r *DefaultRouter) Handle(ctx context.Context, session Session, packet Packet) error {
    packetID := packet.GetID()
    
    r.mu.RLock()
    handler, exists := r.handlers[packetID]
    r.mu.RUnlock()
    
    if !exists {
        return fmt.Errorf("no handler registered for packet ID: %d", packetID)
    }
    
    // 핸들러 실행 중 발생한 패닉을 복구한다.
    defer func() {
        if err := recover(); err != nil {
            // 실제 프로덕션에서는 로거를 사용해야 한다.
            fmt.Printf("panic in packet handler (ID: %d): %v\n", packetID, err)
        }
    }()
    
    return handler(ctx, session, packet)
}
```

`DefaultRouter`는 `sync.RWMutex`를 사용하여 동시성을 안전하게 처리한다. 핸들러를 등록하거나 제거할 때는 쓰기 락을, 핸들러를 조회할 때는 읽기 락을 사용한다. 또한 핸들러 실행 중 패닉이 발생해도 서버가 종료되지 않도록 복구 로직을 포함한다.

## 18.3 연결 관리자 구현

세션 매니저는 모든 클라이언트 연결을 추적하고 관리한다. 브로드캐스트, 특정 세션 검색, 연결 종료 등의 기능을 제공한다.

### 세션 인터페이스 구현

`network-lib/session/session.go` 파일을 생성한다.

```go
package session

import (
    "fmt"
    "net"
    "sync"
    "sync/atomic"
    "time"
    
    "github.com/yourusername/network-lib/packet"
)

// TCPSession은 TCP 연결을 나타내는 세션 구현이다.
type TCPSession struct {
    id         uint64
    conn       net.Conn
    sendChan   chan packet.Packet
    closeChan  chan struct{}
    closeOnce  sync.Once
    userData   map[string]interface{}
    userDataMu sync.RWMutex
    isClosed   atomic.Bool
    
    // 마지막 활동 시간 (하트비트용)
    lastActiveTime atomic.Int64
}

// NewTCPSession은 새로운 TCP 세션을 생성한다.
func NewTCPSession(id uint64, conn net.Conn) *TCPSession {
    session := &TCPSession{
        id:        id,
        conn:      conn,
        sendChan:  make(chan packet.Packet, 100), // 송신 버퍼 크기 100
        closeChan: make(chan struct{}),
        userData:  make(map[string]interface{}),
    }
    
    session.updateLastActiveTime()
    return session
}

// GetID는 세션의 고유 ID를 반환한다.
func (s *TCPSession) GetID() uint64 {
    return s.id
}

// Send는 패킷을 송신 큐에 추가한다.
// 세션이 닫힌 경우 에러를 반환한다.
func (s *TCPSession) Send(p packet.Packet) error {
    if s.isClosed.Load() {
        return fmt.Errorf("session %d is closed", s.id)
    }
    
    select {
    case s.sendChan <- p:
        return nil
    case <-time.After(3 * time.Second):
        return fmt.Errorf("send timeout for session %d", s.id)
    }
}

// Close는 세션을 종료한다.
// 여러 번 호출해도 안전하다.
func (s *TCPSession) Close() error {
    var err error
    s.closeOnce.Do(func() {
        s.isClosed.Store(true)
        close(s.closeChan)
        err = s.conn.Close()
    })
    return err
}

// IsClosed는 세션이 닫혔는지 확인한다.
func (s *TCPSession) IsClosed() bool {
    return s.isClosed.Load()
}

// GetUserData는 세션에 저장된 사용자 정의 데이터를 반환한다.
func (s *TCPSession) GetUserData(key string) (interface{}, bool) {
    s.userDataMu.RLock()
    defer s.userDataMu.RUnlock()
    value, exists := s.userData[key]
    return value, exists
}

// SetUserData는 세션에 사용자 정의 데이터를 저장한다.
func (s *TCPSession) SetUserData(key string, value interface{}) {
    s.userDataMu.Lock()
    defer s.userDataMu.Unlock()
    s.userData[key] = value
}

// GetRemoteAddr는 원격 클라이언트의 주소를 반환한다.
func (s *TCPSession) GetRemoteAddr() net.Addr {
    return s.conn.RemoteAddr()
}

// updateLastActiveTime은 마지막 활동 시간을 현재 시간으로 갱신한다.
func (s *TCPSession) updateLastActiveTime() {
    s.lastActiveTime.Store(time.Now().Unix())
}

// GetLastActiveTime은 마지막 활동 시간을 반환한다.
func (s *TCPSession) GetLastActiveTime() time.Time {
    return time.Unix(s.lastActiveTime.Load(), 0)
}

// GetSendChannel은 송신 채널을 반환한다.
// 이 채널은 내부적으로만 사용되어야 한다.
func (s *TCPSession) GetSendChannel() <-chan packet.Packet {
    return s.sendChan
}

// GetCloseChannel은 종료 채널을 반환한다.
// 이 채널은 세션이 종료될 때 닫힌다.
func (s *TCPSession) GetCloseChannel() <-chan struct{} {
    return s.closeChan
}

// GetConnection은 내부 TCP 연결을 반환한다.
// 이 메서드는 저수준 작업이 필요할 때만 사용한다.
func (s *TCPSession) GetConnection() net.Conn {
    return s.conn
}
```

`TCPSession`은 `packet.Session` 인터페이스를 구현한다. 주요 특징은 다음과 같다.

**비동기 송신**: `sendChan` 채널을 통해 패킷을 버퍼링하여 네트워크 I/O가 블로킹되지 않도록 한다.

**안전한 종료**: `closeOnce`를 사용하여 `Close()` 메서드가 여러 번 호출되어도 한 번만 실행되도록 보장한다.

**사용자 데이터**: 게임 로직에서 세션별로 필요한 정보(플레이어 정보, 방 정보 등)를 저장할 수 있다.

**하트비트 지원**: 마지막 활동 시간을 추적하여 비활성 세션을 감지할 수 있다.

### 세션 매니저 구현

`network-lib/session/manager.go` 파일을 생성한다.

```go
package session

import (
    "fmt"
    "sync"
    "sync/atomic"
    
    "github.com/yourusername/network-lib/packet"
)

// Manager는 모든 세션을 관리하는 매니저다.
type Manager struct {
    sessions   map[uint64]*TCPSession
    mu         sync.RWMutex
    nextID     atomic.Uint64
    onConnect  func(session packet.Session)
    onDisconnect func(session packet.Session)
}

// NewManager는 새로운 세션 매니저를 생성한다.
func NewManager() *Manager {
    return &Manager{
        sessions: make(map[uint64]*TCPSession),
    }
}

// SetOnConnect는 클라이언트 연결 시 호출될 콜백을 설정한다.
func (m *Manager) SetOnConnect(callback func(session packet.Session)) {
    m.onConnect = callback
}

// SetOnDisconnect는 클라이언트 연결 종료 시 호출될 콜백을 설정한다.
func (m *Manager) SetOnDisconnect(callback func(session packet.Session)) {
    m.onDisconnect = callback
}

// Add는 새로운 세션을 추가하고 고유 ID를 할당한다.
func (m *Manager) Add(session *TCPSession) {
    m.mu.Lock()
    m.sessions[session.GetID()] = session
    m.mu.Unlock()
    
    // 연결 콜백 호출
    if m.onConnect != nil {
        m.onConnect(session)
    }
}

// Remove는 세션을 제거한다.
func (m *Manager) Remove(sessionID uint64) {
    m.mu.Lock()
    session, exists := m.sessions[sessionID]
    if exists {
        delete(m.sessions, sessionID)
    }
    m.mu.Unlock()
    
    // 연결 종료 콜백 호출
    if exists && m.onDisconnect != nil {
        m.onDisconnect(session)
    }
}

// Get은 ID로 세션을 조회한다.
func (m *Manager) Get(sessionID uint64) (*TCPSession, bool) {
    m.mu.RLock()
    defer m.mu.RUnlock()
    session, exists := m.sessions[sessionID]
    return session, exists
}

// GetAll은 모든 세션을 반환한다.
// 반환된 슬라이스는 복사본이므로 안전하게 순회할 수 있다.
func (m *Manager) GetAll() []*TCPSession {
    m.mu.RLock()
    defer m.mu.RUnlock()
    
    sessions := make([]*TCPSession, 0, len(m.sessions))
    for _, session := range m.sessions {
        sessions = append(sessions, session)
    }
    return sessions
}

// Count는 현재 연결된 세션 수를 반환한다.
func (m *Manager) Count() int {
    m.mu.RLock()
    defer m.mu.RUnlock()
    return len(m.sessions)
}

// Broadcast는 모든 세션에 패킷을 전송한다.
// 실패한 세션의 ID 목록을 반환한다.
func (m *Manager) Broadcast(p packet.Packet) []uint64 {
    m.mu.RLock()
    defer m.mu.RUnlock()
    
    var failedSessions []uint64
    for id, session := range m.sessions {
        if err := session.Send(p); err != nil {
            failedSessions = append(failedSessions, id)
        }
    }
    return failedSessions
}

// BroadcastExcept는 특정 세션을 제외한 모든 세션에 패킷을 전송한다.
func (m *Manager) BroadcastExcept(p packet.Packet, excludeID uint64) []uint64 {
    m.mu.RLock()
    defer m.mu.RUnlock()
    
    var failedSessions []uint64
    for id, session := range m.sessions {
        if id == excludeID {
            continue
        }
        if err := session.Send(p); err != nil {
            failedSessions = append(failedSessions, id)
        }
    }
    return failedSessions
}

// CloseAll은 모든 세션을 종료한다.
func (m *Manager) CloseAll() {
    m.mu.Lock()
    defer m.mu.Unlock()
    
    for _, session := range m.sessions {
        session.Close()
    }
    m.sessions = make(map[uint64]*TCPSession)
}

// GenerateID는 새로운 세션 ID를 생성한다.
func (m *Manager) GenerateID() uint64 {
    return m.nextID.Add(1)
}

// Find는 조건을 만족하는 첫 번째 세션을 반환한다.
func (m *Manager) Find(predicate func(*TCPSession) bool) (*TCPSession, bool) {
    m.mu.RLock()
    defer m.mu.RUnlock()
    
    for _, session := range m.sessions {
        if predicate(session) {
            return session, true
        }
    }
    return nil, false
}

// Filter는 조건을 만족하는 모든 세션을 반환한다.
func (m *Manager) Filter(predicate func(*TCPSession) bool) []*TCPSession {
    m.mu.RLock()
    defer m.mu.RUnlock()
    
    var result []*TCPSession
    for _, session := range m.sessions {
        if predicate(session) {
            result = append(result, session)
        }
    }
    return result
}
```

세션 매니저는 세션의 생명주기를 관리하고 브로드캐스트 같은 집합 연산을 제공한다. `Find`와 `Filter` 메서드를 통해 특정 조건의 세션을 검색할 수 있어, 예를 들어 특정 방에 있는 플레이어들에게만 메시지를 보내는 것이 가능하다.

## 18.4 패킷 인코더/디코더

패킷 인코더와 디코더는 네트워크를 통해 전송되는 바이트 스트림과 구조화된 패킷 객체 사이를 변환한다.

### 프로토콜 정의

먼저 패킷의 기본 구조를 정의한다. `network-lib/protocol/protocol.go` 파일을 생성한다.

```go
package protocol

import (
    "encoding/binary"
    "fmt"
)

// 패킷 구조:
// +--------+--------+----------+
// | Size   | ID     | Body     |
// | 4bytes | 2bytes | N bytes  |
// +--------+--------+----------+

const (
    // HeaderSize는 패킷 헤더 크기다 (Size 4bytes + ID 2bytes).
    HeaderSize = 6
    
    // MaxPacketSize는 허용되는 최대 패킷 크기다 (1MB).
    MaxPacketSize = 1024 * 1024
    
    // MinPacketSize는 최소 패킷 크기다 (헤더만 있는 경우).
    MinPacketSize = HeaderSize
)

// Header는 패킷 헤더 구조체다.
type Header struct {
    Size uint32 // 전체 패킷 크기 (헤더 포함)
    ID   uint16 // 패킷 ID
}

// EncodeHeader는 헤더를 바이트 슬라이스로 인코딩한다.
func EncodeHeader(header Header) []byte {
    buf := make([]byte, HeaderSize)
    binary.BigEndian.PutUint32(buf[0:4], header.Size)
    binary.BigEndian.PutUint16(buf[4:6], header.ID)
    return buf
}

// DecodeHeader는 바이트 슬라이스에서 헤더를 디코딩한다.
func DecodeHeader(data []byte) (Header, error) {
    if len(data) < HeaderSize {
        return Header{}, fmt.Errorf("invalid header size: %d", len(data))
    }
    
    header := Header{
        Size: binary.BigEndian.Uint32(data[0:4]),
        ID:   binary.BigEndian.Uint16(data[4:6]),
    }
    
    // 패킷 크기 검증
    if header.Size < MinPacketSize || header.Size > MaxPacketSize {
        return Header{}, fmt.Errorf("invalid packet size: %d", header.Size)
    }
    
    return header, nil
}

// GetBodySize는 패킷 바디의 크기를 반환한다.
func (h Header) GetBodySize() uint32 {
    if h.Size < HeaderSize {
        return 0
    }
    return h.Size - HeaderSize
}
```

이 프로토콜은 간단하면서도 효율적이다. 빅 엔디안(Big Endian)을 사용하여 네트워크 바이트 순서를 따르며, 패킷 크기 제한을 통해 메모리 공격을 방지한다.

### 패킷 인코더 구현

`network-lib/packet/encoder.go` 파일을 생성한다.

```go
package packet

import (
    "github.com/yourusername/network-lib/protocol"
)

// Encoder는 패킷을 바이트 스트림으로 인코딩한다.
type Encoder struct{}

// NewEncoder는 새로운 인코더를 생성한다.
func NewEncoder() *Encoder {
    return &Encoder{}
}

// Encode는 패킷을 바이트 슬라이스로 인코딩한다.
// 반환되는 바이트 슬라이스는 헤더와 바디를 모두 포함한다.
func (e *Encoder) Encode(p Packet) ([]byte, error) {
    // 패킷 바디를 직렬화한다.
    body, err := p.Serialize()
    if err != nil {
        return nil, err
    }
    
    // 헤더를 생성한다.
    header := protocol.Header{
        Size: uint32(protocol.HeaderSize + len(body)),
        ID:   uint16(p.GetID()),
    }
    
    // 헤더를 인코딩한다.
    headerBytes := protocol.EncodeHeader(header)
    
    // 헤더와 바디를 결합한다.
    result := make([]byte, len(headerBytes)+len(body))
    copy(result, headerBytes)
    copy(result[len(headerBytes):], body)
    
    return result, nil
}
```

인코더는 패킷 객체를 받아 헤더와 바디를 결합한 완전한 바이트 배열을 생성한다. 이 바이트 배열은 TCP 소켓을 통해 직접 전송될 수 있다.

### 패킷 디코더 구현

`network-lib/packet/decoder.go` 파일을 생성한다.

```go
package packet

import (
    "bufio"
    "fmt"
    "io"
    
    "github.com/yourusername/network-lib/protocol"
)

// Decoder는 바이트 스트림에서 패킷을 디코딩한다.
type Decoder struct {
    reader *bufio.Reader
}

// NewDecoder는 새로운 디코더를 생성한다.
func NewDecoder(reader io.Reader) *Decoder {
    return &Decoder{
        reader: bufio.NewReaderSize(reader, 4096),
    }
}

// Decode는 스트림에서 다음 패킷을 읽어 디코딩한다.
// PacketID와 바디 데이터를 반환한다.
func (d *Decoder) Decode() (PacketID, []byte, error) {
    // 헤더를 읽는다.
    headerBytes := make([]byte, protocol.HeaderSize)
    if _, err := io.ReadFull(d.reader, headerBytes); err != nil {
        return 0, nil, fmt.Errorf("failed to read header: %w", err)
    }
    
    // 헤더를 디코딩한다.
    header, err := protocol.DecodeHeader(headerBytes)
    if err != nil {
        return 0, nil, fmt.Errorf("failed to decode header: %w", err)
    }
    
    // 바디를 읽는다.
    bodySize := header.GetBodySize()
    body := make([]byte, bodySize)
    if bodySize > 0 {
        if _, err := io.ReadFull(d.reader, body); err != nil {
            return 0, nil, fmt.Errorf("failed to read body: %w", err)
        }
    }
    
    return PacketID(header.ID), body, nil
}

// Reset은 디코더의 내부 버퍼를 리셋하고 새로운 리더를 설정한다.
func (d *Decoder) Reset(reader io.Reader) {
    d.reader.Reset(reader)
}
```

디코더는 `io.Reader`를 받아 완전한 패킷을 읽을 때까지 블로킹한다. `bufio.Reader`를 사용하여 작은 읽기 작업들을 버퍼링함으로써 성능을 최적화한다.

### 패킷 팩토리 패턴

패킷 ID로부터 적절한 패킷 인스턴스를 생성하는 팩토리가 필요하다. `network-lib/packet/factory.go` 파일을 생성한다.

```go
package packet

import (
    "fmt"
    "sync"
)

// PacketFactory는 패킷 ID로부터 패킷 인스턴스를 생성한다.
type PacketFactory interface {
    // Create는 패킷 ID에 해당하는 새로운 패킷 인스턴스를 생성한다.
    Create(packetID PacketID) (Packet, error)
}

// PacketCreator는 패킷을 생성하는 함수 타입이다.
type PacketCreator func() Packet

// DefaultPacketFactory는 PacketFactory의 기본 구현이다.
type DefaultPacketFactory struct {
    creators map[PacketID]PacketCreator
    mu       sync.RWMutex
}

// NewDefaultPacketFactory는 새로운 패킷 팩토리를 생성한다.
func NewDefaultPacketFactory() *DefaultPacketFactory {
    return &DefaultPacketFactory{
        creators: make(map[PacketID]PacketCreator),
    }
}

// Register는 패킷 ID에 대한 생성자를 등록한다.
func (f *DefaultPacketFactory) Register(packetID PacketID, creator PacketCreator) {
    f.mu.Lock()
    defer f.mu.Unlock()
    f.creators[packetID] = creator
}

// Create는 패킷 ID에 해당하는 새로운 패킷 인스턴스를 생성한다.
func (f *DefaultPacketFactory) Create(packetID PacketID) (Packet, error) {
    f.mu.RLock()
    creator, exists := f.creators[packetID]
    f.mu.RUnlock()
    
    if !exists {
        return nil, fmt.Errorf("unknown packet ID: %d", packetID)
    }
    
    return creator(), nil
}

// Unregister는 패킷 ID의 생성자를 제거한다.
func (f *DefaultPacketFactory) Unregister(packetID PacketID) {
    f.mu.Lock()
    defer f.mu.Unlock()
    delete(f.creators, packetID)
}
```

패킷 팩토리는 등록-생성 패턴을 사용한다. 게임 서버 시작 시 모든 패킷 타입을 팩토리에 등록하면, 런타임에 패킷 ID만으로 적절한 타입의 인스턴스를 생성할 수 있다.

## 18.5 에러 처리 전략

네트워크 라이브러리에서 에러 처리는 매우 중요하다. 명확한 에러 타입과 일관된 처리 전략이 필요하다.

### 커스텀 에러 타입 정의

`network-lib/errors/errors.go` 파일을 생성한다.

```go
package errors

import (
    "errors"
    "fmt"
)

// 에러 타입 정의
var (
    // ErrSessionClosed는 세션이 이미 닫혔을 때 발생한다.
    ErrSessionClosed = errors.New("session is closed")
    
    // ErrInvalidPacket은 패킷 형식이 잘못되었을 때 발생한다.
    ErrInvalidPacket = errors.New("invalid packet format")
    
    // ErrPacketTooLarge는 패킷 크기가 제한을 초과했을 때 발생한다.
    ErrPacketTooLarge = errors.New("packet size exceeds limit")
    
    // ErrTimeout은 작업이 타임아웃되었을 때 발생한다.
    ErrTimeout = errors.New("operation timeout")
    
    // ErrServerClosed는 서버가 종료되었을 때 발생한다.
    ErrServerClosed = errors.New("server is closed")
)

// NetworkError는 네트워크 관련 에러를 나타낸다.
type NetworkError struct {
    Op  string // 작업 이름 (예: "read", "write")
    Err error  // 원본 에러
}

// Error는 에러 메시지를 반환한다.
func (e *NetworkError) Error() string {
    return fmt.Sprintf("network error during %s: %v", e.Op, e.Err)
}

// Unwrap은 원본 에러를 반환한다.
func (e *NetworkError) Unwrap() error {
    return e.Err
}

// Is는 에러 비교를 지원한다.
func (e *NetworkError) Is(target error) bool {
    return errors.Is(e.Err, target)
}

// PacketError는 패킷 처리 관련 에러를 나타낸다.
type PacketError struct {
    PacketID uint16 // 패킷 ID
    Err      error  // 원본 에러
}

// Error는 에러 메시지를 반환한다.
func (e *PacketError) Error() string {
    return fmt.Sprintf("packet error (ID: %d): %v", e.PacketID, e.Err)
}

// Unwrap은 원본 에러를 반환한다.
func (e *PacketError) Unwrap() error {
    return e.Err
}
```

커스텀 에러 타입을 정의함으로써 에러의 컨텍스트를 명확히 전달할 수 있다. Go 1.13 이상의 에러 랩핑 기능을 활용하여 에러 체인을 유지한다.

### 에러 처리 가이드라인

네트워크 라이브러리에서 에러 처리는 다음 원칙을 따른다.

**복구 가능한 에러**: 일시적인 네트워크 오류나 타임아웃은 재시도 로직으로 처리한다.

**복구 불가능한 에러**: 프로토콜 위반이나 잘못된 패킷 형식은 세션을 종료한다.

**로깅**: 모든 에러는 적절한 로그 레벨로 기록한다. 네트워크 에러는 INFO, 프로토콜 위반은 WARN, 예상치 못한 에러는 ERROR로 기록한다.

**에러 전파**: 호출자가 에러를 적절히 처리할 수 있도록 컨텍스트와 함께 에러를 반환한다.

## 18.6 로깅과 디버깅

네트워크 라이브러리는 독립적인 로깅 인터페이스를 제공하여 게임 서버가 원하는 로깅 시스템을 사용할 수 있도록 한다.

### 로거 인터페이스 정의

`network-lib/logger/logger.go` 파일을 생성한다.

```go
package logger

import (
    "fmt"
    "log"
    "os"
)

// Level은 로그 레벨을 나타낸다.
type Level int

const (
    LevelDebug Level = iota
    LevelInfo
    LevelWarn
    LevelError
)

// String은 로그 레벨의 문자열 표현을 반환한다.
func (l Level) String() string {
    switch l {
    case LevelDebug:
        return "DEBUG"
    case LevelInfo:
        return "INFO"
    case LevelWarn:
        return "WARN"
    case LevelError:
        return "ERROR"
    default:
        return "UNKNOWN"
    }
}

// Logger는 로깅 인터페이스다.
type Logger interface {
    Debug(format string, args ...interface{})
    Info(format string, args ...interface{})
    Warn(format string, args ...interface{})
    Error(format string, args ...interface{})
    SetLevel(level Level)
}

// DefaultLogger는 Logger 인터페이스의 기본 구현이다.
type DefaultLogger struct {
    logger *log.Logger
    level  Level
}

// NewDefaultLogger는 새로운 기본 로거를 생성한다.
func NewDefaultLogger() *DefaultLogger {
    return &DefaultLogger{
        logger: log.New(os.Stdout, "", log.LstdFlags|log.Lmicroseconds),
        level:  LevelInfo,
    }
}

// SetLevel은 로그 레벨을 설정한다.
func (l *DefaultLogger) SetLevel(level Level) {
    l.level = level
}

// Debug는 디버그 레벨 로그를 출력한다.
func (l *DefaultLogger) Debug(format string, args ...interface{}) {
    if l.level <= LevelDebug {
        l.log(LevelDebug, format, args...)
    }
}

// Info는 정보 레벨 로그를 출력한다.
func (l *DefaultLogger) Info(format string, args ...interface{}) {
    if l.level <= LevelInfo {
        l.log(LevelInfo, format, args...)
    }
}

// Warn은 경고 레벨 로그를 출력한다.
func (l *DefaultLogger) Warn(format string, args ...interface{}) {
    if l.level <= LevelWarn {
        l.log(LevelWarn, format, args...)
    }
}

// Error는 에러 레벨 로그를 출력한다.
func (l *DefaultLogger) Error(format string, args ...interface{}) {
    if l.level <= LevelError {
        l.log(LevelError, format, args...)
    }
}

// log는 실제 로그를 출력하는 내부 메서드다.
func (l *DefaultLogger) log(level Level, format string, args ...interface{}) {
    message := fmt.Sprintf(format, args...)
    l.logger.Printf("[%s] %s", level.String(), message)
}

// NoOpLogger는 아무것도 출력하지 않는 로거다.
// 테스트나 로깅이 필요 없는 환경에서 사용한다.
type NoOpLogger struct{}

// NewNoOpLogger는 새로운 NoOp 로거를 생성한다.
func NewNoOpLogger() *NoOpLogger {
    return &NoOpLogger{}
}

func (l *NoOpLogger) Debug(format string, args ...interface{}) {}
func (l *NoOpLogger) Info(format string, args ...interface{})  {}
func (l *NoOpLogger) Warn(format string, args ...interface{})  {}
func (l *NoOpLogger) Error(format string, args ...interface{}) {}
func (l *NoOpLogger) SetLevel(level Level)                     {}
```

로거 인터페이스를 사용하면 게임 서버가 자체 로깅 시스템(예: Zap, Logrus)을 구현하여 주입할 수 있다. 또한 단위 테스트에서는 `NoOpLogger`를 사용하여 로그 출력을 억제할 수 있다.

### 디버깅 헬퍼

`network-lib/logger/debug.go` 파일을 생성한다.

```go
package logger

import (
    "fmt"
    "runtime"
    "strings"
)

// GetCaller는 호출자의 파일 이름과 라인 번호를 반환한다.
// skip은 스택에서 건너뛸 프레임 수다.
func GetCaller(skip int) string {
    _, file, line, ok := runtime.Caller(skip + 1)
    if !ok {
        return "unknown"
    }
    
    // 전체 경로에서 파일 이름만 추출
    parts := strings.Split(file, "/")
    if len(parts) > 0 {
        file = parts[len(parts)-1]
    }
    
    return fmt.Sprintf("%s:%d", file, line)
}

// DumpStack은 현재 고루틴의 스택 트레이스를 반환한다.
func DumpStack() string {
    buf := make([]byte, 4096)
    n := runtime.Stack(buf, false)
    return string(buf[:n])
}

// LogPanic은 패닉을 로깅하고 복구한다.
func LogPanic(logger Logger) {
    if r := recover(); r != nil {
        stack := DumpStack()
        logger.Error("panic recovered: %v\nStack trace:\n%s", r, stack)
    }
}
```

디버깅 헬퍼는 문제 발생 시 컨텍스트를 빠르게 파악할 수 있도록 도와준다. 특히 `LogPanic`은 고루틴에서 발생한 패닉을 안전하게 처리하고 로깅할 수 있게 한다.

---

## 정리

이 장에서는 게임 서버를 위한 네트워크 라이브러리의 핵심 구조를 설계했다. 주요 컴포넌트는 다음과 같다.

```
네트워크 라이브러리 아키텍처
├── 패킷 시스템
│   ├── Packet 인터페이스
│   ├── PacketFactory (패킷 생성)
│   ├── Encoder (직렬화)
│   └── Decoder (역직렬화)
├── 세션 관리
│   ├── Session 인터페이스
│   ├── TCPSession (구현)
│   └── SessionManager (관리)
├── 라우팅
│   ├── Handler 함수 타입
│   └── Router (핸들러 매핑)
├── 에러 처리
│   └── 커스텀 에러 타입
└── 로깅
    └── Logger 인터페이스
```

이러한 설계는 다음과 같은 이점을 제공한다.

**재사용성**: 네트워크 라이브러리를 여러 게임 프로젝트에서 재사용할 수 있다.

**확장성**: 인터페이스 기반 설계로 기능을 쉽게 확장할 수 있다.

**테스트 용이성**: 인터페이스를 통해 Mock 객체를 주입하여 단위 테스트를 작성할 수 있다.

**유지보수성**: 각 컴포넌트의 책임이 명확하게 분리되어 있다.

다음 장에서는 게임 로직 모듈을 네트워크 라이브러리와 분리하는 방법을 다룬다. 의존성 역전 원칙을 적용하여 두 모듈 간의 결합도를 최소화하는 전략을 배운다.



# Chapter 19. 게임 로직 모듈 분리

게임 서버 개발에서 비즈니스 로직과 네트워크 계층을 분리하는 것은 매우 중요하다. 이 장에서는 네트워크 라이브러리와 게임 로직을 독립적인 모듈로 분리하고, 의존성 역전 원칙을 적용하여 유지보수가 용이하고 테스트 가능한 구조를 만드는 방법을 다룬다.

## 19.1 게임 로직 프로젝트 구조

게임 로직 모듈은 네트워크 라이브러리와 완전히 독립적인 프로젝트로 구성된다. 이를 통해 게임 로직을 네트워크 구현과 분리하여 테스트하고 재사용할 수 있다.

### 전체 프로젝트 구조

```
C:\GameProjects\
├── network-lib\                    # 네트워크 라이브러리
│   ├── go.mod
│   ├── session\
│   ├── packet\
│   ├── protocol\
│   ├── logger\
│   └── server\
│
├── poker-game\                      # 포커 게임 서버 (메인 프로젝트)
│   ├── go.mod
│   ├── go.work                      # 워크스페이스 설정
│   ├── main.go                      # 서버 진입점
│   ├── config\                      # 설정 관리
│   │   └── config.go
│   ├── handler\                     # 패킷 핸들러
│   │   ├── login_handler.go
│   │   ├── room_handler.go
│   │   └── game_handler.go
│   └── server\                      # 서버 초기화 및 관리
│       └── server.go
│
└── poker-logic\                     # 포커 게임 로직 (독립 모듈)
    ├── go.mod
    ├── card\                        # 카드 관련 로직
    │   ├── card.go
    │   ├── deck.go
    │   └── hand_evaluator.go
    ├── player\                      # 플레이어 관리
    │   └── player.go
    ├── room\                        # 방 관리
    │   └── room.go
    ├── game\                        # 게임 로직
    │   ├── game.go
    │   ├── state_machine.go
    │   └── betting.go
    └── interfaces\                  # 인터페이스 정의
        └── interfaces.go
```

### 모듈 간 의존성 관계

```
┌─────────────────────────────────────────────────┐
│         poker-game (메인 서버)                  │
│  - 서버 초기화                                  │
│  - 패킷 핸들러 등록                             │
│  - 네트워크와 게임 로직 연결                    │
└────────────┬──────────────────┬─────────────────┘
             │                  │
             │ depends on       │ depends on
             ▼                  ▼
┌─────────────────────┐  ┌──────────────────────┐
│   network-lib       │  │   poker-logic        │
│  - TCP 통신         │  │  - 게임 규칙         │
│  - 세션 관리        │  │  - 카드 로직         │
│  - 패킷 처리        │  │  - 플레이어 관리     │
└─────────────────────┘  └──────────────────────┘
         ▲                        ▲
         │                        │
         └────────────┬───────────┘
                      │
              인터페이스를 통한
              느슨한 결합
```

이 구조에서 `poker-game`은 두 모듈을 사용하는 메인 서버다. `network-lib`와 `poker-logic`은 서로를 모르며, `poker-game`이 둘을 연결하는 역할을 한다.

### 게임 로직 모듈 초기화

먼저 `poker-logic` 디렉토리를 생성하고 모듈을 초기화한다.

```bash
cd C:\GameProjects
mkdir poker-logic
cd poker-logic
go mod init github.com/yourusername/poker-logic
```

`poker-logic/go.mod` 파일이 생성된다.

```go
module github.com/yourusername/poker-logic

go 1.25
```

게임 로직 모듈은 외부 의존성을 최소화한다. 필요한 경우 표준 라이브러리만 사용하거나, 데이터 구조나 알고리즘 관련 라이브러리만 의존한다.

## 19.2 비즈니스 로직과 네트워크 분리

비즈니스 로직과 네트워크를 분리하는 핵심 원칙은 **게임 로직이 네트워크를 알아서는 안 된다**는 것이다. 게임 로직은 순수한 비즈니스 규칙만 처리하고, 네트워크 통신은 상위 계층에서 담당한다.

### 계층 분리 원칙

```
┌─────────────────────────────────────────────────┐
│  Presentation Layer (네트워크 계층)             │
│  - 패킷 수신/송신                               │
│  - 프로토콜 변환                                │
│  - 세션 관리                                    │
└────────────────┬────────────────────────────────┘
                 │ 호출
                 ▼
┌─────────────────────────────────────────────────┐
│  Application Layer (애플리케이션 계층)          │
│  - 패킷 라우팅                                  │
│  - 권한 검증                                    │
│  - 게임 로직 호출                               │
└────────────────┬────────────────────────────────┘
                 │ 호출
                 ▼
┌─────────────────────────────────────────────────┐
│  Business Logic Layer (비즈니스 로직)           │
│  - 게임 규칙 적용                               │
│  - 상태 전이                                    │
│  - 결과 계산                                    │
└─────────────────────────────────────────────────┘
```

### 잘못된 설계 예시

다음은 게임 로직이 네트워크에 의존하는 잘못된 예시다.

```go
// ❌ 나쁜 예: 게임 로직이 네트워크를 직접 참조한다.
package game

import (
    "github.com/yourusername/network-lib/session"
    "github.com/yourusername/network-lib/packet"
)

type PokerGame struct {
    players map[uint64]*session.TCPSession  // 네트워크 세션에 의존
}

func (g *PokerGame) DealCards() {
    // 카드를 나눠주고 직접 네트워크로 전송
    for _, sess := range g.players {
        cardPacket := &packet.CardDealPacket{...}
        sess.Send(cardPacket)  // 게임 로직이 네트워크를 직접 호출
    }
}
```

이 설계의 문제점은 다음과 같다.

- 게임 로직이 네트워크 구현에 강하게 결합되어 있다.
- 네트워크 없이 게임 로직을 테스트할 수 없다.
- 다른 네트워크 라이브러리로 교체하려면 게임 로직도 수정해야 한다.

### 올바른 설계 예시

게임 로직은 순수한 데이터와 함수로만 구성되어야 한다.

```go
// ✅ 좋은 예: 게임 로직은 네트워크를 모른다.
package game

type Player struct {
    ID        string
    Name      string
    Chips     int
    Cards     []Card
    Position  int
}

type PokerGame struct {
    players    []*Player
    deck       *Deck
    communityCards []Card
    currentBet int
    pot        int
}

// DealCards는 순수한 비즈니스 로직만 수행한다.
// 결과를 반환하고, 네트워크 전송은 상위 계층에서 처리한다.
func (g *PokerGame) DealCards() []DealResult {
    results := make([]DealResult, 0)
    
    for _, player := range g.players {
        cards := g.deck.Draw(2)
        player.Cards = cards
        
        results = append(results, DealResult{
            PlayerID: player.ID,
            Cards:    cards,
        })
    }
    
    return results
}

// DealResult는 카드 분배 결과를 나타낸다.
type DealResult struct {
    PlayerID string
    Cards    []Card
}
```

이 설계에서 `DealCards` 메서드는 카드 분배 로직만 수행하고 결과를 반환한다. 네트워크 전송은 상위 계층(애플리케이션 계층)에서 처리한다.

```go
// 애플리케이션 계층에서 게임 로직과 네트워크를 연결
func handleStartGame(ctx context.Context, sess packet.Session, p packet.Packet) error {
    // 1. 게임 로직 실행
    game := getGame(sess)
    results := game.DealCards()
    
    // 2. 결과를 네트워크 패킷으로 변환하여 전송
    for _, result := range results {
        cardPacket := convertToPacket(result)
        targetSession := findSession(result.PlayerID)
        targetSession.Send(cardPacket)
    }
    
    return nil
}
```

## 19.3 의존성 역전 원칙 적용

의존성 역전 원칙(Dependency Inversion Principle, DIP)은 고수준 모듈이 저수준 모듈에 의존하지 않고, 둘 다 추상화에 의존해야 한다는 원칙이다. 게임 서버에서는 게임 로직이 네트워크나 데이터베이스 같은 인프라 계층에 의존하지 않도록 인터페이스를 사용한다.

### 의존성 역전 원칙 개념

```
전통적인 의존성 (잘못된 방법)
┌──────────────┐
│  Game Logic  │
└──────┬───────┘
       │ depends on
       ▼
┌──────────────┐
│   Network    │
└──────────────┘


의존성 역전 (올바른 방법)
┌──────────────┐        ┌──────────────┐
│  Game Logic  │───────>│  Interface   │
└──────────────┘        └──────┬───────┘
                               │ implements
                               ▼
                        ┌──────────────┐
                        │   Network    │
                        └──────────────┘
```

### 인터페이스 정의

게임 로직에 필요한 인터페이스를 정의한다. `poker-logic/interfaces/interfaces.go` 파일을 생성한다.

```go
package interfaces

// PlayerNotifier는 플레이어에게 알림을 보내는 인터페이스다.
// 게임 로직은 이 인터페이스를 통해 외부와 통신한다.
type PlayerNotifier interface {
    // NotifyCardDealt는 플레이어에게 카드가 분배되었음을 알린다.
    NotifyCardDealt(playerID string, cards []Card) error
    
    // NotifyTurnChanged는 턴이 변경되었음을 알린다.
    NotifyTurnChanged(playerID string, timeLimit int) error
    
    // NotifyBettingAction은 베팅 액션을 알린다.
    NotifyBettingAction(playerID string, action BettingAction, amount int) error
    
    // NotifyGameResult는 게임 결과를 알린다.
    NotifyGameResult(results []GameResult) error
}

// Card는 카드를 나타낸다.
type Card struct {
    Suit  string // "spades", "hearts", "diamonds", "clubs"
    Rank  string // "2", "3", ..., "K", "A"
}

// BettingAction은 베팅 액션 타입이다.
type BettingAction int

const (
    ActionFold BettingAction = iota
    ActionCheck
    ActionCall
    ActionRaise
    ActionAllIn
)

// GameResult는 게임 결과를 나타낸다.
type GameResult struct {
    PlayerID  string
    WinAmount int
    Hand      string // 패의 이름 (예: "Royal Flush")
    Cards     []Card
}

// PlayerRepository는 플레이어 정보를 저장하고 조회하는 인터페이스다.
type PlayerRepository interface {
    // GetPlayer는 플레이어 정보를 조회한다.
    GetPlayer(playerID string) (*PlayerInfo, error)
    
    // UpdateChips는 플레이어의 칩을 업데이트한다.
    UpdateChips(playerID string, chips int) error
    
    // GetPlayersByRoom은 특정 방의 플레이어 목록을 조회한다.
    GetPlayersByRoom(roomID string) ([]*PlayerInfo, error)
}

// PlayerInfo는 플레이어 정보를 나타낸다.
type PlayerInfo struct {
    ID    string
    Name  string
    Chips int
}

// Logger는 로깅 인터페이스다.
type Logger interface {
    Debug(format string, args ...interface{})
    Info(format string, args ...interface{})
    Warn(format string, args ...interface{})
    Error(format string, args ...interface{})
}
```

이 인터페이스들은 게임 로직이 외부 세계와 통신하는 유일한 수단이다. 게임 로직은 구체적인 구현을 모르며, 인터페이스만 사용한다.

### 게임 로직에서 인터페이스 사용

`poker-logic/game/game.go` 파일을 생성한다.

```go
package game

import (
    "fmt"
    "github.com/yourusername/poker-logic/interfaces"
)

// PokerGame은 포커 게임을 나타낸다.
type PokerGame struct {
    roomID    string
    players   []string  // 플레이어 ID 목록
    notifier  interfaces.PlayerNotifier  // 인터페이스에 의존
    repo      interfaces.PlayerRepository
    logger    interfaces.Logger
    
    // 게임 상태
    deck           *Deck
    communityCards []interfaces.Card
    pot            int
    currentBet     int
    currentPlayer  int
}

// NewPokerGame은 새로운 포커 게임을 생성한다.
// 의존성을 생성자를 통해 주입받는다 (Dependency Injection).
func NewPokerGame(
    roomID string,
    playerIDs []string,
    notifier interfaces.PlayerNotifier,
    repo interfaces.PlayerRepository,
    logger interfaces.Logger,
) *PokerGame {
    return &PokerGame{
        roomID:   roomID,
        players:  playerIDs,
        notifier: notifier,
        repo:     repo,
        logger:   logger,
        deck:     NewDeck(),
    }
}

// Start는 게임을 시작한다.
func (g *PokerGame) Start() error {
    g.logger.Info("게임 시작: 방 ID=%s, 플레이어 수=%d", g.roomID, len(g.players))
    
    // 덱 초기화 및 셔플
    g.deck.Shuffle()
    
    // 각 플레이어에게 카드 분배
    return g.dealInitialCards()
}

// dealInitialCards는 각 플레이어에게 초기 카드를 분배한다.
func (g *PokerGame) dealInitialCards() error {
    for _, playerID := range g.players {
        // 카드 2장 뽑기
        cards := g.deck.Draw(2)
        
        // 인터페이스를 통해 플레이어에게 알림
        if err := g.notifier.NotifyCardDealt(playerID, cards); err != nil {
            g.logger.Error("카드 분배 알림 실패: 플레이어=%s, 에러=%v", playerID, err)
            return fmt.Errorf("failed to notify card dealt: %w", err)
        }
        
        g.logger.Debug("카드 분배: 플레이어=%s, 카드 수=%d", playerID, len(cards))
    }
    
    return nil
}

// ProcessBetting은 베팅 라운드를 처리한다.
func (g *PokerGame) ProcessBetting(playerID string, action interfaces.BettingAction, amount int) error {
    // 베팅 로직 처리
    g.logger.Info("베팅 처리: 플레이어=%s, 액션=%d, 금액=%d", playerID, action, amount)
    
    // 액션에 따라 게임 상태 업데이트
    switch action {
    case interfaces.ActionFold:
        // 폴드 처리
        return g.handleFold(playerID)
    case interfaces.ActionCall:
        // 콜 처리
        return g.handleCall(playerID, amount)
    case interfaces.ActionRaise:
        // 레이즈 처리
        return g.handleRaise(playerID, amount)
    default:
        return fmt.Errorf("unknown action: %d", action)
    }
}

// handleCall은 콜 액션을 처리한다.
func (g *PokerGame) handleCall(playerID string, amount int) error {
    // 플레이어 정보 조회
    player, err := g.repo.GetPlayer(playerID)
    if err != nil {
        return fmt.Errorf("failed to get player: %w", err)
    }
    
    // 칩 검증
    if player.Chips < amount {
        return fmt.Errorf("insufficient chips: has %d, needs %d", player.Chips, amount)
    }
    
    // 칩 업데이트
    newChips := player.Chips - amount
    if err := g.repo.UpdateChips(playerID, newChips); err != nil {
        return fmt.Errorf("failed to update chips: %w", err)
    }
    
    // 팟에 추가
    g.pot += amount
    
    // 모든 플레이어에게 알림
    if err := g.notifier.NotifyBettingAction(playerID, interfaces.ActionCall, amount); err != nil {
        return fmt.Errorf("failed to notify betting action: %w", err)
    }
    
    return nil
}

// handleFold는 폴드 액션을 처리한다.
func (g *PokerGame) handleFold(playerID string) error {
    g.logger.Info("플레이어 폴드: %s", playerID)
    
    // 플레이어를 활성 목록에서 제거
    // (실제 구현에서는 별도의 상태 관리 필요)
    
    // 모든 플레이어에게 알림
    return g.notifier.NotifyBettingAction(playerID, interfaces.ActionFold, 0)
}

// handleRaise는 레이즈 액션을 처리한다.
func (g *PokerGame) handleRaise(playerID string, amount int) error {
    g.logger.Info("플레이어 레이즈: %s, 금액: %d", playerID, amount)
    
    // 레이즈 금액 검증 및 처리
    // (실제 구현 로직)
    
    g.currentBet = amount
    return g.notifier.NotifyBettingAction(playerID, interfaces.ActionRaise, amount)
}
```

이 설계의 핵심은 `PokerGame`이 구체적인 네트워크 구현이나 데이터베이스를 모른다는 것이다. 모든 외부 통신은 인터페이스를 통해 이루어진다. 이를 통해 다음과 같은 이점을 얻는다.

**테스트 용이성**: Mock 객체를 주입하여 네트워크 없이 게임 로직을 테스트할 수 있다.

**유연성**: 네트워크 구현을 변경해도 게임 로직은 수정할 필요가 없다.

**재사용성**: 같은 게임 로직을 웹소켓, gRPC 등 다른 네트워크 프로토콜에서 재사용할 수 있다.

## 19.4 인터페이스를 통한 결합도 낮추기

인터페이스를 효과적으로 사용하면 모듈 간 결합도를 크게 낮출 수 있다. 여기서는 실제 구현 예시를 통해 결합도를 낮추는 방법을 살펴본다.

### 어댑터 패턴 적용

네트워크 라이브러리와 게임 로직을 연결하는 어댑터를 만든다. 어댑터는 네트워크 세션을 게임 로직의 인터페이스로 변환한다.

`poker-game/adapter/notifier_adapter.go` 파일을 생성한다.

```go
package adapter

import (
    "fmt"
    
    netlib "github.com/yourusername/network-lib/packet"
    "github.com/yourusername/network-lib/session"
    gameif "github.com/yourusername/poker-logic/interfaces"
)

// PlayerNotifierAdapter는 네트워크 세션을 PlayerNotifier 인터페이스로 변환한다.
type PlayerNotifierAdapter struct {
    sessionManager *session.Manager
    packetFactory  PacketFactory  // 게임 결과를 네트워크 패킷으로 변환
}

// PacketFactory는 게임 데이터를 네트워크 패킷으로 변환하는 인터페이스다.
type PacketFactory interface {
    CreateCardDealPacket(playerID string, cards []gameif.Card) netlib.Packet
    CreateTurnChangePacket(playerID string, timeLimit int) netlib.Packet
    CreateBettingActionPacket(playerID string, action gameif.BettingAction, amount int) netlib.Packet
    CreateGameResultPacket(results []gameif.GameResult) netlib.Packet
}

// NewPlayerNotifierAdapter는 새로운 어댑터를 생성한다.
func NewPlayerNotifierAdapter(
    sessionManager *session.Manager,
    packetFactory PacketFactory,
) *PlayerNotifierAdapter {
    return &PlayerNotifierAdapter{
        sessionManager: sessionManager,
        packetFactory:  packetFactory,
    }
}

// NotifyCardDealt는 플레이어에게 카드 분배를 알린다.
func (a *PlayerNotifierAdapter) NotifyCardDealt(playerID string, cards []gameif.Card) error {
    // 플레이어 ID로 세션 찾기
    sess, found := a.findSessionByPlayerID(playerID)
    if !found {
        return fmt.Errorf("session not found for player: %s", playerID)
    }
    
    // 게임 데이터를 네트워크 패킷으로 변환
    packet := a.packetFactory.CreateCardDealPacket(playerID, cards)
    
    // 세션을 통해 패킷 전송
    return sess.Send(packet)
}

// NotifyTurnChanged는 턴 변경을 알린다.
func (a *PlayerNotifierAdapter) NotifyTurnChanged(playerID string, timeLimit int) error {
    sess, found := a.findSessionByPlayerID(playerID)
    if !found {
        return fmt.Errorf("session not found for player: %s", playerID)
    }
    
    packet := a.packetFactory.CreateTurnChangePacket(playerID, timeLimit)
    return sess.Send(packet)
}

// NotifyBettingAction은 베팅 액션을 모든 플레이어에게 브로드캐스트한다.
func (a *PlayerNotifierAdapter) NotifyBettingAction(
    playerID string,
    action gameif.BettingAction,
    amount int,
) error {
    packet := a.packetFactory.CreateBettingActionPacket(playerID, action, amount)
    
    // 모든 세션에 브로드캐스트
    failedSessions := a.sessionManager.Broadcast(packet)
    if len(failedSessions) > 0 {
        return fmt.Errorf("failed to send to %d sessions", len(failedSessions))
    }
    
    return nil
}

// NotifyGameResult는 게임 결과를 모든 플레이어에게 알린다.
func (a *PlayerNotifierAdapter) NotifyGameResult(results []gameif.GameResult) error {
    packet := a.packetFactory.CreateGameResultPacket(results)
    
    failedSessions := a.sessionManager.Broadcast(packet)
    if len(failedSessions) > 0 {
        return fmt.Errorf("failed to send to %d sessions", len(failedSessions))
    }
    
    return nil
}

// findSessionByPlayerID는 플레이어 ID로 세션을 찾는다.
// 세션의 UserData에 플레이어 ID가 저장되어 있다고 가정한다.
func (a *PlayerNotifierAdapter) findSessionByPlayerID(playerID string) (*session.TCPSession, bool) {
    sessions := a.sessionManager.GetAll()
    
    for _, sess := range sessions {
        storedID, exists := sess.GetUserData("playerID")
        if exists && storedID == playerID {
            return sess, true
        }
    }
    
    return nil, false
}
```

어댑터는 게임 로직의 인터페이스를 구현하면서, 내부적으로는 네트워크 라이브러리를 사용한다. 이를 통해 게임 로직과 네트워크 라이브러리는 서로를 모르면서도 통신할 수 있다.

### 리포지토리 어댑터

마찬가지로 데이터 저장소에 대한 어댑터도 만든다. `poker-game/adapter/repository_adapter.go` 파일을 생성한다.

```go
package adapter

import (
    "fmt"
    "sync"
    
    gameif "github.com/yourusername/poker-logic/interfaces"
)

// InMemoryPlayerRepository는 메모리 기반 플레이어 리포지토리 구현이다.
// 실제 프로덕션에서는 데이터베이스를 사용할 수 있다.
type InMemoryPlayerRepository struct {
    players map[string]*gameif.PlayerInfo
    mu      sync.RWMutex
}

// NewInMemoryPlayerRepository는 새로운 메모리 리포지토리를 생성한다.
func NewInMemoryPlayerRepository() *InMemoryPlayerRepository {
    return &InMemoryPlayerRepository{
        players: make(map[string]*gameif.PlayerInfo),
    }
}

// GetPlayer는 플레이어 정보를 조회한다.
func (r *InMemoryPlayerRepository) GetPlayer(playerID string) (*gameif.PlayerInfo, error) {
    r.mu.RLock()
    defer r.mu.RUnlock()
    
    player, exists := r.players[playerID]
    if !exists {
        return nil, fmt.Errorf("player not found: %s", playerID)
    }
    
    // 복사본을 반환하여 동시성 문제 방지
    return &gameif.PlayerInfo{
        ID:    player.ID,
        Name:  player.Name,
        Chips: player.Chips,
    }, nil
}

// UpdateChips는 플레이어의 칩을 업데이트한다.
func (r *InMemoryPlayerRepository) UpdateChips(playerID string, chips int) error {
    r.mu.Lock()
    defer r.mu.Unlock()
    
    player, exists := r.players[playerID]
    if !exists {
        return fmt.Errorf("player not found: %s", playerID)
    }
    
    if chips < 0 {
        return fmt.Errorf("invalid chips amount: %d", chips)
    }
    
    player.Chips = chips
    return nil
}

// GetPlayersByRoom은 특정 방의 플레이어 목록을 조회한다.
func (r *InMemoryPlayerRepository) GetPlayersByRoom(roomID string) ([]*gameif.PlayerInfo, error) {
    r.mu.RLock()
    defer r.mu.RUnlock()
    
    // 실제 구현에서는 roomID를 사용하여 필터링
    // 여기서는 간단히 모든 플레이어 반환
    players := make([]*gameif.PlayerInfo, 0, len(r.players))
    for _, player := range r.players {
        players = append(players, &gameif.PlayerInfo{
            ID:    player.ID,
            Name:  player.Name,
            Chips: player.Chips,
        })
    }
    
    return players, nil
}

// AddPlayer는 새로운 플레이어를 추가한다.
// 이 메서드는 인터페이스에는 없지만 초기화용으로 제공한다.
func (r *InMemoryPlayerRepository) AddPlayer(player *gameif.PlayerInfo) {
    r.mu.Lock()
    defer r.mu.Unlock()
    r.players[player.ID] = player
}
```

## 19.5 테스트 가능한 구조 만들기

인터페이스 기반 설계의 가장 큰 장점은 테스트 가능성이다. Mock 객체를 쉽게 만들 수 있어 게임 로직을 독립적으로 테스트할 수 있다.

### Mock 객체 구현

`poker-logic/game/game_test.go` 파일을 생성한다.

```go
package game

import (
    "testing"
    
    gameif "github.com/yourusername/poker-logic/interfaces"
)

// MockNotifier는 테스트용 Notifier 구현이다.
type MockNotifier struct {
    cardDealtCalls    []CardDealtCall
    turnChangedCalls  []TurnChangedCall
    bettingActionCalls []BettingActionCall
}

type CardDealtCall struct {
    PlayerID string
    Cards    []gameif.Card
}

type TurnChangedCall struct {
    PlayerID  string
    TimeLimit int
}

type BettingActionCall struct {
    PlayerID string
    Action   gameif.BettingAction
    Amount   int
}

func NewMockNotifier() *MockNotifier {
    return &MockNotifier{
        cardDealtCalls:     make([]CardDealtCall, 0),
        turnChangedCalls:   make([]TurnChangedCall, 0),
        bettingActionCalls: make([]BettingActionCall, 0),
    }
}

func (m *MockNotifier) NotifyCardDealt(playerID string, cards []gameif.Card) error {
    m.cardDealtCalls = append(m.cardDealtCalls, CardDealtCall{
        PlayerID: playerID,
        Cards:    cards,
    })
    return nil
}

func (m *MockNotifier) NotifyTurnChanged(playerID string, timeLimit int) error {
    m.turnChangedCalls = append(m.turnChangedCalls, TurnChangedCall{
        PlayerID:  playerID,
        TimeLimit: timeLimit,
    })
    return nil
}

func (m *MockNotifier) NotifyBettingAction(playerID string, action gameif.BettingAction, amount int) error {
    m.bettingActionCalls = append(m.bettingActionCalls, BettingActionCall{
        PlayerID: playerID,
        Action:   action,
        Amount:   amount,
    })
    return nil
}

func (m *MockNotifier) NotifyGameResult(results []gameif.GameResult) error {
    return nil
}

// MockRepository는 테스트용 Repository 구현이다.
type MockRepository struct {
    players map[string]*gameif.PlayerInfo
}

func NewMockRepository() *MockRepository {
    return &MockRepository{
        players: make(map[string]*gameif.PlayerInfo),
    }
}

func (m *MockRepository) GetPlayer(playerID string) (*gameif.PlayerInfo, error) {
    player, exists := m.players[playerID]
    if !exists {
        return nil, nil
    }
    return player, nil
}

func (m *MockRepository) UpdateChips(playerID string, chips int) error {
    if player, exists := m.players[playerID]; exists {
        player.Chips = chips
    }
    return nil
}

func (m *MockRepository) GetPlayersByRoom(roomID string) ([]*gameif.PlayerInfo, error) {
    result := make([]*gameif.PlayerInfo, 0)
    for _, player := range m.players {
        result = append(result, player)
    }
    return result, nil
}

// MockLogger는 테스트용 Logger 구현이다.
type MockLogger struct{}

func (m *MockLogger) Debug(format string, args ...interface{}) {}
func (m *MockLogger) Info(format string, args ...interface{})  {}
func (m *MockLogger) Warn(format string, args ...interface{})  {}
func (m *MockLogger) Error(format string, args ...interface{}) {}

// 테스트 함수
func TestPokerGame_Start(t *testing.T) {
    // Given: 테스트 환경 설정
    playerIDs := []string{"player1", "player2", "player3"}
    mockNotifier := NewMockNotifier()
    mockRepo := NewMockRepository()
    mockLogger := &MockLogger{}
    
    // 플레이어 추가
    for _, id := range playerIDs {
        mockRepo.players[id] = &gameif.PlayerInfo{
            ID:    id,
            Name:  "Player " + id,
            Chips: 1000,
        }
    }
    
    game := NewPokerGame("room1", playerIDs, mockNotifier, mockRepo, mockLogger)
    
    // When: 게임 시작
    err := game.Start()
    
    // Then: 결과 검증
    if err != nil {
        t.Fatalf("게임 시작 실패: %v", err)
    }
    
    // 모든 플레이어에게 카드가 분배되었는지 확인
    if len(mockNotifier.cardDealtCalls) != len(playerIDs) {
        t.Errorf("카드 분배 횟수 불일치: 예상=%d, 실제=%d",
            len(playerIDs), len(mockNotifier.cardDealtCalls))
    }
    
    // 각 플레이어가 2장의 카드를 받았는지 확인
    for i, call := range mockNotifier.cardDealtCalls {
        if len(call.Cards) != 2 {
            t.Errorf("플레이어 %d: 카드 수 불일치: 예상=2, 실제=%d",
                i, len(call.Cards))
        }
    }
}

func TestPokerGame_ProcessBetting_Call(t *testing.T) {
    // Given
    playerIDs := []string{"player1", "player2"}
    mockNotifier := NewMockNotifier()
    mockRepo := NewMockRepository()
    mockLogger := &MockLogger{}
    
    mockRepo.players["player1"] = &gameif.PlayerInfo{
        ID:    "player1",
        Name:  "Player 1",
        Chips: 1000,
    }
    
    game := NewPokerGame("room1", playerIDs, mockNotifier, mockRepo, mockLogger)
    
    // When: 플레이어가 100 칩을 콜
    err := game.ProcessBetting("player1", gameif.ActionCall, 100)
    
    // Then
    if err != nil {
        t.Fatalf("베팅 처리 실패: %v", err)
    }
    
    // 칩이 정확히 차감되었는지 확인
    player, _ := mockRepo.GetPlayer("player1")
    expectedChips := 900
    if player.Chips != expectedChips {
        t.Errorf("칩 차감 오류: 예상=%d, 실제=%d", expectedChips, player.Chips)
    }
    
    // 팟이 정확히 증가했는지 확인
    if game.pot != 100 {
        t.Errorf("팟 증가 오류: 예상=100, 실제=%d", game.pot)
    }
    
    // 베팅 액션 알림이 전송되었는지 확인
    if len(mockNotifier.bettingActionCalls) != 1 {
        t.Errorf("베팅 액션 알림 횟수 오류: 예상=1, 실제=%d",
            len(mockNotifier.bettingActionCalls))
    }
}

func TestPokerGame_ProcessBetting_InsufficientChips(t *testing.T) {
    // Given
    playerIDs := []string{"player1"}
    mockNotifier := NewMockNotifier()
    mockRepo := NewMockRepository()
    mockLogger := &MockLogger{}
    
    mockRepo.players["player1"] = &gameif.PlayerInfo{
        ID:    "player1",
        Name:  "Player 1",
        Chips: 50,  // 부족한 칩
    }
    
    game := NewPokerGame("room1", playerIDs, mockNotifier, mockRepo, mockLogger)
    
    // When: 보유한 것보다 많은 칩을 베팅 시도
    err := game.ProcessBetting("player1", gameif.ActionCall, 100)
    
    // Then: 에러가 발생해야 함
    if err == nil {
        t.Fatal("칩 부족 시 에러가 발생해야 함")
    }
}
```

이 테스트 코드는 네트워크 연결 없이도 게임 로직을 완벽하게 테스트한다. Mock 객체를 사용하여 외부 의존성을 제거하고, 순수한 비즈니스 로직만 검증한다.

### 테스트 실행

```bash
cd C:\GameProjects\poker-logic\game
go test -v
```

출력 예시:
```
=== RUN   TestPokerGame_Start
--- PASS: TestPokerGame_Start (0.00s)
=== RUN   TestPokerGame_ProcessBetting_Call
--- PASS: TestPokerGame_ProcessBetting_Call (0.00s)
=== RUN   TestPokerGame_ProcessBetting_InsufficientChips
--- PASS: TestPokerGame_ProcessBetting_InsufficientChips (0.00s)
PASS
ok      github.com/yourusername/poker-logic/game    0.234s
```

### 통합 테스트를 위한 테스트 더블

실제 네트워크 통신을 포함한 통합 테스트에서는 Fake 객체를 사용할 수 있다. `poker-game/adapter/fake_notifier.go` 파일을 생성한다.

```go
package adapter

import (
    "fmt"
    "sync"
    
    gameif "github.com/yourusername/poker-logic/interfaces"
)

// FakePlayerNotifier는 실제와 유사하게 동작하지만 네트워크를 사용하지 않는 Notifier다.
// 통합 테스트에서 사용된다.
type FakePlayerNotifier struct {
    notifications []Notification
    mu            sync.Mutex
}

type Notification struct {
    Type     string
    PlayerID string
    Data     interface{}
}

func NewFakePlayerNotifier() *FakePlayerNotifier {
    return &FakePlayerNotifier{
        notifications: make([]Notification, 0),
    }
}

func (f *FakePlayerNotifier) NotifyCardDealt(playerID string, cards []gameif.Card) error {
    f.mu.Lock()
    defer f.mu.Unlock()
    
    f.notifications = append(f.notifications, Notification{
        Type:     "CardDealt",
        PlayerID: playerID,
        Data:     cards,
    })
    
    fmt.Printf("[FAKE] 카드 분배 알림: 플레이어=%s, 카드 수=%d\n", playerID, len(cards))
    return nil
}

func (f *FakePlayerNotifier) NotifyTurnChanged(playerID string, timeLimit int) error {
    f.mu.Lock()
    defer f.mu.Unlock()
    
    f.notifications = append(f.notifications, Notification{
        Type:     "TurnChanged",
        PlayerID: playerID,
        Data:     timeLimit,
    })
    
    fmt.Printf("[FAKE] 턴 변경 알림: 플레이어=%s, 제한시간=%d\n", playerID, timeLimit)
    return nil
}

func (f *FakePlayerNotifier) NotifyBettingAction(playerID string, action gameif.BettingAction, amount int) error {
    f.mu.Lock()
    defer f.mu.Unlock()
    
    f.notifications = append(f.notifications, Notification{
        Type:     "BettingAction",
        PlayerID: playerID,
        Data: map[string]interface{}{
            "action": action,
            "amount": amount,
        },
    })
    
    fmt.Printf("[FAKE] 베팅 액션 알림: 플레이어=%s, 액션=%d, 금액=%d\n",
        playerID, action, amount)
    return nil
}

func (f *FakePlayerNotifier) NotifyGameResult(results []gameif.GameResult) error {
    f.mu.Lock()
    defer f.mu.Unlock()
    
    f.notifications = append(f.notifications, Notification{
        Type: "GameResult",
        Data: results,
    })
    
    fmt.Printf("[FAKE] 게임 결과 알림: 결과 수=%d\n", len(results))
    return nil
}

// GetNotifications는 저장된 모든 알림을 반환한다.
func (f *FakePlayerNotifier) GetNotifications() []Notification {
    f.mu.Lock()
    defer f.mu.Unlock()
    
    result := make([]Notification, len(f.notifications))
    copy(result, f.notifications)
    return result
}

// Clear는 저장된 알림을 모두 지운다.
func (f *FakePlayerNotifier) Clear() {
    f.mu.Lock()
    defer f.mu.Unlock()
    f.notifications = make([]Notification, 0)
}
```

---

## 정리

이 장에서는 게임 로직을 네트워크 계층과 완전히 분리하는 방법을 배웠다. 핵심 개념은 다음과 같다.

### 계층 분리

```
┌─────────────────────────────────────────┐
│  poker-game (메인 서버)                 │
│  - 패킷 핸들러                          │
│  - 어댑터 (네트워크 ↔ 게임 로직)        │
└───────────┬─────────────────────────────┘
            │
    ┌───────┴────────┐
    ▼                ▼
┌─────────────┐  ┌──────────────┐
│ network-lib │  │ poker-logic  │
│ (네트워크)  │  │ (게임 로직)  │
└─────────────┘  └──────────────┘
     ▲                  ▲
     │                  │
     └──────────────────┘
        인터페이스로 연결
        (느슨한 결합)
```

### 주요 설계 원칙

**단일 책임 원칙**: 각 모듈은 하나의 책임만 가진다. 게임 로직은 게임 규칙만, 네트워크 라이브러리는 통신만 담당한다.

**의존성 역전**: 고수준 모듈(게임 로직)이 저수준 모듈(네트워크)에 의존하지 않고, 둘 다 추상화(인터페이스)에 의존한다.

**인터페이스 분리**: 클라이언트가 사용하지 않는 메서드에 의존하지 않도록 인터페이스를 작게 분리한다.

**테스트 가능성**: Mock 객체를 쉽게 만들 수 있어 단위 테스트와 통합 테스트가 용이하다.

### 구현 패턴

**의존성 주입**: 생성자를 통해 의존성을 주입받아 결합도를 낮춘다.

**어댑터 패턴**: 서로 다른 인터페이스를 가진 모듈을 연결한다.

**리포지토리 패턴**: 데이터 접근 로직을 추상화한다.

다음 장에서는 포커 게임의 구체적인 설계를 다룬다. 텍사스 홀덤 룰, 게임 상태 머신, 프로토콜 명세 등을 정의하여 실제 구현의 기반을 마련한다.  