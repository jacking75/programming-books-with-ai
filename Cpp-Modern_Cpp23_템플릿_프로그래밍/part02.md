# Modern C++23 템플릿 프로그래밍  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio 2026 (Community 이상)
- **컴파일러**: C++ 23
- **OS**: Windows 10 이상

----- 
  
# Chapter 6. Concepts — 템플릿에 계약을 추가하다

---

> **"좋은 인터페이스는 올바른 사용을 쉽게, 잘못된 사용을 어렵게 만든다. Concepts는 템플릿에 그 원칙을 심는다."**
> — Scott Meyers의 설계 원칙을 Concepts에 적용하며

---

지금까지 우리가 만든 템플릿들은 한 가지 공통된 문제를 안고 있었습니다. 잘못된 타입을 넘기면 **컴파일러가 내부 구현 깊숙한 곳에서 알 수 없는 오류 메시지를 쏟아낸다**는 것입니다. 마치 식당에 들어가서 메뉴를 주문했더니 "주방 내부에서 재료를 썰다가 문제가 생겼습니다"라는 말을 듣는 것처럼요. 손님이 잘못된 주문을 했다면, 주방이 아니라 **입구에서** 막아줘야 합니다. C++20에서 도입된 **Concepts**는 바로 그 역할을 합니다.

---

## 6.1 기존 SFINAE 방식의 복잡함과 고통

**문제를 직접 느껴보자**

먼저 Concepts 없이 "정수 타입만 받는 함수"를 만들려고 할 때 어떤 일이 벌어지는지 살펴봅시다.

```cpp
// ❌ 제약 없는 템플릿 — 어떤 타입이든 받아버림
template<typename T>
T double_value(T x) {
    return x + x;  // std::string도 받아버린다!
}

int main() {
    double_value(42);        // OK
    double_value(3.14);      // OK
    double_value("hello");   // 컴파일 에러, 하지만 메시지가 끔찍함
}
```

`double_value("hello")`를 호출했을 때 Visual Studio가 뱉는 오류는 대략 이런 식입니다.

```
error C2110: '+': 두 포인터를 추가할 수 없습니다.
  'double_value<const char*>(const char*)' 인스턴스화를 통해
  'T double_value(T)' 에서
  여기서 T = const char*
```

오류 메시지가 함수 내부 구현(`x + x`)을 가리키고 있습니다. 함수를 호출한 쪽이 아니라요. 이제 이 문제를 C++17 이전에 어떻게 해결하려 했는지 봅시다.

```cpp
#include <type_traits>

// C++17 이전 SFINAE 방식 — 읽기도 힘들고 이해하기도 어렵다
template<typename T,
         typename = std::enable_if_t<std::is_arithmetic_v<T>>>
T double_value(T x) {
    return x + x;
}
```

이게 바로 **SFINAE(Substitution Failure Is Not An Error)** 기법입니다. "치환 실패는 오류가 아니다"라는 규칙을 악용해서 조건을 만족하지 않으면 해당 함수를 후보에서 조용히 제거하는 방식입니다. 코드가 동작하기는 하지만, 왜 이렇게 써야 하는지 처음 보는 사람은 전혀 이해할 수 없습니다. 이 SFINAE에 대해서는 Chapter 12에서 더 자세히 다루겠습니다. 지금은 "복잡하고 읽기 어렵다"는 것만 기억하고 넘어가겠습니다.

**Concepts가 가져온 변화**

```cpp
#include <concepts>

// ✅ Concepts 방식 — 의도가 명확하게 드러난다
template<typename T>
    requires std::arithmetic<T>   // "T는 산술 타입이어야 한다"
T double_value(T x) {
    return x + x;
}

int main() {
    double_value("hello");  // 에러, 하지만 이제는 명확한 메시지!
}
```

이제 Visual Studio의 오류 메시지는 이렇게 바뀝니다.

```
error C7602: 'double_value': 관련 제약 조건이 충족되지 않습니다.
  제약 조건 'std::arithmetic<const char*>' 이(가) 충족되지 않았습니다.
```

오류의 원인이 **호출 지점에서, 사람이 읽을 수 있는 언어로** 표현됩니다. 이것이 Concepts의 본질적인 가치입니다.

---

## 6.2 `concept` 정의 문법

**기본 문법 구조**

Concept은 특정 타입이 만족해야 하는 **제약 조건의 집합**입니다. `bool` 값을 반환하는 컴파일 타임 술어(predicate)라고 생각해도 됩니다.

```
concept 키워드로 정의
        │
        ▼
template<typename T>
concept 개념이름 = 제약_표현식;
                  │
                  └─ 컴파일 타임에 true/false로 평가되는 표현식
```

```cpp
#include <concepts>
#include <type_traits>

// 가장 단순한 형태: 타입 트레이트로 정의
template<typename T>
concept Integral = std::is_integral_v<T>;

// 여러 조건을 && 로 결합
template<typename T>
concept SignedIntegral = std::is_integral_v<T> && std::is_signed_v<T>;

// || 로 결합 (둘 중 하나면 됨)
template<typename T>
concept IntOrFloat = std::is_integral_v<T> || std::is_floating_point_v<T>;

// 컴파일 타임에 직접 확인해보기
static_assert(Integral<int>);        // OK
static_assert(Integral<bool>);       // OK (bool은 정수형)
static_assert(!Integral<double>);    // OK (double은 정수형 아님)
static_assert(SignedIntegral<int>);  // OK
static_assert(!SignedIntegral<unsigned int>); // OK
```

**Concept을 사용하는 네 가지 문법**

같은 제약을 표현하는 방법이 네 가지 있습니다. 상황에 따라 편한 것을 고르면 됩니다.

```cpp
#include <concepts>

// ── 방법 1: requires 절 (가장 명시적)
template<typename T>
    requires std::integral<T>
void print_int_1(T value) { }

// ── 방법 2: Concept을 typename 자리에 직접 사용 (가장 간결)
template<std::integral T>
void print_int_2(T value) { }

// ── 방법 3: 축약 함수 템플릿 (C++20, 가장 짧음)
void print_int_3(std::integral auto value) { }

// ── 방법 4: trailing requires (반환 타입 뒤에 오는 형태)
template<typename T>
void print_int_4(T value) requires std::integral<T> { }

int main() {
    print_int_1(42);     // OK
    print_int_2(42);     // OK
    print_int_3(42);     // OK
    print_int_4(42);     // OK

    // print_int_1(3.14); // ❌ 컴파일 에러: double은 integral이 아님
}
```

**어떤 문법을 쓸까?**

방법 1(`requires` 절)은 조건이 복잡할 때 가장 읽기 쉽습니다. 방법 2(Concept을 `typename` 자리에)는 단순한 경우에 가장 깔끔합니다. 방법 3(축약 함수 템플릿)은 짧은 유틸리티 함수에 잘 어울립니다. 이 책에서는 주로 방법 1과 방법 2를 사용하겠습니다.

---

## 6.3 `requires` 절 — 제약 조건 표현하기

`requires` 절은 템플릿 파라미터에 조건을 붙이는 문법입니다. `if`문처럼 조건을 표현하지만, **컴파일 타임에** 평가됩니다.

**기본 사용법**

```cpp
#include <concepts>
#include <iostream>

// 단일 조건
template<typename T>
    requires std::floating_point<T>
T square_root_approx(T x) {
    // 뉴턴-랩슨 방법 (간단 버전)
    T result = x / 2;
    for (int i = 0; i < 10; ++i)
        result = (result + x / result) / 2;
    return result;
}

// 여러 조건 결합
template<typename T>
    requires std::integral<T> && std::is_signed_v<T>
T safe_abs(T x) {
    return x < 0 ? -x : x;
}

int main() {
    std::cout << square_root_approx(2.0)  << "\n";  // 1.41421...
    std::cout << square_root_approx(9.0f) << "\n";  // 3.0
    // square_root_approx(4);  // ❌ int는 floating_point가 아님

    std::cout << safe_abs(-42)  << "\n";  // 42
    // safe_abs(-42u);  // ❌ unsigned int는 signed가 아님
}
```

**클래스 템플릿에서의 사용**

```cpp
#include <concepts>
#include <iostream>

// 산술 연산을 지원하는 타입만 받는 스택
template<typename T>
    requires std::is_arithmetic_v<T>
class NumericStack {
    T data_[16]{};
    int top_ = 0;
public:
    void push(T value) { data_[top_++] = value; }
    T    pop()         { return data_[--top_]; }
    bool empty() const { return top_ == 0; }

    // 스택의 합계 — 산술 타입이 보장되니 안전하게 구현 가능
    T sum() const {
        T result{};
        for (int i = 0; i < top_; ++i) result += data_[i];
        return result;
    }
};

int main() {
    NumericStack<int> s;
    s.push(10);
    s.push(20);
    s.push(30);
    std::cout << s.sum() << "\n";  // 60

    // NumericStack<std::string> s2;  // ❌ string은 arithmetic이 아님
}
```

---

## 6.4 `requires` 표현식 — 세부 요구사항 명시하기

`requires` 표현식은 "이 타입이 **이런 연산을 지원하는가**"를 직접 확인하는 강력한 도구입니다. 타입이 특정 멤버 함수나 연산자를 가지고 있는지 컴파일 타임에 검사할 수 있습니다.

**`requires` 절 vs. `requires` 표현식**

이름이 비슷해서 헷갈릴 수 있습니다. 명확히 구분해봅시다.

```
requires 절 (Requires Clause):
  template<typename T>
      requires [조건식]        ← requires 뒤에 bool 표현식
  void func(T x);

requires 표현식 (Requires Expression):
  requires (T x) {             ← requires 뒤에 { } 블록
      { x.size() };            ← "이 표현식이 유효해야 한다"
      { x + x } -> std::same_as<T>;
  }

둘을 조합:
  template<typename T>
      requires requires (T x) { x.size(); }  ← "requires requires"
  void func(T x);
```

`requires requires` 처럼 두 번 쓰이는 것이 이상해 보이지만, 앞의 것은 절(clause)이고 뒤의 것은 표현식(expression)입니다. 보통은 이런 경우 별도의 `concept`으로 분리하는 것이 더 좋습니다.

**`requires` 표현식의 네 가지 요구사항 형태**

```cpp
#include <concepts>
#include <string>

template<typename T>
concept Describable = requires(T x) {
    // ① 단순 표현식 — "이 식이 컴파일 되어야 한다"
    x.describe();

    // ② 타입 요구사항 — "이 타입이 존재해야 한다"
    typename T::value_type;

    // ③ 복합 요구사항 — "이 식의 반환 타입을 검사한다"
    { x.name() } -> std::convertible_to<std::string>;

    // ④ noexcept 요구사항 — "이 식은 예외를 던지지 않아야 한다"
    { x.id() } noexcept -> std::integral<decltype(x.id())>;
};
```

실용적인 예시로 이 네 가지를 직접 써봅시다.

```cpp
#include <concepts>
#include <string>
#include <iostream>

// "컨테이너처럼 동작하는" 타입 정의
template<typename T>
concept ContainerLike = requires(T c) {
    // size() 함수가 있어야 함
    { c.size() } -> std::convertible_to<std::size_t>;
    // empty() 함수가 있어야 함
    { c.empty() } -> std::same_as<bool>;
    // begin(), end() 가 있어야 함
    c.begin();
    c.end();
};

// ContainerLike를 만족하는 타입의 원소 개수를 출력
template<ContainerLike C>
void print_size(const C& container) {
    std::cout << "Size: " << container.size()
              << ", Empty: " << std::boolalpha << container.empty() << "\n";
}

#include <vector>
#include <string>
#include <array>

int main() {
    std::vector<int>   v = {1, 2, 3};
    std::string        s = "hello";
    std::array<int, 5> a = {1, 2, 3, 4, 5};

    print_size(v);  // Size: 3, Empty: false
    print_size(s);  // Size: 5, Empty: false
    print_size(a);  // Size: 5, Empty: false

    // print_size(42);  // ❌ int는 ContainerLike를 만족하지 않음
}
```

**복합 요구사항으로 연산자 검사하기**

```cpp
#include <concepts>

// 두 타입 사이에 + 연산이 가능하고,
// 그 결과 타입을 지정할 수 있는지 확인
template<typename T, typename U = T>
concept Addable = requires(T a, U b) {
    { a + b } -> std::common_with<T>;
};

// 비교 가능한 타입
template<typename T>
concept Comparable = requires(T a, T b) {
    { a <  b } -> std::same_as<bool>;
    { a >  b } -> std::same_as<bool>;
    { a == b } -> std::same_as<bool>;
    { a != b } -> std::same_as<bool>;
};

// 사용 예
template<Comparable T>
T clamp(T value, T lo, T hi) {
    if (value < lo) return lo;
    if (value > hi) return hi;
    return value;
}

int main() {
    std::cout << clamp(15, 0, 10) << "\n";   // 10
    std::cout << clamp(-5, 0, 10) << "\n";   // 0
    std::cout << clamp(5,  0, 10) << "\n";   // 5
}
```

---

## 6.5 표준 라이브러리 Concepts

C++20 표준 라이브러리는 `<concepts>` 헤더와 `<iterator>`, `<ranges>` 헤더에 풍부한 built-in Concept들을 제공합니다. 이것들을 잘 알아두면 대부분의 상황에서 직접 concept을 만들 필요가 없습니다.

**주요 표준 Concepts 한눈에 보기**

```
<concepts> 헤더
├── 핵심 언어 개념
│   ├── same_as<T, U>          — T와 U가 완전히 같은 타입
│   ├── derived_from<D, B>     — D가 B를 상속
│   ├── convertible_to<From,To>— From이 To로 암묵 변환 가능
│   └── common_with<T, U>      — T와 U의 공통 타입이 존재
│
├── 비교 개념
│   ├── equality_comparable<T> — == 와 != 지원
│   └── totally_ordered<T>     — <, >, <=, >= 모두 지원
│
├── 타입 분류 개념
│   ├── integral<T>            — 정수 타입
│   ├── signed_integral<T>     — 부호있는 정수
│   ├── unsigned_integral<T>   — 부호없는 정수
│   ├── floating_point<T>      — 부동소수점 타입
│   └── arithmetic<T>          — integral || floating_point (비표준 주의*)
│
└── 객체 개념
    ├── copyable<T>            — 복사 생성/대입 가능
    ├── movable<T>             — 이동 생성/대입 가능
    ├── default_initializable<T>— 기본 생성 가능
    └── regular<T>             — 값처럼 동작하는 완전한 타입

* std::arithmetic은 C++20 표준에 없음. std::is_arithmetic_v<T> 사용
```

```
<iterator> 헤더
├── input_iterator<I>          — 단방향 읽기 반복자
├── output_iterator<I, T>      — 단방향 쓰기 반복자
├── forward_iterator<I>        — 다중 통과 가능 반복자
├── bidirectional_iterator<I>  — 양방향 반복자
├── random_access_iterator<I>  — 임의 접근 반복자
└── contiguous_iterator<I>     — 연속 메모리 반복자

<ranges> 헤더
├── range<R>                   — begin()/end() 를 가지는 타입
├── sized_range<R>             — size()도 가지는 range
├── input_range<R>             — input_iterator로 순회 가능
├── forward_range<R>           — 다중 통과 가능한 range
├── bidirectional_range<R>     — 양방향 순회 가능한 range
└── random_access_range<R>     — 임의 접근 가능한 range
```

**표준 Concepts 실용 예제**

```cpp
#include <concepts>
#include <ranges>
#include <iostream>
#include <vector>
#include <list>
#include <string>

// ── 예제 1: 정수/부동소수점 타입 구분 처리
template<std::integral T>
void print_type_info(T x) {
    std::cout << x << " is an integer\n";
}

template<std::floating_point T>
void print_type_info(T x) {
    std::cout << x << " is a floating point\n";
}

// ── 예제 2: Range 기반 처리
template<std::ranges::input_range R>
auto sum_range(const R& range) {
    using value_t = std::ranges::range_value_t<R>;
    value_t result{};
    for (const auto& elem : range)
        result += elem;
    return result;
}

// ── 예제 3: 비교 가능한 타입만 받는 min/max
template<std::totally_ordered T>
T my_min(T a, T b) {
    return a < b ? a : b;
}

int main() {
    print_type_info(42);     // 42 is an integer
    print_type_info(3.14);   // 3.14 is a floating point

    std::vector<int> v{1, 2, 3, 4, 5};
    std::list<int>   l{10, 20, 30};
    std::cout << sum_range(v) << "\n";  // 15
    std::cout << sum_range(l) << "\n";  // 60

    std::cout << my_min(3, 7)         << "\n";  // 3
    std::cout << my_min(3.14, 2.71)   << "\n";  // 2.71
    std::cout << my_min('a', 'z')     << "\n";  // a
}
```

---

## 6.6 Concept의 서브섬션(Subsumption) — 우선순위 결정 규칙

Concepts의 가장 강력한 기능 중 하나는 **오버로딩 해결 시 자동으로 우선순위를 결정**한다는 것입니다. 더 구체적인(더 강한) Concept이 더 일반적인 Concept보다 우선 선택됩니다.

**서브섬션이란?**

