using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameLogAnalyzer.Simulator;

/// <summary>
/// 이벤트 목록을 날짜별 JSONL 파일로 저장한다.
/// 각 줄이 독립된 JSON 객체인 JSONL 형식 사용.
/// </summary>
public class JsonlWriter
{
    private readonly string _outputDir;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public JsonlWriter(string outputDir)
    {
        _outputDir = outputDir;
        Directory.CreateDirectory(outputDir);
    }

    /// <summary>
    /// 이벤트 목록을 날짜별로 분류하여 JSONL 파일로 저장한다.
    /// 파일명: logs_yyyy-MM-dd.jsonl
    /// </summary>
    public async Task WriteAsync(IEnumerable<object> events, DateTime date)
    {
        var filename = $"logs_{date:yyyy-MM-dd}.jsonl";
        var filepath = Path.Combine(_outputDir, filename);

        await using var writer = new StreamWriter(filepath, append: false);
        int count = 0;

        foreach (var evt in events)
        {
            string json = JsonSerializer.Serialize(evt, evt.GetType(), _jsonOptions);
            await writer.WriteLineAsync(json);
            count++;
        }

        Console.WriteLine($"  [저장] {filename} — {count:N0}개 이벤트");
    }

    public string GetFilePath(DateTime date)
        => Path.Combine(_outputDir, $"logs_{date:yyyy-MM-dd}.jsonl");
}
