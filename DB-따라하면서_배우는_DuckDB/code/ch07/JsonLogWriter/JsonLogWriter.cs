// code/ch07/JsonLogWriter/JsonLogWriter.cs
using System.Text.Json;
using System.Threading.Channels;

namespace GameLogWriter;

/// <summary>
/// 스레드 안전, 비동기 JSONL 로그 라이터.
/// Channel&lt;T&gt; 기반 생산자-소비자 패턴 사용.
/// </summary>
public sealed class JsonLogWriter : IAsyncDisposable
{
    private readonly Channel<string> _channel;
    private readonly Task _writerTask;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonLogWriter(string filePath)
    {
        _filePath = filePath;

        // 파일 디렉터리가 없으면 생성
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        // 버퍼 크기: 최대 10만 건. 초과 시 가장 오래된 항목 제거 (로그 유실 허용)
        // 게임 서버는 로그 유실보다 서버 응답 지연이 더 나쁜 상황
        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(100_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,   // 단일 소비자 (파일 쓰기 스레드)
            SingleWriter = false   // 다중 생산자 (여러 게임 스레드)
        });

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false  // JSONL은 한 줄로
        };

        // 백그라운드 파일 쓰기 작업 시작
        _writerTask = Task.Run(WriteLoopAsync);
    }

    /// <summary>로그 이벤트를 채널에 기록한다 (논블로킹).</summary>
    public void Write<T>(T logEvent) where T : BaseLogEvent
    {
        // Serialize를 호출하는 순간 타입 정보가 사라지므로
        // 실제 타입을 전달해야 다형성 직렬화가 동작한다
        string json = JsonSerializer.Serialize(logEvent, logEvent.GetType(), _jsonOptions);
        _channel.Writer.TryWrite(json);
    }

    /// <summary>비동기 로그 이벤트 기록 (버퍼가 가득 찰 때 대기).</summary>
    public async ValueTask WriteAsync<T>(T logEvent, CancellationToken ct = default)
        where T : BaseLogEvent
    {
        string json = JsonSerializer.Serialize(logEvent, logEvent.GetType(), _jsonOptions);
        await _channel.Writer.WriteAsync(json, ct);
    }

    /// <summary>백그라운드 파일 쓰기 루프.</summary>
    private async Task WriteLoopAsync()
    {
        // 파일을 append 모드로 열고, 비동기 I/O 사용
        await using var fs = new FileStream(
            _filePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 65536,  // 64KB 버퍼
            useAsync: true);

        await using var writer = new StreamWriter(fs, System.Text.Encoding.UTF8);

        await foreach (string json in _channel.Reader.ReadAllAsync())
        {
            await writer.WriteLineAsync(json);

            // 채널이 잠시 비었을 때 플러시 (배치 플러시로 I/O 횟수 최소화)
            if (_channel.Reader.Count == 0)
            {
                await writer.FlushAsync();
            }
        }

        // 채널 닫힐 때 마지막 플러시
        await writer.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        await _writerTask;
    }
}
