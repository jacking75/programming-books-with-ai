# Chapter 10. unsafe — 존재하지만 거의 쓸 일이 없다

> "unsafe는 탈출구이지 지름길이 아니다. 대부분의 게임 서버 코드에서는 필요하지 않다."

---

## 10.1 unsafe란 무엇인가

Rust의 안전성 보장 중 일부는 컴파일러가 증명할 수 없는 경우에 사람이 직접 책임을 지는 메커니즘이 필요하다. 이것이 `unsafe` 블록이다.

```rust
// unsafe 블록
unsafe {
    // 컴파일러가 안전성을 보장하지 않는 코드
    let raw_ptr: *const i32 = &42 as *const i32;
    let value = *raw_ptr; // 원시 포인터 역참조
}

// unsafe 함수
unsafe fn dangerous_operation(ptr: *mut i32) {
    *ptr = 42; // 이 함수를 호출하는 것 자체가 안전하지 않다
}
```

`unsafe`는 다음 다섯 가지 작업을 허용한다:
1. 원시 포인터(raw pointer) 역참조
2. unsafe 함수/메서드 호출
3. 가변 전역 변수 접근 또는 수정
4. unsafe 트레이트 구현
5. `union` 필드 접근

**중요**: `unsafe` 블록이 있다고 Rust의 모든 안전성 보장이 사라지는 것이 아니다. 소유권, 빌림 규칙은 unsafe 블록 안에서도 여전히 적용된다. 다만 컴파일러가 증명할 수 없는 위의 다섯 가지 작업을 허용할 뿐이다.

---

## 10.2 unsafe가 필요한 진짜 상황

게임 서버 도메인에서 `unsafe`가 정당화되는 경우는 매우 제한적이다.

### 1. 외부 C 라이브러리(FFI) 연동

C로 작성된 게임 엔진 컴포넌트나 외부 라이브러리를 사용할 때:

```rust
// C 라이브러리의 함수 선언
extern "C" {
    fn physics_world_create() -> *mut PhysicsWorld;
    fn physics_world_step(world: *mut PhysicsWorld, dt: f32);
    fn physics_world_destroy(world: *mut PhysicsWorld);
    fn add_rigid_body(
        world: *mut PhysicsWorld,
        mass: f32,
        x: f32, y: f32, z: f32
    ) -> *mut RigidBody;
}

// 안전한 래퍼 구조체
pub struct PhysicsWorldWrapper {
    raw: *mut PhysicsWorld, // 원시 포인터
}

impl PhysicsWorldWrapper {
    pub fn new() -> Self {
        let raw = unsafe { physics_world_create() };
        assert!(!raw.is_null(), "Failed to create physics world");
        PhysicsWorldWrapper { raw }
    }
    
    // 안전한 공개 API
    pub fn step(&mut self, dt: f32) {
        unsafe { physics_world_step(self.raw, dt); }
    }
}

impl Drop for PhysicsWorldWrapper {
    fn drop(&mut self) {
        unsafe { physics_world_destroy(self.raw); }
    }
}

// unsafe impl: 이 래퍼는 실제로 스레드 안전하다고 우리가 보장
unsafe impl Send for PhysicsWorldWrapper {}
```

핵심 원칙: **unsafe를 내부에 가두고 안전한 공개 API를 노출**한다. 이것을 "안전한 추상화(safe abstraction)"라고 한다.

### 2. 성능이 극도로 중요한 특수 경우

```rust
// 경계 검사 비용 제거 (극도로 성능 민감한 코드)
fn sum_unchecked(data: &[f32]) -> f32 {
    let mut sum = 0.0f32;
    // data.len()이 checked이므로 안전하지만, 컴파일러가 최적화 못 하는 경우
    for i in 0..data.len() {
        // 안전한 버전: sum += data[i]; (경계 검사 있음)
        unsafe {
            sum += *data.get_unchecked(i); // 경계 검사 없음 - 프로그래머가 보장
        }
    }
    sum
}
```

그러나 현대 컴파일러는 대부분의 경우 반복자를 사용하면 경계 검사를 제거한다:

```rust
// 컴파일러가 자동으로 경계 검사를 제거
fn sum_safe(data: &[f32]) -> f32 {
    data.iter().sum() // SIMD로 자동 최적화까지 됨
}
```

`unsafe get_unchecked`가 `iter().sum()`보다 실제로 빠른지 벤치마킹을 통해 확인해야 한다.

### 3. 내부 가변성 구현

락 없이 원자적 작업을 수행하는 커스텀 자료구조:

```rust
use std::sync::atomic::{AtomicU64, Ordering};

// 락프리 통계 카운터 (내부적으로 atomic 사용)
pub struct LockFreeCounter {
    count: AtomicU64,
}

impl LockFreeCounter {
    pub fn new() -> Self {
        LockFreeCounter { count: AtomicU64::new(0) }
    }
    
    pub fn increment(&self) {
        self.count.fetch_add(1, Ordering::Relaxed);
    }
    
    pub fn get(&self) -> u64 {
        self.count.load(Ordering::Relaxed)
    }
}
// 이 경우는 unsafe 없음 - 원자 타입이 안전하게 추상화되어 있음
```

---

## 10.3 AI가 unsafe를 쓰는 나쁜 패턴

바이브 코딩 시 AI가 `unsafe`를 사용하는 경우는 대부분 잘못된 방향이다.

### 나쁜 패턴 1: 빌림 오류 회피

```rust
// AI가 생성할 수 있는 코드 - 매우 위험
fn bad_get_two_mut(v: &mut Vec<i32>, i: usize, j: usize) -> (&mut i32, &mut i32) {
    unsafe {
        let ptr = v.as_mut_ptr();
        (&mut *ptr.add(i), &mut *ptr.add(j)) // 두 가변 참조를 동시에 반환
    }
}
```

이 코드는 i == j인 경우 UB(undefined behavior)다. 컴파일 오류를 unsafe로 회피했지만 실제 위험이 남아있다.

**올바른 해결책**:
```rust
fn good_get_two_mut(v: &mut Vec<i32>, i: usize, j: usize) -> (&mut i32, &mut i32) {
    assert_ne!(i, j, "Indices must be different");
    assert!(i < v.len() && j < v.len(), "Indices out of bounds");
    
    // 표준 라이브러리의 안전한 방법
    if i < j {
        let (left, right) = v.split_at_mut(j);
        (&mut left[i], &mut right[0])
    } else {
        let (left, right) = v.split_at_mut(i);
        (&mut right[0], &mut left[j])
    }
}
```

### 나쁜 패턴 2: 라이프타임 어노테이션 회피

```rust
// AI가 생성할 수 있는 코드 - 매우 위험
fn bad_extend_lifetime<'a>(s: &str) -> &'a str {
    unsafe { std::mem::transmute(s) } // 라이프타임 강제 연장
}
```

이것은 댕글링 참조를 만들 수 있다. `transmute`는 Rust에서 가장 위험한 함수 중 하나다.

**올바른 접근**: 라이프타임을 올바르게 설계하거나, 참조 대신 소유된 값(`String`)을 반환한다.

### 나쁜 패턴 3: Send/Sync 강제 구현

```rust
// AI가 생성할 수 있는 코드 - 검토 필요
struct PlayerCache {
    data: *mut HashMap<u64, Player>, // 원시 포인터
}

// AI가 컴파일 오류를 피하려고 추가하는 코드
unsafe impl Send for PlayerCache {}
unsafe impl Sync for PlayerCache {}
```

원시 포인터는 실제로 스레드 안전하지 않다. 이렇게 구현하면 데이터 레이스가 발생할 수 있다.

**올바른 접근**: `Arc<Mutex<HashMap<u64, Player>>>`를 사용한다.

---

## 10.4 unsafe를 대체하는 안전한 도구들

많은 경우 `unsafe` 없이 동일한 목표를 달성할 수 있다.

### std::collections::HashMap → 직접 관리 불필요

C++에서는 자주 해시맵을 커스텀 구현했지만, Rust의 표준 HashMap은 이미 매우 최적화되어 있다. `unsafe` 커스텀 구현이 필요한 경우는 드물다.

### Vec::with_capacity → 사전 할당

```rust
// 최악: 매번 재할당
let mut packets = Vec::new();
for _ in 0..1000 {
    packets.push(create_packet());
}

// 최선: 미리 크기 지정
let mut packets = Vec::with_capacity(1000);
for _ in 0..1000 {
    packets.push(create_packet());
}
```

### 객체 풀 → unsafe 없는 구현

```rust
// unsafe 없이 안전한 객체 풀 구현 (Chapter 6에서 다룸)
use std::sync::Mutex;

pub struct Pool<T> {
    objects: Mutex<Vec<T>>,
    factory: Box<dyn Fn() -> T + Send + Sync>,
}
```

### 인덱스 기반 간접 참조

자기 참조 구조체나 순환 참조 대신 인덱스로 간접 참조:

```rust
// unsafe 없이 복잡한 그래프 구조
struct Graph {
    nodes: Vec<Node>,
    edges: Vec<(usize, usize)>, // 인덱스 기반
}

impl Graph {
    fn add_edge(&mut self, from: usize, to: usize) {
        self.edges.push((from, to));
    }
    
    fn neighbors(&self, node_idx: usize) -> impl Iterator<Item = usize> + '_ {
        self.edges.iter()
            .filter(move |(from, _)| *from == node_idx)
            .map(|(_, to)| *to)
    }
}
```

---

## 10.5 unsafe 코드 작성 시 필수 규칙

어쩔 수 없이 `unsafe`를 써야 할 때 따라야 하는 규칙들.

### 규칙 1: unsafe 범위를 최소화

```rust
// 나쁨 - 너무 많은 코드가 unsafe
unsafe {
    let ptr = get_raw_ptr();
    let val = *ptr;
    process(val);
    do_more_stuff();
    even_more_stuff();
}

// 좋음 - unsafe는 딱 필요한 부분만
let val = unsafe { *get_raw_ptr() };
process(val);
do_more_stuff();
even_more_stuff();
```

### 규칙 2: 불변식을 주석으로 문서화

```rust
/// # Safety
/// 
/// 이 함수를 안전하게 호출하려면:
/// - `ptr`이 null이 아니어야 한다
/// - `ptr`이 가리키는 메모리가 최소 `len * size_of::<T>()` 바이트여야 한다
/// - 메모리가 properly aligned되어 있어야 한다
/// - 반환된 슬라이스의 생존 동안 다른 mutable access가 없어야 한다
pub unsafe fn slice_from_raw<T>(ptr: *const T, len: usize) -> &'static [T] {
    std::slice::from_raw_parts(ptr, len)
}
```

### 규칙 3: 안전한 래퍼로 캡슐화

```rust
// 외부에 노출되는 안전한 API
pub struct AlignedBuffer {
    data: Vec<u8>,
    alignment: usize,
}

impl AlignedBuffer {
    pub fn new(size: usize, alignment: usize) -> Self {
        // 정렬된 메모리 할당 (내부적으로 unsafe 사용)
        // ...
        AlignedBuffer { data: vec![0u8; size + alignment], alignment }
    }
    
    pub fn as_ptr(&self) -> *const u8 {
        // 내부에서 안전하게 계산
        let addr = self.data.as_ptr() as usize;
        let aligned = (addr + self.alignment - 1) & !(self.alignment - 1);
        aligned as *const u8
    }
    
    // 안전한 슬라이스 접근 제공
    pub fn as_slice(&self) -> &[u8] {
        let ptr = self.as_ptr();
        // SAFETY: ptr은 유효한 정렬된 포인터이고,
        // data의 생존 기간 동안 유효하다
        unsafe {
            std::slice::from_raw_parts(ptr, self.data.len() / 2)
        }
    }
}
```

### 규칙 4: 테스트 필수

```rust
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn test_aligned_buffer() {
        let buf = AlignedBuffer::new(100, 64);
        let ptr = buf.as_ptr();
        
        // 정렬 확인
        assert_eq!((ptr as usize) % 64, 0);
    }
    
    #[test]
    fn test_slice_access() {
        let buf = AlignedBuffer::new(100, 16);
        let slice = buf.as_slice();
        assert!(slice.len() > 0);
        let _ = slice[0]; // panic 없이 접근
    }
}
```

Miri를 사용하면 unsafe 코드의 UB를 탐지할 수 있다:
```bash
cargo +nightly miri test
```

---

## 10.6 게임 서버에서 unsafe 없이 최적화하는 방법

대부분의 게임 서버 성능 문제는 `unsafe` 없이 해결할 수 있다.

### 핫패스 최적화 체크리스트

**1. 불필요한 할당 제거**

```rust
// 나쁨: 루프마다 Vec 할당
async fn process_all_players(players: &[Player]) {
    for player in players {
        let events: Vec<Event> = player.pending_events.clone(); // 클론!
        process_events(events).await;
    }
}

// 좋음: 참조 전달
async fn process_all_players(players: &[Player]) {
    for player in players {
        process_events(&player.pending_events).await; // 참조
    }
}
```

**2. 사전 할당 활용**

```rust
impl GameWorld {
    fn new() -> Self {
        GameWorld {
            players: HashMap::with_capacity(10000), // 예상 최대 플레이어
            events: Vec::with_capacity(1000),       // 틱당 예상 이벤트 수
        }
    }
    
    fn tick(&mut self) {
        self.events.clear(); // 해제하지 않고 내용만 비움
        // 이벤트 수집...
        self.process_events(); // 재사용
    }
}
```

**3. 구조체 레이아웃 최적화**

