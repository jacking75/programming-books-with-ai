// code/ch07/CsvLogWriter/Program.cs
using GameCsvWriter;

// 로그 디렉터리 생성
Directory.CreateDirectory("logs");

string[] headers = ["timestamp", "player_id", "skill_id",
                    "target_id", "damage", "is_critical"];

using var csv = new CsvLogWriter("logs/combat-2026-03-28.csv");
csv.WriteHeader(headers);

// 전투 이벤트 기록
csv.WriteRow([
    DateTime.UtcNow.ToString("o"),
    "P001234",
    "305",
    "MON_0088",
    "4520",
    "true"
]);

// 특수 문자가 포함된 경우 (아이템 이름에 쉼표가 있는 경우)
csv.WriteRow([
    DateTime.UtcNow.ToString("o"),
    "P001234",
    "강화석, +7",   // 쉼표 포함 → 자동으로 "강화석, +7"로 이스케이프
    "acquire",
    "1",
    ""
]);

// 큰따옴표가 포함된 경우
csv.WriteRow([
    DateTime.UtcNow.ToString("o"),
    "P005678",
    "100",
    "MON_0010",
    "\"치명타\"_데미지",  // 큰따옴표 포함 → ""치명타""_데미지 로 이스케이프
    "true"
]);

// 멀티스레드 시뮬레이션: 여러 스레드에서 동시 기록
var tasks = Enumerable.Range(1, 5).Select(i => Task.Run(() =>
{
    for (int j = 0; j < 20; j++)
    {
        csv.WriteRow([
            DateTime.UtcNow.ToString("o"),
            $"P{i:D6}",
            (100 + j % 10).ToString(),
            $"MON_{j:D4}",
            Random.Shared.Next(100, 9999).ToString(),
            (Random.Shared.NextDouble() > 0.8).ToString().ToLower()
        ]);
    }
}));

await Task.WhenAll(tasks);

csv.Flush();
Console.WriteLine("CSV 기록 완료");
Console.WriteLine("파일 위치: logs/combat-2026-03-28.csv");

// EscapeCsvField 동작 확인
Console.WriteLine();
Console.WriteLine("=== RFC 4180 이스케이프 테스트 ===");
Console.WriteLine($"일반 값:        {CsvLogWriter.EscapeCsvField("hello")}");
Console.WriteLine($"쉼표 포함:      {CsvLogWriter.EscapeCsvField("hello, world")}");
Console.WriteLine($"큰따옴표 포함:  {CsvLogWriter.EscapeCsvField("say \"hi\"")}");
Console.WriteLine($"줄바꿈 포함:    {CsvLogWriter.EscapeCsvField("line1\nline2")}");
