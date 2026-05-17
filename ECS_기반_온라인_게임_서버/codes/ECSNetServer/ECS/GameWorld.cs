using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.ECS;

// 게임 월드 클래스
public class GameWorld
{
    public EntityManager EntityManager { get; }
    public ComponentManager ComponentManager { get; }

    private readonly Core.ILogger _logger;
    private readonly List<EntityId> _entitiesToDestroy = new();

    public GameWorld(Core.ILogger logger)
    {
        EntityManager = new EntityManager();
        ComponentManager = new ComponentManager();
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInfo("게임 월드 초기화됨");
    }

    public EntityId CreateEntity()
    {
        return EntityManager.CreateEntity();
    }

    public void DestroyEntity(EntityId entityId)
    {
        // 엔티티 제거를 다음 클린업 시점으로 지연
        _entitiesToDestroy.Add(entityId);
    }

    public void CleanUp()
    {
        // 제거 예약된 엔티티들 처리
        foreach (var entityId in _entitiesToDestroy)
        {
            // 엔티티의 모든 컴포넌트 제거
            ComponentManager.RemoveAllComponents(entityId);

            // 엔티티 제거
            EntityManager.DestroyEntity(entityId);
        }

        if (_entitiesToDestroy.Count > 0)
        {
            _logger.LogInfo($"{_entitiesToDestroy.Count}개의 엔티티가 제거됨");
            _entitiesToDestroy.Clear();
        }
    }
}