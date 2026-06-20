# Rust 빌림과 참조: C++ 프로그래머를 위한 실전 가이드  

저자: 최흥배, Claude AI   
      
-----    

# 부록

## A. 빌림 검사기 에러 메시지 해석 가이드

Rust 컴파일러의 빌림 검사기는 처음에는 어렵게 느껴지지만, 에러 메시지를 체계적으로 이해하면 문제 해결이 훨씬 수월해진다. 이 섹션에서는 실전에서 자주 마주치는 에러 메시지들을 분석하고 해결 방법을 제시한다.

### A.1 "cannot borrow as mutable because it is also borrowed as immutable"

이 에러는 불변 빌림이 활성화된 상태에서 가변 빌림을 시도할 때 발생한다.

```rust
// 에러 발생 코드
fn process_data(data: &mut Vec<i32>) {
    let first = &data[0];  // 불변 빌림
    data.push(10);         // 가변 빌림 시도 - 에러!
    println!("{}", first);
}
```

**에러 메시지 분석:**
```
error[E0502]: cannot borrow `*data` as mutable because it is also borrowed as immutable
 --> src/main.rs:3:5
  |
2 |     let first = &data[0];
  |                  ---- immutable borrow occurs here
3 |     data.push(10);
  |     ^^^^^^^^^^^^^ mutable borrow occurs here
4 |     println!("{}", first);
  |                    ----- immutable borrow later used here
```

컴파일러는 세 가지 중요한 정보를 제공한다:
1. 불변 빌림이 발생한 위치
2. 가변 빌림을 시도한 위치
3. 불변 빌림이 사용되는 마지막 위치

**해결 방법 1: 빌림 스코프 축소**
```rust
fn process_data(data: &mut Vec<i32>) {
    let first_value = data[0];  // 값을 복사
    data.push(10);
    println!("{}", first_value);
}
```

**해결 방법 2: 사용 순서 재배치**
```rust
fn process_data(data: &mut Vec<i32>) {
    let first = &data[0];
    println!("{}", first);  // 여기서 불변 빌림 종료
    data.push(10);          // 이제 가변 빌림 가능
}
```

### A.2 "cannot borrow as mutable more than once at a time"

동시에 여러 개의 가변 빌림을 시도할 때 발생하는 에러다.

```rust
// 에러 발생 코드
struct Database {
    users: Vec<String>,
    logs: Vec<String>,
}

impl Database {
    fn add_user_with_log(&mut self, name: String) {
        let users = &mut self.users;    // 첫 번째 가변 빌림
        let logs = &mut self.logs;      // 두 번째 가변 빌림 - 에러!
        
        users.push(name.clone());
        logs.push(format!("Added user: {}", name));
    }
}
```

**에러 메시지:**
```
error[E0499]: cannot borrow `self.logs` as mutable more than once at a time
 --> src/main.rs:9:20
  |
8 |         let users = &mut self.users;
  |                     --------------- first mutable borrow occurs here
9 |         let logs = &mut self.logs;
  |                    ^^^^^^^^^^^^^^ second mutable borrow occurs here
10|         users.push(name.clone());
  |         ----- first borrow later used here
```

**해결 방법: 분할 빌림 활용**
```rust
impl Database {
    fn add_user_with_log(&mut self, name: String) {
        // self 전체를 빌리지 않고 각 필드를 직접 빌림
        self.users.push(name.clone());
        self.logs.push(format!("Added user: {}", name));
    }
}
```

Rust 컴파일러는 구조체의 서로 다른 필드에 대한 동시 가변 빌림을 허용한다. 위 코드처럼 중간 변수 없이 직접 접근하면 분할 빌림이 자동으로 적용된다.

### A.3 "cannot move out of borrowed content"

빌린 데이터에서 소유권을 이동하려 할 때 발생한다.

```rust
// 에러 발생 코드
struct Container {
    value: String,
}

fn take_value(container: &Container) -> String {
    container.value  // 에러! 빌린 값에서 소유권 이동 불가
}
```

**에러 메시지:**
```
error[E0507]: cannot move out of `container.value` which is behind a shared reference
 --> src/main.rs:6:5
  |
6 |     container.value
  |     ^^^^^^^^^^^^^^^ move occurs because `container.value` has type `String`,
  |                     which does not implement the `Copy` trait
```

**해결 방법 1: 참조 반환**
```rust
fn take_value(container: &Container) -> &String {
    &container.value
}
```

**해결 방법 2: 클론 사용**
```rust
fn take_value(container: &Container) -> String {
    container.value.clone()
}
```

**해결 방법 3: 가변 참조로 소유권 이동**
```rust
fn take_value(container: &mut Container) -> String {
    std::mem::take(&mut container.value)
}
```

### A.4 "returns a value referencing data owned by the current function"

함수 내부 데이터에 대한 참조를 반환하려 할 때 발생한다.

```rust
// 에러 발생 코드
fn create_string_ref() -> &String {
    let s = String::from("hello");
    &s  // 에러! 함수가 끝나면 s는 소멸됨
}
```

**에러 메시지:**
```
error[E0515]: cannot return reference to local variable `s`
 --> src/main.rs:3:5
  |
3 |     &s
  |     ^^ returns a reference to data owned by the current function
```

**해결 방법 1: 소유권 반환**
```rust
fn create_string() -> String {
    String::from("hello")
}
```

