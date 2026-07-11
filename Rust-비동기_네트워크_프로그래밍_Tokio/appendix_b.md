# 부록 B. Tokio 생태계

## 1. 유용한 크레이트 소개

### 1.1 핵심 크레이트

```toml
# Cargo.toml
[dependencies]
# 비동기 런타임
tokio = { version = "1.28", features = ["full"] }
# 비동기 스트림 처리
futures = "0.3"
# 로깅 및 트레이싱
tracing = "0.1"
tracing-subscriber = "0.3"
# 에러 처리
thiserror = "1.0"
anyhow = "1.0"
```

### 1.2 네트워킹 크레이트

```toml
# 웹 프레임워크
axum = "0.6"
# HTTP 클라이언트
reqwest = "0.11"
# gRPC
tonic = "0.9"
# WebSocket
tokio-tungstenite = "0.19"
```

### 1.3 데이터베이스 크레이트

```toml
# SQL 데이터베이스
sqlx = { version = "0.6", features = ["runtime-tokio-native-tls", "postgres"] }
# Redis
redis = { version = "0.22", features = ["tokio-comp"] }
# MongoDB
mongodb = "2.5"
```

## 2. 툴체인

### 2.1 개발 도구

```bash
# Rust 툴체인 설치
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# 유용한 도구들 설치
cargo install cargo-watch    # 자동 재컴파일
cargo install cargo-expand   # 매크로 확장 확인
cargo install cargo-edit    # 의존성 관리
cargo install cargo-flamegraph  # 성능 프로파일링
```

### 2.2 디버깅 및 프로파일링

```rust
use tracing_subscriber::{fmt, prelude::*};

async fn setup_debugging() {
    // 트레이싱 설정
    tracing_subscriber::registry()
        .with(fmt::layer())
        .with(tracing_subscriber::EnvFilter::from_default_env())
        .init();

    // 프로파일링 예시
    #[cfg(debug_assertions)]
    {
        console_subscriber::init();
        tokio::spawn(async {
            console_subscriber::run().await;
        });
    }
}
```

## 3. 커뮤니티 리소스

### 3.1 공식 리소스

주요 커뮤니티 리소스 링크는 다음과 같다.

- Tokio 공식 문서: https://tokio.rs
- GitHub 저장소: https://github.com/tokio-rs/tokio
- 디스코드 채널: https://discord.com/invite/tokio
- Reddit: https://reddit.com/r/rust

### 3.2 예제 프로젝트

```rust
// 채팅 서버 예제
use tokio::net::{TcpListener, TcpStream};
use tokio::sync::broadcast;

#[tokio::main]
async fn main() {
    // 채팅 서버 구현
    let listener = TcpListener::bind("127.0.0.1:8080").await.unwrap();
    let (tx, _rx) = broadcast::channel(100);

    loop {
        let (socket, addr) = listener.accept().await.unwrap();
        let tx = tx.clone();

        tokio::spawn(async move {
            handle_connection(socket, tx, addr).await;
        });
    }
}
```

## 4. 추가 학습 자료

### 4.1 학습 로드맵

1. 기초 단계

```rust
// 1. 비동기 프로그래밍 기초
async fn basic_concepts() {
    // Future와 async/await 이해
    let future = async {
        println!("Hello, async world!");
    };

    // 태스크 생성과 실행
    tokio::spawn(future);
}

// 2. 동시성 기초
async fn concurrency_basics() {
    let handles: Vec<_> = (0..10)
        .map(|i| {
            tokio::spawn(async move {
                println!("Task {}", i);
            })
        })
        .collect();

    for handle in handles {
        handle.await.unwrap();
    }
}
```

2. 중급 단계

```rust
// 1. 채널과 동기화
use tokio::sync::{mpsc, Mutex};

async fn intermediate_concepts() {
    // 채널 사용
    let (tx, mut rx) = mpsc::channel(100);

    // 공유 상태 관리
    let shared_state = Arc::new(Mutex::new(Vec::new()));
}

// 2. 스트림 처리
use futures::stream::{self, StreamExt};

async fn stream_processing() {
    let numbers = stream::iter(0..100);

    numbers
        .chunks(10)
        .for_each(|chunk| async move {
            process_chunk(chunk).await;
        })
        .await;
}
```

3. 고급 단계

