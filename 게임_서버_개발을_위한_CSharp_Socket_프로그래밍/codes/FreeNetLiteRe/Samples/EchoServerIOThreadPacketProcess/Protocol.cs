using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNetLite;

namespace EchoServerIOThreadPacketProcess;

public enum ProtocolId : short
{
	Begin = 0,

    Echo = 101,

    End
}



class PacketDef
{
    public const int HeaderSize = 5;
}


public class EchoPacket
{
    public string Data;

    public byte[] ToPacket(ProtocolId packetId, byte[] bodyData)
    {
        if (bodyData == null)
        {
            bodyData = Encoding.UTF8.GetBytes(Data);
        }

        var packetLen = (ushort)(PacketDef.HeaderSize + bodyData.Length);
        var packet = new byte[packetLen];
        var span = packet.AsSpan();

        // Span을 사용하여 헤더를 작성합니다.
        FastBinaryWrite.UInt16(span, packetLen);
        FastBinaryWrite.UInt16(span.Slice(2), (ushort)packetId);

        // bodyData를 packet의 나머지 부분에 복사합니다.
        bodyData.AsSpan().CopyTo(span.Slice(PacketDef.HeaderSize));

        return packet;
    }

    // Decode 메서드가 ReadOnlySpan<byte>를 받도록 수정합니다.
    public void Decode(ReadOnlySpan<byte> bodyData)
    {
        Data = Encoding.UTF8.GetString(bodyData);
    }
}

