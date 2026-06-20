# Arc
  
## `Arc`의 내부 구조
`Arc<T>`는 다음과 같은 힙 구조를 가진다:

```
Arc<T>
 ├── ptr ────────────────┐
 │                       ▼
 │            ┌──────────────────┐
 │            │ ArcInner<T>      │
 │            │  strong: AtomicUsize  ← strong refcount
 │            │  weak: AtomicUsize    ← weak refcount
 │            │  data: T              ← 실제 값
 │            └──────────────────┘
```

### `Arc<T>`가 clone될 때
`Arc::clone(&arc)` 또는 `arc.clone()`을 호출하면:

1. `strong` 카운터 = 1 증가
2. 스택에 Arc 구조체가 새로 복사됨
   (포인터 + 약간의 메타데이터)
3. `T`는 그대로 공유됨 (복사 없음)

즉, **Arc 핸들만 늘어나고, T 데이터는 하나**다.


## `Rc`와 `Arc`의 차이점

| 항목        | Rc            | Arc                   |
| --------- | ------------- | --------------------- |
| 스레드 안전성   | ❌ 없음          | ✅ 있음                  |
| 참조 카운트 타입 | `Cell<usize>` | `AtomicUsize`         |
| Clone 비용  | 매우 저렴         | atomic 연산 때문에 약간 더 비쌈 |
| 사용 상황     | 단일 스레드        | 멀티 스레드 공유             |

### 왜 Arc는 atomic인가?
멀티스레드 환경에서 clone 시 참조 카운트를 증가/감소해야 하는데,
스레드 간 동기화가 필요하기 때문에 **원자적 연산(atomic operations)**을 사용한다.

즉,

```
Arc는 멀티스레드 안전한 Rc이다.
```


## Arc와 Rc 모두 "데이터는 복사되지 않는다"
둘 다 `clone()` 시 다음이 공통이다:

* `T` 데이터는 복사되지 않음
* 스마트 포인터(Rc/Arc)만 새로 생성됨
* 참조 카운트 증가함

그래서 아래 코드는 T를 복사하지 않고, 동일한 T를 바라본다:

```rust
let x = Arc::new(vec![1,2,3]);
let y = Arc::clone(&x);
```

## 메모리 해제는 어떻게 되는가?

* `Arc/Rc` 값 하나가 drop되면 `strong` 카운트가 1 감소
* strong 카운트가 0이 되는 순간 `T`가 해제됨
* weak 포인터가 남아 있는 동안 내부 구조는 약간 남아 있음
* weak 카운트도 0이 되어야 메모리 전체 해제됨

  
## Arc가 복사되는 오해와 진실

❌ 잘못된 개념
"Arc::clone 은 참조(주소) 자체를 복사하는 것이다"

⬇️

⭕ 정확한 개념
"Arc::clone 은 **Arc 스마트 포인터 구조체를 복사하고**, 내부 원자적 참조 카운트를 증가시키는 것이다.
실제 데이터 T는 절대 복사되지 않는다."

   
---

## Arc / Rc의 drop 시 동작

### 기본 구조 복습
개념적으로 `Arc<T>` / `Rc<T>`는 이런 힙 구조를 가진다고 보면 된다.

```text
Arc<T> (스택)
 └─ ptr ─────────────┐
                     ▼
            ArcInner<T> (힙)
             - strong: (Atomic)usize  // strong 카운트
             - weak:   (Atomic)usize  // weak 카운트
             - data:   T             // 실제 데이터
```

`Rc<T>`도 거의 동일한 구조인데, strong/weak이 atomic이 아니고, 싱글 스레드 전용이라는 점만 다르다.

### strong 포인터 drop

```rust
{
    let a = Arc::new(10);
    let b = Arc::clone(&a);
} // a, b 둘 다 여기서 drop
```

각각의 drop에서 일어나는 일은 다음과 같다.

1. `Arc` 값이 스코프 밖으로 나가면 `Drop` 구현이 호출된다.
2. 내부 strong 카운트를 1 감소시킨다.
3. 감소 후 strong 카운트가 0이면:

   * `T`(data)를 drop한다.
   * 그리고 weak 카운트를 1 감소한다(Arc 하나마다 implicit weak 1개를 들고 있다고 보면 된다).
4. weak 카운트까지 0이 되면:

   * `ArcInner<T>` 블록 자체(메타데이터 + data 영역)를 `dealloc`해서 완전히 해제한다.

중요한 점은:

* **strong == 0이 되는 시점에 data(T)가 drop**된다는 것
* **weak == 0까지 되어야 메모리 블록 자체가 해제**된다는 것

