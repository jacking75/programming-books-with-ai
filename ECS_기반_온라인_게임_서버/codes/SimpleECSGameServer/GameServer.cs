using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;


// 게임 서버 클래스
public class GameServer
{
    private readonly ECSWorld world;
    private readonly INetworkManager networkManager;
    private readonly CancellationTokenSource cts;
    private bool isRunning;

    public GameServer()
    {
        world = new ECSWorld();
        networkManager = new SimpleNetworkManager();
        cts = new CancellationTokenSource();

        // 시스템 등록
        world.RegisterSystem(new MovementSystem(world));
        world.RegisterSystem(new HealthSystem(world, OnHealthChanged));
        world.RegisterSystem(new NetworkSystem(world, networkManager));
    }

    public void Start()
    {
        if (isRunning) return;

        isRunning = true;
        Console.WriteLine("게임 서버 시작...");

        // 게임 루프 시작
        Task.Run(() => GameLoop(cts.Token));
    }

    public void Stop()
    {
        if (!isRunning) return;

        cts.Cancel();
        isRunning = false;
        Console.WriteLine("게임 서버 종료...");
    }

    private async Task GameLoop(CancellationToken token)
    {
        const float targetFrameTime = 1.0f / 30.0f; // 30 FPS
        DateTime lastUpdateTime = DateTime.Now;

        while (!token.IsCancellationRequested)
        {
            // 델타 타임 계산
            var currentTime = DateTime.Now;
            float deltaTime = (float)(currentTime - lastUpdateTime).TotalSeconds;
            lastUpdateTime = currentTime;

            // ECS 업데이트
            world.Update(deltaTime);

            // 프레임 레이트 유지
            float sleepTime = targetFrameTime - (float)(DateTime.Now - currentTime).TotalSeconds;
            if (sleepTime > 0)
            {
                await Task.Delay((int)(sleepTime * 1000), token);
            }
        }
    }

    // 플레이어 연결 처리
    public uint ConnectPlayer(string playerName)
    {
        // 클라이언트 ID 생성 (실제로는 네트워크 연결에서 얻음)
        uint clientId = (uint)new Random().Next(1000, 10000);

        // 플레이어 엔티티 생성
        var playerEntity = world.CreateEntity();
        playerEntity.AddComponent(new PlayerComponent(clientId, playerName));
        playerEntity.AddComponent(new PositionComponent(0, 0));
        playerEntity.AddComponent(new VelocityComponent(0, 0));
        playerEntity.AddComponent(new HealthComponent(100, 100));
        playerEntity.AddComponent(new NetworkComponent(clientId));

        Console.WriteLine($"플레이어 연결: {playerName} (ID: {playerEntity.ID}, ClientID: {clientId})");

        // 클라이언트에게 초기 정보 전송
        networkManager.SendMessageToClient(clientId, $"INIT:{playerEntity.ID}");

        return clientId;
    }

    // 플레이어 연결 해제 처리
    public void DisconnectPlayer(uint clientId)
    {
        var entities = world.GetEntitiesWithComponents<NetworkComponent, PlayerComponent>();

        foreach (var entity in entities)
        {
            var network = entity.GetComponent<NetworkComponent>();
            if (network.ClientId == clientId)
            {
                var player = entity.GetComponent<PlayerComponent>();
                Console.WriteLine($"플레이어 연결 해제: {player.Name} (ID: {entity.ID}, ClientID: {clientId})");

                world.DestroyEntity(entity.ID);
                break;
            }
        }
    }

    // 클라이언트 명령 시뮬레이션
    public void SimulateClientCommand(uint clientId, string command)
    {
        ((SimpleNetworkManager)networkManager).SimulateClientMessage(clientId, command);
    }

    // 체력 변경 이벤트 처리
    private void OnHealthChanged(uint entityId, int current, int maximum)
    {
        // 클라이언트에 체력 변경 알림
        networkManager.BroadcastMessage($"HEALTH:{entityId}:{current}:{maximum}");
    }
}