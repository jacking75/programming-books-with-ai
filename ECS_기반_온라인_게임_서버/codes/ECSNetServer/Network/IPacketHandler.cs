using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECSNetServer.Network;



// 패킷 처리를 위한 인터페이스
public interface IPacketHandler
{
    void HandlePacket(IConnection connection, byte[] packetData);
}