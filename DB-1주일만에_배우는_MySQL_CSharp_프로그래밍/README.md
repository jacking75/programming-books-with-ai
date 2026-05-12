# 1주일만에 배우는 MySQL C# 프로그래밍  

저자: 최흥배, AI-Assisted   
    
권장 개발 환경
- **.NET**: .NET 9 이상
- **OS**: Windows 10 이상
- **MySQL**: 8.0

-----    
  
## 목차

### **Day 1: 데이터베이스의 이해와 MySQL 시작**
- 1.1 데이터베이스란 무엇인가?
  - DBMS의 필요성과 역할
  - 관계형 데이터베이스(RDBMS)의 핵심 개념
  - ACID 속성의 이해
- 1.2 MySQL 아키텍처 핵심
  - MySQL 8.0의 주요 구조
  - 스토리지 엔진(InnoDB)의 이해
  - 쿼리 실행 흐름도
- 1.3 MySQL 설치와 환경 설정
  - Windows/Linux 설치
  - MySQL Workbench 기본 사용법
  - 첫 데이터베이스 생성

### **Day 2: SQL 기본 문법과 테이블 설계**
- 2.1 데이터베이스와 테이블 생성
  - CREATE DATABASE/TABLE 문법
  - 데이터 타입의 선택과 활용
  - 제약조건(Primary Key, Foreign Key, Unique, Not Null)
- 2.2 기본 CRUD 명령어
  - INSERT: 데이터 삽입
  - SELECT: 데이터 조회와 WHERE 조건
  - UPDATE: 데이터 수정
  - DELETE: 데이터 삭제
- 2.3 온라인 게임 유저 테이블 설계 실습
  - 유저 정보 테이블 설계
  - 캐릭터 정보 테이블 설계
  - 테이블 간 관계 설정

### **Day 3: C#과 MySQL 연동**
- 3.1 개발 환경 구성
  - .NET 프로젝트 생성
  - MySqlConnector 라이브러리 설치 및 설정
  - 연결 문자열(Connection String) 구성
- 3.2 MySqlConnector 기본 사용법
  - 데이터베이스 연결과 종료
  - 쿼리 실행(ExecuteNonQuery, ExecuteScalar, ExecuteReader)
  - 파라미터를 이용한 안전한 쿼리 실행
  - 트랜잭션 처리
- 3.3 게임 회원가입/로그인 구현
  - 회원가입 기능 개발
  - 로그인 인증 로직
  - 비밀번호 해싱 처리

### **Day 4: SQLKata를 이용한 쿼리 빌더**
- 4.1 SQLKata 라이브러리 소개
  - Query Builder 패턴의 장점
  - SQLKata 설치 및 초기 설정
  - MySqlConnector와 SQLKata 통합
- 4.2 SQLKata 기본 쿼리 작성
  - Select, Where, Join 쿼리 빌딩
  - Insert, Update, Delete 쿼리 빌딩
  - 복잡한 조건문 작성
- 4.3 게임 인벤토리 시스템 구현
  - 아이템 테이블 설계
  - 인벤토리 조회/추가/삭제 로직
  - 아이템 거래 시스템 구현

### **Day 5: 고급 쿼리와 인덱스**
- 5.1 조인(JOIN)의 이해와 활용
  - INNER JOIN, LEFT JOIN, RIGHT JOIN
  - 다중 테이블 조인
  - 조인 성능 최적화
- 5.2 인덱스(Index) 설계
  - 인덱스의 원리와 B-Tree 구조
  - 인덱스 생성 전략
  - 복합 인덱스와 커버링 인덱스
  - EXPLAIN을 통한 쿼리 분석
- 5.3 게임 랭킹 시스템 구현
  - 리더보드 테이블 설계
  - 효율적인 랭킹 조회 쿼리
  - 실시간 랭킹 업데이트

### **Day 6: 게임 콘텐츠 시스템 구현**
- 6.1 친구 시스템 구현
  - 친구 관계 테이블 설계
  - 친구 요청/수락/거절 로직
  - 양방향 관계 처리
- 6.2 우편함(메일) 시스템 구현
  - 우편함 테이블 설계
  - 아이템 첨부 메일 발송
  - 읽음/안읽음 처리 및 보상 수령
- 6.3 던전 보상 시스템 구현
  - 던전 클리어 기록 저장
  - 보상 테이블 설계
  - 확률형 보상 처리 로직

### **Day 7: 실전 성능 최적화와 운영**
- 7.1 트랜잭션과 동시성 제어
  - 트랜잭션 격리 수준(Isolation Level)
  - 락(Lock)의 이해와 데드락 방지
  - 낙관적 락 vs 비관적 락
- 7.2 성능 최적화 전략
  - 쿼리 최적화 체크리스트
  - 커넥션 풀링(Connection Pooling)
  - 배치 처리와 벌크 작업
  - 캐싱 전략
- 7.3 게임 출석 체크 시스템 구현
  - 출석 테이블 설계
  - 연속 출석 계산 로직
  - 월별 출석 현황 조회
- 7.4 실전 팁과 마무리
  - 백업과 복구 기본
  - 보안 고려사항
  - 개발 vs 운영 환경 분리
  - 모니터링과 로깅

---

### **부록**
- A. MySQL 주요 명령어 치트시트
- B. MySqlConnector 주요 메서드 레퍼런스
- C. SQLKata 주요 메서드 레퍼런스
- D. 게임별 데이터베이스 설계 예시
- E. 자주 발생하는 오류와 해결 방법


---   


# 부록
이 부록에서는 MySQL 프로그래밍을 하면서 자주 참조하게 될 핵심 명령어와 메서드, 그리고 다양한 게임 장르별 데이터베이스 설계 예시를 제공한다. 실무에서 빠르게 찾아볼 수 있도록 치트시트 형태로 정리했다.

## A. MySQL 주요 명령어 치트시트

### A.1 데이터베이스 관리 명령어

