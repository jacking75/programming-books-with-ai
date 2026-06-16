# Chapter 11. Cargo와 빌드 워크플로 — 체득해 두면 바이브 코딩이 빨라진다

> "Cargo를 잘 알면 AI가 만든 코드를 빠르게 검증하고, 
> 문제를 정확히 진단하고, 프로덕션 품질의 빌드를 자동화할 수 있다."

---

## 11.1 Cargo란 무엇인가

Cargo는 Rust의 공식 패키지 매니저이자 빌드 시스템이다. C++의 CMake + vcpkg 또는 C#의 MSBuild + NuGet이 하는 일을 하나의 도구로 통합한 것이라고 이해하면 된다.

하지만 Cargo는 그것들보다 훨씬 일관되고 사용하기 편리하다. 의존성 관리, 빌드, 테스트, 벤치마크, 문서 생성이 모두 하나의 명령어 체계에서 이루어진다.

```
cargo 주요 기능:
├── 빌드 관리: cargo build, cargo build --release
├── 패키지 관리: Cargo.toml, crates.io 의존성
├── 워크스페이스: 멀티 크레이트 프로젝트
├── 테스트: cargo test
├── 린팅: cargo clippy
├── 포매팅: cargo fmt
├── 문서: cargo doc
└── 벤치마킹: cargo bench
```

---

## 11.2 핵심 명령어

게임 서버 개발에서 매일 사용하는 명령어들이다.

### cargo check — 빠른 타입 검사

```bash
cargo check
```

**가장 많이 쓰는 명령**. 바이너리를 생성하지 않고 타입 검사와 빌림 검사만 수행한다. `cargo build`보다 훨씬 빠르다.

AI가 코드를 생성했을 때 컴파일 가능한지 빠르게 확인하는 데 사용한다:

```bash
# 일반 체크
cargo check

# 모든 타겟 체크 (테스트, 벤치마크 포함)
cargo check --all-targets

# 특정 패키지만 (워크스페이스에서)
cargo check -p game-server
```

### cargo build — 개발 빌드

```bash
cargo build
```

기본값은 디버그 빌드다. 최적화가 없고, 디버그 심볼이 포함되어 있어 바이너리가 크다.

```bash
# 릴리즈 빌드
cargo build --release

# 특정 바이너리만
cargo build --bin game-server

# 특정 패키지 (워크스페이스)
cargo build -p game-server --release
```

### cargo run — 빌드 후 실행

```bash
# 기본 실행
cargo run

# 릴리즈로 실행 (성능 테스트)
cargo run --release

# 인자 전달
cargo run -- --config production.toml --port 9000
```

### cargo test — 테스트 실행

```bash
# 모든 테스트
cargo test

# 특정 테스트만 (이름 패턴)
cargo test session_handler

# 병렬 테스트 수 제어 (DB 테스트에서 중요)
cargo test -- --test-threads=1

# 테스트 출력 보기
cargo test -- --nocapture

# 통합 테스트만
cargo test --test integration_tests
```

### cargo clippy — 린터

**반드시 사용해야 하는 명령**. Rust 관용 패턴과 일반적인 버그를 잡아준다:

```bash
# 기본 클리피
cargo clippy

# 경고를 에러로 취급 (CI에서 권장)
cargo clippy -- -D warnings

# 특정 린트 비활성화
cargo clippy -- -A clippy::too_many_arguments
```

AI가 생성한 코드는 컴파일되더라도 clippy 경고가 있을 수 있다. 경고를 모두 해결하면 코드 품질이 크게 향상된다.

### cargo fmt — 코드 포매터

```bash
# 포매팅 적용
cargo fmt

# 포매팅 검사만 (CI에서 사용)
cargo fmt --check
```

Rust 커뮤니티는 단일 공식 포매터를 사용한다. AI가 생성한 코드도 `cargo fmt`를 실행하면 일관된 스타일이 된다.

---

## 11.3 Cargo.toml 구조

