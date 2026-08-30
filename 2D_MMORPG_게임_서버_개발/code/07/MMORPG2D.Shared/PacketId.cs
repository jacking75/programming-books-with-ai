namespace MMORPG2D.Shared;

public enum PacketId : ushort
{
    ReqEnterGame = 1001, ResEnterGame = 1002, NtfLeaveGame = 1003,

    ReqMove = 2001, ResMove = 2002,
    NtfPlayerEnter = 2010, NtfPlayerLeave = 2011, NtfPlayerMove = 2012, NtfPlayerList = 2013,

    ReqAttack = 3001,
    NtfAttack = 3010, NtfHpChange = 3011, NtfDie = 3012, NtfReward = 3013, NtfRespawn = 3014,

    NtfMonsterEnter = 4001, NtfMonsterLeave = 4002, NtfMonsterMove = 4003, NtfMonsterList = 4004,

    NtfItemDrop      = 5001,
    NtfItemPickup    = 5002,
    NtfItemList      = 5010,
    NtfInventory     = 5020,
    ReqUseItem       = 5101,
    NtfItemUseResult = 5102,

    ReqEcho = 9001, ResEcho = 9002,
}
