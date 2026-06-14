# 매일 1시간만으로 만들면서 배우는 Rust 프로그래밍:   

# 부록

40일간의 학습을 마친 여러분을 위해, 이 부록에서는 일상적으로 자주 참조하게 될 내용들을 정리한다. 개발 중 빠르게 찾아볼 수 있도록 핵심 명령어, 설정 옵션, 그리고 흔히 마주치는 에러와 해결법을 모았다. 이 부록을 북마크하거나 인쇄하여 책상에 두고 수시로 참고한다.

## A. Cargo 명령어 참고

**기본 프로젝트 관리 명령어:**

Cargo는 Rust의 빌드 도구이자 패키지 관리자다. 다음은 일상적으로 사용하는 핵심 명령어들이다.

**프로젝트 생성과 초기화:**

```bash
# 새 바이너리 프로젝트 생성
cargo new my_project

# 새 라이브러리 프로젝트 생성
cargo new --lib my_library

# 기존 디렉토리를 Cargo 프로젝트로 초기화
cargo init

# 라이브러리로 초기화
cargo init --lib
```

**빌드와 실행:**

```bash
# 디버그 모드로 빌드 (개발용, 빠른 컴파일)
cargo build

# 릴리스 모드로 빌드 (최적화, 느린 컴파일)
cargo build --release

# 특정 타겟으로 빌드
cargo build --target x86_64-unknown-linux-musl

# 프로젝트 실행 (빌드 + 실행)
cargo run

# 릴리스 모드로 실행
cargo run --release

# 인자와 함께 실행
cargo run -- --arg1 value1 --arg2 value2

# 특정 바이너리 실행 (여러 바이너리가 있을 때)
cargo run --bin server
```

**코드 검증과 품질 관리:**

```bash
# 컴파일 가능한지만 검사 (실제 빌드보다 빠름)
cargo check

# 모든 워크스페이스 멤버 검사
cargo check --workspace

# 테스트 실행
cargo test

# 특정 테스트만 실행
cargo test test_name

# 출력 표시하며 테스트
cargo test -- --nocapture

# 단일 스레드로 테스트 (순차 실행)
cargo test -- --test-threads=1

# 문서 테스트만 실행
cargo test --doc

# 벤치마크 실행
cargo bench

# 린터(clippy) 실행
cargo clippy

# 엄격한 clippy 검사
cargo clippy -- -W clippy::all -W clippy::pedantic -W clippy::nursery

# 코드 포맷팅
cargo fmt

# 포맷 검사만 (변경하지 않음)
cargo fmt -- --check
```

**의존성 관리:**

```bash
# 의존성 추가
cargo add tokio

# 특정 버전 추가
cargo add serde@1.0

# 기능 플래그와 함께 추가
cargo add tokio --features full

# 개발 의존성 추가
cargo add --dev criterion

# 빌드 의존성 추가
cargo add --build cc

# 의존성 제거
cargo rm serde

# 의존성 업데이트
cargo update

# 특정 크레이트만 업데이트
cargo update serde

# 의존성 트리 확인
cargo tree

# 중복 의존성 확인
cargo tree --duplicates
```

**문서화:**

```bash
# 문서 생성
cargo doc

# 문서 생성 후 브라우저로 열기
cargo doc --open

# 비공개 항목 포함하여 문서 생성
cargo doc --document-private-items

# 의존성 문서 제외
cargo doc --no-deps
```

**프로젝트 정리와 유지보수:**

```bash
# 빌드 결과물 삭제 (target 디렉토리)
cargo clean

# 사용하지 않는 의존성 찾기
cargo install cargo-udeps
cargo +nightly udeps

# 오래된 의존성 확인
cargo install cargo-outdated
cargo outdated

# 라이선스 확인
cargo install cargo-license
cargo license

# 감사 (보안 취약점 검사)
cargo install cargo-audit
cargo audit

# 프로젝트 크기 분석
cargo install cargo-bloat
cargo bloat --release
```

**고급 명령어:**