**해결 방법 2: 정적 문자열 사용**
```rust
fn create_string_ref() -> &'static str {
    "hello"
}
```

**해결 방법 3: 입력 참조를 기반으로 반환**
```rust
fn get_first_word(s: &String) -> &str {
    s.split_whitespace().next().unwrap_or("")
}
```

### A.5 "lifetime may not live long enough"

라이프타임 관계가 충족되지 않을 때 발생한다.

```rust
// 에러 발생 코드
struct Parser<'a> {
    data: &'a str,
}

impl<'a> Parser<'a> {
    fn get_token(&self) -> &'a str {
        let temp = String::from("token");
        &temp  // 에러! temp의 라이프타임은 'a보다 짧음
    }
}
```

**에러 메시지:**
```
error[E0515]: cannot return reference to local variable `temp`
  --> src/main.rs:9:9
   |
9  |         &temp
   |         ^^^^^ returns a reference to data owned by the current function
```

**해결 방법 1: 라이프타임 분리**
```rust
impl<'a> Parser<'a> {
    fn get_token(&self) -> &str {  // 'a가 아닌 익명 라이프타임
        self.data.split_whitespace().next().unwrap_or("")
    }
}
```

**해결 방법 2: 소유 타입 반환**
```rust
impl<'a> Parser<'a> {
    fn get_token(&self) -> String {
        String::from("token")
    }
}
```

### A.6 "closure may outlive the current function"

클로저가 캡처한 참조보다 오래 살 수 있을 때 발생한다.

```rust
// 에러 발생 코드
fn create_closure() -> impl Fn() {
    let x = 5;
    move || println!("{}", x)  // OK: move로 소유권 이동
}

fn create_ref_closure() -> impl Fn() {
    let s = String::from("hello");
    || println!("{}", s)  // 에러! s에 대한 참조를 캡처
}
```

**에러 메시지:**
```
error[E0373]: closure may outlive the current function, but it borrows `s`,
              which is owned by the current function
 --> src/main.rs:7:5
  |
7 |     || println!("{}", s)
  |     ^^                - `s` is borrowed here
  |     |
  |     may outlive borrowed value `s`
```

**해결 방법 1: move 키워드 사용**
```rust
fn create_ref_closure() -> impl Fn() {
    let s = String::from("hello");
    move || println!("{}", s)  // s의 소유권을 클로저로 이동
}
```

**해결 방법 2: Rc 사용**
```rust
use std::rc::Rc;

fn create_shared_closure() -> impl Fn() {
    let s = Rc::new(String::from("hello"));
    let s_clone = s.clone();
    move || println!("{}", s_clone)
}
```

### A.7 "cannot borrow `self` as mutable because it is also borrowed as immutable"

메서드 호출 체인에서 자주 발생하는 에러다.

```rust
// 에러 발생 코드
struct Builder {
    data: Vec<i32>,
}

impl Builder {
    fn get_data(&self) -> &Vec<i32> {
        &self.data
    }
    
    fn add(&mut self, value: i32) {
        self.data.push(value);
    }
    
    fn process(&mut self) {
        let data = self.get_data();  // 불변 빌림
        self.add(data[0]);           // 가변 빌림 - 에러!
    }
}
```

**해결 방법 1: 값 복사**
```rust
impl Builder {
    fn process(&mut self) {
        let first_value = self.data[0];  // 값 복사
        self.add(first_value);
    }
}
```

**해결 방법 2: 스코프 분리**
```rust
impl Builder {
    fn process(&mut self) {
        let first_value = {
            let data = self.get_data();
            data[0]
        };  // 여기서 불변 빌림 종료
        self.add(first_value);
    }
}
```

**해결 방법 3: 메서드 재설계**
```rust
impl Builder {
    fn get_first(&self) -> Option<i32> {
        self.data.first().copied()
    }
    
    fn process(&mut self) {
        if let Some(value) = self.get_first() {
            self.add(value);
        }
    }
}
```

## B. 자주 마주치는 패턴과 해결책 치트시트

이 섹션은 실전에서 반복적으로 나타나는 빌림 문제들과 즉시 적용 가능한 해결책을 정리한다.

### B.1 컬렉션 순회 중 수정

**문제:**
```rust
// 작동하지 않음
let mut vec = vec![1, 2, 3, 4, 5];
for item in &vec {
    if *item % 2 == 0 {
        vec.push(*item * 2);  // 에러!
    }
}
```

**해결책 모음:**

```rust
// 해결책 1: 인덱스 기반 순회
let mut vec = vec![1, 2, 3, 4, 5];
let len = vec.len();
for i in 0..len {
    if vec[i] % 2 == 0 {
        vec.push(vec[i] * 2);
    }
}

// 해결책 2: 변경사항을 임시 벡터에 수집
let mut vec = vec![1, 2, 3, 4, 5];
let to_add: Vec<i32> = vec.iter()
    .filter(|&&x| x % 2 == 0)
    .map(|&x| x * 2)
    .collect();
vec.extend(to_add);

// 해결책 3: drain과 함께 사용
let mut vec = vec![1, 2, 3, 4, 5];
let items: Vec<i32> = vec.drain(..).collect();
for item in items {
    vec.push(item);
    if item % 2 == 0 {
        vec.push(item * 2);
    }
}

// 해결책 4: retain_mut (Rust 1.61+)
let mut vec = vec![1, 2, 3, 4, 5];
let mut additions = Vec::new();
for item in &vec {
    if *item % 2 == 0 {
        additions.push(*item * 2);
    }
}
vec.extend(additions);
```

