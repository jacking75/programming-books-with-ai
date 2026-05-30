// code/ch08/Ex02_Revenue/Program.cs
// 8.5 결제 및 수익 분석 — ARPU, ARPPU, 결제 전환율
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

        CREATE TABLE session_log (
            session_id INTEGER,
            user_id    INTEGER,
            login_at   TIMESTAMP,
            logout_at  TIMESTAMP
        );

        CREATE TABLE product_info (
            product_id   INTEGER PRIMARY KEY,
            product_name VARCHAR,
            product_type VARCHAR,   -- crystal / pass / skin
            price_krw    INTEGER
        );

        CREATE TABLE payment_log (
            payment_id   INTEGER PRIMARY KEY,
            user_id      INTEGER,
            product_id   INTEGER,
            amount_krw   INTEGER,
            payment_time TIMESTAMP,
            status       VARCHAR    -- success / fail / refund
        );
        """;
    cmd.ExecuteNonQuery();
}

// ── 플레이어 데이터 (20명) ─────────────────────────────────────────────────────
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
};

using (var cmd = connection.CreateCommand())
{
    foreach (var (id, nick, job, lv) in players)
    {
        cmd.CommandText = $"INSERT INTO players VALUES ({id}, '{nick}', '{job}', {lv});";
        cmd.ExecuteNonQuery();
    }
}

// ── 상품 정보 ─────────────────────────────────────────────────────────────────
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        INSERT INTO product_info VALUES
            (101, '크리스탈 60개',  'crystal', 1100),
            (102, '크리스탈 330개', 'crystal', 5500),
            (103, '크리스탈 660개', 'crystal', 11000),
            (104, '크리스탈 1980개','crystal', 33000),
            (105, '모험가 패스',    'pass',    9900),
            (106, '전설 패스',      'pass',    19900),
            (107, '전사 스킨 세트', 'skin',    29000),
            (108, '마법사 스킨 세트','skin',   29000),
            (109, '한정판 마운트',  'skin',    49000);
        """;
    cmd.ExecuteNonQuery();
}

// ── 세션 로그 (2개월치 접속 — 1월, 2월, 3월 일부) ─────────────────────────────
var rand = new Random(42);
int sessionId = 1;

using (var cmd = connection.CreateCommand())
{
    // 1월: 20명 모두 랜덤 접속
    for (int day = 1; day <= 31; day++)
    {
        foreach (var (uid, _, _, _) in players)
        {
            if (rand.Next(0, 3) == 0) continue; // 33% 미접속
            int loginHour = rand.Next(9, 23);
            int playMin = rand.Next(20, 90);
            var login = new DateTime(2026, 1, day, loginHour, rand.Next(0, 60), 0);
            var logout = login.AddMinutes(playMin);
            cmd.CommandText = $"INSERT INTO session_log VALUES ({sessionId++}, {uid}, '{login:yyyy-MM-dd HH:mm:ss}', '{logout:yyyy-MM-dd HH:mm:ss}');";
            cmd.ExecuteNonQuery();
        }
    }
    // 2월: 18명 (신규 2명 이탈 가정)
    for (int day = 1; day <= 28; day++)
    {
        foreach (var (uid, _, _, _) in players)
        {
            if (uid > 18 && day > 15) continue; // 2명은 2월 후반 이탈
            if (rand.Next(0, 3) == 0) continue;
            int loginHour = rand.Next(9, 23);
            int playMin = rand.Next(20, 90);
            var login = new DateTime(2026, 2, day, loginHour, rand.Next(0, 60), 0);
            var logout = login.AddMinutes(playMin);
            cmd.CommandText = $"INSERT INTO session_log VALUES ({sessionId++}, {uid}, '{login:yyyy-MM-dd HH:mm:ss}', '{logout:yyyy-MM-dd HH:mm:ss}');";
            cmd.ExecuteNonQuery();
        }
    }
    // 3월: 현재까지
    for (int day = 1; day <= 27; day++)
    {
        foreach (var (uid, _, _, _) in players)
        {
            if (uid > 16 && day > 20) continue;
            if (rand.Next(0, 3) == 0) continue;
            int loginHour = rand.Next(9, 23);
            int playMin = rand.Next(20, 90);
            var login = new DateTime(2026, 3, day, loginHour, rand.Next(0, 60), 0);
            var logout = login.AddMinutes(playMin);
            cmd.CommandText = $"INSERT INTO session_log VALUES ({sessionId++}, {uid}, '{login:yyyy-MM-dd HH:mm:ss}', '{logout:yyyy-MM-dd HH:mm:ss}');";
            cmd.ExecuteNonQuery();
        }
    }
}

