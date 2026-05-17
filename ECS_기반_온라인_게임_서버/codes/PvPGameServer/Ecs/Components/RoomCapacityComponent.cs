namespace PvPGameServer.Ecs.Components;

public sealed class RoomCapacityComponent : IComponent
{
    public RoomCapacityComponent(int maxUserCount)
    {
        MaxUserCount = maxUserCount;
    }

    public int MaxUserCount { get; }
}
