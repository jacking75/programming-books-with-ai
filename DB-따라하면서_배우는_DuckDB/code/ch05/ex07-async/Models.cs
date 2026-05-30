// code/ch05/ex07-async/Models.cs

// 플레이어 모델 (PlayerService와 Program.cs에서 공유)
public record Player(
    long PlayerId,
    string PlayerName,
    int Level,
    long Experience,
    long Gold,
    int ServerId,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
