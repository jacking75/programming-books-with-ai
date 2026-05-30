# 제10장: 성능 최적화
9장에서는 윈도우 함수, 고급 집계, 통계 분석 등 DuckDB가 제공하는 강력한 분석 기능을 살펴봤다. 이제 그 기능들을 실전에서 더 빠르게, 더 효율적으로 동작하게 만드는 방법을 배울 차례다.

아무리 뛰어난 분석 기능이 있어도, 쿼리가 느리거나 메모리가 부족해 서버가 멈춘다면 실전에서 쓸 수 없다. 이 장에서는 DuckDB의 성능을 한계까지 끌어올리는 다양한 기법을 다룬다. 특히 온라인 게임 서버처럼 대량의 로그가 쏟아지는 환경에서 어떻게 설정을 튜닝하고 코드를 작성해야 하는지를 실제 수치와 함께 보여준다.

---

## 10.1 EXPLAIN과 EXPLAIN ANALYZE로 쿼리 계획 읽기
쿼리가 느릴 때 가장 먼저 해야 할 일은 DuckDB가 그 쿼리를 어떻게 실행하는지 확인하는 것이다. 마치 의사가 환자를 진료하기 전에 X-레이부터 찍듯, DuckDB는 `EXPLAIN`과 `EXPLAIN ANALYZE` 명령으로 쿼리의 실행 계획을 보여준다.

### EXPLAIN: 실행 계획 미리보기
`EXPLAIN`은 쿼리를 실제로 실행하지 않고 DuckDB가 어떤 순서로 어떤 작업을 수행할 계획인지 보여준다.

```sql
EXPLAIN
SELECT
    user_id,
    COUNT(*) AS login_count,
    MAX(login_time) AS last_login
FROM game_logs
WHERE log_type = 'LOGIN'
  AND login_time >= '2024-01-01'
GROUP BY user_id
ORDER BY login_count DESC
LIMIT 10;
```

실행하면 다음과 같은 출력이 나온다.

```
┌─────────────────────────────┐
│         QUERY PLAN          │
└─────────────────────────────┘
┌─────────────────────────────┐
│         PROJECTION          │
│   user_id, login_count,     │
│   last_login                │
└──────────────┬──────────────┘
               │
┌──────────────┴──────────────┐
│         TOP-N HEAP          │
│       TOP 10, ORDER BY      │
│       login_count DESC      │
└──────────────┬──────────────┘
               │
┌──────────────┴──────────────┐
│       HASH_GROUP_BY         │
│   groups: user_id           │
│   agg: count_star(),        │
│        max(login_time)      │
└──────────────┬──────────────┘
               │
┌──────────────┴──────────────┐
│         TABLE_SCAN          │
│       game_logs             │
│   Filters: log_type='LOGIN' │
│     AND login_time >= ...   │
│   ~1,200,000 rows           │
└─────────────────────────────┘
```

계획 트리는 아래에서 위로 읽는다. 가장 아래의 `TABLE_SCAN`이 가장 먼저 실행되고, 결과가 위로 올라가며 `HASH_GROUP_BY` → `TOP-N HEAP` → `PROJECTION` 순서로 처리된다.

주요 노드 유형을 알아두면 계획을 빠르게 해석할 수 있다.

| 노드 유형 | 설명 |
|-----------|------|
| `TABLE_SCAN` | 테이블을 처음부터 끝까지 읽는 풀 스캔 |
| `SEQ_SCAN` | 순차 읽기 (TABLE_SCAN과 유사) |
| `FILTER` | 조건을 적용해 행을 걸러냄 |
| `HASH_JOIN` | 해시 테이블을 이용한 조인 |
| `HASH_GROUP_BY` | 해시 기반 그룹핑 및 집계 |
| `PROJECTION` | 필요한 컬럼만 선택 |
| `TOP-N HEAP` | ORDER BY + LIMIT 최적화 |
| `CROSS_PRODUCT` | 카테시안 곱 (주의 필요) |

`TABLE_SCAN`의 `~1,200,000 rows`처럼 예상 행 수가 표시되는데, 이 숫자가 실제와 크게 다르면 통계가 오래됐다는 신호다. 이럴 때는 `ANALYZE` 명령으로 통계를 갱신해야 한다.

### EXPLAIN ANALYZE: 실제 실행 시간 측정

`EXPLAIN ANALYZE`는 쿼리를 실제로 실행하면서 각 단계별 소요 시간과 처리한 행 수를 측정한다. 이것이 진짜 성능 분석 도구다.

```sql
EXPLAIN ANALYZE
SELECT
    user_id,
    COUNT(*) AS login_count,
    MAX(login_time) AS last_login
FROM game_logs
WHERE log_type = 'LOGIN'
  AND login_time >= '2024-01-01'
GROUP BY user_id
ORDER BY login_count DESC
LIMIT 10;
```

출력 예시:

```
┌─────────────────────────────────────────────────────┐
│                     QUERY PLAN                      │
└─────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────┐
│         PROJECTION  (Time: 0.001ms, Rows: 10)       │
└──────────────────────────┬──────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────┐
│   TOP-N HEAP (Time: 2.3ms, Rows: 10 / 48,291)       │
└──────────────────────────┬──────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────┐
│  HASH_GROUP_BY (Time: 145.2ms, Rows: 48,291)        │
└──────────────────────────┬──────────────────────────┘
                           │
┌──────────────────────────┴──────────────────────────┐
│  TABLE_SCAN game_logs                               │
│  (Time: 823.4ms, Rows: 1,187,432 / 5,000,000)      │
└─────────────────────────────────────────────────────┘

Total Time: 971.0ms
```

각 노드에서 `Time`은 해당 단계의 순수 처리 시간이고, `Rows`는 출력한 행 수(또는 `입력 / 출력` 형식)다.

위 결과를 분석하면:
- `TABLE_SCAN`에서 500만 행 중 약 119만 행(약 24%)이 필터를 통과했다.
- 전체 시간 971ms 중 스캔에 823ms(85%)가 소요됐다. **병목은 TABLE_SCAN에 있다.**
- 이 경우 Parquet 파티셔닝이나 컬럼 프루닝으로 스캔 비용을 줄이는 것이 효과적이다.

### 비효율적인 쿼리 패턴 발견하기

`EXPLAIN ANALYZE`를 통해 자주 발견되는 비효율 패턴들이 있다.

**패턴 1: 불필요한 CROSS_PRODUCT**

```sql
-- 나쁜 예: JOIN 조건 누락
EXPLAIN ANALYZE
SELECT a.user_id, b.item_name
FROM user_actions a, item_purchases b
WHERE a.session_id = b.session_id;
```

계획에 `CROSS_PRODUCT`가 보이면 즉시 `JOIN ... ON` 문법으로 바꿔야 한다. 카테시안 곱은 행 수의 제곱에 비례하므로 데이터가 조금만 많아져도 서버가 멈출 수 있다.

**패턴 2: 예상 행 수와 실제 행 수의 큰 괴리**

```
TABLE_SCAN (Estimated: 100, Actual: 4,800,000)
```

예상치가 100인데 실제로 480만 행을 읽었다면 통계가 매우 오래됐다는 의미다. `ANALYZE game_logs;`를 실행해 통계를 갱신하면 DuckDB가 더 나은 실행 계획을 세울 수 있다.

**패턴 3: 대용량 HASH_JOIN**

```
HASH_JOIN (Time: 3,200ms, Build: 8,000,000 rows)
```

조인의 빌드 단계에서 800만 행을 메모리에 올린다면 메모리 부족 위험이 있다. 이럴 때는 조인 전에 필터링을 강화하거나 파티셔닝으로 데이터를 나눠서 처리해야 한다.

---

## 10.2 인덱스 대신 파티셔닝과 통계 활용

DuckDB를 처음 접하는 개발자들이 자주 묻는 질문이 있다. "인덱스는 어떻게 만들어요?" DuckDB에는 전통적인 B-Tree 인덱스가 없다. 처음엔 의아하게 느껴지지만, 이것은 결함이 아니라 DuckDB의 설계 철학이다.

### 컬럼 지향 스토리지와 인덱스

전통적인 RDBMS(MySQL, PostgreSQL 등)는 행 단위로 데이터를 저장한다. 특정 행을 빠르게 찾으려면 B-Tree 인덱스가 필수적이다.

반면 DuckDB는 컬럼 단위로 데이터를 저장한다. `SELECT SUM(damage)` 같은 집계 쿼리를 실행할 때 `damage` 컬럼 데이터만 읽고 나머지 컬럼은 완전히 건너뛸 수 있다. 또한 같은 컬럼의 값들이 메모리에 연속으로 배치되므로 CPU의 SIMD 명령으로 한 번에 여러 값을 처리할 수 있다.

이 구조에서 B-Tree 인덱스를 추가해 봤자, 컬럼 스캔보다 느린 경우가 대부분이다. 특히 분석 쿼리는 대부분 넓은 범위의 데이터를 집계하는데, 이런 용도에서는 풀 스캔이 인덱스 스캔보다 빠를 수 있다.

### 파티셔닝으로 스캔 범위 줄이기

인덱스 대신 DuckDB에서 쿼리 속도를 높이는 가장 효과적인 방법은 **파티셔닝**이다. 데이터를 여러 파일로 나눠 저장하면, 쿼리에서 필요한 파일만 읽어 처리할 수 있다.

예를 들어 게임 로그를 날짜별로 파티셔닝하면, `WHERE log_date = '2024-01-15'` 조건이 있는 쿼리는 해당 날짜의 파일만 읽는다. 1년치 데이터가 있어도 하루치 파일만 스캔하면 된다.

```sql
-- 게임 로그를 날짜와 로그 유형으로 파티셔닝하여 Parquet으로 저장
COPY (
    SELECT
        log_date,
        log_type,
        user_id,
        server_id,
        event_data
    FROM game_logs_raw
)
TO 'data/game_logs_partitioned/'
(FORMAT PARQUET, PARTITION_BY (log_date, log_type), OVERWRITE_OR_IGNORE);
```

이렇게 하면 다음과 같은 디렉토리 구조가 만들어진다.

