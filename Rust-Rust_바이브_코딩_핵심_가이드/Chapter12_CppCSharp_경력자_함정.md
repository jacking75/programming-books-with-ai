# Chapter 12. C++/C# 경력자가 가장 자주 부딪히는 함정 정리

> "20년 이상의 경험이 오히려 방해가 되는 순간들이 있다. 
> 이 챕터는 그 순간들을 미리 정리한다."

---

## 12.1 함정 1: 여러 가변 참조를 동시에 들고 다니는 습관

### C++ 에서의 패턴

```cpp
// C++ - 여러 포인터를 동시에 들고 있는 것이 자연스럽다
class GameWorld {
    Player* getPlayer(int id);
    Room* getRoom(int id);
    
    void processPlayerInRoom(int playerId, int roomId) {
        Player* player = getPlayer(playerId);  // 포인터
        Room* room = getRoom(roomId);          // 포인터
        
        // 두 포인터를 동시에 수정
        player->position = room->spawnPoint;
        room->playerList.push_back(playerId);
    }
};
```

### Rust에서 막히는 이유

```rust
// Rust - 컴파일 오류
fn process_player_in_room(
    world: &mut GameWorld,
    player_id: u64,
    room_id: u32,
) {
    let player = world.get_player_mut(player_id).unwrap(); // &mut Player
    let room = world.get_room_mut(room_id).unwrap();       // 오류!
    // 이미 world를 가변으로 빌렸는데 또 가변 빌림
}
```

```
error[E0499]: cannot borrow `*world` as mutable more than once at a time
```

### 올바른 Rust 접근법

```rust
// 방법 1: 순차적으로 처리 (가장 간단)
fn process_player_in_room(
    world: &mut GameWorld,
    player_id: u64,
    room_id: u32,
) {
    // 먼저 룸에서 스폰 포인트 조회
    let spawn_point = world.get_room(room_id)
        .map(|r| r.spawn_point);  // 값 복사
    
    // 플레이어 수정
    if let Some(point) = spawn_point {
        if let Some(player) = world.get_player_mut(player_id) {
            player.position = point;
        }
    }
    
    // 룸에 플레이어 추가
    if let Some(room) = world.get_room_mut(room_id) {
        room.player_list.push(player_id);
    }
}

// 방법 2: 구조체 분리 (근본적 해결)
struct GameWorld {
    players: HashMap<u64, Player>,
    rooms: HashMap<u32, Room>,
}

impl GameWorld {
    fn process_player_in_room(&mut self, player_id: u64, room_id: u32) {
        // players와 rooms는 다른 필드이므로 동시 빌림 가능
        let spawn_point = self.rooms.get(&room_id)
            .map(|r| r.spawn_point);
        
        if let Some(point) = spawn_point {
            if let Some(player) = self.players.get_mut(&player_id) {
                player.position = point;
            }
            
            if let Some(room) = self.rooms.get_mut(&room_id) {
                room.player_list.push(player_id);
            }
        }
    }
}
```

**핵심 인식 전환**: "한 객체를 여러 곳에서 동시에 바꾸려는 설계 자체를 다시 보아야 한다."

---

## 12.2 함정 2: C#의 GC를 믿고 객체를 여기저기 던지는 패턴

### C#에서의 패턴

```csharp
// C# - 참조 타입은 자동으로 공유된다
class SessionManager {
    Dictionary<int, Session> sessions = new();
    
    public Session GetSession(int id) => sessions[id]; // 참조 반환
    
    public void ProcessAll() {
        foreach (var session in sessions.Values) {
            HandleSession(session); // Session 참조 전달 - GC가 관리
        }
    }
}
```

```csharp
// 여러 곳에서 같은 객체를 참조해도 GC가 알아서 처리
var session = manager.GetSession(1);
processor1.Use(session);  // 같은 객체
processor2.Use(session);  // 같은 객체
logger.Log(session);      // 같은 객체
```

### Rust에서의 대응

