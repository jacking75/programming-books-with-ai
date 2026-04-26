# Rust처럼 안전한 Modern C++ 프로그래밍  

저자: 최흥배, Claude AI   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **컴파일러**: MSVC v143 (C++20 지원)
- **OS**: Windows 10 이상

-----  
  
# 부록 A: C++26 컴파일러 지원 현황

C++26 표준은 아직 공식 승인 전이지만, 주요 컴파일러들은 이미 많은 기능을 구현하기 시작했다. 이 부록에서는 2025년 11월 현재 GCC, Clang, MSVC의 C++26 기능 지원 현황을 정리한다. 실제 프로젝트에서 특정 기능을 사용하기 전에 이 표를 참고하여 타겟 컴파일러가 해당 기능을 지원하는지 확인할 수 있다.

## A.1 GCC 14+ 기능별 지원 현황

GCC는 전통적으로 C++ 표준 기능을 빠르게 도입하는 편이다. GCC 14부터 C++26 기능들이 실험적으로 추가되기 시작했으며, GCC 15에서는 더 많은 기능이 안정화되었다.

### A.1.1 GCC 14.1 지원 기능

GCC 14.1은 C++26의 기초적인 기능들을 도입했다.

**Pack Indexing (P2662R3)**
- 지원 버전: GCC 14.1+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
template<typename... Ts>
void process() {
    using FirstType = Ts...[0];  // GCC 14.1+에서 작동
    using SecondType = Ts...[1];
    
    static_assert(std::is_same_v<FirstType, int>);
}

process<int, double, char>();  // OK in GCC 14.1+
```

**Placeholder Variables with no name (P2169R4)**
- 지원 버전: GCC 14.1+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
auto [_, value] = std::pair{42, "hello"};  // GCC 14.1+
// _는 무시되는 변수로 사용 가능
```

### A.1.2 GCC 14.2 지원 기능

GCC 14.2에서는 추가적인 언어 기능과 라이브러리 기능이 구현되었다.

**Static constexpr variables in constexpr functions (P2647R1)**
- 지원 버전: GCC 14.2+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
constexpr int compute() {
    static constexpr int cache = 42;  // GCC 14.2+에서 작동
    return cache;
}
```

**Delete Reason (P2573R2)**
- 지원 버전: GCC 14.2+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
class Resource {
public:
    Resource(const Resource&) = delete("복사는 리소스 누수를 일으킬 수 있다");
    // GCC 14.2+는 삭제 이유를 진단 메시지로 표시
};
```

### A.1.3 GCC 15 지원 기능 (실험적)

GCC 15는 2025년 중반에 릴리스되었으며, 더 많은 C++26 기능을 포함한다.

**std::inplace_vector (P0843R14)**
- 지원 버전: GCC 15 (실험적)
- 컴파일 플래그: `-std=c++26`
- 상태: 부분 구현
- 제약사항: trivially copyable 타입에만 완전히 작동

```cpp
#include <inplace_vector>

std::inplace_vector<int, 10> vec;  // GCC 15+
vec.push_back(42);
```

**Reflection (P2996R5 - 부분)**
- 지원 버전: GCC 15 (실험적)
- 컴파일 플래그: `-std=c++26 -freflection`
- 상태: 매우 제한적인 구현
- 제약사항: 기본적인 타입 정보만 제공

```cpp
// GCC 15는 제한적인 리플렉션만 지원
template<typename T>
void inspect() {
    // 기본 기능만 작동
}
```

### A.1.4 GCC 지원 현황 요약표

| 기능 | P번호 | GCC 14.1 | GCC 14.2 | GCC 15 | 비고 |
|------|-------|----------|----------|--------|------|
| Pack Indexing | P2662R3 | ✓ | ✓ | ✓ | 완전 지원 |
| Placeholder Variables | P2169R4 | ✓ | ✓ | ✓ | 완전 지원 |
| Static constexpr in constexpr | P2647R1 | ✗ | ✓ | ✓ | 14.2부터 |
| Delete Reason | P2573R2 | ✗ | ✓ | ✓ | 14.2부터 |
| std::inplace_vector | P0843R14 | ✗ | ✗ | ⚠ | 부분 지원 |
| Hazard Pointers | P2530R3 | ✗ | ✗ | ✗ | 미지원 |
| std::execution | P2300R9 | ✗ | ✗ | ⚠ | 기본 기능만 |
| Reflection | P2996R5 | ✗ | ✗ | ⚠ | 실험적 |
| Contract Annotations | P2900R6 | ✗ | ✗ | ✗ | 미지원 |

범례:
- ✓ : 완전 지원
- ⚠ : 부분 지원 또는 실험적
- ✗ : 미지원

## A.2 Clang 18+ 기능별 지원 현황

Clang은 LLVM 기반의 컴파일러로, C++ 표준 기능을 적극적으로 구현한다. Clang 18부터 C++26 지원이 시작되었다.

### A.2.1 Clang 18.1 지원 기능

Clang 18.1은 2024년 초에 릴리스되어 초기 C++26 기능들을 포함한다.

**Pack Indexing (P2662R3)**
- 지원 버전: Clang 18.1+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
template<std::size_t I, typename... Ts>
using PackElement = Ts...[I];  // Clang 18.1+에서 작동

using Type = PackElement<0, int, double, char>;
static_assert(std::is_same_v<Type, int>);
```

**Placeholder Variables (P2169R4)**
- 지원 버전: Clang 18.1+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
auto [_, x, _] = std::tuple{1, 2, 3};  // Clang 18.1+
// 첫 번째와 세 번째 값은 무시됨
```

### A.2.2 Clang 19.1 지원 기능

Clang 19.1은 2025년 상반기에 릴리스되어 더 많은 기능을 추가했다.

**Static constexpr in constexpr functions (P2647R1)**
- 지원 버전: Clang 19.1+
- 컴파일 플래그: `-std=c++26`
- 상태: 완전 구현

```cpp
constexpr int fibonacci(int n) {
    static constexpr int cache[10] = {0, 1, 1, 2, 3, 5, 8, 13, 21, 34};
    return cache[n];  // Clang 19.1+에서 작동
}
```

**std::inplace_vector (P0843R14)**
- 지원 버전: Clang 19.1+
- 컴파일 플래그: `-std=c++26 -stdlib=libc++`
- 상태: 거의 완전 구현
- 제약사항: libc++ 19+ 필요

```cpp
#include <inplace_vector>

std::inplace_vector<std::string, 5> names;  // Clang 19.1 + libc++ 19
names.push_back("Alice");
names.push_back("Bob");
```

**Hazard Pointers (P2530R3) - 부분**
- 지원 버전: Clang 19.1+
- 컴파일 플래그: `-std=c++26 -stdlib=libc++`
- 상태: 기본 기능만 구현
- 제약사항: 고급 기능 미지원

```cpp
#include <hazard_pointer>

// Clang 19.1에서 기본 사용만 가능
std::hazard_pointer_domain domain;
```

### A.2.3 Clang 20 (개발 중)

Clang 20은 2025년 하반기 릴리스 예정이며, trunk 버전에서 일부 기능을 테스트할 수 있다.

**std::execution (P2300R9) - 실험적**
- 지원 버전: Clang 20 trunk
- 컴파일 플래그: `-std=c++26 -stdlib=libc++ -fexperimental-library`
- 상태: 매우 초기 구현
- 제약사항: API가 변경될 수 있음

```cpp
// Clang 20 trunk에서만 테스트 가능
#include <execution>

namespace ex = std::execution;
// 매우 제한적인 기능만 작동
```

### A.2.4 Clang 지원 현황 요약표

| 기능 | P번호 | Clang 18 | Clang 19 | Clang 20 | 비고 |
|------|-------|----------|----------|----------|------|
| Pack Indexing | P2662R3 | ✓ | ✓ | ✓ | 완전 지원 |
| Placeholder Variables | P2169R4 | ✓ | ✓ | ✓ | 완전 지원 |
| Static constexpr in constexpr | P2647R1 | ✗ | ✓ | ✓ | 19.1부터 |
| Delete Reason | P2573R2 | ✗ | ✓ | ✓ | 19.1부터 |
| std::inplace_vector | P0843R14 | ✗ | ✓ | ✓ | libc++ 필요 |
| Hazard Pointers | P2530R3 | ✗ | ⚠ | ⚠ | 부분 지원 |
| std::execution | P2300R9 | ✗ | ✗ | ⚠ | 실험적 |
| Reflection | P2996R5 | ✗ | ✗ | ⚠ | 매우 제한적 |
| Contract Annotations | P2900R6 | ✗ | ✗ | ✗ | 미지원 |

범례:
- ✓ : 완전 지원
- ⚠ : 부분 지원 또는 실험적
- ✗ : 미지원

## A.3 MSVC 2025+ 지원 현황

Microsoft Visual C++ 컴파일러는 Visual Studio 2025부터 C++26 기능 지원을 시작했다. MSVC는 전통적으로 표준 구현이 다른 컴파일러보다 느린 편이지만, 최근에는 개선되고 있다.

### A.3.1 Visual Studio 2025 (17.12) 지원 기능

Visual Studio 2025의 첫 번째 정식 릴리스는 2025년 상반기에 출시되었다.

**Pack Indexing (P2662R3)**
- 지원 버전: VS 2025 17.12+
- 컴파일 플래그: `/std:c++latest`
- 상태: 완전 구현

```cpp
template<typename... Ts>
auto get_first() -> Ts...[0] {  // VS 2025 17.12+
    // 구현
}
```

**Placeholder Variables (P2169R4)**
- 지원 버전: VS 2025 17.12+
- 컴파일 플래그: `/std:c++latest`
- 상태: 완전 구현

```cpp
auto [_, value] = get_pair();  // VS 2025 17.12+
```

**Static constexpr in constexpr (P2647R1)**
- 지원 버전: VS 2025 17.12+
- 컴파일 플래그: `/std:c++latest`
- 상태: 완전 구현

```cpp
constexpr auto compute() {
    static constexpr int value = 42;  // VS 2025 17.12+
    return value;
}
```

### A.3.2 Visual Studio 2025 (17.13) 지원 기능

VS 2025의 마이너 업데이트에서 추가 기능이 구현되었다.

**Delete Reason (P2573R2)**
- 지원 버전: VS 2025 17.13+
- 컴파일 플래그: `/std:c++latest`
- 상태: 완전 구현

```cpp
class NonCopyable {
    NonCopyable(const NonCopyable&) = delete("복사 불가능한 리소스");
    // VS 2025 17.13+는 에러 메시지에 이유 표시
};
```

### A.3.3 Visual Studio 2025 미지원 기능

다음 기능들은 VS 2025에서 아직 지원되지 않는다.

**std::inplace_vector**
- 상태: 미구현
- 예상 지원: VS 2026 이후

**Hazard Pointers**
- 상태: 미구현
- 예상 지원: 미정

**std::execution**
- 상태: 미구현
- 예상 지원: VS 2026 이후

**Reflection**
- 상태: 미구현
- 예상 지원: 미정

### A.3.4 MSVC 지원 현황 요약표

