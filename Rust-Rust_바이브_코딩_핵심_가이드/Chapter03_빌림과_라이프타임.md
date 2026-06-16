# Chapter 3. 빌림(Borrowing)과 라이프타임 — Rust의 두 번째 기둥

> "빌림은 소유권 없이 값을 사용하는 방법이다. 라이프타임은 그 빌림이 얼마나 오래 유효한지를 추적한다."

---

## 3.1 빌림의 개념

소유권만으로 모든 문제를 해결하면 코드가 매우 불편해진다. 함수에 값을 넘길 때마다 소유권이 이동하고, 다시 돌려받아야 사용할 수 있다면 실용적이지 않다.

```rust
// 소유권만 사용할 경우 - 불편한 코드
fn get_player_name(player: Player) -> (Player, String) {
    let name = player.name.clone();
    (player, name) // player를 돌려줘야 한다!
}

fn main() {
    let player = Player::new("Alice");
    let (player, name) = get_player_name(player);
    // player를 다시 받아야 계속 사용 가능
    println!("{} has name {}", player.id, name);
}
```

이 문제를 해결하는 것이 **빌림(Borrowing)**이다. 소유권을 이전하지 않고 값에 대한 참조(reference)를 만드는 방법이다.

---

## 3.2 참조의 두 가지 종류

Rust의 참조는 두 가지뿐이며, 이 두 가지의 규칙이 데이터 레이스를 컴파일 타임에 차단하는 핵심이다.

### 불변 참조 `&T`

```rust
fn get_player_name(player: &Player) -> &str {
    &player.name // 참조를 반환
}

fn main() {
    let player = Player::new("Alice");
    let name = get_player_name(&player); // 참조를 빌려줌
    println!("{}", name);
    println!("{}", player.id); // player는 여전히 유효!
}
```

불변 참조의 규칙:
- 동시에 여러 개의 불변 참조가 존재할 수 있다
- 불변 참조가 살아있는 동안 원본 값은 수정될 수 없다
- 읽기 전용 접근만 가능하다

```rust
let player = Player::new("Alice");

let ref1 = &player;
let ref2 = &player;
let ref3 = &player;
// 세 개의 불변 참조가 동시에 존재해도 된다
println!("{} {} {}", ref1.name, ref2.name, ref3.name); // OK
```

### 가변 참조 `&mut T`

```rust
fn level_up(player: &mut Player) {
    player.level += 1;
    player.max_hp += 50;
}

fn main() {
    let mut player = Player::new("Alice");
    level_up(&mut player);
    println!("{} is now level {}", player.name, player.level);
}
```

가변 참조의 규칙:
- 특정 시점에 하나의 가변 참조만 존재할 수 있다
- 가변 참조가 살아있는 동안 다른 어떤 참조도(불변 포함) 존재할 수 없다
- 읽기와 쓰기 모두 가능하다

```rust
let mut player = Player::new("Alice");

let ref1 = &mut player;
// let ref2 = &mut player; // 오류! 두 번째 가변 참조 불가
// let ref3 = &player;     // 오류! 가변 참조 중에 불변 참조도 불가
ref1.level += 1;
```

---

## 3.3 빌림 규칙의 의미

이 두 가지 빌림 규칙은 언뜻 제약처럼 보이지만, 이것이 바로 **독자-저자 락(Reader-Writer Lock)을 타입 시스템 수준에서 강제**하는 것이다.

```
규칙 요약:
- 불변 참조(&T): 여러 개 동시 가능 (= 여러 독자 허용)
- 가변 참조(&mut T): 단 하나만 가능 (= 독점 쓰기)
- 불변 참조와 가변 참조는 동시에 존재 불가 (= 쓰는 중에 읽기 금지)
```

