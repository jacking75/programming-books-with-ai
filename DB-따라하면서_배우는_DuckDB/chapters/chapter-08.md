# 제8장: 온라인 게임 콘텐츠별 DuckDB 활용
7장에서 게임 로그를 설계하고 DuckDB에 적재하는 파이프라인을 구축했다. 이제 실제로 수집된 데이터를 어떻게 분석하는지 살펴볼 차례다. 이 장에서는 온라인 게임 서비스에서 가장 중요하게 다루는 분석 주제들을 SQL 쿼리와 함께 실전 방식으로 다룬다.

게임 데이터 분석은 단순히 숫자를 뽑아내는 작업이 아니다. 플레이어가 왜 게임을 떠나는지, 어떤 직업이 너무 강하거나 약한지, 아이템 경제가 무너지고 있는지, 누가 치트를 쓰는지를 데이터로 판단하는 일이다. DuckDB의 빠른 분석 쿼리 능력은 이런 작업에 매우 잘 맞는다.

---

## 8.1 플레이어 행동 분석 — DAU/MAU, 세션 시간, 이탈 지점

### DAU/MAU란?
게임 서비스를 운영하면 가장 먼저 보는 지표가 DAU(Daily Active Users)와 MAU(Monthly Active Users)다.

- **DAU**: 하루에 실제로 접속한 유저 수. 같은 유저가 하루에 여러 번 접속해도 1명으로 센다.
- **MAU**: 한 달 동안 최소 한 번이라도 접속한 유저 수.
- **DAU/MAU 비율**: 월간 유저 중 매일 얼마나 많은 비율이 접속하는지를 나타내는 스티키니스(Stickiness) 지표. 높을수록 게임에 대한 일상적 참여도가 높다.

분석에 사용할 테이블은 7장에서 설계한 `session_log` 테이블이다. 해당 테이블에는 `user_id`, `login_at`, `logout_at` 등의 컬럼이 있다고 가정한다.

---

#### 쿼리 1: DAU 계산
날짜별로 접속한 고유 유저 수를 구한다.

```sql
SELECT
    DATE_TRUNC('day', login_at) AS play_date,
    COUNT(DISTINCT user_id)     AS dau
FROM session_log
WHERE login_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY play_date
ORDER BY play_date;
```

**예상 결과:**

| play_date  | dau   |
|------------|-------|
| 2026-02-27 | 8,412 |
| 2026-02-28 | 9,105 |
| 2026-03-01 | 11,230|
| 2026-03-02 | 10,887|
| ...        | ...   |

**결과 해석:** 주말(토/일)에 DAU가 급격히 올라가는 패턴이 보이면 캐주얼 유저가 많다는 신호다. 평일과 주말 DAU 차이가 2배 이상이면 직장인/학생 유저층이 두껍다는 의미로, 주말 이벤트 설계에 반영할 수 있다.

---

#### 쿼리 2: MAU 계산

```sql
SELECT
    DATE_TRUNC('month', login_at) AS play_month,
    COUNT(DISTINCT user_id)        AS mau
FROM session_log
GROUP BY play_month
ORDER BY play_month;
```

---

#### 쿼리 3: DAU/MAU 비율 (스티키니스)

```sql
WITH daily AS (
    SELECT
        DATE_TRUNC('day',   login_at) AS play_date,
        DATE_TRUNC('month', login_at) AS play_month,
        COUNT(DISTINCT user_id)        AS dau
    FROM session_log
    GROUP BY play_date, play_month
),
monthly AS (
    SELECT
        DATE_TRUNC('month', login_at) AS play_month,
        COUNT(DISTINCT user_id)        AS mau
    FROM session_log
    GROUP BY play_month
)
SELECT
    d.play_date,
    d.dau,
    m.mau,
    ROUND(d.dau * 100.0 / m.mau, 2) AS stickiness_pct
FROM daily d
JOIN monthly m ON d.play_month = m.play_month
ORDER BY d.play_date;
```

**예상 결과:**

| play_date  | dau    | mau    | stickiness_pct |
|------------|--------|--------|----------------|
| 2026-03-01 | 11,230 | 52,400 | 21.43          |
| 2026-03-02 | 10,887 | 52,400 | 20.77          |

**결과 해석:** 스티키니스가 20~25%면 일반적인 수준, 30% 이상이면 매우 높은 참여도를 의미한다. 소셜 네트워크 서비스는 보통 50% 이상을 목표로 한다.

---

#### 쿼리 4: 세션 평균 플레이 시간

```sql
SELECT
    DATE_TRUNC('day', login_at) AS play_date,
    ROUND(AVG(
        EPOCH(logout_at) - EPOCH(login_at)
    ) / 60.0, 1) AS avg_session_min,
    ROUND(PERCENTILE_CONT(0.5) WITHIN GROUP (
        ORDER BY EPOCH(logout_at) - EPOCH(login_at)
    ) / 60.0, 1) AS median_session_min
FROM session_log
WHERE logout_at IS NOT NULL
GROUP BY play_date
ORDER BY play_date;
```

**예상 결과:**

| play_date  | avg_session_min | median_session_min |
|------------|-----------------|--------------------|
| 2026-03-01 | 47.3            | 32.1               |
| 2026-03-02 | 51.8            | 35.4               |

**결과 해석:** 평균이 중앙값보다 훨씬 높으면, 일부 헤비 유저가 평균을 끌어올리고 있다는 의미다. 이 경우 평균보다 중앙값이 실제 일반 유저의 플레이 시간을 더 잘 대표한다.

---

#### 쿼리 5: 이탈 위험 유저 식별

마지막 접속일로부터 N일 이상 접속하지 않은 유저를 이탈 위험군으로 분류한다.

