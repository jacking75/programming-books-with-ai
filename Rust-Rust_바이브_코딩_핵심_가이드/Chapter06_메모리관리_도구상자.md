# Chapter 6. 메모리 관리 도구 상자 — 언제 무엇을 쓰는가

> "Rust에서 메모리 관리 도구를 선택하는 것은 음식을 고르는 것과 같다. 
> 모든 도구가 다 좋지만, 상황에 맞는 도구를 골라야 한다."

---

## 6.1 도구 상자 전체 그림

C++에서는 메모리 관리 방법이 너무 많아서 혼란스러웠다. C#에서는 GC가 다 해주니 생각할 필요가 없었다. Rust는 중간 지점이다: 몇 가지 잘 정의된 도구들이 있고, 그중 하나를 선택해야 한다.

```
메모리 위치별 분류:
├── 스택: 크기가 컴파일 타임에 고정된 값들
│   └── 일반 변수, 배열, 구조체 등
└── 힙: 동적으로 크기가 결정되는 값들
    ├── 단일 소유: Box<T>
    ├── 공유 소유 (단일 스레드): Rc<T>
    └── 공유 소유 (멀티 스레드): Arc<T>
        ├── 읽기 전용 공유: Arc<T>
        ├── 내부 가변성 (단일 스레드): Arc<RefCell<T>>  ← 거의 안 씀
        └── 내부 가변성 (멀티 스레드):
            ├── Arc<Mutex<T>>      ← 일반적 가변 공유
            └── Arc<RwLock<T>>    ← 읽기 많고 쓰기 적을 때
```

---

## 6.2 Box<T> — 힙에 단일 소유 객체

`Box<T>`는 C++의 `std::unique_ptr<T>`에 해당한다. 힙에 값을 할당하고 단일 소유자를 갖는다.

### 언제 Box를 쓰는가

**1. 재귀 자료구조**

컴파일러는 타입의 크기를 컴파일 타임에 알아야 하는데, 재귀 구조는 크기가 무한해진다.

```rust
// 오류 - 재귀 타입의 크기가 무한하다
enum TreeNode {
    Leaf(i32),
    Branch(TreeNode, TreeNode), // TreeNode가 TreeNode를 포함
}

// 해결 - Box로 힙 할당하면 포인터 크기(8바이트)로 고정됨
enum TreeNode {
    Leaf(i32),
    Branch(Box<TreeNode>, Box<TreeNode>),
}
```

게임 서버에서의 예:

```rust
// 이진 공간 분할 트리 (BSP Tree)
enum BspNode {
    Leaf { entities: Vec<EntityId> },
    Branch {
        plane: Plane,
        front: Box<BspNode>,
        back: Box<BspNode>,
    },
}

// 연결 리스트 (실전에서는 Vec이 더 낫지만 예시로)
enum List<T> {
    Nil,
    Cons(T, Box<List<T>>),
}
```

**2. 트레이트 객체**

크기가 다른 타입들을 같은 컬렉션에 담을 때:

```rust
trait Skill {
    fn name(&self) -> &str;
    fn cast(&self, caster: &mut Player, targets: &mut [&mut dyn Damageable]);
}

struct Fireball { damage: u32, radius: f32 }
struct Heal { amount: u32, target_count: u32 }

impl Skill for Fireball { /* ... */ }
impl Skill for Heal { /* ... */ }

// 크기가 다른 Fireball과 Heal을 같은 Vec에 담으려면 Box 필요
struct SkillBook {
    skills: Vec<Box<dyn Skill>>,
}

impl SkillBook {
    fn add_skill(&mut self, skill: Box<dyn Skill>) {
        self.skills.push(skill);
    }
    
    fn cast_all(&self, caster: &mut Player, targets: &mut [&mut dyn Damageable]) {
        for skill in &self.skills {
            skill.cast(caster, targets);
        }
    }
}

// 사용
let mut book = SkillBook { skills: vec![] };
book.add_skill(Box::new(Fireball { damage: 100, radius: 5.0 }));
book.add_skill(Box::new(Heal { amount: 50, target_count: 3 }));
```

**3. 큰 데이터를 스택에서 힙으로**