이것이 멀티스레드 게임 서버에서 데이터 레이스를 원천 차단하는 원리다. 물론 이 규칙은 단일 스레드 내의 참조에 대한 것이고, 스레드 간 공유는 `Send`/`Sync`와 `Mutex`/`RwLock`이 담당한다 (Chapter 7에서 다룬다).

### 실질적 예시: 왜 이 규칙이 필요한가

C++에서 실제로 발생한 버그 시나리오:

```cpp
// C++ - 데이터 레이스 (컴파일 성공, 런타임에 문제 발생)
std::unordered_map<int, std::string> player_names;

// 스레드 1
for (auto& [id, name] : player_names) {
    process(name); // 맵을 읽고 있다
}

// 스레드 2 (동시에 실행)
player_names[new_id] = "Bob"; // 맵을 수정한다 - 이터레이터 무효화!
```

이 코드는 C++에서 컴파일되고 가끔은 정상 동작하지만, 두 스레드가 동시에 실행되면 정의되지 않은 동작이다. Rust에서는:

```rust
// Rust - 컴파일 오류로 차단
let mut map: HashMap<i32, String> = HashMap::new();

let iter = map.iter(); // 불변 참조로 반복자 생성
map.insert(5, "Bob".to_string()); // 오류! 불변 참조 중에 수정 불가
for (k, v) in iter { println!("{}: {}", k, v); }
```

---

## 3.4 라이프타임 이란

라이프타임(Lifetime)은 "참조가 유효한 시간의 범위"를 나타낸다. Rust 컴파일러는 모든 참조에 대해 라이프타임을 추적하여 **댕글링 참조(dangling reference)**가 발생하지 않도록 보장한다.

### 댕글링 참조란

C++에서 흔한 버그:

```cpp
// C++ - 댕글링 포인터 (컴파일 성공, 런타임 오류)
const std::string* get_session_name() {
    std::string name = "Alice"; // 스택에 할당
    return &name; // 로컬 변수의 주소를 반환 - 위험!
} // name이 여기서 소멸됨. 반환된 포인터는 댕글링 포인터!

void process() {
    const std::string* ptr = get_session_name();
    std::cout << *ptr; // 미정의 동작!
}
```

Rust에서는 이것이 컴파일 타임에 차단된다:

```rust
// Rust - 컴파일 오류
fn get_session_name() -> &String {
    let name = String::from("Alice"); // 함수 내 로컬 변수
    &name // 오류! 함수가 끝나면 name이 해제되므로 참조 반환 불가
}
```

```
error[E0106]: missing lifetime specifier
 --> src/main.rs:1:27
  |
1 | fn get_session_name() -> &String {
  |                           ^ expected named lifetime parameter
```

### 라이프타임의 작동 원리

라이프타임은 `'a`, `'b`와 같이 아포스트로피로 시작하는 이름으로 표기된다. 대부분의 경우 **컴파일러가 자동으로 추론(elision)**하므로 명시적으로 쓸 일이 많지 않다.

```rust
// 라이프타임을 명시하지 않아도 컴파일러가 추론하는 경우
fn first_word(s: &str) -> &str {
    // s와 반환 참조는 같은 라이프타임을 가진다고 컴파일러가 추론
    let bytes = s.as_bytes();
    for (i, &item) in bytes.iter().enumerate() {
        if item == b' ' {
            return &s[0..i];
        }
    }
    &s[..]
}
```

하지만 컴파일러가 추론할 수 없는 경우에는 명시해야 한다:

```rust
// 두 참조 중 어느 것의 라이프타임으로 반환할지 모호한 경우
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() > y.len() { x } else { y }
}
```

여기서 `'a`는 "x와 y 중 더 짧은 라이프타임"을 나타낸다. 반환 참조는 x와 y 모두가 유효한 동안만 유효하다.

---

## 3.5 라이프타임 규칙 (Elision Rules)

라이프타임 생략 규칙은 컴파일러가 명시적 어노테이션 없이 라이프타임을 추론하는 방식이다. 세 가지 규칙이 있다.

