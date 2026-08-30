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
int myHp = 0, myMaxHp = 0, myLevel = 1; long myExp = 0;
byte myDir = 2; // facing south by default
var others = new Dictionary<long, (string Name, int X, int Y, int Hp, int MaxHp)>();
var lastMoveSentMs = 0L;

Console.WriteLine("WASD = move, SPACE = attack, Q = quit");
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
                    others[p.Id] = (p.Name, p.X, p.Y, 100, 100);
                Console.WriteLine($"[list] {list.Players.Length} other(s) in sight");
                break;

            case PacketId.NtfPlayerEnter:
                var ne = MemoryPackSerializer.Deserialize<NtfPlayerEnter>(body)!;
                others[ne.Player.Id] = (ne.Player.Name, ne.Player.X, ne.Player.Y, 100, 100);
                Console.WriteLine($"[+] {ne.Player.Name} entered");
                break;

            case PacketId.NtfPlayerLeave:
                var nl = MemoryPackSerializer.Deserialize<NtfPlayerLeave>(body)!;
                if (others.Remove(nl.Id, out var who))
                    Console.WriteLine($"[-] {who.Name} left");
                break;

            case PacketId.NtfPlayerMove:
                var nm = MemoryPackSerializer.Deserialize<NtfPlayerMove>(body)!;
                if (others.TryGetValue(nm.Id, out var prev))
                    others[nm.Id] = (prev.Name, nm.X, nm.Y, prev.Hp, prev.MaxHp);
                break;

            case PacketId.ResMove:
                var mv = MemoryPackSerializer.Deserialize<ResMove>(body)!;
                if (mv.CharacterId == myId) { myX = mv.X; myY = mv.Y; }
                break;

            case PacketId.NtfAttack:
                var atk = MemoryPackSerializer.Deserialize<NtfAttack>(body)!;
                var actor = atk.ActorId == myId ? myName
                    : (others.TryGetValue(atk.ActorId, out var a) ? a.Name : "?");
                Console.WriteLine($"[*] {actor} swings (target={atk.TargetId})");
                break;

            case PacketId.NtfHpChange:
                var hp = MemoryPackSerializer.Deserialize<NtfHpChange>(body)!;
                if (hp.Id == myId)
                {
                    var dmg = myHp - hp.Hp;
                    myHp = hp.Hp; myMaxHp = hp.MaxHp;
                    Console.WriteLine($"[me] HP {myHp}/{myMaxHp} (-{dmg})");
                }
                else if (others.TryGetValue(hp.Id, out var p))
                {
                    var dmg = p.Hp - hp.Hp;
                    others[hp.Id] = (p.Name, p.X, p.Y, hp.Hp, hp.MaxHp);
                    Console.WriteLine($"[{p.Name}] HP {hp.Hp}/{hp.MaxHp} (-{dmg})");
                }
                break;

            case PacketId.NtfDie:
                var die = MemoryPackSerializer.Deserialize<NtfDie>(body)!;
                Console.WriteLine($"[X] {die.Id} died (killer={die.KillerId})");
                break;

            case PacketId.NtfReward:
                var rw = MemoryPackSerializer.Deserialize<NtfReward>(body)!;
                myExp = rw.NewExpTotal; myLevel = rw.NewLevel;
                Console.WriteLine($"[reward] +{rw.Exp}exp -> total {myExp}, lv{myLevel}{(rw.LeveledUp ? " LEVELUP" : "")}");
                break;

            case PacketId.NtfRespawn:
                var rs = MemoryPackSerializer.Deserialize<NtfRespawn>(body)!;
                if (rs.Id == myId) { myX = rs.X; myY = rs.Y; myHp = rs.Hp; myMaxHp = rs.MaxHp; }
                else if (others.TryGetValue(rs.Id, out var p2))
                    others[rs.Id] = (p2.Name, rs.X, rs.Y, rs.Hp, rs.MaxHp);
                Console.WriteLine($"[respawn] {rs.Id} at ({rs.X},{rs.Y})");
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
            myDir = dir.Value;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs - lastMoveSentMs >= MoveSendIntervalMs)
            {
                net.Send(PacketId.ReqMove, new ReqMove { Dir = dir.Value });
                lastMoveSentMs = nowMs;
            }
        }
        else if (k.Key == ConsoleKey.Spacebar)
            net.Send(PacketId.ReqAttack, new ReqAttack { Dir = myDir });
    }
    await Task.Delay(16);
}
