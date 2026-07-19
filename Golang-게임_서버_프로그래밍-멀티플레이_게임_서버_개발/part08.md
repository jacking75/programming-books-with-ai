# Go 게임 서버 프로그래밍 - 소켓 기반 멀티플레이 게임 서버 개발  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio Code
- **버전**: 1.25
- **OS**: Windows 10 이상

-----    
  
# Chapter 28. 빌드와 배포

게임 서버를 개발한 후 실제 환경에 배포하기 위해서는 체계적인 빌드 과정과 배포 전략이 필요하다. 이 장에서는 Go 프로젝트를 다양한 플랫폼으로 빌드하고, 최적화하며, 자동화하는 방법을 다룬다. 특히 Windows에서 개발하면서 Linux 서버에 배포하는 실전 시나리오를 중심으로 설명한다.

## 28.1 크로스 컴파일

크로스 컴파일은 현재 실행 중인 운영 체제와 다른 운영 체제와 아키텍처를 위해 실행 파일을 컴파일하는 것이다. Go 언어는 크로스 컴파일을 매우 쉽게 지원하므로, Windows에서 Linux용 바이너리를 컴파일할 수 있다.

### 기본 크로스 컴파일

Go에서 크로스 컴파일은 환경 변수를 설정하여 수행한다.

```bash
# Windows에서 Linux 64비트용으로 컴파일
set GOOS=linux
set GOARCH=amd64
go build -o gameserver_linux main.go

# macOS용으로 컴파일
set GOOS=darwin
set GOARCH=amd64
go build -o gameserver_mac main.go

# Windows 32비트용으로 컴파일
set GOOS=windows
set GOARCH=386
go build -o gameserver_32bit.exe main.go

# ARM 아키텍처용으로 컴파일 (라즈베리파이 등)
set GOOS=linux
set GOARCH=arm
set GOARM=7
go build -o gameserver_arm main.go
```

PowerShell을 사용하는 경우 문법이 다르다.

```powershell
# PowerShell에서 크로스 컴파일
$env:GOOS = "linux"
$env:GOARCH = "amd64"
go build -o gameserver_linux main.go

# 원래대로 돌리기 (Windows)
$env:GOOS = "windows"
$env:GOARCH = "amd64"
go build -o gameserver.exe main.go
```

### 크로스 컴파일 빌드 헬퍼

매번 환경 변수를 설정하는 것은 번거로우므로 빌드 스크립트를 만드는 것이 효율적이다.

```batch
@echo off
REM build.bat - Windows에서 다양한 플랫폼용 빌드 스크립트

setlocal enabledelayedexpansion

set VERSION=%1
if "%VERSION%"=="" set VERSION=1.0.0

set OUTPUT_DIR=build\v%VERSION%
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

echo Building Game Server v%VERSION%...

REM Linux 64비트
echo Building for Linux (amd64)...
set GOOS=linux
set GOARCH=amd64
go build -o "%OUTPUT_DIR%\gameserver_linux_amd64" -ldflags "-X main.Version=%VERSION%" main.go

REM Linux ARM (라즈베리파이)
echo Building for Linux (arm)...
set GOOS=linux
set GOARCH=arm
set GOARM=7
go build -o "%OUTPUT_DIR%\gameserver_linux_arm" -ldflags "-X main.Version=%VERSION%" main.go

REM Windows 64비트
echo Building for Windows (amd64)...
set GOOS=windows
set GOARCH=amd64
go build -o "%OUTPUT_DIR%\gameserver_windows_amd64.exe" -ldflags "-X main.Version=%VERSION%" main.go

REM macOS
echo Building for macOS (amd64)...
set GOOS=darwin
set GOARCH=amd64
go build -o "%OUTPUT_DIR%\gameserver_macos_amd64" -ldflags "-X main.Version=%VERSION%" main.go

REM macOS ARM64 (Apple Silicon)
echo Building for macOS (arm64)...
set GOOS=darwin
set GOARCH=arm64
go build -o "%OUTPUT_DIR%\gameserver_macos_arm64" -ldflags "-X main.Version=%VERSION%" main.go

REM 원래 환경으로 복구
set GOOS=windows
set GOARCH=amd64

echo Build completed. Binaries are in %OUTPUT_DIR%
pause
```

PowerShell 버전의 빌드 스크립트도 만들어보자.

```powershell
# build.ps1 - PowerShell 빌드 스크립트

param(
    [string]$Version = "1.0.0"
)

$OutputDir = "build\v$Version"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

Write-Host "Building Game Server v$Version..."

$platforms = @(
    @{ OS = "linux"; Arch = "amd64"; Name = "gameserver_linux_amd64" },
    @{ OS = "linux"; Arch = "arm"; Name = "gameserver_linux_arm"; ARM = "7" },
    @{ OS = "windows"; Arch = "amd64"; Name = "gameserver_windows_amd64.exe" },
    @{ OS = "darwin"; Arch = "amd64"; Name = "gameserver_macos_amd64" },
    @{ OS = "darwin"; Arch = "arm64"; Name = "gameserver_macos_arm64" }
)

foreach ($platform in $platforms) {
    Write-Host "Building for $($platform.OS) ($($platform.Arch))..."
    
    $env:GOOS = $platform.OS
    $env:GOARCH = $platform.Arch
    
    if ($platform.ARM) {
        $env:GOARM = $platform.ARM
    }
    
    $outputPath = Join-Path $OutputDir $platform.Name
    go build -o $outputPath -ldflags "-X main.Version=$Version" main.go
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $($platform.OS) ($($platform.Arch))"
        exit 1
    }
}

# 원래 환경으로 복구
$env:GOOS = "windows"
$env:GOARCH = "amd64"

Write-Host "Build completed. Binaries are in $OutputDir"
```

### 조건부 빌드

Go 코드에서 운영 체제별로 다른 코드를 실행하고 싶다면 빌드 태그를 사용한다.

```go
// platform_linux.go
//go:build linux
// +build linux

package main

import (
	"fmt"
	"syscall"
)

// LinuxSpecificInit은 Linux 특화 초기화를 수행한다.
func LinuxSpecificInit() {
	fmt.Println("Initializing Linux-specific features...")
	
	// Linux에서만 사용 가능한 코드
	// 예: 파일 디스크립터 제한 설정
	var limit syscall.Rlimit
	limit.Max = 100000
	limit.Cur = 100000
	syscall.Setrlimit(syscall.RLIMIT_NOFILE, &limit)
}

// GetSystemInfo는 Linux 시스템 정보를 반환한다.
func GetSystemInfo() string {
	return "Running on Linux"
}
```

```go
// platform_windows.go
//go:build windows
// +build windows

package main

import (
	"fmt"
)

// WindowsSpecificInit은 Windows 특화 초기화를 수행한다.
func WindowsSpecificInit() {
	fmt.Println("Initializing Windows-specific features...")
	
	// Windows에서만 사용 가능한 코드
}

// GetSystemInfo는 Windows 시스템 정보를 반환한다.
func GetSystemInfo() string {
	return "Running on Windows"
}
```

```go
// platform_darwin.go
//go:build darwin
// +build darwin

package main

import (
	"fmt"
)

// DarwinSpecificInit은 macOS 특화 초기화를 수행한다.
func DarwinSpecificInit() {
	fmt.Println("Initializing macOS-specific features...")
}

// GetSystemInfo는 macOS 시스템 정보를 반환한다.
func GetSystemInfo() string {
	return "Running on macOS"
}
```

```go
// main.go

package main

import (
	"fmt"
)

func main() {
	fmt.Println("Game Server Starting...")
	
	// 운영 체제별 초기화
	LinuxSpecificInit()
	// 또는 WindowsSpecificInit()
	// 또는 DarwinSpecificInit()
	
	info := GetSystemInfo()
	fmt.Println(info)
	
	fmt.Println("Game Server Started")
}
```

---

## 28.2 빌드 최적화

실행 파일의 크기를 줄이고 성능을 향상시키기 위해 빌드 최적화 기법을 사용한다.

### 바이너리 크기 최적화

Go 빌드 시 여러 플래그를 사용하여 바이너리 크기를 줄일 수 있다.

```bash
# 기본 빌드 (크기가 큼, 디버깅 정보 포함)
go build -o gameserver main.go

# 최적화된 빌드 (심볼 및 디버깅 정보 제거)
go build -ldflags="-s -w" -o gameserver main.go

# UPX로 추가 압축 (별도 설치 필요)
upx --best gameserver

# 리스트 옵션 설명:
# -s: 심볼 테이블 제거 (디버깅 불가능, 크기 감소)
# -w: DWARF 디버깅 정보 제거 (크기 감소)
```

PowerShell에서의 빌드 명령어다.

```powershell
# PowerShell에서 최적화된 빌드
$ldflags = "-s -w"
go build -ldflags $ldflags -o gameserver.exe main.go
```

### 버전 정보 포함

빌드 시 버전, 빌드 시간, 커밋 해시 등을 실행 파일에 포함시킬 수 있다.

```go
// main.go

package main

import (
	"flag"
	"fmt"
	"runtime"
)

var (
	Version   string = "dev"
	BuildTime string = "unknown"
	GitCommit string = "unknown"
)

func printVersion() {
	fmt.Printf("Game Server v%s\n", Version)
	fmt.Printf("Build Time: %s\n", BuildTime)
	fmt.Printf("Git Commit: %s\n", GitCommit)
	fmt.Printf("Go Version: %s\n", runtime.Version())
	fmt.Printf("OS/Arch: %s/%s\n", runtime.GOOS, runtime.GOARCH)
}

func main() {
	versionFlag := flag.Bool("version", false, "Print version information")
	flag.Parse()

	if *versionFlag {
		printVersion()
		return
	}

	fmt.Println("Game Server Starting...")
	fmt.Printf("Version: %s\n", Version)
	fmt.Printf("Build Time: %s\n", BuildTime)
	
	// 서버 실행
}
```

빌드할 때 버전 정보를 포함시킨다.