**규칙 1**: 함수 파라미터의 각 참조에 별도 라이프타임이 부여된다.

```rust
fn foo(x: &i32, y: &i32)
// 실제로는:
fn foo<'a, 'b>(x: &'a i32, y: &'b i32)
```

**규칙 2**: 입력 파라미터가 정확히 하나인 경우, 그 라이프타임이 출력에 적용된다.

```rust
fn first_word(s: &str) -> &str
// 실제로는:
fn first_word<'a>(s: &'a str) -> &'a str
```

**규칙 3**: 메서드의 경우, `&self` 또는 `&mut self`의 라이프타임이 출력에 적용된다.

```rust
impl Player {
    fn get_name(&self) -> &str
    // 실제로는:
    fn get_name<'a>(&'a self) -> &'a str
}
```

이 세 규칙으로 대부분의 경우가 커버된다. 규칙 적용 후에도 모호함이 남는 경우만 명시적 어노테이션이 필요하다.

---

## 3.6 구조체 내 참조와 라이프타임

구조체 안에 참조를 두려면 라이프타임 어노테이션이 필수다. 이것이 Rust에서 가장 복잡하게 느껴지는 부분 중 하나다.

```rust
// 구조체 안에 참조를 두는 경우
struct PacketView<'a> {
    data: &'a [u8], // 외부 버퍼를 빌림
    offset: usize,
}

impl<'a> PacketView<'a> {
    fn new(data: &'a [u8]) -> Self {
        PacketView { data, offset: 0 }
    }
    
    fn read_u32(&mut self) -> u32 {
        let value = u32::from_be_bytes(
            self.data[self.offset..self.offset+4].try_into().unwrap()
        );
        self.offset += 4;
        value
    }
}

fn parse_packet(buffer: &[u8]) -> PacketView {
    PacketView::new(buffer)
    // PacketView는 buffer보다 오래 살 수 없다
}
```

`PacketView<'a>`의 라이프타임 어노테이션 `'a`는 "PacketView가 빌린 슬라이스가 PacketView 자신보다 오래 살아있어야 한다"는 제약을 표현한다.

### 왜 구조체 내 참조가 어려운가

```rust
// 의도: Session이 자신의 버퍼를 참조로 갖고 싶다
struct Session {
    buffer: Vec<u8>,
    view: &[u8], // 오류! 무슨 라이프타임?
                 // buffer는 이 Session 안에 있는데,
                 // 자신의 필드를 참조하는 것은 자기 참조(self-referential)
}
```

이것은 **자기 참조 구조체(self-referential struct)** 문제다. Rust에서는 일반적인 방법으로 만들 수 없다. 이유는 구조체가 이동될 때 내부 참조가 무효화될 수 있기 때문이다.

---

## 3.7 자기 참조 구조체 우회 패턴

자기 참조 구조체는 Rust에서 의도적으로 어렵게 만들어 두었다. 이는 버그의 원인이 되기 쉬운 패턴이기 때문이다. 대신 다음과 같은 우회 패턴을 사용한다.

### 패턴 1: 인덱스/ID 기반 간접 참조

게임 서버에서 가장 많이 사용하는 패턴이다.

```rust
// 나쁜 패턴 (C++ 스타일 포인터 참조)
struct Room {
    id: u32,
    owner: &Player,   // 컴파일 오류!
    players: Vec<&Player>, // 컴파일 오류!
}

// 좋은 패턴 (ID 기반 간접 참조)
struct Room {
    id: u32,
    owner_id: u64,          // 플레이어 ID만 보관
    player_ids: Vec<u64>,   // 플레이어 ID 목록
}

impl Room {
    fn get_owner<'a>(&self, players: &'a PlayerStore) -> Option<&'a Player> {
        players.get(self.owner_id)
    }
    
    fn get_players<'a>(&self, players: &'a PlayerStore) -> Vec<&'a Player> {
        self.player_ids.iter()
            .filter_map(|&id| players.get(id))
            .collect()
    }
}
```

