using MMORPG2D.Shared.Packets;
using MMORPG2D.GameServer.Database;

namespace MMORPG2D.GameServer.Game;

public enum ItemType : byte { Potion = 1, Weapon = 2, Armor = 3 }
public enum EffectKind : byte { HealHp = 1, RestoreMp = 2 }

public class ItemDef
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public ItemType Type { get; init; }
    public EffectKind EffectKind { get; init; }
    public int EffectAmt { get; init; }
    public int MaxStack { get; init; } = 1;
}

public static class ItemDefs
{
    private static readonly Dictionary<int, ItemDef> _defs = new();
    private static readonly Dictionary<int, List<DropEntry>> _drops = new();

    public static ItemDef? Get(int id) => _defs.GetValueOrDefault(id);
    public static List<DropEntry>? DropsForMonster(int monsterId) =>
        _drops.GetValueOrDefault(monsterId);

    public static void Load(ItemDefsLoadResult data)
    {
        if (data.ResultCode != 0)
        {
            Logger.Error($"item defs load failed. result={data.ResultCode}");
            return;
        }

        _defs.Clear();
        _drops.Clear();

        foreach (var r in data.Items)
        {
            _defs[r.Id] = new ItemDef
            {
                Id = r.Id,
                Name = r.Name,
                Type = (ItemType)r.Type,
                EffectKind = (EffectKind)r.EffectKind,
                EffectAmt = r.EffectAmt,
                MaxStack = r.MaxStack,
            };
        }

        foreach (var d in data.Drops)
        {
            var entry = new DropEntry
            {
                ItemDefId = d.ItemDefId,
                Chance = d.Chance,
                QtyMin = d.QtyMin,
                QtyMax = d.QtyMax,
            };
            if (!_drops.TryGetValue(d.MonsterId, out var list))
                _drops[d.MonsterId] = list = new();
            list.Add(entry);
        }

        Logger.Info($"loaded {_defs.Count} item defs, {_drops.Sum(kv => kv.Value.Count)} drop rows");
    }
}

public class DropEntry
{
    public int ItemDefId { get; init; }
    public int Chance { get; init; }
    public int QtyMin { get; init; }
    public int QtyMax { get; init; }
}
