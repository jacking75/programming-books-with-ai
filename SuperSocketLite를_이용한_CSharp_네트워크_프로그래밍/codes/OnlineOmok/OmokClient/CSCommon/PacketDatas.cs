using MemoryPack;
using System;
using System.Collections.Generic;
using System.Text;
using CSCommon;

namespace CSCommon;

// 로그인 요청
[MemoryPackable]
public partial class PKTReqLogin : PkHeader
{
    public string UserID { get; set; }
    public string AuthToken { get; set; }
}

[MemoryPackable]
public partial class PKTResLogin : PkHeader
{
    public short Result { get; set; }
}



[MemoryPackable]
public partial class PKNtfMustClose : PkHeader
{
    public short Result { get; set; }
}



[MemoryPackable]
public partial class PKTReqRoomEnter : PkHeader
{
    public int RoomNumber { get; set; }
}

[MemoryPackable]
public partial class PKTResRoomEnter : PkHeader
{
    public short Result { get; set; }
}

[MemoryPackable]
public partial class PKTNtfRoomUserList : PkHeader
{
    public List<string> UserIDList { get; set; } = new List<string>();
}

[MemoryPackable]
public partial class PKTNtfRoomNewUser : PkHeader
{
    public string UserID { get; set; }
}


[MemoryPackable]
public partial class PKTReqRoomLeave : PkHeader
{
}

[MemoryPackable]
public partial class PKTResRoomLeave : PkHeader
{
    public short Result { get; set; }
}

[MemoryPackable]
public partial class PKTNtfRoomLeaveUser : PkHeader
{
    public string UserID { get; set; }
}


[MemoryPackable]
public partial class PKTReqRoomChat : PkHeader
{
    public string ChatMessage { get; set; }
}

[MemoryPackable]
public partial class PKTResRoomChat : PkHeader
{
    public string ChatMessage { get; set; }
}


[MemoryPackable]
public partial class PKTNtfRoomChat : PkHeader
{
    public string UserID { get; set; }

    public string ChatMessage { get; set; }
}

//오목 플레이 준비 완료 요청
[MemoryPackable]
public partial class PKTReqReadyOmok : PkHeader
{
}

[MemoryPackable]
public partial class PKTResReadyOmok : PkHeader
{
}

[MemoryPackable]
public partial class PKTNtfReadyOmok : PkHeader
{
    public string UserID;
    public bool IsReady;
}


// 오목 시작 통보(서버에서 클라이언트들에게)
[MemoryPackable]
public partial class PKTNtfStartOmok : PkHeader
{
    public string FirstUserID; // 선턴 유저 ID
}


// 돌 두기
[MemoryPackable]
public partial class PKTReqPutMok : PkHeader
{
    public int PosX;
    public int PosY;
}

[MemoryPackable]
public partial class PKTResPutMok : PkHeader
{
}

[MemoryPackable]
public partial class PKTNtfPutMok : PkHeader
{
    public int PosX;
    public int PosY;
    public int Mok;
}

// 오목 게임 종료 통보
[MemoryPackable]
public partial class PKTNtfEndOmok : PkHeader
{
    public string WinUserID;
}

[MemoryPackable]
public partial class PKTReqHeartBeat : PkHeader
{
}

[MemoryPackable]
public partial class PKTNtfHeartBeat : PkHeader
{
    public int HeartBeatState;
}
[MemoryPackable]
public partial class PKTNtfChangeTurn : PkHeader
{
    //public string CurrentPlayerID;
}