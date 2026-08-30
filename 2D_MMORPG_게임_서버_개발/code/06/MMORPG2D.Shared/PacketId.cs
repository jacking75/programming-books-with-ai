namespace MMORPG2D.Shared;

public enum PacketId : ushort
{
    // 1000: auth/session
    ReqEnterGame = 1001,
    ResEnterGame = 1002,
    NtfLeaveGame = 1003,

    // 2000: movement & AOI (player)
    ReqMove = 2001,
    ResMove = 2002,
    NtfPlayerEnter = 2010,
    NtfPlayerLeave = 2011,
    NtfPlayerMove  = 2012,
    NtfPlayerList  = 2013,

    // 3000: combat
    ReqAttack    = 3001,
    NtfAttack    = 3010,
    NtfHpChange  = 3011,
    NtfDie       = 3012,
    NtfReward    = 3013,
    NtfRespawn   = 3014,

    // 4000: monster
    NtfMonsterEnter = 4001,
    NtfMonsterLeave = 4002,
    NtfMonsterMove  = 4003,
    NtfMonsterList  = 4004,

    // 9000: debug
    ReqEcho = 9001,
    ResEcho = 9002,
}
