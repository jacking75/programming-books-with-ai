// code/ch08/Ex03_AnomalyDetect/Program.cs
// 8.6 이상 탐지(Anti-cheat 지원) — 봇, 핵, 비정상 패턴 감지
using DuckDB.NET.Data;

using var connection = new DuckDBConnection("Data Source=:memory:");
connection.Open();

// ── 스키마 생성 ────────────────────────────────────────────────────────────────
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        CREATE TABLE players (
            user_id   INTEGER PRIMARY KEY,
            nickname  VARCHAR,
            job_class VARCHAR,
            level     INTEGER
        );

        -- 아이템 로그 (골드 획득/소비 포함)
        CREATE TABLE item_log (
            log_id      INTEGER,
            user_id     INTEGER,
            item_id     INTEGER,
            action_type VARCHAR,    -- acquire / consume / trade_buy / trade_sell
            quantity    INTEGER,
            gold_amount BIGINT,
            log_time    TIMESTAMP
        );

        -- 전투 로그 (크리티컬 탐지용)
        CREATE TABLE combat_log (
            log_id      INTEGER,
            attacker_id INTEGER,
            defender_id INTEGER,
            skill_id    INTEGER,
            damage      INTEGER,
            is_critical BOOLEAN,
            map_id      INTEGER,
            log_time    TIMESTAMP
        );

        -- 이동 로그 (텔레포트 핵 탐지용)
        CREATE TABLE movement_log (
            log_id   INTEGER,
            user_id  INTEGER,
            pos_x    DOUBLE,
            pos_y    DOUBLE,
            map_id   INTEGER,
            log_time TIMESTAMP
        );
        """;
    cmd.ExecuteNonQuery();
}

// ── 플레이어 데이터 (20명 + 봇/핵 의심 유저 2명) ──────────────────────────────
var players = new (int Id, string Nick, string Job, int Lv)[]
{
    (1,  "철의기사",   "전사",   45),
    (2,  "불꽃마법사", "마법사", 52),
    (3,  "숲의정령",   "궁수",   38),
    (4,  "빛의사제",   "성직자", 41),
    (5,  "폭풍검사",   "전사",   60),
    (6,  "얼음마녀",   "마법사", 55),
    (7,  "그림자궁수", "궁수",   33),
    (8,  "치유자",     "성직자", 48),
    (9,  "붉은용사",   "전사",   27),
    (10, "번개술사",   "마법사", 50),
    (11, "바람사냥꾼", "궁수",   42),
    (12, "달빛사제",   "성직자", 35),
    (13, "강철기사",   "전사",   58),
    (14, "화염법사",   "마법사", 47),
    (15, "독화살",     "궁수",   30),
    (16, "신성기사",   "성직자", 63),
    (17, "대지전사",   "전사",   22),
    (18, "시간마법사", "마법사", 44),
    (19, "독수리눈",   "궁수",   39),
    (20, "부활사제",   "성직자", 57),
    // 봇/핵 의심 유저
    (99001, "GOD모드",   "전사",   99),
    (99042, "빠른발",    "궁수",   99),
};

using (var cmd = connection.CreateCommand())
{
    foreach (var (id, nick, job, lv) in players)
    {
        cmd.CommandText = $"INSERT INTO players VALUES ({id}, '{nick}', '{job}', {lv});";
        cmd.ExecuteNonQuery();
    }
}

// ── 아이템 로그 생성 ───────────────────────────────────────────────────────────
// 정상 유저: 시간당 골드 500~5,000
// 봇 의심 유저 99001, 99042: 특정 시간대 비정상 골드 획득 (수백만)
var rand = new Random(42);
int logId = 1;

// 기준일: 2026-03-25 ~ 2026-03-28
var baseDate = new DateTime(2026, 3, 25);

using (var cmd = connection.CreateCommand())
{
    // 정상 유저 아이템 로그 (3일 × 24시간 × 20명, 약 1,440건)
    for (int day = 0; day < 4; day++)
    {
        for (int hour = 0; hour < 24; hour++)
        {
            foreach (var (uid, _, _, _) in players)
            {
                if (uid >= 99000) continue; // 봇 유저는 별도 처리
                if (rand.Next(0, 3) == 0) continue; // 33% 미활동

                int goldAmount = rand.Next(500, 5001);
                int qty = rand.Next(1, 6);
                var logTime = baseDate.AddDays(day).AddHours(hour).AddMinutes(rand.Next(0, 60));

                cmd.CommandText = $"INSERT INTO item_log VALUES ({logId++}, {uid}, {rand.Next(1, 50)}, 'acquire', {qty}, {goldAmount}, '{logTime:yyyy-MM-dd HH:mm:ss}');";
                cmd.ExecuteNonQuery();
            }
        }
    }

    // 봇 의심 유저 99001 — 2026-03-25 새벽 02:00~04:00 비정상 골드
    for (int hour = 2; hour <= 4; hour++)
    {
        for (int repeat = 0; repeat < 8; repeat++)
        {
            var logTime = new DateTime(2026, 3, 25, hour, rand.Next(0, 60), 0);
            long goldAmount = rand.Next(450_000, 600_001); // 한 번에 45만~60만 골드
            cmd.CommandText = $"INSERT INTO item_log VALUES ({logId++}, 99001, 10, 'acquire', 1, {goldAmount}, '{logTime:yyyy-MM-dd HH:mm:ss}');";
            cmd.ExecuteNonQuery();
        }
    }

    // 봇 의심 유저 99042 — 2026-03-25 새벽 03:00 비정상 골드
    for (int repeat = 0; repeat < 6; repeat++)
    {
        var logTime = new DateTime(2026, 3, 25, 3, rand.Next(0, 60), 0);
        long goldAmount = rand.Next(300_000, 400_001);
        cmd.CommandText = $"INSERT INTO item_log VALUES ({logId++}, 99042, 10, 'acquire', 1, {goldAmount}, '{logTime:yyyy-MM-dd HH:mm:ss}');";
        cmd.ExecuteNonQuery();
    }

    // 소비 로그 추가 (전체 유저)
    for (int day = 0; day < 4; day++)
    {
        foreach (var (uid, _, _, _) in players)
        {
            if (uid >= 99000) continue;
            int cnt = rand.Next(2, 8);
            for (int i = 0; i < cnt; i++)
            {
                var logTime = baseDate.AddDays(day).AddHours(rand.Next(9, 23)).AddMinutes(rand.Next(0, 60));
                int goldAmount = rand.Next(100, 2001);
                cmd.CommandText = $"INSERT INTO item_log VALUES ({logId++}, {uid}, {rand.Next(1, 50)}, 'consume', 1, {goldAmount}, '{logTime:yyyy-MM-dd HH:mm:ss}');";
                cmd.ExecuteNonQuery();
            }
        }
    }
}

// ── 전투 로그 생성 ─────────────────────────────────────────────────────────────
// 정상 유저: 크리티컬 10~20%
// 핵 의심 유저 99001: 크리티컬 85%+
logId = 1;
int combatId = 1;

using (var cmd = connection.CreateCommand())
{
    // 정상 전투 로그 (200건 이상)
    for (int i = 0; i < 250; i++)
    {
        var (uid, _, job, _) = players[rand.Next(0, 20)];
        int targetId = rand.Next(1, 100);
        int skillId = rand.Next(1, 10);
        int damage = rand.Next(300, 5001);
        // 직업별 크리티컬 확률
        bool isCrit = job switch
        {
            "전사"   => rand.NextDouble() < 0.12,
            "마법사" => rand.NextDouble() < 0.10,
            "궁수"   => rand.NextDouble() < 0.20,
            "성직자" => rand.NextDouble() < 0.05,
            _        => rand.NextDouble() < 0.12,
        };
        if (isCrit) damage = (int)(damage * 1.8);
        var logTime = baseDate.AddDays(rand.Next(0, 4)).AddHours(rand.Next(9, 23)).AddMinutes(rand.Next(0, 60));
        cmd.CommandText = $"INSERT INTO combat_log VALUES ({combatId++}, {uid}, {targetId}, {skillId}, {damage}, {(isCrit ? "true" : "false")}, 1, '{logTime:yyyy-MM-dd HH:mm:ss}');";
        cmd.ExecuteNonQuery();
    }

    // 핵 의심 유저 99001 전투 로그 (크리티컬 90%+, 600건)
    for (int i = 0; i < 600; i++)
    {
        int targetId = rand.Next(1, 100);
        int skillId = rand.Next(1, 5);
        int damage = rand.Next(5000, 15001);
        bool isCrit = rand.NextDouble() < 0.92; // 92% 크리티컬
        if (isCrit) damage = (int)(damage * 1.8);
        var logTime = baseDate.AddDays(rand.Next(0, 4)).AddHours(rand.Next(9, 23)).AddMinutes(rand.Next(0, 60));
        cmd.CommandText = $"INSERT INTO combat_log VALUES ({combatId++}, 99001, {targetId}, {skillId}, {damage}, {(isCrit ? "true" : "false")}, 1, '{logTime:yyyy-MM-dd HH:mm:ss}');";
        cmd.ExecuteNonQuery();
    }
}

// ── 이동 로그 생성 ─────────────────────────────────────────────────────────────
// 정상 유저: 이동 속도 초당 10~30 유닛
// 핵 의심 유저 99042: 순간이동 (초당 8,000+ 유닛)
int moveId = 1;

using (var cmd = connection.CreateCommand())
{
    // 정상 이동 로그 (20명 × 20개 좌표 = 400건)
    foreach (var (uid, _, _, _) in players)
    {
        if (uid >= 99000) continue;
        double x = rand.NextDouble() * 1000;
        double y = rand.NextDouble() * 1000;
        var t = new DateTime(2026, 3, 27, rand.Next(9, 22), 0, 0);

        for (int step = 0; step < 20; step++)
        {
            // 정상 이동: 초당 10~30 유닛, 1~3초 간격
            double dx = (rand.NextDouble() - 0.5) * 60; // 최대 30 유닛/초 × 2초
            double dy = (rand.NextDouble() - 0.5) * 60;
            x = Math.Max(0, Math.Min(10000, x + dx));
            y = Math.Max(0, Math.Min(10000, y + dy));
            t = t.AddSeconds(rand.Next(1, 4));

            cmd.CommandText = $"INSERT INTO movement_log VALUES ({moveId++}, {uid}, {x:F2}, {y:F2}, 1, '{t:yyyy-MM-dd HH:mm:ss}');";
            cmd.ExecuteNonQuery();
        }
    }

    // 핵 의심 유저 99042 이동 로그 — 텔레포트
    var hackMoves = new[]
    {
        (500.0,   500.0,  new DateTime(2026, 3, 27, 23, 12,  0)),
        (8920.0, 7430.0,  new DateTime(2026, 3, 27, 23, 12,  1)),  // 1초 만에 8,420 유닛
        (8921.0, 7431.0,  new DateTime(2026, 3, 27, 23, 12,  2)),
        (2100.0,  300.0,  new DateTime(2026, 3, 27, 23, 14, 22)),  // 또 순간이동
        (8900.0, 7100.0,  new DateTime(2026, 3, 27, 23, 14, 23)),
    };

    foreach (var (hx, hy, ht) in hackMoves)
    {
        cmd.CommandText = $"INSERT INTO movement_log VALUES ({moveId++}, 99042, {hx:F2}, {hy:F2}, 1, '{ht:yyyy-MM-dd HH:mm:ss}');";
        cmd.ExecuteNonQuery();
    }
}

Console.WriteLine("샘플 데이터 생성 완료.");
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 탐지 1: 시간당 골드 획득 이상 탐지 (99.9 퍼센타일 초과)
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [탐지 1] 시간당 골드 획득 이상 탐지 ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH hourly_gold AS (
            SELECT
                user_id,
                DATE_TRUNC('hour', log_time) AS log_hour,
                SUM(gold_amount)             AS gold_per_hour
            FROM item_log
            WHERE action_type = 'acquire'
              AND log_time >= '2026-03-25 00:00:00'
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
        """;

    using var reader = cmd.ExecuteReader();
    var suspiciousUsers = new List<(long UserId, DateTime Hour, long Gold, double P999, double Ratio)>();

    while (reader.Read())
    {
        suspiciousUsers.Add((
            reader.GetInt64(0),
            reader.GetDateTime(1),
            reader.GetInt64(2),
            reader.GetDouble(3),
            reader.GetDouble(4)
        ));
    }

    if (suspiciousUsers.Count == 0)
    {
        Console.WriteLine("[정상] 비정상 골드 획득 유저 없음.");
    }
    else
    {
        Console.WriteLine($"[경고] 비정상 골드 획득 의심 유저 {suspiciousUsers.Count}건 발견!");
        Console.WriteLine();
        Console.WriteLine($"{"유저ID",10} {"시간",20} {"시간당골드",14} {"P99.9기준",12} {"배율",8}");
        Console.WriteLine(new string('-', 68));

        foreach (var (userId, hour, gold, p999, ratio) in suspiciousUsers)
        {
            var alertLevel = ratio > 20.0 ? "[즉시검토]" : "[주의]    ";
            Console.WriteLine(
                $"{alertLevel} {userId,8} {hour:yyyy-MM-dd HH:mm} {gold,12:N0} {(long)p999,12:N0} {ratio,7:F2}x");
        }
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 탐지 2: 하루 아이템 획득 z-score 이상 탐지
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [탐지 2] 일별 아이템 획득 z-score 이상 탐지 (z > 3.0) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH daily_item AS (
            SELECT
                user_id,
                CAST(DATE_TRUNC('day', log_time) AS DATE) AS log_day,
                COUNT(*)                                  AS item_acquire_count
            FROM item_log
            WHERE action_type = 'acquire'
              AND log_time >= '2026-03-25 00:00:00'
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
            ROUND(s.avg_count, 1)                                                  AS avg_count,
            ROUND((di.item_acquire_count - s.avg_count) / NULLIF(s.std_count, 0), 2) AS z_score
        FROM daily_item di
        CROSS JOIN stats s
        WHERE (di.item_acquire_count - s.avg_count) / NULLIF(s.std_count, 0) > 3.0
        ORDER BY z_score DESC;
        """;

    using var reader = cmd.ExecuteReader();
    bool hasResult = false;
    Console.WriteLine($"{"유저ID",10} {"날짜",-12} {"획득건수",10} {"평균",8} {"z-score",10}");
    Console.WriteLine(new string('-', 54));
    while (reader.Read())
    {
        hasResult = true;
        var uid = reader.GetInt64(0);
        var day = reader.GetDateTime(1).ToString("yyyy-MM-dd");
        var cnt = reader.GetInt64(2);
        var avg = reader.GetDouble(3);
        var z = reader.GetDouble(4);
        Console.WriteLine($"{uid,10} {day,-12} {cnt,10:N0} {avg,8:F1} {z,10:F2}");
    }
    if (!hasResult) Console.WriteLine("이상 탐지 결과 없음.");
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 탐지 3: 텔레포트 핵 탐지 — 불가능한 이동 속도
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [탐지 3] 텔레포트 핵 탐지 (이동 속도 > 50 유닛/초) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
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
            WHERE log_time >= '2026-03-27 00:00:00'
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
            ROUND(distance, 2)      AS distance,
            elapsed_sec,
            ROUND(speed_per_sec, 2) AS speed_per_sec
        FROM speed_calc
        WHERE speed_per_sec > 50.0
        ORDER BY speed_per_sec DESC
        LIMIT 20;
        """;

    using var reader = cmd.ExecuteReader();
    bool hasResult = false;
    Console.WriteLine($"{"유저ID",10} {"시간",-22} {"이동거리",10} {"경과(초)",10} {"속도/초",10}");
    Console.WriteLine(new string('-', 66));
    while (reader.Read())
    {
        hasResult = true;
        var uid = reader.GetInt64(0);
        var t = reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss");
        var dist = reader.GetDouble(2);
        var elapsed = reader.GetInt64(3);
        var speed = reader.GetDouble(4);
        Console.WriteLine($"{uid,10} {t,-22} {dist,10:F1} {elapsed,10} {speed,10:F1}");
    }
    if (!hasResult) Console.WriteLine("이상 탐지 결과 없음.");
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 탐지 4: 비정상적 크리티컬 패턴 탐지 (직업 평균의 3배 이상)
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [탐지 4] 비정상 크리티컬 패턴 탐지 (직업 평균 × 3 초과) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH user_crit AS (
            SELECT
                c.attacker_id,
                p.job_class,
                COUNT(*)                                AS total_hits,
                SUM(c.is_critical::INT)                 AS crit_hits,
                ROUND(AVG(c.is_critical::INT) * 100, 2) AS crit_rate_pct
            FROM combat_log c
            JOIN players p ON c.attacker_id = p.user_id
            WHERE c.log_time >= '2026-03-25 00:00:00'
            GROUP BY c.attacker_id, p.job_class
            HAVING COUNT(*) >= 50
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
            ROUND(ca.avg_crit_rate, 2)                                   AS avg_crit_rate,
            ROUND(uc.crit_rate_pct / NULLIF(ca.avg_crit_rate, 0), 2)    AS ratio_vs_avg
        FROM user_crit uc
        JOIN class_avg ca ON uc.job_class = ca.job_class
        WHERE uc.crit_rate_pct > ca.avg_crit_rate * 3
        ORDER BY ratio_vs_avg DESC;
        """;

    using var reader = cmd.ExecuteReader();
    bool hasResult = false;
    Console.WriteLine($"{"유저ID",10} {"직업",-8} {"공격횟수",10} {"크리율",8} {"직업평균",10} {"배율",8}");
    Console.WriteLine(new string('-', 58));
    while (reader.Read())
    {
        hasResult = true;
        var uid = reader.GetInt64(0);
        var job = reader.GetString(1);
        var hits = reader.GetInt64(2);
        var critRate = reader.GetDouble(3);
        var avgRate = reader.GetDouble(4);
        var ratio = reader.GetDouble(5);
        Console.WriteLine($"{uid,10} {job,-8} {hits,10:N0} {critRate,7:F1}% {avgRate,9:F1}% {ratio,7:F2}x");
    }
    if (!hasResult) Console.WriteLine("이상 탐지 결과 없음.");
}

Console.WriteLine();
Console.WriteLine("이상 탐지 분석 완료.");
