using MemoryPack;
using SuperSocketLite.SocketBase.Logging;
using ServerClientCommon;


#pragma warning disable CS8602
#pragma warning disable CS8625
#pragma warning disable CS8603
#pragma warning disable CS8619

namespace OmokServer;

public class PKHRoom : PKHandler
{
    private readonly SuperSocketLite.SocketBase.Logging.ILog _logger;

    public PKHRoom(ILog logger) : base(logger)
    {
        this._logger = logger;
    }
    List<Room> _roomList = null;
    int _startRoomNumber;
    
    public void SetRooomList(List<Room> roomList)
    {
        _roomList = roomList;
        _startRoomNumber = roomList[0].Number;
    }

    public void RegistPacketHandler(Dictionary<int, Action<MemoryPackBinaryRequestInfo>> packetHandlerMap)
    {
        packetHandlerMap.Add((int)PACKETID.ReqRoomEnter, RequestRoomEnter);
        packetHandlerMap.Add((int)PACKETID.ReqRoomLeave, RequestLeave);
        packetHandlerMap.Add((int)PACKETID.NtfInRoomLeave, NotifyLeaveInternal);
        packetHandlerMap.Add((int)PACKETID.ReqRoomChat, RequestChat);

        packetHandlerMap.Add((int)PACKETID.ReqReadyOmok, ReqReadyOmok);

        // 오목 게임 관련
        packetHandlerMap.Add((int)PACKETID.ReqPutMok, RequestPlaceStone);
        //packetHandlerMap.Add((int)PACKETID.ResPutMok, ResponsePlaceStone);
        //packetHandlerMap.Add((int)PACKETID.NTFPutMok, NotifyPlaceStone);

        packetHandlerMap.Add((int)PACKETID.NTFEndOmok, NotifyGameEnd);
        packetHandlerMap.Add((int)PACKETID.NtfInTimer, CheckTimer);
    }

    private void CheckTimer(MemoryPackBinaryRequestInfo info) // 타이머
    {
        _logger.Debug("==NtfInTimer 패킷 처리 함수 : CheckTimer 진입");

        // 룸매니저 처리
        _roomMgr.CheckRoom();

        // 유저 매니저 처리
        _userMgr.CheckUser();
    }

    Room GetRoom(int roomNumber)
    {
        var index = roomNumber - _startRoomNumber;

        if( index < 0 || index >= _roomList.Count())
        {
            return null;
        }

        return _roomList[index];
    }
            
    (bool, Room, RoomUser) CheckRoomAndRoomUser(string userNetSessionID)
    {
        var user = _userMgr.GetUser(userNetSessionID);
        if (user == null)
        {
            return (false, null, null);
        }

        var roomNumber = user.RoomNumber;
        var room = GetRoom(roomNumber);

        if(room == null)
        {
            return (false, null, null);
        }

        var roomUser = room.GetUserByNetSessionId(userNetSessionID);

        if (roomUser == null)
        {
            return (false, room, null);
        }

        return (true, room, roomUser);
    }

    public void RequestRoomEnter(MemoryPackBinaryRequestInfo packetData) 
    {
        var sessionID = packetData.SessionID;
        _logger.Debug("RequestRoomEnter");

        try
        {
            var user = _userMgr.GetUser(sessionID); // 유저 정보 가져오기

            if (user == null || user.IsConfirm(sessionID) == false) // 유저 정보가 없거나 세션 아이디가 일치하지 않으면
            {
                ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID);
                return;
            }

            if (user.IsStateRoom()) // 이미 방에 들어가 있는 상태이면
            {
                ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_STATE, sessionID); 
                return;
            }

            var reqData = MemoryPackSerializer.Deserialize<PKTReqRoomEnter>(packetData.Data); 
            
            var room = GetRoom(reqData.RoomNumber); // 방 정보 가져오기