### B.2 HashMap 동시 접근

**문제:**
```rust
// 작동하지 않음
use std::collections::HashMap;

let mut map = HashMap::new();
map.insert("key1", vec![1, 2, 3]);

let val = map.get("key1").unwrap();  // 불변 빌림
map.insert("key2", vec![4, 5, 6]);   // 가변 빌림 - 에러!
println!("{:?}", val);
```

**해결책 모음:**

```rust
use std::collections::HashMap;

// 해결책 1: 값 복제
let mut map = HashMap::new();
map.insert("key1", vec![1, 2, 3]);
let val = map.get("key1").unwrap().clone();
map.insert("key2", vec![4, 5, 6]);
println!("{:?}", val);

// 해결책 2: 스코프 분리
let mut map = HashMap::new();
map.insert("key1", vec![1, 2, 3]);
{
    let val = map.get("key1").unwrap();
    println!("{:?}", val);
}
map.insert("key2", vec![4, 5, 6]);

// 해결책 3: entry API 사용
let mut map = HashMap::new();
map.insert("key1", vec![1, 2, 3]);
map.entry("key2".to_string())
    .or_insert(vec![4, 5, 6]);

// 해결책 4: get_mut으로 직접 수정
let mut map = HashMap::new();
map.insert("key1", vec![1, 2, 3]);
if let Some(val) = map.get_mut("key1") {
    val.push(4);
}
```

### B.3 구조체 필드 동시 접근

**문제:**
```rust
// 작동하지 않음
struct Game {
    score: i32,
    players: Vec<String>,
}

impl Game {
    fn update(&mut self) {
        let players = &self.players;  // 불변 빌림
        self.score += players.len() as i32;  // 가변 빌림 - 에러!
    }
}
```

**해결책 모음:**

```rust
// 해결책 1: 직접 접근 (분할 빌림)
impl Game {
    fn update(&mut self) {
        self.score += self.players.len() as i32;
    }
}

// 해결책 2: 값 추출
impl Game {
    fn update(&mut self) {
        let player_count = self.players.len();
        self.score += player_count as i32;
    }
}

// 해결책 3: 분리된 메서드
impl Game {
    fn player_count(&self) -> usize {
        self.players.len()
    }
    
    fn update(&mut self) {
        let count = self.player_count();
        self.score += count as i32;
    }
}

// 해결책 4: 튜플 패턴으로 필드 직접 접근
impl Game {
    fn update(&mut self) {
        let Game { score, players } = self;
        *score += players.len() as i32;
    }
}
```

### B.4 메서드 체이닝

**문제:**
```rust
// 작동하지 않음
struct Builder {
    value: i32,
}

impl Builder {
    fn add(&mut self, x: i32) -> &mut Self {
        self.value += x;
        self
    }
    
    fn get_value(&self) -> i32 {
        self.value
    }
    
    fn process(&mut self) {
        let v = self.get_value();  // 불변 빌림
        self.add(v);               // 가변 빌림 - 에러!
    }
}
```

**해결책 모음:**

```rust
// 해결책 1: 값 복사
impl Builder {
    fn process(&mut self) {
        let v = self.value;  // Copy 트레이트 활용
        self.add(v);
    }
}

// 해결책 2: 메서드 재설계
impl Builder {
    fn double(&mut self) -> &mut Self {
        self.value *= 2;
        self
    }
}

// 해결책 3: 불변 메서드 반환
impl Builder {
    fn add(mut self, x: i32) -> Self {
        self.value += x;
        self
    }
}

// 해결책 4: 내부 가변성
use std::cell::Cell;

struct Builder {
    value: Cell<i32>,
}

impl Builder {
    fn add(&self, x: i32) {
        self.value.set(self.value.get() + x);
    }
    
    fn get_value(&self) -> i32 {
        self.value.get()
    }
    
    fn process(&self) {
        let v = self.get_value();
        self.add(v);
    }
}
```

### B.5 옵셔널 필드 처리

**문제:**
```rust
// 작동하지 않음
struct Container {
    data: Option<Vec<i32>>,
}

impl Container {
    fn add_to_data(&mut self, value: i32) {
        if let Some(ref mut vec) = self.data {
            vec.push(value);
        } else {
            self.data = Some(vec![value]);  // 에러! self가 이미 빌려짐
        }
    }
}
```

**해결책 모음:**

```rust
// 해결책 1: match 가드 사용
impl Container {
    fn add_to_data(&mut self, value: i32) {
        match &mut self.data {
            Some(vec) => vec.push(value),
            None => self.data = Some(vec![value]),
        }
    }
}

// 해결책 2: get_or_insert_with
impl Container {
    fn add_to_data(&mut self, value: i32) {
        self.data
            .get_or_insert_with(Vec::new)
            .push(value);
    }
}

// 해결책 3: take()와 replace() 활용
impl Container {
    fn add_to_data(&mut self, value: i32) {
        let mut data = self.data.take().unwrap_or_default();
        data.push(value);
        self.data = Some(data);
    }
}

// 해결책 4: as_mut()
impl Container {
    fn add_to_data(&mut self, value: i32) {
        if let Some(vec) = self.data.as_mut() {
            vec.push(value);
        } else {
            self.data = Some(vec![value]);
        }
    }
}
```

