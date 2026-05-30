# 제6장: 파일 데이터 읽기/쓰기
5장에서는 C#으로 DuckDB를 프로그래밍적으로 제어하는 방법을 익혔다. 이번 장에서는 DuckDB의 핵심 강점 중 하나인 **파일 직접 처리 능력**을 깊이 있게 다룬다. DuckDB는 별도의 데이터 적재(ETL) 과정 없이 CSV, JSON, Parquet 파일을 SQL로 직접 쿼리할 수 있다. 온라인 게임 서비스를 운영하다 보면 플레이어 행동 로그, 아이템 거래 기록, 전투 통계 등 다양한 형태의 파일이 쌓인다. DuckDB는 이런 파일들을 마치 테이블처럼 다룰 수 있어 게임 데이터 분석에 최적화된 도구다.

---

## 6.1 CSV 파일 직접 읽기 (`read_csv_auto`)

### CSV 파일이란?
CSV(Comma-Separated Values)는 데이터를 쉼표로 구분하여 저장하는 가장 단순하고 범용적인 파일 형식이다. 게임 서버에서는 플레이어 데이터 내보내기, 배치 처리 결과, 외부 시스템 연동 등 다양한 용도로 CSV 파일을 주고받는다.

### 샘플 CSV 파일 구성

다음과 같은 게임 전투 로그 CSV 파일이 있다고 가정하자. 파일 경로는 `code/ch06/data/battle_log.csv`이다.

```
log_id,timestamp,player_id,player_name,monster_id,monster_name,damage_dealt,damage_taken,result,level,zone_id
1,2025-03-01 09:01:23,1001,DragonSlayer,501,Goblin,320,45,WIN,25,Z01
2,2025-03-01 09:02:11,1002,IceWizard,502,Orc,185,120,WIN,22,Z01
3,2025-03-01 09:03:45,1003,ShadowRogue,503,Troll,410,200,WIN,28,Z02
4,2025-03-01 09:05:02,1001,DragonSlayer,504,BossWolf,250,380,LOSE,25,Z02
5,2025-03-01 09:06:30,1004,HolyPaladin,501,Goblin,95,10,WIN,15,Z01
6,2025-03-01 09:07:55,1002,IceWizard,505,DarkElf,220,95,WIN,22,Z03
7,2025-03-01 09:09:10,1005,BerserkerKing,504,BossWolf,580,310,WIN,35,Z02
8,2025-03-01 09:10:44,1003,ShadowRogue,506,Vampire,330,180,LOSE,28,Z03
9,2025-03-01 09:12:01,1001,DragonSlayer,501,Goblin,290,30,WIN,25,Z01
10,2025-03-01 09:13:22,1004,HolyPaladin,502,Orc,110,85,WIN,15,Z01
```

### `read_csv_auto`로 바로 쿼리하기
DuckDB의 `read_csv_auto` 함수는 파일을 열어 컬럼 타입을 자동으로 추론한다. 별도 스키마 정의 없이 바로 쿼리를 날릴 수 있다.

```sql
-- CSV 파일을 직접 쿼리 (파일을 테이블처럼 사용)
SELECT *
FROM read_csv_auto('code/ch06/data/battle_log.csv')
LIMIT 5;
```

```sql
-- 승률 집계: 플레이어별 승패 현황
SELECT
    player_name,
    COUNT(*) AS total_battles,
    SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END) AS wins,
    ROUND(
        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END) * 100.0 / COUNT(*), 2
    ) AS win_rate_pct
FROM read_csv_auto('code/ch06/data/battle_log.csv')
GROUP BY player_name
ORDER BY win_rate_pct DESC;
```

```sql
-- 자동 타입 감지 확인: 컬럼 타입 조회
DESCRIBE SELECT * FROM read_csv_auto('code/ch06/data/battle_log.csv');
```

`DESCRIBE`를 실행하면 `timestamp` 컬럼이 `TIMESTAMP` 타입으로, `damage_dealt` 같은 숫자 컬럼이 `BIGINT`로 자동 추론된 것을 확인할 수 있다. 이것이 `read_csv_auto`의 핵심 장점이다. 개발자가 직접 타입을 선언하지 않아도 된다.

### 자동 타입 감지의 동작 원리
`read_csv_auto`는 파일 상단의 일부 행을 샘플링하여 각 컬럼의 데이터 패턴을 분석한다. 날짜처럼 생긴 문자열은 `DATE` 또는 `TIMESTAMP`로, 정수처럼 보이는 값은 `INTEGER` 또는 `BIGINT`로, 소수점이 있으면 `DOUBLE`로 추론한다. 이 자동 감지 덕분에 데이터 엔지니어링 작업의 상당 부분이 줄어든다.