```batch
@echo off
REM build.bat - 버전 정보를 포함한 빌드

for /f "tokens=1-4 delims=/ " %%a in ('date /t') do (set mydate=%%c-%%a-%%b)
for /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set mytime=%%a:%%b)

set BUILD_TIME=%mydate% %mytime%
set GIT_COMMIT=unknown

REM git이 설치되어 있으면 커밋 해시 가져오기
for /f %%i in ('git rev-parse --short HEAD 2^>nul') do set GIT_COMMIT=%%i

set LDFLAGS=-X main.Version=1.0.0 -X "main.BuildTime=%BUILD_TIME%" -X main.GitCommit=%GIT_COMMIT% -s -w

echo Building with version info...
echo Version: 1.0.0
echo Build Time: %BUILD_TIME%
echo Git Commit: %GIT_COMMIT%

go build -ldflags "%LDFLAGS%" -o gameserver.exe main.go

echo Build completed: gameserver.exe
```

PowerShell 버전이다.

```powershell
# build_with_version.ps1

param(
    [string]$Version = "1.0.0"
)

# 빌드 시간 가져오기
$BuildTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

# Git 커밋 해시 가져오기
$GitCommit = "unknown"
try {
    $GitCommit = git rev-parse --short HEAD
} catch {
    Write-Host "Git not found, using default commit hash"
}

# LDFLAGS 구성
$ldflags = @(
    "-X main.Version=$Version",
    "-X ""main.BuildTime=$BuildTime""",
    "-X main.GitCommit=$GitCommit",
    "-s",
    "-w"
) -join " "

Write-Host "Building Game Server..."
Write-Host "Version: $Version"
Write-Host "Build Time: $BuildTime"
Write-Host "Git Commit: $GitCommit"

go build -ldflags $ldflags -o gameserver.exe main.go

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful: gameserver.exe"
} else {
    Write-Host "Build failed!"
    exit 1
}
```

### 프로파일 가이드 최적화 (PGO)

Go 1.20 이상에서는 프로파일 가이드 최적화를 지원한다. 실제 실행 패턴을 기반으로 최적화한다.

```go
// main.go

package main

import (
	"fmt"
	"time"
)

// ProcessGameTick은 게임 틱을 처리한다.
func ProcessGameTick(tickNumber int) {
	// 자주 실행되는 핫 경로
	for i := 0; i < 1000; i++ {
		_ = fibonacci(15)
	}
}

// fibonacci는 피보나치 수를 계산한다.
func fibonacci(n int) int {
	if n <= 1 {
		return n
	}
	return fibonacci(n-1) + fibonacci(n-2)
}

func main() {
	fmt.Println("Game Server Starting...")

	// 게임 틱 시뮬레이션
	for tick := 0; tick < 100; tick++ {
		ProcessGameTick(tick)
		time.Sleep(16 * time.Millisecond)
	}

	fmt.Println("Game Server Stopped")
}
```

프로파일을 수집하고 최적화하는 과정이다.

```bash
# 1. CPU 프로파일 수집
go run -cpuprofile=cpu.prof main.go

# 2. 프로파일을 활용하여 빌드 (Go 1.20+)
go build -o gameserver.exe main.go

# 프로파일 보기
go tool pprof cpu.prof
```

---

## 28.3 환경 설정 관리

게임 서버는 개발, 스테이징, 프로덕션 환경에 따라 다른 설정이 필요하다.

### 설정 파일 구조

```go
// config.go

package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"os"
)

// ServerConfig는 서버 설정을 나타낸다.
type ServerConfig struct {
	Server   ServerSettings   `json:"server"`
	Database DatabaseSettings `json:"database"`
	Logging  LoggingSettings  `json:"logging"`
	Game     GameSettings     `json:"game"`
}

// ServerSettings는 서버 관련 설정이다.
type ServerSettings struct {
	Host           string `json:"host"`
	Port           int    `json:"port"`
	MaxConnections int    `json:"max_connections"`
	TickRate       int    `json:"tick_rate"`
	GracefulShutdown int  `json:"graceful_shutdown_seconds"`
}

// DatabaseSettings는 데이터베이스 설정이다.
type DatabaseSettings struct {
	Host     string `json:"host"`
	Port     int    `json:"port"`
	User     string `json:"user"`
	Password string `json:"password"`
	Database string `json:"database"`
	Pool     int    `json:"pool_size"`
}

// LoggingSettings는 로깅 설정이다.
type LoggingSettings struct {
	Level      string `json:"level"`
	OutputPath string `json:"output_path"`
	MaxSize    int    `json:"max_size_mb"`
	MaxBackups int    `json:"max_backups"`
	MaxAge     int    `json:"max_age_days"`
}

// GameSettings는 게임 설정이다.
type GameSettings struct {
	MaxPlayersPerRoom int `json:"max_players_per_room"`
	RoundDuration     int `json:"round_duration_seconds"`
	MinBet            int `json:"min_bet"`
	MaxBet            int `json:"max_bet"`
}

// LoadConfig는 설정 파일을 로드한다.
func LoadConfig(filename string) (*ServerConfig, error) {
	// 환경 변수에서 설정 파일 경로 가져오기
	if env := os.Getenv("CONFIG_FILE"); env != "" {
		filename = env
	}

	data, err := ioutil.ReadFile(filename)
	if err != nil {
		return nil, fmt.Errorf("failed to read config file: %w", err)
	}

	var config ServerConfig
	if err := json.Unmarshal(data, &config); err != nil {
		return nil, fmt.Errorf("failed to parse config: %w", err)
	}

	// 환경 변수로 설정값 오버라이드
	applyEnvironmentOverrides(&config)

	return &config, nil
}

// applyEnvironmentOverrides는 환경 변수로 설정을 오버라이드한다.
func applyEnvironmentOverrides(config *ServerConfig) {
	if host := os.Getenv("SERVER_HOST"); host != "" {
		config.Server.Host = host
	}

	if port := os.Getenv("SERVER_PORT"); port != "" {
		fmt.Sscanf(port, "%d", &config.Server.Port)
	}

	if level := os.Getenv("LOG_LEVEL"); level != "" {
		config.Logging.Level = level
	}

	if dbHost := os.Getenv("DB_HOST"); dbHost != "" {
		config.Database.Host = dbHost
	}
}

// Print는 설정을 출력한다 (비밀번호 제외).
func (sc *ServerConfig) Print() {
	fmt.Println("=== Server Configuration ===")
	fmt.Printf("Server: %s:%d\n", sc.Server.Host, sc.Server.Port)
	fmt.Printf("Max Connections: %d\n", sc.Server.MaxConnections)
	fmt.Printf("Tick Rate: %d\n", sc.Server.TickRate)
	fmt.Printf("Database: %s:%d\n", sc.Database.Host, sc.Database.Port)
	fmt.Printf("Log Level: %s\n", sc.Logging.Level)
	fmt.Printf("Max Players per Room: %d\n", sc.Game.MaxPlayersPerRoom)
	fmt.Println("============================")
}
```

### 환경별 설정 파일

```json
// config.dev.json - 개발 환경
{
  "server": {
    "host": "127.0.0.1",
    "port": 8080,
    "max_connections": 100,
    "tick_rate": 60,
    "graceful_shutdown_seconds": 5
  },
  "database": {
    "host": "localhost",
    "port": 5432,
    "user": "dev",
    "password": "devpassword",
    "database": "gameserver_dev",
    "pool_size": 10
  },
  "logging": {
    "level": "debug",
    "output_path": "./logs",
    "max_size_mb": 10,
    "max_backups": 5,
    "max_age_days": 7
  },
  "game": {
    "max_players_per_room": 6,
    "round_duration_seconds": 300,
    "min_bet": 1,
    "max_bet": 10000
  }
}
```

```json
// config.prod.json - 프로덕션 환경
{
  "server": {
    "host": "0.0.0.0",
    "port": 9090,
    "max_connections": 10000,
    "tick_rate": 60,
    "graceful_shutdown_seconds": 30
  },
  "database": {
    "host": "db.production.example.com",
    "port": 5432,
    "user": "prod_user",
    "password": "${DB_PASSWORD}",
    "database": "gameserver_prod",
    "pool_size": 100
  },
  "logging": {
    "level": "info",
    "output_path": "/var/log/gameserver",
    "max_size_mb": 100,
    "max_backups": 10,
    "max_age_days": 30
  },
  "game": {
    "max_players_per_room": 6,
    "round_duration_seconds": 300,
    "min_bet": 100,
    "max_bet": 1000000
  }
}
```

### main.go에서 설정 로드

```go
// main.go

package main

import (
	"fmt"
	"os"
)

func main() {
	// 환경 결정 (기본값: dev)
	env := os.Getenv("ENVIRONMENT")
	if env == "" {
		env = "dev"
	}

	// 설정 파일 경로 구성
	configFile := fmt.Sprintf("config.%s.json", env)

	// 설정 로드
	config, err := LoadConfig(configFile)
	if err != nil {
		fmt.Printf("Failed to load configuration: %v\n", err)
		os.Exit(1)
	}

	// 설정 출력
	config.Print()

	// 서버 시작
	startGameServer(config)
}

// startGameServer는 서버를 시작한다.
func startGameServer(config *ServerConfig) {
	fmt.Printf("Starting game server on %s:%d\n", config.Server.Host, config.Server.Port)
	// 서버 로직
}
```

배포 시 환경 변수 설정이다.

```bash
# Linux/macOS
export ENVIRONMENT=prod
export SERVER_HOST=0.0.0.0
export SERVER_PORT=9090
export DB_HOST=db.production.example.com
./gameserver

# Windows CMD
set ENVIRONMENT=prod
set SERVER_HOST=0.0.0.0
set SERVER_PORT=9090
gameserver.exe

# Windows PowerShell
$env:ENVIRONMENT = "prod"
$env:SERVER_HOST = "0.0.0.0"
$env:SERVER_PORT = "9090"
.\gameserver.exe
```

---

## 28.4 배포 스크립트 작성

자동화된 배포 스크립트는 실수를 줄이고 일관된 배포를 보장한다.

### Bash 배포 스크립트 (Linux/macOS)

