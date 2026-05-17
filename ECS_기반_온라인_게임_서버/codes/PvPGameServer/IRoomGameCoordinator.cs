using PvPGameServer.Ecs.Components;
using PvPGameServer.Ecs.Entities;

namespace PvPGameServer;

public interface IRoomGameCoordinator
{
    void OnPlayerEntered(RoomEntity room, RoomOccupant occupant);
    void OnPlayerLeft(RoomEntity room, RoomOccupant occupant);
    void OnRoomOwnerChanged(RoomEntity room, RoomOccupant? newOwner);
}
