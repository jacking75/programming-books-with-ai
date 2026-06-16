# Chapter 13. AI에게 Rust 코드를 지시할 때의 체크리스트

> "바이브 코딩의 품질은 사람의 지시문 품질에 비례한다. 
> 한 줄 지시와 한 단락 지시가 만드는 코드의 차이는 하늘과 땅 차이다."

---

## 13.1 왜 지시 품질이 중요한가

AI는 문맥이 없으면 가장 평범한 코드를 생성한다. 게임 서버 코드는 평범해서는 안 된다.

다음 두 지시를 비교해 보자:

**지시 A (나쁨)**:
> "게임 룸 매니저 만들어줘"

**지시 B (좋음)**:
> "Tokio 비동기 런타임 기반 게임 룸 매니저를 만들어줘.
> 
> - 룸 저장은 DashMap<u32, Arc<Mutex<Room>>>으로
> - Mutex는 tokio::sync::Mutex (비동기 컨텍스트)
> - 룸 생성/해제/조회 API 제공
> - 에러 타입은 thiserror로 RoomError 정의
> - 로깅은 tracing crate 사용
> - unsafe 코드 없이 safe Rust만으로
> - 공개 API에는 #[instrument] 어노테이션
> - 단위 테스트 포함 (#[tokio::test])"

지시 A로 받은 코드는 아마 `std::sync::Mutex`, 단순 HashMap, unwrap() 투성이일 것이다. 지시 B는 운영 가능한 코드에 훨씬 가깝다.

이 챕터는 좋은 지시문을 구성하는 모든 요소를 체계적으로 정리한다.

---

## 13.2 체크리스트 1: 프로젝트 컨텍스트

### 반드시 명시해야 하는 것들

**비동기 런타임**:
```
- Tokio 멀티스레드 런타임 (게임 서버 기본)
- Tokio 단일 스레드 런타임 (특수 케이스)
- async-std (대안, 드물게 사용)
- 동기 코드 (특별한 이유가 있는 경우)
```

**Rust 에디션**:
```
edition = "2021"  (최신, 권장)
edition = "2018"  (구 프로젝트)
```

**워크스페이스 구조**:
```
- 단일 크레이트 바이너리
- 워크스페이스 내 특정 크레이트
- 라이브러리 크레이트 (다른 크레이트에서 사용)
```

**예시 지시**:
> "Tokio 1.x 멀티스레드 런타임, edition 2021, 
> game-server 워크스페이스 내 server-core 라이브러리 크레이트로 작성해줘."

---

## 13.3 체크리스트 2: 도메인 요구사항

### 동시 접속 규모

AI가 선택하는 알고리즘, 자료구조, 동기화 방식이 달라진다:

```
소규모 (< 100명): 단순 HashMap, std::sync::Mutex
중규모 (100~10,000명): DashMap, parking_lot
대규모 (10,000명+): 커스텀 샤딩, 락프리 구조체 고려
```

**예시**:
> "최대 동시 접속 5,000명 기준으로 설계해줘. 
> 락 경합을 최소화하기 위해 DashMap 사용 권장."

### 지연 목표

```
일반 게임 서버: p99 < 100ms
실시간 액션: p99 < 20ms
캐주얼 게임: p99 < 500ms
```

**예시**:
> "패킷 처리 지연 p99 < 50ms 목표야. 
> DB 조회는 Redis 캐시를 먼저 확인하고, 캐시 미스 시에만 PostgreSQL 조회해줘."

### 영속화 매체

```
PostgreSQL, MySQL, SQLite (sqlx)
Redis (redis crate)
MongoDB (mongodb crate)
파일 시스템 (tokio::fs)
```

### 프로토콜

```
TCP: tokio::net::TcpListener + tokio-util codec
WebSocket: axum WS 또는 tokio-tungstenite
UDP: tokio::net::UdpSocket
gRPC: tonic
HTTP REST: axum
```

### 직렬화 포맷

```
JSON: 디버깅 용이, 크기 큼 → serde_json
bincode: 빠름, Rust 전용 → bincode
MessagePack: 빠름, 언어 중립 → rmp-serde
Protobuf: 스키마 관리, 버전 호환 → prost
FlatBuffers: 역직렬화 비용 없음 → flatbuffers
```

---

## 13.4 체크리스트 3: 소유권/공유 정책

이것이 Rust 코드 품질을 가장 크게 좌우하는 부분이다.

### 소유권 명시 템플릿

```
[구조체 X]는 [타입 Y]를 소유한다.
[구조체 A]와 [구조체 B]는 [타입 C]를 공유한다.
[함수 F]는 [데이터 D]를 읽기만 한다 / 수정한다 / 소유권을 가져간다.
```

### 공유 패턴 선택

```
한 소유자, 나머지 빌림:
"SessionManager가 Session을 소유하고, 핸들러는 &Session으로 빌려서만 사용해줘."

여러 스레드에서 읽기만:
"Config는 Arc<Config>로 감싸서 모든 태스크에서 &Config 참조로 접근해줘."

여러 스레드에서 읽기+쓰기:
"ServerState는 Arc<RwLock<ServerState>>로 만들어줘.
읽기가 훨씬 많으므로 RwLock 선택."

자주 수정하는 공유 상태:
"PlayerStats는 Arc<Mutex<PlayerStats>>로 만들어줘.
Mutex는 tokio::sync::Mutex (await 가로지를 가능성 있음)."

채널로 이동:
"Session 객체는 생성 후 소유권이 핸들러 태스크로 이동해서 
그 태스크만 접근해줘. 다른 태스크와는 mpsc 채널로 통신해줘."
```

### 예시 지시

> "데이터 흐름을 다음과 같이 설계해줘:
> 
> 1. NetworkLayer가 RawPacket을 소유해서 ParseLayer로 이동시킴
> 2. ParseLayer가 ParsedPacket으로 변환해서 GameLogic으로 이동시킴  
> 3. GameLogic은 Arc<ServerState>를 공유 소유권으로 참조
> 4. ServerState 수정은 tokio::sync::Mutex로 보호
> 5. 결과는 oneshot 채널로 요청자에게 반환
> 
> 중간에 clone()으로 버퍼를 복사하지 않도록 해줘."

---

## 13.5 체크리스트 4: 에러 처리 정책

### 계층별 에러 타입

```
라이브러리 계층: thiserror
애플리케이션 계층: anyhow
API 응답: 도메인 에러 → HTTP 상태 코드 변환
```

### 에러 정책 지시 템플릿

```
- 도메인 에러는 thiserror로 [ErrorType] enum 정의
- 각 에러 변형에 #[error("...")]로 메시지 정의
- 외부 라이브러리 에러 변환은 #[from] 사용
- 핸들러 함수 반환 타입은 anyhow::Result<T>
- unwrap()과 expect()는 사용 금지 (프로토타입 표시 제외)
- 패닉: 불변식 위반 시에만 (assert! 또는 unreachable!)
- 에러 로깅: tracing::error!(error = %e, "컨텍스트")
```

### 완전한 에러 정책 예시

> "에러 처리를 다음 정책으로 구현해줘:
> 
> - SessionError, PacketError, GameError는 thiserror로 각각 정의
> - 각 에러에 적절한 #[error] 메시지 추가
> - SessionError는 io::Error, bincode::Error를 #[from]으로 변환
> - 최상위 핸들러는 anyhow::Result<()>를 반환하고 ?로 전파
> - 에러 발생 위치마다 .context() 또는 .with_context()로 문맥 추가
> - unwrap()은 절대 사용하지 말 것
> - 회복 불가한 상황에만 panic! 사용 (명시적 주석 추가)"

---

## 13.6 체크리스트 5: 비동기/동시성 정책

### 비동기 정책 지시 템플릿

```
- 런타임: Tokio 멀티스레드
- async 함수에서 동기 Mutex를 await 가로질러 보유 금지
  → 비동기 컨텍스트에서 수정 필요한 공유 상태는 tokio::sync::Mutex
  → 락 범위를 await 이전에 닫을 수 있으면 std::sync::Mutex도 가능
- CPU 집약적 작업 (10ms 이상)은 spawn_blocking으로 분리
- 동기 라이브러리 호출은 spawn_blocking으로 분리
- select! 사용 시 취소 안전성 주의
  → DB 트랜잭션 등 원자적 작업은 select! 분기에서 사용 금지
- 채널 버퍼 크기 명시: mpsc(1000), broadcast(500)
```

### 예시 지시

> "비동기 패킷 핸들러를 다음 정책으로 작성해줘:
> 
> - 모든 비동기 I/O는 tokio 네이티브 사용
> - 세션 상태 수정은 tokio::sync::Mutex, 읽기만 할 때는 읽기 락
> - 각 세션은 독립 Tokio 태스크로 실행
> - 세션 → 게임 로직: mpsc 채널 (버퍼 1000)
> - 게임 로직 → 세션: 각 세션의 outgoing mpsc 채널 (버퍼 500)
> - 종료 신호는 tokio::sync::watch 채널
> - select! 사용 시 주석으로 취소 안전성 여부 표시"

---

## 13.7 체크리스트 6: 관측성 (Observability)

### 로깅 정책

```
- tracing 크레이트 사용 (log 크레이트 대신)
- 모든 공개 API/핸들러 진입점에 #[instrument]
  → skip: 로그에 찍지 않을 파라미터 (db 연결, 크기 큰 데이터)
  → fields: 로그에 추가할 필드
- 에러 로깅: error! / warn! 레벨, error = %e 필드 포함
- 성능 측정: info_span! + 타이머
```

### 메트릭 정책

```
- metrics 크레이트 (Prometheus 호환)
- counter!: 이벤트 횟수 (패킷 수신, 로그인 성공/실패 등)
- gauge!: 현재 값 (동시 접속자, 룸 수 등)
- histogram!: 분포 (처리 시간, 패킷 크기 등)
```

### 예시 지시

> "관측성을 다음과 같이 구성해줘:
> 
> - 모든 핸들러에 #[instrument(skip(db, state), fields(session_id = %id))]
> - 패킷 타입별 counter!('packets_processed_total')
> - 처리 시간 histogram!('packet_processing_ms')
> - 활성 세션 수 gauge!('active_sessions')
> - 에러는 tracing::error!(error = %e, session_id, packet_type)"

---

## 13.8 체크리스트 7: 테스트와 품질

### 테스트 정책

```
- 모든 공개 함수에 단위 테스트 (#[test] 또는 #[tokio::test])
- 비동기 테스트: #[tokio::test]
- DB 테스트: 테스트 전용 DB나 in-memory SQLite
- 프로퍼티 테스트: proptest 크레이트 (경계값 자동 탐색)
- 통합 테스트: tests/ 디렉토리
```

### 품질 게이트

```
cargo clippy -- -D warnings    # 경고 없음
cargo fmt --check              # 포매팅 일치
cargo test                     # 모든 테스트 통과
```

### 예시 지시

> "테스트를 다음과 같이 포함해줘:
> 
> - 각 pub 함수마다 #[cfg(test)] 모듈에 최소 한 개 테스트
> - 정상 케이스와 에러 케이스 모두
> - 비동기 함수는 #[tokio::test]
> - 테스트 격리: 테스트 간 상태 공유 없음
> - DB 테스트는 sqlx::test 매크로 활용
> - cargo clippy -- -D warnings 통과하도록"

---

## 13.9 체크리스트 8: 보안과 안전

### 안전 정책

```
- unsafe 코드 사용 금지 (FFI 불가피한 경우 별도 표시)
- 외부 입력 역직렬화: serde의 #[serde(deny_unknown_fields)]
- SQL: sqlx 매크로로 컴파일 타임 검증
- 정수 변환: From/Into 또는 TryFrom (오버플로 방지)
- 패닉 방지: assert! 대신 Result 반환 권장
```

### 예시 지시

> "보안 설정을 다음과 같이 해줘:
> 
> - safe Rust만 사용 (unsafe 없음)
> - 클라이언트 패킷 역직렬화 구조체에 #[serde(deny_unknown_fields)]
> - 모든 SQL은 sqlx query! 또는 query_as! 매크로
> - i32/u32 변환은 as 캐스팅 대신 TryFrom::try_from 사용
> - 배열 인덱싱은 get() 메서드로 안전하게"

---

## 13.10 체크리스트 9: 코드 구조

### 모듈 구조 명시

```
- domain/ : 게임 로직, 도메인 모델
- infra/  : DB, 네트워크, 외부 서비스
- app/    : 핸들러, 라우터, 애플리케이션 서비스
- config/ : 설정 로딩
```

### 가시성 정책

```
- 공개 API: pub
- 크레이트 내부 공유: pub(crate)
- 모듈 내 전용: 기본값 (private)
- 구조체 필드: 원칙적으로 private, 접근자 메서드 제공
```

### 예시 지시

> "코드 구조를 다음과 같이 해줘:
> 
> - domain/player.rs: Player 구조체, PlayerError, 비즈니스 로직
> - domain/room.rs: Room 구조체, RoomError, 룸 관리 로직
> - infra/db.rs: DB 접근 레이어
> - app/handlers.rs: 패킷 핸들러
> - 구조체 필드는 private, pub 메서드로 접근
> - pub(crate) 적극 활용하여 외부 공개 API 최소화"

---

## 13.11 통합 지시문 예시

위의 체크리스트를 모두 통합한 실제 게임 서버 컴포넌트 지시문:

### 세션 핸들러 지시

```
Rust + Tokio 기반 게임 서버의 세션 핸들러를 작성해줘.

[컨텍스트]
- Tokio 1.x 멀티스레드 런타임, edition 2021
- 워크스페이스 내 game-server 크레이트
- TCP + bincode 직렬화 (2바이트 길이 + 데이터)
- 최대 동시 접속 5,000명

[기능 요구사항]
- TCP 연결 수락 후 독립 태스크 생성
- 패킷 수신 / 송신 루프 분리 (OwnedReadHalf, OwnedWriteHalf)
- 세션 인증 (로그인 패킷 처리)
- 하트비트 30초마다 전송
- 10초 타임아웃 (마지막 패킷 수신 기준)
- Graceful 종료 (서버 shutdown 시 DISCONNECT 패킷 전송)

[설계 원칙]
- 소유권: Session 객체는 핸들러 태스크가 독점 소유
- 공유 상태: Arc<SessionManager>로 세션 목록 공유
- 채널: 
  * 게임 → 세션: mpsc::Sender<OutgoingPacket> (버퍼 500)
  * 종료 신호: watch::Receiver<bool>

[에러 처리]
- thiserror로 SessionError enum 정의
  (IoError, ParseError, AuthFailed, Timeout, Disconnected)
- anyhow는 사용하지 말 것 (라이브러리이므로)
- unwrap/expect 사용 금지

[비동기 정책]
- 수신 루프와 송신 루프를 select!로 통합
- 동기 Mutex를 await 가로질러 보유 금지
- CPU 바운드 작업 없으므로 spawn_blocking 불필요

[관측성]
- #[instrument(skip(state), fields(session_id))]
- counter!("sessions_created_total"), counter!("sessions_dropped_total")
- 에러는 tracing::error!(error = %e, session_id)

[테스트]
- SessionHandler 단위 테스트
- 연결 → 인증 → 패킷 처리 → 종료 흐름 통합 테스트
- #[tokio::test] 사용

[제약]
- safe Rust만 사용
- cargo clippy -- -D warnings 통과
```

---

## 13.12 반복 지시와 개선 루프

바이브 코딩은 한 번에 완성되는 것이 아니다. AI와의 반복적인 개선 루프가 중요하다.

### 반복 루프

```
1. 초기 지시 → AI 코드 생성
2. cargo check → 컴파일 오류 확인
3. 오류 이해 → 원인 파악 (단순히 에러 메시지 붙여넣기 금지)
4. 개선 지시 → 원인을 설명하고 올바른 방향 지시
5. cargo clippy → 경고 확인
6. cargo test → 테스트 실행
7. 코드 리뷰 → 의도와 일치하는지 확인
8. 필요시 2로 돌아가기
```

### 컴파일 오류를 AI에게 전달하는 방법

**나쁜 방법**:
```
오류 메시지만 붙여넣기
"error[E0502]: cannot borrow `sessions` as mutable because it is also borrowed as immutable"
```

**좋은 방법**:
```
"다음 컴파일 오류가 발생했어:
error[E0502]: cannot borrow `sessions` as mutable...

이 오류가 발생하는 이유는 [코드 설명]에서 
sessions를 불변 참조로 빌린 상태에서 가변 접근을 시도해서야.

내가 원하는 것은 [의도 설명]이야.

해결 방법으로:
1. 불변 참조 범위를 먼저 끝내고 이후에 가변 접근
2. retain() 메서드로 한 번에 처리
둘 중 어느 방법이 더 적합할지 판단해서 수정해줘."
```

---

## 13.13 지시문 템플릿 라이브러리

자주 쓰이는 지시 패턴들을 모아 두고 재사용하면 효율적이다.

### 기본 서버 컴포넌트 템플릿

```
[타입명]을 Rust + Tokio로 구현해줘.

스택: Tokio 1.x, edition 2021, safe Rust only
에러: thiserror ([타입명]Error), unwrap 금지, ? 연산자 사용
비동기: tokio::sync::Mutex (await 가로지를 때), std::sync::Mutex (동기 전용)
로깅: tracing crate, #[instrument] on pub fns
테스트: #[tokio::test], 정상 + 에러 케이스
품질: clippy -D warnings 통과

소유권: [명시]
공유 정책: [명시]
```

### DB 액세스 레이어 템플릿

```
PostgreSQL 액세스 레이어를 sqlx 0.7로 구현해줘.

- 연결: PgPool (Arc로 감싸지 말 것, PgPool이 이미 Arc 내부 사용)
- 쿼리: query! / query_as! 매크로 (컴파일 타임 검증)
- 트랜잭션: begin/commit/rollback
- 에러: SqlxError 래핑 (thiserror)
- 비동기: async fn, ? 전파
- 마이그레이션: sqlx::migrate!() 매크로

DATABASE_URL 환경변수에서 연결 URL 읽어올 것.
```

---

## 13.14 정리

이 챕터의 핵심:

지시 품질 = 코드 품질 — 구체적인 지시는 구체적인 코드를 낳는다. 모호한 지시는 모호한(또는 틀린) 코드를 낳는다.

9가지 체크리스트 — 프로젝트 컨텍스트, 도메인 요구사항, 소유권 정책, 에러 처리, 비동기 정책, 관측성, 테스트, 보안, 코드 구조.

컴파일 오류 전달 방법 — 에러 메시지만 붙여넣기 하지 말고, 왜 발생했는지 이해한 후 의도를 담아 지시한다.

반복 개선 루프 — check → clippy → test → 리뷰 → 개선 지시. 한 번에 완성되지 않아도 된다.

지시 템플릿 구축 — 자주 쓰는 지시 패턴을 문서화해 두면 다음 프로젝트에서 바로 재사용할 수 있다.

---

*다음 챕터: Chapter 14 — 컴파일러를 친구로 삼는 습관*
