# Chapter 7. 동시성 모델 — Send/Sync는 컴파일러가 검증한다

> "C++에서는 데이터 레이스가 가능하다. C#에서는 가능하지만 예외를 던진다. 
> Rust에서는 컴파일조차 되지 않는다."

---

## 7.1 데이터 레이스의 정의와 심각성

데이터 레이스(Data Race)는 세 조건이 동시에 충족될 때 발생한다:

1. 두 개 이상의 스레드가 같은 메모리 위치에 동시에 접근
2. 그 중 최소 하나의 접근이 쓰기(write)
3. 해당 접근들이 동기화되지 않음

게임 서버 개발자라면 이런 버그를 경험해 봤을 것이다:

```cpp
// C++ 게임 서버 - 데이터 레이스
class PlayerManager {
    std::unordered_map<int, Player> players;

    // 스레드 1에서 호출
    void AddPlayer(int id, Player p) {
        players[id] = p; // 쓰기
    }
    
    // 스레드 2에서 동시에 호출
    Player* GetPlayer(int id) {
        auto it = players.find(id); // 읽기
        return it != players.end() ? &it->second : nullptr;
    }
};
```

이 코드는 컴파일되고, 대부분의 경우 정상 동작하지만, 가끔 예측 불가능하게 크래시하거나 데이터가 오염된다. 재현이 어렵고, 디버깅이 매우 힘들다.

Rust에서는 이런 패턴 자체가 컴파일 타임에 차단된다.

---

## 7.2 Send와 Sync — 자동 트레이트

Rust의 동시성 안전성은 두 개의 **자동 트레이트(Auto Trait)**에 기반한다.

### Send 트레이트

```rust
// Send: "이 타입의 소유권을 다른 스레드로 안전하게 이동할 수 있다"
pub unsafe auto trait Send {}
```

`Send`가 구현된 타입:
- `i32`, `f64`, `bool` 등 기본 타입들
- `String`, `Vec<T>` (T가 Send이면)
- `Arc<T>` (T가 Send + Sync이면)
- `Mutex<T>` (T가 Send이면)

`Send`가 구현되지 않은 타입:
- `Rc<T>`: 원자적이지 않은 참조 카운팅 → 스레드 간 이동 시 카운터 불일치
- `*mut T`, `*const T`: 원시 포인터 → 안전 보장 없음
- `Cell<T>`, `RefCell<T>`: 단일 스레드 내부 가변성

### Sync 트레이트

```rust
// Sync: "이 타입을 여러 스레드에서 &T로 동시에 참조해도 안전하다"
pub unsafe auto trait Sync {}
```

`T`가 `Sync`이면 `&T`는 `Send`이다. 즉, 여러 스레드가 같은 데이터를 동시에 읽어도 안전하다는 의미다.

`Sync`가 아닌 타입:
- `Cell<T>`, `RefCell<T>`: 내부적으로 런타임 가변성 → 스레드 간 동시 참조 위험
- `Rc<T>`: 참조 카운터가 원자적이지 않음

### 자동 구현 규칙

핵심: **사람이 명시적으로 구현하지 않아도 컴파일러가 자동으로 판단한다.**

```rust
struct SafeStruct {
    data: Vec<i32>,    // Send + Sync
    count: u64,        // Send + Sync
}
// SafeStruct는 자동으로 Send + Sync

struct UnsafeStruct {
    data: Vec<i32>,    // Send + Sync
    ptr: *mut u8,      // !Send, !Sync (raw pointer)
}
// UnsafeStruct는 Send도 Sync도 아님 - 컴파일러가 자동으로 제거
```

---

## 7.3 컴파일 타임 데이터 레이스 차단

Tokio 태스크는 `Send + 'static` 바운드를 요구한다. 이것이 데이터 레이스를 원천 차단하는 메커니즘이다.

```rust
use tokio;

// tokio::spawn의 시그니처 (단순화)
pub fn spawn<F>(future: F) -> JoinHandle<F::Output>
where
    F: Future + Send + 'static, // 타입 바운드!
    F::Output: Send + 'static,
```

이 바운드 덕분에:

```rust
// 오류 1: Rc는 Send가 아님
let data = std::rc::Rc::new(vec![1, 2, 3]);
tokio::spawn(async move {
    println!("{:?}", data); // 컴파일 오류!
});
// error: `Rc<Vec<i32>>` cannot be sent between threads safely

// 오류 2: 참조는 'static이 아님 (생존 보장 없음)
let data = vec![1, 2, 3];
let data_ref = &data;
tokio::spawn(async move {
    println!("{:?}", data_ref); // 컴파일 오류!
});
// error: `data_ref` does not live long enough

// 올바른 방법: 소유권을 이동하거나 Arc 사용
let data = vec![1, 2, 3];
tokio::spawn(async move {
    println!("{:?}", data); // OK - 소유권 이동
});

let data = std::sync::Arc::new(vec![1, 2, 3]);
let data_clone = Arc::clone(&data);
tokio::spawn(async move {
    println!("{:?}", data_clone); // OK - Arc는 Send + Sync
});
```