```sql
WITH last_login AS (
    SELECT
        user_id,
        MAX(login_at) AS last_login_at
    FROM session_log
    GROUP BY user_id
)
SELECT
    user_id,
    last_login_at,
    DATEDIFF('day', last_login_at, CURRENT_TIMESTAMP) AS days_since_login,
    CASE
        WHEN DATEDIFF('day', last_login_at, CURRENT_TIMESTAMP) BETWEEN 7  AND 13 THEN '위험'
        WHEN DATEDIFF('day', last_login_at, CURRENT_TIMESTAMP) BETWEEN 14 AND 29 THEN '이탈 임박'
        WHEN DATEDIFF('day', last_login_at, CURRENT_TIMESTAMP) >= 30             THEN '이탈'
        ELSE '활성'
    END AS churn_status
FROM last_login
ORDER BY days_since_login DESC;
```

**예상 결과:**

| user_id | last_login_at       | days_since_login | churn_status |
|---------|---------------------|------------------|--------------|
| 10042   | 2026-02-25 14:22:00 | 31               | 이탈         |
| 20187   | 2026-03-10 09:15:00 | 18               | 이탈 임박    |
| 30991   | 2026-03-20 21:30:00 | 8                | 위험         |

**결과 해석:** 이탈 위험군 유저에게 복귀 보상 쿠폰을 발송하거나, 이탈 임박 유저에게 인게임 알림을 보내는 리텐션 마케팅에 활용할 수 있다.

---

### C# 코드 예시: DAU 조회 및 출력

```csharp
// code/ch08/Ex01_DAU/Program.cs
using DuckDB.NET.Data;

var connectionString = "Data Source=game_logs.db";
using var connection = new DuckDBConnection(connectionString);
connection.Open();

var sql = """
    SELECT
        DATE_TRUNC('day', login_at) AS play_date,
        COUNT(DISTINCT user_id)     AS dau
    FROM session_log
    WHERE login_at >= CURRENT_DATE - INTERVAL '7 days'
    GROUP BY play_date
    ORDER BY play_date
    """;

using var command = connection.CreateCommand();
command.CommandText = sql;

using var reader = command.ExecuteReader();

Console.WriteLine($"{"날짜",-15} {"DAU",10}");
Console.WriteLine(new string('-', 26));

while (reader.Read())
{
    var date = reader.GetDateTime(0).ToString("yyyy-MM-dd");
    var dau  = reader.GetInt64(1);
    Console.WriteLine($"{date,-15} {dau,10:N0}");
}
```

---

## 8.2 전투 밸런스 분석 — 직업별 딜량, 사망 패턴, 스킬 사용 빈도

### 왜 전투 밸런스 분석이 중요한가?

MMORPG나 RPG 장르에서 전투 밸런스는 플레이어 만족도와 직결된다. 특정 직업이 다른 직업보다 압도적으로 강하면 다양성이 사라지고, 반대로 너무 약하면 그 직업 유저들이 이탈한다. 데이터 기반 밸런스 패치는 개발팀의 감에 의존하지 않고, 실제 전투 로그에서 근거를 찾는다.

분석에 사용할 테이블:
- `combat_log`: `log_id`, `attacker_id`, `defender_id`, `skill_id`, `damage`, `is_critical`, `map_id`, `log_time`
- `player_info`: `user_id`, `job_class`, `level`
- `skill_info`: `skill_id`, `skill_name`, `job_class`

---

#### 쿼리 1: 직업별 평균 딜량 비교

```sql
SELECT
    p.job_class,
    COUNT(DISTINCT c.attacker_id)     AS player_count,
    ROUND(AVG(c.damage), 1)           AS avg_damage,
    ROUND(PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY c.damage), 1)
                                      AS median_damage,
    MAX(c.damage)                     AS max_damage,
    ROUND(AVG(c.is_critical::INT) * 100, 2) AS critical_rate_pct
FROM combat_log c
JOIN player_info p ON c.attacker_id = p.user_id
WHERE c.log_time >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY p.job_class
ORDER BY avg_damage DESC;
```

**예상 결과:**

| job_class | player_count | avg_damage | median_damage | max_damage | critical_rate_pct |
|-----------|-------------|------------|---------------|------------|-------------------|
| 어쌔신    | 1,240        | 4,820.3    | 3,910.0       | 98,400     | 28.50             |
| 워리어    | 3,501        | 3,210.5    | 2,980.0       | 45,200     | 12.30             |
| 마법사    | 2,887        | 3,050.8    | 2,700.0       | 120,500    | 9.80              |
| 힐러      | 1,105        | 890.2      | 720.0         | 8,900      | 5.10              |

**결과 해석:** 어쌔신의 평균 딜량이 다른 직업에 비해 50% 이상 높다면 밸런스 패치 검토 대상이다. 특히 크리티컬 발동률까지 높으면 PvP 환경에서 과도한 우위를 점할 수 있다.

---

#### 쿼리 2: 맵/던전별 사망률

```sql
SELECT
    m.map_name,
    COUNT(*)                                          AS total_deaths,
    COUNT(DISTINCT d.user_id)                         AS unique_dead_players,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (), 2) AS death_share_pct
FROM death_log d
JOIN map_info m ON d.map_id = m.map_id
WHERE d.death_time >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY m.map_name
ORDER BY total_deaths DESC
LIMIT 20;
```

**예상 결과:**

| map_name       | total_deaths | unique_dead_players | death_share_pct |
|----------------|-------------|---------------------|-----------------|
| 심연의 던전 5층 | 18,420       | 4,201               | 22.31           |
| 불꽃 용암 지대  | 12,870       | 3,500               | 15.59           |
| 혼돈의 탑 99층  | 9,340        | 1,890               | 11.31           |

**결과 해석:** 특정 맵에서 사망이 집중된다면 난이도 조정이나 몬스터 스탯 재검토가 필요하다. unique_dead_players가 적은데 total_deaths가 많으면, 소수의 유저가 반복 사망한다는 의미로 해당 컨텐츠의 난이도가 과도하게 높다는 신호다.

---

#### 쿼리 3: 가장 많이 사용된 스킬 Top 10

