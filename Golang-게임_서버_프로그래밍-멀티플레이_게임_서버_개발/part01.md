# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 1. 개발 환경 구축

게임 서버 개발을 시작하기 위해서는 먼저 안정적이고 효율적인 개발 환경을 구축해야 한다. 이 장에서는 Windows 11 환경에서 Go 1.25를 설치하고, VSCode를 설정하며, 첫 번째 프로그램을 작성하고 실행하는 과정을 다룬다. 또한 Go의 모듈 시스템을 이해함으로써 향후 네트워크 라이브러리와 게임 로직을 분리된 모듈로 관리하는 기반을 마련한다.

---

## 1.1 Go 1.25 설치 (Windows 11)

### Go 언어 소개

Go(또는 Golang)는 Google에서 개발한 오픈 소스 프로그래밍 언어다. 2009년 처음 공개된 이후 단순성, 높은 성능, 강력한 동시성 지원으로 서버 개발 분야에서 큰 인기를 얻고 있다. 특히 게임 서버 개발에 있어 Go가 가진 다음과 같은 특징들이 매우 유용하다.

```
┌─────────────────────────────────────────────────────────┐
│              Go 언어의 주요 특징                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ✓ 간결한 문법                                           │
│    - 배우기 쉽고 읽기 쉬운 코드                          │
│    - 불필요한 복잡성 제거                                │
│                                                          │
│  ✓ 뛰어난 동시성 처리                                    │
│    - 고루틴(Goroutine)을 통한 경량 스레드               │
│    - 채널(Channel)을 통한 안전한 통신                   │
│                                                          │
│  ✓ 빠른 컴파일과 실행 속도                               │
│    - 네이티브 바이너리 생성                              │
│    - 가비지 컬렉터 최적화                                │
│                                                          │
│  ✓ 강력한 표준 라이브러리                                │
│    - 네트워크, 암호화, JSON 등 내장                     │
│    - 추가 라이브러리 없이도 서버 개발 가능               │
│                                                          │
│  ✓ 정적 타입 시스템                                      │
│    - 컴파일 타임 오류 검출                               │
│    - IDE 자동완성 지원                                   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### Go 1.25 다운로드

Go의 공식 웹사이트에서 최신 버전을 다운로드할 수 있다.

1. 웹 브라우저를 열고 `https://go.dev/dl/`로 이동한다.
2. Windows용 설치 파일을 찾는다. 파일명은 `go1.25.windows-amd64.msi` 형태다.
3. 다운로드 버튼을 클릭하여 설치 파일을 받는다.

> **참고**: 2025년 7월 현재 Go 1.25가 정식 릴리스되지 않았을 수 있다. 그럴 경우 가장 최신 안정 버전(예: 1.23 또는 1.24)을 사용하면 된다. 이 책의 대부분의 내용은 Go 1.18 이후 버전에서 동일하게 작동한다.

### 설치 과정

다운로드한 MSI 파일을 실행하면 설치 마법사가 시작된다.

1. 설치 마법사의 안내에 따라 "Next" 버튼을 클릭한다.
2. 라이선스 동의 화면에서 "I accept the terms in the License Agreement"를 체크하고 "Next"를 클릭한다.
3. 설치 경로를 확인한다. 기본값은 `C:\Program Files\Go`다. 특별한 이유가 없다면 기본 경로를 사용하는 것을 권장한다.
4. "Install" 버튼을 클릭하여 설치를 시작한다.
5. 설치가 완료되면 "Finish" 버튼을 클릭한다.

설치 프로그램은 자동으로 시스템 환경 변수에 Go를 추가한다. 이는 명령 프롬프트나 PowerShell에서 어디서든 `go` 명령어를 사용할 수 있게 해준다.

### 설치 확인

설치가 올바르게 완료되었는지 확인하기 위해 명령 프롬프트 또는 PowerShell을 연다.

**PowerShell 실행 방법:**
1. `Windows 키 + X`를 누른다.
2. "Windows PowerShell" 또는 "터미널"을 선택한다.

PowerShell 창에서 다음 명령어를 입력한다.

```bash
go version
```

정상적으로 설치되었다면 다음과 유사한 출력을 볼 수 있다.

```
go version go1.25.0 windows/amd64
```

이 출력은 Go 버전 1.25.0이 설치되었으며, Windows 운영체제의 64비트 아키텍처에서 실행되고 있음을 의미한다.

### 환경 변수 확인

Go는 몇 가지 중요한 환경 변수를 사용한다. 다음 명령어로 현재 Go 환경을 확인할 수 있다.

```bash
go env
```

출력되는 많은 변수 중 특히 중요한 것들은 다음과 같다.

```
GOROOT=C:\Program Files\Go
GOPATH=C:\Users\<사용자명>\go
GOOS=windows
GOARCH=amd64
```

각 변수의 의미는 다음과 같다.

- **GOROOT**: Go가 설치된 경로다. 표준 라이브러리와 Go 도구들이 이 디렉토리에 있다.
- **GOPATH**: Go 작업 공간의 루트 경로다. 외부 패키지와 컴파일된 바이너리가 저장되는 곳이다.
- **GOOS**: 타겟 운영체제를 나타낸다. Windows에서는 `windows`가 된다.
- **GOARCH**: 타겟 아키텍처를 나타낸다. 64비트 시스템에서는 `amd64`다.

### GOPATH 디렉토리 구조

GOPATH는 다음과 같은 구조를 가진다.

```
C:\Users\<사용자명>\go
│
├── bin/          # 실행 파일들이 저장되는 곳
│   └── ...
│
├── pkg/          # 컴파일된 패키지 객체 파일들
│   └── mod/      # 다운로드된 모듈들
│       └── ...
│
└── src/          # (선택적) Go 1.11 이전 방식의 소스 코드
    └── ...
```

Go 1.11부터 도입된 모듈 시스템을 사용하면 `src` 디렉토리는 필수가 아니다. 프로젝트를 원하는 위치 어디에든 만들 수 있으며, 이는 개발 편의성을 크게 향상시킨다.

---

## 1.2 VSCode 설정 및 Go 확장 설치

### VSCode 소개

Visual Studio Code(VSCode)는 Microsoft에서 개발한 무료 오픈 소스 코드 에디터다. 가볍고 빠르면서도 강력한 기능을 제공하여 많은 개발자들이 선호하는 도구다. Go 개발을 위한 확장 기능이 매우 잘 지원되어 있어 게임 서버 개발에 이상적인 환경을 제공한다.

### VSCode 다운로드 및 설치

1. 웹 브라우저에서 `https://code.visualstudio.com/`로 이동한다.
2. "Download for Windows" 버튼을 클릭하여 설치 파일을 다운로드한다.
3. 다운로드한 설치 파일(`VSCodeUserSetup-x64-<버전>.exe`)을 실행한다.
4. 라이선스 동의 후 설치 옵션을 선택한다. 다음 옵션들을 체크하는 것을 권장한다:
   - "Add 'Open with Code' action to Windows Explorer file context menu"
   - "Add 'Open with Code' action to Windows Explorer directory context menu"
   - "Register Code as an editor for supported file types"
   - "Add to PATH"
5. "Install" 버튼을 클릭하여 설치를 완료한다.

### Go 확장 설치

VSCode에서 Go 개발을 위해서는 공식 Go 확장을 설치해야 한다. 이 확장은 코드 자동완성, 디버깅, 린팅, 포맷팅 등 다양한 기능을 제공한다.

**설치 방법:**

1. VSCode를 실행한다.
2. 왼쪽 사이드바에서 확장(Extensions) 아이콘을 클릭하거나 `Ctrl + Shift + X`를 누른다.
3. 검색창에 "Go"를 입력한다.
4. "Go" 확장(작성자: Go Team at Google)을 찾아 "Install" 버튼을 클릭한다.

```
┌─────────────────────────────────────────────────────┐
│  VSCode Go Extension                                 │
├─────────────────────────────────────────────────────┤
│  제공하는 주요 기능:                                  │
│                                                      │
│  • IntelliSense (자동완성)                           │
│  • 코드 탐색 (정의로 이동, 참조 찾기)                 │
│  • 코드 포맷팅 (자동 들여쓰기, import 정리)           │
│  • 디버깅 (중단점, 변수 조사)                         │
│  • 린팅 (코드 품질 검사)                              │
│  • 테스트 실행 및 커버리지                            │
│  • 리팩토링 도구                                      │
│                                                      │
└─────────────────────────────────────────────────────┘
```

### Go 도구 설치

Go 확장을 처음 사용할 때, VSCode는 여러 Go 도구들을 설치하라는 메시지를 표시한다. 이 도구들은 확장의 다양한 기능을 지원한다.

1. Go 파일을 열면 우측 하단에 알림이 나타난다: "The 'gopls' command is not available..."
2. "Install All" 버튼을 클릭한다.
3. 출력 패널에서 설치 진행 상황을 확인할 수 있다.

주요 도구들은 다음과 같다.

- **gopls**: Go 언어 서버로, IntelliSense와 코드 탐색 기능을 제공한다.
- **dlv**: Delve 디버거로, Go 프로그램을 디버깅할 수 있게 해준다.
- **staticcheck**: 정적 분석 도구로, 코드의 잠재적 버그를 찾아낸다.
- **goimports**: import 문을 자동으로 추가하고 정리한다.

### VSCode 설정 최적화

Go 개발을 위해 VSCode의 설정을 일부 조정하면 더 나은 개발 경험을 얻을 수 있다.

1. `Ctrl + ,`를 눌러 설정 화면을 연다.
2. 우측 상단의 "Open Settings (JSON)" 아이콘을 클릭하여 JSON 설정 파일을 연다.
3. 다음 설정들을 추가한다.

```json
{
    // Go 관련 설정
    "go.useLanguageServer": true,
    "go.toolsManagement.autoUpdate": true,
    
    // 저장 시 자동 포맷팅
    "[go]": {
        "editor.formatOnSave": true,
        "editor.codeActionsOnSave": {
            "source.organizeImports": "explicit"
        }
    },
    
    // 탭 크기 설정 (Go 표준은 탭 사용)
    "go.formatTool": "gofmt",
    "editor.insertSpaces": false,
    "editor.tabSize": 4,
    
    // 린터 설정
    "go.lintTool": "staticcheck",
    "go.lintOnSave": "package",
    
    // 테스트 설정
    "go.testOnSave": false,
    "go.coverOnSave": false,
    
    // 디버깅 설정
    "go.delveConfig": {
        "dlvLoadConfig": {
            "followPointers": true,
            "maxVariableRecurse": 1,
            "maxStringLen": 120,
            "maxArrayValues": 64,
            "maxStructFields": -1
        }
    }
}
```

각 설정의 의미는 다음과 같다.

- **go.useLanguageServer**: gopls 언어 서버를 사용하여 더 나은 IntelliSense를 제공한다.
- **editor.formatOnSave**: 파일 저장 시 자동으로 코드를 포맷팅한다.
- **source.organizeImports**: import 문을 자동으로 정리한다.
- **go.formatTool**: gofmt를 사용하여 Go 표준 코드 스타일을 적용한다.
- **editor.insertSpaces**: Go는 공백 대신 탭을 사용하는 것이 표준이므로 false로 설정한다.
- **go.lintTool**: staticcheck를 사용하여 코드 품질을 검사한다.

---

## 1.3 작업 공간 구성 및 첫 번째 프로그램

### 프로젝트 디렉토리 생성

게임 서버 프로젝트를 위한 작업 공간을 생성한다. 이 책에서는 다음과 같은 디렉토리 구조를 사용할 것이다.

```
D:\GameServer\
│
├── network-lib/      # 네트워크 라이브러리 모듈
│   └── ...
│
├── poker-game/       # 포커 게임 로직 모듈
│   └── ...
│
└── poker-server/     # 포커 게임 서버 실행 프로젝트
    └── ...
```

먼저 기본 디렉토리를 생성한다. PowerShell을 열고 다음 명령어를 실행한다.

```powershell
# D 드라이브로 이동 (원하는 드라이브 선택 가능)
cd D:\

# GameServer 디렉토리 생성
mkdir GameServer
cd GameServer

# 각 프로젝트 디렉토리 생성
mkdir hello-go
```

> **참고**: 이 단계에서는 학습을 위해 `hello-go`라는 간단한 프로젝트를 먼저 만들어본다. 실제 게임 서버 프로젝트는 이후 챕터에서 구성한다.

### VSCode로 프로젝트 열기

```powershell
cd hello-go
code .
```

`code .` 명령어는 현재 디렉토리를 VSCode로 연다. VSCode가 새 창으로 열리고 왼쪽 탐색기에 `hello-go` 폴더가 표시된다.

### 첫 번째 Go 프로그램 작성

VSCode에서 새 파일을 생성한다.

1. 탐색기에서 `hello-go` 폴더를 우클릭한다.
2. "New File"을 선택한다.
3. 파일명을 `main.go`로 입력한다.

`main.go` 파일에 다음 코드를 작성한다.

```go
package main

import "fmt"

func main() {
    fmt.Println("Hello, Game Server!")
}
```

**코드 설명:**

- `package main`: 이 파일이 실행 가능한 프로그램의 일부임을 나타낸다. Go에서 실행 파일을 만들려면 반드시 `main` 패키지가 필요하다.
- `import "fmt"`: 표준 라이브러리의 `fmt` 패키지를 가져온다. 이 패키지는 포맷팅된 I/O 기능을 제공한다.
- `func main()`: 프로그램의 진입점이다. 프로그램이 실행되면 이 함수가 호출된다.
- `fmt.Println(...)`: 문자열을 출력하고 줄바꿈을 추가한다.

### 프로그램 실행

VSCode에서 터미널을 연다. `Ctrl + ` (백틱)` 또는 메뉴에서 "Terminal > New Terminal"을 선택한다.

터미널에서 다음 명령어를 실행한다.

```bash
go run main.go
```

출력 결과:
```
Hello, Game Server!
```

`go run` 명령어는 Go 소스 파일을 즉시 컴파일하고 실행한다. 이는 개발 중 빠른 테스트에 유용하다.

### 빌드하여 실행 파일 만들기

배포를 위해서는 독립 실행 파일을 만들어야 한다.

```bash
go build main.go
```

이 명령어는 `main.exe` 파일을 생성한다. 생성된 실행 파일을 직접 실행할 수 있다.

```bash
.\main.exe
```

출력 결과는 동일하다.

```
Hello, Game Server!
```

`go build`의 장점은 다음과 같다.

- 컴파일된 바이너리는 Go 런타임 없이도 실행된다.
- 다른 운영체제를 위한 크로스 컴파일이 가능하다.
- 배포가 간단하다 (단일 실행 파일만 복사하면 됨).

### 네트워크 프로그램 예제

게임 서버 개발의 감을 잡기 위해 간단한 TCP 에코 서버를 만들어보자.

`echo_server.go` 파일을 생성하고 다음 코드를 작성한다.

```go
package main

import (
    "bufio"
    "fmt"
    "net"
    "os"
)

func main() {
    // TCP 리스너 생성 (포트 8080에서 대기)
    listener, err := net.Listen("tcp", ":8080")
    if err != nil {
        fmt.Println("리스너 생성 실패:", err)
        os.Exit(1)
    }
    defer listener.Close()
    
    fmt.Println("에코 서버가 포트 8080에서 시작되었습니다...")
    
    // 클라이언트 연결 대기
    for {
        conn, err := listener.Accept()
        if err != nil {
            fmt.Println("연결 수락 실패:", err)
            continue
        }
        
        // 각 클라이언트를 별도 고루틴에서 처리
        go handleClient(conn)
    }
}

