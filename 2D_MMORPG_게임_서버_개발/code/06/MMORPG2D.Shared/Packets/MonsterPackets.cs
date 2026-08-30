using MemoryPack;

namespace MMORPG2D.Shared.Packets;

[MemoryPackable]
public partial class MonsterSnapshot
{
    public long Id { get; set; }
    public int DefId { get; set; }
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; }
    public int MaxHp { get; set; }
}

[MemoryPackable]
public partial class NtfMonsterList
{
    public MonsterSnapshot[] Monsters { get; set; } = Array.Empty<MonsterSnapshot>();
}

[MemoryPackable]
public partial class NtfMonsterEnter
{
    public MonsterSnapshot Monster { get; set; } = new();
}

[MemoryPackable]
public partial class NtfMonsterLeave
{
    public long Id { get; set; }
}

[MemoryPackable]
public partial class NtfMonsterMove
{
    public long Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
