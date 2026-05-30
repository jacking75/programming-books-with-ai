# 따라하면서 배우는 DuckDB

**저자:** 최흥배, AI-Assisted

---

## 개발 환경

| 항목 | 버전/사양 |
|------|-----------|
| OS | Windows 11 |
| 언어 | C# (.NET 9) |
| 런타임 | .NET 9 |
| IDE/에디터 | Visual Studio Code + C# DevKit |
| DuckDB | 1.5.1 |
| NuGet 패키지 | DuckDB.NET.Bindings, DuckDB.NET.Data |

---

## 목차

### 제1장: DuckDB란 무엇인가?
- 1.1 DuckDB의 탄생 배경과 철학
- 1.2 OLTP vs OLAP — 왜 두 가지 DB가 필요한가?
- 1.3 DuckDB의 핵심 특징 (임베디드, 컬럼 지향, 인메모리)
- 1.4 DuckDB vs SQLite vs PostgreSQL — 언제 무엇을 쓰나?
- 1.5 DuckDB가 빛나는 사용 사례

### 제2장: 개발 환경 구축
- 2.1 Windows 11에 DuckDB CLI 설치
- 2.2 .NET 9 프로젝트 생성 및 DuckDB.NET NuGet 패키지 설치
- 2.3 DuckDB CLI 첫 실행 — Hello, DuckDB!
- 2.4 VS Code + C# DevKit 환경 설정
- 2.5 DuckDB CLI 기본 명령어 치트시트

### 제3장: DuckDB 핵심 아키텍처 이해
- 3.1 컬럼 지향 스토리지(Columnar Storage)란?
- 3.2 벡터화 실행 엔진(Vectorized Execution Engine)
- 3.3 WAL과 ACID 트랜잭션 처리
- 3.4 파일 포맷 — `.duckdb`, Parquet, Arrow
- 3.5 DuckDB 쿼리 실행 파이프라인
- 3.6 메모리 관리 및 스필(Spill-to-Disk)

### 제4장: SQL 기초 — DuckDB로 처음 배우는 쿼리
- 4.1 테이블 생성, 삽입, 조회 (CREATE / INSERT / SELECT)
- 4.2 WHERE, ORDER BY, LIMIT
- 4.3 집계 함수 (COUNT, SUM, AVG, MIN, MAX)
- 4.4 GROUP BY와 HAVING
- 4.5 JOIN — INNER / LEFT / RIGHT / FULL
- 4.6 서브쿼리와 CTE (WITH 절)
- 4.7 DuckDB 특유의 편리한 SQL 확장 문법

### 제5장: C#에서 DuckDB 다루기
- 5.1 DuckDB.NET 라이브러리 구조 이해
- 5.2 연결(Connection) 열기/닫기 — 파일 DB vs 인메모리 DB
- 5.3 DDL 실행 — 테이블 생성/삭제
- 5.4 DML 실행 — INSERT / UPDATE / DELETE
- 5.5 쿼리 결과를 C# 객체로 매핑
- 5.6 파라미터 바인딩과 SQL 인젝션 방지
- 5.7 트랜잭션 처리 패턴
- 5.8 비동기(async/await) 패턴

### 제6장: 파일 데이터 읽기/쓰기
- 6.1 CSV 파일 직접 읽기 (`read_csv_auto`)
- 6.2 JSON 파일 읽기 (`read_json_auto`)
- 6.3 Parquet 파일 읽기/쓰기
- 6.4 COPY 명령으로 대량 내보내기
- 6.5 C#에서 파일 임포트/익스포트 자동화
- 6.6 복수 파일 글로브 패턴으로 한번에 처리 (`*.csv`)

### 제7장: 온라인 게임 로그 설계와 수집
- 7.1 게임 로그의 종류 — 접속, 전투, 아이템, 결제, 오류
- 7.2 로그 파일 포맷 비교 — CSV vs JSON vs Parquet
- 7.3 추천 포맷 결정 — 게임 로그에는 무엇이 최선인가?
- 7.4 C#으로 구조화된 JSON 로그 라이터 만들기
- 7.5 C#으로 CSV 로그 라이터 만들기 (고성능 버전)
- 7.6 날짜별 로그 파일 로테이션 구현
- 7.7 DuckDB에 로그 파일 일괄 적재 파이프라인

### 제8장: 온라인 게임 콘텐츠별 DuckDB 활용
- 8.1 플레이어 행동 분석 — DAU/MAU, 세션 시간, 이탈 지점
- 8.2 전투 밸런스 분석 — 직업별 딜량, 사망 패턴, 스킬 사용 빈도
- 8.3 아이템 경제 분석 — 획득/소비/거래 흐름, 인플레이션 감지
- 8.4 퀘스트/콘텐츠 완료율 분석 — 병목 지점 찾기
- 8.5 결제 및 수익 분석 — ARPU, ARPPU, 결제 전환율
- 8.6 이상 탐지(Anti-cheat 지원) — 비정상 패턴 SQL로 찾기

### 제9장: 통계 분석 실전
- 9.1 DuckDB 내장 통계 함수 총람
- 9.2 윈도우 함수(Window Function)로 순위·누적·이동 평균 계산
- 9.3 시계열 분석 — 날짜/시간 함수 활용
- 9.4 코호트 분석 — 특정 날짜 가입자 그룹 추적
- 9.5 퍼널(Funnel) 분석 — 단계별 전환율
- 9.6 실전 대시보드용 리포트 SQL 작성

### 제10장: 성능 최적화
- 10.1 EXPLAIN과 EXPLAIN ANALYZE로 쿼리 계획 읽기
- 10.2 인덱스 대신 파티셔닝과 통계 활용
- 10.3 대용량 CSV/JSON 로드 최적화
- 10.4 메모리 설정 튜닝 (`SET memory_limit`)
- 10.5 병렬 쿼리 실행 스레드 설정
- 10.6 C# 애플리케이션 레벨 최적화

### 제11장: 실전 프로젝트 — 게임 로그 분석 시스템
- 11.1 프로젝트 전체 구조 설계
- 11.2 로그 생성 시뮬레이터 구현 (C#)
- 11.3 DuckDB 적재 파이프라인 구현 (C#)
- 11.4 분석 쿼리 모음 구현 (C#)
- 11.5 콘솔 리포트 출력 완성
- 11.6 개선 아이디어 및 다음 단계

### 부록
- A. DuckDB SQL 함수 레퍼런스 (게임 분석용 핵심 함수)
- B. DuckDB.NET API 레퍼런스
- C. 로그 스키마 설계 체크리스트
- D. 참고 자료 및 공식 문서 링크
