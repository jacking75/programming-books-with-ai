# Chapter 8. 비동기 프로그래밍과 Tokio — 게임 서버의 본진

> "현대 게임 서버는 비동기다. 그리고 Rust에서 비동기는 Tokio다."

---

## 8.1 왜 비동기인가

수천 명의 플레이어가 동시에 접속한 게임 서버를 상상해 보자. 각 플레이어의 패킷을 처리하고, 데이터베이스를 조회하고, 다른 플레이어에게 상태를 전송한다.

### 스레드 기반 접근법의 한계

```cpp
// C++ - 클래식 스레드 기반 서버
void HandleClient(Socket socket) {
    while (true) {
        Packet packet = socket.Receive(); // 블로킹 - 스레드가 여기서 기다림
        auto result = ProcessPacket(packet);
        socket.Send(result);
    }
}

int main() {
    while (true) {
        Socket client = server.Accept();
        std::thread(HandleClient, client).detach(); // 클라이언트당 스레드 생성
    }
}
```

10,000 명의 동시 접속이라면 10,000 개의 스레드가 필요하다. 각 스레드는 스택 메모리(기본 8MB)를 사용하므로, 10,000 × 8MB = 80GB의 스택 메모리가 필요하다. 현실적으로 불가능하다.

### 비동기의 해결책

비동기 I/O에서는 I/O가 완료될 때까지 기다리는 동안 스레드를 다른 작업에 사용한다. 수천 개의 동시 연결을 소수의 스레드로 처리할 수 있다.

```
동기 모델:
스레드 1: [패킷 수신 대기......][처리][응답 전송][패킷 수신 대기......]
스레드 2: [패킷 수신 대기......][처리][응답 전송][패킷 수신 대기......]

비동기 모델 (단일 스레드, 여러 연결):
스레드 1: [A처리][B처리][A처리][C처리][B처리][A처리][D처리][...]
          ↑ A가 I/O 대기 중일 때 B, C, D를 처리
```

---

## 8.2 async/await 기초

Rust의 비동기 프로그래밍은 `async`와 `await` 키워드를 중심으로 한다.

### async 함수

```rust
// async fn은 Future를 반환한다
async fn fetch_player_data(id: u64) -> Result<PlayerData, DatabaseError> {
    // 데이터베이스 조회 - I/O 대기 중에 다른 태스크가 실행될 수 있다
    let data = db.query("SELECT * FROM players WHERE id = ?", id).await?;
    Ok(data)
}
```

`async fn`의 작동 방식:
1. 함수 호출 시 **즉시 실행되지 않는다**
2. 대신 `Future<Output = Result<PlayerData, DatabaseError>>`를 반환한다
3. `Future`가 `.await`되거나 `tokio::spawn`으로 실행되어야 실제로 진행된다

### Future 트레이트

```rust
// 표준 라이브러리 Future 트레이트 (단순화)
pub trait Future {
    type Output;
    fn poll(self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output>;
}

enum Poll<T> {
    Ready(T),    // 완료됨, 값을 반환
    Pending,     // 아직 진행 중, 나중에 다시 poll 해줘
}
```

`Future`는 상태 기계(state machine)다. 컴파일러는 `async fn`을 이 상태 기계로 변환한다. `await` 지점마다 상태 전환이 발생한다.

### C#의 async/await와 비교

| 특성 | C# | Rust |
|------|-----|------|
| 런타임 | CLR (내장) | 크레이트 선택 (Tokio 등) |
| Task/Future 생성 | 자동 | async fn 필요 |
| 실행 시작 | 즉시 (기본) | await/spawn 후 |
| 스레드 컨텍스트 | 자동 관리 | 명시적 (spawn_blocking) |
| 취소 | CancellationToken | 암묵적 Drop |

---

## 8.3 Tokio 런타임

Rust 표준 라이브러리에는 비동기 런타임이 없다. Tokio가 실질적 표준이다.

### 기본 설정

```toml
# Cargo.toml
[dependencies]
tokio = { version = "1", features = ["full"] }
```

