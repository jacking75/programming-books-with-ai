// 게임 서버 생성 및 시작
using SimpleECSGameServer;

var server = new GameServer();
server.Start();

// 테스트 플레이어 연결
uint player1Id = server.ConnectPlayer("플레이어1");
uint player2Id = server.ConnectPlayer("플레이어2");

// 테스트 명령어 시뮬레이션
await Task.Run(async () =>
{
    // 간단한 테스트 시나리오
    await Task.Delay(1000);
    server.SimulateClientCommand(player1Id, "MOVE:1:0");

    await Task.Delay(2000);
    server.SimulateClientCommand(player2Id, "MOVE:0:1");

    await Task.Delay(2000);
    server.SimulateClientCommand(player1Id, $"ATTACK:{player2Id}");

    await Task.Delay(5000);
    server.DisconnectPlayer(player1Id);
});

// 종료 대기
Console.WriteLine("서버를 종료하려면 아무 키나 누르세요...");
Console.ReadKey();

server.Stop();