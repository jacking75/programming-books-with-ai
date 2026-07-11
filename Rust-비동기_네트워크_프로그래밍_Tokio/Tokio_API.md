# Tokio 1.52 로 게임 서버 만들기 — 핵심 API 완전 정복

> **현재 최신 버전: Tokio 1.52.1 (2026년 4월 16일 릴리즈)**
> LTS 버전: 1.51.x (2027년 3월까지), MSRV: Rust 1.71+

---

## **0. 프로젝트 설정 및 Cargo.toml**
Tokio는 필요한 기능만 Feature Flag로 선택할 수 있는 구조입니다. 게임 서버에서는 일반적으로 아래처럼 `full`을 사용하거나, 세밀하게 조정합니다.

```toml
[dependencies]
# 개발/프로토타입 단계 — 모든 기능 활성화
tokio = { version = "1.52", features = ["full"] }

# 프로덕션 단계 — 필요한 것만 선택 (바이너리 크기 최적화)
tokio = { version = "1.52", features = [
    "rt-multi-thread",   # 멀티스레드 스케줄러
    "net",               # TCP/UDP 소켓
    "sync",              # 채널, Mutex, RwLock 등
    "time",              # sleep, interval, timeout
    "macros",            # #[tokio::main], tokio::select! 등
    "io-util",           # AsyncReadExt, AsyncWriteExt
    "signal",            # SIGINT, SIGTERM 처리
] }

# 고급 기능 — tracing, io_uring (unstable)
# .cargo/config.toml 에 아래 추가 필요:
# [build]
# rustflags = ["--cfg", "tokio_unstable"]
tokio = { version = "1.52", features = ["full", "tracing"] }
```

Feature Flag 각각의 의미를 한눈에 정리하면 다음과 같습니다.

| Feature | 제공 기능 | 게임 서버 필요성 |
|---|---|---|
| `rt-multi-thread` | 멀티스레드 Work-Stealing 스케줄러 | ✅ 필수 |
| `net` | TCP/UDP/Unix 소켓 | ✅ 필수 |
| `sync` | 채널, Mutex, RwLock, Semaphore | ✅ 필수 |
| `time` | sleep, interval, timeout | ✅ 필수 |
| `macros` | `#[tokio::main]`, `select!`, `join!` | ✅ 필수 |
| `io-util` | AsyncRead/Write 확장 메서드 | ✅ 필수 |
| `signal` | OS 시그널 처리 | ✅ 권장 |
| `tracing` | 비동기 디버깅/계측 (unstable) | ✅ 권장 |

---

## **1. Runtime — 비동기 세계의 심장**
Tokio 런타임은 모든 비동기 코드를 실행하는 실행 엔진입니다. 게임 서버는 수천 명의 플레이어를 동시에 처리해야 하므로, 런타임 선택과 설정이 성능에 직결됩니다.

**세 가지 런타임 타입:**

```rust
// ① 가장 간단한 방법 — 멀티스레드 런타임 자동 설정
#[tokio::main]
async fn main() {
    // CPU 코어 수만큼 스레드 자동 생성 (기본값)
    println!("게임 서버 시작!");
}

// ② 고급 설정 — Builder로 세밀하게 제어
use tokio::runtime::Builder;

fn main() {
    let runtime = Builder::new_multi_thread()
        .worker_threads(8)              // 워커 스레드 수 명시
        .max_blocking_threads(64)       // 블로킹 스레드 풀 최대 크기
        .thread_name("game-worker")     // 디버깅용 스레드 이름
        .thread_stack_size(3 * 1024 * 1024) // 스택 크기 3MB
        .enable_all()                   // 모든 드라이버 활성화
        .build()
        .unwrap();

    runtime.block_on(async_main());
}

async fn async_main() {
    println!("서버 실행 중");
}

// ③ 단일 스레드 런타임 — 싱글 게임 룸, 테스트용
#[tokio::main(flavor = "current_thread")]
async fn main() {
    // 하나의 스레드에서 모든 태스크 실행
    // Send 트레이트 불필요 → Rc, RefCell 사용 가능
}
```

**1.51에서 안정화된 `LocalRuntime`** (Tokio 1.51.0):

```rust
use tokio::runtime::Builder;

// !Send 타입을 다루는 로컬 런타임 — 게임 룸 단위 격리에 유용
fn main() {
    // LocalRuntime은 단일 스레드에서 !Send 태스크를 실행
    let local_rt = Builder::new_current_thread()
        .enable_all()
        .build_local(&Default::default()) // 1.51에서 stabilized
        .unwrap();

    local_rt.block_on(async {
        // Rc, RefCell 등 !Send 타입 자유롭게 사용 가능
        let game_state = std::rc::Rc::new(vec![1, 2, 3]);
        println!("로컬 게임 상태: {:?}", game_state);
    });
}
```

---


## **2. tokio::task — 태스크 관리의 핵심**
비동기 태스크는 OS 스레드보다 훨씬 가벼운 실행 단위입니다. Tokio는 수십만 개의 태스크를 단 몇 개의 스레드 위에서 효율적으로 스케줄링합니다. 게임 서버에서는 플레이어 한 명당 하나의 태스크를 할당하는 패턴이 매우 흔합니다.

### **2.1 tokio::spawn — 태스크 생성**

```rust
use tokio::task::JoinHandle;

async fn handle_player(player_id: u32) {
    println!("플레이어 {} 연결됨", player_id);
    // 소켓 I/O, 게임 로직 처리 ...
    tokio::time::sleep(tokio::time::Duration::from_secs(1)).await;
    println!("플레이어 {} 처리 완료", player_id);
}

#[tokio::main]
async fn main() {
    // spawn은 즉시 반환되고, 태스크는 백그라운드에서 실행됨
    let handle: JoinHandle<String> = tokio::spawn(async {
        // 이 클로저는 반드시 'static + Send 를 만족해야 함
        "작업 결과".to_string()
    });

    // 태스크가 끝날 때까지 대기하고 결과 수신
    match handle.await {
        Ok(result) => println!("결과: {}", result),
        Err(e) if e.is_panic() => eprintln!("태스크가 패닉!"),
        Err(e) if e.is_cancelled() => eprintln!("태스크가 취소됨"),
        Err(e) => eprintln!("기타 오류: {:?}", e),
    }

    // 여러 플레이어를 동시에 처리
    let mut handles = Vec::new();
    for player_id in 0..100 {
        let handle = tokio::spawn(handle_player(player_id));
        handles.push(handle);
    }

    // 모든 플레이어 처리 완료 대기
    for handle in handles {
        let _ = handle.await;
    }
}
```

### **2.2 JoinSet — 동적 태스크 집합 관리**
게임 서버에서 클라이언트 연결이 들어올 때마다 동적으로 태스크를 생성하고 관리하는 패턴에 이상적입니다.

```rust
use tokio::task::JoinSet;

#[tokio::main]
async fn main() {
    let mut set: JoinSet<(u32, String)> = JoinSet::new();

    // 게임 룸에 입장하는 플레이어들을 동적으로 추가
    for player_id in 0..10u32 {
        set.spawn(async move {
            // 각 플레이어의 게임 세션 처리
            tokio::time::sleep(tokio::time::Duration::from_millis(100 * player_id as u64)).await;
            (player_id, format!("플레이어 {} 세션 종료", player_id))
        });
    }

    // 완료된 태스크부터 순서대로 결과 수집 (완료 순서 보장 X)
    while let Some(result) = set.join_next().await {
        match result {
            Ok((id, msg)) => println!("[완료] ID={}: {}", id, msg),
            Err(e) => eprintln!("[오류] {:?}", e),
        }
    }

    // 1.49에서 추가된 Extend 구현으로 이터레이터 직접 주입 가능
    let mut set2: JoinSet<u32> = JoinSet::new();
    let futures = (0..5u32).map(|i| async move { i * 2 });
    set2.extend(futures); // Tokio 1.49+ 에서 사용 가능
}
```

### **2.3 spawn_blocking — CPU 집약 작업 분리**
비동기 런타임의 워커 스레드를 블로킹 작업이 점유하면 전체 서버가 멈추는 치명적인 문제가 발생합니다. 물리 연산, 경로 탐색(A*), 직렬화 같은 무거운 CPU 작업은 반드시 분리해야 합니다.

```rust
use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize)]
struct GameState {
    players: Vec<String>,
    tick: u64,
}

async fn process_game_tick(state: GameState) -> Vec<u8> {
    // 무거운 직렬화 작업을 블로킹 스레드풀로 오프로드
    tokio::task::spawn_blocking(move || {
        // 여기서는 블로킹 OK — 별도의 스레드 풀에서 실행됨
        let json = serde_json::to_vec(&state).unwrap();

        // CPU 집약적 게임 로직 (A* 경로 탐색, 물리 시뮬레이션 등)
        // std::thread::sleep, heavy computation 모두 여기서
        json
    })
    .await
    .unwrap()
}

async fn calculate_pathfinding(start: (i32, i32), end: (i32, i32)) -> Vec<(i32, i32)> {
    tokio::task::spawn_blocking(move || {
        // A* 알고리즘 — 블로킹 스레드에서 안전하게 실행
        expensive_astar(start, end)
    })
    .await
    .unwrap()
}

fn expensive_astar(start: (i32, i32), end: (i32, i32)) -> Vec<(i32, i32)> {
    // 실제 구현 생략
    vec![start, end]
}
```

### **2.4 tokio::task::yield_now — 협력적 멀티태스킹**

```rust
async fn long_computation() {
    for i in 0..1_000_000 {
        // 무거운 루프 중간에 다른 태스크에게 실행 기회 양보
        if i % 1000 == 0 {
            tokio::task::yield_now().await;
        }
        // ... 연산 ...
    }
}
```

---

## **3. tokio::sync — 태스크 간 통신과 동기화**
게임 서버의 핵심은 수많은 태스크들이 데이터를 안전하게 주고받는 것입니다. Tokio는 네 가지 채널 타입과 다양한 동기화 프리미티브를 제공합니다.

### **3.1 mpsc — 다대일(Many-to-One) 채널**
여러 플레이어 태스크가 하나의 게임 로직 태스크로 메시지를 보낼 때 사용합니다. 게임 서버에서 가장 많이 쓰이는 패턴입니다.

