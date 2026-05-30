// code/ch05/ex04-mapping/Models.cs

// 플레이어 모델
public record Player(
    long PlayerId,
    string PlayerName,
    int Level,
    long Experience,
    long Gold,
    int ServerId,
    DateTime CreatedAt,
    DateTime? LastLoginAt  // NULL 허용 필드는 Nullable 타입으로
);

// 전투 로그 모델
public record BattleLog(
    long LogId,
    long PlayerId,
    int MonsterId,
    int DamageDealt,
    int DamageTaken,
    bool IsVictory,
    double? BattleDuration,
    DateTime LoggedAt
);

// 아이템 모델
public record Item(
    int ItemId,
    string ItemName,
    string ItemType,
    string Rarity,
    int BasePower
);
