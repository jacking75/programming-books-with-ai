using MemoryPack;

namespace MMORPG2D.Shared.Packets;

[MemoryPackable]
public partial class WorldItem
{
    public long Id { get; set; }
    public int DefId { get; set; }
    public int Qty { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

[MemoryPackable]
public partial class NtfItemDrop
{
    public WorldItem Item { get; set; } = new();
}

[MemoryPackable]
public partial class NtfItemPickup
{
    public long Id { get; set; }
    public long PickerId { get; set; }
}

[MemoryPackable]
public partial class NtfItemList
{
    public WorldItem[] Items { get; set; } = Array.Empty<WorldItem>();
}

[MemoryPackable]
public partial class InventorySlot
{
    public int SlotIdx { get; set; }
    public int DefId { get; set; }
    public int Qty { get; set; }
}

[MemoryPackable]
public partial class NtfInventory
{
    public InventorySlot[] Slots { get; set; } = Array.Empty<InventorySlot>();
}

[MemoryPackable]
public partial class ReqUseItem
{
    public int SlotIdx { get; set; }
}

[MemoryPackable]
public partial class NtfItemUseResult
{
    public bool Ok { get; set; }
    public string Reason { get; set; } = "";
}