            if (room == null) 
            {
                ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_ROOM_NUMBER, sessionID);
                return;
            }

            if (room.AddUser(user.ID(), sessionID) == false) 
            {
                ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_FAIL_ADD_USER, sessionID);
                return;
            }


            user.EnteredRoom(reqData.RoomNumber); // 유저가 방에 들어갔다고 표시

            room.NotifyPacketUserList(sessionID); // 방에 있는 유저 리스트 전송
            room.NofifyPacketNewUser(sessionID, user.ID()); // 새로운 유저에게 방에 있는 유저 리스트 전송

            ResponseEnterRoomToClient(ERROR_CODE.NONE, sessionID); // 방 입장 성공

            // 방에 입장했을 때 시각 저장
            room.StartTime = DateTime.Now;
            _logger.Debug($"방에 입장한 시각 , StartTime : {room.StartTime}");
            _logger.Debug("RequestEnterInternal - Success");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
        }
    }

    void ResponseEnterRoomToClient(ERROR_CODE errorCode, string sessionID)
    {
        var resRoomEnter = new PKTResRoomEnter()
        {
            Result = (short)errorCode
        };

        var sendPacket = MemoryPackSerializer.Serialize(resRoomEnter);
        MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.ResRoomEnter);
        
        NetSendFunc(sessionID, sendPacket);
    }

    public void RequestLeave(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionID = packetData.SessionID;
        _logger.Debug("방나가기 요청 받음");

        try
        {
            var user = _userMgr.GetUser(sessionID);
            if(user == null)
            {
                return;
            }

            if(LeaveRoomUser(sessionID, user.RoomNumber) == false)
            {
                return;
            }

            user.LeaveRoom();

            ResponseLeaveRoomToClient(sessionID);

            _logger.Debug("Room RequestLeave - Success");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
        }
    }

    bool LeaveRoomUser(string sessionID, int roomNumber)
    {
        _logger.Debug($"LeaveRoomUser. SessionID:{sessionID}");

        var room = GetRoom(roomNumber);
        if (room == null)
        {
            return false;
        }

        var roomUser = room.GetUserByNetSessionId(sessionID);
        if (roomUser == null)
        {
            return false;
        }
                    
        var userID = roomUser.UserID;
        room.RemoveUser(roomUser);

        room.NotifyPacketLeaveUser(userID);
        return true;
    }

    void ResponseLeaveRoomToClient(string sessionID)
    {
        var resRoomLeave = new PKTResRoomLeave()
        {
            Result = (short)ERROR_CODE.NONE
        };

        var sendPacket = MemoryPackSerializer.Serialize(resRoomLeave);
        MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.ResRoomLeave);
   
        NetSendFunc(sessionID, sendPacket);
    }

    public void NotifyLeaveInternal(MemoryPackBinaryRequestInfo packetData) // 방을 나가지 않은 상태에서 접속 끊었을 때
    {
        var sessionID = packetData.SessionID;
        _logger.Debug($"NotifyLeaveInternal. SessionID: {sessionID}");

        var reqData = MemoryPackSerializer.Deserialize<PKTInternalNtfRoomLeave>(packetData.Data);            
        LeaveRoomUser(sessionID, reqData.RoomNumber);
    }
            
    public void RequestChat(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionID = packetData.SessionID;
        _logger.Debug("Room RequestChat");

        try
        {
            var roomObject = CheckRoomAndRoomUser(sessionID);

            if(roomObject.Item1 == false)
            {
                return;
            }


            var reqData = MemoryPackSerializer.Deserialize<PKTReqRoomChat>(packetData.Data);

            var notifyPacket = new PKTNtfRoomChat()
            {
                UserID = roomObject.Item3.UserID,
                ChatMessage = reqData.ChatMessage
            };

            var sendPacket = MemoryPackSerializer.Serialize(notifyPacket);
            MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.NtfRoomChat);
            
            roomObject.Item2.Broadcast("", sendPacket);

            _logger.Debug("Room RequestChat - Success");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
        }
    }

    public void ReqReadyOmok(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionID = packetData.SessionID;
        _logger.Debug("ReqReadyOmok");

        try
        {
            var user = _userMgr.GetUser(sessionID); 
            
            //유저 정보 가져오기
            if (user == null || user.IsConfirm(sessionID) == false) // 유저 정보가 없거나 세션 아이디가 일치하지 않으면
            {
                ResponseEnterRoomToClient(ERROR_CODE.ROOM_ENTER_INVALID_USER, sessionID);
                return;
            }
            var room = GetRoom(user.RoomNumber);
            if (room == null)
            {
                _logger.Error("Room not found for the user");
                return;
            }

            var roomUser = room.GetUserByNetSessionId(sessionID);
            roomUser.SetReady();

            NotifyReadyOmok(ERROR_CODE.NONE, sessionID); 

            _logger.Debug("ReqReadyOmok - Success");

            // Check if all users are ready
            if (room.AreAllUsersReady())
            {
                room.StartGame();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
        }
    }

    void NotifyReadyOmok(ERROR_CODE errorCode, string sessionID)
    {
        var user = _userMgr.GetUser(sessionID);
        //var room = GetRoom(user.RoomNumber);
        var roomUser = (GetRoom(user.RoomNumber)).GetUserByNetSessionId(sessionID);

        var notifyPacket = new PKTNtfReadyOmok()
        {
            UserID = user.ID(),
            IsReady = roomUser.GetIsReady()
        };

        var sendPacket = MemoryPackSerializer.Serialize(notifyPacket);
        MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.NtfReadyOmok);

        NetSendFunc(sessionID, sendPacket);
    }

    // RequestPlaceStone 함수: 클라이언트로부터 오목 돌 두기 요청을 받아 처리
    public void RequestPlaceStone(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionID = packetData.SessionID;
        _logger.Debug("RequestPlaceStone");

        try
        {
            var roomObject = CheckRoomAndRoomUser(sessionID);
            if (!roomObject.Item1) // 유효하지 않은 요청 처리
            {
                _logger.Error("Invalid room or user.");
                return;
            }
            int StoneColor = roomObject.Item3.StoneColor;

            var reqData = MemoryPackSerializer.Deserialize<PKTReqPutMok>(packetData.Data);
          

            // 게임 로직 처리: 돌 두기
            bool result = roomObject.Item2.game.PlaceStone(reqData.PosX, reqData.PosY, StoneColor);
            if (!result)
            {
                _logger.Error("Failed to place stone.");
                return;
            }

            // NotifyPlaceStone
            var notifyPacket = new PKTNtfPutMok()
            {
                PosX = reqData.PosX,
                PosY = reqData.PosY,
                Mok = StoneColor
            };

            var sendPacket = MemoryPackSerializer.Serialize(notifyPacket);
            MemoryPackPacketHeadInfo.Write(sendPacket, PACKETID.NTFPutMok);
            roomObject.Item2.Broadcast(sessionID, sendPacket); // 해당 방의 모든 유저에게 통보

            _logger.Debug("Stone placed successfully.");

            // 돌 둔 시각 저장
            roomObject.Item2.TurnTime = DateTime.Now;
            _logger.Debug($"돌 둔 시각 저장 , TurnTime : {roomObject.Item2.TurnTime}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex.ToString());
        }
    }

    void NotifyPlaceStone(ERROR_CODE errorCode, string sessionID)
    {
        //var sessionID = packetData.SessionID;

        _logger.Debug("NotifyPlaceStone");
    }

    public void NotifyGameEnd(MemoryPackBinaryRequestInfo packetData)
    {
        var sessionID = packetData.SessionID;
        _logger.Debug("NotifyGameEnd");
    }

}
