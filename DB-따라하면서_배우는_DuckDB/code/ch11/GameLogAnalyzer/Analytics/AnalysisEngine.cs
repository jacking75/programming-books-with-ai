using DuckDB.NET.Data;

namespace GameLogAnalyzer.Analysis;

// ─────────────────────────────────────────────────────────────────────
// 분석 결과 레코드 타입들
// ─────────────────────────────────────────────────────────────────────

public record DauRecord(string Date, int Dau, int NewPlayers, double AvgSessionMin, int TotalSessions);

public record BattleBalanceRecord(
    string DungeonName, string Difficulty,
    int TotalBattles, double SuccessRate,
    double AvgDuration, double AvgGoldEarned);

public record ItemEconomyRecord(
    string ItemCategory, string Action,
    long TotalQuantity, long TotalGoldValue,
    double AvgGoldPerItem);

public record RevenueReport(
    List<RevenueRow> Rows,
    long GrandTotal);

public record RevenueRow(
    string Date, int PayingUsers,
    long TotalRevenue, double Arpu, double Arppu,
    long MaxSinglePayment);

public record AnomalyRecord(
    string PlayerId, string AnomalyType,
    string Detail, string DetectedAt);

/// <summary>
/// DuckDB에 저장된 게임 로그를 분석하는 엔진.
/// 각 메서드는 독립적인 분석 쿼리를 실행하고 결과를 반환한다.
/// </summary>
public class AnalysisEngine
{
    private readonly DuckDBConnection _conn;

    public AnalysisEngine(DuckDBConnection conn)
    {
        _conn = conn;
    }

    // ──────────────────────────────────────────────────
    // 11.4.1 일별 활성 사용자(DAU) 분석
    // ──────────────────────────────────────────────────

    /// <summary>
    /// 날짜별 DAU, 신규 플레이어 수, 평균 세션 시간을 반환한다.
    /// </summary>
    public Task<List<DauRecord>> GetDailyActiveUsersAsync()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                date::VARCHAR                                  AS date,
                COUNT(DISTINCT player_id)                      AS dau,
                COUNT(DISTINCT CASE WHEN is_first_login THEN player_id END)
                                                               AS new_players,
                ROUND(AVG(session_duration_seconds) / 60.0, 1) AS avg_session_min,
                COUNT(*)                                       AS total_sessions
            FROM login_events
            GROUP BY date
            ORDER BY date
            """;

        var result = new List<DauRecord>();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new DauRecord(
                Date: reader.GetString(0),
                Dau: reader.GetInt32(1),
                NewPlayers: reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                AvgSessionMin: reader.GetDouble(3),
                TotalSessions: reader.GetInt32(4)
            ));
        }

        return Task.FromResult(result);
    }

    // 동기 버전 (내부 사용)
    public List<DauRecord> GetDailyActiveUsers()
        => GetDailyActiveUsersAsync().GetAwaiter().GetResult();

    // ──────────────────────────────────────────────────
    // 11.4.2 전투 균형 분석
    // ──────────────────────────────────────────────────

    /// <summary>
    /// 던전별, 난이도별 클리어율과 평균 전투 시간, 평균 골드 획득량을 반환한다.
    /// 클리어율이 너무 높거나 낮은 콘텐츠는 밸런스 조정 대상이다.
    /// </summary>
    public Task<List<BattleBalanceRecord>> GetBattleBalanceReportAsync()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                dungeon_name,
                difficulty,
                COUNT(*)                                AS total_battles,
                ROUND(AVG(is_success::INTEGER) * 100, 1) AS success_rate,
                ROUND(AVG(duration_seconds) / 60.0, 1)  AS avg_duration_min,
                ROUND(AVG(gold_earned), 0)               AS avg_gold_earned
            FROM battle_events
            GROUP BY dungeon_name, difficulty
            ORDER BY dungeon_name, difficulty
            """;