```rust
use tokio::sync::mpsc;

#[derive(Debug)]
enum PlayerAction {
    Move { player_id: u32, x: f32, y: f32 },
    Attack { attacker_id: u32, target_id: u32 },
    Chat { player_id: u32, message: String },
    Disconnect { player_id: u32 },
}

#[tokio::main]
async fn main() {
    // bounded: 버퍼 크기 제한 → 백프레셔(backpressure) 자동 처리
    let (tx, mut rx) = mpsc::channel::<PlayerAction>(1024);

    // unbounded: 버퍼 무제한 → 메모리 주의
    // let (tx, mut rx) = mpsc::unbounded_channel::<PlayerAction>();

    // 여러 플레이어 태스크가 동일한 sender의 clone을 사용
    for player_id in 0..10u32 {
        let tx_clone = tx.clone(); // Sender는 Clone 가능
        tokio::spawn(async move {
            // 소켓에서 읽은 액션을 게임 서버로 전송
            tx_clone
                .send(PlayerAction::Move {
                    player_id,
                    x: 1.0,
                    y: 2.0,
                })
                .await
                .expect("게임 서버가 종료됨");

            // send_timeout으로 타임아웃 설정 가능
            // tx_clone.send_timeout(action, Duration::from_secs(1)).await
        });
    }
    drop(tx); // 원본 sender drop → 모든 clone이 drop되면 채널 닫힘

    // 게임 서버 메인 루프 — 모든 플레이어 액션을 순서대로 처리
    while let Some(action) = rx.recv().await {
        match action {
            PlayerAction::Move { player_id, x, y } => {
                println!("플레이어 {} 이동: ({}, {})", player_id, x, y);
            }
            PlayerAction::Disconnect { player_id } => {
                println!("플레이어 {} 연결 끊김", player_id);
            }
            _ => {}
        }
    }
    println!("모든 플레이어 처리 완료");
}
```

### **3.2 broadcast — 일대다(One-to-Many) 채널**
서버에서 모든 플레이어에게 게임 이벤트를 동시에 브로드캐스트할 때 사용합니다.

```rust
use tokio::sync::broadcast;

#[derive(Debug, Clone)]
enum GameEvent {
    PlayerJoined { player_id: u32, name: String },
    PlayerMoved { player_id: u32, x: f32, y: f32 },
    GameStarted,
    GameEnded { winner_id: u32 },
}

#[tokio::main]
async fn main() {
    // 버퍼 크기: 뒤처진 수신자가 놓칠 수 있는 최대 메시지 수
    let (tx, _rx) = broadcast::channel::<GameEvent>(256);

    // 각 플레이어 연결마다 수신자 구독
    let mut handles = Vec::new();
    for player_id in 0..5u32 {
        let mut rx = tx.subscribe(); // 새로운 수신자 생성
        handles.push(tokio::spawn(async move {
            loop {
                match rx.recv().await {
                    Ok(event) => {
                        println!("[플레이어 {}] 이벤트 수신: {:?}", player_id, event);
                        if let GameEvent::GameEnded { .. } = event {
                            break;
                        }
                    }
                    Err(broadcast::error::RecvError::Lagged(n)) => {
                        // 느린 수신자가 n개 메시지를 놓침 — 중요한 경우 재연결 처리
                        eprintln!("[플레이어 {}] {}개 메시지 누락!", player_id, n);
                    }
                    Err(broadcast::error::RecvError::Closed) => break,
                }
            }
        }));
    }

    // 게임 이벤트 브로드캐스트
    tokio::time::sleep(tokio::time::Duration::from_millis(10)).await;
    tx.send(GameEvent::GameStarted).unwrap();
    tx.send(GameEvent::PlayerMoved { player_id: 1, x: 5.0, y: 3.0 }).unwrap();
    tx.send(GameEvent::GameEnded { winner_id: 1 }).unwrap();

    for h in handles { let _ = h.await; }
}
```

### **3.3 oneshot — 일회성 요청/응답 패턴**
단 한 번의 응답이 필요한 경우 (예: DB 쿼리 결과, RPC 응답)에 사용합니다.

```rust
use tokio::sync::oneshot;

#[derive(Debug)]
struct QueryRequest {
    player_id: u32,
    respond_to: oneshot::Sender<PlayerData>, // 응답 채널을 요청에 포함
}

#[derive(Debug)]
struct PlayerData {
    name: String,
    level: u32,
    score: u64,
}

// DB 서비스 태스크
async fn db_service(mut rx: tokio::sync::mpsc::Receiver<QueryRequest>) {
    while let Some(req) = rx.recv().await {
        // DB 쿼리 시뮬레이션
        let data = PlayerData {
            name: format!("Player_{}", req.player_id),
            level: 42,
            score: 9999,
        };
        // 결과를 요청자에게 직접 전송 — 에러 무시 (수신자가 이미 drop됐을 수 있음)
        let _ = req.respond_to.send(data);
    }
}

async fn get_player_data(
    db_tx: &tokio::sync::mpsc::Sender<QueryRequest>,
    player_id: u32,
) -> Result<PlayerData, String> {
    let (respond_to, rx) = oneshot::channel();

    db_tx
        .send(QueryRequest { player_id, respond_to })
        .await
        .map_err(|_| "DB 서비스 종료됨".to_string())?;

    rx.await.map_err(|_| "응답 없음".to_string())
}
```

### **3.4 watch — 최신 상태 공유 채널**
게임 설정, 서버 상태 등 "항상 최신 값"만 중요한 경우에 사용합니다. 중간 값이 누락되어도 괜찮은 상황에 적합합니다.

```rust
use tokio::sync::watch;

#[derive(Debug, Clone)]
struct ServerConfig {
    max_players: u32,
    tick_rate: u32,
    maintenance_mode: bool,
}

#[tokio::main]
async fn main() {
    let initial_config = ServerConfig {
        max_players: 100,
        tick_rate: 60,
        maintenance_mode: false,
    };

    let (config_tx, config_rx) = watch::channel(initial_config);

    // 여러 워커가 최신 설정을 공유
    for worker_id in 0..3u32 {
        let mut rx = config_rx.clone();
        tokio::spawn(async move {
            loop {
                // changed()는 값이 변경될 때까지 대기
                if rx.changed().await.is_err() {
                    break; // 송신자가 drop됨
                }
                let config = rx.borrow_and_update().clone();
                println!(
                    "[워커 {}] 설정 업데이트: tick_rate={}",
                    worker_id, config.tick_rate
                );
                if config.maintenance_mode {
                    println!("[워커 {}] 점검 모드 진입", worker_id);
                    break;
                }
            }
        });
    }

    // 관리자가 설정 변경
    tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
    config_tx.send(ServerConfig {
        max_players: 200,
        tick_rate: 120,
        maintenance_mode: false,
    }).unwrap();

    tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
    config_tx.send(ServerConfig {
        max_players: 0,
        tick_rate: 0,
        maintenance_mode: true,
    }).unwrap();

    tokio::time::sleep(tokio::time::Duration::from_millis(200)).await;
}
```

**채널 선택 가이드:**

| 채널 | 방향 | 특징 | 게임 서버 사용 사례 |
|---|---|---|---|
| `mpsc` | N → 1 | 버퍼링, 백프레셔 | 클라이언트 → 게임 로직 |
| `broadcast` | 1 → N | 모든 구독자에게 전달 | 서버 → 모든 플레이어 |
| `oneshot` | 1 → 1 (일회성) | 최소 오버헤드 | RPC, DB 쿼리 응답 |
| `watch` | 1 → N (최신 값) | 항상 최신 상태만 | 서버 설정, 게임 상태 공유 |

### **3.5 Mutex / RwLock — 공유 상태 보호**

```rust
use std::collections::HashMap;
use std::sync::Arc;
use tokio::sync::{Mutex, RwLock};

#[derive(Debug, Clone)]
struct Player {
    name: String,
    position: (f32, f32),
}

// ✅ 읽기가 많고 쓰기가 드문 경우 — RwLock 사용
// 예: 플레이어 목록 조회는 빈번하지만 입장/퇴장은 드문 경우
type PlayerRegistry = Arc<RwLock<HashMap<u32, Player>>>;

async fn get_all_players(registry: &PlayerRegistry) -> Vec<Player> {
    let guard = registry.read().await; // 여러 태스크가 동시에 읽기 가능
    guard.values().cloned().collect()
}

async fn add_player(registry: &PlayerRegistry, id: u32, player: Player) {
    let mut guard = registry.write().await; // 독점 쓰기 잠금
    guard.insert(id, player);
}

// ✅ 쓰기가 빈번한 경우 — Mutex 사용
// 또는 게임 점수처럼 원자적 업데이트가 필요한 경우
type Scoreboard = Arc<Mutex<HashMap<u32, u64>>>;

async fn update_score(scoreboard: &Scoreboard, player_id: u32, delta: u64) {
    let mut guard = scoreboard.lock().await;
    *guard.entry(player_id).or_insert(0) += delta;
    // guard가 이 블록을 벗어나면 자동으로 잠금 해제
}

// ⚠️ 주의: .await를 Mutex 잠금 상태에서 호출하면 데드락 위험!
async fn bad_example(mutex: Arc<Mutex<u32>>) {
    let guard = mutex.lock().await;
    // 절대 하지 말 것 — 잠금 상태에서 네트워크/파일 I/O await
    // some_io_operation().await; // ← 데드락 위험!
    drop(guard); // 명시적으로 먼저 해제 후 await 사용
}
```

### **3.6 Semaphore — 동시성 제한**

```rust
use std::sync::Arc;
use tokio::sync::Semaphore;

// 게임 서버에서 동시 접속 가능한 최대 플레이어 수 제한
const MAX_CONCURRENT_PLAYERS: usize = 1000;

#[tokio::main]
async fn main() {
    let semaphore = Arc::new(Semaphore::new(MAX_CONCURRENT_PLAYERS));

    for player_id in 0..1500u32 {
        let sem = semaphore.clone();
        tokio::spawn(async move {
            // 허가(permit)를 획득해야만 서비스 진입 가능
            let permit = match sem.try_acquire() {
                Ok(p) => p, // 즉시 허가
                Err(_) => {
                    eprintln!("서버 포화 — 플레이어 {} 접속 거부", player_id);
                    return;
                }
            };

            // 또는 대기하며 허가 획득
            // let permit = sem.acquire().await.unwrap();

            println!("플레이어 {} 접속 승인", player_id);
            tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
            // permit이 drop되면 자동으로 자원 반환
            drop(permit);
        });
    }

    tokio::time::sleep(tokio::time::Duration::from_secs(2)).await;
}
```

