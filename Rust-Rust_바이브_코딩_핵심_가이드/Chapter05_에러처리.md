# Chapter 5. 에러 처리 — 예외(exception)는 없다

> "에러는 숨기거나 던지는 것이 아니라, 값으로 반환하고 명시적으로 다루는 것이다."

---

## 5.1 예외 없는 세계

C++과 C#을 20년 이상 사용해 온 개발자에게 가장 낯선 변화 중 하나는 **예외(exception)가 없다**는 것이다. Rust에는 `try/catch/finally`가 없다. `throw`도 없다.

대신 에러는 **값**으로 처리된다. 함수가 에러를 발생시킬 수 있다면, 그 함수의 반환 타입이 에러를 포함한다. 이것이 전부다.

### 왜 예외를 없앴는가

예외 기반 에러 처리의 문제점들:

**1. 예외가 발생하는지 알기 어렵다**

```cpp
// C++ - 이 함수가 예외를 던지는가?
void process_packet(const Packet& p) {
    deserialize(p);  // 예외를 던질 수 있는가? 코드 읽어봐야 안다
    handle(p);       // 마찬가지
}
```

Rust에서는 반환 타입이 `Result`이면 에러 가능성이 있고, 아니면 없다. 함수 시그니처만 봐도 에러 가능성을 알 수 있다.

**2. 에러 처리를 강제하지 않는다**

```cpp
// C++ - 에러 처리를 빠뜨려도 컴파일된다
void bad_practice() {
    try {
        risky_operation();
    } catch (...) {
        // 모든 예외를 삼켜버린다 - 실제 에러를 숨긴다!
    }
}
```

**3. 성능 오버헤드**

예외 처리는 스택 언와인딩(stack unwinding) 메커니즘이 필요하며, 예외가 발생하지 않더라도 컴파일된 코드 크기와 성능에 영향을 미친다.

---

## 5.2 Result<T, E>

Rust의 에러 처리 중심에는 `Result<T, E>` enum이 있다.

```rust
// 표준 라이브러리에서의 정의
enum Result<T, E> {
    Ok(T),    // 성공: T 타입의 값
    Err(E),   // 실패: E 타입의 에러
}
```

### 기본 사용

```rust
use std::num::ParseIntError;

fn parse_player_id(s: &str) -> Result<u64, ParseIntError> {
    s.parse::<u64>()
}

fn main() {
    match parse_player_id("12345") {
        Ok(id) => println!("Player ID: {}", id),
        Err(e) => println!("Parse error: {}", e),
    }
    
    match parse_player_id("abc") {
        Ok(id) => println!("Player ID: {}", id),
        Err(e) => println!("Parse error: {}", e),
    }
}
```

### Result 처리 메서드들

```rust
let result: Result<i32, String> = Ok(42);

// unwrap: Ok면 값 반환, Err면 패닉 (프로토타입에서만)
let value = result.unwrap(); // 42

// expect: unwrap과 같지만 패닉 메시지 포함
let value = result.expect("Failed to get value"); // 42

// is_ok / is_err
if result.is_ok() { println!("Success!"); }

// map: Ok 값을 변환
let doubled = result.map(|v| v * 2); // Ok(84)

// map_err: Err 값을 변환
let result: Result<i32, String> = Err("oops".to_string());
let mapped = result.map_err(|e| format!("Error: {}", e)); // Err("Error: oops")

// and_then: Ok면 다음 Result 반환 (체이닝)
fn double_if_positive(n: i32) -> Result<i32, String> {
    if n > 0 { Ok(n * 2) } else { Err("Not positive".to_string()) }
}

let result: Result<i32, String> = Ok(5);
let chained = result.and_then(double_if_positive); // Ok(10)

// unwrap_or: Err면 기본값 반환
let value = result.unwrap_or(0);

// unwrap_or_else: Err면 클로저 실행
let value = result.unwrap_or_else(|e| {
    log::warn!("Using default due to: {}", e);
    0
});
```

---

## 5.3 Option<T>

`Option<T>`는 값이 있을 수도 없을 수도 있는 경우를 표현한다. C++/C#의 null을 대체한다.

```rust
enum Option<T> {
    Some(T),  // 값이 있다
    None,     // 값이 없다
}
```

### 게임 서버에서의 활용

