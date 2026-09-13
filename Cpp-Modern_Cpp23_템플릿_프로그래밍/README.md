# Modern C++23 템플릿 프로그래밍  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio 2026 (Community 이상)
- **컴파일러**: C++ 23
- **OS**: Windows 10 이상

-----  
  
# 목차
  
## **PART 1. 템플릿의 첫걸음 — 기초 문법과 개념**

**Chapter 1. 왜 템플릿인가?**

1.1 코드 중복의 문제 — 타입별로 함수를 복붙하던 시절의 고통

1.2 매크로로 해결하려 했던 C의 방식과 그 한계

1.3 템플릿이란 무엇인가 — 컴파일러가 코드를 찍어내는 틀(mold)

1.4 템플릿의 두 얼굴: 제네릭 프로그래밍 vs. 메타프로그래밍


**Chapter 2. 함수 템플릿 (Function Template)**

2.1 기본 문법: `template<typename T>`

2.2 타입 추론(Template Argument Deduction) — 컴파일러가 T를 읽는 법

2.3 명시적 타입 지정: `add<int>(a, b)`

2.4 다중 타입 파라미터: `template<typename T, typename U>`

2.5 반환 타입 추론과 `auto`의 활용

2.6 인라인과 헤더 파일: 왜 `.h`에 구현을 넣는가

> 🛠 실습: `max`, `swap`, `print` 함수 템플릿 만들기


**Chapter 3. 클래스 템플릿 (Class Template)**

3.1 기본 문법과 멤버 함수 정의

3.2 클래스 템플릿의 타입 추론 (C++17 CTAD)

3.3 기본 타입 파라미터(Default Template Arguments)

3.4 멤버 함수 템플릿 (Member Function Template)

3.5 static 멤버와 템플릿

> 🛠 실습: 제네릭 `Pair<T, U>` 클래스 만들기

---

**Chapter 4. 비타입 템플릿 파라미터 (Non-type Template Parameter)**

4.1 정수, 포인터, 열거형을 파라미터로

4.2 컴파일 타임 상수로 배열 크기 지정하기

4.3 C++20부터 가능한 부동소수점, 클래스 타입 NTTP

4.4 `auto` 비타입 파라미터로 타입 유연성 높이기

> 🛠 실습: 고정 크기 `Stack<T, size_t N>` 만들기

---

**Chapter 5. 템플릿 특수화 (Template Specialization)**

5.1 전체 특수화 (Full Specialization)

5.2 부분 특수화 (Partial Specialization) — 클래스 템플릿만의 특권

5.3 함수 템플릿의 오버로딩 vs. 특수화 — 어느 것을 써야 하는가

5.4 포인터 타입, 배열 타입에 대한 부분 특수화

> 🛠 실습: `bool` 타입에 특화된 비트 압축 `Vector<bool>` 흉내내기

---

## **PART 2. 현대적 템플릿 — C++20/23 핵심 기능**

---

**Chapter 6. Concepts — 템플릿에 계약을 추가하다**

6.1 기존 SFINAE 방식의 복잡함과 고통

6.2 `concept` 정의 문법

6.3 `requires` 절 — 제약 조건 표현하기

6.4 `requires` 표현식 — 세부 요구사항 명시하기

6.5 표준 라이브러리 Concepts: `std::integral`, `std::floating_point`, `std::ranges::range` 등

6.6 Concept의 서브섬션(Subsumption) — 우선순위 결정 규칙
  
> 🛠 실습: `Printable`, `Arithmetic`, `Container` 컨셉 직접 만들기

---

**Chapter 7. 가변 인자 템플릿 (Variadic Templates)**

7.1 파라미터 팩(Parameter Pack) 기초

7.2 팩 확장(Pack Expansion) 패턴

7.3 `sizeof...` 연산자

7.4 폴드 표현식(Fold Expression) — C++17의 선물

7.5 재귀 vs. 폴드 표현식 비교
  
> 🛠 실습: `print_all`, `type_list`, 타입 안전 `tuple_apply` 만들기

---

**Chapter 8. 템플릿과 `auto` — 타입 추론의 심화**