func handleClient(conn net.Conn) {
    defer conn.Close()
    
    clientAddr := conn.RemoteAddr().String()
    fmt.Printf("클라이언트 연결됨: %s\n", clientAddr)
    
    // 클라이언트로부터 데이터 읽기
    scanner := bufio.NewScanner(conn)
    for scanner.Scan() {
        message := scanner.Text()
        fmt.Printf("[%s] 받음: %s\n", clientAddr, message)
        
        // 받은 메시지를 그대로 돌려보냄 (에코)
        _, err := conn.Write([]byte(message + "\n"))
        if err != nil {
            fmt.Printf("전송 실패: %v\n", err)
            return
        }
    }
    
    if err := scanner.Err(); err != nil {
        fmt.Printf("읽기 오류: %v\n", err)
    }
    
    fmt.Printf("클라이언트 연결 종료: %s\n", clientAddr)
}
```

**코드 설명:**

이 프로그램은 간단한 TCP 에코 서버다. 클라이언트가 보낸 메시지를 그대로 돌려보낸다.

- `net.Listen("tcp", ":8080")`: TCP 리스너를 생성하여 포트 8080에서 연결을 대기한다.
- `defer listener.Close()`: 함수가 종료될 때 리스너를 닫도록 예약한다. `defer`는 Go의 중요한 기능으로, 리소스 정리를 보장한다.
- `listener.Accept()`: 클라이언트의 연결 요청을 수락한다. 이 함수는 블로킹 함수로, 연결이 들어올 때까지 대기한다.
- `go handleClient(conn)`: 새로운 고루틴을 생성하여 클라이언트를 처리한다. 이를 통해 여러 클라이언트를 동시에 처리할 수 있다.
- `bufio.NewScanner(conn)`: 연결에서 데이터를 줄 단위로 읽기 위한 스캐너를 생성한다.
- `scanner.Scan()`: 다음 줄을 읽는다. 더 이상 읽을 데이터가 없으면 false를 반환한다.

서버를 실행한다.

```bash
go run echo_server.go
```

다른 터미널 창을 열고 텔넷으로 서버에 접속하여 테스트할 수 있다.

```bash
telnet localhost 8080
```

텍스트를 입력하면 서버가 그대로 돌려보낸다.

> **참고**: Windows 11에서 텔넷 클라이언트가 기본적으로 비활성화되어 있을 수 있다. "Windows 기능 켜기/끄기"에서 "텔넷 클라이언트"를 활성화하거나, 별도의 TCP 클라이언트 프로그램을 작성할 수 있다.

이 간단한 예제를 통해 Go에서의 네트워크 프로그래밍이 얼마나 직관적인지 확인할 수 있다. 표준 라이브러리만으로도 강력한 네트워크 서버를 구축할 수 있다.

---

## 1.4 Go 모듈 시스템 이해하기

### Go 모듈이란?

Go 모듈은 Go 1.11부터 도입된 의존성 관리 시스템이다. 모듈을 사용하면 프로젝트의 의존성을 명시적으로 선언하고 버전을 관리할 수 있다. 이는 재현 가능한 빌드를 보장하고 다른 개발자와의 협업을 용이하게 한다.

```
┌─────────────────────────────────────────────────────────┐
│           Go 모듈의 주요 개념                            │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  Module (모듈)                                           │
│    └─ 관련된 Go 패키지들의 모음                          │
│    └─ go.mod 파일로 정의됨                               │
│                                                          │
│  go.mod 파일                                             │
│    └─ 모듈의 경로와 이름                                 │
│    └─ Go 버전 요구사항                                   │
│    └─ 의존하는 다른 모듈들                               │
│                                                          │
│  go.sum 파일                                             │
│    └─ 의존성의 체크섬                                    │
│    └─ 무결성 검증에 사용                                 │
│                                                          │
│  replace 지시문                                          │
│    └─ 로컬 모듈 참조                                     │
│    └─ 모듈 경로 재지정                                   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 모듈 초기화

새 프로젝트를 모듈로 초기화해보자. 앞서 만든 `hello-go` 디렉토리에서 다음 명령어를 실행한다.

```bash
go mod init example.com/hello-go
```

이 명령어는 `go.mod` 파일을 생성한다. 파일 내용을 확인해보자.

```
module example.com/hello-go

go 1.25
```

**각 부분의 의미:**

- `module example.com/hello-go`: 모듈의 경로다. 일반적으로 저장소 URL 형태를 사용하지만, 로컬 개발에서는 임의의 경로를 사용할 수 있다.
- `go 1.25`: 이 모듈이 요구하는 최소 Go 버전이다.

### 외부 패키지 사용

외부 패키지를 사용하는 예제를 만들어보자. UUID를 생성하는 인기 있는 패키지를 사용한다.

`uuid_example.go` 파일을 생성하고 다음 코드를 작성한다.

```go
package main

import (
    "fmt"
    "github.com/google/uuid"
)

func main() {
    // 새 UUID 생성
    id := uuid.New()
    fmt.Printf("생성된 UUID: %s\n", id.String())
    
    // 플레이어 ID로 사용 예시
    playerID := uuid.New()
    fmt.Printf("플레이어 ID: %s\n", playerID.String())
}
```

코드를 저장하고 다음 명령어를 실행한다.

```bash
go mod tidy
```

`go mod tidy` 명령어는 다음 작업을 수행한다.

1. 소스 코드에서 import된 패키지를 찾는다.
2. 필요한 패키지를 다운로드한다.
3. `go.mod` 파일에 의존성을 추가한다.
4. `go.sum` 파일을 생성/업데이트한다.

이제 `go.mod` 파일을 다시 확인하면 다음과 같이 변경되었음을 알 수 있다.

```
module example.com/hello-go

go 1.25

require github.com/google/uuid v1.6.0
```

`require` 줄이 추가되었다. 이는 프로젝트가 `github.com/google/uuid` 패키지의 버전 1.6.0을 사용함을 나타낸다.

프로그램을 실행한다.

```bash
go run uuid_example.go
```

출력 예시:
```
생성된 UUID: 8f3b7c9e-6d2a-4f1b-9e3c-5a8b7d6c4e2f
플레이어 ID: 3a1b5c7d-9e2f-4a6b-8c3d-1e5f7a9b2c4d
```

### 로컬 모듈 참조 (replace 지시문)

게임 서버 프로젝트에서는 네트워크 라이브러리와 게임 로직을 별도의 모듈로 분리할 것이다. 이들은 GitHub에 올리지 않고 로컬 디렉토리에 위치한다. 이런 경우 `replace` 지시문을 사용한다.

실제 프로젝트 구조를 미리 만들어보자.

```powershell
# GameServer 디렉토리로 이동
cd D:\GameServer

# 네트워크 라이브러리 모듈 생성
mkdir network-lib
cd network-lib
go mod init gameserver/network

# 간단한 패키지 생성
mkdir tcp
```

`network-lib/tcp/listener.go` 파일을 생성한다.

```go
package tcp

import "fmt"

// StartServer는 TCP 서버를 시작하는 함수다
func StartServer(port int) {
    fmt.Printf("TCP 서버를 포트 %d에서 시작합니다...\n", port)
    // 실제 구현은 나중에 추가
}
```

이제 이 로컬 모듈을 사용하는 메인 프로젝트를 만든다.

```powershell
cd D:\GameServer
mkdir test-local-module
cd test-local-module
go mod init gameserver/test
```

`test-local-module/main.go` 파일을 생성한다.

```go
package main

import "gameserver/network/tcp"

func main() {
    tcp.StartServer(8080)
}
```

이 상태에서 `go run main.go`를 실행하면 오류가 발생한다. Go는 `gameserver/network` 모듈을 찾을 수 없기 때문이다.

```
main.go:3:8: no required module provides package gameserver/network/tcp
```

`replace` 지시문을 사용하여 로컬 모듈을 참조하도록 `go.mod` 파일을 수정한다.

```
module gameserver/test

go 1.25

require gameserver/network v0.0.0

replace gameserver/network => ../network-lib
```

**각 부분의 설명:**

- `require gameserver/network v0.0.0`: 이 모듈이 `gameserver/network` 모듈을 필요로 함을 선언한다. 버전은 임시로 `v0.0.0`을 사용한다.
- `replace gameserver/network => ../network-lib`: `gameserver/network` 모듈을 찾을 때 `../network-lib` 디렉토리를 사용하도록 지시한다.

이제 프로그램을 실행하면 정상적으로 동작한다.

```bash
go run main.go
```

출력:
```
TCP 서버를 포트 8080에서 시작합니다...
```

### 모듈 의존성 그래프 확인

프로젝트가 복잡해지면 의존성을 시각적으로 확인하는 것이 유용하다.

```bash
go mod graph
```

현재 프로젝트의 경우 다음과 유사한 출력을 볼 수 있다.

```
gameserver/test gameserver/network@v0.0.0
```

이는 `gameserver/test` 모듈이 `gameserver/network` 모듈에 의존함을 보여준다.

### 멀티 모듈 워크스페이스 (go.work)

Go 1.18부터 도입된 워크스페이스 기능을 사용하면 여러 모듈을 동시에 개발할 때 편리하다. `replace` 지시문 없이도 로컬 모듈들을 함께 작업할 수 있다.

```powershell
cd D:\GameServer
go work init ./network-lib ./test-local-module
```

이 명령어는 `go.work` 파일을 생성한다.

```
go 1.25

use (
    ./network-lib
    ./test-local-module
)
```

`go.work` 파일이 있으면 `replace` 지시문 없이도 로컬 모듈들이 서로를 찾을 수 있다. 다만 이 파일은 개발 환경에서만 사용되며, 버전 관리 시스템(Git)에 커밋하지 않는 것이 일반적이다.

### 모듈 명령어 요약

자주 사용하는 모듈 관련 명령어들이다.

| 명령어 | 설명 |
|--------|------|
| `go mod init <모듈경로>` | 새 모듈을 초기화한다 |
| `go mod tidy` | 의존성을 정리하고 불필요한 항목을 제거한다 |
| `go mod download` | 의존성을 다운로드한다 |
| `go mod verify` | 의존성의 무결성을 검증한다 |
| `go mod graph` | 의존성 그래프를 출력한다 |
| `go mod why <패키지>` | 특정 패키지가 왜 필요한지 설명한다 |
| `go mod vendor` | 의존성을 vendor 디렉토리로 복사한다 |
| `go get <패키지>@<버전>` | 특정 버전의 패키지를 추가한다 |

### 실전 팁

게임 서버 개발 시 모듈을 사용하는 몇 가지 권장사항이다.

1. **명확한 모듈 경로 사용**: 나중에 GitHub에 올릴 가능성을 고려하여 `github.com/<사용자명>/<프로젝트명>` 형태를 사용하는 것이 좋다.

2. **의미 있는 버전 태깅**: Git 태그를 사용하여 버전을 관리한다. 예: `v1.0.0`, `v1.1.0`

3. **go.sum 파일 커밋**: 이 파일은 의존성의 무결성을 보장하므로 반드시 버전 관리에 포함시킨다.

4. **go.work 파일은 로컬만**: 개인 개발 환경 설정이므로 `.gitignore`에 추가한다.

5. **주기적인 go mod tidy**: 개발 과정에서 사용하지 않는 의존성이 쌓일 수 있으므로 주기적으로 정리한다.

---

## 정리

이 장에서는 Go 게임 서버 개발을 위한 기초를 다졌다.

1. **Go 1.25 설치**: Windows 11 환경에서 Go를 설치하고 정상 작동을 확인했다.

2. **VSCode 설정**: Go 개발에 최적화된 에디터를 설정하고 필요한 확장과 도구들을 설치했다.

3. **첫 프로그램**: Hello World부터 간단한 TCP 에코 서버까지 작성하여 Go의 기본 문법과 네트워크 프로그래밍을 맛보았다.

4. **모듈 시스템**: 의존성 관리의 핵심인 모듈 시스템을 이해했다. 특히 로컬 모듈 참조 방법은 네트워크 라이브러리와 게임 로직을 분리하는 데 필수적이다.

다음 장에서는 Go 언어의 핵심 문법을 본격적으로 학습하여 게임 서버 개발에 필요한 언어적 기초를 다질 것이다.  
    

# Chapter 2. Go 언어 핵심 문법

게임 서버를 개발하기 위해서는 Go 언어의 핵심 문법을 탄탄히 이해해야 한다. 이 장에서는 변수와 타입 시스템부터 포인터까지, 게임 서버 개발에 필수적인 Go의 기본 문법을 다룬다. 각 개념은 실제 게임 서버에서 사용될 수 있는 예제와 함께 설명한다.

---

## 2.1 변수와 타입 시스템

### 변수 선언

Go는 정적 타입 언어로, 모든 변수는 명확한 타입을 가진다. 하지만 타입 추론 기능을 통해 간결한 코드 작성이 가능하다.

```go
package main

import "fmt"

func main() {
    // 방법 1: var 키워드를 사용한 선언
    var playerName string
    playerName = "Player1"
    
    // 방법 2: var 키워드와 초기값을 함께 사용
    var playerLevel int = 1
    
    // 방법 3: 타입 추론을 사용한 선언
    var playerHealth = 100  // int로 추론됨
    
    // 방법 4: 짧은 선언 (함수 내부에서만 사용 가능)
    playerMana := 50
    
    fmt.Printf("플레이어: %s, 레벨: %d, 체력: %d, 마나: %d\n", 
        playerName, playerLevel, playerHealth, playerMana)
}
```

**코드 설명:**

Go에서 변수를 선언하는 방법은 여러 가지다. `var` 키워드를 사용하는 전통적인 방법과 `:=` 연산자를 사용하는 짧은 선언 방법이 있다. 짧은 선언은 함수 내부에서만 사용할 수 있으며, 타입을 자동으로 추론한다는 장점이 있다.

게임 서버 개발에서는 명확성을 위해 패키지 레벨 변수는 `var`를 사용하고, 함수 내부의 지역 변수는 `:=`를 사용하는 것이 일반적이다.

### 기본 타입

Go는 다양한 기본 타입을 제공한다.

```go
package main

import "fmt"

func main() {
    // 정수형
    var playerId int32 = 10001       // 32비트 정수
    var sessionId int64 = 9876543210 // 64비트 정수
    var roomCount uint8 = 255        // 부호 없는 8비트 정수 (0~255)
    
    // 부동소수점
    var damageMultiplier float32 = 1.5
    var criticalRate float64 = 0.25
    
    // 불리언
    var isConnected bool = true
    var isGameStarted bool = false
    
    // 문자열
    var serverName string = "Game Server 1"
    var welcomeMessage = "환영합니다!"
    
    // 문자 (rune은 int32의 별칭)
    var unicodeChar rune = '가'
    
    // 바이트 (uint8의 별칭)
    var packetType byte = 0x01
    
    fmt.Printf("플레이어 ID: %d\n", playerId)
    fmt.Printf("세션 ID: %d\n", sessionId)
    fmt.Printf("방 개수: %d\n", roomCount)
    fmt.Printf("데미지 배율: %.2f\n", damageMultiplier)
    fmt.Printf("크리티컬 확률: %.2f%%\n", criticalRate*100)
    fmt.Printf("연결 상태: %t\n", isConnected)
    fmt.Printf("게임 시작 여부: %t\n", isGameStarted)
    fmt.Printf("서버 이름: %s\n", serverName)
    fmt.Printf("환영 메시지: %s\n", welcomeMessage)
    fmt.Printf("유니코드 문자: %c (코드: %d)\n", unicodeChar, unicodeChar)
    fmt.Printf("패킷 타입: 0x%02X\n", packetType)
}
```

**타입 선택 가이드:**

게임 서버에서 타입을 선택할 때는 다음을 고려한다.

```
┌─────────────────────────────────────────────────────────┐
│           게임 서버에서의 타입 선택                      │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  플레이어 ID, 세션 ID                                    │
│    → int64 (큰 범위, 음수 불필요하면 uint64)             │
│                                                          │
│  방 번호, 작은 카운터                                    │
│    → int32, uint32                                       │
│                                                          │
│  상태 플래그, 열거형                                     │
│    → uint8, byte                                         │
│                                                          │
│  확률, 배율                                              │
│    → float64 (정밀도 필요)                               │
│                                                          │
│  불리언 플래그                                           │
│    → bool                                                │
│                                                          │
│  텍스트 데이터                                           │
│    → string                                              │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 상수

상수는 컴파일 타임에 값이 결정되며, 런타임에 변경할 수 없다.

```go
package main

import "fmt"

// 패키지 레벨 상수
const (
    MaxPlayers     = 4        // 방당 최대 플레이어 수
    MinBetAmount   = 10       // 최소 베팅 금액
    MaxBetAmount   = 1000     // 최대 베팅 금액
    DefaultChips   = 10000    // 기본 칩
)

// iota를 사용한 열거형 상수
const (
    PlayerStateDisconnected = iota  // 0
    PlayerStateConnected            // 1
    PlayerStateLobby                // 2
    PlayerStateInGame               // 3
)

const (
    PacketTypeLogin = iota + 1  // 1부터 시작
    PacketTypeLogout            // 2
    PacketTypeJoinRoom          // 3
    PacketTypeLeaveRoom         // 4
    PacketTypeGameStart         // 5
)