```
Concept A가 Concept B를 포함(subsume)한다
= A를 만족하면 반드시 B도 만족한다
= A는 B보다 더 구체적이다
= 오버로딩 해결 시 A가 우선순위를 가진다

예:
  signed_integral<T>  →  integral<T>  →  (아무 제약 없음)
       더 구체적            덜 구체적         가장 일반적
```

```mermaid
graph TD
    A["아무 제약 없음\n(unconstrained)"]
    B["integral&lt;T&gt;\n정수 타입"]
    C["signed_integral&lt;T&gt;\n부호있는 정수"]
    D["unsigned_integral&lt;T&gt;\n부호없는 정수"]

    B -->|"subsumes"| A
    C -->|"subsumes"| B
    D -->|"subsumes"| B

    style A fill:#f5f5f5,stroke:#999
    style B fill:#dae8fc,stroke:#6c8ebf
    style C fill:#d5e8d4,stroke:#82b366
    D fill:#ffe6cc,stroke:#d6b656
```

**서브섬션 동작 예제**

```cpp
#include <concepts>
#include <iostream>

// 가장 일반적인 버전
template<typename T>
void process(T x) {
    std::cout << "Generic: " << x << "\n";
}

// integral이면 이 버전 사용 (위 버전보다 구체적)
template<std::integral T>
void process(T x) {
    std::cout << "Integral: " << x << "\n";
}

// signed_integral이면 이 버전 사용 (위 버전보다 더 구체적)
template<std::signed_integral T>
void process(T x) {
    std::cout << "Signed integral: " << x << "\n";
}

int main() {
    process(3.14);   // Generic: 3.14
    process(10u);    // Integral: 10      (unsigned → integral이지만 signed_integral은 아님)
    process(-5);     // Signed integral: -5  (가장 구체적인 버전 선택!)
}
```

컴파일러는 `signed_integral<T>`가 `integral<T>`를 포함(subsume)한다는 것을 알기 때문에, `int`에 대해 세 후보 중 가장 구체적인 것을 자동으로 선택합니다.

**직접 만든 Concept의 서브섬션**

서브섬션은 기존 Concept을 **다른 Concept으로 정의할 때** 자동으로 작동합니다.

```cpp
#include <concepts>
#include <iostream>

// NumberLike: 산술 연산 가능
template<typename T>
concept NumberLike = std::integral<T> || std::floating_point<T>;

// ExactNumber: NumberLike이면서 copy 가능 (더 구체적)
// → NumberLike를 포함(subsume)함
template<typename T>
concept ExactNumber = NumberLike<T> && std::copyable<T>;

template<NumberLike T>
void compute(T x) {
    std::cout << "NumberLike version: " << x << "\n";
}

template<ExactNumber T>
void compute(T x) {
    std::cout << "ExactNumber version: " << x << "\n";
}

int main() {
    compute(42);    // ExactNumber version (더 구체적)
    compute(3.14);  // ExactNumber version (더 구체적)
}
```

**⚠️ 서브섬션이 작동하지 않는 경우**

Concept을 `&&`와 `||`로 직접 결합한 경우에만 서브섬션이 작동합니다. 우회적으로 정의하면 컴파일러가 포함 관계를 인식하지 못해 **모호성 오류**가 발생할 수 있습니다.

```cpp
// ❌ 서브섬션이 작동하지 않는 예 — 컴파일러가 관계를 알 수 없음
template<typename T>
concept MyIntegral = std::is_integral_v<T>;  // type_traits 우회

template<typename T>
concept MySignedIntegral = MyIntegral<T> && std::is_signed_v<T>;

template<MyIntegral T>
void bad_process(T x) { }

template<MySignedIntegral T>
void bad_process(T x) { }

// bad_process(-5);  // ❌ 모호성 오류! 컴파일러가 우선순위를 모름

// ✅ 올바른 방법 — 표준 Concept을 직접 사용
template<typename T>
concept GoodSignedIntegral = std::integral<T> && std::signed_integral<T>;
```

---

## 🛠 실습: `Printable`, `Arithmetic`, `Container` 컨셉 직접 만들기

이제 배운 내용을 종합해서 실용적인 Concept 세 가지를 직접 만들어봅니다.

**전체 코드**

```cpp
#include <concepts>
#include <ranges>
#include <iostream>
#include <string>
#include <vector>
#include <list>
#include <sstream>

// ══════════════════════════════════════════════════════
// ① Printable — std::cout으로 출력 가능한 타입
// ══════════════════════════════════════════════════════
template<typename T>
concept Printable = requires(std::ostream& os, const T& value) {
    { os << value } -> std::same_as<std::ostream&>;
};

// Printable을 활용하는 함수
template<Printable T>
void print(const T& value) {
    std::cout << value << "\n";
}

// 여러 값을 구분자와 함께 출력
template<Printable T>
void print_with_sep(const T& value, std::string_view sep = ", ") {
    std::cout << value << sep;
}

// ══════════════════════════════════════════════════════
// ② Arithmetic — 사칙연산이 모두 가능한 타입
// ══════════════════════════════════════════════════════
template<typename T>
concept Arithmetic = requires(T a, T b) {
    { a + b } -> std::convertible_to<T>;
    { a - b } -> std::convertible_to<T>;
    { a * b } -> std::convertible_to<T>;
    { a / b } -> std::convertible_to<T>;
    // 단항 연산
    { -a }    -> std::convertible_to<T>;
} && std::copyable<T>
  && std::default_initializable<T>;

// Arithmetic 타입에 대한 평균 계산
template<Arithmetic T>
T average(T a, T b) {
    return (a + b) / T{2};
}

// Arithmetic 타입에 대한 선형 보간
template<Arithmetic T>
T lerp(T start, T end, T t) {
    return start + (end - start) * t;
}

// ══════════════════════════════════════════════════════
// ③ Container — STL 스타일 컨테이너 타입
// ══════════════════════════════════════════════════════
template<typename T>
concept Container = requires(T c) {
    // 반복자 인터페이스
    { c.begin() } -> std::input_or_output_iterator;
    { c.end()   } -> std::input_or_output_iterator;
    // 크기 관련
    { c.size()  } -> std::convertible_to<std::size_t>;
    { c.empty() } -> std::same_as<bool>;
    // 연관 타입
    typename T::value_type;
    typename T::iterator;
} && std::copyable<T>;

// Container 타입의 모든 원소를 출력
template<Container C>
    requires Printable<typename C::value_type>   // 원소도 출력 가능해야 함
void print_container(const C& container, std::string_view name = "") {
    if (!name.empty()) std::cout << name << ": ";
    std::cout << "[";
    bool first = true;
    for (const auto& elem : container) {
        if (!first) std::cout << ", ";
        std::cout << elem;
        first = false;
    }
    std::cout << "] (size=" << container.size() << ")\n";
}

// Container 타입의 합계 계산 (원소가 Arithmetic인 경우)
template<Container C>
    requires Arithmetic<typename C::value_type>
typename C::value_type container_sum(const C& container) {
    typename C::value_type result{};
    for (const auto& elem : container)
        result = result + elem;
    return result;
}

// ══════════════════════════════════════════════════════
// 세 Concept이 제대로 동작하는지 static_assert로 검증
// ══════════════════════════════════════════════════════
static_assert(Printable<int>);
static_assert(Printable<double>);
static_assert(Printable<std::string>);
static_assert(!Printable<std::vector<int>>);  // vector는 << 연산자 없음

static_assert(Arithmetic<int>);
static_assert(Arithmetic<double>);
static_assert(Arithmetic<float>);
static_assert(!Arithmetic<std::string>);      // 나눗셈이 안됨
static_assert(!Arithmetic<bool>);             // bool 나눗셈은 convertible_to<bool> 충족 안 됨

static_assert(Container<std::vector<int>>);
static_assert(Container<std::list<double>>);
static_assert(Container<std::string>);        // string도 container!
static_assert(!Container<int>);               // int는 container가 아님

// ══════════════════════════════════════════════════════
// main: 실제 동작 확인
// ══════════════════════════════════════════════════════
int main() {
    std::cout << "=== Printable 테스트 ===\n";
    print(42);
    print(3.14);
    print(std::string("hello, concept!"));

    std::cout << "\n=== Arithmetic 테스트 ===\n";
    std::cout << "average(3, 7)          = " << average(3, 7)          << "\n";
    std::cout << "average(1.0, 2.0)      = " << average(1.0, 2.0)      << "\n";
    std::cout << "lerp(0.0f, 100.0f, 0.3f) = " << lerp(0.0f, 100.0f, 0.3f) << "\n";

    std::cout << "\n=== Container 테스트 ===\n";
    std::vector<int>    v  = {1, 2, 3, 4, 5};
    std::list<double>   l  = {1.1, 2.2, 3.3};
    std::string         s  = "Hello";

    print_container(v, "vector<int>");
    print_container(l, "list<double>");
    print_container(s, "string");

    std::cout << "vector sum = " << container_sum(v) << "\n";
    std::cout << "list sum   = " << container_sum(l) << "\n";
}
```

**실행 결과**

```
=== Printable 테스트 ===
42
3.14
hello, concept!

=== Arithmetic 테스트 ===
average(3, 7)            = 5
average(1.0, 2.0)        = 1.5
lerp(0.0f, 100.0f, 0.3f) = 30

=== Container 테스트 ===
vector<int>: [1, 2, 3, 4, 5] (size=5)
list<double>: [1.1, 2.2, 3.3] (size=3)
string: [H, e, l, l, o] (size=5)
vector sum = 15
list sum   = 6.6
```

**Concepts의 제약 조건이 오류 메시지를 어떻게 개선하는지**

잘못된 타입을 넘겼을 때 Visual Studio 2026에서 출력되는 오류를 비교해봅시다.

```
❌ 잘못된 호출:
    print_container(42);

✅ Concepts 적용 후 오류 메시지:
    error C7602: 'print_container': 관련 제약 조건이 충족되지 않습니다.
    note: 'Container<int>' 제약 조건이 충족되지 않았습니다.
    note: 요구사항 '{ c.begin() }' 이(가) 충족되지 않았습니다: int에는 begin()이 없습니다.

❌ Concepts 없이 같은 코드:
    error C2039: 'begin': 'int'의 멤버가 아닙니다.
    error C2039: 'end': 'int'의 멤버가 아닙니다.
    error C2039: 'size': 'int'의 멤버가 아닙니다.
    ... (10줄 이상의 내부 오류)
```

---

**이 장에서 배운 것 정리**

이 장에서 우리는 C++20이 가져온 가장 중요한 혁신 중 하나인 Concepts를 완전히 이해했습니다. SFINAE라는 복잡한 우회 방법에서 벗어나, `concept` 키워드로 타입에 대한 요구사항을 명확하고 읽기 쉽게 표현할 수 있게 되었습니다. `requires` 절과 `requires` 표현식으로 제약 조건을 세밀하게 명시하고, 서브섬션 규칙을 통해 오버로딩 우선순위도 자동으로 처리됩니다. 표준 라이브러리의 built-in Concepts를 활용하면 대부분의 경우 직접 만들 필요도 없습니다. 다음 장에서는 가변 인자 템플릿(Variadic Templates)을 통해 임의 개수의 파라미터를 다루는 방법을 배웁니다.




# Chapter 7. 가변 인자 템플릿 (Variadic Templates)

---

> **이 챕터에서 배울 것**
>
> 지금까지 우리는 `template<typename T>`처럼 **고정된 수의 타입 파라미터**만 다뤘다. 하지만 현실에서는 `std::make_tuple(1, 2.0, "hello")`처럼 **몇 개의 인자가 올지 모르는** 상황이 훨씬 많다. 가변 인자 템플릿(Variadic Templates)은 바로 이 문제를 우아하게 해결하는 C++11 이후의 핵심 기능이다. 폴드 표현식(C++17)까지 더하면, 과거에 수십 줄이던 코드가 한 줄로 줄어드는 마법을 경험하게 될 것이다.

---

## 7.1 파라미터 팩(Parameter Pack) 기초

**파라미터 팩이란 무엇인가:**

파라미터 팩(Parameter Pack)은 "0개 이상의 타입이나 값을 하나의 이름으로 묶은 것"이다. `...`(줄임표, ellipsis) 기호가 핵심이며, 이 점 세 개가 등장하는 위치에 따라 의미가 달라지므로 주의깊게 살펴보자.

```
                 템플릿 파라미터 팩 선언
                 ┌──────────────────┐
template<typename... Types>
//        ┌──────────────────┐
//        팩 이름은 "Types"
void print(Types... args)
//         └──────────────────┘
//         함수 파라미터 팩 선언
{
    // 여기서 args를 사용하려면 팩 확장(...)이 필요하다
}
```

`...`이 붙는 위치가 두 군데임에 주목하자. 첫 번째는 **선언** 위치(`typename... Types`, `Types... args`)이고, 두 번째는 나중에 배울 **확장** 위치(`args...`)이다. 이 둘을 혼동하지 않는 것이 가변 인자 템플릿 이해의 절반이다.

**가장 단순한 예제:**

```cpp
#include <iostream>

// 파라미터 팩을 받는 함수 템플릿
template<typename... Types>
void show_types() {
    std::cout << "인자 개수: " << sizeof...(Types) << "\n";
}

int main() {
    show_types<>();           // 인자 개수: 0
    show_types<int>();        // 인자 개수: 1
    show_types<int, double, std::string>(); // 인자 개수: 3
}
```

위 코드에서 `typename... Types`는 "0개 이상의 타입을 `Types`라는 이름의 팩으로 받겠다"는 선언이다. `sizeof...(Types)`는 팩에 담긴 타입의 수를 컴파일 타임에 반환하며, 이것이 바로 다음 절에서 다룰 `sizeof...` 연산자다.

**타입과 값을 함께 다루는 팩:**

```cpp
#include <iostream>

// 값(함수 파라미터) 팩을 받는 예제
template<typename... Args>
void print_args(Args... args) {
    // 지금은 팩 확장을 아직 모르므로 sizeof...만 확인
    std::cout << "인자 개수: " << sizeof...(args) << "\n";
}

int main() {
    print_args();                    // 인자 개수: 0
    print_args(1, 2.5, "hello");     // 인자 개수: 3
    print_args(true, 'A', 42, 3.14); // 인자 개수: 4
}
```

`Types`는 **타입** 파라미터 팩이고, `args`는 **값** 파라미터 팩이다. 보통 함수 템플릿에서는 이 둘이 함께 등장하여 서로 짝을 이룬다 — `Types`가 타입 목록이면 `args`는 그 타입들의 값 목록이다.

---

## 7.2 팩 확장(Pack Expansion) 패턴

**팩 확장이란 무엇인가:**

파라미터 팩은 `...`을 붙여서 **확장(expand)** 해야 비로소 사용할 수 있다. 팩 확장은 "팩에 있는 모든 원소를 쉼표로 이어 붙인 목록으로 펼쳐라"는 명령이다.

```
pack 확장 전:           args     (하나의 팩 이름)
pack 확장 후: args...  →  E1, E2, E3, ...   (쉼표로 나열된 목록)
```

```
패턴(pattern)이 있는 확장:
&args...  →  &E1, &E2, &E3
std::forward<Args>(args)...  →  std::forward<A1>(a1), std::forward<A2>(a2), ...
```

팩 확장의 핵심 규칙은 바로 **패턴(pattern)**이다. `...` 바로 앞에 있는 전체 표현식이 패턴이 되어, 팩의 각 원소에 대해 반복 적용된다.

**팩 확장이 허용되는 주요 위치:**

```
┌─────────────────────────────────────────────────────┐
│           팩 확장이 가능한 문맥들                     │
├─────────────────────────┬───────────────────────────┤
│   문맥                  │   예시                     │
├─────────────────────────┼───────────────────────────┤
│ 함수 인자 목록           │ f(args...)                 │
│ 템플릿 인자 목록         │ Tuple<Types...>            │
│ 초기화 리스트            │ {args...}                  │
│ 람다 캡처                │ [args...]{ ... }           │
│ 상속(base specifier)    │ struct X : Bases...        │
│ sizeof...               │ sizeof...(args)            │
└─────────────────────────┴───────────────────────────┘
```

**실제 팩 확장 예제 — 재귀 방식:**

재귀는 가변 인자 템플릿을 다루는 가장 전통적인 방법이다. 팩을 하나씩 줄여가며 처리하다가 팩이 비었을 때 기저 케이스(base case)로 처리를 끝낸다.

```cpp
#include <iostream>

// ① 기저 케이스(base case): 인자가 0개일 때
void print() {
    std::cout << "\n";
}

// ② 재귀 케이스: 첫 번째 인자 처리 후 나머지를 재귀 호출
template<typename First, typename... Rest>
void print(First first, Rest... rest) {
    std::cout << first;
    if constexpr (sizeof...(rest) > 0) {
        std::cout << ", ";
    }
    print(rest...);  // ← 팩 확장: rest가 하나씩 줄어든다
}

int main() {
    print(1, 2.5, "hello", true);
    // 출력: 1, 2.5, hello, 1
}
```

이 재귀 호출이 어떻게 펼쳐지는지 살펴보자.

```
print(1, 2.5, "hello", true)
 └─ print(2.5, "hello", true)
     └─ print("hello", true)
         └─ print(true)
             └─ print()   ← 기저 케이스
```