### weak 포인터 drop
`Weak<T>`가 drop될 때는:

1. weak 카운트를 1 감소
2. 감소 후 weak == 0이고 strong == 0이면, 이제서야 `ArcInner` 전체를 해제

즉, `Arc`가 단 하나도 남지 않고, `Weak`도 없는 시점에 메모리 전체를 정리한다.

### 순환 참조(사이클) 관련
`Rc<T>`를 사용할 때, `Rc<T>`끼리 서로를 strong으로 가리키면 strong 카운트가 절대 0이 되지 않아 메모리 누수가 난다. 그래서 일반적으로:

* 소유 관계(부모 → 자식)는 `Rc`
* 역참조(자식 → 부모)는 `Weak`

으로 설계해서, 부모가 drop되면 자식 쪽 weak가 무효가 되도록 만들고, 사이클을 끊는다.

`Arc`도 마찬가지로 구조는 같고, 주로 멀티 스레드 환경에서 사용될 뿐이다.


## `Arc::get_mut` / `Rc::get_mut` 동작 방식

### 요구 조건
`Arc::get_mut(&mut Arc<T>) -> Option<&mut T>`는 **"현재 이 Arc가 유일한 strong 소유자이며 weak도 없어야 한다"**일 때만 `&mut T`를 돌려준다.

대략 조건은 다음과 같다고 보면 된다.

* strong 카운트 == 1
* weak 카운트 == 1 (implicit weak 하나만 존재: ArcInner 내부 구조용)

그래야 “이 데이터(T)는 전역적으로 이 호출자만 수정한다”는 것이 보장된다.

`Rc::get_mut`도 동일한 개념인데, 싱글 스레드이므로 atomic이 아니고, 내부 카운트를 그냥 읽어 비교한다.

### 왜 약한 참조(Weak)도 없어야 하는가?
`&mut T`는 Rust에서 **전역적으로 딱 하나**만 존재해야 하는 “배타적 접근 권한”을 의미한다.

* 만약 Weak가 남아있으면, 나중에 `upgrade()`를 통해 새로운 `Arc` / `Rc` strong 포인터가 생길 수 있다.
* 그러면 `&mut T`가 유효한 동안, 다른 곳에서 `&T`나 또 다른 `&mut T`가 생길 잠재성이 생긴다.
* 이는 Rust의 aliasing 규칙에 위배된다.

그래서 Weak도 없어야 안전하다.

### 구현 아이디어(단순화해서 표현)
대략 이런 느낌이라고 보면 된다 (실제 표준 라이브러리 코드는 더 복잡하지만):

```rust
impl<T> Arc<T> {
    pub fn get_mut(this: &mut Arc<T>) -> Option<&mut T> {
        if Arc::strong_count(this) == 1 && Arc::weak_count(this) == 1 {
            // 이제 유일 소유자이므로, 내부의 T에 대한 &mut T를 만들어도 된다
            unsafe {
                let inner = this.ptr.as_ptr();
                Some(&mut (*inner).data)
            }
        } else {
            None
        }
    }
}
```

핵심은:

* strong/weak 카운트를 확인해서 “유일한 소유자”인지 검사
* 맞으면 unsafe 포인터 연산으로 `&mut T`를 꺼낸다

---

## `Arc::make_mut` / `Rc::make_mut` (Copy-on-Write)
`make_mut`는 “copy-on-write(COW)” 스타일로 동작한다.

```rust
let mut a = Arc::new(vec![1, 2, 3]);
let mut b = Arc::clone(&a);

// 여기서 a, b 둘 다 같은 vec을 공유
let x = Arc::make_mut(&mut a);
// 이제 a는 독립적인 vec을 갖고 있을 수 있다
x.push(4);
```

### 동작 로직(개념)
`Arc::make_mut(&mut Arc<T>) -> &mut T`는 다음과 같이 동작한다고 보면 된다.

1. 먼저 `Arc::get_mut(&mut Arc<T>)`를 호출해서

   * 이 Arc가 **유일 strong 소유자이고 weak도 없으면**
   * 그대로 `&mut T`를 반환

2. 만약 유일 소유자가 아니라면:

   * 기존 데이터를 `T: Clone`을 이용해 깊은 복사한다.
   * 새로운 `Arc::new(cloned_T)`를 만들어서, 기존 `Arc` 대신 이 새 Arc를 `&mut Arc`에 집어넣는다.
   * 새로 만든 Arc는 strong=1, weak=1이므로, 내부 data에 대해 안전하게 `&mut T`를 만들어 반환한다.

즉, 의사코드로 표현하면:

```rust
pub fn make_mut(this: &mut Arc<T>) -> &mut T 
where
    T: Clone,
{
    if let Some(r) = Arc::get_mut(this) {
        return r; // 이미 유일 소유자
    }

    // 공유 중이므로 복사해서 새 Arc를 만든다
    let cloned = T::clone(&**this); // (*this) : Arc<T> -> &T
    *this = Arc::new(cloned);
    Arc::get_mut(this).unwrap()
}
```

`Rc::make_mut`도 거의 똑같이 동작한다.

### 언제 유용한가

* 읽기 위주로 공유하다가, **어쩌다가 한 군데만 데이터를 수정해야 할 때**
* 멀티 스레드 환경에서 공유 구조체에 copy-on-write를 적용하고 싶을 때
* “많이 읽고, 가끔 쓴다” 패턴에서 성능 좋게 쓰기 위해

---

## Arc / Rc의 alignment와 메모리 레이아웃

### ArcInner<T> 레이아웃
간단화하면 대략 이런 구조가 된다고 보면 된다.

```rust
#[repr(C)]
struct ArcInner<T> {
    strong: AtomicUsize, // 또는 Cell<usize> for Rc
    weak: AtomicUsize,
    data: T,
}
```

`#[repr(C)]`와 비슷한 효과를 내도록 배치한다고 생각하면:

1. strong, weak 카운트가 먼저 온다.
2. 그 뒤에 `T`가 위치한다.

### alignment 규칙

* `ArcInner<T>`의 alignment는 보통 `max(align_of::<AtomicUsize>(), align_of::<T>())`와 같다.
* 실제로는 T가 struct라면, 그 struct의 alignment까지 고려된다.
* `Arc<T>`가 가리키는 포인터는 이 `ArcInner<T>`의 시작 주소를 가리킨다.

즉, 실제 힙 메모리 배치는:

```text
[ strong | weak | data(T) ... ]
^
|
Arc<T> 내부 포인터
```

이렇게 되어 있고, `data`는 내부에서 `offset`을 통해 접근한다.

### 왜 이런 레이아웃을 쓰는가

* 참조 카운터와 데이터가 같은 allocation에 들어가면 **캐시 지역성**이 좋다.
* allocation 1번이면 되므로, small object를 반복해서 할당할 때 오버헤드가 줄어든다.
* weak도 같은 블록에 있어서 메모리 해제 시 한 번에 정리하기 좋다.

---

## Arc/Rc 내부 구현(표준 라이브러리 관점에서 개략 분석)
실제 `std` 내부 코드를 그대로 보여줄 수는 없어서, 구조를 단순화해서 설명하겠다.

### 5-1. Arc의 주요 필드

대충 이런 느낌이다.

```rust
pub struct Arc<T> {
    ptr: NonNull<ArcInner<T>>,
    // PhantomData<T> 등등
}
```

`NonNull`을 쓰는 이유:

* `Option<Arc<T>>`를 최적화해서, `None`을 null pointer로 표현 가능
* NonNull은 null 일 수 없다는 정보를 컴파일러에 줘서 최적화 가능

### clone 구현 개념

```rust
impl<T> Clone for Arc<T> {
    fn clone(&self) -> Self {
        // strong 카운트 증가
        increment_strong(self.ptr);

        Arc { ptr: self.ptr }
    }
}
```

`increment_strong`은 내부에서 `fetch_add(1, Ordering::Relaxed or Acquire/Release)` 같은 atomic 연산을 수행한다.

### drop 구현 개념

```rust
impl<T> Drop for Arc<T> {
    fn drop(&mut self) {
        if decrement_strong(self.ptr) == 0 {
            // strong == 0 => data drop
            unsafe {
                ptr::drop_in_place(&mut (*self.ptr.as_ptr()).data);
            }

            // data drop 후 weak 카운트 줄이기
            if decrement_weak(self.ptr) == 0 {
                // weak == 0 => 전체 allocation 해제
                unsafe {
                    deallocate(self.ptr);
                }
            }
        }
    }
}
```

여기서도 `decrement_strong` / `decrement_weak`은 atomic `fetch_sub`를 사용한다.

### Rc는 어떤 차이가 있는가
`Rc<T>`는 논리는 거의 같은데, 싱글 스레드 전용이라서:

* 카운터 타입이 `Cell<usize>` 또는 `usize` 기반의 non-atomic 구조
* 참조 카운트 증가/감소 시 그냥 읽고 쓴다 (atomic 연산 없음)
* 따라서 멀티 스레드에서 동시에 `Rc`를 조작하면 UB이다.

반대로, Arc는:

* `AtomicUsize`
* 멀티 스레드에서 안전하게 참조 카운트 조작 가능
* 대신 atomic 연산 비용이 있어서, 싱글 스레드만 쓴다면 Rc가 더 싸다.

   

## Fat Pointer와 Arc의 관계

### Fat pointer란 무엇인가
Rust에서 포인터는 크게 두 종류가 있다.

* **Thin pointer**: 그냥 주소 하나만 있는 포인터

  * 예: `&T`, `*const T` (T가 Sized일 때)
* **Fat pointer**: 주소 + 부가 정보가 있는 포인터

  * 예:

    * `&[T]` → (data 주소, len)
    * `&str` → (data 주소, len)
    * `&dyn Trait` → (data 주소, vtable 주소)

`Arc<T>`도 내부적으로는 “T에 대한 포인터”를 들고 있으므로, T가 unsized이면 이 포인터도 fat pointer가 된다.

### `Arc<dyn Trait>` / `Arc<[T]>`는 어떻게 생겼나

```rust
let x: Arc<dyn SomeTrait>;
// 내부 ptr 필드는 사실상 (data_ptr, vtable_ptr) 쌍을 가진 fat pointer가 된다.

let y: Arc<[u8]>;
// 내부 ptr 필드는 (data_ptr, len)을 포함한 fat pointer가 된다.
```

즉, `Arc<T>`의 `ptr: NonNull<ArcInner<T>>`가 T가 unsized일 때는 **fat pointer**가 된다.

이 말은:

* 컴파일러 입장에서 `ArcInner<T>`의 레이아웃 계산이 T의 unsized 정보를 포함해서 이뤄진다는 뜻이다.
* `Arc<[T]>`, `Arc<str>`, `Arc<dyn Trait>` 같은 타입도 자연스럽게 지원할 수 있게 된다.

---

## `Arc<[T]>` / `Arc<str>` 최적화

### Box<[T]>와 비슷한 방식
`Box<[T]>`가 어떻게 생기는지 먼저 생각해보면 좋다.

```text
allocation:
[ T, T, T, ... T ]  // len 개

Box<[T]>는 이 allocation의 시작 주소 + len 을 갖고 있음
```

`Arc<[T]>`도 비슷하지만, **참조 카운터 메타데이터(ArcInner)가 앞에 붙는다**고 보면 된다.

```text
allocation:
[ ArcInnerHeader (strong, weak, etc...) | T, T, T, ... T ]

Arc<[T]>의 fat pointer:
  - data_ptr = T 배열의 시작 주소(헤더 뒤)
  - len = 요소 개수
```

하지만 `ArcInner<[T]>` 전체의 시작 주소는 사실상 헤더 부분부터고, 내부에서 슬라이스 부분의 offset을 알고 있다.

즉, 힙에 놓인 실제 메모리는 대략 다음과 같이 정리된다.

```text
[ strong | weak | (기타 메타) | T[0] | T[1] | ... | T[len-1] ]
 ^         ^
 |         └─ data 시작 offset
 |
 ArcInner 시작
```

### 왜 이런 레이아웃이 좋나

* **allocation 1번**으로 메타데이터 + 배열을 모두 담을 수 있다.
* 카운터와 데이터가 연속해서 있어 캐시 효율이 좋다.
* `Arc<[T]>`와 `Arc<str>`은 slice/str 특화 최적화가 적용되므로, 종종 `Arc<Vec<T>>`보다 메모리 레이아웃이 더 컴팩트해질 수 있다.
  (Vec은 capacity, len 등 추가 필드가 있기 때문이다)

그래서 문자열 공유를 많이 할 때는:

```rust
Arc<str>
Arc<[u8]>
```

같은 타입이 꽤 실용적이다.

---

## `Arc::from_raw` / `Arc::into_raw`의 위험 포인트
이 부분이 진짜 사고 나기 좋은 구간이다.

### 3-1. 기본 개념

```rust
impl<T> Arc<T> {
    pub fn into_raw(this: Self) -> *const T;
    pub unsafe fn from_raw(ptr: *const T) -> Self;
}
```

의도는:

* `into_raw`로 Arc를 **소유권 있는 포인터**로 “분해”하여 C API 등 외부로 넘기고
* 나중에 같은 포인터를 `from_raw`로 다시 Arc로 “재조립”해서 관리권을 Rust 쪽이 되찾는 패턴이다.

하지만 규칙을 잘못 지키면 바로 UB가 난다.

### 절대 하면 안 되는 것들

#### ① 같은 raw 포인터로 `from_raw`를 두 번 호출

