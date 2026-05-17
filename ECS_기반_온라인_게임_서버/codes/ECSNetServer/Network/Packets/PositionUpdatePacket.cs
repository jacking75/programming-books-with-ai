using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Network.Packets;


// 위치 업데이트 패킷
public class PositionUpdatePacket : JsonPacket
{
    public override ushort PacketId => 2001;
    public int EntityId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}