이 패턴의 장점:
- 자기 참조 문제가 없다
- Room은 PlayerStore 없이도 이동할 수 있다
- Player가 제거되어도 Room에 댕글링 참조가 생기지 않는다
- 직렬화/역직렬화가 쉽다

### 패턴 2: Arc로 공유 소유권

여러 곳에서 같은 데이터에 접근해야 할 때:

```rust
use std::sync::Arc;

struct Room {
    id: u32,
    owner: Arc<Player>,          // 공유 소유권
    players: Vec<Arc<Player>>,
}
```

`Arc`는 참조 카운팅으로 공유 소유권을 구현한다. Chapter 6에서 상세히 다룬다.

### 패턴 3: 데이터를 분리하고 합치기

복잡한 자기 참조가 필요한 경우, 구조체를 분리하고 필요할 때 합치는 방법:

```rust
// 분리된 구조체들
struct PlayerData {
    id: u64,
    name: String,
    stats: Stats,
}

struct RoomData {
    id: u32,
    owner_id: u64,
    player_ids: Vec<u64>,
}

// 필요할 때 합치는 뷰 구조체 (단기 생존)
struct RoomView<'a> {
    room: &'a RoomData,
    players: Vec<&'a PlayerData>,
}

fn get_room_view<'a>(
    room: &'a RoomData, 
    player_store: &'a PlayerStore
) -> RoomView<'a> {
    let players = room.player_ids.iter()
        .filter_map(|&id| player_store.get(id))
        .collect();
    RoomView { room, players }
}
```

---

## 3.8 빌림 검사기(Borrow Checker)가 거부하는 패턴들

Rust의 빌림 검사기가 거부하는 패턴들과 그 이유를 이해하면, AI가 생성한 코드의 오류를 빠르게 진단할 수 있다.

### 패턴 1: 루프 내에서 컬렉션 수정

```rust
// 오류 코드
fn remove_dead_players(players: &mut Vec<Player>) {
    for player in players.iter() { // 불변 참조로 반복
        if player.is_dead() {
            players.retain(|p| p.id != player.id); // 오류! 가변 참조 요구
        }
    }
}
```

```
error[E0502]: cannot borrow `players` as mutable because it is also borrowed as immutable
```

해결책:

```rust
// 해결책 1: retain 사용
fn remove_dead_players(players: &mut Vec<Player>) {
    players.retain(|p| !p.is_dead());
}

// 해결책 2: 먼저 인덱스 수집, 후에 제거
fn remove_dead_players(players: &mut Vec<Player>) {
    let dead_ids: Vec<u64> = players.iter()
        .filter(|p| p.is_dead())
        .map(|p| p.id)
        .collect();
    
    players.retain(|p| !dead_ids.contains(&p.id));
}
```

### 패턴 2: HashMap에서 읽은 후 수정

게임 서버에서 매우 자주 발생하는 패턴:

```rust
// 오류 코드
fn update_room_owner(rooms: &mut HashMap<u32, Room>, room_id: u32) {
    if let Some(room) = rooms.get(&room_id) { // 불변 참조
        let new_owner_id = room.find_next_owner(); // room 사용
        rooms.get_mut(&room_id).unwrap().owner_id = new_owner_id; // 오류!
        // get()이 반환한 불변 참조가 아직 살아있는 상태에서 get_mut 요청
    }
}
```

해결책:

```rust
// 해결책 1: 필요한 값을 먼저 복사
fn update_room_owner(rooms: &mut HashMap<u32, Room>, room_id: u32) {
    let new_owner_id = rooms.get(&room_id)
        .map(|room| room.find_next_owner());
    // 불변 참조가 여기서 끝남
    
    if let Some(new_id) = new_owner_id {
        if let Some(room) = rooms.get_mut(&room_id) {
            room.owner_id = new_id;
        }
    }
}

// 해결책 2: entry API 사용
fn update_room_owner(rooms: &mut HashMap<u32, Room>, room_id: u32) {
    rooms.entry(room_id).and_modify(|room| {
        let new_id = room.find_next_owner();
        room.owner_id = new_id;
    });
}
```

