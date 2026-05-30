// code/ch09/DailyReportGenerator/HtmlReportBuilder.cs
// DuckDB 분석 결과를 HTML 리포트 파일로 변환한다.

using System.Text;

namespace DailyReportGenerator;

/// <summary>
/// 각 분석 결과를 HTML 테이블로 변환하고 파일로 저장한다.
/// </summary>
public class HtmlReportBuilder
{
    private readonly StringBuilder _sb = new();
    private readonly string _reportDate;

    public HtmlReportBuilder(string reportDate)
    {
        _reportDate = reportDate;
        AppendHeader();
    }

    // -------------------------------------------------------------------------
    // 헤더 / 푸터
    // -------------------------------------------------------------------------

    private void AppendHeader()
    {
        _sb.AppendLine($"""
            <!DOCTYPE html>
            <html lang="ko">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>일일 통계 리포트 - {_reportDate}</title>
                <style>
                    * {{ box-sizing: border-box; margin: 0; padding: 0; }}
                    body {{
                        font-family: 'Malgun Gothic', 'Apple SD Gothic Neo', sans-serif;
                        background: #f0f2f5;
                        color: #333;
                        padding: 32px;
                    }}
                    header {{
                        margin-bottom: 32px;
                    }}
                    header h1 {{
                        font-size: 26px;
                        color: #1a1a2e;
                    }}
                    header p {{
                        font-size: 13px;
                        color: #888;
                        margin-top: 4px;
                    }}
                    .section {{
                        background: white;
                        border-radius: 10px;
                        padding: 24px;
                        margin-bottom: 24px;
                        box-shadow: 0 2px 6px rgba(0,0,0,.08);
                    }}
                    .section h2 {{
                        font-size: 16px;
                        font-weight: 700;
                        color: #1a1a2e;
                        margin-bottom: 16px;
                        padding-bottom: 10px;
                        border-bottom: 2px solid #e8eaf0;
                    }}
                    table {{
                        border-collapse: collapse;
                        width: 100%;
                        font-size: 13px;
                    }}
                    th {{
                        background: #2c7be5;
                        color: white;
                        padding: 10px 14px;
                        text-align: left;
                        font-weight: 600;
                    }}
                    td {{
                        padding: 9px 14px;
                        border-bottom: 1px solid #f0f0f0;
                    }}
                    tr:last-child td {{
                        border-bottom: none;
                    }}
                    tr:hover td {{
                        background: #f7f9ff;
                    }}
                    .num {{
                        text-align: right;
                        font-variant-numeric: tabular-nums;
                    }}
                    .badge {{
                        display: inline-block;
                        padding: 2px 8px;
                        border-radius: 12px;
                        font-size: 11px;
                        font-weight: 600;
                    }}
                    .badge-high {{ background: #d4edda; color: #155724; }}
                    .badge-mid  {{ background: #fff3cd; color: #856404; }}
                    .badge-low  {{ background: #f8d7da; color: #721c24; }}
                </style>
            </head>
            <body>
            <header>
                <h1>일일 통계 리포트</h1>
                <p>기준일: {_reportDate} &nbsp;|&nbsp; 생성 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
            </header>
            """);
    }

    private void AppendFooter()
    {
        _sb.AppendLine("""
            </body>
            </html>
            """);
    }

    // -------------------------------------------------------------------------
    // 1. 일별 DAU 테이블
    // -------------------------------------------------------------------------

    public void AddDauSection(IEnumerable<(string Date, int Dau)> rows)
    {
        _sb.AppendLine("""
            <div class="section">
              <h2>일별 DAU 추이 (최근 7일)</h2>
              <table>
                <thead><tr><th>날짜</th><th class="num">DAU</th></tr></thead>
                <tbody>
            """);

        foreach (var (date, dau) in rows)
        {
            _sb.AppendLine($"""
                  <tr><td>{date}</td><td class="num">{dau:N0}</td></tr>
                """);
        }

        _sb.AppendLine("""
                </tbody>
              </table>
            </div>
            """);
    }

    // -------------------------------------------------------------------------
    // 2. 직업별 전투 통계
    // -------------------------------------------------------------------------

    public void AddJobStatsSection(IEnumerable<JobStatRow> rows)
    {
        _sb.AppendLine("""
            <div class="section">
              <h2>직업별 전투 통계 (윈도우 함수 활용)</h2>
              <table>
                <thead>
                  <tr>
                    <th>직업</th>
                    <th class="num">전투 수</th>
                    <th class="num">평균 점수</th>
                    <th class="num">중앙값 점수</th>
                    <th class="num">표준편차</th>
                    <th class="num">P99 점수</th>
                    <th class="num">평균 플레이(분)</th>
                    <th class="num">점수 순위</th>
                  </tr>
                </thead>
                <tbody>
            """);

        foreach (var r in rows)
        {
            _sb.AppendLine($"""
                  <tr>
                    <td>{r.JobClass}</td>
                    <td class="num">{r.BattleCount:N0}</td>
                    <td class="num">{r.AvgScore:F1}</td>
                    <td class="num">{r.MedianScore:F1}</td>
                    <td class="num">{r.StddevScore:F1}</td>
                    <td class="num">{r.P99Score:F1}</td>
                    <td class="num">{r.AvgDuration:F1}</td>
                    <td class="num">{r.RankByScore}</td>
                  </tr>
                """);
        }

        _sb.AppendLine("""
                </tbody>
              </table>
            </div>
            """);
    }