컴파일러는 이 호출 체인 전체를 **컴파일 타임**에 인스턴스화한다. 런타임에는 이미 각 단계의 함수가 모두 생성되어 있는 상태다.

**팩 확장 패턴의 다양한 변형:**

패턴은 단순히 팩 이름만이 아니어도 된다. `...` 앞에 오는 어떤 표현식이든 패턴이 될 수 있다.

```cpp
#include <iostream>
#include <string>

template<typename... Args>
void demonstrate_patterns(Args... args) {
    // 패턴 1: 단순 확장 → args → E1, E2, E3
    auto t1 = std::make_tuple(args...);

    // 패턴 2: 주소 취하기 → &args → &E1, &E2, &E3
    auto t2 = std::make_tuple(&args...);

    // 패턴 3: std::move 적용 → std::move(args) → std::move(E1), ...
    auto t3 = std::make_tuple(std::move(args)...);

    std::cout << "튜플 원소 수: " << std::tuple_size_v<decltype(t1)> << "\n";
}

int main() {
    demonstrate_patterns(1, 2.5, std::string("hi"));
    // 출력: 튜플 원소 수: 3
}
```

---

## 7.3 `sizeof...` 연산자

`sizeof...`는 파라미터 팩에 담긴 원소의 수를 **컴파일 타임 상수**로 반환하는 단항 연산자다. 일반 `sizeof`와 달리 항상 괄호가 필요하며, 평가 결과는 `std::size_t` 타입의 상수 표현식이다.

```cpp
#include <iostream>

template<typename... Types>
void count_demo(Types... args) {
    // 타입 팩의 크기
    constexpr std::size_t type_count = sizeof...(Types);

    // 값 팩의 크기 (항상 같은 값)
    constexpr std::size_t arg_count  = sizeof...(args);

    static_assert(type_count == arg_count); // 항상 참

    std::cout << "파라미터 개수: " << type_count << "\n";
}

int main() {
    count_demo();                   // 파라미터 개수: 0
    count_demo(1);                  // 파라미터 개수: 1
    count_demo(1, 'A', 3.14);       // 파라미터 개수: 3
}
```

**`sizeof...`의 실용적 활용 — 컴파일 타임 분기:**

```cpp
#include <iostream>

template<typename... Args>
void smart_print(Args... args) {
    if constexpr (sizeof...(Args) == 0) {
        std::cout << "(빈 인자)\n";
    } else if constexpr (sizeof...(Args) == 1) {
        // 인자가 정확히 1개일 때 특별 처리
        std::cout << "단일 인자: ";
        (std::cout << ... << args);  // 폴드 표현식 (7.4에서 자세히)
        std::cout << "\n";
    } else {
        std::cout << sizeof...(Args) << "개의 인자\n";
    }
}

int main() {
    smart_print();          // (빈 인자)
    smart_print(42);        // 단일 인자: 42
    smart_print(1, 2, 3);   // 3개의 인자
}
```

`sizeof...`가 반환하는 값은 `constexpr`이므로 `if constexpr`의 조건으로 완벽하게 사용할 수 있다. 이 조합은 가변 인자 템플릿에서 자주 등장하는 관용구다.

---

## 7.4 폴드 표현식(Fold Expression) — C++17의 선물

**폴드 표현식 이전의 고통:**

C++17 이전에는 파라미터 팩 전체에 어떤 연산을 적용하려면 반드시 재귀를 써야 했다. 모든 인자를 더하는 함수 하나를 만들기 위해 기저 케이스와 재귀 케이스, 두 개의 함수가 필요했다. C++17의 폴드 표현식은 이 반복적인 패턴을 단 한 줄로 압축했다.

**폴드 표현식의 네 가지 형태:**

폴드 표현식은 파라미터 팩을 이진 연산자로 **접어(fold)** 하나의 값으로 만드는 표현식이다. `op` 자리에는 `+`, `-`, `*`, `&&`, `||`, `,`, `<<` 등 C++ 이진 연산자 32가지가 모두 들어갈 수 있다.

```
┌──────────────────────────────────────────────────────────────────┐
│                  폴드 표현식 4가지 형태                            │
├──────────────────┬────────────────────┬──────────────────────────┤
│  형태            │  문법              │  펼친 결과               │
├──────────────────┼────────────────────┼──────────────────────────┤
│ 단항 우측 폴드   │ (pack op ...)      │ E1 op (E2 op (E3 op E4)) │
│ 단항 좌측 폴드   │ (... op pack)      │ ((E1 op E2) op E3) op E4 │
│ 이항 우측 폴드   │ (pack op ... op I) │ E1 op (E2 op (E3 op I))  │
│ 이항 좌측 폴드   │ (I op ... op pack) │ ((I op E1) op E2) op E3  │
└──────────────────┴────────────────────┴──────────────────────────┘

* pack: 팩을 포함하는 표현식
* op:   이진 연산자
* I:    팩을 포함하지 않는 초기값 표현식
* 괄호()는 폴드 표현식의 필수 구성 요소다.
```

**폴드 표현식의 직관적 이해 — 덧셈 예제:**

```
pack = {1, 2, 3, 4}라 할 때

단항 우측 폴드 (pack + ...)
  → 1 + (2 + (3 + 4))   ← 오른쪽부터 묶임

단항 좌측 폴드 (... + pack)
  → ((1 + 2) + 3) + 4   ← 왼쪽부터 묶임

이항 좌측 폴드 (0 + ... + pack)
  → ((( 0 + 1) + 2) + 3) + 4   ← 초기값 0부터 시작

이항 우측 폴드 (pack + ... + 0)
  → 1 + (2 + (3 + (4 + 0)))   ← 초기값 0이 마지막에
```

**실제 코드로 보는 폴드 표현식:**

```cpp
#include <iostream>

// ① 합계 — 이항 좌측 폴드 (초기값 0)
template<typename... Args>
auto sum(Args... args) {
    return (0 + ... + args);  // 빈 팩일 때도 안전하게 0 반환
}

// ② 논리 AND — 단항 좌측 폴드
template<typename... Args>
bool all_true(Args... args) {
    return (... && args);  // 빈 팩: true (항등원)
}

// ③ 논리 OR — 단항 좌측 폴드
template<typename... Args>
bool any_true(Args... args) {
    return (... || args);  // 빈 팩: false (항등원)
}

// ④ 출력 — 이항 좌측 폴드 (<<)
template<typename... Args>
void print_all(Args&&... args) {
    (std::cout << ... << args) << "\n";
}

int main() {
    std::cout << sum(1, 2, 3, 4, 5) << "\n";  // 15
    std::cout << sum()              << "\n";  // 0 (빈 팩)

    std::cout << all_true(true, true, true)  << "\n";  // 1
    std::cout << all_true(true, false, true) << "\n";  // 0

    std::cout << any_true(false, false, true) << "\n"; // 1

    print_all("Hello", ", ", "World", "!");    // Hello, World!
}
```

**쉼표 연산자 폴드 — 각 원소에 독립적 연산 적용:**

쉼표 연산자(`,`)로 폴드하면 팩의 각 원소에 임의의 연산을 **순서대로** 적용할 수 있다. 이것은 가변 인자 템플릿에서 가장 자주 쓰이는 패턴 중 하나다.

```cpp
#include <iostream>
#include <vector>

// 임의 개수의 원소를 벡터에 삽입
template<typename T, typename... Args>
void push_all(std::vector<T>& vec, Args&&... args) {
    // 쉼표 폴드: vec.push_back(E1), vec.push_back(E2), ...
    (vec.push_back(std::forward<Args>(args)), ...);
}

// 각 원소에 함수 적용 후 출력
template<typename F, typename... Args>
void for_each_arg(F func, Args&&... args) {
    (func(std::forward<Args>(args)), ...);
}

int main() {
    std::vector<int> v;
    push_all(v, 10, 20, 30, 40);

    for (int x : v) std::cout << x << " ";
    std::cout << "\n";  // 10 20 30 40

    for_each_arg([](auto x) { std::cout << x << " "; },
                 1, 2.5, "hi", 'A');
    std::cout << "\n";  // 1 2.5 hi A
}
```

**빈 팩과 연산자 항등원:**

단항 폴드에서 팩이 비어있을 때는 세 가지 연산자만 허용된다. 그 외의 연산자(예: `+`, `*`)는 빈 팩에 단항 폴드를 쓰면 컴파일 오류가 발생한다.

```
┌──────────────────────────────────────────────────────┐
│  빈 팩에서 단항 폴드의 결과                           │
├─────────────────┬────────────────────────────────────┤
│  연산자         │  빈 팩의 결과                       │
├─────────────────┼────────────────────────────────────┤
│  &&             │  true    (논리적 항등원)             │
│  ||             │  false   (논리적 항등원)             │
│  ,              │  void()                            │
│  그 외 (+, * …) │  ❌ 컴파일 오류 → 이항 폴드를 써라   │
└─────────────────┴────────────────────────────────────┘
```

```cpp
template<typename... Args>
auto safe_sum(Args... args) {
    return (0 + ... + args);  // ✅ 이항 폴드: 빈 팩 → 0
}

template<typename... Args>
auto unsafe_sum(Args... args) {
    return (... + args);  // ❌ 빈 팩이면 컴파일 오류!
}
```

---

## 7.5 재귀 vs. 폴드 표현식 비교

C++17 이전에는 가변 인자 템플릿을 다루는 유일한 방법이 재귀였다. 이제 폴드 표현식이 있으므로, 언제 어느 것을 쓸지 명확히 알아두자.

**같은 기능, 두 가지 구현 방식:**

아래는 모든 인자를 출력하는 함수를 두 가지 방법으로 구현한 것이다.

```cpp
#include <iostream>

// ─── 방법 A: 재귀 (C++11 스타일) ─────────────────────────────

// 기저 케이스 (必수): 팩이 비었을 때
void print_recursive() {}

// 재귀 케이스: 첫 인자 처리 후 나머지 재귀
template<typename First, typename... Rest>
void print_recursive(First first, Rest... rest) {
    std::cout << first << " ";
    print_recursive(rest...);
}

// ─── 방법 B: 폴드 표현식 (C++17 스타일) ──────────────────────

template<typename... Args>
void print_fold(Args&&... args) {
    ((std::cout << args << " "), ...);
    std::cout << "\n";
}

int main() {
    print_recursive(1, 2.5, "hello");
    std::cout << "\n";

    print_fold(1, 2.5, "hello");
}
```

**두 방법의 특성 비교:**

```
┌────────────────────┬──────────────────────┬──────────────────────┐
│  비교 항목         │  재귀 방식           │  폴드 표현식         │
├────────────────────┼──────────────────────┼──────────────────────┤
│  C++ 버전          │  C++11 이상          │  C++17 이상          │
│  코드 길이         │  길다 (함수 2개)     │  짧다 (함수 1개)     │
│  가독성            │  직관적 (단계별)     │  선언적 (의도 명확)  │
│  컴파일 인스턴스   │  N+1개 함수 생성     │  최적화 가능         │
│  원소별 로직 차이  │  쉽다 (first/rest)  │  어렵다              │
│  복잡한 처리       │  적합               │  단순 연산에 적합    │
└────────────────────┴──────────────────────┴──────────────────────┘
```

**재귀가 여전히 필요한 경우:**

모든 상황에서 폴드 표현식이 우월한 것은 아니다. 원소마다 **다른 처리**를 해야 하거나, **이전 원소의 결과**를 다음 원소 처리에 활용해야 할 때는 재귀가 더 자연스럽다.

```cpp
#include <iostream>
#include <string>

// 원소를 인덱스와 함께 출력 — 재귀가 더 자연스러운 경우
template<std::size_t Index = 0, typename First, typename... Rest>
void print_indexed(First first, Rest... rest) {
    std::cout << "[" << Index << "] " << first << "\n";
    if constexpr (sizeof...(rest) > 0) {
        print_indexed<Index + 1>(rest...);
    }
}

int main() {
    print_indexed(10, "hello", 3.14);
    // [0] 10
    // [1] hello
    // [2] 3.14
}
```

**C++20 이후의 관용구 — 람다와 쉼표 폴드:**

C++20에서는 람다의 표현력이 향상되어, 아래처럼 인덱스까지 활용하는 관용구도 가능하다.

```cpp
#include <iostream>
#include <utility>

template<typename... Args>
void print_indexed_fold(Args&&... args) {
    std::size_t i = 0;
    // 쉼표 폴드 안에서 람다를 즉시 호출 (IIFE 패턴)
    ([&]{ std::cout << "[" << i++ << "] " << args << "\n"; }(), ...);
}

int main() {
    print_indexed_fold(10, "hello", 3.14);
    // [0] 10
    // [1] hello
    // [2] 3.14
}
```

이처럼 "단순 반복"은 폴드 표현식이 담당하고, "복잡한 단계별 처리"는 재귀가 담당하는 역할 분담이 현대 C++ 가변 인자 템플릿 프로그래밍의 기본 원칙이다.

---

## 🛠 실습: `print_all`, `type_list`, 타입 안전 `tuple_apply` 만들기

이제 이 챕터에서 배운 개념들을 종합하여 실용적인 코드를 작성해보자.

---

**실습 ①: `print_all` — 구분자를 지정할 수 있는 출력 함수**

단순히 출력만 하는 것에서 한 발짝 나아가, 원소 사이에 **구분자(delimiter)** 를 지정할 수 있는 버전을 만들어보자.

```cpp
#include <iostream>
#include <string_view>

// 구분자를 받아서 원소 사이에 삽입하며 출력
template<typename... Args>
void print_all(std::string_view delimiter, Args&&... args) {
    // sizeof...(args)가 0이면 아무것도 출력 안 함
    if constexpr (sizeof...(Args) == 0) return;

    std::size_t count = 0;
    const std::size_t total = sizeof...(Args);

    ([&]{
        std::cout << args;
        if (++count < total) std::cout << delimiter;
    }(), ...);

    std::cout << "\n";
}

int main() {
    print_all(", ", 1, 2.5, "hello", 'A');
    // 출력: 1, 2.5, hello, A

    print_all(" | ", 10, 20, 30);
    // 출력: 10 | 20 | 30

    print_all("-");       // 인자 없음: 아무것도 출력 안 함

    print_all(" ", "C++", "Variadic", "Templates", "are", "fun!");
    // 출력: C++ Variadic Templates are fun!
}
```

---

**실습 ②: `TypeList` — 컴파일 타임 타입 목록**

타입 목록 자체를 하나의 타입으로 표현하는 `TypeList`는 고급 메타프로그래밍의 기초다. 여기서는 기본 구조와 몇 가지 유용한 연산을 구현해본다.

```cpp
#include <iostream>
#include <type_traits>

// ─── TypeList: 타입들을 담는 컴파일 타임 컨테이너 ──────────────

template<typename... Types>
struct TypeList {
    // 목록의 크기
    static constexpr std::size_t size = sizeof...(Types);

    // 비어있는지 확인
    static constexpr bool empty = (size == 0);
};

// ─── TypeList 연산: 크기 조회 ──────────────────────────────────

template<typename List>
struct Size;

template<typename... Types>
struct Size<TypeList<Types...>> {
    static constexpr std::size_t value = sizeof...(Types);
};

template<typename List>
constexpr std::size_t size_v = Size<List>::value;

// ─── TypeList 연산: 두 리스트 합치기 ──────────────────────────

template<typename List1, typename List2>
struct Concat;

template<typename... Types1, typename... Types2>
struct Concat<TypeList<Types1...>, TypeList<Types2...>> {
    using type = TypeList<Types1..., Types2...>;
};

template<typename L1, typename L2>
using concat_t = typename Concat<L1, L2>::type;

// ─── TypeList 연산: 특정 타입 포함 여부 확인 ──────────────────

template<typename T, typename List>
struct Contains;

template<typename T, typename... Types>
struct Contains<T, TypeList<Types...>> {
    // 폴드 표현식으로 OR 연산
    static constexpr bool value = (std::is_same_v<T, Types> || ...);
};

template<typename T, typename List>
constexpr bool contains_v = Contains<T, List>::value;

// ─── TypeList 이름 출력 (데모용) ───────────────────────────────

template<typename... Types>
void print_type_names(TypeList<Types...>) {
    std::size_t i = 0;
    ([&]{
        // __FUNCSIG__ 대신 typeid 활용 (Visual Studio 호환)
        if (i++ > 0) std::cout << ", ";
        std::cout << typeid(Types).name();
    }(), ...);
    std::cout << "\n";
}

int main() {
    using IntTypes   = TypeList<int, long, long long>;
    using FloatTypes = TypeList<float, double, long double>;
    using AllTypes   = concat_t<IntTypes, FloatTypes>;

    std::cout << "IntTypes 크기: "  << size_v<IntTypes>  << "\n"; // 3
    std::cout << "FloatTypes 크기: " << size_v<FloatTypes> << "\n"; // 3
    std::cout << "AllTypes 크기: "  << size_v<AllTypes>  << "\n"; // 6

    std::cout << "int 포함 여부: "
              << contains_v<int, IntTypes> << "\n";        // 1
    std::cout << "double 포함 여부: "
              << contains_v<double, IntTypes> << "\n";     // 0

    std::cout << "AllTypes: ";
    print_type_names(AllTypes{});
}
```

