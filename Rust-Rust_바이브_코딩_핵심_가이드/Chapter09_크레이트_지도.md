# Chapter 9. 게임 서버에서 자주 쓰는 크레이트 지도

> "Rust 생태계는 작은 표준 라이브러리와 풍부한 크레이트로 구성된다. 
> 게임 서버 개발자에게 필요한 크레이트들을 한눈에 정리한다."

---

## 9.1 크레이트 생태계 철학

Rust 표준 라이브러리(`std`)는 의도적으로 작게 유지된다. 모든 것을 표준에 넣으면 버전 안정성 유지가 어렵기 때문이다. 대신 `cargo` 생태계의 크레이트들이 실용적 기능을 제공한다.

이것이 처음에는 불편하게 느껴질 수 있다. "왜 HTTP 클라이언트가 표준에 없지?" 하지만 이 접근법의 장점은 각 영역에서 가장 좋은 라이브러리가 생태계 표준이 될 수 있다는 것이다.

`crates.io`에서 원하는 크레이트를 찾고, `Cargo.toml`에 추가한다:

```toml
[dependencies]
tokio = { version = "1", features = ["full"] }
serde = { version = "1", features = ["derive"] }
axum = "0.7"
sqlx = { version = "0.7", features = ["postgres", "runtime-tokio-rustls"] }
```

---

## 9.2 비동기 런타임: tokio

```toml
[dependencies]
tokio = { version = "1", features = ["full"] }
tokio-util = "0.7"
```

Tokio는 이미 Chapter 8에서 상세히 다뤘으므로, 여기서는 자주 쓰는 서브 기능들을 정리한다.

### tokio::fs — 비동기 파일 I/O

```rust
use tokio::fs;
use tokio::io::{AsyncReadExt, AsyncWriteExt};

// 설정 파일 읽기
async fn load_config(path: &str) -> anyhow::Result<ServerConfig> {
    let content = fs::read_to_string(path).await?;
    Ok(serde_json::from_str(&content)?)
}

// 로그 파일 쓰기
async fn append_log(path: &str, entry: &str) -> anyhow::Result<()> {
    let mut file = fs::OpenOptions::new()
        .append(true)
        .create(true)
        .open(path)
        .await?;
    file.write_all(entry.as_bytes()).await?;
    file.write_all(b"\n").await?;
    Ok(())
}
```

### tokio-util — 코덱과 프레이밍

게임 서버에서 TCP 스트림을 패킷으로 나누는 코덱이 필수적이다:

```rust
use tokio_util::codec::{Encoder, Decoder, Framed};
use bytes::{BytesMut, BufMut, Buf};

// 길이-접두사 방식 패킷 코덱
struct GamePacketCodec;

impl Decoder for GamePacketCodec {
    type Item = Vec<u8>;
    type Error = std::io::Error;
    
    fn decode(&mut self, src: &mut BytesMut) -> Result<Option<Vec<u8>>, Self::Error> {
        if src.len() < 4 {
            return Ok(None); // 헤더가 아직 없음
        }
        
        let length = u32::from_be_bytes([src[0], src[1], src[2], src[3]]) as usize;
        
        if src.len() < 4 + length {
            src.reserve(4 + length - src.len());
            return Ok(None); // 데이터가 아직 덜 옴
        }
        
        src.advance(4);
        Ok(Some(src.split_to(length).to_vec()))
    }
}

impl Encoder<Vec<u8>> for GamePacketCodec {
    type Error = std::io::Error;
    
    fn encode(&mut self, item: Vec<u8>, dst: &mut BytesMut) -> Result<(), Self::Error> {
        dst.put_u32(item.len() as u32);
        dst.put_slice(&item);
        Ok(())
    }
}

// 사용
use tokio::net::TcpStream;

async fn handle_framed(stream: TcpStream) {
    let mut framed = Framed::new(stream, GamePacketCodec);
    
    use futures::StreamExt;
    while let Some(result) = framed.next().await {
        match result {
            Ok(packet) => handle_packet(packet).await,
            Err(e) => { log::error!("Decode error: {}", e); break; }
        }
    }
}
```