```rust
let arc = Arc::new(10);
let ptr = Arc::into_raw(arc);

unsafe {
    let a1 = Arc::from_raw(ptr);
    let a2 = Arc::from_raw(ptr); // ❌ UB: 같은 strong을 두 번 생성
}
```

문제점:

* `into_raw`는 strong 카운트를 줄이지 않고 단순히 Arc를 포인터로 변환할 뿐이다.
* `from_raw(ptr)`를 한 번 호출하면, 그 포인터에 대해 **새로운 Arc strong 소유자 1개**가 생긴다.
* 같은 포인터를 두 번 `from_raw`하면 strong 카운트 관리가 꼬이고, drop 시 double free 같은 문제가 일어난다.

**규칙**
`into_raw`로 얻은 포인터는 “정확히 한 번만” `from_raw`로 되돌린다는 가정 하에 설계되어 있다.

#### ② `into_raw`로 얻은 포인터를 임의로 변형

```rust
let arc = Arc::new(10);
let ptr = Arc::into_raw(arc);

unsafe {
    let fake = ptr.add(1); // ❌ 완전 잘못된 주소
    let a = Arc::from_raw(fake); // UB
}
```

* from_raw에는 반드시 **into_raw로 나온 정확히 그 포인터**를 넣어야 한다.
* Arc는 포인터 앞뒤에 메타데이터, alignment 등 다양한 정보가 걸려 있어서, 주소를 건드리면 답이 없다.

#### ③ `Weak`용 포인터와 섞어 쓰기
Weak도 비슷한 `into_raw`, `from_raw`가 있는데, 이 둘을 혼동하면 안 된다.

```rust
let arc = Arc::new(10);
let weak = Arc::downgrade(&arc);
let ptr = Weak::into_raw(weak);

unsafe {
    let a = Arc::from_raw(ptr as *const i32); // ❌ UB, Weak용 포인터를 Arc로 재조립
}
```

Weak 포인터가 가리키는 위치와 Arc 데이터 포인터의 의미가 다를 수 있어 UB가 된다.

### 안전한 패턴 예시

```rust
use std::sync::Arc;

extern "C" fn c_api_callback(ptr: *const i32) {
    unsafe {
        // 다시 Arc로 되돌려 one-shot으로 쓰고 drop
        let arc = Arc::from_raw(ptr);
        println!("{}", *arc);
        // 여기서 drop 되지만, 필요하다면 Arc::into_raw로 다시 보낼 수도 있다.
    }
}

fn main() {
    let arc = Arc::new(42);
    let raw = Arc::into_raw(arc);
    // raw를 C API에 넘겨 보관시킴
    unsafe {
        c_api_callback(raw);
    }
}
```

이 경우:

* `into_raw` 한 번
* `from_raw` 한 번
  → 참조 카운트 일관성이 유지된다.

---

## Arc / Rc와 기타 메모리 최적화 트릭
몇 가지 자주 쓰이는 패턴을 정리한다.

### 빈 값 최적화 (zero-sized type, ZST)

`Arc<()>`, `Arc<PhantomData<T>>` 같은 ZST는 데이터가 실제론 필요 없으므로, 구현에서 별도 최적화를 적용할 수 있다.

* allocation을 완전히 생략하거나
* 아주 작은 블록만 사용하거나

등등의 최적화가 들어갈 수 있다. 다만 구체 구현은 버전에 따라 달라질 수 있어서 “이렇게 되어 있다”라고 단정짓기는 어렵다.

### `Arc<dyn Trait>` vs `Arc<T>`

```rust
let a: Arc<dyn Trait> = Arc::new(Concrete);
```

이런 코드를 쓰면:

* 내부에서 `Arc<Concrete>`를 만들고
* 그 포인터를 `Arc<dyn Trait>`로 “unsize coercion” 한다.

이 과정에서 포인터가 thin → fat으로 바뀌고, vtable 포인터가 붙는다.
따라서 `Arc<dyn Trait>`는 런타임에 **다형성**을 가지지만, 포인터 크기는 2 word가 된다.

### DST (`[T]`, `str`, `dyn Trait`)용 Arc 생성
표준 라이브러리는 `Arc<[T]>`, `Arc<str>`, `Arc<dyn Trait>`을 만들기 위한 편의 함수들을 제공한다:

* `Arc::from(slice)` → `Arc<[T]>`
* `Arc::<str>::from("...")`
* trait object는 보통 `Arc::new(T) as Arc<dyn Trait>` 형태로 만든다.

이때 내부에서:

* 슬라이스 길이만큼만 allocation을 하고
* 앞에 refcount 헤더를 두고
* 뒤에 T 요소를 연속으로 배치하는 최적화가 들어간다.  