### B.6 중첩된 구조체 수정

**문제:**
```rust
// 작동하지 않음
struct Inner {
    value: i32,
}

struct Outer {
    inner: Inner,
    multiplier: i32,
}

impl Outer {
    fn update(&mut self) {
        let inner = &mut self.inner;  // 가변 빌림
        inner.value *= self.multiplier;  // 다시 self 접근 - 에러!
    }
}
```

**해결책 모음:**

```rust
// 해결책 1: 직접 접근
impl Outer {
    fn update(&mut self) {
        self.inner.value *= self.multiplier;
    }
}

// 해결책 2: 값 추출
impl Outer {
    fn update(&mut self) {
        let multiplier = self.multiplier;
        self.inner.value *= multiplier;
    }
}

// 해결책 3: 구조 분해
impl Outer {
    fn update(&mut self) {
        let Outer { inner, multiplier } = self;
        inner.value *= *multiplier;
    }
}

// 해결책 4: 별도 함수
impl Inner {
    fn multiply(&mut self, factor: i32) {
        self.value *= factor;
    }
}

impl Outer {
    fn update(&mut self) {
        self.inner.multiply(self.multiplier);
    }
}
```

### B.7 클로저와 빌림

**문제:**
```rust
// 작동하지 않음
struct Processor {
    data: Vec<i32>,
}

impl Processor {
    fn process_with<F>(&mut self, f: F) 
    where
        F: Fn(&Vec<i32>),
    {
        f(&self.data);
        self.data.push(0);  // 에러! self가 불변 빌림됨
    }
}
```

**해결책 모음:**

```rust
// 해결책 1: FnOnce 사용
impl Processor {
    fn process_with<F>(&mut self, f: F) 
    where
        F: FnOnce(&Vec<i32>),
    {
        f(&self.data);
        self.data.push(0);
    }
}

// 해결책 2: 순서 변경
impl Processor {
    fn process_with<F>(&mut self, f: F) 
    where
        F: Fn(&Vec<i32>),
    {
        self.data.push(0);
        f(&self.data);
    }
}

// 해결책 3: 데이터 복제 전달
impl Processor {
    fn process_with<F>(&mut self, f: F) 
    where
        F: Fn(Vec<i32>),
    {
        f(self.data.clone());
        self.data.push(0);
    }
}

// 해결책 4: 스코프 분리
impl Processor {
    fn process_with<F>(&mut self, f: F) 
    where
        F: Fn(&Vec<i32>),
    {
        {
            f(&self.data);
        }
        self.data.push(0);
    }
}
```

### B.8 반복자와 소유권

**문제:**
```rust
// 작동하지 않음
let vec = vec![1, 2, 3, 4, 5];
let iter = vec.iter();
let sum: i32 = iter.sum();
let product: i32 = iter.product();  // 에러! iter가 이미 소비됨
```

**해결책 모음:**

```rust
// 해결책 1: 여러 반복자 생성
let vec = vec![1, 2, 3, 4, 5];
let sum: i32 = vec.iter().sum();
let product: i32 = vec.iter().product();

// 해결책 2: clone 활용
let vec = vec![1, 2, 3, 4, 5];
let iter = vec.iter().cloned();
let sum: i32 = iter.clone().sum();
let product: i32 = iter.product();

// 해결책 3: collect 후 재사용
let vec = vec![1, 2, 3, 4, 5];
let collected: Vec<_> = vec.iter().cloned().collect();
let sum: i32 = collected.iter().sum();
let product: i32 = collected.iter().product();

// 해결책 4: 필요한 값만 먼저 계산
let vec = vec![1, 2, 3, 4, 5];
let sum: i32 = vec.iter().sum();
let product: i32 = vec.iter().product();
```

## C. C++ 디자인 패턴의 Rust 대응표

C++ 개발자가 Rust로 전환할 때 가장 혼란스러운 부분은 익숙한 디자인 패턴을 어떻게 구현해야 하는지다. 이 섹션은 주요 C++ 패턴과 Rust의 관용적 대응 방법을 비교한다.

### C.1 싱글톤 패턴

**C++ 방식:**
```cpp
class Singleton {
private:
    static Singleton* instance;
    Singleton() {}
    
public:
    static Singleton* getInstance() {
        if (instance == nullptr) {
            instance = new Singleton();
        }
        return instance;
    }
};
```

**Rust 대응:**

```rust
// 방법 1: lazy_static 사용
use lazy_static::lazy_static;
use std::sync::Mutex;

struct Config {
    value: i32,
}

lazy_static! {
    static ref CONFIG: Mutex<Config> = Mutex::new(Config { value: 0 });
}

fn use_config() {
    let mut config = CONFIG.lock().unwrap();
    config.value = 42;
}

// 방법 2: once_cell (Rust 1.70+ 표준 라이브러리)
use std::sync::OnceLock;

static CONFIG: OnceLock<Config> = OnceLock::new();

fn get_config() -> &'static Config {
    CONFIG.get_or_init(|| Config { value: 0 })
}

// 방법 3: 스레드 안전성이 필요 없는 경우
use std::cell::RefCell;

thread_local! {
    static CONFIG: RefCell<Config> = RefCell::new(Config { value: 0 });
}

fn use_config() {
    CONFIG.with(|config| {
        config.borrow_mut().value = 42;
    });
}
```