큰 구조체를 스택에 두면 스택 오버플로 위험이 있다:

```rust
// 큰 배열 - 스택 오버플로 위험
let large_map: [[Tile; 1000]; 1000] = [[Tile::Empty; 1000]; 1000]; // 4MB 스택!

// Box로 힙에 할당
let large_map: Box<[[Tile; 1000]; 1000]> = 
    Box::new([[Tile::Empty; 1000]; 1000]);
```

---

## 6.3 Rc<T> — 단일 스레드 공유 소유권

`Rc<T>`는 Reference Counting의 약자로, 단일 스레드 내에서 여러 소유자가 같은 값을 공유할 때 사용한다.

```rust
use std::rc::Rc;

let shared_config = Rc::new(GameConfig {
    max_players: 100,
    tick_rate: 20,
});

let config_for_room1 = Rc::clone(&shared_config); // 참조 카운트 +1
let config_for_room2 = Rc::clone(&shared_config); // 참조 카운트 +1

// 모든 Rc가 drop될 때 GameConfig가 해제됨
```

### Rc의 제한사항

게임 서버에서 `Rc`를 직접 쓸 일은 거의 없다. `Rc`는 `Send`가 아니어서 스레드 간 이동이 불가능하기 때문이다.

```rust
let shared = Rc::new(42);
tokio::spawn(async move {
    println!("{}", shared); // 컴파일 오류! Rc<i32>는 Send 아님
});
```

→ **멀티스레드 서버에서는 항상 `Arc`를 사용한다.**

---

## 6.4 Arc<T> — 멀티스레드 공유 소유권

`Arc<T>`는 Atomic Reference Counting의 약자. 스레드 간 안전하게 공유할 수 있는 공유 소유권이다.

```rust
use std::sync::Arc;

let shared_state = Arc::new(ServerState::new());

// 여러 태스크에서 공유
for i in 0..10 {
    let state = Arc::clone(&shared_state);
    tokio::spawn(async move {
        // state를 사용하지만 소유하지 않음 (공유)
        let player_count = state.player_count();
        println!("Task {}: {} players", i, player_count);
    });
}
```

### Arc의 불변성

`Arc<T>` 자체는 **읽기 전용**이다. 여러 스레드에서 참조를 공유하므로, 수정을 허용하면 데이터 레이스가 발생한다.

```rust
let shared = Arc::new(vec![1, 2, 3]);

// 읽기는 가능
println!("{:?}", shared);

// 수정은 불가 - Arc는 불변
// shared.push(4); // 오류! Arc<Vec<i32>>는 DerefMut 없음
```

수정이 필요하면 내부에 가변성을 추가해야 한다 → `Mutex` 또는 `RwLock`

### Arc의 성능 비용

원자적 참조 카운팅은 비원자적 `Rc`보다 느리다. 하지만 실제로는 거의 문제가 되지 않는다. 참조 카운팅 자체가 캐시 친화적이고, 멀티코어 환경에서의 오버헤드는 수 나노초 수준이다.

핫패스에서 `Arc::clone()`을 매우 자주 호출하는 경우라면 참조를 미리 받아두는 것이 좋다:

```rust
// 매 요청마다 Arc::clone - 불필요한 원자 연산
async fn handle_request(state_arc: Arc<AppState>) {
    let local_state = state_arc.as_ref(); // 클론 없이 참조
    // ...
}
```

---

## 6.5 Mutex<T>와 RwLock<T> — 가변 공유

공유된 데이터를 수정해야 할 때는 락이 필요하다.

### Mutex<T>

```rust
use std::sync::{Arc, Mutex};

struct SessionManager {
    sessions: Mutex<HashMap<u64, Session>>,
}

impl SessionManager {
    fn new() -> Arc<Self> {
        Arc::new(SessionManager {
            sessions: Mutex::new(HashMap::new()),
        })
    }
    
    fn add_session(&self, session: Session) {
        let mut guard = self.sessions.lock().unwrap();
        guard.insert(session.id, session);
        // guard가 drop되면 락이 해제됨
    }
    
    fn get_session_count(&self) -> usize {
        self.sessions.lock().unwrap().len()
    }
}
```