### **3.7 Notify — 이벤트 신호 전달**

```rust
use std::sync::Arc;
use tokio::sync::Notify;

// 게임 매치메이킹 — 조건이 충족될 때 대기 중인 태스크 깨우기
#[tokio::main]
async fn main() {
    let match_ready = Arc::new(Notify::new());

    // 대기 중인 플레이어들
    for player_id in 0..4u32 {
        let notify = match_ready.clone();
        tokio::spawn(async move {
            notify.notified().await; // 신호 대기
            println!("플레이어 {} 매치 시작!", player_id);
        });
    }

    tokio::time::sleep(tokio::time::Duration::from_millis(500)).await;
    match_ready.notify_waiters(); // 대기 중인 모든 태스크 깨우기
    // match_ready.notify_one(); // 하나만 깨우기

    tokio::time::sleep(tokio::time::Duration::from_millis(100)).await;
}
```

---

## **4. tokio::net — 네트워크 I/O**
게임 서버의 핵심인 TCP/UDP 통신을 담당합니다.

### **4.1 TcpListener / TcpStream — TCP 서버**

```rust
use tokio::io::{AsyncReadExt, AsyncWriteExt, BufReader};
use tokio::net::{TcpListener, TcpStream};

// 게임 패킷 구조 (길이-본문 프로토콜)
async fn handle_connection(stream: TcpStream, player_id: u32) {
    // 소켓을 읽기/쓰기로 분리 — 동시에 읽고 쓸 수 있게 됨
    let (read_half, mut write_half) = stream.into_split();
    let mut reader = BufReader::new(read_half);

    let mut len_buf = [0u8; 4]; // 패킷 길이 헤더 (4바이트)

    loop {
        // 길이 헤더 읽기
        match reader.read_exact(&mut len_buf).await {
            Ok(_) => {}
            Err(e) if e.kind() == std::io::ErrorKind::UnexpectedEof => {
                println!("플레이어 {} 연결 종료", player_id);
                break;
            }
            Err(e) => {
                eprintln!("읽기 오류: {:?}", e);
                break;
            }
        }

        let packet_len = u32::from_be_bytes(len_buf) as usize;
        if packet_len > 65536 {
            eprintln!("패킷 크기 초과 — 연결 종료");
            break;
        }

        // 본문 읽기
        let mut body = vec![0u8; packet_len];
        if reader.read_exact(&mut body).await.is_err() {
            break;
        }

        // 에코 응답 (길이 헤더 + 본문)
        let response_len = (body.len() as u32).to_be_bytes();
        write_half.write_all(&response_len).await.unwrap();
        write_half.write_all(&body).await.unwrap();
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let listener = TcpListener::bind("0.0.0.0:9999").await?;
    println!("게임 서버 리스닝: 0.0.0.0:9999");

    // TCP 최적화 옵션 설정
    // listener.set_ttl(128)?; // TTL 설정

    let mut player_counter = 0u32;

    loop {
        // 새로운 클라이언트 연결 수락
        let (stream, addr) = listener.accept().await?;
        player_counter += 1;
        let player_id = player_counter;

        println!("새 플레이어 #{} 연결: {}", player_id, addr);

        // TCP 소켓 옵션 최적화
        stream.set_nodelay(true)?; // Nagle 알고리즘 비활성화 → 레이턴시 감소
        // stream.set_linger(None)?; // 소켓 종료 시 즉시 닫기

        // 각 플레이어를 별도 태스크에서 처리
        tokio::spawn(handle_connection(stream, player_id));
    }
}
```

### **4.2 UdpSocket — UDP (게임 실시간 데이터)**
빠른 위치 업데이트, 음성 채팅 등 약간의 패킷 손실이 허용되는 실시간 데이터에 UDP를 사용합니다.

```rust
use tokio::net::UdpSocket;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let socket = UdpSocket::bind("0.0.0.0:9998").await?;
    println!("UDP 서버 시작: 0.0.0.0:9998");

    let mut buf = vec![0u8; 1500]; // MTU 크기

    loop {
        let (n, peer_addr) = socket.recv_from(&mut buf).await?;
        let packet = buf[..n].to_vec();

        println!(
            "UDP 패킷 수신: {} 바이트 from {}",
            n, peer_addr
        );

        // 위치 정보 브로드캐스트 시뮬레이션
        // 실제로는 다른 플레이어 주소 목록을 관리하며 전송
        socket.send_to(&packet, peer_addr).await?;
    }
}
```

### **4.3 소켓 분리 (split) — 동시 읽기/쓰기**

```rust
use std::sync::Arc;
use tokio::net::TcpStream;
use tokio::sync::mpsc;
use tokio::io::{AsyncReadExt, AsyncWriteExt};

async fn bidirectional_handler(stream: TcpStream) {
    let (mut read_half, mut write_half) = stream.into_split();
    let (tx, mut rx) = mpsc::channel::<Vec<u8>>(128);

    // 읽기 태스크 — 소켓에서 데이터 수신
    let read_task = tokio::spawn(async move {
        let mut buf = vec![0u8; 4096];
        loop {
            match read_half.read(&mut buf).await {
                Ok(0) => break,
                Ok(n) => {
                    println!("수신: {} 바이트", n);
                    // 게임 로직에 전달...
                }
                Err(_) => break,
            }
        }
    });

    // 쓰기 태스크 — 채널에서 데이터를 받아 소켓으로 전송
    let write_task = tokio::spawn(async move {
        while let Some(data) = rx.recv().await {
            if write_half.write_all(&data).await.is_err() {
                break;
            }
        }
    });

    // 어느 하나라도 종료되면 함께 종료
    tokio::select! {
        _ = read_task => {},
        _ = write_task => {},
    }
}
```

---

## **5. tokio::time — 시간 관련 유틸리티**
게임 서버의 게임 루프, 타임아웃, 쿨다운 등 모든 시간 관련 기능을 담당합니다.

### **5.1 sleep — 비동기 대기**

```rust
use tokio::time::{sleep, Duration};

async fn cooldown_example() {
    println!("스킬 사용!");
    sleep(Duration::from_secs(5)).await; // 5초 쿨다운
    println!("스킬 재사용 가능!");
}

// ⚠️ 절대 사용 금지 — std::thread::sleep은 비동기 런타임을 블로킹함
async fn bad_sleep() {
    std::thread::sleep(std::time::Duration::from_secs(1)); // ← 매우 위험!
}
```

### **5.2 interval — 주기적 실행 (게임 루프의 핵심)**

```rust
use tokio::time::{interval, interval_at, Duration, Instant, MissedTickBehavior};

// 서버 게임 루프 — 초당 60회 틱
async fn game_loop() {
    let tick_duration = Duration::from_millis(1000 / 60); // ~16.67ms
    let mut ticker = interval(tick_duration);

    // 틱을 놓쳤을 때의 동작 설정
    ticker.set_missed_tick_behavior(MissedTickBehavior::Skip);
    // Burst: 빠르게 따라잡기 (기본값)
    // Skip: 놓친 틱 건너뛰기 ← 게임 루프에 적합
    // Delay: 다음 틱을 현재 기준으로 재설정

    let mut tick_count = 0u64;

    loop {
        ticker.tick().await; // 다음 틱까지 대기
        tick_count += 1;

        // 게임 상태 업데이트
        update_physics();
        process_player_inputs();
        broadcast_state_to_clients();

        if tick_count % 60 == 0 {
            println!("1초 경과, 총 {} 틱 처리", tick_count);
        }
    }
}

fn update_physics() { /* 물리 업데이트 */ }
fn process_player_inputs() { /* 입력 처리 */ }
fn broadcast_state_to_clients() { /* 상태 브로드캐스트 */ }
```

### **5.3 timeout — 시간 초과 처리**

```rust
use tokio::time::{timeout, Duration};

async fn wait_for_player_action() -> Option<String> {
    // 실제로는 채널에서 수신
    tokio::time::sleep(Duration::from_secs(10)).await;
    Some("플레이어 액션".to_string())
}

async fn game_turn_handler(player_id: u32) {
    println!("플레이어 {}의 턴 — 30초 제한", player_id);

    match timeout(Duration::from_secs(30), wait_for_player_action()).await {
        Ok(Some(action)) => {
            println!("플레이어 {} 액션: {}", player_id, action);
        }
        Ok(None) => {
            println!("플레이어 {} 연결 끊김", player_id);
        }
        Err(_elapsed) => {
            // timeout 반환 타입은 Result<T, Elapsed>
            println!("플레이어 {} 시간 초과! 자동 패스", player_id);
        }
    }
}
```

### **5.4 timeout_at — 절대 시각 기반 타임아웃**

```rust
use tokio::time::{timeout_at, Instant, Duration};

async fn match_with_deadline() {
    // 매치 종료 절대 시각 설정
    let match_end = Instant::now() + Duration::from_secs(300); // 5분 후

    match timeout_at(match_end, run_match()).await {
        Ok(_) => println!("매치 정상 종료"),
        Err(_) => println!("제한 시간 초과로 매치 종료"),
    }
}

async fn run_match() {
    loop {
        tokio::time::sleep(Duration::from_secs(1)).await;
        // 매치 진행...
    }
}
```

---

## **6. tokio::select! — 여러 비동기 분기 동시 대기**
`select!`는 게임 서버에서 가장 강력하고 핵심적인 매크로 중 하나입니다. 여러 비동기 작업 중 먼저 완료되는 것에 반응합니다.