func main() {
    fmt.Println("게임 설정:")
    fmt.Printf("  최대 플레이어: %d명\n", MaxPlayers)
    fmt.Printf("  베팅 범위: %d ~ %d\n", MinBetAmount, MaxBetAmount)
    fmt.Printf("  시작 칩: %d\n", DefaultChips)
    
    fmt.Println("\n플레이어 상태:")
    fmt.Printf("  연결 끊김: %d\n", PlayerStateDisconnected)
    fmt.Printf("  연결됨: %d\n", PlayerStateConnected)
    fmt.Printf("  로비: %d\n", PlayerStateLobby)
    fmt.Printf("  게임 중: %d\n", PlayerStateInGame)
    
    fmt.Println("\n패킷 타입:")
    fmt.Printf("  로그인: %d\n", PacketTypeLogin)
    fmt.Printf("  로그아웃: %d\n", PacketTypeLogout)
    fmt.Printf("  방 입장: %d\n", PacketTypeJoinRoom)
}
```

**iota의 활용:**

`iota`는 Go의 특별한 상수 생성 메커니즘이다. 상수 선언 블록에서 0부터 시작하여 자동으로 증가하는 값을 생성한다. 게임 서버에서 패킷 타입이나 상태 코드를 정의할 때 매우 유용하다.

### 타입 변환

Go는 암시적 타입 변환을 지원하지 않는다. 모든 타입 변환은 명시적으로 수행해야 한다.

```go
package main

import "fmt"

func main() {
    // 정수 간 변환
    var playerLevel int32 = 50
    var maxLevel int64 = int64(playerLevel) + 10
    
    // 정수와 부동소수점 간 변환
    var totalDamage int = 100
    var damageMultiplier float64 = 1.5
    var actualDamage = float64(totalDamage) * damageMultiplier
    
    // 부동소수점을 정수로 변환 (소수점 이하 버림)
    var finalDamage int = int(actualDamage)
    
    // 문자열 변환은 strconv 패키지 사용
    import "strconv"
    var scoreStr string = "12345"
    score, err := strconv.Atoi(scoreStr)  // string to int
    if err != nil {
        fmt.Println("변환 오류:", err)
        return
    }
    
    var highScore int = 99999
    highScoreStr := strconv.Itoa(highScore)  // int to string
    
    fmt.Printf("레벨: %d (타입: int32)\n", playerLevel)
    fmt.Printf("최대 레벨: %d (타입: int64)\n", maxLevel)
    fmt.Printf("실제 데미지: %.2f\n", actualDamage)
    fmt.Printf("최종 데미지: %d\n", finalDamage)
    fmt.Printf("점수: %d (문자열 '%s'에서 변환)\n", score, scoreStr)
    fmt.Printf("최고 점수: %s (정수 %d에서 변환)\n", highScoreStr, highScore)
}
```

**중요 사항:**

타입 변환 시 주의할 점은 데이터 손실이 발생할 수 있다는 것이다. 예를 들어, `float64`를 `int`로 변환하면 소수점 이하가 버려진다. 게임 서버에서 점수나 데미지 계산 시 이를 반드시 고려해야 한다.

### 제로값

Go에서 선언만 하고 초기화하지 않은 변수는 타입의 제로값(zero value)을 가진다.

```go
package main

import "fmt"

func main() {
    var playerCount int        // 0
    var playerName string      // ""
    var isActive bool          // false
    var winRate float64        // 0.0
    var playerPtr *int         // nil
    
    fmt.Printf("정수의 제로값: %d\n", playerCount)
    fmt.Printf("문자열의 제로값: '%s' (빈 문자열)\n", playerName)
    fmt.Printf("불리언의 제로값: %t\n", isActive)
    fmt.Printf("부동소수점의 제로값: %f\n", winRate)
    fmt.Printf("포인터의 제로값: %v\n", playerPtr)
    
    // 제로값의 활용
    if playerName == "" {
        playerName = "Guest"
    }
    fmt.Printf("플레이어 이름: %s\n", playerName)
}
```

**제로값의 중요성:**

제로값은 Go의 중요한 특징이다. 변수를 선언하기만 해도 안전한 기본값을 가지므로, 초기화되지 않은 변수로 인한 예측 불가능한 동작을 방지한다. 게임 서버에서 구조체를 생성할 때 이 특성이 특히 유용하다.

---

## 2.2 함수와 메서드

### 함수 기본

함수는 Go 프로그래밍의 기본 구성 요소다.

```go
package main

import "fmt"

// 기본 함수
func greetPlayer(name string) {
    fmt.Printf("환영합니다, %s님!\n", name)
}

// 반환값이 있는 함수
func calculateDamage(baseDamage int, multiplier float64) int {
    return int(float64(baseDamage) * multiplier)
}

// 여러 개의 반환값
func divideChips(totalChips, playerCount int) (int, int) {
    chipsPerPlayer := totalChips / playerCount
    remainder := totalChips % playerCount
    return chipsPerPlayer, remainder
}

// 명명된 반환값
func calculateWinnings(betAmount int, winMultiplier float64) (winnings int, profit int) {
    winnings = int(float64(betAmount) * winMultiplier)
    profit = winnings - betAmount
    return  // naked return (명명된 반환값이 자동으로 반환됨)
}

// 가변 인자 함수
func sumScores(scores ...int) int {
    total := 0
    for _, score := range scores {
        total += score
    }
    return total
}

func main() {
    greetPlayer("홍길동")
    
    damage := calculateDamage(100, 1.5)
    fmt.Printf("데미지: %d\n", damage)
    
    chipsPerPlayer, remainder := divideChips(10000, 4)
    fmt.Printf("플레이어당 칩: %d, 나머지: %d\n", chipsPerPlayer, remainder)
    
    winnings, profit := calculateWinnings(1000, 2.5)
    fmt.Printf("상금: %d, 순이익: %d\n", winnings, profit)
    
    total := sumScores(100, 200, 150, 300)
    fmt.Printf("총점: %d\n", total)
}
```

**함수 설계 원칙:**

게임 서버에서 함수를 설계할 때는 다음을 고려한다.

1. **단일 책임**: 각 함수는 하나의 명확한 작업을 수행해야 한다.
2. **명확한 이름**: 함수 이름만 봐도 무엇을 하는지 알 수 있어야 한다.
3. **에러 반환**: 실패할 수 있는 작업은 에러를 반환해야 한다.

### 에러 처리를 포함한 함수

Go에서는 에러를 반환값으로 처리하는 것이 관례다.

```go
package main

import (
    "errors"
    "fmt"
)

// 에러를 반환하는 함수
func placeBet(currentChips, betAmount int) (int, error) {
    if betAmount <= 0 {
        return currentChips, errors.New("베팅 금액은 0보다 커야 합니다")
    }
    
    if betAmount > currentChips {
        return currentChips, fmt.Errorf("칩이 부족합니다 (보유: %d, 베팅: %d)", 
            currentChips, betAmount)
    }
    
    return currentChips - betAmount, nil
}

// 여러 에러 조건을 처리하는 함수
func joinRoom(roomId int, playerCount, maxPlayers int) error {
    if roomId <= 0 {
        return errors.New("유효하지 않은 방 ID")
    }
    
    if playerCount >= maxPlayers {
        return errors.New("방이 가득 찼습니다")
    }
    
    return nil
}

func main() {
    currentChips := 5000
    
    // 성공 케이스
    remainingChips, err := placeBet(currentChips, 1000)
    if err != nil {
        fmt.Println("베팅 실패:", err)
    } else {
        fmt.Printf("베팅 성공! 남은 칩: %d\n", remainingChips)
        currentChips = remainingChips
    }
    
    // 실패 케이스 1: 금액이 0 이하
    _, err = placeBet(currentChips, -500)
    if err != nil {
        fmt.Println("베팅 실패:", err)
    }
    
    // 실패 케이스 2: 칩 부족
    _, err = placeBet(currentChips, 10000)
    if err != nil {
        fmt.Println("베팅 실패:", err)
    }
    
    // 방 입장 시도
    err = joinRoom(1, 3, 4)
    if err != nil {
        fmt.Println("입장 실패:", err)
    } else {
        fmt.Println("방 입장 성공!")
    }
    
    err = joinRoom(1, 4, 4)
    if err != nil {
        fmt.Println("입장 실패:", err)
    }
}
```

**에러 처리 패턴:**

```
┌─────────────────────────────────────────────────────────┐
│              Go의 에러 처리 패턴                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  1. 에러를 마지막 반환값으로 반환                        │
│     func doSomething() (result, error)                   │
│                                                          │
│  2. 에러가 없으면 nil 반환                               │
│     if success { return result, nil }                    │
│                                                          │
│  3. 호출하는 쪽에서 즉시 확인                            │
│     result, err := doSomething()                         │
│     if err != nil { /* 에러 처리 */ }                    │
│                                                          │
│  4. 빠른 실패 (early return)                             │
│     에러가 있으면 즉시 반환하여 중첩을 줄임              │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 고차 함수와 클로저

Go는 함수를 일급 객체로 취급한다. 함수를 변수에 할당하고, 다른 함수의 인자로 전달하거나, 반환값으로 사용할 수 있다.

```go
package main

import "fmt"

// 함수 타입 정의
type DamageCalculator func(int) int

// 데미지 계산 함수를 반환하는 함수
func createDamageCalculator(multiplier float64) DamageCalculator {
    return func(baseDamage int) int {
        return int(float64(baseDamage) * multiplier)
    }
}

// 필터 함수를 인자로 받는 함수
func filterPlayers(players []string, filter func(string) bool) []string {
    result := make([]string, 0)
    for _, player := range players {
        if filter(player) {
            result = append(result, player)
        }
    }
    return result
}

func main() {
    // 클로저를 활용한 데미지 계산기
    normalDamage := createDamageCalculator(1.0)
    criticalDamage := createDamageCalculator(2.5)
    
    baseDamage := 100
    fmt.Printf("일반 데미지: %d\n", normalDamage(baseDamage))
    fmt.Printf("크리티컬 데미지: %d\n", criticalDamage(baseDamage))
    
    // 고차 함수 활용
    players := []string{"Player1", "Guest1", "Player2", "Guest2", "Player3"}
    
    // 게스트가 아닌 플레이어만 필터링
    registeredPlayers := filterPlayers(players, func(name string) bool {
        return len(name) > 5 && name[:5] != "Guest"
    })
    
    fmt.Println("등록된 플레이어:", registeredPlayers)
    
    // 클로저를 사용한 카운터
    counter := 0
    incrementScore := func(points int) int {
        counter += points
        return counter
    }
    
    fmt.Println("점수:", incrementScore(100))
    fmt.Println("점수:", incrementScore(50))
    fmt.Println("점수:", incrementScore(200))
}
```

**클로저의 활용:**

클로저는 자신이 선언된 환경의 변수에 접근할 수 있는 함수다. 게임 서버에서 상태를 캡슐화하거나 콜백 함수를 구현할 때 유용하다.

### 메서드

메서드는 특정 타입에 속한 함수다. 구조체에 메서드를 정의하여 객체 지향적인 코드를 작성할 수 있다.

```go
package main

import "fmt"

// 플레이어 구조체
type Player struct {
    Name  string
    Chips int
    Level int
}

// 값 리시버 메서드 (복사본에 대해 작동)
func (p Player) GetInfo() string {
    return fmt.Sprintf("%s (레벨 %d, 칩 %d)", p.Name, p.Level, p.Chips)
}

// 포인터 리시버 메서드 (원본을 수정)
func (p *Player) AddChips(amount int) {
    p.Chips += amount
}

func (p *Player) RemoveChips(amount int) error {
    if amount > p.Chips {
        return fmt.Errorf("칩이 부족합니다 (보유: %d, 필요: %d)", p.Chips, amount)
    }
    p.Chips -= amount
    return nil
}

func (p *Player) LevelUp() {
    p.Level++
    fmt.Printf("%s님이 레벨 %d로 올랐습니다!\n", p.Name, p.Level)
}

func main() {
    player := Player{
        Name:  "홍길동",
        Chips: 10000,
        Level: 1,
    }
    
    fmt.Println(player.GetInfo())
    
    // 칩 추가
    player.AddChips(5000)
    fmt.Println("칩 추가 후:", player.GetInfo())
    
    // 칩 제거
    err := player.RemoveChips(3000)
    if err != nil {
        fmt.Println("칩 제거 실패:", err)
    } else {
        fmt.Println("칩 제거 후:", player.GetInfo())
    }
    
    // 레벨업
    player.LevelUp()
    fmt.Println(player.GetInfo())
}
```

**값 리시버 vs 포인터 리시버:**

```
┌─────────────────────────────────────────────────────────┐
│         값 리시버 vs 포인터 리시버                       │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  값 리시버 (Value Receiver)                              │
│    func (p Player) Method()                              │
│    • 복사본에 대해 작동                                  │
│    • 원본을 변경하지 않음                                │
│    • 작은 구조체에 적합                                  │
│    • 읽기 전용 메서드에 사용                             │
│                                                          │
│  포인터 리시버 (Pointer Receiver)                        │
│    func (p *Player) Method()                             │
│    • 원본에 대해 작동                                    │
│    • 원본을 변경할 수 있음                               │
│    • 큰 구조체에 효율적                                  │
│    • 상태를 변경하는 메서드에 사용                       │
│                                                          │
│  권장 사항:                                              │
│    • 일관성: 한 타입의 모든 메서드는 같은 리시버 사용   │
│    • 변경 필요시: 포인터 리시버 사용                     │
│    • 대형 구조체: 포인터 리시버가 효율적                │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2.3 제어문 (if, switch, for)

### if 문

Go의 `if` 문은 조건문 앞에 간단한 문장을 실행할 수 있는 특징이 있다.

```go
package main

import (
    "fmt"
    "math/rand"
    "time"
)

func rollDice() int {
    rand.Seed(time.Now().UnixNano())
    return rand.Intn(6) + 1
}

func getBetResult(betAmount, playerChips int) (int, error) {
    if betAmount <= 0 {
        return playerChips, fmt.Errorf("베팅 금액은 0보다 커야 합니다")
    }
    
    if betAmount > playerChips {
        return playerChips, fmt.Errorf("칩이 부족합니다")
    }
    
    return playerChips - betAmount, nil
}

func main() {
    // 기본 if 문
    playerLevel := 5
    if playerLevel >= 10 {
        fmt.Println("고급 플레이어")
    } else if playerLevel >= 5 {
        fmt.Println("중급 플레이어")
    } else {
        fmt.Println("초보 플레이어")
    }
    
    // if 문에서 변수 초기화
    if result := rollDice(); result >= 5 {
        fmt.Printf("주사위 결과: %d - 승리!\n", result)
    } else {
        fmt.Printf("주사위 결과: %d - 패배!\n", result)
    }
    
    // 에러 처리와 함께 사용
    playerChips := 5000
    if newChips, err := getBetResult(1000, playerChips); err != nil {
        fmt.Println("베팅 실패:", err)
    } else {
        fmt.Printf("베팅 성공! 남은 칩: %d\n", newChips)
        playerChips = newChips
    }
    
    // 중첩된 if 문
    roomPlayerCount := 3
    maxPlayers := 4
    gameStarted := false
    
    if roomPlayerCount < maxPlayers {
        fmt.Println("방에 자리가 있습니다")
        if !gameStarted {
            fmt.Println("게임이 시작되지 않았습니다. 입장 가능!")
        } else {
            fmt.Println("게임이 진행 중입니다. 대기해야 합니다.")
        }
    } else {
        fmt.Println("방이 가득 찼습니다")
    }
}
```

**if 문 스타일 가이드:**

Go에서는 간결성과 가독성을 중요시한다. 다음과 같은 스타일을 권장한다.

- 중괄호는 같은 줄에 시작한다.
- 불필요한 else를 피하고 early return을 사용한다.
- if 문 안에서 변수 초기화를 활용한다.

```go
// 좋은 예: early return
func processPlayer(player *Player) error {
    if player == nil {
        return errors.New("플레이어가 nil입니다")
    }
    
    if player.Chips <= 0 {
        return errors.New("칩이 부족합니다")
    }
    
    // 정상 처리 로직
    return nil
}