C#의 `lock` 블록과의 결정적 차이: **데이터 자체가 Mutex 안에 있다.** 락을 잡지 않고 HashMap에 접근하는 것이 타입 시스템상 불가능하다.

```csharp
// C# - 락 없이 접근 가능 (실수 가능!)
Dictionary<int, Session> sessions;
object lockObj = new object();

void AddSession(Session s)
{
    lock(lockObj) { sessions[s.Id] = s; } // 락을 잡아야 하는데...
}

int GetCount()
{
    return sessions.Count; // 락 없이 접근 - 실수!
}
```

```rust
// Rust - 락 없이 접근 자체가 불가능
let mgr = SessionManager::new();
// mgr.sessions.len() // 오류! Mutex를 통하지 않고 접근 불가
// mgr.sessions.lock().unwrap().len() // OK
```

### RwLock<T>

읽기가 쓰기보다 훨씬 많은 경우 `RwLock`이 유리하다.

```rust
use std::sync::RwLock;

struct ConfigStore {
    config: RwLock<HashMap<String, String>>,
}

impl ConfigStore {
    // 여러 독자가 동시에 읽을 수 있음
    fn get(&self, key: &str) -> Option<String> {
        let guard = self.config.read().unwrap();
        guard.get(key).cloned()
    }
    
    // 쓰기는 독점적
    fn set(&self, key: String, value: String) {
        let mut guard = self.config.write().unwrap();
        guard.insert(key, value);
    }
}
```

### Mutex vs RwLock 선택

| 상황 | 권장 |
|------|------|
| 읽기/쓰기 균형이 비슷 | Mutex |
| 읽기 >> 쓰기 (예: 설정 조회) | RwLock |
| 쓰기가 빠른 경우 | Mutex (오버헤드 적음) |
| 게임 상태 (빈번한 수정) | Mutex |

---

## 6.6 비동기 컨텍스트의 Mutex — 핵심 함정

**이것이 게임 서버 개발자들이 가장 많이 실수하는 부분이다.**

### 동기 Mutex를 await 가로질러 사용하면 안 되는 이유

```rust
use std::sync::Mutex; // 표준 라이브러리 Mutex - 비동기에서 위험!

struct Server {
    sessions: Mutex<HashMap<u64, Session>>,
}

impl Server {
    // 위험한 코드!
    async fn handle_packet(&self, id: u64, packet: Packet) {
        let mut guard = self.sessions.lock().unwrap(); // 락 획득
        
        if let Some(session) = guard.get_mut(&id) {
            session.process_packet(packet).await; // await! 락을 들고 있는 채로 await!
        }
        // guard가 여기서 drop - 락 해제
    }
}
```

문제: `await` 지점에서 현재 태스크가 일시 정지(suspend)된다. 이 동안 같은 워커 스레드에서 다른 태스크가 실행될 수 있다. 그 태스크가 같은 `Mutex`를 잠그려 하면 워커 스레드가 블로킹되어 **데드락** 또는 **성능 저하**가 발생한다.

### 해결책 1: tokio::sync::Mutex 사용

```rust
use tokio::sync::Mutex; // tokio의 비동기 Mutex!

struct Server {
    sessions: Mutex<HashMap<u64, Session>>,
}

impl Server {
    async fn handle_packet(&self, id: u64, packet: Packet) {
        let mut guard = self.sessions.lock().await; // 비동기 락 획득
        
        if let Some(session) = guard.get_mut(&id) {
            session.process_packet(packet).await; // 안전
        }
        // guard drop시 비동기적으로 해제
    }
}
```

### 해결책 2: 락 범위 최소화

더 좋은 방법은 락을 최소한의 범위에서만 보유하는 것이다:

```rust
use std::sync::Mutex; // 표준 라이브러리 Mutex도 OK - 단, await 전에 해제

impl Server {
    async fn handle_packet(&self, id: u64, packet: Packet) {
        // 1단계: 락으로 필요한 데이터 가져오기 (동기, 빠름)
        let session_data = {
            let guard = self.sessions.lock().unwrap();
            guard.get(&id).cloned() // 데이터 복사
        }; // guard가 여기서 drop - 락 해제
        
        // 2단계: 데이터로 비동기 작업 수행 (락 없음)
        if let Some(data) = session_data {
            let result = process_with_io(data, packet).await;
            
            // 3단계: 결과를 락으로 저장 (동기, 빠름)
            let mut guard = self.sessions.lock().unwrap();
            if let Some(session) = guard.get_mut(&id) {
                session.apply_result(result);
            }
        }
    }
}
```