---

**실습 ③: 타입 안전 `tuple_apply` — 튜플의 모든 원소에 함수 적용**

`std::apply`는 표준 라이브러리에 이미 있지만, 직접 구현해보는 것은 `std::index_sequence`와 팩 확장을 이해하는 최고의 실습이다.

```cpp
#include <iostream>
#include <tuple>
#include <utility>
#include <string>

// ─── 구현 내부: index_sequence를 이용한 튜플 언패킹 ────────────

// 헬퍼: std::index_sequence를 받아서 팩 확장으로 튜플 원소를 펼침
template<typename Func, typename Tuple, std::size_t... Is>
decltype(auto) apply_impl(Func&& f, Tuple&& t,
                          std::index_sequence<Is...>) {
    // std::get<0>(t), std::get<1>(t), ... 로 팩 확장
    return std::forward<Func>(f)(std::get<Is>(std::forward<Tuple>(t))...);
}

// ─── 공개 인터페이스 ────────────────────────────────────────────

template<typename Func, typename Tuple>
decltype(auto) my_apply(Func&& f, Tuple&& t) {
    // 튜플 크기만큼 index_sequence 생성: {0, 1, 2, ..., N-1}
    constexpr std::size_t N = std::tuple_size_v<std::decay_t<Tuple>>;
    return apply_impl(std::forward<Func>(f),
                      std::forward<Tuple>(t),
                      std::make_index_sequence<N>{});
}

// ─── 보너스: 튜플의 모든 원소를 변환하여 새 튜플 반환 ──────────

template<typename Func, typename Tuple, std::size_t... Is>
auto transform_tuple_impl(Func&& f, Tuple&& t,
                           std::index_sequence<Is...>) {
    return std::make_tuple(f(std::get<Is>(std::forward<Tuple>(t)))...);
}

template<typename Func, typename Tuple>
auto transform_tuple(Func&& f, Tuple&& t) {
    constexpr std::size_t N = std::tuple_size_v<std::decay_t<Tuple>>;
    return transform_tuple_impl(std::forward<Func>(f),
                                std::forward<Tuple>(t),
                                std::make_index_sequence<N>{});
}

int main() {
    // ── my_apply 테스트 ──────────────────────────────────────────
    auto t1 = std::make_tuple(1, 2, 3);

    int result = my_apply([](int a, int b, int c) {
        return a + b + c;
    }, t1);
    std::cout << "합계: " << result << "\n";  // 합계: 6

    // 이질적 타입 튜플
    auto t2 = std::make_tuple(42, std::string("hello"), 3.14);

    my_apply([](auto a, auto b, auto c) {
        std::cout << a << ", " << b << ", " << c << "\n";
    }, t2);  // 42, hello, 3.14

    // ── transform_tuple 테스트 ───────────────────────────────────
    auto t3 = std::make_tuple(1, 2, 3, 4, 5);

    // 각 원소를 2배로
    auto doubled = transform_tuple([](auto x) { return x * 2; }, t3);
    std::apply([](auto... xs) {
        ((std::cout << xs << " "), ...);
    }, doubled);
    std::cout << "\n";  // 2 4 6 8 10
}
```

이 구현에서 핵심은 `std::make_index_sequence<N>`이다. 이것은 `{0, 1, 2, ..., N-1}`이라는 정수 팩을 담은 `std::index_sequence<0, 1, 2, ..., N-1>` 타입을 생성하며, 이를 함수 파라미터로 받아서 `std::get<Is>...`라는 팩 확장으로 튜플의 모든 원소를 한꺼번에 펼칠 수 있다.

```
std::make_index_sequence<3>
        │
        ▼
std::index_sequence<0, 1, 2>   ← 타입으로 {0,1,2} 팩을 담아 전달
        │
        ▼  (패턴: std::get<Is>(t))
std::get<0>(t), std::get<1>(t), std::get<2>(t)  ← 팩 확장 결과
```

---

**📌 이 챕터의 핵심 요약:**

이번 챕터에서 다룬 내용을 한 장으로 정리하면 다음과 같다.

```
가변 인자 템플릿의 큰 그림
─────────────────────────────────────────────────────────
① 파라미터 팩 선언     template<typename... Ts>   ← "..."가 이름 앞
                        void f(Ts... args)

② 팩 확장              f(args...)                 ← "..."가 이름 뒤
                        pattern...                ← 패턴 전체가 반복됨

③ sizeof...            sizeof...(args)            ← 컴파일 타임 크기

④ 폴드 표현식 (C++17)
   단항 좌측:  (... op pack)    → ((E1 op E2) op E3)
   단항 우측:  (pack op ...)    → E1 op (E2 op E3)
   이항 좌측:  (I op ... op pack)
   이항 우측:  (pack op ... op I)

⑤ 재귀 vs. 폴드
   단순 누적/반복  → 폴드 표현식 선호 (C++17+)
   원소별 다른 처리 → 재귀 또는 람다+쉼표폴드
─────────────────────────────────────────────────────────
```

가변 인자 템플릿은 처음 접할 때 `...`의 위치가 낯설게 느껴지지만, **선언할 때는 이름 앞에, 확장할 때는 이름 뒤에** 붙는다는 규칙 하나만 기억하면 의외로 단순하다. 다음 챕터에서는 이 가변 인자 템플릿과 함께 자주 등장하는 `auto`, `decltype`, 완벽 전달 패턴을 깊이 있게 살펴볼 것이다.




# Chapter 8. 템플릿과 `auto` — 타입 추론의 심화

---

> **이 챕터에서 배울 것**
>
> 템플릿 프로그래밍에서 `auto`와 그 친척들(`decltype`, `decltype(auto)`)은 단순한 편의 문법이 아니다. 이들은 C++ 타입 시스템의 정밀한 규칙 위에 서 있으며, 잘못 이해하면 미묘한 버그의 원인이 된다. 특히 **보편 참조(Universal Reference)** 와 **완벽 전달(Perfect Forwarding)** 은 "왜 `T&&`인데 lvalue도 받나요?"라는 질문처럼, 처음 보면 마법처럼 느껴지는 C++의 핵심 메커니즘이다. 이 챕터에서는 이 메커니즘 전체를 시각적으로, 단계적으로 해부한다.

---

## 8.1 `auto`, `decltype`, `decltype(auto)` 차이 완벽 정리

세 키워드는 모두 타입을 자동으로 결정하지만, 적용하는 **규칙이 완전히 다르다**. 이 차이를 이해하는 것이 이 챕터의 출발점이다.

**`auto` — 템플릿 타입 추론 규칙을 그대로 따른다:**

`auto`가 타입을 추론하는 방식은 함수 템플릿에서 `T`를 추론하는 방식과 동일하다. 가장 중요한 특징은 **참조와 최상위 cv 한정자(const/volatile)를 제거한다**는 것이다.

```cpp
#include <iostream>
#include <typeinfo>

int  x     = 42;
int& ref_x = x;
const int cx = 100;

auto a = x;      // int       (값 복사)
auto b = ref_x;  // int       (참조 제거! int& 아님)
auto c = cx;     // int       (const 제거! const int 아님)

auto& d = x;     // int&      (& 를 직접 붙여야 참조)
const auto& e = x; // const int& (const&로 명시해야 const 참조)
auto&& f = x;   // int&      (보편 참조 — 8.2에서 설명)
```

```
┌─────────────────────────────────────────────────────────────┐
│  auto 추론 규칙: "초기화 식의 참조와 최상위 const를 벗겨낸다" │
├─────────────────┬───────────────────────────────────────────┤
│  초기화 식      │  auto로 추론된 타입                        │
├─────────────────┼───────────────────────────────────────────┤
│  int            │  int                                       │
│  int&           │  int    ← 참조 제거                        │
│  const int      │  int    ← const 제거                       │
│  const int&     │  int    ← 둘 다 제거                       │
│  int&&          │  int    ← 참조 제거                        │
│  int*           │  int*   ← 포인터는 그대로                  │
│  const int*     │  const int* ← 하위 const는 유지            │
└─────────────────┴───────────────────────────────────────────┘
```

**`decltype` — 표현식의 선언 타입을 그대로 반환한다:**

`decltype`은 `auto`와 반대다. 참조도, const도 절대 벗겨내지 않는다. 게다가 표현식을 괄호로 한 번 더 감싸면 결과가 달라지는 특별한 규칙도 있다.

```cpp
int  x  = 42;
int& rx = x;
const int cx = 100;

// ── 변수에 적용 ────────────────────────────────────────────
decltype(x)   d1 = x;   // int       (선언 타입 그대로)
decltype(rx)  d2 = x;   // int&      (참조 유지!)
decltype(cx)  d3 = cx;  // const int (const 유지!)

// ── 표현식에 적용 ──────────────────────────────────────────
decltype(x + 1) d4 = 0; // int       (산술 식 → rvalue → 값 타입)
decltype(x = 1) d5 = x; // int&      (대입 식 → lvalue → 참조 타입)

// ── 괄호의 마법 ────────────────────────────────────────────
decltype(x)    d6 = x;  // int       (변수 이름 → 선언 타입)
decltype((x))  d7 = x;  // int&      (괄호 → lvalue 표현식 → 참조!)
//        ^^^
//        괄호 하나가 결과를 바꾼다!
```

`decltype((x))`가 `int&`인 이유는 괄호로 감싸면 "변수 이름"이 아닌 "lvalue 표현식"으로 해석되기 때문이다. `decltype`은 lvalue 표현식에 대해 항상 lvalue 참조를 반환한다.

```
┌──────────────────────────────────────────────────────────────┐
│  decltype 규칙 요약                                           │
├──────────────────────────────────────────────────────────────┤
│  decltype(변수이름)  → 그 변수의 선언 타입 (참조/const 유지)  │
│  decltype(lvalue식) → T&   (lvalue 표현식 → lvalue 참조)     │
│  decltype(xvalue식) → T&&  (xvalue 표현식 → rvalue 참조)     │
│  decltype(prvalue식)→ T    (순수 rvalue → 값 타입)           │
└──────────────────────────────────────────────────────────────┘
```

**`decltype(auto)` — decltype 규칙으로 추론하되, 초기화 식을 기준으로:**

`decltype(auto)`는 C++14에 도입된 키워드 조합이다. `auto`처럼 초기화 식으로부터 타입을 추론하되, `auto`의 "벗겨내기" 규칙 대신 `decltype`의 "그대로 유지" 규칙을 적용한다. 주로 **함수의 반환 타입을 정확하게 전달**할 때 사용한다.

```cpp
int  x  = 42;
int& rx = x;

auto         a = rx;  // int    ← auto는 참조를 벗겨냄
decltype(auto) b = rx;  // int& ← decltype 규칙: 참조 유지!

// 함수 반환 타입에서의 차이 ─────────────────────────────────

int& get_ref() { return x; }
int  get_val() { return x; }

auto          r1 = get_ref(); // int    (참조 제거: 복사본 생성)
decltype(auto) r2 = get_ref(); // int& (참조 유지: 원본을 가리킴)

auto          r3 = get_val(); // int
decltype(auto) r4 = get_val(); // int  (rvalue이므로 동일)
```

세 키워드의 차이를 한눈에 비교해보자.

```
┌────────────────┬─────────────────┬──────────────────────────────┐
│  키워드        │  규칙           │  주 사용처                    │
├────────────────┼─────────────────┼──────────────────────────────┤
│  auto          │  템플릿 추론    │  일반 변수, 루프, 람다 등     │
│                │  (벗겨내기)     │  타입이 길 때 생략 목적       │
├────────────────┼─────────────────┼──────────────────────────────┤
│  decltype(e)   │  표현식 타입    │  타입을 정확하게 쿼리할 때    │
│                │  (그대로)       │  반환 타입 후행 선언 등       │
├────────────────┼─────────────────┼──────────────────────────────┤
│  decltype(auto)│  decltype 규칙  │  반환 타입 완벽 전달          │
│                │  + 초기화 식    │  제네릭 래퍼 함수             │
└────────────────┴─────────────────┴──────────────────────────────┘
```

**실용 예제 — 제네릭 래퍼에서의 반환 타입 전달:**

```cpp
#include <utility>

// ❌ auto: 참조가 사라져 버린다
template<typename F, typename... Args>
auto call_wrong(F&& f, Args&&... args) {
    return f(std::forward<Args>(args)...);
    // 반환이 int&이어도 int로 복사된다
}

// ✅ decltype(auto): 반환 타입을 정확하게 전달
template<typename F, typename... Args>
decltype(auto) call_correct(F&& f, Args&&... args) {
    return f(std::forward<Args>(args)...);
    // 반환이 int&이면 int&로, int이면 int로 전달
}

int g_value = 42;
int& get_ref() { return g_value; }

int main() {
    auto          r1 = call_wrong(get_ref);   // int (복사)
    decltype(auto) r2 = call_correct(get_ref); // int& (참조 유지)

    r2 = 100;  // g_value가 100으로 바뀐다
}
```

---

## 8.2 보편 참조(Universal Reference)와 `T&&`

**`&&`는 항상 rvalue 참조가 아니다:**

C++에서 `&&`는 두 가지 완전히 다른 의미를 가진다. 어떤 문맥에서 등장하느냐에 따라 **rvalue 참조**이기도 하고, **보편 참조(Universal Reference)** 이기도 하다. Scott Meyers가 붙인 이 이름은 C++17 표준에서 **전달 참조(Forwarding Reference)** 라는 공식 이름을 얻었다.

```cpp
// ① rvalue 참조: T가 이미 확정된 구체 타입일 때
void func(int&& x);           // ← int rvalue만 받음
std::string&& s = std::string("hello");  // ← rvalue 참조

// ② 보편 참조: T가 추론되어야 하는 템플릿 파라미터일 때
template<typename T>
void func(T&& x);             // ← lvalue도, rvalue도 받음 (보편 참조!)

auto&& y = something;         // ← auto도 추론 대상이므로 보편 참조
```

보편 참조의 핵심 조건은 **타입 추론이 발생하는 `T&&` 형태**여야 한다는 것이다. `const T&&`나 `std::vector<T>&&`처럼 변형된 형태는 보편 참조가 아니다.

```
┌──────────────────────────────────────────────────────────────┐
│  보편 참조 vs. rvalue 참조 구분                               │
├──────────────────────────────────────────────────────────────┤
│  template<typename T>                                        │
│  void f(T&& x);      → ✅ 보편 참조 (T가 추론됨)             │
│                                                              │
│  template<typename T>                                        │
│  void g(const T&& x); → ❌ rvalue 참조 (const 때문에)        │
│                                                              │
│  template<typename T>                                        │
│  void h(std::vector<T>&& x); → ❌ rvalue 참조 (T가 고정됨)   │
│                                                              │
│  auto&& y = expr;    → ✅ 보편 참조 (auto가 추론됨)           │
└──────────────────────────────────────────────────────────────┘
```

**보편 참조가 작동하는 원리 — 참조 붕괴(Reference Collapsing):**

보편 참조가 lvalue도, rvalue도 받을 수 있는 비밀은 **참조 붕괴(Reference Collapsing)** 규칙에 있다. C++에서는 "참조의 참조"를 직접 만들 수 없지만, 템플릿 인스턴스화 과정에서 이것이 발생할 수 있다. 이때 다음 규칙으로 하나의 참조 타입으로 **붕괴**된다.

```
┌─────────────────────────────────────────────────────────┐
│  참조 붕괴(Reference Collapsing) 규칙                   │
│                                                         │
│  T& &   → T&     (& & → &)                             │
│  T& &&  → T&     (& && → &)   ← lvalue 전달 시 이 경우  │
│  T&& &  → T&     (&& & → &)                            │
│  T&& && → T&&    (&& && → &&) ← rvalue 전달 시 이 경우  │
│                                                         │
│  기억법: & 가 하나라도 있으면 &, 둘 다 && 여야 &&        │
└─────────────────────────────────────────────────────────┘
```

이 규칙이 `template<typename T> void f(T&& x)`에 적용되는 과정을 살펴보자.

```
lvalue int 전달 시:
  f(my_int)에서 my_int는 lvalue
  → T는 int& 로 추론됨
  → T&&는 (int&)&& → int& (붕괴!)
  → 결국 f(int& x)처럼 동작 ← lvalue 참조!

rvalue int 전달 시:
  f(42)에서 42는 rvalue
  → T는 int 로 추론됨 (참조 없이)
  → T&&는 int&& (붕괴 없음)
  → 결국 f(int&& x)처럼 동작 ← rvalue 참조!
```

```cpp
#include <iostream>
#include <typeinfo>

template<typename T>
void show_ref_type(T&& x) {
    if constexpr (std::is_lvalue_reference_v<T>) {
        std::cout << "T = lvalue ref → 보편 참조는 lvalue 참조\n";
    } else {
        std::cout << "T = non-ref   → 보편 참조는 rvalue 참조\n";
    }
}

int main() {
    int a = 10;
    show_ref_type(a);    // T = int&  → lvalue 참조
    show_ref_type(42);   // T = int   → rvalue 참조
    show_ref_type(std::move(a)); // T = int → rvalue 참조
}
```

---

## 8.3 완벽 전달(Perfect Forwarding): `std::forward<T>`