```rust
use tokio::sync::mpsc;
use tokio::time::{sleep, Duration};

#[derive(Debug)]
enum ClientMessage {
    Ping,
    GameAction(String),
}

async fn handle_player_session(
    player_id: u32,
    mut action_rx: mpsc::Receiver<ClientMessage>,
    mut shutdown_rx: mpsc::Receiver<()>,
) {
    let mut heartbeat = tokio::time::interval(Duration::from_secs(30));
    let mut idle_timeout = sleep(Duration::from_secs(120));
    tokio::pin!(idle_timeout); // Future를 스택에 고정

    loop {
        tokio::select! {
            // 분기 1: 클라이언트 메시지 수신
            Some(msg) = action_rx.recv() => {
                match msg {
                    ClientMessage::Ping => {
                        println!("[{}] Ping 수신", player_id);
                    }
                    ClientMessage::GameAction(action) => {
                        println!("[{}] 액션: {}", player_id, action);
                        // 유휴 타이머 리셋
                        idle_timeout.as_mut().reset(
                            tokio::time::Instant::now() + Duration::from_secs(120)
                        );
                    }
                }
            }

            // 분기 2: 주기적 하트비트
            _ = heartbeat.tick() => {
                println!("[{}] 하트비트 전송", player_id);
                // 클라이언트에 keep-alive 전송...
            }

            // 분기 3: 유휴 타임아웃
            _ = &mut idle_timeout => {
                println!("[{}] 유휴 타임아웃 — 연결 종료", player_id);
                break;
            }

            // 분기 4: 서버 셧다운 신호
            _ = shutdown_rx.recv() => {
                println!("[{}] 서버 셧다운 — 세션 종료", player_id);
                break;
            }

            // else 분기: 모든 채널이 닫힌 경우
            else => {
                println!("[{}] 모든 채널 종료", player_id);
                break;
            }
        }
    }
}
```

**`select!`의 `biased` 모드 — 우선순위 처리:**

```rust
async fn priority_handler(
    mut high_priority_rx: mpsc::Receiver<String>,
    mut low_priority_rx: mpsc::Receiver<String>,
) {
    loop {
        // biased: 위에서 아래 순서로 우선순위 처리
        // (기본값은 공정한 랜덤 선택)
        tokio::select! {
            biased;

            Some(msg) = high_priority_rx.recv() => {
                println!("⚡ 고우선순위: {}", msg); // 항상 먼저 처리
            }
            Some(msg) = low_priority_rx.recv() => {
                println!("📦 저우선순위: {}", msg);
            }
            else => break,
        }
    }
}
```

---

## **7. tokio::join! / try_join! — 병렬 작업 조합**
여러 비동기 작업을 동시에 실행하고 모두 완료될 때까지 기다립니다.

```rust
use tokio::join;

async fn load_player_profile(player_id: u32) -> String {
    tokio::time::sleep(tokio::time::Duration::from_millis(50)).await;
    format!("프로필_{}", player_id)
}

async fn load_player_inventory(player_id: u32) -> Vec<String> {
    tokio::time::sleep(tokio::time::Duration::from_millis(30)).await;
    vec![format!("아이템_{}", player_id)]
}

async fn load_player_achievements(player_id: u32) -> u32 {
    tokio::time::sleep(tokio::time::Duration::from_millis(40)).await;
    player_id * 10
}

async fn load_player_data(player_id: u32) {
    // 세 작업을 순차가 아닌 병렬로 실행 — 총 시간: max(50,30,40)=50ms
    let (profile, inventory, achievements) = join!(
        load_player_profile(player_id),
        load_player_inventory(player_id),
        load_player_achievements(player_id)
    );

    println!("프로필: {}", profile);
    println!("인벤토리: {:?}", inventory);
    println!("업적: {}", achievements);
}

// 오류 처리가 필요한 경우 try_join! 사용
use tokio::try_join;

async fn fallible_load(player_id: u32) -> Result<String, String> {
    if player_id == 0 {
        return Err("잘못된 플레이어 ID".to_string());
    }
    Ok(format!("데이터_{}", player_id))
}

async fn safe_load(player_id: u32) -> Result<(), String> {
    // 하나라도 Err이면 즉시 반환
    let (data1, data2) = try_join!(
        fallible_load(player_id),
        fallible_load(player_id + 1)
    )?;
    println!("{}, {}", data1, data2);
    Ok(())
}
```

---

## **8. tokio::signal — OS 시그널 처리 (우아한 종료)**
프로덕션 서버에서 `SIGINT`(Ctrl+C), `SIGTERM`을 처리하여 플레이어 데이터를 안전하게 저장하고 종료하는 것은 매우 중요합니다.

```rust
use tokio::signal;

async fn graceful_shutdown_handler() {
    // Unix 시그널 처리
    #[cfg(unix)]
    {
        use signal::unix::{signal, SignalKind};

        let mut sigterm = signal(SignalKind::terminate()).unwrap();
        let mut sigint = signal(SignalKind::interrupt()).unwrap();

        tokio::select! {
            _ = sigterm.recv() => println!("SIGTERM 수신 — 서버 종료 시작"),
            _ = sigint.recv()  => println!("SIGINT 수신 — 서버 종료 시작"),
        }
    }

    // 플랫폼 공통 (Windows 포함)
    // signal::ctrl_c().await.unwrap();
    // println!("Ctrl+C 수신");
}

#[tokio::main]
async fn main() {
    let (shutdown_tx, mut shutdown_rx) = tokio::sync::broadcast::channel::<()>(1);

    // 시그널 핸들러 태스크
    let tx_clone = shutdown_tx.clone();
    tokio::spawn(async move {
        graceful_shutdown_handler().await;
        println!("셧다운 신호를 모든 태스크에 전파...");
        let _ = tx_clone.send(());
    });

    // 메인 서버 루프
    tokio::select! {
        _ = run_server() => {}
        _ = shutdown_rx.recv() => {
            println!("서버 종료 중...");
            // 저장, 정리 작업 수행
            save_all_player_data().await;
            println!("서버 정상 종료 완료");
        }
    }
}

async fn run_server() {
    loop {
        tokio::time::sleep(tokio::time::Duration::from_secs(1)).await;
    }
}

async fn save_all_player_data() {
    println!("플레이어 데이터 저장 중...");
    tokio::time::sleep(tokio::time::Duration::from_millis(500)).await;
    println!("저장 완료");
}
```

---

## **9. tokio::io — 비동기 I/O 트레이트와 유틸리티**

```rust
use tokio::io::{AsyncBufReadExt, AsyncReadExt, AsyncWriteExt, BufReader, BufWriter};
use tokio::net::TcpStream;

async fn buffered_io_example(stream: TcpStream) {
    let (read_half, write_half) = stream.into_split();

    // 버퍼링된 읽기 — 시스템 콜 횟수 감소
    let mut reader = BufReader::new(read_half);
    // 버퍼링된 쓰기 — 작은 패킷을 묶어서 전송
    let mut writer = BufWriter::new(write_half);

    // 줄 단위 읽기 (텍스트 프로토콜)
    let mut line = String::new();
    loop {
        line.clear();
        match reader.read_line(&mut line).await {
            Ok(0) => break, // EOF
            Ok(_) => {
                let response = format!("ECHO: {}", line.trim());
                writer.write_all(response.as_bytes()).await.unwrap();
                writer.write_all(b"\n").await.unwrap();
                writer.flush().await.unwrap(); // 버퍼 즉시 전송
            }
            Err(_) => break,
        }
    }
}

// copy — 두 스트림 간 데이터 복사 (프록시 서버 등에 유용)
async fn proxy_example(client: TcpStream, server: TcpStream) {
    let (mut client_read, mut client_write) = client.into_split();
    let (mut server_read, mut server_write) = server.into_split();

    let client_to_server = tokio::io::copy(&mut client_read, &mut server_write);
    let server_to_client = tokio::io::copy(&mut server_read, &mut client_write);

    let _ = tokio::join!(client_to_server, server_to_client);
}
```

---

## **10. 고급 패턴 — Actor 모델로 게임 서버 구조화**
대규모 게임 서버에서는 Actor 패턴을 사용하여 각 게임 엔티티(플레이어, 룸, 아이템)를 독립적인 태스크로 관리합니다.

```rust
use std::collections::HashMap;
use tokio::sync::{mpsc, oneshot};

// --- Actor 메시지 정의 ---
#[derive(Debug)]
enum RoomMessage {
    // 요청 메시지 (oneshot으로 응답)
    GetPlayerCount { respond_to: oneshot::Sender<usize> },
    GetPlayerList { respond_to: oneshot::Sender<Vec<String>> },
    // 명령 메시지 (응답 불필요)
    PlayerJoin { player_id: u32, name: String },
    PlayerLeave { player_id: u32 },
    BroadcastMessage { sender_id: u32, text: String },
}

// --- Actor 구조체 ---
struct GameRoomActor {
    room_id: u32,
    players: HashMap<u32, String>,
    receiver: mpsc::Receiver<RoomMessage>,
}

impl GameRoomActor {
    fn new(room_id: u32, receiver: mpsc::Receiver<RoomMessage>) -> Self {
        GameRoomActor {
            room_id,
            players: HashMap::new(),
            receiver,
        }
    }

    async fn run(mut self) {
        println!("[룸 {}] Actor 시작", self.room_id);
        while let Some(msg) = self.receiver.recv().await {
            self.handle_message(msg).await;
        }
        println!("[룸 {}] Actor 종료", self.room_id);
    }

    async fn handle_message(&mut self, msg: RoomMessage) {
        match msg {
            RoomMessage::PlayerJoin { player_id, name } => {
                println!("[룸 {}] {} 입장", self.room_id, name);
                self.players.insert(player_id, name);
            }
            RoomMessage::PlayerLeave { player_id } => {
                if let Some(name) = self.players.remove(&player_id) {
                    println!("[룸 {}] {} 퇴장", self.room_id, name);
                }
            }
            RoomMessage::GetPlayerCount { respond_to } => {
                let _ = respond_to.send(self.players.len());
            }
            RoomMessage::GetPlayerList { respond_to } => {
                let list = self.players.values().cloned().collect();
                let _ = respond_to.send(list);
            }
            RoomMessage::BroadcastMessage { sender_id, text } => {
                let sender_name = self.players.get(&sender_id)
                    .cloned()
                    .unwrap_or_else(|| "Unknown".to_string());
                println!("[룸 {}] {}: {}", self.room_id, sender_name, text);
            }
        }
    }
}

// --- Actor 핸들 (외부에서 Actor와 통신하는 인터페이스) ---
#[derive(Clone)]
struct GameRoomHandle {
    sender: mpsc::Sender<RoomMessage>,
}

impl GameRoomHandle {
    fn new(room_id: u32) -> Self {
        let (sender, receiver) = mpsc::channel(256);
        let actor = GameRoomActor::new(room_id, receiver);
        tokio::spawn(actor.run()); // Actor를 백그라운드 태스크로 실행
        GameRoomHandle { sender }
    }

    async fn join(&self, player_id: u32, name: String) {
        let _ = self.sender.send(RoomMessage::PlayerJoin { player_id, name }).await;
    }

    async fn leave(&self, player_id: u32) {
        let _ = self.sender.send(RoomMessage::PlayerLeave { player_id }).await;
    }

    async fn get_player_count(&self) -> usize {
        let (tx, rx) = oneshot::channel();
        let _ = self.sender.send(RoomMessage::GetPlayerCount { respond_to: tx }).await;
        rx.await.unwrap_or(0)
    }

    async fn broadcast(&self, sender_id: u32, text: String) {
        let _ = self.sender.send(RoomMessage::BroadcastMessage { sender_id, text }).await;
    }
}

// --- 사용 예시 ---
#[tokio::main]
async fn main() {
    let room = GameRoomHandle::new(1);

    room.join(101, "AlphaPlayer".to_string()).await;
    room.join(102, "BetaPlayer".to_string()).await;
    room.join(103, "GammaPlayer".to_string()).await;

    room.broadcast(101, "안녕하세요!".to_string()).await;

    tokio::time::sleep(tokio::time::Duration::from_millis(10)).await;

    let count = room.get_player_count().await;
    println!("현재 플레이어 수: {}", count);

    room.leave(102).await;

    tokio::time::sleep(tokio::time::Duration::from_millis(10)).await;
    let count = room.get_player_count().await;
    println!("퇴장 후 플레이어 수: {}", count);
}
```