```
data/game_logs_partitioned/
├── log_date=2024-01-01/
│   ├── log_type=LOGIN/
│   │   └── data_0.parquet
│   ├── log_type=LOGOUT/
│   │   └── data_0.parquet
│   └── log_type=PURCHASE/
│       └── data_0.parquet
├── log_date=2024-01-02/
│   ├── log_type=LOGIN/
│   └── ...
└── ...
```

파티셔닝된 데이터는 Hive 파티션 스타일 경로를 자동으로 인식한다.

```sql
-- 특정 날짜와 타입만 읽기 (해당 디렉토리만 스캔)
SELECT COUNT(*), SUM(CAST(json_extract_string(event_data, '$.amount') AS DOUBLE))
FROM read_parquet('data/game_logs_partitioned/**/*.parquet',
                  hive_partitioning = true)
WHERE log_date = '2024-01-15'
  AND log_type = 'PURCHASE';
```

### 파티션 프루닝 효과 확인

`EXPLAIN ANALYZE`로 파티셔닝 효과를 직접 확인할 수 있다.

```sql
-- 파티셔닝 전: 전체 데이터 스캔
EXPLAIN ANALYZE
SELECT COUNT(*) FROM game_logs
WHERE log_date = '2024-01-15';
-- TABLE_SCAN: 365일치 전체 데이터 스캔 → 약 1억 8천만 행

-- 파티셔닝 후: 해당 날짜 파일만 스캔
EXPLAIN ANALYZE
SELECT COUNT(*) FROM read_parquet('data/game_logs_partitioned/**/*.parquet',
                                   hive_partitioning = true)
WHERE log_date = '2024-01-15';
-- PARQUET_SCAN: 1일치 데이터만 스캔 → 약 50만 행
```

파티셔닝 전후 성능 비교:

```
┌─────────────────────────────────────────────────┐
│            파티셔닝 성능 비교 (1일치 조회)         │
├──────────────────────┬──────────────────────────┤
│ 방법                 │ 쿼리 시간                │
├──────────────────────┼──────────────────────────┤
│ 비파티셔닝 CSV       │ 45,200ms (45초)          │
│ 비파티셔닝 Parquet   │ 12,300ms (12초)          │
│ 날짜 파티셔닝 Parquet│    480ms (0.5초)         │
│ 날짜+타입 파티셔닝   │    120ms (0.12초)        │
└──────────────────────┴──────────────────────────┘
  데이터: 1년치 게임 로그, 총 1억 8천만 행
```

날짜+타입 이중 파티셔닝을 적용하면 비파티셔닝 CSV 대비 **약 377배** 빠르다.

### 통계 정보 갱신 (ANALYZE)

DuckDB는 테이블의 컬럼별로 최솟값, 최댓값, 고유값 수, NULL 비율 등의 통계를 관리한다. 이 통계를 기반으로 최적의 실행 계획을 세운다.

대량 데이터를 INSERT하거나 파일을 변경한 후에는 통계를 갱신해야 한다.

```sql
-- 특정 테이블 통계 갱신
ANALYZE game_logs;

-- 전체 테이블 통계 갱신
ANALYZE;

-- 현재 통계 확인 (DuckDB 내부 카탈로그 조회)
SELECT * FROM duckdb_columns()
WHERE table_name = 'game_logs';
```

---

## 10.3 대용량 CSV/JSON 로드 최적화

게임 서버 운영 환경에서는 수십 GB에서 수백 GB에 달하는 로그 파일을 DuckDB로 읽어야 하는 경우가 많다. 파일 로드 시간이 길면 분석 업무 전체가 지연되므로 최적화가 필수다.

### Parquet이 CSV보다 빠른 이유

Parquet 형식은 분석 쿼리를 위해 설계된 컬럼 지향 바이너리 포맷이다. CSV와 비교했을 때 다음과 같은 이점이 있다.

**1. 컬럼 프루닝**: `SELECT user_id, damage FROM logs`처럼 일부 컬럼만 필요할 때, Parquet은 해당 컬럼 데이터만 디스크에서 읽는다. CSV는 모든 컬럼을 읽은 후 필요 없는 컬럼을 버린다.

**2. 내장 압축**: Parquet은 컬럼별로 최적의 압축을 적용한다. 반복 문자열이 많은 `log_type` 같은 컬럼은 90% 이상 압축되기도 한다.

**3. 타입 정보 내장**: CSV는 모든 값을 문자열로 저장하므로 DuckDB가 읽을 때마다 타입을 추론해야 한다. Parquet은 타입 정보가 메타데이터에 저장되어 있어 추론 비용이 없다.

**4. Row Group 스킵**: Parquet은 Row Group마다 각 컬럼의 min/max 통계를 저장한다. 범위 조건 쿼리에서 통계 범위를 벗어난 Row Group은 통째로 건너뛸 수 있다.

