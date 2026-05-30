# 제4장: SQL 기초 — DuckDB로 처음 배우는 쿼리
3장에서는 DuckDB의 내부 아키텍처가 어떻게 동작하는지 살펴봤다. 컬럼 기반 스토리지, 벡터화 실행 엔진, 트랜잭션 처리 방식을 이해했으니, 이제 실제로 DuckDB에 데이터를 넣고 꺼내는 방법을 배울 차례다.

이 장에서는 SQL의 가장 기본적인 문법부터 시작해서, DuckDB가 표준 SQL에 더해 제공하는 편리한 확장 문법까지 차근차근 익혀간다. 모든 예제는 온라인 게임의 실제 데이터를 다루는 상황으로 구성했다. 플레이어 정보, 전투 로그, 아이템, 퀘스트 등 게임 서버 개발자라면 매일 마주치는 데이터를 직접 손으로 입력하고 조회해 보면서 SQL 감각을 키워보자.

> **준비**: 예제를 따라 하려면 DuckDB CLI가 설치되어 있어야 한다. 터미널에서 `duckdb`를 입력해 DuckDB 프롬프트(`D >`)가 뜨는지 확인한 뒤 시작하자.

---

## 4.1 테이블 생성, 삽입, 조회 (CREATE / INSERT / SELECT)

### 테이블이란 무엇인가
데이터베이스에서 **테이블(table)**은 데이터를 저장하는 기본 단위다. 엑셀 시트처럼 행(row)과 열(column)로 구성된다. 각 열에는 이름과 데이터 타입이 정해져 있고, 각 행이 하나의 데이터 레코드를 나타낸다.

게임 서버에서는 플레이어 정보, 전투 기록, 아이템 목록 등을 테이블로 관리한다. 지금부터 이 테이블들을 직접 만들어 보자.

### CREATE TABLE — 테이블 만들기
`CREATE TABLE` 문은 새로운 테이블을 생성한다. 테이블 이름과 각 컬럼의 이름, 데이터 타입을 지정한다.

```sql
-- 플레이어 테이블 생성
CREATE TABLE players (
    player_id    INTEGER PRIMARY KEY,
    username     VARCHAR(50) NOT NULL,
    level        INTEGER DEFAULT 1,
    class        VARCHAR(20),
    server_id    INTEGER,
    created_at   TIMESTAMP DEFAULT current_timestamp,
    total_playtime_hours DOUBLE DEFAULT 0.0
);
```

각 컬럼의 의미를 살펴보자.

- `player_id INTEGER PRIMARY KEY`: 플레이어를 고유하게 식별하는 정수형 ID. `PRIMARY KEY`는 중복 값을 허용하지 않고, NULL이 될 수 없음을 의미한다.
- `username VARCHAR(50) NOT NULL`: 최대 50자의 문자열. `NOT NULL`은 반드시 값이 있어야 함을 의미한다.
- `level INTEGER DEFAULT 1`: 기본값이 1인 정수형 레벨.
- `created_at TIMESTAMP DEFAULT current_timestamp`: 레코드가 삽입될 때 자동으로 현재 시간이 입력된다.

이어서 전투 로그 테이블과 아이템 테이블도 만들어 두자.

```sql
-- 전투 로그 테이블
CREATE TABLE battle_logs (
    log_id       BIGINT PRIMARY KEY,
    attacker_id  INTEGER NOT NULL,
    defender_id  INTEGER NOT NULL,
    damage       INTEGER NOT NULL,
    skill_name   VARCHAR(50),
    is_critical  BOOLEAN DEFAULT false,
    logged_at    TIMESTAMP DEFAULT current_timestamp
);

-- 아이템 테이블
CREATE TABLE items (
    item_id      INTEGER PRIMARY KEY,
    item_name    VARCHAR(100) NOT NULL,
    item_type    VARCHAR(30),   -- weapon, armor, potion, ...
    grade        VARCHAR(10),   -- normal, rare, epic, legendary
    attack_power INTEGER DEFAULT 0,
    defense_power INTEGER DEFAULT 0,
    price        INTEGER DEFAULT 0
);

-- 퀘스트 테이블
CREATE TABLE quests (
    quest_id     INTEGER PRIMARY KEY,
    player_id    INTEGER NOT NULL,
    quest_name   VARCHAR(100) NOT NULL,
    status       VARCHAR(20) DEFAULT 'in_progress',  -- in_progress, completed, failed
    started_at   TIMESTAMP,
    completed_at TIMESTAMP
);
```

> **DuckDB의 데이터 타입**: DuckDB는 `INTEGER`, `BIGINT`, `DOUBLE`, `VARCHAR`, `BOOLEAN`, `TIMESTAMP`, `DATE` 등 표준 SQL 타입을 모두 지원한다. 특히 `HUGEINT`(128비트 정수), `LIST`, `STRUCT`, `MAP` 같은 확장 타입도 지원하는데, 이는 4.7절에서 다룬다.

### INSERT — 데이터 삽입하기
테이블을 만들었으면 데이터를 넣어야 한다. `INSERT INTO` 문으로 레코드를 삽입한다.

```sql
-- 플레이어 데이터 삽입
INSERT INTO players (player_id, username, level, class, server_id, total_playtime_hours)
VALUES
    (1, 'DragonSlayer', 75, 'Warrior',  1, 320.5),
    (2, 'ShadowArrow',  62, 'Archer',   1, 215.0),
    (3, 'HolyLight',    88, 'Healer',   2, 540.2),
    (4, 'DarkMage',     45, 'Mage',     1,  98.7),
    (5, 'IronShield',   91, 'Paladin',  2, 612.0);

-- 아이템 데이터 삽입
INSERT INTO items (item_id, item_name, item_type, grade, attack_power, defense_power, price)
VALUES
    (101, 'Iron Sword',       'weapon', 'normal',    50,   0,  500),
    (102, 'Steel Armor',      'armor',  'normal',     0,  60,  800),
    (103, 'Dragon Fang Bow',  'weapon', 'epic',      180,   0, 8500),
    (104, 'Holy Shield',      'armor',  'rare',        0, 120, 4200),
    (105, 'Phoenix Staff',    'weapon', 'legendary', 320,  30, 25000),
    (106, 'Health Potion',    'potion', 'normal',      0,   0,   100);

-- 전투 로그 삽입
INSERT INTO battle_logs (log_id, attacker_id, defender_id, damage, skill_name, is_critical, logged_at)
VALUES
    (1, 1, 2,  250, 'Dragon Strike',  false, '2025-03-01 10:05:00'),
    (2, 2, 1,   80, 'Arrow Shot',     false, '2025-03-01 10:05:02'),
    (3, 1, 2,  620, 'Dragon Strike',  true,  '2025-03-01 10:05:05'),
    (4, 3, 4,  140, 'Holy Beam',      false, '2025-03-01 11:00:00'),
    (5, 4, 3,  310, 'Dark Explosion', true,  '2025-03-01 11:00:03'),
    (6, 1, 3,  200, 'Sword Slash',    false, '2025-03-01 12:30:00'),
    (7, 5, 4,  170, 'Shield Bash',    false, '2025-03-01 12:31:00');

-- 퀘스트 데이터 삽입
INSERT INTO quests (quest_id, player_id, quest_name, status, started_at, completed_at)
VALUES
    (1, 1, 'Defeat the Dragon King',   'completed', '2025-02-20 09:00:00', '2025-02-20 11:30:00'),
    (2, 1, 'Collect 10 Dragon Scales', 'completed', '2025-02-21 14:00:00', '2025-02-21 16:00:00'),
    (3, 2, 'Shadow Forest Patrol',     'in_progress','2025-03-01 08:00:00', NULL),
    (4, 3, 'Heal 100 Players',         'completed', '2025-02-25 10:00:00', '2025-02-28 18:00:00'),
    (5, 4, 'Learn Dark Magic Vol.3',   'in_progress','2025-03-01 09:00:00', NULL),
    (6, 5, 'Guard the Castle Gate',    'failed',    '2025-02-28 20:00:00', NULL);
```

