using MemoryPack;
using System;
using System.Collections.Generic;
using PvPGameServer.Ecs.Components;
using PvPGameServer.Ecs.Entities;
using PvPGameServer.Ecs.Systems;

namespace PvPGameServer;

public class PKHRoom : PKHandler
{
    IRoomGameCoordinator _gameCoordinator;

    public void RegistPacketHandler(Dictionary<int, Action<MemoryPackBinaryRequestInfo>> packetHandlerDict)
    {
        packetHandlerDict.Add((int)PacketId.ReqRoomEnter, HandleRequestRoomEnter);
        packetHandlerDict.Add((int)PacketId.ReqRoomLeave, HandleRequestLeave);
        packetHandlerDict.Add((int)PacketId.NtfInRoomLeave, HandleNotifyLeaveInternal);
        packetHandlerDict.Add((int)PacketId.ReqRoomChat, HandleRequestChat);
    }

    public void SetGameCoordinator(IRoomGameCoordinator coordinator)
    {
        _gameCoordinator = coordinator;
    }

    public void HandleRequestRoomEnter(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;
        MainServer.s_MainLogger.Debug("RequestRoomEnter");

        try
        {
            if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player) == false)
            {
                SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidUser, sessionId);
                return;
            }

            if (string.Equals(player.Session.SessionId, sessionId, StringComparison.Ordinal) == false)
            {
                SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidUser, sessionId);
                return;
            }

            if (player.Membership.IsInRoom)
            {
                SendResponseEnterRoomToClient(ErrorCode.RoomEnterInvalidState, sessionId);
                return;
            }

            var reqData = MemoryPackSerializer.Deserialize<PKTReqRoomEnter>(packetData.Data);

            var enterResult = _roomSystem.TryEnterRoom(reqData.RoomNumber, player, out var room, out var occupant);
            if (enterResult != ErrorCode.None)
            {
                SendResponseEnterRoomToClient(enterResult, sessionId);
                return;
            }

            SendRoomUserList(room, sessionId);

            if (room.Occupancy.TryGetLeader(out var currentLeader))
            {
                var ownerPacket = new PKTNtfRoomMasterChanged
                {
                    UserId = currentLeader.UserId
                };

                var ownerSendPacket = MemoryPackSerializer.Serialize(ownerPacket);
                MemoryPackPacketHeader.Write(ownerSendPacket, PacketId.NtfRoomMasterChanged);
                NetSendFunc(sessionId, ownerSendPacket);
            }

            _gameCoordinator?.OnPlayerEntered(room, occupant);

            if (room.Occupancy.TryGetLeader(out var leader) &&
                string.Equals(leader.SessionId, occupant.SessionId, StringComparison.Ordinal))
            {
                _gameCoordinator?.OnRoomOwnerChanged(room, leader);
            }

            NotifyRoomNewUser(room, occupant, sessionId);

            SendResponseEnterRoomToClient(ErrorCode.None, sessionId);
            MainServer.s_MainLogger.Debug("RequestEnterInternal - Success");
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }

    public void HandleRequestLeave(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;
        MainServer.s_MainLogger.Debug("방나가기 요청 받음");

        try
        {
            if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player) == false)
            {
                return;
            }

            if (_roomSystem.TryRemovePlayerFromRoom(player, out var room, out var removed) == false)
            {
                return;
            }

            SendResponseLeaveRoomToClient(sessionId);
            _gameCoordinator?.OnPlayerLeft(room, removed);
            NotifyRoomLeaveUser(room, removed.UserId);
            NotifyOwnerChanged(room);
            MainServer.s_MainLogger.Debug("Room RequestLeave - Success");
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }

    public void HandleNotifyLeaveInternal(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;
        MainServer.s_MainLogger.Debug($"NotifyLeaveInternal. SessionID: {sessionId}");

        if (_roomSystem.TryRemovePlayerBySession(sessionId, out var room, out var removed))
        {
            _gameCoordinator?.OnPlayerLeft(room, removed);
            NotifyRoomLeaveUser(room, removed.UserId);
            NotifyOwnerChanged(room);
        }
    }

    public void HandleRequestChat(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;
        MainServer.s_MainLogger.Debug("Room RequestChat");

        try
        {
            if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player) == false)
            {
                return;
            }

            if (player.Membership.IsInRoom == false || player.Membership.RoomEntityId.HasValue == false)
            {
                return;
            }

            if (_roomSystem.TryGetRoomByEntityId(player.Membership.RoomEntityId.Value, out var room) == false)
            {
                return;
            }

            if (room.Occupancy.TryGetBySession(sessionId, out var occupant) == false)
            {
                return;
            }

            var reqData = MemoryPackSerializer.Deserialize<PKTReqRoomChat>(packetData.Data);

            var notifyPacket = new PKTNtfRoomChat()
            {
                UserID = occupant.UserId,
                ChatMessage = reqData.ChatMessage
            };

            var sendPacket = MemoryPackSerializer.Serialize(notifyPacket);
            MemoryPackPacketHeader.Write(sendPacket, PacketId.NtfRoomChat);

            Broadcast(room, string.Empty, sendPacket);

            MainServer.s_MainLogger.Debug("Room RequestChat - Success");
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }

    void SendResponseEnterRoomToClient(ErrorCode errorCode, string sessionId)
    {
        var resRoomEnter = new PKTResRoomEnter()
        {
            Result = (short)errorCode
        };

        var sendPacket = MemoryPackSerializer.Serialize(resRoomEnter);
        MemoryPackPacketHeader.Write(sendPacket, PacketId.ResRoomEnter);

        NetSendFunc(sessionId, sendPacket);
    }

    void SendResponseLeaveRoomToClient(string sessionId)
    {
        var resRoomLeave = new PKTResRoomLeave()
        {
            Result = (short)ErrorCode.None
        };

        var sendPacket = MemoryPackSerializer.Serialize(resRoomLeave);
        MemoryPackPacketHeader.Write(sendPacket, PacketId.ResRoomLeave);

        NetSendFunc(sessionId, sendPacket);
    }

    void SendRoomUserList(RoomEntity room, string targetSessionId)
    {
        var packet = new PKTNtfRoomUserList();
        foreach (var occupant in _roomSystem.GetOccupants(room))
        {
            packet.UserIDList.Add(occupant.UserId);
        }

        var sendPacket = MemoryPackSerializer.Serialize(packet);
        MemoryPackPacketHeader.Write(sendPacket, PacketId.NtfRoomUserList);

        NetSendFunc(targetSessionId, sendPacket);
    }

    void NotifyRoomNewUser(RoomEntity room, RoomOccupant newOccupant, string excludeSessionId)
    {
        var packet = new PKTNtfRoomNewUser()
        {
            UserID = newOccupant.UserId
        };

        var sendPacket = MemoryPackSerializer.Serialize(packet);
        MemoryPackPacketHeader.Write(sendPacket, PacketId.NtfRoomNewUser);

        Broadcast(room, excludeSessionId, sendPacket);
    }

    void NotifyRoomLeaveUser(RoomEntity room, string userId)
    {
        if (room.Occupancy.IsEmpty)
        {
            return;
        }

        var packet = new PKTNtfRoomLeaveUser()
        {
            UserID = userId
        };

        var sendPacket = MemoryPackSerializer.Serialize(packet);
        MemoryPackPacketHeader.Write(sendPacket, PacketId.NtfRoomLeaveUser);

        Broadcast(room, string.Empty, sendPacket);
    }

    void NotifyOwnerChanged(RoomEntity room)
    {
        if (room.Occupancy.TryGetLeader(out var leader))
        {
            _gameCoordinator?.OnRoomOwnerChanged(room, leader);
        }
        else
        {
            _gameCoordinator?.OnRoomOwnerChanged(room, null);
        }
    }
}