### Mutex 사용 결정 트리

```
await를 가로질러 데이터에 접근해야 하는가?
├── 예 → tokio::sync::Mutex 사용
└── 아니오 → 
    락 보유 시간이 짧은가? (<1ms)
    ├── 예 → std::sync::Mutex 또는 parking_lot::Mutex
    └── 아니오 (복잡한 연산) → 락 범위 축소 후 std::sync::Mutex
```

---

## 6.7 RefCell<T> — 런타임 빌림 검사

`RefCell<T>`은 컴파일 타임 빌림 규칙을 런타임으로 미루는 장치다.

```rust
use std::cell::RefCell;

struct GameState {
    players: RefCell<Vec<Player>>,
}

impl GameState {
    fn update(&self) { // &self (불변 참조)인데도 수정 가능
        let mut players = self.players.borrow_mut(); // 런타임에 빌림 검사
        for player in players.iter_mut() {
            player.update();
        }
    }
}
```

**멀티스레드 서버에서는 거의 쓸 일이 없다.** `RefCell`은 `Sync`가 아니어서 스레드 간 공유가 불가능하다. 단일 스레드 게임 로직이나 테스트 코드에서 가끔 유용하다.

---

## 6.8 Weak<T> — 순환 참조 방지

`Arc`만으로 양방향 참조를 만들면 순환 참조가 발생하여 메모리가 해제되지 않는다.

```rust
use std::sync::{Arc, Weak};

struct Room {
    id: u32,
    players: Vec<Arc<Player>>,
}

struct Player {
    id: u64,
    current_room: Option<Weak<Room>>, // Weak 참조 - 역방향
}

// 올바른 패턴:
// Room → Player (Arc: 강한 참조, Room이 Player를 소유)
// Player → Room (Weak: 약한 참조, 순환 없음)
```

`Weak`는 참조 카운트를 증가시키지 않는다. 따라서 Room이 해제되어도 Player의 `Weak<Room>`은 `None`이 된다.

```rust
impl Player {
    fn get_room(&self) -> Option<Arc<Room>> {
        self.current_room.as_ref()?.upgrade() // Weak -> Option<Arc>
    }
    
    fn room_id(&self) -> Option<u32> {
        self.get_room().map(|r| r.id)
    }
}
```

---

## 6.9 동시 자료구조 — 더 나은 선택지들

게임 서버에서 `Arc<Mutex<HashMap>>` 패턴은 단순하지만 경합이 많을 때 성능 병목이 된다. 더 효율적인 대안들이 있다.

### DashMap — 샤딩된 동시 해시맵

```toml
[dependencies]
dashmap = "5"
```

```rust
use dashmap::DashMap;

struct SessionStore {
    sessions: DashMap<u64, Session>,
}

impl SessionStore {
    fn new() -> Arc<Self> {
        Arc::new(SessionStore {
            sessions: DashMap::new(),
        })
    }
    
    fn insert(&self, session: Session) {
        self.sessions.insert(session.id, session);
    }
    
    fn get(&self, id: u64) -> Option<dashmap::mapref::one::Ref<'_, u64, Session>> {
        self.sessions.get(&id)
    }
    
    fn remove(&self, id: u64) -> Option<(u64, Session)> {
        self.sessions.remove(&id)
    }
}
```

DashMap은 내부적으로 여러 샤드(shard)로 분할되어 있어서 다른 키에 대한 동시 접근이 경합 없이 가능하다. `Arc<Mutex<HashMap>>`보다 높은 동시성을 제공한다.

### parking_lot — 표준보다 빠른 동기화 프리미티브

```toml
[dependencies]
parking_lot = "0.12"
```