```bash
# 빌드 스크립트만 실행
cargo build --build-plan

# 확장 (expansion) 확인 (매크로 펼치기)
cargo expand

# 어셈블리 코드 확인
cargo asm function_name

# LLVM IR 확인
cargo llvm-ir function_name

# 워크스페이스 관련
cargo build --workspace      # 모든 멤버 빌드
cargo test --workspace       # 모든 멤버 테스트
cargo build -p member_name   # 특정 멤버만 빌드

# 기능 플래그 관련
cargo build --features "feature1 feature2"
cargo build --all-features
cargo build --no-default-features
```

**Cargo.toml 설정 옵션:**

프로젝트의 `Cargo.toml` 파일은 프로젝트 메타데이터와 의존성을 정의한다. 주요 설정 옵션들을 살펴본다.

**기본 패키지 정보:**

```toml
[package]
name = "my_project"
version = "0.1.0"
edition = "2021"              # Rust 에디션 (2015, 2018, 2021)
authors = ["Your Name <you@example.com>"]
license = "MIT OR Apache-2.0"
description = "간단한 설명"
repository = "https://github.com/user/repo"
homepage = "https://example.com"
documentation = "https://docs.rs/my_project"
readme = "README.md"
keywords = ["network", "async", "server"]
categories = ["network-programming"]
rust-version = "1.70"         # 최소 Rust 버전

# 크레이트 타입 지정
[lib]
name = "my_library"
path = "src/lib.rs"
crate-type = ["lib"]          # lib, dylib, staticlib, cdylib 등

# 바이너리 설정
[[bin]]
name = "server"
path = "src/bin/server.rs"

[[bin]]
name = "client"
path = "src/bin/client.rs"
```

**의존성 설정:**

```toml
[dependencies]
# 기본 의존성
serde = "1.0"

# 버전 지정 방법
tokio = "1.35"              # 1.35.x
tokio = "^1.35"             # 1.35 이상, 2.0 미만 (기본)
tokio = "~1.35"             # 1.35.x (패치만)
tokio = "1.35.0"            # 정확히 1.35.0

# 기능 플래그
tokio = { version = "1.35", features = ["full"] }
serde = { version = "1.0", features = ["derive"], default-features = false }

# Git 저장소에서
my_crate = { git = "https://github.com/user/repo" }
my_crate = { git = "https://github.com/user/repo", branch = "main" }
my_crate = { git = "https://github.com/user/repo", tag = "v0.1.0" }
my_crate = { git = "https://github.com/user/repo", rev = "abc123" }

# 로컬 경로에서
my_crate = { path = "../my_crate" }

# 선택적 의존성 (기능으로 활성화)
optional_crate = { version = "1.0", optional = true }

[dev-dependencies]
# 테스트와 벤치마크용
criterion = "0.5"
proptest = "1.4"

[build-dependencies]
# 빌드 스크립트용
cc = "1.0"
```

**기능 플래그 정의:**

```toml
[features]
# 기본적으로 활성화되는 기능
default = ["std", "serde"]

# 기능 정의
std = []
serde = ["dep:serde"]         # serde 의존성 활성화
full = ["std", "serde", "async"]
async = ["tokio"]

# 다른 크레이트의 기능 전파
tokio-support = ["tokio/full"]
```

**프로필 설정:**

```toml
[profile.dev]
# 개발 프로필 (cargo build)
opt-level = 0        # 최적화 레벨 (0~3, s, z)
debug = true         # 디버그 정보 포함
split-debuginfo = "unpacked"
strip = false        # 심볼 제거 안 함
debug-assertions = true
overflow-checks = true
lto = false         # Link Time Optimization
panic = 'unwind'    # 'unwind' 또는 'abort'
incremental = true  # 증분 컴파일
codegen-units = 256 # 병렬 컴파일 단위

[profile.release]
# 릴리스 프로필 (cargo build --release)
opt-level = 3
debug = false
strip = true
lto = true          # 전체 프로그램 최적화
codegen-units = 1   # 더 나은 최적화를 위해 1로
panic = 'abort'     # 바이너리 크기 감소

[profile.bench]
# 벤치마크 프로필
inherits = "release"

[profile.test]
# 테스트 프로필
inherits = "dev"

# 커스텀 프로필
[profile.production]
inherits = "release"
lto = "fat"
codegen-units = 1
strip = true
```