| 기능 | P번호 | VS 2025 17.12 | VS 2025 17.13 | 비고 |
|------|-------|---------------|---------------|------|
| Pack Indexing | P2662R3 | ✓ | ✓ | 완전 지원 |
| Placeholder Variables | P2169R4 | ✓ | ✓ | 완전 지원 |
| Static constexpr in constexpr | P2647R1 | ✓ | ✓ | 완전 지원 |
| Delete Reason | P2573R2 | ✗ | ✓ | 17.13부터 |
| std::inplace_vector | P0843R14 | ✗ | ✗ | 미지원 |
| Hazard Pointers | P2530R3 | ✗ | ✗ | 미지원 |
| std::execution | P2300R9 | ✗ | ✗ | 미지원 |
| Reflection | P2996R5 | ✗ | ✗ | 미지원 |
| Contract Annotations | P2900R6 | ✗ | ✗ | 미지원 |

범례:
- ✓ : 완전 지원
- ⚠ : 부분 지원 또는 실험적
- ✗ : 미지원

## A.4 크로스 플랫폼 개발 가이드

다양한 컴파일러와 플랫폼을 지원해야 하는 프로젝트에서는 기능 탐지와 조건부 컴파일이 필수적이다.

### A.4.1 기능 탐지 매크로

각 컴파일러는 지원하는 기능을 나타내는 기능 테스트 매크로를 제공한다.

```cpp
// Pack Indexing 지원 확인
#if __cpp_pack_indexing >= 202311L
    // Pack indexing 사용 가능
    template<typename... Ts>
    using First = Ts...[0];
#else
    // 대체 구현
    template<typename... Ts>
    using First = std::tuple_element_t<0, std::tuple<Ts...>>;
#endif
```

```cpp
// inplace_vector 지원 확인
#ifdef __cpp_lib_inplace_vector
    #include <inplace_vector>
    template<typename T, std::size_t N>
    using StackVector = std::inplace_vector<T, N>;
#else
    // 대체 구현으로 boost::static_vector 또는 자체 구현 사용
    #include <boost/container/static_vector.hpp>
    template<typename T, std::size_t N>
    using StackVector = boost::container::static_vector<T, N>;
#endif
```

### A.4.2 컴파일러별 조건부 컴파일

특정 컴파일러에서만 기능을 사용해야 할 때는 컴파일러 탐지 매크로를 활용한다.

```cpp
// GCC 15+ 또는 Clang 19+에서만 inplace_vector 사용
#if (defined(__GNUC__) && __GNUC__ >= 15) || \
    (defined(__clang__) && __clang_major__ >= 19)
    #define HAS_INPLACE_VECTOR 1
    #include <inplace_vector>
#else
    #define HAS_INPLACE_VECTOR 0
#endif

template<typename T, std::size_t N>
class SafeVector {
#if HAS_INPLACE_VECTOR
    std::inplace_vector<T, N> storage;
#else
    std::array<T, N> storage;
    std::size_t size_ = 0;
#endif
    
public:
    void push_back(const T& value) {
#if HAS_INPLACE_VECTOR
        storage.push_back(value);
#else
        if (size_ < N) {
            storage[size_++] = value;
        }
#endif
    }
};
```

### A.4.3 최소 지원 버전 권장사항

실무 프로젝트에서는 다음과 같은 컴파일러 버전을 최소 요구사항으로 설정하는 것을 권장한다.

**2025년 하반기 기준 권장 최소 버전**
- GCC 14.2 이상
- Clang 19.1 이상 (libc++ 19 포함)
- MSVC 2025 17.13 이상

이 버전들은 다음 핵심 기능을 지원한다.
- Pack Indexing
- Placeholder Variables
- Static constexpr in constexpr functions
- Delete Reason

**고급 기능이 필요한 경우**
- GCC 15+ (inplace_vector 부분 지원)
- Clang 19.1+ with libc++ 19 (inplace_vector 완전 지원)
- MSVC: 현재 미지원, 대체 라이브러리 필요

### A.4.4 CMake 설정 예제

CMake를 사용하여 컴파일러 버전을 확인하고 적절한 플래그를 설정할 수 있다.

```cmake
cmake_minimum_required(VERSION 3.25)
project(SafeCppProject CXX)

# C++26 요구
set(CMAKE_CXX_STANDARD 26)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 컴파일러 버전 확인
if(CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    if(CMAKE_CXX_COMPILER_VERSION VERSION_LESS "14.2")
        message(FATAL_ERROR "GCC 14.2 이상이 필요하다")
    endif()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++26")
    
elseif(CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    if(CMAKE_CXX_COMPILER_VERSION VERSION_LESS "19.1")
        message(FATAL_ERROR "Clang 19.1 이상이 필요하다")
    endif()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++26 -stdlib=libc++")
    
elseif(CMAKE_CXX_COMPILER_ID STREQUAL "MSVC")
    if(CMAKE_CXX_COMPILER_VERSION VERSION_LESS "19.43")
        message(FATAL_ERROR "MSVC 2025 17.13 이상이 필요하다")
    endif()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /std:c++latest")
endif()

# 기능별 탐지
include(CheckCXXSourceCompiles)

# Pack Indexing 지원 확인
check_cxx_source_compiles("
    template<typename... Ts>
    using First = Ts...[0];
    int main() {}
" HAS_PACK_INDEXING)

if(HAS_PACK_INDEXING)
    add_compile_definitions(HAS_PACK_INDEXING=1)
endif()

# inplace_vector 지원 확인
check_cxx_source_compiles("
    #include <inplace_vector>
    int main() {
        std::inplace_vector<int, 10> v;
    }
" HAS_INPLACE_VECTOR)

if(HAS_INPLACE_VECTOR)
    add_compile_definitions(HAS_INPLACE_VECTOR=1)
else()
    # 대체 라이브러리 찾기
    find_package(Boost REQUIRED)
    include_directories(${Boost_INCLUDE_DIRS})
endif()
```

## A.5 온라인 컴파일러 지원 현황

개발 환경을 구축하지 않고 C++26 기능을 테스트하려면 온라인 컴파일러를 활용할 수 있다.

### A.5.1 Compiler Explorer (godbolt.org)

Compiler Explorer는 가장 인기 있는 온라인 C++ 컴파일러다.

**지원 컴파일러 (2025년 11월 기준)**
- GCC trunk, 14.2, 14.1
- Clang trunk, 19.1, 18.1
- MSVC 19.latest

**사용 방법**
1. https://godbolt.org 접속
2. 컴파일러 선택 (예: GCC 14.2)
3. 컴파일러 옵션에 `-std=c++26` 추가
4. 코드 작성 및 테스트

**예제 코드**
```cpp
#include <iostream>

template<typename... Ts>
void show_types() {
    using First = Ts...[0];
    using Second = Ts...[1];
    
    std::cout << "First type size: " << sizeof(First) << "\n";
    std::cout << "Second type size: " << sizeof(Second) << "\n";
}

int main() {
    show_types<int, double, char>();
}
```

### A.5.2 Quick C++ Bench (quick-bench.com)

성능 비교가 필요할 때 사용할 수 있다.

**지원 컴파일러**
- GCC 14.2
- Clang 19.1

**제약사항**
- C++26 기능이 제한적으로 지원됨
- 실험적 기능은 사용 불가능

### A.5.3 C++ Insights (cppinsights.io)

C++ 코드가 컴파일러에 의해 어떻게 변환되는지 확인할 수 있다.

**지원 기능**
- 템플릿 인스턴스화 시각화
- Pack indexing 확장 보기
- 암묵적 변환 확인

**제약사항**
- Clang 기반이므로 GCC/MSVC 특화 기능 미지원
- C++26 지원이 제한적

## A.6 지원 현황 업데이트 추적

C++26 기능 지원 현황은 계속 변화하므로 최신 정보를 확인해야 한다.

### A.6.1 공식 지원 현황 페이지

각 컴파일러는 공식 지원 현황 페이지를 제공한다.

**GCC C++ 지원 현황**
- URL: https://gcc.gnu.org/projects/cxx-status.html
- C++26 섹션에서 각 제안서별 구현 상태 확인 가능

**Clang C++ 지원 현황**
- URL: https://clang.llvm.org/cxx_status.html
- 각 언어 기능별 구현 버전 명시

**MSVC C++ 지원 현황**
- URL: https://learn.microsoft.com/en-us/cpp/overview/visual-cpp-language-conformance
- Visual Studio 버전별 기능 지원 표 제공

### A.6.2 cppreference.com

cppreference.com은 각 기능의 컴파일러 지원 버전을 문서화한다.

**예제: Pack Indexing 페이지**
- URL: https://en.cppreference.com/w/cpp/language/pack_indexing
- 페이지 하단의 "Compiler support" 섹션 참조

### A.6.3 기능별 제안서 추적

특정 기능의 구현 상태를 자세히 알고 싶다면 제안서를 직접 확인할 수 있다.

**C++ 표준 위원회 페이지**
- URL: https://wg21.link/
- 예: https://wg21.link/p2662 (Pack Indexing)

각 제안서는 구현 경험과 컴파일러 피드백을 포함한다.

## A.7 실무 적용 전략

컴파일러 지원 현황을 고려한 실무 적용 전략을 제시한다.

### A.7.1 단계별 도입 전략

**1단계: 안정적 기능만 사용 (2025년 하반기)**
- Pack Indexing
- Placeholder Variables
- Static constexpr in constexpr
- Delete Reason

이 기능들은 GCC 14.2+, Clang 19.1+, MSVC 2025 17.13+에서 모두 지원된다.

```cpp
// 1단계에서 안전하게 사용 가능한 코드
template<typename... Ts>
class TypeList {
    using First = Ts...[0];  // 모든 주요 컴파일러 지원
    
    static constexpr std::size_t size = sizeof...(Ts);
};
```

**2단계: 조건부로 고급 기능 사용 (2025년 말 ~ 2026년 초)**
- std::inplace_vector (Clang 19.1+에서만 완전 지원)
- 기타 컴파일러에서는 대체 구현 사용

```cpp
#ifdef __cpp_lib_inplace_vector
    #include <inplace_vector>
    using Buffer = std::inplace_vector<char, 1024>;
#else
    #include "fallback/static_vector.hpp"
    using Buffer = StaticVector<char, 1024>;
#endif
```

**3단계: 실험적 기능 평가 (2026년 이후)**
- std::execution
- Reflection
- 프로덕션 코드에는 아직 사용 금지

### A.7.2 CI/CD 파이프라인 설정

여러 컴파일러에서 테스트하는 것이 중요하다.

```yaml
# GitHub Actions 예제
name: Multi-Compiler Test

on: [push, pull_request]

jobs:
  test:
    strategy:
      matrix:
        compiler:
          - { name: GCC, version: 14.2, cc: gcc-14, cxx: g++-14 }
          - { name: GCC, version: 15, cc: gcc-15, cxx: g++-15 }
          - { name: Clang, version: 19, cc: clang-19, cxx: clang++-19 }
        build_type: [Debug, Release]
    
    runs-on: ubuntu-24.04
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Install ${{ matrix.compiler.name }} ${{ matrix.compiler.version }}
        run: |
          # 컴파일러 설치 스크립트
          
      - name: Configure
        run: |
          cmake -B build \
            -DCMAKE_BUILD_TYPE=${{ matrix.build_type }} \
            -DCMAKE_C_COMPILER=${{ matrix.compiler.cc }} \
            -DCMAKE_CXX_COMPILER=${{ matrix.compiler.cxx }}
            
      - name: Build
        run: cmake --build build
        
      - name: Test
        run: cd build && ctest --output-on-failure
```

### A.7.3 팀 개발 가이드라인