```sql
SELECT
    s.skill_name,
    s.job_class,
    COUNT(*)                          AS use_count,
    ROUND(AVG(c.damage), 1)           AS avg_damage,
    ROUND(SUM(c.damage) / 1e6, 2)     AS total_dmg_million
FROM combat_log c
JOIN skill_info s ON c.skill_id = s.skill_id
WHERE c.log_time >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY s.skill_name, s.job_class
ORDER BY use_count DESC
LIMIT 10;
```

**예상 결과:**

| skill_name   | job_class | use_count  | avg_damage | total_dmg_million |
|--------------|-----------|------------|------------|-------------------|
| 기본 공격    | 공용      | 15,820,400 | 450.2      | 7,123.82          |
| 파이어볼     | 마법사    | 3,240,100  | 2,840.5    | 9,204.12          |
| 더블 슬래시  | 어쌔신    | 2,980,200  | 3,120.0    | 9,299.82          |

---

#### 쿼리 4: 스킬 연속 사용 패턴 분석 (콤보)

플레이어가 어떤 스킬 조합을 주로 쓰는지 분석한다. `LEAD` 윈도우 함수를 사용해 연속된 두 스킬을 한 쌍으로 묶는다.

```sql
WITH skill_sequence AS (
    SELECT
        attacker_id,
        skill_id                                    AS skill_1,
        LEAD(skill_id) OVER (
            PARTITION BY attacker_id
            ORDER BY log_time
        )                                           AS skill_2,
        log_time
    FROM combat_log
    WHERE log_time >= CURRENT_DATE - INTERVAL '3 days'
)
SELECT
    s1.skill_name AS first_skill,
    s2.skill_name AS second_skill,
    COUNT(*)      AS combo_count
FROM skill_sequence ss
JOIN skill_info s1 ON ss.skill_1 = s1.skill_id
JOIN skill_info s2 ON ss.skill_2 = s2.skill_id
WHERE ss.skill_2 IS NOT NULL
GROUP BY s1.skill_name, s2.skill_name
ORDER BY combo_count DESC
LIMIT 15;
```

**예상 결과:**

| first_skill  | second_skill | combo_count |
|--------------|--------------|-------------|
| 더블 슬래시  | 관통 일격    | 1,240,300   |
| 파이어볼     | 아이스 볼트  | 980,200     |
| 기본 공격    | 파워 슬래시  | 870,100     |

**결과 해석:** 특정 스킬 콤보가 압도적으로 많이 쓰인다면, 그 조합이 최적의 딜 사이클로 굳어진 것이다. 전투 다양성을 늘리려면 다른 콤보도 경쟁력을 가질 수 있도록 밸런스 조정이 필요하다.

---

## 8.3 아이템 경제 분석 — 획득/소비/거래 흐름, 인플레이션 감지

### 게임 경제 시스템의 중요성

온라인 게임에서 아이템 경제는 매우 섬세하게 관리해야 한다. 골드나 아이템이 지나치게 많이 생성(인플레이션)되면 아이템 가치가 하락하고, 반대로 너무 빠르게 소비(디플레이션)되면 신규 유저가 진입하기 어려워진다. DuckDB를 사용하면 대량의 거래 로그에서 이런 경제 흐름을 빠르게 파악할 수 있다.

분석에 사용할 테이블:
- `item_log`: `log_id`, `user_id`, `item_id`, `action_type`('acquire'/'consume'/'trade_buy'/'trade_sell'), `quantity`, `gold_amount`, `log_time`
- `item_info`: `item_id`, `item_name`, `item_grade`, `base_price`

---

#### 쿼리 1: 시간대별 아이템 획득/소비 비율

```sql
SELECT
    HOUR(log_time)                              AS hour_of_day,
    SUM(CASE WHEN action_type = 'acquire'      THEN quantity ELSE 0 END) AS acquired,
    SUM(CASE WHEN action_type = 'consume'      THEN quantity ELSE 0 END) AS consumed,
    ROUND(
        SUM(CASE WHEN action_type = 'consume'  THEN quantity ELSE 0 END) * 100.0 /
        NULLIF(SUM(CASE WHEN action_type = 'acquire' THEN quantity ELSE 0 END), 0),
        2
    ) AS consume_rate_pct
FROM item_log
WHERE log_time >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY hour_of_day
ORDER BY hour_of_day;
```

**예상 결과:**

| hour_of_day | acquired  | consumed  | consume_rate_pct |
|-------------|-----------|-----------|------------------|
| 0           | 12,400    | 8,100     | 65.32            |
| 12          | 45,200    | 32,800    | 72.57            |
| 20          | 98,700    | 75,400    | 76.39            |
| 22          | 120,500   | 89,200    | 74.07            |

**결과 해석:** 소비율이 70~80% 수준을 유지하면 건강한 경제 상태다. 소비율이 지속적으로 하락(예: 50% 이하)하면 아이템이 시장에 축적되고 있다는 신호로, 소비처 컨텐츠를 늘려야 할 수 있다.

---

#### 쿼리 2: 가장 많이 거래된 아이템 Top 20

```sql
SELECT
    i.item_name,
    i.item_grade,
    COUNT(*)                       AS trade_count,
    SUM(il.quantity)               AS total_quantity,
    ROUND(AVG(il.gold_amount), 0)  AS avg_price,
    i.base_price,
    ROUND(
        (AVG(il.gold_amount) - i.base_price) * 100.0 / i.base_price,
        2
    ) AS price_premium_pct
FROM item_log il
JOIN item_info i ON il.item_id = i.item_id
WHERE il.action_type IN ('trade_buy', 'trade_sell')
  AND il.log_time >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY i.item_name, i.item_grade, i.base_price
ORDER BY trade_count DESC
LIMIT 20;
```

**예상 결과:**

| item_name     | item_grade | trade_count | total_quantity | avg_price | base_price | price_premium_pct |
|---------------|------------|-------------|----------------|-----------|------------|-------------------|
| 강화석 +5     | 일반       | 420,100     | 890,200        | 1,240     | 1,000      | 24.00             |
| 전설 검 파편  | 전설       | 18,200      | 22,400         | 95,400    | 50,000     | 90.80             |
| 회복 포션(대) | 일반       | 380,200     | 1,240,000      | 480       | 500        | -4.00             |