// ── 결제 로그 (30건 이상) ──────────────────────────────────────────────────────
// 결제 유저: 1,2,3,5,6,10,13,14,16 (헤비: 5,6,13,16)
var productIds = new[] { 101, 102, 103, 104, 105, 106, 107, 108, 109 };
var productPrices = new Dictionary<int, int>
{
    {101, 1100}, {102, 5500}, {103, 11000}, {104, 33000},
    {105, 9900}, {106, 19900}, {107, 29000}, {108, 29000}, {109, 49000},
};

var payments = new List<(int PayId, int UserId, int ProdId, int Amount, DateTime PayTime, string Status)>();

int payId = 1;

// 1월 결제
var jan1Payers = new[] {(1, 102), (2, 103), (5, 104), (6, 103), (10, 102), (13, 104), (16, 106)};
foreach (var (uid, pid) in jan1Payers)
{
    var t = new DateTime(2026, 1, rand.Next(3, 28), rand.Next(10, 22), rand.Next(0, 60), 0);
    payments.Add((payId++, uid, pid, productPrices[pid], t, "success"));
}
// 1월 추가 결제 (헤비 유저 2건)
payments.Add((payId++, 5,  107, 29000, new DateTime(2026, 1, 20, 15, 30, 0), "success"));
payments.Add((payId++, 13, 108, 29000, new DateTime(2026, 1, 25, 18, 0,  0), "success"));

// 2월 결제
var feb1Payers = new[] {(1, 102), (2, 105), (3, 102), (5, 104), (6, 106), (10, 103), (13, 104), (14, 102), (16, 104)};
foreach (var (uid, pid) in feb1Payers)
{
    var t = new DateTime(2026, 2, rand.Next(2, 26), rand.Next(10, 22), rand.Next(0, 60), 0);
    payments.Add((payId++, uid, pid, productPrices[pid], t, "success"));
}
// 2월 환불 1건
payments.Add((payId++, 3, 101, 1100, new DateTime(2026, 2, 14, 10, 0, 0), "refund"));
// 2월 실패 1건
payments.Add((payId++, 7, 103, 11000, new DateTime(2026, 2, 18, 20, 0, 0), "fail"));

// 3월 결제 (지금까지)
var mar1Payers = new[] {(1, 103), (2, 106), (5, 109), (6, 104), (10, 105), (13, 108), (14, 102), (16, 104)};
foreach (var (uid, pid) in mar1Payers)
{
    var t = new DateTime(2026, 3, rand.Next(2, 25), rand.Next(10, 22), rand.Next(0, 60), 0);
    payments.Add((payId++, uid, pid, productPrices[pid], t, "success"));
}
payments.Add((payId++, 5,  107, 29000, new DateTime(2026, 3, 10, 14, 0, 0), "success"));
payments.Add((payId++, 16, 109, 49000, new DateTime(2026, 3, 15, 19, 30, 0), "success"));

using (var cmd = connection.CreateCommand())
{
    foreach (var (pid2, uid, prodId, amount, time, status) in payments)
    {
        cmd.CommandText = $"INSERT INTO payment_log VALUES ({pid2}, {uid}, {prodId}, {amount}, '{time:yyyy-MM-dd HH:mm:ss}', '{status}');";
        cmd.ExecuteNonQuery();
    }
}