```bash
#!/bin/bash
# deploy.sh - Linux 서버로의 배포 스크립트

set -e  # 에러 발생 시 즉시 종료

# 설정
VERSION=$1
ENVIRONMENT=$2
REMOTE_USER=gameadmin
REMOTE_HOST=game.example.com
REMOTE_PATH=/opt/gameserver
BINARY_NAME=gameserver
LOG_PATH=/var/log/gameserver

# 인자 검증
if [ -z "$VERSION" ] || [ -z "$ENVIRONMENT" ]; then
    echo "Usage: ./deploy.sh <version> <dev|staging|prod>"
    exit 1
fi

if [ "$ENVIRONMENT" != "dev" ] && [ "$ENVIRONMENT" != "staging" ] && [ "$ENVIRONMENT" != "prod" ]; then
    echo "Invalid environment. Choose from: dev, staging, prod"
    exit 1
fi

echo "=========================================="
echo "Deploying Game Server v$VERSION to $ENVIRONMENT"
echo "=========================================="

# 1. 빌드
echo "[1/5] Building..."
GOOS=linux GOARCH=amd64 go build \
    -ldflags="-X main.Version=$VERSION -X main.Environment=$ENVIRONMENT -s -w" \
    -o build/$BINARY_NAME main.go

if [ ! -f "build/$BINARY_NAME" ]; then
    echo "Build failed!"
    exit 1
fi

echo "Build successful"

# 2. 테스트
echo "[2/5] Running tests..."
go test -v ./...

echo "Tests passed"

# 3. 원격 서버에 업로드
echo "[3/5] Uploading to remote server..."
scp build/$BINARY_NAME $REMOTE_USER@$REMOTE_HOST:$REMOTE_PATH/

scp config.$ENVIRONMENT.json $REMOTE_USER@$REMOTE_HOST:$REMOTE_PATH/config.json

echo "Upload completed"

# 4. 원격 서버에서 서비스 재시작
echo "[4/5] Restarting service..."
ssh $REMOTE_USER@$REMOTE_HOST << EOF
    set -e
    
    # 현재 프로세스 종료 (graceful shutdown)
    if systemctl is-active --quiet gameserver; then
        systemctl stop gameserver
        sleep 5
    fi
    
    # 백업 생성
    if [ -f $REMOTE_PATH/$BINARY_NAME ]; then
        cp $REMOTE_PATH/$BINARY_NAME $REMOTE_PATH/$BINARY_NAME.backup.$(date +%s)
    fi
    
    # 권한 설정
    chmod +x $REMOTE_PATH/$BINARY_NAME
    
    # 서비스 시작
    systemctl start gameserver
    
    # 서비스 상태 확인
    sleep 2
    systemctl status gameserver
EOF

echo "Service restarted"

# 5. 헬스 체크
echo "[5/5] Health check..."
sleep 3

if ssh $REMOTE_USER@$REMOTE_HOST "curl -s http://localhost:8080/health | grep -q 'ok'"; then
    echo "Health check passed"
    echo "=========================================="
    echo "Deployment successful!"
    echo "Version: $VERSION"
    echo "Environment: $ENVIRONMENT"
    echo "=========================================="
else
    echo "Health check failed!"
    echo "Rolling back..."
    
    ssh $REMOTE_USER@$REMOTE_HOST << EOF
        if [ -f $REMOTE_PATH/$BINARY_NAME.backup ]; then
            mv $REMOTE_PATH/$BINARY_NAME.backup $REMOTE_PATH/$BINARY_NAME
            systemctl restart gameserver
        fi
EOF
    
    exit 1
fi
```

### PowerShell 배포 스크립트 (Windows)

```powershell
# deploy.ps1 - Windows 개발 환경에서 Linux 서버로 배포하는 스크립트

param(
    [string]$Version = "1.0.0",
    [string]$Environment = "dev",
    [string]$RemoteHost = "game.example.com",
    [string]$RemoteUser = "gameadmin",
    [string]$RemotePath = "/opt/gameserver"
)

# 함수 정의
function Write-Step {
    param([string]$Message)
    Write-Host ">>> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Exit {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
    exit 1
}

# 인자 검증
if ($Environment -notmatch "^(dev|staging|prod)$") {
    Write-Error-Exit "Invalid environment. Choose from: dev, staging, prod"
}

Write-Host "==========================================" -ForegroundColor Yellow
Write-Host "Deploying Game Server v$Version to $Environment" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Yellow

# 1. 빌드
Write-Step "Building..."
$buildTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$gitCommit = git rev-parse --short HEAD 2>$null || "unknown"

$ldflags = @(
    "-X main.Version=$Version",
    "-X main.Environment=$Environment",
    "-X ""main.BuildTime=$buildTime""",
    "-X main.GitCommit=$gitCommit",
    "-s",
    "-w"
) -join " "

$env:GOOS = "linux"
$env:GOARCH = "amd64"

go build -ldflags $ldflags -o "build\gameserver" main.go

if ($LASTEXITCODE -ne 0) {
    Write-Error-Exit "Build failed"
}

Write-Success "Build successful"

# 2. 테스트
Write-Step "Running tests..."
go test -v ./...

if ($LASTEXITCODE -ne 0) {
    Write-Error-Exit "Tests failed"
}

Write-Success "Tests passed"

# 3. 원격 서버에 업로드 (WinSCP 또는 PuTTY 사용)
Write-Step "Uploading to remote server..."

# WinSCP를 사용하는 경우
$winscp = "C:\Program Files (x86)\WinSCP\WinSCP.com"
if (Test-Path $winscp) {
    $script = @"
option batch on
open sftp://${RemoteUser}@${RemoteHost}/
put build\gameserver $RemotePath\
put config.$Environment.json $RemotePath\config.json
close
exit
"@
    
    $script | & $winscp
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Exit "Upload failed"
    }
} else {
    Write-Host "WinSCP not found. Please install WinSCP or use alternative tool." -ForegroundColor Yellow
    Write-Host "Alternatively, configure plink and pscp manually."
}

Write-Success "Upload completed"

# 4. SSH로 원격 서버 명령 실행 (Plink 사용)
Write-Step "Restarting service..."

$plink = "C:\Program Files (x86)\PuTTY\plink.exe"
if (Test-Path $plink) {
    $commands = @"
set -e
if systemctl is-active --quiet gameserver; then
    systemctl stop gameserver
    sleep 5
fi
if [ -f $RemotePath/gameserver ]; then
    cp $RemotePath/gameserver $RemotePath/gameserver.backup.\$(date +%s)
fi
chmod +x $RemotePath/gameserver
systemctl start gameserver
sleep 2
systemctl status gameserver
"@
    
    $commands | & $plink -ssh "${RemoteUser}@${RemoteHost}" -batch
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Exit "Service restart failed"
    }
} else {
    Write-Host "PuTTY plink not found. Please install PuTTY." -ForegroundColor Yellow
}

Write-Success "Service restarted"

# 5. 헬스 체크
Write-Step "Health check..."
Start-Sleep -Seconds 3

try {
    $healthCheck = & $plink -ssh "${RemoteUser}@${RemoteHost}" -batch "curl -s http://localhost:8080/health"
    
    if ($healthCheck -match "ok") {
        Write-Success "Health check passed"
        Write-Host "==========================================" -ForegroundColor Green
        Write-Host "Deployment successful!" -ForegroundColor Green
        Write-Host "Version: $Version" -ForegroundColor Green
        Write-Host "Environment: $Environment" -ForegroundColor Green
        Write-Host "==========================================" -ForegroundColor Green
    } else {
        Write-Error-Exit "Health check failed"
    }
} catch {
    Write-Host "Health check skipped (check manually)" -ForegroundColor Yellow
}

# 환경 복구
$env:GOOS = "windows"
$env:GOARCH = "amd64"
```

---

## 28.5 버전 관리 전략

효율적인 버전 관리는 배포 품질을 보장한다.

### Semantic Versioning

게임 서버는 의미 있는 버전 체계를 따른다.

```go
// version.go

package main

import (
	"fmt"
)

// Version는 의미 있는 버전을 나타낸다.
type Version struct {
	Major int
	Minor int
	Patch int
	Build string // 빌드 메타데이터
}

// String은 버전을 문자열로 반환한다.
func (v Version) String() string {
	s := fmt.Sprintf("%d.%d.%d", v.Major, v.Minor, v.Patch)
	if v.Build != "" {
		s += fmt.Sprintf("+%s", v.Build)
	}
	return s
}

// IsCompatible은 두 버전의 호환성을 검사한다.
// 같은 메이저 버전은 호환된다고 가정한다.
func (v Version) IsCompatible(other Version) bool {
	return v.Major == other.Major
}

// NewVersion은 새로운 버전을 생성한다.
func NewVersion(major, minor, patch int) Version {
	return Version{
		Major: major,
		Minor: minor,
		Patch: patch,
	}
}

// IncrementMajor는 메이저 버전을 증가시킨다.
func (v *Version) IncrementMajor() {
	v.Major++
	v.Minor = 0
	v.Patch = 0
}

// IncrementMinor는 마이너 버전을 증가시킨다.
func (v *Version) IncrementMinor() {
	v.Minor++
	v.Patch = 0
}

// IncrementPatch는 패치 버전을 증가시킨다.
func (v *Version) IncrementPatch() {
	v.Patch++
}
```

### 버전 태그 관리

Git을 사용하여 버전을 관리한다.

```bash
# 현재 버전 확인
git describe --tags --always

# 새 버전으로 태그 생성 및 푸시
git tag -a v1.2.0 -m "Release version 1.2.0"
git push origin v1.2.0

# 모든 태그 확인
git tag

# 특정 버전으로 체크아웃
git checkout v1.2.0
```

### PowerShell에서 버전 관리