---

## **11. 성능 최적화 및 고급 팁**

### **핵심 최적화 원칙들:**
CPU 바운드 작업과 I/O 바운드 작업을 명확히 구분하는 것이 첫 번째입니다. Tokio 워커 스레드(기본: CPU 코어 수)는 I/O 대기가 주인 비동기 태스크를 위한 것이고, CPU 집약 작업은 반드시 `spawn_blocking`이나 `rayon`으로 분리해야 합니다.

```rust
// ✅ 올바른 구조: I/O와 CPU 작업 분리
async fn process_game_frame(raw_data: Vec<u8>) -> Vec<u8> {
    // 1단계: I/O (네트워크 수신) — 비동기 태스크에서 처리
    // raw_data는 이미 수신됨

    // 2단계: CPU 작업 — 블로킹 스레드 풀로 오프로드
    let processed = tokio::task::spawn_blocking(move || {
        // 무거운 패킷 파싱, 암호화 해제, 물리 연산 등
        heavy_computation(raw_data)
    })
    .await
    .unwrap();

    // 3단계: I/O (네트워크 전송) — 다시 비동기 태스크로
    processed
}

fn heavy_computation(data: Vec<u8>) -> Vec<u8> {
    data // 실제 구현 생략
}
```

`Arc<Mutex<T>>`보다는 채널 기반 메시지 패싱을 우선하는 것이 두 번째 핵심 원칙입니다. 잠금 경쟁(lock contention)은 비동기 서버에서 가장 흔한 성능 병목이기 때문입니다. 읽기가 빈번하다면 `Mutex` 대신 `RwLock`을, 단순 카운터라면 `std::sync::atomic`을 사용하세요.

```rust
use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::Arc;

// ✅ 원자 타입 — 잠금 없이 카운터 관리
let connected_players = Arc::new(AtomicU64::new(0));
let counter = connected_players.clone();

tokio::spawn(async move {
    counter.fetch_add(1, Ordering::Relaxed);
    // 세션 처리...
    counter.fetch_sub(1, Ordering::Relaxed);
});
```

`select!` 내에서 무거운 연산을 수행하지 않는 것도 중요합니다. `select!` 분기는 가능한 빨리 끝내고, 무거운 작업은 별도 태스크로 `spawn`해야 합니다.

```rust
// ⚠️ 잘못된 예
tokio::select! {
    msg = rx.recv() => {
        heavy_computation(); // ← select! 안에서 무거운 작업 금지
    }
}

// ✅ 올바른 예
tokio::select! {
    msg = rx.recv() => {
        if let Some(m) = msg {
            tokio::spawn(async move {
                heavy_computation(); // 별도 태스크로 분리
            });
        }
    }
}
```

### **취소 안전성(Cancellation Safety) 주의:**
`select!`에서 분기가 선택되지 않으면 나머지 Future는 취소됩니다. 이때 데이터 손실이 발생하지 않도록 취소 안전한 API를 사용해야 합니다.

```rust
// ✅ mpsc::Receiver::recv() — 취소 안전 (메시지는 큐에 남음)
// ✅ tokio::time::sleep() — 취소 안전
// ✅ TcpListener::accept() — 취소 안전
// ⚠️ AsyncWriteExt::write_all() — 부분 쓰기 후 취소 시 데이터 손실 가능
// ⚠️ AsyncReadExt::read_exact() — 부분 읽기 후 취소 시 데이터 손실 가능
```

---

## **12. 전체 구조 요약**
게임 서버를 설계할 때 Tokio API를 계층별로 활용하면 다음과 같은 구조가 됩니다.

```
┌─────────────────────────────────────────────────────────┐
│                    게임 서버 아키텍처                     │
├─────────────────────────────────────────────────────────┤
│  tokio::net          │  TcpListener, UdpSocket          │  ← 네트워크 계층
│  (클라이언트 연결)    │  TcpStream.into_split()          │
├─────────────────────────────────────────────────────────┤
│  tokio::spawn        │  플레이어별 독립 태스크            │  ← 태스크 계층
│  JoinSet             │  동적 태스크 집합 관리             │
├─────────────────────────────────────────────────────────┤
│  tokio::sync         │  mpsc: 플레이어 → 게임 로직       │  ← 통신 계층
│  (채널)              │  broadcast: 서버 → 모든 플레이어  │
│                      │  oneshot: RPC 요청/응답           │
│                      │  watch: 설정/상태 공유            │
├─────────────────────────────────────────────────────────┤
│  tokio::sync         │  Mutex: 공유 상태 보호            │  ← 동기화 계층
│  (동기화)            │  RwLock: 읽기 최적화              │
│                      │  Semaphore: 동시성 제한           │
├─────────────────────────────────────────────────────────┤
│  tokio::time         │  interval: 게임 루프 (60Hz)      │  ← 시간 계층
│                      │  timeout: 턴 제한 시간            │
│                      │  sleep: 쿨다운, 지연              │
├─────────────────────────────────────────────────────────┤
│  tokio::select!      │  이벤트 멀티플렉싱                │  ← 제어 흐름
│  tokio::join!        │  병렬 데이터 로딩                 │
├─────────────────────────────────────────────────────────┤
│  spawn_blocking      │  물리 연산, 경로 탐색, 직렬화     │  ← CPU 분리
│  LocalRuntime(1.51)  │  !Send 타입 처리 (룸 격리)        │
└─────────────────────────────────────────────────────────┘
```

Tokio는 단순한 비동기 런타임을 넘어, 게임 서버를 구성하는 거의 모든 요소를 안전하고 고성능으로 처리할 수 있는 완전한 생태계입니다. 초보자라면 `spawn` → `mpsc 채널` → `select!` 순서로 익히고, 고급 사용자라면 Actor 패턴과 취소 안전성, `spawn_blocking` 분리를 중심으로 아키텍처를 설계하는 것을 추천합니다.

  

# Tokio 게임 서버 — 심화 편: 꼭 알아야 할 추가 지식

> 앞에서 다루지 않은 **6가지 핵심 영역**을 심층적으로 설명합니다.

---

## **1. tokio-util — Tokio의 공식 확장 크레이트 (패킷 프레이밍)**
게임 서버에서 가장 실수하기 쉬운 부분이 바로 **TCP 스트림 파싱**입니다. TCP는 스트림 기반 프로토콜이라 "패킷 경계"가 없습니다. 즉, `read()`를 한 번 호출한다고 하나의 완전한 게임 패킷이 오는 것이 **전혀 보장되지 않습니다.** `tokio-util`의 `Codec` 시스템은 이 문제를 우아하게 해결합니다.

```toml
[dependencies]
tokio-util = { version = "0.7", features = ["codec"] }
bytes = "1"
futures = "0.3"
```

### **1.1 커스텀 Codec 구현 — 게임 바이너리 프로토콜**
실제 게임 패킷 구조(매직 헤더 + 커맨드 ID + 페이로드)를 직접 구현해 봅니다.

```rust
use bytes::{Buf, BufMut, BytesMut};
use tokio_util::codec::{Decoder, Encoder};

// 게임 패킷 구조:
// [매직 2바이트][커맨드 2바이트][길이 4바이트][페이로드 N바이트]
const MAGIC: u16 = 0xCAFE;
const MAX_PACKET_SIZE: usize = 64 * 1024; // 64KB 제한

#[derive(Debug, Clone)]
pub struct GamePacket {
    pub command: u16,
    pub payload: Vec<u8>,
}

pub struct GameCodec;

// ─── Decoder: 바이트 스트림 → GamePacket ───────────────────
impl Decoder for GameCodec {
    type Item = GamePacket;
    type Error = std::io::Error;

    fn decode(&mut self, src: &mut BytesMut) -> Result<Option<Self::Item>, Self::Error> {
        // 헤더 최소 크기(8바이트) 미만이면 더 기다림
        if src.len() < 8 {
            return Ok(None);
        }

        // 매직 넘버 검증 (슬라이스로 피킹 — 버퍼 소비 안 함)
        let magic = u16::from_be_bytes([src[0], src[1]]);
        if magic != MAGIC {
            return Err(std::io::Error::new(
                std::io::ErrorKind::InvalidData,
                format!("잘못된 매직 넘버: 0x{:04X}", magic),
            ));
        }

        // 페이로드 길이 파악
        let payload_len = u32::from_be_bytes([src[4], src[5], src[6], src[7]]) as usize;

        // DoS 방어: 비정상적으로 큰 패킷 거부
        if payload_len > MAX_PACKET_SIZE {
            return Err(std::io::Error::new(
                std::io::ErrorKind::InvalidData,
                format!("패킷 크기 초과: {} bytes", payload_len),
            ));
        }

        // 전체 패킷이 아직 안 왔으면 대기
        // → reserve()로 미리 공간 확보해 재할당 최소화
        let total_len = 8 + payload_len;
        if src.len() < total_len {
            src.reserve(total_len - src.len());
            return Ok(None);
        }

        // 완전한 패킷 수신! 버퍼에서 소비
        let command = u16::from_be_bytes([src[2], src[3]]);
        src.advance(8); // 헤더 소비
        let payload = src.split_to(payload_len).to_vec();

        Ok(Some(GamePacket { command, payload }))
    }
}

// ─── Encoder: GamePacket → 바이트 스트림 ───────────────────
impl Encoder<GamePacket> for GameCodec {
    type Error = std::io::Error;

    fn encode(&mut self, packet: GamePacket, dst: &mut BytesMut) -> Result<(), Self::Error> {
        let payload_len = packet.payload.len();
        if payload_len > MAX_PACKET_SIZE {
            return Err(std::io::Error::new(
                std::io::ErrorKind::InvalidData,
                "전송 패킷 크기 초과",
            ));
        }

        // 필요한 공간 한 번에 예약 → 재할당 방지
        dst.reserve(8 + payload_len);

        dst.put_u16(MAGIC);
        dst.put_u16(packet.command);
        dst.put_u32(payload_len as u32);
        dst.put_slice(&packet.payload);

        Ok(())
    }
}
```