### 옵션을 직접 지정하는 방법
자동 감지가 잘못 동작할 경우, 직접 옵션을 지정할 수 있다.

```sql
-- 헤더 없는 CSV, 구분자를 탭으로 지정
SELECT *
FROM read_csv_auto(
    'code/ch06/data/battle_log_noheader.csv',
    header = false,
    delim = '\t',
    columns = {
        'log_id': 'INTEGER',
        'timestamp': 'TIMESTAMP',
        'player_id': 'INTEGER',
        'player_name': 'VARCHAR'
    }
);
```

```sql
-- 특정 컬럼만 선택하고 필터링
SELECT
    player_name,
    monster_name,
    damage_dealt,
    result
FROM read_csv_auto('code/ch06/data/battle_log.csv')
WHERE zone_id = 'Z02'
  AND result = 'WIN'
ORDER BY damage_dealt DESC;
```

### CSV 파일을 영구 테이블로 적재
분석을 반복적으로 수행한다면 CSV를 DuckDB 테이블로 한 번만 적재해 두는 것이 효율적이다.

```sql
-- CSV 내용을 테이블로 생성
CREATE TABLE battle_log AS
SELECT * FROM read_csv_auto('code/ch06/data/battle_log.csv');

-- 이후에는 일반 테이블처럼 사용
SELECT zone_id, AVG(damage_dealt) AS avg_damage
FROM battle_log
GROUP BY zone_id
ORDER BY avg_damage DESC;
```

---

## 6.2 JSON 파일 읽기 (`read_json_auto`)

### 게임 데이터와 JSON
JSON은 게임 서버와 클라이언트 사이의 통신, 설정 파일, 이벤트 로그 등 계층적(nested) 데이터를 표현할 때 많이 사용된다. 아이템 습득 이벤트라면 플레이어 정보, 아이템 세부 정보, 위치 정보 등이 중첩 구조로 저장되는 경우가 흔하다.

### 샘플 JSON 파일

파일 경로: `code/ch06/data/item_acquisition.json`

```json
[
  {
    "event_id": 1,
    "timestamp": "2025-03-01T09:15:00",
    "player": {
      "id": 1001,
      "name": "DragonSlayer",
      "level": 25,
      "class": "Warrior"
    },
    "item": {
      "id": 3001,
      "name": "Iron Sword",
      "grade": "Rare",
      "stats": { "attack": 85, "durability": 100 }
    },
    "location": {
      "zone_id": "Z02",
      "x": 234.5,
      "y": 178.2
    },
    "source": "MONSTER_DROP"
  },
  {
    "event_id": 2,
    "timestamp": "2025-03-01T09:18:30",
    "player": {
      "id": 1002,
      "name": "IceWizard",
      "level": 22,
      "class": "Mage"
    },
    "item": {
      "id": 3002,
      "name": "Magic Staff",
      "grade": "Epic",
      "stats": { "magic_attack": 130, "mana": 50 }
    },
    "location": {
      "zone_id": "Z03",
      "x": 512.0,
      "y": 300.1
    },
    "source": "QUEST_REWARD"
  },
  {
    "event_id": 3,
    "timestamp": "2025-03-01T09:22:45",
    "player": {
      "id": 1003,
      "name": "ShadowRogue",
      "level": 28,
      "class": "Rogue"
    },
    "item": {
      "id": 3003,
      "name": "Shadow Dagger",
      "grade": "Legendary",
      "stats": { "attack": 200, "critical": 45 }
    },
    "location": {
      "zone_id": "Z03",
      "x": 488.3,
      "y": 295.7
    },
    "source": "MONSTER_DROP"
  }
]
```

### `read_json_auto`로 중첩 JSON 처리
DuckDB의 `read_json_auto`는 중첩된 JSON 구조를 자동으로 인식하여 컬럼으로 펼쳐준다.

```sql
-- JSON 파일 전체 조회
SELECT *
FROM read_json_auto('code/ch06/data/item_acquisition.json');
```

결과를 보면 `player`, `item`, `location` 같은 객체 컬럼이 `STRUCT` 타입으로 표현된다. 이 구조체의 하위 필드에 접근하려면 `.` 연산자를 사용한다.