```powershell
# version-manager.ps1

param(
    [ValidateSet("major", "minor", "patch", "show")]
    [string]$Action = "show"
)

$versionFile = "VERSION"

function Read-Version {
    if (Test-Path $versionFile) {
        $content = Get-Content $versionFile -Raw
        return $content.Trim()
    }
    return "0.0.0"
}

function Write-Version {
    param([string]$Version)
    Set-Content $versionFile -Value $Version -NoNewline
}

function Parse-Version {
    param([string]$Version)
    $parts = $Version.Split(".")
    return @{
        Major = [int]$parts[0]
        Minor = [int]$parts[1]
        Patch = [int]$parts[2]
    }
}

function Format-Version {
    param($Major, $Minor, $Patch)
    return "$Major.$Minor.$Patch"
}

# 현재 버전
$currentVersion = Read-Version
$versionInfo = Parse-Version $currentVersion

switch ($Action) {
    "major" {
        $versionInfo.Major++
        $versionInfo.Minor = 0
        $versionInfo.Patch = 0
    }
    "minor" {
        $versionInfo.Minor++
        $versionInfo.Patch = 0
    }
    "patch" {
        $versionInfo.Patch++
    }
    "show" {
        Write-Host "Current version: $currentVersion"
        return
    }
}

$newVersion = Format-Version $versionInfo.Major $versionInfo.Minor $versionInfo.Patch
Write-Version $newVersion
Write-Host "Version bumped to $newVersion"

# Git 태그 생성
git tag -a "v$newVersion" -m "Release version $newVersion"
git push origin "v$newVersion"
Write-Host "Git tag created: v$newVersion"
```

### 배포 파이프라인 흐름도

배포 과정을 시각화하면 다음과 같다.

```
┌─────────────────┐
│  커밋 및 푸시   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 자동 테스트     │
└────────┬────────┘
         │
    ┌────┴────┐
    │          │
    ▼(실패)    ▼(성공)
┌────────┐   ┌──────────────┐
│  중단  │   │ 빌드 (멀티플)│
└────────┘   └────────┬─────┘
                      │
                      ▼
             ┌──────────────────┐
             │ Dev 환경 배포    │
             └────────┬─────────┘
                      │
                      ▼
             ┌──────────────────┐
             │ 통합 테스트      │
             └────────┬─────────┘
                      │
                 ┌────┴────────┐
                 │             │
            ┌────▼──┐   ┌──────▼─────┐
            │ 승인  │   │ 자동 롤백  │
            └────┬──┘   └────────────┘
                 │
                 ▼
      ┌──────────────────────┐
      │ Staging 환경 배포    │
      └─────────┬────────────┘
                 │
                 ▼
      ┌──────────────────────┐
      │ UAT 및 성능 테스트   │
      └─────────┬────────────┘
                 │
            ┌────┴─────┐
            │           │
       ┌────▼──┐   ┌───▼──────┐
       │ 승인  │   │ 자동 롤백│
       └────┬──┘   └──────────┘
            │
            ▼
  ┌──────────────────────┐
  │ Production 배포      │
  │ (자동 또는 수동)     │
  └──────────────────────┘
```

### CI/CD 설정 예제 (GitHub Actions)

```yaml
# .github/workflows/deploy.yml

name: Deploy Game Server

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Set up Go
        uses: actions/setup-go@v4
        with:
          go-version: '1.25'
      
      - name: Run tests
        run: go test -v ./...
      
      - name: Build for Linux
        run: |
          GOOS=linux GOARCH=amd64 go build \
            -ldflags="-X main.Version=${{ github.ref_name }}" \
            -o gameserver main.go
      
      - name: Create Release
        uses: actions/create-release@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          tag_name: ${{ github.ref }}
          release_name: Release ${{ github.ref_name }}
          draft: false
          prerelease: false
      
      - name: Upload Release Asset
        uses: actions/upload-release-asset@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          upload_url: ${{ steps.create_release.outputs.upload_url }}
          asset_path: ./gameserver
          asset_name: gameserver_linux_amd64
          asset_content_type: application/octet-stream
      
      - name: Deploy to Production
        if: success()
        run: |
          mkdir -p ~/.ssh
          echo "${{ secrets.DEPLOY_KEY }}" > ~/.ssh/deploy_key
          chmod 600 ~/.ssh/deploy_key
          ssh -i ~/.ssh/deploy_key -o StrictHostKeyChecking=no \
            gameadmin@game.example.com \
            "bash /opt/gameserver/deploy.sh ${{ github.ref_name }}"
```

이 장에서 다루는 빌드와 배포 기법들은 안정적이고 반복 가능한 배포를 가능하게 한다. 크로스 컴파일로 여러 플랫폼을 지원하고, 자동화된 스크립트로 실수를 줄이며, 버전 관리로 배포 이력을 추적할 수 있다.
   
  
# Chapter 29. 운영 고려사항

게임 서버를 운영하는 것은 개발만큼이나 중요하다. 안정적인 서비스를 제공하려면 로그를 체계적으로 관리하고, 장애에 신속하게 대응하며, 데이터를 안전하게 보호해야 한다. 이 장에서는 실제 운영 환경에서 마주치는 다양한 상황을 처리하는 방법을 다룬다.

## 29.1 로그 로테이션

로그 파일이 무한정 커지면 디스크 공간을 낭비하고 로그 조회 성능을 떨어뜨린다. 따라서 일정 크기에 도달하거나 특정 시간이 지나면 로그 파일을 교체해야 한다.

### 크기 기반 로테이션

```go
// log_rotator.go

package main

import (
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// RotationPolicy는 로그 로테이션 정책을 정의한다.
type RotationPolicy struct {
	MaxFileSize  int64  // 파일 최대 크기 (바이트)
	MaxBackups   int    // 보관할 백업 파일 수
	MaxAgeDays   int    // 파일 보관 기간 (일)
	CompressionFormat string // 압축 포맷 (gzip, none)
}

// RotatingFileWriter는 자동으로 로그 파일을 로테이션한다.
type RotatingFileWriter struct {
	logDir      string
	baseName    string
	currentFile *os.File
	currentSize int64
	policy      RotationPolicy
	mu          sync.Mutex
	writeCount  int64
}

// NewRotatingFileWriter는 새로운 로테이팅 파일 라이터를 생성한다.
func NewRotatingFileWriter(logDir, baseName string, policy RotationPolicy) (*RotatingFileWriter, error) {
	// 로그 디렉토리 생성
	if err := os.MkdirAll(logDir, 0755); err != nil {
		return nil, fmt.Errorf("failed to create log directory: %w", err)
	}

	rfw := &RotatingFileWriter{
		logDir:   logDir,
		baseName: baseName,
		policy:   policy,
	}

	// 새로운 로그 파일 열기
	if err := rfw.openNewFile(); err != nil {
		return nil, err
	}

	return rfw, nil
}

// Write는 데이터를 로그 파일에 쓴다.
func (rfw *RotatingFileWriter) Write(p []byte) (int, error) {
	rfw.mu.Lock()
	defer rfw.mu.Unlock()

	// 로테이션 필요 여부 확인
	if rfw.currentSize+int64(len(p)) > rfw.policy.MaxFileSize {
		if err := rfw.rotate(); err != nil {
			return 0, err
		}
	}

	// 파일에 쓰기
	n, err := rfw.currentFile.Write(p)
	if err == nil {
		rfw.currentSize += int64(n)
		rfw.writeCount++

		// 일정 횟수마다 정리 작업 수행
		if rfw.writeCount%1000 == 0 {
			go rfw.cleanup()
		}
	}

	return n, err
}

// openNewFile은 새로운 로그 파일을 연다.
func (rfw *RotatingFileWriter) openNewFile() error {
	if rfw.currentFile != nil {
		rfw.currentFile.Close()
	}

	// 파일명: gameserver-2025-01-15-14-30-45.log
	now := time.Now()
	filename := filepath.Join(rfw.logDir,
		fmt.Sprintf("%s-%04d-%02d-%02d-%02d-%02d-%02d.log",
			rfw.baseName,
			now.Year(), now.Month(), now.Day(),
			now.Hour(), now.Minute(), now.Second()))

	file, err := os.OpenFile(filename, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err != nil {
		return fmt.Errorf("failed to open log file: %w", err)
	}

	rfw.currentFile = file
	rfw.currentSize = 0

	return nil
}

// rotate는 현재 로그 파일을 로테이션한다.
func (rfw *RotatingFileWriter) rotate() error {
	if err := rfw.openNewFile(); err != nil {
		return err
	}

	// 백그라운드에서 정리 작업 수행
	go rfw.cleanup()

	return nil
}

// cleanup은 오래된 로그 파일을 삭제한다.
func (rfw *RotatingFileWriter) cleanup() {
	files, err := filepath.Glob(filepath.Join(rfw.logDir, rfw.baseName+"*.log"))
	if err != nil {
		return
	}

	// 파일을 수정 시간으로 정렬 (가장 오래된 것부터)
	type fileInfo struct {
		path    string
		modTime time.Time
	}

	var fileInfos []fileInfo
	now := time.Now()

	for _, file := range files {
		info, err := os.Stat(file)
		if err != nil {
			continue
		}

		fileInfos = append(fileInfos, fileInfo{
			path:    file,
			modTime: info.ModTime(),
		})
	}

	// 버블 정렬로 정렬 (간단하게 구현)
	for i := 0; i < len(fileInfos)-1; i++ {
		for j := i + 1; j < len(fileInfos); j++ {
			if fileInfos[i].modTime.After(fileInfos[j].modTime) {
				fileInfos[i], fileInfos[j] = fileInfos[j], fileInfos[i]
			}
		}
	}

	// 오래된 파일 삭제
	deletedCount := 0
	for _, fInfo := range fileInfos {
		// 최대 백업 수 초과 확인
		if deletedCount >= len(fileInfos)-rfw.policy.MaxBackups {
			break
		}

		// 파일 나이 확인
		age := now.Sub(fInfo.modTime)
		if age > time.Duration(rfw.policy.MaxAgeDays)*24*time.Hour {
			if err := os.Remove(fInfo.path); err == nil {
				deletedCount++
			}
		}
	}
}

// Close는 로그 파일을 닫는다.
func (rfw *RotatingFileWriter) Close() error {
	rfw.mu.Lock()
	defer rfw.mu.Unlock()

	if rfw.currentFile != nil {
		return rfw.currentFile.Close()
	}
	return nil
}

// 사용 예제
func main() {
	policy := RotationPolicy{
		MaxFileSize:       10 * 1024 * 1024, // 10MB
		MaxBackups:        10,                // 최대 10개 파일 보관
		MaxAgeDays:        30,                // 30일 후 삭제
		CompressionFormat: "gzip",
	}

	logger, err := NewRotatingFileWriter("./logs", "gameserver", policy)
	if err != nil {
		fmt.Printf("Failed to create logger: %v\n", err)
		return
	}
	defer logger.Close()

	// 로그 작성 시뮬레이션
	for i := 0; i < 1000; i++ {
		message := fmt.Sprintf("[%d] Game server log message: %s\n", i, time.Now().Format(time.RFC3339))
		logger.Write([]byte(message))
	}

	fmt.Println("Logging completed")
}
```