```rust
// 1. 커스텀 Future 구현
use std::future::Future;
use std::pin::Pin;
use std::task::{Context, Poll};

struct MyFuture {
    value: Option<i32>,
}

impl Future for MyFuture {
    type Output = i32;

    fn poll(mut self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
        if let Some(value) = self.value.take() {
            Poll::Ready(value)
        } else {
            cx.waker().wake_by_ref();
            Poll::Pending
        }
    }
}

// 2. 런타임 커스터마이징
use tokio::runtime::Builder;

async fn advanced_runtime() {
    let runtime = Builder::new_multi_thread()
        .worker_threads(4)
        .enable_all()
        .build()
        .unwrap();

    runtime.block_on(async {
        // 커스텀 런타임에서 작업 실행
    });
}
```

### 4.2 실전 프로젝트 아이디어

1. 기초 프로젝트

```rust
// HTTP API 서버
use axum::{
    routing::{get, post},
    Router,
};

#[tokio::main]
async fn main() {
    let app = Router::new()
        .route("/", get(|| async { "Hello, World!" }))
        .route("/users", post(create_user));

    axum::Server::bind(&"0.0.0.0:3000".parse().unwrap())
        .serve(app.into_make_service())
        .await
        .unwrap();
}
```

2. 중급 프로젝트

```rust
// 분산 작업 처리 시스템
use tokio::sync::mpsc;
use serde::{Serialize, Deserialize};

#[derive(Serialize, Deserialize)]
struct Job {
    id: String,
    payload: Vec<u8>,
}

async fn distributed_system() {
    let (job_sender, job_receiver) = mpsc::channel(100);
    let (result_sender, result_receiver) = mpsc::channel(100);

    // 작업자 스폰
    for _ in 0..4 {
        let job_receiver = job_receiver.clone();
        let result_sender = result_sender.clone();

        tokio::spawn(async move {
            process_jobs(job_receiver, result_sender).await;
        });
    }
}
```

## 주요 학습 포인트

1. 유용한 크레이트
   - 핵심 비동기 라이브러리
   - 네트워킹 도구
   - 데이터베이스 연동
   - 유틸리티 크레이트

2. 개발 툴체인
   - Rust 도구
   - 디버깅 도구
   - 프로파일링 도구
   - CI/CD 도구

3. 커뮤니티 리소스
   - 공식 문서
   - 예제 코드
   - 포럼 및 채팅
   - 블로그 및 튜토리얼

4. 학습 경로
   - 기초 개념
   - 중급 기술
   - 고급 주제
   - 실전 프로젝트

## 실용적인 조언

1. 단계적 학습
   - 기본기부터 시작
   - 실습 위주의 학습
   - 점진적 난이도 상승
   - 실제 프로젝트 적용

2. 커뮤니티 참여
   - 질문과 답변
   - 코드 리뷰
   - 오픈소스 기여
   - 지식 공유

3. 지속적 학습
   - 새로운 기능 탐구
   - 최신 트렌드 파악
   - 성능 최적화
   - 보안 고려사항

이러한 리소스와 도구들을 활용하면서, 실제 프로젝트에 적용하고 경험을 쌓는 것이 중요하다.

## 부록 자료: Tokio 런타임 심화

### Tokio runtime

https://tokio.rs/docs/getting-started/runtime/

런타임은 값을 반환할 때까지 Future의 poll을 반복해서 호출할 책임이 있다.
이것은 몇가지 다른 방법으로 할 수 있다.
예를 들면 basic_scheduler 설정에서는 현재의 스레드를 블럭하고, 스폰된 모든 태스크를 이 장소에서 처리한다.
threaded_scheduler 설정은 워크 스티링 스레드 풀을 사용하여 복수의 스레드에 부하를 분산한다.
threaded_scheduler는 애플리케이션 용의 default 이고, basic_scheduler은 테스트 용의 default 이다.

최종적으로는 모든 비동기 코드는 폴링 되어야 한다.
Future를 폴링하는 것은 Tokio 런타임의 일이지만 이것을 하기 위해서는 Future를 Tokio에 전달해야 한다.
tokio::spawn 함수를 사용하여 직접 Tokio에 Future를 전달할 수 있지만 Tokio가 이미 알고 있는 중에서 .await를 사용할 수도 있다.
아래 예에서는 비동기 함수 TcpStream::connect에 의해 생성된 Future에 대해서는 Tokio에 전달되지 못했다.

```rust
use tokio::net::TcpStream;

#[tokio::main]
async fn main() {
  // Create a tcp stream, but do not call await.
  TcpStream::connect("127.0.0.1:6142");
}
```

### 태스크 스폰

Tokio의 유니크한 것 중의 하나로 다른 비동기 태스트 안에서 런타임에 Future를 생성할 수 있는 것이다.
태스크는 애플리케이션의 "로직 단위"이다.
태스크는 Go의 고루틴이나 Erlang의 프로세스와 비슷하지만 비동기이다. 바꾸어 말하면 태스크는 **비동기 그린 스레드**이다.

