# Chapter 2. 소유권(Ownership) — 가장 먼저, 가장 깊이 이해할 것

> "Rust의 모든 것은 소유권에서 출발한다. 소유권을 이해하면 나머지는 자연스럽게 따라온다."

---

## 2.1 소유권이란 무엇인가

소유권(Ownership)은 Rust에서 메모리와 자원을 관리하는 핵심 메커니즘이다. 다른 언어에는 존재하지 않는 개념으로, 처음에는 낯설고 불편하게 느껴지지만, 체화하고 나면 "왜 다른 언어들은 이렇게 하지 않았지?"라는 생각이 들 정도로 우아한 설계다.

소유권의 기본 아이디어는 단순하다: **모든 값(value)은 정확히 하나의 변수(소유자, owner)만 가질 수 있다.** 소유자가 스코프를 벗어나면 값은 즉시 해제된다.

이 단순한 규칙 하나가 GC 없이 메모리 안전성을 보장하는 전체 시스템의 기반이다.

---

## 2.2 소유권의 세 가지 규칙

Rust의 소유권 시스템은 다음 세 가지 규칙으로 완전히 정의된다.

**규칙 1: Rust에서 모든 값은 소유자(owner)가 있다.**

```rust
let s = String::from("game server"); // s가 이 String의 소유자다
```

**규칙 2: 한 번에 소유자는 오직 하나뿐이다.**

```rust
let s1 = String::from("hello");
let s2 = s1; // s1의 소유권이 s2로 이동했다
             // 이제 s2가 유일한 소유자, s1은 소유자가 아니다
```

**규칙 3: 소유자가 스코프를 벗어나면 값은 해제된다.**

```rust
{
    let s = String::from("hello"); // s가 유효한 범위 시작
    // s를 사용할 수 있다
} // 스코프 종료, s의 drop이 자동으로 호출되어 메모리 해제
  // 이 시점 이후 s는 유효하지 않다
```

이 세 규칙이 전부다. 이것만으로 GC 없이 메모리 안전성이 보장된다.

---

## 2.3 이동(Move) 시맨틱 — Rust의 기본값

C++와 Rust의 가장 큰 인지 전환점은 **이동이 기본값**이라는 것이다.

### C++에서의 값 전달

C++에서 객체를 다른 변수에 대입하면 기본적으로 **복사**가 발생한다.

```cpp
// C++ - 기본적으로 복사
std::vector<int> v1 = {1, 2, 3};
std::vector<int> v2 = v1; // v1의 내용이 v2로 복사된다
// v1과 v2 모두 유효하며 독립적인 데이터를 갖는다

std::vector<int> v3 = std::move(v1); // 명시적 이동
// 이제 v1은 빈 상태이고, v3가 원래 v1의 데이터를 갖는다
// 하지만 v1을 계속 사용해도 컴파일 오류가 나지 않는다 (위험!)
```

C++의 문제는 `std::move` 후에도 원래 변수를 사용할 수 있다는 것이다. 이동된 객체의 상태는 "유효하지만 미지정(valid but unspecified)"이므로, 사용하면 미정의 동작이 될 수 있다.

### Rust에서의 이동

Rust에서는 값을 다른 변수에 대입하면 기본적으로 **이동**이 발생한다.

```rust
// Rust - 기본적으로 이동
let v1 = vec![1, 2, 3];
let v2 = v1; // v1의 소유권이 v2로 이동
             // v1은 더 이상 유효하지 않다

println!("{:?}", v2); // OK
println!("{:?}", v1); // 컴파일 오류!
```

```
error[E0382]: borrow of moved value: `v1`
```

이동 후 원래 변수를 사용하려 하면 **컴파일 타임에** 오류가 발생한다. 런타임에 조용히 잘못된 메모리를 읽는 일이 불가능하다.

### 이동의 비용

Rust에서 이동은 단순히 스택 데이터를 복사하는 것이다. `Vec<T>`의 경우, 힙 데이터를 복사하는 것이 아니라 **포인터, 길이, 용량** 세 값(24바이트)만 새 위치로 복사된다. 힙 할당은 그대로 유지된다.

