// 로거 생성
var logger = new ECSNetServer.Core.ConsoleLogger();

// 네트워크 리스너 생성
var listener = new ECSNetServer.Network.TcpNetworkListener();
var networkManager = new ECSNetServer.Network.NetworkManager(listener);

// 게임 월드 생성
var world = new ECSNetServer.ECS.GameWorld(logger);

// 시스템 매니저 생성
var systemManager = new ECSNetServer.ECS.SystemManager(logger);

// 게임 시스템 등록
systemManager.RegisterSystem(new ECSNetServer.Systems.MovementSystem(world, logger));
systemManager.RegisterSystem(new ECSNetServer.Systems.NetworkSyncSystem(world, networkManager, logger));

// 게임 서버 생성
var gameServer = new ECSNetServer.Core.GameServer(world, systemManager, networkManager, logger);

// 서버 시작
int port = 7777;
await gameServer.StartAsync(port);

Console.WriteLine("서버가 시작되었습니다. 종료하려면 아무 키나 누르세요...");
Console.ReadKey();

// 서버 종료
await gameServer.StopAsync();