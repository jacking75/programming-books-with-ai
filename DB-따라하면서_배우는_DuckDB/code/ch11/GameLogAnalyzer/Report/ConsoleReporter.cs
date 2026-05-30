using GameLogAnalyzer.Analysis;

namespace GameLogAnalyzer.Report;

/// <summary>
/// 분석 결과를 콘솔에 보기 좋은 표 형식으로 출력한다.
/// ANSI 색상 코드를 활용하여 가독성을 높인다.
/// </summary>
public static class ConsoleReporter
{
    // ANSI 색상 상수
    private const string Reset  = "\x1b[0m";
    private const string Bold   = "\x1b[1m";
    private const string Cyan   = "\x1b[36m";
    private const string Green  = "\x1b[32m";
    private const string Yellow = "\x1b[33m";
    private const string Red    = "\x1b[31m";
    private const string Gray   = "\x1b[90m";

    // ──────────────────────────────────────────────
    // 공통 유틸리티
    // ──────────────────────────────────────────────

    public static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"{Cyan}{Bold}{'═'.Repeat(70)}{Reset}");
        Console.WriteLine($"{Cyan}{Bold}  {title}{Reset}");
        Console.WriteLine($"{Cyan}{Bold}{'═'.Repeat(70)}{Reset}");
    }

    public static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"{Yellow}{Bold}▶ {title}{Reset}");
        Console.WriteLine($"{Gray}{'─'.Repeat(70)}{Reset}");
    }

    public static void PrintSummary(long loginCount, long battleCount, long itemCount, long paymentCount)
    {
        PrintSection("적재된 데이터 요약");
        Console.WriteLine($"  {"접속 이벤트",-20} {loginCount,10:N0}  건");
        Console.WriteLine($"  {"전투 이벤트",-20} {battleCount,10:N0}  건");
        Console.WriteLine($"  {"아이템 이벤트",-20} {itemCount,10:N0}  건");
        Console.WriteLine($"  {"결제 이벤트",-20} {paymentCount,10:N0}  건");
        Console.WriteLine($"  {"────────────────────",-20} {"──────────",10}");
        Console.WriteLine($"  {"합계",-20} {loginCount + battleCount + itemCount + paymentCount,10:N0}  건");
    }

    // ──────────────────────────────────────────────
    // DAU 리포트
    // ──────────────────────────────────────────────

    public static void PrintDauReport(List<DauRecord> rows)
    {
        PrintSection("일별 활성 사용자 (DAU) 리포트");

        Console.WriteLine($"  {"날짜",-12} {"DAU",6} {"신규",6} {"평균세션(분)",14} {"총세션수",10}");
        Console.WriteLine($"  {new string('─', 52)}");

        foreach (var r in rows)
        {
            Console.WriteLine(
                $"  {r.Date,-12} {r.Dau,6:N0} {r.NewPlayers,6:N0} {r.AvgSessionMin,14:N1} {r.TotalSessions,10:N0}");
        }

        Console.WriteLine($"  {new string('─', 52)}");
        Console.WriteLine($"  {"평균",-12} {rows.Average(r => r.Dau),6:N0} {"-",6} {rows.Average(r => r.AvgSessionMin),14:N1} {rows.Sum(r => r.TotalSessions),10:N0}");
    }

    // ──────────────────────────────────────────────
    // 전투 균형 리포트
    // ──────────────────────────────────────────────

    public static void PrintBattleBalanceReport(List<BattleBalanceRecord> rows)
    {
        PrintSection("전투 균형 분석 리포트");

        Console.WriteLine($"  {"던전 이름",-14} {"난이도",-10} {"전투수",8} {"성공률",8} {"평균시간",10} {"평균골드",10}");
        Console.WriteLine($"  {new string('─', 64)}");

        string? lastDungeon = null;
        foreach (var r in rows)
        {
            if (lastDungeon != null && lastDungeon != r.DungeonName)
                Console.WriteLine($"  {new string('·', 64)}");

            lastDungeon = r.DungeonName;

            // 성공률에 따라 색상 표시
            string successColor = r.SuccessRate >= 80 ? Green :
                                  r.SuccessRate >= 50 ? Yellow : Red;

            Console.WriteLine(
                $"  {r.DungeonName,-14} {r.Difficulty,-10} {r.TotalBattles,8:N0} " +
                $"{successColor}{r.SuccessRate,7:N1}%{Reset} " +
                $"{r.AvgDuration,9:N1}분 {r.AvgGoldEarned,10:N0}G");
        }
    }

    // ──────────────────────────────────────────────
    // 아이템 경제 리포트
    // ──────────────────────────────────────────────

    public static void PrintItemEconomyReport(List<ItemEconomyRecord> rows)
    {
        PrintSection("아이템 경제 분석 리포트");

        Console.WriteLine($"  {"카테고리",-10} {"액션",-10} {"총수량",10} {"총골드",14} {"개당골드",10}");
        Console.WriteLine($"  {new string('─', 58)}");

        string? lastCategory = null;
        foreach (var r in rows)
        {
            if (lastCategory != null && lastCategory != r.ItemCategory)
                Console.WriteLine($"  {new string('·', 58)}");

            lastCategory = r.ItemCategory;

            string actionColor = r.Action == "acquire" ? Green :
                                 r.Action == "sell" ? Yellow : Reset;

            Console.WriteLine(
                $"  {r.ItemCategory,-10} {actionColor}{r.Action,-10}{Reset} " +
                $"{r.TotalQuantity,10:N0} {r.TotalGoldValue,14:N0}G {r.AvgGoldPerItem,10:N0}G");
        }
    }

    // ──────────────────────────────────────────────
    // 매출 리포트
    // ──────────────────────────────────────────────

    public static void PrintRevenueReport(List<RevenueRow> rows)
    {
        PrintSection("매출 분석 리포트 (ARPU / ARPPU)");

        Console.WriteLine($"  {"날짜",-12} {"결제유저",8} {"총매출",14} {"ARPU",10} {"ARPPU",10} {"최대단건",12}");
        Console.WriteLine($"  {new string('─', 70)}");

        long grandTotal = 0;
        foreach (var r in rows)
        {
            grandTotal += r.TotalRevenue;
            Console.WriteLine(
                $"  {r.Date,-12} {r.PayingUsers,8:N0} {r.TotalRevenue,14:N0}원 " +
                $"{r.Arpu,10:N0}원 {r.Arppu,10:N0}원 {r.MaxSinglePayment,12:N0}원");
        }

        Console.WriteLine($"  {new string('─', 70)}");
        Console.WriteLine($"  {Green}{Bold}  7일 총매출: {grandTotal:N0}원{Reset}");
    }

    // ──────────────────────────────────────────────
    // 이상 탐지 리포트
    // ──────────────────────────────────────────────

    public static void PrintAnomalyReport(List<AnomalyRecord> rows)
    {
        PrintSection("이상 탐지 리포트");

        if (rows.Count == 0)
        {
            Console.WriteLine($"  {Green}이상 징후가 탐지되지 않았습니다.{Reset}");
            return;
        }

        Console.WriteLine($"  {Red}{Bold}총 {rows.Count}건의 이상 징후가 탐지되었습니다.{Reset}");
        Console.WriteLine();
        Console.WriteLine($"  {"플레이어ID",-12} {"탐지유형",-16} {"세부사항",-30} {"탐지일시",-15}");
        Console.WriteLine($"  {new string('─', 73)}");

        foreach (var r in rows)
        {
            Console.WriteLine(
                $"  {Red}{r.PlayerId,-12}{Reset} {Yellow}{r.AnomalyType,-16}{Reset} " +
                $"{r.Detail,-30} {Gray}{r.DetectedAt,-15}{Reset}");
        }
    }

    public static void PrintFooter()
    {
        Console.WriteLine();
        Console.WriteLine($"{Cyan}{Bold}{'═'.Repeat(70)}{Reset}");
        Console.WriteLine($"{Cyan}{Bold}  분석 완료 — Game Log Analyzer v1.0{Reset}");
        Console.WriteLine($"{Cyan}{Bold}{'═'.Repeat(70)}{Reset}");
        Console.WriteLine();
    }
}

// char 확장 — 반복 문자열 생성
file static class CharExtensions
{
    public static string Repeat(this char c, int count) => new string(c, count);
}