```
이동 전:
v1 → [ptr | len=3 | cap=3] → [1, 2, 3] (힙)

이동 후:
v2 → [ptr | len=3 | cap=3] → [1, 2, 3] (같은 힙 메모리)
v1 → (무효화됨)
```

힙 데이터를 복사하지 않으므로 이동은 O(1) 연산이다. 벡터의 크기에 관계없이 동일한 비용이다.

---

## 2.4 복사(Copy)와 클론(Clone)

이동이 기본값이지만, 어떤 타입은 이동 대신 복사가 일어난다.

### Copy 트레이트

스택에만 존재하고 힙 할당이 없는 단순한 타입은 `Copy` 트레이트를 구현한다. 이런 타입은 이동 대신 자동으로 복사된다.

Copy 타입의 예:
- 정수 타입: `i8`, `i16`, `i32`, `i64`, `i128`, `isize`, `u8`, `u16`, `u32`, `u64`, `u128`, `usize`
- 부동소수점 타입: `f32`, `f64`
- 불리언: `bool`
- 문자: `char`
- Copy 타입만으로 이루어진 튜플: `(i32, f64)` 등
- Copy 타입의 고정 크기 배열: `[i32; 5]` 등

```rust
let x = 5;    // i32는 Copy
let y = x;    // x의 값이 복사된다
println!("{}", x); // OK - x는 여전히 유효하다
println!("{}", y); // OK
```

```rust
let s = String::from("hello"); // String은 Copy가 아님 (힙 할당)
let t = s;    // s의 소유권이 t로 이동
println!("{}", s); // 컴파일 오류!
```

### Clone 트레이트

힙 할당이 있는 타입(String, Vec 등)을 명시적으로 깊은 복사하려면 `clone()`을 사용한다.

```rust
let s1 = String::from("hello");
let s2 = s1.clone(); // 힙 데이터까지 완전히 복사
println!("{}", s1);  // OK - s1은 여전히 유효하다
println!("{}", s2);  // OK
```

**중요**: `clone()`은 명시적이기 때문에 개발자가 "여기서 힙 데이터를 복사하는 비용이 든다"는 것을 의식하게 된다. AI가 생성한 코드에 `clone()`이 많다면, 그것이 정말 필요한지 검토해야 한다.

---

## 2.5 함수와 소유권

함수에 값을 전달할 때도 소유권 규칙이 적용된다.

### 함수로 소유권 이동

```rust
fn takes_ownership(s: String) {
    println!("{}", s);
} // s가 스코프를 벗어나면 drop이 호출된다

fn makes_copy(x: i32) {
    println!("{}", x);
} // x는 Copy이므로 이동이 아닌 복사

fn main() {
    let s = String::from("hello");
    takes_ownership(s); // s의 소유권이 함수로 이동
    // println!("{}", s); // 오류! s는 더 이상 유효하지 않다
    
    let x = 5;
    makes_copy(x);      // x는 Copy이므로 복사
    println!("{}", x);  // OK - x는 여전히 유효하다
}
```

### 함수에서 소유권 반환

함수에서 값을 반환하면 소유권이 호출자에게 이동한다.

```rust
fn gives_ownership() -> String {
    let s = String::from("world");
    s // s의 소유권이 호출자에게 반환된다
}

fn takes_and_gives_back(s: String) -> String {
    s // 받은 소유권을 그대로 반환
}

fn main() {
    let s1 = gives_ownership();
    let s2 = String::from("hello");
    let s3 = takes_and_gives_back(s2);
    // s2는 더 이상 유효하지 않음
    // s1과 s3는 유효함
}
```

이 패턴은 번거롭다. 함수에 값을 넘겼다가 돌려받아야 할 때마다 소유권을 주고받아야 한다. 이 문제를 해결하는 것이 **빌림(Borrowing)**이며, 다음 챕터에서 다룬다.

---

## 2.6 게임 서버에서의 소유권 패턴

이론은 충분하다. 실제 게임 서버 코드에서 소유권이 어떻게 적용되는지 살펴본다.

### 패킷 버퍼의 소유권

게임 서버에서 패킷을 수신하고 처리하는 과정에서 소유권이 자연스럽게 흐른다.

