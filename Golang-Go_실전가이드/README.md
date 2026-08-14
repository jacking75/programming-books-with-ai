# Golang 실전 가이드    

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **컴파일러**: Go 1.26  
  
대상 독자:  
- Go의 기본 문법 (변수, 함수, 포인터, 구조체, 인터페이스, 오류 처리)을 이해하는 사람  
- goroutine이나 channel을 사용한 병행 처리를 본격적으로 배우고 싶은 분  
- Go로 웹 API 서버를 구축하고 싶은 분  
- CLI 툴, gRPC, 데이터베이스 연계 등 실무 수준의 Go를 익히고 싶은 분  
    
-----  
    
## 목차  
- Chapter 01: goroutine과 channel — 경량 thread의 기동, channel에 의한 형태 안전한 통신, select문에 의한 멀티플렉싱
- Chapter 02: 동기 프리미티브 — Mutex, RWMutex, WaitGroup, Once, sync.Map 등 공유 리소스를 안전하게 처리하는 방법
- Chapter 03: 병렬 패턴—Worker Pool, Fan-out/Fan-in, Pipeline, 세마포어 등 실용적인 설계 패턴
- Chapter 04: Context — 취소 전파, 타임아웃 제어, 요청 범위 값 관리
- Chapter 05: net/http — 표준 라이브러리만으로 만드는 웹 서버, Handler 설계, 미들웨어
- Chapter 06: Gin / Echo — 인기 프레임워크의 기본적인 사용법과 구분
- Chapter 07: 데이터베이스 — database/sql, sqlx, GORM을 통한 CRUD, 연결 풀 관리, 트랜잭션, 마이그레이션
- Chapter 08: gRPC — Protocol Buffers 서비스 정의, 코드 생성, 양방향 스트리밍
- Chapter 09: 테스트 —httptest, 테이블 구동 테스트, 모의, 테스트 도우미 실습 기법
- Chapter 10: CLI 개발 — 표준 flag 패키지에서 cobra+viper에 의한 본격 CLI 툴 개발, 크로스 컴파일
- Chapter 11: 제네릭 스— 타입 파라미터, 제약(any, comparable, cmp.Ordered), 실천 패턴과 사용 장소
- Chapter 12: 프로파일링 —벤치마크 테스트, pprof로 핫스팟 식별, trace로 실행 시각화  