**결과 해석:** price_premium_pct가 높은 아이템은 수요에 비해 공급이 부족한 상태다. 특히 전설 등급 파편의 90% 프리미엄은 드롭률 조정을 검토할 근거가 된다.

---

#### 쿼리 3: 아이템 가격 추이 — 인플레이션 감지

특정 아이템의 주간 평균 가격 변화를 추적한다.

```sql
SELECT
    DATE_TRUNC('week', il.log_time)   AS week_start,
    i.item_name,
    ROUND(AVG(il.gold_amount), 0)     AS avg_weekly_price,
    ROUND(
        (AVG(il.gold_amount) - LAG(AVG(il.gold_amount)) OVER (
            PARTITION BY i.item_name
            ORDER BY DATE_TRUNC('week', il.log_time)
        )) * 100.0 /
        NULLIF(LAG(AVG(il.gold_amount)) OVER (
            PARTITION BY i.item_name
            ORDER BY DATE_TRUNC('week', il.log_time)
        ), 0),
        2
    ) AS wow_change_pct
FROM item_log il
JOIN item_info i ON il.item_id = i.item_id
WHERE il.action_type IN ('trade_buy', 'trade_sell')
  AND i.item_name = '전설 검 파편'
GROUP BY week_start, i.item_name
ORDER BY week_start;
```

**예상 결과:**

| week_start | item_name    | avg_weekly_price | wow_change_pct |
|------------|--------------|------------------|----------------|
| 2026-02-02 | 전설 검 파편 | 50,200           | NULL           |
| 2026-02-09 | 전설 검 파편 | 58,100           | 15.74          |
| 2026-02-16 | 전설 검 파편 | 72,400           | 24.61          |
| 2026-02-23 | 전설 검 파편 | 95,400           | 31.77          |

**결과 해석:** 3주 연속 20% 이상의 가격 상승은 인플레이션 경고 신호다. 드롭률 증가 이벤트나 제작 레시피 추가 등 공급 확대 방안을 신속히 검토해야 한다.

---

#### 쿼리 4: 골드 유입/유출 밸런스

```sql
SELECT
    DATE_TRUNC('day', log_time)      AS day,
    SUM(CASE WHEN action_type IN ('acquire', 'trade_sell') THEN gold_amount ELSE 0 END)
                                     AS gold_inflow,
    SUM(CASE WHEN action_type IN ('consume', 'trade_buy')  THEN gold_amount ELSE 0 END)
                                     AS gold_outflow,
    SUM(CASE WHEN action_type IN ('acquire', 'trade_sell') THEN gold_amount ELSE 0 END) -
    SUM(CASE WHEN action_type IN ('consume', 'trade_buy')  THEN gold_amount ELSE 0 END)
                                     AS net_gold
FROM item_log
GROUP BY day
ORDER BY day;
```

net_gold가 지속적으로 양수(+)이면 골드가 시장에 쌓이는 인플레이션 압력, 지속적으로 음수(-)이면 골드가 부족해지는 디플레이션 압력이다.

---

## 8.4 퀘스트/콘텐츠 완료율 분석 — 병목 지점 찾기

### 퍼널 분석이란?

퍼널(Funnel) 분석은 사용자가 특정 목표에 도달하기까지 거치는 단계별 이탈률을 시각화하는 방법이다. 게임에서는 주로 메인 퀘스트의 단계별 완료율, 또는 튜토리얼의 단계별 통과율을 분석한다. 어느 단계에서 유저가 많이 이탈하는지 파악하면, 그 구간의 난이도나 설명이 문제라는 것을 알 수 있다.

분석에 사용할 테이블:
- `quest_log`: `log_id`, `user_id`, `quest_id`, `step`, `status`('start'/'complete'/'abandon'), `log_time`
- `quest_info`: `quest_id`, `quest_name`, `quest_type`, `step`, `step_name`

---

#### 쿼리 1: 퀘스트 단계별 완료율 퍼널

```sql
WITH quest_funnel AS (
    SELECT
        qi.step,
        qi.step_name,
        COUNT(DISTINCT CASE WHEN ql.status IN ('start', 'complete') THEN ql.user_id END)
            AS started,
        COUNT(DISTINCT CASE WHEN ql.status = 'complete' THEN ql.user_id END)
            AS completed
    FROM quest_log ql
    JOIN quest_info qi ON ql.quest_id = qi.quest_id AND ql.step = qi.step
    WHERE qi.quest_id = 1001  -- 메인 퀘스트 1001번
    GROUP BY qi.step, qi.step_name
)
SELECT
    step,
    step_name,
    started,
    completed,
    ROUND(completed * 100.0 / NULLIF(started, 0), 2) AS completion_rate_pct,
    ROUND(
        (started - completed) * 100.0 / NULLIF(started, 0),
        2
    ) AS dropout_rate_pct
FROM quest_funnel
ORDER BY step;
```

**예상 결과:**

| step | step_name           | started | completed | completion_rate_pct | dropout_rate_pct |
|------|---------------------|---------|-----------|---------------------|------------------|
| 1    | 마을 NPC 대화       | 50,000  | 49,800    | 99.60               | 0.40             |
| 2    | 슬라임 10마리 사냥  | 49,800  | 48,200    | 96.79               | 3.21             |
| 3    | 동굴 보스 처치      | 48,200  | 31,400    | 65.15               | **34.85**        |
| 4    | 아이템 제작 퀘스트  | 31,400  | 28,900    | 92.04               | 7.96             |
| 5    | 최종 관문 클리어    | 28,900  | 25,100    | 86.85               | 13.15            |

**결과 해석:** 3단계 동굴 보스 처치에서 34.85%라는 극히 높은 이탈률이 확인된다. 이 구간은 난이도가 갑자기 올라가거나 설명이 부족한 것이다. 개발팀은 이 구간에 가이드 메시지를 추가하거나 보스 스탯을 하향 조정하는 것을 고려해야 한다.