```rust
// 패킷 처리 파이프라인에서 소유권 흐름
struct PacketBuffer {
    data: Vec<u8>,
    session_id: u64,
}

struct NetworkLayer;
struct DeserializeLayer;
struct GameLogicLayer;

impl NetworkLayer {
    // 네트워크로부터 패킷을 읽어 소유권을 생성한다
    fn read_packet(&self) -> PacketBuffer {
        PacketBuffer {
            data: vec![/* ... */],
            session_id: 12345,
        }
    }
}

impl DeserializeLayer {
    // 패킷 소유권을 받아서 역직렬화 후 반환
    fn process(&self, raw: PacketBuffer) -> GamePacket {
        // raw는 여기서 소비된다 (소유권이 이 함수로 이동)
        let game_packet = deserialize(raw.data);
        GamePacket {
            session_id: raw.session_id,
            payload: game_packet,
        }
    }
}

impl GameLogicLayer {
    // 게임 패킷 소유권을 받아서 처리
    fn handle(&self, packet: GamePacket) {
        // packet은 여기서 처리되고 해제된다
    }
}

fn main() {
    let network = NetworkLayer;
    let deserializer = DeserializeLayer;
    let game_logic = GameLogicLayer;
    
    let raw = network.read_packet();       // PacketBuffer 생성
    let packet = deserializer.process(raw); // raw의 소유권이 process로 이동
    game_logic.handle(packet);             // packet의 소유권이 handle로 이동
    // 모든 메모리가 자동으로 해제됨
}
```

이 코드에서 버퍼는 명확한 파이프라인을 따라 흐른다. 어느 시점에서도 같은 데이터에 두 개의 소유자가 없다. 버퍼가 해제되는 시점도 명확하다.

### 세션 객체의 소유권

게임 서버에서 세션(플레이어 연결)은 중요한 자원이다. 세션의 소유권을 어디에 둘 것인가가 설계의 핵심이다.

```rust
// 세션의 소유권을 SessionManager가 갖는 패턴
use std::collections::HashMap;

struct Session {
    id: u64,
    player_name: String,
    // ... 기타 세션 데이터
}

struct SessionManager {
    sessions: HashMap<u64, Session>, // SessionManager가 모든 Session의 소유자
}

impl SessionManager {
    fn new() -> Self {
        SessionManager {
            sessions: HashMap::new(),
        }
    }
    
    // 새 세션 추가 - 소유권을 받아서 map에 저장
    fn add_session(&mut self, session: Session) {
        self.sessions.insert(session.id, session);
    }
    
    // 세션 제거 - 소유권을 꺼내서 반환 (Option으로 감싸짐)
    fn remove_session(&mut self, id: u64) -> Option<Session> {
        self.sessions.remove(&id)
    }
    
    // 세션 참조 반환 - 소유권을 이동하지 않고 빌려줌
    fn get_session(&self, id: u64) -> Option<&Session> {
        self.sessions.get(&id)
    }
    
    // 세션 수정 - 가변 참조 반환
    fn get_session_mut(&mut self, id: u64) -> Option<&mut Session> {
        self.sessions.get_mut(&id)
    }
}
```

이 설계에서:
- `SessionManager`가 모든 `Session`의 유일한 소유자다
- `get_session`은 빌림(참조)을 반환한다 - 소유권 이전 없음
- `remove_session`은 소유권을 꺼내서 반환한다
- C++에서 발생하던 "누가 이 포인터를 해제해야 하는가?" 문제가 사라졌다

AI에게 이런 구조를 지시할 때:
> "SessionManager가 Session의 소유권을 HashMap으로 관리하도록 해줘. 
> 외부에서는 참조만 빌려주고 소유권은 이동하지 않도록 해줘."

### 룸 구조에서의 소유권

더 복잡한 예: 게임 룸이 플레이어 세션에 대한 참조를 필요로 하는 경우.

```rust
// 잘못된 접근: Room이 Session을 소유하려는 시도
// 이미 SessionManager가 소유하는데 Room도 소유하려면 두 소유자가 생긴다
struct Room {
    id: u32,
    players: Vec<Session>, // 오류: SessionManager와 Room 둘 다 소유 불가
}
```

