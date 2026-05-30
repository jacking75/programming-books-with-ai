// code/ch07/JsonLogWriter/LogEvents.cs
using System.Text.Json.Serialization;

namespace GameLogWriter;

// 모든 로그 이벤트의 기반 클래스
public abstract record BaseLogEvent
{
    [JsonPropertyName("log_type")]
    public abstract string LogType { get; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

// 접속 로그
public record SessionLogEvent : BaseLogEvent
{
    public override string LogType => "session";

    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }  // "login" or "logout"

    [JsonPropertyName("player_id")]
    public required string PlayerId { get; init; }

    [JsonPropertyName("server_id")]
    public required string ServerId { get; init; }

    [JsonPropertyName("ip")]
    public required string Ip { get; init; }

    [JsonPropertyName("client_version")]
    public required string ClientVersion { get; init; }
}

// 전투 로그
public record CombatLogEvent : BaseLogEvent
{
    public override string LogType => "combat";

    [JsonPropertyName("player_id")]
    public required string PlayerId { get; init; }

    [JsonPropertyName("skill_id")]
    public required int SkillId { get; init; }

    [JsonPropertyName("target_id")]
    public required string TargetId { get; init; }

    [JsonPropertyName("damage")]
    public required int Damage { get; init; }

    [JsonPropertyName("is_critical")]
    public required bool IsCritical { get; init; }
}

// 아이템 로그
public record ItemLogEvent : BaseLogEvent
{
    public override string LogType => "item";

    [JsonPropertyName("player_id")]
    public required string PlayerId { get; init; }

    [JsonPropertyName("item_id")]
    public required int ItemId { get; init; }

    [JsonPropertyName("item_name")]
    public required string ItemName { get; init; }

    [JsonPropertyName("action")]
    public required string Action { get; init; }  // "acquire", "consume", "trade"

    [JsonPropertyName("quantity")]
    public required int Quantity { get; init; }
}

// 결제 로그
public record PaymentLogEvent : BaseLogEvent
{
    public override string LogType => "payment";

    [JsonPropertyName("player_id")]
    public required string PlayerId { get; init; }

    [JsonPropertyName("product_id")]
    public required string ProductId { get; init; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; init; }

    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    [JsonPropertyName("payment_method")]
    public required string PaymentMethod { get; init; }
}