```rust
struct SessionManager {
    sessions: HashMap<u64, Session>,
}

impl SessionManager {
    // 반환 타입이 Option이므로 "없을 수 있다"는 게 명시됨
    fn get_session(&self, id: u64) -> Option<&Session> {
        self.sessions.get(&id)
    }
    
    fn get_player_name(&self, session_id: u64) -> Option<&str> {
        // Option 체이닝
        self.get_session(session_id)
            .map(|session| session.player_name.as_str())
    }
}

// 사용
fn handle_chat(manager: &SessionManager, sender_id: u64, msg: &str) {
    // if let으로 None 처리
    if let Some(session) = manager.get_session(sender_id) {
        broadcast(session.room_id, msg);
    } else {
        log::warn!("Unknown session: {}", sender_id);
    }
    
    // Option 메서드 활용
    let name = manager.get_player_name(sender_id)
        .unwrap_or("Unknown Player");
    log::info!("{}: {}", name, msg);
}
```

### Option과 Result 변환

```rust
// Option -> Result
let option: Option<i32> = Some(42);
let result: Result<i32, &str> = option.ok_or("Value was None");

// Result -> Option (에러 무시)
let result: Result<i32, String> = Ok(42);
let option: Option<i32> = result.ok();

// None을 에러로 변환 (? 연산자와 함께)
fn get_player_level(manager: &SessionManager, id: u64) -> Result<u32, GameError> {
    let session = manager.get_session(id)
        .ok_or(GameError::SessionNotFound(id))?;
    Ok(session.player_level)
}
```

---

## 5.4 `?` 연산자 — 에러 전파의 핵심

`?` 연산자는 에러 처리 코드의 반복을 획기적으로 줄여준다.

### `?` 없이 작성한 경우

```rust
fn load_player_config(path: &str) -> Result<PlayerConfig, GameError> {
    let file_content = match std::fs::read_to_string(path) {
        Ok(content) => content,
        Err(e) => return Err(GameError::Io(e)),
    };
    
    let config = match serde_json::from_str::<PlayerConfig>(&file_content) {
        Ok(cfg) => cfg,
        Err(e) => return Err(GameError::Json(e)),
    };
    
    if config.max_hp == 0 {
        return Err(GameError::InvalidConfig("max_hp cannot be 0".to_string()));
    }
    
    Ok(config)
}
```

### `?` 를 사용한 경우

```rust
fn load_player_config(path: &str) -> Result<PlayerConfig, GameError> {
    let file_content = std::fs::read_to_string(path)?; // Err면 즉시 반환
    let config: PlayerConfig = serde_json::from_str(&file_content)?;
    
    if config.max_hp == 0 {
        return Err(GameError::InvalidConfig("max_hp cannot be 0".to_string()));
    }
    
    Ok(config)
}
```

`?` 연산자의 동작:
1. `Result::Ok(v)` → v를 꺼내서 계속 진행
2. `Result::Err(e)` → `From::from(e)`를 통해 에러 타입을 변환한 후 즉시 반환

### `?` 연산자와 `From` 트레이트

```rust
#[derive(Debug)]
enum GameError {
    Io(std::io::Error),
    Json(serde_json::Error),
    InvalidConfig(String),
}

impl From<std::io::Error> for GameError {
    fn from(e: std::io::Error) -> Self { GameError::Io(e) }
}

impl From<serde_json::Error> for GameError {
    fn from(e: serde_json::Error) -> Self { GameError::Json(e) }
}

// 이제 ?를 사용할 때 자동으로 From 변환이 일어난다
async fn process_config(path: &str) -> Result<(), GameError> {
    let content = tokio::fs::read_to_string(path).await?; // io::Error -> GameError::Io
    let config: serde_json::Value = serde_json::from_str(&content)?; // serde::Error -> GameError::Json
    Ok(())
}
```

---

## 5.5 커스텀 에러 타입 설계

게임 서버에서 에러 타입을 어떻게 설계해야 하는가?

### `thiserror` 크레이트 사용

라이브러리 또는 도메인 레이어에서는 `thiserror`로 명확한 에러 타입을 정의한다.

```toml
# Cargo.toml
[dependencies]
thiserror = "1.0"
```