### 패턴 3: 두 가변 참조가 필요한 경우

```rust
// 오류 코드
fn transfer_items(inventory: &mut Vec<Item>, from: usize, to: usize) {
    let item = &inventory[from]; // 불변 참조
    inventory[to].add(item);     // 오류! 이미 불변 참조가 있음
}
```

해결책:

```rust
// 해결책 1: 인덱스 기반으로 분리
fn transfer_items(inventory: &mut Vec<Item>, from: usize, to: usize) {
    let item = inventory[from].clone(); // 복사
    inventory[to].add(&item);
}

// 해결책 2: split_at_mut 사용 (두 비겹치는 가변 슬라이스)
fn transfer_items(inventory: &mut Vec<Item>, from: usize, to: usize) {
    if from < to {
        let (left, right) = inventory.split_at_mut(to);
        right[0].add(&left[from]);
    } else {
        let (left, right) = inventory.split_at_mut(from);
        left[to].add(&right[0]);
    }
}
```

---

## 3.9 NLL (Non-Lexical Lifetimes)

Rust 2018 에디션부터 도입된 NLL은 빌림 검사기를 더 정교하게 만들었다. 이전에는 참조의 라이프타임이 선언된 변수의 스코프 끝까지였지만, NLL에서는 **마지막으로 사용된 시점**까지다.

```rust
// NLL 이전 (Rust 2015 에디션 스타일)
let mut v = vec![1, 2, 3];
let last = v.last(); // 불변 참조 시작
v.push(4);           // 오류! (NLL 이전에는 오류)
println!("{:?}", last); // 마지막 사용 시점

// NLL 이후 (Rust 2018/2021 에디션)
let mut v = vec![1, 2, 3];
let last = v.last(); // 불변 참조
println!("{:?}", last); // 여기서 참조가 끝남
v.push(4);           // OK! last가 이미 사용되었음
```

NLL 덕분에 많은 경우 더 자연스러운 코드를 작성할 수 있다. 하지만 여전히 한계가 있으며, 컴파일러가 코드 흐름을 완전히 추론할 수 없는 경우에는 명시적 스코프 분리가 필요하다.

---

## 3.10 게임 서버에서의 빌림 패턴

실제 게임 서버 코드에서 자주 등장하는 빌림 패턴들을 정리한다.

### 패턴 1: 읽기 전용 접근 집중화

게임 상태 조회는 불변 참조로, 수정은 명시적인 뮤테이터로 분리한다.

```rust
struct GameWorld {
    players: HashMap<u64, Player>,
    rooms: HashMap<u32, Room>,
    items: HashMap<u64, Item>,
}

impl GameWorld {
    // 읽기 메서드들 - &self 사용
    fn get_player(&self, id: u64) -> Option<&Player> {
        self.players.get(&id)
    }
    
    fn get_room_players(&self, room_id: u32) -> Vec<&Player> {
        self.rooms.get(&room_id)
            .map(|room| {
                room.player_ids.iter()
                    .filter_map(|&id| self.players.get(&id))
                    .collect()
            })
            .unwrap_or_default()
    }
    
    // 쓰기 메서드들 - &mut self 사용
    fn move_player_to_room(&mut self, player_id: u64, room_id: u32) {
        if let Some(player) = self.players.get_mut(&player_id) {
            let old_room_id = player.room_id;
            player.room_id = Some(room_id);
            
            // 이전 방에서 제거
            if let Some(old_id) = old_room_id {
                if let Some(old_room) = self.rooms.get_mut(&old_id) {
                    old_room.player_ids.retain(|&id| id != player_id);
                }
            }
            
            // 새 방에 추가
            if let Some(new_room) = self.rooms.get_mut(&room_id) {
                new_room.player_ids.push(player_id);
            }
        }
    }
}
```

