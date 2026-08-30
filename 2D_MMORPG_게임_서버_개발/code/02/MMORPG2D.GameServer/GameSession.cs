using SuperSocketLite.SocketBase;
using MMORPG2D.Shared;

namespace MMORPG2D.GameServer;

/// <summary>
/// 한 클라이언트와의 연결을 나타낸다. 1주차에는 식별 정보만 들고 있다.
/// 2주차 이후 AccountId/CharacterId 가 채워지고, 3주차에 World 가 추가된다.
/// </summary>
public class GameSession : AppSession<GameSession, GameRequestInfo>
{
    /// <summary>로그인이 끝난 후의 계정 ID. 0 이면 미인증.</summary>
    public long AccountId { get; set; }

    /// <summary>월드에 입장한 후의 캐릭터 ID. 0 이면 월드 밖.</summary>
    public long CharacterId { get; set; }

    /// <summary>패킷 ID + 본문 객체로 손쉽게 보내기 위한 헬퍼.</summary>
    public void SendPacket<T>(PacketId id, T body)
    {
        var bytes = PacketEncoder.Encode(id, body);
        Send(bytes, 0, bytes.Length);
    }
}