```
┌──────────────────────────────────────────────────────┐
│       파일 형식별 로드 성능 비교 (10GB, 5천만 행)      │
├───────────────────────┬──────────────┬───────────────┤
│ 형식                  │ 로드 시간    │ 파일 크기     │
├───────────────────────┼──────────────┼───────────────┤
│ CSV (비압축)          │  95.3초      │  10.2 GB      │
│ CSV.GZ (gzip 압축)    │  72.1초      │   2.1 GB      │
│ Parquet (SNAPPY)      │  12.4초      │   1.3 GB      │
│ Parquet (ZSTD)        │  10.8초      │   0.9 GB      │
└───────────────────────┴──────────────┴───────────────┘
  환경: 8코어 CPU, 32GB RAM, NVMe SSD
```

### 압축 파일 직접 읽기

DuckDB는 `.gz`, `.zst`, `.bz2` 등 압축된 CSV 파일을 별도의 압축 해제 없이 직접 읽을 수 있다.

```sql
-- gzip 압축 CSV 직접 읽기
SELECT COUNT(*) FROM read_csv_auto('logs/game_log_2024-01.csv.gz');

-- 와일드카드로 여러 압축 파일 동시 읽기
SELECT
    log_type,
    COUNT(*) AS event_count
FROM read_csv_auto('logs/game_log_2024-*.csv.gz')
GROUP BY log_type;

-- 여러 Parquet 파일 동시 읽기 (글로브 패턴)
SELECT COUNT(*) FROM read_parquet('logs/parquet/year=2024/month=*/*.parquet');
```

### 스키마 명시로 타입 추론 비용 제거

`read_csv_auto`는 편리하지만 파일의 첫 수천 행을 읽어 타입을 추론하는 비용이 있다. 대용량 파일을 반복적으로 읽는다면 스키마를 명시해서 이 비용을 없애는 것이 좋다.

```sql
-- 타입 추론 없이 스키마를 직접 지정
SELECT *
FROM read_csv(
    'logs/game_log_2024-01.csv',
    columns = {
        'log_time':    'TIMESTAMP',
        'log_type':    'VARCHAR',
        'user_id':     'BIGINT',
        'server_id':   'INTEGER',
        'session_id':  'VARCHAR',
        'event_data':  'VARCHAR'
    },
    header = true,
    delim = ',',
    parallel = true
);
```

`parallel = true`는 파일을 여러 스레드로 나눠서 읽게 한다. 멀티코어 환경에서 대용량 파일을 읽을 때 성능이 크게 향상된다.

### 데이터를 Parquet으로 변환해 두기

한 번 사용하고 버릴 게 아니라면, 처음 로드할 때 Parquet으로 변환해 두는 것이 좋다.

```sql
-- CSV를 읽어서 Parquet으로 저장 (한 번만 실행)
COPY (
    SELECT
        CAST(log_time AS TIMESTAMP) AS log_time,
        log_type,
        CAST(user_id AS BIGINT) AS user_id,
        CAST(server_id AS INTEGER) AS server_id,
        event_data
    FROM read_csv('logs/raw/*.csv', header=true)
)
TO 'logs/processed/game_logs.parquet'
(FORMAT PARQUET, COMPRESSION ZSTD);

-- 이후에는 Parquet 파일을 직접 읽기 (훨씬 빠름)
SELECT * FROM 'logs/processed/game_logs.parquet'
WHERE log_type = 'PURCHASE';
```

### JSON 로드 최적화

게임 로그가 JSON 형식이라면 `read_json_auto`나 `read_json`을 사용한다.

```sql
-- JSON Lines 형식 (줄마다 JSON 객체 하나)
SELECT *
FROM read_json(
    'logs/events.jsonl',
    format = 'newline_delimited',
    columns = {
        'timestamp': 'TIMESTAMP',
        'event':     'VARCHAR',
        'user_id':   'BIGINT',
        'payload':   'JSON'
    }
);
```

JSON은 Parquet에 비해 파싱 비용이 높다. 가능하다면 JSON 로그를 수집 단계에서 Parquet으로 변환하는 ETL 파이프라인을 구축하는 것이 좋다.

---

## 10.4 메모리 설정 튜닝

DuckDB는 기본적으로 시스템 메모리의 80%를 사용한다. 단독으로 분석 작업을 수행할 때는 좋지만, 게임 서버 프로세스와 같은 서버에서 실행할 때는 반드시 메모리 한도를 설정해야 한다.

### memory_limit 설정

```sql
-- 메모리 한도를 4GB로 설정
SET memory_limit = '4GB';

-- 현재 설정 확인
SELECT current_setting('memory_limit');

-- 다른 단위 표기 방법
SET memory_limit = '4096MB';
SET memory_limit = '4096000KB';
```

C#에서 DuckDB를 연결할 때 설정하는 방법:

```csharp
using DuckDB.NET.Data;

var connectionString = "DataSource=:memory:";
using var connection = new DuckDBConnection(connectionString);
connection.Open();

// 연결 후 즉시 메모리 한도 설정
using var cmd = connection.CreateCommand();
cmd.CommandText = "SET memory_limit = '4GB'";
cmd.ExecuteNonQuery();

cmd.CommandText = "SET temp_directory = 'C:/temp/duckdb_spill'";
cmd.ExecuteNonQuery();
```

### Spill-to-Disk 이해하기