이것은 컴파일되지 않는다. 이미 `SessionManager`가 `Session`을 소유하고 있기 때문이다. 해결책은 여러 가지다.

**방법 1: 세션 ID만 보유**

```rust
struct Room {
    id: u32,
    player_ids: Vec<u64>, // 소유권 없이 ID만 보관
}

impl Room {
    // 플레이어 처리가 필요할 때 SessionManager에서 참조를 빌려온다
    fn broadcast_message(&self, session_manager: &SessionManager, msg: &str) {
        for &id in &self.player_ids {
            if let Some(session) = session_manager.get_session(id) {
                // session은 &Session - 빌림
                session.send_message(msg);
            }
        }
    }
}
```

**방법 2: Arc로 공유 소유권 (다음 챕터에서 상세히 다룸)**

```rust
use std::sync::Arc;

struct Room {
    id: u32,
    players: Vec<Arc<Session>>, // 공유 소유권
}
```

어떤 방법을 선택하느냐는 설계 결정이다. 방법 1은 더 단순하고 성능이 좋지만, Room이 유효한 세션인지 항상 확인해야 한다. 방법 2는 Room이 직접 Session에 접근할 수 있지만 Arc의 오버헤드가 있다.

AI에게 지시할 때 이 결정을 명확히 해야 한다:
> "Room은 세션 ID 목록만 갖고, 세션 데이터가 필요할 때 SessionManager를 통해 접근하도록 해줘."

---

## 2.7 스코프와 소유권

소유권은 스코프(scope)와 밀접하게 연결된다. 스코프를 의도적으로 활용하면 자원의 해제 시점을 제어할 수 있다.

### 블록 스코프를 활용한 자원 관리

```rust
fn handle_request(db_pool: &DbPool, request: Request) -> Response {
    // 데이터베이스 연결은 이 블록 내에서만 필요하다
    let result = {
        let conn = db_pool.get_connection(); // 연결 획득
        let data = conn.query(&request.sql); // 쿼리 실행
        process_data(data)                   // 데이터 처리
    }; // 블록 끝: conn이 drop되어 연결이 풀로 반환됨
    
    // 이 시점에는 데이터베이스 연결이 없다
    build_response(result)
}
```

C++의 RAII와 비슷하지만, Rust는 이것을 **언어 수준에서 강제**한다. "연결을 해제하는 것을 잊어버리는" 실수가 불가능하다.

게임 서버에서 이 패턴은 다양하게 활용된다:
- 락(Mutex guard)을 최소한의 스코프에서만 보유
- DB 연결을 쿼리가 끝나면 즉시 반환
- 임시 버퍼를 처리 후 즉시 해제

### 조기 반환과 소유권

Rust에서 함수가 일찍 반환되어도 소유한 자원은 자동으로 해제된다.

```rust
fn process_packet(packet: PacketBuffer) -> Result<GameEvent, PacketError> {
    if packet.data.len() < 4 {
        return Err(PacketError::TooShort);
        // packet이 여기서 drop됨 - 메모리 해제
    }
    
    let packet_type = u32::from_be_bytes(packet.data[..4].try_into()?);
    
    if packet_type > MAX_PACKET_TYPE {
        return Err(PacketError::UnknownType(packet_type));
        // packet이 여기서 drop됨
    }
    
    // 정상 처리 경로
    Ok(parse_packet(packet))
    // parse_packet이 packet의 소유권을 가져감
}
```

어떤 경로로 반환되든 메모리는 반드시 해제된다. "이 경로에서 cleanup을 빠뜨렸나?"를 걱정할 필요가 없다.

---

## 2.8 Move 시맨틱과 스레드

소유권의 이동은 스레드 간 데이터 전달에서 특히 중요하다.

### 스레드로 데이터 이동