팀에서 C++26 기능을 사용할 때는 명확한 가이드라인이 필요하다.

**권장 가이드라인 예시**

```cpp
// project_guidelines.hpp
// 프로젝트에서 사용 가능한 C++26 기능 정의

// 필수 컴파일러 버전
// - GCC 14.2+
// - Clang 19.1+ with libc++
// - MSVC 2025 17.13+

// 레벨 1: 모든 컴파일러에서 사용 가능
#define LEVEL1_PACK_INDEXING 1
#define LEVEL1_PLACEHOLDER_VAR 1
#define LEVEL1_STATIC_CONSTEXPR 1
#define LEVEL1_DELETE_REASON 1

// 레벨 2: 조건부 사용 (대체 구현 필수)
#ifdef __cpp_lib_inplace_vector
    #define LEVEL2_INPLACE_VECTOR 1
#else
    #define LEVEL2_INPLACE_VECTOR 0
#endif

// 레벨 3: 사용 금지 (아직 불안정)
#define LEVEL3_EXECUTION 0
#define LEVEL3_REFLECTION 0

// 사용 예시
#if LEVEL1_PACK_INDEXING
    template<typename... Ts>
    using First = Ts...[0];  // OK: 모든 컴파일러 지원
#endif

#if LEVEL2_INPLACE_VECTOR
    // OK: 대체 구현 존재
    std::inplace_vector<int, 10> vec;
#else
    StaticVector<int, 10> vec;  // 대체 구현
#endif

#if LEVEL3_EXECUTION
    #error "std::execution은 아직 사용 금지"
#endif
```

이러한 접근 방식을 통해 팀 전체가 일관된 방식으로 C++26 기능을 도입할 수 있다.

  
---  

# 부록 B: 마이그레이션 가이드

기존 C++ 코드베이스를 안전한 현대적 C++로 전환하는 것은 도전적인 작업이다. 이 부록에서는 레거시 코드를 점진적으로 개선하여 안전성을 높이는 실용적인 전략을 제공한다. 한 번에 모든 것을 바꾸려 하지 말고, 단계적으로 접근하면 리스크를 최소화하면서 안전성을 향상시킬 수 있다.

## B.1 기존 C++ 코드를 안전하게 업그레이드하는 방법

레거시 코드베이스를 현대적이고 안전한 C++로 전환하려면 체계적인 접근이 필요하다. 무작정 코드를 수정하기보다는 현재 상태를 파악하고 우선순위를 정해야 한다.

### B.1.1 현재 코드베이스 분석

마이그레이션을 시작하기 전에 코드의 현재 상태를 정확히 파악해야 한다.

**정적 분석 도구 실행**

먼저 정적 분석 도구를 사용하여 잠재적 문제를 식별한다.

```bash
# Clang-Tidy로 전체 코드베이스 분석
clang-tidy src/**/*.cpp -- -std=c++20 \
  -checks='modernize-*,cppcoreguidelines-*,readability-*'

# 결과를 파일로 저장
clang-tidy src/**/*.cpp -- -std=c++20 \
  -checks='modernize-*,cppcoreguidelines-*' \
  > analysis_results.txt
```

**위험도 높은 패턴 검색**

코드베이스에서 위험한 패턴을 직접 검색한다.

```bash
# 원시 포인터를 사용하는 new/delete 찾기
grep -r "new " src/ | wc -l
grep -r "delete " src/ | wc -l

# C 스타일 캐스트 찾기
grep -r "([a-z_]*\*)" src/ | wc -l

# 배열 인덱싱 찾기
grep -r "\[.*\]" src/ | wc -l

# NULL 사용 찾기 (nullptr 대신)
grep -r "NULL" src/ | wc -l
```

**분석 스크립트 예제**

코드베이스의 안전성 지표를 자동으로 수집하는 Python 스크립트를 작성한다.

```python
#!/usr/bin/env python3
import os
import re
from collections import defaultdict

def analyze_codebase(root_dir):
    stats = defaultdict(int)
    
    for root, dirs, files in os.walk(root_dir):
        for file in files:
            if not file.endswith(('.cpp', '.h', '.hpp')):
                continue
                
            filepath = os.path.join(root, file)
            with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
                
                # 위험한 패턴 카운트
                stats['raw_new'] += len(re.findall(r'\bnew\s+', content))
                stats['raw_delete'] += len(re.findall(r'\bdelete\s+', content))
                stats['c_cast'] += len(re.findall(r'\([a-zA-Z_]\w*\s*\*\)', content))
                stats['null_macro'] += len(re.findall(r'\bNULL\b', content))
                stats['array_access'] += len(re.findall(r'\w+\[[\w\s+\-*/%]+\]', content))
                
                # 안전한 패턴 카운트
                stats['unique_ptr'] += len(re.findall(r'std::unique_ptr', content))
                stats['shared_ptr'] += len(re.findall(r'std::shared_ptr', content))
                stats['vector'] += len(re.findall(r'std::vector', content))
                stats['nullptr'] += len(re.findall(r'\bnullptr\b', content))
                
                stats['total_files'] += 1
    
    return stats

def print_report(stats):
    print("=== 코드베이스 안전성 분석 ===\n")
    print(f"총 파일 수: {stats['total_files']}\n")
    
    print("위험한 패턴:")
    print(f"  raw new:        {stats['raw_new']}")
    print(f"  raw delete:     {stats['raw_delete']}")
    print(f"  C-style cast:   {stats['c_cast']}")
    print(f"  NULL 매크로:    {stats['null_macro']}")
    print(f"  배열 인덱싱:    {stats['array_access']}\n")
    
    print("안전한 패턴:")
    print(f"  unique_ptr:     {stats['unique_ptr']}")
    print(f"  shared_ptr:     {stats['shared_ptr']}")
    print(f"  vector:         {stats['vector']}")
    print(f"  nullptr:        {stats['nullptr']}\n")
    
    # 안전성 점수 계산 (0-100)
    danger_score = (stats['raw_new'] + stats['raw_delete'] + 
                   stats['c_cast'] + stats['null_macro'])
    safety_score = (stats['unique_ptr'] + stats['shared_ptr'] + 
                   stats['vector'] + stats['nullptr'])
    
    if danger_score + safety_score > 0:
        safety_percentage = (safety_score / (danger_score + safety_score)) * 100
        print(f"안전성 점수: {safety_percentage:.1f}%")

if __name__ == '__main__':
    stats = analyze_codebase('src')
    print_report(stats)
```

### B.1.2 우선순위 결정

모든 코드를 한 번에 수정할 수 없으므로 우선순위를 정해야 한다.

**위험도 매트릭스**

각 파일이나 모듈을 위험도와 중요도에 따라 분류한다.

```cpp
// risk_matrix.hpp
#pragma once
#include <string>
#include <vector>
#include <map>

struct ModuleRisk {
    std::string name;
    int unsafe_new_count = 0;
    int unsafe_delete_count = 0;
    int c_cast_count = 0;
    int array_access_count = 0;
    int null_dereference_risk = 0;
    
    // 총 위험 점수 계산
    int total_risk() const {
        return unsafe_new_count * 3 +    // new/delete는 높은 위험
               unsafe_delete_count * 3 +
               c_cast_count * 2 +         // 캐스트는 중간 위험
               array_access_count * 1 +   // 배열은 낮은 위험
               null_dereference_risk * 5; // null 역참조는 최고 위험
    }
    
    // 우선순위 계산 (높을수록 먼저 수정)
    enum class Priority { LOW, MEDIUM, HIGH, CRITICAL };
    
    Priority calculate_priority(int business_importance) const {
        int risk = total_risk();
        int combined = risk * business_importance;
        
        if (combined > 100) return Priority::CRITICAL;
        if (combined > 50) return Priority::HIGH;
        if (combined > 20) return Priority::MEDIUM;
        return Priority::LOW;
    }
};

class MigrationPlanner {
    std::map<std::string, ModuleRisk> modules;
    std::map<std::string, int> business_importance;
    
public:
    void add_module(const std::string& name, const ModuleRisk& risk) {
        modules[name] = risk;
    }
    
    void set_business_importance(const std::string& name, int importance) {
        business_importance[name] = importance;
    }
    
    std::vector<std::string> get_migration_order() const {
        std::vector<std::pair<std::string, int>> scored;
        
        for (const auto& [name, risk] : modules) {
            int importance = business_importance.count(name) 
                ? business_importance.at(name) : 5;
            int score = risk.total_risk() * importance;
            scored.push_back({name, score});
        }
        
        // 점수 기준 정렬 (높은 것부터)
        std::sort(scored.begin(), scored.end(),
            [](const auto& a, const auto& b) {
                return a.second > b.second;
            });
        
        std::vector<std::string> order;
        for (const auto& [name, _] : scored) {
            order.push_back(name);
        }
        return order;
    }
    
    void print_plan() const {
        std::cout << "=== 마이그레이션 우선순위 ===\n\n";
        
        auto order = get_migration_order();
        for (size_t i = 0; i < order.size(); ++i) {
            const auto& name = order[i];
            const auto& risk = modules.at(name);
            int importance = business_importance.count(name) 
                ? business_importance.at(name) : 5;
            
            std::cout << (i + 1) << ". " << name << "\n";
            std::cout << "   위험도: " << risk.total_risk() << "\n";
            std::cout << "   중요도: " << importance << "\n";
            std::cout << "   우선순위: ";
            
            auto priority = risk.calculate_priority(importance);
            switch (priority) {
                case ModuleRisk::Priority::CRITICAL:
                    std::cout << "긴급 (즉시 수정 필요)\n";
                    break;
                case ModuleRisk::Priority::HIGH:
                    std::cout << "높음 (이번 스프린트)\n";
                    break;
                case ModuleRisk::Priority::MEDIUM:
                    std::cout << "중간 (다음 스프린트)\n";
                    break;
                case ModuleRisk::Priority::LOW:
                    std::cout << "낮음 (여유 있을 때)\n";
                    break;
            }
            std::cout << "\n";
        }
    }
};
```

**사용 예제**

```cpp
// analyze_project.cpp
#include "risk_matrix.hpp"

int main() {
    MigrationPlanner planner;
    
    // 각 모듈의 위험도 분석 결과 입력
    ModuleRisk auth_module;
    auth_module.name = "authentication";
    auth_module.unsafe_new_count = 15;
    auth_module.unsafe_delete_count = 15;
    auth_module.c_cast_count = 8;
    auth_module.null_dereference_risk = 12;
    
    ModuleRisk ui_module;
    ui_module.name = "user_interface";
    ui_module.unsafe_new_count = 5;
    ui_module.array_access_count = 30;
    
    ModuleRisk logging_module;
    logging_module.name = "logging";
    logging_module.c_cast_count = 3;
    logging_module.array_access_count = 10;
    
    planner.add_module("authentication", auth_module);
    planner.add_module("user_interface", ui_module);
    planner.add_module("logging", logging_module);
    
    // 비즈니스 중요도 설정 (1-10)
    planner.set_business_importance("authentication", 10);  // 매우 중요
    planner.set_business_importance("user_interface", 7);   // 중요
    planner.set_business_importance("logging", 4);          // 보통
    
    planner.print_plan();
    
    return 0;
}
```

### B.1.3 단계별 리팩토링 체크리스트

각 모듈을 수정할 때 따라야 할 체크리스트다.