메모리 한도를 초과하는 쿼리를 실행하면 DuckDB는 즉시 오류를 내지 않는다. 대신 임시 디렉토리에 데이터를 써서 처리를 계속한다. 이것을 **Spill-to-Disk**라고 한다.

```sql
-- 임시 파일이 저장될 디렉토리 설정
SET temp_directory = 'D:/temp/duckdb_spill';
```

Spill-to-Disk는 메모리 부족 상황에서 쿼리가 죽지 않게 해주는 안전망이지만, 디스크 I/O가 메모리보다 수십~수백 배 느리기 때문에 성능이 크게 저하된다. 자주 발생한다면 메모리를 늘리거나 쿼리를 분할해야 한다.

Spill 발생 여부는 `EXPLAIN ANALYZE` 결과에서 확인할 수 있다.

```
HASH_JOIN (Time: 8,420ms, Rows: 50,000,000)
  [SPILLED: 12.4 GB to disk]
```

### 게임 서버 환경에서의 메모리 설정 가이드

게임 서버는 다양한 프로세스가 공존한다. 서버 머신 메모리를 어떻게 분배할지는 서버의 역할에 따라 달라진다.

```
게임 서버 머신 메모리 배분 예시 (총 32GB RAM):

┌─────────────────────────────────────────────────────┐
│                  메모리 배분 전략                    │
├────────────────────────────┬────────────────────────┤
│ 역할                       │ DuckDB memory_limit    │
├────────────────────────────┼────────────────────────┤
│ 게임 서버 (분석 보조)      │ 2GB (시스템의 6%)      │
│ 전용 분석 서버 (서버 없음) │ 24GB (시스템의 75%)    │
│ 개발 환경 로컬 머신        │ 8GB (시스템의 25%)     │
│ 컨테이너 (4GB 할당)        │ 3GB (컨테이너의 75%)   │
└────────────────────────────┴────────────────────────┘
```

게임 서버와 DuckDB 분석이 같은 머신에 있다면 게임 서버 프로세스가 메모리 부족으로 죽지 않도록 여유 공간을 넉넉히 남겨야 한다. 전용 분석 서버가 따로 있다면 메모리를 최대한 활용하도록 설정한다.

### 추가 메모리 관련 설정

```sql
-- 버퍼 매니저 크기 (자주 사용하는 데이터를 캐시할 메모리)
SET max_memory = '8GB';

-- 외부 스레드와 공유 시 메모리 한도 적용 범위
SET memory_limit = '4GB';

-- 집계 임시 데이터 크기 제한
SET max_temp_directory_size = '50GB';
```

---

## 10.5 병렬 쿼리 실행 스레드 설정

DuckDB의 강점 중 하나는 멀티코어 CPU를 자동으로 활용하는 병렬 쿼리 실행이다. 기본적으로 시스템의 모든 CPU 코어를 사용하지만, 게임 서버 환경에서는 코어 수를 제한해야 할 수 있다.

### 스레드 수 설정

```sql
-- 현재 스레드 수 확인
SELECT current_setting('threads');

-- 스레드 수를 4개로 제한
SET threads = 4;

-- 최대 스레드 수로 복원
SET threads TO DEFAULT;
```

C#에서 설정하는 방법:

```csharp
using DuckDB.NET.Data;

// 파일 기반 데이터베이스 연결 시 스레드 설정
var connectionString = "DataSource=game_analytics.db";
using var connection = new DuckDBConnection(connectionString);
connection.Open();

using var cmd = connection.CreateCommand();

// CPU 코어를 게임 서버와 나눠서 사용
cmd.CommandText = "SET threads = 4";
cmd.ExecuteNonQuery();

// 메모리도 함께 제한
cmd.CommandText = "SET memory_limit = '4GB'";
cmd.ExecuteNonQuery();
```

### 병렬 실행의 효과 측정

```sql
-- 단일 스레드 실행
SET threads = 1;
EXPLAIN ANALYZE
SELECT log_type, COUNT(*) FROM game_logs GROUP BY log_type;
-- Total Time: 4,820ms

-- 4 스레드 실행
SET threads = 4;
EXPLAIN ANALYZE
SELECT log_type, COUNT(*) FROM game_logs GROUP BY log_type;
-- Total Time: 1,340ms

-- 8 스레드 실행
SET threads = 8;
EXPLAIN ANALYZE
SELECT log_type, COUNT(*) FROM game_logs GROUP BY log_type;
-- Total Time: 780ms
```

스레드 수에 따른 성능 변화:

```
┌──────────────────────────────────────────────────┐
│        스레드 수별 쿼리 성능 (1억 행 집계)         │
├────────────┬───────────┬────────────────────────┤
│ 스레드 수  │ 소요 시간 │ 단일 스레드 대비 속도  │
├────────────┼───────────┼────────────────────────┤
│      1     │  4,820ms  │           1x           │
│      2     │  2,580ms  │          1.9x          │
│      4     │  1,340ms  │          3.6x          │
│      8     │    780ms  │          6.2x          │
│     16     │    510ms  │          9.4x          │
└────────────┴───────────┴────────────────────────┘
  참고: 16코어 CPU 환경. 코어 수 이상의 스레드는 효과가 감소.
```