---

## 7.4 Send/Sync 오류 진단 가이드

AI가 생성한 코드에서 Send/Sync 관련 컴파일 오류가 발생했을 때 진단하는 방법.

### 오류 패턴 1: "cannot be sent between threads safely"

```
error[E0277]: `Rc<PlayerState>` cannot be sent between threads safely
  --> src/server.rs:42:9
   |
42 |         tokio::spawn(async move {
   |         ^^^^^^^^^^^^ `Rc<PlayerState>` cannot be sent between threads safely
   |
   = help: the trait `Send` is not implemented for `Rc<PlayerState>`
```

**진단**: 태스크 클로저가 `Rc<T>` 타입의 변수를 캡처하고 있다.

**해결**: `Rc<T>` → `Arc<T>` 교체

```rust
// 이전
let state = Rc::new(PlayerState::new());

// 이후
let state = Arc::new(PlayerState::new());
let state_clone = Arc::clone(&state);
tokio::spawn(async move {
    use_state(&state_clone).await;
});
```

### 오류 패턴 2: "RefCell cannot be shared between threads"

```
error[E0277]: `RefCell<HashMap<u64, Session>>` cannot be shared between threads safely
```

**진단**: `RefCell`이 포함된 타입을 여러 스레드에서 `&T`로 공유하려 했다.

**해결**: `RefCell<T>` → `Mutex<T>` 또는 `RwLock<T>`

```rust
// 이전 - 단일 스레드 패턴
struct SessionStore {
    sessions: RefCell<HashMap<u64, Session>>,
}

// 이후 - 멀티스레드 패턴
struct SessionStore {
    sessions: Mutex<HashMap<u64, Session>>, // 또는 DashMap
}
```

### 오류 패턴 3: "does not live long enough" in spawn

```
error[E0597]: `config` does not live long enough
  --> src/main.rs:15:20
```

**진단**: 스택에 있는 변수의 참조를 태스크에 넘기려 했다. 태스크의 생존 시간이 참조의 생존 시간보다 길 수 있다.

**해결**: 참조 대신 소유권 이동 또는 `Arc`로 공유

```rust
// 이전 - 참조 전달 (수명 오류)
async fn run(config: &Config) {
    tokio::spawn(async {
        use_config(config).await; // config가 run보다 짧게 살 수 있음
    });
}

// 이후 - 소유권 이동
async fn run(config: Config) {
    tokio::spawn(async move {
        use_config(config).await; // config의 소유권이 태스크로 이동
    });
}

// 또는 - Arc로 공유
async fn run(config: Arc<Config>) {
    let config = Arc::clone(&config);
    tokio::spawn(async move {
        use_config(&config).await;
    });
}
```

---

## 7.5 스레드 안전 설계 패턴

게임 서버에서 Send/Sync를 올바르게 활용하는 주요 패턴들.

### 패턴 1: 공유 상태는 Arc로 감싸기

```rust
// 서버 전체에서 공유되는 상태
pub struct SharedState {
    pub sessions: DashMap<u64, Arc<Mutex<Session>>>,
    pub config: RwLock<ServerConfig>,
    pub broadcast_tx: tokio::sync::broadcast::Sender<BroadcastMessage>,
}

// Arc로 감싸서 여러 곳에서 공유
pub type SharedStateRef = Arc<SharedState>;

async fn handle_connection(
    socket: TcpStream,
    state: SharedStateRef, // 클론 비용이 싼 Arc
) {
    let session_id = generate_session_id();
    // ...
}
```

### 패턴 2: 스레드 로컬 데이터

스레드마다 독립적인 데이터가 필요한 경우:

```rust
use std::cell::RefCell;

thread_local! {
    // 각 스레드마다 독립적인 카운터
    static PACKET_COUNT: RefCell<u64> = RefCell::new(0);
}

fn increment_counter() {
    PACKET_COUNT.with(|counter| {
        *counter.borrow_mut() += 1;
    });
}

fn get_count() -> u64 {
    PACKET_COUNT.with(|counter| *counter.borrow())
}
```

Tokio 워커 스레드에서 사용할 때는 태스크가 다른 스레드로 이동할 수 있으므로 주의해야 한다.

### 패턴 3: 채널로 데이터 이동

공유하지 않고 이동시키는 패턴이 더 안전하고 성능이 좋은 경우가 많다:

```rust
// 공유 대신 이동 - 락 없이도 안전
use tokio::sync::mpsc;

struct PacketProcessor {
    rx: mpsc::Receiver<Packet>,
    // 이 구조체는 단 하나의 태스크에서만 사용됨
    state: HashMap<u64, PlayerState>,
}

impl PacketProcessor {
    async fn run(mut self) {
        while let Some(packet) = self.rx.recv().await {
            self.process(packet).await;
        }
    }
    
    async fn process(&mut self, packet: Packet) {
        // 단일 태스크이므로 락 없이 자유롭게 수정
        self.state.entry(packet.player_id)
            .or_default()
            .apply_packet(&packet);
    }
}
```

---

## 7.6 원자적 타입 (Atomic Types)

단순한 카운터나 플래그의 경우 Mutex 없이 원자적 타입을 사용할 수 있다.

```rust
use std::sync::atomic::{AtomicU64, AtomicBool, Ordering};

struct ServerMetrics {
    total_packets: AtomicU64,
    active_sessions: AtomicU64,
    is_shutting_down: AtomicBool,
}

impl ServerMetrics {
    fn record_packet(&self) {
        self.total_packets.fetch_add(1, Ordering::Relaxed);
    }
    
    fn increment_sessions(&self) {
        self.active_sessions.fetch_add(1, Ordering::SeqCst);
    }
    
    fn decrement_sessions(&self) {
        self.active_sessions.fetch_sub(1, Ordering::SeqCst);
    }
    
    fn request_shutdown(&self) {
        self.is_shutting_down.store(true, Ordering::SeqCst);
    }
    
    fn is_shutting_down(&self) -> bool {
        self.is_shutting_down.load(Ordering::Relaxed)
    }
}
```

### 메모리 순서 (Ordering)

원자 연산의 메모리 순서는 성능과 정확성 사이의 트레이드오프다:

- `Ordering::Relaxed`: 가장 빠름, 순서 보장 없음 (단순 카운터에 적합)
- `Ordering::Acquire`/`Release`: 읽기/쓰기 동기화 (자주 사용)
- `Ordering::SeqCst`: 가장 강한 보장, 가장 느림 (중요한 플래그에 적합)

게임 서버에서의 규칙:
- 단순 통계 카운터: `Relaxed`
- 상태 플래그 (shutdown, maintenance mode): `SeqCst`
- 자체 구현 락: `Acquire`/`Release` (되도록 피할 것)

---

## 7.7 데드락 방지

Rust가 데이터 레이스를 방지하지만, **데드락은 방지하지 않는다.** 데드락은 런타임 문제로 남는다.

### 데드락 발생 패턴

```rust
// 락 순서 역전 - 데드락!
async fn transfer_gold(
    from_room: Arc<Mutex<Room>>,
    to_room: Arc<Mutex<Room>>,
    amount: u32,
) {
    let mut from = from_room.lock().await; // 락 1 획득
    let mut to = to_room.lock().await;     // 락 2 획득 시도
    // 동시에 반대 방향으로 transfer_gold가 실행 중이면 데드락!
    
    from.gold -= amount;
    to.gold += amount;
}
```

### 데드락 방지 전략

**1. 락 순서 강제**

```rust
use std::cmp::Ordering;

async fn transfer_gold(
    room_a: Arc<Mutex<Room>>,
    room_b: Arc<Mutex<Room>>,
    amount: u32,
) {
    // 항상 포인터 주소가 작은 것을 먼저 잠근다
    let (first, second) = if Arc::as_ptr(&room_a) < Arc::as_ptr(&room_b) {
        (&room_a, &room_b)
    } else {
        (&room_b, &room_a)
    };
    
    let mut first_guard = first.lock().await;
    let mut second_guard = second.lock().await;
    // 안전하게 처리
}
```

**2. 채널 기반 아키텍처 (락 없음)**

```rust
// 각 Room이 자신의 태스크를 가지고 메시지로 통신
struct RoomActor {
    id: u32,
    state: RoomState,
    rx: mpsc::Receiver<RoomCommand>,
}

enum RoomCommand {
    AddGold { amount: u32, reply: oneshot::Sender<bool> },
    RemoveGold { amount: u32, reply: oneshot::Sender<bool> },
}

impl RoomActor {
    async fn run(mut self) {
        while let Some(cmd) = self.rx.recv().await {
            match cmd {
                RoomCommand::AddGold { amount, reply } => {
                    self.state.gold += amount;
                    let _ = reply.send(true);
                }
                RoomCommand::RemoveGold { amount, reply } => {
                    if self.state.gold >= amount {
                        self.state.gold -= amount;
                        let _ = reply.send(true);
                    } else {
                        let _ = reply.send(false);
                    }
                }
            }
        }
    }
}

// 사용 - 데드락 불가
async fn transfer_gold(
    from_tx: mpsc::Sender<RoomCommand>,
    to_tx: mpsc::Sender<RoomCommand>,
    amount: u32,
) -> bool {
    let (reply_tx, reply_rx) = oneshot::channel();
    from_tx.send(RoomCommand::RemoveGold { amount, reply: reply_tx }).await.ok();
    
    if reply_rx.await.unwrap_or(false) {
        let (reply_tx2, reply_rx2) = oneshot::channel();
        to_tx.send(RoomCommand::AddGold { amount, reply: reply_tx2 }).await.ok();
        reply_rx2.await.unwrap_or(false)
    } else {
        false
    }
}
```

