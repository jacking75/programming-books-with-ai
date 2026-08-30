namespace MMORPG2D.Shared;

/// <summary>
/// 게임 전체에서 사용하는 패킷 ID. 한 번 발급한 값은 다른 의미로 재사용하지 않는다.
/// 100단위 대역으로 도메인을 묶어 둔다.
/// </summary>
public enum PacketId : ushort
{
    // 1000번대: 인증/세션 (3주차부터 사용)
    ReqEnterGame = 1001,
    ResEnterGame = 1002,

    // 9000번대: 디버그/에코
    ReqEcho = 9001,
    ResEcho = 9002,
}
