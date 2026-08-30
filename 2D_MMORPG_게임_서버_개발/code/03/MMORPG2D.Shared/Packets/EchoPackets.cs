using MemoryPack;

namespace MMORPG2D.Shared.Packets;

[MemoryPackable]
public partial class ReqEcho
{
    public string Message { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class ResEcho
{
    public string Message { get; set; } = string.Empty;
    public long ServerTimeUtcMs { get; set; }
}