```rust
// 멀티스레드 런타임 (기본, CPU 코어 수만큼 워커 스레드)
#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let server = GameServer::new().await?;
    server.run().await?;
    Ok(())
}

// 또는 명시적으로 런타임 빌드
fn main() {
    let runtime = tokio::runtime::Builder::new_multi_thread()
        .worker_threads(4)
        .thread_name("game-worker")
        .enable_all()
        .build()
        .unwrap();
    
    runtime.block_on(async {
        // 비동기 코드 실행
    });
}
```

### 단일 스레드 vs 멀티스레드 런타임

```rust
// 멀티스레드 (기본 - 게임 서버 기본 선택)
#[tokio::main]
async fn main() { /* ... */ }

// 단일 스레드 (특수 케이스)
#[tokio::main(flavor = "current_thread")]
async fn main() { /* ... */ }
```

게임 서버에는 멀티스레드 런타임이 적합하다. I/O 바운드 작업과 CPU 바운드 작업이 혼재하며, 여러 코어를 활용해야 하기 때문이다.

---

## 8.4 태스크 (Tasks)

Tokio 태스크는 운영체제 스레드보다 훨씬 가볍다. 수백만 개의 태스크를 동시에 실행할 수 있다.

### 태스크 생성

```rust
use tokio::task::JoinHandle;

async fn handle_connection(socket: TcpStream) -> Result<(), SessionError> {
    // 연결 처리 로직
    Ok(())
}

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    let listener = TcpListener::bind("0.0.0.0:9000").await?;
    
    loop {
        let (socket, addr) = listener.accept().await?;
        
        // 각 연결을 독립 태스크로 실행
        tokio::spawn(async move {
            if let Err(e) = handle_connection(socket).await {
                log::error!("Connection error from {}: {}", addr, e);
            }
        });
    }
}
```

### JoinHandle로 태스크 결과 수집

```rust
async fn parallel_queries(
    db: Arc<Database>,
    ids: Vec<u64>,
) -> Vec<Result<Player, DbError>> {
    let handles: Vec<JoinHandle<Result<Player, DbError>>> = ids.into_iter()
        .map(|id| {
            let db = Arc::clone(&db);
            tokio::spawn(async move {
                db.get_player(id).await
            })
        })
        .collect();
    
    // 모든 태스크 완료 대기
    let results = futures::future::join_all(handles).await;
    
    results.into_iter()
        .map(|r| r.unwrap_or_else(|e| Err(DbError::TaskPanic(e))))
        .collect()
}
```

### 태스크 취소

Tokio 태스크는 `JoinHandle::abort()`로 취소할 수 있다.

```rust
async fn session_with_timeout(socket: TcpStream) {
    let handle = tokio::spawn(async move {
        handle_session(socket).await
    });
    
    // 30초 타임아웃
    tokio::select! {
        result = &mut handle => {
            match result {
                Ok(Ok(())) => log::info!("Session completed"),
                Ok(Err(e)) => log::error!("Session error: {}", e),
                Err(e) => log::error!("Task panicked: {}", e),
            }
        }
        _ = tokio::time::sleep(Duration::from_secs(30)) => {
            log::warn!("Session timeout, aborting");
            handle.abort();
        }
    }
}
```

---

## 8.5 채널 — 게임 서버 설계의 뼈대

Tokio는 여러 종류의 채널을 제공한다. 채널의 선택이 게임 서버 아키텍처를 결정한다.

### mpsc — 다대일 채널

Multi-Producer Single-Consumer: 여러 생산자, 단일 소비자.

