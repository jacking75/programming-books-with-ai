using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleECSGameServer;


// --------------------------------
// 시스템 인터페이스
// --------------------------------
public interface ISystem
{
    void Update(float deltaTime);
}

// --------------------------------
// 이동 시스템 구현
// --------------------------------
public class MovementSystem : ISystem
{
    private readonly ECSWorld world;

    public MovementSystem(ECSWorld world)
    {
        this.world = world;
    }

    public void Update(float deltaTime)
    {
        var entities = world.GetEntitiesWithComponents<PositionComponent, VelocityComponent>();

        foreach (var entity in entities)
        {
            var position = entity.GetComponent<PositionComponent>();
            var velocity = entity.GetComponent<VelocityComponent>();

            position.X += velocity.X * deltaTime;
            position.Y += velocity.Y * deltaTime;

            entity.AddComponent(position);
        }
    }
}

// --------------------------------
// 체력 시스템 구현
// --------------------------------
public class HealthSystem : ISystem
{
    private readonly ECSWorld world;
    private readonly Action<uint, int, int> onHealthChanged;

    public HealthSystem(ECSWorld world, Action<uint, int, int> onHealthChanged = null)
    {
        this.world = world;
        this.onHealthChanged = onHealthChanged;
    }

    public void Update(float deltaTime)
    {
        var entities = world.GetEntitiesWithComponents<HealthComponent>();

        foreach (var entity in entities)
        {
            var health = entity.GetComponent<HealthComponent>();

            // 체력 관련 로직
            if (health.Current <= 0)
            {
                world.DestroyEntity(entity.ID);
            }

            // 체력 변경 알림
            onHealthChanged?.Invoke(entity.ID, health.Current, health.Maximum);
        }
    }

    public void DamageEntity(uint entityId, int damage)
    {
        if (world.TryGetEntity(entityId, out var entity) && entity.HasComponent<HealthComponent>())
        {
            var health = entity.GetComponent<HealthComponent>();
            health.Current = Math.Max(0, health.Current - damage);
            entity.AddComponent(health);
        }
    }

    public void HealEntity(uint entityId, int amount)
    {
        if (world.TryGetEntity(entityId, out var entity) && entity.HasComponent<HealthComponent>())
        {
            var health = entity.GetComponent<HealthComponent>();
            health.Current = Math.Min(health.Maximum, health.Current + amount);
            entity.AddComponent(health);
        }
    }
}