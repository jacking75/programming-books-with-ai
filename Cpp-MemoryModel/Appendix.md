# C++23 메모리 모델(Memory Order) 완벽 가이드  

저자: 최흥배, Claude AI   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **컴파일러**: MSVC v143 (C++20 지원)
- **OS**: Windows 10 이상

-----
  
# 📚 부록

## A. 빠른 참조 가이드

### A.1 메모리 순서 치트시트

#### 메모리 순서 요약표

```
┌─────────────────────┬──────────────┬──────────────┬─────────────────────┐
│   메모리 순서       │   동기화     │   성능       │   주요 사용 사례    │
├─────────────────────┼──────────────┼──────────────┼─────────────────────┤
│ memory_order_       │   없음       │   최고       │ 단순 카운터,        │
│ relaxed             │              │              │ 통계 수집           │
├─────────────────────┼──────────────┼──────────────┼─────────────────────┤
│ memory_order_       │   부분적     │   높음       │ Producer-Consumer,  │
│ acquire/release     │              │              │ 플래그 동기화       │
├─────────────────────┼──────────────┼──────────────┼─────────────────────┤
│ memory_order_       │   완전       │   낮음       │ 강한 순서 보장이    │
│ seq_cst (기본값)    │              │              │ 필요한 경우         │
├─────────────────────┼──────────────┼──────────────┼─────────────────────┤
│ memory_order_       │   acquire +  │   중간       │ Read-Modify-Write   │
│ acq_rel             │   release    │              │ 연산                │
├─────────────────────┼──────────────┼──────────────┼─────────────────────┤
│ memory_order_       │   최소       │   최고       │ 로드 전용           │
│ consume             │              │              │ (사용 비권장)       │
└─────────────────────┴──────────────┴──────────────┴─────────────────────┘
```

#### 연산별 사용 가능한 메모리 순서

```cpp
// Load 연산 (load, wait 등)
atomic.load(memory_order_relaxed);
atomic.load(memory_order_acquire);
atomic.load(memory_order_seq_cst);
atomic.load(memory_order_consume);  // 비권장

// Store 연산 (store, notify_one/all 등)
atomic.store(value, memory_order_relaxed);
atomic.store(value, memory_order_release);
atomic.store(value, memory_order_seq_cst);

// Read-Modify-Write 연산 (exchange, compare_exchange, fetch_add 등)
atomic.exchange(value, memory_order_relaxed);
atomic.exchange(value, memory_order_acquire);
atomic.exchange(value, memory_order_release);
atomic.exchange(value, memory_order_acq_rel);
atomic.exchange(value, memory_order_seq_cst);
```

#### 빠른 결정 트리

```
메모리 순서 선택하기
│
├─ 다른 스레드와 데이터 동기화가 필요한가?
│  │
│  NO → memory_order_relaxed
│  │     (단순 카운터, 통계)
│  │
│  YES → 가장 강한 순서 보장이 필요한가?
│        │
│        YES → memory_order_seq_cst
│        │     (복잡한 동기화, 확신이 없을 때)
│        │
│        NO → Producer-Consumer 패턴인가?
│              │
│              YES → memory_order_acquire (load)
│              │     memory_order_release (store)
│              │
│              NO → Read-Modify-Write 연산인가?
│                    │
│                    YES → memory_order_acq_rel
│                    │
│                    NO → memory_order_seq_cst (안전한 선택)
```

### A.2 일반적인 패턴 요약

#### 패턴 1: 단순 카운터

```cpp
// ✅ 올바른 사용
std::atomic<int> counter{0};

void increment() {
    counter.fetch_add(1, std::memory_order_relaxed);
}

int get_value() {
    return counter.load(std::memory_order_relaxed);
}
```

#### 패턴 2: 플래그 기반 동기화

```cpp
// ✅ 올바른 사용
std::atomic<bool> ready{false};
int data = 0;

// Producer
void produce() {
    data = 42;  // 1. 데이터 준비
    ready.store(true, std::memory_order_release);  // 2. 준비 완료 신호
}

// Consumer
void consume() {
    while (!ready.load(std::memory_order_acquire))  // 1. 준비 대기
        ;
    assert(data == 42);  // 2. 데이터 사용 (항상 42)
}
```