```rust
// 방법 1: 빌림 - 소유권 없이 참조만 전달 (가장 자연스러운 방법)
fn process_session(session_manager: &SessionManager, id: u64) {
    if let Some(session) = session_manager.get_session(id) {
        // session은 &Session - 빌림, 소유권 이전 없음
        process(&session);
        log_session(&session);
    }
}

// 방법 2: Arc - 공유 소유권 (여러 태스크에서 공유)
use std::sync::Arc;

struct SessionManager {
    sessions: HashMap<u64, Arc<Session>>,
}

async fn process_in_parallel(
    mgr: Arc<SessionManager>,
    id: u64,
) {
    let session = mgr.sessions.get(&id).map(Arc::clone);
    
    if let Some(session) = session {
        let s1 = Arc::clone(&session);
        let s2 = Arc::clone(&session);
        
        // 두 태스크가 같은 Session을 공유
        let t1 = tokio::spawn(async move { process_combat(s1).await });
        let t2 = tokio::spawn(async move { process_inventory(s2).await });
        
        let _ = tokio::join!(t1, t2);
    }
}

// 방법 3: 채널 - 소유권 이전 (액터 패턴)
async fn actor_pattern(
    session: Session,  // 소유권
    tx: mpsc::Sender<Event>,
) {
    // session의 소유권이 이 태스크에 있음
    // 다른 태스크와는 채널로 통신
    loop {
        if let Some(event) = session.next_event().await {
            tx.send(event).await.ok();
        }
    }
}
```

**핵심 인식 전환**: 소유권을 명확히 한 곳에 두고, 나머지는 빌리거나 채널로 메시지를 주고받는 액터 스타일.

---

## 12.3 함정 3: 상속 기반 설계

### C++/C# 에서의 패턴

```cpp
// C++ - 상속 계층
class Entity {
public:
    virtual void Update(float dt) = 0;
    virtual void Render() = 0;
    Position pos;
    int hp;
};

class Character : public Entity {
public:
    virtual void Attack() = 0;
    int attack_power;
};

class Player : public Character {
public:
    void Update(float dt) override { ... }
    void Render() override { ... }
    void Attack() override { ... }
    InputBuffer input;
};

class NPC : public Character {
public:
    void Update(float dt) override { ... }
    void Render() override { ... }
    void Attack() override { ... }
    AIBehavior ai;
};
```

### Rust에서의 대응

```rust
// 트레이트로 능력 정의
trait Updatable {
    fn update(&mut self, dt: f32);
}

trait Renderable {
    fn render_state(&self) -> RenderState;
}

trait Attackable {
    fn attack_power(&self) -> u32;
    fn attack(&mut self, target: &mut dyn Damageable);
}

trait Damageable {
    fn take_damage(&mut self, amount: u32);
    fn current_hp(&self) -> u32;
}

// 공유 컴포넌트 (상속 없이 컴포지션)
#[derive(Debug, Clone)]
struct Transform {
    position: Vec3,
    rotation: f32,
}

#[derive(Debug, Clone)]
struct Health {
    current: u32,
    max: u32,
}

// Player는 필요한 능력만 선택적으로 구현
struct Player {
    transform: Transform,
    health: Health,
    attack: u32,
    input: InputBuffer,
}

impl Updatable for Player {
    fn update(&mut self, dt: f32) {
        self.process_input(dt);
        // ...
    }
}

impl Damageable for Player {
    fn take_damage(&mut self, amount: u32) {
        self.health.current = self.health.current.saturating_sub(amount);
    }
    fn current_hp(&self) -> u32 { self.health.current }
}

impl Attackable for Player {
    fn attack_power(&self) -> u32 { self.attack }
    fn attack(&mut self, target: &mut dyn Damageable) {
        target.take_damage(self.attack);
    }
}

// NPC는 다른 조합
struct NPC {
    transform: Transform,
    health: Health,
    attack: u32,
    ai: AIBehavior,
}

impl Updatable for NPC {
    fn update(&mut self, dt: f32) {
        self.ai.think(dt, &self.transform);
    }
}

// Wall은 Updatable이 없음 - 필요한 것만
struct Wall {
    transform: Transform,
    health: Health,
}

impl Damageable for Wall {
    fn take_damage(&mut self, amount: u32) {
        self.health.current = self.health.current.saturating_sub(amount);
    }
    fn current_hp(&self) -> u32 { self.health.current }
}
```

