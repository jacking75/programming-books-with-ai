using SuperSocketLite.SocketBase;
using MMORPG2D.GameServer.Game;
using MMORPG2D.Shared;

namespace MMORPG2D.GameServer;

public class GameSession : AppSession<GameSession, GameRequestInfo>
{
    /// <summary>로그인이 끝난 후의 계정 ID. 0 이면 미인증.</summary>
    public long AccountId { get; set; }

    /// <summary>월드에 입장한 후의 캐릭터 ID. 0 이면 월드 밖.</summary>
    public long CharacterId { get; set; }

    /// <summary>입장한 PlayerCharacter. null 이면 월드 밖.</summary>
    public PlayerCharacter? Player { get; set; }

    /// <summary>패킷 ID + 본문 객체로 손쉽게 보내기 위한 헬퍼.</summary>
    public void SendPacket<T>(PacketId id, T body)
    {
        var bytes = PacketEncoder.Encode(id, body);
        Send(bytes, 0, bytes.Length);
    }
}

