using MemoryPack;
using MMORPG2D.Client.Net;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

const string ApiBase = "http://localhost:5050";
const string GameHost = "127.0.0.1";
const int GamePort = 7777;
const int MaxMovePacketsPerSecond = 5;
const int MoveSendIntervalMs = 1000 / MaxMovePacketsPerSecond;

var api = new ApiClient(ApiBase);
Console.Write("login id: "); var id = Console.ReadLine() ?? "";
Console.Write("password: "); var pw = Console.ReadLine() ?? "";
if (!await api.LoginAsync(id, pw)) { Console.WriteLine("login failed"); return; }

var chars = await api.ListCharactersAsync();
if (chars.Count == 0) { Console.WriteLine("no character"); return; }
var ch = chars[0];

using var net = new GameNetClient();
await net.ConnectAsync(GameHost, GamePort);
net.Send(PacketId.ReqEnterGame, new ReqEnterGame { Token = api.Token!, CharacterId = ch.Id });

long myId = 0; int myX = 0, myY = 0; string myName = "";
var others = new Dictionary<long, (string Name, int X, int Y)>();
var lastMoveSentMs = 0L;

Console.WriteLine("WASD = move, Q = quit");
while (true)
{
    while (net.TryRead(out var pid, out var body))
    {
        switch (pid)
        {
            case PacketId.ResEnterGame:
                var ent = MemoryPackSerializer.Deserialize<ResEnterGame>(body)!;
                if (ent.ResultCode != 0) { Console.WriteLine($"enter failed code={ent.ResultCode}"); return; }
                myId = ent.CharacterId; myX = ent.X; myY = ent.Y; myName = ent.Name;
                Console.WriteLine($"[me] {myName} ({myX},{myY})");
                break;

            case PacketId.NtfPlayerList:
                var list = MemoryPackSerializer.Deserialize<NtfPlayerList>(body)!;
                foreach (var p in list.Players)
                    others[p.Id] = (p.Name, p.X, p.Y);
                Console.WriteLine($"[list] {list.Players.Length} other players in sight");
                break;

            case PacketId.NtfPlayerEnter:
                var ne = MemoryPackSerializer.Deserialize<NtfPlayerEnter>(body)!;
                others[ne.Player.Id] = (ne.Player.Name, ne.Player.X, ne.Player.Y);
                Console.WriteLine($"[+] {ne.Player.Name} entered sight at ({ne.Player.X},{ne.Player.Y})");
                break;

            case PacketId.NtfPlayerLeave:
                var nl = MemoryPackSerializer.Deserialize<NtfPlayerLeave>(body)!;
                if (others.Remove(nl.Id, out var who))
                    Console.WriteLine($"[-] {who.Name} left sight");
                break;

            case PacketId.NtfPlayerMove:
                var nm = MemoryPackSerializer.Deserialize<NtfPlayerMove>(body)!;
                if (others.TryGetValue(nm.Id, out var prev))
                    others[nm.Id] = (prev.Name, nm.X, nm.Y);
                break;

            case PacketId.ResMove:
                var mv = MemoryPackSerializer.Deserialize<ResMove>(body)!;
                if (mv.CharacterId == myId) { myX = mv.X; myY = mv.Y; }
                Console.WriteLine($"[me] -> ({myX},{myY}) others={others.Count}");
                break;
        }
    }

    if (Console.KeyAvailable)
    {
        var k = Console.ReadKey(intercept: true);
        if (k.Key == ConsoleKey.Q) break;
        byte? dir = k.Key switch
        {
            ConsoleKey.W => 0, ConsoleKey.D => 1,
            ConsoleKey.S => 2, ConsoleKey.A => 3,
            _ => null,
        };
        if (dir.HasValue)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs - lastMoveSentMs >= MoveSendIntervalMs)
            {
                net.Send(PacketId.ReqMove, new ReqMove { Dir = dir.Value });
                lastMoveSentMs = nowMs;
            }
        }
    }
    await Task.Delay(16);
}