**1단계: 간단한 교체 (자동화 가능)**

가장 먼저 기계적으로 교체할 수 있는 부분부터 시작한다.

```cpp
// 변경 전: NULL 사용
int* ptr = NULL;
if (ptr == NULL) {
    // ...
}

// 변경 후: nullptr 사용
int* ptr = nullptr;
if (ptr == nullptr) {
    // ...
}
```

```cpp
// 변경 전: C 스타일 캐스트
double value = 3.14;
int truncated = (int)value;

// 변경 후: static_cast
double value = 3.14;
int truncated = static_cast<int>(value);
```

```cpp
// 변경 전: typedef
typedef std::vector<int> IntVector;

// 변경 후: using
using IntVector = std::vector<int>;
```

자동 변환 스크립트를 사용할 수 있다.

```bash
# clang-tidy로 자동 수정
clang-tidy -fix \
  -checks='-*,modernize-use-nullptr,modernize-use-using' \
  src/myfile.cpp -- -std=c++20
```

**2단계: 원시 포인터를 스마트 포인터로 변경**

소유권이 명확한 원시 포인터부터 변경한다.

```cpp
// 변경 전: 위험한 원시 포인터
class ResourceManager {
    Resource* resource;  // 누가 삭제해야 하는가?
    
public:
    ResourceManager() : resource(new Resource()) {}
    
    ~ResourceManager() {
        delete resource;  // 예외 안전하지 않음
    }
    
    Resource* get_resource() { return resource; }
};

// 변경 후: 명확한 소유권
class ResourceManager {
    std::unique_ptr<Resource> resource;  // 소유권 명확
    
public:
    ResourceManager() : resource(std::make_unique<Resource>()) {}
    
    // 소멸자 자동 생성됨 (예외 안전)
    
    Resource* get_resource() { return resource.get(); }
    
    // 관찰만 하는 경우
    const Resource& view_resource() const { return *resource; }
};
```

**3단계: C 스타일 배열을 std::vector 또는 std::array로 변경**

고정 크기 배열은 std::array로, 동적 배열은 std::vector로 변경한다.

```cpp
// 변경 전: C 스타일 배열
void process_data(int* data, size_t size) {
    for (size_t i = 0; i < size; ++i) {
        data[i] *= 2;  // 경계 검사 없음
    }
}

void old_code() {
    int buffer[100];  // 크기 하드코딩
    process_data(buffer, 100);
}

// 변경 후: 안전한 컨테이너
void process_data(std::span<int> data) {
    for (int& value : data) {  // 범위 기반 for
        value *= 2;
    }
}

void new_code() {
    std::array<int, 100> buffer{};  // 초기화 보장
    process_data(buffer);
    
    // 또는 동적 크기가 필요하면
    std::vector<int> dynamic_buffer(100);
    process_data(dynamic_buffer);
}
```

**4단계: 함수 매개변수 개선**

원시 포인터 매개변수를 참조나 std::span으로 변경한다.

```cpp
// 변경 전: 불명확한 의미
void process(int* values, size_t count);  // nullable? 배열?

// 변경 후: 명확한 의미
void process(std::span<int> values);         // 배열이 확실함
void process_single(int& value);             // 단일 값 (non-null)
void process_optional(int* value);           // 여전히 포인터지만 nullable 의도 명확
void process_optional(std::optional<std::reference_wrapper<int>> value);  // 더 안전
```

**5단계: 에러 처리 개선**

에러 코드를 예외나 std::expected로 변경한다.

```cpp
// 변경 전: 에러 코드
int parse_int(const char* str, int* out_value) {
    if (!str) return -1;
    if (!out_value) return -1;
    
    char* end;
    long value = strtol(str, &end, 10);
    if (end == str) return -2;  // 파싱 실패
    
    *out_value = static_cast<int>(value);
    return 0;  // 성공
}

void old_error_handling() {
    int value;
    int result = parse_int("123", &value);
    if (result != 0) {
        // 에러 처리
    }
}

// 변경 후: std::expected (C++23) 또는 std::optional
std::optional<int> parse_int(std::string_view str) {
    if (str.empty()) return std::nullopt;
    
    try {
        size_t pos;
        int value = std::stoi(std::string(str), &pos);
        if (pos != str.size()) return std::nullopt;
        return value;
    } catch (...) {
        return std::nullopt;
    }
}

void new_error_handling() {
    if (auto value = parse_int("123")) {
        // 성공: *value 사용
    } else {
        // 실패 처리
    }
}
```

### B.1.4 마이그레이션 체크리스트 템플릿

각 파일을 수정할 때 사용할 체크리스트다.

```markdown
# 파일 마이그레이션 체크리스트

파일: ________________
담당자: ________________
날짜: ________________

## 1단계: 기본 현대화
- [ ] NULL → nullptr 변경
- [ ] typedef → using 변경
- [ ] C 스타일 캐스트 → C++ 캐스트
- [ ] #define 상수 → constexpr 변수

## 2단계: 메모리 안전성
- [ ] new/delete → std::unique_ptr/std::shared_ptr
- [ ] 원시 배열 → std::vector/std::array
- [ ] 수동 메모리 관리 → RAII
- [ ] 댕글링 포인터 가능성 제거

## 3단계: 타입 안전성
- [ ] void* 사용 제거
- [ ] C 스타일 가변 인자 → 템플릿/std::initializer_list
- [ ] 암묵적 변환 최소화
- [ ] enum → enum class

## 4단계: 범위 안전성
- [ ] 원시 포인터 매개변수 → std::span/참조
- [ ] 수동 범위 검사 → 컨테이너 메서드
- [ ] 배열 인덱싱 → 범위 기반 for
- [ ] at() 사용 (디버그 모드)

## 5단계: 에러 처리
- [ ] 에러 코드 → 예외/std::optional
- [ ] NULL 체크 → std::optional/std::unique_ptr
- [ ] assert → static_assert (가능한 경우)
- [ ] 예외 안전성 보장 확인

## 6단계: 동시성 안전성
- [ ] 전역 변수 → thread_local/동기화
- [ ] 수동 뮤텍스 → std::lock_guard
- [ ] 원자성 없는 증가 → std::atomic
- [ ] 데이터 레이스 가능성 제거

## 테스트
- [ ] 기존 단위 테스트 통과
- [ ] 새로운 안전성 테스트 추가
- [ ] 메모리 누수 검사 (valgrind/asan)
- [ ] 스레드 세이프티 검사 (tsan)

## 코드 리뷰
- [ ] 동료 리뷰 완료
- [ ] 성능 영향 확인
- [ ] 문서 업데이트
```

## B.2 점진적 도입 전략

한 번에 모든 코드를 바꾸는 것은 위험하다. 점진적으로 안전성을 높이는 전략이 필요하다.

### B.2.1 혼합 코드베이스 관리

레거시 코드와 현대적 코드가 공존하는 상황을 관리하는 방법이다.

**인터페이스 레이어 패턴**

레거시 코드와 새 코드 사이에 중간 레이어를 두어 변환한다.

```cpp
// legacy_api.h (변경 불가능한 레거시 코드)
typedef struct {
    char* data;
    int size;
} LegacyBuffer;

void legacy_process_buffer(LegacyBuffer* buffer);
int legacy_create_buffer(int size, LegacyBuffer** out_buffer);
void legacy_free_buffer(LegacyBuffer* buffer);
```

```cpp
// modern_wrapper.hpp (새로운 안전한 래퍼)
#pragma once
#include "legacy_api.h"
#include <memory>
#include <span>
#include <vector>

// RAII 래퍼로 레거시 리소스 관리
class SafeBuffer {
    std::unique_ptr<LegacyBuffer, decltype(&legacy_free_buffer)> buffer;
    
public:
    explicit SafeBuffer(int size) 
        : buffer(nullptr, &legacy_free_buffer) {
        LegacyBuffer* raw_buffer = nullptr;
        int result = legacy_create_buffer(size, &raw_buffer);
        if (result != 0 || !raw_buffer) {
            throw std::runtime_error("버퍼 생성 실패");
        }
        buffer.reset(raw_buffer);
    }
    
    // 안전한 인터페이스 제공
    std::span<char> data() {
        return std::span<char>(buffer->data, buffer->size);
    }
    
    std::span<const char> data() const {
        return std::span<const char>(buffer->data, buffer->size);
    }
    
    size_t size() const {
        return static_cast<size_t>(buffer->size);
    }
    
    // 레거시 API 호출이 필요한 경우
    void process() {
        legacy_process_buffer(buffer.get());
    }
};

// 완전히 새로운 안전한 API
class ModernBuffer {
    std::vector<char> data_;
    
public:
    explicit ModernBuffer(size_t size) : data_(size) {}
    
    std::span<char> data() { return data_; }
    std::span<const char> data() const { return data_; }
    size_t size() const { return data_.size(); }
    
    // 레거시 API와의 상호 운용이 필요한 경우
    void process_with_legacy() {
        LegacyBuffer legacy_view{
            .data = data_.data(),
            .size = static_cast<int>(data_.size())
        };
        legacy_process_buffer(&legacy_view);
    }
};
```

**사용 예제**

```cpp
// 기존 코드 (레거시)
void old_way() {
    LegacyBuffer* buffer = nullptr;
    int result = legacy_create_buffer(1024, &buffer);
    if (result != 0) {
        // 에러 처리
        return;
    }
    
    legacy_process_buffer(buffer);
    
    legacy_free_buffer(buffer);  // 잊어버리기 쉬움!
}

// 중간 단계 (래퍼 사용)
void transition_way() {
    try {
        SafeBuffer buffer(1024);
        buffer.process();  // 자동으로 정리됨
    } catch (const std::exception& e) {
        // 에러 처리
    }
}

// 최종 목표 (완전히 현대적)
void new_way() {
    ModernBuffer buffer(1024);
    auto data = buffer.data();
    // 안전하게 데이터 사용
}
```

### B.2.2 모듈별 마이그레이션 전략

각 모듈의 특성에 따라 다른 전략을 사용한다.

**전략 1: 바텀업 마이그레이션**

의존성이 없는 하위 모듈부터 시작한다.

```cpp
// 1단계: 유틸리티 함수부터 현대화
// old_utils.hpp
namespace utils {
    char* string_duplicate(const char* str);  // 위험
    void string_free(char* str);
}

// new_utils.hpp
namespace utils {
    std::string string_duplicate(std::string_view str) {
        return std::string(str);  // 안전하고 간단
    }
    // string_free는 불필요 (자동 관리)
}
```

```cpp
// 2단계: 하위 모듈 현대화
// old_parser.hpp
class OldParser {
    char* data;
    int size;
public:
    OldParser(const char* input);
    ~OldParser() { delete[] data; }
    int parse(int** out_values);
};

// new_parser.hpp
class NewParser {
    std::string data;
public:
    explicit NewParser(std::string_view input) : data(input) {}
    std::vector<int> parse();  // 훨씬 안전
};
```

```cpp
// 3단계: 상위 모듈도 점진적으로 변경
// old_application.cpp
void old_process() {
    OldParser parser("some data");
    int* values = nullptr;
    int count = parser.parse(&values);
    // values 사용
    delete[] values;  // 수동 관리
}

// transition_application.cpp (중간 단계)
void transition_process() {
    OldParser parser("some data");  // 아직 레거시
    int* values = nullptr;
    int count = parser.parse(&values);
    
    // 안전한 컨테이너로 즉시 이동
    std::vector<int> safe_values(values, values + count);
    delete[] values;
    
    // 이제 안전하게 사용
    for (int value : safe_values) {
        // 처리
    }
}

// new_application.cpp (최종)
void new_process() {
    NewParser parser("some data");
    auto values = parser.parse();  // 완전히 안전
    
    for (int value : values) {
        // 처리
    }
}
```