이 액터 패턴이 Chapter 8에서 다루는 Tokio 채널의 핵심 사용 사례다.

---

## 7.8 unsafe impl Send/Sync

가끔 컴파일러가 자동으로 `Send`/`Sync`를 부여하지 않지만, 사람이 직접 안전하다고 확인한 경우 명시적으로 구현할 수 있다.

```rust
// 원시 포인터를 포함하지만 안전하게 사용하는 타입
struct SyncWrapper<T> {
    ptr: *mut T, // 이 때문에 자동으로 !Send, !Sync
}

// 사람이 책임지고 구현
unsafe impl<T: Send> Send for SyncWrapper<T> {}
unsafe impl<T: Sync> Sync for SyncWrapper<T> {}
```

**경고**: `unsafe impl Send/Sync`는 C++에서 "이 코드는 스레드 안전하다고 믿습니다"라고 주석을 다는 것과 같다. AI가 이 코드를 생성했다면 반드시 검토가 필요하다.

게임 서버 코드에서는 이것이 필요한 경우가 거의 없다. 대부분 `Arc`, `Mutex`, `RwLock`으로 해결된다.

---

## 7.9 동시성 도구 비교 요약

| 도구 | 용도 | Send/Sync | await 가능 |
|------|------|-----------|-----------|
| `Arc<T>` | 공유 소유권 | 예 (T가 Send+Sync이면) | - |
| `std::sync::Mutex<T>` | 가변 공유, 동기 | 예 | 금지 (await 가로지르면 안 됨) |
| `tokio::sync::Mutex<T>` | 가변 공유, 비동기 | 예 | 가능 |
| `std::sync::RwLock<T>` | 읽기 많은 가변 공유, 동기 | 예 | 금지 |
| `tokio::sync::RwLock<T>` | 읽기 많은 가변 공유, 비동기 | 예 | 가능 |
| `DashMap<K,V>` | 동시 HashMap | 예 | - |
| `AtomicU64` 등 | 단순 카운터/플래그 | 예 | - |
| `Rc<T>` | 단일 스레드 공유 소유권 | 아니오 | - |
| `RefCell<T>` | 단일 스레드 내부 가변성 | 아니오 | - |

---

## 7.10 AI에게 동시성 관련 지시를 내리는 법

### 스레드 안전 요구사항 명시

> "이 구조체는 여러 Tokio 태스크에서 Arc로 공유되어야 해.
> Send + Sync를 만족해야 하므로 Rc나 RefCell을 쓰지 말고,
> 가변 상태는 tokio::sync::Mutex 또는 RwLock으로 보호해줘."

### 채널 기반 아키텍처 요청

> "RoomManager는 단일 태스크에서만 상태를 수정하는 액터 패턴으로 설계해줘.
> 외부에서는 mpsc 채널로 RoomCommand 메시지를 보내고,
> oneshot 채널로 응답을 받는 구조로 해줘.
> 이렇게 하면 Mutex 없이도 스레드 안전해져."

### Send/Sync 오류 해결

> "이 컴파일 오류: 'Rc<PlayerData> cannot be sent between threads safely'
> 원인: PlayerData를 Rc로 감싸서 Tokio 태스크에 move 했다.
> 해결: Rc를 Arc로 교체해줘.
> PlayerData는 변경이 필요하므로 Arc<Mutex<PlayerData>>로 만들어줘."

---

## 7.11 정리

데이터 레이스 컴파일 타임 차단 — Send와 Sync 자동 트레이트가 스레드 간 안전하지 않은 데이터 이동/공유를 컴파일 오류로 만든다.

Send — 소유권을 다른 스레드로 이동 가능. Tokio 태스크는 Send를 요구한다.

Sync — 여러 스레드에서 `&T`로 동시 접근 가능. `Arc<T>`는 T가 Sync이면 Sync.

Rc, RefCell은 스레드 안전하지 않다 — 멀티스레드 서버에서는 Arc, Mutex, RwLock을 사용한다.

데드락은 컴파일러가 잡지 못한다 — 락 순서 강제 또는 채널 기반 액터 패턴으로 방지한다.

unsafe impl은 금물 — AI가 이를 생성했다면 반드시 검토하고, 대부분의 경우 표준 동기화 도구로 대체 가능하다.

---

*다음 챕터: Chapter 8 — 비동기 프로그래밍과 Tokio: 게임 서버의 본진*