**핵심 인식 전환**: "Player가 Character를 상속한다"가 아니라 "Player는 Updatable, Attackable, Damageable 트레이트를 구현한다".

---

## 12.4 함정 4: null 대신 Option<T>

### C/C++에서의 패턴

```cpp
// C++ - null 반환
Player* getPlayer(int id) {
    auto it = players.find(id);
    return it != players.end() ? &it->second : nullptr;
}

// 사용 - null 체크를 잊기 쉽다
Player* p = getPlayer(id);
p->attack(); // nullptr이면 크래시!
```

### Rust에서의 대응

```rust
// Rust - Option 반환
fn get_player(&self, id: u64) -> Option<&Player> {
    self.players.get(&id)
}

// 강제로 처리해야 함
if let Some(player) = world.get_player(id) {
    player.attack();
} else {
    log::warn!("Player {} not found", id);
}

// 또는 체이닝
world.get_player(id)
    .map(|p| p.attack_power)
    .unwrap_or(0);

// 또는 ? 연산자 (Result와 결합)
fn get_player_attack(world: &GameWorld, id: u64) -> Result<u32, GameError> {
    let player = world.get_player(id)
        .ok_or(GameError::PlayerNotFound(id))?;
    Ok(player.attack_power)
}
```

**시그니처에서 null 가능성이 보인다**: `Option<&Player>`를 반환하는 함수라는 것을 보는 순간 "없을 수 있다"는 것을 알 수 있다. C++에서 주석으로 표현하던 것이 타입으로 표현된다.

---

## 12.5 함정 5: 문자열 두 가지

C++의 `std::string`, C#의 `string`에 익숙한 개발자에게 Rust의 두 가지 문자열이 혼란스럽다.

### String vs &str

```
String: 소유된 문자열 (힙 할당, 가변, 크기 가변)
&str:   문자열 슬라이스 (빌림, 불변, 참조)
```

```rust
// String - 소유, 힙에 저장
let owned: String = String::from("hello");
let owned: String = "hello".to_string();
let owned: String = format!("hello {}", "world");

// &str - 슬라이스, 빌림
let slice: &str = "hello";          // 'static 문자열 리터럴
let slice: &str = &owned;           // String의 슬라이스
let slice: &str = &owned[0..5];     // 부분 슬라이스
```

### 함수 인자 규칙

```rust
// 읽기만 한다면 &str을 받아야 한다
// String도 &str으로 역참조되므로 두 타입 모두 받을 수 있음
fn print_name(name: &str) {
    println!("{}", name);
}

let owned = String::from("Alice");
print_name(&owned);   // String -> &str 자동 변환
print_name("Bob");    // 리터럴도 OK

// 소유권이 필요하다면 String
fn store_name(name: String) {
    // name을 어딘가에 저장
}

// 반환 시 - String 반환 (소유권 이전)
fn get_player_name(player: &Player) -> String {
    player.name.clone()
}

// 또는 참조 반환 (Player 라이프타임에 묶임)
fn get_player_name_ref(player: &Player) -> &str {
    &player.name
}
```

### 게임 서버에서의 문자열 패턴

```rust
struct Player {
    name: String,   // 소유된 문자열
    class: String,
}

// 자주 쓰는 패턴
impl Player {
    fn name(&self) -> &str { &self.name }  // 참조 반환
    fn class(&self) -> &str { &self.class }
    
    fn display_name(&self) -> String {
        format!("[{}] {}", self.class, self.name) // 새 String 생성
    }
}

// 로그에서
log::info!("Player {} logged in", player.name()); // &str - 빌림
```

---

## 12.6 함정 6: 순환 참조와 Weak

### C++ 스마트 포인터의 순환 참조