```rust
use tokio::sync::mpsc;

// 패킷 처리 파이프라인
async fn packet_pipeline(num_workers: usize) {
    // 네트워크 -> 파서 채널
    let (raw_tx, mut raw_rx) = mpsc::channel::<RawPacket>(10_000);
    
    // 파서 -> 게임 로직 채널
    let (parsed_tx, mut parsed_rx) = mpsc::channel::<ParsedPacket>(10_000);
    
    // 네트워크 수신 태스크들 (여러 개)
    for _ in 0..num_workers {
        let tx = raw_tx.clone(); // 여러 생산자
        tokio::spawn(async move {
            loop {
                let packet = receive_raw().await;
                if tx.send(packet).await.is_err() {
                    break; // 수신자가 닫혔음
                }
            }
        });
    }
    drop(raw_tx); // 원본 sender drop (클론들이 살아있으면 채널은 열려있음)
    
    // 파서 태스크 (단일 소비자)
    let parsed_tx_clone = parsed_tx.clone();
    tokio::spawn(async move {
        while let Some(raw) = raw_rx.recv().await {
            if let Ok(parsed) = parse_packet(raw) {
                let _ = parsed_tx_clone.send(parsed).await;
            }
        }
    });
    
    // 게임 로직 태스크 (단일 소비자)
    tokio::spawn(async move {
        while let Some(packet) = parsed_rx.recv().await {
            handle_game_packet(packet).await;
        }
    });
}
```

### oneshot — 일회성 응답 채널

요청-응답 패턴에서 사용:

```rust
use tokio::sync::oneshot;

// 데이터베이스 액터 패턴
enum DbCommand {
    GetPlayer {
        id: u64,
        reply: oneshot::Sender<Result<Player, DbError>>,
    },
    SavePlayer {
        player: Player,
        reply: oneshot::Sender<Result<(), DbError>>,
    },
}

struct DbActor {
    rx: mpsc::Receiver<DbCommand>,
    db: Database,
}

impl DbActor {
    async fn run(mut self) {
        while let Some(cmd) = self.rx.recv().await {
            match cmd {
                DbCommand::GetPlayer { id, reply } => {
                    let result = self.db.get_player(id).await;
                    let _ = reply.send(result); // 결과 전송
                }
                DbCommand::SavePlayer { player, reply } => {
                    let result = self.db.save_player(player).await;
                    let _ = reply.send(result);
                }
            }
        }
    }
}

// 클라이언트 측 사용
async fn get_player_from_actor(
    db_tx: &mpsc::Sender<DbCommand>,
    id: u64,
) -> Result<Player, DbError> {
    let (reply_tx, reply_rx) = oneshot::channel();
    
    db_tx.send(DbCommand::GetPlayer { id, reply: reply_tx }).await
        .map_err(|_| DbError::ActorDead)?;
    
    reply_rx.await
        .map_err(|_| DbError::ReplyDropped)?
}
```

### broadcast — 일대다 팬아웃

한 생산자가 여러 소비자에게 같은 메시지를 전송:

```rust
use tokio::sync::broadcast;

struct ChatRoom {
    tx: broadcast::Sender<ChatMessage>,
}

impl ChatRoom {
    fn new() -> Self {
        let (tx, _) = broadcast::channel(1000);
        ChatRoom { tx }
    }
    
    fn subscribe(&self) -> broadcast::Receiver<ChatMessage> {
        self.tx.subscribe()
    }
    
    fn broadcast(&self, msg: ChatMessage) -> Result<usize, broadcast::error::SendError<ChatMessage>> {
        self.tx.send(msg)
    }
}

// 각 세션이 수신자를 구독
async fn session_receiver_loop(
    mut rx: broadcast::Receiver<ChatMessage>,
    socket_tx: impl SinkExt<Message>,
) {
    loop {
        match rx.recv().await {
            Ok(msg) => {
                // 직렬화해서 소켓으로 전송
                let bytes = serialize_chat_message(msg);
                socket_tx.send(Message::Binary(bytes)).await.ok();
            }
            Err(broadcast::error::RecvError::Lagged(n)) => {
                // 너무 느린 수신자 - n개 메시지 손실
                log::warn!("Subscriber lagged, missed {} messages", n);
            }
            Err(broadcast::error::RecvError::Closed) => break,
        }
    }
}
```

### watch — 최신 상태 브로드캐스트

항상 최신 값만 중요한 경우 (예: 게임 상태, 설정):

