using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.ECS;



// 엔티티 매니저
public class EntityManager
{
    private int _nextEntityId = 1;
    private readonly HashSet<EntityId> _entities = new();

    public EntityId CreateEntity()
    {
        var entityId = new EntityId(_nextEntityId++);
        _entities.Add(entityId);
        return entityId;
    }

    public bool DestroyEntity(EntityId entityId)
    {
        return _entities.Remove(entityId);
    }

    public bool EntityExists(EntityId entityId)
    {
        return _entities.Contains(entityId);
    }

    public IEnumerable<EntityId> GetAllEntities()
    {
        return _entities;
    }
}