```rust
use thiserror::Error;

#[derive(Debug, Error)]
pub enum SessionError {
    #[error("Session {0} not found")]
    NotFound(u64),
    
    #[error("Session {0} already exists")]
    AlreadyExists(u64),
    
    #[error("Session {id} is in state {state:?}, expected {expected:?}")]
    InvalidState {
        id: u64,
        state: SessionState,
        expected: SessionState,
    },
    
    #[error("Connection error: {0}")]
    Connection(#[from] std::io::Error),
    
    #[error("Serialization error: {0}")]
    Serialization(#[from] bincode::Error),
}

#[derive(Debug, Error)]
pub enum GameLogicError {
    #[error("Insufficient resources: needed {needed}, have {available}")]
    InsufficientResources { needed: u32, available: u32 },
    
    #[error("Invalid target: {0}")]
    InvalidTarget(String),
    
    #[error("Session error: {0}")]
    Session(#[from] SessionError),
}
```

`thiserror`의 장점:
- `#[error("...")]` 속성으로 Display 자동 구현
- `#[from]`으로 From 자동 구현
- 원본 에러 소스 추적 자동 지원

### `anyhow` 크레이트 사용

애플리케이션 레이어(main, 핸들러)에서는 `anyhow`로 여러 에러 타입을 하나로 합친다.

```toml
[dependencies]
anyhow = "1.0"
```

```rust
use anyhow::{Context, Result, bail, ensure};

// anyhow::Result<T>는 Result<T, anyhow::Error>의 단축형
async fn handle_login(
    session_mgr: &SessionManager,
    db: &Database,
    packet: LoginPacket,
) -> Result<LoginResponse> {
    // 어떤 에러 타입이든 ?로 전파 가능
    let player = db.find_player(&packet.username).await
        .context("Failed to query player from database")?;
    
    ensure!(
        verify_password(&packet.password, &player.password_hash),
        "Invalid password for player {}", packet.username
    );
    
    let session = session_mgr.create_session(player.id)
        .context("Failed to create session")?;
    
    Ok(LoginResponse {
        session_token: session.token.clone(),
        player_data: player.into_public(),
    })
}

// bail!은 즉시 Err 반환
fn validate_packet_size(size: usize) -> Result<()> {
    if size > MAX_PACKET_SIZE {
        bail!("Packet too large: {} > {}", size, MAX_PACKET_SIZE);
    }
    Ok(())
}
```

### 에러 처리 계층화 전략

```
애플리케이션 진입점 (main, 핸들러)
    ↑ anyhow::Error (모든 에러를 합침)
    
도메인 서비스 (GameService, SessionService)
    ↑ thiserror로 정의한 도메인 에러
    
인프라 레이어 (DB, Network, Cache)
    ↑ 라이브러리 에러 (sqlx::Error, io::Error 등)
```

```rust
// 인프라 레이어 - 구체적 에러 타입
pub async fn query_player(db: &Pool, id: u64) -> Result<Player, sqlx::Error> {
    sqlx::query_as!(Player, "SELECT * FROM players WHERE id = ?", id)
        .fetch_one(db)
        .await
}

// 도메인 레이어 - 비즈니스 에러로 변환
#[derive(Debug, thiserror::Error)]
pub enum PlayerError {
    #[error("Player {0} not found")]
    NotFound(u64),
    #[error("Database error: {0}")]
    Database(#[from] sqlx::Error),
}

pub async fn get_player(db: &Pool, id: u64) -> Result<Player, PlayerError> {
    query_player(db, id).await.map_err(|e| {
        if matches!(e, sqlx::Error::RowNotFound) {
            PlayerError::NotFound(id)
        } else {
            PlayerError::Database(e)
        }
    })
}

// 애플리케이션 레이어 - anyhow로 합침
async fn handle_player_info_request(
    db: &Pool,
    player_id: u64,
) -> anyhow::Result<PlayerInfoResponse> {
    let player = get_player(db, player_id).await
        .with_context(|| format!("Handling player info request for {}", player_id))?;
    
    Ok(PlayerInfoResponse::from(player))
}
```

---

## 5.6 패닉(panic!)과 회복 불가능한 에러

`panic!`은 완전히 다른 개념이다. 복구할 수 없는 치명적 오류 상황에서 사용한다.

### 언제 패닉을 쓰는가