```rust
use tokio::sync::watch;

// 서버 상태 모니터링
let (server_status_tx, server_status_rx) = watch::channel(ServerStatus {
    player_count: 0,
    tick_rate: 20,
    is_maintenance: false,
});

// 상태 업데이트 (단일 생산자)
tokio::spawn(async move {
    loop {
        tokio::time::sleep(Duration::from_secs(1)).await;
        let new_status = ServerStatus {
            player_count: get_player_count(),
            ..ServerStatus::default()
        };
        server_status_tx.send(new_status).ok();
    }
});

// 상태 구독 (여러 소비자)
let mut rx = server_status_rx.clone();
tokio::spawn(async move {
    loop {
        if rx.changed().await.is_err() { break; }
        let status = rx.borrow();
        log::info!("Player count: {}", status.player_count);
    }
});
```

### 채널 종류 선택 가이드

| 채널 | 생산자 | 소비자 | 주요 용도 |
|------|--------|--------|----------|
| `mpsc` | 다수 | 하나 | 패킷 처리, 작업 큐 |
| `oneshot` | 하나 | 하나 | 요청-응답, DB 조회 |
| `broadcast` | 하나 | 다수 | 채팅, 이벤트 팬아웃 |
| `watch` | 하나 | 다수 | 서버 상태, 설정 |

---

## 8.6 select! 매크로 — 여러 비동기 작업 경쟁

`select!`는 여러 Future 중 먼저 완료되는 것을 선택한다.

```rust
use tokio::select;
use tokio::time::{sleep, Duration};

async fn session_main_loop(mut session: Session) -> Result<(), SessionError> {
    let mut heartbeat = tokio::time::interval(Duration::from_secs(30));
    
    loop {
        select! {
            // 패킷 수신
            result = session.recv_packet() => {
                match result {
                    Ok(packet) => {
                        session.handle_packet(packet).await?;
                    }
                    Err(e) => {
                        log::info!("Session disconnected: {}", e);
                        return Ok(());
                    }
                }
            }
            
            // 30초마다 하트비트
            _ = heartbeat.tick() => {
                session.send_heartbeat().await?;
            }
            
            // 서버 측에서 내려보내는 메시지
            Some(msg) = session.outgoing.recv() => {
                session.send_raw(msg).await?;
            }
            
            // 종료 신호
            _ = session.shutdown_rx.changed() => {
                if *session.shutdown_rx.borrow() {
                    session.send_disconnect_notice().await.ok();
                    return Ok(());
                }
            }
        }
    }
}
```

### select!의 주의사항 — 취소 안전성

`select!`에서 한 분기가 선택되면 **선택되지 않은 나머지 Future들은 취소(drop)**된다.

```rust
// 문제가 될 수 있는 패턴
async fn risky() {
    let mut in_progress_operation = start_operation(); // 작업 시작
    
    select! {
        _ = &mut in_progress_operation => { /* 완료 */ }
        _ = timeout() => {
            // 타임아웃 선택됨
            // in_progress_operation이 drop된다
            // 만약 in_progress_operation이 파일을 쓰는 중이었다면?
            // 파일이 불완전한 상태로 남을 수 있다
        }
    }
}
```

취소 안전한 작업들:
- 채널 recv (취소 가능, 다음 recv에서 다시 받음)
- sleep (취소 가능, 상태 없음)
- 파일 읽기의 일부 구현

취소 안전하지 않은 작업들:
- 파일 쓰기 중간
- 데이터베이스 트랜잭션 중간
- 여러 단계 프로토콜 중간

취소 불안전한 작업을 select!에 사용할 때는 별도 태스크로 분리:

```rust
async fn safe_version() {
    // 별도 태스크에서 실행 - 취소되어도 태스크는 계속 실행됨
    let handle = tokio::spawn(async {
        write_to_database().await
    });
    
    select! {
        result = handle => { /* 완료 처리 */ }
        _ = timeout() => {
            // handle.abort()는 하지 않는다
            // 태스크는 계속 실행되고 완료를 기다리지 않음
        }
    }
}
```

