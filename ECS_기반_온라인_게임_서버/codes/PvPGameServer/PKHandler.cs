using System;
using PvPGameServer.Ecs.Entities;
using PvPGameServer.Ecs.Systems;


namespace PvPGameServer;

public class PKHandler
{
    public static Func<string, byte[], bool> NetSendFunc;
    public static Action<MemoryPackBinaryRequestInfo> DistributeInnerPacket;

    protected PlayerSystem _playerSystem;
    protected RoomSystem _roomSystem;


    public void Init(PlayerSystem playerSystem, RoomSystem roomSystem)
    {
        _playerSystem = playerSystem ?? throw new ArgumentNullException(nameof(playerSystem));
        _roomSystem = roomSystem ?? throw new ArgumentNullException(nameof(roomSystem));
    }           
    protected void Broadcast(RoomEntity room, string excludeSessionId, byte[] sendPacket)
    {
        if (room == null)
        {
            throw new ArgumentNullException(nameof(room));
        }

        foreach (var occupant in _roomSystem.GetOccupants(room))
        {
            if (string.Equals(occupant.SessionId, excludeSessionId, StringComparison.Ordinal))
            {
                continue;
            }

            NetSendFunc(occupant.SessionId, sendPacket);
        }
    }

    protected void Broadcast(RoomEntity room, byte[] sendPacket)
        => Broadcast(room, string.Empty, sendPacket);
}