```rust
use parking_lot::{Mutex, RwLock};

// std::sync보다 더 빠른 구현
// 독성 처리(poisoning)가 없어서 unwrap 불필요
let mu: Mutex<HashMap<u64, Session>> = Mutex::new(HashMap::new());
let guard = mu.lock(); // unwrap 불필요!
```

표준 라이브러리의 `Mutex::lock()`은 `LockResult<MutexGuard<T>>`를 반환하는데, `parking_lot::Mutex::lock()`은 `MutexGuard<T>`를 바로 반환한다. 코드가 더 간결해진다.

### crossbeam 채널 — 락프리 큐

```toml
[dependencies]
crossbeam = "0.8"
```

```rust
use crossbeam::channel;

// 비동기가 필요 없는 스레드 간 통신에 crossbeam 채널 사용
let (tx, rx) = channel::bounded(1000);

// 생산자 스레드
std::thread::spawn(move || {
    loop {
        let packet = receive_raw_packet();
        tx.send(packet).unwrap();
    }
});

// 소비자 스레드  
std::thread::spawn(move || {
    for packet in rx.iter() {
        process_packet(packet);
    }
});
```

---

## 6.10 메모리 관리 도구 선택 가이드

게임 서버를 설계할 때 어떤 도구를 선택할지 결정하는 흐름:

```
이 데이터의 소유자가 하나인가?
├── 예 → 소유권 또는 Box<T>
└── 아니오 (여러 소유자) →
    멀티스레드에서 공유하는가?
    ├── 아니오 (단일 스레드) → Rc<T>
    └── 예 (멀티스레드) →
        수정이 필요한가?
        ├── 아니오 → Arc<T>
        └── 예 →
            비동기 컨텍스트에서 await 가로질러 보유하는가?
            ├── 예 → tokio::sync::Mutex<T>
            └── 아니오 →
                읽기가 압도적으로 많은가?
                ├── 예 → Arc<RwLock<T>>
                └── 아니오 → Arc<Mutex<T>>
                    (또는 DashMap, parking_lot::Mutex 고려)
```

---

## 6.11 객체 풀 패턴 — 게임 서버 최적화

게임 서버에서 자주 생성/해제되는 객체(패킷 버퍼, 임시 벡터 등)에 대해 객체 풀을 사용하면 할당 비용을 줄일 수 있다.

```rust
use std::sync::{Arc, Mutex};

struct ObjectPool<T> {
    objects: Mutex<Vec<T>>,
    create: Box<dyn Fn() -> T + Send + Sync>,
}

impl<T: Send> ObjectPool<T> {
    fn new(create: impl Fn() -> T + Send + Sync + 'static) -> Arc<Self> {
        Arc::new(ObjectPool {
            objects: Mutex::new(Vec::new()),
            create: Box::new(create),
        })
    }
    
    fn acquire(&self) -> PoolGuard<T> {
        let obj = self.objects.lock().unwrap().pop()
            .unwrap_or_else(|| (self.create)());
        PoolGuard { obj: Some(obj), pool: self }
    }
    
    fn release(&self, obj: T) {
        self.objects.lock().unwrap().push(obj);
    }
}

struct PoolGuard<'a, T: Send> {
    obj: Option<T>,
    pool: &'a ObjectPool<T>,
}

impl<'a, T: Send> std::ops::Deref for PoolGuard<'a, T> {
    type Target = T;
    fn deref(&self) -> &T { self.obj.as_ref().unwrap() }
}

impl<'a, T: Send> std::ops::DerefMut for PoolGuard<'a, T> {
    fn deref_mut(&mut self) -> &mut T { self.obj.as_mut().unwrap() }
}

impl<'a, T: Send> Drop for PoolGuard<'a, T> {
    fn drop(&mut self) {
        if let Some(obj) = self.obj.take() {
            self.pool.release(obj);
        }
    }
}

// 사용
let buffer_pool: Arc<ObjectPool<Vec<u8>>> = ObjectPool::new(|| Vec::with_capacity(4096));

async fn handle_packet(pool: Arc<ObjectPool<Vec<u8>>>) {
    let mut buffer = pool.acquire(); // 풀에서 가져옴
    buffer.clear();
    // buffer 사용...
} // buffer가 drop되면 풀로 반환됨
```

---

## 6.12 AI에게 메모리 관리 도구 관련 지시를 내리는 법