    // -------------------------------------------------------------------------
    // 3. 시간대별 접속 패턴
    // -------------------------------------------------------------------------

    public void AddHourlyPatternSection(IEnumerable<(int Hour, int LoginCount, int UniqueUsers, double Pct)> rows)
    {
        _sb.AppendLine("""
            <div class="section">
              <h2>시간대별 접속 패턴</h2>
              <table>
                <thead>
                  <tr>
                    <th>시간대</th>
                    <th class="num">접속 횟수</th>
                    <th class="num">고유 유저 수</th>
                    <th class="num">비중(%)</th>
                  </tr>
                </thead>
                <tbody>
            """);

        foreach (var (hour, loginCount, uniqueUsers, pct) in rows)
        {
            string badgeClass = pct >= 10 ? "badge-high" : pct >= 5 ? "badge-mid" : "badge-low";
            _sb.AppendLine($"""
                  <tr>
                    <td>{hour:D2}시</td>
                    <td class="num">{loginCount:N0}</td>
                    <td class="num">{uniqueUsers:N0}</td>
                    <td class="num"><span class="badge {badgeClass}">{pct:F2}%</span></td>
                  </tr>
                """);
        }

        _sb.AppendLine("""
                </tbody>
              </table>
            </div>
            """);
    }

    // -------------------------------------------------------------------------
    // 4. 코호트 분석
    // -------------------------------------------------------------------------

    public void AddCohortSection(IEnumerable<CohortRow> rows)
    {
        _sb.AppendLine("""
            <div class="section">
              <h2>코호트 분석 (가입 주차별 리텐션)</h2>
              <table>
                <thead>
                  <tr>
                    <th>코호트 주차</th>
                    <th class="num">초기 유저</th>
                    <th class="num">경과 주차</th>
                    <th class="num">잔존 유저</th>
                    <th class="num">리텐션(%)</th>
                  </tr>
                </thead>
                <tbody>
            """);

        foreach (var r in rows)
        {
            string badgeClass = r.RetentionPct >= 50 ? "badge-high" : r.RetentionPct >= 25 ? "badge-mid" : "badge-low";
            _sb.AppendLine($"""
                  <tr>
                    <td>{r.CohortWeek}</td>
                    <td class="num">{r.CohortUsers}</td>
                    <td class="num">{r.WeekNum}</td>
                    <td class="num">{r.ActiveUsers}</td>
                    <td class="num"><span class="badge {badgeClass}">{r.RetentionPct:F1}%</span></td>
                  </tr>
                """);
        }

        _sb.AppendLine("""
                </tbody>
              </table>
            </div>
            """);
    }

    // -------------------------------------------------------------------------
    // 5. 결제 퍼널 분석
    // -------------------------------------------------------------------------

    public void AddFunnelSection(IEnumerable<FunnelRow> rows)
    {
        _sb.AppendLine("""
            <div class="section">
              <h2>결제 퍼널 분석</h2>
              <table>
                <thead>
                  <tr>
                    <th>단계</th>
                    <th>퍼널 스텝</th>
                    <th class="num">유저 수</th>
                    <th class="num">전체 전환율(%)</th>
                    <th class="num">직전 단계 전환율(%)</th>
                    <th class="num">이탈 수</th>
                  </tr>
                </thead>
                <tbody>
            """);

        foreach (var r in rows)
        {
            string badgeClass = r.PrevStepCvrPct >= 70 ? "badge-high" : r.PrevStepCvrPct >= 40 ? "badge-mid" : "badge-low";
            _sb.AppendLine($"""
                  <tr>
                    <td>{r.StepOrder}</td>
                    <td>{r.FunnelStep}</td>
                    <td class="num">{r.Users}</td>
                    <td class="num">{r.TotalCvrPct:F1}%</td>
                    <td class="num"><span class="badge {badgeClass}">{r.PrevStepCvrPct:F1}%</span></td>
                    <td class="num">{r.DropOff}</td>
                  </tr>
                """);
        }

        _sb.AppendLine("""
                </tbody>
              </table>
            </div>
            """);
    }

    // -------------------------------------------------------------------------
    // 저장
    // -------------------------------------------------------------------------

    public async Task SaveAsync(string filePath)
    {
        AppendFooter();
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(filePath, _sb.ToString(), Encoding.UTF8);
    }
}

// -------------------------------------------------------------------------
// 데이터 레코드 정의
// -------------------------------------------------------------------------

public record JobStatRow(
    string JobClass,
    int BattleCount,
    double AvgScore,
    double MedianScore,
    double StddevScore,
    double P99Score,
    double AvgDuration,
    double MedianDuration,
    int RankByScore,
    int RankByDuration
);

public record CohortRow(
    string CohortWeek,
    int CohortUsers,
    int WeekNum,
    int ActiveUsers,
    double RetentionPct
);

public record FunnelRow(
    int StepOrder,
    string FunnelStep,
    int Users,
    double TotalCvrPct,
    double PrevStepCvrPct,
    int DropOff
);