> **팁**: `INSERT INTO ... VALUES` 구문에서 여러 행을 한 번에 삽입할 때는 각 행을 쉼표로 구분한다. 수십만 건의 대량 데이터를 넣을 때는 CSV나 Parquet 파일에서 직접 읽어오는 방식이 훨씬 빠르다(6장에서 다룬다).

### SELECT — 데이터 조회하기
데이터를 넣었으면 꺼내보자. `SELECT` 문은 테이블에서 원하는 데이터를 조회한다.

```sql
-- 모든 플레이어 조회
SELECT * FROM players;
```

**실행 결과:**
```
┌───────────┬─────────────┬───────┬─────────┬───────────┬─────────────────────┬──────────────────────┐
│ player_id │  username   │ level │  class  │ server_id │     created_at      │ total_playtime_hours │
│   int32   │   varchar   │ int32 │ varchar │   int32   │      timestamp      │        double        │
├───────────┼─────────────┼───────┼─────────┼───────────┼─────────────────────┼──────────────────────┤
│         1 │ DragonSlayer│    75 │ Warrior │         1 │ 2025-03-28 ...      │                320.5 │
│         2 │ ShadowArrow │    62 │ Archer  │         1 │ 2025-03-28 ...      │                215.0 │
│         3 │ HolyLight   │    88 │ Healer  │         2 │ 2025-03-28 ...      │                542.2 │
│         4 │ DarkMage    │    45 │ Mage    │         1 │ 2025-03-28 ...      │                 98.7 │
│         5 │ IronShield  │    91 │ Paladin │         2 │ 2025-03-28 ...      │                612.0 │
└───────────┴─────────────┴───────┴─────────┴───────────┴─────────────────────┴──────────────────────┘
```

특정 컬럼만 선택해서 보고 싶다면 `*` 대신 컬럼 이름을 나열한다.

```sql
-- 유저명, 레벨, 직업만 조회
SELECT username, level, class FROM players;
```

**실행 결과:**
```
┌─────────────┬───────┬─────────┐
│  username   │ level │  class  │
│   varchar   │ int32 │ varchar │
├─────────────┼───────┼─────────┤
│ DragonSlayer│    75 │ Warrior │
│ ShadowArrow │    62 │ Archer  │
│ HolyLight   │    88 │ Healer  │
│ DarkMage    │    45 │ Mage    │
│ IronShield  │    91 │ Paladin │
└─────────────┴───────┴─────────┘
```

`SELECT`에서 컬럼에 **별칭(alias)**을 붙이려면 `AS` 키워드를 사용한다.

```sql
-- 컬럼 별칭 사용
SELECT
    username    AS 플레이어명,
    level       AS 레벨,
    class       AS 직업,
    total_playtime_hours AS 총_플레이시간
FROM players;
```

> **포인트**: `SELECT *`는 편리하지만, 컬럼이 많을 때는 필요한 컬럼만 명시하는 것이 성능에 유리하다. DuckDB는 컬럼 기반 스토리지이므로 사용하지 않는 컬럼을 아예 읽지 않아 특히 효율적이다.

---
  

## 4.2 WHERE, ORDER BY, LIMIT

### WHERE — 조건으로 필터링하기
`WHERE` 절은 특정 조건에 맞는 행만 골라서 조회할 때 사용한다. 게임 데이터에서는 "레벨 50 이상인 플레이어만 조회", "크리티컬 공격만 조회" 같은 필터링이 자주 필요하다.

```sql
-- 레벨이 70 이상인 플레이어만 조회
SELECT username, level, class
FROM players
WHERE level >= 70;
```

**실행 결과:**
```
┌─────────────┬───────┬─────────┐
│  username   │ level │  class  │
├─────────────┼───────┼─────────┤
│ DragonSlayer│    75 │ Warrior │
│ HolyLight   │    88 │ Healer  │
│ IronShield  │    91 │ Paladin │
└─────────────┴───────┴─────────┘
```

`WHERE` 절에는 다양한 비교 연산자와 논리 연산자를 사용할 수 있다.

```sql
-- AND: 서버 1에 속하면서 레벨이 60 이상인 플레이어
SELECT username, level, server_id
FROM players
WHERE server_id = 1 AND level >= 60;

-- OR: Warrior이거나 Paladin인 플레이어
SELECT username, class
FROM players
WHERE class = 'Warrior' OR class = 'Paladin';

-- NOT: Mage가 아닌 플레이어
SELECT username, class
FROM players
WHERE NOT class = 'Mage';

-- BETWEEN: 레벨이 60~80 사이인 플레이어
SELECT username, level
FROM players
WHERE level BETWEEN 60 AND 80;

-- IN: 특정 직업군에 속하는 플레이어
SELECT username, class
FROM players
WHERE class IN ('Warrior', 'Archer', 'Mage');

-- LIKE: 이름에 'Shadow'가 포함된 플레이어 (% = 임의의 문자열)
SELECT username
FROM players
WHERE username LIKE '%Shadow%';

-- IS NULL: 완료 시간이 없는(아직 진행 중인) 퀘스트
SELECT quest_name, status
FROM quests
WHERE completed_at IS NULL;
```

**IS NULL 실행 결과:**
```
┌──────────────────────────┬─────────────┐
│        quest_name        │   status    │
├──────────────────────────┼─────────────┤
│ Shadow Forest Patrol     │ in_progress │
│ Learn Dark Magic Vol.3   │ in_progress │
│ Guard the Castle Gate    │ failed      │
└──────────────────────────┴─────────────┘
```

비교 연산자 정리표를 알아두자.

