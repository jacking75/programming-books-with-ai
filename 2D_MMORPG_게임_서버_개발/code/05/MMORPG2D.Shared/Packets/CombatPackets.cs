using MemoryPack;

namespace MMORPG2D.Shared.Packets;

[MemoryPackable]
public partial class ReqAttack
{
    public byte Dir { get; set; }
}

[MemoryPackable]
public partial class NtfAttack
{
    public long ActorId { get; set; }
    public long TargetId { get; set; }   // 0 = no target hit
    public byte Dir { get; set; }
}

[MemoryPackable]
public partial class NtfHpChange
{
    public long Id { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
}

[MemoryPackable]
public partial class NtfDie
{
    public long Id { get; set; }
    public long KillerId { get; set; }
}

[MemoryPackable]
public partial class NtfReward
{
    public long Exp { get; set; }
    public long NewExpTotal { get; set; }
    public int NewLevel { get; set; }
    public bool LeveledUp { get; set; }
}

[MemoryPackable]
public partial class NtfRespawn
{
    public long Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
}