```rust
// 1. 불변식(invariant) 위반 - 절대 발생하면 안 되는 상황
fn get_element(v: &[i32], idx: usize) -> i32 {
    assert!(idx < v.len(), "Index {} out of bounds (len={})", idx, v.len());
    v[idx]
}

// 2. 개발/테스트 중 미구현 코드
fn handle_event(event: GameEvent) {
    match event {
        GameEvent::PlayerJoined { .. } => handle_join(),
        GameEvent::PlayerLeft { .. } => handle_leave(),
        GameEvent::ChatMessage { .. } => todo!("Chat not implemented yet"),
    }
}

// 3. 논리적으로 불가능한 상황
fn get_cardinal_direction(angle: f32) -> &'static str {
    let normalized = ((angle % 360.0) + 360.0) % 360.0;
    match normalized as u32 {
        0..=44 | 315..=360 => "North",
        45..=134 => "East",
        135..=224 => "South",
        225..=314 => "West",
        _ => unreachable!("Normalized angle should be 0-359"),
    }
}
```

### 게임 서버에서의 패닉 처리

Tokio에서 각 태스크는 독립적으로 패닉을 처리한다. 태스크가 패닉해도 전체 서버가 죽지 않는다.

```rust
use tokio::task::JoinHandle;

async fn start_session_handler(session: Session) -> JoinHandle<()> {
    tokio::spawn(async move {
        if let Err(e) = handle_session(session).await {
            log::error!("Session handler error: {:?}", e);
        }
    })
}

// 패닉 감지
async fn run_with_panic_handling(session: Session) {
    let handle = start_session_handler(session).await;
    
    match handle.await {
        Ok(()) => log::info!("Session handler completed normally"),
        Err(e) if e.is_panic() => {
            log::error!("Session handler panicked: {:?}", e);
            // 필요시 알람 발송
        }
        Err(e) => log::error!("Session handler error: {:?}", e),
    }
}
```

### `unwrap()`과 `expect()` 사용 지침

```rust
// 프로토타입에서만 허용
let config = std::fs::read_to_string("config.json").unwrap();

// 프로덕션 코드에서는 expect로 문맥 추가
let config = std::fs::read_to_string("config.json")
    .expect("config.json must exist in the working directory");

// 가장 좋은 방법: ?로 에러 전파
fn load_config() -> Result<Config, GameError> {
    let content = std::fs::read_to_string("config.json")?;
    let config: Config = serde_json::from_str(&content)?;
    Ok(config)
}
```

AI가 생성한 코드에 `unwrap()`이 많다면 즉시 경계 신호다. 게임 서버 운영 관점에서 모든 `unwrap()`은 잠재적 서버 다운 지점이다.

---

## 5.7 에러 컨텍스트와 로깅

에러가 발생했을 때 "어디서 어떤 맥락으로 발생했는가"가 중요하다.

### anyhow의 context/with_context

```rust
use anyhow::Context;

async fn process_login(db: &Pool, packet: &LoginPacket) -> anyhow::Result<Session> {
    let player = db.find_player(&packet.username).await
        .with_context(|| format!(
            "Finding player for login: username={}", 
            packet.username
        ))?;
    
    let session = create_session(player.id).await
        .context("Creating new session after login")?;
    
    Ok(session)
}
```

에러가 발생하면 전체 체인이 출력된다:
```
Error: Creating new session after login
Caused by:
    Finding player for login: username=Alice
    Caused by:
        connection pool timeout
```

### tracing과 에러 연동

```rust
use tracing::{error, warn, info, instrument};

#[instrument(skip(db, packet))]
async fn handle_packet(
    db: &Pool,
    session_id: u64,
    packet: GamePacket,
) -> Result<(), GameError> {
    info!("Processing packet type={:?}", packet.packet_type);
    
    let result = process_packet(db, &packet).await;
    
    if let Err(ref e) = result {
        error!(
            error = %e,
            session_id = session_id,
            packet_type = ?packet.packet_type,
            "Packet processing failed"
        );
    }
    
    result
}
```

---

## 5.8 에러 처리 패턴 모음

게임 서버에서 자주 쓰이는 에러 처리 패턴들.

### 패턴 1: 에러 무시 (신중하게!)

```rust
// 에러를 명시적으로 무시 (의도를 명확히)
let _ = cleanup_resources(session_id); // 이미 정리되었을 수 있음

// 또는 더 명시적으로
cleanup_resources(session_id).unwrap_or_else(|e| {
    log::warn!("Cleanup failed (non-critical): {}", e);
});
```

### 패턴 2: 에러 집계

여러 작업을 수행하고 모든 에러를 모을 때:

```rust
async fn batch_process_players(
    db: &Pool,
    player_ids: &[u64],
) -> Vec<Result<Player, DatabaseError>> {
    let futures: Vec<_> = player_ids.iter()
        .map(|&id| load_player(db, id))
        .collect();
    
    futures::future::join_all(futures).await
}

// 첫 번째 에러에서 중단하지 않고 모든 결과 수집
let results = batch_process_players(&db, &ids).await;
let (successes, failures): (Vec<_>, Vec<_>) = results.into_iter()
    .partition(Result::is_ok);
```

### 패턴 3: 재시도 로직

```rust
use tokio::time::{sleep, Duration};

async fn with_retry<F, Fut, T, E>(
    max_attempts: u32,
    f: F,
) -> Result<T, E>
where
    F: Fn() -> Fut,
    Fut: std::future::Future<Output = Result<T, E>>,
    E: std::fmt::Debug,
{
    let mut last_error = None;
    
    for attempt in 1..=max_attempts {
        match f().await {
            Ok(v) => return Ok(v),
            Err(e) => {
                log::warn!("Attempt {}/{} failed: {:?}", attempt, max_attempts, e);
                last_error = Some(e);
                
                if attempt < max_attempts {
                    let delay = Duration::from_millis(100 * 2u64.pow(attempt - 1));
                    sleep(delay).await;
                }
            }
        }
    }
    
    Err(last_error.unwrap())
}

// 사용
let player = with_retry(3, || db.load_player(id)).await?;
```

### 패턴 4: 에러를 이벤트로 변환

```rust
enum PlayerEvent {
    Connected(Session),
    Disconnected { session_id: u64, reason: DisconnectReason },
    Error { session_id: u64, error: GameError },
}

async fn session_loop(
    mut session: Session,
    tx: tokio::sync::mpsc::Sender<PlayerEvent>,
) {
    loop {
        match session.recv_packet().await {
            Ok(packet) => {
                if let Err(e) = process_packet(&mut session, packet).await {
                    tx.send(PlayerEvent::Error {
                        session_id: session.id,
                        error: e,
                    }).await.ok();
                }
            }
            Err(_) => {
                tx.send(PlayerEvent::Disconnected {
                    session_id: session.id,
                    reason: DisconnectReason::ConnectionLost,
                }).await.ok();
                break;
            }
        }
    }
}
```

---

## 5.9 비동기 컨텍스트에서의 에러 처리

`async/await`와 에러 처리를 결합할 때의 주의사항.

### async 함수의 에러 반환

```rust
// async 함수도 Result를 반환할 수 있다
async fn connect_to_database(url: &str) -> Result<Pool, sqlx::Error> {
    sqlx::PgPool::connect(url).await
}

// ?는 async 함수 안에서도 동작한다
async fn initialize_server(config: &Config) -> anyhow::Result<()> {
    let db = connect_to_database(&config.database_url).await
        .context("Failed to connect to database")?;
    
    run_migrations(&db).await
        .context("Failed to run database migrations")?;
    
    Ok(())
}
```

### spawn된 태스크의 에러 처리

```rust
// tokio::spawn은 JoinHandle을 반환한다
// 태스크 내의 에러를 외부로 전달하려면 JoinHandle을 .await해야 한다

async fn run_worker(data: WorkData) -> Result<WorkResult, WorkError> {
    // ... 작업 처리
    Ok(WorkResult::new())
}

async fn main_loop() {
    let handle = tokio::spawn(async move {
        run_worker(WorkData::new()).await
    });
    
    match handle.await {
        Ok(Ok(result)) => println!("Work done: {:?}", result),
        Ok(Err(e)) => eprintln!("Work failed: {}", e),
        Err(join_err) => eprintln!("Task panicked: {:?}", join_err),
    }
}
```

### select!에서의 에러

```rust
use tokio::select;

async fn session_handler(mut session: Session) -> Result<(), SessionError> {
    loop {
        select! {
            result = session.recv_packet() => {
                match result {
                    Ok(packet) => {
                        process_packet(&mut session, packet).await?;
                    }
                    Err(e) => {
                        log::info!("Session {} disconnected: {}", session.id, e);
                        return Ok(());
                    }
                }
            }
            
            _ = session.heartbeat_timer.tick() => {
                session.send_ping().await
                    .context("Failed to send heartbeat")?;
            }
            
            Some(msg) = session.outgoing_rx.recv() => {
                session.send(msg).await
                    .context("Failed to send outgoing message")?;
            }
        }
    }
}
```

