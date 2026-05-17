using System;
using System.Collections.Generic;
using System.Linq;

namespace PvPGameServer.Ecs;

public sealed class Entity
{
    readonly Dictionary<Type, IComponent> _components = new();
    readonly object _syncRoot = new();

    public Entity(int id, EntityType type)
    {
        Id = id;
        Type = type;
    }

    public int Id { get; }
    public EntityType Type { get; }

    public void AddComponent<TComponent>(TComponent component)
        where TComponent : class, IComponent
    {
        ArgumentNullException.ThrowIfNull(component);

        lock (_syncRoot)
        {
            var componentType = typeof(TComponent);
            if (_components.ContainsKey(componentType))
            {
                throw new InvalidOperationException($"Entity {this} already has component {componentType.Name}.");
            }

            _components.Add(componentType, component);
        }
    }

    public void SetComponent<TComponent>(TComponent component)
        where TComponent : class, IComponent
    {
        ArgumentNullException.ThrowIfNull(component);

        lock (_syncRoot)
        {
            _components[typeof(TComponent)] = component;
        }
    }

    public bool TryGetComponent<TComponent>(out TComponent component)
        where TComponent : class, IComponent
    {
        lock (_syncRoot)
        {
            if (_components.TryGetValue(typeof(TComponent), out var stored))
            {
                component = (TComponent)stored;
                return true;
            }
        }

        component = null;
        return false;
    }

    public TComponent GetComponent<TComponent>()
        where TComponent : class, IComponent
    {
        if (TryGetComponent<TComponent>(out var component))
        {
            return component;
        }

        throw new KeyNotFoundException($"Entity {this} does not have component {typeof(TComponent).Name}.");
    }

    public bool RemoveComponent<TComponent>()
        where TComponent : class, IComponent
    {
        lock (_syncRoot)
        {
            return _components.Remove(typeof(TComponent));
        }
    }

    public bool HasComponent<TComponent>()
        where TComponent : class, IComponent
    {
        lock (_syncRoot)
        {
            return _components.ContainsKey(typeof(TComponent));
        }
    }

    public IReadOnlyCollection<IComponent> GetAllComponents()
    {
        lock (_syncRoot)
        {
            return _components.Values.ToArray();
        }
    }

    public override string ToString()
    {
        return $"{Type}#{Id}";
    }
}
