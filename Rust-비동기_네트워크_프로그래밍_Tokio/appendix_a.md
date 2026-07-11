# 부록 A. 일반적인 문제 해결

## 1. 자주 발생하는 문제

### 1.1 비동기 런타임 관련 문제

```rust
// 문제: 런타임 블로킹
// ❌ 잘못된 예시
#[tokio::main]
async fn main() {
    // 블로킹 작업을 직접 실행
    std::thread::sleep(Duration::from_secs(5));
}

// ✅ 올바른 예시
#[tokio::main]
async fn main() {
    // tokio::spawn을 사용하여 별도 스레드에서 실행
    tokio::spawn(async {
        tokio::time::sleep(Duration::from_secs(5)).await;
    });
}
```

### 1.2 타임아웃 처리

```rust
use tokio::time::{timeout, Duration};

async fn fetch_with_timeout<T>(
    future: impl Future<Output = Result<T, Error>>,
    duration: Duration,
) -> Result<T, Error> {
    match timeout(duration, future).await {
        Ok(result) => result,
        Err(_) => Err(Error::Timeout),
    }
}

// 사용 예시
async fn example() -> Result<(), Error> {
    let result = fetch_with_timeout(
        async_operation(),
        Duration::from_secs(5)
    ).await?;
    Ok(())
}
```

### 1.3 메모리 누수 방지

```rust
use tokio::sync::mpsc;
use std::sync::Arc;

struct ResourceManager {
    resources: Arc<Mutex<Vec<Resource>>>,
}

impl ResourceManager {
    async fn cleanup(&self) {
        let mut resources = self.resources.lock().await;
        for resource in resources.drain(..) {
            resource.release().await;
        }
    }

    // 리소스 사용이 끝나면 자동으로 정리
    async fn with_resource<F, R>(&self, f: F) -> R
    where
        F: FnOnce(&Resource) -> Future<Output = R>,
    {
        let resource = self.acquire_resource().await;
        let result = f(&resource).await;
        self.release_resource(resource).await;
        result
    }
}
```

## 2. 디버깅 전략

### 2.1 로그 기반 디버깅

```rust
use tracing::{info, error, instrument};

#[instrument]
async fn process_request(request: Request) -> Result<Response, Error> {
    info!(
        request_id = %request.id,
        method = %request.method,
        "Processing request"
    );

    match handle_request(request).await {
        Ok(response) => {
            info!(
                request_id = %request.id,
                status = %response.status,
                "Request completed successfully"
            );
            Ok(response)
        }
        Err(e) => {
            error!(
                request_id = %request.id,
                error = %e,
                "Request processing failed"
            );
            Err(e)
        }
    }
}
```

### 2.2 비동기 스택 트레이스

```rust
use futures::future::ready;
use tokio::task;
use std::panic;

async fn debug_task() {
    // 패닉 발생 시 스택 트레이스 출력
    panic::set_hook(Box::new(|panic_info| {
        println!("Task panicked: {:?}", panic_info);
        println!("Stack trace:\n{:?}", backtrace::Backtrace::new());
    }));

    task::spawn(async {
        // 디버그를 위한 컨텍스트 추가
        ready(()).await;
        panic!("Task failed");
    }).await.unwrap();
}
```

## 3. 성능 최적화 팁

### 3.1 Task 최적화

```rust
use tokio::sync::mpsc;
use futures::stream::{self, StreamExt};

async fn optimize_tasks() {
    // 배치 처리를 통한 최적화
    let (tx, mut rx) = mpsc::channel(1000);

    // 여러 작업을 동시에 처리
    stream::iter(0..1000)
        .map(|i| async move {
            let result = process_item(i).await;
            tx.send(result).await.unwrap();
        })
        .buffer_unwind(10) // 동시 처리 제한
        .for_each_concurrent(None, |f| async move {
            f.await;
        })
        .await;
}

// 리소스 풀링
struct ConnectionPool {
    connections: Vec<Connection>,
    available: tokio::sync::Semaphore,
}

impl ConnectionPool {
    async fn acquire(&self) -> impl Drop + '_  {
        let _permit = self.available.acquire().await.unwrap();
        // 커넥션 반환
        PooledConnection { /* ... */ }
    }
}
```

### 3.2 메모리 최적화

