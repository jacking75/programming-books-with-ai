namespace MMORPG2D.GameServer.Game;

/// <summary>
/// 게임 도메인 객체로서의 한 캐릭터. 세션은 통신 객체이고 PlayerCharacter 는 게임 상태 객체.
/// 둘을 분리해 두면 4주차에 다른 사용자도 같은 자료구조로 들고 다닐 수 있다.
/// </summary>
public class PlayerCharacter
{
    public GameSession Session { get; }
    public long CharacterId { get; }
    public long AccountId { get; }
    public string Name { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Level { get; set; }
    public long Exp { get; set; }

    public PlayerCharacter(GameSession session, long characterId, long accountId,
        string name, int x, int y, int level, long exp)
    {
        Session = session;
        CharacterId = characterId;
        AccountId = accountId;
        Name = name;
        X = x; Y = y;
        Level = level; Exp = exp;
    }
}