| 연산자 | 의미 | 예시 |
|--------|------|------|
| `=` | 같다 | `level = 75` |
| `<>` 또는 `!=` | 같지 않다 | `class <> 'Mage'` |
| `>` | 크다 | `damage > 300` |
| `>=` | 크거나 같다 | `level >= 70` |
| `<` | 작다 | `price < 1000` |
| `<=` | 작거나 같다 | `level <= 50` |
| `BETWEEN a AND b` | a 이상 b 이하 | `level BETWEEN 60 AND 80` |
| `IN (...)` | 목록에 포함 | `class IN ('Warrior', 'Mage')` |
| `LIKE` | 패턴 매칭 | `username LIKE 'Dragon%'` |
| `IS NULL` | NULL이다 | `completed_at IS NULL` |
| `IS NOT NULL` | NULL이 아니다 | `completed_at IS NOT NULL` |

### ORDER BY — 정렬하기
`ORDER BY` 절은 결과를 특정 컬럼 기준으로 정렬한다. **ASC**는 오름차순(기본값), **DESC**는 내림차순이다.

```sql
-- 레벨 내림차순으로 정렬 (레벨 높은 플레이어가 위에)
SELECT username, level, class
FROM players
ORDER BY level DESC;
```

**실행 결과:**
```
┌─────────────┬───────┬─────────┐
│  username   │ level │  class  │
├─────────────┼───────┼─────────┤
│ IronShield  │    91 │ Paladin │
│ HolyLight   │    88 │ Healer  │
│ DragonSlayer│    75 │ Warrior │
│ ShadowArrow │    62 │ Archer  │
│ DarkMage    │    45 │ Mage    │
└─────────────┴───────┴─────────┘
```

여러 컬럼으로 정렬할 수도 있다. 먼저 지정한 컬럼으로 정렬하고, 값이 같으면 다음 컬럼으로 정렬한다.

```sql
-- 서버 번호 오름차순, 같은 서버면 레벨 내림차순
SELECT username, server_id, level
FROM players
ORDER BY server_id ASC, level DESC;
```

**실행 결과:**
```
┌─────────────┬───────────┬───────┐
│  username   │ server_id │ level │
├─────────────┼───────────┼───────┤
│ DragonSlayer│         1 │    75 │
│ ShadowArrow │         1 │    62 │
│ DarkMage    │         1 │    45 │
│ IronShield  │         2 │    91 │
│ HolyLight   │         2 │    88 │
└─────────────┴───────────┴───────┘
```

### LIMIT — 결과 개수 제한하기

`LIMIT` 절은 반환할 최대 행 수를 제한한다. 전체 데이터가 수백만 건이어도 필요한 만큼만 가져올 수 있어 매우 유용하다.

```sql
-- 레벨 TOP 3 플레이어
SELECT username, level, class
FROM players
ORDER BY level DESC
LIMIT 3;
```

**실행 결과:**
```
┌─────────────┬───────┬─────────┐
│  username   │ level │  class  │
├─────────────┼───────┼─────────┤
│ IronShield  │    91 │ Paladin │
│ HolyLight   │    88 │ Healer  │
│ DragonSlayer│    75 │ Warrior │
└─────────────┴───────┴─────────┘
```

`OFFSET`을 함께 쓰면 페이지네이션(paging)을 구현할 수 있다.

```sql
-- 4~5번째 플레이어 (3개를 건너뛰고 2개 조회)
SELECT username, level
FROM players
ORDER BY level DESC
LIMIT 2 OFFSET 3;
```

**실행 결과:**
```
┌─────────────┬───────┐
│  username   │ level │
├─────────────┼───────┤
│ ShadowArrow │    62 │
│ DarkMage    │    45 │
└─────────────┴───────┘
```

> **DuckDB 팁**: `LIMIT`와 `ORDER BY`를 함께 쓸 때 DuckDB는 전체 정렬을 하지 않고 **TopK 알고리즘**을 사용해 최적화한다. 수백만 건의 전투 로그에서 상위 10건만 가져오는 경우 매우 빠르게 동작한다.

---

## 4.3 집계 함수 (COUNT, SUM, AVG, MIN, MAX)

### 집계 함수란
**집계 함수(aggregate function)**는 여러 행의 값을 하나의 값으로 요약하는 함수다. 게임 데이터 분석에서는 "총 피해량은 얼마인가?", "평균 레벨은 얼마인가?", "플레이어가 몇 명인가?" 같은 질문에 답할 때 쓴다.

주요 집계 함수 다섯 가지를 살펴보자.

### COUNT — 행 개수 세기

```sql
-- 전체 플레이어 수
SELECT COUNT(*) AS total_players FROM players;
```

**실행 결과:**
```
┌───────────────┐
│ total_players │
│     int64     │
├───────────────┤
│             5 │
└───────────────┘
```

`COUNT(컬럼명)`은 해당 컬럼이 NULL이 아닌 행만 센다. `COUNT(*)`는 NULL 여부에 관계없이 전체 행을 센다.

```sql
-- 퀘스트를 완료한 건수 (completed_at이 NULL이 아닌 것만)
SELECT COUNT(completed_at) AS completed_quest_count
FROM quests;
```

**실행 결과:**
```
┌───────────────────────┐
│ completed_quest_count │
│         int64         │
├───────────────────────┤
│                     3 │
└───────────────────────┘
```

`COUNT(DISTINCT 컬럼)` 으로 중복을 제거한 고유값 개수를 셀 수 있다.

```sql
-- 전투에 참여한 고유 공격자 수
SELECT COUNT(DISTINCT attacker_id) AS unique_attackers
FROM battle_logs;
```

**실행 결과:**
```
┌──────────────────┐
│ unique_attackers │
│      int64       │
├──────────────────┤
│                4 │
└──────────────────┘
```

### SUM — 합산하기

```sql
-- 전체 전투에서 발생한 총 피해량
SELECT SUM(damage) AS total_damage
FROM battle_logs;
```

**실행 결과:**
```
┌──────────────┐
│ total_damage │
│    int128    │
├──────────────┤
│         1770 │
└──────────────┘
```

```sql
-- 플레이어 1번이 가한 총 피해량
SELECT SUM(damage) AS player1_total_damage
FROM battle_logs
WHERE attacker_id = 1;
```

**실행 결과:**
```
┌──────────────────────┐
│ player1_total_damage │
│        int128        │
├──────────────────────┤
│                 1070 │
└──────────────────────┘
```

### AVG — 평균 구하기

```sql
-- 전체 플레이어의 평균 레벨
SELECT AVG(level) AS avg_level
FROM players;
```

**실행 결과:**
```
┌───────────┐
│ avg_level │
│  double   │
├───────────┤
│      72.2 │
└───────────┘
```

```sql
-- 전투 로그의 평균 피해량
SELECT ROUND(AVG(damage), 1) AS avg_damage
FROM battle_logs;
```

**실행 결과:**
```
┌────────────┐
│ avg_damage │
│   double   │
├────────────┤
│      252.9 │
└────────────┘
```

### MIN / MAX — 최솟값, 최댓값 구하기

