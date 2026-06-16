// 게임 서버 연결당 비동기 루프 패턴 — 본문 5.6.1 예제
// (Program.cs에서 직접 실행되지는 않지만, 코드로 참고할 수 있도록 포함)

using System.Net.Sockets;

namespace Ch05_ServerAsync;

public sealed class GameSession
{
    private readonly TcpClient _client;
    private readonly CancellationTokenSource _cts = new();

    public GameSession(TcpClient client) => _client = client;

    public async Task RunAsync()
    {
        var stream = _client.GetStream();
        var buffer = new byte[4096];

        try
        {
            while (!_cts.IsCancellationRequested)
            {
                int n = await stream.ReadAsync(buffer, _cts.Token);
                if (n == 0) break;
                await HandlePacketAsync(buffer.AsMemory(0, n));
            }
        }
        catch (OperationCanceledException) { /* 정상 종료 */ }
    }

    public void Stop() => _cts.Cancel();

    private static Task HandlePacketAsync(ReadOnlyMemory<byte> data)
    {
        // TODO: 실제 게임 패킷 처리 (DB 조회, 월드 업데이트 등)
        return Task.CompletedTask;
    }
}