태스크는 런타임에 전달되고, 런타임은 태스크 스케줄링을 한다.
런타임은 보통 많은 태스크를 하나의 스레드나 작은 스레드에 걸쳐서 스케줄링한다.
태스크는 계산 분하 높은 로직을 실행해서는 안된다. 그러므로 태스크로 피보나치 수열을 계산을 해서는 안된다.

태스크는 tokio::spawn을 사용하여 생성할 수 있다. 예를 들면 아래처럼한다.

```rust
#[tokio::main]
async fn main() {
    let handle = tokio::spawn(async {
        println!("doing some work, asynchronously");

        // Return a value for the example
        "result of the computation"
    });

    // Wait for the spawned task to finish
    let res = handle.await;

    println!("got {:?}", res);
}
```

여기에서는 태스크의 스폰은 다른 Future나 스트림 중에서 할 수 있고, 복수의 일을 동시에 할 수 있다.
위의 예에서는 외측의 스트림 안에서 Future를 스폰하고 있다. 스트림에서 값을 얻을 때마다 단순하게 내측의 Future를 실행한다.

Rust/tokio로 비동기 채팅 서버: https://qiita.com/mas-yo/items/71182a411ad755d1a8a7

## 참고 링크

- [Tokio 살펴보기](https://blog.naver.com/sssang97/222348642211)
- [(일어) Tokio 튜토리얼](https://zenn.dev/magurotuna/books/tokio-tutorial-ja)
- [(일어) Rust의 Tokio이 모든 features 배열](https://scrapbox.io/nwtgck/Rust%E3%81%AETokio%E3%81%AE%E3%81%99%E3%81%B9%E3%81%A6%E3%81%AE_features_%E9%85%8D%E5%88%97)
- [(일어) Rust: tokio에 의한 비동기 프로그래밍](https://hazm.at/mox/lang/rust/tokio/index.html)
- [(일어) Rust: Non-blocking I/O programming with mio::Poll](https://hazm.at/mox/lang/rust/recipes/async/poll/index.html)
- [(일어) Rust와 비동기 라이브러리 Tokio로 작성한 심플 Redis를 소개](https://thinkit.co.jp/article/18509)
- [(일어) Rust: tokio를 사용하여 독자적인 프로토콜 메시지를 받는 TCP 서버 만들기](https://castaneai.hatenablog.com/entry/rust-tcp-server-with-custom-protocol)
- [(일어) Rust의 비동기 런타임 tokio::main 깊게 파기](https://qiita.com/ryuma017/items/1f31f5441ed5df80f1cc)
- [(일어) Rust의 tokio로 자작 TCP relay server 만들기](https://zenn.dev/higumachan/articles/dbb01734d5a5c2)
- [(일어) Rust와 Tokio에 의한 차세대 네트워크 인프라](https://dev.classmethod.jp/articles/reinvent2020-opn205-next-gen-networking-infrastructure-with-rust-and-tokio/)
- [(일어) tokio의 기본적인 플로우 제어와 자원 관리 방법(Rust)](https://zenn.dev/scirexs/articles/c8ba3f4d9c5f0f)
- [(일어) StreamMap](https://docs.rs/tokio/0.2.22/tokio/stream/struct.StreamMap.html)
- [tokio-uring](https://github.com/tokio-rs/tokio-uring)
- [(일어) tokio-console: top 같은 Rust 명령 라인 툴](https://zenn.dev/tfutada/articles/4dbb9659bb8102)
- [(일어) Rust의 Tokio로 비동기와 그린 스레드를 이해하기](https://zenn.dev/tfutada/articles/5e87d6e7131e8e)
- [(일어) Tokio의 read 와 read_buf 의 차이](https://zenn.dev/hanaasagi/articles/4735bc6fa86042)
- [(일어) Rust의 tokio에서 워커스레드를 기동하는 방법](https://qiita.com/suin/items/546ef700b49450675b5a)
- [(일어) Mini-Redis Tutorial에서 시작하는 tokio](https://blog.ymgyt.io/entry/mini_redis_tutorial_to_get_started_with_tokio)
- [(일어) tokio-metrics로 runtime과 task의 metrics를 취득하기](https://zenn.dev/fraim/articles/tokio-metrics)
- [(일어) Rust로 동시 접속 가능한 채팅 서버 만들기 (1/n): 단일 클라이언트와의 접속](https://zenn.dev/yongikim/articles/rust-chat-server-1)
- [(일어) Rust로 동시 접속 가능한 채팅 서버 만들기 (2/n): 복수 접속의 구현](https://zenn.dev/yongikim/articles/rust-chat-server-2)