### **1.2 Framed — Codec을 소켓에 연결**

```rust
use futures::{SinkExt, StreamExt};
use tokio::net::{TcpListener, TcpStream};
use tokio_util::codec::Framed;

async fn handle_framed_connection(stream: TcpStream, player_id: u32) {
    // TcpStream + GameCodec = 자동으로 패킷 경계를 처리하는 스트림/싱크
    let mut framed = Framed::new(stream, GameCodec);

    while let Some(result) = framed.next().await {
        match result {
            Ok(packet) => {
                println!(
                    "[플레이어 {}] 패킷 수신: cmd=0x{:04X}, len={}",
                    player_id,
                    packet.command,
                    packet.payload.len()
                );

                // 응답 패킷 전송 — 내부적으로 Encoder 호출
                let response = GamePacket {
                    command: packet.command | 0x8000, // 응답 플래그
                    payload: b"OK".to_vec(),
                };
                if framed.send(response).await.is_err() {
                    break;
                }
            }
            Err(e) => {
                eprintln!("[플레이어 {}] 프로토콜 오류: {}", player_id, e);
                break; // 잘못된 패킷 → 연결 종료
            }
        }
    }
    println!("[플레이어 {}] 연결 종료", player_id);
}

// ─── 내장 Codec: LengthDelimitedCodec ───────────────────────
// 간단한 경우 직접 구현 없이 바로 사용 가능
use tokio_util::codec::LengthDelimitedCodec;

async fn simple_length_prefixed(stream: TcpStream) {
    let mut framed = LengthDelimitedCodec::builder()
        .length_field_length(4)          // 길이 헤더: 4바이트
        .max_frame_length(MAX_PACKET_SIZE)
        .new_framed(stream);

    while let Some(Ok(frame)) = framed.next().await {
        // frame은 BytesMut — 길이 헤더가 제거된 순수 페이로드
        println!("프레임 수신: {} bytes", frame.len());
        // 에코 응답
        let _ = framed.send(frame.freeze()).await;
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let listener = TcpListener::bind("0.0.0.0:9999").await?;

    loop {
        let (stream, addr) = listener.accept().await?;
        stream.set_nodelay(true)?;
        let player_id = addr.port() as u32; // 임시 ID

        tokio::spawn(handle_framed_connection(stream, player_id));
    }
}
```

---

## **2. CancellationToken & TaskTracker — 우아한 셧다운 (tokio-util)**
이전에 `broadcast` 채널로 셧다운을 구현했지만, `tokio-util`의 `CancellationToken`과 `TaskTracker`는 훨씬 체계적이고 구조적인 방법을 제공합니다. 프로덕션 서버에서는 이 패턴을 사용하는 것이 표준입니다.

```toml
[dependencies]
tokio-util = { version = "0.7", features = ["rt"] }
```

```rust
use std::time::Duration;
use tokio::signal;
use tokio_util::sync::CancellationToken;
use tokio_util::task::TaskTracker;

// ─── 서버 컴포넌트: 토큰을 받아 스스로 종료하는 방법을 앎 ─────
async fn player_session(player_id: u32, token: CancellationToken) {
    let mut heartbeat = tokio::time::interval(Duration::from_secs(1));

    loop {
        tokio::select! {
            // 취소 신호 수신 시 즉시 정리 후 종료
            _ = token.cancelled() => {
                println!("[플레이어 {}] 셧다운 신호 수신, 세션 저장 중...", player_id);
                save_session(player_id).await;
                println!("[플레이어 {}] 세션 저장 완료, 종료", player_id);
                return;
            }
            _ = heartbeat.tick() => {
                // 정상 게임 로직
                // println!("[플레이어 {}] 틱", player_id);
            }
        }
    }
}

async fn save_session(player_id: u32) {
    tokio::time::sleep(Duration::from_millis(50)).await;
    println!("[플레이어 {}] DB 저장 완료", player_id);
}

// ─── 자식 토큰 — 하위 컴포넌트만 선택적으로 취소 ─────────────
async fn game_room(room_id: u32, parent_token: CancellationToken) {
    // 이 룸만을 위한 자식 토큰
    // parent_token이 취소되면 child도 자동 취소됨
    // child만 취소해도 parent에는 영향 없음
    let room_token = parent_token.child_token();

    // 룸 내부에서 문제 발생 시 이 룸만 종료
    let inner_token = room_token.clone();
    tokio::spawn(async move {
        tokio::time::sleep(Duration::from_secs(30)).await; // 30초 게임 시간
        println!("[룸 {}] 게임 종료, 룸만 취소", room_id);
        inner_token.cancel(); // 이 룸의 태스크들만 종료
    });

    tokio::select! {
        _ = room_token.cancelled() => {
            println!("[룸 {}] 취소됨", room_id);
        }
    }
}

#[tokio::main]
async fn main() {
    // ① 최상위 취소 토큰 — 서버 전체의 생명주기
    let shutdown_token = CancellationToken::new();

    // ② TaskTracker — "모든 태스크가 끝날 때까지 기다리는" 기능
    let tracker = TaskTracker::new();

    // ③ 플레이어 세션 스폰 (tracker를 통해 — 종료 시 추적 가능)
    for player_id in 0..10u32 {
        let token = shutdown_token.clone();
        tracker.spawn(player_session(player_id, token));
    }

    // ④ 시그널 대기 태스크 (tracker에 포함하지 않음 — 마지막까지 살아있어야 함)
    let token_for_signal = shutdown_token.clone();
    tokio::spawn(async move {
        // Ctrl+C 또는 SIGTERM 대기
        tokio::select! {
            _ = signal::ctrl_c() => {
                println!("\n[서버] Ctrl+C 수신!");
            }
        }
        println!("[서버] 셧다운 시작 — 모든 플레이어 세션에 신호 전파...");
        token_for_signal.cancel(); // 전체 서버 취소
    });

    // ⑤ 더 이상 새 태스크를 받지 않음을 선언
    tracker.close();

    // ⑥ 모든 추적 태스크가 완전히 종료될 때까지 대기
    println!("[서버] 모든 플레이어 세션 종료 대기 중...");
    tracker.wait().await;
    println!("[서버] 모든 세션 종료 완료. 서버 정상 종료!");
}
```

**`CancellationToken` vs `broadcast` 채널 비교:**

| 기능 | `broadcast` 채널 | `CancellationToken` |
|---|---|---|
| 취소 신호 전파 | ✅ | ✅ |
| 자식 토큰(부분 취소) | ❌ | ✅ |
| 이미 취소됐는지 동기 확인 | ❌ | ✅ (`is_cancelled()`) |
| 모든 태스크 종료 대기 | 직접 구현 필요 | `TaskTracker.wait()` |
| 메모리 사용 | 버퍼 크기 필요 | 매우 경량 |

---

## **3. tracing — 비동기 서버의 관측 가능성 (Observability)**
일반 `println!`이나 `log`는 비동기 환경에서 어느 태스크에서 어느 맥락에서 찍힌 로그인지 추적이 불가능합니다. `tracing`은 **비동기 Span 컨텍스트**를 자동으로 전파하며, 게임 서버 디버깅의 필수 도구입니다.

```toml
[dependencies]
tracing = "0.1"
tracing-subscriber = { version = "0.3", features = ["env-filter", "fmt"] }
```

```rust
use tracing::{debug, error, info, instrument, warn, Instrument};

// ─── #[instrument] — 함수를 자동으로 Span으로 감쌈 ──────────
#[instrument(fields(player_id, room_id))]
async fn handle_player_login(player_id: u32, username: &str) -> Result<String, String> {
    info!("로그인 시도"); // 자동으로 player_id, username 포함

    let token = authenticate(player_id, username).await?;

    info!(token_len = token.len(), "로그인 성공");
    Ok(token)
}

async fn authenticate(player_id: u32, username: &str) -> Result<String, String> {
    // DB 쿼리 시뮬레이션
    if username.is_empty() {
        return Err("빈 사용자명".to_string());
    }
    Ok(format!("token_{}", player_id))
}

// ─── 수동 Span 생성 — 세밀한 추적 ──────────────────────────
async fn process_game_tick(tick: u64) {
    // Span 생성 — 여러 await 포인트에 걸쳐 컨텍스트 유지
    let span = tracing::span!(
        tracing::Level::DEBUG,
        "game_tick",
        tick_id = tick,
        player_count = 100u32
    );

    async move {
        debug!("틱 처리 시작");

        // 물리 업데이트
        {
            let _physics = tracing::span!(tracing::Level::TRACE, "physics_update").entered();
            // 물리 계산...
            tokio::time::sleep(std::time::Duration::from_micros(500)).await;
        }

        // 브로드캐스트
        {
            let _broadcast = tracing::span!(tracing::Level::TRACE, "state_broadcast").entered();
            // 상태 전송...
        }

        debug!("틱 처리 완료");
    }
    .instrument(span) // ← 비동기 함수에 Span을 붙이는 핵심!
    .await;
}

// ─── 구조화된 이벤트 로깅 ────────────────────────────────────
async fn game_event_logger(player_id: u32) {
    // 키=값 쌍으로 구조화된 로그 → JSON 출력 가능
    info!(
        player_id = player_id,
        action = "kill",
        target_id = 42u32,
        weapon = "sword",
        damage = 150u32,
        "플레이어가 적을 처치"
    );

    warn!(
        player_id = player_id,
        ping_ms = 450u32,
        "플레이어 핑 높음"
    );

    error!(
        player_id = player_id,
        error_code = 0xDEADu32,
        "치명적 패킷 오류"
    );
}

#[tokio::main]
async fn main() {
    // ─── 구독자 설정 (출력 형식 결정) ───────────────────────
    tracing_subscriber::fmt()
        .with_env_filter(
            tracing_subscriber::EnvFilter::from_default_env()
                .add_directive("game_server=debug".parse().unwrap())
                .add_directive("tokio=warn".parse().unwrap()),
        )
        .with_target(true)      // 모듈 경로 표시
        .with_thread_ids(true)  // 스레드 ID 표시 — 어느 워커인지 확인
        .with_file(true)        // 소스 파일 위치
        .with_line_number(true)
        // .json() // 프로덕션에서는 JSON 형식으로
        .init();

    info!("게임 서버 시작");

    // 각 플레이어 태스크에 player_id 컨텍스트 자동 전파
    for i in 0..3u32 {
        tokio::spawn(
            async move {
                handle_player_login(i, "testuser").await.ok();
                process_game_tick(i as u64).await;
            }
            .instrument(tracing::span!(
                tracing::Level::INFO,
                "player_session",
                player_id = i
            )),
        );
    }

    tokio::time::sleep(std::time::Duration::from_millis(100)).await;
}
```