---

## 9.3 네트워크 프레임워크

### axum — HTTP/WebSocket 서버

```toml
[dependencies]
axum = "0.7"
tower = "0.4"
tower-http = { version = "0.5", features = ["cors", "trace"] }
```

REST API와 WebSocket을 모두 지원한다:

```rust
use axum::{
    Router,
    routing::{get, post},
    extract::{State, Json, WebSocketUpgrade},
    response::IntoResponse,
};
use std::sync::Arc;

type AppState = Arc<GameServer>;

// REST API 라우트
fn create_api_router(state: AppState) -> Router {
    Router::new()
        .route("/health", get(health_check))
        .route("/players/:id", get(get_player))
        .route("/rooms", get(list_rooms).post(create_room))
        .route("/ws", get(websocket_handler))
        .with_state(state)
        .layer(tower_http::cors::CorsLayer::permissive())
        .layer(tower_http::trace::TraceLayer::new_for_http())
}

async fn health_check() -> &'static str {
    "OK"
}

async fn get_player(
    State(server): State<AppState>,
    axum::extract::Path(id): axum::extract::Path<u64>,
) -> impl IntoResponse {
    match server.get_player_info(id).await {
        Ok(player) => (axum::http::StatusCode::OK, Json(player)).into_response(),
        Err(e) => (axum::http::StatusCode::NOT_FOUND, e.to_string()).into_response(),
    }
}

async fn websocket_handler(
    ws: WebSocketUpgrade,
    State(server): State<AppState>,
) -> impl IntoResponse {
    ws.on_upgrade(move |socket| handle_ws_connection(socket, server))
}

async fn handle_ws_connection(
    mut socket: axum::extract::ws::WebSocket,
    server: AppState,
) {
    use axum::extract::ws::Message;
    
    while let Some(Ok(msg)) = socket.recv().await {
        match msg {
            Message::Binary(data) => {
                let response = server.process_packet(data).await;
                if socket.send(Message::Binary(response)).await.is_err() {
                    break;
                }
            }
            Message::Close(_) => break,
            _ => {}
        }
    }
}
```

### tokio-tungstenite — WebSocket (axum 없이)

TCP 위에 WebSocket 프로토콜만 필요할 때:

```toml
[dependencies]
tokio-tungstenite = "0.21"
```

```rust
use tokio_tungstenite::{accept_async, tungstenite::Message};

async fn handle_ws(stream: TcpStream) -> anyhow::Result<()> {
    let ws_stream = accept_async(stream).await?;
    let (mut write, mut read) = ws_stream.split();
    
    use futures::{StreamExt, SinkExt};
    while let Some(msg) = read.next().await {
        match msg? {
            Message::Binary(data) => {
                let response = process(data).await;
                write.send(Message::Binary(response)).await?;
            }
            Message::Close(_) => break,
            _ => {}
        }
    }
    
    Ok(())
}
```

### tonic — gRPC 서버/클라이언트

서버 간 통신에 gRPC를 사용할 때:

```toml
[dependencies]
tonic = "0.11"
prost = "0.12"

[build-dependencies]
tonic-build = "0.11"
```

```protobuf
// proto/game.proto
syntax = "proto3";
package game;

service GameService {
    rpc GetPlayer (GetPlayerRequest) returns (Player);
    rpc JoinRoom (JoinRoomRequest) returns (JoinRoomResponse);
}

message GetPlayerRequest { uint64 player_id = 1; }
message Player {
    uint64 id = 1;
    string name = 2;
    uint32 level = 3;
}
```

```rust
// build.rs
fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_build::compile_protos("proto/game.proto")?;
    Ok(())
}
```