```toml
[package]
name = "game-server"
version = "0.1.0"
edition = "2021"          # 2015, 2018, 2021 (최신 기능 사용)
authors = ["Dev Team <dev@company.com>"]
description = "Awesome game server"

# 의존성 섹션들
[dependencies]
# 크레이트 이름 = "버전"
tokio = { version = "1", features = ["full"] }
serde = { version = "1.0", features = ["derive"] }

# 선택적 의존성
redis = { version = "0.24", optional = true }

[dev-dependencies]    # 테스트에만 사용
tokio-test = "0.4"
proptest = "1"

[build-dependencies]  # build.rs에서 사용
tonic-build = "0.11"

# 바이너리 정의 (src/bin/ 이외 위치)
[[bin]]
name = "game-server"
path = "src/main.rs"

[[bin]]
name = "admin-tool"
path = "src/bin/admin.rs"

# 라이브러리 정의
[lib]
name = "game_core"
path = "src/lib.rs"
```

### 버전 지정 방식

```toml
[dependencies]
# 정확한 버전 (권장하지 않음)
crate-a = "=1.2.3"

# 패치 버전만 업데이트 허용 (1.2.x)
crate-b = "~1.2"

# 마이너 버전까지 업데이트 허용 (1.x.x) - 기본값
crate-c = "1"
crate-d = "^1.0"

# 범위 지정
crate-e = ">= 1.5, < 2.0"

# 특정 커밋 (최후 수단)
crate-f = { git = "https://github.com/...", rev = "abc123" }

# 로컬 경로
crate-g = { path = "../local-crate" }
```

### Features 시스템

```toml
[features]
# 기본 활성화 기능
default = ["metrics", "logging"]

# 추가 기능들
metrics = ["dep:metrics", "dep:metrics-exporter-prometheus"]
logging = ["dep:tracing", "dep:tracing-subscriber"]
redis-cache = ["dep:redis"]
grpc = ["dep:tonic", "dep:prost"]
full = ["metrics", "logging", "redis-cache", "grpc"]

[dependencies]
# optional = true면 feature로 활성화됨
metrics = { version = "0.22", optional = true }
metrics-exporter-prometheus = { version = "0.13", optional = true }
tracing = { version = "0.1", optional = true }
tracing-subscriber = { version = "0.3", optional = true }
redis = { version = "0.24", optional = true }
tonic = { version = "0.11", optional = true }
prost = { version = "0.12", optional = true }
```

```bash
# 기본 features
cargo build

# 특정 feature 추가
cargo build --features redis-cache

# 기본 features 비활성화 후 특정만 활성화
cargo build --no-default-features --features grpc

# 모든 features
cargo build --features full
```

의존성에서 기본 features 비활성화:

```toml
tokio = { version = "1", default-features = false, features = ["rt-multi-thread", "net", "sync"] }
```

---

## 11.4 워크스페이스 (Workspace)

멀티 크레이트 프로젝트 구성. C#의 솔루션 파일에 해당한다.

### 프로젝트 구조

```
game-project/
├── Cargo.toml          # 워크스페이스 루트
├── Cargo.lock          # 의존성 잠금 파일 (단일)
├── game-core/          # 공유 라이브러리
│   ├── Cargo.toml
│   └── src/lib.rs
├── game-server/        # 서버 바이너리
│   ├── Cargo.toml
│   └── src/main.rs
├── game-admin/         # 관리 도구
│   ├── Cargo.toml
│   └── src/main.rs
└── game-protocol/      # 프로토콜 정의
    ├── Cargo.toml
    └── src/lib.rs
```

### 워크스페이스 Cargo.toml

```toml
# game-project/Cargo.toml (루트)
[workspace]
members = [
    "game-core",
    "game-server",
    "game-admin",
    "game-protocol",
]
resolver = "2"  # 의존성 해석기 버전 2 (권장)

# 공통 의존성 버전 고정 (Cargo 1.64+)
[workspace.dependencies]
tokio = { version = "1", features = ["full"] }
serde = { version = "1", features = ["derive"] }
anyhow = "1"
thiserror = "1"
tracing = "0.1"

[workspace.package]
version = "0.1.0"
edition = "2021"
authors = ["Game Team"]
```

