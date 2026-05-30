// code/ch09/DailyReportGenerator/ReportQuery.cs
// 9장 통계 분석 실전에서 사용하는 분석 쿼리 모음

namespace DailyReportGenerator;

/// <summary>
/// 인메모리 DuckDB에 샘플 데이터를 생성하고 분석 쿼리를 실행하는 클래스
/// </summary>
public static class ReportQuery
{
    // -------------------------------------------------------------------------
    // 샘플 데이터 생성
    // -------------------------------------------------------------------------

    /// <summary>
    /// 30일치 샘플 데이터를 생성한다 (players, login_logs, battle_logs, payments)
    /// </summary>
    public static string CreateSampleData() => """
        -- 플레이어 기본 정보
        CREATE OR REPLACE TABLE players AS
        SELECT
            p.user_id,
            p.job_class,
            p.register_date
        FROM (
            VALUES
                (1001, '전사',  DATE '2025-05-01'),
                (1002, '마법사',DATE '2025-05-01'),
                (1003, '궁수',  DATE '2025-05-03'),
                (1004, '힐러',  DATE '2025-05-05'),
                (1005, '전사',  DATE '2025-05-08'),
                (1006, '마법사',DATE '2025-05-10'),
                (1007, '궁수',  DATE '2025-05-12'),
                (1008, '힐러',  DATE '2025-05-15'),
                (1009, '전사',  DATE '2025-05-18'),
                (1010, '마법사',DATE '2025-05-20')
        ) p(user_id, job_class, register_date);

        -- 30일치 로그인 로그 생성
        CREATE OR REPLACE TABLE login_logs AS
        WITH date_range AS (
            SELECT unnest(generate_series(
                DATE '2025-06-01',
                DATE '2025-06-30',
                INTERVAL '1 day'
            ))::DATE AS login_date
        ),
        player_date AS (
            SELECT
                p.user_id,
                p.job_class,
                d.login_date,
                -- 접속 시각: 각 유저마다 시간대를 다르게
                (login_date::TIMESTAMP + INTERVAL '1 hour' * ((p.user_id * 3 + day(login_date)) % 24)) AS login_time
            FROM players p
            CROSS JOIN date_range d
            -- 일부 날짜는 접속 안 함 (70% 확률 접속)
            WHERE (p.user_id + day(d.login_date)) % 10 < 7
        )
        SELECT
            user_id,
            job_class,
            login_date,
            login_time
        FROM player_date;

        -- 전투 로그
        CREATE OR REPLACE TABLE battle_logs AS
        SELECT
            l.user_id,
            l.job_class,
            l.login_date        AS battle_date,
            -- 직업별 점수 분포 차이 적용
            CASE l.job_class
                WHEN '전사'   THEN 5000 + (l.user_id * 17 + day(l.login_date) * 31) % 8000
                WHEN '마법사' THEN 7000 + (l.user_id * 13 + day(l.login_date) * 41) % 10000
                WHEN '궁수'   THEN 6000 + (l.user_id * 19 + day(l.login_date) * 23) % 9000
                WHEN '힐러'   THEN 4000 + (l.user_id * 11 + day(l.login_date) * 37) % 7000
                ELSE           5000 + (l.user_id * 7  + day(l.login_date) * 29) % 6000
            END                 AS score,
            -- 세션 시간(분): 20~120분
            20 + (l.user_id * 7 + day(l.login_date) * 13) % 100 AS duration_min
        FROM login_logs l;

        -- 결제 로그
        CREATE OR REPLACE TABLE payments AS
        SELECT
            p.user_id,
            p.job_class,
            p.register_date,
            -- 결제 단계: 일부 유저만 결제까지 완료
            CASE
                WHEN (p.user_id + d.step_order) % 5 = 0 THEN 'payment_done'
                WHEN (p.user_id + d.step_order) % 5 = 1 THEN 'payment_submit'
                WHEN (p.user_id + d.step_order) % 5 = 2 THEN 'payment_page'
                WHEN (p.user_id + d.step_order) % 5 = 3 THEN 'item_select'
                ELSE                                          'shop_open'
            END AS step,
            d.step_order,
            DATE '2025-06-15' + INTERVAL '1 hour' * d.step_order AS event_time
        FROM players p
        CROSS JOIN (
            VALUES (1), (2), (3), (4), (5)
        ) d(step_order)
        WHERE (p.user_id * d.step_order) % 3 = 0;
        """;

    // -------------------------------------------------------------------------
    // 1. 일별 DAU 추이 (최근 7일)
    // -------------------------------------------------------------------------