**핵심 차이점:**
- Rust는 전역 가변 상태를 직접 허용하지 않는다
- `Mutex`, `RwLock`으로 스레드 안전성을 명시적으로 관리한다
- `lazy_static`이나 `OnceLock`으로 지연 초기화를 구현한다

### C.2 팩토리 패턴

**C++ 방식:**
```cpp
class Shape {
public:
    virtual void draw() = 0;
    virtual ~Shape() {}
};

class Circle : public Shape {
public:
    void draw() override { /* ... */ }
};

class Square : public Shape {
public:
    void draw() override { /* ... */ }
};

class ShapeFactory {
public:
    static Shape* createShape(const std::string& type) {
        if (type == "circle") return new Circle();
        if (type == "square") return new Square();
        return nullptr;
    }
};
```

**Rust 대응:**

```rust
// 방법 1: Enum과 매칭
#[derive(Debug)]
enum Shape {
    Circle { radius: f64 },
    Square { side: f64 },
}

impl Shape {
    fn create(shape_type: &str, size: f64) -> Option<Self> {
        match shape_type {
            "circle" => Some(Shape::Circle { radius: size }),
            "square" => Some(Shape::Square { side: size }),
            _ => None,
        }
    }
    
    fn draw(&self) {
        match self {
            Shape::Circle { radius } => {
                println!("Drawing circle with radius {}", radius);
            }
            Shape::Square { side } => {
                println!("Drawing square with side {}", side);
            }
        }
    }
}

// 방법 2: Trait 객체 사용
trait Shape {
    fn draw(&self);
}

struct Circle {
    radius: f64,
}

impl Shape for Circle {
    fn draw(&self) {
        println!("Drawing circle with radius {}", self.radius);
    }
}

struct Square {
    side: f64,
}

impl Shape for Square {
    fn draw(&self) {
        println!("Drawing square with side {}", self.side);
    }
}

fn create_shape(shape_type: &str, size: f64) -> Option<Box<dyn Shape>> {
    match shape_type {
        "circle" => Some(Box::new(Circle { radius: size })),
        "square" => Some(Box::new(Square { side: size })),
        _ => None,
    }
}

// 방법 3: 제네릭과 타입 제약
trait ShapeFactory {
    type Output: Shape;
    fn create(size: f64) -> Self::Output;
}

struct CircleFactory;
impl ShapeFactory for CircleFactory {
    type Output = Circle;
    fn create(size: f64) -> Self::Output {
        Circle { radius: size }
    }
}
```

**핵심 차이점:**
- Rust는 enum을 사용한 대수적 데이터 타입이 강력하다
- 트레이트 객체(`Box<dyn Trait>`)는 힙 할당을 수반한다
- 제네릭을 사용하면 컴파일 타임에 타입이 결정되어 더 효율적이다

### C.3 옵저버 패턴

**C++ 방식:**
```cpp
class Observer {
public:
    virtual void update(int value) = 0;
    virtual ~Observer() {}
};

class Subject {
private:
    std::vector<Observer*> observers;
    int state;
    
public:
    void attach(Observer* obs) {
        observers.push_back(obs);
    }
    
    void setState(int value) {
        state = value;
        notify();
    }
    
    void notify() {
        for (auto obs : observers) {
            obs->update(state);
        }
    }
};
```

**Rust 대응:**

```rust
// 방법 1: 클로저를 사용한 간단한 방식
type Callback = Box<dyn Fn(i32)>;

struct Subject {
    observers: Vec<Callback>,
    state: i32,
}

impl Subject {
    fn new() -> Self {
        Subject {
            observers: Vec::new(),
            state: 0,
        }
    }
    
    fn attach<F>(&mut self, observer: F)
    where
        F: Fn(i32) + 'static,
    {
        self.observers.push(Box::new(observer));
    }
    
    fn set_state(&mut self, value: i32) {
        self.state = value;
        self.notify();
    }
    
    fn notify(&self) {
        for observer in &self.observers {
            observer(self.state);
        }
    }
}

// 사용 예
fn main() {
    let mut subject = Subject::new();
    
    subject.attach(|value| {
        println!("Observer 1 received: {}", value);
    });
    
    subject.attach(|value| {
        println!("Observer 2 received: {}", value);
    });
    
    subject.set_state(42);
}

// 방법 2: 채널을 사용한 방식
use std::sync::mpsc::{channel, Sender};

struct Subject {
    senders: Vec<Sender<i32>>,
    state: i32,
}

impl Subject {
    fn new() -> Self {
        Subject {
            senders: Vec::new(),
            state: 0,
        }
    }
    
    fn subscribe(&mut self) -> std::sync::mpsc::Receiver<i32> {
        let (tx, rx) = channel();
        self.senders.push(tx);
        rx
    }
    
    fn set_state(&mut self, value: i32) {
        self.state = value;
        self.senders.retain(|sender| sender.send(value).is_ok());
    }
}

// 방법 3: Weak 참조를 활용한 옵저버
use std::rc::{Rc, Weak};
use std::cell::RefCell;

trait Observer {
    fn update(&self, value: i32);
}

struct Subject {
    observers: Vec<Weak<dyn Observer>>,
    state: i32,
}

impl Subject {
    fn new() -> Self {
        Subject {
            observers: Vec::new(),
            state: 0,
        }
    }
    
    fn attach(&mut self, observer: Weak<dyn Observer>) {
        self.observers.push(observer);
    }
    
    fn set_state(&mut self, value: i32) {
        self.state = value;
        self.notify();
    }
    
    fn notify(&mut self) {
        self.observers.retain(|weak_obs| {
            if let Some(obs) = weak_obs.upgrade() {
                obs.update(self.state);
                true
            } else {
                false  // 관찰자가 삭제되면 목록에서 제거
            }
        });
    }
}
```