```sql
-- 중첩 필드 직접 접근
SELECT
    event_id,
    timestamp,
    player.name   AS player_name,
    player.level  AS player_level,
    item.name     AS item_name,
    item.grade    AS item_grade,
    location.zone_id AS zone_id,
    source
FROM read_json_auto('code/ch06/data/item_acquisition.json');
```

```sql
-- 등급별 아이템 습득 현황
SELECT
    item.grade AS grade,
    COUNT(*) AS count,
    source
FROM read_json_auto('code/ch06/data/item_acquisition.json')
GROUP BY item.grade, source
ORDER BY count DESC;
```

### JSON Lines 형식 처리
서버 로그는 각 줄이 독립된 JSON 객체인 JSONL(JSON Lines) 형식을 자주 사용한다. DuckDB는 이 형식도 자동으로 처리한다.

파일 경로: `code/ch06/data/server_events.jsonl`

```
{"event_id":1,"type":"LOGIN","player_id":1001,"timestamp":"2025-03-01T09:00:00","ip":"192.168.1.1"}
{"event_id":2,"type":"LOGIN","player_id":1002,"timestamp":"2025-03-01T09:00:05","ip":"192.168.1.2"}
{"event_id":3,"type":"LOGOUT","player_id":1001,"timestamp":"2025-03-01T10:30:00","ip":"192.168.1.1"}
{"event_id":4,"type":"PURCHASE","player_id":1003,"timestamp":"2025-03-01T11:15:22","ip":"10.0.0.5"}
{"event_id":5,"type":"LOGOUT","player_id":1002,"timestamp":"2025-03-01T12:00:00","ip":"192.168.1.2"}
```

```sql
-- JSONL 파일 읽기 (format 옵션 명시)
SELECT
    event_id,
    type,
    player_id,
    timestamp
FROM read_json_auto(
    'code/ch06/data/server_events.jsonl',
    format = 'newline_delimited'
)
ORDER BY timestamp;
```

```sql
-- 이벤트 유형별 집계
SELECT
    type,
    COUNT(*) AS event_count
FROM read_json_auto(
    'code/ch06/data/server_events.jsonl',
    format = 'newline_delimited'
)
GROUP BY type
ORDER BY event_count DESC;
```

---
  

## 6.3 Parquet 파일 읽기/쓰기

### Parquet이란?
Parquet은 Apache 재단이 만든 **컬럼 지향 파일 형식**이다. CSV가 행(row) 단위로 데이터를 저장하는 반면, Parquet은 열(column) 단위로 저장한다. 이 덕분에 특정 컬럼만 선택하는 분석 쿼리에서 I/O가 대폭 줄어든다. 또한 내장 압축 기능으로 파일 크기가 CSV 대비 10분의 1 수준으로 줄어드는 경우도 있다. 대규모 게임 로그 처리에 매우 적합한 형식이다.

### CSV vs Parquet 비교

| 항목 | CSV | Parquet |
|------|-----|---------|
| 파일 구조 | 행 지향 | 컬럼 지향 |
| 압축 | 없음 (별도 압축 필요) | 내장 압축 (Snappy, ZSTD 등) |
| 타입 정보 | 없음 (텍스트) | 명시적 타입 저장 |
| 쿼리 속도 | 느림 (전체 행 스캔) | 빠름 (필요 컬럼만 읽음) |
| 가독성 | 텍스트 에디터로 열람 가능 | 바이너리 (전용 도구 필요) |
| 적합 용도 | 소규모 데이터 교환 | 대용량 분석 |

### Parquet 파일 읽기
DuckDB는 Parquet 파일을 기본적으로 지원한다. 별도 플러그인 없이 바로 사용할 수 있다.

```sql
-- Parquet 파일 직접 쿼리
SELECT *
FROM read_parquet('code/ch06/data/battle_log.parquet')
LIMIT 10;
```

```sql
-- 컬럼 선택 (Parquet은 필요한 컬럼만 읽으므로 매우 빠름)
SELECT
    player_name,
    SUM(damage_dealt) AS total_damage,
    COUNT(*) AS battle_count
FROM read_parquet('code/ch06/data/battle_log.parquet')
GROUP BY player_name
ORDER BY total_damage DESC;
```

### Parquet 파일 쓰기
`COPY TO` 명령이나 `EXPORT` 구문으로 Parquet 파일을 생성할 수 있다.

```sql
-- 테이블을 Parquet으로 저장
COPY battle_log TO 'code/ch06/output/battle_log.parquet' (FORMAT PARQUET);
```