---

#### 쿼리 2: 가장 많이 포기된 퀘스트

```sql
SELECT
    qi.quest_name,
    COUNT(CASE WHEN ql.status = 'start'    THEN 1 END) AS start_count,
    COUNT(CASE WHEN ql.status = 'complete' THEN 1 END) AS complete_count,
    COUNT(CASE WHEN ql.status = 'abandon'  THEN 1 END) AS abandon_count,
    ROUND(
        COUNT(CASE WHEN ql.status = 'abandon' THEN 1 END) * 100.0 /
        NULLIF(COUNT(CASE WHEN ql.status = 'start' THEN 1 END), 0),
        2
    ) AS abandon_rate_pct
FROM quest_log ql
JOIN quest_info qi ON ql.quest_id = qi.quest_id
WHERE ql.log_time >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY qi.quest_name
ORDER BY abandon_rate_pct DESC
LIMIT 15;
```

**예상 결과:**

| quest_name       | start_count | complete_count | abandon_count | abandon_rate_pct |
|------------------|-------------|----------------|---------------|------------------|
| 용암 지대 생존전 | 12,400      | 3,800          | 8,200         | 66.13            |
| 시간 제한 레이드  | 9,800       | 4,100          | 5,500         | 56.12            |

---

#### 쿼리 3: 컨텐츠 평균 완료 시간

```sql
SELECT
    qi.quest_name,
    ROUND(AVG(
        EPOCH(complete_time) - EPOCH(start_time)
    ) / 60.0, 1)                  AS avg_completion_min,
    COUNT(*)                       AS sample_count
FROM (
    SELECT
        quest_id,
        user_id,
        MIN(CASE WHEN status = 'start'    THEN log_time END) AS start_time,
        MAX(CASE WHEN status = 'complete' THEN log_time END) AS complete_time
    FROM quest_log
    GROUP BY quest_id, user_id
    HAVING complete_time IS NOT NULL
) t
JOIN quest_info qi ON t.quest_id = qi.quest_id
GROUP BY qi.quest_name
ORDER BY avg_completion_min DESC;
```

---

## 8.5 결제 및 수익 분석 — ARPU, ARPPU, 결제 전환율

### 수익 지표의 의미

게임 서비스의 수익성을 판단하는 핵심 지표는 세 가지다.

- **ARPU (Average Revenue Per User)**: 전체 유저 기준 인당 평균 수익. 게임의 전반적인 수익 효율을 본다.
- **ARPPU (Average Revenue Per Paying User)**: 결제한 유저만을 대상으로 한 인당 평균 수익. 결제 유저의 지출 성향을 본다.
- **결제 전환율 (Conversion Rate)**: 전체 유저 중 실제로 결제한 유저의 비율.

세 지표를 함께 보면 수익 구조를 입체적으로 파악할 수 있다. 예를 들어 ARPU는 낮은데 ARPPU가 높다면, 소수의 고액 결제자(고래, Whale)가 수익을 지탱하고 있다는 의미다.

분석에 사용할 테이블:
- `payment_log`: `payment_id`, `user_id`, `product_id`, `amount_krw`, `payment_time`, `status`('success'/'fail'/'refund')
- `product_info`: `product_id`, `product_name`, `product_type`('crystal'/'pass'/'skin' 등)

---

#### 쿼리 1: 월별 ARPU, ARPPU, 전환율

```sql
WITH monthly_payment AS (
    SELECT
        DATE_TRUNC('month', payment_time)    AS pay_month,
        COUNT(DISTINCT user_id)              AS paying_users,
        SUM(amount_krw)                      AS total_revenue
    FROM payment_log
    WHERE status = 'success'
    GROUP BY pay_month
),
monthly_dau AS (
    SELECT
        DATE_TRUNC('month', login_at)        AS play_month,
        COUNT(DISTINCT user_id)              AS total_users
    FROM session_log
    GROUP BY play_month
)
SELECT
    mp.pay_month,
    md.total_users,
    mp.paying_users,
    mp.total_revenue,
    ROUND(mp.total_revenue / NULLIF(md.total_users,   0), 0) AS arpu,
    ROUND(mp.total_revenue / NULLIF(mp.paying_users,  0), 0) AS arppu,
    ROUND(mp.paying_users  * 100.0 / NULLIF(md.total_users, 0), 2) AS conversion_pct
FROM monthly_payment mp
JOIN monthly_dau md ON mp.pay_month = md.play_month
ORDER BY mp.pay_month;
```

**예상 결과:**

| pay_month  | total_users | paying_users | total_revenue  | arpu   | arppu   | conversion_pct |
|------------|-------------|--------------|----------------|--------|---------|----------------|
| 2026-01-01 | 52,400      | 3,148        | 157,400,000    | 3,004  | 49,968  | 6.01           |
| 2026-02-01 | 55,200      | 3,312        | 165,600,000    | 2,999  | 49,999  | 6.00           |

**결과 해석:** 전환율 6%, ARPPU 약 5만 원은 국내 모바일 게임 평균 수준이다. ARPU가 3,000원대라는 것은 전체 유저 중 과금 비율이 낮다는 의미이므로, 저가 상품을 늘려 라이트 결제자를 유도하는 전략을 고려할 수 있다.

---

#### 쿼리 2: 상품 유형별 수익 구성

```sql
SELECT
    pi.product_type,
    COUNT(*)                              AS purchase_count,
    SUM(pl.amount_krw)                    AS total_revenue,
    ROUND(
        SUM(pl.amount_krw) * 100.0 /
        SUM(SUM(pl.amount_krw)) OVER (),
        2
    ) AS revenue_share_pct,
    ROUND(AVG(pl.amount_krw), 0)          AS avg_purchase_amount
FROM payment_log pl
JOIN product_info pi ON pl.product_id = pi.product_id
WHERE pl.status = 'success'
  AND pl.payment_time >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY pi.product_type
ORDER BY total_revenue DESC;
```