**핵심 차이점:**
- Rust는 순환 참조를 방지하기 위해 `Weak` 참조를 활용한다
- 클로저나 채널을 사용하면 더 간결하게 구현할 수 있다
- 옵저버의 생명주기 관리가 명시적이어야 한다

### C.4 전략 패턴

**C++ 방식:**
```cpp
class Strategy {
public:
    virtual int execute(int a, int b) = 0;
    virtual ~Strategy() {}
};

class AddStrategy : public Strategy {
public:
    int execute(int a, int b) override {
        return a + b;
    }
};

class MultiplyStrategy : public Strategy {
public:
    int execute(int a, int b) override {
        return a * b;
    }
};

class Context {
private:
    Strategy* strategy;
    
public:
    void setStrategy(Strategy* s) {
        strategy = s;
    }
    
    int executeStrategy(int a, int b) {
        return strategy->execute(a, b);
    }
};
```

**Rust 대응:**

```rust
// 방법 1: 함수 포인터/클로저
struct Context {
    strategy: Box<dyn Fn(i32, i32) -> i32>,
}

impl Context {
    fn new<F>(strategy: F) -> Self
    where
        F: Fn(i32, i32) -> i32 + 'static,
    {
        Context {
            strategy: Box::new(strategy),
        }
    }
    
    fn execute(&self, a: i32, b: i32) -> i32 {
        (self.strategy)(a, b)
    }
    
    fn set_strategy<F>(&mut self, strategy: F)
    where
        F: Fn(i32, i32) -> i32 + 'static,
    {
        self.strategy = Box::new(strategy);
    }
}

// 사용
fn main() {
    let mut ctx = Context::new(|a, b| a + b);
    println!("Result: {}", ctx.execute(5, 3));  // 8
    
    ctx.set_strategy(|a, b| a * b);
    println!("Result: {}", ctx.execute(5, 3));  // 15
}

// 방법 2: Trait 사용
trait Strategy {
    fn execute(&self, a: i32, b: i32) -> i32;
}

struct AddStrategy;
impl Strategy for AddStrategy {
    fn execute(&self, a: i32, b: i32) -> i32 {
        a + b
    }
}

struct MultiplyStrategy;
impl Strategy for MultiplyStrategy {
    fn execute(&self, a: i32, b: i32) -> i32 {
        a * b
    }
}

struct Context {
    strategy: Box<dyn Strategy>,
}

impl Context {
    fn new(strategy: Box<dyn Strategy>) -> Self {
        Context { strategy }
    }
    
    fn execute(&self, a: i32, b: i32) -> i32 {
        self.strategy.execute(a, b)
    }
}

// 방법 3: Enum 사용 (가장 Rust다운 방식)
enum Strategy {
    Add,
    Multiply,
    Custom(Box<dyn Fn(i32, i32) -> i32>),
}

impl Strategy {
    fn execute(&self, a: i32, b: i32) -> i32 {
        match self {
            Strategy::Add => a + b,
            Strategy::Multiply => a * b,
            Strategy::Custom(f) => f(a, b),
        }
    }
}

struct Context {
    strategy: Strategy,
}
```

**핵심 차이점:**
- Rust의 enum과 패턴 매칭이 더 명확한 경우가 많다
- 클로저를 직접 저장하면 가장 유연하다
- 제네릭을 사용하면 런타임 오버헤드를 제거할 수 있다

### C.5 빌더 패턴

**C++ 방식:**
```cpp
class Product {
private:
    std::string partA;
    std::string partB;
    
public:
    void setPartA(const std::string& part) { partA = part; }
    void setPartB(const std::string& part) { partB = part; }
};

class Builder {
protected:
    Product* product;
    
public:
    virtual void buildPartA() = 0;
    virtual void buildPartB() = 0;
    Product* getResult() { return product; }
};
```

**Rust 대응:**

```rust
// 방법 1: 소비하는 빌더 (가장 일반적)
#[derive(Default)]
struct Product {
    part_a: String,
    part_b: String,
    part_c: Option<i32>,
}

struct ProductBuilder {
    part_a: String,
    part_b: String,
    part_c: Option<i32>,
}

impl ProductBuilder {
    fn new() -> Self {
        ProductBuilder {
            part_a: String::new(),
            part_b: String::new(),
            part_c: None,
        }
    }
    
    fn part_a(mut self, value: String) -> Self {
        self.part_a = value;
        self
    }
    
    fn part_b(mut self, value: String) -> Self {
        self.part_b = value;
        self
    }
    
    fn part_c(mut self, value: i32) -> Self {
        self.part_c = Some(value);
        self
    }
    
    fn build(self) -> Product {
        Product {
            part_a: self.part_a,
            part_b: self.part_b,
            part_c: self.part_c,
        }
    }
}

// 사용
fn main() {
    let product = ProductBuilder::new()
        .part_a("A".to_string())
        .part_b("B".to_string())
        .part_c(42)
        .build();
}

// 방법 2: 참조를 사용하는 빌더
impl ProductBuilder {
    fn part_a(&mut self, value: String) -> &mut Self {
        self.part_a = value;
        self
    }
    
    fn part_b(&mut self, value: String) -> &mut Self {
        self.part_b = value;
        self
    }
    
    fn build(&self) -> Product {
        Product {
            part_a: self.part_a.clone(),
            part_b: self.part_b.clone(),
            part_c: self.part_c,
        }
    }
}

// 방법 3: derive_builder 크레이트 사용
use derive_builder::Builder;

#[derive(Builder, Debug)]
struct Product {
    part_a: String,
    part_b: String,
    #[builder(default)]
    part_c: Option<i32>,
}

// 자동 생성됨
fn main() {
    let product = ProductBuilder::default()
        .part_a("A".to_string())
        .part_b("B".to_string())
        .build()
        .unwrap();
}
```

