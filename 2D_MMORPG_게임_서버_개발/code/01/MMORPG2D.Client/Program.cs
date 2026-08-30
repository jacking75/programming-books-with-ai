using System.Buffers.Binary;
using System.Net.Sockets;
using MemoryPack;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

// 1주차 클라이언트는 콘솔이다. 2주차에서 MonoGame 프로젝트로 교체된다.
const string Host = "127.0.0.1";
const int Port = 7777;

using var tcp = new TcpClient();
Console.WriteLine($"connecting to {Host}:{Port} ...");
await tcp.ConnectAsync(Host, Port);
Console.WriteLine("connected");

var ns = tcp.GetStream();
using var cts = new CancellationTokenSource();

// 백그라운드 수신 루프
var recv = Task.Run(() => ReceiveLoop(ns, cts.Token));

// 사용자 입력 루프
Console.WriteLine("type a message, ENTER to send. /quit to exit.");
while (true)
{
    var line = Console.ReadLine();
    if (line == null || line == "/quit") break;
    var sentAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var bytes = PacketEncoder.Encode(PacketId.ReqEcho, new ReqEcho { Message = line });
    await ns.WriteAsync(bytes, 0, bytes.Length, cts.Token);
    Console.WriteLine($"> sent at {sentAt} ms");
}

cts.Cancel();
try { await recv; } catch { /* ignore */ }
Console.WriteLine("bye");

// ----------------------------------------------------------------

static async Task ReceiveLoop(NetworkStream ns, CancellationToken ct)
{
    var header = new byte[PacketEncoder.HeaderSize];
    while (!ct.IsCancellationRequested)
    {
        await ReadExact(ns, header, header.Length, ct);
        var totalLen = PacketEncoder.ReadTotalLength(header);
        var pktId = PacketEncoder.ReadPacketId(header);
        var bodyLen = totalLen - PacketEncoder.HeaderSize;
        var body = new byte[bodyLen];
        if (bodyLen > 0)
            await ReadExact(ns, body, bodyLen, ct);

        Dispatch(pktId, body);
    }
}

static async Task ReadExact(NetworkStream ns, byte[] buf, int len, CancellationToken ct)
{
    int total = 0;
    while (total < len)
    {
        var n = await ns.ReadAsync(buf.AsMemory(total, len - total), ct);
        if (n == 0) throw new EndOfStreamException("server closed");
        total += n;
    }
}

static void Dispatch(PacketId id, byte[] body)
{
    switch (id)
    {
        case PacketId.ResEcho:
            var res = MemoryPackSerializer.Deserialize<ResEcho>(body)!;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Console.WriteLine(
                $"< ResEcho '{res.Message}' (server t={res.ServerTimeUtcMs}, " +
                $"client t={nowMs}, gap={nowMs - res.ServerTimeUtcMs} ms)");
            break;
        default:
            Console.WriteLine($"< unknown packet {id} ({body.Length} bytes)");
            break;
    }
}