```rust
// src/grpc_server.rs
use tonic::{transport::Server, Request, Response, Status};

pub mod game {
    tonic::include_proto!("game");
}

use game::game_service_server::{GameService, GameServiceServer};
use game::{GetPlayerRequest, Player};

struct GameServiceImpl {
    db: Arc<Database>,
}

#[tonic::async_trait]
impl GameService for GameServiceImpl {
    async fn get_player(
        &self,
        request: Request<GetPlayerRequest>,
    ) -> Result<Response<Player>, Status> {
        let id = request.into_inner().player_id;
        
        match self.db.get_player(id).await {
            Ok(player) => Ok(Response::new(Player {
                id: player.id,
                name: player.name,
                level: player.level,
            })),
            Err(_) => Err(Status::not_found(format!("Player {} not found", id))),
        }
    }
}

pub async fn run_grpc_server(db: Arc<Database>) -> anyhow::Result<()> {
    Server::builder()
        .add_service(GameServiceServer::new(GameServiceImpl { db }))
        .serve("0.0.0.0:50051".parse()?)
        .await?;
    Ok(())
}
```

---

## 9.4 직렬화: serde 생태계

```toml
[dependencies]
serde = { version = "1", features = ["derive"] }
serde_json = "1"           # JSON
bincode = "1"              # 빠른 바이너리
rmp-serde = "1"            # MessagePack
prost = "0.12"             # Protobuf
```

### 게임 서버 직렬화 선택 기준

| 포맷 | 크기 | 속도 | 스키마 | 용도 |
|------|------|------|--------|------|
| JSON | 큼 | 보통 | 없음 | REST API, 설정 파일 |
| bincode | 작음 | 매우 빠름 | 없음 | 인메모리 캐시, 동일 언어 간 |
| MessagePack | 작음 | 빠름 | 없음 | 언어 간 바이너리 |
| Protobuf | 매우 작음 | 빠름 | 있음 | 언어 간, 버전 관리 필요시 |

실시간 게임 패킷: bincode 또는 Protobuf를 추천한다.

```rust
use serde::{Serialize, Deserialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct MovePacket {
    pub player_id: u64,
    pub position: [f32; 3],
    pub velocity: [f32; 3],
    pub timestamp: u64,
}

// JSON (REST API)
let json = serde_json::to_string(&packet)?;
let packet: MovePacket = serde_json::from_str(&json)?;

// bincode (TCP 게임 패킷)
let bytes = bincode::serialize(&packet)?;
let packet: MovePacket = bincode::deserialize(&bytes)?;
```

### 커스텀 직렬화

```rust
use serde::{Serializer, Deserializer};

#[derive(Debug)]
struct PlayerId(u64);

impl Serialize for PlayerId {
    fn serialize<S: Serializer>(&self, serializer: S) -> Result<S::Ok, S::Error> {
        serializer.serialize_u64(self.0)
    }
}

impl<'de> Deserialize<'de> for PlayerId {
    fn deserialize<D: Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        let id = u64::deserialize(deserializer)?;
        Ok(PlayerId(id))
    }
}
```

---

## 9.5 데이터베이스

### sqlx — 컴파일 타임 SQL 검증

```toml
[dependencies]
sqlx = { version = "0.7", features = [
    "runtime-tokio-rustls",
    "postgres",      # 또는 "mysql", "sqlite"
    "macros",
    "migrate",
] }
```

sqlx의 핵심 장점은 **컴파일 타임 SQL 검증**이다:

```rust
use sqlx::PgPool;

// sqlx::query_as! 매크로 - 컴파일 타임에 SQL 문법과 타입 검증
#[derive(Debug, sqlx::FromRow)]
pub struct PlayerRow {
    pub id: i64,
    pub name: String,
    pub level: i32,
    pub gold: i64,
    pub created_at: chrono::DateTime<chrono::Utc>,
}

pub async fn get_player(pool: &PgPool, id: i64) -> Result<Option<PlayerRow>, sqlx::Error> {
    // SQL 오류가 있으면 컴파일 실패! (환경 변수 DATABASE_URL 필요)
    sqlx::query_as!(
        PlayerRow,
        r#"
        SELECT id, name, level, gold, created_at
        FROM players
        WHERE id = $1
        "#,
        id
    )
    .fetch_optional(pool)
    .await
}

pub async fn update_player_gold(
    pool: &PgPool,
    id: i64,
    delta: i64,
) -> Result<i64, sqlx::Error> {
    let new_gold = sqlx::query_scalar!(
        "UPDATE players SET gold = gold + $1 WHERE id = $2 RETURNING gold",
        delta,
        id
    )
    .fetch_one(pool)
    .await?;
    
    Ok(new_gold)
}

// 트랜잭션
pub async fn transfer_gold(
    pool: &PgPool,
    from_id: i64,
    to_id: i64,
    amount: i64,
) -> Result<(), sqlx::Error> {
    let mut tx = pool.begin().await?;
    
    sqlx::query!(
        "UPDATE players SET gold = gold - $1 WHERE id = $2 AND gold >= $1",
        amount, from_id
    )
    .execute(&mut *tx)
    .await?;
    
    sqlx::query!(
        "UPDATE players SET gold = gold + $1 WHERE id = $2",
        amount, to_id
    )
    .execute(&mut *tx)
    .await?;
    
    tx.commit().await?;
    Ok(())
}
```

### Redis — 캐시와 세션 스토어

```toml
[dependencies]
redis = { version = "0.24", features = ["tokio-comp", "connection-manager"] }
```

```rust
use redis::AsyncCommands;

pub struct RedisCache {
    conn: redis::aio::ConnectionManager,
}

impl RedisCache {
    pub async fn new(url: &str) -> anyhow::Result<Self> {
        let client = redis::Client::open(url)?;
        let conn = redis::aio::ConnectionManager::new(client).await?;
        Ok(RedisCache { conn })
    }
    
    // 세션 토큰 저장 (30분 TTL)
    pub async fn store_session(
        &mut self,
        token: &str,
        player_id: u64,
    ) -> anyhow::Result<()> {
        let key = format!("session:{}", token);
        self.conn.set_ex(&key, player_id, 1800).await?;
        Ok(())
    }
    
    // 세션 조회
    pub async fn get_session(&mut self, token: &str) -> anyhow::Result<Option<u64>> {
        let key = format!("session:{}", token);
        let id: Option<u64> = self.conn.get(&key).await?;
        Ok(id)
    }
    
    // 랭킹 (Sorted Set)
    pub async fn update_player_score(
        &mut self,
        player_id: u64,
        score: f64,
    ) -> anyhow::Result<()> {
        self.conn.zadd("leaderboard", score, player_id).await?;
        Ok(())
    }
    
    pub async fn get_top_players(&mut self, count: i64) -> anyhow::Result<Vec<(u64, f64)>> {
        let results: Vec<(u64, f64)> = self.conn
            .zrevrange_withscores("leaderboard", 0, count - 1)
            .await?;
        Ok(results)
    }
    
    // Pub/Sub (서버 간 이벤트)
    pub async fn publish_event(&mut self, channel: &str, event: &str) -> anyhow::Result<()> {
        self.conn.publish(channel, event).await?;
        Ok(())
    }
}
```

---

## 9.6 로깅과 추적: tracing 생태계

```toml
[dependencies]
tracing = "0.1"
tracing-subscriber = { version = "0.3", features = ["env-filter", "json"] }
tracing-appender = "0.2"
```

단순한 로그가 아니라 **분산 추적(distributed tracing)**을 지원하는 생태계다.

### 기본 설정

```rust
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt, EnvFilter};

fn init_tracing() {
    let env_filter = EnvFilter::try_from_default_env()
        .unwrap_or_else(|_| EnvFilter::new("info"));
    
    // 파일 로그 (비동기, 매일 롤링)
    let file_appender = tracing_appender::rolling::daily("logs", "game-server");
    let (non_blocking, _guard) = tracing_appender::non_blocking(file_appender);
    
    tracing_subscriber::registry()
        .with(env_filter)
        .with(
            tracing_subscriber::fmt::layer()
                .with_target(false)
                .with_thread_ids(true)
        )
        .with(
            tracing_subscriber::fmt::layer()
                .json()
                .with_writer(non_blocking)
        )
        .init();
}
```

### 구조화된 로깅과 span