        var result = new List<BattleBalanceRecord>();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new BattleBalanceRecord(
                DungeonName: reader.GetString(0),
                Difficulty: reader.GetString(1),
                TotalBattles: reader.GetInt32(2),
                SuccessRate: reader.GetDouble(3),
                AvgDuration: reader.GetDouble(4),
                AvgGoldEarned: reader.GetDouble(5)
            ));
        }

        return Task.FromResult(result);
    }

    public List<BattleBalanceRecord> GetBattleBalanceReport()
        => GetBattleBalanceReportAsync().GetAwaiter().GetResult();

    // ──────────────────────────────────────────────────
    // 11.4.3 아이템 경제 분석
    // ──────────────────────────────────────────────────

    /// <summary>
    /// 아이템 카테고리별 획득/소비/판매 수량과 골드 흐름을 분석한다.
    /// 경제 불균형(인플레/디플레 징후)을 파악하는 데 활용한다.
    /// </summary>
    public Task<List<ItemEconomyRecord>> GetItemEconomyReportAsync()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                item_category,
                action,
                SUM(quantity)               AS total_quantity,
                SUM(gold_value)             AS total_gold_value,
                ROUND(AVG(gold_value::DOUBLE / NULLIF(quantity, 0)), 0)
                                            AS avg_gold_per_item
            FROM item_events
            GROUP BY item_category, action
            ORDER BY item_category, action
            """;

        var result = new List<ItemEconomyRecord>();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(new ItemEconomyRecord(
                ItemCategory: reader.GetString(0),
                Action: reader.GetString(1),
                TotalQuantity: reader.GetInt64(2),
                TotalGoldValue: reader.GetInt64(3),
                AvgGoldPerItem: reader.IsDBNull(4) ? 0 : reader.GetDouble(4)
            ));
        }

        return Task.FromResult(result);
    }

    public List<ItemEconomyRecord> GetItemEconomyReport()
        => GetItemEconomyReportAsync().GetAwaiter().GetResult();

    // ──────────────────────────────────────────────────
    // 11.4.4 매출 분석 (ARPU / ARPPU)
    // ──────────────────────────────────────────────────

    /// <summary>
    /// 날짜별 결제 통계를 반환한다.
    /// ARPU: 전체 유저 대비 평균 매출
    /// ARPPU: 결제 유저 대비 평균 매출
    /// </summary>
    public Task<RevenueReport> GetRevenueReportAsync()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            WITH daily_dau AS (
                SELECT date::DATE AS date, COUNT(DISTINCT player_id) AS dau
                FROM login_events
                GROUP BY date
            ),
            daily_payment AS (
                SELECT
                    date::DATE                  AS date,
                    COUNT(DISTINCT player_id)   AS paying_users,
                    SUM(amount_krw)             AS total_revenue,
                    MAX(amount_krw)             AS max_single_payment
                FROM payment_events
                GROUP BY date
            )
            SELECT
                p.date::VARCHAR                                     AS date,
                p.paying_users,
                p.total_revenue,
                ROUND(p.total_revenue::DOUBLE / NULLIF(d.dau, 0), 0) AS arpu,
                ROUND(p.total_revenue::DOUBLE / NULLIF(p.paying_users, 0), 0) AS arppu,
                p.max_single_payment
            FROM daily_payment p
            JOIN daily_dau d ON p.date = d.date
            ORDER BY p.date
            """;

        var rows = new List<RevenueRow>();
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            rows.Add(new RevenueRow(
                Date: reader.GetString(0),
                PayingUsers: reader.GetInt32(1),
                TotalRevenue: reader.GetInt64(2),
                Arpu: reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                Arppu: reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                MaxSinglePayment: reader.GetInt64(5)
            ));
        }

        long grandTotal = rows.Sum(r => r.TotalRevenue);
        return Task.FromResult(new RevenueReport(rows, grandTotal));
    }

    public List<RevenueRow> GetRevenueReport()
        => GetRevenueReportAsync().GetAwaiter().GetResult().Rows;

    // ──────────────────────────────────────────────────
    // 11.4.5 이상 탐지(Anomaly Detection)
    // ──────────────────────────────────────────────────

    /// <summary>
    /// 비정상적인 행동 패턴을 가진 플레이어를 탐지한다.
    /// 탐지 규칙:
    ///   1. 하루에 전투를 50회 이상 한 플레이어 (비정상 플레이)
    ///   2. 성공률이 99% 이상인 플레이어 (핵 의심)
    ///   3. 하루 결제 금액이 100만 원 이상인 플레이어 (이상 결제)
    ///   4. 세션 시간이 8시간 이상인 플레이어 (이상 접속)
    /// </summary>
    public Task<List<AnomalyRecord>> GetAnomalyDetectionReportAsync()
    {
        var result = new List<AnomalyRecord>();

        // 규칙 1: 과다 전투
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    player_id,
                    date::VARCHAR,
                    COUNT(*) AS battle_count
                FROM battle_events
                GROUP BY player_id, date
                HAVING COUNT(*) >= 50
                ORDER BY battle_count DESC
                """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AnomalyRecord(
                    PlayerId: reader.GetString(0),
                    AnomalyType: "과다 전투",
                    Detail: $"하루 전투 횟수: {reader.GetInt64(2)}회",
                    DetectedAt: reader.GetString(1)
                ));
            }
        }

        // 규칙 2: 비정상 성공률
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    player_id,
                    ROUND(AVG(is_success::INTEGER) * 100, 2) AS success_rate,
                    COUNT(*) AS total_battles
                FROM battle_events
                GROUP BY player_id
                HAVING AVG(is_success::INTEGER) >= 0.99
                   AND COUNT(*) >= 20
                ORDER BY success_rate DESC
                """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AnomalyRecord(
                    PlayerId: reader.GetString(0),
                    AnomalyType: "비정상 성공률",
                    Detail: $"성공률 {reader.GetDouble(1)}% ({reader.GetInt64(2)}전)",
                    DetectedAt: "전체 기간"
                ));
            }
        }

        // 규칙 3: 이상 결제
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    player_id,
                    date::VARCHAR,
                    SUM(amount_krw) AS daily_total
                FROM payment_events
                GROUP BY player_id, date
                HAVING SUM(amount_krw) >= 1000000
                ORDER BY daily_total DESC
                """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AnomalyRecord(
                    PlayerId: reader.GetString(0),
                    AnomalyType: "이상 결제",
                    Detail: $"일일 결제 {reader.GetInt64(2):N0}원",
                    DetectedAt: reader.GetString(1)
                ));
            }
        }

        // 규칙 4: 이상 세션
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT
                    player_id,
                    date::VARCHAR,
                    MAX(session_duration_seconds) AS max_session
                FROM login_events
                GROUP BY player_id, date
                HAVING MAX(session_duration_seconds) >= 28800
                ORDER BY max_session DESC
                """;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int seconds = reader.GetInt32(2);
                result.Add(new AnomalyRecord(
                    PlayerId: reader.GetString(0),
                    AnomalyType: "이상 세션",
                    Detail: $"세션 시간 {seconds / 3600}시간 {(seconds % 3600) / 60}분",
                    DetectedAt: reader.GetString(1)
                ));
            }
        }

        return Task.FromResult(result);
    }

    public List<AnomalyRecord> GetAnomalyDetectionReport()
        => GetAnomalyDetectionReportAsync().GetAwaiter().GetResult();

    // ──────────────────────────────────────────────────
    // 보조: 전체 데이터 요약
    // ──────────────────────────────────────────────────

    public (long LoginCount, long BattleCount, long ItemCount, long PaymentCount) GetDataSummary()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM login_events)   AS login_count,
                (SELECT COUNT(*) FROM battle_events)  AS battle_count,
                (SELECT COUNT(*) FROM item_events)    AS item_count,
                (SELECT COUNT(*) FROM payment_events) AS payment_count
            """;

        using var reader = cmd.ExecuteReader();
        reader.Read();
        return (
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3)
        );
    }
}
