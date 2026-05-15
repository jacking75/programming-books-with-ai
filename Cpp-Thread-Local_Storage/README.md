# 실전 Thread-Local Storage: 락(Lock) 없는 고성능 멀티스레딩과 C++ 최적화 기법

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **컴파일러**: MSVC v143 (C++20 지원)
- **OS**: Windows 10 이상

-----  

## 목차

- 1. 기본 개념과 이론
    - 1.1 Thread-Local Storage란?
    - 1.2 왜 필요한가? (동기화 vs TLS)
    - 1.3 Win32 API의 TLS vs C++11 thread_local 비교

- 2. Win32 API TLS
    - 2.1 TLS 함수들 (TlsAlloc, TlsSetValue, TlsGetValue, TlsFree)
    - 2.2 기본 사용 예제
    - 2.3 한계점과 주의사항

- 3. C++ thread_local
    - 3.1 기본 문법과 사용법
    - 3.2 초기화와 생명주기
    - 3.3 클래스 멤버와의 활용

- 4. 고성능 프로그래밍 활용 사례
    - 4.1 락 없는(Lock-free) 메모리 할당자
        - 스레드별 메모리 풀 구현
        - 성능 비교 (mutex vs thread_local)
    - 4.2 캐싱 최적화
        - 스레드별 캐시 구현
        - False sharing 회피
    - 4.3 난수 생성기 최적화
        - 스레드별 PRNG 상태 유지
        - 경합 제거
    - 4.4 에러 핸들링
        - errno 스타일의 스레드별 에러 코드
        - 예외 성능 개선
    - 4.5 프로파일링과 통계 수집
        - 스레드별 성능 카운터
        - 락 없는 통계 집계
    - 4.6 컨텍스트 전파
        - 스레드별 요청 컨텍스트
        - 로깅 컨텍스트

- 5. 고급 패턴과 주의사항
    - 5.1 초기화 순서 문제
    - 5.2 동적 라이브러리에서의 TLS
    - 5.3 성능 고려사항
    - 5.4 메모리 누수 방지

- 6. 실전 종합 예제