#### 패턴 3: Double-Checked Locking

```cpp
// ✅ 올바른 사용
class Singleton {
    static std::atomic<Singleton*> instance;
    static std::mutex mtx;

public:
    static Singleton* get_instance() {
        Singleton* tmp = instance.load(std::memory_order_acquire);
        if (tmp == nullptr) {
            std::lock_guard<std::mutex> lock(mtx);
            tmp = instance.load(std::memory_order_relaxed);
            if (tmp == nullptr) {
                tmp = new Singleton();
                instance.store(tmp, std::memory_order_release);
            }
        }
        return tmp;
    }
};
```

#### 패턴 4: 스핀락

```cpp
// ✅ 올바른 사용
class SpinLock {
    std::atomic<bool> locked{false};

public:
    void lock() {
        while (locked.exchange(true, std::memory_order_acquire))
            ;
    }

    void unlock() {
        locked.store(false, std::memory_order_release);
    }
};
```

#### 패턴 5: Lock-Free 스택 (간단한 버전)

```cpp
// ✅ 올바른 사용
template<typename T>
class LockFreeStack {
    struct Node {
        T data;
        Node* next;
    };
    std::atomic<Node*> head{nullptr};

public:
    void push(const T& value) {
        Node* new_node = new Node{value, nullptr};
        new_node->next = head.load(std::memory_order_relaxed);
        
        while (!head.compare_exchange_weak(
            new_node->next, 
            new_node,
            std::memory_order_release,
            std::memory_order_relaxed))
            ;
    }

    bool pop(T& result) {
        Node* old_head = head.load(std::memory_order_acquire);
        
        while (old_head && !head.compare_exchange_weak(
            old_head,
            old_head->next,
            std::memory_order_release,
            std::memory_order_acquire))
            ;
        
        if (old_head) {
            result = old_head->data;
            delete old_head;
            return true;
        }
        return false;
    }
};
```

### A.3 성능 비교 표

#### 상대적 성능 오버헤드 (x86-64 기준)

```
┌──────────────────────┬──────────────┬──────────────┬──────────────┐
│   연산               │   Relaxed    │  Acq/Rel     │  Seq_Cst     │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Load                 │   1x         │   1x         │   1x         │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Store                │   1x         │   1x         │   1-2x       │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Read-Modify-Write    │   1x         │   1x         │   1-2x       │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Fence                │   N/A        │   1-2x       │   2-3x       │
└──────────────────────┴──────────────┴──────────────┴──────────────┘

* 상대적 수치이며 실제 성능은 프로세서와 워크로드에 따라 다름
* x86-64는 강한 메모리 모델로 차이가 적지만, ARM에서는 차이가 더 큼
```

#### ARM 아키텍처 성능 비교

```
┌──────────────────────┬──────────────┬──────────────┬──────────────┐
│   연산               │   Relaxed    │  Acq/Rel     │  Seq_Cst     │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Load                 │   1x         │   1.5-2x     │   2-3x       │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Store                │   1x         │   1.5-2x     │   3-4x       │
├──────────────────────┼──────────────┼──────────────┼──────────────┤
│ Read-Modify-Write    │   1x         │   2-3x       │   4-5x       │
└──────────────────────┴──────────────┴──────────────┴──────────────┘

* ARM은 약한 메모리 모델로 메모리 순서 차이가 성능에 큰 영향
```

### A.4 흔한 실수와 해결책

#### ❌ 실수 1: 잘못된 메모리 순서 조합

```cpp
// ❌ 잘못된 코드
std::atomic<bool> ready{false};
int data = 0;

// Producer
void produce() {
    data = 42;
    ready.store(true, std::memory_order_relaxed);  // ❌ release여야 함
}

// Consumer
void consume() {
    if (ready.load(std::memory_order_relaxed))  // ❌ acquire여야 함
        process(data);  // 데이터 레이스!
}
```

```cpp
// ✅ 올바른 코드
void produce() {
    data = 42;
    ready.store(true, std::memory_order_release);  // ✅
}

void consume() {
    if (ready.load(std::memory_order_acquire))  // ✅
        process(data);  // 안전함
}
```

#### ❌ 실수 2: 스핀락에서 잘못된 메모리 순서