### **3.1 tokio-console — 실시간 비동기 디버거**
`tokio-console`은 실행 중인 서버의 모든 태스크를 실시간으로 모니터링할 수 있는 `htop` 같은 TUI 도구입니다. **태스크 정체(stall), 느린 poll, 데드락**을 즉시 발견할 수 있습니다.

```toml
# Cargo.toml
[dependencies]
console-subscriber = "0.4"
tokio = { version = "1.52", features = ["full", "tracing"] }

# .cargo/config.toml — 필수!
[build]
rustflags = ["--cfg", "tokio_unstable"]
```

```rust
#[tokio::main]
async fn main() {
    // 프로덕션에는 tracing-subscriber, 개발에는 console-subscriber
    console_subscriber::init(); // tokio-console과 연결

    // 이후 코드는 동일
    // 터미널에서 `tokio-console` 명령어로 실시간 확인
    run_game_server().await;
}

async fn run_game_server() {
    // 의도적으로 느린 태스크 — console에서 즉시 발견됨
    tokio::spawn(async {
        loop {
            // 이 태스크는 console에서 "slow poll"로 표시됨
            std::thread::sleep(std::time::Duration::from_millis(10)); // ← 잘못된 블로킹!
            tokio::task::yield_now().await;
        }
    });

    tokio::time::sleep(std::time::Duration::from_secs(60)).await;
}
```

---

## **4. tokio-metrics — 프로덕션 메트릭 수집**
개발 디버깅은 `tokio-console`로, **프로덕션 모니터링**은 `tokio-metrics`로 합니다. Prometheus, Grafana와 연동하여 서버 상태를 대시보드로 확인할 수 있습니다.

```toml
[dependencies]
tokio-metrics = { version = "0.4", features = ["rt"] }
```

```rust
use std::time::Duration;
use tokio_metrics::{RuntimeMonitor, TaskMonitor};

#[tokio::main]
async fn main() {
    // ─── 런타임 메트릭 ────────────────────────────────────────
    let handle = tokio::runtime::Handle::current();
    let runtime_monitor = RuntimeMonitor::new(&handle);

    // 5초마다 런타임 상태 출력 (실제로는 Prometheus에 전송)
    tokio::spawn(async move {
        let mut interval = tokio::time::interval(Duration::from_secs(5));
        for metrics in runtime_monitor.intervals() {
            interval.tick().await;
            println!("=== 런타임 메트릭 ===");
            println!("  워커 스레드 수:     {}", metrics.workers_count);
            println!("  활성 태스크 수:     {}", metrics.live_tasks_count);
            println!("  글로벌 큐 깊이:     {}", metrics.global_queue_depth);
            println!("  워커 바쁜 시간:     {:?}", metrics.total_busy_duration);
            println!("  busy_ratio:         {:.2}%", metrics.busy_ratio() * 100.0);
        }
    });

    // ─── 태스크 메트릭 ────────────────────────────────────────
    let player_monitor = TaskMonitor::new();
    let game_logic_monitor = TaskMonitor::new();

    // 메트릭 리포터 — 주기적으로 수집
    {
        let p_monitor = player_monitor.clone();
        let g_monitor = game_logic_monitor.clone();
        tokio::spawn(async move {
            let mut interval = tokio::time::interval(Duration::from_secs(10));
            loop {
                interval.tick().await;
                for metrics in p_monitor.intervals() {
                    println!("=== 플레이어 태스크 메트릭 ===");
                    println!("  계측 태스크 수:   {}", metrics.instrumented_count);
                    println!("  평균 첫 poll 지연: {:?}", metrics.mean_first_poll_delay());
                    println!("  평균 poll 시간:   {:?}", metrics.mean_poll_duration());
                    println!("  느린 poll 비율:   {:.2}%", metrics.slow_poll_ratio() * 100.0);
                    // slow_poll_ratio가 높다 → spawn_blocking이 필요한 CPU 작업 있음
                    if metrics.slow_poll_ratio() > 0.1 {
                        tracing::warn!(
                            ratio = metrics.slow_poll_ratio(),
                            "⚠️ 느린 poll 감지 — CPU 블로킹 작업 확인 필요"
                        );
                    }
                }
                for metrics in g_monitor.intervals() {
                    println!("=== 게임 로직 태스크 메트릭 ===");
                    println!("  평균 스케줄링 지연: {:?}", metrics.mean_scheduled_duration());
                    // 스케줄링 지연이 크다 → 런타임이 과부하 상태
                }
            }
        });
    }

    // ─── 실제 태스크 계측 ─────────────────────────────────────
    for player_id in 0..50u32 {
        // instrument()로 감싸면 자동으로 메트릭 수집
        let task = player_monitor.instrument(async move {
            tokio::time::sleep(Duration::from_millis(player_id as u64 * 10)).await;
        });
        tokio::spawn(task);
    }

    let game_task = game_logic_monitor.instrument(async {
        loop {
            tokio::time::sleep(Duration::from_millis(16)).await; // 60fps
            // 게임 로직...
        }
    });
    tokio::spawn(game_task);

    tokio::time::sleep(Duration::from_secs(60)).await;
}
```

---

## **5. tokio::task_local! — 태스크 로컬 저장소**
스레드 로컬(`thread_local!`)처럼 각 **태스크**마다 독립적인 저장소를 가질 수 있습니다. 요청 ID, 플레이어 컨텍스트 같은 정보를 함수 인자로 계속 전달하지 않아도 됩니다.

```rust
use tokio::task_local;

// 태스크 로컬 변수 선언
task_local! {
    // 현재 처리 중인 플레이어 ID
    static CURRENT_PLAYER_ID: u32;

    // 현재 요청의 트레이스 ID
    static TRACE_ID: String;
}

// 깊이 중첩된 함수에서도 인자 없이 컨텍스트 접근
async fn deep_nested_logic() {
    let player_id = CURRENT_PLAYER_ID.with(|id| *id);
    let trace_id = TRACE_ID.with(|id| id.clone());

    println!("[{}] 플레이어 {} 처리 중", trace_id, player_id);
    // DB 쿼리, 로깅 등에서 자동으로 컨텍스트 사용 가능
}

async fn middle_layer() {
    // player_id를 파라미터로 안 받아도 됨!
    deep_nested_logic().await;
}

async fn handle_request(player_id: u32, request_id: &str) {
    let trace_id = format!("req-{}-{}", player_id, request_id);

    // scope() 안에서만 이 값들이 유효
    CURRENT_PLAYER_ID
        .scope(player_id, async move {
            TRACE_ID
                .scope(trace_id, async move {
                    // 이 블록 안의 모든 .await에서 컨텍스트 유지
                    middle_layer().await;
                    tokio::time::sleep(std::time::Duration::from_millis(10)).await;
                    middle_layer().await; // 여기서도 동일한 컨텍스트
                })
                .await;
        })
        .await;
}

#[tokio::main]
async fn main() {
    // 여러 플레이어가 동시에 각자의 컨텍스트를 가짐
    let handles: Vec<_> = (0..5u32)
        .map(|i| {
            tokio::spawn(handle_request(i, &format!("{:04X}", i * 0x100)))
        })
        .collect();

    for h in handles {
        let _ = h.await;
    }
}
```

---

## **6. 테스트 전략 — `#[tokio::test]`와 시간 모킹**
비동기 서버의 테스트는 동기 코드보다 훨씬 까다롭습니다. 특히 `sleep`, `interval`, `timeout`이 포함된 로직은 실제 시간을 기다리면 테스트가 극도로 느려집니다. Tokio는 **시간 자체를 제어**할 수 있는 강력한 테스트 도구를 제공합니다.