**"완벽 전달"이 필요한 이유:**

함수가 인자를 받아서 다른 함수에 그대로 넘겨주는 **래퍼(wrapper) 함수**를 만들 때 문제가 생긴다. 함수 파라미터는 이름이 있는 순간 **lvalue**가 되어버리기 때문이다.

```cpp
#include <iostream>
#include <string>

void process(std::string& s)  { std::cout << "lvalue 처리: " << s << "\n"; }
void process(std::string&& s) { std::cout << "rvalue 처리: " << s << "\n"; }

// ❌ 잘못된 래퍼: 항상 lvalue로 전달된다
template<typename T>
void bad_wrapper(T&& arg) {
    process(arg);   // arg는 이름이 있으므로 lvalue!
}

// ✅ 완벽 전달: 원래의 값 카테고리를 보존한다
template<typename T>
void good_wrapper(T&& arg) {
    process(std::forward<T>(arg)); // T에 따라 lvalue/rvalue 결정
}

int main() {
    std::string s = "hello";

    bad_wrapper(s);            // lvalue 처리 (ok)
    bad_wrapper(std::string("world")); // lvalue 처리 ← 잘못됨!

    good_wrapper(s);           // lvalue 처리 (ok)
    good_wrapper(std::string("world")); // rvalue 처리 ← 올바름!
}
```

**`std::forward`의 작동 원리:**

`std::forward<T>`는 T가 무엇이냐에 따라 캐스팅 방향을 결정한다. 사실 내부적으로는 매우 단순한 조건부 캐스팅이다.

```
std::forward<T>(arg)의 동작:

  T가 lvalue 참조 타입(T = int&)일 때:
    → static_cast<int&>(arg)  ← lvalue로 캐스팅
    → 결과: lvalue

  T가 비참조 타입(T = int)일 때:
    → static_cast<int&&>(arg) ← rvalue로 캐스팅
    → 결과: rvalue
```

```cpp
// std::forward의 단순화된 구현 (이해 목적)
template<typename T>
T&& forward(std::remove_reference_t<T>& arg) noexcept {
    return static_cast<T&&>(arg);
}
// T = int&  → 반환: int& &&  → 붕괴 → int&   (lvalue)
// T = int   → 반환: int&&               (rvalue)
```

**완벽 전달의 전체 흐름 시각화:**

```
호출자                래퍼 함수 (good_wrapper)         목적 함수 (process)
─────────────────────────────────────────────────────────────────────
good_wrapper(s)    →  T = int& (lvalue이므로)
                      arg: int& 타입
                      forward<int&>(arg)
                      = static_cast<int&>(arg)
                                                   →  process(int& s)

good_wrapper(42)   →  T = int (rvalue이므로)
                      arg: int&& 타입 (이름있으면 lvalue지만)
                      forward<int>(arg)
                      = static_cast<int&&>(arg)
                                                   →  process(int&& s)
```

**가변 인자와 완벽 전달 — 실전에서 가장 흔한 패턴:**

완벽 전달은 가변 인자 템플릿과 함께 쓰일 때 진짜 위력을 발휘한다. `std::make_unique`, `std::make_shared`, `std::vector::emplace_back`이 모두 이 패턴을 쓴다.

```cpp
#include <memory>
#include <iostream>
#include <string>

struct Widget {
    Widget(int id, std::string name) {
        std::cout << "Widget(" << id << ", " << name << ") 생성\n";
    }
};

// make_unique와 동일한 원리
template<typename T, typename... Args>
std::unique_ptr<T> my_make_unique(Args&&... args) {
    return std::unique_ptr<T>(new T(std::forward<Args>(args)...));
    //                                  ^^^^^^^^^^^^^^^^^^^^^^^^^^^
    //                                  각 인자의 값 카테고리 보존
}

int main() {
    auto w = my_make_unique<Widget>(42, std::string("MyWidget"));
    // Widget(42, MyWidget) 생성 — std::string이 복사 없이 이동됨
}
```

---

## 8.4 축약 함수 템플릿(Abbreviated Function Template): `auto` 파라미터 (C++20)

C++14에서 람다에 `auto` 파라미터가 허용되었을 때, 개발자들은 자연스럽게 일반 함수에도 같은 문법이 있으면 좋겠다고 생각했다. C++20은 이 요청을 받아들였다. **축약 함수 템플릿(Abbreviated Function Template)** 은 `auto`를 함수 파라미터 타입으로 사용하여 `template<typename T>` 선언을 생략하는 문법이다.

**기존 방식과 축약 방식 비교:**

```cpp
// ─── C++17 방식: 명시적 template 선언 필요 ────────────────────

template<typename T>
void print_v1(const T& value) {
    std::cout << value << "\n";
}

template<typename T, typename U>
auto add_v1(T a, U b) {
    return a + b;
}

// ─── C++20 축약 방식: auto로 간결하게 ────────────────────────

void print_v2(const auto& value) {        // ← template<typename T> 생략
    std::cout << value << "\n";
}

auto add_v2(auto a, auto b) {             // ← 각 auto마다 별도의 T, U
    return a + b;
}
```

두 방식은 **의미적으로 동일**하다. 컴파일러는 `auto` 파라미터 하나마다 독립적인 템플릿 파라미터를 자동으로 생성한다.

```
void print_v2(const auto& value)
     ↕ 컴파일러가 내부적으로 변환
template<typename __T1>
void print_v2(const __T1& value)

auto add_v2(auto a, auto b)
     ↕ 컴파일러가 내부적으로 변환
template<typename __T1, typename __T2>
auto add_v2(__T1 a, __T2 b)
```

**축약 함수 템플릿과 보편 참조:**

```cpp
// 보편 참조도 축약 문법으로 쓸 수 있다
void forward_demo(auto&& value) {
    // value는 보편 참조
    // 단, forward 시에는 decltype(value)를 써야 한다
    some_function(std::forward<decltype(value)>(value));
}

// 주의: template<typename T>와 달리 T를 명시적으로 알 수 없으므로
// std::forward<T> 대신 std::forward<decltype(value)>를 사용한다
```

**`auto&&`와 `decltype` — 축약 템플릿에서 완벽 전달:**

명시적 템플릿에서는 `T`를 직접 알 수 있어서 `std::forward<T>`를 쓸 수 있었다. 축약 템플릿에서는 `T`가 이름이 없으므로 `decltype(param)`으로 해당 타입을 얻어야 한다.

```cpp
#include <utility>
#include <iostream>
#include <string>

void sink(std::string& s)  { std::cout << "lvalue: " << s << "\n"; }
void sink(std::string&& s) { std::cout << "rvalue: " << s << "\n"; }

// 명시적 템플릿 방식
template<typename T>
void relay_explicit(T&& val) {
    sink(std::forward<T>(val));      // T를 직접 사용
}

// 축약 템플릿 방식
void relay_abbreviated(auto&& val) {
    sink(std::forward<decltype(val)>(val)); // decltype으로 타입 획득
}

int main() {
    std::string s = "hello";
    relay_abbreviated(s);                   // lvalue: hello
    relay_abbreviated(std::string("world")); // rvalue: world
}
```

**`auto` 파라미터와 가변 인자 결합:**

```cpp
#include <iostream>

// auto 가변 파라미터 — C++20
void print_all(const auto&... args) {
    ((std::cout << args << " "), ...);
    std::cout << "\n";
}

int main() {
    print_all(1, 2.5, "hello", 'A');
    // 출력: 1 2.5 hello A
}
```

---

## 8.5 `auto` 파라미터와 Concept 결합

축약 함수 템플릿의 `auto`에 Concept(Chapter 6 참고)을 붙이면, 짧고 의도가 명확한 제약된 함수 템플릿을 만들 수 있다. 이것을 **제약된 auto(Constrained Auto)** 라고 한다.

**기본 문법 — `Concept auto`:**

```cpp
#include <concepts>
#include <iostream>

// ─── 기존 방식 (C++20 전) ─────────────────────────────────────

template<std::integral T>
void print_int_old(T val) {
    std::cout << "정수: " << val << "\n";
}

// ─── 제약된 auto (C++20) ──────────────────────────────────────

void print_int(std::integral auto val) {
    //         ^^^^^^^^^^^^^^^^
    //         "auto이되 std::integral을 만족하는 타입만"
    std::cout << "정수: " << val << "\n";
}

void print_float(std::floating_point auto val) {
    std::cout << "실수: " << val << "\n";
}

int main() {
    print_int(42);          // ✅ int는 integral
    print_int(3L);          // ✅ long도 integral
    // print_int(3.14);     // ❌ 컴파일 오류: double은 integral 아님

    print_float(3.14);      // ✅ double은 floating_point
    // print_float(42);     // ❌ 컴파일 오류
}
```

**제약된 auto와 참조, const 결합:**

`Concept auto`에 `&`, `&&`, `const`를 자유롭게 조합할 수 있다.

```cpp
#include <concepts>
#include <ranges>
#include <iostream>
#include <vector>
#include <string>

// const 참조로 받기
void show(const std::ranges::range auto& container) {
    for (const auto& elem : container) {
        std::cout << elem << " ";
    }
    std::cout << "\n";
}

// 보편 참조로 받기 (완벽 전달용)
void process(std::integral auto&& val) {
    std::cout << "처리: " << val << "\n";
}

int main() {
    std::vector<int> v = {1, 2, 3, 4, 5};
    std::string      s = "hello";

    show(v);  // 1 2 3 4 5
    show(s);  // h e l l o

    int x = 42;
    process(x);    // lvalue
    process(100);  // rvalue
}
```

**커스텀 Concept과 auto 파라미터 결합:**

```cpp
#include <concepts>
#include <iostream>
#include <string>

// 커스텀 Concept 정의
template<typename T>
concept Printable = requires(T t) {
    { std::cout << t } -> std::same_as<std::ostream&>;
};

template<typename T>
concept Addable = requires(T a, T b) {
    { a + b } -> std::convertible_to<T>;
};

// Concept + auto 결합으로 간결한 제약 함수 생성
void print_value(Printable auto const& val) {
    std::cout << val << "\n";
}

auto double_it(Addable auto val) {
    return val + val;
}

int main() {
    print_value(42);            // 42
    print_value(3.14);          // 3.14
    print_value(std::string("hi")); // hi

    std::cout << double_it(21)  << "\n"; // 42
    std::cout << double_it(1.5) << "\n"; // 3
    std::cout << double_it(std::string("ha")) << "\n"; // haha
}
```

**같은 함수를 네 가지 방법으로 쓰기 — 진화의 역사:**

Concept과 auto가 어떻게 코드를 진화시켜 왔는지 하나의 예제로 살펴보자.

```cpp
#include <concepts>
#include <iostream>

// ① C++17: enable_if (구시대적, 읽기 어려움)
template<typename T, typename = std::enable_if_t<std::is_arithmetic_v<T>>>
T square_v1(T x) { return x * x; }

// ② C++20: concept + 명시적 template
template<std::floating_point T>
T square_v2(T x) { return x * x; }

// ③ C++20: requires 절
template<typename T>
    requires std::floating_point<T>
T square_v3(T x) { return x * x; }

// ④ C++20: 제약된 auto (가장 간결)
std::floating_point auto square_v4(std::floating_point auto x) {
    return x * x;
}
// 반환 타입도 Concept으로 제약 가능! ──────────────────────────────────^

int main() {
    std::cout << square_v4(3.0)  << "\n"; // 9
    std::cout << square_v4(2.5f) << "\n"; // 6.25
    // square_v4(3);  // ❌ int는 floating_point 아님
}
```

반환 타입에도 `Concept auto`를 사용할 수 있다는 점이 특히 강력하다. `std::floating_point auto square_v4(...)`는 "이 함수가 반환하는 타입은 반드시 `std::floating_point`를 만족해야 한다"는 계약이 된다. 만약 구현 내부에서 실수로 `int`를 반환하면 컴파일 오류가 발생한다.

---

## 🛠 실습: 완벽 전달 래퍼 함수, `make_*` 스타일 팩토리 함수 만들기

이 챕터에서 배운 `auto`, `decltype(auto)`, 보편 참조, 완벽 전달, 제약된 auto를 모두 활용하는 실습이다.

---

**실습 ①: `invoke_and_log` — 완벽 전달 래퍼**

함수를 호출하기 전후로 로그를 출력하되, 인자와 반환값의 값 카테고리를 완벽하게 보존하는 래퍼를 만들어보자.

```cpp
#include <iostream>
#include <utility>
#include <string>
#include <chrono>

// ─── 완벽 전달 래퍼: 함수 호출을 감싸서 로그를 남긴다 ─────────

template<typename F, typename... Args>
decltype(auto) invoke_and_log(std::string_view name, F&& func, Args&&... args) {
    std::cout << "[LOG] " << name << " 호출 시작\n";

    // decltype(auto): 반환 타입(참조/값)을 그대로 전달
    decltype(auto) result = std::forward<F>(func)(
        std::forward<Args>(args)...  // 각 인자의 값 카테고리 보존
    );

    std::cout << "[LOG] " << name << " 호출 완료\n";
    return result;  // decltype(auto)이므로 참조도 그대로 반환
}

// ─── 테스트용 함수들 ────────────────────────────────────────────

int add(int a, int b) {
    return a + b;
}

std::string greet(std::string name) {
    return "Hello, " + name + "!";
}

int g_counter = 0;
int& get_counter() { return g_counter; }  // 참조 반환 함수

int main() {
    // 일반 함수 호출
    auto sum = invoke_and_log("add", add, 10, 20);
    std::cout << "결과: " << sum << "\n\n";

    // 문자열 이동
    auto msg = invoke_and_log("greet", greet, std::string("World"));
    std::cout << "결과: " << msg << "\n\n";

    // 참조 반환 함수 — decltype(auto) 덕분에 참조가 유지됨
    decltype(auto) ref = invoke_and_log("get_counter", get_counter);
    ref = 42;  // g_counter가 수정됨
    std::cout << "g_counter = " << g_counter << "\n";
}
```

---

**실습 ②: `make_*` 스타일 팩토리 함수**

C++ 표준 라이브러리의 `std::make_unique`, `std::make_shared` 패턴을 직접 구현하고, 거기에 Concept 제약까지 추가해보자.

```cpp
#include <memory>
#include <concepts>
#include <iostream>
#include <string>

// ─── Concept: 기본 생성자가 있는 타입 ─────────────────────────

template<typename T>
concept DefaultConstructible = std::is_default_constructible_v<T>;

// ─── 범용 팩토리 함수 ─────────────────────────────────────────

// 임의 타입의 unique_ptr 생성 (완벽 전달 사용)
template<typename T, typename... Args>
std::unique_ptr<T> make_unique_ptr(Args&&... args) {
    return std::make_unique<T>(std::forward<Args>(args)...);
}

// 기본 생성 가능한 타입만 허용하는 팩토리
template<DefaultConstructible T>
std::unique_ptr<T> make_default() {
    return std::make_unique<T>();
}

// ─── 테스트용 클래스 ───────────────────────────────────────────

struct Connection {
    std::string host;
    int port;

    Connection(std::string h, int p)
        : host(std::move(h)), port(p) {
        std::cout << "Connection(" << host << ":" << port << ") 생성\n";
    }
    ~Connection() {
        std::cout << "Connection(" << host << ":" << port << ") 소멸\n";
    }
};

struct Config {
    Config() { std::cout << "Config 기본 생성\n"; }
};

int main() {
    // 완벽 전달: std::string은 이동, int는 값으로 전달
    auto conn = make_unique_ptr<Connection>(std::string("localhost"), 8080);
    std::cout << conn->host << ":" << conn->port << "\n\n";

    // Concept 제약 팩토리
    auto cfg = make_default<Config>();

    // make_default<Connection>();
    // ❌ Connection은 기본 생성자 없음 → 컴파일 오류
}
```

---

**실습 ③: `transform_if` — 제약된 auto로 만드는 알고리즘**

Concept + 축약 함수 템플릿을 활용하여 표준 라이브러리 스타일의 알고리즘을 만들어보자. 조건을 만족하는 원소만 변환하여 새 컨테이너에 담는 `transform_if`다.

```cpp
#include <concepts>
#include <vector>
#include <functional>
#include <iostream>

// ─── 필요한 Concept 정의 ───────────────────────────────────────

template<typename F, typename T>
concept Predicate = requires(F f, T t) {
    { f(t) } -> std::convertible_to<bool>;
};

template<typename F, typename In, typename Out>
concept Transformer = requires(F f, In in) {
    { f(in) } -> std::convertible_to<Out>;
};

// ─── transform_if 구현 ────────────────────────────────────────

template<
    std::ranges::input_range Range,          // 범위 Concept
    typename OutType = std::ranges::range_value_t<Range>
>
auto transform_if(
    const Range& input,
    Predicate<std::ranges::range_value_t<Range>> auto pred,  // 제약된 auto
    Transformer<std::ranges::range_value_t<Range>, OutType> auto transform
) -> std::vector<OutType> {
    std::vector<OutType> result;
    for (const auto& elem : input) {
        if (pred(elem)) {
            result.push_back(transform(elem));
        }
    }
    return result;
}

int main() {
    std::vector<int> numbers = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};

    // 짝수만 골라서 제곱
    auto evens_squared = transform_if(
        numbers,
        [](int x) { return x % 2 == 0; },   // Predicate
        [](int x) { return x * x; }          // Transformer
    );

    for (int v : evens_squared)
        std::cout << v << " ";
    std::cout << "\n";
    // 출력: 4 16 36 64 100

    // 5 이상인 수를 문자열로 변환
    auto large_as_string = transform_if<std::vector<int>, std::string>(
        numbers,
        [](int x) { return x >= 5; },
        [](int x) { return std::to_string(x); }
    );

    for (const auto& s : large_as_string)
        std::cout << s << " ";
    std::cout << "\n";
    // 출력: 5 6 7 8 9 10
}
```

