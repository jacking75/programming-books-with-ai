using MemoryPack;
using ServerClientCommon;

#pragma warning disable CS8625
#pragma warning disable CS8618


namespace OmokServer;

public class InnerPakcetMaker
{
    public static MemoryPackBinaryRequestInfo MakeNTFInnerRoomLeavePacket(string sessionID, int roomNumber, string userID)
    {            
        var packet = new PKTInternalNtfRoomLeave()
        {
            RoomNumber = roomNumber,
            UserID = userID,
        };

        var sendData = MemoryPackSerializer.Serialize(packet);
        MemoryPackPacketHeadInfo.Write(sendData, PACKETID.NtfInRoomLeave);
        
        var memoryPakcPacket = new MemoryPackBinaryRequestInfo(null);
        memoryPakcPacket.Data = sendData;
        memoryPakcPacket.SessionID = sessionID;
        return memoryPakcPacket;
    }

    public static MemoryPackBinaryRequestInfo MakeNTFInConnectOrDisConnectClientPacket(bool isConnect, string sessionID)
    {
        var memoryPakcPacket = new MemoryPackBinaryRequestInfo(null);
        memoryPakcPacket.Data = new byte[MemoryPackPacketHeadInfo.HeadSize];
        
        if (isConnect)
        {
            MemoryPackPacketHeadInfo.WritePacketId(memoryPakcPacket.Data, (UInt16)PACKETID.NtfInConnectClient);
        }
        else
        {
            MemoryPackPacketHeadInfo.WritePacketId(memoryPakcPacket.Data, (UInt16)PACKETID.NtfInDisconnectClinet);
        }

        memoryPakcPacket.SessionID = sessionID;
        return memoryPakcPacket;
    }
}

[MemoryPackable]
public partial class PKTInternalNtfCheckHeartBeat : PkHeader
{
    public int HeartBeatState { get; set; }
}
public enum HeartBeatState
{
    Connect = 0,
    DisConnect = 1
}


[MemoryPackable]
public partial class PKTInternalNtfRoomLeave : PkHeader
{
    public int RoomNumber { get; set; }
    public string UserID { get; set; }
}
