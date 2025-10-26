# 게임 서버 개발자를 위한 최신 Win32 API 프로그래밍  

저자: 최흥배, Claude AI   
    
권장 개발 환경
- **IDE**: Visual Studio 2022 (Community 이상)
- **컴파일러**: MSVC v143 (C++20 지원)
- **OS**: Windows 10 이상

-----  

## 목차

### 제1부: 기초 준비
**Chapter 1. Win32 API 기초**
- Win32 API 개요 및 역사
- 데이터 타입과 호출 규약
- 에러 처리 패턴 (GetLastError, HRESULT)
- Unicode vs ANSI 문자열 처리

### 제2부: 핵심 시스템 프로그래밍
**Chapter 2. 메모리 관리**
- VirtualAlloc/VirtualFree를 이용한 대용량 메모리 관리
- 힙 관리 (HeapCreate, HeapAlloc)
- 메모리 매핑 파일 (CreateFileMapping, MapViewOfFile)
- 게임 서버를 위한 메모리 풀 구현

**Chapter 3. 파일 시스템**
- 파일 I/O 기본 (CreateFile, ReadFile, WriteFile)
- 비동기 파일 I/O (Overlapped I/O)
- 디렉토리 모니터링 (ReadDirectoryChangesW)
- 로그 파일 관리 및 로테이션

**Chapter 4. 프로세스와 스레드**
- 스레드 생성 및 관리 (CreateThread, WaitForSingleObject)
- 스레드 풀 활용 (ThreadPool API)
- 프로세스 간 통신 기초
- 스레드 동기화 객체들

### 제3부: 동기화와 성능
**Chapter 5. 동기화 객체 심화**
- Critical Section vs Mutex 성능 비교
- Semaphore와 Event 활용
- Reader-Writer Lock (SRWLock)
- Condition Variable 패턴

**Chapter 6. 인터락 연산과 무잠금 프로그래밍**
- InterlockedXXX 함수군 완전 정복
- 무잠금 큐와 스택 구현
- 메모리 배리어와 캐시 일관성
- ABA 문제 해결 방법

### 제4부: 시스템 모니터링과 최적화
**Chapter 7. 성능 카운터와 프로파일링**
- Performance Counter API 활용
- CPU 사용률 및 메모리 사용량 모니터링
- ETW (Event Tracing for Windows) 기초
- Visual Studio 프로파일러 연동

**Chapter 8. 시스템 정보 수집**
- GetSystemInfo를 통한 하드웨어 정보
- WMI를 이용한 시스템 모니터링
- 네트워크 인터페이스 정보 수집
- 실시간 리소스 모니터링 도구 제작

### 제5부: 고급 주제
**Chapter 9. 보안과 권한 관리**
- Access Token과 사용자 권한
- 프로세스 권한 상승 (UAC)
- 서비스 계정 관리
- 게임 서버 보안 고려사항

**Chapter 10. 서비스 프로그래밍**
- Windows 서비스 기초
- 서비스 제어 관리자 (SCM) 연동
- 서비스 디버깅 기법
- 게임 서버를 서비스로 배포하기

**Chapter 11. COM과 WinRT**
- COM 기초 개념
- WinRT API 활용
- Windows Runtime 인터페이스
- 모던 Windows API와의 연동


### 부록
**부록 A. 자주 사용하는 Win32 API 레퍼런스**
**부록 B. 에러 코드 및 디버깅 가이드**
**부록 C. 성능 최적화 체크리스트**
**부록 D. Visual Studio 2022 팁과 트릭**