스레드가 늘어도 성능 향상이 코어 수에 비례하지 않는 이유는 스레드 간 동기화 오버헤드, 메모리 대역폭 포화, 디스크 I/O 병목 때문이다.

### 게임 서버에서 DuckDB 스레드 수 결정 방법

게임 서버와 DuckDB가 같은 머신에서 동작할 때 스레드 수 결정 공식:

```
DuckDB 스레드 수 = 전체 논리 코어 수 - 게임 서버 최소 필요 코어 수 - OS 예비 코어 수(1~2)
```

예시:
- 16코어 서버
- 게임 서버 최소 필요 코어: 8개
- OS 예비: 2개
- DuckDB 스레드: 16 - 8 - 2 = **6개**

분석 작업이 피크 타임(게임 이용자가 가장 많은 시간)에 겹치지 않도록 스케줄링하는 것도 중요하다. 새벽 시간대 배치 분석은 스레드를 늘리고, 낮 시간대 실시간 쿼리는 스레드를 줄이는 동적 설정이 이상적이다.

```csharp
// 시간대별 동적 스레드 설정 예시
public static int GetOptimalThreadCount()
{
    var hour = DateTime.Now.Hour;

    // 새벽 2~6시: 배치 분석 시간, 최대 스레드 사용
    if (hour >= 2 && hour < 6)
        return Environment.ProcessorCount - 2;

    // 낮 12~22시: 피크 타임, 스레드 최소화
    if (hour >= 12 && hour < 22)
        return 2;

    // 그 외: 중간 수준
    return Math.Max(2, Environment.ProcessorCount / 4);
}
```

---

## 10.6 C# 애플리케이션 레벨 최적화

DuckDB 자체 설정 외에도 C# 코드에서 어떻게 DuckDB를 호출하느냐에 따라 성능이 크게 달라진다. 특히 데이터를 INSERT할 때의 방식이 가장 큰 차이를 만든다.

### 단건 INSERT vs 배치 INSERT 성능 비교

가장 흔한 실수는 반복문 안에서 단건 INSERT를 실행하는 것이다.

```csharp
// 나쁜 예: 단건 INSERT 반복 (절대 이렇게 하지 말 것!)
public static async Task InsertLogsSlow(
    DuckDBConnection connection,
    List<GameLogEntry> logs)
{
    foreach (var log in logs)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO game_logs (log_time, log_type, user_id, server_id, event_data)
            VALUES ($log_time, $log_type, $user_id, $server_id, $event_data)";

        cmd.Parameters.Add(new DuckDBParameter("log_time", log.LogTime));
        cmd.Parameters.Add(new DuckDBParameter("log_type", log.LogType));
        cmd.Parameters.Add(new DuckDBParameter("user_id", log.UserId));
        cmd.Parameters.Add(new DuckDBParameter("server_id", log.ServerId));
        cmd.Parameters.Add(new DuckDBParameter("event_data", log.EventData));

        await cmd.ExecuteNonQueryAsync();
        // 매번 SQL 파싱, 실행 계획 생성, 트랜잭션 커밋 발생!
    }
}
```

10만 건을 이 방식으로 INSERT하면 약 **45초**가 걸린다. 실전에서는 사용할 수 없는 수준이다.

### Appender를 이용한 배치 INSERT

DuckDB의 `DuckDBAppender`는 대량 데이터를 빠르게 삽입하기 위해 설계된 API다. 내부적으로 컬럼 지향 배치 버퍼에 데이터를 모아서 한 번에 기록한다.

```csharp
// code/ch10/BatchInsertExample.cs

using DuckDB.NET.Data;

public class GameLogRepository
{
    private readonly DuckDBConnection _connection;

    public GameLogRepository(DuckDBConnection connection)
    {
        _connection = connection;
        EnsureTableExists();
    }

    private void EnsureTableExists()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS game_logs (
                log_time    TIMESTAMP NOT NULL,
                log_type    VARCHAR   NOT NULL,
                user_id     BIGINT    NOT NULL,
                server_id   INTEGER   NOT NULL,
                event_data  VARCHAR
            )";
        cmd.ExecuteNonQuery();
    }

    // 빠른 방법: DuckDBAppender 사용
    public void InsertLogsBatch(IEnumerable<GameLogEntry> logs)
    {
        using var appender = _connection.CreateAppender("game_logs");

        foreach (var log in logs)
        {
            appender.BeginRow();
            appender.AppendValue(log.LogTime);
            appender.AppendValue(log.LogType);
            appender.AppendValue(log.UserId);
            appender.AppendValue(log.ServerId);
            appender.AppendValue(log.EventData);
            appender.EndRow();
        }
        // Dispose 시 자동으로 Flush (or 명시적으로 appender.Close())
    }

    // 중간 방법: 다중 VALUES INSERT
    public void InsertLogsMultiValues(List<GameLogEntry> logs, int batchSize = 1000)
    {
        for (int i = 0; i < logs.Count; i += batchSize)
        {
            var batch = logs.Skip(i).Take(batchSize).ToList();
            var values = string.Join(",\n",
                batch.Select((_, idx) =>
                    $"($t{idx}, $y{idx}, $u{idx}, $s{idx}, $e{idx})"));

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = $@"
                INSERT INTO game_logs
                    (log_time, log_type, user_id, server_id, event_data)
                VALUES {values}";

            for (int j = 0; j < batch.Count; j++)
            {
                cmd.Parameters.Add(new DuckDBParameter($"t{j}", batch[j].LogTime));
                cmd.Parameters.Add(new DuckDBParameter($"y{j}", batch[j].LogType));
                cmd.Parameters.Add(new DuckDBParameter($"u{j}", batch[j].UserId));
                cmd.Parameters.Add(new DuckDBParameter($"s{j}", batch[j].ServerId));
                cmd.Parameters.Add(new DuckDBParameter($"e{j}", batch[j].EventData));
            }

            cmd.ExecuteNonQuery();
        }
    }
}

public record GameLogEntry(
    DateTime LogTime,
    string LogType,
    long UserId,
    int ServerId,
    string? EventData
);
```

