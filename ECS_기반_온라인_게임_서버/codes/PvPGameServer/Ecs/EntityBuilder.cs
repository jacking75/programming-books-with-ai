namespace PvPGameServer.Ecs;

public sealed class EntityBuilder
{
    readonly Entity _entity;

    public EntityBuilder(Entity entity)
    {
        _entity = entity;
    }

    public EntityBuilder With<TComponent>(TComponent component) where TComponent : class, IComponent
    {
        _entity.AddComponent(component);
        return this;
    }

    public EntityBuilder Set<TComponent>(TComponent component) where TComponent : class, IComponent
    {
        _entity.SetComponent(component);
        return this;
    }

    public Entity Build()
    {
        return _entity;
    }
}