```sql
-- 가장 낮은 레벨과 가장 높은 레벨
SELECT
    MIN(level) AS min_level,
    MAX(level) AS max_level
FROM players;
```

**실행 결과:**
```
┌───────────┬───────────┐
│ min_level │ max_level │
│   int32   │   int32   │
├───────────┼───────────┤
│        45 │        91 │
└───────────┴───────────┘
```

```sql
-- 한 번의 공격에서 가장 큰 피해와 가장 작은 피해
SELECT
    MIN(damage) AS min_hit,
    MAX(damage) AS max_hit,
    AVG(damage) AS avg_hit
FROM battle_logs;
```

**실행 결과:**
```
┌─────────┬─────────┬────────────────────┐
│ min_hit │ max_hit │      avg_hit       │
│  int32  │  int32  │       double       │
├─────────┼─────────┼────────────────────┤
│      80 │     620 │ 252.857142857142.. │
└─────────┴─────────┴────────────────────┘
```

> **알아두기**: DuckDB에서 `SUM`의 결과 타입은 오버플로우 방지를 위해 `INTEGER`의 경우 `HUGEINT`(128비트)로 자동 승격된다. 대용량 게임 로그를 분석할 때 수십억 단위 합산도 안전하게 처리된다.

---

## 4.4 GROUP BY와 HAVING

### GROUP BY — 그룹별로 집계하기
집계 함수만 사용하면 전체 데이터를 하나의 숫자로 요약한다. 하지만 실무에서는 "직업별 평균 레벨", "서버별 플레이어 수"처럼 **그룹별로** 나눠서 집계하고 싶은 경우가 많다.

`GROUP BY`는 지정한 컬럼의 값이 같은 행들을 하나의 그룹으로 묶어 집계한다.

```sql
-- 직업(class)별 플레이어 수와 평균 레벨
SELECT
    class,
    COUNT(*)           AS player_count,
    ROUND(AVG(level), 1) AS avg_level,
    MAX(level)         AS max_level,
    MIN(level)         AS min_level
FROM players
GROUP BY class
ORDER BY avg_level DESC;
```

**실행 결과:**
```
┌─────────┬──────────────┬───────────┬───────────┬───────────┐
│  class  │ player_count │ avg_level │ max_level │ min_level │
│ varchar │    int64     │  double   │   int32   │   int32   │
├─────────┼──────────────┼───────────┼───────────┼───────────┤
│ Paladin │            1 │      91.0 │        91 │        91 │
│ Healer  │            1 │      88.0 │        88 │        88 │
│ Warrior │            1 │      75.0 │        75 │        75 │
│ Archer  │            1 │      62.0 │        62 │        62 │
│ Mage    │            1 │      45.0 │        45 │        45 │
└─────────┴──────────────┴───────────┴───────────┴───────────┘
```

```sql
-- 서버별 플레이어 수와 총 플레이 시간
SELECT
    server_id,
    COUNT(*)                         AS player_count,
    ROUND(SUM(total_playtime_hours)) AS total_playtime,
    ROUND(AVG(total_playtime_hours)) AS avg_playtime
FROM players
GROUP BY server_id
ORDER BY server_id;
```

**실행 결과:**
```
┌───────────┬──────────────┬────────────────┬──────────────┐
│ server_id │ player_count │ total_playtime │ avg_playtime │
│   int32   │    int64     │    double      │    double    │
├───────────┼──────────────┼────────────────┼──────────────┤
│         1 │            3 │            634 │          211 │
│         2 │            2 │           1152 │          576 │
└───────────┴──────────────┴────────────────┴──────────────┘
```

```sql
-- 공격자(attacker_id)별 총 피해량, 평균 피해량, 크리티컬 횟수
SELECT
    attacker_id,
    COUNT(*)           AS attack_count,
    SUM(damage)        AS total_damage,
    ROUND(AVG(damage)) AS avg_damage,
    SUM(CASE WHEN is_critical THEN 1 ELSE 0 END) AS critical_count
FROM battle_logs
GROUP BY attacker_id
ORDER BY total_damage DESC;
```

**실행 결과:**
```
┌─────────────┬──────────────┬──────────────┬────────────┬────────────────┐
│ attacker_id │ attack_count │ total_damage │ avg_damage │ critical_count │
│    int32    │    int64     │    int128    │   double   │     int128     │
├─────────────┼──────────────┼──────────────┼────────────┼────────────────┤
│           1 │            3 │         1070 │        357 │              1 │
│           4 │            1 │          310 │        310 │              1 │
│           3 │            1 │          140 │        140 │              0 │
│           5 │            1 │          170 │        170 │              0 │
│           2 │            1 │           80 │         80 │              0 │
└─────────────┴──────────────┴──────────────┴────────────┴────────────────┘
```

> **주의**: `GROUP BY`를 사용할 때 `SELECT` 절에는 `GROUP BY`에 포함된 컬럼 또는 집계 함수만 올 수 있다. `GROUP BY class`를 썼는데 `SELECT`에 `username`을 넣으면 오류가 발생한다.

### HAVING — 그룹 결과 필터링하기
`WHERE`는 행을 집계하기 **전에** 필터링하지만, `HAVING`은 집계된 결과에 **조건을 적용**한다.

예를 들어 "전투 횟수가 1회를 초과하는 공격자만 보고 싶다"는 요구는 `COUNT(*)` 결과에 조건을 거는 것이므로 `HAVING`을 사용해야 한다.

```sql
-- 공격 횟수가 2회 이상인 플레이어만 조회
SELECT
    attacker_id,
    COUNT(*) AS attack_count,
    SUM(damage) AS total_damage
FROM battle_logs
GROUP BY attacker_id
HAVING COUNT(*) >= 2
ORDER BY total_damage DESC;
```

**실행 결과:**
```
┌─────────────┬──────────────┬──────────────┐
│ attacker_id │ attack_count │ total_damage │
│    int32    │    int64     │    int128    │
├─────────────┼──────────────┼──────────────┤
│           1 │            3 │         1070 │
└─────────────┴──────────────┴──────────────┘
```

```sql
-- 퀘스트 완료 수가 2건 이상인 플레이어
SELECT
    player_id,
    COUNT(*) AS completed_quests
FROM quests
WHERE status = 'completed'
GROUP BY player_id
HAVING COUNT(*) >= 2;
```

**실행 결과:**
```
┌───────────┬──────────────────┐
│ player_id │ completed_quests │
│   int32   │      int64       │
├───────────┼──────────────────┤
│         1 │                2 │
└───────────┴──────────────────┘
```

`WHERE`와 `HAVING`을 함께 사용해 더욱 정밀하게 필터링할 수 있다.