### 패턴 2: 빌림 분리 (Split Borrowing)

Rust는 구조체의 서로 다른 필드를 동시에 가변 빌림할 수 있다.

```rust
struct PlayerState {
    inventory: Inventory,
    stats: Stats,
    position: Position,
}

fn update_player(state: &mut PlayerState) {
    // 서로 다른 필드를 동시에 가변 빌림 가능
    let inventory = &mut state.inventory;
    let stats = &mut state.stats;
    
    // inventory와 stats를 동시에 수정
    if inventory.use_item(ItemType::HpPotion) {
        stats.hp = (stats.hp + 100).min(stats.max_hp);
    }
}
```

같은 필드를 두 번 빌릴 수는 없지만, 다른 필드는 동시에 가변 빌림이 된다.

### 패턴 3: 임시 참조 활용

```rust
struct PacketHandler {
    sessions: HashMap<u64, Session>,
    world: GameWorld,
}

impl PacketHandler {
    fn handle_attack(&mut self, attacker_id: u64, target_id: u64) {
        // 공격자의 공격력을 먼저 읽는다
        let damage = {
            let attacker = self.sessions.get(&attacker_id)
                .and_then(|s| self.world.get_player(s.player_id));
            attacker.map(|a| a.attack_power).unwrap_or(0)
        }; // attacker 참조가 여기서 끝남
        
        // 그 후 대상을 수정한다
        if damage > 0 {
            if let Some(target) = self.world.get_player_mut(target_id) {
                target.take_damage(damage);
            }
        }
    }
}
```

중괄호 블록을 사용하여 참조의 수명을 명시적으로 제한하는 패턴이다. 이것은 NLL이 있어도 여전히 유용한 패턴이다.

---

## 3.11 라이프타임 어노테이션이 필요한 실제 상황

게임 서버 코드에서 라이프타임 어노테이션을 명시해야 하는 경우:

### 함수가 여러 참조 중 하나를 반환할 때

```rust
fn get_dominant_player<'a>(
    room: &'a Room, 
    players: &'a HashMap<u64, Player>
) -> Option<&'a Player> {
    room.player_ids.iter()
        .filter_map(|&id| players.get(&id))
        .max_by_key(|p| p.score)
}
```

### 구조체가 참조를 포함할 때

```rust
// 패킷 파싱 뷰 - 원본 버퍼를 소유하지 않고 빌림
struct PacketParser<'a> {
    buffer: &'a [u8],
    cursor: usize,
}

impl<'a> PacketParser<'a> {
    fn new(data: &'a [u8]) -> Self {
        PacketParser { buffer: data, cursor: 0 }
    }
    
    fn read_string(&mut self, len: usize) -> &'a str {
        let slice = &self.buffer[self.cursor..self.cursor + len];
        self.cursor += len;
        std::str::from_utf8(slice).unwrap()
    }
}

fn parse_chat_packet(raw: &[u8]) -> (&str, &str) {
    let mut parser = PacketParser::new(raw);
    let sender = parser.read_string(16);
    let message = parser.read_string(256);
    (sender, message)
    // sender와 message는 raw의 라이프타임에 묶여 있다
}
```

### 메서드에서 self의 필드를 반환할 때

```rust
struct Config {
    server_name: String,
    host: String,
}

impl Config {
    // 컴파일러가 &self의 라이프타임으로 추론한다
    fn server_name(&self) -> &str {
        &self.server_name
    }
    
    // 두 필드 중 하나를 반환하는 경우 - 명시 불필요 (elision)
    fn get_address(&self) -> &str {
        &self.host
    }
}
```

---

## 3.12 `'static` 라이프타임