**전략 2: 탑다운 마이그레이션**

공개 API부터 현대화하고 내부 구현은 나중에 한다.

```cpp
// 1단계: 공개 API 현대화 (내부는 레거시 유지)
class ModernAPI {
    // 내부적으로는 아직 레거시 사용
    struct LegacyImpl;
    std::unique_ptr<LegacyImpl> impl;
    
public:
    ModernAPI();
    ~ModernAPI();
    
    // 현대적인 인터페이스 제공
    std::optional<int> compute(std::string_view input);
    std::vector<std::string> get_results() const;
};

// modern_api.cpp
struct ModernAPI::LegacyImpl {
    // 아직 레거시 코드
    char* data;
    int* results;
    int result_count;
    
    LegacyImpl() : data(nullptr), results(nullptr), result_count(0) {}
    
    ~LegacyImpl() {
        delete[] data;
        delete[] results;
    }
};

ModernAPI::ModernAPI() : impl(std::make_unique<LegacyImpl>()) {}
ModernAPI::~ModernAPI() = default;

std::optional<int> ModernAPI::compute(std::string_view input) {
    // 레거시 구현 호출하지만 안전한 인터페이스 제공
    impl->data = new char[input.size() + 1];
    std::copy(input.begin(), input.end(), impl->data);
    impl->data[input.size()] = '\0';
    
    // 레거시 처리
    int result = legacy_compute(impl->data);
    if (result < 0) return std::nullopt;
    
    return result;
}
```

```cpp
// 2단계: 내부 구현을 점진적으로 현대화
struct ModernAPI::LegacyImpl {
    // data는 현대화됨
    std::string data;
    
    // results는 아직 레거시
    int* results;
    int result_count;
    
    LegacyImpl() : results(nullptr), result_count(0) {}
    
    ~LegacyImpl() {
        delete[] results;
    }
};
```

```cpp
// 3단계: 완전히 현대화
struct ModernAPI::ModernImpl {  // 이름도 변경
    std::string data;
    std::vector<int> results;
    
    // 기본 생성자/소멸자로 충분
};
```

### B.2.3 테스트 주도 마이그레이션

각 변경마다 테스트를 유지하여 회귀를 방지한다.

**마이그레이션 전 테스트 작성**

```cpp
// legacy_buffer_test.cpp
#include <gtest/gtest.h>
#include "legacy_api.h"

// 기존 동작을 캡처하는 테스트
TEST(LegacyBufferTest, CreateAndFree) {
    LegacyBuffer* buffer = nullptr;
    ASSERT_EQ(0, legacy_create_buffer(100, &buffer));
    ASSERT_NE(nullptr, buffer);
    ASSERT_NE(nullptr, buffer->data);
    ASSERT_EQ(100, buffer->size);
    
    legacy_free_buffer(buffer);
}

TEST(LegacyBufferTest, ProcessBuffer) {
    LegacyBuffer* buffer = nullptr;
    legacy_create_buffer(100, &buffer);
    
    // 초기값 설정
    for (int i = 0; i < buffer->size; ++i) {
        buffer->data[i] = static_cast<char>(i);
    }
    
    legacy_process_buffer(buffer);
    
    // 결과 검증
    for (int i = 0; i < buffer->size; ++i) {
        EXPECT_EQ(static_cast<char>(i * 2), buffer->data[i]);
    }
    
    legacy_free_buffer(buffer);
}
```

**래퍼에 대한 테스트**

```cpp
// safe_buffer_test.cpp
#include <gtest/gtest.h>
#include "modern_wrapper.hpp"

// 래퍼가 레거시와 같은 동작을 하는지 확인
TEST(SafeBufferTest, CreateAndAutoDestroy) {
    {
        SafeBuffer buffer(100);
        EXPECT_EQ(100, buffer.size());
        EXPECT_NE(nullptr, buffer.data().data());
    }
    // 자동으로 정리됨 (메모리 누수 없음)
}

TEST(SafeBufferTest, ProcessBuffer) {
    SafeBuffer buffer(100);
    
    // 초기값 설정 (안전한 방식)
    auto data = buffer.data();
    for (size_t i = 0; i < data.size(); ++i) {
        data[i] = static_cast<char>(i);
    }
    
    buffer.process();
    
    // 결과 검증 (레거시와 동일)
    for (size_t i = 0; i < data.size(); ++i) {
        EXPECT_EQ(static_cast<char>(i * 2), data[i]);
    }
}

// 추가 안전성 테스트
TEST(SafeBufferTest, ThrowsOnInvalidSize) {
    EXPECT_THROW(SafeBuffer buffer(-1), std::runtime_error);
    EXPECT_THROW(SafeBuffer buffer(0), std::runtime_error);
}
```

**현대적 구현 테스트**

```cpp
// modern_buffer_test.cpp
#include <gtest/gtest.h>
#include "modern_wrapper.hpp"

TEST(ModernBufferTest, BasicUsage) {
    ModernBuffer buffer(100);
    EXPECT_EQ(100, buffer.size());
    
    auto data = buffer.data();
    std::fill(data.begin(), data.end(), 'A');
    
    // 모든 요소가 'A'인지 확인
    EXPECT_TRUE(std::all_of(data.begin(), data.end(),
        [](char c) { return c == 'A'; }));
}

TEST(ModernBufferTest, InteropWithLegacy) {
    ModernBuffer buffer(100);
    
    // 초기값
    auto data = buffer.data();
    for (size_t i = 0; i < data.size(); ++i) {
        data[i] = static_cast<char>(i);
    }
    
    // 레거시 API 호출
    buffer.process_with_legacy();
    
    // 레거시와 동일한 결과
    for (size_t i = 0; i < data.size(); ++i) {
        EXPECT_EQ(static_cast<char>(i * 2), data[i]);
    }
}
```

### B.2.4 성능 회귀 방지

마이그레이션 중 성능이 떨어지지 않도록 주의한다.

**벤치마크 기준선 설정**

```cpp
// benchmark_baseline.cpp
#include <benchmark/benchmark.h>
#include "legacy_api.h"

static void BM_LegacyBuffer(benchmark::State& state) {
    for (auto _ : state) {
        LegacyBuffer* buffer = nullptr;
        legacy_create_buffer(state.range(0), &buffer);
        legacy_process_buffer(buffer);
        legacy_free_buffer(buffer);
    }
}
BENCHMARK(BM_LegacyBuffer)->Range(8, 8<<10);
```

**래퍼 성능 측정**

```cpp
// benchmark_wrapper.cpp
#include <benchmark/benchmark.h>
#include "modern_wrapper.hpp"

static void BM_SafeBuffer(benchmark::State& state) {
    for (auto _ : state) {
        SafeBuffer buffer(state.range(0));
        buffer.process();
    }
}
BENCHMARK(BM_SafeBuffer)->Range(8, 8<<10);

static void BM_ModernBuffer(benchmark::State& state) {
    for (auto _ : state) {
        ModernBuffer buffer(state.range(0));
        buffer.process_with_legacy();
    }
}
BENCHMARK(BM_ModernBuffer)->Range(8, 8<<10);
```

**성능 비교 자동화**

```bash
#!/bin/bash
# compare_performance.sh

echo "=== 성능 벤치마크 비교 ==="

# 기준선 실행
./benchmark_baseline --benchmark_out=baseline.json --benchmark_out_format=json

# 새 구현 실행
./benchmark_wrapper --benchmark_out=wrapper.json --benchmark_out_format=json

# 결과 비교
python3 -m benchmark_tools compare baseline.json wrapper.json
```

**성능 저하가 발견되면**

```cpp
// 성능 저하 원인 분석
class SafeBufferOptimized {
    std::unique_ptr<LegacyBuffer, decltype(&legacy_free_buffer)> buffer;
    
    // 캐싱으로 성능 개선
    mutable std::span<char> cached_span;
    mutable bool span_valid = false;
    
public:
    std::span<char> data() {
        if (!span_valid) {
            cached_span = std::span<char>(buffer->data, buffer->size);
            span_valid = true;
        }
        return cached_span;
    }
};
```

## B.3 호환성 문제 해결

마이그레이션 중 자주 발생하는 호환성 문제와 해결 방법이다.

### B.3.1 ABI 호환성 유지

기존 바이너리와의 호환성을 유지해야 하는 경우가 있다.

**문제: 클래스 레이아웃 변경**

```cpp
// 기존 (ABI 노출)
class OldAPI {
public:
    void method();
    
private:
    int* data;      // 원시 포인터
    int size;
};
```

스마트 포인터로 바꾸면 ABI가 깨진다.

```cpp
// 잘못된 변경 (ABI 깨짐)
class OldAPI {
public:
    void method();
    
private:
    std::unique_ptr<int[]> data;  // 크기가 다름!
    size_t size;                   // 타입도 변경
};
```

**해결: Pimpl 패턴**

```cpp
// old_api.hpp (공개 헤더 - ABI 유지)
class OldAPI {
public:
    OldAPI();
    ~OldAPI();
    
    // 복사/이동 연산 선언
    OldAPI(const OldAPI&);
    OldAPI& operator=(const OldAPI&);
    OldAPI(OldAPI&&) noexcept;
    OldAPI& operator=(OldAPI&&) noexcept;
    
    void method();
    
private:
    class Impl;
    Impl* pimpl;  // 원시 포인터 유지 (ABI 호환)
};
```

```cpp
// old_api.cpp (구현 - 자유롭게 현대화)
class OldAPI::Impl {
public:
    std::vector<int> data;  // 현대적 구현
    
    void method() {
        // 안전한 코드
    }
};

OldAPI::OldAPI() : pimpl(new Impl()) {}

OldAPI::~OldAPI() {
    delete pimpl;
}

OldAPI::OldAPI(const OldAPI& other) 
    : pimpl(new Impl(*other.pimpl)) {}

OldAPI& OldAPI::operator=(const OldAPI& other) {
    if (this != &other) {
        *pimpl = *other.pimpl;
    }
    return *this;
}

OldAPI::OldAPI(OldAPI&& other) noexcept 
    : pimpl(other.pimpl) {
    other.pimpl = nullptr;
}

OldAPI& OldAPI::operator=(OldAPI&& other) noexcept {
    if (this != &other) {
        delete pimpl;
        pimpl = other.pimpl;
        other.pimpl = nullptr;
    }
    return *this;
}

void OldAPI::method() {
    pimpl->method();
}
```

**더 나은 해결: std::unique_ptr with custom deleter**

```cpp
// old_api.hpp
class OldAPI {
public:
    OldAPI();
    ~OldAPI();
    
    // Rule of five
    OldAPI(const OldAPI&);
    OldAPI& operator=(const OldAPI&);
    OldAPI(OldAPI&&) noexcept;
    OldAPI& operator=(OldAPI&&) noexcept;
    
    void method();
    
private:
    class Impl;
    struct ImplDeleter {
        void operator()(Impl* p) const;
    };
    std::unique_ptr<Impl, ImplDeleter> pimpl;
};
```