8.1 `auto`, `decltype`, `decltype(auto)` 차이 완벽 정리

8.2 보편 참조(Universal Reference)와 `T&&`

8.3 완벽 전달(Perfect Forwarding): `std::forward<T>`

8.4 축약 함수 템플릿(Abbreviated Function Template): `auto` 파라미터 (C++20)

8.5 `auto` 파라미터와 Concept 결합

> 🛠 실습: 완벽 전달 래퍼 함수, `make_*` 스타일 팩토리 함수 만들기

---

**Chapter 9. `if constexpr` 와 컴파일 타임 분기**

9.1 기존 태그 디스패치(Tag Dispatch)의 번거로움

9.2 `if constexpr` 문법과 동작 원리

9.3 `if consteval` — C++23의 새 기능: 컴파일/런타임 분기

9.4 `constexpr` 함수, `consteval` 함수, `constinit` 변수

9.5 컴파일 타임에 분기하는 템플릿 코드 작성 패턴

> 🛠 실습: 타입에 따라 다르게 동작하는 `serialize<T>` 함수 만들기

---

**Chapter 10. Deducing `this` — C++23의 게임 체인저**

10.1 기존 `const` 오버로딩의 코드 중복 문제

10.2 명시적 객체 파라미터(Explicit Object Parameter) 문법

10.3 CRTP 패턴을 `deducing this`로 단순화하기

10.4 재귀 람다 구현하기

10.5 빌더(Builder) 패턴을 우아하게 구현하기
  
> 🛠 실습: CRTP 없는 정적 다형성 구현, 재귀 람다 계승(factorial)

---

## **PART 3. 타입 메타프로그래밍 — 컴파일 타임을 다루다**

---

**Chapter 11. 타입 트레이트 (Type Traits)**

11.1 `<type_traits>` 헤더의 주요 도구들 한눈에 보기

11.2 타입 검사: `is_integral_v`, `is_same_v`, `is_base_of_v`

11.3 타입 변환: `remove_cv_t`, `decay_t`, `common_type_t`

11.4 C++23의 새 트레이트들

11.5 커스텀 타입 트레이트 만들기

> 🛠 실습: 직렬화 가능 타입인지 검사하는 커스텀 트레이트 만들기

---

**Chapter 12. SFINAE와 오버로딩 해결 (그리고 작별 인사)**

12.1 SFINAE란 — "치환 실패는 오류가 아니다"

12.2 `std::enable_if`의 동작 방식

12.3 Concepts 이전에 왜 이런 고통이 필요했는가

12.4 레거시 코드 읽기: SFINAE → Concept으로 리팩터링

> 📌 이 챕터는 "읽기 능력" 목적 — 신규 코드엔 Concept을 쓰자

---

**Chapter 13. 컴파일 타임 계산 (Compile-time Computation)**

13.1 `constexpr`로 컴파일 타임에 값 계산하기

13.2 `std::integral_constant`와 타입-값 묶음

13.3 컴파일 타임 피보나치, 팩토리얼, 소수 판별

13.4 `constexpr` 컨테이너: `std::array`와 `std::string` (C++20)

13.5 컴파일 타임 정렬 알고리즘 구현

> 🛠 실습: 컴파일 타임 `lookup table` 생성기 만들기

---

**Chapter 14. 템플릿 템플릿 파라미터**

14.1 컨테이너 타입 자체를 파라미터로 받기

14.2 문법과 추론 규칙

14.3 C++17의 클래스 템플릿 인수 추론(CTAD)과의 조합

14.4 실용적인 사용처: 컨테이너 어댑터 패턴

> 🛠 실습: `Stack<T, Container = std::vector>` 만들기

---

## **PART 4. 실전 패턴과 응용 — 현업에서 쓰는 기법**

---

**Chapter 15. 정책 기반 설계 (Policy-Based Design)**

15.1 정책(Policy)이란 무엇인가 — 행동을 타입으로 표현하기

15.2 Andrei Alexandrescu의 아이디어와 현대적 재해석

15.3 정렬 정책, 로깅 정책, 메모리 정책 예제

