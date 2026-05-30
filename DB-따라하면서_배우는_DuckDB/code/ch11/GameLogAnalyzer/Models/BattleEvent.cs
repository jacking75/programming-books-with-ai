namespace GameLogAnalyzer.Models;

public class BattleEvent
{
    public string EventType { get; init; } = "battle";
    public string PlayerId { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string Date { get; init; } = "";
    public string DungeonId { get; init; } = "";
    public string DungeonName { get; init; } = "";
    public string Difficulty { get; init; } = ""; // Normal, Hard, Extreme
    public bool IsSuccess { get; init; }
    public int DurationSeconds { get; init; }
    public int DamageDealt { get; init; }
    public int DamageTaken { get; init; }
    public int GoldEarned { get; init; }
    public int ExpEarned { get; init; }
    public int PlayerLevel { get; init; }
}