```sql
-- 데이터베이스 조회
SHOW DATABASES;

-- 데이터베이스 생성
CREATE DATABASE game_db 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

-- 데이터베이스 선택
USE game_db;

-- 데이터베이스 삭제
DROP DATABASE game_db;

-- 현재 사용 중인 데이터베이스 확인
SELECT DATABASE();

-- 데이터베이스 문자셋 변경
ALTER DATABASE game_db 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;
```

### A.2 테이블 관리 명령어

```sql
-- 테이블 목록 조회
SHOW TABLES;

-- 테이블 구조 확인
DESCRIBE users;
DESC users;
SHOW COLUMNS FROM users;

-- 테이블 생성 쿼리 확인
SHOW CREATE TABLE users;

-- 테이블 생성
CREATE TABLE users (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    level INT NOT NULL DEFAULT 1,
    exp BIGINT NOT NULL DEFAULT 0,
    gold INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    last_login DATETIME,
    INDEX idx_username (username),
    INDEX idx_level_exp (level DESC, exp DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 테이블 삭제
DROP TABLE users;

-- 테이블이 존재할 경우에만 삭제
DROP TABLE IF EXISTS users;

-- 테이블 비우기 (데이터만 삭제, 구조 유지)
TRUNCATE TABLE users;

-- 테이블 이름 변경
RENAME TABLE old_users TO users;
ALTER TABLE old_users RENAME TO users;

-- 컬럼 추가
ALTER TABLE users ADD COLUMN nickname VARCHAR(50);

-- 컬럼 삭제
ALTER TABLE users DROP COLUMN nickname;

-- 컬럼 수정
ALTER TABLE users MODIFY COLUMN level INT NOT NULL DEFAULT 1;

-- 컬럼 이름 변경
ALTER TABLE users CHANGE COLUMN old_name new_name VARCHAR(50);

-- 컬럼 순서 변경
ALTER TABLE users MODIFY COLUMN email VARCHAR(100) AFTER username;
```

### A.3 인덱스 관리 명령어

```sql
-- 인덱스 조회
SHOW INDEX FROM users;
SHOW KEYS FROM users;

-- 인덱스 생성
CREATE INDEX idx_username ON users(username);

-- 복합 인덱스 생성
CREATE INDEX idx_level_exp ON users(level DESC, exp DESC);

-- 유니크 인덱스 생성
CREATE UNIQUE INDEX uk_email ON users(email);

-- 인덱스 삭제
DROP INDEX idx_username ON users;
ALTER TABLE users DROP INDEX idx_username;

-- 인덱스 추가 (ALTER TABLE 사용)
ALTER TABLE users ADD INDEX idx_level (level);
ALTER TABLE users ADD UNIQUE KEY uk_email (email);

-- 전문 검색 인덱스 (FULLTEXT)
CREATE FULLTEXT INDEX idx_content ON posts(title, content);
```

### A.4 데이터 조작 명령어 (CRUD)

```sql
-- INSERT: 단일 행 삽입
INSERT INTO users (username, email, password_hash) 
VALUES ('player1', 'player1@example.com', 'hashed_password');

-- INSERT: 여러 행 한번에 삽입
INSERT INTO users (username, email, password_hash) VALUES
('player1', 'player1@example.com', 'hash1'),
('player2', 'player2@example.com', 'hash2'),
('player3', 'player3@example.com', 'hash3');

-- INSERT: 중복 시 무시
INSERT IGNORE INTO users (username, email, password_hash)
VALUES ('player1', 'player1@example.com', 'hash');

-- INSERT: 중복 시 업데이트
INSERT INTO users (username, email, level) 
VALUES ('player1', 'player1@example.com', 10)
ON DUPLICATE KEY UPDATE level = level + 1;

-- SELECT: 기본 조회
SELECT * FROM users;
SELECT user_id, username, level FROM users;

-- SELECT: WHERE 조건
SELECT * FROM users WHERE level >= 10;
SELECT * FROM users WHERE username = 'player1';
SELECT * FROM users WHERE level BETWEEN 10 AND 20;
SELECT * FROM users WHERE username IN ('player1', 'player2');
SELECT * FROM users WHERE username LIKE 'player%';
SELECT * FROM users WHERE email IS NOT NULL;

-- SELECT: 정렬
SELECT * FROM users ORDER BY level DESC;
SELECT * FROM users ORDER BY level DESC, exp DESC;

-- SELECT: 개수 제한
SELECT * FROM users LIMIT 10;
SELECT * FROM users LIMIT 10 OFFSET 20;  -- 21번째부터 10개

-- SELECT: 집계 함수
SELECT COUNT(*) FROM users;
SELECT COUNT(DISTINCT username) FROM users;
SELECT AVG(level) FROM users;
SELECT SUM(gold) FROM users;
SELECT MAX(level), MIN(level) FROM users;

-- SELECT: GROUP BY
SELECT level, COUNT(*) as count 
FROM users 
GROUP BY level;

SELECT level, COUNT(*) as count 
FROM users 
GROUP BY level 
HAVING count > 10;

-- UPDATE: 데이터 수정
UPDATE users SET level = level + 1 WHERE user_id = 1;
UPDATE users SET gold = gold - 100, updated_at = NOW() WHERE user_id = 1;

-- UPDATE: 조건부 수정
UPDATE users SET level = 100 WHERE level > 100;

-- DELETE: 데이터 삭제
DELETE FROM users WHERE user_id = 1;
DELETE FROM users WHERE level < 10;

-- DELETE: 전체 삭제
DELETE FROM users;
```

### A.5 조인(JOIN) 명령어