### 멤버 크레이트 Cargo.toml

```toml
# game-server/Cargo.toml
[package]
name = "game-server"
version.workspace = true    # 워크스페이스에서 상속
edition.workspace = true

[dependencies]
game-core = { path = "../game-core" }
game-protocol = { path = "../game-protocol" }

# 워크스페이스 의존성 재사용 (버전 고정 효과)
tokio = { workspace = true }
serde = { workspace = true }
anyhow = { workspace = true }

# 이 크레이트만의 의존성
axum = "0.7"
```

### 워크스페이스 명령어

```bash
# 전체 빌드
cargo build

# 특정 크레이트만
cargo build -p game-server

# 전체 테스트
cargo test

# 특정 크레이트 테스트
cargo test -p game-core
```

---

## 11.5 빌드 프로파일

```toml
# Cargo.toml

# 개발 빌드 (cargo build)
[profile.dev]
opt-level = 0          # 최적화 없음
debug = true           # 디버그 심볼 포함
overflow-checks = true # 정수 오버플로 패닉

# 릴리즈 빌드 (cargo build --release)
[profile.release]
opt-level = 3          # 최대 최적화
lto = "thin"           # Link Time Optimization (빠른 버전)
codegen-units = 1      # 단일 코드 생성 단위 (더 좋은 최적화)
panic = "abort"        # 패닉 시 스택 언와인딩 없이 즉시 중단 (바이너리 크기 감소)
strip = true           # 디버그 심볼 제거 (바이너리 크기 감소)

# 최대 성능 프로파일
[profile.production]
inherits = "release"
lto = "fat"            # 전체 LTO (느리지만 더 좋은 최적화)
codegen-units = 1

# 테스트용 최적화 빌드 (성능 테스트 시)
[profile.bench]
inherits = "release"
debug = true           # 프로파일링을 위한 심볼 유지
```

### 게임 서버 릴리즈 빌드 시 주의사항

```bash
# 릴리즈 빌드
cargo build --release

# 빌드 후 바이너리 위치
# target/release/game-server

# 실제 크기 확인
ls -lh target/release/game-server

# strip (심볼 제거, 이미 프로파일에 strip=true면 불필요)
strip target/release/game-server
```

`panic = "abort"` 설정은 패닉이 발생했을 때 스택을 언와인딩하지 않고 즉시 프로세스를 종료한다. 게임 서버에서 패닉이 발생하면 해당 프로세스를 재시작하는 경우가 많으므로 이 설정이 적합하다. 단, `Drop` 트레이트가 호출되지 않으므로 자원이 정리되지 않는다.

---

## 11.6 Cargo.lock

`Cargo.lock`은 의존성의 정확한 버전을 기록하는 파일이다.

**핵심 규칙**:
- **바이너리 프로젝트** (게임 서버): `Cargo.lock`을 **git에 커밋**한다. 재현 가능한 빌드를 보장한다.
- **라이브러리 크레이트**: `Cargo.lock`을 `.gitignore`에 추가한다. 사용자가 의존성 버전을 선택할 수 있어야 한다.

```bash
# 의존성 업데이트
cargo update          # 모든 의존성 업데이트 (버전 범위 내)
cargo update -p tokio # 특정 크레이트만 업데이트

# 현재 의존성 확인
cargo tree                    # 전체 의존성 트리
cargo tree -p game-server     # 특정 패키지의 의존성
cargo tree --duplicates       # 중복된 의존성 버전 확인
```

---

## 11.7 빌드 스크립트 (build.rs)

빌드 전에 실행되는 스크립트. Protobuf 컴파일, C 코드 빌드 등에 사용된다.

