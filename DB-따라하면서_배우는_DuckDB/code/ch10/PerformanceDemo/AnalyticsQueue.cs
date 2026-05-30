// AnalyticsQueue.cs
// Channel 기반 백그라운드 분석 큐

using System.Threading.Channels;
using DuckDB.NET.Data;

/// <summary>
/// 분석 쿼리를 순차적으로 처리하는 백그라운드 큐.
/// 게임 서버의 메인 스레드를 블록하지 않으면서
/// DuckDB 커넥션을 안전하게 단일 스레드에서 사용한다.
/// </summary>
public class AnalyticsQueue : IAsyncDisposable
{
    private readonly Channel<Func<DuckDBConnection, Task>> _channel;
    private readonly DuckDBConnection _connection;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _cts = new();

    public int ProcessedCount { get; private set; }

    public AnalyticsQueue(string databasePath)
    {
        _channel = Channel.CreateBounded<Func<DuckDBConnection, Task>>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        _connection = new DuckDBConnection($"DataSource={databasePath}");
        _connection.Open();

        // game_logs 테이블이 없으면 생성
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS game_logs (
                log_time    TIMESTAMP NOT NULL,
                log_type    VARCHAR   NOT NULL,
                user_id     BIGINT    NOT NULL,
                server_id   INTEGER   NOT NULL,
                event_data  VARCHAR
            )";
        cmd.ExecuteNonQuery();

        // 단일 워커 스레드에서만 DuckDB 커넥션 사용
        _workerTask = Task.Run(ProcessQueueAsync);
    }

    /// <summary>
    /// 임의의 DuckDB 작업을 큐에 넣는다.
    /// </summary>
    public async Task EnqueueAsync(Func<DuckDBConnection, Task> work)
    {
        await _channel.Writer.WriteAsync(work, _cts.Token);
    }

    /// <summary>
    /// 로그 배치 삽입 편의 메서드.
    /// DuckDBAppender를 사용해 효율적으로 삽입한다.
    /// </summary>
    public Task EnqueueBatchInsert(IReadOnlyList<GameLogEntry> logs)
    {
        return EnqueueAsync(async conn =>
        {
            await Task.Run(() =>
            {
                using var appender = conn.CreateAppender("game_logs");
                foreach (var log in logs)
                {
                    appender.BeginRow();
                    appender.AppendValue(log.LogTime);
                    appender.AppendValue(log.LogType);
                    appender.AppendValue(log.UserId);
                    appender.AppendValue(log.ServerId);
                    appender.AppendValue(log.EventData);
                    appender.EndRow();
                }
            });
        });
    }

    /// <summary>
    /// SELECT 쿼리 결과를 콜백으로 처리하는 편의 메서드.
    /// </summary>
    public Task EnqueueQuery(string sql, Action<DuckDBDataReader> resultHandler)
    {
        return EnqueueAsync(async conn =>
        {
            await Task.Run(() =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                using var reader = cmd.ExecuteReader();
                resultHandler(reader);
            });
        });
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var work in _channel.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                await work(_connection);
                ProcessedCount++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AnalyticsQueue] Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 큐에 남은 모든 작업이 완료될 때까지 기다린다.
    /// </summary>
    public async Task FlushAsync()
    {
        var tcs = new TaskCompletionSource();
        await EnqueueAsync(_ =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });
        await tcs.Task;
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
        try
        {
            await _workerTask;
        }
        catch (OperationCanceledException)
        {
            // 정상 취소
        }
        _connection.Dispose();
        _cts.Dispose();
    }
}