```rust
use tracing::{info, warn, error, instrument, Span};

// #[instrument]로 자동으로 함수 span 생성
#[instrument(skip(db), fields(player_id = %id))]
async fn handle_login(
    db: &Database,
    id: u64,
    password: &str,
) -> Result<Session, AuthError> {
    info!("Login attempt");
    
    let player = db.get_player(id).await
        .map_err(|e| {
            error!(error = %e, "Database error during login");
            AuthError::Database(e)
        })?;
    
    if !verify_password(password, &player.password_hash) {
        warn!("Invalid password attempt");
        return Err(AuthError::InvalidCredentials);
    }
    
    let session = create_session(id).await?;
    info!(session_id = session.id, "Login successful");
    
    Ok(session)
}

// 수동 span 생성
async fn process_batch(packets: Vec<Packet>) {
    let span = tracing::info_span!("process_batch", packet_count = packets.len());
    
    async {
        for packet in packets {
            process_one(packet).await;
        }
    }
    .instrument(span)
    .await;
}
```

### OpenTelemetry 연동 (분산 추적)

```toml
[dependencies]
opentelemetry = "0.21"
opentelemetry-jaeger = "0.20"
tracing-opentelemetry = "0.22"
```

```rust
fn init_opentelemetry() -> anyhow::Result<()> {
    let tracer = opentelemetry_jaeger::new_agent_pipeline()
        .with_service_name("game-server")
        .install_batch(opentelemetry_sdk::runtime::Tokio)?;
    
    tracing_subscriber::registry()
        .with(tracing_opentelemetry::layer().with_tracer(tracer))
        .init();
    
    Ok(())
}
```

---

## 9.7 설정 관리

```toml
[dependencies]
config = "0.13"
```

```rust
use serde::Deserialize;
use config::{Config, Environment, File};

#[derive(Debug, Deserialize)]
pub struct ServerConfig {
    pub host: String,
    pub port: u16,
    pub database_url: String,
    pub redis_url: String,
    pub max_connections: u32,
    pub tick_rate: u32,
    pub log_level: String,
}

pub fn load_config() -> anyhow::Result<ServerConfig> {
    let config = Config::builder()
        // 기본값 파일
        .add_source(File::with_name("config/default"))
        // 환경별 오버라이드 (config/production.toml 등)
        .add_source(File::with_name(&format!(
            "config/{}",
            std::env::var("APP_ENV").unwrap_or_else(|_| "development".into())
        )).required(false))
        // 환경변수 오버라이드 (APP__DATABASE_URL 등)
        .add_source(Environment::with_prefix("APP").separator("__"))
        .build()?;
    
    Ok(config.try_deserialize()?)
}
```

설정 파일 예시:

```toml
# config/default.toml
host = "0.0.0.0"
port = 9000
database_url = "postgres://localhost/gamedb"
redis_url = "redis://localhost"
max_connections = 10000
tick_rate = 20
log_level = "info"
```

---

## 9.8 시간과 날짜: chrono / time

```toml
[dependencies]
chrono = { version = "0.4", features = ["serde"] }
```

```rust
use chrono::{Utc, Duration, DateTime};

// 현재 시간
let now: DateTime<Utc> = Utc::now();

// 만료 시간 계산
let token_expiry = now + Duration::hours(24);

// 데이터베이스에서 읽은 타임스탬프
let created_at: DateTime<Utc> = row.created_at;
let age = now - created_at;

if age > Duration::days(30) {
    log::info!("Account is over 30 days old");
}

// 직렬화 (serde feature)
#[derive(Serialize, Deserialize)]
struct PlayerRecord {
    #[serde(with = "chrono::serde::ts_milliseconds")]
    last_login: DateTime<Utc>,
}
```

---

## 9.9 난수: rand

```toml
[dependencies]
rand = "0.8"
```