// 나쁜 예: 불필요한 중첩
func processPlayerBad(player *Player) error {
    if player != nil {
        if player.Chips > 0 {
            // 정상 처리 로직
            return nil
        } else {
            return errors.New("칩이 부족합니다")
        }
    } else {
        return errors.New("플레이어가 nil입니다")
    }
}
```

### switch 문

`switch` 문은 여러 조건을 간결하게 표현할 수 있다.

```go
package main

import "fmt"

// 플레이어 상태
const (
    StateDisconnected = iota
    StateConnected
    StateLobby
    StateInGame
    StateSpectating
)

// 패킷 타입
const (
    PacketLogin = iota + 1
    PacketLogout
    PacketJoinRoom
    PacketLeaveRoom
    PacketGameStart
    PacketBet
    PacketFold
)

func getStateMessage(state int) string {
    switch state {
    case StateDisconnected:
        return "연결 끊김"
    case StateConnected:
        return "연결됨"
    case StateLobby:
        return "로비 대기 중"
    case StateInGame:
        return "게임 중"
    case StateSpectating:
        return "관전 중"
    default:
        return "알 수 없는 상태"
    }
}

func handlePacket(packetType int) {
    switch packetType {
    case PacketLogin:
        fmt.Println("로그인 처리")
    case PacketLogout:
        fmt.Println("로그아웃 처리")
    case PacketJoinRoom, PacketLeaveRoom:  // 여러 케이스를 하나로
        fmt.Println("방 입장/퇴장 처리")
    case PacketGameStart:
        fmt.Println("게임 시작 처리")
    case PacketBet, PacketFold:
        fmt.Println("게임 액션 처리")
    default:
        fmt.Println("알 수 없는 패킷 타입")
    }
}

func getPlayerRank(score int) string {
    // 조건식을 사용한 switch
    switch {
    case score >= 10000:
        return "다이아몬드"
    case score >= 5000:
        return "플래티넘"
    case score >= 2000:
        return "골드"
    case score >= 1000:
        return "실버"
    default:
        return "브론즈"
    }
}

func processValue(value interface{}) {
    // 타입 switch
    switch v := value.(type) {
    case int:
        fmt.Printf("정수: %d\n", v)
    case string:
        fmt.Printf("문자열: %s\n", v)
    case bool:
        fmt.Printf("불리언: %t\n", v)
    case Player:
        fmt.Printf("플레이어: %s\n", v.Name)
    default:
        fmt.Printf("알 수 없는 타입: %T\n", v)
    }
}

type Player struct {
    Name string
}

func main() {
    // 기본 switch
    state := StateLobby
    fmt.Println("상태:", getStateMessage(state))
    
    // 패킷 처리
    handlePacket(PacketJoinRoom)
    handlePacket(PacketBet)
    
    // 조건식 switch
    score := 7500
    fmt.Printf("점수 %d -> 랭크: %s\n", score, getPlayerRank(score))
    
    // 타입 switch
    processValue(42)
    processValue("Hello")
    processValue(true)
    processValue(Player{Name: "홍길동"})
}
```

**switch 문의 특징:**

```
┌─────────────────────────────────────────────────────────┐
│              Go switch 문의 특징                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  1. break 불필요                                         │
│     각 case는 자동으로 break됨                           │
│     fallthrough 키워드로 다음 case로 진행 가능           │
│                                                          │
│  2. 조건식 switch                                        │
│     switch { case 조건: ... }                            │
│     if-else-if 체인을 대체                               │
│                                                          │
│  3. 타입 switch                                          │
│     interface{} 값의 타입을 검사                         │
│     타입에 따른 다른 처리                                │
│                                                          │
│  4. 여러 값 매칭                                         │
│     case 1, 2, 3:                                        │
│     여러 값을 한 번에 처리                               │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### for 문

Go에는 `while`이나 `do-while`이 없다. `for` 문 하나로 모든 반복을 처리한다.

```go
package main

import "fmt"

func main() {
    // 기본 for 문 (C 스타일)
    fmt.Println("1. 기본 for 문:")
    for i := 0; i < 5; i++ {
        fmt.Printf("  턴 %d\n", i+1)
    }
    
    // while처럼 사용
    fmt.Println("\n2. while 스타일:")
    remainingChips := 1000
    betAmount := 100
    round := 1
    for remainingChips >= betAmount {
        remainingChips -= betAmount
        fmt.Printf("  라운드 %d - 남은 칩: %d\n", round, remainingChips)
        round++
    }
    
    // 무한 루프
    fmt.Println("\n3. 무한 루프 (조건부 탈출):")
    counter := 0
    for {
        counter++
        fmt.Printf("  카운터: %d\n", counter)
        if counter >= 3 {
            break
        }
    }
    
    // range를 사용한 반복
    fmt.Println("\n4. range를 사용한 슬라이스 반복:")
    players := []string{"Player1", "Player2", "Player3", "Player4"}
    for index, player := range players {
        fmt.Printf("  좌석 %d: %s\n", index+1, player)
    }
    
    // 인덱스 무시
    fmt.Println("\n5. 인덱스 무시:")
    for _, player := range players {
        fmt.Printf("  플레이어: %s\n", player)
    }
    
    // 맵 반복
    fmt.Println("\n6. 맵 반복:")
    scores := map[string]int{
        "Player1": 1500,
        "Player2": 2300,
        "Player3": 1800,
    }
    for player, score := range scores {
        fmt.Printf("  %s: %d점\n", player, score)
    }
    
    // continue 사용
    fmt.Println("\n7. continue 사용 (짝수만 출력):")
    for i := 1; i <= 10; i++ {
        if i%2 != 0 {
            continue
        }
        fmt.Printf("  %d ", i)
    }
    fmt.Println()
    
    // 중첩 루프와 레이블
    fmt.Println("\n8. 중첩 루프:")
outer:
    for i := 1; i <= 3; i++ {
        for j := 1; j <= 3; j++ {
            if i*j >= 6 {
                fmt.Println("  6 이상 발견, 종료")
                break outer
            }
            fmt.Printf("  %d x %d = %d\n", i, j, i*j)
        }
    }
}
```

**for 문 활용 패턴:**

게임 서버에서 자주 사용하는 패턴들이다.

```go
package main

import "fmt"

type Player struct {
    ID   int
    Name string
    Chips int
}

func main() {
    players := []Player{
        {ID: 1, Name: "Player1", Chips: 1000},
        {ID: 2, Name: "Player2", Chips: 500},
        {ID: 3, Name: "Player3", Chips: 2000},
        {ID: 4, Name: "Player4", Chips: 100},
    }
    
    // 패턴 1: 필터링
    fmt.Println("칩이 1000 이상인 플레이어:")
    for _, player := range players {
        if player.Chips >= 1000 {
            fmt.Printf("  %s: %d칩\n", player.Name, player.Chips)
        }
    }
    
    // 패턴 2: 변환 (슬라이스 생성)
    fmt.Println("\n플레이어 이름 목록:")
    names := make([]string, 0, len(players))
    for _, player := range players {
        names = append(names, player.Name)
    }
    fmt.Println(names)
    
    // 패턴 3: 집계
    totalChips := 0
    for _, player := range players {
        totalChips += player.Chips
    }
    fmt.Printf("\n전체 칩 합계: %d\n", totalChips)
    
    // 패턴 4: 검색
    targetID := 3
    var foundPlayer *Player
    for i := range players {
        if players[i].ID == targetID {
            foundPlayer = &players[i]
            break
        }
    }
    if foundPlayer != nil {
        fmt.Printf("\n플레이어 검색 성공: %s\n", foundPlayer.Name)
    }
}
```

---

## 2.4 배열, 슬라이스, 맵

### 배열

배열은 고정 크기의 동일한 타입 요소들의 모음이다.

```go
package main

import "fmt"

func main() {
    // 배열 선언과 초기화
    var cards [52]string
    fmt.Printf("카드 배열 크기: %d\n", len(cards))
    
    // 초기값과 함께 선언
    var suits = [4]string{"스페이드", "하트", "다이아몬드", "클로버"}
    fmt.Println("무늬:", suits)
    
    // 크기 자동 추론
    ranks := [...]string{"A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"}
    fmt.Printf("랭크 개수: %d\n", len(ranks))
    
    // 특정 인덱스에 값 할당
    var playerScores [4]int
    playerScores[0] = 100
    playerScores[1] = 200
    playerScores[2] = 150
    playerScores[3] = 300
    fmt.Println("플레이어 점수:", playerScores)
    
    // 배열 순회
    fmt.Println("\n점수 상세:")
    for i, score := range playerScores {
        fmt.Printf("  플레이어 %d: %d점\n", i+1, score)
    }
    
    // 배열은 값 타입 (복사됨)
    original := [3]int{1, 2, 3}
    copied := original
    copied[0] = 100
    fmt.Printf("\n원본: %v\n", original)
    fmt.Printf("복사본: %v\n", copied)
}
```

**배열의 한계:**

배열은 크기가 고정되어 있고 타입의 일부다. `[3]int`와 `[4]int`는 서로 다른 타입이다. 이러한 제약 때문에 실무에서는 슬라이스를 더 많이 사용한다.

### 슬라이스

슬라이스는 동적 크기의 배열이다. 게임 서버에서 가장 많이 사용하는 자료구조 중 하나다.

```go
package main

import "fmt"

func main() {
    // 슬라이스 선언
    var players []string
    fmt.Printf("플레이어 슬라이스 (nil): %v, len: %d, cap: %d\n", 
        players, len(players), cap(players))
    
    // 리터럴로 초기화
    activePlayers := []string{"Player1", "Player2", "Player3"}
    fmt.Printf("활성 플레이어: %v, len: %d, cap: %d\n", 
        activePlayers, len(activePlayers), cap(activePlayers))
    
    // make로 생성 (길이와 용량 지정)
    scores := make([]int, 5)        // 길이 5, 용량 5
    fmt.Printf("점수 슬라이스: %v\n", scores)
    
    chips := make([]int, 0, 10)     // 길이 0, 용량 10
    fmt.Printf("칩 슬라이스: %v, len: %d, cap: %d\n", 
        chips, len(chips), cap(chips))
    
    // append를 사용한 요소 추가
    players = append(players, "Player1")
    players = append(players, "Player2", "Player3")
    fmt.Printf("플레이어 추가 후: %v, len: %d, cap: %d\n", 
        players, len(players), cap(players))
    
    // 슬라이싱
    cards := []string{"A♠", "2♠", "3♠", "4♠", "5♠", "6♠", "7♠"}
    fmt.Println("\n슬라이싱:")
    fmt.Println("  전체:", cards)
    fmt.Println("  처음 3개:", cards[:3])
    fmt.Println("  마지막 3개:", cards[len(cards)-3:])
    fmt.Println("  중간 3개:", cards[2:5])
    
    // 슬라이스 복사
    original := []int{1, 2, 3, 4, 5}
    copied := make([]int, len(original))
    copy(copied, original)
    copied[0] = 100
    fmt.Printf("\n원본: %v\n", original)
    fmt.Printf("복사본: %v\n", copied)
    
    // 슬라이스는 참조 타입
    shared := original
    shared[0] = 999
    fmt.Printf("원본 (shared 수정 후): %v\n", original)
}
```

**슬라이스의 내부 구조:**

```
┌─────────────────────────────────────────────────────────┐
│              슬라이스의 내부 구조                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  슬라이스는 3개의 필드로 구성됨:                         │
│                                                          │
│  ┌──────────────────────────────────────┐               │
│  │ 포인터  │  길이(len)  │  용량(cap)  │               │
│  └──────────────────────────────────────┘               │
│       │                                                  │
│       └──> 실제 배열                                     │
│            [elem0][elem1][elem2][...]                    │
│                                                          │
│  • 포인터: 실제 배열의 시작 위치                         │
│  • 길이: 현재 사용 중인 요소 개수                        │
│  • 용량: 할당된 전체 공간                                │
│                                                          │
│  append 시 용량 초과하면:                                │
│    → 새로운 배열 할당 (보통 2배 크기)                    │
│    → 기존 요소 복사                                      │
│    → 새 요소 추가                                        │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**슬라이스 활용 패턴:**

```go
package main

import "fmt"

type Player struct {
    ID   int
    Name string
}

func main() {
    players := []Player{
        {ID: 1, Name: "Player1"},
        {ID: 2, Name: "Player2"},
        {ID: 3, Name: "Player3"},
        {ID: 4, Name: "Player4"},
    }
    
    // 패턴 1: 특정 요소 제거 (순서 유지)
    removeIndex := 1
    players = append(players[:removeIndex], players[removeIndex+1:]...)
    fmt.Println("Player2 제거 후:", players)
    
    // 패턴 2: 특정 요소 제거 (순서 무시, 더 빠름)
    players = []Player{
        {ID: 1, Name: "Player1"},
        {ID: 2, Name: "Player2"},
        {ID: 3, Name: "Player3"},
        {ID: 4, Name: "Player4"},
    }
    removeIndex = 1
    players[removeIndex] = players[len(players)-1]  // 마지막 요소를 제거 위치로
    players = players[:len(players)-1]              // 마지막 요소 제거
    fmt.Println("Player2 제거 후 (빠른 방법):", players)
    
    // 패턴 3: 필터링
    players = []Player{
        {ID: 1, Name: "Player1"},
        {ID: 2, Name: "Guest1"},
        {ID: 3, Name: "Player2"},
        {ID: 4, Name: "Guest2"},
    }
    registeredPlayers := make([]Player, 0)
    for _, p := range players {
        if len(p.Name) > 6 && p.Name[:6] != "Guest1" && p.Name[:6] != "Guest2" {
            registeredPlayers = append(registeredPlayers, p)
        }
    }
    fmt.Println("등록된 플레이어:", registeredPlayers)
    
    // 패턴 4: 미리 용량 할당 (성능 최적화)
    count := 1000
    // 나쁜 예
    badSlice := []int{}
    for i := 0; i < count; i++ {
        badSlice = append(badSlice, i)  // 여러 번 재할당 발생
    }
    
    // 좋은 예
    goodSlice := make([]int, 0, count)
    for i := 0; i < count; i++ {
        goodSlice = append(goodSlice, i)  // 재할당 없음
    }
    
    fmt.Printf("좋은 예의 용량: %d\n", cap(goodSlice))
}
```

### 맵

맵은 키-값 쌍을 저장하는 해시 테이블이다.

```go
package main

import "fmt"

func main() {
    // 맵 선언
    var playerScores map[string]int
    fmt.Printf("맵 (nil): %v\n", playerScores)
    
    // make로 생성
    playerScores = make(map[string]int)
    fmt.Printf("맵 (초기화): %v\n", playerScores)
    
    // 리터럴로 초기화
    playerChips := map[string]int{
        "Player1": 10000,
        "Player2": 5000,
        "Player3": 7500,
    }
    fmt.Println("플레이어 칩:", playerChips)
    
    // 요소 추가/수정
    playerChips["Player4"] = 12000
    playerChips["Player1"] = 15000  // 기존 값 수정
    fmt.Println("추가/수정 후:", playerChips)
    
    // 요소 접근
    chips := playerChips["Player2"]
    fmt.Printf("Player2의 칩: %d\n", chips)
    
    // 존재하지 않는 키 접근 (제로값 반환)
    unknownChips := playerChips["Player99"]
    fmt.Printf("Player99의 칩: %d (존재하지 않음)\n", unknownChips)
    
    // 키 존재 확인
    chips, exists := playerChips["Player3"]
    if exists {
        fmt.Printf("Player3의 칩: %d\n", chips)
    }
    
    chips, exists = playerChips["Player99"]
    if !exists {
        fmt.Println("Player99는 존재하지 않습니다")
    }
    
    // 요소 삭제
    delete(playerChips, "Player4")
    fmt.Println("Player4 삭제 후:", playerChips)
    
    // 맵 순회 (순서는 보장되지 않음)
    fmt.Println("\n전체 플레이어:")
    for player, chips := range playerChips {
        fmt.Printf("  %s: %d칩\n", player, chips)
    }
    
    // 맵의 길이
    fmt.Printf("\n플레이어 수: %d명\n", len(playerChips))
}
```

**맵 활용 패턴:**

```go
package main

import (
    "fmt"
    "sort"
)

type PlayerInfo struct {
    Level int
    Chips int
    Wins  int
}