```sql
-- 쿼리 결과를 Parquet으로 저장 (압축 알고리즘 지정)
COPY (
    SELECT player_id, player_name, zone_id, SUM(damage_dealt) AS total_damage
    FROM battle_log
    GROUP BY player_id, player_name, zone_id
)
TO 'code/ch06/output/player_zone_summary.parquet'
(FORMAT PARQUET, COMPRESSION ZSTD);
```

```sql
-- Parquet 파일의 메타데이터 확인 (컬럼 타입, Row Group 정보 등)
SELECT *
FROM parquet_metadata('code/ch06/output/battle_log.parquet');
```

```sql
-- Parquet 스키마 확인
SELECT *
FROM parquet_schema('code/ch06/output/battle_log.parquet');
```

### Parquet 파티셔닝

대용량 데이터를 날짜나 카테고리별로 파티셔닝하면 쿼리 성능이 더욱 향상된다.

```sql
-- 날짜별로 파티셔닝하여 저장
COPY (
    SELECT
        *,
        DATE_TRUNC('day', timestamp) AS log_date
    FROM battle_log
)
TO 'code/ch06/output/battle_partitioned'
(FORMAT PARQUET, PARTITION_BY (log_date));
```

이 명령을 실행하면 `battle_partitioned/log_date=2025-03-01/` 같은 디렉토리 구조로 파일이 자동 분할된다.

---
  

## 6.4 COPY 명령으로 대량 내보내기

### COPY 명령 개요
`COPY`는 DuckDB에서 파일과 테이블 사이에 데이터를 대량으로 이동시키는 명령이다. `INSERT INTO`로 한 건씩 넣는 것보다 훨씬 빠르며, 파일 포맷을 지정하여 다양한 형태로 내보낼 수 있다.

### COPY TO: 데이터 내보내기

```sql
-- 테이블 전체를 CSV로 내보내기
COPY battle_log TO 'code/ch06/output/battle_log_export.csv' (FORMAT CSV, HEADER TRUE);
```

```sql
-- 쿼리 결과를 CSV로 내보내기 (구분자, 인코딩 지정)
COPY (
    SELECT
        player_id,
        player_name,
        COUNT(*)        AS total_battles,
        SUM(damage_dealt) AS total_damage,
        SUM(damage_taken) AS total_damage_taken,
        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END) AS wins
    FROM battle_log
    GROUP BY player_id, player_name
    ORDER BY total_damage DESC
)
TO 'code/ch06/output/player_stats.csv'
(FORMAT CSV, HEADER TRUE, DELIMITER ',');
```

```sql
-- JSON 형태로 내보내기
COPY battle_log TO 'code/ch06/output/battle_log.json' (FORMAT JSON);
```

```sql
-- Parquet으로 내보내기 (압축 포함)
COPY battle_log TO 'code/ch06/output/battle_log.parquet'
(FORMAT PARQUET, COMPRESSION SNAPPY);
```

### COPY FROM: 데이터 적재

```sql
-- 기존 테이블에 CSV 데이터 추가
COPY battle_log FROM 'code/ch06/data/battle_log_new.csv' (FORMAT CSV, HEADER TRUE);
```

```sql
-- Parquet 파일로부터 새 테이블 생성
CREATE TABLE battle_log_parquet AS
SELECT * FROM read_parquet('code/ch06/data/battle_log.parquet');
```

### 대용량 내보내기 팁
파일 크기가 커지면 단일 파일보다 여러 파일로 분할하는 것이 유리하다. DuckDB는 행 수 기준으로 파일을 자동 분할할 수 있다.

```sql
-- 100만 행마다 파일 분할 (파일명에 자동 번호 부여)
COPY battle_log TO 'code/ch06/output/battle_chunk_*.csv'
(FORMAT CSV, HEADER TRUE);
```

---
  

## 6.5 C#에서 파일 임포트/익스포트 자동화

### 게임 서버에서의 활용 시나리오
게임 서버는 보통 다음과 같은 자동화된 파일 처리 흐름을 필요로 한다.

1. 매일 자정에 게임 로그 파일을 DuckDB에 임포트
2. 통계 집계 쿼리 실행
3. 결과를 CSV나 Parquet으로 내보내어 BI 도구나 외부 시스템에 전달

C#에서 이 과정을 자동화하는 코드를 작성해 보자.

파일 경로: `code/ch06/FileImportExport.cs`

