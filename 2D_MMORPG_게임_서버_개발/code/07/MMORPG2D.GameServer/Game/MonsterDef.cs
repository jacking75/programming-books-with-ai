namespace MMORPG2D.GameServer.Game;

public class MonsterDef
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int MaxHp { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int ExpReward { get; init; }
    public int MoveSpeed { get; init; } = 1;
    public int AggroRange { get; init; } = 192;
    public int RespawnSec { get; init; } = 10;
}

public class MonsterSpawn
{
    public int Id { get; init; }
    public int MonsterId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
}
