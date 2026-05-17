using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;

// --------------------------------
// 엔티티 클래스
// --------------------------------
public class Entity
{
    public readonly uint ID;
    private Dictionary<Type, IComponent> components = new();

    public Entity(uint id)
    {
        ID = id;
    }

    public void AddComponent<T>(T component) where T : IComponent
    {
        components[typeof(T)] = component;
    }

    public void RemoveComponent<T>() where T : IComponent
    {
        components.Remove(typeof(T));
    }

    public T GetComponent<T>() where T : IComponent
    {
        return (T)components[typeof(T)];
    }

    public bool HasComponent<T>() where T : IComponent
    {
        return components.ContainsKey(typeof(T));
    }

    public bool HasComponents(Type[] types)
    {
        return types.All(t => components.ContainsKey(t));
    }
}