```sql
-- INNER JOIN
SELECT u.username, c.character_name, c.level
FROM users u
INNER JOIN characters c ON u.user_id = c.user_id;

-- LEFT JOIN
SELECT u.username, c.character_name
FROM users u
LEFT JOIN characters c ON u.user_id = c.user_id;

-- RIGHT JOIN
SELECT u.username, c.character_name
FROM users u
RIGHT JOIN characters c ON u.user_id = c.user_id;

-- CROSS JOIN
SELECT u.username, i.item_name
FROM users u
CROSS JOIN items i;

-- SELF JOIN (자기 자신과 조인)
SELECT f1.user_id, f2.user_id as friend_id
FROM friends f1
INNER JOIN friends f2 ON f1.user_id = f2.friend_id;

-- 다중 테이블 조인
SELECT u.username, c.character_name, i.item_name, inv.quantity
FROM users u
INNER JOIN characters c ON u.user_id = c.user_id
INNER JOIN inventory inv ON c.character_id = inv.character_id
INNER JOIN items i ON inv.item_id = i.item_id;
```

### A.6 서브쿼리 명령어

```sql
-- WHERE 절 서브쿼리
SELECT username FROM users 
WHERE user_id IN (SELECT user_id FROM characters WHERE level > 50);

-- FROM 절 서브쿼리
SELECT avg_level FROM (
    SELECT AVG(level) as avg_level FROM users
) as subquery;

-- EXISTS 서브쿼리
SELECT username FROM users u
WHERE EXISTS (
    SELECT 1 FROM characters c 
    WHERE c.user_id = u.user_id AND c.level > 50
);

-- 상관 서브쿼리
SELECT username, (
    SELECT COUNT(*) FROM characters c 
    WHERE c.user_id = u.user_id
) as character_count
FROM users u;
```

### A.7 트랜잭션 명령어

```sql
-- 트랜잭션 시작
START TRANSACTION;
BEGIN;

-- 커밋 (변경사항 확정)
COMMIT;

-- 롤백 (변경사항 취소)
ROLLBACK;

-- 세이브포인트 생성
SAVEPOINT sp1;

-- 세이브포인트로 롤백
ROLLBACK TO SAVEPOINT sp1;

-- 자동 커밋 설정
SET autocommit = 0;  -- 비활성화
SET autocommit = 1;  -- 활성화

-- 트랜잭션 격리 수준 설정
SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;
SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ;
SET SESSION TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SET SESSION TRANSACTION ISOLATION LEVEL SERIALIZABLE;

-- 락 사용
SELECT * FROM users WHERE user_id = 1 FOR UPDATE;  -- 배타적 락
SELECT * FROM users WHERE user_id = 1 FOR SHARE;   -- 공유 락
```

### A.8 유용한 유틸리티 명령어

```sql
-- 테이블 상태 확인
SHOW TABLE STATUS LIKE 'users';

-- 프로세스 목록 확인
SHOW PROCESSLIST;
SHOW FULL PROCESSLIST;

-- 변수 확인
SHOW VARIABLES LIKE '%character%';
SHOW VARIABLES LIKE '%timeout%';

-- 시스템 상태 확인
SHOW STATUS;
SHOW STATUS LIKE 'Threads%';

-- 쿼리 실행 계획 확인
EXPLAIN SELECT * FROM users WHERE level > 50;
EXPLAIN FORMAT=JSON SELECT * FROM users WHERE level > 50;

-- 경고 메시지 확인
SHOW WARNINGS;

-- 에러 메시지 확인
SHOW ERRORS;

-- 테이블 최적화
OPTIMIZE TABLE users;

-- 테이블 분석
ANALYZE TABLE users;

-- 테이블 체크
CHECK TABLE users;

-- 테이블 복구
REPAIR TABLE users;
```

### A.9 날짜/시간 함수

```sql
-- 현재 날짜/시간
SELECT NOW();                    -- 현재 날짜와 시간
SELECT CURDATE();               -- 현재 날짜
SELECT CURTIME();               -- 현재 시간
SELECT UNIX_TIMESTAMP();        -- Unix 타임스탬프

-- 날짜 연산
SELECT DATE_ADD(NOW(), INTERVAL 1 DAY);
SELECT DATE_SUB(NOW(), INTERVAL 1 MONTH);
SELECT DATEDIFF('2025-12-31', NOW());

-- 날짜 포맷
SELECT DATE_FORMAT(NOW(), '%Y-%m-%d %H:%i:%s');
SELECT DATE_FORMAT(NOW(), '%Y년 %m월 %d일');

-- 날짜 추출
SELECT YEAR(NOW());
SELECT MONTH(NOW());
SELECT DAY(NOW());
SELECT HOUR(NOW());
SELECT MINUTE(NOW());
SELECT SECOND(NOW());

-- 요일 확인
SELECT DAYOFWEEK(NOW());        -- 1=일요일, 7=토요일
SELECT WEEKDAY(NOW());          -- 0=월요일, 6=일요일

-- 날짜 비교
SELECT * FROM users WHERE created_at >= DATE_SUB(NOW(), INTERVAL 7 DAY);
SELECT * FROM users WHERE DATE(created_at) = CURDATE();
```

### A.10 문자열 함수

```sql
-- 문자열 연결
SELECT CONCAT('Hello', ' ', 'World');
SELECT CONCAT_WS(',', 'apple', 'banana', 'cherry');

-- 문자열 길이
SELECT LENGTH('Hello');         -- 바이트 길이
SELECT CHAR_LENGTH('Hello');    -- 문자 길이

-- 대소문자 변환
SELECT UPPER('hello');
SELECT LOWER('HELLO');

-- 공백 제거
SELECT TRIM('  hello  ');
SELECT LTRIM('  hello');
SELECT RTRIM('hello  ');

-- 부분 문자열
SELECT SUBSTRING('Hello World', 1, 5);
SELECT LEFT('Hello World', 5);
SELECT RIGHT('Hello World', 5);

-- 문자열 검색
SELECT LOCATE('World', 'Hello World');
SELECT INSTR('Hello World', 'World');

-- 문자열 치환
SELECT REPLACE('Hello World', 'World', 'MySQL');
```

