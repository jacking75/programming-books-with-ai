// code/ch08/Ex01_DAU/Program.cs
// 8.1 플레이어 행동 분석 — DAU/MAU, 세션 시간, 이탈 지점
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
            level     INTEGER,
            join_date DATE
        );

        CREATE TABLE session_log (
            session_id  INTEGER,
            user_id     INTEGER,
            login_at    TIMESTAMP,
            logout_at   TIMESTAMP
        );
        """;
    cmd.ExecuteNonQuery();
}

// ── 플레이어 데이터 (20명) ─────────────────────────────────────────────────────
var players = new[]
{
    (1,  "철의기사",  "전사",   45),
    (2,  "불꽃마법사", "마법사", 52),
    (3,  "숲의정령",  "궁수",   38),
    (4,  "빛의사제",  "성직자", 41),
    (5,  "폭풍검사",  "전사",   60),
    (6,  "얼음마녀",  "마법사", 55),
    (7,  "그림자궁수", "궁수",  33),
    (8,  "치유자",   "성직자", 48),
    (9,  "붉은용사",  "전사",   27),
    (10, "번개술사",  "마법사", 50),
    (11, "바람사냥꾼", "궁수",  42),
    (12, "달빛사제",  "성직자", 35),
    (13, "강철기사",  "전사",   58),
    (14, "화염법사",  "마법사", 47),
    (15, "독화살",   "궁수",   30),
    (16, "신성기사",  "성직자", 63),
    (17, "대지전사",  "전사",   22),
    (18, "시간마법사", "마법사", 44),
    (19, "독수리눈",  "궁수",   39),
    (20, "부활사제",  "성직자", 57),
};

using (var cmd = connection.CreateCommand())
{
    foreach (var (id, nick, job, lv) in players)
    {
        cmd.CommandText = $"""
            INSERT INTO players VALUES ({id}, '{nick}', '{job}', {lv}, '2025-01-15');
            """;
        cmd.ExecuteNonQuery();
    }
}

// ── 세션 로그 생성 (7일치, 유저당 평균 3~5회 접속) ─────────────────────────────
// 기준일: 2026-03-22 ~ 2026-03-28
var baseDate = new DateTime(2026, 3, 22);
var rand = new Random(42);
var sessionId = 1;

using (var cmd = connection.CreateCommand())
{
    // 모든 유저 7일간 접속 기록 생성
    foreach (var (userId, _, _, _, _) in players)
    {
        for (int day = 0; day < 7; day++)
        {
            // 각 날짜마다 1~3회 접속 (일부는 미접속)
            int sessionsPerDay = rand.Next(0, 4); // 0~3회
            if (userId <= 15 && sessionsPerDay == 0) sessionsPerDay = 1; // 활성 유저는 최소 1회

            for (int s = 0; s < sessionsPerDay; s++)
            {
                int loginHour = rand.Next(9, 24);
                int loginMin = rand.Next(0, 60);
                int playMinutes = rand.Next(15, 120); // 15분~2시간

                var loginAt = baseDate.AddDays(day).AddHours(loginHour).AddMinutes(loginMin);
                var logoutAt = loginAt.AddMinutes(playMinutes);

                cmd.CommandText = $"""
                    INSERT INTO session_log VALUES (
                        {sessionId++},
                        {userId},
                        '{loginAt:yyyy-MM-dd HH:mm:ss}',
                        '{logoutAt:yyyy-MM-dd HH:mm:ss}'
                    );
                    """;
                cmd.ExecuteNonQuery();
            }
        }
    }

    // 이탈 위험 유저 추가 (마지막 접속이 오래된 유저)
    cmd.CommandText = $"""
        INSERT INTO session_log VALUES ({sessionId++}, 1, '2026-02-20 10:00:00', '2026-02-20 11:30:00');
        INSERT INTO session_log VALUES ({sessionId++}, 2, '2026-03-08 15:00:00', '2026-03-08 16:45:00');
        INSERT INTO session_log VALUES ({sessionId++}, 3, '2026-03-18 20:00:00', '2026-03-18 21:00:00');
        """;
    cmd.ExecuteNonQuery();
}

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 1: 날짜별 DAU
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 1] 날짜별 DAU (일별 순수 접속자 수) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT
            CAST(DATE_TRUNC('day', login_at) AS DATE) AS play_date,
            COUNT(DISTINCT user_id)                   AS dau
        FROM session_log
        WHERE login_at >= '2026-03-22'
        GROUP BY play_date
        ORDER BY play_date;
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"날짜",-15} {"DAU",6}");
    Console.WriteLine(new string('-', 22));
    while (reader.Read())
    {
        var date = reader.GetDateTime(0).ToString("yyyy-MM-dd");
        var dau = reader.GetInt64(1);
        Console.WriteLine($"{date,-15} {dau,6:N0}");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 2: MAU (월별 순수 접속자 수)
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 2] MAU (월별 순수 접속자 수) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT
            CAST(DATE_TRUNC('month', login_at) AS DATE) AS play_month,
            COUNT(DISTINCT user_id)                     AS mau
        FROM session_log
        GROUP BY play_month
        ORDER BY play_month;
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"월",-12} {"MAU",6}");
    Console.WriteLine(new string('-', 19));
    while (reader.Read())
    {
        var month = reader.GetDateTime(0).ToString("yyyy-MM");
        var mau = reader.GetInt64(1);
        Console.WriteLine($"{month,-12} {mau,6:N0}");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 3: DAU/MAU 스티키니스 비율
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 3] DAU/MAU 스티키니스 비율 ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH daily AS (
            SELECT
                CAST(DATE_TRUNC('day',   login_at) AS DATE) AS play_date,
                CAST(DATE_TRUNC('month', login_at) AS DATE) AS play_month,
                COUNT(DISTINCT user_id)                     AS dau
            FROM session_log
            WHERE login_at >= '2026-03-22'
            GROUP BY play_date, play_month
        ),
        monthly AS (
            SELECT
                CAST(DATE_TRUNC('month', login_at) AS DATE) AS play_month,
                COUNT(DISTINCT user_id)                     AS mau
            FROM session_log
            WHERE login_at >= '2026-03-01'
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
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"날짜",-15} {"DAU",5} {"MAU",5} {"스티키니스",12}");
    Console.WriteLine(new string('-', 40));
    while (reader.Read())
    {
        var date = reader.GetDateTime(0).ToString("yyyy-MM-dd");
        var dau = reader.GetInt64(1);
        var mau = reader.GetInt64(2);
        var sticky = reader.GetDouble(3);
        Console.WriteLine($"{date,-15} {dau,5:N0} {mau,5:N0} {sticky,10:F2}%");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 4: 세션 평균/중앙값 플레이 시간
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 4] 날짜별 세션 평균/중앙값 플레이 시간 ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT
            CAST(DATE_TRUNC('day', login_at) AS DATE)                   AS play_date,
            ROUND(AVG(
                EPOCH(logout_at) - EPOCH(login_at)
            ) / 60.0, 1)                                                AS avg_session_min,
            ROUND(PERCENTILE_CONT(0.5) WITHIN GROUP (
                ORDER BY EPOCH(logout_at) - EPOCH(login_at)
            ) / 60.0, 1)                                                AS median_session_min,
            COUNT(*)                                                    AS session_count
        FROM session_log
        WHERE logout_at IS NOT NULL
          AND login_at >= '2026-03-22'
        GROUP BY play_date
        ORDER BY play_date;
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"날짜",-15} {"평균(분)",10} {"중앙값(분)",12} {"세션수",8}");
    Console.WriteLine(new string('-', 48));
    while (reader.Read())
    {
        var date = reader.GetDateTime(0).ToString("yyyy-MM-dd");
        var avg = reader.GetDouble(1);
        var median = reader.GetDouble(2);
        var cnt = reader.GetInt64(3);
        Console.WriteLine($"{date,-15} {avg,10:F1} {median,12:F1} {cnt,8:N0}");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 5: 이탈 위험 유저 식별
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 5] 이탈 위험 유저 식별 (기준일: 2026-03-28) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH last_login AS (
            SELECT
                user_id,
                MAX(login_at) AS last_login_at
            FROM session_log
            GROUP BY user_id
        )
        SELECT
            l.user_id,
            p.nickname,
            p.job_class,
            CAST(l.last_login_at AS DATE)                                    AS last_login_date,
            DATEDIFF('day', l.last_login_at, TIMESTAMP '2026-03-28 00:00:00') AS days_since_login,
            CASE
                WHEN DATEDIFF('day', l.last_login_at, TIMESTAMP '2026-03-28 00:00:00') BETWEEN 7  AND 13 THEN '위험'
                WHEN DATEDIFF('day', l.last_login_at, TIMESTAMP '2026-03-28 00:00:00') BETWEEN 14 AND 29 THEN '이탈 임박'
                WHEN DATEDIFF('day', l.last_login_at, TIMESTAMP '2026-03-28 00:00:00') >= 30             THEN '이탈'
                ELSE '활성'
            END AS churn_status
        FROM last_login l
        JOIN players p ON l.user_id = p.user_id
        ORDER BY days_since_login DESC;
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"유저ID",6} {"닉네임",-12} {"직업",-8} {"마지막 접속",-14} {"미접속일",8} {"상태",-10}");
    Console.WriteLine(new string('-', 58));
    while (reader.Read())
    {
        var uid = reader.GetInt32(0);
        var nick = reader.GetString(1);
        var job = reader.GetString(2);
        var lastDate = reader.GetDateTime(3).ToString("yyyy-MM-dd");
        var days = reader.GetInt32(4);
        var status = reader.GetString(5);
        Console.WriteLine($"{uid,6} {nick,-12} {job,-8} {lastDate,-14} {days,8} {status,-10}");
    }
}

Console.WriteLine();
Console.WriteLine("분석 완료.");