**예상 결과:**

| product_type | purchase_count | total_revenue | revenue_share_pct | avg_purchase_amount |
|--------------|----------------|---------------|-------------------|---------------------|
| crystal      | 8,200          | 98,400,000    | 62.50             | 12,000              |
| pass         | 3,100          | 46,500,000    | 29.55             | 15,000              |
| skin         | 420            | 12,600,000    | 8.00              | 30,000              |

---

#### 쿼리 3: 결제 코호트 분석 — 첫 결제 후 재구매율

```sql
WITH first_payment AS (
    SELECT
        user_id,
        MIN(DATE_TRUNC('month', payment_time)) AS first_pay_month
    FROM payment_log
    WHERE status = 'success'
    GROUP BY user_id
),
cohort_activity AS (
    SELECT
        fp.user_id,
        fp.first_pay_month,
        DATE_TRUNC('month', pl.payment_time) AS pay_month,
        DATEDIFF('month', fp.first_pay_month, DATE_TRUNC('month', pl.payment_time))
            AS months_since_first
    FROM first_payment fp
    JOIN payment_log pl ON fp.user_id = pl.user_id AND pl.status = 'success'
)
SELECT
    first_pay_month,
    months_since_first,
    COUNT(DISTINCT user_id) AS retained_payers
FROM cohort_activity
GROUP BY first_pay_month, months_since_first
ORDER BY first_pay_month, months_since_first;
```

이 쿼리의 결과를 코호트 표로 시각화하면, 첫 결제 후 N개월 뒤에도 결제를 유지하는 비율을 한눈에 볼 수 있다. 이 수치가 급격히 떨어지면 구독형 패스 상품의 가치를 높여 재구매를 유도해야 한다.

---

#### 쿼리 4: 환불율 모니터링

```sql
SELECT
    DATE_TRUNC('month', payment_time) AS pay_month,
    SUM(CASE WHEN status = 'success' THEN amount_krw ELSE 0 END) AS gross_revenue,
    SUM(CASE WHEN status = 'refund'  THEN amount_krw ELSE 0 END) AS refund_amount,
    ROUND(
        SUM(CASE WHEN status = 'refund'  THEN amount_krw ELSE 0 END) * 100.0 /
        NULLIF(SUM(CASE WHEN status = 'success' THEN amount_krw ELSE 0 END), 0),
        2
    ) AS refund_rate_pct
FROM payment_log
GROUP BY pay_month
ORDER BY pay_month;
```

**결과 해석:** 환불율이 지속적으로 상승하면 상품 가치에 대한 유저 불만이 높아지고 있다는 신호다. 앱스토어 정책상 환불율이 일정 수준을 넘으면 계정 제재를 받을 수 있으므로 반드시 모니터링해야 한다.

---

### C# 코드 예시: 월별 수익 지표 출력

```csharp
// code/ch08/Ex02_Revenue/Program.cs
using DuckDB.NET.Data;

var connectionString = "Data Source=game_logs.db";
using var connection = new DuckDBConnection(connectionString);
connection.Open();

var sql = """
    WITH monthly_payment AS (
        SELECT
            DATE_TRUNC('month', payment_time) AS pay_month,
            COUNT(DISTINCT user_id)           AS paying_users,
            SUM(amount_krw)                   AS total_revenue
        FROM payment_log
        WHERE status = 'success'
        GROUP BY pay_month
    ),
    monthly_dau AS (
        SELECT
            DATE_TRUNC('month', login_at)     AS play_month,
            COUNT(DISTINCT user_id)           AS total_users
        FROM session_log
        GROUP BY play_month
    )
    SELECT
        mp.pay_month,
        md.total_users,
        mp.paying_users,
        mp.total_revenue,
        ROUND(mp.total_revenue / NULLIF(md.total_users,  0), 0) AS arpu,
        ROUND(mp.total_revenue / NULLIF(mp.paying_users, 0), 0) AS arppu,
        ROUND(mp.paying_users * 100.0 / NULLIF(md.total_users, 0), 2) AS conversion_pct
    FROM monthly_payment mp
    JOIN monthly_dau md ON mp.pay_month = md.play_month
    ORDER BY mp.pay_month DESC
    LIMIT 6
    """;

using var command = connection.CreateCommand();
command.CommandText = sql;
using var reader = command.ExecuteReader();

Console.WriteLine($"{"월",-12} {"총유저",8} {"결제유저",8} {"수익(원)",14} {"ARPU",8} {"ARPPU",8} {"전환율",8}");
Console.WriteLine(new string('-', 72));

while (reader.Read())
{
    var month      = reader.GetDateTime(0).ToString("yyyy-MM");
    var totalUsers = reader.GetInt64(1);
    var payUsers   = reader.GetInt64(2);
    var revenue    = reader.GetInt64(3);
    var arpu       = reader.GetInt64(4);
    var arppu      = reader.GetInt64(5);
    var conversion = reader.GetDouble(6);

    Console.WriteLine(
        $"{month,-12} {totalUsers,8:N0} {payUsers,8:N0} {revenue,14:N0} " +
        $"{arpu,8:N0} {arppu,8:N0} {conversion,7:F2}%");
}
```

---

## 8.6 이상 탐지(Anti-cheat 지원) — 비정상 패턴 SQL로 찾기

### 데이터 기반 어뷰징 탐지
게임 서버에서 모든 부정 행위를 실시간으로 막는 것은 어렵다. 그러나 DuckDB를 사용하면 수집된 로그에서 통계적으로 비정상적인 패턴을 빠르게 찾아낼 수 있다. 이상 탐지는 완벽한 판단을 내리는 것이 아니라, **의심 대상을 추려 수동 검토 범위를 줄이는** 작업이다.

주요 탐지 대상:
- 시간당 비정상적으로 많은 골드 획득
- 하루에 너무 많은 아이템 획득
- 불가능한 속도의 이동 (텔레포트 핵)
- 연속 크리티컬 또는 100% 회피 패턴