```cpp
// old_api.cpp
void OldAPI::ImplDeleter::operator()(Impl* p) const {
    delete p;
}

OldAPI::OldAPI() : pimpl(new Impl()) {}
OldAPI::~OldAPI() = default;
OldAPI::OldAPI(OldAPI&&) noexcept = default;
OldAPI& OldAPI::operator=(OldAPI&&) noexcept = default;

OldAPI::OldAPI(const OldAPI& other)
    : pimpl(new Impl(*other.pimpl)) {}

OldAPI& OldAPI::operator=(const OldAPI& other) {
    if (this != &other) {
        pimpl.reset(new Impl(*other.pimpl));
    }
    return *this;
}
```

### B.3.2 타사 라이브러리 통합

타사 C 라이브러리와 상호 운용해야 하는 경우가 많다.

**문제: C 라이브러리 콜백**

```c
// third_party.h (C 라이브러리)
typedef void (*callback_t)(void* user_data, int value);

void register_callback(callback_t callback, void* user_data);
```

C++ 객체 메서드를 콜백으로 사용하고 싶다.

**해결: 정적 래퍼 함수**

```cpp
// callback_wrapper.hpp
#include "third_party.h"
#include <functional>
#include <memory>

class CallbackManager {
    using CallbackFunc = std::function<void(int)>;
    
    // 콜백별 상태 저장
    struct CallbackState {
        CallbackFunc func;
    };
    
    static std::map<void*, std::unique_ptr<CallbackState>> callbacks;
    
    // C 콜백 래퍼
    static void static_callback(void* user_data, int value) {
        auto* state = static_cast<CallbackState*>(user_data);
        if (state && state->func) {
            state->func(value);
        }
    }
    
public:
    static void register_callback(CallbackFunc func) {
        auto state = std::make_unique<CallbackState>();
        state->func = std::move(func);
        
        void* key = state.get();
        ::register_callback(&static_callback, state.get());
        
        callbacks[key] = std::move(state);
    }
    
    static void unregister_callback(void* key) {
        callbacks.erase(key);
    }
};

std::map<void*, std::unique_ptr<CallbackManager::CallbackState>> 
    CallbackManager::callbacks;
```

**사용 예제**

```cpp
class DataProcessor {
    int counter = 0;
    
public:
    void start_processing() {
        // 멤버 함수를 콜백으로 등록
        CallbackManager::register_callback([this](int value) {
            this->on_data_received(value);
        });
    }
    
private:
    void on_data_received(int value) {
        counter += value;
        std::cout << "Received: " << value 
                  << ", Total: " << counter << "\n";
    }
};
```

### B.3.3 컴파일 타임 호환성

다른 C++ 표준 버전과 호환되어야 하는 경우다.

**문제: C++26 기능을 사용하고 싶지만 C++17도 지원해야 함**

```cpp
// 이상적인 C++26 코드
template<typename... Ts>
void process() {
    using First = Ts...[0];  // C++26 Pack Indexing
    // ...
}
```

**해결: 조건부 컴파일과 대체 구현**

```cpp
// compat.hpp
#pragma once

// Pack Indexing 지원 확인
#if __cpp_pack_indexing >= 202311L
    #define HAS_PACK_INDEXING 1
#else
    #define HAS_PACK_INDEXING 0
#endif

// 버전별 구현
#if HAS_PACK_INDEXING
    // C++26: 직접 사용
    template<typename... Ts>
    using pack_element_0 = Ts...[0];
    
    template<typename... Ts>
    using pack_element_1 = Ts...[1];
#else
    // C++17 대체 구현
    template<typename... Ts>
    using pack_element_0 = std::tuple_element_t<0, std::tuple<Ts...>>;
    
    template<typename... Ts>
    using pack_element_1 = std::tuple_element_t<1, std::tuple<Ts...>>;
#endif

// 사용자 코드는 동일
template<typename... Ts>
void process() {
    using First = pack_element_0<Ts...>;
    using Second = pack_element_1<Ts...>;
    // ...
}
```

**std::span 폴백**

```cpp
// span_compat.hpp
#pragma once

#if __cpp_lib_span >= 202002L
    #include <span>
    using std::span;
    using std::dynamic_extent;
#else
    // 간단한 span 구현 (또는 외부 라이브러리 사용)
    #include <gsl/span>  // Microsoft GSL
    using gsl::span;
    constexpr std::size_t dynamic_extent = gsl::dynamic_extent;
#endif
```

**std::optional 폴백**

```cpp
// optional_compat.hpp
#pragma once

#if __cpp_lib_optional >= 201606L
    #include <optional>
    using std::optional;
    using std::nullopt;
#else
    #include <experimental/optional>
    using std::experimental::optional;
    using std::experimental::nullopt;
#endif
```

### B.3.4 빌드 시스템 업그레이드

CMake 설정도 현대화해야 한다.

**기존 빌드 설정**

```cmake
# 오래된 CMakeLists.txt
cmake_minimum_required(VERSION 2.8)
project(MyProject)

include_directories(include)

add_executable(myapp
    src/main.cpp
    src/utils.cpp
)

target_link_libraries(myapp pthread)
```

**현대적 빌드 설정**

```cmake
# 현대적 CMakeLists.txt
cmake_minimum_required(VERSION 3.20)

project(MyProject
    VERSION 1.0.0
    LANGUAGES CXX
)

# C++ 표준 설정
set(CMAKE_CXX_STANDARD 26)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# 타겟 정의
add_executable(myapp
    src/main.cpp
    src/utils.cpp
)

# 타겟 속성 (include_directories 대신)
target_include_directories(myapp
    PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# 링크 라이브러리 (pthread 대신 Threads::Threads)
find_package(Threads REQUIRED)
target_link_libraries(myapp
    PRIVATE
        Threads::Threads
)

# 컴파일러 경고 활성화
if(MSVC)
    target_compile_options(myapp PRIVATE /W4 /WX)
else()
    target_compile_options(myapp PRIVATE
        -Wall -Wextra -Wpedantic -Werror
    )
endif()

# 새니타이저 옵션
option(ENABLE_ASAN "Enable AddressSanitizer" OFF)
if(ENABLE_ASAN)
    target_compile_options(myapp PRIVATE -fsanitize=address)
    target_link_options(myapp PRIVATE -fsanitize=address)
endif()
```

**Feature 테스트와 호환성**

```cmake
# feature_detection.cmake
include(CheckCXXSourceCompiles)

# Pack Indexing 지원 확인
check_cxx_source_compiles("
    template<typename... Ts>
    using First = Ts...[0];
    int main() { return 0; }
" HAS_PACK_INDEXING)

if(HAS_PACK_INDEXING)
    target_compile_definitions(myapp PRIVATE HAS_PACK_INDEXING=1)
endif()

# std::span 지원 확인
check_cxx_source_compiles("
    #include <span>
    int main() {
        int arr[] = {1, 2, 3};
        std::span<int> s(arr);
        return 0;
    }
" HAS_STD_SPAN)

if(NOT HAS_STD_SPAN)
    # GSL을 대체로 사용
    find_package(Microsoft.GSL REQUIRED)
    target_link_libraries(myapp PRIVATE Microsoft.GSL::GSL)
endif()
```

### B.3.5 문서화와 지식 전파

팀 전체가 마이그레이션 과정을 이해해야 한다.

**마이그레이션 가이드 문서**

```markdown
# 프로젝트 마이그레이션 가이드

## 현재 상태 (2025년 11월)
- C++17 기반 코드베이스
- 목표: C++26 + 안전한 프로그래밍 관행

## 진행 단계

### 1단계: 준비 (완료)
- [x] 정적 분석 도구 도입
- [x] 단위 테스트 커버리지 70% 달성
- [x] 벤치마크 기준선 수립

### 2단계: 기본 현대화 (진행 중)
- [x] NULL → nullptr
- [x] typedef → using
- [x] C-cast → C++ cast
- [ ] Raw new/delete → smart pointers (60% 완료)

### 3단계: 안전성 강화 (계획)
- [ ] C 배열 → std::vector/array
- [ ] 원시 포인터 → std::span/참조
- [ ] 에러 코드 → 예외/std::optional

### 4단계: 최적화 (미래)
- [ ] C++26 features 도입
- [ ] Zero-cost abstraction 검증

## 가이드라인

### 새 코드 작성 시
모든 새 코드는 다음 규칙을 따른다:
1. 스마트 포인터만 사용
2. std::vector/array 사용
3. 범위 기반 for 선호
4. constexpr 적극 활용

### 기존 코드 수정 시
레거시 코드를 수정할 때:
1. 주변 코드도 함께 현대화
2. 수정 전후 성능 측정
3. 단위 테스트 추가/업데이트

## 리소스
- 내부 Wiki: https://wiki.company.com/cpp-migration
- 코드 리뷰 체크리스트: docs/code_review.md
- 질문/토론: #cpp-migration Slack 채널
```

**코드 리뷰 체크리스트**

```markdown
# C++ 코드 리뷰 체크리스트

## 메모리 안전성
- [ ] raw new/delete 없음
- [ ] 스마트 포인터 올바르게 사용
- [ ] 댕글링 포인터 가능성 없음
- [ ] 메모리 누수 없음 (Valgrind/ASAN 확인)

## 타입 안전성
- [ ] C 스타일 캐스트 없음
- [ ] void* 사용 없음 (불가피한 경우 문서화)
- [ ] enum class 사용
- [ ] 암묵적 변환 최소화

## 범위 안전성
- [ ] C 배열 대신 std::vector/array
- [ ] 배열 인덱싱보다 범위 기반 for
- [ ] std::span 또는 참조로 매개변수 전달
- [ ] 경계 검사 (릴리스 빌드에서도)

## 에러 처리
- [ ] 에러 코드 대신 예외/std::optional
- [ ] 예외 안전성 보장 (최소 basic guarantee)
- [ ] RAII 원칙 준수

## 성능
- [ ] 불필요한 복사 없음
- [ ] Move semantics 활용
- [ ] 벤치마크 통과 (성능 회귀 없음)

## 테스트
- [ ] 단위 테스트 추가/업데이트
- [ ] 경계 조건 테스트
- [ ] 메모리 안전성 테스트 (ASAN)
- [ ] 스레드 안전성 테스트 (TSAN, 필요시)

## 문서화
- [ ] API 문서 업데이트
- [ ] 복잡한 로직에 주석
- [ ] Migration notes (레거시 코드 변경 시)
```

이러한 체크리스트와 가이드를 통해 팀 전체가 일관되게 마이그레이션을 진행할 수 있다.


---  
  
# 부록 C: 추가 리소스

안전한 C++ 프로그래밍을 위한 여정은 이 책으로 끝나지 않는다. 이 부록에서는 계속해서 학습하고 실습할 수 있는 다양한 리소스를 소개한다. 라이브러리, 도구, 온라인 플랫폼, 커뮤니티 등을 활용하여 안전한 C++ 개발 역량을 지속적으로 향상시킬 수 있다.

## C.1 관련 라이브러리와 도구들

실무에서 안전한 C++ 코드를 작성할 때 도움이 되는 라이브러리와 도구들을 소개한다.

### C.1.1 안전성 강화 라이브러리

**Microsoft GSL (Guidelines Support Library)**

C++ Core Guidelines를 구현한 라이브러리로, 표준 라이브러리에 아직 포함되지 않은 안전성 도구들을 제공한다.

