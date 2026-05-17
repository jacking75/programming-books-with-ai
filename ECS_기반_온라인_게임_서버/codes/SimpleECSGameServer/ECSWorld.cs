using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;


// --------------------------------
// ECS 월드 클래스
// --------------------------------
public class ECSWorld
{
    private uint nextEntityId = 1;
    private Dictionary<uint, Entity> entities = new();
    private List<ISystem> systems = new();

    // 엔티티 생성
    public Entity CreateEntity()
    {
        var id = nextEntityId++;
        var entity = new Entity(id);
        entities[id] = entity;
        return entity;
    }

    // 엔티티 제거
    public void DestroyEntity(uint entityId)
    {
        if (entities.ContainsKey(entityId))
        {
            entities.Remove(entityId);
        }
    }

    // 엔티티 가져오기
    public bool TryGetEntity(uint entityId, out Entity entity)
    {
        return entities.TryGetValue(entityId, out entity);
    }

    // 특정 컴포넌트를 가진 엔티티들 가져오기
    public IEnumerable<Entity> GetEntitiesWithComponents<T>() where T : IComponent
    {
        return entities.Values.Where(e => e.HasComponent<T>());
    }

    // 여러 컴포넌트를 가진 엔티티들 가져오기
    public IEnumerable<Entity> GetEntitiesWithComponents<T1, T2>()
        where T1 : IComponent
        where T2 : IComponent
    {
        var types = new Type[] { typeof(T1), typeof(T2) };
        return entities.Values.Where(e => e.HasComponents(types));
    }

    // 시스템 등록
    public void RegisterSystem(ISystem system)
    {
        systems.Add(system);
    }

    // 모든 시스템 업데이트
    public void Update(float deltaTime)
    {
        foreach (var system in systems)
        {
            system.Update(deltaTime);
        }
    }

    // 엔티티 수 반환
    public int EntityCount => entities.Count;
}
