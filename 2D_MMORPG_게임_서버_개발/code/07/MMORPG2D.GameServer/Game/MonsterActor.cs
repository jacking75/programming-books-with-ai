namespace MMORPG2D.GameServer.Game;

public enum AiState { Idle, Wander, Chase, Attack, Dead }

public class MonsterActor
{
    public long Id { get; init; }
    public MonsterDef Def { get; init; } = null!;
    public int SpawnX { get; init; }
    public int SpawnY { get; init; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public bool IsAlive => Hp > 0;
    public AiState State { get; set; } = AiState.Idle;
    public long TargetId { get; set; }
    public int WanderRemaining { get; set; }
    public DateTime LastAttackUtc { get; set; } = DateTime.MinValue;

    public string Name => Def.Name;
    public int MaxHp => Def.MaxHp;
    public int Attack => Def.Attack;
    public int Defense => Def.Defense;
    public int AggroRange => Def.AggroRange;
    public int RespawnSec => Def.RespawnSec;
}