```rust
use rand::{Rng, SeedableRng};
use rand::rngs::StdRng;

// 스레드 로컬 난수 생성기 (빠름)
let mut rng = rand::thread_rng();

// 아이템 드롭 확률
let drop_chance: f64 = rng.gen::<f64>(); // 0.0 ~ 1.0
if drop_chance < 0.05 {
    println!("Rare item dropped!");
}

// 범위 내 정수
let damage: u32 = rng.gen_range(50..=100);

// 벡터 섞기 (던전 방 순서 등)
let mut rooms: Vec<RoomId> = all_rooms.clone();
use rand::seq::SliceRandom;
rooms.shuffle(&mut rng);

// 재현 가능한 시드 (테스트, 리플레이)
let mut seeded_rng = StdRng::seed_from_u64(42);
let value: u32 = seeded_rng.gen_range(0..100);
```

---

## 9.10 수학과 게임 물리: glam, nalgebra

```toml
[dependencies]
glam = "0.25"       # 게임용 수학 라이브러리 (빠름)
nalgebra = "0.32"   # 선형 대수 (더 포괄적)
```

### glam — 게임 개발에 최적화

```rust
use glam::{Vec2, Vec3, Quat, Mat4};

// 2D 위치와 이동
let position = Vec2::new(100.0, 200.0);
let velocity = Vec2::new(5.0, -3.0);
let dt = 0.016; // 16ms
let new_position = position + velocity * dt;

// 3D 변환
let translation = Vec3::new(1.0, 2.0, 3.0);
let rotation = Quat::from_rotation_y(std::f32::consts::FRAC_PI_4); // 45도
let scale = Vec3::ONE;
let transform = Mat4::from_scale_rotation_translation(scale, rotation, translation);

// 충돌 감지 - 두 원 겹침
fn circles_overlap(pos1: Vec2, r1: f32, pos2: Vec2, r2: f32) -> bool {
    pos1.distance_squared(pos2) < (r1 + r2).powi(2)
}

// A* 등에서 맨해튼 거리
fn manhattan_distance(a: Vec2, b: Vec2) -> f32 {
    (a.x - b.x).abs() + (a.y - b.y).abs()
}
```

---

## 9.11 메트릭: metrics / prometheus

```toml
[dependencies]
metrics = "0.22"
metrics-exporter-prometheus = "0.13"
```

```rust
use metrics::{counter, gauge, histogram};

// 메트릭 초기화
fn init_metrics() {
    metrics_exporter_prometheus::PrometheusBuilder::new()
        .with_http_listener(([0, 0, 0, 0], 9090))
        .install()
        .expect("Failed to install Prometheus exporter");
}

// 코드 곳곳에서 메트릭 기록
async fn handle_packet(packet: &Packet) {
    counter!("packets_received_total", "type" => packet.type_name()).increment(1);
    
    let start = std::time::Instant::now();
    process_packet(packet).await;
    let elapsed = start.elapsed().as_secs_f64();
    
    histogram!("packet_processing_duration_seconds", "type" => packet.type_name())
        .record(elapsed);
}

async fn update_session_count(count: u64) {
    gauge!("active_sessions").set(count as f64);
}
```

---

## 9.12 ECS: bevy_ecs (선택적)

대규모 게임 서버에서 엔티티-컴포넌트-시스템 패턴이 필요할 때:

```toml
[dependencies]
bevy_ecs = "0.13"  # bevy 게임 엔진의 ECS만 독립적으로 사용
```

```rust
use bevy_ecs::prelude::*;

// 컴포넌트 정의
#[derive(Component)]
struct Position { x: f32, y: f32 }

#[derive(Component)]
struct Velocity { x: f32, y: f32 }

#[derive(Component)]
struct Health(u32);

#[derive(Component)]
struct InCombat;

// 시스템 정의
fn movement_system(mut query: Query<(&Velocity, &mut Position)>, dt: Res<DeltaTime>) {
    for (vel, mut pos) in query.iter_mut() {
        pos.x += vel.x * dt.0;
        pos.y += vel.y * dt.0;
    }
}

fn combat_system(
    mut commands: Commands,
    mut attackers: Query<(&mut Health, &InCombat)>,
) {
    for (mut health, _) in attackers.iter_mut() {
        health.0 = health.0.saturating_sub(5);
        if health.0 == 0 {
            // 죽은 엔티티 처리
        }
    }
}

// 월드 구성
fn setup_game() -> World {
    let mut world = World::new();
    
    // 엔티티 생성
    world.spawn((
        Position { x: 0.0, y: 0.0 },
        Velocity { x: 1.0, y: 0.0 },
        Health(100),
    ));
    
    world
}
```

