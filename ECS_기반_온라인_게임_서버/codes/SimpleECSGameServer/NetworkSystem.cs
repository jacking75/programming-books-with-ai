using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;


public class NetworkSystem : ISystem
{
    private readonly ECSWorld world;
    private readonly INetworkManager networkManager;

    public NetworkSystem(ECSWorld world, INetworkManager networkManager)
    {
        this.world = world;
        this.networkManager = networkManager;

        // 클라이언트 메시지 수신 이벤트 등록
        networkManager.OnMessageReceived += HandleClientMessage;
    }

    public void Update(float deltaTime)
    {
        // 엔티티 상태 동기화 등 네트워크 관련 처리
        var entities = world.GetEntitiesWithComponents<NetworkComponent, PositionComponent>();

        foreach (var entity in entities)
        {
            var network = entity.GetComponent<NetworkComponent>();
            var position = entity.GetComponent<PositionComponent>();

            // 위치 정보 전송
            networkManager.SendMessageToClient(
                network.ClientId,
                $"POS:{entity.ID}:{position.X}:{position.Y}"
            );
        }
    }

    private void HandleClientMessage(uint clientId, string message)
    {
        // 간단한, 메시지 처리 로직
        var parts = message.Split(':');

        if (parts.Length < 1) return;

        switch (parts[0])
        {
            case "MOVE":
                if (parts.Length >= 3 &&
                    float.TryParse(parts[1], out float x) &&
                    float.TryParse(parts[2], out float y))
                {
                    HandleMoveCommand(clientId, x, y);
                }
                break;

            case "ATTACK":
                if (parts.Length >= 2 &&
                    uint.TryParse(parts[1], out uint targetId))
                {
                    HandleAttackCommand(clientId, targetId);
                }
                break;
        }
    }

    private void HandleMoveCommand(uint clientId, float x, float y)
    {
        // 클라이언트 엔티티 찾기
        var playerEntity = FindPlayerEntityByClientId(clientId);

        if (playerEntity != null && playerEntity.HasComponent<VelocityComponent>())
        {
            // 속도 컴포넌트 업데이트
            var velocity = new VelocityComponent(x, y);
            playerEntity.AddComponent(velocity);
        }
    }

    private void HandleAttackCommand(uint clientId, uint targetId)
    {
        // 간단한 공격 처리 로직
        var playerEntity = FindPlayerEntityByClientId(clientId);

        if (playerEntity != null &&
            world.TryGetEntity(targetId, out var targetEntity) &&
            targetEntity.HasComponent<HealthComponent>())
        {
            // 공격 로직 (체력 시스템을 통해 데미지 처리)
            var healthSystem = world.GetSystem<HealthSystem>();
            healthSystem?.DamageEntity(targetId, 10);

            // 공격 결과 알림
            networkManager.BroadcastMessage($"ATTACK:{playerEntity.ID}:{targetId}:10");
        }
    }

    private Entity FindPlayerEntityByClientId(uint clientId)
    {
        var entities = world.GetEntitiesWithComponents<NetworkComponent, PlayerComponent>();

        foreach (var entity in entities)
        {
            var network = entity.GetComponent<NetworkComponent>();
            if (network.ClientId == clientId)
            {
                return entity;
            }
        }

        return null;
    }
}