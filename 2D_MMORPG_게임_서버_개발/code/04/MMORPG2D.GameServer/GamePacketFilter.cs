using SuperSocketLite.SocketEngine.Protocol;
using MMORPG2D.Shared;

namespace MMORPG2D.GameServer;

/// <summary>
/// 길이(2) + PacketId(2) 헤더를 먼저 읽고, 헤더의 Length 값을 보고 Body 를 잘라 낸다.
/// SuperSocketLite 가 큰 공용 버퍼를 재사용하기 때문에 Body 는 반드시 복사본을 만들어 둔다.
/// </summary>
public sealed class GamePacketFilter
    : FixedHeaderReceiveFilter<GameRequestInfo>
{
    public GamePacketFilter() : base(PacketEncoder.HeaderSize) { }

    protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
    {
        var totalLen = PacketEncoder.ReadTotalLength(header.AsSpan(offset, length));
        return totalLen - PacketEncoder.HeaderSize;
    }

    protected override GameRequestInfo ResolveRequestInfo(
        ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
    {
        var pktId = PacketEncoder.ReadPacketId(
            header.Array!.AsSpan(header.Offset, header.Count));

        // 공유 버퍼에서 우리만의 복사본을 만든다 — 다음 패킷이 같은 자리를 덮어쓰기 전에.
        var bodyCopy = new byte[length];
        Buffer.BlockCopy(bodyBuffer, offset, bodyCopy, 0, length);

        return new GameRequestInfo(pktId, bodyCopy);
    }
}