```cpp
// ❌ 잘못된 코드
class BadSpinLock {
    std::atomic<bool> locked{false};

public:
    void lock() {
        while (locked.exchange(true, std::memory_order_relaxed))  // ❌
            ;
    }
    
    void unlock() {
        locked.store(false, std::memory_order_relaxed);  // ❌
    }
};
```

```cpp
// ✅ 올바른 코드
class GoodSpinLock {
    std::atomic<bool> locked{false};

public:
    void lock() {
        while (locked.exchange(true, std::memory_order_acquire))  // ✅
            ;
    }
    
    void unlock() {
        locked.store(false, std::memory_order_release);  // ✅
    }
};
```

#### ❌ 실수 3: compare_exchange에서 잘못된 메모리 순서

```cpp
// ❌ 잘못된 코드 (성능 저하)
bool old_value = true;
atomic.compare_exchange_weak(
    old_value, 
    false,
    std::memory_order_seq_cst,  // ❌ 너무 강함
    std::memory_order_seq_cst   // ❌ 실패 시에도 seq_cst 불필요
);
```

```cpp
// ✅ 올바른 코드
bool old_value = true;
atomic.compare_exchange_weak(
    old_value, 
    false,
    std::memory_order_release,  // ✅ 성공 시
    std::memory_order_relaxed   // ✅ 실패 시
);
```

### A.5 디버깅 체크리스트

```
□ 모든 shared 변수가 atomic이거나 뮤텍스로 보호되는가?
□ Acquire-Release 쌍이 올바르게 매칭되는가?
□ Relaxed를 사용할 때 정말 동기화가 필요 없는가?
□ ThreadSanitizer로 테스트했는가?
□ 여러 플랫폼(x86, ARM)에서 테스트했는가?
□ 스트레스 테스트를 충분히 수행했는가?
□ False sharing이 발생하지 않는가?
□ ABA 문제를 고려했는가? (Lock-free 구조)
```

### A.6 Visual Studio 2022 단축키 모음

```
┌─────────────────────────────┬──────────────────────────────┐
│   기능                      │   단축키                     │
├─────────────────────────────┼──────────────────────────────┤
│ 중단점 설정/해제            │   F9                         │
├─────────────────────────────┼──────────────────────────────┤
│ 디버깅 시작                 │   F5                         │
├─────────────────────────────┼──────────────────────────────┤
│ 한 단계씩 실행 (Step Over)  │   F10                        │
├─────────────────────────────┼──────────────────────────────┤
│ 한 단계씩 실행 (Step Into)  │   F11                        │
├─────────────────────────────┼──────────────────────────────┤
│ Threads 창 열기             │   Ctrl+Alt+H                 │
├─────────────────────────────┼──────────────────────────────┤
│ Parallel Stacks 창 열기     │   Ctrl+Shift+D, S            │
├─────────────────────────────┼──────────────────────────────┤
│ Parallel Watch 창 열기      │   Ctrl+Shift+D, 1-4          │
├─────────────────────────────┼──────────────────────────────┤
│ Concurrency Visualizer      │   Analyze → Concurrency      │
│                             │   Visualizer                 │
└─────────────────────────────┴──────────────────────────────┘
```

---

## B. 추가 학습 자료

### B.1 온라인 리소스

#### 공식 문서

1. **C++ Reference - Memory Order**
   - URL: https://en.cppreference.com/w/cpp/atomic/memory_order
   - 설명: C++ atomic 연산과 메모리 순서에 대한 가장 정확하고 완전한 레퍼런스다
   - 특징: 각 메모리 순서별 상세 설명, 예제 코드, 노트 포함

2. **ISO C++ Standards**
   - URL: https://isocpp.org/std/the-standard
   - 설명: C++ 표준 문서의 공식 정보와 링크를 제공한다
   - 특징: 최신 표준 드래프트도 확인 가능

#### 학습 사이트

3. **Preshing on Programming**
   - URL: https://preshing.com/
   - 추천 글:
     - "An Introduction to Lock-Free Programming"
     - "Acquire and Release Semantics"
     - "Memory Ordering at Compile Time"
   - 설명: Jeff Preshing의 블로그로 메모리 모델에 대한 명확한 설명과 시각화를 제공한다