---

## 8.7 블로킹 작업 처리

CPU 집약적이거나 블로킹 I/O 작업은 비동기 워커 스레드를 막는다.

### spawn_blocking

```rust
async fn process_replay(replay_data: Vec<u8>) -> Result<ReplayStats, Error> {
    // CPU 집약적 분석 - 블로킹 가능
    let stats = tokio::task::spawn_blocking(move || {
        // 이 블록은 별도 스레드 풀에서 실행됨
        analyze_replay_cpu_intensive(&replay_data)
    }).await??; // JoinHandle<Result<..>>를 두 번 ?
    
    Ok(stats)
}

// 동기 데이터베이스 드라이버 (비동기 지원이 없는 경우)
async fn query_with_sync_driver(conn: DbConn, sql: &str) -> Result<Vec<Row>, Error> {
    let sql = sql.to_string();
    tokio::task::spawn_blocking(move || {
        conn.query(&sql) // 블로킹 쿼리
    }).await?
}
```

### spawn_blocking vs async

| 작업 유형 | 권장 방법 |
|----------|----------|
| 비동기 I/O (tokio::fs, sqlx) | async/await |
| CPU 집약적 계산 | spawn_blocking |
| 동기 라이브러리 사용 | spawn_blocking |
| 파일 I/O (tokio::fs 사용 가능) | tokio::fs (비동기) |

---

## 8.8 타이머와 타임아웃

게임 서버에서 타이머는 핵심 기능이다.

```rust
use tokio::time::{sleep, interval, timeout, Duration, Instant};

// 단순 지연
async fn delayed_respawn(player_id: u64) {
    sleep(Duration::from_secs(5)).await;
    spawn_player(player_id).await;
}

// 주기적 실행 (게임 틱)
async fn game_tick_loop(mut world: GameWorld) {
    let tick_duration = Duration::from_millis(50); // 20 TPS
    let mut interval = interval(tick_duration);
    interval.set_missed_tick_behavior(tokio::time::MissedTickBehavior::Skip);
    
    loop {
        interval.tick().await;
        
        let tick_start = Instant::now();
        world.update_tick().await;
        let elapsed = tick_start.elapsed();
        
        if elapsed > tick_duration {
            log::warn!("Tick overrun: {:?}", elapsed);
        }
    }
}

// 타임아웃
async fn connect_with_timeout(addr: &str) -> Result<TcpStream, ConnectionError> {
    match timeout(Duration::from_secs(5), TcpStream::connect(addr)).await {
        Ok(Ok(stream)) => Ok(stream),
        Ok(Err(e)) => Err(ConnectionError::Io(e)),
        Err(_) => Err(ConnectionError::Timeout),
    }
}
```

---

## 8.9 실전 게임 서버 아키텍처

모든 비동기 개념을 통합한 실제 게임 서버 구조:

```rust
use tokio::net::TcpListener;
use tokio::sync::{mpsc, broadcast, watch};
use std::sync::Arc;

// 전체 서버 상태
pub struct GameServer {
    config: Arc<ServerConfig>,
    sessions: Arc<SessionManager>,
    rooms: Arc<RoomManager>,
    shutdown_tx: watch::Sender<bool>,
}

// 세션 매니저 내부 구조
struct SessionManagerInner {
    sessions: DashMap<u64, Arc<SessionHandle>>,
    broadcast_tx: broadcast::Sender<ServerBroadcast>,
}

// 각 세션의 핸들 (태스크 간 통신용)
pub struct SessionHandle {
    pub id: u64,
    pub player_id: Option<u64>,
    pub outgoing_tx: mpsc::Sender<OutgoingPacket>,
    pub shutdown_tx: watch::Sender<bool>,
}

impl GameServer {
    pub async fn run(&self) -> anyhow::Result<()> {
        let listener = TcpListener::bind(&self.config.bind_addr).await?;
        log::info!("Server listening on {}", self.config.bind_addr);
        
        let mut shutdown_rx = self.shutdown_tx.subscribe();
        
        loop {
            select! {
                accept_result = listener.accept() => {
                    let (socket, addr) = accept_result?;
                    log::info!("New connection from {}", addr);
                    
                    let server = Arc::new(self.clone_handles());
                    tokio::spawn(async move {
                        if let Err(e) = server.handle_connection(socket, addr).await {
                            log::error!("Connection error: {}", e);
                        }
                    });
                }
                
                _ = shutdown_rx.changed() => {
                    if *shutdown_rx.borrow() {
                        log::info!("Shutdown signal received");
                        break;
                    }
                }
            }
        }
        
        // Graceful shutdown
        self.graceful_shutdown().await;
        Ok(())
    }
    
    async fn handle_connection(
        &self,
        socket: TcpStream,
        addr: std::net::SocketAddr,
    ) -> anyhow::Result<()> {
        let session_id = generate_session_id();
        
        // 세션 아웃고잉 채널
        let (outgoing_tx, mut outgoing_rx) = mpsc::channel::<OutgoingPacket>(1000);
        
        // 세션 종료 채널
        let (shutdown_tx, shutdown_rx) = watch::channel(false);
        
        // 세션 핸들 등록
        let handle = Arc::new(SessionHandle {
            id: session_id,
            player_id: None,
            outgoing_tx,
            shutdown_tx,
        });
        self.sessions.register(Arc::clone(&handle)).await;
        
        // 소켓 분리
        let (read_half, write_half) = socket.into_split();
        
        // 수신 태스크
        let recv_handle = {
            let sessions = Arc::clone(&self.sessions);
            let handle = Arc::clone(&handle);
            tokio::spawn(async move {
                session_recv_loop(session_id, read_half, sessions, handle, shutdown_rx).await
            })
        };
        
        // 송신 태스크
        let send_handle = tokio::spawn(async move {
            session_send_loop(write_half, outgoing_rx).await
        });
        
        // 두 태스크 중 하나라도 종료되면 정리
        select! {
            r = recv_handle => { log::debug!("Recv loop ended: {:?}", r); }
            r = send_handle => { log::debug!("Send loop ended: {:?}", r); }
        }
        
        self.sessions.unregister(session_id).await;
        Ok(())
    }
}

// 수신 루프
async fn session_recv_loop(
    session_id: u64,
    mut reader: tokio::net::tcp::OwnedReadHalf,
    sessions: Arc<SessionManager>,
    handle: Arc<SessionHandle>,
    mut shutdown_rx: watch::Receiver<bool>,
) -> Result<(), SessionError> {
    let mut codec = PacketCodec::new();
    let mut buf = BytesMut::with_capacity(4096);
    
    loop {
        select! {
            read_result = reader.read_buf(&mut buf) => {
                match read_result {
                    Ok(0) => return Ok(()), // 연결 종료
                    Ok(_) => {
                        while let Some(packet) = codec.decode(&mut buf)? {
                            sessions.dispatch_packet(session_id, packet).await?;
                        }
                    }
                    Err(e) => return Err(SessionError::Io(e)),
                }
            }
            
            _ = shutdown_rx.changed() => {
                if *shutdown_rx.borrow() {
                    return Ok(());
                }
            }
        }
    }
}

// 송신 루프
async fn session_send_loop(
    mut writer: tokio::net::tcp::OwnedWriteHalf,
    mut rx: mpsc::Receiver<OutgoingPacket>,
) -> Result<(), SessionError> {
    while let Some(packet) = rx.recv().await {
        let bytes = packet.serialize();
        writer.write_all(&bytes).await
            .map_err(SessionError::Io)?;
    }
    Ok(())
}
```

---

## 8.10 성능 최적화 포인트

### 채널 버퍼 크기

