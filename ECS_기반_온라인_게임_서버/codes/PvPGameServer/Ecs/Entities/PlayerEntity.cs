using System;
using PvPGameServer.Ecs.Components;

namespace PvPGameServer.Ecs.Entities;

public sealed class PlayerEntity
{
    public PlayerEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Type != EntityType.Player)
        {
            throw new ArgumentException($"Entity {entity} is not a player entity.", nameof(entity));
        }

        Entity = entity;
    }

    public Entity Entity { get; }

    public int Id => Entity.Id;

    public PlayerIdentityComponent Identity => Entity.GetComponent<PlayerIdentityComponent>();

    public NetworkSessionComponent Session => Entity.GetComponent<NetworkSessionComponent>();

    public RoomMembershipComponent Membership => Entity.GetComponent<RoomMembershipComponent>();

    public PlayerLifecycleComponent Lifecycle => Entity.GetComponent<PlayerLifecycleComponent>();

    public static PlayerEntity Create(EntityWorld world, ulong sequenceNumber, string userId, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or whitespace.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));
        }

        var entity = world.CreateEntity(EntityType.Player);

        entity.AddComponent(new PlayerIdentityComponent(sequenceNumber, userId));
        entity.AddComponent(new NetworkSessionComponent(sessionId));
        entity.AddComponent(new RoomMembershipComponent());
        entity.AddComponent(new PlayerLifecycleComponent(PlayerLifecycleState.Connected));

        return new PlayerEntity(entity);
    }

    public override string ToString()
    {
        return Entity.ToString();
    }
}
