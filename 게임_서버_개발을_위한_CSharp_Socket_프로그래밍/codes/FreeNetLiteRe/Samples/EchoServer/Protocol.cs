#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNetLite;

namespace EchoServer;

public enum ProtocolId : UInt16
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
    public string? Data;

    /// <summary>
    /// 패킷 데이터를 직렬화합니다. bodyData가 null이면 this.Data를 사용합니다.
    /// </summary>
    public byte[] ToPacket(ProtocolId packetId, byte[]? bodyData)
    {
        bodyData ??= Encoding.UTF8.GetBytes(Data ?? string.Empty);

        var packetLen = (ushort)(PacketDef.HeaderSize + bodyData.Length);
        var packet = new byte[packetLen];
        var span = packet.AsSpan();

        FastBinaryWrite.UInt16(span, packetLen);
        FastBinaryWrite.UInt16(span[2..], (ushort)packetId);
        bodyData.CopyTo(span[PacketDef.HeaderSize..]);

        return packet;
    }

    /// <summary>
    /// 바이트 스팬으로부터 패킷 데이터를 역직렬화합니다.
    /// </summary>
    public void Decode(ReadOnlySpan<byte> bodyData)
    {
        Data = Encoding.UTF8.GetString(bodyData);
    }
}
