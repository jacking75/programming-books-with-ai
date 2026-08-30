using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Game;

public class Inventory
{
    public const int Slots = 10;

    public class Slot
    {
        public int SlotIdx { get; set; }
        public int DefId { get; set; }
        public int Qty { get; set; }
    }

    private readonly Dictionary<int, Slot> _bySlot = new();

    public IReadOnlyDictionary<int, Slot> All => _bySlot;

    public bool TryAdd(int defId, int qty, int maxStack)
    {
        // try to merge into existing stack
        foreach (var s in _bySlot.Values)
        {
            if (s.DefId != defId || s.Qty >= maxStack) continue;
            var add = Math.Min(qty, maxStack - s.Qty);
            s.Qty += add; qty -= add;
            if (qty == 0) return true;
        }
        // find empty slot
        for (int i = 0; i < Slots; i++)
        {
            if (_bySlot.ContainsKey(i)) continue;
            var put = Math.Min(qty, maxStack);
            _bySlot[i] = new Slot { SlotIdx = i, DefId = defId, Qty = put };
            qty -= put;
            if (qty == 0) return true;
        }
        return false;
    }

    public bool TryGetSlot(int idx, out Slot slot)
        => _bySlot.TryGetValue(idx, out slot!);

    public void RemoveSlot(int idx) => _bySlot.Remove(idx);

    public void SetLoadedSlot(int slotIdx, int defId, int qty)
    {
        _bySlot[slotIdx] = new Slot
        {
            SlotIdx = slotIdx,
            DefId = defId,
            Qty = qty,
        };
    }

    public InventorySlot[] ToSnapshot()
        => _bySlot.Values.Select(s => new InventorySlot
        { SlotIdx = s.SlotIdx, DefId = s.DefId, Qty = s.Qty }).ToArray();
}