```rust
use bytes::BytesMut;

struct OptimizedBuffer {
    buffer: BytesMut,
}

impl OptimizedBuffer {
    fn new() -> Self {
        Self {
            buffer: BytesMut::with_capacity(4096),
        }
    }

    async fn process_data(&mut self, data: &[u8]) {
        // 버퍼 재사용
        self.buffer.clear();
        self.buffer.extend_from_slice(data);

        // 데이터 처리
        process_buffer(&mut self.buffer).await;
    }
}
```

## 4. 모범 사례

### 4.1 에러 처리

```rust
use thiserror::Error;

#[derive(Error, Debug)]
enum AppError {
    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("Database error: {0}")]
    Database(#[from] sqlx::Error),

    #[error("Validation error: {0}")]
    Validation(String),
}

async fn handle_error() -> Result<(), AppError> {
    // 구조화된 에러 처리
    let result = do_something().await
        .map_err(|e| AppError::Validation(e.to_string()))?;

    Ok(())
}
```

### 4.2 리소스 관리

```rust
use std::sync::Arc;
use tokio::sync::Mutex;

struct ResourceGuard<T> {
    resource: Arc<Mutex<T>>,
}

impl<T> ResourceGuard<T> {
    async fn with_resource<F, R>(&self, f: F) -> R
    where
        F: FnOnce(&mut T) -> R,
    {
        let mut guard = self.resource.lock().await;
        f(&mut guard)
    }
}

// RAII 패턴 적용
impl<T> Drop for ResourceGuard<T> {
    fn drop(&mut self) {
        // 리소스 정리
    }
}
```

## 5. 안티패턴

### 5.1 피해야 할 패턴들

```rust
// ❌ 안티패턴: 과도한 동기화
async fn bad_practice() {
    let mutex = Arc::new(Mutex::new(0));

    for _ in 0..100 {
        let mutex_clone = mutex.clone();
        tokio::spawn(async move {
            let mut guard = mutex_clone.lock().await;
            *guard += 1;
        });
    }
}

// ✅ 개선된 버전: 채널 사용
async fn good_practice() {
    let (tx, mut rx) = mpsc::channel(100);

    for _ in 0..100 {
        let tx = tx.clone();
        tokio::spawn(async move {
            tx.send(1).await.unwrap();
        });
    }

    let mut sum = 0;
    while let Some(value) = rx.recv().await {
        sum += value;
    }
}
```

### 5.2 성능 저하 요인

```rust
// ❌ 안티패턴: 불필요한 클론
async fn bad_performance() {
    let data = Arc::new(vec![1, 2, 3]);

    for _ in 0..1000 {
        let data_clone = data.clone(); // 불필요한 클론
        tokio::spawn(async move {
            println!("{:?}", data_clone);
        });
    }
}

// ✅ 개선된 버전: 참조 사용
async fn good_performance() {
    let data = Arc::new(vec![1, 2, 3]);

    stream::iter(0..1000)
        .for_each_concurrent(None, |_| {
            let data = Arc::clone(&data);
            async move {
                println!("{:?}", data);
            }
        })
        .await;
}
```

주요 고려사항은 다음과 같다.

1. 자주 발생하는 문제
   - 런타임 블로킹 방지
   - 적절한 타임아웃 설정
   - 메모리 관리
   - 에러 처리

2. 디버깅 전략
   - 구조화된 로깅
   - 스택 트레이스 분석
   - 메트릭 모니터링
   - 디버그 도구 활용

3. 성능 최적화
   - 비동기 작업 최적화
   - 메모리 사용 최적화
   - 리소스 풀링
   - 배치 처리

4. 모범 사례
   - 구조화된 에러 처리
   - RAII 패턴
   - 리소스 관리
   - 테스트 가능한 설계

5. 안티패턴
   - 과도한 동기화 피하기
   - 불필요한 클론 최소화
   - 블로킹 작업 관리
   - 메모리 누수 방지

실제 적용 시 주의사항은 다음과 같다.

- 상황에 맞는 해결책 선택
- 성능과 가독성의 균형
- 테스트 용이성 고려
- 유지보수성 확보

이러한 가이드라인을 따르면서 실제 상황에 맞게 적절히 조정하여 적용하는 것이 중요하다.
