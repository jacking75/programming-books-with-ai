namespace GameLogAnalyzer.Models;

public class LoginEvent
{
    public string EventType { get; init; } = "login";
    public string PlayerId { get; init; } = "";
    public string Nickname { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string Date { get; init; } = "";       // yyyy-MM-dd
    public int Level { get; init; }
    public string Region { get; init; } = "";     // KR, US, JP, EU
    public string Platform { get; init; } = "";   // PC, Mobile
    public int SessionDurationSeconds { get; init; }
    public bool IsFirstLogin { get; init; }
}