세 가지 방법의 성능 비교 (10만 건 INSERT):

```
┌──────────────────────────────────────────────────────┐
│           INSERT 방법별 성능 비교 (10만 건)            │
├──────────────────────────┬───────────┬───────────────┤
│ 방법                     │ 소요 시간 │ 초당 처리량   │
├──────────────────────────┼───────────┼───────────────┤
│ 단건 INSERT (반복문)     │  45,200ms │  2,212 rows/s │
│ 다중 VALUES (1000건씩)   │   1,840ms │ 54,348 rows/s │
│ DuckDBAppender           │     320ms │ 312,500 rows/s│
└──────────────────────────┴───────────┴───────────────┘
```

`DuckDBAppender`는 단건 INSERT보다 약 **141배** 빠르다.

### 커넥션 재사용 패턴

DuckDB의 파일 기반 데이터베이스는 연결을 열 때 파일을 열고 초기화하는 비용이 발생한다. 매번 새 커넥션을 만드는 것보다 커넥션을 재사용하는 것이 훨씬 효율적이다.

```csharp
// code/ch10/ConnectionManager.cs

using DuckDB.NET.Data;

/// <summary>
/// DuckDB 커넥션 싱글턴 매니저.
/// DuckDB는 멀티스레드에서 단일 커넥션을 공유하여 사용한다.
/// (DuckDB의 단일 파일 DB는 동시 쓰기에 내부 잠금을 사용함)
/// </summary>
public sealed class DuckDbConnectionManager : IDisposable
{
    private static DuckDbConnectionManager? _instance;
    private static readonly Lock _lock = new();

    private readonly DuckDBConnection _connection;
    private bool _disposed;

    private DuckDbConnectionManager(string databasePath)
    {
        _connection = new DuckDBConnection($"DataSource={databasePath}");
        _connection.Open();
        ApplyOptimalSettings();
    }

    public static DuckDbConnectionManager GetInstance(string databasePath)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                _instance ??= new DuckDbConnectionManager(databasePath);
            }
        }
        return _instance;
    }

    private void ApplyOptimalSettings()
    {
        using var cmd = _connection.CreateCommand();

        // 게임 서버 환경 최적 설정
        var settings = new[]
        {
            "SET memory_limit = '4GB'",
            "SET threads = 4",
            "SET temp_directory = 'C:/temp/duckdb_spill'",
            "PRAGMA enable_progress_bar = false"  // 콘솔 출력 비활성화
        };

        foreach (var setting in settings)
        {
            cmd.CommandText = setting;
            cmd.ExecuteNonQuery();
        }
    }

    public DuckDBConnection Connection => _connection;

    public DuckDBCommand CreateCommand()
    {
        return _connection.CreateCommand();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection.Dispose();
            _disposed = true;
        }
    }
}
```

### 비동기 처리 주의사항

DuckDB .NET 라이브러리는 비동기 메서드(`ExecuteNonQueryAsync`, `ExecuteReaderAsync`)를 제공하지만, 내부적으로는 동기 코드를 Thread Pool에서 실행한다. 따라서 진정한 비동기 I/O가 아니라 **스레드 오프로딩**에 가깝다.

주의해야 할 사항:

```csharp
// 주의: 여러 비동기 쿼리를 동시에 실행하면 안 됨
// DuckDB는 단일 커넥션에서 동시 쿼리를 지원하지 않는다!

// 나쁜 예: 동시 실행 시도 (예외 발생 가능)
var task1 = cmd1.ExecuteNonQueryAsync();
var task2 = cmd2.ExecuteNonQueryAsync();
await Task.WhenAll(task1, task2);  // 위험!

// 좋은 예: 순차 실행
await cmd1.ExecuteNonQueryAsync();
await cmd2.ExecuteNonQueryAsync();
```

분석 쿼리를 백그라운드에서 실행하려면 별도의 DuckDB 파일을 열거나, 단일 커넥션에서 순차적으로 실행하도록 큐를 만들어야 한다.