**워크스페이스 설정:**

```toml
# 루트 Cargo.toml
[workspace]
members = [
    "server",
    "client",
    "common",
]
exclude = ["old_code"]

# 워크스페이스 전체 의존성 버전 관리
[workspace.dependencies]
tokio = { version = "1.35", features = ["full"] }
serde = { version = "1.0", features = ["derive"] }

# 각 멤버에서는
[dependencies]
tokio = { workspace = true }
serde = { workspace = true }
```

**빌드 설정:**

```toml
[build]
# 빌드 스크립트
build = "build.rs"

# 제외할 파일
exclude = ["tests/*", "benches/*"]

# 포함할 파일 (배포 시)
include = [
    "src/**/*",
    "Cargo.toml",
    "README.md",
    "LICENSE",
]
```

**타겟 별 의존성:**

```toml
# 플랫폼별 의존성
[target.'cfg(unix)'.dependencies]
nix = "0.27"

[target.'cfg(windows)'.dependencies]
winapi = "0.3"

[target.'cfg(target_os = "linux")'.dependencies]
libc = "0.2"

# 특정 타겟
[target.x86_64-pc-windows-msvc.dependencies]
windows = "0.51"
```

**유용한 Cargo 설정 파일:**

프로젝트 루트 또는 홈 디렉토리의 `.cargo/config.toml` 파일로 Cargo 동작을 커스터마이징할 수 있다.

```toml
# .cargo/config.toml

[build]
# 타겟 디렉토리 변경
target-dir = "build"

# 기본 타겟 설정
target = "x86_64-unknown-linux-gnu"

# 병렬 작업 수
jobs = 4

# 증분 컴파일
incremental = true

[target.x86_64-unknown-linux-gnu]
# 빠른 링커 사용
linker = "clang"
rustflags = ["-C", "link-arg=-fuse-ld=lld"]

[alias]
# 자주 쓰는 명령어 단축
b = "build"
c = "check"
t = "test"
r = "run"
rr = "run --release"
br = "build --release"
cw = "check --workspace"
tw = "test --workspace"

# 복잡한 명령어 단축
check-all = "check --all-features --all-targets"
test-all = "test --all-features --all-targets"

[net]
# Git fetch 병렬 처리
git-fetch-with-cli = true

[term]
# 컬러 출력
color = "auto"           # auto, always, never

[profile.dev.package."*"]
# 의존성만 최적화 (개발 시 속도 향상)
opt-level = 2
```

## B. 소유권과 빌림 치트시트

**소유권 규칙 요약:**

Rust의 소유권 시스템은 메모리 안전성의 핵심이다. 세 가지 기본 규칙을 항상 기억한다.

```
소유권의 세 가지 규칙:

1. 각 값은 정확히 하나의 소유자(owner)를 갖는다
   ┌─────────┐
   │  값     │
   │  "abc"  │
   └────┬────┘
        │ 소유됨
   ┌────▼────┐
   │ 변수 s  │
   └─────────┘

2. 한 번에 하나의 소유자만 존재할 수 있다
   let s1 = String::from("hello");
   let s2 = s1;  // s1의 소유권이 s2로 이동
   // s1은 더 이상 사용 불가

3. 소유자가 스코프를 벗어나면 값은 drop된다
   {
       let s = String::from("hello");
       // s 사용
   } // 여기서 s가 drop됨
```

**빌림 규칙 요약:**

