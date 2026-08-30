using MemoryPack;

namespace MMORPG2D.Shared.Packets;

[MemoryPackable]
public partial class PlayerSnapshot
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Level { get; set; }
}

[MemoryPackable]
public partial class NtfPlayerList
{
    public PlayerSnapshot[] Players { get; set; } = Array.Empty<PlayerSnapshot>();
}

[MemoryPackable]
public partial class NtfPlayerEnter
{
    public PlayerSnapshot Player { get; set; } = new();
}

[MemoryPackable]
public partial class NtfPlayerLeave
{
    public long Id { get; set; }
}

[MemoryPackable]
public partial class NtfPlayerMove
{
    public long Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