### 시간 기반 로테이션

일일 로테이션이 필요한 경우 시간 기반 로테이션을 사용한다.

```go
// daily_log_rotator.go

package main

import (
	"fmt"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// DailyRotatingLogger는 매일 자정에 로그 파일을 교체한다.
type DailyRotatingLogger struct {
	logDir      string
	baseName    string
	currentFile *os.File
	currentDate time.Time
	mu          sync.Mutex
	stopCh      chan struct{}
}

// NewDailyRotatingLogger는 새로운 일일 로테이팅 로거를 생성한다.
func NewDailyRotatingLogger(logDir, baseName string) (*DailyRotatingLogger, error) {
	if err := os.MkdirAll(logDir, 0755); err != nil {
		return nil, fmt.Errorf("failed to create log directory: %w", err)
	}

	drl := &DailyRotatingLogger{
		logDir:   logDir,
		baseName: baseName,
		stopCh:   make(chan struct{}),
	}

	if err := drl.openNewFile(); err != nil {
		return nil, err
	}

	// 백그라운드에서 로테이션 모니터 시작
	go drl.rotationMonitor()

	return drl, nil
}

// Write는 데이터를 로그 파일에 쓴다.
func (drl *DailyRotatingLogger) Write(p []byte) (int, error) {
	drl.mu.Lock()
	defer drl.mu.Unlock()

	// 날짜가 바뀌었는지 확인
	today := time.Now().Truncate(24 * time.Hour)
	if !today.Equal(drl.currentDate) {
		if err := drl.openNewFile(); err != nil {
			return 0, err
		}
	}

	return drl.currentFile.Write(p)
}

// openNewFile은 새로운 로그 파일을 연다.
func (drl *DailyRotatingLogger) openNewFile() error {
	if drl.currentFile != nil {
		drl.currentFile.Close()
	}

	// 파일명: gameserver-2025-01-15.log
	now := time.Now()
	drl.currentDate = now.Truncate(24 * time.Hour)

	filename := filepath.Join(drl.logDir,
		fmt.Sprintf("%s-%04d-%02d-%02d.log",
			drl.baseName,
			now.Year(), now.Month(), now.Day()))

	file, err := os.OpenFile(filename, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err != nil {
		return fmt.Errorf("failed to open log file: %w", err)
	}

	drl.currentFile = file
	return nil
}

// rotationMonitor는 매일 자정에 로그 파일을 교체하는지 모니터한다.
func (drl *DailyRotatingLogger) rotationMonitor() {
	for {
		// 다음 자정까지의 시간 계산
		now := time.Now()
		nextMidnight := now.Truncate(24*time.Hour).Add(24 * time.Hour)
		duration := nextMidnight.Sub(now)

		select {
		case <-time.After(duration):
			drl.mu.Lock()
			drl.openNewFile()
			drl.mu.Unlock()

		case <-drl.stopCh:
			return
		}
	}
}

// Close는 로거를 종료한다.
func (drl *DailyRotatingLogger) Close() error {
	close(drl.stopCh)

	drl.mu.Lock()
	defer drl.mu.Unlock()

	if drl.currentFile != nil {
		return drl.currentFile.Close()
	}
	return nil
}
```

### Systemd와의 통합

Linux 서버에서는 systemd를 사용하여 로그를 관리할 수 있다.

```ini
# /etc/systemd/system/gameserver.service

[Unit]
Description=Game Server
After=network.target

[Service]
Type=simple
User=gameadmin
WorkingDirectory=/opt/gameserver
ExecStart=/opt/gameserver/gameserver
Restart=always
RestartSec=10

# 로그 설정
StandardOutput=journal
StandardError=journal
SyslogIdentifier=gameserver

# 리소스 제한
LimitNOFILE=65536
LimitNPROC=32768

[Install]
WantedBy=multi-user.target
```

Systemd 저널 관리다.

```bash
# 최근 로그 조회
journalctl -u gameserver -n 50

# 실시간 로그 모니터링
journalctl -u gameserver -f

# 시간 범위로 로그 조회
journalctl -u gameserver --since "2025-01-15 10:00:00" --until "2025-01-15 11:00:00"

# 로그를 파일로 저장
journalctl -u gameserver > gameserver.log

# 저널 용량 제한 설정 (100MB)
journalctl --vacuum-size=100M
```

---

## 29.2 에러 리포팅

발생한 에러를 추적하고 분석하는 것은 서버 안정성을 높이기 위해 필수적이다.

### 에러 모니터링 시스템

```go
// error_reporter.go

package main

import (
	"encoding/json"
	"fmt"
	"os"
	"runtime"
	"sync"
	"time"
)

// ErrorReport는 에러 정보를 담는다.
type ErrorReport struct {
	Timestamp   time.Time `json:"timestamp"`
	ErrorType   string    `json:"error_type"`
	Message     string    `json:"message"`
	StackTrace  string    `json:"stack_trace"`
	Context     map[string]interface{} `json:"context"`
	Severity    string    `json:"severity"` // "low", "medium", "high", "critical"
	ServerInfo  ServerInfo `json:"server_info"`
}

// ServerInfo는 서버 정보를 담는다.
type ServerInfo struct {
	Version    string `json:"version"`
	Environment string `json:"environment"`
	Hostname   string `json:"hostname"`
	GoVersion  string `json:"go_version"`
}

// ErrorReporter는 에러를 보고한다.
type ErrorReporter struct {
	reports   []*ErrorReport
	mu        sync.RWMutex
	maxSize   int
	outputDir string
	serverInfo ServerInfo
}

// NewErrorReporter는 새로운 에러 리포터를 생성한다.
func NewErrorReporter(outputDir string, maxSize int) *ErrorReporter {
	hostname, _ := os.Hostname()

	return &ErrorReporter{
		reports:   make([]*ErrorReport, 0, maxSize),
		maxSize:   maxSize,
		outputDir: outputDir,
		serverInfo: ServerInfo{
			Version:     "1.0.0",
			Environment: os.Getenv("ENVIRONMENT"),
			Hostname:    hostname,
			GoVersion:   runtime.Version(),
		},
	}
}

// Report는 에러를 보고한다.
func (er *ErrorReporter) Report(errType string, err error, severity string, context map[string]interface{}) {
	report := &ErrorReport{
		Timestamp:  time.Now(),
		ErrorType:  errType,
		Message:    err.Error(),
		StackTrace: getStackTrace(),
		Context:    context,
		Severity:   severity,
		ServerInfo: er.serverInfo,
	}

	er.mu.Lock()
	er.reports = append(er.reports, report)

	// 버퍼가 가득 차면 파일로 저장
	if len(er.reports) >= er.maxSize {
		er.flushUnsafe()
	}
	er.mu.Unlock()

	// 치명적 에러는 즉시 저장
	if severity == "critical" {
		er.Flush()
	}
}

// Flush는 모든 에러 보고를 파일로 저장한다.
func (er *ErrorReporter) Flush() {
	er.mu.Lock()
	defer er.mu.Unlock()
	er.flushUnsafe()
}

// flushUnsafe는 동기화 없이 버퍼를 플러시한다.
func (er *ErrorReporter) flushUnsafe() {
	if len(er.reports) == 0 {
		return
	}

	filename := filepath.Join(er.outputDir,
		fmt.Sprintf("error_report_%s.json", time.Now().Format("2006-01-02-15-04-05")))

	file, err := os.OpenFile(filename, os.O_CREATE|os.O_WRONLY|os.O_APPEND, 0644)
	if err != nil {
		fmt.Printf("Failed to open error report file: %v\n", err)
		return
	}
	defer file.Close()

	encoder := json.NewEncoder(file)
	for _, report := range er.reports {
		if err := encoder.Encode(report); err != nil {
			fmt.Printf("Failed to encode error report: %v\n", err)
		}
	}

	er.reports = er.reports[:0]
}

// GetStats는 에러 통계를 반환한다.
func (er *ErrorReporter) GetStats() map[string]int {
	er.mu.RLock()
	defer er.mu.RUnlock()

	stats := make(map[string]int)
	for _, report := range er.reports {
		stats[report.ErrorType]++
		stats[report.Severity]++
	}

	return stats
}

// getStackTrace는 현재 스택 트레이스를 반환한다.
func getStackTrace() string {
	buf := make([]byte, 4096)
	n := runtime.Stack(buf, false)
	return string(buf[:n])
}

// 사용 예제
func main() {
	os.MkdirAll("./error_reports", 0755)

	reporter := NewErrorReporter("./error_reports", 100)
	defer reporter.Flush()

	// 에러 보고 예제
	context := map[string]interface{}{
		"player_id": 123,
		"room_id":   456,
		"action":    "bet",
	}

	fmt.Println("Error reporting system started")

	// 다양한 심각도의 에러 시뮬레이션
	reporter.Report("NetworkError", fmt.Errorf("connection timeout"), "high", context)
	reporter.Report("GameLogicError", fmt.Errorf("invalid hand evaluation"), "medium", context)
	reporter.Report("DatabaseError", fmt.Errorf("connection pool exhausted"), "critical", context)

	// 통계 출력
	stats := reporter.GetStats()
	fmt.Println("Error Statistics:")
	for errorType, count := range stats {
		fmt.Printf("  %s: %d\n", errorType, count)
	}
}
```

### 에러 알림 시스템

critical 에러 발생 시 즉시 알림을 받아야 한다.