- GitHub: https://github.com/microsoft/GSL
- 설치: 헤더 온리 라이브러리
- 주요 기능: span, not_null, owner, narrow_cast

```cpp
#include <gsl/gsl>

// not_null로 null 포인터 방지
void process(gsl::not_null<int*> ptr) {
    *ptr = 42;  // null 검사 불필요
}

// span으로 안전한 범위 전달
void process_array(gsl::span<int> data) {
    for (auto& value : data) {
        value *= 2;
    }
}

// narrow_cast로 안전한 변환
int main() {
    long big_value = 1000000;
    
    // 범위 검사와 함께 변환
    int small_value = gsl::narrow_cast<int>(big_value);
    
    // 데이터 손실 가능성이 있으면 예외 발생
    try {
        int value = gsl::narrow<int>(big_value);
    } catch (const gsl::narrowing_error&) {
        // 변환 실패 처리
    }
    
    return 0;
}
```

**Abseil (Google)**

Google이 개발한 C++ 라이브러리로, 안전하고 효율적인 유틸리티를 제공한다.

- 웹사이트: https://abseil.io
- GitHub: https://github.com/abseil/abseil-cpp
- 주요 기능: 문자열 처리, 컨테이너, 시간, 동기화

```cpp
#include <absl/strings/string_view.h>
#include <absl/container/flat_hash_map.h>
#include <absl/types/optional.h>

// 안전한 문자열 처리
absl::string_view extract_name(absl::string_view input) {
    auto pos = input.find(':');
    if (pos != absl::string_view::npos) {
        return input.substr(0, pos);
    }
    return input;
}

// 효율적인 해시맵
absl::flat_hash_map<std::string, int> create_scores() {
    return {
        {"Alice", 95},
        {"Bob", 87},
        {"Charlie", 92}
    };
}

// Optional 타입
absl::optional<int> find_score(const std::string& name) {
    auto scores = create_scores();
    auto it = scores.find(name);
    if (it != scores.end()) {
        return it->second;
    }
    return absl::nullopt;
}
```

**Boost 라이브러리**

오랜 역사를 가진 C++ 라이브러리 컬렉션으로, 많은 기능이 나중에 표준 라이브러리에 포함되었다.

- 웹사이트: https://www.boost.org
- 주요 모듈: Smart Pointers, Containers, Algorithms, Asio (네트워킹)

```cpp
#include <boost/container/static_vector.hpp>
#include <boost/outcome.hpp>

// 스택 기반 벡터 (std::inplace_vector 대체)
boost::container::static_vector<int, 10> fixed_buffer;

// Result 타입으로 에러 처리
namespace outcome = boost::outcome_v2;

outcome::result<int> parse_number(const std::string& str) {
    try {
        return std::stoi(str);
    } catch (const std::exception& e) {
        return outcome::failure(e.what());
    }
}

void use_result() {
    auto result = parse_number("123");
    if (result) {
        int value = result.value();
        // 사용
    } else {
        // 에러 처리
    }
}
```

**range-v3**

Eric Niebler가 개발한 범위 라이브러리로, C++20 Ranges의 기반이 되었다.

- GitHub: https://github.com/ericniebler/range-v3
- C++20 Ranges보다 더 많은 기능 제공

```cpp
#include <range/v3/all.hpp>

// 함수형 스타일의 데이터 처리
auto process_numbers(const std::vector<int>& numbers) {
    return numbers 
        | ranges::views::filter([](int n) { return n > 0; })
        | ranges::views::transform([](int n) { return n * 2; })
        | ranges::views::take(10)
        | ranges::to<std::vector>();
}

// 무한 시퀀스도 안전하게
auto fibonacci() {
    return ranges::views::generate([a=0, b=1]() mutable {
        int result = a;
        int next = a + b;
        a = b;
        b = next;
        return result;
    });
}

void use_fibonacci() {
    auto first_10 = fibonacci() 
        | ranges::views::take(10) 
        | ranges::to<std::vector>();
}
```

**fmt 라이브러리**

타입 안전한 문자열 포맷팅 라이브러리로, C++20 std::format의 기반이다.

- GitHub: https://github.com/fmtlib/fmt
- 웹사이트: https://fmt.dev

```cpp
#include <fmt/core.h>
#include <fmt/ranges.h>

// 타입 안전한 포맷팅
void print_info(const std::string& name, int age, double score) {
    // printf와 달리 컴파일 타임에 타입 검사
    fmt::print("Name: {}, Age: {}, Score: {:.2f}\n", name, age, score);
}

// 컨테이너 직접 출력
void print_vector(const std::vector<int>& vec) {
    fmt::print("Vector: {}\n", vec);
}

// 사용자 정의 타입 포맷팅
struct Point {
    int x, y;
};

template <>
struct fmt::formatter<Point> {
    constexpr auto parse(format_parse_context& ctx) {
        return ctx.begin();
    }
    
    template <typename FormatContext>
    auto format(const Point& p, FormatContext& ctx) {
        return fmt::format_to(ctx.out(), "({}, {})", p.x, p.y);
    }
};

void use_custom_format() {
    Point p{10, 20};
    fmt::print("Point: {}\n", p);  // "Point: (10, 20)"
}
```

### C.1.2 정적 분석 도구

**Clang-Tidy**

Clang 기반의 정적 분석 도구로, 코드 품질과 안전성을 개선한다.

- 문서: https://clang.llvm.org/extra/clang-tidy/
- 수백 가지 검사 규칙 제공

```bash
# 기본 사용법
clang-tidy myfile.cpp -- -std=c++26

# 특정 검사만 활성화
clang-tidy myfile.cpp -checks='modernize-*,cppcoreguidelines-*' -- -std=c++26

# 자동 수정
clang-tidy myfile.cpp -fix -checks='modernize-use-nullptr' -- -std=c++26

# 전체 프로젝트 분석
run-clang-tidy -p build/
```

**.clang-tidy 설정 파일 예제**

```yaml
# .clang-tidy
Checks: >
  -*,
  bugprone-*,
  cert-*,
  clang-analyzer-*,
  cppcoreguidelines-*,
  google-*,
  misc-*,
  modernize-*,
  performance-*,
  readability-*,
  -modernize-use-trailing-return-type,
  -readability-identifier-length

CheckOptions:
  - key: readability-identifier-naming.VariableCase
    value: lower_case
  - key: readability-identifier-naming.FunctionCase
    value: lower_case
  - key: readability-identifier-naming.ClassCase
    value: CamelCase
  - key: cppcoreguidelines-avoid-magic-numbers.IgnoredIntegerValues
    value: '0;1;2;-1'
```

**Clang Static Analyzer**

더 깊은 수준의 정적 분석을 수행한다.

```bash
# scan-build로 전체 빌드 분석
scan-build cmake --build build/

# HTML 리포트 생성
scan-build -o analysis-results cmake --build build/
```

**Cppcheck**

독립적인 C++ 정적 분석 도구다.

- 웹사이트: https://cppcheck.sourceforge.io
- GitHub: https://github.com/danmar/cppcheck

```bash
# 기본 분석
cppcheck src/

# 더 엄격한 검사
cppcheck --enable=all --inconclusive --std=c++26 src/

# 특정 에러만
cppcheck --enable=warning,performance,portability src/

# XML 리포트
cppcheck --xml --xml-version=2 src/ 2> report.xml
```

**PVS-Studio**

상업용 정적 분석 도구로, 무료 라이선스도 제공한다.

- 웹사이트: https://pvs-studio.com
- 매우 정교한 분석 제공

### C.1.3 동적 분석 도구

**AddressSanitizer (ASan)**

메모리 에러를 런타임에 탐지한다.

```bash
# 컴파일 시 활성화
g++ -fsanitize=address -g myfile.cpp -o myapp

# 또는 CMake
cmake -DCMAKE_CXX_FLAGS="-fsanitize=address -g" ..
```

```cpp
// 메모리 에러 예제 (ASan이 탐지함)
void memory_errors() {
    int* arr = new int[10];
    
    // 버퍼 오버런 (ASan 탐지)
    arr[10] = 42;
    
    delete[] arr;
    
    // Use-after-free (ASan 탐지)
    arr[0] = 10;
    
    // 메모리 누수 (ASan 탐지)
    int* leak = new int[100];
}
```

**ThreadSanitizer (TSan)**

데이터 레이스를 탐지한다.

```bash
# 컴파일 시 활성화
g++ -fsanitize=thread -g myfile.cpp -o myapp
```

```cpp
// 데이터 레이스 예제 (TSan이 탐지함)
int shared_counter = 0;

void increment_unsafe() {
    std::thread t1([&] {
        for (int i = 0; i < 1000; ++i) {
            ++shared_counter;  // 레이스 조건
        }
    });
    
    std::thread t2([&] {
        for (int i = 0; i < 1000; ++i) {
            ++shared_counter;  // 레이스 조건
        }
    });
    
    t1.join();
    t2.join();
}
```

**UndefinedBehaviorSanitizer (UBSan)**

정의되지 않은 동작을 탐지한다.

```bash
# 컴파일 시 활성화
g++ -fsanitize=undefined -g myfile.cpp -o myapp
```

```cpp
// UB 예제들 (UBSan이 탐지함)
void undefined_behaviors() {
    // 정수 오버플로우
    int max = std::numeric_limits<int>::max();
    int overflow = max + 1;  // UBSan 탐지
    
    // null 포인터 역참조
    int* null_ptr = nullptr;
    *null_ptr = 42;  // UBSan 탐지
    
    // 잘못된 캐스트
    int value = 42;
    auto* ptr = reinterpret_cast<double*>(&value);
    double bad = *ptr;  // UBSan 탐지
}
```

**MemorySanitizer (MSan)**

초기화되지 않은 메모리 사용을 탐지한다.

```bash
# Clang 전용
clang++ -fsanitize=memory -g myfile.cpp -o myapp
```

```cpp
// 초기화되지 않은 메모리 (MSan 탐지)
void uninitialized_memory() {
    int x;  // 초기화 안 됨
    if (x > 0) {  // MSan 탐지
        // ...
    }
}
```

**Valgrind**

메모리 디버깅과 프로파일링 도구 모음이다.

- 웹사이트: https://valgrind.org

```bash
# 메모리 에러 검사
valgrind --leak-check=full ./myapp

# 캐시 프로파일링
valgrind --tool=cachegrind ./myapp

# 힙 프로파일링
valgrind --tool=massif ./myapp
```

### C.1.4 빌드 시스템과 패키지 관리자

**CMake**

크로스 플랫폼 빌드 시스템이다.

- 웹사이트: https://cmake.org
- 문서: https://cmake.org/documentation/

```cmake
# 현대적 CMake 예제
cmake_minimum_required(VERSION 3.20)

project(SafeCppProject
    VERSION 1.0.0
    LANGUAGES CXX
)

# C++26 요구
set(CMAKE_CXX_STANDARD 26)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# 타겟 정의
add_executable(myapp
    src/main.cpp
    src/module.cpp
)

# 외부 라이브러리
find_package(fmt REQUIRED)
find_package(Microsoft.GSL REQUIRED)

target_link_libraries(myapp
    PRIVATE
        fmt::fmt
        Microsoft.GSL::GSL
)

# 컴파일 옵션
target_compile_options(myapp PRIVATE
    $<$<CXX_COMPILER_ID:MSVC>:/W4 /WX>
    $<$<NOT:$<CXX_COMPILER_ID:MSVC>>:-Wall -Wextra -Wpedantic -Werror>
)

# 새니타이저 옵션
option(ENABLE_SANITIZERS "Enable sanitizers" OFF)
if(ENABLE_SANITIZERS)
    target_compile_options(myapp PRIVATE
        -fsanitize=address,undefined
    )
    target_link_options(myapp PRIVATE
        -fsanitize=address,undefined
    )
endif()
```

