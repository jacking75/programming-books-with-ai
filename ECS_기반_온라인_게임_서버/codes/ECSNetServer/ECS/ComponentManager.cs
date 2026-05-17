using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.ECS;


// 컴포넌트 매니저
public class ComponentManager
{
    // 엔티티별 컴포넌트 저장 (타입별)
    private readonly Dictionary<Type, Dictionary<EntityId, IComponent>> _components = new();

    // 컴포넌트 추가
    public void AddComponent<T>(EntityId entityId, T component) where T : IComponent
    {
        var type = typeof(T);

        if (!_components.TryGetValue(type, out var componentsOfType))
        {
            componentsOfType = new Dictionary<EntityId, IComponent>();
            _components[type] = componentsOfType;
        }

        componentsOfType[entityId] = component;
    }

    // 컴포넌트 가져오기
    public T GetComponent<T>(EntityId entityId) where T : IComponent
    {
        var type = typeof(T);

        if (_components.TryGetValue(type, out var componentsOfType) &&
            componentsOfType.TryGetValue(entityId, out var component))
        {
            return (T)component;
        }

        return default;
    }

    // 컴포넌트 제거
    public bool RemoveComponent<T>(EntityId entityId) where T : IComponent
    {
        var type = typeof(T);

        if (_components.TryGetValue(type, out var componentsOfType))
        {
            return componentsOfType.Remove(entityId);
        }

        return false;
    }

    // 엔티티가 컴포넌트를 가지고 있는지 확인
    public bool HasComponent<T>(EntityId entityId) where T : IComponent
    {
        var type = typeof(T);

        return _components.TryGetValue(type, out var componentsOfType) &&
               componentsOfType.ContainsKey(entityId);
    }

    // 특정 타입의 컴포넌트를 가진 모든 엔티티 가져오기
    public IEnumerable<EntityId> GetEntitiesWithComponent<T>() where T : IComponent
    {
        var type = typeof(T);

        if (_components.TryGetValue(type, out var componentsOfType))
        {
            return componentsOfType.Keys;
        }

        return Enumerable.Empty<EntityId>();
    }

    // 여러 컴포넌트를 모두 가진 엔티티 가져오기
    public IEnumerable<EntityId> GetEntitiesWithComponents(Type[] componentTypes)
    {
        if (componentTypes == null || componentTypes.Length == 0)
        {
            return Enumerable.Empty<EntityId>();
        }

        // 첫 번째 컴포넌트 타입으로 시작
        if (!_components.TryGetValue(componentTypes[0], out var firstComponentDict))
        {
            return Enumerable.Empty<EntityId>();
        }

        var result = new HashSet<EntityId>(firstComponentDict.Keys);

        // 나머지 컴포넌트 타입에 대해 교집합 구하기
        for (int i = 1; i < componentTypes.Length; i++)
        {
            if (!_components.TryGetValue(componentTypes[i], out var nextComponentDict))
            {
                return Enumerable.Empty<EntityId>();
            }

            result.IntersectWith(nextComponentDict.Keys);

            if (result.Count == 0)
            {
                break;
            }
        }

        return result;
    }

    // 엔티티의 모든 컴포넌트 제거
    public void RemoveAllComponents(EntityId entityId)
    {
        foreach (var componentsOfType in _components.Values)
        {
            componentsOfType.Remove(entityId);
        }
    }
}