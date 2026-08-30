namespace MMORPG2D.GameServer.Game;

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
    public long MoveRateWindowStartMs { get; set; }
    public int MoveRateWindowCount { get; set; }

    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public DateTime LastAttackUtc { get; set; } = DateTime.MinValue;
    public bool IsAlive => Hp > 0;

    // Week 7 - inventory
    public Inventory Inventory { get; set; } = new();

    public PlayerCharacter(GameSession session, long characterId, long accountId,
        string name, int x, int y, int level, long exp,
        int hp, int maxHp, int attack, int defense)
    {
        Session = session;
        CharacterId = characterId;
        AccountId = accountId;
        Name = name;
        X = x; Y = y;
        Level = level; Exp = exp;
        Hp = hp; MaxHp = maxHp;
        Attack = attack; Defense = defense;
    }
}