4. **Bartosz Milewski's Programming Cafe**
   - URL: https://bartoszmilewski.com/
   - 추천 글: C++ 동시성과 함수형 프로그래밍 관련 시리즈
   - 설명: 이론적 배경과 실용적 구현을 균형있게 다룬다

5. **Modernes C++**
   - URL: https://www.modernescpp.com/
   - 추천 시리즈: "C++ Memory Model" 시리즈
   - 설명: Rainer Grimm의 현대 C++ 기법 블로그다

#### 비디오 강의

6. **CppCon YouTube Channel**
   - URL: https://www.youtube.com/user/CppCon
   - 추천 영상:
     - "The C++ Memory Model" - Herb Sutter
     - "Atomic Weapons" - Herb Sutter
     - "Lock-Free Programming" - Fedor Pikus
   - 설명: 세계 최고 C++ 전문가들의 발표 영상이다

7. **C++ Weekly with Jason Turner**
   - URL: https://www.youtube.com/c/JasonTurner-lefticus
   - 설명: 짧고 명확한 C++ 팁과 기법을 다룬다
   - 특징: 실용적이고 현대적인 C++ 사용법 중심

#### 대화형 학습 도구

8. **Compiler Explorer (Godbolt)**
   - URL: https://godbolt.org/
   - 설명: 다양한 컴파일러의 어셈블리 출력을 실시간으로 확인할 수 있다
   - 활용: 메모리 순서가 실제 어셈블리 코드에 미치는 영향을 학습할 수 있다

9. **C++ Insights**
   - URL: https://cppinsights.io/
   - 설명: C++ 코드를 컴파일러가 보는 방식으로 변환해 보여준다
   - 활용: 템플릿 인스턴스화와 암묵적 변환을 이해하는 데 유용하다

#### 실습 사이트

10. **LeetCode - Concurrency**
    - URL: https://leetcode.com/problemset/concurrency/
    - 설명: 동시성 관련 문제를 풀며 연습할 수 있다
    - 특징: 실제 면접 문제 기반

### B.2 필독 도서

#### 기본서

1. **"C++ Concurrency in Action" (2nd Edition)**
   - 저자: Anthony Williams
   - ISBN: 978-1617294693
   - 내용: C++ 동시성 프로그래밍의 바이블이다. 메모리 모델부터 고급 패턴까지 상세히 다룬다
   - 난이도: 중급~고급
   - 추천 대상: 동시성 프로그래밍을 체계적으로 학습하려는 개발자

2. **"The Art of Multiprocessor Programming" (2nd Edition)**
   - 저자: Maurice Herlihy, Nir Shavit, Victor Luchangco, Michael Spear
   - ISBN: 978-0124159501
   - 내용: 멀티프로세서 프로그래밍의 이론과 실제를 다룬다
   - 난이도: 고급
   - 특징: 알고리즘과 이론적 배경이 강하다

#### 심화서

3. **"Is Parallel Programming Hard, And, If So, What Can You Do About It?"**
   - 저자: Paul E. McKenney
   - 무료 PDF: https://arxiv.org/abs/1701.00854
   - 내용: 리눅스 커널 개발자의 실전 경험을 바탕으로 한 병렬 프로그래밍 가이드다
   - 난이도: 중급~고급
   - 특징: 실무 중심, 무료로 제공

4. **"Systems Performance" (2nd Edition)**
   - 저자: Brendan Gregg
   - ISBN: 978-0136820154
   - 내용: 시스템 성능 분석과 최적화를 다룬다
   - 난이도: 고급
   - 특징: 실전 성능 분석 기법 포함

#### 참고서

5. **"Effective Modern C++" (한국어판: "Effective Modern C++")**
   - 저자: Scott Meyers
   - ISBN: 978-1491903995
   - 내용: 현대 C++의 베스트 프랙티스 (Item 40: atomic 관련)
   - 난이도: 중급
   - 추천 대상: 모든 C++ 개발자

### B.3 C++ 표준 문서 참조

#### Working Draft 읽는 법