func main() {
    // 복잡한 값을 가진 맵
    players := map[string]PlayerInfo{
        "Player1": {Level: 5, Chips: 10000, Wins: 25},
        "Player2": {Level: 3, Chips: 5000, Wins: 10},
        "Player3": {Level: 7, Chips: 15000, Wins: 40},
    }
    
    fmt.Println("플레이어 정보:")
    for name, info := range players {
        fmt.Printf("  %s: 레벨 %d, 칩 %d, 승리 %d\n", 
            name, info.Level, info.Chips, info.Wins)
    }
    
    // 맵의 키를 정렬하여 순회
    fmt.Println("\n이름순 정렬:")
    names := make([]string, 0, len(players))
    for name := range players {
        names = append(names, name)
    }
    sort.Strings(names)
    
    for _, name := range names {
        info := players[name]
        fmt.Printf("  %s: 레벨 %d\n", name, info.Level)
    }
    
    // 중첩 맵 (방 ID -> 플레이어 ID -> 플레이어 정보)
    rooms := make(map[int]map[int]string)
    rooms[1] = make(map[int]string)
    rooms[1][101] = "Player1"
    rooms[1][102] = "Player2"
    
    rooms[2] = make(map[int]string)
    rooms[2][103] = "Player3"
    
    fmt.Println("\n방 정보:")
    for roomID, players := range rooms {
        fmt.Printf("  방 %d:\n", roomID)
        for playerID, name := range players {
            fmt.Printf("    플레이어 %d: %s\n", playerID, name)
        }
    }
    
    // 맵을 사용한 집합(Set) 구현
    activeRooms := make(map[int]bool)
    activeRooms[1] = true
    activeRooms[5] = true
    activeRooms[10] = true
    
    // 존재 확인
    if activeRooms[5] {
        fmt.Println("\n방 5는 활성화 상태입니다")
    }
    
    // 집합 연산
    fmt.Println("\n활성화된 방:")
    for roomID := range activeRooms {
        fmt.Printf("  방 %d\n", roomID)
    }
}
```

**맵 사용 시 주의사항:**

```
┌─────────────────────────────────────────────────────────┐
│              맵 사용 시 주의사항                         │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  1. 순서 보장 안 됨                                      │
│     range로 순회 시 순서가 매번 다를 수 있음             │
│     정렬이 필요하면 키를 별도로 정렬                     │
│                                                          │
│  2. nil 맵에 쓰기 불가                                   │
│     var m map[string]int  // nil                         │
│     m["key"] = 1  // 런타임 패닉!                        │
│     반드시 make로 초기화 필요                            │
│                                                          │
│  3. 동시성 안전하지 않음                                 │
│     여러 고루틴에서 동시 접근 시 문제 발생               │
│     sync.Map 또는 뮤텍스 사용 필요                       │
│                                                          │
│  4. 키는 비교 가능한 타입만                              │
│     슬라이스, 맵, 함수는 키로 사용 불가                  │
│     구조체는 모든 필드가 비교 가능하면 가능              │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 2.5 포인터의 이해와 활용

### 포인터 기본

포인터는 메모리 주소를 저장하는 변수다. Go에서 포인터는 C/C++보다 안전하게 설계되었다.

```go
package main

import "fmt"

func main() {
    // 일반 변수
    playerChips := 10000
    fmt.Printf("변수 값: %d\n", playerChips)
    fmt.Printf("변수 주소: %p\n", &playerChips)
    
    // 포인터 변수 선언
    var chipsPtr *int
    fmt.Printf("포인터 (nil): %v\n", chipsPtr)
    
    // 포인터에 주소 할당
    chipsPtr = &playerChips
    fmt.Printf("포인터 값 (주소): %p\n", chipsPtr)
    fmt.Printf("포인터가 가리키는 값: %d\n", *chipsPtr)
    
    // 포인터를 통한 값 수정
    *chipsPtr = 15000
    fmt.Printf("수정 후 playerChips: %d\n", playerChips)
    
    // new를 사용한 포인터 생성
    scorePtr := new(int)
    fmt.Printf("new로 생성한 포인터: %p, 값: %d\n", scorePtr, *scorePtr)
    *scorePtr = 100
    fmt.Printf("값 할당 후: %d\n", *scorePtr)
}
```

**포인터 연산자:**

- `&`: 주소 연산자 - 변수의 메모리 주소를 얻는다
- `*`: 역참조 연산자 - 포인터가 가리키는 값을 얻거나 수정한다

```
┌─────────────────────────────────────────────────────────┐
│              메모리 레이아웃                             │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  playerChips := 10000                                    │
│                                                          │
│  메모리 주소      값                                     │
│  0x00001234      10000    ← playerChips                 │
│                                                          │
│  chipsPtr := &playerChips                                │
│                                                          │
│  메모리 주소      값                                     │
│  0x00005678      0x00001234  ← chipsPtr                 │
│                  (playerChips의 주소)                    │
│                                                          │
│  *chipsPtr = 15000                                       │
│  → 0x00001234 주소의 값을 15000으로 변경                │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 함수와 포인터

포인터는 함수에서 값을 수정하거나 큰 구조체를 효율적으로 전달할 때 사용한다.

```go
package main

import "fmt"

// 값 전달 (복사본이 전달됨)
func addChipsByValue(chips int, amount int) int {
    chips += amount
    return chips
}

// 포인터 전달 (원본을 수정)
func addChipsByPointer(chips *int, amount int) {
    *chips += amount
}

type Player struct {
    Name  string
    Chips int
    Level int
}

// 값 리시버 (복사본에서 작동, 원본 변경 안 됨)
func (p Player) ShowInfo() {
    fmt.Printf("플레이어: %s, 칩: %d, 레벨: %d\n", p.Name, p.Chips, p.Level)
}

// 포인터 리시버 (원본을 수정)
func (p *Player) AddChips(amount int) {
    p.Chips += amount
}

func (p *Player) LevelUp() {
    p.Level++
}

// 큰 구조체를 포인터로 전달 (효율적)
func processPlayer(p *Player) {
    fmt.Printf("처리 중: %s\n", p.Name)
    // 큰 구조체를 복사하지 않고 참조만 전달
}

func main() {
    // 값 전달 예제
    chips := 10000
    newChips := addChipsByValue(chips, 5000)
    fmt.Printf("값 전달 - 원본: %d, 결과: %d\n", chips, newChips)
    
    // 포인터 전달 예제
    chips = 10000
    addChipsByPointer(&chips, 5000)
    fmt.Printf("포인터 전달 - 원본: %d\n", chips)
    
    // 구조체와 포인터
    player := Player{
        Name:  "홍길동",
        Chips: 10000,
        Level: 1,
    }
    
    player.ShowInfo()
    player.AddChips(5000)
    player.ShowInfo()
    player.LevelUp()
    player.ShowInfo()
    
    // 포인터를 명시적으로 전달
    processPlayer(&player)
}
```

**값 전달 vs 포인터 전달:**

```
┌─────────────────────────────────────────────────────────┐
│         값 전달 vs 포인터 전달                           │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  값 전달 (Pass by Value)                                 │
│    func process(p Player)                                │
│    • 복사본이 전달됨                                     │
│    • 원본은 변경되지 않음                                │
│    • 작은 데이터에 적합                                  │
│    • 안전하지만 비효율적일 수 있음                       │
│                                                          │
│  포인터 전달 (Pass by Pointer)                           │
│    func process(p *Player)                               │
│    • 메모리 주소만 전달됨                                │
│    • 원본을 수정할 수 있음                               │
│    • 큰 구조체에 효율적                                  │
│    • nil 체크 필요                                       │
│                                                          │
│  권장 사항:                                              │
│    • 수정 필요: 포인터 사용                              │
│    • 큰 구조체 (> 몇백 바이트): 포인터 사용              │
│    • 읽기만: 값 전달도 고려 (불변성 보장)                │
│    • 일관성: 한 타입은 한 가지 방식 선택                 │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### nil 포인터 처리

포인터의 가장 큰 위험은 nil 포인터 역참조다. 게임 서버에서는 이를 반드시 확인해야 한다.

```go
package main

import "fmt"

type Player struct {
    Name  string
    Chips int
}

func updatePlayer(p *Player, chips int) error {
    // nil 체크는 필수
    if p == nil {
        return fmt.Errorf("플레이어가 nil입니다")
    }
    
    p.Chips = chips
    return nil
}

func getPlayerInfo(p *Player) string {
    if p == nil {
        return "플레이어 없음"
    }
    return fmt.Sprintf("%s (%d칩)", p.Name, p.Chips)
}

func main() {
    // 정상 케이스
    player := &Player{Name: "홍길동", Chips: 10000}
    err := updatePlayer(player, 15000)
    if err != nil {
        fmt.Println("에러:", err)
    } else {
        fmt.Println(getPlayerInfo(player))
    }
    
    // nil 케이스
    var nilPlayer *Player
    err = updatePlayer(nilPlayer, 5000)
    if err != nil {
        fmt.Println("에러:", err)
    }
    
    fmt.Println(getPlayerInfo(nilPlayer))
    
    // nil 체크 없이 사용하면 패닉 발생
    // nilPlayer.Chips = 1000  // 런타임 패닉!
}
```

### 포인터와 슬라이스/맵

슬라이스와 맵은 이미 참조 타입이므로 포인터로 전달할 필요가 거의 없다.

```go
package main

import "fmt"

func addPlayer(players []string, name string) []string {
    // 슬라이스는 참조 타입이지만, 슬라이스 헤더 자체는 값으로 전달됨
    // append는 새 슬라이스를 반환할 수 있으므로 반환값을 사용해야 함
    return append(players, name)
}

func updatePlayerChips(playerChips map[string]int, name string, chips int) {
    // 맵은 참조 타입 - 함수 내에서 수정하면 원본이 변경됨
    playerChips[name] = chips
}

func main() {
    // 슬라이스
    players := []string{"Player1", "Player2"}
    fmt.Println("초기 플레이어:", players)
    
    players = addPlayer(players, "Player3")
    fmt.Println("추가 후:", players)
    
    // 맵
    playerChips := map[string]int{
        "Player1": 1000,
        "Player2": 2000,
    }
    fmt.Println("\n초기 칩:", playerChips)
    
    updatePlayerChips(playerChips, "Player1", 5000)
    updatePlayerChips(playerChips, "Player3", 3000)
    fmt.Println("수정 후:", playerChips)
}
```

**슬라이스와 포인터:**

슬라이스는 특별한 경우다. 슬라이스 자체는 참조 타입이지만, `append`를 사용할 때는 반환값을 사용해야 한다.

```go
package main

import "fmt"

// 잘못된 예
func addPlayerWrong(players []string, name string) {
    players = append(players, name)  // 호출자의 슬라이스는 변경 안 됨
}

// 올바른 예 1: 반환값 사용
func addPlayerCorrect1(players []string, name string) []string {
    return append(players, name)
}

// 올바른 예 2: 포인터 사용 (슬라이스를 수정해야 할 때)
func addPlayerCorrect2(players *[]string, name string) {
    *players = append(*players, name)
}

func main() {
    // 잘못된 예
    players := []string{"Player1"}
    addPlayerWrong(players, "Player2")
    fmt.Println("잘못된 예:", players)  // Player2가 추가되지 않음
    
    // 올바른 예 1
    players = []string{"Player1"}
    players = addPlayerCorrect1(players, "Player2")
    fmt.Println("올바른 예 1:", players)
    
    // 올바른 예 2
    players = []string{"Player1"}
    addPlayerCorrect2(&players, "Player2")
    fmt.Println("올바른 예 2:", players)
}
```

### 게임 서버에서의 포인터 활용

실제 게임 서버 코드에서 포인터가 어떻게 사용되는지 예제를 통해 살펴본다.

```go
package main

import "fmt"

type Player struct {
    ID    int
    Name  string
    Chips int
}

type Room struct {
    ID      int
    Players []*Player  // 포인터 슬라이스
    MaxSize int
}

// 방에 플레이어 추가
func (r *Room) AddPlayer(p *Player) error {
    if len(r.Players) >= r.MaxSize {
        return fmt.Errorf("방이 가득 찼습니다")
    }
    
    r.Players = append(r.Players, p)
    return nil
}

// 방에서 플레이어 제거
func (r *Room) RemovePlayer(playerID int) bool {
    for i, p := range r.Players {
        if p.ID == playerID {
            // 순서를 유지하지 않는 빠른 제거
            r.Players[i] = r.Players[len(r.Players)-1]
            r.Players = r.Players[:len(r.Players)-1]
            return true
        }
    }
    return false
}

// 방의 모든 플레이어에게 칩 분배
func (r *Room) DistributeChips(totalChips int) {
    if len(r.Players) == 0 {
        return
    }
    
    chipsPerPlayer := totalChips / len(r.Players)
    for _, p := range r.Players {
        p.Chips += chipsPerPlayer  // 포인터를 통해 원본 수정
    }
}

// 방 정보 출력
func (r *Room) ShowInfo() {
    fmt.Printf("방 %d (%d/%d):\n", r.ID, len(r.Players), r.MaxSize)
    for _, p := range r.Players {
        fmt.Printf("  - %s (ID: %d, 칩: %d)\n", p.Name, p.ID, p.Chips)
    }
}

func main() {
    // 플레이어 생성
    player1 := &Player{ID: 1, Name: "홍길동", Chips: 0}
    player2 := &Player{ID: 2, Name: "김철수", Chips: 0}
    player3 := &Player{ID: 3, Name: "이영희", Chips: 0}
    
    // 방 생성
    room := &Room{
        ID:      1,
        Players: make([]*Player, 0, 4),
        MaxSize: 4,
    }
    
    // 플레이어 추가
    room.AddPlayer(player1)
    room.AddPlayer(player2)
    room.AddPlayer(player3)
    
    room.ShowInfo()
    
    // 칩 분배
    fmt.Println("\n10000칩 분배:")
    room.DistributeChips(10000)
    room.ShowInfo()
    
    // 플레이어 제거
    fmt.Println("\n플레이어 2 제거:")
    room.RemovePlayer(2)
    room.ShowInfo()
    
    // 원본 플레이어도 변경되었는지 확인
    fmt.Printf("\n원본 player1 칩: %d\n", player1.Chips)
}
```

**포인터 슬라이스의 장점:**

1. **메모리 효율성**: 큰 구조체를 복사하지 않고 참조만 저장한다.
2. **원본 수정 가능**: 슬라이스의 요소를 통해 원본 데이터를 수정할 수 있다.
3. **공유**: 여러 컬렉션에서 같은 객체를 참조할 수 있다.

---

## 정리

이 장에서는 Go 언어의 핵심 문법을 학습했다.

1. **변수와 타입 시스템**: 정적 타입 언어인 Go의 타입 시스템과 타입 추론, 상수와 iota를 이용한 열거형 생성 방법을 배웠다.

2. **함수와 메서드**: 다중 반환값, 명명된 반환값, 가변 인자, 고차 함수와 클로저, 그리고 메서드의 값/포인터 리시버 차이를 이해했다.

3. **제어문**: if, switch, for 문의 다양한 활용 방법을 익혔다. 특히 Go만의 독특한 기능들 (if 문의 초기화 문장, 조건 없는 switch, range를 이용한 반복)을 배웠다.

4. **배열, 슬라이스, 맵**: 게임 서버에서 가장 많이 사용하는 자료구조들을 다뤘다. 배열의 한계와 슬라이스의 내부 구조, 맵의 특성과 주의사항을 학습했다.

5. **포인터**: 메모리 주소를 다루는 포인터의 개념과 함수에서의 활용, nil 처리, 그리고 게임 서버에서 포인터를 효과적으로 사용하는 방법을 배웠다.

다음 장에서는 구조체와 인터페이스를 통해 더 복잡한 데이터 구조를 만들고, Go의 강력한 다형성 기능을 활용하는 방법을 배울 것이다.


# Chapter 3. 구조체와 인터페이스

게임 서버 개발에서 구조체(struct)와 인터페이스(interface)는 핵심적인 역할을 한다. 구조체는 플레이어, 게임 방, 패킷 등의 복잡한 데이터를 표현하고, 인터페이스는 서로 다른 타입들이 공통된 동작을 수행할 수 있게 한다. 이 장에서는 게임 서버 개발에 필요한 구조체와 인터페이스의 모든 것을 다룬다.

## 3.1 구조체 정의와 메서드

### 구조체의 기본 개념

구조체는 관련된 데이터를 하나로 묶는 사용자 정의 타입이다. 게임 서버에서는 플레이어 정보, 게임 상태, 네트워크 패킷 등을 구조체로 표현한다.

