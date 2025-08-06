using MemoryPack;
using SuperSocketLite.SocketBase.Logging;
using ServerClientCommon;


#pragma warning disable CS8602

namespace OmokServer;
public class PKHCommon : PKHandler
{
    private readonly ILog _logger;

    public PKHCommon(ILog logger) : base(logger)
    {
        this._logger = logger;
    }

    public void RegistPacketHandler(Dictionary<int, Action<MemoryPackBinaryRequestInfo>> packetHandlerMap)
    {
        packetHandlerMap.Add((int)PACKETID.NtfInConnectClient, NotifyInConnectClient);
        packetHandlerMap.Add((int)PACKETID.NtfInDisconnectClinet, NotifyInDisConnectClient);

        packetHandlerMap.Add((int)PACKETID.ReqLogin, RequestLogin);

        packetHandlerMap.Add((int)PACKETID.ReqHeartBeat, RequestHeartBeat);
    }

    public void NotifyInConnectClient(MemoryPackBinaryRequestInfo requestData)
    {
    }

    public void NotifyInDisConnectClient(MemoryPackBinaryRequestInfo requestData)
    {
        var sessionID = requestData.SessionID;
        var user = _userMgr.GetUser(sessionID);
        
        if (user != null)
        {
            var roomNum = user.RoomNumber;

            if (roomNum != Room.InvalidRoomNumber)
            {
                var internalPacket = InnerPakcetMaker.MakeNTFInnerRoomLeavePacket(sessionID, roomNum, user.ID());                
                DistributeInnerPacket(internalPacket);
            }
            _userMgr.RemoveUser(sessionID);
        }
    }

    public void RequestLogin(MemoryPackBinaryRequestInfo packetData)
    {
        //DB를 통해서 로그인 처리를 해야하지만 여기서는 유저 중복만 아니면 로그인 되는 것으로 처리하였다.

        var sessionID = packetData.SessionID;
        _logger.Debug("로그인 요청 받음");

        try
        {
            if(_userMgr.GetUser(sessionID) != null)
            {
                ResponseLoginToClient(ERROR_CODE.LOGIN_ALREADY_WORKING, packetData.SessionID);
                return;
            }
                            
            var reqData = MemoryPackSerializer.Deserialize< PKTReqLogin>(packetData.Data);
            
            // 로그인 처리
            var errorCode = _userMgr.AddUser(reqData.UserID, sessionID);
            if (errorCode != ERROR_CODE.NONE) // 로그인 실패
            {
                ResponseLoginToClient(errorCode, packetData.SessionID);

                if (errorCode == ERROR_CODE.LOGIN_FULL_USER_COUNT)
                {
                    NotifyMustCloseToClient(ERROR_CODE.LOGIN_FULL_USER_COUNT, packetData.SessionID);
                }
                return;
            }
            ResponseLoginToClient(errorCode, packetData.SessionID);
            _logger.Debug($"로그인 결과. UserID:{reqData.UserID}, {errorCode}");

        }
        catch(Exception ex)
        {
            // 패킷 해제에 의해서 로그가 남지 않도록 로그 수준을 Debug로 한다.
            _logger.Error(ex.ToString());
        }
    }
            
    public void ResponseLoginToClient(ERROR_CODE errorCode, string sessionID)
    {
        var resLogin = new PKTResLogin()
        {
            Result = (short)errorCode
        };

        var sendData = MemoryPackSerializer.Serialize(resLogin);
        MemoryPackPacketHeadInfo.Write(sendData, PACKETID.ResLogin);

        NetSendFunc(sessionID, sendData);
    }

    public void NotifyMustCloseToClient(ERROR_CODE errorCode, string sessionID)
    {
        var resLogin = new PKNtfMustClose()
        {
            Result = (short)errorCode
        };

        var sendData = MemoryPackSerializer.Serialize(resLogin);
        MemoryPackPacketHeadInfo.Write(sendData, PACKETID.NtfMustClose);

        NetSendFunc(sessionID, sendData);
    }


    public void RequestHeartBeat(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionID = packetData.SessionID;
        var user = _userMgr.GetUser(sessionID);
        if (user == null)
        {
            return;
        }
        user.LoginTime = DateTime.Now;
        //MainServer.MainLogger.Debug($"RequestHeartBeat LoginTime:{user.LoginTime}");
    }
}
