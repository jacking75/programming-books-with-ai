// code/ch07/LogRotation/Program.cs
using GameLogWriter;

Console.WriteLine("=== 로그 로테이션 데모 ===");
Console.WriteLine();

// --- 1. 기본 로테이션 (날짜별) ---
Console.WriteLine("[1] 날짜별 로테이션 라이터 생성 (logs/ 디렉터리)");
await using var combatLogger = new RotatingJsonLogWriter(
    logDirectory:      "logs",
    logPrefix:         "combat",
    maxFileSizeBytes:  1_073_741_824L  // 1GB
);

// 전투 로그 100건 기록
for (int i = 0; i < 100; i++)
{
    combatLogger.Write(new CombatLogEvent
    {
        PlayerId   = $"P{i % 10:D6}",
        SkillId    = 100 + (i % 5),
        TargetId   = $"MON_{i:D4}",
        Damage     = Random.Shared.Next(500, 9999),
        IsCritical = Random.Shared.NextDouble() > 0.75
    });
}

// 접속 로그용 별도 라이터
await using var sessionLogger = new RotatingJsonLogWriter(
    logDirectory:     "logs",
    logPrefix:        "session",
    maxFileSizeBytes: 104_857_600L  // 100MB
);

sessionLogger.Write(new SessionLogEvent
{
    EventType     = "login",
    PlayerId      = "P000001",
    ServerId      = "S-KR-01",
    Ip            = "203.0.113.1",
    ClientVersion = "2.5.1"
});

sessionLogger.Write(new SessionLogEvent
{
    EventType     = "logout",
    PlayerId      = "P000001",
    ServerId      = "S-KR-01",
    Ip            = "203.0.113.1",
    ClientVersion = "2.5.1"
});

Console.WriteLine();

// --- 2. 크기별 로테이션 시뮬레이션 ---
Console.WriteLine("[2] 크기별 로테이션 시뮬레이션 (파일 크기 한도: 1KB)");
await using var smallLogger = new RotatingJsonLogWriter(
    logDirectory:     "logs/small-test",
    logPrefix:        "test",
    maxFileSizeBytes: 1024  // 1KB - 로테이션이 자주 발생하도록 설정
);

for (int i = 0; i < 50; i++)
{
    smallLogger.Write(new CombatLogEvent
    {
        PlayerId   = $"P{i:D6}",
        SkillId    = 200,
        TargetId   = $"MON_{i:D4}",
        Damage     = 1000 + i,
        IsCritical = i % 3 == 0
    });
}

Console.WriteLine();
Console.WriteLine("DisposeAsync 호출 중... (버퍼의 모든 로그 플러시)");

// DisposeAsync는 await using 종료 시 자동 호출됨
// 여기서는 명시적으로 표시하기 위한 메시지만 출력

Console.WriteLine();
Console.WriteLine("=== 생성된 파일 구조 ===");
Console.WriteLine("logs/");

if (Directory.Exists("logs"))
{
    foreach (string file in Directory.EnumerateFiles("logs", "*.jsonl", SearchOption.AllDirectories))
    {
        var info = new FileInfo(file);
        Console.WriteLine($"  {file.Replace("logs/", "").Replace("logs\\", "")} ({info.Length:N0} bytes)");
    }
}

Console.WriteLine();
Console.WriteLine("로테이션 데모 완료.");
Console.WriteLine();
Console.WriteLine("파일명 규칙:");
Console.WriteLine("  combat-YYYY-MM-DD.jsonl       ← 당일 첫 번째 파일");
Console.WriteLine("  combat-YYYY-MM-DD_001.jsonl   ← 1GB 초과 후 두 번째 파일");
Console.WriteLine("  combat-YYYY-MM-DD_002.jsonl   ← 세 번째 파일");
