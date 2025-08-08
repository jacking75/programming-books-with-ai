namespace CSCommon
{
    // 1 ~ 10000
    public enum PACKETID : int
    {
        // 클라이언트
        //CS_BEGIN        = 1001,

        ReqLogin = 1002,
        ResLogin = 1003,
        NtfMustClose = 1005,

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

        CsEnd = 1100,

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

        End = 1100,

        // 시스템, 서버 - 서버
        SsStart = 8001,

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
    }
}