```sql
-- 서버 1의 플레이어 중, 평균 피해량이 200 이상인 공격자
SELECT
    b.attacker_id,
    p.username,
    COUNT(*)           AS attack_count,
    ROUND(AVG(damage)) AS avg_damage
FROM battle_logs b
JOIN players p ON b.attacker_id = p.player_id
WHERE p.server_id = 1
GROUP BY b.attacker_id, p.username
HAVING AVG(damage) >= 200
ORDER BY avg_damage DESC;
```

**실행 결과:**
```
┌─────────────┬─────────────┬──────────────┬────────────┐
│ attacker_id │  username   │ attack_count │ avg_damage │
│    int32    │   varchar   │    int64     │   double   │
├─────────────┼─────────────┼──────────────┼────────────┤
│           1 │ DragonSlayer│            3 │        357 │
└─────────────┴─────────────┴──────────────┴────────────┘
```

> **SQL 실행 순서 기억하기**: SQL은 작성 순서대로 실행되지 않는다. 실제 실행 순서는 `FROM → WHERE → GROUP BY → HAVING → SELECT → ORDER BY → LIMIT` 이다. 이 순서를 알면 어떤 절에서 어떤 필터를 써야 하는지 헷갈리지 않는다.

---

## 4.5 JOIN — INNER / LEFT / RIGHT / FULL

### JOIN이란
지금까지는 단일 테이블에서만 데이터를 조회했다. 하지만 실무에서는 여러 테이블의 데이터를 연결해서 봐야 하는 경우가 훨씬 많다. 예를 들어 전투 로그에는 `attacker_id`만 있고 플레이어 이름은 없기 때문에, 이름을 알려면 `players` 테이블과 연결해야 한다.

**JOIN**은 두 테이블을 공통 컬럼(키)을 기준으로 연결하는 연산이다.

### INNER JOIN — 양쪽에 모두 있는 행만

`INNER JOIN`은 두 테이블에서 **조건을 만족하는 행만** 결과에 포함한다. 한쪽 테이블에만 있는 행은 버려진다.

```sql
-- 전투 로그에 공격자 이름 붙이기
SELECT
    b.log_id,
    p.username    AS attacker_name,
    b.damage,
    b.skill_name,
    b.is_critical,
    b.logged_at
FROM battle_logs b
INNER JOIN players p ON b.attacker_id = p.player_id
ORDER BY b.log_id;
```

**실행 결과:**
```
┌────────┬─────────────┬────────┬────────────────┬─────────────┬─────────────────────┐
│ log_id │ attacker_nm │ damage │   skill_name   │ is_critical │      logged_at      │
├────────┼─────────────┼────────┼────────────────┼─────────────┼─────────────────────┤
│      1 │ DragonSlayer│    250 │ Dragon Strike  │    false    │ 2025-03-01 10:05:00 │
│      2 │ ShadowArrow │     80 │ Arrow Shot     │    false    │ 2025-03-01 10:05:02 │
│      3 │ DragonSlayer│    620 │ Dragon Strike  │    true     │ 2025-03-01 10:05:05 │
│      4 │ HolyLight   │    140 │ Holy Beam      │    false    │ 2025-03-01 11:00:00 │
│      5 │ DarkMage    │    310 │ Dark Explosion │    true     │ 2025-03-01 11:00:03 │
│      6 │ DragonSlayer│    200 │ Sword Slash    │    false    │ 2025-03-01 12:30:00 │
│      7 │ IronShield  │    170 │ Shield Bash    │    false    │ 2025-03-01 12:31:00 │
└────────┴─────────────┴────────┴────────────────┴─────────────┴─────────────────────┘
```

세 개의 테이블을 연속으로 JOIN할 수도 있다.

```sql
-- 전투 로그에 공격자와 피격자 이름 모두 붙이기
SELECT
    b.log_id,
    att.username  AS attacker_name,
    def.username  AS defender_name,
    b.damage,
    b.skill_name,
    b.is_critical
FROM battle_logs b
INNER JOIN players att ON b.attacker_id = att.player_id
INNER JOIN players def ON b.defender_id = def.player_id
ORDER BY b.log_id;
```

**실행 결과:**
```
┌────────┬─────────────┬─────────────┬────────┬────────────────┬─────────────┐
│ log_id │ attacker_nm │ defender_nm │ damage │   skill_name   │ is_critical │
├────────┼─────────────┼─────────────┼────────┼────────────────┼─────────────┤
│      1 │ DragonSlayer│ ShadowArrow │    250 │ Dragon Strike  │    false    │
│      2 │ ShadowArrow │ DragonSlayer│     80 │ Arrow Shot     │    false    │
│      3 │ DragonSlayer│ ShadowArrow │    620 │ Dragon Strike  │    true     │
│      4 │ HolyLight   │ DarkMage    │    140 │ Holy Beam      │    false    │
│      5 │ DarkMage    │ HolyLight   │    310 │ Dark Explosion │    true     │
│      6 │ DragonSlayer│ HolyLight   │    200 │ Sword Slash    │    false    │
│      7 │ IronShield  │ DarkMage    │    170 │ Shield Bash    │    false    │
└────────┴─────────────┴─────────────┴────────┴────────────────┴─────────────┘
```

### LEFT JOIN — 왼쪽 테이블 기준
`LEFT JOIN`은 왼쪽 테이블의 **모든 행**을 결과에 포함한다. 오른쪽 테이블에 매칭되는 행이 없으면 NULL로 채운다.

"아직 전투 기록이 없는 플레이어도 포함해서 보고 싶을 때" 유용하다.

```sql
-- 모든 플레이어와 그들의 전투 기록 (전투 기록 없어도 포함)
SELECT
    p.username,
    p.class,
    COUNT(b.log_id) AS battle_count
FROM players p
LEFT JOIN battle_logs b ON p.player_id = b.attacker_id
GROUP BY p.player_id, p.username, p.class
ORDER BY battle_count DESC;
```

**실행 결과:**
```
┌─────────────┬─────────┬──────────────┐
│  username   │  class  │ battle_count │
├─────────────┼─────────┼──────────────┤
│ DragonSlayer│ Warrior │            3 │
│ ShadowArrow │ Archer  │            1 │
│ HolyLight   │ Healer  │            1 │
│ DarkMage    │ Mage    │            1 │
│ IronShield  │ Paladin │            1 │
└─────────────┴─────────┴──────────────┘
```

```sql
-- 퀘스트가 없는 플레이어도 포함해서 조회
SELECT
    p.username,
    q.quest_name,
    q.status
FROM players p
LEFT JOIN quests q ON p.player_id = q.player_id
ORDER BY p.player_id, q.quest_id;
```

### RIGHT JOIN — 오른쪽 테이블 기준
`RIGHT JOIN`은 `LEFT JOIN`의 반대다. 오른쪽 테이블의 **모든 행**을 포함하고, 왼쪽에 매칭이 없으면 NULL로 채운다.

```sql
-- 모든 퀘스트와 그 퀘스트를 수행한 플레이어 정보
-- (플레이어 테이블에 없는 player_id가 있는 퀘스트도 포함)
SELECT
    p.username,
    q.quest_name,
    q.status
FROM players p
RIGHT JOIN quests q ON p.player_id = q.player_id
ORDER BY q.quest_id;
```