---

**📌 이 챕터의 핵심 요약:**

```
┌──────────────────────────────────────────────────────────────────┐
│  Chapter 8 핵심 정리                                              │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ① auto vs decltype vs decltype(auto)                            │
│     auto           → 참조/const 제거 (템플릿 추론 규칙)           │
│     decltype(e)    → 표현식 타입 그대로 (벗겨내지 않음)           │
│     decltype(auto) → decltype 규칙으로 초기화 식 추론             │
│                                                                  │
│  ② 보편 참조(T&&): T가 추론될 때만 보편 참조                      │
│     lvalue 전달 → T = T&  → T& && → T& (붕괴)                   │
│     rvalue 전달 → T = T   → T&&         (유지)                   │
│                                                                  │
│  ③ std::forward<T>(arg)                                          │
│     T = T&  → lvalue로 캐스팅                                    │
│     T = T   → rvalue로 캐스팅                                    │
│     auto&&  → std::forward<decltype(arg)>(arg) 사용              │
│                                                                  │
│  ④ 축약 함수 템플릿 (C++20)                                       │
│     void f(auto x)  ≡  template<typename T> void f(T x)         │
│     각 auto마다 독립적인 타입 파라미터 생성                       │
│                                                                  │
│  ⑤ 제약된 auto (C++20)                                           │
│     Concept auto  →  해당 Concept을 만족하는 타입만 허용          │
│     파라미터, 반환 타입 모두에 적용 가능                          │
└──────────────────────────────────────────────────────────────────┘
```

이 챕터에서 다룬 개념들, 특히 보편 참조와 완벽 전달은 처음 만나면 "왜 이렇게 복잡한가?" 싶지만, 사실은 참조 붕괴라는 하나의 일관된 규칙에서 모두 비롯된다. 일단 그 규칙을 이해하면 `std::forward`의 동작도, `auto&&`의 의미도 모두 자연스럽게 이해된다. 다음 챕터에서는 이런 타입 추론과 짝을 이루는 또 다른 강력한 도구인 `if constexpr`과 컴파일 타임 분기를 살펴볼 것이다.




# Chapter 9. `if constexpr`와 컴파일 타임 분기

---

## 9.1 기존 태그 디스패치(Tag Dispatch)의 번거로움

템플릿 프로그래밍을 처음 배울 때 가장 당황스러운 순간 중 하나는, "타입에 따라 다르게 동작하는 함수"를 만들려고 할 때입니다. 언뜻 생각하면 그냥 `if`문을 쓰면 될 것 같지만, 컴파일러는 그렇게 호락호락하지 않습니다.

**왜 일반 `if`문으로는 안 될까?**

아래 코드를 먼저 살펴봅시다.

```cpp
#include <iostream>
#include <string>

template <typename T>
void print_value(T value) {
    if (std::is_integral_v<T>) {
        // 정수라면 16진수로도 출력
        std::cout << value << " (hex: " << std::hex << value << ")\n"; // ❌ 컴파일 에러 가능!
    } else {
        std::cout << value << "\n";
    }
}

int main() {
    print_value(42);
    print_value(3.14);       // double에 std::hex 적용 시 컴파일 에러
    print_value(std::string{"hello"}); // string에 std::hex 적용 시 컴파일 에러
}
```

`if (std::is_integral_v<T>)`의 조건이 런타임에는 `false`가 될지 몰라도, **컴파일러는 `if`문의 양쪽 분기를 모두 컴파일합니다.** `double`이나 `std::string`에 `std::hex`를 적용하는 코드는 문법적으로 잘못되었기 때문에 컴파일 에러가 발생합니다.

이 문제를 C++17 이전에는 **태그 디스패치(Tag Dispatch)** 또는 **`std::enable_if`** 로 해결했는데, 코드가 매우 장황해졌습니다.

```cpp
#include <iostream>
#include <type_traits>

// C++17 이전의 태그 디스패치 방식
// 태그 구조체 정의
struct integral_tag {};
struct non_integral_tag {};

// 타입에 따라 다른 구현을 가진 함수들
template <typename T>
void print_impl(T value, integral_tag) {
    std::cout << value << " (hex: " << std::hex << value << ")\n";
}

template <typename T>
void print_impl(T value, non_integral_tag) {
    std::cout << value << "\n";
}

// 진입점: 태그를 선택하는 역할
template <typename T>
void print_value(T value) {
    using tag = std::conditional_t<
        std::is_integral_v<T>,
        integral_tag,
        non_integral_tag
    >;
    print_impl(value, tag{});
}

int main() {
    print_value(42);
    print_value(3.14);
}
```

동작은 하지만, 단순히 "타입에 따라 다르게 출력한다"는 것을 표현하기 위해 태그 구조체 2개, 구현 함수 2개, 진입 함수 1개로 코드가 폭발합니다. 이처럼 **코드가 여러 조각으로 분산**되어 가독성이 크게 떨어집니다.

```
태그 디스패치의 구조적 문제:

┌─────────────────────────────────────────┐
│          print_value<T>(value)          │  ← 사용자가 호출하는 진입점
└────────────────┬────────────────────────┘
                 │ 태그 선택
        ┌────────┴────────┐
        ▼                 ▼
┌──────────────┐  ┌──────────────────┐
│ integral_tag │  │ non_integral_tag │  ← 태그 구조체 (의미 없는 타입들)
└──────┬───────┘  └────────┬─────────┘
       │                   │
       ▼                   ▼
┌──────────────┐  ┌─────────────────┐
│ print_impl() │  │  print_impl()   │  ← 분리된 구현 함수들
│ (integral)   │  │  (non-integral) │
└──────────────┘  └─────────────────┘

코드가 4~5곳으로 쪼개짐 → 가독성 최악 😱
```

---

## 9.2 `if constexpr` 문법과 동작 원리

C++17에서 등장한 `if constexpr`는 이 모든 번거로움을 한 방에 해결합니다. 핵심 아이디어는 단순합니다. **컴파일 타임에 조건을 평가하여, 거짓인 분기는 아예 컴파일에서 제외**합니다.

**기본 문법**

```cpp
if constexpr (컴파일_타임_조건식) {
    // 조건이 true일 때만 컴파일되는 코드
} else {
    // 조건이 false일 때만 컴파일되는 코드
}
```

조건식은 반드시 **`constexpr`로 평가 가능한 `bool` 값**이어야 합니다. 타입 트레이트(`std::is_integral_v<T>` 등)가 여기에 딱 맞습니다.

앞서 문제가 됐던 코드를 `if constexpr`로 다시 작성해 봅시다.

```cpp
#include <iostream>
#include <string>
#include <type_traits>

template <typename T>
void print_value(T value) {
    if constexpr (std::is_integral_v<T>) {
        // T가 정수 타입일 때만 이 블록이 컴파일됨
        std::cout << std::dec << value
                  << " (hex: 0x" << std::hex << value << ")\n";
    } else if constexpr (std::is_floating_point_v<T>) {
        // T가 부동소수점 타입일 때만 이 블록이 컴파일됨
        std::cout << std::fixed << value << "\n";
    } else {
        // 그 외 타입 (std::string 등)
        std::cout << value << "\n";
    }
}

int main() {
    print_value(42);          // 42 (hex: 0x2a)
    print_value(3.14);        // 3.140000
    print_value(std::string{"hello"});  // hello
}
```

태그 구조체도 없고, 분리된 함수도 없습니다. 하나의 함수 안에서 깔끔하게 표현됩니다.

**컴파일 타임에 무슨 일이 벌어지는가?**

컴파일러는 `print_value<int>`를 인스턴스화할 때 대략 아래와 같이 동작합니다.

```
print_value<int> 인스턴스화 시:
┌──────────────────────────────────────┐
│ if constexpr (true)  ← is_integral_v<int> = true
│   → 이 블록만 컴파일  ✅
│ else if constexpr (...) → 무시 ❌ (컴파일 안 됨)
│ else               → 무시 ❌ (컴파일 안 됨)
└──────────────────────────────────────┘

print_value<double> 인스턴스화 시:
┌──────────────────────────────────────┐
│ if constexpr (false) → 무시 ❌
│ else if constexpr (true) ← is_floating_point_v<double> = true
│   → 이 블록만 컴파일  ✅
│ else               → 무시 ❌
└──────────────────────────────────────┘

print_value<std::string> 인스턴스화 시:
┌──────────────────────────────────────┐
│ if constexpr (false) → 무시 ❌
│ else if constexpr (false) → 무시 ❌
│ else               → 이 블록만 컴파일 ✅
└──────────────────────────────────────┘
```

**일반 `if`와 `if constexpr`의 결정적 차이**

```cpp
template <typename T>
void demo(T value) {
    // ❌ 일반 if: 양쪽 분기 모두 컴파일 → T=double이면 .size() 없어서 에러!
    if (std::is_same_v<T, std::string>) {
        std::cout << value.size();  // double에는 size()가 없음 → 컴파일 에러!
    }

    // ✅ if constexpr: false인 분기는 컴파일에서 제외
    if constexpr (std::is_same_v<T, std::string>) {
        std::cout << value.size();  // T=std::string일 때만 컴파일됨
    }
}
```

`if constexpr`의 버려진 분기(discarded branch)는 **문법 검사는 하지만, 의미론적 검사(타입 체크, 멤버 존재 여부 등)는 하지 않습니다.** 다만 완전히 문법적으로 잘못된 코드(예: 닫히지 않은 괄호)는 여전히 에러가 납니다.

> 💡 **핵심 규칙:** `if constexpr`의 버려진 분기는 "있는 척"만 하고 실제로는 컴파일되지 않습니다. 따라서 그 타입에 존재하지 않는 멤버 함수를 호출해도 에러가 나지 않습니다. 단, 이 동작은 **템플릿 내부**에서만 의미 있습니다. 템플릿이 아닌 일반 함수에서 `if constexpr`를 써도 버려진 분기는 여전히 타입 검사를 받습니다.

---

## 9.3 `if consteval` — C++23의 새 기능: 컴파일/런타임 분기

C++23에는 `if constexpr`와 비슷하지만 목적이 다른 `if consteval`이 추가되었습니다. 이것은 **"지금 이 코드가 컴파일 타임에 실행되고 있는가, 런타임에 실행되고 있는가"** 를 구분합니다.

**왜 이게 필요한가?**

`constexpr` 함수는 컴파일 타임에도, 런타임에도 호출될 수 있습니다. 그런데 컴파일 타임에는 더 효율적이거나 정확한 방법이 있고, 런타임에는 다른 방법을 써야 하는 경우가 있습니다. 예를 들어 컴파일 타임에는 특정 내장 함수(`__builtin_is_constant_evaluated` 등)를 쓸 수 있지만 런타임에는 쓸 수 없습니다.

```cpp
#include <iostream>
#include <cmath>

// C++23의 if consteval
constexpr double my_sqrt(double x) {
    if consteval {
        // 컴파일 타임에 실행될 때: 뉴턴-랩슨 방법으로 직접 계산
        // (std::sqrt는 constexpr이 아닌 컴파일러도 있기 때문)
        double guess = x / 2.0;
        for (int i = 0; i < 20; ++i) {
            guess = (guess + x / guess) / 2.0;
        }
        return guess;
    } else {
        // 런타임에 실행될 때: 최적화된 라이브러리 함수 사용
        return std::sqrt(x);
    }
}

int main() {
    constexpr double ct_result = my_sqrt(2.0);  // 컴파일 타임 경로
    double rt_result = my_sqrt(2.0);            // 런타임 경로

    std::cout << "컴파일 타임: " << ct_result << "\n";
    std::cout << "런타임:     " << rt_result << "\n";
}
```

**`if consteval`의 부정 형태: `if !consteval`**

```cpp
constexpr int compute(int x) {
    if !consteval {
        // 런타임에만 실행되는 코드 (로깅, 디버깅 등)
        std::cout << "[런타임] compute 호출됨\n";
    }
    return x * x;
}
```

**`if constexpr` vs `if consteval` 비교**

| 구분 | `if constexpr` | `if consteval` |
|------|---------------|----------------|
| 도입 | C++17 | C++23 |
| 조건의 의미 | 컴파일 타임 **값**에 따른 분기 | 현재 **실행 컨텍스트** (컴파일/런타임) 에 따른 분기 |
| 주요 용도 | 타입에 따른 코드 분기 | 컴파일/런타임 최적화 경로 분리 |
| 조건 작성 | `(some_constexpr_condition)` | 조건 없음 (컨텍스트 자체가 조건) |

---

## 9.4 `constexpr` 함수, `consteval` 함수, `constinit` 변수

`if constexpr`와 `if consteval`을 제대로 활용하려면, 이 세 가지 키워드의 차이를 명확히 이해해야 합니다.

**`constexpr` 함수 — 컴파일/런타임 양쪽 모두 가능**

```cpp
constexpr int square(int x) {
    return x * x;
}

int main() {
    constexpr int a = square(5);  // ✅ 컴파일 타임에 계산 → a = 25 (리터럴로 대체됨)
    int n = 3;
    int b = square(n);            // ✅ 런타임에도 동작
}
```

`constexpr` 함수는 **양면 가능(ambidextrous)**입니다. 컴파일 타임 맥락에서 호출되면 컴파일 타임에, 그렇지 않으면 런타임에 실행됩니다.

**`consteval` 함수 — 컴파일 타임만 허용 (C++20)**

```cpp
consteval int cube(int x) {
    return x * x * x;
}

int main() {
    constexpr int a = cube(3);  // ✅ 컴파일 타임에만 사용 가능
    int n = 3;
    // int b = cube(n);         // ❌ 컴파일 에러! n은 런타임 값
}
```

`consteval`로 선언된 함수는 **즉시 함수(immediate function)**라고 부르며, 반드시 컴파일 타임에 호출되어야 합니다. 상수가 아닌 인자를 넣으면 컴파일 에러가 납니다. 이는 "이 함수는 절대로 런타임에 실행되어서는 안 된다"는 강한 의도 표현입니다.

**`constinit` 변수 — 컴파일 타임 초기화 보장 (C++20)**

```cpp
constinit int global_cache = 42;   // ✅ 컴파일 타임에 초기화됨이 보장
// constinit int bad = some_runtime_func(); // ❌ 컴파일 에러

int main() {
    global_cache = 100;  // ✅ constinit은 const가 아니므로 런타임에 변경 가능
}
```

`constinit`는 `const`와 달리 변수의 **초기화**만 컴파일 타임에 일어나도록 강제하며, 초기화 이후의 변경은 허용합니다. 전역 변수의 **정적 초기화 순서 문제(Static Initialization Order Fiasco)**를 방지하는 데 유용합니다.

**세 키워드를 한눈에 비교**

```
                컴파일 타임 사용   런타임 사용    이후 변경
                ─────────────────────────────────────────
constexpr 함수       ✅               ✅             -
consteval 함수       ✅               ❌             -
constexpr 변수       ✅               ✅             ❌ (const)
constinit 변수       ✅ (초기화)      ✅             ✅
const 변수           ✅/✅            ✅             ❌
```

---

## 9.5 컴파일 타임에 분기하는 템플릿 코드 작성 패턴

실전에서 자주 등장하는 `if constexpr` 활용 패턴들을 살펴보겠습니다.

**패턴 1: 타입 특성에 따른 분기**

가장 기본적인 패턴입니다. `<type_traits>`의 타입 체크 도구들과 함께 사용합니다.

```cpp
#include <iostream>
#include <type_traits>
#include <string>

template <typename T>
std::string to_string_ex(const T& value) {
    if constexpr (std::is_same_v<T, bool>) {
        return value ? "true" : "false";
    } else if constexpr (std::is_integral_v<T>) {
        return std::to_string(value);
    } else if constexpr (std::is_floating_point_v<T>) {
        return std::to_string(value);
    } else if constexpr (std::is_same_v<T, std::string>) {
        return '"' + value + '"';  // 문자열은 따옴표로 감쌈
    } else {
        // 위 조건에 해당하지 않는 타입에서 컴파일 에러 발생시키기
        static_assert(sizeof(T) == 0, "to_string_ex: 지원하지 않는 타입");
    }
}

int main() {
    std::cout << to_string_ex(true)             << "\n"; // true
    std::cout << to_string_ex(42)               << "\n"; // 42
    std::cout << to_string_ex(3.14)             << "\n"; // 3.140000
    std::cout << to_string_ex(std::string{"hi"}) << "\n"; // "hi"
}
```