```csharp
using DuckDB.NET.Data;
using System;
using System.IO;

namespace Ch06FileIO
{
    /// <summary>
    /// DuckDB를 이용한 게임 로그 파일 임포트/익스포트 자동화
    /// </summary>
    public class GameLogFileProcessor
    {
        private readonly string _dbPath;
        private readonly string _dataDirectory;
        private readonly string _outputDirectory;

        public GameLogFileProcessor(
            string dbPath,
            string dataDirectory,
            string outputDirectory)
        {
            _dbPath = dbPath;
            _dataDirectory = dataDirectory;
            _outputDirectory = outputDirectory;

            Directory.CreateDirectory(outputDirectory);
        }

        /// <summary>
        /// CSV 파일을 DuckDB 테이블로 임포트
        /// </summary>
        public void ImportCsvToTable(string csvFileName, string tableName)
        {
            string csvPath = Path.Combine(_dataDirectory, csvFileName)
                                 .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();

            // 테이블이 이미 존재하면 삭제 후 재생성
            command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            command.ExecuteNonQuery();

            // CSV를 읽어 테이블 생성 (타입 자동 감지)
            command.CommandText = $@"
                CREATE TABLE {tableName} AS
                SELECT * FROM read_csv_auto('{csvPath}')";
            command.ExecuteNonQuery();

            // 임포트된 행 수 확인
            command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            long rowCount = (long)(command.ExecuteScalar() ?? 0L);

            Console.WriteLine($"[임포트 완료] {csvFileName} -> {tableName}: {rowCount:N0}행");
        }

        /// <summary>
        /// 테이블을 CSV 파일로 익스포트
        /// </summary>
        public void ExportTableToCsv(string tableName, string outputFileName)
        {
            string outputPath = Path.Combine(_outputDirectory, outputFileName)
                                    .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                COPY {tableName}
                TO '{outputPath}'
                (FORMAT CSV, HEADER TRUE)";
            command.ExecuteNonQuery();

            long fileSize = new FileInfo(outputPath.Replace('/', '\\')).Length;
            Console.WriteLine($"[익스포트 완료] {tableName} -> {outputFileName} ({fileSize:N0} bytes)");
        }

        /// <summary>
        /// 쿼리 결과를 Parquet 파일로 저장
        /// </summary>
        public void ExportQueryToParquet(string query, string outputFileName)
        {
            string outputPath = Path.Combine(_outputDirectory, outputFileName)
                                    .Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                COPY ({query})
                TO '{outputPath}'
                (FORMAT PARQUET, COMPRESSION ZSTD)";
            command.ExecuteNonQuery();

            Console.WriteLine($"[Parquet 저장 완료] -> {outputFileName}");
        }

        /// <summary>
        /// 일별 게임 통계 리포트 생성
        /// </summary>
        public void GenerateDailyReport(DateTime reportDate)
        {
            string dateStr = reportDate.ToString("yyyy-MM-dd");
            Console.WriteLine($"\n===== {dateStr} 일별 게임 통계 리포트 생성 중 =====\n");

            // 1단계: 해당 날짜의 CSV 파일 임포트
            string csvFileName = $"battle_log_{dateStr.Replace("-", "")}.csv";
            ImportCsvToTable(csvFileName, "battle_log_daily");

            // 2단계: 플레이어별 통계 집계 후 Parquet 저장
            string playerStatsQuery = $@"
                SELECT
                    player_id,
                    player_name,
                    COUNT(*)                                              AS total_battles,
                    SUM(damage_dealt)                                     AS total_damage_dealt,
                    SUM(damage_taken)                                     AS total_damage_taken,
                    SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)      AS wins,
                    SUM(CASE WHEN result = 'LOSE' THEN 1 ELSE 0 END)     AS losses,
                    ROUND(
                        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)
                        * 100.0 / COUNT(*), 2
                    )                                                     AS win_rate
                FROM battle_log_daily
                GROUP BY player_id, player_name
                ORDER BY total_damage_dealt DESC";

            ExportQueryToParquet(
                playerStatsQuery,
                $"player_stats_{dateStr.Replace("-", "")}.parquet");

            // 3단계: 구역별 통계를 CSV로 내보내기 (BI 도구 연동용)
            string zoneStatsQuery = $@"
                SELECT
                    zone_id,
                    COUNT(DISTINCT player_id)                             AS unique_players,
                    COUNT(*)                                              AS total_battles,
                    AVG(damage_dealt)                                     AS avg_damage,
                    MAX(damage_dealt)                                     AS max_damage
                FROM battle_log_daily
                GROUP BY zone_id
                ORDER BY total_battles DESC";

            // 임시 뷰로 만든 후 CSV 내보내기
            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();

            cmd.CommandText = $"CREATE OR REPLACE VIEW zone_stats_view AS {zoneStatsQuery}";
            cmd.ExecuteNonQuery();

            ExportTableToCsv(
                "zone_stats_view",
                $"zone_stats_{dateStr.Replace("-", "")}.csv");

            Console.WriteLine($"\n[완료] {dateStr} 리포트 생성이 모두 끝났습니다.");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var processor = new GameLogFileProcessor(
                dbPath: "game_analytics.db",
                dataDirectory: "code/ch06/data",
                outputDirectory: "code/ch06/output"
            );

            // 오늘 날짜 기준 리포트 생성
            processor.GenerateDailyReport(DateTime.Today);
        }
    }
}
```

