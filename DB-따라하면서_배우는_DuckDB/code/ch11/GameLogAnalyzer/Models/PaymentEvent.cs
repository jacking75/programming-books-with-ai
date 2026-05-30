namespace GameLogAnalyzer.Models;

public class PaymentEvent
{
    public string EventType { get; init; } = "payment";
    public string PlayerId { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string Date { get; init; } = "";
    public string ProductId { get; init; } = "";
    public string ProductName { get; init; } = "";
    public int AmountKrw { get; init; }           // 결제금액 (원화)
    public string Currency { get; init; } = "KRW";
    public string PaymentMethod { get; init; } = ""; // card, mobile, gift
    public string Store { get; init; } = "";      // AppStore, GooglePlay, Steam, PC
}