Console.WriteLine($"총 결제 건수: {payments.Count}건 삽입 완료.");
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 1: 월별 ARPU, ARPPU, 결제 전환율
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 1] 월별 ARPU / ARPPU / 결제 전환율 ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH monthly_payment AS (
            SELECT
                CAST(DATE_TRUNC('month', payment_time) AS DATE) AS pay_month,
                COUNT(DISTINCT user_id)                         AS paying_users,
                SUM(amount_krw)                                 AS total_revenue
            FROM payment_log
            WHERE status = 'success'
            GROUP BY pay_month
        ),
        monthly_users AS (
            SELECT
                CAST(DATE_TRUNC('month', login_at) AS DATE) AS play_month,
                COUNT(DISTINCT user_id)                     AS total_users
            FROM session_log
            GROUP BY play_month
        )
        SELECT
            mp.pay_month,
            mu.total_users,
            mp.paying_users,
            mp.total_revenue,
            ROUND(mp.total_revenue * 1.0 / NULLIF(mu.total_users,  0), 0) AS arpu,
            ROUND(mp.total_revenue * 1.0 / NULLIF(mp.paying_users, 0), 0) AS arppu,
            ROUND(mp.paying_users  * 100.0 / NULLIF(mu.total_users, 0), 2) AS conversion_pct
        FROM monthly_payment mp
        JOIN monthly_users mu ON mp.pay_month = mu.play_month
        ORDER BY mp.pay_month;
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"월",-12} {"총유저",8} {"결제유저",8} {"수익(원)",14} {"ARPU",8} {"ARPPU",8} {"전환율",8}");
    Console.WriteLine(new string('-', 72));
    while (reader.Read())
    {
        var month = reader.GetDateTime(0).ToString("yyyy-MM");
        var totalUsers = reader.GetInt64(1);
        var payUsers = reader.GetInt64(2);
        var revenue = reader.GetInt64(3);
        var arpu = reader.GetDouble(4);
        var arppu = reader.GetDouble(5);
        var conversion = reader.GetDouble(6);
        Console.WriteLine(
            $"{month,-12} {totalUsers,8:N0} {payUsers,8:N0} {revenue,14:N0} " +
            $"{arpu,8:N0} {arppu,8:N0} {conversion,7:F2}%");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 2: 상품 유형별 수익 구성
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 2] 상품 유형별 수익 구성 ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT
            pi.product_type,
            COUNT(*)                              AS purchase_count,
            SUM(pl.amount_krw)                    AS total_revenue,
            ROUND(
                SUM(pl.amount_krw) * 100.0 /
                SUM(SUM(pl.amount_krw)) OVER (),
                2
            )                                     AS revenue_share_pct,
            ROUND(AVG(pl.amount_krw), 0)          AS avg_purchase_amount
        FROM payment_log pl
        JOIN product_info pi ON pl.product_id = pi.product_id
        WHERE pl.status = 'success'
        GROUP BY pi.product_type
        ORDER BY total_revenue DESC;
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"상품유형",-10} {"구매건수",8} {"총수익(원)",14} {"수익비중",10} {"평균금액",10}");
    Console.WriteLine(new string('-', 56));
    while (reader.Read())
    {
        var ptype = reader.GetString(0);
        var cnt = reader.GetInt64(1);
        var rev = reader.GetInt64(2);
        var share = reader.GetDouble(3);
        var avg = reader.GetDouble(4);
        Console.WriteLine($"{ptype,-10} {cnt,8:N0} {rev,14:N0} {share,9:F2}% {avg,10:N0}");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 3: 결제 코호트 — 첫 결제 후 재구매율
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 3] 결제 코호트 (첫 결제 후 월별 재구매 유지율) ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        WITH first_payment AS (
            SELECT
                user_id,
                CAST(DATE_TRUNC('month', MIN(payment_time)) AS DATE) AS first_pay_month
            FROM payment_log
            WHERE status = 'success'
            GROUP BY user_id
        ),
        cohort_activity AS (
            SELECT
                fp.user_id,
                fp.first_pay_month,
                CAST(DATE_TRUNC('month', pl.payment_time) AS DATE)   AS pay_month,
                DATEDIFF('month', fp.first_pay_month,
                    CAST(DATE_TRUNC('month', pl.payment_time) AS DATE)) AS months_since_first
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
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"첫결제월",-12} {"경과월",8} {"유지결제자",10}");
    Console.WriteLine(new string('-', 33));
    while (reader.Read())
    {
        var firstMonth = reader.GetDateTime(0).ToString("yyyy-MM");
        var monthsElapsed = reader.GetInt64(1);
        var retained = reader.GetInt64(2);
        Console.WriteLine($"{firstMonth,-12} {monthsElapsed,8:N0}개월 {retained,10:N0}명");
    }
}

Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════════════════
// 분석 4: 환불율 모니터링
// ═══════════════════════════════════════════════════════════════════════════════
Console.WriteLine("=== [분석 4] 월별 환불율 모니터링 ===");
Console.WriteLine();

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = """
        SELECT
            CAST(DATE_TRUNC('month', payment_time) AS DATE)              AS pay_month,
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
        """;

    using var reader = cmd.ExecuteReader();
    Console.WriteLine($"{"월",-12} {"총수익(원)",14} {"환불액(원)",12} {"환불율",8}");
    Console.WriteLine(new string('-', 50));
    while (reader.Read())
    {
        var month = reader.GetDateTime(0).ToString("yyyy-MM");
        var gross = reader.GetInt64(1);
        var refund = reader.GetInt64(2);
        var rate = reader.GetDouble(3);
        Console.WriteLine($"{month,-12} {gross,14:N0} {refund,12:N0} {rate,7:F2}%");
    }
}

Console.WriteLine();
Console.WriteLine("분석 완료.");