### A.11 수학 함수

```sql
-- 반올림
SELECT ROUND(123.456, 2);       -- 123.46
SELECT CEIL(123.456);           -- 124
SELECT FLOOR(123.456);          -- 123

-- 절대값
SELECT ABS(-123);               -- 123

-- 난수 생성
SELECT RAND();                  -- 0.0 ~ 1.0
SELECT FLOOR(RAND() * 100);     -- 0 ~ 99

-- 거듭제곱
SELECT POW(2, 3);               -- 8
SELECT SQRT(16);                -- 4

-- 최대/최소
SELECT GREATEST(10, 20, 30);    -- 30
SELECT LEAST(10, 20, 30);       -- 10
```

### A.12 조건 함수

```sql
-- IF 함수
SELECT IF(level >= 50, 'High', 'Low') FROM users;

-- CASE 문
SELECT username,
    CASE 
        WHEN level >= 80 THEN '고수'
        WHEN level >= 50 THEN '중수'
        ELSE '초보'
    END as grade
FROM users;

-- IFNULL / COALESCE
SELECT IFNULL(nickname, username) FROM users;
SELECT COALESCE(nickname, username, 'Unknown') FROM users;

-- NULLIF
SELECT NULLIF(gold, 0) FROM users;  -- gold가 0이면 NULL 반환
```

## B. SQLKata 주요 메서드 레퍼런스

### B.1 기본 쿼리 빌딩

```csharp
using SqlKata;
using SqlKata.Execution;

// SELECT - 모든 컬럼
var query = new Query("users");
var users = await _db.Query("users").GetAsync();

// SELECT - 특정 컬럼
var query = new Query("users").Select("user_id", "username", "level");
var users = await _db.Query("users")
    .Select("user_id", "username", "level")
    .GetAsync();

// SELECT - 별칭 사용
var query = new Query("users").Select("user_id as id", "username as name");
var query = new Query("users as u").Select("u.user_id", "u.username");

// SELECT - Raw 표현식
var query = new Query("users")
    .SelectRaw("COUNT(*) as total")
    .SelectRaw("AVG(level) as avg_level");

// DISTINCT
var query = new Query("users").Distinct().Select("level");

// LIMIT / OFFSET
var query = new Query("users").Limit(10);
var query = new Query("users").Limit(10).Offset(20);

// 단일 행 조회
var user = await _db.Query("users")
    .Where("user_id", 1)
    .FirstOrDefaultAsync();

// 첫 번째 행만 조회
var user = await _db.Query("users")
    .OrderByDesc("created_at")
    .FirstAsync();
```

### B.2 WHERE 조건

```csharp
// 기본 WHERE
var query = new Query("users").Where("level", 50);
var query = new Query("users").Where("level", ">", 50);
var query = new Query("users").Where("username", "player1");

// 여러 WHERE 조건 (AND)
var query = new Query("users")
    .Where("level", ">", 50)
    .Where("gold", ">", 1000);

// OR 조건
var query = new Query("users")
    .Where("level", ">", 80)
    .OrWhere("gold", ">", 10000);

// WHERE IN
var query = new Query("users")
    .WhereIn("user_id", new[] { 1, 2, 3, 4, 5 });

// WHERE NOT IN
var query = new Query("users")
    .WhereNotIn("status", new[] { "banned", "deleted" });

// WHERE NULL
var query = new Query("users").WhereNull("deleted_at");
var query = new Query("users").WhereNotNull("email");

// WHERE BETWEEN
var query = new Query("users").WhereBetween("level", 10, 50);

// WHERE LIKE
var query = new Query("users").WhereLike("username", "%player%");
var query = new Query("users").WhereStarts("username", "player");
var query = new Query("users").WhereEnds("username", "123");
var query = new Query("users").WhereContains("username", "player");

// WHERE DATE
var query = new Query("users")
    .WhereDate("created_at", "2025-12-26");
var query = new Query("users")
    .WhereDateBetween("created_at", startDate, endDate);

// WHERE RAW
var query = new Query("users")
    .WhereRaw("level > ? AND gold > ?", 50, 1000);

// 중첩 WHERE (서브 그룹)
var query = new Query("users")
    .Where("status", "active")
    .Where(q => q
        .Where("level", ">", 80)
        .OrWhere("gold", ">", 10000)
    );
```

### B.3 JOIN

```csharp
// INNER JOIN
var query = new Query("users")
    .Join("characters", "users.user_id", "characters.user_id");

// LEFT JOIN
var query = new Query("users")
    .LeftJoin("characters", "users.user_id", "characters.user_id");

// RIGHT JOIN
var query = new Query("users")
    .RightJoin("characters", "users.user_id", "characters.user_id");

// CROSS JOIN
var query = new Query("users")
    .CrossJoin("items");

// 복잡한 JOIN 조건
var query = new Query("users")
    .Join("characters", join => join
        .On("users.user_id", "characters.user_id")
        .Where("characters.level", ">", 50)
    );

// 여러 테이블 JOIN
var query = new Query("users as u")
    .Join("characters as c", "u.user_id", "c.user_id")
    .Join("inventory as i", "c.character_id", "i.character_id")
    .Join("items as it", "i.item_id", "it.item_id");
```

### B.4 ORDER BY

```csharp
// 기본 정렬
var query = new Query("users").OrderBy("level");
var query = new Query("users").OrderByDesc("level");

// 여러 컬럼 정렬
var query = new Query("users")
    .OrderByDesc("level")
    .ThenByDesc("exp");

// RAW 정렬
var query = new Query("users")
    .OrderByRaw("FIELD(status, 'active', 'inactive', 'banned')");
```

### B.5 GROUP BY / HAVING

