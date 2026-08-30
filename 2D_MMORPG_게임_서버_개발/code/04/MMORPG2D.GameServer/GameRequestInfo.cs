using SuperSocketLite.SocketBase.Protocol;
using MMORPG2D.Shared;

namespace MMORPG2D.GameServer;

/// <summary>
/// SuperSocketLite 가 만들어 주는 한 패킷의 표현. PacketId 와 Body 만 들고 있는다.
/// Body 는 ReceiveFilter 안에서 복사된 새 배열이라 안전하게 들고 다닐 수 있다.
/// </summary>
public class GameRequestInfo : IRequestInfo
{
    public PacketId PacketId { get; }
    public byte[] Body { get; }

    public string Key => PacketId.ToString();

    public GameRequestInfo(PacketId packetId, byte[] body)
    {
        PacketId = packetId;
        Body = body;
    }
}
