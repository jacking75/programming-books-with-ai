# Chapter 4. 타입 시스템과 트레이트(Trait) — 인터페이스가 아니다

> "트레이트는 인터페이스를 닮았지만, 훨씬 더 강력하고 유연하다. 그리고 그 강력함이 게임 서버 설계를 근본적으로 바꿀 수 있다."

---

## 4.1 Rust 타입 시스템의 특징

Rust의 타입 시스템은 다른 주류 언어들과 여러 면에서 다르다. 가장 중요한 특징들을 먼저 정리한다.

### 대수적 데이터 타입 (Algebraic Data Types)

Rust의 `enum`은 C++의 열거형이나 C#의 enum과 전혀 다르다. Rust의 enum은 각 변형(variant)이 데이터를 가질 수 있는 **합 타입(sum type)**이다.

```rust
// C#의 enum - 단순히 이름 있는 정수
enum Direction { North, South, East, West }

// Rust의 enum - 각 변형이 다른 데이터를 가질 수 있다
enum GameEvent {
    PlayerJoined { player_id: u64, name: String },
    PlayerLeft { player_id: u64, reason: DisconnectReason },
    ChatMessage { sender_id: u64, content: String, timestamp: u64 },
    ItemDropped { item_id: u64, position: Position, quantity: u32 },
    BattleResult { winner_id: u64, loser_id: u64, damage: u32 },
}
```

이것이 `Option<T>`와 `Result<T, E>`의 구현 방식이다. 이 패턴은 "이 함수의 반환값이 어떤 상태들을 가질 수 있는가"를 타입 수준에서 표현한다.

### 구조적 패턴 매칭

enum과 결합하여 모든 경우를 처리하도록 강제하는 `match`가 있다.

```rust
fn process_event(event: GameEvent) {
    match event {
        GameEvent::PlayerJoined { player_id, name } => {
            log::info!("Player {} ({}) joined", name, player_id);
            // 입장 처리
        }
        GameEvent::PlayerLeft { player_id, reason } => {
            cleanup_player(player_id, reason);
        }
        GameEvent::ChatMessage { sender_id, content, timestamp } => {
            broadcast_chat(sender_id, &content, timestamp);
        }
        GameEvent::ItemDropped { item_id, position, quantity } => {
            spawn_item_on_map(item_id, position, quantity);
        }
        GameEvent::BattleResult { winner_id, loser_id, damage } => {
            update_battle_stats(winner_id, loser_id, damage);
        }
        // 새로운 변형을 추가하면 여기서 컴파일 오류 발생
        // 즉 처리를 빠뜨리는 것이 불가능하다
    }
}
```

C++ 또는 C#에서 `switch`에 새 enum 값을 추가했을 때 처리를 빠뜨리는 버그는 Rust에서 컴파일 타임에 차단된다.

---

## 4.2 트레이트란 무엇인가

트레이트(Trait)는 타입이 가질 수 있는 **능력(capability)**을 정의하는 방법이다. C#의 `interface`나 C++의 순수 가상 클래스와 비슷하지만, 중요한 차이점이 있다.

### 기본 트레이트 정의

```rust
// 트레이트 정의: 어떤 능력인지 선언
trait Damageable {
    fn take_damage(&mut self, amount: u32);
    fn current_hp(&self) -> u32;
    fn max_hp(&self) -> u32;
    
    // 기본 구현도 제공할 수 있다
    fn is_alive(&self) -> bool {
        self.current_hp() > 0
    }
    
    fn hp_ratio(&self) -> f32 {
        self.current_hp() as f32 / self.max_hp() as f32
    }
}

// 트레이트 구현
struct Player {
    hp: u32,
    max_hp: u32,
    defense: u32,
}

impl Damageable for Player {
    fn take_damage(&mut self, amount: u32) {
        let actual_damage = amount.saturating_sub(self.defense);
        self.hp = self.hp.saturating_sub(actual_damage);
    }
    
    fn current_hp(&self) -> u32 { self.hp }
    fn max_hp(&self) -> u32 { self.max_hp }
}

struct Monster {
    hp: u32,
    max_hp: u32,
    armor: f32,
}

impl Damageable for Monster {
    fn take_damage(&mut self, amount: u32) {
        let reduced = (amount as f32 * (1.0 - self.armor)) as u32;
        self.hp = self.hp.saturating_sub(reduced);
    }
    
    fn current_hp(&self) -> u32 { self.hp }
    fn max_hp(&self) -> u32 { self.max_hp }
}
```

