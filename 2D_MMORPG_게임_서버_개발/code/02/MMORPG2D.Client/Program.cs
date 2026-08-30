using MMORPG2D.Client.Net;

// 2주차 클라이언트: API 서버를 통해 회원가입/로그인/캐릭터 관리 플로우를 콘솔에서 실행해 본다.
// 3주차에서 MonoGame UI 로 교체한다.

const string ApiBase = "http://localhost:5050";

var api = new ApiClient(ApiBase);

Console.Write("login id: "); var id = Console.ReadLine() ?? "";
Console.Write("password: "); var pw = Console.ReadLine() ?? "";

if (!await api.LoginAsync(id, pw))
{
    Console.WriteLine("로그인 실패. 회원가입을 시도한다.");
    if (await api.RegisterAsync(id, pw) && await api.LoginAsync(id, pw))
        Console.WriteLine("회원가입 + 로그인 성공.");
    else
    {
        Console.WriteLine("회원가입까지 실패."); return;
    }
}

Console.WriteLine($"token = {api.Token}");

var worlds = await api.GetWorldsAsync();
Console.WriteLine("=== 월드 ===");
foreach (var w in worlds) Console.WriteLine($"  [{w.Id}] {w.Name} (cap {w.Capacity})");

var chars = await api.ListCharactersAsync();
if (chars.Count == 0)
{
    Console.Write("캐릭터가 없다. 새 이름을 입력하라: ");
    var name = Console.ReadLine() ?? "Hero";
    var newId = await api.CreateCharacterAsync(worlds[0].Id, name);
    Console.WriteLine($"생성된 캐릭터 id = {newId}");
    chars = await api.ListCharactersAsync();
}

Console.WriteLine("=== 내 캐릭터 ===");
foreach (var c in chars)
    Console.WriteLine($"  [{c.Id}] {c.Name} Lv{c.Level} world={c.WorldId} ({c.PosX},{c.PosY})");

Console.WriteLine();
Console.WriteLine("3주차에서 이 토큰과 캐릭터 id 를 들고 게임 서버에 입장한다.");