```csharp
// code/ch10/AnalyticsQueue.cs

using System.Threading.Channels;
using DuckDB.NET.Data;

/// <summary>
/// 분석 쿼리를 순차적으로 처리하는 백그라운드 큐.
/// 게임 서버의 메인 스레드를 블록하지 않으면서
/// DuckDB 커넥션을 안전하게 단일 스레드에서 사용한다.
/// </summary>
public class AnalyticsQueue : IAsyncDisposable
{
    private readonly Channel<Func<DuckDBConnection, Task>> _channel;
    private readonly DuckDBConnection _connection;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _cts = new();

    public AnalyticsQueue(string databasePath)
    {
        _channel = Channel.CreateBounded<Func<DuckDBConnection, Task>>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        _connection = new DuckDBConnection($"DataSource={databasePath}");
        _connection.Open();

        // 단일 워커 스레드에서만 DuckDB 커넥션 사용
        _workerTask = Task.Run(ProcessQueueAsync);
    }

    public async Task EnqueueAsync(Func<DuckDBConnection, Task> work)
    {
        await _channel.Writer.WriteAsync(work, _cts.Token);
    }

    // 로그 배치 삽입 편의 메서드
    public Task EnqueueBatchInsert(IReadOnlyList<GameLogEntry> logs)
    {
        return EnqueueAsync(async conn =>
        {
            await Task.Run(() =>
            {
                using var appender = conn.CreateAppender("game_logs");
                foreach (var log in logs)
                {
                    appender.BeginRow();
                    appender.AppendValue(log.LogTime);
                    appender.AppendValue(log.LogType);
                    appender.AppendValue(log.UserId);
                    appender.AppendValue(log.ServerId);
                    appender.AppendValue(log.EventData);
                    appender.EndRow();
                }
            });
        });
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var work in _channel.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                await work(_connection);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AnalyticsQueue] Error: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
        await _workerTask;
        _connection.Dispose();
        _cts.Dispose();
    }
}
```

### 종합 성능 비교 요약

최적화 기법을 적용했을 때의 실전 시나리오별 성능 변화:

```
┌─────────────────────────────────────────────────────────────────┐
│          시나리오별 최적화 전/후 성능 비교                       │
├───────────────────────────────┬────────────┬────────────────────┤
│ 시나리오                      │ 최적화 전  │ 최적화 후          │
├───────────────────────────────┼────────────┼────────────────────┤
│ 1일치 로그 조회 (1억 행)      │  45.2초    │  0.12초 (파티셔닝) │
│ 10만 건 로그 INSERT           │  45.2초    │  0.32초 (Appender) │
│ 10GB CSV 파일 로드            │  95.3초    │ 10.8초 (Parquet)   │
│ 복잡한 집계 쿼리 (8코어)     │  4.82초    │  0.78초 (8스레드)  │
└───────────────────────────────┴────────────┴────────────────────┘
```

---

## 이 장의 핵심 정리

이 장에서는 DuckDB 쿼리와 시스템 전반의 성능을 최적화하는 다양한 기법을 배웠다.

**쿼리 분석**
- `EXPLAIN`으로 실행 계획의 구조를 확인하고, `EXPLAIN ANALYZE`로 각 단계의 실제 시간과 행 수를 측정한다.
- 실행 계획에서 `TABLE_SCAN`의 예상 행 수가 실제와 크게 다르면 `ANALYZE`로 통계를 갱신한다.

**데이터 구조 설계**
- DuckDB는 컬럼 지향 스토리지이므로 전통적인 B-Tree 인덱스 대신 **파티셔닝**으로 스캔 범위를 줄인다.
- `COPY ... PARTITION_BY`로 날짜, 로그 유형 등 자주 필터링하는 컬럼을 기준으로 Parquet 파일을 파티셔닝한다.
- Parquet 형식은 CSV보다 로드 속도가 최대 10배 이상 빠르고, 파일 크기도 훨씬 작다.

**시스템 설정**
- `SET memory_limit`으로 DuckDB의 메모리 사용량을 제한해 게임 서버와 안전하게 공존한다.
- `SET threads`로 CPU 코어 사용량을 조절한다. 피크 타임에는 줄이고 배치 시간에는 늘린다.
- `SET temp_directory`로 Spill-to-Disk 위치를 지정한다. 용량이 충분한 디스크를 사용한다.

**C# 코드 레벨**
- 대량 INSERT는 반드시 `DuckDBAppender`를 사용한다. 단건 INSERT보다 140배 이상 빠르다.
- 커넥션은 싱글턴으로 관리해 재사용한다. 매번 새 커넥션을 열면 불필요한 초기화 비용이 생긴다.
- 단일 커넥션에서 동시 쿼리 실행은 지원되지 않는다. 순차 처리 큐 패턴을 사용한다.

---

## 다음 장 예고
10장에서 배운 성능 최적화 기법들은 강력하지만, 각각을 독립적으로 익혔을 때는 전체적인 그림이 잘 보이지 않는다.

11장에서는 1장부터 10장까지 배운 모든 것을 하나의 실전 프로젝트로 통합한다. 온라인 게임 운영팀이 실제로 사용하는 **게임 로그 분석 대시보드**를 처음부터 끝까지 구축한다. 데이터 수집 파이프라인 설계, Parquet 파티셔닝 적용, C# 분석 API 구현, 그리고 성능 튜닝까지 전 과정을 직접 만들어보자.  