### C# 인터페이스와의 차이점

| 특성 | C# interface | Rust trait |
|------|-------------|-----------|
| 기존 타입에 사후 구현 | 불가 (타입 수정 필요) | 가능 (고아 규칙 내에서) |
| 기본 구현 | C# 8.0+ 가능 | 항상 가능 |
| 정적 메서드 | 불가 (C# 8.0+ 가능) | 가능 |
| 연산자 오버로딩 | 별도 문법 | 트레이트로 통합 |
| 디스패치 방식 | 항상 동적 (vtable) | 정적 또는 동적 선택 |
| 필드 포함 | 불가 | 불가 (동일) |

---

## 4.3 트레이트 구현의 유연성

트레이트의 강력한 점은 **기존 타입에 사후적으로 능력을 부여**할 수 있다는 것이다.

### 사후 구현 (Blanket Implementation)

```rust
// 서드파티 라이브러리의 타입
struct Vec3 { x: f32, y: f32, z: f32 }

// 내 프로젝트에서 정의한 트레이트
trait SerializeToPacket {
    fn to_bytes(&self) -> Vec<u8>;
    fn from_bytes(data: &[u8]) -> Option<Self> where Self: Sized;
}

// Vec3에 대한 SerializeToPacket 구현
// Vec3를 수정하지 않고 능력을 추가
impl SerializeToPacket for Vec3 {
    fn to_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::with_capacity(12);
        bytes.extend_from_slice(&self.x.to_le_bytes());
        bytes.extend_from_slice(&self.y.to_le_bytes());
        bytes.extend_from_slice(&self.z.to_le_bytes());
        bytes
    }
    
    fn from_bytes(data: &[u8]) -> Option<Self> {
        if data.len() < 12 { return None; }
        Some(Vec3 {
            x: f32::from_le_bytes(data[0..4].try_into().ok()?),
            y: f32::from_le_bytes(data[4..8].try_into().ok()?),
            z: f32::from_le_bytes(data[8..12].try_into().ok()?),
        })
    }
}
```

### 고아 규칙 (Orphan Rule)

사후 구현에는 제약이 있다. **트레이트나 타입 중 하나는 현재 크레이트에서 정의**되어야 한다.

```rust
// 가능: 내 트레이트 + 외부 타입
impl MyTrait for ExternalType { ... }

// 가능: 외부 트레이트 + 내 타입
impl ExternalTrait for MyType { ... }

// 불가: 외부 트레이트 + 외부 타입 (고아 구현)
// impl std::fmt::Display for Vec<i32> { ... } // 오류!
```

이 제약은 서로 다른 라이브러리가 같은 타입에 같은 트레이트를 구현하는 충돌을 방지한다.

---

## 4.4 정적 디스패치 vs 동적 디스패치

Rust에서 트레이트를 사용하는 방법은 두 가지이며, 이 선택이 성능에 영향을 미친다.

### 정적 디스패치: 제네릭과 `impl Trait`

컴파일 타임에 구체 타입이 결정되어 인라인화 가능:

```rust
// 제네릭 방식 - 컴파일 타임에 각 타입에 대해 별도 코드 생성
fn apply_damage<T: Damageable>(target: &mut T, damage: u32) {
    target.take_damage(damage);
    if !target.is_alive() {
        println!("Target defeated!");
    }
}

// impl Trait 문법 - 제네릭의 단순화된 표현
fn apply_damage_simple(target: &mut impl Damageable, damage: u32) {
    target.take_damage(damage);
}
```

**단형화(Monomorphization)**: 컴파일러는 `apply_damage::<Player>`, `apply_damage::<Monster>` 등 구체 타입마다 별도의 코드를 생성한다. 가상 함수 호출 없이 직접 호출이 가능하므로 인라인화가 된다.

```rust
// 컴파일러가 생성하는 것과 유사한 코드
fn apply_damage_player(target: &mut Player, damage: u32) {
    // Player::take_damage가 직접 인라인됨
    let actual = damage.saturating_sub(target.defense);
    target.hp = target.hp.saturating_sub(actual);
    if target.hp == 0 {
        println!("Target defeated!");
    }
}

fn apply_damage_monster(target: &mut Monster, damage: u32) {
    // Monster::take_damage가 직접 인라인됨
    let reduced = (damage as f32 * (1.0 - target.armor)) as u32;
    target.hp = target.hp.saturating_sub(reduced);
    if target.hp == 0 {
        println!("Target defeated!");
    }
}
```

런타임 오버헤드가 없다. C++ 템플릿과 동일한 방식이다.

### 동적 디스패치: `dyn Trait`

런타임에 구체 타입을 결정해야 할 때 사용:

```rust
// Box<dyn Trait> - 힙에 저장된 트레이트 객체
fn get_all_targets(world: &GameWorld) -> Vec<Box<dyn Damageable>> {
    let mut targets: Vec<Box<dyn Damageable>> = Vec::new();
    for player in &world.players {
        targets.push(Box::new(player.clone()));
    }
    for monster in &world.monsters {
        targets.push(Box::new(monster.clone()));
    }
    targets
}

// &dyn Trait - 참조 형태의 트레이트 객체
fn deal_aoe_damage(targets: &mut [&mut dyn Damageable], damage: u32) {
    for target in targets {
        target.take_damage(damage);
    }
}
```

`dyn Trait`는 내부적으로 **vtable 포인터**와 **데이터 포인터**를 가진 Fat Pointer다. 메서드 호출 시 vtable을 통해 함수 포인터를 찾아 호출한다. 이것이 C++의 가상 함수, C#의 인터페이스와 동일한 메커니즘이다.

### 언제 어떤 것을 쓰는가

| 상황 | 권장 방식 | 이유 |
|------|-----------|------|
| 컴파일 타임에 타입이 알려짐 | 제네릭/`impl Trait` | 성능 최적 |
| 이기종 컬렉션 필요 | `Box<dyn Trait>` | 다른 타입들을 같은 컬렉션에 |
| 설정에 따라 구현이 달라짐 | `Box<dyn Trait>` | 런타임 교체 가능 |
| 바이너리 크기 중요 | `dyn Trait` | 단형화 없이 하나의 코드 |
| 핫패스 코드 | 제네릭/`impl Trait` | vtable 오버헤드 없음 |

게임 서버에서는 대부분 제네릭을 선호하지만, 플러그인 시스템이나 이기종 이벤트 큐에서는 `dyn Trait`가 필수적이다.

---

## 4.5 게임 서버에서 반드시 알아야 하는 트레이트들

### Send와 Sync

이 두 트레이트는 Rust 동시성 안전성의 핵심이다.

```rust
// Send: 다른 스레드로 소유권을 안전하게 이동시킬 수 있다
// 대부분의 타입은 Send이다

// Sync: 여러 스레드에서 &T 참조를 공유해도 안전하다
// T가 Sync이면 &T는 Send이다
```

자동으로 구현되는 규칙:
- 모든 필드가 `Send`이면 그 타입도 `Send`
- 모든 필드가 `Sync`이면 그 타입도 `Sync`

`Send`/`Sync`가 아닌 타입들:
- `Rc<T>`: 원자적이지 않은 참조 카운팅 - `Send` 아님
- `RefCell<T>`: 런타임 빌림 검사 - `Sync` 아님
- `*mut T` (원시 포인터): `Send`/`Sync` 아님

```rust
// Tokio 태스크에는 Send가 필요하다
let session_data = Rc::new(vec![1, 2, 3]); // Rc는 Send 아님
tokio::spawn(async move {
    process(session_data).await; // 컴파일 오류!
    // Rc<Vec<i32>>는 Send를 구현하지 않는다
});

// Arc 사용
let session_data = Arc::new(vec![1, 2, 3]); // Arc는 Send + Sync
tokio::spawn(async move {
    process(session_data).await; // OK
});
```

이 오류 메시지를 만나면:
```
error[E0277]: `Rc<...>` cannot be sent between threads safely
```
해결책은 `Rc` → `Arc` 교체다.

### Clone과 Copy

```rust
#[derive(Clone, Debug)]
struct PlayerStats {
    level: u32,
    attack: u32,
    defense: u32,
}

#[derive(Clone, Copy, Debug)]
struct Position {
    x: f32,
    y: f32,
    z: f32,
}
// Position은 Copy이므로 할당 시 자동으로 복사된다
```

언제 `Copy`를 구현하는가:
- 모든 필드가 `Copy`인 타입
- 힙 할당이 없는 타입
- 복사 비용이 매우 낮은 타입 (보통 수십 바이트 이하)

게임 서버에서 `Copy`가 유용한 타입들:
- `Position`, `Vector2/3`
- `PlayerId`, `RoomId`, `ItemId` (newtype 패턴)
- 단순 상태 플래그들

### Debug와 Display

```rust
#[derive(Debug)] // {:?} 포맷 자동 생성
struct Session {
    id: u64,
    player_name: String,
}

use std::fmt;
impl fmt::Display for Session { // {} 포맷 수동 구현
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "Session[{}:{}]", self.id, self.player_name)
    }
}

// 로깅에서 사용
let session = Session { id: 1, player_name: "Alice".to_string() };
println!("{:?}", session);  // Debug: Session { id: 1, player_name: "Alice" }
println!("{}", session);    // Display: Session[1:Alice]
log::info!("Connected: {}", session);
```

게임 서버 로깅에서 `Display`를 잘 구현해 두면 로그 가독성이 크게 향상된다.

### From/Into

타입 변환을 표현하는 표준 트레이트:

```rust
// From을 구현하면 Into가 자동으로 생긴다
#[derive(Debug)]
struct SessionId(u64);

impl From<u64> for SessionId {
    fn from(id: u64) -> Self {
        SessionId(id)
    }
}

// 사용
let id: SessionId = 12345u64.into();
let id = SessionId::from(12345u64);

// 오류 타입 변환에서도 활용
#[derive(Debug)]
enum AppError {
    Io(std::io::Error),
    Database(sqlx::Error),
    ParseInt(std::num::ParseIntError),
}

impl From<std::io::Error> for AppError {
    fn from(e: std::io::Error) -> Self {
        AppError::Io(e)
    }
}

// ? 연산자가 From을 활용한다
async fn process() -> Result<(), AppError> {
    let data = tokio::fs::read("config.json").await?; // io::Error -> AppError 자동 변환
    Ok(())
}
```

### Default

기본값을 가지는 타입을 표현:

```rust
#[derive(Default, Debug)]
struct PlayerConfig {
    max_hp: u32,    // 기본값 0
    move_speed: f32, // 기본값 0.0
    name: String,   // 기본값 ""
}

// 또는 직접 구현
impl Default for PlayerConfig {
    fn default() -> Self {
        PlayerConfig {
            max_hp: 100,
            move_speed: 5.0,
            name: "Unknown".to_string(),
        }
    }
}

// 일부 필드만 커스터마이징
let config = PlayerConfig {
    name: "Alice".to_string(),
    ..PlayerConfig::default() // 나머지는 기본값
};
```

---

## 4.6 트레이트 바운드 (Trait Bounds)

제네릭 코드에서 타입이 특정 트레이트를 구현해야 함을 표현한다.

### 기본 바운드 문법

```rust
// T는 Damageable을 구현해야 한다
fn apply_damage<T: Damageable>(target: &mut T, damage: u32) {
    target.take_damage(damage);
}

// 여러 트레이트 바운드
fn send_packet<T: Serialize + Send + 'static>(
    sender: &mut NetworkSender,
    data: T,
) {
    // T는 Serialize, Send를 모두 구현해야 하고 'static이어야 한다
}

// where 절을 이용한 가독성 향상
fn process_game_event<T, E>(handler: &mut T, event: E) -> Result<(), E::Error>
where
    T: GameEventHandler,
    E: GameEvent + Debug + Clone,
    E::Error: std::error::Error + Send + 'static,
{
    handler.handle(event)
}
```

### 조건부 구현 (Conditional Implementation)

```rust
use std::fmt::Debug;

struct GameLogger<T: Debug> {
    data: T,
}

// T가 Display도 구현하는 경우에만 pretty_print 메서드 제공
impl<T: Debug + std::fmt::Display> GameLogger<T> {
    fn pretty_print(&self) {
        println!("{}", self.data);
    }
}

// T가 Debug만 구현하는 경우에도 debug_print 제공
impl<T: Debug> GameLogger<T> {
    fn debug_print(&self) {
        println!("{:?}", self.data);
    }
}
```

---

## 4.7 트레이트 객체와 게임 서버 이벤트 시스템

게임 서버에서 트레이트 객체(`dyn Trait`)가 매우 유용한 곳이 이벤트 핸들러 시스템이다.

```rust
use std::any::Any;

// 게임 이벤트 트레이트
trait EventHandler: Send + Sync {
    fn handle(&self, event: &dyn GameEvent);
    fn event_type(&self) -> EventType;
}

trait GameEvent: Send + Sync + Any {
    fn event_type(&self) -> EventType;
    fn as_any(&self) -> &dyn Any;
}

// 구체 이벤트들
struct PlayerJoinEvent {
    player_id: u64,
    timestamp: u64,
}

impl GameEvent for PlayerJoinEvent {
    fn event_type(&self) -> EventType { EventType::PlayerJoin }
    fn as_any(&self) -> &dyn Any { self }
}

// 이벤트 버스
struct EventBus {
    handlers: HashMap<EventType, Vec<Box<dyn EventHandler>>>,
}

impl EventBus {
    fn register(&mut self, handler: Box<dyn EventHandler>) {
        self.handlers
            .entry(handler.event_type())
            .or_default()
            .push(handler);
    }
    
    fn dispatch(&self, event: &dyn GameEvent) {
        if let Some(handlers) = self.handlers.get(&event.event_type()) {
            for handler in handlers {
                handler.handle(event);
            }
        }
    }
}
```

---

## 4.8 연산자 오버로딩과 표준 트레이트

Rust의 연산자 오버로딩은 트레이트로 구현된다.

```rust
use std::ops::{Add, Sub, Mul, Neg};

#[derive(Debug, Clone, Copy, PartialEq)]
struct Vec2 {
    x: f32,
    y: f32,
}

impl Add for Vec2 {
    type Output = Vec2;
    fn add(self, other: Vec2) -> Vec2 {
        Vec2 { x: self.x + other.x, y: self.y + other.y }
    }
}

impl Sub for Vec2 {
    type Output = Vec2;
    fn sub(self, other: Vec2) -> Vec2 {
        Vec2 { x: self.x - other.x, y: self.y - other.y }
    }
}

impl Mul<f32> for Vec2 {
    type Output = Vec2;
    fn mul(self, scalar: f32) -> Vec2 {
        Vec2 { x: self.x * scalar, y: self.y * scalar }
    }
}

impl Vec2 {
    fn dot(&self, other: &Vec2) -> f32 {
        self.x * other.x + self.y * other.y
    }
    
    fn length(&self) -> f32 {
        (self.x * self.x + self.y * self.y).sqrt()
    }
    
    fn normalize(&self) -> Vec2 {
        let len = self.length();
        *self * (1.0 / len)
    }
}

// 사용
let pos = Vec2 { x: 1.0, y: 2.0 };
let velocity = Vec2 { x: 3.0, y: 4.0 };
let new_pos = pos + velocity * 0.016; // 16ms 이동
```

### PartialEq, Eq, Hash

컬렉션과 비교에 필수인 트레이트들:

```rust
#[derive(Debug, Clone, PartialEq, Eq, Hash)]
struct PlayerId(u64);

// 이제 HashMap의 키로 사용 가능
let mut player_map: HashMap<PlayerId, Player> = HashMap::new();

// == 연산자로 비교 가능
let id1 = PlayerId(1);
let id2 = PlayerId(1);
assert_eq!(id1, id2);
```

### Ord와 PartialOrd

정렬이 필요한 타입:

```rust
#[derive(Debug, Clone, PartialEq, Eq, PartialOrd, Ord)]
struct Priority(u32);

// 우선순위 큐에서 사용 가능
use std::collections::BinaryHeap;
let mut queue: BinaryHeap<Priority> = BinaryHeap::new();
queue.push(Priority(5));
queue.push(Priority(1));
queue.push(Priority(3));

// 높은 우선순위가 먼저 나온다
while let Some(p) = queue.pop() {
    println!("{:?}", p); // Priority(5), Priority(3), Priority(1)
}
```

---

## 4.9 Iterator 트레이트 심화

`Iterator` 트레이트는 Rust에서 가장 강력한 추상화 중 하나다.

```rust
trait Iterator {
    type Item;
    fn next(&mut self) -> Option<Self::Item>;
    
    // 수십 개의 기본 구현 메서드들
    fn map<B, F>(self, f: F) -> Map<Self, F> where F: FnMut(Self::Item) -> B { ... }
    fn filter<P>(self, predicate: P) -> Filter<Self, P> { ... }
    fn flat_map<U, F>(self, f: F) -> FlatMap<Self, U, F> { ... }
    // ...
}
```

직접 구현하는 예:

```rust
// 게임 월드의 활성 플레이어를 순회하는 이터레이터
struct ActivePlayerIter<'a> {
    players: std::slice::Iter<'a, Player>,
}

impl<'a> Iterator for ActivePlayerIter<'a> {
    type Item = &'a Player;
    
    fn next(&mut self) -> Option<&'a Player> {
        loop {
            match self.players.next() {
                Some(player) if player.is_active() => return Some(player),
                Some(_) => continue, // 비활성 플레이어 스킵
                None => return None,
            }
        }
    }
}

impl GameWorld {
    fn active_players(&self) -> ActivePlayerIter<'_> {
        ActivePlayerIter { players: self.players.iter() }
    }
}

// 사용
let total_damage: u32 = world.active_players()
    .filter(|p| p.is_in_combat())
    .map(|p| p.attack_power)
    .sum();
```

모든 이터레이터 어댑터(map, filter, sum 등)는 **지연 평가(lazy evaluation)**되고 컴파일 타임에 인라인화된다. 중간 컬렉션 생성이 없다.

---

## 4.10 Newtype 패턴

타입 안전성을 높이는 Rust의 관용 패턴:

```rust
// 단순 u64로 플레이어 ID와 세션 ID를 구별하기 어렵다
fn transfer(from: u64, to: u64, amount: u64) { ... } // 인자 순서 실수 가능

// Newtype 패턴으로 구별
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
struct PlayerId(u64);

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
struct SessionId(u64);

#[derive(Debug, Clone, Copy, PartialEq, Eq, PartialOrd, Ord)]
struct Gold(u64);

// 이제 타입이 다르므로 혼동 불가
fn transfer_gold(from: PlayerId, to: PlayerId, amount: Gold) { ... }

// 잘못 넘기면 컴파일 오류
let session_id = SessionId(42);
let player_id = PlayerId(1);
let amount = Gold(100);
transfer_gold(session_id, player_id, amount); // 오류! SessionId != PlayerId
```

---

## 4.11 상속 없는 다형성

Rust에는 상속이 없다. 이것이 처음에는 제약으로 느껴지지만, **컴포지션과 트레이트**로 상속보다 더 유연한 다형성을 달성할 수 있다.

### C++ 상속 기반 설계

```cpp
// C++ - 상속 기반
class GameEntity {
public:
    virtual void update(float dt) = 0;
    virtual void render() = 0;
    Position pos;
};

class Player : public GameEntity {
    void update(float dt) override { ... }
    void render() override { ... }
};

class Monster : public GameEntity { ... };
```

### Rust 트레이트 + 컴포지션

```rust
// Rust - 트레이트와 컴포지션
trait Updatable {
    fn update(&mut self, dt: f32);
}

trait Networked {
    fn send_state_update(&self, buffer: &mut PacketBuffer);
    fn apply_server_update(&mut self, data: &[u8]);
}

trait Collidable {
    fn bounding_box(&self) -> BoundingBox;
    fn on_collision(&mut self, other: &dyn Collidable);
}

// 공유 컴포넌트
#[derive(Debug, Clone)]
struct Transform {
    position: Vec3,
    rotation: Quaternion,
    scale: Vec3,
}

// 플레이어는 모든 능력을 가진다
struct Player {
    transform: Transform,
    stats: PlayerStats,
    input_buffer: InputBuffer,
}

impl Updatable for Player {
    fn update(&mut self, dt: f32) {
        self.process_input(dt);
        self.apply_physics(dt);
    }
}

impl Networked for Player {
    fn send_state_update(&self, buffer: &mut PacketBuffer) {
        buffer.write_transform(&self.transform);
        buffer.write_stats(&self.stats);
    }
    fn apply_server_update(&mut self, data: &[u8]) { ... }
}

impl Collidable for Player {
    fn bounding_box(&self) -> BoundingBox {
        BoundingBox::from_capsule(&self.transform.position, 0.5, 1.8)
    }
    fn on_collision(&mut self, other: &dyn Collidable) { ... }
}

// 정적 장애물은 Updatable이 아니다
struct Wall {
    transform: Transform,
    bounding_box: BoundingBox,
}

impl Collidable for Wall {
    fn bounding_box(&self) -> BoundingBox { self.bounding_box.clone() }
    fn on_collision(&mut self, _other: &dyn Collidable) { /* 벽은 반응 없음 */ }
}
```

이 설계의 장점:
- `Player`와 `Wall`이 공통 베이스 클래스 없이 각자 필요한 능력만 구현
- 새 능력 추가가 기존 타입에 영향 없음
- 다이아몬드 상속 문제 없음

---

## 4.12 Serialize/Deserialize — serde

게임 서버에서 가장 중요한 외부 트레이트 중 하나다.

```rust
use serde::{Serialize, Deserialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PlayerState {
    pub id: u64,
    pub name: String,
    pub level: u32,
    pub position: Position,
    
    // 특정 필드 직렬화 커스터마이징
    #[serde(rename = "hp")]
    pub current_hp: u32,
    
    #[serde(skip_serializing_if = "Option::is_none")]
    pub guild_id: Option<u64>,
    
    #[serde(default)]
    pub buff_ids: Vec<u32>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Position {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

// JSON으로 직렬화
let state = PlayerState { /* ... */ };
let json = serde_json::to_string(&state)?;

// bincode로 직렬화 (바이너리, 게임 서버에 적합)
let bytes = bincode::serialize(&state)?;
let deserialized: PlayerState = bincode::deserialize(&bytes)?;
```

AI에게 지시할 때:
> "플레이어 상태 구조체에 serde의 Serialize/Deserialize를 derive해줘.
> null인 옵션 필드는 직렬화에서 생략하고, 
> guild_id 필드는 클라이언트에게 보낼 때는 포함하지 않도록 serde(skip) 설정해줘."

---

## 4.13 AI에게 트레이트 관련 지시를 내리는 법

### 정적 vs 동적 디스패치 명시

> "이 함수는 핫패스이므로 가상 호출 오버헤드 없이 제네릭으로 작성해줘.
> T: Damageable + Send로 바운드를 걸어줘."

vs

> "이벤트 핸들러 목록은 런타임에 등록/해제가 가능해야 하므로 
> Box<dyn EventHandler>로 저장하는 방식으로 만들어줘."

### 트레이트 구현 방향

> "Player, Monster, Tower에 모두 적용할 수 있는 Attackable 트레이트를 
> 정의해줘. 기본 구현으로 take_damage는 방어력을 뺀 실제 피해를 
> hp에서 빼는 것으로 해줘. 각 타입은 defense() 메서드만 오버라이드하면 되도록."

### Newtype 패턴 요청

> "player_id, session_id, room_id가 모두 u64인데 실수로 뒤섞이지 않도록 
> 각각 PlayerId(u64), SessionId(u64), RoomId(u64) newtype으로 감싸줘.
> Debug, Clone, Copy, PartialEq, Eq, Hash를 derive해줘."

---

## 4.14 정리

이 챕터의 핵심:

트레이트는 인터페이스가 아니다 — 기존 타입에 사후 구현이 가능하고, 정적/동적 디스패치를 선택할 수 있으며, 연산자 오버로딩도 트레이트로 통합되어 있다.

정적 vs 동적 디스패치 — 제네릭/`impl Trait`는 컴파일 타임 단형화로 런타임 오버헤드가 없다. `Box<dyn Trait>`은 vtable을 통한 런타임 디스패치로 유연성을 제공한다.

게임 서버 필수 트레이트 — `Send`/`Sync`(스레드 안전성), `Clone`/`Copy`(복사), `Debug`/`Display`(출력), `From`/`Into`(변환), `Serialize`/`Deserialize`(직렬화).

상속 없는 다형성 — 트레이트와 컴포지션으로 C++ 상속보다 유연하고 명확한 설계가 가능하다.

Newtype 패턴 — 같은 기본 타입을 구별할 때 컴파일 타임 타입 안전성을 제공한다.

---

*다음 챕터: Chapter 5 — 에러 처리: 예외(exception)는 없다*