### JSON 파일 임포트 자동화
파일 경로: `code/ch06/JsonImporter.cs`

```csharp
using DuckDB.NET.Data;
using System;
using System.IO;

namespace Ch06FileIO
{
    public class GameEventJsonImporter
    {
        private readonly string _dbPath;

        public GameEventJsonImporter(string dbPath)
        {
            _dbPath = dbPath;
        }

        /// <summary>
        /// JSON 파일을 DuckDB에 임포트 (중첩 구조 자동 처리)
        /// </summary>
        public void ImportJsonEvents(string jsonFilePath)
        {
            string normalizedPath = jsonFilePath.Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();

            // 중첩 JSON을 플랫 테이블로 변환하여 저장
            command.CommandText = $@"
                CREATE OR REPLACE TABLE item_events AS
                SELECT
                    event_id,
                    CAST(timestamp AS TIMESTAMP)   AS event_time,
                    player.id                       AS player_id,
                    player.name                     AS player_name,
                    player.level                    AS player_level,
                    player.class                    AS player_class,
                    item.id                         AS item_id,
                    item.name                       AS item_name,
                    item.grade                      AS item_grade,
                    location.zone_id                AS zone_id,
                    source
                FROM read_json_auto('{normalizedPath}')";

            command.ExecuteNonQuery();

            command.CommandText = "SELECT COUNT(*) FROM item_events";
            long count = (long)(command.ExecuteScalar() ?? 0L);

            Console.WriteLine($"[JSON 임포트] {Path.GetFileName(jsonFilePath)}: {count}건 처리 완료");
        }

        /// <summary>
        /// 임포트된 이벤트 통계 출력
        /// </summary>
        public void PrintEventStats()
        {
            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    item_grade,
                    source,
                    COUNT(*) AS count
                FROM item_events
                GROUP BY item_grade, source
                ORDER BY count DESC";

            using var reader = command.ExecuteReader();

            Console.WriteLine("\n--- 아이템 등급별 습득 현황 ---");
            Console.WriteLine($"{"등급",-12} {"출처",-16} {"건수",6}");
            Console.WriteLine(new string('-', 36));

            while (reader.Read())
            {
                Console.WriteLine(
                    $"{reader.GetString(0),-12} " +
                    $"{reader.GetString(1),-16} " +
                    $"{reader.GetInt64(2),6:N0}");
            }
        }
    }
}
```

---
  

## 6.6 복수 파일 글로브 패턴으로 한번에 처리 (`*.csv`)

### 왜 글로브 패턴이 필요한가?
실제 게임 서버 운영 환경에서는 로그 파일이 날짜별, 서버별, 시간대별로 분산 저장된다. 예를 들어 다음과 같은 디렉토리 구조를 생각해 보자.

```
code/ch06/data/logs/
├── battle_log_20250301.csv
├── battle_log_20250302.csv
├── battle_log_20250303.csv
├── battle_log_20250304.csv
├── battle_log_20250305.csv
├── battle_log_20250306.csv
└── battle_log_20250307.csv
```

이 파일들을 하나씩 임포트하고 UNION ALL로 합치는 것은 번거롭다. DuckDB의 **글로브 패턴**을 사용하면 단 한 줄로 모든 파일을 한번에 읽을 수 있다.

### 글로브 패턴 기본 사용법

```sql
-- *.csv 패턴으로 모든 CSV 파일을 한번에 읽기
SELECT COUNT(*) AS total_rows
FROM read_csv_auto('code/ch06/data/logs/*.csv');
```