---

#### 쿼리 1: 시간당 골드 획득 이상 탐지
정상 유저의 골드 획득량 상위 99.9%를 넘는 유저를 추출한다.

```sql
WITH hourly_gold AS (
    SELECT
        user_id,
        DATE_TRUNC('hour', log_time) AS log_hour,
        SUM(gold_amount)             AS gold_per_hour
    FROM item_log
    WHERE action_type = 'acquire'
      AND log_time >= CURRENT_DATE - INTERVAL '3 days'
    GROUP BY user_id, log_hour
),
threshold AS (
    SELECT PERCENTILE_CONT(0.999) WITHIN GROUP (ORDER BY gold_per_hour)
        AS p999_gold
    FROM hourly_gold
)
SELECT
    hg.user_id,
    hg.log_hour,
    hg.gold_per_hour,
    t.p999_gold,
    ROUND(hg.gold_per_hour * 1.0 / NULLIF(t.p999_gold, 0), 2) AS ratio_vs_p999
FROM hourly_gold hg
CROSS JOIN threshold t
WHERE hg.gold_per_hour > t.p999_gold
ORDER BY hg.gold_per_hour DESC;
```

**예상 결과:**

| user_id | log_hour            | gold_per_hour | p999_gold | ratio_vs_p999 |
|---------|---------------------|---------------|-----------|---------------|
| 99001   | 2026-03-25 02:00:00 | 4,820,000     | 150,000   | 32.13         |
| 99042   | 2026-03-25 03:00:00 | 3,100,000     | 150,000   | 20.67         |

**결과 해석:** ratio_vs_p999가 10 이상이면 강한 이상 신호다. 새벽 2~3시에 이런 패턴이 나타나면 자동화 프로그램(봇) 사용을 강하게 의심할 수 있다.

---

#### 쿼리 2: 하루 아이템 획득 이상 탐지

```sql
WITH daily_item AS (
    SELECT
        user_id,
        DATE_TRUNC('day', log_time) AS log_day,
        COUNT(*)                    AS item_acquire_count
    FROM item_log
    WHERE action_type = 'acquire'
      AND log_time >= CURRENT_DATE - INTERVAL '7 days'
    GROUP BY user_id, log_day
),
stats AS (
    SELECT
        AVG(item_acquire_count)    AS avg_count,
        STDDEV(item_acquire_count) AS std_count
    FROM daily_item
)
SELECT
    di.user_id,
    di.log_day,
    di.item_acquire_count,
    s.avg_count,
    ROUND((di.item_acquire_count - s.avg_count) / NULLIF(s.std_count, 0), 2)
        AS z_score
FROM daily_item di
CROSS JOIN stats s
WHERE (di.item_acquire_count - s.avg_count) / NULLIF(s.std_count, 0) > 5.0
ORDER BY z_score DESC;
```

**결과 해석:** z-score가 5 이상이면 통계적으로 정상 분포에서 극단적으로 벗어난 값이다. z-score 3 이상을 1차 의심 대상, 5 이상을 즉시 검토 대상으로 설정하는 것이 일반적이다.

---

#### 쿼리 3: 불가능한 이동 속도 탐지 (텔레포트 핵)

이동 로그에서 연속된 두 위치 간 이동 속도가 게임에서 허용하는 최대 속도를 초과하는 경우를 찾는다.

```sql
WITH move_sequence AS (
    SELECT
        user_id,
        pos_x,
        pos_y,
        log_time,
        LAG(pos_x)    OVER (PARTITION BY user_id ORDER BY log_time) AS prev_x,
        LAG(pos_y)    OVER (PARTITION BY user_id ORDER BY log_time) AS prev_y,
        LAG(log_time) OVER (PARTITION BY user_id ORDER BY log_time) AS prev_time
    FROM movement_log
    WHERE log_time >= CURRENT_DATE - INTERVAL '1 day'
),
speed_calc AS (
    SELECT
        user_id,
        log_time,
        SQRT(POWER(pos_x - prev_x, 2) + POWER(pos_y - prev_y, 2)) AS distance,
        EPOCH(log_time) - EPOCH(prev_time)                          AS elapsed_sec,
        SQRT(POWER(pos_x - prev_x, 2) + POWER(pos_y - prev_y, 2))
            / NULLIF(EPOCH(log_time) - EPOCH(prev_time), 0)         AS speed_per_sec
    FROM move_sequence
    WHERE prev_x IS NOT NULL
)
SELECT
    user_id,
    log_time,
    ROUND(distance, 2)       AS distance,
    elapsed_sec,
    ROUND(speed_per_sec, 2)  AS speed_per_sec
FROM speed_calc
WHERE speed_per_sec > 50.0   -- 최대 허용 속도: 초당 50 유닛
ORDER BY speed_per_sec DESC
LIMIT 100;
```

**예상 결과:**

| user_id | log_time            | distance  | elapsed_sec | speed_per_sec |
|---------|---------------------|-----------|-------------|---------------|
| 77301   | 2026-03-27 23:12:05 | 8,420.00  | 1           | 8,420.00      |
| 77301   | 2026-03-27 23:14:22 | 6,800.00  | 1           | 6,800.00      |

**결과 해석:** 1초 만에 8,420 유닛 이동은 물리적으로 불가능하다. 이 유저에게는 즉시 이동 기록 상세 조회와 제재 검토가 필요하다.

---

#### 쿼리 4: 비정상적 크리티컬 패턴 탐지

특정 유저의 크리티컬 발동률이 직업 평균의 3배 이상인 경우를 찾는다.