```
빌림의 규칙:

1. 여러 개의 불변 참조(&T) 또는
   정확히 하나의 가변 참조(&mut T)

   ✓ 가능:
   let r1 = &x;
   let r2 = &x;
   let r3 = &x;

   ✓ 가능:
   let r1 = &mut x;

   ✗ 불가능:
   let r1 = &x;
   let r2 = &mut x;  // 에러!

2. 참조는 항상 유효해야 함 (댕글링 참조 금지)

   ✗ 불가능:
   fn dangle() -> &String {
       let s = String::from("hello");
       &s  // s가 drop되므로 에러!
   }

   ✓ 가능:
   fn no_dangle() -> String {
       let s = String::from("hello");
       s  // 소유권 이동
   }
```

**주요 타입별 동작:**

```rust
// Copy 타입 (스택에 저장되는 작은 타입들)
let x = 5;
let y = x;  // 복사됨, x도 여전히 유효

// Copy 타입들:
// - 모든 정수 타입 (i32, u64 등)
// - 부동소수점 타입 (f32, f64)
// - bool
// - char
// - Copy 타입만으로 구성된 튜플과 배열

// Move 타입 (힙 할당 또는 Drop 구현)
let s1 = String::from("hello");
let s2 = s1;  // 이동됨, s1은 무효화

// Move 타입들:
// - String
// - Vec<T>
// - Box<T>
// - HashMap, HashSet
// - 대부분의 구조체 (Copy를 구현하지 않은)
```

**참조와 빌림 패턴:**

```rust
// 읽기 전용 공유
fn read_only(s: &String) {
    println!("{}", s);
    // s.push_str("!"); // 에러: 가변 참조 필요
}

let text = String::from("hello");
read_only(&text);
read_only(&text);  // 여러 번 빌릴 수 있음
println!("{}", text);  // text는 여전히 유효

// 수정 가능
fn modify(s: &mut String) {
    s.push_str(" world");
}

let mut text = String::from("hello");
modify(&mut text);
println!("{}", text);  // "hello world"

// 소유권 가져가기
fn take_ownership(s: String) {
    println!("{}", s);
}  // s가 여기서 drop

let text = String::from("hello");
take_ownership(text);
// println!("{}", text);  // 에러: text는 이동됨
```

**흔한 에러 메시지와 해결법:**

**1. "value used after move"**

```rust
// 문제
let s1 = String::from("hello");
let s2 = s1;
println!("{}", s1);  // 에러!

// 해결 1: clone 사용
let s1 = String::from("hello");
let s2 = s1.clone();
println!("{}", s1);  // OK

// 해결 2: 참조 사용
let s1 = String::from("hello");
let s2 = &s1;
println!("{}", s1);  // OK

// 해결 3: s1을 나중에 사용
let s1 = String::from("hello");
println!("{}", s1);
let s2 = s1;  // 이동은 마지막에
```

**2. "cannot borrow as mutable more than once"**

```rust
// 문제
let mut s = String::from("hello");
let r1 = &mut s;
let r2 = &mut s;  // 에러!
println!("{} {}", r1, r2);

// 해결 1: 빌림 스코프 분리
let mut s = String::from("hello");
{
    let r1 = &mut s;
    println!("{}", r1);
}  // r1이 여기서 끝남
let r2 = &mut s;  // OK
println!("{}", r2);

// 해결 2: 순차적으로 사용
let mut s = String::from("hello");
let r1 = &mut s;
println!("{}", r1);
// r1을 더 이상 사용하지 않음
let r2 = &mut s;  // OK
println!("{}", r2);
```

**3. "cannot borrow as mutable because it is also borrowed as immutable"**

```rust
// 문제
let mut s = String::from("hello");
let r1 = &s;
let r2 = &s;
let r3 = &mut s;  // 에러!
println!("{} {} {}", r1, r2, r3);

// 해결: 불변 참조 사용 후 가변 참조
let mut s = String::from("hello");
let r1 = &s;
let r2 = &s;
println!("{} {}", r1, r2);
// r1, r2를 더 이상 사용하지 않음

let r3 = &mut s;  // OK
println!("{}", r3);
```

**4. "returns a value referencing data owned by the current function"**

