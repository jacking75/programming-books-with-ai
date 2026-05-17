using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Systems;


// 이동 시스템
public class MovementSystem : ECS.ISystem
{
    private readonly ECS.GameWorld _world;
    private readonly Core.ILogger _logger;

    public MovementSystem(ECS.GameWorld world, Core.ILogger logger)
    {
        _world = world;
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.LogInfo("이동 시스템 초기화됨");
    }

    public void Update(Core.TickContext context)
    {
        // 위치와 속도 컴포넌트를 모두 가진 엔티티를 찾는다
        var componentTypes = new[]
        {
                typeof(Components.PositionComponent),
                typeof(Components.VelocityComponent)
            };

        var entities = _world.ComponentManager.GetEntitiesWithComponents(componentTypes);

        foreach (var entity in entities)
        {
            var position = _world.ComponentManager.GetComponent<Components.PositionComponent>(entity);
            var velocity = _world.ComponentManager.GetComponent<Components.VelocityComponent>(entity);

            // 위치 업데이트
            var newPosition = new Components.PositionComponent(
                position.X + velocity.VX * context.DeltaTime,
                position.Y + velocity.VY * context.DeltaTime
            );

            // 업데이트된 위치 저장
            _world.ComponentManager.AddComponent(entity, newPosition);
        }
    }
}