```sql
-- 일주일치 전투 로그 통합 분석
SELECT
    player_name,
    COUNT(*)          AS total_battles,
    SUM(damage_dealt) AS total_damage,
    MAX(damage_dealt) AS max_single_damage
FROM read_csv_auto('code/ch06/data/logs/battle_log_*.csv')
GROUP BY player_name
ORDER BY total_damage DESC;
```

```sql
-- filename 메타데이터 컬럼으로 파일 출처 확인
SELECT
    filename,
    COUNT(*) AS row_count
FROM read_csv_auto(
    'code/ch06/data/logs/*.csv',
    filename = true
)
GROUP BY filename
ORDER BY filename;
```

`filename = true` 옵션을 주면 각 행이 어느 파일에서 왔는지 알 수 있는 `filename` 컬럼이 자동으로 추가된다. 파일별 데이터 품질 확인이나 디버깅에 유용하다.

### 재귀적 글로브 패턴 (`**`)
Parquet 파티셔닝처럼 디렉토리가 중첩된 경우에는 `**` 패턴을 사용한다.

```
code/ch06/data/partitioned/
├── date=2025-03-01/
│   ├── zone=Z01/
│   │   └── data.parquet
│   └── zone=Z02/
│       └── data.parquet
└── date=2025-03-02/
    ├── zone=Z01/
    │   └── data.parquet
    └── zone=Z02/
        └── data.parquet
```

```sql
-- 중첩 디렉토리의 모든 Parquet 파일 읽기
SELECT *
FROM read_parquet('code/ch06/data/partitioned/**/*.parquet')
LIMIT 20;
```

```sql
-- 특정 날짜 파티션만 읽기 (Hive 파티셔닝 자동 인식)
SELECT *
FROM read_parquet(
    'code/ch06/data/partitioned/**/*.parquet',
    hive_partitioning = true
)
WHERE date = '2025-03-01';
```

`hive_partitioning = true` 옵션을 주면 디렉토리 이름의 `key=value` 패턴을 자동으로 컬럼으로 인식한다. 즉 `date=2025-03-01` 폴더는 `date` 컬럼 값이 `2025-03-01`인 행으로 자동 변환된다. 불필요한 파티션 디렉토리는 아예 읽지 않으므로 매우 효율적이다.

### C#에서 글로브 패턴 활용
파일 경로: `code/ch06/GlobPatternQuery.cs`

```csharp
using DuckDB.NET.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ch06FileIO
{
    public class MultiFileAnalyzer
    {
        private readonly string _dbPath;

        public MultiFileAnalyzer(string dbPath)
        {
            _dbPath = dbPath;
        }

        /// <summary>
        /// 글로브 패턴으로 여러 날짜의 전투 로그를 통합 집계
        /// </summary>
        public List<PlayerWeeklyStat> GetWeeklyPlayerStats(
            string logDirectory,
            DateTime weekStart)
        {
            // 글로브 패턴 경로 생성
            string globPattern = Path.Combine(logDirectory, "battle_log_*.csv")
                                     .Replace('\\', '/');

            string weekStartStr = weekStart.ToString("yyyyMMdd");
            string weekEndStr   = weekStart.AddDays(6).ToString("yyyyMMdd");

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT
                    player_id,
                    player_name,
                    COUNT(*)                                            AS total_battles,
                    SUM(damage_dealt)                                   AS total_damage,
                    SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)    AS wins,
                    ROUND(
                        SUM(CASE WHEN result = 'WIN' THEN 1 ELSE 0 END)
                        * 100.0 / COUNT(*), 2
                    )                                                   AS win_rate,
                    MIN(timestamp::DATE)                                AS first_battle_date,
                    MAX(timestamp::DATE)                                AS last_battle_date
                FROM read_csv_auto('{globPattern}', filename = true)
                WHERE
                    REGEXP_MATCHES(
                        filename,
                        'battle_log_(' || '{weekStartStr}' || '|' ||
                        -- 날짜 범위 필터는 timestamp 컬럼으로 처리
                        '.*' || ')'
                    )
                    AND timestamp::DATE BETWEEN '{weekStart:yyyy-MM-dd}'
                                            AND '{weekStart.AddDays(6):yyyy-MM-dd}'
                GROUP BY player_id, player_name
                ORDER BY total_damage DESC";

            var results = new List<PlayerWeeklyStat>();
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                results.Add(new PlayerWeeklyStat
                {
                    PlayerId       = reader.GetInt32(0),
                    PlayerName     = reader.GetString(1),
                    TotalBattles   = reader.GetInt64(2),
                    TotalDamage    = reader.GetInt64(3),
                    Wins           = reader.GetInt64(4),
                    WinRate        = reader.GetDouble(5)
                });
            }

            return results;
        }

        /// <summary>
        /// 여러 파일을 통합하여 Parquet으로 아카이빙
        /// </summary>
        public void ArchiveCsvToParquet(
            string sourceGlob,
            string outputParquetPath)
        {
            string normalizedOutput = outputParquetPath.Replace('\\', '/');

            using var connection = new DuckDBConnection($"Data Source={_dbPath}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                COPY (
                    SELECT * FROM read_csv_auto('{sourceGlob}')
                )
                TO '{normalizedOutput}'
                (FORMAT PARQUET, COMPRESSION ZSTD)";

            command.ExecuteNonQuery();

            long fileSize = new FileInfo(outputParquetPath).Length;
            Console.WriteLine(
                $"[아카이빙 완료] {Path.GetFileName(outputParquetPath)} " +
                $"({fileSize / 1024.0 / 1024.0:F2} MB)");
        }
    }

    public record PlayerWeeklyStat
    {
        public int    PlayerId     { get; init; }
        public string PlayerName   { get; init; } = string.Empty;
        public long   TotalBattles { get; init; }
        public long   TotalDamage  { get; init; }
        public long   Wins         { get; init; }
        public double WinRate      { get; init; }
    }
}
```