> 💡 `static_assert(sizeof(T) == 0, ...)` 는 `if constexpr`의 버려지지 않은 분기에 도달했을 때 강제로 컴파일 에러를 발생시키는 관용 표현입니다. `false`를 직접 쓰면 템플릿 인스턴스화 전에 에러가 나므로, 타입 `T`에 의존하는 `sizeof(T) == 0`을 사용합니다.

**패턴 2: 멤버 존재 여부에 따른 분기 (Concepts와 결합)**

C++20 `requires` 표현식과 `if constexpr`를 결합하면 특정 멤버나 연산이 지원되는지 확인하고 분기할 수 있습니다.

```cpp
#include <iostream>
#include <vector>
#include <type_traits>

template <typename T>
void describe(const T& container) {
    std::cout << "타입: " << typeid(T).name() << "\n";

    // size() 멤버가 있는 타입이면 크기 출력
    if constexpr (requires { container.size(); }) {
        std::cout << "  크기: " << container.size() << "\n";
    }

    // begin()/end()가 있는 순회 가능한 타입이면 원소 출력
    if constexpr (requires { std::begin(container); std::end(container); }) {
        std::cout << "  원소: ";
        for (const auto& elem : container) {
            std::cout << elem << " ";
        }
        std::cout << "\n";
    }
}

int main() {
    std::vector<int> v{1, 2, 3, 4, 5};
    describe(v);

    int arr[] = {10, 20, 30};
    describe(arr);
}
```

**패턴 3: 가변 인자 템플릿과 `if constexpr` 재귀 종료**

가변 인자 템플릿의 재귀를 종료할 때도 `if constexpr`가 매우 유용합니다. C++17 이전에는 재귀 종료를 위한 별도의 특수화(specialization) 함수가 필요했지만, 이제는 하나의 함수로 해결됩니다.

```cpp
#include <iostream>

// C++17 이전 방식: 종료 함수가 별도로 필요
// void print_all() {} // 빈 함수
// template<typename T, typename... Rest>
// void print_all(T first, Rest... rest) { ... }

// if constexpr를 사용한 방식: 하나의 함수로 해결
template <typename T, typename... Rest>
void print_all(T first, Rest... rest) {
    std::cout << first;
    if constexpr (sizeof...(rest) > 0) {
        std::cout << ", ";
        print_all(rest...);  // 재귀 호출, rest가 0개면 이 줄은 컴파일 안 됨
    } else {
        std::cout << "\n";
    }
}

int main() {
    print_all(1, 2.5, "hello", 'A');  // 1, 2.5, hello, A
}
```

`sizeof...(rest) > 0`이 `false`일 때 `print_all(rest...)`가 버려지므로, 인자 없이 `print_all()`을 호출하는 문제가 생기지 않습니다.

**패턴 4: 포인터 vs. 값 타입 분기**

```cpp
#include <iostream>
#include <type_traits>

template <typename T>
void safe_print(T value) {
    if constexpr (std::is_pointer_v<T>) {
        if (value != nullptr) {
            std::cout << *value << "\n";
        } else {
            std::cout << "(null)\n";
        }
    } else {
        std::cout << value << "\n";
    }
}

int main() {
    int x = 42;
    int* p = &x;
    int* null_p = nullptr;

    safe_print(x);       // 42
    safe_print(p);       // 42 (역참조)
    safe_print(null_p);  // (null)
    safe_print(3.14);    // 3.14
}
```

---

## 🛠 실습: 타입에 따라 다르게 동작하는 `serialize<T>` 함수 만들기

지금까지 배운 내용을 모두 활용해서 실용적인 `serialize<T>` 함수를 만들어 봅시다. 이 함수는 다양한 타입을 JSON-like 문자열로 직렬화합니다.

**요구사항:**
- `bool` → `true` / `false`
- 정수 타입 → 그대로 숫자
- 부동소수점 타입 → 소수점 포함
- `std::string` → 따옴표로 감싸기
- 순회 가능한 컨테이너 → `[elem1, elem2, ...]` 형식
- 그 외 → 컴파일 에러

```cpp
#include <iostream>
#include <string>
#include <vector>
#include <list>
#include <type_traits>
#include <sstream>

// 전방 선언: 컨테이너 내부 원소에서 재귀적으로 serialize를 쓰기 위해 필요
template <typename T>
std::string serialize(const T& value);

// 컨테이너를 감지하는 requires 표현식 (헬퍼)
template <typename T>
concept Iterable = requires(T t) {
    std::begin(t);
    std::end(t);
} && !std::is_same_v<T, std::string>; // string은 별도로 처리

template <typename T>
std::string serialize(const T& value) {
    if constexpr (std::is_same_v<T, bool>) {
        // bool은 정수로 변환되기 전에 먼저 처리해야 함
        return value ? "true" : "false";

    } else if constexpr (std::is_integral_v<T>) {
        return std::to_string(value);

    } else if constexpr (std::is_floating_point_v<T>) {
        std::ostringstream oss;
        oss << value;
        return oss.str();

    } else if constexpr (std::is_same_v<T, std::string>) {
        return '"' + value + '"';

    } else if constexpr (Iterable<T>) {
        // 컨테이너: [ elem1, elem2, ... ] 형식
        std::string result = "[";
        bool first = true;
        for (const auto& elem : value) {
            if (!first) result += ", ";
            result += serialize(elem);  // 재귀적으로 각 원소를 직렬화
            first = false;
        }
        result += "]";
        return result;

    } else {
        // 지원하지 않는 타입은 컴파일 에러
        static_assert(sizeof(T) == 0,
            "serialize(): 지원하지 않는 타입입니다. "
            "bool, 정수, 부동소수점, string, Iterable 컨테이너만 지원합니다.");
    }
}

int main() {
    // 기본 타입 직렬화
    std::cout << serialize(true)              << "\n"; // true
    std::cout << serialize(false)             << "\n"; // false
    std::cout << serialize(42)                << "\n"; // 42
    std::cout << serialize(-7)                << "\n"; // -7
    std::cout << serialize(3.14)              << "\n"; // 3.14
    std::cout << serialize(std::string{"hi"}) << "\n"; // "hi"

    std::cout << "\n";

    // 컨테이너 직렬화
    std::vector<int> vi{1, 2, 3, 4, 5};
    std::cout << serialize(vi) << "\n";          // [1, 2, 3, 4, 5]

    std::vector<std::string> vs{"apple", "banana", "cherry"};
    std::cout << serialize(vs) << "\n";          // ["apple", "banana", "cherry"]

    std::list<double> ld{1.1, 2.2, 3.3};
    std::cout << serialize(ld) << "\n";          // [1.1, 2.2, 3.3]

    // 중첩 컨테이너 (벡터의 벡터)
    std::vector<std::vector<int>> vvi{{1, 2}, {3, 4}, {5}};
    std::cout << serialize(vvi) << "\n";         // [[1, 2], [3, 4], [5]]

    // ❌ 아래 줄의 주석을 해제하면 컴파일 에러 (의도된 동작)
    // struct Foo {};
    // std::cout << serialize(Foo{}) << "\n";
}
```

**예상 출력:**

```
true
false
42
-7
3.14
"hi"

[1, 2, 3, 4, 5]
["apple", "banana", "cherry"]
[1.1, 2.2, 3.3]
[[1, 2], [3, 4], [5]]
```

**실행 흐름 다이어그램:**

```
serialize(value) 호출
        │
        ▼
┌─────────────────────────────────┐
│   if constexpr (is bool?)       │─── true ──► "true" / "false"
├─────────────────────────────────┤
│   if constexpr (is integral?)   │─── true ──► to_string(value)
├─────────────────────────────────┤
│   if constexpr (is float?)      │─── true ──► ostringstream 변환
├─────────────────────────────────┤
│   if constexpr (is string?)     │─── true ──► '"' + value + '"'
├─────────────────────────────────┤
│   if constexpr (Iterable?)      │─── true ──► "[" + 재귀 serialize + "]"
│                                 │                      │
│                                 │                      ▼
│                                 │             각 원소에 대해 serialize()
│                                 │             재귀 호출 (중첩도 OK!)
├─────────────────────────────────┤
│   else                          │────────────► static_assert → 컴파일 에러
└─────────────────────────────────┘
```

**Visual Studio 2026에서 테스트하기:**

Visual Studio에서 새 C++ 프로젝트를 만들고, 프로젝트 속성에서 `C++ 언어 표준`을 **ISO C++23 표준 (`/std:c++23`)** 으로 설정합니다. 그런 다음 위 코드를 `main.cpp`에 붙여넣고 빌드하면 됩니다.

> 💡 **Visual Studio 팁:** 출력 창에서 `if constexpr`의 버려진 분기는 회색으로 표시되어 어떤 분기가 현재 타입에 대해 활성화되지 않았는지 시각적으로 확인할 수 있습니다. 이 기능을 활용하면 디버깅이 훨씬 쉬워집니다.

---

## 📌 이 장의 핵심 요약

이 장에서 배운 내용을 정리하면 다음과 같습니다.

**`if constexpr`의 핵심 가치**는 템플릿 내부에서 타입에 따른 코드 분기를 하나의 함수 안에 깔끔하게 표현할 수 있다는 점입니다. 버려진 분기는 컴파일되지 않으므로, 해당 타입에 존재하지 않는 멤버나 연산을 사용해도 안전합니다.

**`if consteval`(C++23)**은 같은 `constexpr` 함수가 컴파일 타임에 실행될 때와 런타임에 실행될 때 서로 다른 코드 경로를 타게 하여, 각 환경에 최적화된 구현을 선택할 수 있게 해줍니다.

**`constexpr`, `consteval`, `constinit`**는 각각 양쪽 컨텍스트 허용, 컴파일 타임 전용, 컴파일 타임 초기화 보장이라는 서로 다른 강도의 컴파일 타임 보장을 제공하는 도구들입니다.

이 도구들을 조합하면, 과거에 태그 디스패치나 `std::enable_if`로 복잡하게 처리하던 패턴을 훨씬 읽기 쉽고 유지보수하기 좋은 코드로 작성할 수 있습니다.




# Chapter 10. Deducing `this` — C++23의 게임 체인저

---

## 10.1 기존 `const` 오버로딩의 코드 중복 문제

C++를 어느 정도 써본 분이라면 다음과 같은 코드를 본 적이 있을 것입니다.

```cpp
class MyContainer {
    int data_[10] = {};
public:
    // const 버전과 non-const 버전이 거의 동일한 코드로 두 번 작성됨
    int& operator[](size_t idx) {
        return data_[idx];          // 거의 동일한 구현
    }
    const int& operator[](size_t idx) const {
        return data_[idx];          // 거의 동일한 구현
    }
};
```

단 한 줄짜리 함수니까 괜찮아 보이지만, 실제 코드에서 `operator[]`의 구현이 복잡해지면 이야기가 달라집니다. 범위 검사, 로깅, 캐시 처리 등 로직이 추가될수록 두 함수는 완전히 동일한 코드를 복붙한 채 유지보수됩니다.

이 문제가 얼마나 고통스러운지 조금 더 현실적인 예로 살펴봅시다.

```cpp
#include <vector>
#include <stdexcept>
#include <iostream>

class DataStore {
    std::vector<int> data_;

public:
    DataStore(std::initializer_list<int> il) : data_(il) {}

    // ❌ 이 두 함수는 구현이 완전히 동일하지만 두 번 작성해야 함
    int& get(size_t idx) {
        if (idx >= data_.size())
            throw std::out_of_range("index out of range");
        std::cout << "[접근 로그] idx=" << idx << "\n";
        return data_[idx];
    }

    const int& get(size_t idx) const {  // 복붙 + 약간의 수정
        if (idx >= data_.size())
            throw std::out_of_range("index out of range");
        std::cout << "[접근 로그] idx=" << idx << "\n";
        return data_[idx];
    }
};
```

이 문제를 해결하려고 C++23 이전에는 `const_cast`를 이용한 우회 기법을 쓰기도 했습니다.

```cpp
// C++23 이전의 "덜 나쁜" 해결책 — 여전히 보기 싫음
int& get(size_t idx) {
    // const 버전을 호출한 뒤 const를 벗겨내는 꼼수
    return const_cast<int&>(std::as_const(*this).get(idx));
}
const int& get(size_t idx) const {
    if (idx >= data_.size()) throw std::out_of_range("...");
    return data_[idx];
}
```

`const_cast`는 항상 불안한 느낌을 주고, 코드를 처음 보는 사람은 "왜 이렇게 했지?"라는 의문을 가질 수밖에 없습니다.

C++23의 **Deducing `this`**는 이 모든 문제를 근본적으로 해결합니다.

---

## 10.2 명시적 객체 파라미터(Explicit Object Parameter) 문법

**Deducing `this`**의 공식 명칭은 **"명시적 객체 파라미터(Explicit Object Parameter)"** 입니다. 핵심 아이디어는 멤버 함수에서 암묵적으로 존재하던 `this` 포인터를 **명시적인 첫 번째 파라미터**로 꺼내는 것입니다.

**기본 문법**

```cpp
struct MyClass {
    // 기존 방식
    void foo() const;

    // C++23 — 명시적 객체 파라미터
    void foo(this const MyClass& self);  // ← 'this' 키워드가 첫 파라미터 앞에!
};
```

`this` 키워드가 첫 번째 파라미터 앞에 붙는다는 점이 특이합니다. 이제 `this`는 숨겨진 포인터가 아니라 이름을 가진 일반 파라미터가 됩니다.

더 강력한 것은, 이 파라미터를 **템플릿 파라미터**로 만들 수 있다는 점입니다.

```cpp
struct MyClass {
    // Self의 타입을 컴파일러가 추론! (deducing 'this')
    void foo(this auto& self);

    // 또는 명시적 템플릿 파라미터로
    template <typename Self>
    void foo(this Self& self);
};
```

`this auto& self`라고 쓰면 컴파일러가 호출 컨텍스트에 따라 `Self`의 타입을 추론합니다. 객체가 `const`라면 `Self = const MyClass`로, `const`가 아니라면 `Self = MyClass`로 추론됩니다.

이제 앞서 문제가 됐던 `get` 함수를 하나로 합쳐봅시다.

```cpp
#include <vector>
#include <stdexcept>
#include <iostream>

class DataStore {
    std::vector<int> data_;

public:
    DataStore(std::initializer_list<int> il) : data_(il) {}

    // ✅ C++23: 단 하나의 함수로 const/non-const 모두 처리
    auto& get(this auto& self, size_t idx) {
        if (idx >= self.data_.size())
            throw std::out_of_range("index out of range");
        std::cout << "[접근 로그] idx=" << idx << "\n";
        return self.data_[idx];  // Self가 const면 const int&, 아니면 int& 반환
    }
};

int main() {
    DataStore store{10, 20, 30, 40, 50};

    store.get(2) = 99;       // non-const 경로 → int& 반환
    std::cout << store.get(2) << "\n";  // 99

    const DataStore& cref = store;
    // cref.get(2) = 0;      // ❌ 컴파일 에러! const int&는 수정 불가
    std::cout << cref.get(2) << "\n";  // 99 — const 경로
}
```

`this auto& self`에서 `auto`가 추론하는 타입을 그림으로 표현하면 다음과 같습니다.

```
store.get(2) 호출 시:
┌─────────────────────────────────────────┐
│  self의 타입: DataStore&                │
│  반환 타입:   int&                      │
│  → 수정 가능                            │
└─────────────────────────────────────────┘

cref.get(2) 호출 시:
┌─────────────────────────────────────────┐
│  self의 타입: const DataStore&          │
│  반환 타입:   const int&                │
│  → 수정 불가                            │
└─────────────────────────────────────────┘

하나의 함수 정의로 두 가지 동작 모두 처리! 🎉
```

> ⚠️ **주의:** 명시적 객체 파라미터를 사용하는 함수 안에서는 `this`를 사용할 수 없습니다. 대신 `self`(또는 직접 지정한 이름)를 통해 객체에 접근해야 합니다.

**값으로 받는 `self` — 이동 의미론 활용**

명시적 객체 파라미터는 참조뿐 아니라 값으로도 받을 수 있어서, rvalue 참조까지 한 번에 처리할 수 있습니다.

```cpp
#include <string>
#include <iostream>

struct Message {
    std::string text;

    // 값으로 self를 받으면 lvalue/rvalue 모두 처리 가능
    std::string get_text(this Message self) {
        return std::move(self.text);  // rvalue로 호출 시 이동 발생
    }
};

int main() {
    Message m{"Hello, World!"};
    std::string t1 = m.get_text();              // 복사 후 이동
    std::string t2 = Message{"Hi!"}.get_text(); // 이동만 발생
}
```

---

## 10.3 CRTP 패턴을 `deducing this`로 단순화하기

**CRTP(Curiously Recurring Template Pattern)**는 C++에서 정적 다형성(virtual 없이 다형성)을 구현하는 대표적인 패턴입니다. 하지만 문법이 복잡하고 직관적이지 않아서 처음 보는 사람은 당혹스럽습니다.

**기존 CRTP 방식**

```cpp
// ❌ C++20 이전의 CRTP — 문법이 기묘하다
template <typename Derived>
struct Base {
    void interface() {
        // 기반 클래스에서 파생 클래스의 함수를 호출하기 위해 캐스팅 필요
        static_cast<Derived*>(this)->implementation();
    }
};

struct ConcreteA : Base<ConcreteA> {  // 자기 자신을 템플릿 인자로!
    void implementation() {
        std::cout << "ConcreteA 구현\n";
    }
};

struct ConcreteB : Base<ConcreteB> {  // 마찬가지로 자기 자신을!
    void implementation() {
        std::cout << "ConcreteB 구현\n";
    }
};
```

