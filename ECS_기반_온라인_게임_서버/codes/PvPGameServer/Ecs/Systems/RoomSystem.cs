using System;
using System.Collections.Generic;
using PvPGameServer.Ecs.Components;
using PvPGameServer.Ecs.Entities;

namespace PvPGameServer.Ecs.Systems;

public sealed class RoomSystem
{
    readonly EntityWorld _world;
    readonly object _syncRoot = new();
    readonly Dictionary<int, RoomEntity> _roomsByNumber = new();
    readonly Dictionary<int, RoomEntity> _roomsByEntityId = new();
    readonly Dictionary<string, int> _roomEntityIdBySessionId = new(StringComparer.Ordinal);

    public RoomSystem(EntityWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public int RoomCount
    {
        get
        {
            lock (_syncRoot)
            {
                return _roomsByNumber.Count;
            }
        }
    }

    public void InitializeRooms(int roomCount, int roomStartNumber, int maxUserCount)
    {
        if (roomCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roomCount), roomCount, "At least one room is required.");
        }

        if (maxUserCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUserCount), maxUserCount, "Rooms must allow at least one user.");
        }

        lock (_syncRoot)
        {
            ClearRooms();

            for (var i = 0; i < roomCount; i++)
            {
                var roomNumber = roomStartNumber + i;
                var room = RoomEntity.Create(_world, i, roomNumber, maxUserCount);
                _roomsByNumber.Add(roomNumber, room);
                _roomsByEntityId.Add(room.Id, room);
            }
        }
    }

    public bool TryGetRoomByNumber(int roomNumber, out RoomEntity room)
    {
        lock (_syncRoot)
        {
            return _roomsByNumber.TryGetValue(roomNumber, out room);
        }
    }

    public bool TryGetRoomByEntityId(int roomEntityId, out RoomEntity room)
    {
        lock (_syncRoot)
        {
            return _roomsByEntityId.TryGetValue(roomEntityId, out room);
        }
    }

    public ErrorCode TryEnterRoom(int roomNumber, PlayerEntity player, out RoomEntity room, out RoomOccupant occupant)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_syncRoot)
        {
            if (_roomsByNumber.TryGetValue(roomNumber, out room) == false)
            {
                occupant = default;
                return ErrorCode.RoomEnterInvalidRoomNumber;
            }

            var candidate = new RoomOccupant(player.Id, player.Identity.UserId, player.Session.SessionId);
            if (room.Occupancy.TryAdd(candidate) == false)
            {
                occupant = default;
                return ErrorCode.RoomEnterFailAddUser;
            }

            _roomEntityIdBySessionId[player.Session.SessionId] = room.Id;
            player.Membership.EnterRoom(room.Id, room.Info.Number);
            player.Lifecycle.SetState(PlayerLifecycleState.InRoom);

            occupant = candidate;
            return ErrorCode.None;
        }
    }

    public bool TryRemovePlayerFromRoom(PlayerEntity player, out RoomEntity room, out RoomOccupant removed)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_syncRoot)
        {
            removed = default;
            room = null;

            if (player.Membership.IsInRoom == false || player.Membership.RoomEntityId.HasValue == false)
            {
                return false;
            }

            if (_roomsByEntityId.TryGetValue(player.Membership.RoomEntityId.Value, out room) == false)
            {
                _roomEntityIdBySessionId.Remove(player.Session.SessionId);
                player.Membership.LeaveRoom();
                player.Lifecycle.SetState(PlayerLifecycleState.LoggedIn);
                return false;
            }

            if (room.Occupancy.RemoveByEntityId(player.Id, out removed) == false)
            {
                _roomEntityIdBySessionId.Remove(player.Session.SessionId);
                player.Membership.LeaveRoom();
                player.Lifecycle.SetState(PlayerLifecycleState.LoggedIn);
                return false;
            }

            _roomEntityIdBySessionId.Remove(player.Session.SessionId);
            player.Membership.LeaveRoom();
            player.Lifecycle.SetState(PlayerLifecycleState.LoggedIn);
            return true;
        }
    }

    public bool TryRemovePlayerBySession(string sessionId, out RoomEntity room, out RoomOccupant removed)
    {
        room = null;
        removed = default;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        if (TryRemovePlayerBySessionInternal(sessionId, out room, out removed) == false)
        {
            return false;
        }

        if (_world.TryGetEntity(removed.PlayerEntityId, out var entity))
        {
            var player = new PlayerEntity(entity);
            player.Membership.LeaveRoom();
            player.Lifecycle.SetState(PlayerLifecycleState.LoggedIn);
        }

        return true;
    }

    public IReadOnlyCollection<RoomOccupant> GetOccupants(RoomEntity room)
    {
        ArgumentNullException.ThrowIfNull(room);
        return room.Occupancy.GetSnapshot();
    }

    void ClearRooms()
    {
        foreach (var room in _roomsByNumber.Values)
        {
            room.Occupancy.Clear();
            _world.DestroyEntity(room.Id);
        }

        _roomsByNumber.Clear();
        _roomsByEntityId.Clear();
        _roomEntityIdBySessionId.Clear();
    }

    bool TryRemovePlayerBySessionInternal(string sessionId, out RoomEntity room, out RoomOccupant removed)
    {
        lock (_syncRoot)
        {
            removed = default;
            room = null;

            if (_roomEntityIdBySessionId.TryGetValue(sessionId, out var roomEntityId) == false)
            {
                return false;
            }

            if (_roomsByEntityId.TryGetValue(roomEntityId, out room) == false)
            {
                _roomEntityIdBySessionId.Remove(sessionId);
                return false;
            }

            if (room.Occupancy.TryGetBySession(sessionId, out var occupant) == false)
            {
                _roomEntityIdBySessionId.Remove(sessionId);
                return false;
            }

            if (room.Occupancy.RemoveByEntityId(occupant.PlayerEntityId, out removed) == false)
            {
                _roomEntityIdBySessionId.Remove(sessionId);
                return false;
            }

            _roomEntityIdBySessionId.Remove(sessionId);
            return true;
        }
    }
}
