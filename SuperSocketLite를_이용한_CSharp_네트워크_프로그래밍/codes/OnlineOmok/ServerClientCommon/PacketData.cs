using MemoryPack;


#pragma warning disable CS8618

namespace ServerClientCommon;
public struct MemoryPackPacketHeadInfo
{
    const int PacketHeaderMemoryPackStartPos = 1;
    public const int HeadSize = 6;

    public UInt16 TotalSize;
    public UInt16 Id;
    public byte Type;

    public static UInt16 GetTotalSize(byte[] data, int startPos)
    {
        return FastBinaryRead.UInt16(data, startPos + PacketHeaderMemoryPackStartPos);
    }

    public static void WritePacketId(byte[] data, UInt16 packetId)
    {
        FastBinaryWrite.UInt16(data, PacketHeaderMemoryPackStartPos + 2, packetId);
    }

    public void Read(byte[] headerData)
    {
        var pos = PacketHeaderMemoryPackStartPos;

        TotalSize = FastBinaryRead.UInt16(headerData, pos);
        pos += 2;

        Id = FastBinaryRead.UInt16(headerData, pos);
        pos += 2;

        Type = headerData[pos];
        pos += 1;
    }
        
    public static void Write(byte[] packetData, PACKETID packetId, byte type = 0)
    {
        var pos = PacketHeaderMemoryPackStartPos;

        FastBinaryWrite.UInt16(packetData, pos, (UInt16)packetData.Length);
        pos += 2;

        FastBinaryWrite.UInt16(packetData, pos, (UInt16)packetId);
        pos += 2;

        packetData[pos] = type;
    }

    public void DebugConsolOutHeaderInfo()
    {
        Console.WriteLine("DebugConsolOutHeaderInfo");
        Console.WriteLine("TotalSize : " + TotalSize);
        Console.WriteLine("Id : " + Id);
        Console.WriteLine("Type : " + Type);
    }
}



[MemoryPackable]
public partial class PkHeader
{
    public UInt16 TotalSize { get; set; } = 0;
    public UInt16 Id { get; set; } = 0;
    public byte Type { get; set; } = 0; // 속성 : 암호화? 압축? 
}


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
public partial class PKTReqLogout : PkHeader
{
    public string UserID { get; set; }
}

[MemoryPackable]
public partial class PKTResLogout : PkHeader
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
}

[MemoryPackable]

public partial class PKTNtfInCheckRoom : PkHeader
{
}

[MemoryPackable]

public partial class PKTNtfInCheckTurn : PkHeader
{
}

public partial class PKTReqInRedis : PkHeader
{
}
public partial class PKTReqInMysql : PkHeader
{
}
public partial class PKTReqInMysqlUserInfo : PkHeader
{
}

public partial class PKTReqInMysqlUserInfo : PkHeader
{
}

[MemoryPackable]
public partial class PKTReqInWin : PkHeader
{
    public string WinUserID;
}
[MemoryPackable]
public partial class PKTReqInLose : PkHeader
{
    public string LoseUserID;
}
[MemoryPackable]
public partial class PKTReqInDraw : PkHeader
{
    public string UserID;
}

[MemoryPackable]
public partial class PKTReqInRedisInsertRoomInfo : PkHeader
{
    public int RoomNumber { get; set; }
    public RoomInfo roomInfo { get; set; }
}

[MemoryPackable]
public partial class PKTReqInRedisDeleteRoomInfo : PkHeader
{
    public int RoomNumber { get; set; }
}

[MemoryPackable]
public partial class RoomInfo
{
    public int RoomNumber { get; set; }
    public string ServerAddress { get; set; }
    public RoomInfo(int roomNumber, string serverAddress)
    {
        RoomNumber = roomNumber;
        ServerAddress = serverAddress; // "localhost:32451"
    }
}