```
┌─────────────────────────────┐
│        Player               │
├─────────────────────────────┤
│ - ID: string                │
│ - Name: string              │
│ - Chips: int64              │
│ - Position: int             │
├─────────────────────────────┤
│ + Bet(amount int64)         │
│ + Fold()                    │
│ + GetInfo() PlayerInfo      │
└─────────────────────────────┘
```

### 구조체 선언과 초기화

```go
package main

import (
	"fmt"
	"time"
)

// 플레이어 구조체 정의
type Player struct {
	ID       string
	Name     string
	Chips    int64
	Position int
	IsActive bool
	JoinedAt time.Time
}

func main() {
	// 방법 1: 필드명을 명시한 초기화 (권장)
	player1 := Player{
		ID:       "player001",
		Name:     "Alice",
		Chips:    10000,
		Position: 0,
		IsActive: true,
		JoinedAt: time.Now(),
	}

	// 방법 2: 순서대로 초기화 (비권장 - 필드 순서가 바뀌면 문제 발생)
	player2 := Player{
		"player002",
		"Bob",
		15000,
		1,
		true,
		time.Now(),
	}

	// 방법 3: 일부 필드만 초기화 (나머지는 제로값)
	player3 := Player{
		ID:   "player003",
		Name: "Charlie",
	}

	// 방법 4: new 키워드 사용 (포인터 반환)
	player4 := new(Player)
	player4.ID = "player004"
	player4.Name = "David"
	player4.Chips = 20000

	fmt.Printf("Player1: %+v\n", player1)
	fmt.Printf("Player3 (일부만 초기화): %+v\n", player3) // Chips=0, IsActive=false
	fmt.Printf("Player4 (포인터): %+v\n", player4)
}
```

위 코드에서 `player3`의 경우 초기화하지 않은 필드는 제로값(zero value)으로 설정된다. `int64`는 0, `bool`은 false, `time.Time`은 제로 시간이 된다.

### 구조체 메서드

메서드는 특정 타입과 연결된 함수다. 게임 서버에서 플레이어가 베팅하거나 폴드하는 동작을 메서드로 구현할 수 있다.

```go
package main

import "fmt"

type Player struct {
	ID       string
	Name     string
	Chips    int64
	Position int
	IsActive bool
	CurrentBet int64
}

// 값 리시버 메서드 - 원본을 수정하지 않음
func (p Player) GetInfo() string {
	return fmt.Sprintf("Player %s (ID: %s) - Chips: %d", p.Name, p.ID, p.Chips)
}

// 포인터 리시버 메서드 - 원본을 수정함
func (p *Player) Bet(amount int64) error {
	if amount <= 0 {
		return fmt.Errorf("베팅 금액은 양수여야 한다")
	}
	if p.Chips < amount {
		return fmt.Errorf("칩이 부족하다 (보유: %d, 필요: %d)", p.Chips, amount)
	}
	
	p.Chips -= amount
	p.CurrentBet += amount
	return nil
}

// 포인터 리시버 메서드
func (p *Player) Fold() {
	p.IsActive = false
	p.CurrentBet = 0
}

// 포인터 리시버 메서드
func (p *Player) Win(amount int64) {
	p.Chips += amount
	p.CurrentBet = 0
}

func main() {
	player := Player{
		ID:       "player001",
		Name:     "Alice",
		Chips:    10000,
		Position: 0,
		IsActive: true,
	}

	// 값 리시버 메서드 호출
	info := player.GetInfo()
	fmt.Println(info)

	// 포인터 리시버 메서드 호출
	err := player.Bet(500)
	if err != nil {
		fmt.Printf("베팅 실패: %v\n", err)
	} else {
		fmt.Printf("베팅 성공. 남은 칩: %d, 현재 베팅: %d\n", 
			player.Chips, player.CurrentBet)
	}

	// 추가 베팅
	player.Bet(1000)
	fmt.Printf("추가 베팅 후 - 칩: %d, 베팅액: %d\n", 
		player.Chips, player.CurrentBet)

	// 폴드
	player.Fold()
	fmt.Printf("폴드 후 - IsActive: %v, CurrentBet: %d\n", 
		player.IsActive, player.CurrentBet)
}
```

**값 리시버 vs 포인터 리시버 선택 기준:**

1. **포인터 리시버를 사용해야 할 때:**
   - 메서드가 구조체 필드를 수정해야 할 때
   - 구조체가 크기가 클 때 (복사 비용 절감)
   - 일관성을 위해 (한 타입에 포인터 리시버가 하나라도 있으면 모두 포인터 리시버 사용 권장)

2. **값 리시버를 사용해야 할 때:**
   - 메서드가 구조체를 수정하지 않을 때
   - 구조체가 작고 기본 타입처럼 동작해야 할 때
   - 동시성 안전성이 중요할 때 (복사본을 사용하므로)

### 중첩 구조체

게임 서버에서는 복잡한 데이터 구조가 필요하다. 중첩 구조체를 사용하면 데이터를 계층적으로 표현할 수 있다.

```go
package main

import (
	"fmt"
	"time"
)

// 위치 정보
type Position struct {
	SeatNumber int
	TableID    string
}

// 통계 정보
type Statistics struct {
	GamesPlayed  int
	GamesWon     int
	TotalWinnings int64
	WinRate      float64
}

// 플레이어 구조체 (중첩 구조체 포함)
type Player struct {
	ID         string
	Name       string
	Chips      int64
	Pos        Position    // 중첩 구조체
	Stats      Statistics  // 중첩 구조체
	IsActive   bool
	JoinedAt   time.Time
	CurrentBet int64
}

// 통계 업데이트 메서드
func (s *Statistics) UpdateAfterGame(won bool, winnings int64) {
	s.GamesPlayed++
	if won {
		s.GamesWon++
		s.TotalWinnings += winnings
	}
	if s.GamesPlayed > 0 {
		s.WinRate = float64(s.GamesWon) / float64(s.GamesPlayed) * 100
	}
}

// 플레이어 정보 출력 메서드
func (p *Player) PrintInfo() {
	fmt.Printf("=== 플레이어 정보 ===\n")
	fmt.Printf("ID: %s\n", p.ID)
	fmt.Printf("이름: %s\n", p.Name)
	fmt.Printf("칩: %d\n", p.Chips)
	fmt.Printf("위치: 테이블 %s, 좌석 %d\n", p.Pos.TableID, p.Pos.SeatNumber)
	fmt.Printf("통계: %d게임 플레이, %d승, 승률 %.2f%%\n", 
		p.Stats.GamesPlayed, p.Stats.GamesWon, p.Stats.WinRate)
	fmt.Printf("총 수익: %d\n", p.Stats.TotalWinnings)
	fmt.Println()
}

func main() {
	player := Player{
		ID:    "player001",
		Name:  "Alice",
		Chips: 10000,
		Pos: Position{
			SeatNumber: 3,
			TableID:    "table-001",
		},
		Stats: Statistics{
			GamesPlayed: 0,
			GamesWon:    0,
		},
		IsActive: true,
		JoinedAt: time.Now(),
	}

	player.PrintInfo()

	// 게임 결과 반영
	player.Stats.UpdateAfterGame(true, 1500)
	player.Chips += 1500

	player.Stats.UpdateAfterGame(false, 0)
	
	player.Stats.UpdateAfterGame(true, 800)
	player.Chips += 800

	player.PrintInfo()
}
```

### 익명 필드와 임베딩

Go는 상속을 지원하지 않지만, 구조체 임베딩을 통해 유사한 효과를 낼 수 있다.

```go
package main

import "fmt"

// 기본 엔티티
type Entity struct {
	ID        string
	CreatedAt int64
	UpdatedAt int64
}

// Entity의 메서드
func (e *Entity) SetID(id string) {
	e.ID = id
}

func (e *Entity) GetID() string {
	return e.ID
}

// Player는 Entity를 임베딩한다
type Player struct {
	Entity          // 익명 필드 (임베딩)
	Name   string
	Chips  int64
}

// Room도 Entity를 임베딩한다
type Room struct {
	Entity          // 익명 필드 (임베딩)
	Title      string
	MaxPlayers int
	Players    []*Player
}

func main() {
	player := Player{
		Entity: Entity{
			ID:        "player001",
			CreatedAt: 1234567890,
		},
		Name:  "Alice",
		Chips: 10000,
	}

	// 임베딩된 Entity의 필드와 메서드에 직접 접근 가능
	fmt.Println("플레이어 ID:", player.ID) // player.Entity.ID와 동일
	fmt.Println("생성 시간:", player.CreatedAt)

	// 임베딩된 Entity의 메서드 호출
	fmt.Println("GetID():", player.GetID())

	// 명시적으로 Entity에 접근도 가능
	fmt.Println("Entity.ID:", player.Entity.ID)

	room := Room{
		Entity: Entity{
			ID:        "room001",
			CreatedAt: 1234567891,
		},
		Title:      "VIP Room",
		MaxPlayers: 6,
	}

	fmt.Println("\n방 ID:", room.ID)
	fmt.Println("방 제목:", room.Title)
	fmt.Println("GetID():", room.GetID())
}
```

임베딩을 사용하면 중복 코드를 줄이고 공통 기능을 재사용할 수 있다. 게임 서버에서 모든 엔티티가 ID와 생성시간을 가져야 한다면, 기본 Entity를 만들고 이를 임베딩하는 방식이 효율적이다.

## 3.2 인터페이스의 개념과 활용

### 인터페이스 기본

인터페이스는 메서드의 집합을 정의한 타입이다. 어떤 타입이든 인터페이스가 정의한 모든 메서드를 구현하면 그 인터페이스를 만족한다. Go의 인터페이스는 암시적(implicit)으로 구현된다.

```
┌──────────────────┐         ┌──────────────────┐
│   GameEntity     │         │   Serializable   │
├──────────────────┤         ├──────────────────┤
│ + Update()       │         │ + Marshal()      │
│ + GetID() string │         │ + Unmarshal()    │
└──────────────────┘         └──────────────────┘
         △                            △
         │                            │
         └────────────┬───────────────┘
                      │
              ┌───────────────┐
              │    Player     │
              ├───────────────┤
              │ - ID: string  │
              │ - Name: string│
              └───────────────┘
```

```go
package main

import (
	"encoding/json"
	"fmt"
)

// Serializable 인터페이스 - 직렬화 가능한 객체
type Serializable interface {
	Marshal() ([]byte, error)
	Unmarshal(data []byte) error
}

// GameEntity 인터페이스 - 게임 엔티티의 공통 동작
type GameEntity interface {
	GetID() string
	GetType() string
	Update(deltaTime float64)
}

// Player 구조체
type Player struct {
	ID       string  `json:"id"`
	Name     string  `json:"name"`
	Chips    int64   `json:"chips"`
	Position int     `json:"position"`
}

// Player가 Serializable 인터페이스를 구현
func (p *Player) Marshal() ([]byte, error) {
	return json.Marshal(p)
}

func (p *Player) Unmarshal(data []byte) error {
	return json.Unmarshal(data, p)
}

// Player가 GameEntity 인터페이스를 구현
func (p *Player) GetID() string {
	return p.ID
}

func (p *Player) GetType() string {
	return "Player"
}

func (p *Player) Update(deltaTime float64) {
	// 플레이어 상태 업데이트 로직
	fmt.Printf("Player %s updated (delta: %.2f)\n", p.Name, deltaTime)
}

// 인터페이스를 받는 범용 함수
func SaveEntity(s Serializable) error {
	data, err := s.Marshal()
	if err != nil {
		return fmt.Errorf("직렬화 실패: %w", err)
	}
	fmt.Printf("저장된 데이터: %s\n", string(data))
	return nil
}

func ProcessEntity(entity GameEntity) {
	fmt.Printf("처리 중: %s (타입: %s)\n", entity.GetID(), entity.GetType())
	entity.Update(0.016) // 60fps 기준 약 16ms
}

func main() {
	player := &Player{
		ID:       "player001",
		Name:     "Alice",
		Chips:    10000,
		Position: 0,
	}

	// Player는 Serializable 인터페이스를 구현하므로 SaveEntity에 전달 가능
	err := SaveEntity(player)
	if err != nil {
		fmt.Printf("에러: %v\n", err)
	}

	// Player는 GameEntity 인터페이스를 구현하므로 ProcessEntity에 전달 가능
	ProcessEntity(player)
}
```

### 빈 인터페이스와 타입 단언

빈 인터페이스(`interface{}` 또는 Go 1.18부터 `any`)는 모든 타입을 받을 수 있다. 게임 서버에서 다양한 타입의 패킷을 처리할 때 유용하다.

```go
package main

import "fmt"

// 패킷 인터페이스
type Packet interface {
	GetPacketID() uint16
}

// 로그인 요청 패킷
type LoginRequest struct {
	PacketID uint16
	UserID   string
	Password string
}

func (l *LoginRequest) GetPacketID() uint16 {
	return l.PacketID
}

// 베팅 요청 패킷
type BetRequest struct {
	PacketID uint16
	Amount   int64
}

func (b *BetRequest) GetPacketID() uint16 {
	return b.PacketID
}

// 채팅 메시지 패킷
type ChatMessage struct {
	PacketID uint16
	Message  string
}

func (c *ChatMessage) GetPacketID() uint16 {
	return c.PacketID
}

// 범용 패킷 핸들러
func HandlePacket(packet Packet) {
	fmt.Printf("패킷 ID: %d 처리 중\n", packet.GetPacketID())

	// 타입 단언(Type Assertion) - 방법 1
	if loginReq, ok := packet.(*LoginRequest); ok {
		fmt.Printf("로그인 요청: UserID=%s\n", loginReq.UserID)
		return
	}

	// 타입 단언 - 방법 2 (타입 스위치)
	switch p := packet.(type) {
	case *BetRequest:
		fmt.Printf("베팅 요청: Amount=%d\n", p.Amount)
	case *ChatMessage:
		fmt.Printf("채팅 메시지: %s\n", p.Message)
	default:
		fmt.Printf("알 수 없는 패킷 타입\n")
	}
}

// any(빈 인터페이스)를 사용한 범용 함수
func PrintValue(value any) {
	switch v := value.(type) {
	case int:
		fmt.Printf("정수: %d\n", v)
	case string:
		fmt.Printf("문자열: %s\n", v)
	case *Player:
		fmt.Printf("플레이어: %s (칩: %d)\n", v.Name, v.Chips)
	default:
		fmt.Printf("알 수 없는 타입: %T\n", v)
	}
}

type Player struct {
	Name  string
	Chips int64
}

func main() {
	// 다양한 패킷 처리
	login := &LoginRequest{
		PacketID: 1001,
		UserID:   "alice",
		Password: "secret",
	}
	HandlePacket(login)

	bet := &BetRequest{
		PacketID: 2001,
		Amount:   500,
	}
	HandlePacket(bet)

	chat := &ChatMessage{
		PacketID: 3001,
		Message:  "Hello, world!",
	}
	HandlePacket(chat)

	fmt.Println("\n=== any 타입 테스트 ===")
	PrintValue(42)
	PrintValue("Go 언어")
	PrintValue(&Player{Name: "Bob", Chips: 15000})
	PrintValue(3.14)
}
```

**타입 단언 주의사항:**
타입 단언이 실패하면 패닉이 발생한다. 안전하게 사용하려면 항상 두 번째 반환값(ok)을 확인해야 한다.

```go
// 위험 - 타입이 맞지 않으면 패닉 발생
loginReq := packet.(*LoginRequest)

// 안전 - ok를 확인
if loginReq, ok := packet.(*LoginRequest); ok {
    // 안전하게 사용
}
```

## 3.3 다형성 구현

인터페이스를 활용하면 다형성(polymorphism)을 구현할 수 있다. 서로 다른 타입이 동일한 인터페이스를 구현하여 같은 방식으로 처리될 수 있다.

