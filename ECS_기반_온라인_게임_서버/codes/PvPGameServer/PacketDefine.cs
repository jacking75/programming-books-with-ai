namespace PvPGameServer;

// 0 ~ 9999
public enum ErrorCode : short
{
    None                        = 0, // 에러가 아니다

    // 서버 초기화 에라
    RedisInitFail             = 1,    // Redis 초기화 에러

    // 로그인 
    LoginInvalidAuthToken             = 1001, // 로그인 실패: 잘못된 인증 토큰
    AddUserDuplication                = 1002,
    RemoveUserSearchFailureUserId  = 1003,
    UserAuthSearchFailureUserId    = 1004,
    UserAuthAlreadySetAuth          = 1005,
    LoginAlreadyWorking = 1006,
    LoginFullUserCount = 1007,

    DbLoginInvalidPassword   = 1011,
    DbLoginEmptyUser         = 1012,
    DbLoginException          = 1013,

    RoomEnterInvalidState = 1021,
    RoomEnterInvalidUser = 1022,
    RoomEnterErrorSystem = 1023,
    RoomEnterInvalidRoomNumber = 1024,
    RoomEnterFailAddUser = 1025,

    GameNotInRoom = 1201,
    GameAlreadyInProgress = 1202,
    GameCooldown = 1203,
    GamePendingApproval = 1204,
    GameStartOnlyChallenger = 1205,
    GameStartNotEnoughPlayers = 1206,
    GameStartOnlyLeaderCanApprove = 1207,
    GameStartUnknownRequester = 1208,
    GameStartNoPendingRequest = 1209,
    GamePendingRequestExists = 1210,
    GameMoveNotParticipant = 1211,
    GameMoveNotYourTurn = 1212,
    GameMoveOutOfBoard = 1213,
    GameMoveCellOccupied = 1214,
    GameMoveForbiddenDoubleThree = 1215,
    GameMoveForbiddenDoubleFour = 1216,
    GameMoveForbiddenOverline = 1217,
    GameNotInProgress = 1218,
}

// 1 ~ 10000
public enum PacketId : int
{
    ReqResTestEcho = 101,
    
           
    // 클라이언트
    CsBegin        = 1001,

    ReqLogin       = 1002,
    ResLogin       = 1003,
    NtfMustClose       = 1005,

    ReqRoomEnter = 1015,
    ResRoomEnter = 1016,
    NtfRoomUserList = 1017,
    NtfRoomNewUser = 1018,

    ReqRoomLeave = 1021,
    ResRoomLeave = 1022,
    NtfRoomLeaveUser = 1023,

    ReqRoomChat = 1026,
    NtfRoomChat = 1027,

    ReqRoomGameStart = 1030,
    ResRoomGameStart = 1031,
    NtfRoomGameStartRequested = 1032,
    ReqRoomGameStartDecision = 1033,
    ResRoomGameStartDecision = 1034,
    NtfRoomGameStartRejected = 1035,
    NtfRoomGameStarted = 1036,
    ReqRoomGamePlaceStone = 1037,
    ResRoomGamePlaceStone = 1038,
    NtfRoomGameStonePlaced = 1039,
    NtfRoomGameTurnChanged = 1040,
    NtfRoomGamePlayerTimedOut = 1041,
    NtfRoomGameEnded = 1042,
    NtfRoomGameState = 1043,
    NtfRoomMasterChanged = 1044,

    ReqRoomDevAllRoomStartGame = 1091,
    ResRoomDevAllRoomStartGame = 1092,

    ReqRoomDevAllRoomEndGame = 1093,
    ResRoomDevAllRoomEndGame = 1094,

    CsEnd          = 1100,


    // 시스템, 서버 - 서버
    S2sStart    = 8001,

    NtfInConnectClient = 8011,
    NtfInDisconnectClient = 8012,

    ReqSsServerinfo = 8021,
    ResSsServerinfo = 8023,

    ReqInRoomEnter = 8031,
    ResInRoomEnter = 8032,

    NtfInRoomLeave = 8036,


    // DB 8101 ~ 9000
    ReqDbLogin = 8101,
    ResDbLogin = 8102,
}



