// code/ch07/JsonLogWriter/Program.cs
using GameLogWriter;

// 로그 디렉터리 생성
Directory.CreateDirectory("logs");

// 로그 라이터 생성
await using var logger = new JsonLogWriter("logs/game-2026-03-28.jsonl");

// 접속 로그 기록
logger.Write(new SessionLogEvent
{
    EventType     = "login",
    PlayerId      = "P001234",
    ServerId      = "S-KR-01",
    Ip            = "203.0.113.42",
    ClientVersion = "2.5.1"
});

// 전투 로그 기록 (논블로킹 — 게임 루프에서 사용)
logger.Write(new CombatLogEvent
{
    PlayerId   = "P001234",
    SkillId    = 305,
    TargetId   = "MON_0088",
    Damage     = 4520,
    IsCritical = true
});

// 아이템 로그 기록
logger.Write(new ItemLogEvent
{
    PlayerId = "P001234",
    ItemId   = 10045,
    ItemName = "강화석 +7",
    Action   = "acquire",
    Quantity = 1
});

// 결제 로그 기록
logger.Write(new PaymentLogEvent
{
    PlayerId      = "P001234",
    ProductId     = "PACK_DIAMOND_100",
    Amount        = 11000m,
    Currency      = "KRW",
    PaymentMethod = "mobile"
});

// 멀티스레드 시뮬레이션: 여러 스레드에서 동시 기록
var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(() =>
{
    for (int j = 0; j < 100; j++)
    {
        logger.Write(new CombatLogEvent
        {
            PlayerId   = $"P{i:D6}",
            SkillId    = 100 + (j % 10),
            TargetId   = $"MON_{j:D4}",
            Damage     = Random.Shared.Next(100, 9999),
            IsCritical = Random.Shared.NextDouble() > 0.8
        });
    }
}));

await Task.WhenAll(tasks);

Console.WriteLine("로그 기록 완료. DisposeAsync 호출 시 버퍼 플러시.");
Console.WriteLine($"파일 위치: logs/game-2026-03-28.jsonl");