```go
package main

import (
	"fmt"
	"math/rand"
	"time"
)

// AI 전략 인터페이스
type AIStrategy interface {
	Decide(chips int64, currentBet int64) (action string, amount int64)
	GetName() string
}

// 공격적인 AI
type AggressiveAI struct {
	Name string
}

func (a *AggressiveAI) Decide(chips int64, currentBet int64) (string, int64) {
	// 70% 확률로 레이즈
	if rand.Float64() < 0.7 {
		raiseAmount := currentBet + int64(rand.Intn(500)+100)
		if raiseAmount <= chips {
			return "raise", raiseAmount
		}
	}
	return "call", currentBet
}

func (a *AggressiveAI) GetName() string {
	return a.Name
}

// 보수적인 AI
type ConservativeAI struct {
	Name string
}

func (c *ConservativeAI) Decide(chips int64, currentBet int64) (string, int64) {
	// 30% 확률로 폴드
	if rand.Float64() < 0.3 {
		return "fold", 0
	}
	// 그 외에는 콜
	if currentBet <= chips {
		return "call", currentBet
	}
	return "fold", 0
}

func (c *ConservativeAI) GetName() string {
	return c.Name
}

// 랜덤 AI
type RandomAI struct {
	Name string
}

func (r *RandomAI) Decide(chips int64, currentBet int64) (string, int64) {
	actions := []string{"fold", "call", "raise"}
	action := actions[rand.Intn(len(actions))]
	
	switch action {
	case "fold":
		return "fold", 0
	case "call":
		if currentBet <= chips {
			return "call", currentBet
		}
		return "fold", 0
	case "raise":
		raiseAmount := currentBet + int64(rand.Intn(1000)+100)
		if raiseAmount <= chips {
			return "raise", raiseAmount
		}
		return "call", currentBet
	}
	return "fold", 0
}

func (r *RandomAI) GetName() string {
	return r.Name
}

// AI 플레이어
type AIPlayer struct {
	ID       string
	Chips    int64
	Strategy AIStrategy // 인터페이스 타입
	IsActive bool
}

// AI 플레이어의 행동 결정
func (p *AIPlayer) MakeDecision(currentBet int64) {
	if !p.IsActive {
		fmt.Printf("%s는 이미 폴드했다\n", p.ID)
		return
	}

	action, amount := p.Strategy.Decide(p.Chips, currentBet)
	
	fmt.Printf("%s (%s 전략) - ", p.ID, p.Strategy.GetName())
	
	switch action {
	case "fold":
		fmt.Println("폴드")
		p.IsActive = false
	case "call":
		fmt.Printf("콜 %d\n", amount)
		p.Chips -= amount
	case "raise":
		fmt.Printf("레이즈 %d\n", amount)
		p.Chips -= amount
	}
}

func main() {
	rand.Seed(time.Now().UnixNano())

	// 서로 다른 전략을 가진 AI 플레이어들
	players := []*AIPlayer{
		{
			ID:       "AI_1",
			Chips:    10000,
			Strategy: &AggressiveAI{Name: "공격형"},
			IsActive: true,
		},
		{
			ID:       "AI_2",
			Chips:    10000,
			Strategy: &ConservativeAI{Name: "보수형"},
			IsActive: true,
		},
		{
			ID:       "AI_3",
			Chips:    10000,
			Strategy: &RandomAI{Name: "랜덤형"},
			IsActive: true,
		},
	}

	// 베팅 라운드 시뮬레이션
	currentBet := int64(100)
	fmt.Printf("=== 베팅 라운드 (현재 베팅: %d) ===\n", currentBet)
	
	for _, player := range players {
		player.MakeDecision(currentBet)
	}

	fmt.Println("\n=== 레이즈 후 (현재 베팅: 500) ===")
	currentBet = 500
	
	for _, player := range players {
		player.MakeDecision(currentBet)
	}

	// 전략 동적 변경 (다형성의 장점)
	fmt.Println("\n=== AI_1의 전략을 보수형으로 변경 ===")
	players[0].Strategy = &ConservativeAI{Name: "보수형"}
	players[0].IsActive = true
	players[0].MakeDecision(currentBet)
}
```

위 예제는 다형성의 강력함을 보여준다. `AIStrategy` 인터페이스를 구현하는 서로 다른 전략들을 런타임에 교체할 수 있으며, `AIPlayer`는 구체적인 전략 구현을 알 필요가 없다. 이는 게임 서버에서 매우 유용한데, 예를 들어 난이도에 따라 AI 전략을 바꾸거나, A/B 테스트를 위해 다른 알고리즘을 적용할 수 있다.

## 3.4 임베딩을 통한 코드 재사용

### 구조체 임베딩의 활용

게임 서버에서 공통 기능을 가진 여러 타입을 만들 때, 구조체 임베딩을 사용하면 코드 중복을 크게 줄일 수 있다.

```go
package main

import (
	"fmt"
	"sync"
	"time"
)

// 기본 엔티티 - 모든 게임 객체가 공유하는 기능
type BaseEntity struct {
	mu        sync.RWMutex
	id        string
	createdAt time.Time
	updatedAt time.Time
}

func NewBaseEntity(id string) BaseEntity {
	now := time.Now()
	return BaseEntity{
		id:        id,
		createdAt: now,
		updatedAt: now,
	}
}

func (b *BaseEntity) GetID() string {
	b.mu.RLock()
	defer b.mu.RUnlock()
	return b.id
}

func (b *BaseEntity) UpdateTimestamp() {
	b.mu.Lock()
	defer b.mu.Unlock()
	b.updatedAt = time.Now()
}

func (b *BaseEntity) GetCreatedAt() time.Time {
	b.mu.RLock()
	defer b.mu.RUnlock()
	return b.createdAt
}

func (b *BaseEntity) GetUpdatedAt() time.Time {
	b.mu.RLock()
	defer b.mu.RUnlock()
	return b.updatedAt
}

// 플레이어 - BaseEntity를 임베딩
type Player struct {
	BaseEntity
	name     string
	chips    int64
	isActive bool
}

func NewPlayer(id, name string, chips int64) *Player {
	return &Player{
		BaseEntity: NewBaseEntity(id),
		name:       name,
		chips:      chips,
		isActive:   true,
	}
}

func (p *Player) Bet(amount int64) error {
	if amount > p.chips {
		return fmt.Errorf("칩이 부족하다")
	}
	p.chips -= amount
	p.UpdateTimestamp() // BaseEntity의 메서드 호출
	return nil
}

func (p *Player) GetInfo() string {
	return fmt.Sprintf("Player[ID:%s, Name:%s, Chips:%d, Created:%s]",
		p.GetID(), p.name, p.chips, p.GetCreatedAt().Format("15:04:05"))
}

// 게임 방 - BaseEntity를 임베딩
type Room struct {
	BaseEntity
	title      string
	maxPlayers int
	players    []*Player
	mu         sync.RWMutex
}

func NewRoom(id, title string, maxPlayers int) *Room {
	return &Room{
		BaseEntity: NewBaseEntity(id),
		title:      title,
		maxPlayers: maxPlayers,
		players:    make([]*Player, 0, maxPlayers),
	}
}

func (r *Room) AddPlayer(player *Player) error {
	r.mu.Lock()
	defer r.mu.Unlock()

	if len(r.players) >= r.maxPlayers {
		return fmt.Errorf("방이 가득 찼다")
	}

	r.players = append(r.players, player)
	r.UpdateTimestamp() // BaseEntity의 메서드 호출
	return nil
}

func (r *Room) GetPlayerCount() int {
	r.mu.RLock()
	defer r.mu.RUnlock()
	return len(r.players)
}

func (r *Room) GetInfo() string {
	r.mu.RLock()
	defer r.mu.RUnlock()
	return fmt.Sprintf("Room[ID:%s, Title:%s, Players:%d/%d, Created:%s]",
		r.GetID(), r.title, len(r.players), r.maxPlayers,
		r.GetCreatedAt().Format("15:04:05"))
}

func main() {
	// 플레이어 생성
	player1 := NewPlayer("player001", "Alice", 10000)
	player2 := NewPlayer("player002", "Bob", 15000)

	fmt.Println(player1.GetInfo())
	fmt.Println(player2.GetInfo())

	// 베팅
	time.Sleep(time.Second)
	err := player1.Bet(500)
	if err != nil {
		fmt.Printf("베팅 실패: %v\n", err)
	}

	fmt.Printf("\n베팅 후:\n")
	fmt.Println(player1.GetInfo())
	fmt.Printf("마지막 업데이트: %s\n", 
		player1.GetUpdatedAt().Format("15:04:05.000"))

	// 방 생성
	room := NewRoom("room001", "VIP Room", 6)
	fmt.Printf("\n%s\n", room.GetInfo())

	// 플레이어 추가
	time.Sleep(time.Second)
	room.AddPlayer(player1)
	room.AddPlayer(player2)

	fmt.Printf("\n플레이어 추가 후:\n")
	fmt.Println(room.GetInfo())
	fmt.Printf("마지막 업데이트: %s\n", 
		room.GetUpdatedAt().Format("15:04:05.000"))
}
```

### 인터페이스 임베딩

구조체뿐만 아니라 인터페이스도 임베딩할 수 있다. 이를 통해 여러 인터페이스를 조합한 새로운 인터페이스를 만들 수 있다.

```go
package main

import (
	"encoding/json"
	"fmt"
)

// 읽기 가능한 인터페이스
type Readable interface {
	Read() ([]byte, error)
}

// 쓰기 가능한 인터페이스
type Writable interface {
	Write(data []byte) error
}

// 읽기/쓰기 가능한 인터페이스 (인터페이스 임베딩)
type ReadWritable interface {
	Readable
	Writable
}

// 직렬화 가능한 인터페이스
type Serializable interface {
	Marshal() ([]byte, error)
	Unmarshal(data []byte) error
}

// 저장 가능한 인터페이스 (여러 인터페이스 임베딩)
type Persistable interface {
	Serializable
	GetID() string
	Validate() error
}

// 게임 상태 구조체
type GameState struct {
	ID          string `json:"id"`
	RoomID      string `json:"room_id"`
	CurrentTurn int    `json:"current_turn"`
	Pot         int64  `json:"pot"`
}

// GameState가 Persistable 인터페이스를 구현
func (g *GameState) Marshal() ([]byte, error) {
	return json.Marshal(g)
}

func (g *GameState) Unmarshal(data []byte) error {
	return json.Unmarshal(data, g)
}

func (g *GameState) GetID() string {
	return g.ID
}

func (g *GameState) Validate() error {
	if g.ID == "" {
		return fmt.Errorf("ID가 비어있다")
	}
	if g.RoomID == "" {
		return fmt.Errorf("RoomID가 비어있다")
	}
	return nil
}

// 저장소 인터페이스
type Repository interface {
	Save(entity Persistable) error
	Load(id string) (Persistable, error)
}

// 메모리 저장소
type MemoryRepository struct {
	data map[string][]byte
}

func NewMemoryRepository() *MemoryRepository {
	return &MemoryRepository{
		data: make(map[string][]byte),
	}
}

func (r *MemoryRepository) Save(entity Persistable) error {
	// 유효성 검사
	if err := entity.Validate(); err != nil {
		return fmt.Errorf("유효성 검사 실패: %w", err)
	}

	// 직렬화
	data, err := entity.Marshal()
	if err != nil {
		return fmt.Errorf("직렬화 실패: %w", err)
	}

	// 저장
	r.data[entity.GetID()] = data
	fmt.Printf("저장 완료: ID=%s, Size=%d bytes\n", entity.GetID(), len(data))
	return nil
}

func (r *MemoryRepository) Load(id string) (Persistable, error) {
	data, exists := r.data[id]
	if !exists {
		return nil, fmt.Errorf("ID %s를 찾을 수 없다", id)
	}

	// 새 GameState 생성 및 역직렬화
	state := &GameState{}
	if err := state.Unmarshal(data); err != nil {
		return nil, fmt.Errorf("역직렬화 실패: %w", err)
	}

	fmt.Printf("로드 완료: ID=%s\n", id)
	return state, nil
}

func main() {
	repo := NewMemoryRepository()

	// 게임 상태 생성
	state := &GameState{
		ID:          "state001",
		RoomID:      "room001",
		CurrentTurn: 3,
		Pot:         5000,
	}

	// 저장
	fmt.Println("=== 저장 ===")
	err := repo.Save(state)
	if err != nil {
		fmt.Printf("저장 실패: %v\n", err)
		return
	}

	// 로드
	fmt.Println("\n=== 로드 ===")
	loaded, err := repo.Load("state001")
	if err != nil {
		fmt.Printf("로드 실패: %v\n", err)
		return
	}

	// 타입 단언하여 사용
	if loadedState, ok := loaded.(*GameState); ok {
		fmt.Printf("로드된 상태: %+v\n", loadedState)
	}

	// 잘못된 ID로 로드 시도
	fmt.Println("\n=== 존재하지 않는 ID 로드 시도 ===")
	_, err = repo.Load("invalid_id")
	if err != nil {
		fmt.Printf("예상된 에러: %v\n", err)
	}
}
```

### 실전 패턴: 핸들러 체이닝

게임 서버에서 패킷을 처리할 때, 여러 단계의 검증과 처리가 필요하다. 인터페이스 임베딩과 구조체 임베딩을 활용하면 미들웨어 패턴을 구현할 수 있다.

```go
package main

import (
	"fmt"
	"time"
)

// 컨텍스트 - 요청 처리에 필요한 정보
type Context struct {
	PlayerID  string
	SessionID string
	Timestamp time.Time
	Data      map[string]interface{}
}

// 핸들러 인터페이스
type Handler interface {
	Handle(ctx *Context) error
}

// 핸들러 함수 타입
type HandlerFunc func(ctx *Context) error

// HandlerFunc가 Handler 인터페이스를 구현
func (f HandlerFunc) Handle(ctx *Context) error {
	return f(ctx)
}

// 체인 핸들러 - 여러 핸들러를 순차적으로 실행
type ChainHandler struct {
	handlers []Handler
}

func NewChainHandler(handlers ...Handler) *ChainHandler {
	return &ChainHandler{
		handlers: handlers,
	}
}

func (c *ChainHandler) Handle(ctx *Context) error {
	for _, handler := range c.handlers {
		if err := handler.Handle(ctx); err != nil {
			return err
		}
	}
	return nil
}

// 인증 핸들러
func AuthenticationHandler(ctx *Context) error {
	fmt.Printf("[인증] 플레이어 %s 검증 중...\n", ctx.PlayerID)
	
	if ctx.PlayerID == "" {
		return fmt.Errorf("플레이어 ID가 없다")
	}
	
	// 실제로는 데이터베이스 조회 등
	fmt.Println("[인증] 통과")
	return nil
}

// 로깅 핸들러
func LoggingHandler(ctx *Context) error {
	fmt.Printf("[로깅] %s - Player: %s, Session: %s\n",
		ctx.Timestamp.Format("15:04:05"),
		ctx.PlayerID,
		ctx.SessionID)
	return nil
}

// 속도 제한 핸들러
func RateLimitHandler(ctx *Context) error {
	fmt.Println("[속도제한] 요청 빈도 확인 중...")
	// 실제로는 Redis 등을 사용한 속도 제한 로직
	fmt.Println("[속도제한] 통과")
	return nil
}

// 비즈니스 로직 핸들러
func BetHandler(ctx *Context) error {
	fmt.Println("[베팅] 베팅 처리 중...")
	
	amount, ok := ctx.Data["amount"].(int64)
	if !ok {
		return fmt.Errorf("베팅 금액이 올바르지 않다")
	}
	
	fmt.Printf("[베팅] %d 칩 베팅 완료\n", amount)
	return nil
}

func main() {
	// 핸들러 체인 구성
	betChain := NewChainHandler(
		HandlerFunc(LoggingHandler),
		HandlerFunc(AuthenticationHandler),
		HandlerFunc(RateLimitHandler),
		HandlerFunc(BetHandler),
	)

	// 요청 컨텍스트 생성
	ctx := &Context{
		PlayerID:  "player001",
		SessionID: "session123",
		Timestamp: time.Now(),
		Data: map[string]interface{}{
			"amount": int64(500),
		},
	}

	// 체인 실행
	fmt.Println("=== 베팅 요청 처리 ===")
	err := betChain.Handle(ctx)
	if err != nil {
		fmt.Printf("처리 실패: %v\n", err)
	} else {
		fmt.Println("\n처리 성공!")
	}

	// 잘못된 요청
	fmt.Println("\n=== 잘못된 요청 처리 ===")
	invalidCtx := &Context{
		PlayerID:  "", // 빈 ID
		SessionID: "session456",
		Timestamp: time.Now(),
		Data: map[string]interface{}{
			"amount": int64(1000),
		},
	}

	err = betChain.Handle(invalidCtx)
	if err != nil {
		fmt.Printf("예상된 실패: %v\n", err)
	}
}
```