15.4 Concept으로 정책 인터페이스 제약하기

> 🛠 실습: 로깅 전략을 교체할 수 있는 `Logger<Policy>` 만들기

---

**Chapter 16. 타입 리스트와 `std::tuple` 활용**

16.1 `std::tuple` 내부 구조 엿보기

16.2 `std::get<N>`, `std::get<T>`, `std::apply`

16.3 `std::index_sequence`를 이용한 튜플 순회

16.4 컴파일 타임 타입 리스트 조작

16.5 C++23 `std::tuple` 개선 사항

> 🛠 실습: 튜플의 모든 원소를 출력하는 `print_tuple` 만들기

---

**Chapter 17. 표현식 템플릿 (Expression Templates) 맛보기**

17.1 `+` 연산자가 임시 객체를 만드는 문제

17.2 표현식 자체를 타입으로 표현하는 아이디어

17.3 간단한 벡터 연산에서의 지연 평가(Lazy Evaluation)

17.4 현대 C++에서의 대안: Ranges와 Views

> 🛠 실습: 간단한 지연 평가 덧셈 표현식 템플릿 구현

---

**Chapter 18. C++23 표준 라이브러리 템플릿 활용**

18.1 `std::expected<T, E>` — 오류 처리의 새로운 방식

18.2 `std::mdspan` — 다차원 배열을 제네릭하게 다루기

18.3 `std::generator<T>` — 코루틴과 템플릿의 만남

18.4 Ranges 라이브러리와 커스텀 View 만들기

18.5 `std::format`과 커스텀 포매터 템플릿 만들기

> 🛠 실습: `std::expected`를 활용한 타입 안전 파서 만들기

---

**Chapter 19. 람다와 템플릿의 결합**

19.1 제네릭 람다(Generic Lambda): `[](auto x)`

19.2 명시적 템플릿 파라미터 람다 (C++20): `[]<typename T>(T x)`

19.3 람다로 만드는 오버로드 세트 (Overload Set)

19.4 즉시 호출 람다(IIFE)와 `consteval` 람다

19.5 `deducing this` + 람다 = 재귀 람다

> 🛠 실습: `std::variant` 방문자를 람다 오버로드로 구현하기

---

## **PART 5. 마스터 프로젝트 — 실전 미니 라이브러리 구현**

---

**Chapter 20. 미니 프로젝트 ①: 타입 안전 유닛 시스템**

---

**Chapter 21. 미니 프로젝트 ②: 간단한 함수형 파이프라인**

---

**Chapter 22. 미니 프로젝트 ③: 컴파일 타임 반사(Reflection) 흉내내기**

---

## **부록 (Appendix)**

- **부록 A.** 템플릿 오류 메시지 읽는 법 — Visual Studio 2026 기준
- **부록 B.** 컴파일러 탐색기(Compiler Explorer / godbolt.org) 활용법
- **부록 C.** 주요 Concept 레퍼런스 카드 (치트시트)
- **부록 D.** C++11 → C++14 → C++17 → C++20 → C++23 템플릿 변천사
- **부록 E.** 더 읽을거리 & 참고 자료

---

## **목차 구성 요약**

```
PART 1 (Ch.1~5)   : 기초 문법            ─── 초급
PART 2 (Ch.6~10)  : C++20/23 핵심 기능   ─── 초~중급
PART 3 (Ch.11~14) : 타입 메타프로그래밍  ─── 중급
PART 4 (Ch.15~19) : 실전 패턴            ─── 중~고급
PART 5 (Ch.20~22) : 미니 프로젝트        ─── 종합 실습
```

---

> 📌 **집필 원칙 요약**
> - 모든 예제 코드는 **30줄 이내**를 원칙으로 합니다
> - 각 챕터는 **왜 필요한가 → 문법 → 실습** 의 3단 구조를 따릅니다
> - Visual Studio 2026 + `/std:c++23` 기준으로 **바로 실행 가능한** 코드만 수록합니다
> - SFINAE처럼 구식 기법은 **"읽기 능력"** 용도로만 최소 수록합니다  