### 올바른 스마트 포인터 지정

> "SessionManager는 Arc<Mutex<HashMap<u64, Session>>>으로 구현해줘.
> 비동기 컨텍스트에서 사용하므로 tokio::sync::Mutex를 써줘.
> 여러 태스크에서 SessionManager를 공유하므로 Arc로 감싸야 해."

### 불필요한 Arc 방지

> "이 구조체는 단일 태스크에서만 사용되고 공유가 필요 없어.
> Arc나 Mutex 없이 직접 소유권으로 관리해줘."

### DashMap 활용

> "플레이어 룩업이 매우 빈번하고 동시에 여러 태스크에서 접근해.
> Arc<Mutex<HashMap>> 대신 DashMap을 써서 경합을 줄여줘.
> dashmap 크레이트를 사용해."

### await 관련 Mutex 오류 해결

> "이 코드에서 std::sync::Mutex의 guard를 await 지점을 가로질러 들고 있어.
> 두 가지 방법 중 하나를 선택해줘:
> 1. tokio::sync::Mutex로 교체
> 2. guard 스코프를 await 이전에 닫고 필요한 데이터를 복사해서 사용
> 게임 서버의 핫패스이므로 락 보유 시간이 짧아야 해. 방법 2를 권장."

---

## 6.13 실전 패턴: 게임 서버 상태 관리

모든 도구를 조합한 실전 예시:

```rust
use std::sync::Arc;
use tokio::sync::{Mutex, RwLock};
use dashmap::DashMap;

// 게임 서버 전체 상태
pub struct GameServer {
    // 자주 읽고 가끔 쓰는 설정 - RwLock
    config: Arc<RwLock<ServerConfig>>,
    
    // 빈번한 읽기/쓰기, 키별 독립 접근 - DashMap
    sessions: Arc<DashMap<u64, Arc<Session>>>,
    
    // 룸 관리 - RwLock (룸 추가/삭제 드뭄)
    rooms: Arc<RwLock<HashMap<u32, Arc<Mutex<Room>>>>>,
    
    // 메시지 브로드캐스터 - 읽기 전용 공유
    broadcaster: Arc<Broadcaster>,
}

impl GameServer {
    pub async fn handle_player_join(&self, session_id: u64, player_id: u64) {
        // 세션 업데이트 - DashMap으로 직접 접근
        if let Some(session) = self.sessions.get(&session_id) {
            let mut sess = session.lock().await; // Session 자체는 Mutex로 보호
            sess.set_player_id(player_id);
        }
        
        // 설정 읽기 - RwLock 읽기 락
        let max_players = {
            let config = self.config.read().await;
            config.max_players_per_room
        };
        
        // 적절한 룸 찾기
        let room = {
            let rooms = self.rooms.read().await;
            rooms.values()
                .find(|r| {
                    // 각 룸의 상태를 확인
                    // 실제로는 try_lock 등을 활용해야 함
                    true
                })
                .map(Arc::clone)
        };
        
        if let Some(room_arc) = room {
            let mut room = room_arc.lock().await;
            room.add_player(player_id);
        }
    }
}
```

---

## 6.14 정리

이 챕터의 핵심:

도구 선택 기준 — 소유자 수, 스레드 공유 여부, 수정 필요 여부, 비동기 컨텍스트 여부에 따라 도구를 선택한다.

`Arc<Mutex<T>>` 패턴 — 멀티스레드 가변 공유의 기본 패턴. 단, await를 가로질러 락을 보유하면 안 된다.

비동기 Mutex의 중요성 — `await` 지점을 가로질러 락을 보유해야 한다면 반드시 `tokio::sync::Mutex`를 사용한다.

성능 최적화 — DashMap(샤딩된 HashMap), parking_lot(빠른 락), crossbeam(락프리 채널)으로 표준 도구보다 나은 성능을 낼 수 있다.

AI 지시의 핵심 — 공유 패턴과 비동기 여부를 명확히 지정하면 AI가 올바른 스마트 포인터 조합을 생성한다.

---

*다음 챕터: Chapter 7 — 동시성 모델: Send/Sync는 컴파일러가 검증한다*