```
C++ 표준 문서 구조 (N4950 - C++23 Working Draft 기준)

[atomics]                     // 섹션 32: Atomic operations library
├─ [atomics.order]            // 32.4: Order and consistency
│  ├─ memory_order 열거형 정의
│  ├─ 각 메모리 순서의 의미
│  └─ happens-before 관계 정의
│
├─ [atomics.types.generic]    // 32.5: Atomic types
│  ├─ atomic<T> 템플릿
│  └─ 특수화 목록
│
├─ [atomics.types.operations] // 32.5.6: Operations on atomic types
│  ├─ load, store
│  ├─ exchange
│  ├─ compare_exchange_weak/strong
│  └─ fetch_add, fetch_sub 등
│
└─ [atomics.fences]           // 32.4: Fences
   ├─ atomic_thread_fence
   └─ atomic_signal_fence
```

#### 주요 섹션 빠른 참조

1. **[intro.multithread] - Multi-threaded executions and data races**
   - 데이터 레이스의 정의
   - 스레드 간 동기화 기본 개념
   - 중요도: ★★★★★

2. **[atomics.order] - Order and consistency**
   - memory_order 열거형
   - 각 메모리 순서의 정확한 의미
   - happens-before, synchronizes-with 관계
   - 중요도: ★★★★★

3. **[atomics.lockfree] - Lock-free property**
   - is_lock_free(), is_always_lock_free
   - 플랫폼별 보장 사항
   - 중요도: ★★★★

4. **[atomics.types.operations] - Operations on atomic types**
   - 모든 atomic 연산의 정확한 동작
   - 메모리 순서 매개변수 사용법
   - 중요도: ★★★★★

#### Working Draft 다운로드

```
최신 C++ 표준 드래프트:
https://github.com/cplusplus/draft

C++23 Final Draft:
https://www.open-std.org/jtc1/sc22/wg21/docs/papers/2023/n4950.pdf

C++20 표준:
https://www.iso.org/standard/79358.html
```

### B.4 실전 코드 예제 저장소

#### GitHub 저장소

1. **Anthony Williams - Concurrency Examples**
   - URL: https://github.com/anthonywilliams/ccia_code_samples
   - 설명: "C++ Concurrency in Action" 책의 모든 예제 코드다
   - 특징: 챕터별로 정리되어 있고 빌드 스크립트 포함

2. **Folly - Facebook Open-source Library**
   - URL: https://github.com/facebook/folly
   - 설명: 페이스북의 고성능 C++ 라이브러리다
   - 주목: `folly/synchronization/` 디렉토리의 lock-free 구조들
   - 특징: 실전 검증된 프로덕션 코드

3. **moodycamel::ConcurrentQueue**
   - URL: https://github.com/cameron314/concurrentqueue
   - 설명: 고성능 lock-free 큐 구현이다
   - 특징: 상세한 문서와 벤치마크 포함

4. **Abseil - Google's C++ Library**
   - URL: https://github.com/abseil/abseil-cpp
   - 설명: 구글의 C++ 라이브러리다
   - 주목: `absl/synchronization/` 디렉토리
   - 특징: 현대적인 C++ 스타일

### B.5 도구와 라이브러리

#### 디버깅 도구

1. **ThreadSanitizer (TSan)**
   - 포함: GCC, Clang, Visual Studio (일부)
   - 기능: 데이터 레이스 탐지
   - 사용법:
     ```bash
     # GCC/Clang
     g++ -fsanitize=thread -g program.cpp -o program
     ```

2. **Valgrind - Helgrind**
   - URL: https://valgrind.org/
   - 기능: 멀티스레드 버그 탐지
   - 플랫폼: Linux, macOS
   - 사용법:
     ```bash
     valgrind --tool=helgrind ./program
     ```

3. **Intel Inspector**
   - URL: https://www.intel.com/content/www/us/en/developer/tools/oneapi/inspector.html
   - 기능: 메모리 오류, 스레드 오류 탐지
   - 플랫폼: Windows, Linux
   - 특징: GUI 기반, 상세한 리포트

#### 성능 분석 도구

4. **perf (Linux)**
   - 기본 포함: Linux 커널
   - 기능: CPU 성능 카운터, 프로파일링
   - 사용법:
     ```bash
     perf record -g ./program
     perf report
     ```

5. **VTune Profiler**
   - URL: https://www.intel.com/content/www/us/en/developer/tools/oneapi/vtune-profiler.html
   - 기능: 마이크로아키텍처 분석, 핫스팟 탐지
   - 특징: 하드웨어 이벤트 상세 분석