```cpp
// C++ - shared_ptr 순환 참조 메모리 누수
struct Player {
    std::shared_ptr<Room> current_room; // 강한 참조
};

struct Room {
    std::vector<std::shared_ptr<Player>> players; // 강한 참조
};

// Player -> Room -> Player (순환!) - 메모리 해제 안 됨!
```

### Rust에서의 Arc 순환 참조

Rust도 `Arc`로 같은 문제를 만들 수 있다:

```rust
// Rust - Arc 순환 참조 메모리 누수
struct Player {
    room: Arc<Room>,  // 강한 참조
}

struct Room {
    players: Vec<Arc<Player>>, // 강한 참조
}

// Room과 Player가 서로를 Arc로 참조하면 해제되지 않음
```

### 올바른 해결: Weak

```rust
use std::sync::{Arc, Weak};

struct Player {
    id: u64,
    room: Option<Weak<Room>>,  // 약한 참조 (역방향)
}

struct Room {
    id: u32,
    players: Vec<Arc<Player>>, // 강한 참조 (정방향)
}

impl Player {
    fn get_room(&self) -> Option<Arc<Room>> {
        self.room.as_ref()?.upgrade() // Weak -> Option<Arc>
    }
    
    fn room_id(&self) -> Option<u32> {
        self.get_room().map(|r| r.id)
    }
}

// 설계 원칙:
// 부모 → 자식: Arc (강한 참조)
// 자식 → 부모: Weak (약한 참조)
// Room이 Player를 소유, Player는 Room을 Weak으로만 참조
```

### 더 나은 대안: ID 기반 참조

순환 참조 자체를 피하는 것이 가장 좋다:

```rust
// 순환 없음 - ID로 간접 참조
struct Player {
    id: u64,
    room_id: Option<u32>,  // 소유 없음, ID만
}

struct Room {
    id: u32,
    player_ids: Vec<u64>, // 소유 없음, ID만
}

// 필요할 때 인덱스를 통해 접근
fn player_room<'a>(player: &Player, rooms: &'a HashMap<u32, Room>) -> Option<&'a Room> {
    player.room_id.and_then(|id| rooms.get(&id))
}
```

---

## 12.7 함정 7: 클로저와 캡처

### C++ 람다와의 차이

```cpp
// C++ - 캡처 방식 명시
auto lambda = [&]() { use(x); };  // 참조 캡처
auto lambda = [=]() { use(x); };  // 값 캡처
auto lambda = [x]() { use(x); };  // x만 값 캡처
```

### Rust 클로저

```rust
let x = 5;
let add_x = |n| n + x;  // x를 캡처 (불변 참조)
println!("{}", add_x(3)); // OK - x는 여전히 유효
```

```rust
let mut count = 0;
let mut increment = || { count += 1; }; // 가변 캡처
increment(); // count = 1
increment(); // count = 2
// println!("{}", count); // 오류! count를 가변으로 빌린 동안
```

### move 클로저

```rust
let data = vec![1, 2, 3];

// move 없이 - 스레드보다 data가 짧게 살면 오류
// thread::spawn(|| println!("{:?}", data)); // 오류!

// move로 소유권 이전
thread::spawn(move || {
    println!("{:?}", data); // data의 소유권이 스레드로
});
// data는 더 이상 사용 불가
```

Tokio 비동기 태스크에서:

```rust
let session_id = 42u64;
let name = "Alice".to_string();

// 클로저가 외부 변수를 캡처
tokio::spawn(async move {
    // session_id는 Copy이므로 복사됨
    // name의 소유권은 이 태스크로 이동
    handle_session(session_id, name).await;
});
```

---

## 12.8 함정 8: Iterator 소비와 재사용

```rust
let v = vec![1, 2, 3, 4, 5];
let iter = v.iter();

// 이터레이터는 소비된다
let sum: i32 = iter.sum(); // iter 소비됨
// let max = iter.max(); // 오류! iter는 이미 소비됨
```

### 해결책: 이터레이터를 여러 번 만들기