---

## 5.10 에러 타입 설계 가이드라인

### 1. 라이브러리 크레이트 - 세밀한 에러 타입

라이브러리를 사용하는 쪽이 에러에 따라 다른 처리를 해야 한다:

```rust
// 세밀한 에러 타입 - 호출자가 각 케이스를 처리할 수 있다
#[derive(Debug, thiserror::Error)]
pub enum AuthError {
    #[error("Invalid credentials")]
    InvalidCredentials,
    
    #[error("Account locked: too many failed attempts")]
    AccountLocked,
    
    #[error("Token expired")]
    TokenExpired,
    
    #[error("Database error: {0}")]
    Database(#[from] sqlx::Error),
}
```

### 2. 애플리케이션 코드 - anyhow로 단순화

빠른 개발, 에러를 로그에 남기기만 하면 되는 경우:

```rust
use anyhow::{Context, Result};

async fn handle_everything() -> Result<()> {
    do_thing_a().await.context("thing A")?;
    do_thing_b().await.context("thing B")?;
    Ok(())
}
```

### 3. 핸들러에서 에러를 응답으로 변환

```rust
use axum::{response::IntoResponse, http::StatusCode, Json};

async fn login_handler(
    Json(request): Json<LoginRequest>,
) -> impl IntoResponse {
    match process_login(request).await {
        Ok(response) => (StatusCode::OK, Json(response)).into_response(),
        Err(e) => {
            match e {
                AuthError::InvalidCredentials => {
                    (StatusCode::UNAUTHORIZED, "Invalid credentials").into_response()
                }
                AuthError::AccountLocked => {
                    (StatusCode::FORBIDDEN, "Account locked").into_response()
                }
                e => {
                    log::error!("Internal error during login: {:?}", e);
                    (StatusCode::INTERNAL_SERVER_ERROR, "Internal error").into_response()
                }
            }
        }
    }
}
```

---

## 5.11 AI에게 에러 처리 지시를 내리는 법

### 에러 타입 설계 요청

> "세션 관련 에러를 thiserror로 정의해줘.
> NotFound, ConnectionFailed(io::Error), AuthenticationFailed, 
> RateLimited { attempts: u32, max: u32 } 케이스를 포함하고,
> io::Error에는 #[from]을 써서 자동 변환이 되도록 해줘."

### unwrap 금지 요청

> "이 코드에서 unwrap()과 expect()를 모두 적절한 에러 처리로 교체해줘.
> 함수 반환 타입은 anyhow::Result<T>로 하고,
> 각 오류 발생 지점에 .context()로 설명을 추가해줘."

### 비동기 에러 전파

> "이 async 함수에서 database 에러와 serialization 에러를 
> 각각 DatabaseError와 SerializationError로 감싸서 
> ? 연산자로 전파할 수 있도록 해줘.
> From 구현을 thiserror #[from]으로 자동화해줘."

### 재시도 로직

> "DB 연결 실패 시 최대 3회까지 지수 백오프로 재시도하는 
> retry_with_backoff 함수를 만들어줘.
> 재시도 간격은 100ms, 200ms, 400ms으로 하고,
> 모든 시도가 실패하면 마지막 에러를 반환해줘."

---

## 5.12 정리

에러는 값이다 — Rust에서 에러는 예외로 던지는 것이 아니라 `Result<T, E>`로 반환한다. 이것이 에러 처리를 명시적이고 검증 가능하게 만든다.

`?` 연산자 — `Ok`면 값을 꺼내고, `Err`면 즉시 반환한다. `From` 트레이트와 결합하여 에러 타입을 자동으로 변환한다.

계층화된 에러 — 라이브러리 레이어는 `thiserror`로 세밀한 에러 타입을, 애플리케이션 레이어는 `anyhow`로 통합 처리한다.

패닉의 자리 — 진짜 불변식 위반이나 프로그래밍 버그에만 사용한다. 게임 서버에서는 태스크 단위로 패닉을 격리하여 전체 서버 다운을 방지한다.

unwrap의 위험 — AI가 생성한 코드의 `unwrap()`은 잠재적 서버 다운 지점이다. 항상 `?` 또는 명시적 처리로 교체해야 한다.

---

*다음 챕터: Chapter 6 — 메모리 관리 도구 상자: 언제 무엇을 쓰는가*