    public static string DailyDauTrend() => """
        WITH date_series AS (
            SELECT unnest(generate_series(
                DATE '2025-06-24',
                DATE '2025-06-30',
                INTERVAL '1 day'
            ))::DATE AS day_slot
        ),
        daily_active AS (
            SELECT
                login_date AS day_slot,
                count(DISTINCT user_id) AS dau
            FROM login_logs
            WHERE login_date BETWEEN DATE '2025-06-24' AND DATE '2025-06-30'
            GROUP BY login_date
        )
        SELECT
            ds.day_slot,
            coalesce(da.dau, 0) AS dau
        FROM date_series ds
        LEFT JOIN daily_active da ON ds.day_slot = da.day_slot
        ORDER BY ds.day_slot;
        """;

    // -------------------------------------------------------------------------
    // 2. 직업별 전투 통계 (윈도우 함수 활용)
    // -------------------------------------------------------------------------

    public static string JobBattleStats() => """
        WITH job_stats AS (
            SELECT
                job_class,
                count(*)                                                          AS battle_count,
                round(avg(score), 1)                                              AS avg_score,
                round(median(score), 1)                                           AS median_score,
                round(stddev_samp(score), 1)                                      AS stddev_score,
                round(percentile_cont(0.99) WITHIN GROUP (ORDER BY score), 1)    AS p99_score,
                round(avg(duration_min), 1)                                       AS avg_duration,
                round(median(duration_min), 1)                                    AS median_duration
            FROM battle_logs
            GROUP BY job_class
        )
        SELECT
            job_class,
            battle_count,
            avg_score,
            median_score,
            stddev_score,
            p99_score,
            avg_duration,
            median_duration,
            RANK() OVER (ORDER BY avg_score DESC)         AS rank_by_score,
            RANK() OVER (ORDER BY avg_duration DESC)      AS rank_by_duration
        FROM job_stats
        ORDER BY avg_score DESC;
        """;

    // -------------------------------------------------------------------------
    // 3. 시간대별 접속 패턴
    // -------------------------------------------------------------------------

    public static string HourlyLoginPattern() => """
        SELECT
            hour(login_time)            AS hour_of_day,
            count(*)                    AS login_count,
            count(DISTINCT user_id)     AS unique_users,
            round(count(*) * 100.0 / sum(count(*)) OVER (), 2) AS pct
        FROM login_logs
        GROUP BY hour_of_day
        ORDER BY hour_of_day;
        """;

    // -------------------------------------------------------------------------
    // 4. 코호트 분석 (가입 주차별 리텐션)
    // -------------------------------------------------------------------------

    public static string CohortRetention() => """
        WITH cohort_base AS (
            SELECT
                user_id,
                date_trunc('week', register_date)::DATE AS cohort_week
            FROM players
        ),
        activity AS (
            SELECT
                cb.user_id,
                cb.cohort_week,
                floor(datediff('day', cb.cohort_week, l.login_date) / 7)::INT AS weeks_since_register
            FROM cohort_base cb
            JOIN login_logs l ON cb.user_id = l.user_id
            WHERE weeks_since_register >= 0
        ),
        cohort_size AS (
            SELECT cohort_week, count(DISTINCT user_id) AS cohort_users
            FROM cohort_base
            GROUP BY cohort_week
        ),
        retention_raw AS (
            SELECT
                cohort_week,
                weeks_since_register,
                count(DISTINCT user_id) AS active_users
            FROM activity
            GROUP BY cohort_week, weeks_since_register
        )
        SELECT
            r.cohort_week,
            cs.cohort_users,
            r.weeks_since_register                                  AS week_num,
            r.active_users,
            round(r.active_users * 100.0 / cs.cohort_users, 1)     AS retention_pct
        FROM retention_raw r
        JOIN cohort_size cs ON r.cohort_week = cs.cohort_week
        ORDER BY r.cohort_week, r.weeks_since_register;
        """;

    // -------------------------------------------------------------------------
    // 5. 결제 퍼널 분석
    // -------------------------------------------------------------------------

    public static string PaymentFunnel() => """
        WITH step_counts AS (
            SELECT
                step,
                count(DISTINCT user_id) AS users,
                CASE step
                    WHEN 'shop_open'      THEN 1
                    WHEN 'item_select'    THEN 2
                    WHEN 'payment_page'   THEN 3
                    WHEN 'payment_submit' THEN 4
                    WHEN 'payment_done'   THEN 5
                END AS step_order
            FROM payments
            GROUP BY step
        )
        SELECT
            step_order,
            step                                                                        AS funnel_step,
            users,
            round(users * 100.0 / first_value(users) OVER (ORDER BY step_order), 1)   AS total_cvr_pct,
            round(users * 100.0 / lag(users, 1, users) OVER (ORDER BY step_order), 1) AS prev_step_cvr_pct,
            lag(users, 1, users) OVER (ORDER BY step_order) - users                   AS drop_off
        FROM step_counts
        ORDER BY step_order;
        """;
}