6. **Tracy Profiler**
   - URL: https://github.com/wolfpld/tracy
   - 기능: 실시간 프레임 프로파일러
   - 특징: 오픈소스, 멀티스레드 시각화 우수

#### 테스트 프레임워크

7. **Google Test (gtest)**
   - URL: https://github.com/google/googletest
   - 기능: 단위 테스트 프레임워크
   - 특징: Death test, 스레드 안전성 테스트 지원

8. **Catch2**
   - URL: https://github.com/catchorg/Catch2
   - 기능: 헤더 온리 테스트 프레임워크
   - 특징: 간단한 사용법, BDD 스타일 지원

### B.6 커뮤니티와 포럼

1. **C++ Slack**
   - URL: https://cpplang.slack.com/
   - 설명: C++ 개발자들의 실시간 커뮤니티다
   - 채널 추천: #concurrency, #performance

2. **Reddit - r/cpp**
   - URL: https://www.reddit.com/r/cpp/
   - 설명: C++ 뉴스, 질문, 토론
   - 특징: 활발한 커뮤니티, 전문가 답변

3. **Stack Overflow**
   - 태그: [c++], [concurrency], [atomic], [memory-model]
   - URL: https://stackoverflow.com/questions/tagged/c%2b%2b+concurrency
   - 특징: 검증된 답변, 풍부한 예제

4. **C++ Korea (한국 커뮤니티)**
   - Facebook 그룹: C++ Korea
   - 설명: 한국 C++ 개발자 커뮤니티다
   - 특징: 한국어로 질문과 토론 가능

### B.7 학습 로드맵 제안

#### 초급 → 중급 (2-3개월)

```
Week 1-2: 기초 이론
├─ C++ Reference - memory_order 읽기
├─ "C++ Concurrency in Action" Ch. 5 읽기
└─ 간단한 atomic 연산 실습

Week 3-4: Acquire-Release
├─ Producer-Consumer 패턴 구현
├─ Preshing 블로그 "Acquire and Release" 읽기
└─ 스핀락 구현 연습

Week 5-6: Sequential Consistency
├─ 여러 메모리 순서 비교 실험
├─ 성능 차이 측정
└─ 언제 사용할지 판단 연습

Week 7-8: 실전 프로젝트
├─ Thread-safe 자료구조 구현
├─ ThreadSanitizer로 검증
└─ 벤치마크 작성
```

#### 중급 → 고급 (3-6개월)

```
Month 1: Lock-Free 자료구조
├─ Lock-Free 스택/큐 구현
├─ ABA 문제 이해와 해결
└─ moodycamel::ConcurrentQueue 분석

Month 2: 메모리 모델 심화
├─ Happens-before 관계 완벽 이해
├─ C++ 표준 [atomics] 섹션 읽기
└─ 플랫폼별 차이 학습 (x86 vs ARM)

Month 3: 성능 최적화
├─ False sharing 실험
├─ 캐시 라인 정렬 기법
└─ VTune으로 성능 분석

Month 4-6: 대규모 프로젝트
├─ 복잡한 동시성 시스템 설계
├─ 실전 프로덕션 코드 분석 (Folly, Abseil)
└─ 오픈소스 기여
```

### B.8 자주 참조할 치트시트 링크

1. **C++ Memory Model Cheat Sheet**
   - https://people.cs.pitt.edu/~xianeizhang/notes/cpp11_mem.html
   - 핵심 개념을 한 페이지에 정리한 자료다

2. **Atomic Weapons Talk Slides**
   - https://herbsutter.com/2013/02/11/atomic-weapons-the-c-memory-model-and-modern-hardware/
   - Herb Sutter의 유명한 발표 슬라이드다

3. **cppreference Quick Reference**
   - https://en.cppreference.com/w/cpp/atomic
   - 모든 atomic 함수의 빠른 참조다

---

이상으로 부록을 마친다. 이 자료들을 활용하여 C++ 메모리 모델을 깊이 있게 학습하고, 실전 프로젝트에 적용할 수 있을 것이다. 궁금한 점이 있다면 커뮤니티에 질문하고, 표준 문서를 참조하며, 지속적으로 실습하는 것이 중요하다.  