```rust
let v = vec![1, 2, 3, 4, 5];

// 새 이터레이터 생성
let sum: i32 = v.iter().sum();       // 첫 번째 이터레이터
let max = v.iter().max();            // 두 번째 이터레이터

// 또는 수집 후 재사용
let filtered: Vec<&i32> = v.iter().filter(|&&x| x > 2).collect();
let filtered_sum: i32 = filtered.iter().map(|&&x| x).sum();
let filtered_max = filtered.iter().max();
```

---

## 12.9 함정 9: 정수 오버플로 동작

### C++에서의 정수 오버플로

```cpp
// C++ - 부호 있는 정수 오버플로는 UB
int x = INT_MAX;
int y = x + 1; // 미정의 동작!

// unsigned는 wrap-around
unsigned u = UINT_MAX;
unsigned v = u + 1; // 0으로 wrap
```

### Rust에서의 정수 오버플로

```rust
// Rust - 디버그 빌드에서는 패닉
let x: i32 = i32::MAX;
let y = x + 1; // 디버그: 패닉! 릴리즈: wrap-around

// 명시적 제어
let z = x.wrapping_add(1);         // 항상 wrap
let z = x.checked_add(1);         // None 반환 (오버플로 시)
let z = x.saturating_add(1);      // MAX에서 멈춤
let z = x.overflowing_add(1);     // (결과, 오버플로_여부) 반환
```

게임 서버에서:

```rust
// HP 계산 - saturating 사용 권장
fn take_damage(&mut self, damage: u32) {
    self.hp = self.hp.saturating_sub(damage); // 0 이하로 내려가지 않음
}

fn heal(&mut self, amount: u32) {
    self.hp = self.hp.saturating_add(amount).min(self.max_hp); // max_hp 초과 없음
}

// 골드 계산 - checked 사용 권장
fn spend_gold(&mut self, amount: u64) -> Result<(), GameError> {
    self.gold = self.gold.checked_sub(amount)
        .ok_or(GameError::InsufficientGold { 
            needed: amount, 
            available: self.gold 
        })?;
    Ok(())
}
```

---

## 12.10 함정 10: 타입 추론 의존

Rust는 강력한 타입 추론을 제공하지만, 때로는 명시적으로 지정해야 한다.

```rust
// 추론 성공
let x = 5;           // i32 추론
let s = "hello";     // &str 추론
let v = vec![1, 2];  // Vec<i32> 추론

// 추론 실패 - 명시 필요
let mut map = HashMap::new(); // 오류! 타입 모름
map.insert("key", 1);        // 이 시점에 추론 가능하지만...

// 명시적 타입 지정 방법
let mut map: HashMap<&str, i32> = HashMap::new();
// 또는
let mut map = HashMap::<&str, i32>::new();
// 또는
let mut map = HashMap::new();
map.insert("key", 1_i32); // 터보피시
```

게임 서버에서 흔히 발생하는 경우:

```rust
// collect()는 타겟 타입을 명시해야 함
let player_ids: Vec<u64> = world.players.keys().copied().collect();
// 또는
let player_ids = world.players.keys().copied().collect::<Vec<u64>>();

// sum()의 타입
let total_score: u64 = world.players.values()
    .map(|p| p.score as u64)
    .sum();
```

---

## 12.11 함정 11: impl 블록 분리

C++/C#에서는 클래스 정의에 메서드가 모두 포함된다. Rust에서는 데이터와 메서드가 분리된다.

```rust
// 데이터 정의 (struct)
struct GameServer {
    players: HashMap<u64, Player>,
    config: Config,
}

// 메서드 구현 (impl)
impl GameServer {
    fn new(config: Config) -> Self { /* ... */ }
    fn add_player(&mut self, player: Player) { /* ... */ }
}

// 트레이트 구현 (또 다른 impl)
impl Drop for GameServer {
    fn drop(&mut self) {
        log::info!("GameServer shutting down");
    }
}

impl std::fmt::Display for GameServer {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        write!(f, "GameServer({} players)", self.players.len())
    }
}

// 여러 impl 블록이 가능 (파일을 나눌 때 유용)
impl GameServer {
    // 네트워크 관련 메서드들
    async fn accept_connection(&self) { /* ... */ }
    async fn broadcast(&self, msg: &[u8]) { /* ... */ }
}

impl GameServer {
    // 게임 로직 관련 메서드들
    fn process_tick(&mut self) { /* ... */ }
    fn handle_combat(&mut self, attacker: u64, target: u64) { /* ... */ }
}
```