```go
// error_notifier.go

package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"sync"
)

// Notifier는 에러 알림을 보낸다.
type Notifier interface {
	Notify(report *ErrorReport) error
}

// SlackNotifier는 Slack으로 알림을 보낸다.
type SlackNotifier struct {
	webhookURL string
	mu         sync.Mutex
}

// NewSlackNotifier는 새로운 Slack 노티파이어를 생성한다.
func NewSlackNotifier(webhookURL string) *SlackNotifier {
	return &SlackNotifier{
		webhookURL: webhookURL,
	}
}

// Notify는 Slack으로 알림을 보낸다.
func (sn *SlackNotifier) Notify(report *ErrorReport) error {
	sn.mu.Lock()
	defer sn.mu.Unlock()

	// 심각도에 따른 색상 설정
	color := "#36a64f" // green
	if report.Severity == "high" {
		color = "#ff9900" // orange
	} else if report.Severity == "critical" {
		color = "#ff0000" // red
	}

	// Slack 메시지 구성
	payload := map[string]interface{}{
		"attachments": []map[string]interface{}{
			{
				"color": color,
				"title": fmt.Sprintf("[%s] %s", report.Severity, report.ErrorType),
				"text":  report.Message,
				"fields": []map[string]interface{}{
					{
						"title": "Server",
						"value": report.ServerInfo.Hostname,
						"short": true,
					},
					{
						"title": "Timestamp",
						"value": report.Timestamp.Format("2006-01-02 15:04:05"),
						"short": true,
					},
					{
						"title": "Context",
						"value": fmt.Sprintf("%v", report.Context),
						"short": false,
					},
				},
			},
		},
	}

	data, err := json.Marshal(payload)
	if err != nil {
		return err
	}

	resp, err := http.Post(sn.webhookURL, "application/json", bytes.NewBuffer(data))
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("slack notification failed with status %d", resp.StatusCode)
	}

	return nil
}

// EmailNotifier는 이메일로 알림을 보낸다.
type EmailNotifier struct {
	smtpServer string
	fromEmail  string
	toEmail    string
	mu         sync.Mutex
}

// NewEmailNotifier는 새로운 이메일 노티파이어를 생성한다.
func NewEmailNotifier(smtpServer, fromEmail, toEmail string) *EmailNotifier {
	return &EmailNotifier{
		smtpServer: smtpServer,
		fromEmail:  fromEmail,
		toEmail:    toEmail,
	}
}

// Notify는 이메일로 알림을 보낸다.
func (en *EmailNotifier) Notify(report *ErrorReport) error {
	en.mu.Lock()
	defer en.mu.Unlock()

	// 실제 구현에서는 net/smtp를 사용하여 이메일 전송
	fmt.Printf("Email notification sent to %s\n", en.toEmail)
	fmt.Printf("Subject: [%s] %s - %s\n", report.Severity, report.ErrorType, report.Message)

	return nil
}

// MultiNotifier는 여러 노티파이어를 사용한다.
type MultiNotifier struct {
	notifiers []Notifier
	mu        sync.Mutex
}

// NewMultiNotifier는 새로운 멀티 노티파이어를 생성한다.
func NewMultiNotifier(notifiers ...Notifier) *MultiNotifier {
	return &MultiNotifier{
		notifiers: notifiers,
	}
}

// Notify는 모든 노티파이어로 알림을 보낸다.
func (mn *MultiNotifier) Notify(report *ErrorReport) error {
	mn.mu.Lock()
	defer mn.mu.Unlock()

	var lastErr error
	for _, notifier := range mn.notifiers {
		if err := notifier.Notify(report); err != nil {
			lastErr = err
		}
	}

	return lastErr
}
```

---

## 29.3 핫픽스 전략

버그가 발견되었을 때 신속하게 대응할 수 있는 핫픽스 프로세스가 필요하다.

### 핫픽스 배포 절차

```
┌──────────────────────────────────────────┐
│         버그 발견 및 보고                 │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    심각도 판단 및 우선순위 결정           │
│   - Critical: 즉시 배포                  │
│   - High: 당일 배포                      │
│   - Medium: 계획된 배포                  │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    개발 환경에서 버그 재현 및 수정        │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    단위 테스트 및 통합 테스트 작성/실행   │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    Staging 환경에서 완전히 검증           │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    운영팀 승인 및 배포 계획               │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    프로덕션 배포 (블루-그린 또는 카나리)  │
└──────────┬───────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────┐
│    헬스 체크 및 모니터링                  │
│   - 에러율 모니터링                      │
│   - 성능 메트릭 확인                     │
│   - 사용자 피드백 수집                   │
└──────────┬───────────────────────────────┘
           │
      ┌────┴─────────┐
      │              │
   (성공)          (실패)
      │              │
      ▼              ▼
   배포 완료    자동 롤백 및
               재검토
```

### 핫픽스 스크립트

```bash
#!/bin/bash
# hotfix.sh - 핫픽스 배포 자동화 스크립트

set -e

# 설정
HOTFIX_ID=$1
SEVERITY=$2 # critical, high, medium
DESCRIPTION=$3

if [ -z "$HOTFIX_ID" ] || [ -z "$SEVERITY" ] || [ -z "$DESCRIPTION" ]; then
    echo "Usage: ./hotfix.sh <hotfix_id> <severity> <description>"
    echo "Example: ./hotfix.sh HF-001 critical 'Player disconnection bug'"
    exit 1
fi

echo "=========================================="
echo "Starting Hotfix Deployment"
echo "ID: $HOTFIX_ID"
echo "Severity: $SEVERITY"
echo "Description: $DESCRIPTION"
echo "=========================================="

# 1. 현재 상태 저장 (롤백용)
echo "[1/7] Saving current state..."
BACKUP_DIR="backups/hotfix_$HOTFIX_ID"
mkdir -p "$BACKUP_DIR"
cp gameserver "$BACKUP_DIR/gameserver.backup"

# 2. 메인 브랜치에서 최신 코드 받기
echo "[2/7] Getting latest code..."
git fetch origin main
git merge origin/main

# 3. 핫픽스 브랜치 생성
echo "[3/7] Creating hotfix branch..."
BRANCH="hotfix/$HOTFIX_ID"
git checkout -b "$BRANCH"

# 4. 빌드
echo "[4/7] Building..."
GOOS=linux GOARCH=amd64 go build \
    -ldflags="-X main.Version=hotfix-$HOTFIX_ID -X main.Severity=$SEVERITY" \
    -o gameserver main.go

# 5. 테스트
echo "[5/7] Running tests..."
go test -v ./...

# 6. Staging 배포
echo "[6/7] Deploying to staging..."
scp gameserver gameadmin@staging.example.com:/opt/gameserver/
ssh gameadmin@staging.example.com "systemctl restart gameserver"
sleep 5

# 헬스 체크
if ! ssh gameadmin@staging.example.com "curl -f http://localhost:8080/health > /dev/null"; then
    echo "Staging health check failed!"
    exit 1
fi

echo "[7/7] Hotfix ready for production..."
echo "Manual approval required before production deployment"
echo "Run: git push origin $BRANCH"
```

### 롤백 메커니즘

```go
// rollback.go

package main

import (
	"fmt"
	"os"
	"os/exec"
	"time"
)

// RollbackPoint는 롤백 지점을 나타낸다.
type RollbackPoint struct {
	Timestamp   time.Time
	Version     string
	Description string
	BinaryPath  string
	ConfigPath  string
}

// RollbackManager는 롤백을 관리한다.
type RollbackManager struct {
	rollbackDir string
	points      []*RollbackPoint
	current     *RollbackPoint
}

// NewRollbackManager는 새로운 롤백 매니저를 생성한다.
func NewRollbackManager(rollbackDir string) *RollbackManager {
	os.MkdirAll(rollbackDir, 0755)

	return &RollbackManager{
		rollbackDir: rollbackDir,
		points:      make([]*RollbackPoint, 0),
	}
}

// SaveCheckpoint는 현재 상태를 롤백 지점으로 저장한다.
func (rm *RollbackManager) SaveCheckpoint(version, description, binaryPath, configPath string) error {
	checkpoint := &RollbackPoint{
		Timestamp:   time.Now(),
		Version:     version,
		Description: description,
		BinaryPath:  binaryPath,
		ConfigPath:  configPath,
	}

	// 파일 복사
	backupName := fmt.Sprintf("gameserver_%s_%s", version, time.Now().Format("20060102150405"))
	backupPath := fmt.Sprintf("%s/%s", rm.rollbackDir, backupName)

	cmd := exec.Command("cp", binaryPath, backupPath)
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("failed to backup binary: %w", err)
	}

	checkpoint.BinaryPath = backupPath
	rm.points = append(rm.points, checkpoint)
	rm.current = checkpoint

	fmt.Printf("Checkpoint saved: %s\n", version)
	return nil
}

// Rollback은 특정 버전으로 롤백한다.
func (rm *RollbackManager) Rollback(version string) error {
	// 해당 버전의 체크포인트 찾기
	var target *RollbackPoint
	for _, point := range rm.points {
		if point.Version == version {
			target = point
			break
		}
	}

	if target == nil {
		return fmt.Errorf("rollback point not found: %s", version)
	}

	fmt.Printf("Rolling back to version: %s\n", version)

	// 서비스 중지
	cmd := exec.Command("systemctl", "stop", "gameserver")
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("failed to stop service: %w", err)
	}

	// 바이너리 복원
	cmd = exec.Command("cp", target.BinaryPath, "/opt/gameserver/gameserver")
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("failed to restore binary: %w", err)
	}

	// 설정 복원
	if target.ConfigPath != "" {
		cmd = exec.Command("cp", target.ConfigPath, "/opt/gameserver/config.json")
		if err := cmd.Run(); err != nil {
			return fmt.Errorf("failed to restore config: %w", err)
		}
	}

	// 서비스 시작
	cmd = exec.Command("systemctl", "start", "gameserver")
	if err := cmd.Run(); err != nil {
		return fmt.Errorf("failed to start service: %w", err)
	}

	rm.current = target
	fmt.Printf("Rollback completed to version: %s\n", version)

	return nil
}

// ListCheckpoints는 모든 체크포인트를 나열한다.
func (rm *RollbackManager) ListCheckpoints() {
	fmt.Println("Available Checkpoints:")
	for i, point := range rm.points {
		fmt.Printf("[%d] Version: %s, Time: %s, Description: %s\n",
			i, point.Version, point.Timestamp.Format(time.RFC3339), point.Description)
	}
}

// AutomaticRollback은 에러 발생 시 자동 롤백을 수행한다.
func (rm *RollbackManager) AutomaticRollback(errorCount int, threshold int) bool {
	if errorCount > threshold {
		fmt.Printf("Error threshold exceeded (%d > %d), initiating automatic rollback\n", errorCount, threshold)

		if len(rm.points) > 1 {
			// 이전 버전으로 롤백
			previousVersion := rm.points[len(rm.points)-2].Version
			if err := rm.Rollback(previousVersion); err != nil {
				fmt.Printf("Automatic rollback failed: %v\n", err)
				return false
			}
			return true
		}
	}

	return false
}
```