```sql
WITH user_crit AS (
    SELECT
        c.attacker_id,
        p.job_class,
        COUNT(*)                              AS total_hits,
        SUM(c.is_critical::INT)               AS crit_hits,
        ROUND(AVG(c.is_critical::INT) * 100, 2) AS crit_rate_pct
    FROM combat_log c
    JOIN player_info p ON c.attacker_id = p.user_id
    WHERE c.log_time >= CURRENT_DATE - INTERVAL '3 days'
    GROUP BY c.attacker_id, p.job_class
    HAVING COUNT(*) >= 500    -- 최소 500회 이상 공격한 유저만 집계
),
class_avg AS (
    SELECT
        job_class,
        AVG(crit_rate_pct) AS avg_crit_rate
    FROM user_crit
    GROUP BY job_class
)
SELECT
    uc.attacker_id,
    uc.job_class,
    uc.total_hits,
    uc.crit_rate_pct,
    ca.avg_crit_rate,
    ROUND(uc.crit_rate_pct / NULLIF(ca.avg_crit_rate, 0), 2) AS ratio_vs_avg
FROM user_crit uc
JOIN class_avg ca ON uc.job_class = ca.job_class
WHERE uc.crit_rate_pct > ca.avg_crit_rate * 3
ORDER BY ratio_vs_avg DESC;
```

**결과 해석:** 같은 직업 평균의 3배 이상 크리티컬이 나온다면 전투 확률 조작을 의심해야 한다. 물론 특정 버프 아이템이나 스킬 조합으로 크리티컬 확률이 높아질 수 있으므로, 해당 유저의 장비와 버프 상태를 추가로 확인해야 한다.

---

### C# 코드 예시: 이상 탐지 결과 자동 알림

```csharp
// code/ch08/Ex03_AnomalyDetect/Program.cs
using DuckDB.NET.Data;

var connectionString = "Data Source=game_logs.db";
using var connection = new DuckDBConnection(connectionString);
connection.Open();

// 비정상 골드 획득 유저 조회
var sql = """
    WITH hourly_gold AS (
        SELECT
            user_id,
            DATE_TRUNC('hour', log_time) AS log_hour,
            SUM(gold_amount)             AS gold_per_hour
        FROM item_log
        WHERE action_type = 'acquire'
          AND log_time >= NOW() - INTERVAL '24 hours'
        GROUP BY user_id, log_hour
    ),
    threshold AS (
        SELECT PERCENTILE_CONT(0.999) WITHIN GROUP (ORDER BY gold_per_hour)
            AS p999_gold
        FROM hourly_gold
    )
    SELECT
        hg.user_id,
        hg.log_hour,
        hg.gold_per_hour,
        ROUND(hg.gold_per_hour * 1.0 / NULLIF(t.p999_gold, 0), 2) AS ratio
    FROM hourly_gold hg
    CROSS JOIN threshold t
    WHERE hg.gold_per_hour > t.p999_gold
    ORDER BY hg.gold_per_hour DESC
    """;

using var command = connection.CreateCommand();
command.CommandText = sql;
using var reader = command.ExecuteReader();

var suspiciousUsers = new List<(long UserId, DateTime Hour, long Gold, double Ratio)>();

while (reader.Read())
{
    suspiciousUsers.Add((
        reader.GetInt64(0),
        reader.GetDateTime(1),
        reader.GetInt64(2),
        reader.GetDouble(3)
    ));
}

if (suspiciousUsers.Count == 0)
{
    Console.WriteLine("[정상] 비정상 골드 획득 유저 없음.");
    return;
}

Console.WriteLine($"[경고] 비정상 골드 획득 의심 유저 {suspiciousUsers.Count}명 발견!");
Console.WriteLine();
Console.WriteLine($"{"유저ID",10} {"시간",20} {"시간당 골드",14} {"배율",8}");
Console.WriteLine(new string('-', 56));

foreach (var (userId, hour, gold, ratio) in suspiciousUsers)
{
    var alertLevel = ratio > 20.0 ? "[즉시검토]" : "[주의]    ";
    Console.WriteLine(
        $"{alertLevel} {userId,8} {hour:yyyy-MM-dd HH:mm} {gold,12:N0} {ratio,7:F2}x");
}
```

---

## 이 장의 핵심 정리

이 장에서는 온라인 게임 서비스에서 실제로 사용하는 6가지 분석 영역을 DuckDB SQL 쿼리와 C# 코드로 구현했다. 각 영역의 핵심을 정리하면 다음과 같다.

| 분석 영역 | 핵심 지표 | 주요 SQL 기법 |
|-----------|-----------|--------------|
| 플레이어 행동 | DAU, MAU, 세션 시간, 이탈 유저 | `DATE_TRUNC`, `DATEDIFF`, `COUNT(DISTINCT)` |
| 전투 밸런스 | 직업별 딜량, 사망률, 스킬 콤보 | `PERCENTILE_CONT`, `LEAD`, 윈도우 함수 |
| 아이템 경제 | 획득/소비 비율, 가격 추이 | `LAG`, 인플레이션 감지 |
| 퀘스트 완료율 | 퍼널 분석, 이탈 구간 | `CASE WHEN`, 코호트 |
| 결제/수익 | ARPU, ARPPU, 전환율 | 집계 + `JOIN` |
| 이상 탐지 | 봇, 핵 의심 유저 | `PERCENTILE_CONT`, z-score, `LAG` |

데이터 분석의 가치는 쿼리 자체가 아니라, 그 결과를 실제 게임 개선에 반영하는 것에 있다. 이탈률이 높은 퀘스트 구간을 수정하고, 과도하게 강한 직업을 패치하고, 봇 유저를 제재하는 것 — 이 모든 의사결정의 근거를 DuckDB로 만들어낼 수 있다.

---

## 다음 장 예고
8장에서는 실무 분석 쿼리를 통해 게임 데이터에서 의미 있는 인사이트를 뽑아내는 방법을 배웠다. 9장에서는 한 단계 더 나아가 **통계 분석 실전**을 다룬다. 단순 집계를 넘어서 회귀 분석, 상관관계 분석, 시계열 예측 등 DuckDB에서 활용할 수 있는 고급 통계 기법을 소개한다. 데이터가 말하는 패턴을 수식으로 표현하고, 미래의 트렌드를 예측하는 방법을 살펴볼 것이다.  