**핵심 차이점:**
- Rust는 self를 소비하는 방식이 일반적이다 (메서드 체이닝)
- 옵셔널 필드는 `Option<T>`로 명시적으로 표현한다
- `derive_builder` 매크로로 보일러플레이트를 줄일 수 있다

### C.6 상태 패턴

**C++ 방식:**
```cpp
class State {
public:
    virtual void handle(Context* context) = 0;
    virtual ~State() {}
};

class ConcreteStateA : public State {
public:
    void handle(Context* context) override;
};

class Context {
private:
    State* state;
    
public:
    void setState(State* s) { state = s; }
    void request() { state->handle(this); }
};
```

**Rust 대응:**

```rust
// 방법 1: Enum으로 상태 표현 (타입 안전성 최고)
enum State {
    Initial,
    Processing { progress: u32 },
    Complete { result: String },
}

struct Context {
    state: State,
}

impl Context {
    fn new() -> Self {
        Context {
            state: State::Initial,
        }
    }
    
    fn start_processing(&mut self) {
        match self.state {
            State::Initial => {
                self.state = State::Processing { progress: 0 };
            }
            _ => println!("Cannot start processing from current state"),
        }
    }
    
    fn update_progress(&mut self, amount: u32) {
        match &mut self.state {
            State::Processing { progress } => {
                *progress += amount;
                if *progress >= 100 {
                    self.state = State::Complete {
                        result: "Done".to_string(),
                    };
                }
            }
            _ => println!("Not in processing state"),
        }
    }
}

// 방법 2: 타입 시스템을 활용한 상태 패턴
struct Initial;
struct Processing;
struct Complete;

struct Context<S> {
    state: S,
}

impl Context<Initial> {
    fn new() -> Self {
        Context { state: Initial }
    }
    
    fn start(self) -> Context<Processing> {
        Context { state: Processing }
    }
}

impl Context<Processing> {
    fn process(self) -> Context<Complete> {
        // 처리 로직
        Context { state: Complete }
    }
}

impl Context<Complete> {
    fn result(&self) -> String {
        "Done".to_string()
    }
}

// 컴파일 타임에 상태 전환 검증
fn main() {
    let ctx = Context::new();
    let ctx = ctx.start();
    let ctx = ctx.process();
    let result = ctx.result();
    
    // ctx.start(); // 컴파일 에러! Complete 상태에서는 start 불가
}

// 방법 3: Trait 객체 사용 (C++와 유사)
trait State {
    fn handle(self: Box<Self>) -> Box<dyn State>;
}

struct StateA;
struct StateB;

impl State for StateA {
    fn handle(self: Box<Self>) -> Box<dyn State> {
        println!("Handling in State A");
        Box::new(StateB)
    }
}

impl State for StateB {
    fn handle(self: Box<Self>) -> Box<dyn State> {
        println!("Handling in State B");
        Box::new(StateA)
    }
}

struct Context {
    state: Box<dyn State>,
}

impl Context {
    fn request(&mut self) {
        let state = std::mem::replace(
            &mut self.state,
            Box::new(StateA)  // 임시 값
        );
        self.state = state.handle();
    }
}
```

**핵심 차이점:**
- Rust의 enum은 상태와 데이터를 함께 표현하기 최적이다
- 타입 시스템을 활용하면 불가능한 상태 전환을 컴파일 타임에 방지한다
- Trait 객체는 C++ 스타일과 유사하지만 소유권 때문에 복잡하다

### C.7 데코레이터 패턴

**C++ 방식:**
```cpp
class Component {
public:
    virtual std::string operation() = 0;
    virtual ~Component() {}
};

class ConcreteComponent : public Component {
public:
    std::string operation() override {
        return "ConcreteComponent";
    }
};

class Decorator : public Component {
protected:
    Component* component;
    
public:
    Decorator(Component* comp) : component(comp) {}
    std::string operation() override {
        return component->operation();
    }
};
```

**Rust 대응:**