```csharp
// GROUP BY
var query = new Query("users")
    .Select("level", "COUNT(*) as count")
    .GroupBy("level");

// HAVING
var query = new Query("users")
    .Select("level", "COUNT(*) as count")
    .GroupBy("level")
    .Having("count", ">", 10);

// HAVING RAW
var query = new Query("users")
    .Select("level", "AVG(exp) as avg_exp")
    .GroupBy("level")
    .HavingRaw("AVG(exp) > ?", 5000);
```

### B.6 집계 함수

```csharp
// COUNT
var count = await _db.Query("users").CountAsync<int>();
var count = await _db.Query("users")
    .Where("level", ">", 50)
    .CountAsync<int>();

// SUM
var totalGold = await _db.Query("users")
    .SumAsync<long>("gold");

// AVG
var avgLevel = await _db.Query("users")
    .AverageAsync<double>("level");

// MAX / MIN
var maxLevel = await _db.Query("users")
    .MaxAsync<int>("level");
var minLevel = await _db.Query("users")
    .MinAsync<int>("level");

// 여러 집계 함수 동시 사용
var stats = await _db.Query("users")
    .SelectRaw("COUNT(*) as total")
    .SelectRaw("AVG(level) as avg_level")
    .SelectRaw("SUM(gold) as total_gold")
    .FirstOrDefaultAsync();
```

### B.7 INSERT

```csharp
// 단일 INSERT
var id = await _db.Query("users").InsertGetIdAsync<long>(new
{
    username = "player1",
    email = "player1@example.com",
    password_hash = "hashed_password"
});

// INSERT (ID 반환 없음)
await _db.Query("users").InsertAsync(new
{
    username = "player1",
    email = "player1@example.com"
});

// 여러 행 INSERT
await _db.Query("users").InsertAsync(new[]
{
    new { username = "player1", email = "player1@example.com" },
    new { username = "player2", email = "player2@example.com" },
    new { username = "player3", email = "player3@example.com" }
});
```

### B.8 UPDATE

```csharp
// 기본 UPDATE
await _db.Query("users")
    .Where("user_id", 1)
    .UpdateAsync(new
    {
        level = 100,
        updated_at = DateTime.Now
    });

// 증감 연산
await _db.Query("users")
    .Where("user_id", 1)
    .IncrementAsync("level", 1);

await _db.Query("users")
    .Where("user_id", 1)
    .DecrementAsync("gold", 100);

// 여러 컬럼 증감
await _db.Query("users")
    .Where("user_id", 1)
    .IncrementAsync(new Dictionary<string, int>
    {
        { "level", 1 },
        { "exp", 100 }
    });

// UPDATE RAW
await _db.Query("users")
    .Where("user_id", 1)
    .UpdateAsync(new
    {
        gold = new SqlKata.UnsafeLiteral("gold + 100")
    });
```

### B.9 DELETE

```csharp
// 기본 DELETE
await _db.Query("users")
    .Where("user_id", 1)
    .DeleteAsync();

// 조건부 DELETE
await _db.Query("users")
    .Where("level", "<", 10)
    .DeleteAsync();

// 전체 삭제 (주의!)
await _db.Query("users").DeleteAsync();
```

### B.10 서브쿼리

```csharp
// WHERE 절 서브쿼리
var subQuery = new Query("characters")
    .Select("user_id")
    .Where("level", ">", 50);

var query = new Query("users")
    .WhereIn("user_id", subQuery);

// FROM 절 서브쿼리
var subQuery = new Query("users")
    .SelectRaw("AVG(level) as avg_level");

var query = new Query()
    .From(subQuery, "sub")
    .Select("avg_level");

// SELECT 절 서브쿼리
var query = new Query("users as u")
    .Select("u.username")
    .SelectRaw("(SELECT COUNT(*) FROM characters c WHERE c.user_id = u.user_id) as char_count");
```

### B.11 트랜잭션

```csharp
using var transaction = await _db.Connection.BeginTransactionAsync();

try
{
    // 여러 쿼리 실행
    await _db.Query("users")
        .Where("user_id", 1)
        .UpdateAsync(new { gold = 900 });

    await _db.Query("inventory")
        .InsertAsync(new { user_id = 1, item_id = 100 });

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### B.12 유용한 메서드

```csharp
// 쿼리 문자열 출력 (디버깅용)
var query = new Query("users")
    .Where("level", ">", 50);
var sql = _compiler.Compile(query).ToString();
Console.WriteLine(sql);

// 페이징
var page = 2;
var pageSize = 20;
var query = new Query("users")
    .OrderByDesc("level")
    .ForPage(page, pageSize);

// Chunk (대량 데이터 처리)
await _db.Query("users").ChunkAsync(1000, async (users) =>
{
    foreach (var user in users)
    {
        // 처리 로직
    }
});

// EXISTS
var exists = await _db.Query("users")
    .Where("username", "player1")
    .ExistsAsync();

// Clone (쿼리 복사)
var baseQuery = new Query("users")
    .Where("status", "active");
