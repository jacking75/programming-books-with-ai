using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PvPGameServer.Ecs;

public sealed class EntityWorld : IEnumerable<Entity>, IDisposable
{
    readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    readonly Dictionary<int, Entity> _entities = new();
    readonly Dictionary<EntityType, HashSet<int>> _typeIndex = new();
    int _nextEntityId = 1;

    public Entity CreateEntity(EntityType type)
    {
        Entity entity;

        _lock.EnterWriteLock();
        try
        {
            var entityId = _nextEntityId++;
            entity = new Entity(entityId, type);
            _entities.Add(entityId, entity);

            if (_typeIndex.TryGetValue(type, out var set) == false)
            {
                set = new HashSet<int>();
                _typeIndex[type] = set;
            }

            set.Add(entityId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return entity;
    }

    public bool DestroyEntity(int entityId)
    {
        EntityType entityType;

        _lock.EnterWriteLock();
        try
        {
            if (_entities.TryGetValue(entityId, out var entity) == false)
            {
                return false;
            }

            entityType = entity.Type;
            _entities.Remove(entityId);
            if (_typeIndex.TryGetValue(entityType, out var set))
            {
                set.Remove(entityId);
                if (set.Count == 0)
                {
                    _typeIndex.Remove(entityType);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        return true;
    }

    public bool TryGetEntity(int entityId, out Entity entity)
    {
        _lock.EnterReadLock();
        try
        {
            if (_entities.TryGetValue(entityId, out entity))
            {
                return true;
            }

            entity = null;
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IReadOnlyCollection<Entity> GetEntities(EntityType type)
    {
        _lock.EnterReadLock();
        try
        {
            if (_typeIndex.TryGetValue(type, out var set) == false)
            {
                return Array.Empty<Entity>();
            }

            var result = new List<Entity>(set.Count);
            foreach (var id in set)
            {
                if (_entities.TryGetValue(id, out var entity))
                {
                    result.Add(entity);
                }
            }

            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public IEnumerator<Entity> GetEnumerator()
    {
        List<Entity> snapshot;

        _lock.EnterReadLock();
        try
        {
            snapshot = _entities.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _entities.Clear();
            _typeIndex.Clear();
            _nextEntityId = 1;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