`Base<ConcreteA>`처럼 자기 자신을 템플릿 인자로 넘기는 문법이 처음 보면 매우 이상하게 느껴집니다. 그리고 `static_cast<Derived*>(this)`라는 강제 캐스팅도 불안합니다.

**`deducing this`로 CRTP 대체하기**

```cpp
#include <iostream>

// ✅ C++23: deducing this로 CRTP 없이 정적 다형성 구현
struct Base {
    void interface(this auto& self) {
        // self의 실제 타입(파생 클래스)에서 implementation을 찾음
        self.implementation();
    }

    // 파생 클래스가 구현을 제공하지 않으면 기본 동작 사용
    void implementation() {
        std::cout << "Base 기본 구현\n";
    }
};

struct ConcreteA : Base {           // 단순한 상속! 템플릿 인자 불필요
    void implementation() {
        std::cout << "ConcreteA 구현\n";
    }
};

struct ConcreteB : Base {
    void implementation() {
        std::cout << "ConcreteB 구현\n";
    }
};

struct ConcreteC : Base {
    // implementation()을 정의하지 않으면 Base의 기본 구현 사용
};

int main() {
    ConcreteA a;
    ConcreteB b;
    ConcreteC c;

    a.interface();  // ConcreteA 구현
    b.interface();  // ConcreteB 구현
    c.interface();  // Base 기본 구현
}
```

기존 CRTP와 `deducing this` 방식을 구조적으로 비교해 봅시다.

```
[CRTP 방식]                          [Deducing this 방식]

template<typename Derived>           struct Base {
struct Base {                            void interface(this auto& self) {
    void interface() {                       self.implementation();
        static_cast<Derived*>            }
            (this)->implementation();    };
    }
};                                   struct ConcreteA : Base { ... };
                                     struct ConcreteB : Base { ... };
struct ConcreteA
    : Base<ConcreteA> { ... };   ← 자기 자신을 인자로 줄 필요 없음!
struct ConcreteB
    : Base<ConcreteB> { ... };   ← static_cast도 필요 없음!

복잡 😵                              단순 😊
```

**실용적인 CRTP 대체 예제 — `Comparable` 믹스인**

많은 비교 연산자를 자동으로 제공하는 믹스인 클래스를 만들어 봅시다. 파생 클래스가 `<` 연산자만 구현하면 나머지(`>`, `<=`, `>=`, `==`, `!=`)를 자동으로 얻습니다.

```cpp
#include <iostream>

struct Comparable {
    // this auto& self 덕분에 파생 클래스 타입을 알 수 있음
    bool operator>(this const auto& self, const auto& other) {
        return other < self;
    }
    bool operator<=(this const auto& self, const auto& other) {
        return !(other < self);
    }
    bool operator>=(this const auto& self, const auto& other) {
        return !(self < other);
    }
    bool operator==(this const auto& self, const auto& other) {
        return !(self < other) && !(other < self);
    }
    bool operator!=(this const auto& self, const auto& other) {
        return (self < other) || (other < self);
    }
};

struct Point : Comparable {
    int x, y;

    // < 만 정의하면 나머지 비교 연산자는 Comparable에서 자동으로!
    bool operator<(const Point& other) const {
        return (x * x + y * y) < (other.x * other.x + other.y * other.y);
    }
};

int main() {
    Point p1{3, 4};  // 원점에서 거리 = 5
    Point p2{1, 1};  // 원점에서 거리 ≈ 1.4

    std::cout << std::boolalpha;
    std::cout << "p1 > p2: "  << (p1 > p2)  << "\n";  // true
    std::cout << "p1 <= p2: " << (p1 <= p2) << "\n";  // false
    std::cout << "p1 == p2: " << (p1 == p2) << "\n";  // false
}
```

---

## 10.4 재귀 람다 구현하기

람다는 C++에서 매우 유용하지만, 한 가지 큰 제약이 있습니다. 람다 자신이 이름이 없기 때문에 **자기 자신을 재귀적으로 호출할 수 없습니다.** C++23 이전에는 이 문제를 우회하기 위해 여러 꼼수가 필요했습니다.

**C++23 이전의 재귀 람다 — 불편한 방법들**

```cpp
#include <iostream>
#include <functional>

int main() {
    // 방법 1: std::function으로 감싸기 — 성능 오버헤드 있음
    std::function<int(int)> factorial;
    factorial = [&factorial](int n) -> int {
        return n <= 1 ? 1 : n * factorial(n - 1);
    };

    // 방법 2: 인자로 자기 자신을 받기 — 호출 문법이 어색함
    auto factorial2 = [](auto& self, int n) -> int {
        return n <= 1 ? 1 : n * self(self, n - 1);  // self(self, ...) 가 이상함!
    };
    std::cout << factorial2(factorial2, 5) << "\n";  // 호출도 어색함
}
```

두 방법 모두 문제가 있습니다. `std::function`은 타입 소거로 인한 성능 비용이 있고, 두 번째 방법은 `self(self, n-1)`처럼 자기 자신을 두 번 써야 하는 어색한 호출 문법을 가집니다.

**`deducing this`로 우아한 재귀 람다**

```cpp
#include <iostream>

int main() {
    // ✅ C++23: deducing this를 이용한 재귀 람다
    auto factorial = [](this auto self, int n) -> int {
        return n <= 1 ? 1 : n * self(n - 1);  // self(n-1)로 자연스럽게 재귀!
    };

    std::cout << factorial(5)  << "\n";  // 120
    std::cout << factorial(10) << "\n";  // 3628800
}
```

`this auto self`에서 `self`는 람다 자신의 타입으로 추론됩니다. 따라서 `self(n-1)`은 람다를 재귀 호출하는 것과 같습니다. 그리고 이것은 `std::function`을 쓰지 않으므로 성능 비용도 없습니다.

좀 더 복잡한 예로, **트리 순회**를 재귀 람다로 구현해 봅시다.

```cpp
#include <iostream>
#include <vector>

struct TreeNode {
    int value;
    std::vector<TreeNode> children;
};

int main() {
    // 트리 구조 생성
    TreeNode root{1, {
        {2, {{4, {}}, {5, {}}}},
        {3, {{6, {}}, {7, {}}}}
    }};

    // ✅ 재귀 람다로 트리를 DFS 순회
    auto dfs = [](this auto self, const TreeNode& node, int depth) -> void {
        std::cout << std::string(depth * 2, ' ') << node.value << "\n";
        for (const auto& child : node.children) {
            self(child, depth + 1);  // 재귀 호출!
        }
    };

    dfs(root, 0);
}
```

**출력:**

```
1
  2
    4
    5
  3
    6
    7
```

---

## 10.5 빌더(Builder) 패턴을 우아하게 구현하기

빌더 패턴은 복잡한 객체의 생성 과정을 단계적으로 표현하는 패턴입니다. 특히 메서드 체이닝(method chaining)을 통해 다음처럼 유창한 인터페이스(Fluent Interface)를 제공합니다.

```cpp
auto query = QueryBuilder{}
    .select("name", "age")
    .from("users")
    .where("age > 18")
    .limit(10)
    .build();
```

**기존 빌더 패턴의 상속 문제**

빌더를 상속하면 문제가 생깁니다. 부모 빌더의 메서드가 `Base&`를 반환하므로 체이닝 도중 파생 클래스의 메서드를 호출할 수 없게 됩니다.

```cpp
// ❌ 기존 방식의 문제
struct BaseBuilder {
    BaseBuilder& set_name(std::string name) {
        name_ = name;
        return *this;  // Base&를 반환하므로 파생 클래스 메서드 접근 불가!
    }
    std::string name_;
};

struct AdvancedBuilder : BaseBuilder {
    AdvancedBuilder& set_age(int age) {
        age_ = age;
        return *this;
    }
    int age_ = 0;
};

int main() {
    AdvancedBuilder b;
    // ❌ set_name이 BaseBuilder&를 반환하여 set_age를 찾을 수 없음
    // b.set_name("Alice").set_age(30);
}
```

**`deducing this`로 체이닝 문제 해결**

```cpp
#include <iostream>
#include <string>
#include <vector>

struct BaseBuilder {
    // ✅ self의 실제 타입을 반환 → 파생 클래스에서도 체이닝 유지!
    auto& set_name(this auto& self, std::string name) {
        self.name_ = std::move(name);
        return self;  // 실제 타입(파생 클래스)의 참조를 반환
    }
    std::string name_;
};

struct PersonBuilder : BaseBuilder {
    auto& set_age(this auto& self, int age) {
        self.age_ = age;
        return self;
    }
    auto& set_job(this auto& self, std::string job) {
        self.job_ = std::move(job);
        return self;
    }

    void build() const {
        std::cout << "이름: " << name_
                  << ", 나이: " << age_
                  << ", 직업: " << job_ << "\n";
    }

    int age_ = 0;
    std::string job_;
};

int main() {
    PersonBuilder{}
        .set_name("Alice")   // BaseBuilder의 메서드 → PersonBuilder& 반환
        .set_age(30)         // PersonBuilder의 메서드 → PersonBuilder& 반환
        .set_job("Engineer") // PersonBuilder의 메서드 → PersonBuilder& 반환
        .build();            // 완성!
}
```

**출력:**

```
이름: Alice, 나이: 30, 직업: Engineer
```

`this auto& self`에서 `self`의 타입이 실제 호출한 객체의 타입(`PersonBuilder`)으로 추론되므로, `BaseBuilder`의 메서드가 호출되어도 `PersonBuilder&`를 반환합니다. 체이닝이 완벽하게 작동합니다.

이 동작을 그림으로 표현하면 다음과 같습니다.

```
PersonBuilder{}.set_name("Alice").set_age(30).set_job("Engineer").build()
     │               │                │              │
     │               │                │              │
     ▼               ▼                ▼              ▼
PersonBuilder&  PersonBuilder&  PersonBuilder&  PersonBuilder&
  (self 타입)      반환됨!          반환됨!         반환됨!

BaseBuilder의 함수도 self = PersonBuilder&로 추론되어
PersonBuilder&를 반환 → 체이닝 끊김 없음 ✅
```

---

## 🛠 실습: CRTP 없는 정적 다형성 구현, 재귀 람다 계승(factorial)

이번 실습에서는 두 가지를 만들어 봅니다. 첫 번째는 `deducing this`를 이용한 정적 다형성 구현이고, 두 번째는 재귀 람다로 만드는 피보나치 수열입니다.

**실습 1: 동물 소리 시뮬레이터 — 정적 다형성**

`virtual` 함수 없이도 다형적으로 동작하는 동물 클래스 계층을 만들어 봅시다.

```cpp
#include <iostream>
#include <string>

// 기반 구조체: deducing this로 정적 다형성 제공
struct Animal {
    // 파생 클래스가 구현할 함수 (기본값 제공)
    std::string speak() const { return "..."; }
    std::string name()  const { return "동물"; }

    // deducing this로 파생 클래스의 구현을 호출
    void introduce(this const auto& self) {
        std::cout << self.name() << ": " << self.speak() << "\n";
    }

    // 여러 번 말하기 — 이것도 파생 클래스 타입으로 동작
    void speak_times(this const auto& self, int n) {
        for (int i = 0; i < n; ++i) {
            self.introduce();
        }
    }
};

struct Dog : Animal {
    std::string speak() const { return "멍멍!"; }
    std::string name()  const { return "강아지"; }
};

struct Cat : Animal {
    std::string speak() const { return "야옹~"; }
    std::string name()  const { return "고양이"; }
};

struct Duck : Animal {
    std::string speak() const { return "꽥꽥!"; }
    std::string name()  const { return "오리"; }
    // 오리는 name()을 재정의하지 않으면 "동물"이 출력됨
    // → 여기서는 재정의함
};

int main() {
    Dog   dog;
    Cat   cat;
    Duck  duck;
    Animal unknown;  // 기본 구현 사용

    dog.introduce();       // 강아지: 멍멍!
    cat.introduce();       // 고양이: 야옹~
    duck.introduce();      // 오리: 꽥꽥!
    unknown.introduce();   // 동물: ...

    std::cout << "\n--- 강아지가 3번 말하기 ---\n";
    dog.speak_times(3);
    // 강아지: 멍멍!
    // 강아지: 멍멍!
    // 강아지: 멍멍!
}
```

**실습 2: 재귀 람다로 피보나치 수열 계산**

메모이제이션(memoization)까지 적용한 재귀 람다를 만들어 봅시다.

```cpp
#include <iostream>
#include <unordered_map>

int main() {
    // ✅ 캐시를 활용하는 재귀 람다 (메모이제이션 피보나치)
    std::unordered_map<int, long long> cache;

    auto fib = [&cache](this auto self, int n) -> long long {
        if (n <= 1) return n;

        // 캐시에 있으면 바로 반환
        if (auto it = cache.find(n); it != cache.end()) {
            return it->second;
        }

        // 없으면 계산 후 캐시에 저장
        long long result = self(n - 1) + self(n - 2);
        cache[n] = result;
        return result;
    };

    // 피보나치 수열 출력
    std::cout << "피보나치 수열 (0~15번째):\n";
    for (int i = 0; i <= 15; ++i) {
        std::cout << "fib(" << i << ") = " << fib(i) << "\n";
    }

    // 큰 수도 빠르게 계산 (메모이제이션 덕분에)
    cache.clear();
    std::cout << "\nfib(50) = " << fib(50) << "\n";
}
```

**예상 출력 (일부):**

```
피보나치 수열 (0~15번째):
fib(0) = 0
fib(1) = 1
fib(2) = 1
fib(3) = 2
fib(4) = 3
fib(5) = 5
fib(6) = 8
fib(7) = 13
fib(8) = 21
fib(9) = 34
fib(10) = 55
...
fib(15) = 610

fib(50) = 12586269025
```

**실습 3: 체이닝 빌더로 HTML 생성기 만들기**

앞서 배운 빌더 패턴을 응용해서 간단한 HTML 태그 생성기를 만들어 봅시다.

```cpp
#include <iostream>
#include <string>
#include <vector>

struct HtmlElement {
    std::string tag;
    std::string content;
    std::vector<std::pair<std::string, std::string>> attrs;

    std::string render() const {
        std::string result = "<" + tag;
        for (const auto& [key, val] : attrs) {
            result += " " + key + "=\"" + val + "\"";
        }
        result += ">" + content + "</" + tag + ">";
        return result;
    }
};

struct HtmlBuilder {
    HtmlElement elem;

    HtmlBuilder(std::string tag) {
        elem.tag = std::move(tag);
    }

    // deducing this로 체이닝 유지
    auto& text(this auto& self, std::string content) {
        self.elem.content = std::move(content);
        return self;
    }

    auto& attr(this auto& self, std::string key, std::string value) {
        self.elem.attrs.emplace_back(std::move(key), std::move(value));
        return self;
    }

    std::string build(this const auto& self) {
        return self.elem.render();
    }
};

int main() {
    auto html = HtmlBuilder{"a"}
        .attr("href", "https://example.com")
        .attr("class", "link")
        .text("여기를 클릭하세요")
        .build();

    std::cout << html << "\n";
    // <a href="https://example.com" class="link">여기를 클릭하세요</a>

    auto img = HtmlBuilder{"img"}
        .attr("src", "photo.jpg")
        .attr("alt", "사진")
        .attr("width", "300")
        .build();

    std::cout << img << "\n";
    // <img src="photo.jpg" alt="사진" width="300"></img>
}
```

---

## 📌 이 장의 핵심 요약

이 장에서 다룬 **Deducing `this`**는 C++23에서 가장 실용적이고 영향력 있는 기능 중 하나입니다.

**해결한 문제들**을 정리하면 다음과 같습니다. 첫째로 `const`/`non-const` 멤버 함수 오버로딩의 코드 중복을 `this auto& self` 하나로 해결합니다. 둘째로 CRTP의 복잡한 문법(`Base<Derived>`, `static_cast<Derived*>(this)`)을 단순한 상속으로 대체합니다. 셋째로 `std::function` 없이도 진정한 재귀 람다를 작성할 수 있게 합니다. 넷째로 빌더 패턴의 상속 체이닝 문제를 자연스럽게 해결합니다.

**핵심 문법 정리:**

| 문법 | 의미 |
|------|------|
| `void f(this MyClass& self)` | 명시적 객체 파라미터 (non-const) |
| `void f(this const MyClass& self)` | 명시적 객체 파라미터 (const) |
| `void f(this auto& self)` | 추론되는 객체 파라미터 (const 여부 자동 결정) |
| `void f(this auto self)` | 값으로 받기 (복사/이동 시 유용) |
| `auto& f(this auto& self)` | 체이닝을 위해 self 참조 반환 |

`deducing this`는 단순히 문법 설탕(syntactic sugar)이 아닙니다. 기존에 불가능했거나 매우 복잡했던 패턴들을 자연스럽고 직관적으로 표현할 수 있게 해주는 진정한 게임 체인저입니다.  