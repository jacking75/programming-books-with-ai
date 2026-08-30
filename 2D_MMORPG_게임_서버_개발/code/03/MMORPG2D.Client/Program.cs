using MemoryPack;
using MMORPG2D.Client.Net;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

const string ApiBase = "http://localhost:5050";
const string GameHost = "127.0.0.1";
const int GamePort = 7777;

var api = new ApiClient(ApiBase);

Console.Write("login id: "); var id = Console.ReadLine() ?? "";
Console.Write("password: "); var pw = Console.ReadLine() ?? "";
if (!await api.LoginAsync(id, pw)) { Console.WriteLine("login failed"); return; }

var chars = await api.ListCharactersAsync();
if (chars.Count == 0) { Console.WriteLine("no character. run week02 client first."); return; }
var ch = chars[0];
Console.WriteLine($"입장할 캐릭터: {ch.Name} (id={ch.Id})");

using var net = new GameNetClient();
await net.ConnectAsync(GameHost, GamePort);

net.Send(PacketId.ReqEnterGame, new ReqEnterGame
{
    Token = api.Token!, CharacterId = ch.Id
});

long myId = 0; int myX = 0, myY = 0; string myName = "";

Console.WriteLine("WASD 로 이동, Q 로 종료");
while (true)
{
    while (net.TryRead(out var pid, out var body))
    {
        switch (pid)
        {
            case PacketId.ResEnterGame:
                var ent = MemoryPackSerializer.Deserialize<ResEnterGame>(body)!;
                if (ent.ResultCode != 0)
                {
                    Console.WriteLine($"입장 실패 code={ent.ResultCode}");
                    return;
                }
                myId = ent.CharacterId; myX = ent.X; myY = ent.Y; myName = ent.Name;
                Console.WriteLine($"입장 OK: {myName} ({myX},{myY})");
                break;
            case PacketId.ResMove:
                var mv = MemoryPackSerializer.Deserialize<ResMove>(body)!;
                if (mv.CharacterId == myId) { myX = mv.X; myY = mv.Y; }
                Console.WriteLine($"  move → ({myX},{myY})");
                break;
        }
    }

    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Q) break;
        byte? dir = key.Key switch
        {
            ConsoleKey.W => 0, ConsoleKey.D => 1,
            ConsoleKey.S => 2, ConsoleKey.A => 3,
            _ => null,
        };
        if (dir.HasValue) net.Send(PacketId.ReqMove, new ReqMove { Dir = dir.Value });
    }
    await Task.Delay(16);
}
Console.WriteLine("bye");

