using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Systems;


// 네트워크 상태 전파 시스템
public class NetworkSyncSystem : ECS.ISystem
{
    private readonly ECS.GameWorld _world;
    private readonly Network.NetworkManager _networkManager;
    private readonly Core.ILogger _logger;

    // 다음 상태 동기화 틱
    private long _nextSyncTick = 0;
    // 동기화 간격 (틱)
    private const int SyncInterval = 5;

    public NetworkSyncSystem(
        ECS.GameWorld world,
        Network.NetworkManager networkManager,
        Core.ILogger logger)
    {
        _world = world;
        _networkManager = networkManager;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInfo("네트워크 동기화 시스템 초기화됨");
    }

    public void Update(Core.TickContext context)
    {
        // 설정된 간격마다 상태 동기화
        if (context.CurrentTick >= _nextSyncTick)
        {
            SyncGameState();
            _nextSyncTick = context.CurrentTick + SyncInterval;
        }
    }

    private void SyncGameState()
    {
        // 위치와 네트워크 컴포넌트를 모두 가진 엔티티를 찾는다
        var componentTypes = new[]
        {
                typeof(Components.PositionComponent),
                typeof(Components.NetworkComponent)
            };

        var entities = _world.ComponentManager.GetEntitiesWithComponents(componentTypes);

        foreach (var entity in entities)
        {
            var position = _world.ComponentManager.GetComponent<Components.PositionComponent>(entity);
            var network = _world.ComponentManager.GetComponent<Components.NetworkComponent>(entity);

            // 여기서는 간단한 예시만 구현
            // 실제로는 위치 정보를 담은 패킷을 생성하여 전송해야 함
            var positionUpdatePacket = new Network.Packets.PositionUpdatePacket
            {
                EntityId = entity.Id,
                X = position.X,
                Y = position.Y
            };

            // 패킷 직렬화 및 전송
            var packetData = positionUpdatePacket.Serialize();

            // 실제 구현에서는 브로드캐스트 대신 관련 클라이언트에게만 전송
            _networkManager.BroadcastAsync(packetData).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger.LogError($"패킷 전송 오류: {t.Exception?.InnerException?.Message}");
                }
            });
        }
    }
}