using MemoryPack;
using System;
using System.Collections.Generic;
using PvPGameServer.Ecs.Entities;

namespace PvPGameServer;

public class PKHCommon : PKHandler
{
    public void RegistPacketHandler(Dictionary<int, Action<MemoryPackBinaryRequestInfo>> packetHandlerDict)
    {
        packetHandlerDict.Add((int)PacketId.NtfInConnectClient, HandleNotifyInConnectClient);
        packetHandlerDict.Add((int)PacketId.NtfInDisconnectClient, HandleNotifyInDisConnectClient);
        packetHandlerDict.Add((int)PacketId.ReqLogin, HandleRequestLogin);
    }

    public void HandleNotifyInConnectClient(MemoryPackBinaryRequestInfo requestData)
    {
    }

    public void HandleNotifyInDisConnectClient(MemoryPackBinaryRequestInfo requestData)
    {
        var sessionId = requestData.SessionID;
        if (_playerSystem.TryGetPlayerBySessionId(sessionId, out var player))
        {
            var membership = player.Membership;
            if (membership.IsInRoom && membership.RoomNumber.HasValue)
            {
                var internalPacket = InnerPakcetMaker.MakeNTFInnerRoomLeavePacket(
                    sessionId,
                    membership.RoomNumber.Value,
                    player.Identity.UserId);

                DistributeInnerPacket(internalPacket);
            }

            _playerSystem.RemovePlayerBySessionId(sessionId);
        }
    }

    public void HandleRequestLogin(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionId = packetData.SessionID;
        MainServer.s_MainLogger.Debug("로그인 요청 받음");

        try
        {
            if (_playerSystem.TryGetPlayerBySessionId(sessionId, out _))
            {
                ResponseLoginToClient(ErrorCode.LoginAlreadyWorking, sessionId);
                return;
            }

            var reqData = MemoryPackSerializer.Deserialize<PKTReqLogin>(packetData.Data);
            var errorCode = _playerSystem.TryAddPlayer(sessionId, reqData.UserID, out var player);

            if (errorCode != ErrorCode.None)
            {
                ResponseLoginToClient(errorCode, sessionId);

                if (errorCode == ErrorCode.LoginFullUserCount)
                {
                    NotifyMustCloseToClient(ErrorCode.LoginFullUserCount, sessionId);
                }

                return;
            }

            ResponseLoginToClient(ErrorCode.None, sessionId);

            MainServer.s_MainLogger.Debug($"로그인 결과. UserID:{reqData.UserID}, {errorCode}");
        }
        catch (Exception ex)
        {
            MainServer.s_MainLogger.Error(ex.ToString());
        }
    }

    public void ResponseLoginToClient(ErrorCode errorCode, string sessionId)
    {
        var resLogin = new PKTResLogin()
        {
            Result = (short)errorCode
        };

        var sendData = MemoryPackSerializer.Serialize(resLogin);
        MemoryPackPacketHeader.Write(sendData, PacketId.ResLogin);

        NetSendFunc(sessionId, sendData);
    }

    public void NotifyMustCloseToClient(ErrorCode errorCode, string sessionId)
    {
        var resLogin = new PKNtfMustClose()
        {
            Result = (short)errorCode
        };

        var sendData = MemoryPackSerializer.Serialize(resLogin);
        MemoryPackPacketHeader.Write(sendData, PacketId.NtfMustClose);

        NetSendFunc(sessionId, sendData);
    }
}