> **실용 팁**: 실무에서는 `LEFT JOIN`을 훨씬 더 자주 쓴다. `RIGHT JOIN`은 테이블 순서를 바꿔 `LEFT JOIN`으로 표현할 수 있기 때문에, 가독성을 위해 대부분 `LEFT JOIN`을 사용한다.

### FULL OUTER JOIN — 양쪽 모두 포함
`FULL OUTER JOIN`(또는 `FULL JOIN`)은 양쪽 테이블의 **모든 행**을 결과에 포함한다. 한쪽에만 있는 행은 상대편 컬럼을 NULL로 채운다.

```sql
-- 양쪽에서 매칭되지 않는 경우를 포함한 전체 조인
SELECT
    p.username,
    q.quest_name,
    q.status
FROM players p
FULL OUTER JOIN quests q ON p.player_id = q.player_id
ORDER BY p.player_id NULLS LAST;
```

### JOIN 유형 요약

| JOIN 유형 | 결과 포함 범위 |
|-----------|--------------|
| `INNER JOIN` | 양쪽 테이블에서 조건을 만족하는 행만 |
| `LEFT JOIN` | 왼쪽 테이블 전체 + 오른쪽 매칭 (없으면 NULL) |
| `RIGHT JOIN` | 오른쪽 테이블 전체 + 왼쪽 매칭 (없으면 NULL) |
| `FULL OUTER JOIN` | 양쪽 테이블 전체 (매칭 없으면 서로 NULL) |
| `CROSS JOIN` | 양쪽 테이블의 모든 조합 (카테시안 곱) |

---

## 4.6 서브쿼리와 CTE (WITH 절)

### 서브쿼리 — 쿼리 안의 쿼리
**서브쿼리(subquery)**는 다른 쿼리의 내부에 중첩된 쿼리다. `SELECT`, `FROM`, `WHERE` 절에 모두 사용할 수 있다.

**WHERE 절의 서브쿼리:**
```sql
-- 평균 레벨보다 높은 플레이어 조회
SELECT username, level, class
FROM players
WHERE level > (SELECT AVG(level) FROM players)
ORDER BY level DESC;
```

**실행 결과:**
```
┌─────────────┬───────┬─────────┐
│  username   │ level │  class  │
├─────────────┼───────┼─────────┤
│ IronShield  │    91 │ Paladin │
│ HolyLight   │    88 │ Healer  │
│ DragonSlayer│    75 │ Warrior │
└─────────────┴───────┴─────────┘
(평균 레벨 72.2보다 높은 플레이어)
```

**IN 절의 서브쿼리:**
```sql
-- 전투 기록이 있는 플레이어의 정보 조회
SELECT username, level, class
FROM players
WHERE player_id IN (
    SELECT DISTINCT attacker_id FROM battle_logs

);
```

**FROM 절의 서브쿼리 (인라인 뷰):**
```sql
-- 공격자별 총 피해량을 구한 다음, 총 피해량이 200 이상인 것만 필터링
SELECT *
FROM (
    SELECT
        attacker_id,
        SUM(damage)  AS total_damage,
        COUNT(*)     AS attack_count
    FROM battle_logs
    GROUP BY attacker_id
) sub
WHERE total_damage >= 200
ORDER BY total_damage DESC;
```

**실행 결과:**
```
┌─────────────┬──────────────┬──────────────┐
│ attacker_id │ total_damage │ attack_count │
├─────────────┼──────────────┼──────────────┤
│           1 │         1070 │            3 │
│           4 │          310 │            1 │
│           5 │          170 │            1 │
└─────────────┴──────────────┴──────────────l'┘
```

### CTE — WITH 절로 가독성 높이기
서브쿼리가 복잡해지면 코드를 읽기 어려워진다. **CTE(Common Table Expression, 공통 테이블 표현식)**는 `WITH` 절을 사용해 쿼리 상단에 임시 테이블을 이름 붙여 정의하는 방법이다. 쿼리가 훨씬 읽기 쉬워진다.

```sql
-- CTE 기본 구조
WITH cte_name AS (
    -- 여기에 서브쿼리 작성
    SELECT ...
)
SELECT * FROM cte_name;
```

서브쿼리로 작성한 앞의 예제를 CTE로 바꿔보자.

```sql
-- CTE를 사용해 공격자 통계 분석
WITH attacker_stats AS (
    SELECT
        attacker_id,
        SUM(damage)  AS total_damage,
        COUNT(*)     AS attack_count,
        MAX(damage)  AS max_single_damage
    FROM battle_logs
    GROUP BY attacker_id
)
SELECT
    p.username,
    a.total_damage,
    a.attack_count,
    a.max_single_damage
FROM attacker_stats a
JOIN players p ON a.attacker_id = p.player_id
WHERE a.total_damage >= 200
ORDER BY a.total_damage DESC;
```

**실행 결과:**
```
┌─────────────┬──────────────┬──────────────┬───────────────────┐
│  username   │ total_damage │ attack_count │ max_single_damage │
├─────────────┼──────────────┼──────────────┼───────────────────┤
│ DragonSlayer│         1070 │            3 │               620 │
│ DarkMage    │          310 │            1 │               310 │
│ IronShield  │          170 │            1 │               170 │
└─────────────┴──────────────┴──────────────┴───────────────────┘
```

CTE는 여러 개를 연속으로 정의할 수 있다. 각 CTE는 이전에 정의된 CTE를 참조할 수 있어 단계적으로 분석을 쌓아 올릴 수 있다.

```sql
-- 다중 CTE: 단계별 분석
WITH
-- 1단계: 공격자별 기본 통계
attacker_stats AS (
    SELECT
        attacker_id,
        COUNT(*)           AS attack_count,
        SUM(damage)        AS total_damage,
        AVG(damage)        AS avg_damage,
        SUM(CASE WHEN is_critical THEN 1 ELSE 0 END) AS critical_count
    FROM battle_logs
    GROUP BY attacker_id
),
-- 2단계: 크리티컬 비율 계산
attacker_with_crit_rate AS (
    SELECT
        attacker_id,
        attack_count,
        total_damage,
        ROUND(avg_damage, 1) AS avg_damage,
        critical_count,
        ROUND(100.0 * critical_count / attack_count, 1) AS crit_rate_pct
    FROM attacker_stats
),
-- 3단계: 플레이어 이름과 합치기
final AS (
    SELECT
        p.username,
        a.*
    FROM attacker_with_crit_rate a
    JOIN players p ON a.attacker_id = p.player_id
)
SELECT * FROM final
ORDER BY total_damage DESC;
```