ECS는 대규모 게임 월드 시뮬레이션에 적합하지만, 소규모 멀티플레이어 게임에는 오히려 복잡성이 증가할 수 있다. 신중하게 도입해야 한다.

---

## 9.13 추천 Cargo.toml 템플릿

게임 서버를 시작할 때 기본으로 포함할 의존성:

```toml
[package]
name = "game-server"
version = "0.1.0"
edition = "2021"

[dependencies]
# 비동기 런타임
tokio = { version = "1", features = ["full"] }
tokio-util = { version = "0.7", features = ["codec"] }
futures = "0.3"

# 네트워크
axum = "0.7"
tower-http = { version = "0.5", features = ["cors", "trace"] }
tokio-tungstenite = "0.21"

# 직렬화
serde = { version = "1", features = ["derive"] }
serde_json = "1"
bincode = "1"

# 데이터베이스
sqlx = { version = "0.7", features = ["runtime-tokio-rustls", "postgres", "macros", "migrate", "chrono"] }
redis = { version = "0.24", features = ["tokio-comp", "connection-manager"] }

# 동시성
dashmap = "5"
parking_lot = "0.12"

# 에러 처리
anyhow = "1"
thiserror = "1"

# 로깅
tracing = "0.1"
tracing-subscriber = { version = "0.3", features = ["env-filter", "json"] }
tracing-appender = "0.2"

# 유틸리티
chrono = { version = "0.4", features = ["serde"] }
rand = "0.8"
uuid = { version = "1", features = ["v4"] }
config = "0.13"
bytes = "1"

# 메트릭
metrics = "0.22"
metrics-exporter-prometheus = "0.13"

[profile.release]
opt-level = 3
lto = "thin"
codegen-units = 1

[profile.dev]
opt-level = 0
```

---

## 9.14 AI에게 크레이트 스택 지시를 내리는 법

### 스택을 명확히 지정

> "이 게임 서버 코드는 다음 스택으로 작성해줘:
> - 런타임: Tokio (멀티스레드)
> - HTTP API: axum 0.7
> - WebSocket: axum의 WebSocket 지원
> - 데이터베이스: sqlx + PostgreSQL (비동기, 컴파일 타임 쿼리 검증)
> - 캐시: redis crate (tokio-comp feature)
> - 직렬화: serde + bincode (TCP 패킷), serde_json (REST)
> - 로깅: tracing + tracing-subscriber
> - 에러: thiserror (도메인) + anyhow (핸들러)"

### 버전 명시

> "sqlx는 0.7.x 버전으로 해줘.
> sqlx 0.8에서 API가 바뀌었으니 0.7 문법으로 작성해줘."

### 특정 기능만 선택

> "tokio는 full feature 대신 
> rt-multi-thread, net, sync, time, io-util feature만 활성화해줘."

---

## 9.15 정리

이 챕터는 Rust 게임 서버 개발의 생태계 지도다.

핵심 스택 — Tokio(런타임) + axum(HTTP/WS) + sqlx(DB) + serde(직렬화) + tracing(로깅)이 거의 표준이다.

선택의 기준 — "이 작업에 가장 잘 맞는 도구는 무엇인가"를 기준으로 선택한다. 모든 크레이트를 다 쓸 필요는 없다.

버전 명시의 중요성 — AI에게 코드를 요청할 때 크레이트 버전을 명시하지 않으면 구버전 API로 코드를 생성할 수 있다.

스택 고정 — 프로젝트 초기에 사용할 크레이트 스택을 결정하고 AI 지시문에 항상 포함하면 일관된 코드를 얻을 수 있다.

---

*다음 챕터: Chapter 10 — unsafe: 존재하지만 거의 쓸 일이 없다*