`'static`은 특별한 라이프타임으로, 프로그램 전체 실행 기간 동안 유효한 참조를 나타낸다.

```rust
// 문자열 리터럴은 'static이다 (프로그램 바이너리에 저장됨)
let s: &'static str = "Game Server";

// 상수도 'static이다
const MAX_PLAYERS: u32 = 10000;
```

게임 서버에서 `'static`이 등장하는 맥락:

```rust
// Tokio 태스크는 'static 바운드를 요구한다
// 태스크가 언제 끝날지 모르므로 빌린 데이터를 갖고 있으면 안 된다
tokio::spawn(async move {
    // 'static + Send 바운드 필요
    handle_connection(session).await;
});

// 따라서 태스크에 넘기는 데이터는:
// 1. 소유된 데이터여야 한다 (move)
// 2. Arc로 감싼 공유 참조여야 한다
let shared_state = Arc::new(GameState::new());
let state_clone = Arc::clone(&shared_state);
tokio::spawn(async move {
    process(state_clone).await; // Arc는 'static + Send
});
```

---

## 3.13 AI에게 빌림/라이프타임 관련 지시를 내리는 법

### 빌림 오류를 만났을 때

빌림 오류가 발생했을 때 AI에게 올바른 지시를 내리려면 "왜 컴파일러가 거부했는지"를 이해해야 한다.

**불변 참조 중에 수정하려는 오류:**
> "players 맵에서 먼저 모든 dead player의 ID를 collect로 수집하고 
> (불변 참조 단계), 그 다음 ID 목록으로 retain을 호출해서 삭제해줘 
> (가변 참조 단계). 두 단계로 분리해야 빌림 충돌을 피할 수 있어."

**자기 참조 구조체 오류:**
> "Player 구조체 안에 자신의 소유 데이터를 참조하는 필드를 두지 말고, 
> 필요한 슬라이스는 함수 인자로 받도록 해줘. 
> 아니면 인덱스를 저장하고 외부에서 접근하는 방식을 써줘."

**Tokio 태스크 'static 오류:**
> "이 데이터를 태스크에 공유하려면 Arc로 감싸서 clone한 뒤 move로 넘겨줘. 
> 참조로 넘기면 'static 바운드를 만족하지 못해."

### 라이프타임 어노테이션이 필요한 경우 명시

> "이 함수는 두 &str 파라미터 중 하나를 반환하는데, 
> 반환 참조의 라이프타임은 두 파라미터 중 더 짧은 것을 따라야 해. 
> 라이프타임 파라미터를 명시해줘."

---

## 3.14 정리

이 챕터의 핵심:

빌림의 두 종류 — 불변 참조(`&T`)는 동시에 여러 개 가능하고, 가변 참조(`&mut T`)는 동시에 하나만 가능하다. 불변과 가변은 동시에 존재할 수 없다.

빌림 규칙의 의미 — 이 규칙이 컴파일 타임에 데이터 레이스를 차단한다. 독자-저자 락을 타입 시스템이 강제하는 것이다.

라이프타임 — 참조가 유효한 시간 범위다. 대부분의 경우 컴파일러가 추론하지만, 두 참조 중 하나를 반환하거나 구조체가 참조를 포함할 때 명시가 필요하다.

자기 참조 우회 — 구조체 안에 자신의 데이터를 참조로 두는 패턴은 Rust에서 어렵다. ID/인덱스 기반 간접 참조, 또는 Arc를 사용한다.

바이브 코딩에서의 접근 — 빌림 오류를 "빨간 줄 제거"로 접근하지 말고, "왜 컴파일러가 거부했는가"를 이해한 후 두 단계 분리(읽기 후 쓰기), 중간 값 복사 등의 정확한 해결책을 AI에게 지시한다.

---

*다음 챕터: Chapter 4 — 타입 시스템과 트레이트(Trait): 인터페이스가 아니다*
