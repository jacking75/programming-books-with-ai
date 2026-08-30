using System.Collections.Concurrent;
using System.Net.Sockets;
using MMORPG2D.Shared;

namespace MMORPG2D.Client.Net;

/// <summary>
/// 게임 서버와의 TCP 연결을 다룬다. 수신 스레드가 inbox 큐에 쌓고,
/// 게임 루프 (혹은 메인 스레드) 가 TryRead 로 비운다.
/// </summary>
public class GameNetClient : IDisposable
{
    private readonly TcpClient _tcp = new();
    private NetworkStream? _ns;
    private readonly ConcurrentQueue<(PacketId, byte[])> _inbox = new();
    private CancellationTokenSource? _cts;

    public bool IsConnected => _tcp.Connected;

    public async Task ConnectAsync(string host, int port)
    {
        await _tcp.ConnectAsync(host, port);
        _ns = _tcp.GetStream();
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    public void Send<T>(PacketId id, T body)
    {
        if (_ns is null) throw new InvalidOperationException("not connected");
        var bytes = PacketEncoder.Encode(id, body);
        _ns.Write(bytes, 0, bytes.Length);
    }

    public bool TryRead(out PacketId id, out byte[] body)
    {
        if (_inbox.TryDequeue(out var pair))
        {
            id = pair.Item1; body = pair.Item2; return true;
        }
        id = default; body = Array.Empty<byte>(); return false;
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var header = new byte[PacketEncoder.HeaderSize];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await ReadExact(_ns!, header, header.Length, ct);
                var totalLen = PacketEncoder.ReadTotalLength(header);
                var pktId = PacketEncoder.ReadPacketId(header);
                var bodyLen = totalLen - PacketEncoder.HeaderSize;
                var body = new byte[bodyLen];
                if (bodyLen > 0)
                    await ReadExact(_ns!, body, bodyLen, ct);
                _inbox.Enqueue((pktId, body));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[net] recv loop ended: {ex.Message}");
        }
    }

    private static async Task ReadExact(NetworkStream ns, byte[] buf, int len, CancellationToken ct)
    {
        int total = 0;
        while (total < len)
        {
            var n = await ns.ReadAsync(buf.AsMemory(total, len - total), ct);
            if (n == 0) throw new EndOfStreamException("server closed");
            total += n;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _ns?.Dispose();
        _tcp.Dispose();
    }
}