```rust
// build.rs (프로젝트 루트)
fn main() -> Result<(), Box<dyn std::error::Error>> {
    // Protobuf 컴파일
    tonic_build::configure()
        .build_server(true)
        .build_client(true)
        .compile(&["proto/game.proto"], &["proto"])?;
    
    // 빌드 시 환경변수 설정
    println!("cargo:rustc-env=BUILD_TIME={}", chrono::Utc::now());
    
    // 파일 변경 감지 (이 파일이 바뀌면 재빌드)
    println!("cargo:rerun-if-changed=proto/game.proto");
    println!("cargo:rerun-if-changed=build.rs");
    
    Ok(())
}
```

---

## 11.8 CI/CD 파이프라인

게임 서버 프로젝트의 표준 CI 설정.

### GitHub Actions 예시

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  CARGO_TERM_COLOR: always
  DATABASE_URL: postgres://postgres:password@localhost/test_db

jobs:
  check:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: password
          POSTGRES_DB: test_db
        ports:
          - 5432:5432
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Install Rust
      uses: dtolnay/rust-toolchain@stable
      with:
        components: rustfmt, clippy
    
    - name: Cache
      uses: Swatinem/rust-cache@v2
    
    # 포매팅 검사
    - name: Check formatting
      run: cargo fmt --check
    
    # 클리피 (경고를 에러로)
    - name: Clippy
      run: cargo clippy -- -D warnings
    
    # 빌드
    - name: Build
      run: cargo build --all-features
    
    # 테스트
    - name: Test
      run: cargo test --all-features
    
    # 보안 감사 (선택적)
    - name: Security audit
      run: |
        cargo install cargo-audit
        cargo audit

  release-build:
    runs-on: ubuntu-latest
    needs: check
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    - uses: dtolnay/rust-toolchain@stable
    - uses: Swatinem/rust-cache@v2
    
    - name: Build release
      run: cargo build --release
    
    - name: Upload artifact
      uses: actions/upload-artifact@v4
      with:
        name: game-server
        path: target/release/game-server
```

---

## 11.9 유용한 Cargo 플러그인

```bash
# 설치
cargo install cargo-watch
cargo install cargo-edit
cargo install cargo-audit
cargo install cargo-expand
cargo install cargo-flamegraph
cargo install cargo-outdated
```

### cargo-watch — 파일 변경 감지 자동 빌드

```bash
# 파일 변경 시 자동으로 check 실행
cargo watch -x check

# check + test 연속 실행
cargo watch -x check -x test

# clippy도 포함
cargo watch -x "clippy -- -D warnings" -x test
```

바이브 코딩 시 AI가 코드를 생성하는 동안 `cargo watch`를 켜두면 실시간으로 컴파일 상태를 확인할 수 있다.

### cargo-edit — 의존성 편집

```bash
# 의존성 추가
cargo add serde --features derive
cargo add tokio --features full

# 의존성 제거
cargo rm redis

# 의존성 업그레이드
cargo upgrade
```

### cargo-expand — 매크로 전개 확인

```bash
# derive 매크로가 실제로 어떤 코드를 생성하는지 확인
cargo expand
cargo expand --bin game-server
```

AI가 파악하기 어려운 매크로 관련 오류를 디버깅할 때 유용하다.

---

## 11.10 환경 변수와 설정

```rust
// 빌드 시 환경 변수 (build.rs에서 설정)
const BUILD_TIME: &str = env!("BUILD_TIME");
const GIT_HASH: &str = env!("GIT_HASH");