```rust
use std::thread;

fn spawn_worker(data: Vec<u8>) -> thread::JoinHandle<Vec<u8>> {
    // data의 소유권이 새 스레드로 이동
    thread::spawn(move || {
        // 이 클로저가 data의 소유자가 된다
        let processed = process(data);
        processed // 처리된 데이터를 반환
    })
}

fn main() {
    let data = vec![1, 2, 3, 4, 5];
    let handle = spawn_worker(data);
    // data는 이제 워커 스레드가 소유한다 - 여기서 data를 쓰면 컴파일 오류!
    
    let result = handle.join().unwrap();
}
```

`move` 키워드는 클로저가 외부 변수의 소유권을 가져온다는 것을 명시한다. 이것은 **스레드가 존재하는 동안 데이터가 유효함을 보장**한다. C++에서 스레드 함수에 포인터를 넘겼다가 원래 데이터가 먼저 해제되는 버그가 Rust에서는 원천적으로 불가능하다.

### Tokio 비동기 태스크에서의 이동

게임 서버에서 주로 사용하는 Tokio 비동기 태스크도 동일한 원칙을 따른다.

```rust
use tokio;

async fn handle_session(session: Session) {
    // session의 소유권이 이 태스크로 이동
    loop {
        let packet = session.recv_packet().await;
        match packet {
            Ok(p) => process(p).await,
            Err(_) => break,
        }
    }
    // session이 여기서 drop됨 - 소켓이 닫힘
}

#[tokio::main]
async fn main() {
    let session = accept_connection().await;
    // session의 소유권이 태스크로 이동
    tokio::spawn(handle_session(session));
    // 이 시점에서 main은 session을 더 이상 소유하지 않는다
}
```

---

## 2.9 소유권과 컬렉션

게임 서버에서 자주 사용하는 컬렉션(`Vec`, `HashMap` 등)에서의 소유권 패턴을 알아야 한다.

### Vec의 소유권

```rust
let mut players: Vec<Player> = Vec::new();
let player = Player::new(1, "Alice");
players.push(player); // player의 소유권이 Vec으로 이동
// player 변수는 더 이상 유효하지 않다

// Vec에서 원소를 꺼내면 소유권이 이동한다
let removed = players.remove(0); // 첫 번째 원소의 소유권이 removed로 이동
// players에는 이제 그 원소가 없다
```

### 반복과 소유권

`for` 루프가 컬렉션을 소비(consume)하는 경우:

```rust
let players = vec![
    Player::new(1, "Alice"),
    Player::new(2, "Bob"),
];

// into_iter()는 Vec의 소유권을 가져와서 각 원소를 이동한다
for player in players {
    // player는 각 반복에서 소유된다
    process_player(player); // player의 소유권이 이동
} // 모든 원소가 소비됨
// players는 더 이상 유효하지 않다 (이미 소비됨)
```

참조로 반복하면 소유권을 유지할 수 있다:

```rust
let players = vec![
    Player::new(1, "Alice"),
    Player::new(2, "Bob"),
];

// iter()는 &Player 참조로 반복한다
for player in &players {
    // player는 &Player - 빌림
    read_player_stats(player);
}
// players는 여전히 유효하다

// iter_mut()는 &mut Player로 반복한다
for player in &mut players {
    player.update_tick();
}
```

---

## 2.10 소유권 오류 패턴과 해결법

AI가 생성한 코드에서 자주 보이는 소유권 오류 패턴과 그 해결법을 정리한다.

### 패턴 1: 이동 후 사용

```rust
// 오류 코드
fn wrong() {
    let data = vec![1, 2, 3];
    let processed = transform(data); // data의 소유권 이동
    println!("{:?}", data);          // 오류! data는 이미 이동됨
}

// 해결책 1: clone (비용이 있음 - 꼭 필요한지 검토)
fn fixed_v1() {
    let data = vec![1, 2, 3];
    let processed = transform(data.clone()); // 복사본을 넘김
    println!("{:?}", data);                   // OK
}

// 해결책 2: 참조 전달 (권장)
fn transform_ref(data: &[i32]) -> Vec<i32> { /* ... */ vec![] }

fn fixed_v2() {
    let data = vec![1, 2, 3];
    let processed = transform_ref(&data); // 참조를 넘김
    println!("{:?}", data);               // OK
}
```

### 패턴 2: 컬렉션에서 원소 이동

