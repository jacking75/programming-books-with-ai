namespace PvPGameServer.Ecs.Components;

public sealed class RoomInfoComponent : IComponent
{
    public RoomInfoComponent(int index, int number)
    {
        Index = index;
        Number = number;
    }

    public int Index { get; }
    public int Number { get; }
}