---

## 29.4 데이터 백업

게임 서버의 플레이어 데이터와 설정은 반드시 백업되어야 한다.

### 백업 전략

```go
// backup.go

package main

import (
	"archive/tar"
	"compress/gzip"
	"fmt"
	"io"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// BackupConfig는 백업 설정이다.
type BackupConfig struct {
	SourcePaths      []string      // 백업할 경로 목록
	DestinationDir   string        // 백업 저장 디렉토리
	MaxBackups       int           // 유지할 최대 백업 수
	FullBackupInterval int         // 전체 백업 주기 (시간)
	IncrementalInterval int        // 증분 백업 주기 (시간)
	Compression      bool          // 압축 여부
}

// BackupManager는 백업을 관리한다.
type BackupManager struct {
	config    BackupConfig
	lastFullBackup time.Time
	lastBackupTime time.Time
	mu        sync.Mutex
	stopCh    chan struct{}
}

// NewBackupManager는 새로운 백업 매니저를 생성한다.
func NewBackupManager(config BackupConfig) *BackupManager {
	os.MkdirAll(config.DestinationDir, 0755)

	bm := &BackupManager{
		config: config,
		stopCh: make(chan struct{}),
	}

	// 백그라운드에서 정기적 백업 실행
	go bm.scheduleBackups()

	return bm
}

// BackupNow는 즉시 백업을 수행한다.
func (bm *BackupManager) BackupNow(backupType string) error {
	bm.mu.Lock()
	defer bm.mu.Unlock()

	filename := fmt.Sprintf("backup_%s_%s.tar.gz",
		backupType, time.Now().Format("20060102150405"))
	filepath := filepath.Join(bm.config.DestinationDir, filename)

	fmt.Printf("Starting backup: %s\n", filename)

	file, err := os.Create(filepath)
	if err != nil {
		return fmt.Errorf("failed to create backup file: %w", err)
	}
	defer file.Close()

	// 압축 설정
	var writer io.Writer = file
	var gzipWriter *gzip.Writer

	if bm.config.Compression {
		gzipWriter = gzip.NewWriter(file)
		defer gzipWriter.Close()
		writer = gzipWriter
	}

	tarWriter := tar.NewWriter(writer)
	defer tarWriter.Close()

	// 모든 소스 경로 백업
	for _, sourcePath := range bm.config.SourcePaths {
		if err := bm.addToTar(tarWriter, sourcePath); err != nil {
			return err
		}
	}

	bm.lastBackupTime = time.Now()
	if backupType == "full" {
		bm.lastFullBackup = time.Now()
	}

	fmt.Printf("Backup completed: %s\n", filename)

	// 오래된 백업 정리
	bm.cleanupOldBackups()

	return nil
}

// addToTar는 파일/디렉토리를 tar 아카이브에 추가한다.
func (bm *BackupManager) addToTar(tw *tar.Writer, path string) error {
	return filepath.Walk(path, func(file string, info os.FileInfo, err error) error {
		if err != nil {
			return err
		}

		header, err := tar.FileInfoHeader(info, file)
		if err != nil {
			return err
		}

		// 경로 정규화
		header.Name = filepath.ToSlash(file)

		if err := tw.WriteHeader(header); err != nil {
			return err
		}

		// 파일 내용 복사
		if info.IsDir() {
			return nil
		}

		f, err := os.Open(file)
		if err != nil {
			return err
		}
		defer f.Close()

		_, err = io.Copy(tw, f)
		return err
	})
}

// Restore는 백업에서 복원한다.
func (bm *BackupManager) Restore(backupFile string) error {
	bm.mu.Lock()
	defer bm.mu.Unlock()

	fmt.Printf("Starting restore from: %s\n", backupFile)

	file, err := os.Open(backupFile)
	if err != nil {
		return fmt.Errorf("failed to open backup file: %w", err)
	}
	defer file.Close()

	var reader io.Reader = file

	// 압축 파일인지 확인
	gzipReader, err := gzip.NewReader(file)
	if err == nil {
		defer gzipReader.Close()
		reader = gzipReader
	}

	tarReader := tar.NewReader(reader)

	// 아카이브에서 파일 추출
	for {
		header, err := tarReader.Next()
		if err == io.EOF {
			break
		}
		if err != nil {
			return err
		}

		// 대상 경로 생성
		path := header.Name

		switch header.Typeflag {
		case tar.TypeDir:
			if err := os.MkdirAll(path, os.FileMode(header.Mode)); err != nil {
				return err
			}

		case tar.TypeReg:
			if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil {
				return err
			}

			outFile, err := os.Create(path)
			if err != nil {
				return err
			}

			if _, err := io.Copy(outFile, tarReader); err != nil {
				outFile.Close()
				return err
			}
			outFile.Close()
		}
	}

	fmt.Println("Restore completed")
	return nil
}

// scheduleBackups는 정기적으로 백업을 수행한다.
func (bm *BackupManager) scheduleBackups() {
	fullBackupTicker := time.NewTicker(time.Duration(bm.config.FullBackupInterval) * time.Hour)
	defer fullBackupTicker.Stop()

	for {
		select {
		case <-fullBackupTicker.C:
			if err := bm.BackupNow("full"); err != nil {
				fmt.Printf("Full backup failed: %v\n", err)
			}

		case <-bm.stopCh:
			return
		}
	}
}

// cleanupOldBackups는 오래된 백업을 삭제한다.
func (bm *BackupManager) cleanupOldBackups() {
	files, err := filepath.Glob(filepath.Join(bm.config.DestinationDir, "backup_*.tar.gz"))
	if err != nil {
		return
	}

	// 파일을 수정 시간으로 정렬
	type fileInfo struct {
		path    string
		modTime time.Time
	}

	var fileInfos []fileInfo
	for _, f := range files {
		info, err := os.Stat(f)
		if err != nil {
			continue
		}
		fileInfos = append(fileInfos, fileInfo{path: f, modTime: info.ModTime()})
	}

	// 최신 파일부터 유지하고 나머지 삭제
	if len(fileInfos) > bm.config.MaxBackups {
		// 간단히 구현 (실제로는 시간순 정렬 필요)
		for i := bm.config.MaxBackups; i < len(fileInfos); i++ {
			os.Remove(fileInfos[i].path)
		}
	}
}

// Stop은 백업 매니저를 종료한다.
func (bm *BackupManager) Stop() {
	close(bm.stopCh)
}

// 사용 예제
func main() {
	config := BackupConfig{
		SourcePaths:     []string{"/opt/gameserver/data", "/opt/gameserver/config"},
		DestinationDir:  "/backups/gameserver",
		MaxBackups:      10,
		FullBackupInterval: 24,  // 24시간
		IncrementalInterval: 6,  // 6시간
		Compression:     true,
	}

	manager := NewBackupManager(config)
	defer manager.Stop()

	// 즉시 백업 수행
	if err := manager.BackupNow("full"); err != nil {
		fmt.Printf("Backup error: %v\n", err)
	}

	// 복원 테스트
	backupFiles, _ := filepath.Glob("/backups/gameserver/backup_*.tar.gz")
	if len(backupFiles) > 0 {
		fmt.Printf("Available backups: %v\n", backupFiles)
	}
}
```

---

## 29.5 장애 대응 시나리오

다양한 장애 상황에 대한 대응 계획을 사전에 수립해야 한다.

### 장애 대응 체크리스트

