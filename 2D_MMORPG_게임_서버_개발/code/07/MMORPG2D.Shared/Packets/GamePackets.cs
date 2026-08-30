using MemoryPack;

namespace MMORPG2D.Shared.Packets;

[MemoryPackable]
public partial class ReqEnterGame
{
    public string Token { get; set; } = "";
    public long CharacterId { get; set; }
}

[MemoryPackable]
public partial class ResEnterGame
{
    /// <summary>0 = ok. 1 = invalid token. 2 = invalid character. 3 = world full.</summary>
    public int ResultCode { get; set; }
    public long CharacterId { get; set; }
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
}

[MemoryPackable]
public partial class NtfLeaveGame
{
    public int Reason { get; set; }
}

[MemoryPackable]
public partial class ReqMove
{
    /// <summary>0=N 1=E 2=S 3=W</summary>
    public byte Dir { get; set; }
}

[MemoryPackable]
public partial class ResMove
{
    public long CharacterId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}