```rust
// 방법 1: 함수형 조합
type Operation = Box<dyn Fn() -> String>;

fn base_operation() -> String {
    "Base".to_string()
}

fn decorator_a<F>(f: F) -> impl Fn() -> String
where
    F: Fn() -> String + 'static,
{
    move || format!("A({})", f())
}

fn decorator_b<F>(f: F) -> impl Fn() -> String
where
    F: Fn() -> String + 'static,
{
    move || format!("B({})", f())
}

// 사용
fn main() {
    let op = decorator_a(|| decorator_b(base_operation)());
    println!("{}", op());  // "A(B(Base))"
}

// 방법 2: Trait과 제네릭
trait Component {
    fn operation(&self) -> String;
}

struct ConcreteComponent;

impl Component for ConcreteComponent {
    fn operation(&self) -> String {
        "ConcreteComponent".to_string()
    }
}

struct DecoratorA<C: Component> {
    component: C,
}

impl<C: Component> Component for DecoratorA<C> {
    fn operation(&self) -> String {
        format!("A({})", self.component.operation())
    }
}

struct DecoratorB<C: Component> {
    component: C,
}

impl<C: Component> Component for DecoratorB<C> {
    fn operation(&self) -> String {
        format!("B({})", self.component.operation())
    }
}

// 사용
fn main() {
    let component = ConcreteComponent;
    let decorated = DecoratorA {
        component: DecoratorB { component },
    };
    println!("{}", decorated.operation());  // "A(B(ConcreteComponent))"
}

// 방법 3: 박스를 사용한 동적 조합
struct DynamicComponent {
    inner: Box<dyn Component>,
}

impl Component for DynamicComponent {
    fn operation(&self) -> String {
        format!("Dynamic({})", self.inner.operation())
    }
}

fn add_decorator(component: Box<dyn Component>) -> Box<dyn Component> {
    Box::new(DynamicComponent { inner: component })
}
```

**핵심 차이점:**
- Rust는 제네릭을 사용해 컴파일 타임에 데코레이터 체인을 구성할 수 있다
- 함수형 접근이 더 자연스러운 경우가 많다
- 동적 디스패치가 필요하면 `Box<dyn Trait>`을 사용한다

### C.8 커맨드 패턴

**C++ 방식:**
```cpp
class Command {
public:
    virtual void execute() = 0;
    virtual ~Command() {}
};

class ConcreteCommand : public Command {
private:
    Receiver* receiver;
    
public:
    ConcreteCommand(Receiver* r) : receiver(r) {}
    void execute() override {
        receiver->action();
    }
};

class Invoker {
private:
    std::vector<Command*> commands;
    
public:
    void addCommand(Command* cmd) {
        commands.push_back(cmd);
    }
    
    void executeAll() {
        for (auto cmd : commands) {
            cmd->execute();
        }
    }
};
```

**Rust 대응:**

```rust
// 방법 1: 클로저 사용 (가장 간단)
struct Invoker {
    commands: Vec<Box<dyn Fn()>>,
}

impl Invoker {
    fn new() -> Self {
        Invoker {
            commands: Vec::new(),
        }
    }
    
    fn add_command<F>(&mut self, cmd: F)
    where
        F: Fn() + 'static,
    {
        self.commands.push(Box::new(cmd));
    }
    
    fn execute_all(&self) {
        for cmd in &self.commands {
            cmd();
        }
    }
}

// 사용
fn main() {
    let mut invoker = Invoker::new();
    
    let value = 42;
    invoker.add_command(move || println!("Command 1: {}", value));
    invoker.add_command(|| println!("Command 2"));
    
    invoker.execute_all();
}

// 방법 2: Trait 사용 (실행 취소 지원)
trait Command {
    fn execute(&mut self);
    fn undo(&mut self);
}

struct AddCommand {
    receiver: i32,
    value: i32,
}

impl Command for AddCommand {
    fn execute(&mut self) {
        self.receiver += self.value;
        println!("Added {}, result: {}", self.value, self.receiver);
    }
    
    fn undo(&mut self) {
        self.receiver -= self.value;
        println!("Undid add, result: {}", self.receiver);
    }
}

struct CommandHistory {
    commands: Vec<Box<dyn Command>>,
}

impl CommandHistory {
    fn execute(&mut self, mut cmd: Box<dyn Command>) {
        cmd.execute();
        self.commands.push(cmd);
    }
    
    fn undo(&mut self) {
        if let Some(mut cmd) = self.commands.pop() {
            cmd.undo();
        }
    }
}

// 방법 3: Enum으로 명시적 커맨드
enum Command {
    Add(i32),
    Multiply(i32),
    Reset,
}

struct Calculator {
    value: i32,
    history: Vec<Command>,
}

impl Calculator {
    fn execute(&mut self, cmd: Command) {
        match cmd {
            Command::Add(n) => self.value += n,
            Command::Multiply(n) => self.value *= n,
            Command::Reset => self.value = 0,
        }
        self.history.push(cmd);
    }
    
    fn undo(&mut self) {
        if let Some(cmd) = self.history.pop() {
            match cmd {
                Command::Add(n) => self.value -= n,
                Command::Multiply(n) => self.value /= n,
                Command::Reset => { /* 이전 값 복원 로직 */ }
            }
        }
    }
}
```

**핵심 차이점:**
- 클로저로 간단한 커맨드를 우아하게 표현할 수 있다
- Enum은 제한된 커맨드 집합에 타입 안전성을 제공한다
- 실행 취소 기능이 필요하면 Trait이 적합하다

이 부록을 통해 C++ 프로그래머가 Rust의 빌림 검사기 에러를 빠르게 해결하고, 익숙한 디자인 패턴을 Rust의 관용적 방식으로 구현할 수 있다. Rust의 소유권 시스템은 처음엔 제약으로 느껴지지만, 이를 활용하면 더 안전하고 명확한 코드를 작성할 수 있다.  