```rust
// 오류 코드
fn wrong() {
    let players = vec![Player::new(1), Player::new(2)];
    for player in players {
        process(player);
    }
    println!("{:?}", players); // 오류! players는 이미 소비됨
}

// 해결책: 참조로 반복
fn fixed() {
    let players = vec![Player::new(1), Player::new(2)];
    for player in &players {
        process_ref(player); // &Player를 받는 함수 필요
    }
    println!("{}", players.len()); // OK
}
```

### 패턴 3: 구조체 필드의 부분 이동

```rust
struct GameState {
    players: Vec<Player>,
    map: GameMap,
}

// 오류 코드
fn wrong(state: GameState) {
    let players = state.players; // players를 이동
    println!("{:?}", state.map); // 오류? 아니다! 이건 OK
    // println!("{:?}", state);  // 이건 오류 - state 전체는 부분적으로 이동됨
}
```

흥미롭게도 Rust는 구조체 필드를 개별적으로 이동할 수 있다. 하지만 일부 필드가 이동된 구조체 전체는 사용할 수 없다.

```rust
// 안전한 패턴: 구조체를 해체(destructure)
fn process_state(GameState { players, map }: GameState) {
    // 명시적으로 두 필드를 별도 소유권으로 분리
    process_players(players);
    process_map(map);
}
```

---

## 2.11 AI에게 소유권 관련 지시를 내리는 법

바이브 코딩에서 소유권을 올바르게 다루려면, AI에게 "누가 무엇을 소유하는가"를 명확히 지시해야 한다.

### 좋은 지시 패턴

**소유권 명시:**
> "SessionManager가 Session의 유일한 소유자가 되도록 해줘. 
> 다른 컴포넌트는 세션에 접근할 때 SessionManager를 통해서만 하도록."

**참조 전달 명시:**
> "이 함수들은 데이터를 수정하지 않으므로 &T 참조를 받도록 해줘.
> 데이터의 소유권을 함수로 이동하지 않도록."

**라이프타임 회피 명시:**
> "구조체 안에 참조를 두지 말고, 필요할 때 ID로 조회하는 방식을 써줘."

**이동 vs 복사 명시:**
> "이 처리 파이프라인에서는 버퍼의 소유권이 각 단계로 이동하도록 설계해줘.
> 중간에 clone()을 사용하지 않도록."

### 소유권 오류가 났을 때 AI에게 전달하는 법

단순히 오류 메시지를 붙여넣기 하는 것보다 의도를 함께 설명하는 것이 더 좋은 결과를 만든다.

**나쁜 지시:**
> "error[E0382]: borrow of moved value: `session` 오류 고쳐줘"

**좋은 지시:**
> "session을 process_packet에 넘긴 후에도 session의 정보(session.id)를 
> 로깅에 사용해야 해. session의 소유권을 이동하지 않고 &Session 참조로 
> process_packet 함수를 수정해줘. 아니면 session.id를 먼저 복사해 두는 방법도 좋아."

---

## 2.12 정리

이 챕터에서 다룬 핵심 개념:

소유권의 세 규칙 — 모든 값은 소유자가 있고, 소유자는 하나뿐이며, 소유자가 스코프를 벗어나면 값이 해제된다.

이동이 기본값 — Rust에서 값을 다른 변수에 대입하거나 함수에 전달하면 기본적으로 소유권이 이동한다. Copy 트레이트가 있는 타입만 자동 복사된다.

함수와 소유권 — 함수로 값을 전달하면 소유권이 이동하고, 반환하면 소유권이 돌아온다. 빌림을 사용하면 소유권 이동 없이 값에 접근할 수 있다.

게임 서버 패턴 — SessionManager가 Session을 소유하고, 다른 컴포넌트는 ID로 간접 참조하거나 참조를 빌리는 패턴이 가장 자연스럽다.

바이브 코딩에서의 지시 — "누가 소유하는가"를 명확히 하고, clone()의 필요성을 신중히 검토하며, 소유권 오류의 원인을 이해한 후 의도를 담아 AI에게 지시해야 한다.

---

*다음 챕터: Chapter 3 — 빌림(Borrowing)과 라이프타임: Rust의 두 번째 기둥*