```rust
// 너무 작으면 백압력(backpressure) 발생 - 생산자가 블로킹됨
// 너무 크면 메모리 낭비 및 지연 증가

// 게임 서버 권장값
let (tx, rx) = mpsc::channel(1_000);    // 일반 패킷
let (tx, rx) = mpsc::channel(10_000);  // 높은 처리량 필요 시
let (tx, rx) = mpsc::unbounded_channel(); // 백압력이 필요 없고 메모리 예산이 있을 때
```

### 태스크 수 제어

```rust
use tokio::sync::Semaphore;

// 동시 DB 연결 수 제한
let db_semaphore = Arc::new(Semaphore::new(100));

async fn db_query_with_limit(
    sem: Arc<Semaphore>,
    db: Arc<Database>,
    query: &str,
) -> Result<Vec<Row>, DbError> {
    let _permit = sem.acquire().await?; // 100개 이상은 대기
    db.query(query).await
    // _permit이 drop되면 슬롯 반환
}
```

### 배치 처리

```rust
use tokio::time::{sleep, Duration};

// 여러 요청을 모아서 한 번에 처리 (배치)
async fn batch_processor(mut rx: mpsc::Receiver<BatchRequest>) {
    let mut batch: Vec<BatchRequest> = Vec::with_capacity(100);
    let batch_interval = Duration::from_millis(5);
    
    loop {
        let deadline = sleep(batch_interval);
        tokio::pin!(deadline);
        
        loop {
            select! {
                Some(req) = rx.recv() => {
                    batch.push(req);
                    if batch.len() >= 100 { break; } // 최대 배치 크기
                }
                _ = &mut deadline => break, // 타임아웃
                else => return, // 채널 닫힘
            }
        }
        
        if !batch.is_empty() {
            process_batch(std::mem::take(&mut batch)).await;
        }
    }
}
```

---

## 8.11 AI에게 비동기 코드 지시를 내리는 법

### 런타임과 채널 구조 명시

> "Tokio 멀티스레드 런타임 기반으로 작성해줘.
> 세션 → 게임 로직 방향은 mpsc 채널(버퍼 10000)로,
> 게임 로직 → 세션 방향은 각 세션의 outgoing mpsc 채널(버퍼 1000)로,
> 서버 브로드캐스트(채팅, 공지)는 broadcast 채널로 설계해줘."

### 블로킹 작업 분리 명시

> "이 함수는 CPU 집약적인 AI 경로 계산을 해.
> async 함수 안에서 직접 호출하지 말고 spawn_blocking으로 분리해줘."

### select! 취소 안전성 확인

> "이 select! 코드에서 db_operation 분기가 취소될 때 
> 부분적으로 완료된 DB 작업이 있을 수 있어.
> db_operation을 별도 spawn 태스크로 분리해서 
> select!가 취소해도 태스크는 계속 실행되도록 해줘."

### Mutex 사용 오류 수정

> "이 코드에서 std::sync::Mutex의 guard를 
> .await 지점을 가로질러 들고 있어.
> tokio::sync::Mutex로 교체하거나,
> guard를 await 전에 명시적으로 drop하도록 수정해줘."

---

## 8.12 정리

이 챕터의 핵심:

async/await 기본 원리 — `async fn`은 Future를 반환하고, `await`으로 진행한다. 런타임이 여러 Future를 협력적으로 스케줄링한다.

Tokio — Rust 게임 서버의 표준 비동기 런타임. 멀티스레드 워크 스틸링 스케줄러로 코어를 효율적으로 활용한다.

채널 선택 — mpsc(다대일 작업 큐), oneshot(요청-응답), broadcast(팬아웃), watch(상태 공유). 채널 선택이 아키텍처를 결정한다.

select! 취소 안전성 — 선택되지 않은 Future는 취소된다. 부분 완료 상태가 문제될 수 있는 작업은 별도 spawn 태스크로 분리한다.

블로킹 작업 — CPU 집약적 작업이나 동기 라이브러리는 `spawn_blocking`으로 분리한다. 비동기 워커 스레드를 막으면 전체 성능이 저하된다.

---

*다음 챕터: Chapter 9 — 게임 서버에서 자주 쓰는 크레이트 지도*
