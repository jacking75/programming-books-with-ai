namespace GameLogAnalyzer.Models;

public class ItemEvent
{
    public string EventType { get; init; } = "item";
    public string PlayerId { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string Date { get; init; } = "";
    public string Action { get; init; } = "";     // acquire, consume, sell, craft
    public string ItemId { get; init; } = "";
    public string ItemName { get; init; } = "";
    public string ItemCategory { get; init; } = ""; // weapon, armor, potion, material
    public int Quantity { get; init; }
    public int GoldValue { get; init; }
    public string Source { get; init; } = "";     // dungeon, shop, craft, event
}