```rust
// ─── 기본 비동기 테스트 ──────────────────────────────────────
#[cfg(test)]
mod tests {
    use super::*;
    use std::time::Duration;
    use tokio::sync::mpsc;

    #[tokio::test] // 단일 스레드 런타임으로 테스트 실행
    async fn test_player_message_received() {
        let (tx, mut rx) = mpsc::channel::<String>(10);

        tokio::spawn(async move {
            tx.send("안녕!".to_string()).await.unwrap();
        });

        let msg = rx.recv().await.unwrap();
        assert_eq!(msg, "안녕!");
    }

    // ─── 시간 일시정지 — 타이머 테스트의 혁명 ────────────────────
    // start_paused = true → 시간이 멈춘 상태로 시작
    // 다른 Future가 없으면 자동으로 시간이 "점프"됨
    #[tokio::test(start_paused = true)]
    async fn test_cooldown_instantly() {
        async fn skill_with_cooldown(tx: mpsc::Sender<&'static str>) {
            tx.send("스킬 사용").await.unwrap();
            tokio::time::sleep(Duration::from_secs(30)).await; // 30초 쿨다운
            tx.send("쿨다운 완료").await.unwrap();
        }

        let (tx, mut rx) = mpsc::channel(2);
        tokio::spawn(skill_with_cooldown(tx));

        assert_eq!(rx.recv().await.unwrap(), "스킬 사용");

        // 실제로 30초를 기다리지 않음!
        // sleep 이외의 Future가 없으면 시간이 자동으로 30초 앞으로 점프
        assert_eq!(rx.recv().await.unwrap(), "쿨다운 완료");
        // 이 테스트는 ~0ms 안에 완료됨
    }

    // ─── 수동 시간 제어 — advance/pause/resume ─────────────────
    #[tokio::test(start_paused = true)]
    async fn test_game_loop_ticks() {
        use std::sync::{
            atomic::{AtomicU32, Ordering},
            Arc,
        };

        let tick_count = Arc::new(AtomicU32::new(0));
        let counter = tick_count.clone();

        tokio::spawn(async move {
            let mut interval = tokio::time::interval(Duration::from_millis(16));
            loop {
                interval.tick().await;
                counter.fetch_add(1, Ordering::Relaxed);
            }
        });

        // 다른 코드를 잠깐 실행할 기회를 주고
        tokio::task::yield_now().await;

        // 1초 앞으로 점프 (약 62회 틱 예상)
        tokio::time::advance(Duration::from_secs(1)).await;
        tokio::task::yield_now().await;

        let count = tick_count.load(Ordering::Relaxed);
        println!("1초간 틱 횟수: {}", count);
        assert!(count >= 60, "초당 최소 60틱 이상이어야 함: {}", count);
    }

    // ─── I/O 모킹 — 실제 네트워크 없이 소켓 테스트 ──────────────
    // tokio-test 크레이트 필요: tokio-test = "0.4"
    #[tokio::test]
    async fn test_echo_handler() {
        use tokio::io::{AsyncRead, AsyncWrite, AsyncWriteExt, BufReader, AsyncBufReadExt};

        async fn echo_handler<R, W>(reader: R, mut writer: W)
        where
            R: AsyncRead + Unpin,
            W: AsyncWrite + Unpin,
        {
            let mut reader = BufReader::new(reader);
            let mut line = String::new();
            while reader.read_line(&mut line).await.unwrap_or(0) > 0 {
                writer.write_all(line.as_bytes()).await.unwrap();
                line.clear();
            }
        }

        // 가상 소켓 — 실제 네트워크 없이 테스트
        let mock_reader = tokio_test::io::Builder::new()
            .read(b"Hello\n")
            .read(b"World\n")
            .build();
        let mock_writer = tokio_test::io::Builder::new()
            .write(b"Hello\n")
            .write(b"World\n")
            .build();

        echo_handler(mock_reader, mock_writer).await;
        // 테스트 통과! Builder의 기대값과 실제 동작이 일치
    }

    // ─── 패닉 전파 테스트 ────────────────────────────────────────
    #[tokio::test]
    async fn test_task_panic_is_caught() {
        let handle = tokio::spawn(async {
            panic!("의도적인 패닉!");
        });

        let result = handle.await;
        assert!(result.is_err());
        assert!(result.unwrap_err().is_panic());
        // 다른 태스크는 전혀 영향받지 않음
    }

    // ─── 멀티스레드 런타임으로 테스트 ────────────────────────────
    #[tokio::test(flavor = "multi_thread", worker_threads = 4)]
    async fn test_concurrent_players() {
        let (tx, mut rx) = mpsc::channel(100);

        let mut handles = Vec::new();
        for i in 0..20u32 {
            let tx = tx.clone();
            handles.push(tokio::spawn(async move {
                tokio::time::sleep(Duration::from_millis(i as u64)).await;
                tx.send(i).await.unwrap();
            }));
        }
        drop(tx);

        let mut received = Vec::new();
        while let Some(v) = rx.recv().await {
            received.push(v);
        }
        for h in handles {
            h.await.unwrap();
        }

        assert_eq!(received.len(), 20);
    }
}
```

---

## **7. 오류 처리 & 패닉 전략 — 서버를 절대 죽이지 않기**
게임 서버에서 한 플레이어의 버그가 전체 서버를 죽여서는 안 됩니다. Tokio의 태스크 격리와 적절한 패닉 훅을 조합합니다.

```rust
use std::panic;

// ─── 전역 패닉 훅 — 패닉 정보 로깅 후 선택적 종료 ─────────────
fn setup_panic_handler() {
    panic::set_hook(Box::new(|panic_info| {
        // 패닉 발생 위치와 메시지 추출
        let location = panic_info
            .location()
            .map(|l| format!("{}:{}:{}", l.file(), l.line(), l.column()))
            .unwrap_or_else(|| "unknown".to_string());

        let message = if let Some(s) = panic_info.payload().downcast_ref::<&str>() {
            s.to_string()
        } else if let Some(s) = panic_info.payload().downcast_ref::<String>() {
            s.clone()
        } else {
            "Unknown panic".to_string()
        };

        // tracing을 통해 구조화된 로그로 기록
        tracing::error!(
            location = %location,
            message = %message,
            "💥 패닉 발생!"
        );

        // 게임 서버는 개별 태스크 패닉 시 종료하지 않음
        // (치명적 오류만 프로세스 종료)
    }));
}

// ─── 태스크 수준 오류 처리 ──────────────────────────────────
async fn resilient_player_session(player_id: u32) {
    // 개별 태스크의 패닉은 JoinHandle을 통해 안전하게 잡힘
    // 다른 태스크에 전혀 영향 없음
    let result = tokio::spawn(async move {
        // 플레이어 세션 로직 — 패닉이 발생해도 OK
        if player_id == 7 {
            panic!("플레이어 7의 버그!"); // 이 태스크만 종료됨
        }
        format!("플레이어 {} 정상 종료", player_id)
    })
    .await;

    match result {
        Ok(msg) => tracing::info!("{}", msg),
        Err(e) if e.is_panic() => {
            tracing::error!(player_id, "플레이어 태스크 패닉 — 세션 재시작 가능");
            // 필요시 세션 재시작 로직
        }
        Err(e) if e.is_cancelled() => {
            tracing::info!(player_id, "플레이어 태스크 취소됨");
        }
        Err(e) => tracing::error!(player_id, error = ?e, "알 수 없는 오류"),
    }
}

// ─── anyhow / thiserror 연동 패턴 ──────────────────────────
// [dependencies]
// anyhow = "1"
// thiserror = "2"
use std::fmt;

#[derive(Debug)]
enum GameError {
    ConnectionClosed,
    InvalidPacket(String),
    AuthFailed { player_id: u32, reason: String },
    Timeout(std::time::Duration),
}

impl fmt::Display for GameError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            GameError::ConnectionClosed => write!(f, "연결이 종료됨"),
            GameError::InvalidPacket(msg) => write!(f, "잘못된 패킷: {}", msg),
            GameError::AuthFailed { player_id, reason } => {
                write!(f, "플레이어 {} 인증 실패: {}", player_id, reason)
            }
            GameError::Timeout(d) => write!(f, "타임아웃: {:?}", d),
        }
    }
}

impl std::error::Error for GameError {}

async fn handle_with_proper_errors(player_id: u32) -> Result<(), GameError> {
    let timeout = std::time::Duration::from_secs(30);

    tokio::time::timeout(timeout, async {
        // ...실제 로직...
        Ok::<(), GameError>(())
    })
    .await
    .map_err(|_| GameError::Timeout(timeout))?
}

#[tokio::main]
async fn main() {
    setup_panic_handler();
    tracing_subscriber::fmt::init();

    // 모든 플레이어 세션 — 어느 하나의 패닉도 서버를 죽이지 않음
    let handles: Vec<_> = (0..10u32)
        .map(|i| tokio::spawn(resilient_player_session(i)))
        .collect();

    for h in handles {
        let _ = h.await;
    }
    println!("모든 세션 처리 완료");
}
```

---

## **8. 전체 추가 지식 로드맵**
이제 이전 답변과 이번 심화 편을 합치면 다음과 같은 완전한 그림이 됩니다.

```
┌─────────────────────────────────────────────────────────────────┐
│              Tokio 게임 서버 완전 지식 맵 (2026)                  │
├────────────────────────┬────────────────────────────────────────┤
│  【이전 편】 핵심 API    │  【이번 편】 심화/필수 주변 지식          │
├────────────────────────┼────────────────────────────────────────┤
│  Runtime               │  tokio-util Codec / Framed             │
│  spawn / JoinSet       │  → 패킷 프레이밍, 바이너리 프로토콜     │
│  spawn_blocking        │                                        │
│  mpsc / broadcast /    │  CancellationToken + TaskTracker       │
│  oneshot / watch       │  → 프로덕션급 우아한 셧다운              │
│  Mutex / RwLock /      │                                        │
│  Semaphore / Notify    │  tracing + tracing-subscriber          │
│  TcpListener /         │  → 비동기 Span, 구조화 로그             │
│  TcpStream / Udp       │                                        │
│  sleep / interval /    │  tokio-console                        │
│  timeout               │  → 실시간 태스크 디버거                 │
│  select! / join!       │                                        │
│  signal                │  tokio-metrics (TaskMonitor /          │
│  io (AsyncRead/Write)  │   RuntimeMonitor)                      │
│  Actor 패턴            │  → 프로덕션 Prometheus 연동             │
│                        │                                        │
│                        │  task_local!                           │
│                        │  → 태스크 컨텍스트 전파                 │
│                        │                                        │
│                        │  #[tokio::test] + 시간 모킹             │
│                        │  → start_paused, advance, mock I/O     │
│                        │                                        │
│                        │  패닉 훅 + 오류 처리 전략               │
│                        │  → 서버 안정성 보장                     │
└────────────────────────┴────────────────────────────────────────┘
```

**학습 우선순위 요약:**

초보자라면 Codec/Framed → CancellationToken → `#[tokio::test]` 순서로 익히는 것을 추천합니다. Codec은 실제 게임 서버를 만드는 즉시 마주치는 문제이고, CancellationToken은 서버 종료를 안전하게 처리하는 표준 방법이며, 테스트 도구는 코드를 짜면서 동시에 배워야 하기 때문입니다. 중급 이상이라면 `tracing` + `tokio-console` 조합으로 **관측 가능성**을 갖추고, `tokio-metrics`로 프로덕션 모니터링 체계를 구축하는 것이 서버 운영 품질을 크게 높여줄 것입니다.  