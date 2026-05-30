namespace GameLogAnalyzer.Models;

/// <summary>
/// 플레이어 타입 — 과금 성향을 나타낸다
/// </summary>
public enum PlayerType
{
    Free,       // 무과금 플레이어
    Light,      // 소과금 플레이어 (월 1~5만원)
    Heavy,      // 중과금 플레이어 (월 5~20만원)
    Whale       // 고과금 플레이어 (월 20만원 이상)
}

/// <summary>
/// 플레이 시간 패턴
/// </summary>
public enum PlayPattern
{
    Morning,    // 아침 출근 전 (7~9시)
    Lunch,      // 점심 시간 (12~13시)
    Evening,    // 퇴근 후 저녁 (18~22시)
    Night,      // 심야 (22~2시)
    Casual      // 랜덤 시간대
}

/// <summary>
/// 가상 플레이어 프로파일
/// </summary>
public class Player
{
    public string PlayerId { get; init; } = "";
    public string Nickname { get; init; } = "";
    public int Level { get; set; }
    public PlayerType Type { get; init; }
    public PlayPattern Pattern { get; init; }

    // 플레이어별 특성 (현실적 분포를 위한 시드값)
    public double ActivityRate { get; init; }   // 0.0~1.0, 접속 확률
    public double BattleSkill { get; init; }    // 0.0~1.0, 전투 실력
    public int AvgSessionMinutes { get; init; } // 평균 세션 길이(분)
}