**실행 결과:**
```
┌─────────────┬─────────────┬──────────────┬──────────────┬────────────┬────────────────┬──────────────┐
│  username   │ attacker_id │ attack_count │ total_damage │ avg_damage │ critical_count │ crit_rate_pct│
├─────────────┼─────────────┼──────────────┼──────────────┼────────────┼────────────────┼──────────────┤
│ DragonSlayer│           1 │            3 │         1070 │      356.7 │              1 │         33.3 │
│ DarkMage    │           4 │            1 │          310 │      310.0 │              1 │        100.0 │
│ IronShield  │           5 │            1 │          170 │      170.0 │              0 │          0.0 │
│ HolyLight   │           3 │            1 │          140 │      140.0 │              0 │          0.0 │
│ ShadowArrow │           2 │            1 │           80 │       80.0 │              0 │          0.0 │
└─────────────┴─────────────┴──────────────┴──────────────┴────────────┴────────────────┴──────────────┘
```

> **CTE vs 서브쿼리**: CTE는 서브쿼리에 이름을 붙여 여러 번 참조할 수 있다. 같은 서브쿼리를 두 번 이상 쓸 때 CTE를 사용하면 코드 중복을 줄이고 가독성을 높일 수 있다. 복잡한 분석 쿼리는 항상 CTE로 단계를 나눠 작성하는 것을 권장한다.

---

## 4.7 DuckDB 특유의 편리한 SQL 확장 문법
DuckDB는 표준 SQL을 완벽히 지원하면서도, 데이터 분석 작업을 더 편리하게 만드는 다양한 확장 문법을 제공한다. 이 절에서는 실무에서 자주 쓰는 DuckDB만의 특징적인 문법들을 소개한다.

### FROM 절 생략 — 간단한 계산
DuckDB에서는 `FROM` 절 없이 `SELECT`만으로 간단한 계산이 가능하다.

```sql
-- 기본 산술 연산
SELECT 1 + 1;
-- 결과: 2

SELECT 100 * 3.14159 AS circle_area;
-- 결과: 314.159

-- 현재 날짜와 시간
SELECT current_date AS today, current_timestamp AS now;

-- 문자열 처리
SELECT
    upper('dragonslayer') AS upper_name,
    length('DuckDB is awesome') AS str_length;
```

### SELECT * EXCLUDE — 특정 컬럼 제외하기
컬럼이 많은 테이블에서 한두 개만 빼고 다 가져오고 싶을 때 유용하다.

```sql
-- created_at, total_playtime_hours 컬럼을 제외한 모든 컬럼 조회
SELECT * EXCLUDE (created_at, total_playtime_hours)
FROM players;
```

**실행 결과:**
```
┌───────────┬─────────────┬───────┬─────────┬───────────┐
│ player_id │  username   │ level │  class  │ server_id │
├───────────┼─────────────┼───────┼─────────┼───────────┤
│         1 │ DragonSlayer│    75 │ Warrior │         1 │
│         2 │ ShadowArrow │    62 │ Archer  │         1 │
│         3 │ HolyLight   │    88 │ Healer  │         2 │
│         4 │ DarkMage    │    45 │ Mage    │         1 │
│         5 │ IronShield  │    91 │ Paladin │         2 │
└───────────┴─────────────┴───────┴─────────┴───────────┘
```

### SELECT * REPLACE — 특정 컬럼 값을 변환해서 가져오기
모든 컬럼을 가져오되, 특정 컬럼의 값만 변환하고 싶을 때 사용한다.

```sql
-- level 컬럼만 10% 보정해서 보기 (나머지 컬럼은 원본 그대로)
SELECT * REPLACE (level * 1.1 AS level)
FROM players;
```

```sql
-- 아이템 가격을 달러 표시로 변환 (price를 달러로 가정)
SELECT * REPLACE (ROUND(price / 1300.0, 2) AS price)
FROM items;
```

### COLUMNS() 표현식 — 여러 컬럼에 함수 일괄 적용
`COLUMNS()`는 정규식 패턴에 매칭되는 여러 컬럼에 동시에 함수를 적용한다.

```sql
-- items 테이블에서 _power로 끝나는 컬럼들의 최대값 한 번에 구하기
SELECT MAX(COLUMNS('.*_power'))
FROM items;
```

**실행 결과:**
```
┌────────────────────────┬──────────────────────────┐
│ max(items.attack_power)│ max(items.defense_power) │
├────────────────────────┼──────────────────────────┤
│                    320 │                      120 │
└────────────────────────┴──────────────────────────┘
```

```sql
-- id로 끝나는 컬럼들만 선택
SELECT COLUMNS('.*_id')
FROM battle_logs
LIMIT 3;
```

### LIST, STRUCT, MAP — 복잡한 데이터 타입

DuckDB는 배열(`LIST`), 구조체(`STRUCT`), 맵(`MAP`) 같은 복잡한 타입을 컬럼에 저장할 수 있다. 게임 아이템의 속성이나 스탯 묶음을 하나의 컬럼에 저장하는 데 활용할 수 있다.

```sql
-- STRUCT 타입으로 스탯 묶음 저장
CREATE TABLE player_stats (
    player_id  INTEGER,
    username   VARCHAR,
    stats      STRUCT(hp INTEGER, mp INTEGER, str INTEGER, agi INTEGER, int INTEGER)
);

INSERT INTO player_stats VALUES
    (1, 'DragonSlayer', {'hp': 2500, 'mp': 800,  'str': 180, 'agi': 120, 'int': 60}),
    (2, 'ShadowArrow',  {'hp': 1800, 'mp': 900,  'str': 130, 'agi': 200, 'int': 80}),
    (3, 'HolyLight',    {'hp': 2000, 'mp': 1500, 'str': 90,  'agi': 100, 'int': 210});

-- STRUCT 내부 필드 접근 (점 표기법)
SELECT
    username,
    stats.hp   AS hp,
    stats.str  AS strength,
    stats.agi  AS agility
FROM player_stats
ORDER BY stats.hp DESC;
```

**실행 결과:**
```
┌─────────────┬──────┬──────────┬─────────┐
│  username   │  hp  │ strength │ agility │
├─────────────┼──────┼──────────┼─────────┤
│ DragonSlayer│ 2500 │      180 │     120 │
│ HolyLight   │ 2000 │       90 │     100 │
│ ShadowArrow │ 1800 │      130 │     200 │
└─────────────┴──────┴──────────┴─────────┘
```

```sql
-- LIST 타입으로 아이템 인벤토리 저장
CREATE TABLE inventories (
    player_id  INTEGER,
    items_held INTEGER[]   -- INTEGER의 LIST 타입
);

INSERT INTO inventories VALUES
    (1, [101, 103, 106]),
    (2, [101, 102, 106]),
    (3, [104, 105, 106]);

-- 인벤토리에 특정 아이템이 있는지 확인
SELECT player_id
FROM inventories
WHERE list_contains(items_held, 106);   -- item_id 106(포션)을 가진 플레이어

-- LIST의 길이
SELECT player_id, len(items_held) AS item_count
FROM inventories;
```