var query1 = baseQuery.Clone().Where("level", ">", 50);
var query2 = baseQuery.Clone().Where("gold", ">", 1000);
```

## C. 게임별 데이터베이스 설계 예시

### C.1 MMORPG 데이터베이스 설계

MMORPG는 복잡한 시스템이 많아 테이블이 많이 필요하다.

```
┌─────────────────────────────────────────────────┐
│          MMORPG Database Schema                 │
├─────────────────────────────────────────────────┤
│                                                  │
│  [Users] ─────┬───── [Characters]               │
│      │        │           │                      │
│      │        │           ├───── [Inventory]     │
│      │        │           ├───── [Equipment]     │
│      │        │           └───── [Skills]        │
│      │        │                                  │
│      │        ├───── [Friends]                   │
│      │        ├───── [Guilds] ─── [GuildMembers] │
│      │        ├───── [Mails]                     │
│      │        └───── [Attendance]                │
│      │                                           │
│      └─────────────── [PurchaseHistory]         │
│                                                  │
│  [Items] ───────────── [Inventory]              │
│  [Monsters] ──────── [MonsterDrops]             │
│  [Quests] ────────── [QuestProgress]            │
│  [Dungeons] ───────── [DungeonRecords]          │
│                                                  │
└─────────────────────────────────────────────────┘
```

```sql
-- 유저 계정 테이블
CREATE TABLE users (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME,
    status ENUM('active', 'inactive', 'banned') NOT NULL DEFAULT 'active',
    INDEX idx_username (username),
    INDEX idx_email (email),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 캐릭터 테이블
CREATE TABLE characters (
    character_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    character_name VARCHAR(50) NOT NULL UNIQUE,
    class ENUM('warrior', 'mage', 'archer', 'priest') NOT NULL,
    level INT NOT NULL DEFAULT 1,
    exp BIGINT NOT NULL DEFAULT 0,
    hp INT NOT NULL DEFAULT 100,
    mp INT NOT NULL DEFAULT 100,
    strength INT NOT NULL DEFAULT 10,
    intelligence INT NOT NULL DEFAULT 10,
    dexterity INT NOT NULL DEFAULT 10,
    gold BIGINT NOT NULL DEFAULT 0,
    map_id INT NOT NULL DEFAULT 1,
    position_x FLOAT NOT NULL DEFAULT 0,
    position_y FLOAT NOT NULL DEFAULT 0,
    position_z FLOAT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_character_name (character_name),
    INDEX idx_level (level DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 아이템 마스터 테이블
CREATE TABLE items (
    item_id INT AUTO_INCREMENT PRIMARY KEY,
    item_name VARCHAR(100) NOT NULL,
    item_type ENUM('weapon', 'armor', 'consumable', 'material', 'quest') NOT NULL,
    grade ENUM('common', 'uncommon', 'rare', 'epic', 'legendary') NOT NULL DEFAULT 'common',
    description TEXT,
    max_stack INT NOT NULL DEFAULT 1,
    price INT NOT NULL DEFAULT 0,
    required_level INT NOT NULL DEFAULT 1,
    attack_power INT NOT NULL DEFAULT 0,
    defense_power INT NOT NULL DEFAULT 0,
    INDEX idx_item_type (item_type),
    INDEX idx_grade (grade)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 인벤토리 테이블
CREATE TABLE inventory (
    inventory_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    character_id BIGINT NOT NULL,
    item_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    slot_index INT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id),
    UNIQUE KEY uk_character_slot (character_id, slot_index),
    INDEX idx_character_id (character_id),
    INDEX idx_item_id (item_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 장비 테이블
CREATE TABLE equipment (
    equipment_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    character_id BIGINT NOT NULL,
    slot_type ENUM('weapon', 'helmet', 'armor', 'gloves', 'boots', 'accessory1', 'accessory2') NOT NULL,
    item_id INT NOT NULL,
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id),
    UNIQUE KEY uk_character_slot (character_id, slot_type),
    INDEX idx_character_id (character_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 길드 테이블
CREATE TABLE guilds (
    guild_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    guild_name VARCHAR(50) NOT NULL UNIQUE,
    master_character_id BIGINT NOT NULL,
    level INT NOT NULL DEFAULT 1,
    exp BIGINT NOT NULL DEFAULT 0,
    notice TEXT,
    max_members INT NOT NULL DEFAULT 50,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (master_character_id) REFERENCES characters(character_id),
    INDEX idx_guild_name (guild_name),
    INDEX idx_level (level DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 길드 멤버 테이블
CREATE TABLE guild_members (
    guild_member_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    guild_id BIGINT NOT NULL,
    character_id BIGINT NOT NULL,
    rank ENUM('master', 'officer', 'member') NOT NULL DEFAULT 'member',
    contribution INT NOT NULL DEFAULT 0,
    joined_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (guild_id) REFERENCES guilds(guild_id) ON DELETE CASCADE,
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    UNIQUE KEY uk_character (character_id),
    INDEX idx_guild_id (guild_id),
    INDEX idx_contribution (contribution DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 퀘스트 마스터 테이블
CREATE TABLE quests (
    quest_id INT AUTO_INCREMENT PRIMARY KEY,
    quest_name VARCHAR(100) NOT NULL,
    description TEXT,
    required_level INT NOT NULL DEFAULT 1,
    quest_type ENUM('main', 'sub', 'daily', 'weekly') NOT NULL,
    reward_exp INT NOT NULL DEFAULT 0,
    reward_gold INT NOT NULL DEFAULT 0,
    INDEX idx_quest_type (quest_type)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 퀘스트 진행 테이블
CREATE TABLE quest_progress (
    progress_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    character_id BIGINT NOT NULL,
    quest_id INT NOT NULL,
    status ENUM('in_progress', 'completed', 'abandoned') NOT NULL DEFAULT 'in_progress',
    progress_data JSON,
    started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at DATETIME,
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    FOREIGN KEY (quest_id) REFERENCES quests(quest_id),
    INDEX idx_character_status (character_id, status),
    INDEX idx_quest_id (quest_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 던전 기록 테이블
CREATE TABLE dungeon_records (
    record_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    character_id BIGINT NOT NULL,
    dungeon_id INT NOT NULL,
    difficulty ENUM('normal', 'hard', 'hell') NOT NULL,
    clear_time INT,  -- 초 단위
    score INT NOT NULL DEFAULT 0,
    is_cleared BOOLEAN NOT NULL DEFAULT FALSE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (character_id) REFERENCES characters(character_id) ON DELETE CASCADE,
    INDEX idx_character_dungeon (character_id, dungeon_id),
    INDEX idx_score (dungeon_id, score DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### C.2 모바일 퍼즐 게임 데이터베이스 설계

모바일 퍼즐 게임은 비교적 간단한 구조를 가진다.

```sql
-- 유저 테이블
CREATE TABLE users (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    device_id VARCHAR(100) NOT NULL UNIQUE,
    nickname VARCHAR(50),
    level INT NOT NULL DEFAULT 1,
    exp BIGINT NOT NULL DEFAULT 0,
    coin INT NOT NULL DEFAULT 0,
    gem INT NOT NULL DEFAULT 0,
    heart INT NOT NULL DEFAULT 5,
    last_heart_recharged DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME,
    INDEX idx_device_id (device_id),
    INDEX idx_level (level DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 스테이지 기록 테이블
CREATE TABLE stage_records (
    record_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    stage_id INT NOT NULL,
    stars INT NOT NULL DEFAULT 0,  -- 0~3 별점
    best_score INT NOT NULL DEFAULT 0,
    clear_count INT NOT NULL DEFAULT 0,
    first_cleared_at DATETIME,
    last_played_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_stage (user_id, stage_id),
    INDEX idx_user_id (user_id),
    INDEX idx_score (stage_id, best_score DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 보유 아이템 테이블
CREATE TABLE user_items (
    user_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    item_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_item (user_id, item_id),
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 일일 미션 테이블
CREATE TABLE daily_missions (
    mission_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    mission_type ENUM('play_stage', 'clear_stage', 'use_item', 'login') NOT NULL,
    target_count INT NOT NULL,
    current_count INT NOT NULL DEFAULT 0,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    is_rewarded BOOLEAN NOT NULL DEFAULT FALSE,
    mission_date DATE NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_user_date (user_id, mission_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 랭킹 캐시 테이블
CREATE TABLE ranking_cache (
    rank INT NOT NULL,
    user_id BIGINT NOT NULL,
    nickname VARCHAR(50),
    level INT NOT NULL,
    total_stars INT NOT NULL,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (rank),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_total_stars (total_stars DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### C.3 전략 시뮬레이션 게임 데이터베이스 설계

전략 시뮬레이션 게임은 자원 관리와 건물 시스템이 중요하다.

```sql
-- 유저 테이블
CREATE TABLE users (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    empire_name VARCHAR(50) NOT NULL,
    level INT NOT NULL DEFAULT 1,
    exp BIGINT NOT NULL DEFAULT 0,
    gold BIGINT NOT NULL DEFAULT 1000,
    wood BIGINT NOT NULL DEFAULT 500,
    stone BIGINT NOT NULL DEFAULT 500,
    food BIGINT NOT NULL DEFAULT 500,
    power INT NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME,
    INDEX idx_username (username),
    INDEX idx_power (power DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 건물 테이블
CREATE TABLE buildings (
    building_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    building_type ENUM('castle', 'barrack', 'farm', 'mine', 'lumbermill', 'warehouse') NOT NULL,
    level INT NOT NULL DEFAULT 1,
    position_x INT NOT NULL,
    position_y INT NOT NULL,
    upgrade_started_at DATETIME,
    upgrade_finished_at DATETIME,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_upgrade (upgrade_finished_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 군대 테이블
CREATE TABLE troops (
    troop_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    unit_type ENUM('infantry', 'cavalry', 'archer', 'siege') NOT NULL,
    quantity INT NOT NULL DEFAULT 0,
    training_quantity INT NOT NULL DEFAULT 0,
    training_finished_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_unit (user_id, unit_type),
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 전투 기록 테이블
CREATE TABLE battle_records (
    battle_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    attacker_id BIGINT NOT NULL,
    defender_id BIGINT NOT NULL,
    result ENUM('attacker_win', 'defender_win', 'draw') NOT NULL,
    attacker_casualties JSON,  -- {"infantry": 10, "cavalry": 5}
    defender_casualties JSON,
    resources_plundered JSON,  -- {"gold": 1000, "food": 500}
    battle_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (attacker_id) REFERENCES users(user_id),
    FOREIGN KEY (defender_id) REFERENCES users(user_id),
    INDEX idx_attacker (attacker_id, battle_time),
    INDEX idx_defender (defender_id, battle_time)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 동맹(길드) 테이블
CREATE TABLE alliances (
    alliance_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    alliance_name VARCHAR(50) NOT NULL UNIQUE,
    leader_id BIGINT NOT NULL,
    level INT NOT NULL DEFAULT 1,
    total_power BIGINT NOT NULL DEFAULT 0,
    member_count INT NOT NULL DEFAULT 1,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (leader_id) REFERENCES users(user_id),
    INDEX idx_alliance_name (alliance_name),
    INDEX idx_total_power (total_power DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 동맹 멤버 테이블
CREATE TABLE alliance_members (
    member_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    alliance_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    rank ENUM('leader', 'officer', 'member') NOT NULL DEFAULT 'member',
    joined_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (alliance_id) REFERENCES alliances(alliance_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user (user_id),
    INDEX idx_alliance_id (alliance_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 연구 테이블
CREATE TABLE research (
    research_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    tech_type ENUM('economy', 'military', 'defense', 'construction') NOT NULL,
    tech_name VARCHAR(50) NOT NULL,
    level INT NOT NULL DEFAULT 0,
    max_level INT NOT NULL DEFAULT 10,
    research_started_at DATETIME,
    research_finished_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_tech (user_id, tech_name),
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### C.4 실시간 PVP 게임 데이터베이스 설계

실시간 PVP 게임은 매칭과 전적 기록이 중요하다.

```sql
-- 유저 테이블
CREATE TABLE users (
    user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    level INT NOT NULL DEFAULT 1,
    exp BIGINT NOT NULL DEFAULT 0,
    rating INT NOT NULL DEFAULT 1000,  -- MMR
    tier ENUM('bronze', 'silver', 'gold', 'platinum', 'diamond', 'master') NOT NULL DEFAULT 'bronze',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login DATETIME,
    INDEX idx_username (username),
    INDEX idx_rating (rating DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 캐릭터/덱 테이블
CREATE TABLE user_decks (
    deck_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    deck_name VARCHAR(50) NOT NULL,
    deck_data JSON NOT NULL,  -- 덱 구성 정보
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 매치 기록 테이블
CREATE TABLE match_records (
    match_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    player1_id BIGINT NOT NULL,
    player2_id BIGINT NOT NULL,
    winner_id BIGINT,  -- NULL이면 무승부
    player1_rating_before INT NOT NULL,
    player2_rating_before INT NOT NULL,
    player1_rating_after INT NOT NULL,
    player2_rating_after INT NOT NULL,
    match_duration INT,  -- 초 단위
    match_data JSON,  -- 상세 전투 데이터
    match_time DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (player1_id) REFERENCES users(user_id),
    FOREIGN KEY (player2_id) REFERENCES users(user_id),
    INDEX idx_player1 (player1_id, match_time),
    INDEX idx_player2 (player2_id, match_time),
    INDEX idx_match_time (match_time DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 시즌 통계 테이블
CREATE TABLE season_stats (
    stat_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    season INT NOT NULL,
    total_matches INT NOT NULL DEFAULT 0,
    wins INT NOT NULL DEFAULT 0,
    losses INT NOT NULL DEFAULT 0,
    draws INT NOT NULL DEFAULT 0,
    highest_rating INT NOT NULL DEFAULT 1000,
    current_rating INT NOT NULL DEFAULT 1000,
    tier ENUM('bronze', 'silver', 'gold', 'platinum', 'diamond', 'master') NOT NULL DEFAULT 'bronze',
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_season (user_id, season),
    INDEX idx_season_rating (season, current_rating DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 시즌 보상 수령 기록
CREATE TABLE season_rewards (
    reward_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    season INT NOT NULL,
    tier ENUM('bronze', 'silver', 'gold', 'platinum', 'diamond', 'master') NOT NULL,
    rewards JSON NOT NULL,
    claimed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_season (user_id, season)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### C.5 공통 유틸리티 테이블

모든 게임에 공통적으로 필요한 테이블들이다.

```sql
-- 공지사항 테이블
CREATE TABLE announcements (
    announcement_id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    type ENUM('maintenance', 'update', 'event', 'notice') NOT NULL,
    priority INT NOT NULL DEFAULT 0,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_active_date (is_active, start_date, end_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 쿠폰 테이블
CREATE TABLE coupons (
    coupon_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    coupon_code VARCHAR(50) NOT NULL UNIQUE,
    coupon_type ENUM('item', 'currency', 'exp') NOT NULL,
    reward_data JSON NOT NULL,
    max_usage INT NOT NULL DEFAULT 1,
    current_usage INT NOT NULL DEFAULT 0,
    expire_at DATETIME NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_coupon_code (coupon_code),
    INDEX idx_expire (is_active, expire_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 쿠폰 사용 기록
CREATE TABLE coupon_usage (
    usage_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    coupon_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    used_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (coupon_id) REFERENCES coupons(coupon_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_coupon_user (coupon_id, user_id),
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 이벤트 테이블
CREATE TABLE events (
    event_id INT AUTO_INCREMENT PRIMARY KEY,
    event_name VARCHAR(100) NOT NULL,
    event_type ENUM('login', 'mission', 'purchase', 'ranking') NOT NULL,
    event_data JSON NOT NULL,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_active_date (is_active, start_date, end_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 이벤트 참여 기록
CREATE TABLE event_participants (
    participant_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    event_id INT NOT NULL,
    user_id BIGINT NOT NULL,
    progress_data JSON,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    is_rewarded BOOLEAN NOT NULL DEFAULT FALSE,
    participated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (event_id) REFERENCES events(event_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    UNIQUE KEY uk_event_user (event_id, user_id),
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 로그 테이블 (파티셔닝 권장)
CREATE TABLE game_logs (
    log_id BIGINT AUTO_INCREMENT,
    user_id BIGINT,
    log_type VARCHAR(50) NOT NULL,
    log_data JSON,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (log_id, created_at),
    INDEX idx_user_type (user_id, log_type),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
PARTITION BY RANGE (TO_DAYS(created_at)) (
    PARTITION p20251201 VALUES LESS THAN (TO_DAYS('2025-12-01')),
    PARTITION p20260101 VALUES LESS THAN (TO_DAYS('2026-01-01')),
    PARTITION p20260201 VALUES LESS THAN (TO_DAYS('2026-02-01')),
    PARTITION p_future VALUES LESS THAN MAXVALUE
);
```

---

## 부록 정리

이 부록에서는 MySQL 프로그래밍에 필요한 핵심 레퍼런스를 제공했다.

**A. MySQL 주요 명령어 치트시트**에서는 데이터베이스 관리, 테이블 생성/수정, 인덱스 관리, CRUD 작업, 조인, 트랜잭션 등 실무에서 자주 사용하는 SQL 명령어를 빠르게 찾아볼 수 있도록 정리했다. 날짜/시간 함수, 문자열 함수, 수학 함수, 조건 함수 등 유용한 내장 함수도 포함했다.

**B. SQLKata 주요 메서드 레퍼런스**에서는 C#에서 SQLKata를 사용할 때 필요한 모든 주요 메서드를 예제와 함께 정리했다. SELECT, WHERE, JOIN, INSERT, UPDATE, DELETE부터 서브쿼리, 트랜잭션, 페이징까지 실무에 바로 적용할 수 있는 코드 패턴을 제공했다.

**C. 게임별 데이터베이스 설계 예시**에서는 MMORPG, 모바일 퍼즐 게임, 전략 시뮬레이션 게임, 실시간 PVP 게임 등 다양한 게임 장르별로 실제 사용할 수 있는 데이터베이스 스키마를 제공했다. 각 게임 장르의 특성에 맞는 테이블 설계와 인덱스 전략을 배울 수 있다.

이 부록을 책갈피로 활용하여 실무에서 빠르게 참조하기 바란다. MySQL과 C#을 활용한 게임 백엔드 개발에 큰 도움이 될 것이다.    