```rust
// 문제: 댕글링 참조
fn dangle() -> &String {
    let s = String::from("hello");
    &s  // 에러: s는 함수 종료 시 drop됨
}

// 해결 1: 소유권 반환
fn no_dangle() -> String {
    let s = String::from("hello");
    s  // 소유권 이동
}

// 해결 2: 정적 수명
fn static_ref() -> &'static str {
    "hello"  // 문자열 리터럴은 'static
}

// 해결 3: 라이프타임 명시 (입력에서 온 참조)
fn first_word<'a>(s: &'a str) -> &'a str {
    s.split_whitespace().next().unwrap_or("")
}
```

**5. "lifetime may not live long enough"**

```rust
// 문제
struct Wrapper<'a> {
    data: &'a str,
}

fn create_wrapper() -> Wrapper<'static> {
    let s = String::from("hello");
    Wrapper { data: &s }  // 에러!
}

// 해결 1: 소유한 데이터 사용
struct Wrapper {
    data: String,
}

fn create_wrapper() -> Wrapper {
    Wrapper { 
        data: String::from("hello") 
    }
}

// 해결 2: 정적 문자열
fn create_wrapper() -> Wrapper<'static> {
    Wrapper { data: "hello" }
}
```

**6. "cannot move out of borrowed content"**

```rust
// 문제
fn take_string(opt: &Option<String>) {
    if let Some(s) = opt {
        // s는 &String 타입
        let owned = *s;  // 에러: 빌린 것을 move할 수 없음
    }
}

// 해결 1: clone
fn take_string(opt: &Option<String>) {
    if let Some(s) = opt {
        let owned = s.clone();  // OK
    }
}

// 해결 2: 참조로 작업
fn take_string(opt: &Option<String>) {
    if let Some(s) = opt {
        println!("{}", s);  // 참조 사용
    }
}

// 해결 3: 소유권 가져오기
fn take_string(opt: Option<String>) {
    if let Some(s) = opt {
        let owned = s;  // OK
    }
}
```

**라이프타임 체크시트:**

```rust
// 기본 라이프타임 생략 규칙

// 규칙 1: 각 참조 매개변수는 자신만의 라이프타임을 갖는다
fn foo<'a, 'b>(x: &'a str, y: &'b str)

// 규칙 2: 입력 라이프타임이 하나면 출력도 그것을 사용
fn foo<'a>(x: &'a str) -> &'a str

// 규칙 3: 메서드에서 &self가 있으면 출력은 self의 라이프타임
impl MyStruct {
    fn method<'a>(&'a self, x: &str) -> &'a str
}

// 명시적 라이프타임이 필요한 경우

// 두 입력 중 하나를 반환
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() > y.len() { x } else { y }
}

// 구조체에 참조 저장
struct ImportantExcerpt<'a> {
    part: &'a str,
}

impl<'a> ImportantExcerpt<'a> {
    fn level(&self) -> i32 { 3 }
    
    fn announce_and_return_part(&self, announcement: &str) -> &str {
        println!("주목: {}", announcement);
        self.part  // self의 라이프타임 반환
    }
}

// 정적 라이프타임
let s: &'static str = "영원히 살아있는 문자열";
```

**메모리 레이아웃 시각화:**

```
스택 vs 힙:

스택                         힙
┌──────────┐                ┌────────────────┐
│ i32: 5   │                │                │
├──────────┤                │  String 데이터 │
│ bool:    │                │  "hello world" │
│  true    │                │                │
├──────────┤                └────────▲───────┘
│ ptr ─────┼─────────────────────────┘
│ len: 11  │     String은 스택에 ptr, len, cap 저장
│ cap: 11  │     실제 데이터는 힙에 저장
└──────────┘

참조의 메모리:

  변수                      데이터
┌──────────┐             ┌──────────┐
│ s: ptr ──┼────────────>│ "hello"  │
│    len   │             │  (힙)    │
│    cap   │             └──────────┘
└──────────┘                  ▲
                              │
┌──────────┐                  │
│ r: ptr ──┼──────────────────┘
└──────────┘     참조는 포인터만 저장
                 (8 bytes on 64-bit)
```