```go
// incident_response.go

package main

import (
	"fmt"
	"log"
	"time"
)

// IncidentSeverity는 장애 심각도를 정의한다.
type IncidentSeverity int

const (
	Low IncidentSeverity = iota
	Medium
	High
	Critical
)

// Incident는 장애를 나타낸다.
type Incident struct {
	ID          string
	Title       string
	Description string
	Severity    IncidentSeverity
	StartTime   time.Time
	Detection   string // 어떻게 감지되었는가
	ImpactedService string
	ImpactedUsers int
}

// ResponsePlan은 장애 대응 계획이다.
type ResponsePlan struct {
	Incident      *Incident
	Steps         []ResponseStep
	CurrentStep   int
	Status        string // "identified", "investigating", "resolving", "resolved"
	Timeline      []TimelineEntry
}

// ResponseStep는 대응 단계를 나타낸다.
type ResponseStep struct {
	Order       int
	Action      string
	Responsible string
	EstimatedTime int // 분
	Completed   bool
	CompletedAt time.Time
}

// TimelineEntry는 타임라인 기록이다.
type TimelineEntry struct {
	Time    time.Time
	Message string
}

// IncidentResponseManager는 장애 대응을 관리한다.
type IncidentResponseManager struct {
	incidents map[string]*ResponsePlan
}

// NewIncidentResponseManager는 새로운 장애 대응 매니저를 생성한다.
func NewIncidentResponseManager() *IncidentResponseManager {
	return &IncidentResponseManager{
		incidents: make(map[string]*ResponsePlan),
	}
}

// CreateIncident는 새로운 장애를 등록한다.
func (irm *IncidentResponseManager) CreateIncident(incident *Incident) *ResponsePlan {
	plan := &ResponsePlan{
		Incident:    incident,
		Status:      "identified",
		Steps:       irm.getResponseSteps(incident),
		Timeline:    make([]TimelineEntry, 0),
	}

	irm.incidents[incident.ID] = plan

	plan.AddTimelineEntry(fmt.Sprintf("Incident detected: %s (%s)", incident.Title, incident.Detection))

	return plan
}

// getResponseSteps는 장애 유형에 따른 대응 단계를 반환한다.
func (irm *IncidentResponseManager) getResponseSteps(incident *Incident) []ResponseStep {
	steps := []ResponseStep{
		{
			Order:          1,
			Action:         "Assess severity and impact",
			Responsible:    "On-call Engineer",
			EstimatedTime:  5,
		},
		{
			Order:          2,
			Action:         "Notify stakeholders and incident commander",
			Responsible:    "Incident Commander",
			EstimatedTime:  5,
		},
		{
			Order:          3,
			Action:         "Isolate affected services if necessary",
			Responsible:    "On-call Engineer",
			EstimatedTime:  10,
		},
		{
			Order:          4,
			Action:         "Investigate root cause",
			Responsible:    "Senior Engineer",
			EstimatedTime:  20,
		},
		{
			Order:          5,
			Action:         "Implement temporary fix or workaround",
			Responsible:    "Senior Engineer",
			EstimatedTime:  30,
		},
		{
			Order:          6,
			Action:         "Deploy permanent fix",
			Responsible:    "DevOps Engineer",
			EstimatedTime:  15,
		},
		{
			Order:          7,
			Action:         "Verify service recovery",
			Responsible:    "On-call Engineer",
			EstimatedTime:  10,
		},
		{
			Order:          8,
			Action:         "Create post-incident report",
			Responsible:    "Incident Commander",
			EstimatedTime:  60,
		},
	}

	return steps
}

// CompleteStep은 대응 단계를 완료한다.
func (plan *ResponsePlan) CompleteStep() {
	if plan.CurrentStep < len(plan.Steps) {
		step := &plan.Steps[plan.CurrentStep]
		step.Completed = true
		step.CompletedAt = time.Now()

		message := fmt.Sprintf("Step %d completed: %s (took %d minutes)",
			step.Order,
			step.Action,
			int(step.CompletedAt.Sub(time.Now()).Minutes()))
		plan.AddTimelineEntry(message)

		plan.CurrentStep++
	}
}

// AddTimelineEntry는 타임라인에 항목을 추가한다.
func (plan *ResponsePlan) AddTimelineEntry(message string) {
	entry := TimelineEntry{
		Time:    time.Now(),
		Message: message,
	}
	plan.Timeline = append(plan.Timeline, entry)
	log.Printf("[%s] %s\n", plan.Incident.ID, message)
}

// ResolveIncident는 장애를 해결 완료 상태로 변경한다.
func (plan *ResponsePlan) ResolveIncident() {
	plan.Status = "resolved"
	plan.AddTimelineEntry("Incident resolved")
}

// PrintTimeline은 타임라인을 출력한다.
func (plan *ResponsePlan) PrintTimeline() {
	fmt.Printf("\n=== Incident Timeline: %s ===\n", plan.Incident.ID)
	for _, entry := range plan.Timeline {
		fmt.Printf("[%s] %s\n", entry.Time.Format("15:04:05"), entry.Message)
	}
	fmt.Println()
}

// 장애 대응 시나리오 예제

func scenario_DatabaseConnectionPoolExhausted() {
	fmt.Println("\n=== Scenario: Database Connection Pool Exhausted ===")

	manager := NewIncidentResponseManager()

	incident := &Incident{
		ID:              "INC-001",
		Title:           "Database Connection Pool Exhausted",
		Description:     "All database connections are in use, new requests are timing out",
		Severity:        Critical,
		StartTime:       time.Now(),
		Detection:       "Automated monitoring alert",
		ImpactedService: "Game Logic Server",
		ImpactedUsers:   5000,
	}

	plan := manager.CreateIncident(incident)

	// 대응 진행
	fmt.Printf("Incident: %s (Severity: %v)\n", incident.Title, incident.Severity)
	fmt.Printf("Impacted Users: %d\n", incident.ImpactedUsers)

	// 각 단계 실행
	for i := 0; i < 3; i++ {
		step := plan.Steps[i]
		fmt.Printf("\nExecuting Step %d: %s\n", step.Order, step.Action)
		fmt.Printf("Responsible: %s (Est. %d min)\n", step.Responsible, step.EstimatedTime)

		// 실제로는 여기서 대응 작업이 수행됨
		time.Sleep(1 * time.Second)
		plan.CompleteStep()
	}

	plan.AddTimelineEntry("Temporary fix applied: increased connection pool size")
	plan.AddTimelineEntry("Service recovery confirmed")
	plan.ResolveIncident()

	plan.PrintTimeline()
}

func scenario_MemoryLeak() {
	fmt.Println("\n=== Scenario: Memory Leak Detection ===")

	manager := NewIncidentResponseManager()

	incident := &Incident{
		ID:              "INC-002",
		Title:           "Memory Leak in Game Logic Server",
		Description:     "Server memory usage continuously increases without recovery",
		Severity:        High,
		StartTime:       time.Now(),
		Detection:       "Memory monitoring dashboard alert",
		ImpactedService: "Game Logic Server",
		ImpactedUsers:   1000,
	}

	plan := manager.CreateIncident(incident)

	fmt.Printf("Incident: %s (Severity: %v)\n", incident.Title, incident.Severity)

	// 대응
	plan.AddTimelineEntry("Memory profiling initiated with pprof")
	plan.AddTimelineEntry("Memory leak identified in session cleanup code")
	plan.AddTimelineEntry("Fix developed and tested in dev environment")
	plan.AddTimelineEntry("Hotfix deployed to staging")
	plan.AddTimelineEntry("Production deployment approved")
	plan.AddTimelineEntry("Memory usage stabilized after deployment")
	plan.ResolveIncident()

	plan.PrintTimeline()
}

func scenario_NetworkPartition() {
	fmt.Println("\n=== Scenario: Network Partition ===")

	manager := NewIncidentResponseManager()

	incident := &Incident{
		ID:              "INC-003",
		Title:           "Network Partition Between Game Server and Database",
		Description:     "Database is unreachable from game server cluster",
		Severity:        Critical,
		StartTime:       time.Now(),
		Detection:       "Database health check failure",
		ImpactedService: "All Services",
		ImpactedUsers:   10000,
	}

	plan := manager.CreateIncident(incident)

	fmt.Printf("Incident: %s (Severity: %v)\n", incident.Title, incident.Severity)

	// 대응
	plan.AddTimelineEntry("Network connectivity issue detected")
	plan.AddTimelineEntry("Network team engaged")
	plan.AddTimelineEntry("Failover to backup database initiated")
	plan.AddTimelineEntry("Services reconnected to backup database")
	plan.AddTimelineEntry("Network issue resolved by infrastructure team")
	plan.AddTimelineEntry("Failback to primary database completed")
	plan.ResolveIncident()

	plan.PrintTimeline()
}

func main() {
	scenario_DatabaseConnectionPoolExhausted()
	scenario_MemoryLeak()
	scenario_NetworkPartition()

	fmt.Println("\n=== Incident Response Best Practices ===")
	fmt.Println("1. Rapid Response: Start addressing within 5 minutes of detection")
	fmt.Println("2. Communication: Keep stakeholders informed every 15 minutes")
	fmt.Println("3. Focus: Restore service first, investigate root cause later")
	fmt.Println("4. Documentation: Record all actions and timeline")
	fmt.Println("5. Testing: Test fixes in lower environments before production")
	fmt.Println("6. Rollback Plan: Always have a way to quickly revert changes")
	fmt.Println("7. Post-Mortem: Review and learn from every incident")
}
```

### 운영 체크리스트

```markdown
# Game Server Operations Checklist

## Daily Tasks
- [ ] Review error logs and metrics dashboard
- [ ] Check database backup completion
- [ ] Verify all services are running normally
- [ ] Review player reported issues
- [ ] Check disk space on all servers

## Weekly Tasks
- [ ] Review performance trends
- [ ] Test backup and restore procedures
- [ ] Update runbooks based on new incidents
- [ ] Review and rotate API keys/credentials
- [ ] Capacity planning review

## Monthly Tasks
- [ ] Full database integrity check
- [ ] Security patching assessment
- [ ] Team training and documentation update
- [ ] Performance optimization review
- [ ] Disaster recovery drill

## Per-Deployment Tasks
- [ ] Pre-deployment checklist
  - [ ] All tests passing
  - [ ] Performance benchmarks acceptable
  - [ ] Database migration tested
  - [ ] Rollback plan documented
  - [ ] Monitoring and alerts ready
  
- [ ] Post-deployment verification
  - [ ] Health checks passing
  - [ ] Error rates normal
  - [ ] Performance metrics acceptable
  - [ ] Player reports no issues
  - [ ] Database replication healthy

## Incident Response
- [ ] 장애 감지 후 5분 내 대응 시작
- [ ] Slack/PagerDuty에 알림 전송
- [ ] 장애 대응 계획 시작
- [ ] 진행 상황을 15분마다 갱신
- [ ] 우회 방안(Workaround) 적용 고려
- [ ] 영구 해결책(Permanent Fix) 개발 및 테스트
- [ ] 프로덕션 배포 전 승인 획득
- [ ] 배포 후 모니터링 강화
- [ ] 장애 해결 확인
- [ ] 사후 분석 보고서 작성
```

이 장에서 다루는 운영 고려사항들은 게임 서버를 안정적으로 운영하기 위해 반드시 필요하다. 체계적인 로그 관리로 문제의 원인을 파악하고, 신속한 에러 리포팅으로 문제에 빠르게 대응하며, 안전한 백업과 복구 프로세스로 데이터를 보호할 수 있다. 또한 미리 준비된 핫픽스 전략과 장애 대응 계획을 통해 운영 중 발생하는 다양한 상황에 효과적으로 대처할 수 있다.   