### PIVOT — 행을 열로 변환

`PIVOT`은 행 데이터를 열로 변환(피벗)하는 분석용 문법이다. 예를 들어 퀘스트 상태별 집계 결과를 가로로 펼쳐보고 싶을 때 유용하다.

```sql
-- 플레이어별 퀘스트 상태 개수를 피벗
PIVOT quests
ON status
USING COUNT(quest_id)
GROUP BY player_id;
```

**실행 결과:**
```
┌───────────┬───────────┬─────────────┬────────┐
│ player_id │ completed │ in_progress │ failed │
│   int32   │  int64    │    int64    │ int64  │
├───────────┼───────────┼─────────────┼────────┤
│         1 │         2 │           0 │      0 │
│         2 │         0 │           1 │      0 │
│         3 │         1 │           0 │      0 │
│         4 │         0 │           1 │      0 │
│         5 │         0 │           0 │      1 │
└───────────┴───────────┴─────────────┴────────┘
```

### UNPIVOT — 열을 행으로 변환
`UNPIVOT`은 `PIVOT`의 역연산이다. 여러 컬럼에 분산된 값을 하나의 컬럼으로 모은다.

```sql
-- 아이템의 attack_power와 defense_power를 세로로 펼치기
UNPIVOT items
ON attack_power, defense_power
INTO
    NAME stat_type
    VALUE stat_value
WHERE stat_value > 0
ORDER BY item_id, stat_type;
```

**실행 결과:**
```
┌─────────┬───────────────────┬──────────┬───────────┬───────────┬───────┬────────────┐
│ item_id │     item_name     │item_type │   grade   │  stat_type│stat_val│   price   │
├─────────┼───────────────────┼──────────┼───────────┼───────────┼────────┼───────────┤
│     101 │ Iron Sword        │ weapon   │ normal    │attack_pow.│     50 │       500 │
│     102 │ Steel Armor       │ armor    │ normal    │defense_p. │     60 │       800 │
│     103 │ Dragon Fang Bow   │ weapon   │ epic      │attack_pow.│    180 │      8500 │
│     104 │ Holy Shield       │ armor    │ rare      │defense_p. │    120 │      4200 │
│     105 │ Phoenix Staff     │ weapon   │ legendary │attack_pow.│    320 │     25000 │
│     105 │ Phoenix Staff     │ weapon   │ legendary │defense_p. │     30 │     25000 │
└─────────┴───────────────────┴──────────┴───────────┴───────────┴────────┴───────────┘
```

### 람다 함수와 리스트 처리
DuckDB는 리스트(배열) 컬럼을 처리할 때 람다 함수를 사용할 수 있다.

```sql
-- 인벤토리에서 item_id가 100 이상인 아이템만 필터링
SELECT
    player_id,
    list_filter(items_held, x -> x >= 103) AS high_tier_items
FROM inventories;

-- 리스트의 각 값에 10을 더하기
SELECT
    player_id,
    list_transform(items_held, x -> x + 1000) AS shifted_ids
FROM inventories;
```

### 날짜/시간 편의 문법
DuckDB는 날짜와 시간 처리를 위한 편리한 문법도 제공한다.

```sql
-- 날짜 연산 (INTERVAL 사용)
SELECT
    quest_name,
    started_at,
    started_at + INTERVAL '7 days' AS deadline
FROM quests
WHERE status = 'in_progress';

-- 날짜 차이 계산
SELECT
    quest_name,
    datediff('hour', started_at, completed_at) AS duration_hours
FROM quests
WHERE completed_at IS NOT NULL;

-- 날짜 트런케이션 (일 단위로 자르기)
SELECT
    DATE_TRUNC('hour', logged_at) AS battle_hour,
    COUNT(*) AS battles_per_hour
FROM battle_logs
GROUP BY DATE_TRUNC('hour', logged_at)
ORDER BY battle_hour;
```

**실행 결과:**
```
┌─────────────────────┬──────────────────┐
│    battle_hour      │ battles_per_hour │
├─────────────────────┼──────────────────┤
│ 2025-03-01 10:00:00 │                3 │
│ 2025-03-01 11:00:00 │                2 │
│ 2025-03-01 12:00:00 │                2 │
└─────────────────────┴──────────────────┘
```

---

## 이 장의 핵심 정리

이 장에서 배운 내용을 정리해 보자.

1. **테이블 생성과 데이터 삽입**: `CREATE TABLE`로 테이블 스키마를 정의하고, `INSERT INTO ... VALUES`로 데이터를 삽입한다. 컬럼에는 데이터 타입, `NOT NULL`, `DEFAULT`, `PRIMARY KEY` 같은 제약 조건을 붙일 수 있다.

2. **데이터 조회**: `SELECT ... FROM`으로 데이터를 조회한다. `WHERE`는 행 필터링, `ORDER BY`는 정렬, `LIMIT`은 결과 수 제한에 사용한다.

3. **집계 함수**: `COUNT`, `SUM`, `AVG`, `MIN`, `MAX`로 여러 행을 하나의 값으로 요약한다. `GROUP BY`로 그룹별 집계를 하고, `HAVING`으로 집계 결과를 필터링한다.

4. **JOIN**: 두 테이블을 공통 키로 연결한다. `INNER JOIN`(교집합), `LEFT JOIN`(왼쪽 기준), `RIGHT JOIN`(오른쪽 기준), `FULL OUTER JOIN`(합집합)의 차이를 이해해야 한다.

5. **서브쿼리와 CTE**: 쿼리 안에 쿼리를 중첩하는 서브쿼리, 그리고 가독성을 높이는 `WITH` 절(CTE)을 활용해 복잡한 분석 쿼리를 단계적으로 작성한다.

6. **DuckDB 확장 문법**: `SELECT * EXCLUDE`, `SELECT * REPLACE`, `COLUMNS()`, `PIVOT/UNPIVOT`, `LIST/STRUCT/MAP` 타입, 람다 함수 등 DuckDB만의 강력한 기능을 활용하면 분석 쿼리를 더 간결하게 작성할 수 있다.

---

## 다음 장 예고
4장에서는 DuckDB CLI 환경에서 SQL을 직접 실행하며 기본기를 다졌다. `CREATE TABLE`로 테이블을 만들고, 게임 플레이어와 전투 로그 데이터를 넣고 꺼내는 모든 과정을 손으로 익혔다.

**5장에서는 C#(.NET 9) 코드에서 DuckDB를 프로그래밍적으로 제어하는 방법을 배운다.** DuckDB의 공식 C# 라이브러리(`DuckDB.NET`)를 사용해 연결을 열고 닫는 방법, 파라미터화된 쿼리로 안전하게 데이터를 삽입하는 방법, 게임 서버 코드에서 대용량 전투 로그를 효율적으로 처리하는 패턴을 다룬다. SQL 쿼리를 코드로 래핑하면 훨씬 강력한 게임 분석 시스템을 만들 수 있다.    