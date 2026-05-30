// code/ch07/LogRotation/RotatingJsonLogWriter.cs
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace GameLogWriter;

// 모든 로그 이벤트의 기반 클래스 (이 프로젝트에서 재정의)
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
    public required string EventType { get; init; }

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

/// <summary>
/// 날짜별 + 크기별 로테이션을 지원하는 JSONL 로그 라이터.
/// </summary>
public sealed class RotatingJsonLogWriter : IAsyncDisposable
{
    private readonly string _logDirectory;
    private readonly string _logPrefix;        // 예: "combat"
    private readonly long _maxFileSizeBytes;   // 예: 1GB = 1073741824L

    private readonly Channel<string> _channel;
    private readonly Task _writerTask;
    private readonly JsonSerializerOptions _jsonOptions;

    // 현재 파일 상태 (단일 소비자 스레드에서만 접근)
    private StreamWriter? _currentWriter;
    private string _currentFilePath = "";
    private DateTime _currentFileDate;
    private int _rotationIndex;
    private long _currentFileSize;

    public RotatingJsonLogWriter(
        string logDirectory,
        string logPrefix,
        long maxFileSizeBytes = 1_073_741_824L)  // 기본 1GB
    {
        _logDirectory     = logDirectory;
        _logPrefix        = logPrefix;
        _maxFileSizeBytes = maxFileSizeBytes;

        Directory.CreateDirectory(logDirectory);

        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100_000)
        {
            FullMode     = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
        _writerTask  = Task.Run(WriteLoopAsync);
    }

    public void Write<T>(T logEvent) where T : BaseLogEvent
    {
        string json = JsonSerializer.Serialize(logEvent, logEvent.GetType(), _jsonOptions);
        _channel.Writer.TryWrite(json);
    }

    public async ValueTask WriteAsync<T>(T logEvent, CancellationToken ct = default)
        where T : BaseLogEvent
    {
        string json = JsonSerializer.Serialize(logEvent, logEvent.GetType(), _jsonOptions);
        await _channel.Writer.WriteAsync(json, ct);
    }

    private async Task WriteLoopAsync()
    {
        await foreach (string json in _channel.Reader.ReadAllAsync())
        {
            await EnsureFileOpenAsync();

            // 실제 크기: UTF-8 인코딩 후 바이트 수 추적
            int lineBytes = System.Text.Encoding.UTF8.GetByteCount(json + Environment.NewLine);
            _currentFileSize += lineBytes;

            await _currentWriter!.WriteLineAsync(json);

            if (_channel.Reader.Count == 0)
                await _currentWriter.FlushAsync();
        }

        if (_currentWriter != null)
        {
            await _currentWriter.FlushAsync();
            await _currentWriter.DisposeAsync();
        }
    }

    /// <summary>현재 파일이 열려 있고 로테이션 조건을 충족하지 않는지 확인한다.</summary>
    private async Task EnsureFileOpenAsync()
    {
        DateTime today = DateTime.UtcNow.Date;

        bool needRotate =
            _currentWriter == null ||              // 아직 열리지 않음
            _currentFileDate != today ||            // 날짜 변경
            _currentFileSize >= _maxFileSizeBytes;  // 크기 초과

        if (!needRotate) return;

        // 기존 파일 닫기
        if (_currentWriter != null)
        {
            await _currentWriter.FlushAsync();
            await _currentWriter.DisposeAsync();
        }

        // 날짜가 바뀌면 로테이션 인덱스 초기화
        if (_currentFileDate != today)
        {
            _currentFileDate = today;
            _rotationIndex   = 0;
        }
        else
        {
            _rotationIndex++;
        }

        // 파일명 결정
        string datePart = today.ToString("yyyy-MM-dd");
        string fileName = _rotationIndex == 0
            ? $"{_logPrefix}-{datePart}.jsonl"
            : $"{_logPrefix}-{datePart}_{_rotationIndex:D3}.jsonl";

        _currentFilePath = Path.Combine(_logDirectory, fileName);
        _currentFileSize = 0;

        var fs = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write,
                                FileShare.Read, 65536, useAsync: true);
        _currentWriter = new StreamWriter(fs, System.Text.Encoding.UTF8);

        Console.WriteLine($"[LogRotation] 새 파일 생성: {_currentFilePath}");
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        await _writerTask;
    }
}