### 글로브 패턴 활용 팁 정리

```sql
-- 단일 디렉토리의 모든 CSV
read_csv_auto('logs/*.csv')

-- 특정 접두사로 시작하는 파일
read_csv_auto('logs/battle_log_202503*.csv')

-- 모든 하위 디렉토리의 Parquet
read_parquet('data/**/*.parquet')

-- 복수 경로를 리스트로 전달
read_csv_auto(['logs/march.csv', 'logs/april.csv'])

-- 파일 출처 컬럼 추가
read_csv_auto('logs/*.csv', filename = true)
```

---
  

## 이 장의 핵심 정리
이번 장에서 학습한 내용을 정리하면 다음과 같다.

| 기능 | SQL 함수/명령 | 특징 |
|------|--------------|------|
| CSV 읽기 | `read_csv_auto()` | 타입 자동 감지, 헤더 자동 인식 |
| JSON 읽기 | `read_json_auto()` | 중첩 구조 자동 처리, JSONL 지원 |
| Parquet 읽기 | `read_parquet()` | 컬럼 지향, 빠른 집계 쿼리 |
| 파일 내보내기 | `COPY TO` | CSV/JSON/Parquet 포맷 지원 |
| 파일 임포트 | `COPY FROM` | 테이블에 대량 적재 |
| 다중 파일 처리 | 글로브 패턴 `*.csv` | 수십 개 파일을 단일 쿼리로 처리 |
| 파티셔닝 | `hive_partitioning = true` | 디렉토리 이름을 컬럼으로 자동 인식 |

DuckDB의 파일 처리 능력은 기존 데이터베이스와 차별화되는 가장 강력한 기능이다. 별도의 ETL 파이프라인을 구축하지 않아도 파일을 바로 SQL로 분석하고, 결과를 다양한 포맷으로 내보낼 수 있다. 특히 글로브 패턴을 활용하면 날짜별로 쌓인 수백 개의 게임 로그 파일을 단 한 줄의 SQL로 통합 분석하는 것이 가능하다.

C#에서 이 기능들을 활용하면 게임 서버의 일별/주별 리포트 생성, 로그 아카이빙, BI 도구 연동 등의 자동화 파이프라인을 매우 간결하게 구현할 수 있다.

---

## 다음 장 예고
지금까지는 이미 존재하는 파일을 읽고 쓰는 방법에 집중했다. 7장에서는 본격적인 **온라인 게임 로그 설계와 수집 파이프라인**을 다룬다. 게임 서버에서 어떤 이벤트를 어떤 구조로 기록해야 분석에 유리한지, 실시간으로 발생하는 로그를 DuckDB로 효율적으로 적재하는 방법은 무엇인지, 그리고 여러 게임 서버 인스턴스에서 오는 로그를 중앙 집중식으로 관리하는 구조를 살펴볼 것이다. 이 장에서 배운 파일 처리 능력이 7장의 파이프라인 구축에서 핵심 역할을 하게 된다.   