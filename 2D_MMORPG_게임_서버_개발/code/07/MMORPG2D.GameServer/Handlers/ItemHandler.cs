using MemoryPack;
using MMORPG2D.GameServer.Game;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Handlers;

public static class ItemHandler
{
    public static void Register(PacketHandler h)
    {
        h.Register(PacketId.ReqUseItem, OnReqUseItem);
    }

    public static void DropFromMonster(MonsterActor m, PlayerCharacter killer)
    {
        var entries = ItemDefs.DropsForMonster(m.Def.Id);
        if (entries == null || entries.Count == 0) return;

        var rng = new Random();
        foreach (var e in entries)
        {
            if (rng.Next(0, 10000) >= e.Chance) continue;
            var qty = rng.Next(e.QtyMin, e.QtyMax + 1);
            WorldItems.Instance.Spawn(e.ItemDefId, qty, m.X, m.Y, TimeSpan.FromSeconds(30));
        }
    }

    public static void TryAutoPick(PlayerCharacter p)
    {
        var item = WorldItems.Instance.TryPickAtTile(p.X, p.Y, out var taken);
        if (!taken || item == null) return;

        var def = ItemDefs.Get(item.DefId);
        if (def == null) return;

        if (p.Inventory.TryAdd(item.DefId, item.Qty, def.MaxStack))
        {
            WorldItems.BroadcastPickup(item.Id, p.CharacterId);
            SendInventory(p);
        }
        else
        {
            // 인벤이 가득 - 다시 떨어뜨리기 (단순화)
            WorldItems.Instance.Spawn(item.DefId, item.Qty, item.X, item.Y, TimeSpan.FromSeconds(30));
        }
    }

    private static void OnReqUseItem(GameSession s, byte[] body)
    {
        if (s.Player is not { } me || !me.IsAlive) return;
        var req = MemoryPackSerializer.Deserialize<ReqUseItem>(body)!;

        if (!me.Inventory.TryGetSlot(req.SlotIdx, out var slot))
        {
            s.SendPacket(PacketId.NtfItemUseResult, new NtfItemUseResult { Ok = false, Reason = "empty" });
            return;
        }

        var def = ItemDefs.Get(slot.DefId);
        if (def == null)
        {
            s.SendPacket(PacketId.NtfItemUseResult, new NtfItemUseResult { Ok = false, Reason = "unknown" });
            return;
        }

        if (def.Type != ItemType.Potion)
        {
            s.SendPacket(PacketId.NtfItemUseResult, new NtfItemUseResult { Ok = false, Reason = "not_usable" });
            return;
        }

        if (def.EffectKind == EffectKind.HealHp)
        {
            if (me.Hp >= me.MaxHp)
            {
                s.SendPacket(PacketId.NtfItemUseResult, new NtfItemUseResult { Ok = false, Reason = "already_full" });
                return;
            }
            me.Hp = Math.Min(me.MaxHp, me.Hp + def.EffectAmt);

            // 본인+시야 안 모두에게 HP 알림
            var ntfHp = new NtfHpChange { Id = me.CharacterId, Hp = me.Hp, MaxHp = me.MaxHp };
            foreach (var n in World.Default.Aoi.Neighbors(me.X, me.Y))
                n.Session.SendPacket(PacketId.NtfHpChange, ntfHp);
        }

        slot.Qty--;
        if (slot.Qty == 0) me.Inventory.RemoveSlot(slot.SlotIdx);

        SendInventory(me);
        s.SendPacket(PacketId.NtfItemUseResult, new NtfItemUseResult { Ok = true });
    }

    public static void SendInventory(PlayerCharacter p)
    {
        p.Session.SendPacket(PacketId.NtfInventory, new NtfInventory
        {
            Slots = p.Inventory.ToSnapshot(),
        });
    }
}
