namespace ServerClientCommon;

// 0 ~ 9999
public enum ERROR_CODE : short
{
    NONE                        = 0, // 에러가 아니다

    // 서버 초기화 에라
    REDIS_INIT_FAIL             = 1,    // Redis 초기화 에러

    // 로그인 
    LOGIN_INVALID_AUTHTOKEN             = 1001, // 로그인 실패: 잘못된 인증 토큰
    ADD_USER_DUPLICATION                = 1002,
    REMOVE_USER_SEARCH_FAILURE_USER_ID  = 1003,
    USER_AUTH_SEARCH_FAILURE_USER_ID    = 1004,
    USER_AUTH_ALREADY_SET_AUTH          = 1005,
    LOGIN_ALREADY_WORKING = 1006,
    LOGIN_FULL_USER_COUNT = 1007,

    DB_LOGIN_INVALID_PASSWORD   = 1011,
    DB_LOGIN_EMPTY_USER         = 1012,
    DB_LOGIN_EXCEPTION          = 1013,

    ROOM_ENTER_INVALID_STATE = 1021,
    ROOM_ENTER_INVALID_USER = 1022,
    ROOM_ENTER_ERROR_SYSTEM = 1023,
    ROOM_ENTER_INVALID_ROOM_NUMBER = 1024,
    ROOM_ENTER_FAIL_ADD_USER = 1025,
}

// 1 ~ 10000
public enum PACKETID : int
{
    // 클라이언트
    //CS_BEGIN        = 1001,

    ReqLogin = 1002,
    ResLogin = 1003,
    ReqLogout = 1004,
    ResLogout = 1005,

    NtfMustClose = 1010,

    ReqRoomEnter = 1015,
    ResRoomEnter = 1016,
    NtfRoomUserList = 1017,
    NtfRoomNewUser = 1018,

    ReqRoomLeave = 1021,
    ResRoomLeave = 1022,
    NtfRoomLeaveUser = 1023,

    ReqRoomChat = 1026,
    NtfRoomChat = 1027,

    //ReqRoomDevAllRoomStartGame = 1091,
    //ResRoomDevAllRoomStartGame = 1092,

    //ReqRoomDevAllRoomEndGame = 1093,
    //ResRoomDevAllRoomEndGame = 1094,

    CsEnd          = 1100,

    // 오목 게임 로직 관련
    ReqReadyOmok = 1031,
    ResReadyOmok = 1032,
    NtfReadyOmok = 1033,

    NtfStartOmok = 1034,

    ReqPutMok = 1035,
    ResPutMok = 1036,
    NTFPutMok = 1037,

    NTFEndOmok = 1038,

    ReqNextTurn = 1040,
    NtfChangeTurn = 1041,

    NtfInTimer = 1045,


    NtfInCheckRoom = 1045,
    NtfInCheckTurn = 1046,

    End = 1100,

    // 시스템, 서버 - 서버
    SsStart    = 8001,

    NtfInConnectClient = 8011,
    NtfInDisconnectClinet = 8012,


    //ReqSsServerInfo = 8021,
    //ResSsServerInfo = 8023,

    NtfInRoomLeave = 8036,

    ReqHeartBeat = 8041,
    NtfHeartBeat = 8042,

    // DB 8101 ~ 9000
    ReqDbLogin = 8101,
    ResDbLogin = 8102,

    ReqInRedis = 8200,

    ReqInMysql = 8300,

    ReqInUserWin = 8301,
    ReqInUserLose = 8302,
    ReqInUserDraw = 8303,

    ReqInMysqlUserInfo = 8401,
    ResInMysqlUserInfo = 8402,

    ReqInRedisInsertRoomInfo = 8501,
    ReqInRedisDeleteRoomInfo = 8502
}