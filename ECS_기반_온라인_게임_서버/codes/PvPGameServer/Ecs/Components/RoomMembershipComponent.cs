namespace PvPGameServer.Ecs.Components;

public sealed class RoomMembershipComponent : IComponent
{
    public int? RoomEntityId { get; private set; }
    public int? RoomNumber { get; private set; }

    public bool IsInRoom => RoomEntityId.HasValue;

    public void EnterRoom(int roomEntityId, int roomNumber)
    {
        RoomEntityId = roomEntityId;
        RoomNumber = roomNumber;
    }

    public void LeaveRoom()
    {
        RoomEntityId = null;
        RoomNumber = null;
    }
}
