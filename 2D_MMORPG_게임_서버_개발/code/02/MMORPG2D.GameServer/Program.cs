using Microsoft.Extensions.Configuration;
using SuperSocketLite.SocketBase.Config;
using MMORPG2D.GameServer;
using MMORPG2D.GameServer.Handlers;

// 1) 설정 읽기
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var ip = config["GameServer:Ip"] ?? "Any";
var port = int.Parse(config["GameServer:Port"] ?? "7777");
var name = config["GameServer:Name"] ?? "GameServer";

// 2) DB / Redis 헬스 체크 (학습용)
await BootHealthCheck.RunAsync(
    config["Database:ConnectionString"]!,
    config["Redis:ConnectionString"]!);

// 3) 서버 본체 셋업
var server = new GameServer();
var ok = server.Setup(
    new RootConfig(),
    new ServerConfig
    {
        Name = name,
        Ip = ip,
        Port = port,
        Mode = SuperSocketLite.SocketBase.SocketMode.Tcp,
        MaxConnectionNumber = 1000,
        MaxRequestLength = 1024 * 16,
    });

if (!ok)
{
    Logger.Error("server setup failed");
    return;
}

// 4) 핸들러 등록
var handler = new PacketHandler();
EchoHandler.Register(handler);
server.NewRequestReceived += (s, req) => handler.Dispatch(s, req);

// 5) 시작
if (!server.Start())
{
    Logger.Error("server start failed");
    return;
}

Logger.Info($"server started on {ip}:{port}. press Ctrl+C to quit.");
await Task.Delay(-1);