```rust
// 나쁨 - 불필요한 패딩
struct BadPlayer {
    alive: bool,         // 1 byte
    // padding: 7 bytes (다음 필드 정렬을 위해)
    score: u64,          // 8 bytes
    name: String,        // 24 bytes
    hp: u8,              // 1 byte
    // padding: 7 bytes
}

// 좋음 - 크기 순 정렬
struct GoodPlayer {
    score: u64,          // 8 bytes
    name: String,        // 24 bytes
    hp: u8,              // 1 byte
    alive: bool,         // 1 byte
    // padding: 6 bytes
}
// 구조체 크기: 40 bytes (BadPlayer는 48 bytes)
```

**4. SIMD는 컴파일러에게 맡기기**

```rust
// 반복자를 사용하면 컴파일러가 SIMD 자동 생성
let sum: f32 = positions.iter().map(|p| p.x).sum();

// 컴파일러 힌트 (target_feature로 명시)
#[target_feature(enable = "avx2")]
unsafe fn fast_sum(data: &[f32]) -> f32 {
    data.iter().sum() // 이미 SIMD로 최적화됨
}
```

**5. 캐시 친화적 자료구조**

```rust
// 나쁨 - 캐시 미스 유발 (포인터 추적)
struct SceneNode {
    children: Vec<Box<SceneNode>>, // 힙 힙 힙...
}

// 좋음 - 슬롯맵 (연속 메모리)
use slotmap::{SlotMap, DefaultKey};

struct Scene {
    nodes: SlotMap<DefaultKey, SceneNodeData>,
    children: HashMap<DefaultKey, Vec<DefaultKey>>,
}
```

---

## 10.7 unsafe 코드 리뷰 체크리스트

AI가 생성한 코드에 `unsafe`가 있다면 다음을 확인한다.

**검토 항목**:

1. **unsafe가 정말 필요한가?** 안전한 대안이 없는지 먼저 확인한다.

2. **불변식이 문서화되어 있는가?** `/// # Safety` 주석이 있는지 확인한다.

3. **원시 포인터의 유효성이 보장되는가?**
   - null 체크가 있는가?
   - 포인터가 가리키는 메모리가 여전히 유효한가?
   - 정렬 요구사항을 만족하는가?

4. **두 개의 가변 참조가 같은 메모리를 가리키지 않는가?**

5. **데이터 레이스가 없는가?** 여러 스레드에서 접근하는 경우 동기화가 되어 있는가?

6. **UB(Undefined Behavior) 가능성은 없는가?**
   - 초기화되지 않은 메모리 읽기
   - 정수 오버플로 (release 모드에서 UB)
   - `transmute`를 통한 잘못된 타입 변환

---

## 10.8 AI에게 unsafe 관련 지시를 내리는 법

### 기본 원칙: safe Rust 요구

> "이 코드를 safe Rust만으로 작성해줘. unsafe 블록, unsafe fn, 
> unsafe impl을 사용하지 말아줘."

### unsafe가 불가피한 경우

> "이 외부 C 라이브러리 바인딩을 작성해줘.
> unsafe 코드는 최소한의 범위로 유지하고,
> 안전한 Rust 래퍼 API를 공개 인터페이스로 노출해줘.
> 모든 unsafe 블록에 # Safety 주석으로 불변식을 문서화해줘."

### unsafe 오류 수정

> "이 코드에서 AI가 빌림 오류를 피하려고 unsafe transmute를 사용했어.
> unsafe 없이 동일한 기능을 구현해줘.
> 힌트: split_at_mut이나 인덱스 기반 접근으로 해결 가능할 것 같아."

---

## 10.9 정리

unsafe의 의미 — 컴파일러가 안전성을 증명할 수 없는 다섯 가지 작업을 사람이 책임지고 수행할 수 있게 한다.

게임 서버에서는 거의 불필요 — FFI 바인딩, 극도의 성능 최적화, 커스텀 동기화 구조체 외에는 safe Rust로 모든 것이 가능하다.

AI의 unsafe 생성은 경고 신호 — 빌림 오류나 라이프타임 오류를 unsafe로 회피하는 코드는 버그의 씨앗이다. 원인을 파악하고 안전한 방법을 찾아야 한다.

안전한 대안들 — 대부분의 경우 객체 풀, 사전 할당, 인덱스 기반 간접 참조, 표준 컬렉션의 안전한 메서드로 unsafe 없이 목표를 달성할 수 있다.

불가피한 경우의 규칙 — 범위 최소화, 불변식 문서화, 안전한 래퍼 캡슐화, 테스트 필수.

---

*다음 챕터: Chapter 11 — Cargo와 빌드 워크플로*