**실전 패턴:**

```rust
// 패턴 1: 빌더 패턴 (메서드 체이닝)
struct Config {
    host: String,
    port: u16,
}

impl Config {
    fn new() -> Self {
        Config {
            host: "localhost".to_string(),
            port: 8080,
        }
    }
    
    fn host(&mut self, host: String) -> &mut Self {
        self.host = host;
        self  // &mut self 반환
    }
    
    fn port(&mut self, port: u16) -> &mut Self {
        self.port = port;
        self
    }
}

// 사용
let config = Config::new()
    .host("0.0.0.0".to_string())
    .port(3000);

// 패턴 2: 내부 가변성 (RefCell)
use std::cell::RefCell;

struct Database {
    cache: RefCell<HashMap<String, String>>,
}

impl Database {
    fn get(&self, key: &str) -> Option<String> {
        // 불변 참조지만 내부 수정 가능
        let mut cache = self.cache.borrow_mut();
        cache.get(key).cloned()
    }
}

// 패턴 3: Arc + Mutex (스레드 간 공유)
use std::sync::{Arc, Mutex};

let counter = Arc::new(Mutex::new(0));
let mut handles = vec![];

for _ in 0..10 {
    let counter = Arc::clone(&counter);
    let handle = std::thread::spawn(move || {
        let mut num = counter.lock().unwrap();
        *num += 1;
    });
    handles.push(handle);
}

for handle in handles {
    handle.join().unwrap();
}

// 패턴 4: Cow (Copy-on-Write)
use std::borrow::Cow;

fn process_text(text: &str) -> Cow<str> {
    if text.contains("ERROR") {
        // 수정 필요 - 소유한 String 반환
        Cow::Owned(text.replace("ERROR", "WARNING"))
    } else {
        // 수정 불필요 - 빌린 참조 반환
        Cow::Borrowed(text)
    }
}
```

**성능 고려사항:**

```rust
// ❌ 비효율적: 불필요한 clone
fn bad_example(data: &Vec<String>) -> Vec<String> {
    let mut result = data.clone();  // 전체 복사!
    result.retain(|s| s.len() > 5);
    result
}

// ✓ 효율적: 참조 사용
fn good_example(data: &Vec<String>) -> Vec<&String> {
    data.iter()
        .filter(|s| s.len() > 5)
        .collect()
}

// ❌ 비효율적: 반복적인 할당
fn bad_concat(parts: &[&str]) -> String {
    let mut result = String::new();
    for part in parts {
        result = result + part;  // 매번 새 String!
    }
    result
}

// ✓ 효율적: 미리 용량 할당
fn good_concat(parts: &[&str]) -> String {
    let total_len: usize = parts.iter().map(|s| s.len()).sum();
    let mut result = String::with_capacity(total_len);
    for part in parts {
        result.push_str(part);
    }
    result
}

// ✓ 더 좋음: iterator 사용
fn best_concat(parts: &[&str]) -> String {
    parts.join("")
}
```

**디버깅 팁:**

```rust
// 소유권 이동 추적
let s = String::from("hello");
dbg!(&s);  // 참조로 전달하여 소유권 유지
println!("{:?}", s);  // 여전히 사용 가능

// 라이프타임 디버깅
// 컴파일러 에러 메시지를 주의깊게 읽는다
// 예: "borrowed value does not live long enough"
// -> 참조가 가리키는 값이 먼저 drop됨

// Miri로 unsafe 코드 검증
// cargo +nightly miri test

// Clippy 경고 수정
// cargo clippy --fix
```

이 부록을 일상적인 개발 중 빠른 참조 가이드로 활용한다. 시간이 지나면서 이러한 패턴들이 자연스럽게 손에 익게 되지만, 처음에는 자주 참고하며 올바른 습관을 만드는 것이 중요하다.