---

## 12.12 함정 12: 파일/모듈 구조

### Rust의 모듈 시스템

```
C#의 네임스페이스와 다름:
- Rust 모듈은 파일 시스템과 대응됨
- `mod` 키워드로 명시적으로 선언
```

```
src/
├── main.rs           # 루트 모듈
├── lib.rs            # 라이브러리 루트 (있는 경우)
├── session/
│   ├── mod.rs        # session 모듈 (또는 session.rs)
│   ├── handler.rs    # session::handler 모듈
│   └── state.rs      # session::state 모듈
├── game/
│   ├── mod.rs
│   ├── world.rs
│   └── player.rs
└── config.rs
```

```rust
// main.rs
mod session;    // src/session/mod.rs 또는 src/session.rs
mod game;       // src/game/mod.rs
mod config;     // src/config.rs

use session::SessionManager;
use game::GameWorld;
```

```rust
// src/session/mod.rs
pub mod handler;  // src/session/handler.rs
pub mod state;    // src/session/state.rs

pub use handler::SessionHandler;  // 재공개 (re-export)
pub use state::SessionState;
```

### pub 가시성 규칙

```rust
// private (기본값) - 같은 모듈 내에서만
struct InternalState { ... }

// pub - 외부에서 접근 가능
pub struct PublicApi { ... }

// pub(crate) - 크레이트 내에서만
pub(crate) struct InternalApi { ... }

// pub(super) - 부모 모듈에서 접근 가능
pub(super) struct SiblingAccess { ... }
```

---

## 12.13 정리 표

| 상황 | C++/C# 습관 | Rust 올바른 방법 |
|------|------------|-----------------|
| 여러 곳에서 수정 | 여러 포인터/참조 | 순차 처리 또는 구조체 분리 |
| 공유 객체 | GC 참조 | Arc<Mutex<T>> 또는 채널 |
| 상속 | class 상속 | 트레이트 + 컴포지션 |
| null 반환 | nullptr, null | Option<T> |
| 순환 참조 | shared_ptr 양방향 | Arc(한 방향) + Weak(역방향) |
| 문자열 수정 | 한 가지 타입 | String(소유) vs &str(빌림) |
| 정수 오버플로 | 암묵적 wrap/UB | saturating_, checked_, wrapping_ |
| 이터레이터 재사용 | for 루프 재실행 | 새 이터레이터 생성 |

---

## 12.14 AI에게 지시할 때 피해야 할 패턴

```
X 나쁜 지시:
"플레이어와 룸 두 개를 동시에 수정하는 함수를 만들어줘"

O 좋은 지시:
"플레이어의 룸 이동을 처리하는 함수를 만들어줘.
PlayerManager와 RoomManager 두 구조체에 분리된 데이터를 수정해야 하는데,
Rust 빌림 규칙을 지키면서 구현해줘. 
플레이어 ID와 룸 ID만 저장하고 직접 참조는 하지 않는 방식으로."
```

```
X 나쁜 지시:
"이 상속 계층을 Rust로 변환해줘"

O 좋은 지시:
"C++ 상속 계층을 Rust 트레이트와 컴포지션으로 재설계해줘.
Entity, Character, Player 계층 대신,
Updatable, Renderable, Damageable 트레이트를 정의하고
각 타입(Player, Monster, NPC)이 필요한 트레이트만 구현하도록 해줘."
```

---

*다음 챕터: Chapter 13 — AI에게 Rust 코드를 지시할 때의 체크리스트*
