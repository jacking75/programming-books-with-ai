using System;
using PvPGameServer.Ecs.Components;

namespace PvPGameServer.Ecs.Entities;

public sealed class RoomEntity
{
    public RoomEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Type != EntityType.Room)
        {
            throw new ArgumentException($"Entity {entity} is not a room entity.", nameof(entity));
        }

        Entity = entity;
    }

    public Entity Entity { get; }

    public int Id => Entity.Id;

    public RoomInfoComponent Info => Entity.GetComponent<RoomInfoComponent>();

    public RoomCapacityComponent Capacity => Entity.GetComponent<RoomCapacityComponent>();

    public RoomOccupancyComponent Occupancy => Entity.GetComponent<RoomOccupancyComponent>();

    public static RoomEntity Create(EntityWorld world, int roomIndex, int roomNumber, int maxUserCount)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (maxUserCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUserCount), maxUserCount, "Room must allow at least one user.");
        }

        var entity = world.CreateEntity(EntityType.Room);
        entity.AddComponent(new RoomInfoComponent(roomIndex, roomNumber));
        entity.AddComponent(new RoomCapacityComponent(maxUserCount));
        entity.AddComponent(new RoomOccupancyComponent());

        return new RoomEntity(entity);
    }

    public override string ToString()
    {
        return Entity.ToString();
    }
}