// 런타임 환경 변수
fn main() {
    let log_level = std::env::var("LOG_LEVEL")
        .unwrap_or_else(|_| "info".to_string());
    
    let port: u16 = std::env::var("PORT")
        .unwrap_or_else(|_| "9000".to_string())
        .parse()
        .expect("PORT must be a number");
}
```

### .env 파일 활용 (개발 환경)

```toml
# Cargo.toml
[dependencies]
dotenvy = "0.15"
```

```bash
# .env 파일
DATABASE_URL=postgres://localhost/gamedb_dev
REDIS_URL=redis://localhost
PORT=9001
LOG_LEVEL=debug
```

```rust
fn main() {
    dotenvy::dotenv().ok(); // .env 파일 로드 (없으면 무시)
    
    let database_url = std::env::var("DATABASE_URL")
        .expect("DATABASE_URL must be set");
}
```

---

## 11.11 테스트 작성과 실행

### 단위 테스트

```rust
// src/session.rs
impl Session {
    pub fn is_authenticated(&self) -> bool {
        self.player_id.is_some()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn new_session_is_not_authenticated() {
        let session = Session::new(42);
        assert!(!session.is_authenticated());
    }
    
    #[test]
    fn authenticated_session() {
        let mut session = Session::new(42);
        session.set_player_id(100);
        assert!(session.is_authenticated());
    }
}
```

### 비동기 테스트

```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[tokio::test]
    async fn test_session_timeout() {
        let session = Session::new(42);
        tokio::time::sleep(Duration::from_millis(100)).await;
        // 테스트 로직
    }
    
    // 멀티스레드 런타임으로 테스트
    #[tokio::test(flavor = "multi_thread")]
    async fn test_concurrent_sessions() {
        // 동시성 테스트
    }
}
```

### 통합 테스트

```
project/
├── src/
│   └── lib.rs
└── tests/
    ├── session_tests.rs    # 통합 테스트
    └── db_tests.rs
```

```rust
// tests/session_tests.rs
use game_core::*;

#[tokio::test]
async fn test_full_session_lifecycle() {
    let server = TestServer::start().await;
    
    let client = TestClient::connect(server.addr()).await;
    client.login("alice", "password").await.unwrap();
    
    let room = client.join_room(1).await.unwrap();
    assert_eq!(room.player_count, 1);
    
    client.disconnect().await;
    server.stop().await;
}
```

---

## 11.12 AI에게 Cargo 관련 지시를 내리는 법

### 워크스페이스 구조 요청

> "다음 구조의 Cargo 워크스페이스를 만들어줘:
> - game-core: 게임 로직 공유 라이브러리 (lib)
> - game-server: TCP 게임 서버 (bin)
> - game-api: REST API 서버 (bin)
> - game-protocol: 프로토콜 정의 (lib)
> 
> 공통 의존성(tokio, serde, anyhow)은 워크스페이스 레벨에서 버전을 통일해줘."

### CI 설정 요청

> "GitHub Actions CI 파일을 만들어줘.
> - cargo fmt --check
> - cargo clippy -- -D warnings
> - cargo test --all-features
> 세 단계를 순서대로 실행하고, 
> 실패하면 PR 머지를 막도록 해줘.
> Tokio 인스트루먼트가 필요하므로 RUST_LOG 환경변수도 설정해줘."

### 릴리즈 프로파일 설정

> "게임 서버의 릴리즈 프로파일을 설정해줘.
> - opt-level = 3
> - lto = thin
> - panic = abort
> - strip = true
> 개발 빌드는 빠른 컴파일을 위해 opt-level = 0으로 유지해줘."

---

## 11.13 정리

이 챕터의 핵심:

핵심 명령어 — `cargo check`(빠른 검증), `cargo clippy -- -D warnings`(품질), `cargo fmt`(포매팅), `cargo test`(테스트). 이 네 가지가 바이브 코딩의 검증 사이클을 만든다.

Cargo.toml 구조 — 의존성 버전 명시, features 시스템 활용, default-features = false로 불필요한 기능 제외.

워크스페이스 — 멀티 크레이트 프로젝트 구성. 공통 의존성 버전을 워크스페이스 레벨에서 통일한다.

릴리즈 프로파일 — 게임 서버에는 `opt-level=3, lto=thin, panic=abort`가 일반적인 설정이다.

CI — `cargo fmt --check`, `cargo clippy -- -D warnings`, `cargo test`를 CI에 강제하면 코드 품질이 자동으로 유지된다.

---

*다음 챕터: Chapter 12 — C++/C# 경력자가 자주 부딪히는 함정*