이 예제는 실제 게임 서버에서 사용하는 미들웨어 패턴을 보여준다. 각 핸들러는 독립적으로 동작하며, 체인을 통해 순차적으로 실행된다. 인증, 로깅, 속도 제한 등의 공통 로직을 재사용할 수 있고, 새로운 핸들러를 쉽게 추가할 수 있다.

---

## 정리

이 장에서는 구조체와 인터페이스의 핵심 개념을 다루었다. 구조체는 데이터를 묶어 표현하고, 메서드를 통해 동작을 정의한다. 인터페이스는 타입의 행동을 정의하며, 다형성을 구현하는 핵심 도구다. 임베딩은 코드 재사용의 강력한 수단이다.

게임 서버 개발에서 이러한 개념들은 다음과 같이 활용된다:

- **구조체**: 플레이어, 방, 패킷 등 게임 엔티티를 표현한다
- **메서드**: 엔티티의 동작(베팅, 이동, 공격 등)을 구현한다
- **인터페이스**: 다양한 타입이 동일한 방식으로 처리되도록 한다 (예: 다양한 패킷 타입)
- **다형성**: AI 전략, 게임 모드 등을 유연하게 교체할 수 있게 한다
- **임베딩**: 공통 기능(ID 관리, 타임스탬프 등)을 재사용한다

다음 Chapter 4에서는 Go의 에러 처리 방식을 학습하여, 안정적인 게임 서버를 만드는 방법을 알아본다.

   
# Chapter 4. 에러 처리

## 4.1 Go의 에러 처리 철학

Go 언어의 에러 처리 철학은 다른 언어와 매우 다르다. 많은 언어에서는 예외(Exception)를 발생시키고 try-catch 블록으로 처리하는 방식을 사용한다. 하지만 Go는 명시적인 에러 반환을 강조한다.

Go의 에러 처리 원칙은 다음과 같다. 첫째, 에러는 일반적인 제어 흐름의 일부로 취급한다. 둘째, 함수가 실패할 수 있다면 반드시 에러를 반환값으로 전달한다. 셋째, 호출자는 반환된 에러를 명시적으로 확인하고 처리해야 한다.

이러한 철학의 장점은 명확하다. 프로그래머가 어느 부분에서 에러가 발생할 수 있는지 코드를 읽으면서 바로 알 수 있다. 또한 에러 처리 경로를 명시적으로 작성하므로 예상치 못한 에러 전파를 방지할 수 있다.

```go
package main

import (
	"fmt"
)

// 에러가 발생할 수 있는 함수의 기본 패턴
func divide(a, b float64) (float64, error) {
	if b == 0 {
		return 0, fmt.Errorf("division by zero")
	}
	return a / b, nil
}

func main() {
	// 에러를 명시적으로 확인한다
	result, err := divide(10, 2)
	if err != nil {
		fmt.Println("에러 발생:", err)
		return
	}
	fmt.Printf("결과: %v\n", result)

	// 에러가 발생하는 경우
	result, err = divide(10, 0)
	if err != nil {
		fmt.Println("에러 발생:", err)
		return
	}
	fmt.Printf("결과: %v\n", result)
}
```

위 예제에서 `divide` 함수는 두 개의 반환값을 가진다. 첫 번째는 계산 결과이고, 두 번째는 에러다. Go의 관례에 따르면 에러는 항상 마지막 반환값이다. 호출자는 `if err != nil`로 에러를 확인해야 한다.

이 패턴은 게임 서버 개발에서도 중요하다. 네트워크 통신, 파일 I/O, 데이터베이스 접근 등 많은 작업이 실패할 수 있기 때문이다. 명시적인 에러 처리는 서버의 안정성을 크게 높인다.

## 4.2 error 인터페이스

Go에서 에러는 `error` 인터페이스로 표현된다. 이는 매우 간단한 인터페이스다.

```go
type error interface {
	Error() string
}
```

이 인터페이스는 오직 하나의 메서드만 가진다. `Error()` 메서드는 에러 메시지를 문자열로 반환한다. 이렇게 단순한 인터페이스 덕분에 누구나 자신만의 에러 타입을 만들 수 있다.

Go 표준 라이브러리에서 가장 간단한 에러 생성 방법은 `errors.New` 함수를 사용하는 것이다.

```go
package main

import (
	"errors"
	"fmt"
)

func validateUsername(username string) error {
	if username == "" {
		return errors.New("username cannot be empty")
	}
	if len(username) < 3 {
		return errors.New("username must be at least 3 characters long")
	}
	return nil
}

func main() {
	err := validateUsername("")
	if err != nil {
		fmt.Println("검증 실패:", err)
	}

	err = validateUsername("ab")
	if err != nil {
		fmt.Println("검증 실패:", err)
	}

	err = validateUsername("player123")
	if err != nil {
		fmt.Println("검증 성공")
	} else {
		fmt.Println("검증 성공")
	}
}
```

더 나은 방법은 `fmt.Errorf` 함수를 사용하는 것이다. 이 함수는 포맷 문자열을 지원하므로 동적인 에러 메시지를 생성할 수 있다.

```go
package main

import (
	"fmt"
)

func connectToServer(host string, port int) error {
	if port < 1 || port > 65535 {
		return fmt.Errorf("invalid port number: %d (must be 1-65535)", port)
	}
	// 실제 연결 로직...
	return nil
}

func main() {
	err := connectToServer("localhost", 99999)
	if err != nil {
		fmt.Println(err)
		// 출력: invalid port number: 99999 (must be 1-65535)
	}
}
```

게임 서버에서는 여러 단계의 작업을 순차적으로 수행하는 경우가 많다. 예를 들어 클라이언트 연결을 받고, 인증을 확인하고, 게임 상태를 초기화하는 식이다. 각 단계에서 에러가 발생할 수 있으므로 모두 확인해야 한다.

```go
package main

import (
	"fmt"
)

func authenticate(token string) error {
	if token == "" {
		return fmt.Errorf("empty token")
	}
	return nil
}

func loadPlayerData(playerID int) error {
	if playerID <= 0 {
		return fmt.Errorf("invalid player ID: %d", playerID)
	}
	return nil
}

func joinGame(token string, playerID int) error {
	// 단계 1: 인증
	if err := authenticate(token); err != nil {
		return err
	}

	// 단계 2: 플레이어 데이터 로드
	if err := loadPlayerData(playerID); err != nil {
		return err
	}

	// 단계 3: 게임 참여 (성공)
	return nil
}

func main() {
	err := joinGame("", 1)
	if err != nil {
		fmt.Println("게임 참여 실패:", err)
	}

	err = joinGame("token123", 0)
	if err != nil {
		fmt.Println("게임 참여 실패:", err)
	}

	err = joinGame("token123", 1)
	if err != nil {
		fmt.Println("게임 참여 실패:", err)
	} else {
		fmt.Println("게임 참여 성공")
	}
}
```

## 4.3 커스텀 에러 타입 만들기

기본 에러만으로는 부족한 경우가 많다. 게임 서버 개발에서는 에러의 종류를 구분하고, 추가 정보를 담고 싶을 때가 있다. 이럴 때 커스텀 에러 타입을 만들 수 있다.

```go
package main

import (
	"fmt"
)

// 게임 서버에서 발생할 수 있는 에러 종류를 정의한다
type GameError struct {
	Code    int    // 에러 코드
	Message string // 에러 메시지
	Details string // 상세 정보
}

// error 인터페이스를 구현한다
func (e *GameError) Error() string {
	return fmt.Sprintf("[%d] %s: %s", e.Code, e.Message, e.Details)
}

// 에러 코드 상수 정의
const (
	ErrCodeInvalidPlayer = 1001
	ErrCodeRoomFull      = 1002
	ErrCodeGameStarted   = 1003
)

func joinRoom(roomID int, maxPlayers int, currentPlayers int) error {
	if roomID <= 0 {
		return &GameError{
			Code:    ErrCodeInvalidPlayer,
			Message: "Invalid room ID",
			Details: fmt.Sprintf("room ID must be positive, got %d", roomID),
		}
	}

	if currentPlayers >= maxPlayers {
		return &GameError{
			Code:    ErrCodeRoomFull,
			Message: "Room is full",
			Details: fmt.Sprintf("max %d players, current %d", maxPlayers, currentPlayers),
		}
	}

	return nil
}

func main() {
	err := joinRoom(-1, 4, 2)
	if err != nil {
		fmt.Println(err)
		// 출력: [1001] Invalid room ID: room ID must be positive, got -1
	}

	err = joinRoom(1, 4, 4)
	if err != nil {
		fmt.Println(err)
		// 출력: [1002] Room is full: max 4 players, current 4
	}
}
```

더 나아가, 에러 타입을 확인하여 다르게 처리할 수 있다. 이를 통해 같은 종류의 에러는 같은 방식으로 처리할 수 있다.

```go
package main

import (
	"fmt"
)

type GameError struct {
	Code    int
	Message string
	Details string
}

func (e *GameError) Error() string {
	return fmt.Sprintf("[%d] %s: %s", e.Code, e.Message, e.Details)
}

const (
	ErrCodeRoomFull = 1002
)

func tryJoinRoom() error {
	// 방이 가득 찬 경우를 시뮬레이션한다
	return &GameError{
		Code:    ErrCodeRoomFull,
		Message: "Room is full",
		Details: "max 4 players, current 4",
	}
}

func main() {
	err := tryJoinRoom()
	if err != nil {
		// 에러 타입을 확인한다
		if gameErr, ok := err.(*GameError); ok {
			switch gameErr.Code {
			case ErrCodeRoomFull:
				fmt.Println("대기실로 이동합니다")
			default:
				fmt.Println("다른 에러가 발생했습니다")
			}
		}
	}
}
```

게임 서버에서는 클라이언트에게 에러 정보를 전송해야 하는 경우가 많다. 에러 코드를 네트워크 프로토콜에 포함시키면 클라이언트가 어떤 상황이 발생했는지 알 수 있다.

```go
package main

import (
	"fmt"
)

// 네트워크를 통해 전송할 에러 응답
type ErrorResponse struct {
	Code    int    `json:"code"`
	Message string `json:"message"`
}

type GameError struct {
	Code    int
	Message string
	Details string
}

func (e *GameError) Error() string {
	return fmt.Sprintf("[%d] %s: %s", e.Code, e.Message, e.Details)
}

func (e *GameError) ToResponse() ErrorResponse {
	return ErrorResponse{
		Code:    e.Code,
		Message: e.Message,
	}
}

const (
	ErrCodeAuthFailed = 2001
	ErrCodeTimeout    = 2002
)

func authenticatePlayer(token string) error {
	if token == "" {
		return &GameError{
			Code:    ErrCodeAuthFailed,
			Message: "Authentication failed",
			Details: "token is empty",
		}
	}
	return nil
}

func main() {
	err := authenticatePlayer("")
	if err != nil {
		if gameErr, ok := err.(*GameError); ok {
			response := gameErr.ToResponse()
			fmt.Printf("클라이언트에 전송할 응답: Code=%d, Message=%s\n",
				response.Code, response.Message)
		}
	}
}
```

## 4.4 패닉과 리커버

Go에는 두 가지 에러 처리 메커니즘이 있다. 하나는 지금까지 설명한 `error` 반환값이고, 다른 하나는 패닉(panic)과 리커버(recover)다.

패닉은 프로그램의 실행을 중단시킨다. 예를 들어 배열의 범위를 벗어난 인덱스에 접근하거나, nil 포인터를 역참조하면 패닉이 발생한다. 정상적인 에러 처리로 대응할 수 없는 심각한 상황에서만 패닉이 발생한다.

```go
package main

import (
	"fmt"
)

func main() {
	// 배열 범위 초과 접근으로 패닉 발생
	arr := []int{1, 2, 3}
	fmt.Println(arr[10]) // panic: runtime error: index out of range [10] with length 3
}
```

리커버는 패닉으로부터 복구하는 메커니즘이다. `recover()` 함수는 `defer`와 함께 사용되며, 패닉 값을 반환한다.

```go
package main

import (
	"fmt"
)

func safeDivide(a, b float64) (result float64) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Println("복구됨. 패닉 값:", r)
			result = 0
		}
	}()

	if b == 0 {
		panic("division by zero")
	}
	return a / b
}

func main() {
	result := safeDivide(10, 2)
	fmt.Println("결과:", result)

	result = safeDivide(10, 0)
	fmt.Println("결과:", result)
}
```

게임 서버에서는 패닉과 리커버를 신중하게 사용해야 한다. 특정 고루틴에서 패닉이 발생하면 그 고루틴만 중단되고 다른 고루틴은 계속 실행된다. 따라서 각 클라이언트를 처리하는 고루틴에서 패닉이 발생해도 서버 전체가 중단되지 않는다.

```go
package main

import (
	"fmt"
)

// 클라이언트 연결을 처리하는 함수
func handleClient(clientID int) {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("클라이언트 %d 처리 중 패닉 발생: %v\n", clientID, r)
		}
	}()

	fmt.Printf("클라이언트 %d 연결됨\n", clientID)

	// 클라이언트 처리 로직
	if clientID == 2 {
		panic("unexpected error in client processing")
	}

	fmt.Printf("클라이언트 %d 완료\n", clientID)
}

func main() {
	for i := 1; i <= 3; i++ {
		handleClient(i)
	}
	fmt.Println("서버 계속 실행 중")
}
```

출력 결과를 보면 클라이언트 2에서 패닉이 발생했지만, 클라이언트 1과 3은 정상적으로 처리되고 서버는 계속 실행된다.

하지만 실무에서는 패닉을 피하는 것이 좋다. 대신 에러를 반환값으로 처리하는 것이 명시적이고 디버깅하기도 쉽다. 패닉은 정말 예상하지 못한 심각한 상황에만 사용해야 한다.

```go
package main

import (
	"fmt"
)

// 에러를 반환하는 방식을 권장한다
func processPlayerAction(actionType string) error {
	switch actionType {
	case "fold":
		return nil
	case "raise":
		return nil
	case "invalid":
		return fmt.Errorf("invalid action type: %s", actionType)
	default:
		return fmt.Errorf("unknown action type: %s", actionType)
	}
}

func main() {
	err := processPlayerAction("invalid")
	if err != nil {
		fmt.Println("액션 처리 실패:", err)
	}
}
```

에러 처리에 대한 정리를 하면, Go에서는 명시적인 에러 반환이 기본이고, 커스텀 에러 타입을 만들어 에러 정보를 풍부하게 전달할 수 있다. 패닉과 리커버는 예외적인 상황에만 사용한다. 게임 서버는 많은 동시 연결을 처리해야 하므로, 한 클라이언트의 에러가 다른 클라이언트에게 영향을 주지 않도록 각 고루틴에서 에러를 적절히 처리해야 한다.

---

## Chapter 4 정리 다이어그램

Go의 에러 처리 흐름을 시각화하면 다음과 같다.

```
┌─────────────────────────────────────────────────────────────┐
│                    함수 호출                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
    ┌────▼─────┐           ┌────▼─────┐
    │   성공   │           │  실패     │
    │(err=nil) │           │(err!=nil) │
    └────┬─────┘           └────┬─────┘
         │                      │
    ┌────▼──────────┐    ┌──────▼──────────┐
    │결과 값 사용   │    │에러 처리        │
    │              │    │                  │
    │              │    ├─► 로그 기록      │
    │              │    ├─► 사용자에게 알림│
    │              │    ├─► 복구 시도      │
    │              │    └─► 프로그램 종료  │
    └────┬─────────┘    └──────┬──────────┘
         │                      │
         └──────────┬───────────┘
                    │
            ┌───────▼────────┐
            │ 다음 작업 진행  │
            └────────────────┘
```

또한 Go의 에러 처리 철학을 요약하면 다음과 같다.

```
에러 처리의 명시성 (Explicit Error Handling)
        │
        ├─► 에러는 반환값으로 전달
        │
        ├─► 호출자가 반드시 확인
        │
        ├─► 예외 메커니즘 사용 안 함
        │
        └─► 코드 흐름이 명확함
```

---

이제 에러 처리의 기초를 이해했다. 다음 장에서는 Go의 가장 강력한 기능 중 하나인 고루틴(Goroutine)에 대해 배운다. 고루틴은 경량 스레드처럼 동작하며, 게임 서버가 많은 동시 연결을 처리할 수 있게 해준다.  