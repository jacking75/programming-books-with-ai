namespace MMORPG2D.Shared;

public enum PacketId : ushort
{
    // 1000번대: 인증/세션
    ReqEnterGame = 1001,
    ResEnterGame = 1002,
    NtfLeaveGame = 1003,

    // 2000번대: 이동/위치
    ReqMove = 2001,
    ResMove = 2002,
    NtfPlayerEnter = 2010,
    NtfPlayerLeave = 2011,
    NtfPlayerMove  = 2012,
    NtfPlayerList  = 2013,

    // 9000번대: 디버그/에코
    ReqEcho = 9001,
    ResEcho = 9002,
}

