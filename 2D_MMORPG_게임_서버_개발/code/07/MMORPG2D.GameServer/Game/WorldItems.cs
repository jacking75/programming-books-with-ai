using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Game;

public class WorldItemEntity
{
    public long Id { get; init; }
    public int DefId { get; init; }
    public int Qty { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public DateTime ExpireUtc { get; set; }
}

public class WorldItems
{
    public static WorldItems Instance { get; } = new();
    private long _nextId = 5_000_000_000;
    private readonly Dictionary<long, WorldItemEntity> _items = new();
    private readonly object _lock = new();
    private readonly Timer _expireTimer;

    private WorldItems()
    {
        _expireTimer = new Timer(_ => ExpireTick(), null, 1000, 1000);
    }

    public WorldItemEntity Spawn(int defId, int qty, int x, int y, TimeSpan ttl)
    {
        var w = new WorldItemEntity
        {
            Id = Interlocked.Increment(ref _nextId),
            DefId = defId, Qty = qty, X = x, Y = y,
            ExpireUtc = DateTime.UtcNow.Add(ttl),
        };
        lock (_lock) _items[w.Id] = w;
        BroadcastDrop(w);
        return w;
    }

    public WorldItemEntity? TryPickAtTile(int x, int y, out bool taken)
    {
        taken = false;
        lock (_lock)
        {
            foreach (var w in _items.Values)
            {
                if (w.X == x && w.Y == y)
                {
                    if (_items.Remove(w.Id))
                    { taken = true; return w; }
                }
            }
        }
        return null;
    }

    public List<WorldItemEntity> Near(int px, int py, int radius)
    {
        var r2 = (long)radius * radius;
        var list = new List<WorldItemEntity>();
        lock (_lock)
        {
            foreach (var w in _items.Values)
            {
                var dx = w.X - px; var dy = w.Y - py;
                if ((long)dx * dx + (long)dy * dy <= r2) list.Add(w);
            }
        }
        return list;
    }

    private void ExpireTick()
    {
        var now = DateTime.UtcNow;
        List<WorldItemEntity> expired;
        lock (_lock)
        {
            expired = _items.Values.Where(w => w.ExpireUtc <= now).ToList();
            foreach (var w in expired) _items.Remove(w.Id);
        }
        foreach (var w in expired)
            BroadcastPickup(w.Id, 0); // pickerId=0 means despawn
    }

    private static void BroadcastDrop(WorldItemEntity w)
    {
        var ntf = new NtfItemDrop
        {
            Item = new WorldItem { Id = w.Id, DefId = w.DefId, Qty = w.Qty, X = w.X, Y = w.Y },
        };
        foreach (var n in World.Default.Aoi.Neighbors(w.X, w.Y))
            n.Session.SendPacket(PacketId.NtfItemDrop, ntf);
    }

    public static void BroadcastPickup(long id, long pickerId)
    {
        var ntf = new NtfItemPickup { Id = id, PickerId = pickerId };
        // 모든 시야에 방송 — 학습용 단순화 (위치를 잃었으므로)
        foreach (var p in World.Default.All())
            p.Session.SendPacket(PacketId.NtfItemPickup, ntf);
    }
}