**Conan**

C++ 패키지 관리자다.

- 웹사이트: https://conan.io
- 문서: https://docs.conan.io

```ini
# conanfile.txt
[requires]
fmt/10.1.1
gsl-lite/0.41.0
catch2/3.4.0

[generators]
CMakeDeps
CMakeToolchain

[options]
fmt:header_only=True
```

```bash
# 패키지 설치
conan install . --build=missing

# CMake와 통합
cmake -DCMAKE_TOOLCHAIN_FILE=conan_toolchain.cmake ..
cmake --build .
```

**vcpkg**

Microsoft의 C++ 패키지 관리자다.

- GitHub: https://github.com/microsoft/vcpkg

```bash
# vcpkg 설치
git clone https://github.com/microsoft/vcpkg
./vcpkg/bootstrap-vcpkg.sh

# 패키지 설치
./vcpkg/vcpkg install fmt gsl catch2

# CMake 통합
cmake -DCMAKE_TOOLCHAIN_FILE=./vcpkg/scripts/buildsystems/vcpkg.cmake ..
```

## C.2 온라인 컴파일러와 테스트 환경

개발 환경 없이도 C++ 코드를 작성하고 테스트할 수 있는 온라인 도구들이다.

### C.2.1 Compiler Explorer (Godbolt)

가장 인기 있는 온라인 C++ 컴파일러로, 여러 컴파일러와 버전을 비교할 수 있다.

- URL: https://godbolt.org
- 주요 기능: 어셈블리 출력, 다중 컴파일러 비교, 라이브러리 지원

**활용 방법**

```cpp
// Godbolt에서 테스트할 코드
#include <vector>
#include <algorithm>

// 최적화 비교를 위한 코드
int sum_vector(const std::vector<int>& vec) {
    return std::accumulate(vec.begin(), vec.end(), 0);
}

// Pack indexing 테스트 (C++26)
template<typename... Ts>
auto get_first() -> Ts...[0] {
    // GCC 14+ 또는 Clang 18+에서 테스트
}
```

**유용한 기능들**

1. 여러 컴파일러 동시 실행
   - GCC 14, Clang 18, MSVC 최신 버전 비교
   - 동일한 코드의 어셈블리 출력 비교

2. 컴파일러 옵션 설정
   - `-std=c++26`
   - `-O3 -march=native`
   - `-fsanitize=address`

3. 라이브러리 포함
   - Boost
   - Range-v3
   - fmt
   - Catch2

4. 코드 공유
   - 짧은 URL로 코드 공유
   - 영구 링크 생성

### C.2.2 Quick C++ Benchmarks

성능 비교를 위한 온라인 벤치마크 도구다.

- URL: https://quick-bench.com

```cpp
// Quick Bench 벤치마크 예제
static void vector_reserve(benchmark::State& state) {
    for (auto _ : state) {
        std::vector<int> vec;
        vec.reserve(1000);
        for (int i = 0; i < 1000; ++i) {
            vec.push_back(i);
        }
    }
}
BENCHMARK(vector_reserve);

static void vector_no_reserve(benchmark::State& state) {
    for (auto _ : state) {
        std::vector<int> vec;
        for (int i = 0; i < 1000; ++i) {
            vec.push_back(i);
        }
    }
}
BENCHMARK(vector_no_reserve);
```

### C.2.3 C++ Insights

C++ 코드가 컴파일러에 의해 어떻게 변환되는지 보여준다.

- URL: https://cppinsights.io
- Clang 기반

```cpp
// C++ Insights 예제
class Widget {
public:
    Widget() = default;
    Widget(int value) : data(value) {}
    
private:
    int data = 0;
};

// C++ Insights는 다음을 보여줌:
// - 생성된 기본 생성자
// - 암묵적 복사/이동 생성자
// - 소멸자
// - 멤버 초기화 과정
```

**활용 사례**

1. 템플릿 인스턴스화 확인
2. 람다 함수의 실제 클래스 구조
3. Range-based for의 내부 구현
4. 구조적 바인딩의 변환 과정

### C.2.4 Wandbox

일본에서 개발된 온라인 컴파일러로, 다양한 언어를 지원한다.

- URL: https://wandbox.org
- C++ 외에도 여러 언어 지원

```cpp
// Wandbox에서 실행할 코드
#include <iostream>
#include <vector>
#include <ranges>

int main() {
    std::vector<int> numbers = {1, 2, 3, 4, 5};
    
    auto even_doubled = numbers 
        | std::views::filter([](int n) { return n % 2 == 0; })
        | std::views::transform([](int n) { return n * 2; });
    
    for (int n : even_doubled) {
        std::cout << n << " ";
    }
}
```

### C.2.5 Coliru

간단한 온라인 C++ 컴파일러다.

- URL: http://coliru.stacked-crooked.com
- GCC와 Clang 지원

### C.2.6 Replit

클라우드 기반 IDE로, 프로젝트 전체를 관리할 수 있다.

- URL: https://replit.com
- 주요 기능: 파일 관리, 협업, 버전 관리

```cpp
// Replit에서 다중 파일 프로젝트
// main.cpp
#include "module.hpp"
#include <iostream>

int main() {
    Module m;
    std::cout << m.process(42) << "\n";
}

// module.hpp
#pragma once

class Module {
public:
    int process(int value);
};

// module.cpp
#include "module.hpp"

int Module::process(int value) {
    return value * 2;
}
```

### C.2.7 온라인 도구 활용 전략

**학습 단계**

1. 간단한 예제: Godbolt 또는 Wandbox
2. 성능 비교: Quick Bench
3. 내부 구조 이해: C++ Insights
4. 프로젝트 구조: Replit

**실무 활용**

1. 코드 공유와 토론
   - Godbolt 링크로 동료와 공유
   - 어셈블리 출력으로 최적화 논의

2. 면접 준비
   - 온라인 코딩 테스트 연습
   - 실시간 코드 작성 연습

3. 컴파일러 버전 테스트
   - 새 표준 기능 지원 확인
   - 다른 컴파일러 동작 비교

## C.3 학습 리소스

### C.3.1 공식 문서와 표준

**C++ 표준 문서**

- ISO C++ 위원회: https://isocpp.org
- 표준 초안: https://eel.is/c++draft/
- 제안서 목록: https://wg21.link/

**cppreference.com**

가장 포괄적인 C++ 레퍼런스다.

- URL: https://en.cppreference.com
- 모든 표준 라이브러리 문서화
- 코드 예제와 컴파일러 지원 정보

### C.3.2 온라인 강의와 튜토리얼

**LearnCpp.com**

초보자부터 고급까지 체계적인 C++ 튜토리얼이다.

- URL: https://www.learncpp.com

**CppCon YouTube 채널**

연례 C++ 컨퍼런스의 발표 영상을 제공한다.

- URL: https://www.youtube.com/user/CppCon

**C++ Weekly (Jason Turner)**

매주 C++ 팁과 기법을 소개하는 YouTube 시리즈다.

- URL: https://www.youtube.com/c/lefticus1

### C.3.3 블로그와 기술 문서

**Modernes C++ (Rainer Grimm)**

현대적 C++에 대한 깊이 있는 글을 제공한다.

- URL: https://www.modernescpp.com

**Fluent C++ (Jonathan Boccara)**

표현력 있는 C++ 코드 작성법을 다룬다.

- URL: https://www.fluentcpp.com

**Simplify C++ (Arne Mertz)**

복잡한 C++ 개념을 명확하게 설명한다.

- URL: https://arne-mertz.de

### C.3.4 커뮤니티와 포럼

**Stack Overflow**

C++ 태그는 가장 활발한 커뮤니티 중 하나다.

- URL: https://stackoverflow.com/questions/tagged/c++

**Reddit r/cpp**

C++ 뉴스, 토론, 질문을 다룬다.

- URL: https://www.reddit.com/r/cpp/

**C++ Slack**

실시간 채팅으로 토론한다.

- URL: https://cpplang.slack.com

**Discord 서버들**

- Together C & C++
- C++ Help

### C.3.5 도서 추천

**입문자용**

- "C++ Primer" (Stanley Lippman)
- "Programming: Principles and Practice Using C++" (Bjarne Stroustrup)

**중급자용**

- "Effective Modern C++" (Scott Meyers)
- "C++ Concurrency in Action" (Anthony Williams)

**고급자용**

- "C++ Templates: The Complete Guide" (David Vandevoorde)
- "C++ Move Semantics: The Complete Guide" (Nicolai Josuttis)

**안전성 관련**

- "C++ Core Guidelines" (온라인)
- "Beautiful C++: 30 Core Guidelines" (J. Guy Davidson, Kate Gregory)

## C.4 실전 프로젝트 아이디어

학습한 내용을 적용할 수 있는 프로젝트 아이디어들이다.

### C.4.1 초급 프로젝트

**안전한 문자열 파서**

```cpp
// 목표: 타입 안전한 CSV 파서 작성
class CSVParser {
    std::string_view data;
    
public:
    explicit CSVParser(std::string_view input) : data(input) {}
    
    std::vector<std::vector<std::string>> parse();
    std::optional<std::string> get_cell(size_t row, size_t col) const;
};
```

**메모리 안전한 버퍼**

```cpp
// 목표: 범위 검사가 있는 링 버퍼 구현
template<typename T, size_t N>
class RingBuffer {
    std::array<T, N> data;
    size_t read_pos = 0;
    size_t write_pos = 0;
    
public:
    std::optional<T> read();
    bool write(const T& value);
    size_t available() const;
};
```

### C.4.2 중급 프로젝트

**스레드 안전한 로깅 시스템**

```cpp
// 목표: 멀티스레드 환경에서 안전한 로거
class ThreadSafeLogger {
    mutable std::mutex mutex;
    std::ofstream file;
    
public:
    void log(std::string_view level, std::string_view message);
    void flush();
};
```

**타입 안전한 설정 관리자**

```cpp
// 목표: 컴파일 타임 타입 검사를 지원하는 설정 시스템
template<typename T>
class TypedConfig {
    std::unordered_map<std::string, T> values;
    
public:
    void set(std::string_view key, T value);
    std::optional<T> get(std::string_view key) const;
};
```

### C.4.3 고급 프로젝트

**메모리 풀 관리자**

```cpp
// 목표: 안전하고 효율적인 메모리 풀
template<typename T, size_t BlockSize = 1024>
class MemoryPool {
public:
    T* allocate();
    void deallocate(T* ptr);
    size_t available() const;
};
```

**비동기 I/O 라이브러리**

```cpp
// 목표: std::execution 기반 비동기 파일 I/O
namespace async_io {
    auto read_file(std::filesystem::path path) 
        -> std::execution::sender auto;
    
    auto write_file(std::filesystem::path path, std::span<const byte> data)
        -> std::execution::sender auto;
}
```

이러한 리소스와 프로젝트들을 통해 안전한 C++ 프로그래밍 기술을 지속적으로 발전시킬 수 있다.  