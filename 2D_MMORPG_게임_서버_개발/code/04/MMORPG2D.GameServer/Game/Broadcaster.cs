using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Game;

/// <summary>
/// AOI 기반 브로드캐스트 헬퍼. 본인을 제외한 시야 안 사용자에게 패킷을 보낸다.
/// </summary>
public static class Broadcaster
{
    public static void NotifyEnter(PlayerCharacter me)
    {
        // 시야 안의 다른 플레이어 → 나에게 NtfPlayerList
        var neighbors = World.Default.Aoi.Neighbors(me.X, me.Y);
        var snapshots = neighbors
            .Where(o => o.CharacterId != me.CharacterId)
            .Select(o => o.ToSnapshot())
            .ToArray();
        me.Session.SendPacket(PacketId.NtfPlayerList,
            new NtfPlayerList { Players = snapshots });

        // 시야 안의 다른 플레이어 ← NtfPlayerEnter (나)
        var enter = new NtfPlayerEnter { Player = me.ToSnapshot() };
        foreach (var other in neighbors)
            if (other.CharacterId != me.CharacterId)
                other.Session.SendPacket(PacketId.NtfPlayerEnter, enter);
    }

    public static void NotifyLeave(PlayerCharacter me)
    {
        var neighbors = World.Default.Aoi.Neighbors(me.X, me.Y);
        var leave = new NtfPlayerLeave { Id = me.CharacterId };
        foreach (var other in neighbors)
            if (other.CharacterId != me.CharacterId)
                other.Session.SendPacket(PacketId.NtfPlayerLeave, leave);
    }

    public static void NotifyMove(PlayerCharacter me)
    {
        var neighbors = World.Default.Aoi.Neighbors(me.X, me.Y);
        var ntf = new NtfPlayerMove { Id = me.CharacterId, X = me.X, Y = me.Y };
        foreach (var other in neighbors)
            if (other.CharacterId != me.CharacterId)
                other.Session.SendPacket(PacketId.NtfPlayerMove, ntf);
    }

    public static void NotifyCellChange(PlayerCharacter me, CellId oldCell, CellId newCell)
    {
        if (oldCell == newCell) return;

        // 옛 시야 (이전 위치 기준) ↔ 새 시야 (새 위치 기준)
        // 학습용 단순 구현: 두 셀 인접의 합집합에서 분리한다.
        var oldList = NeighborsOfCell(oldCell);
        var newList = NeighborsOfCell(newCell);

        var oldIds = oldList.Select(p => p.CharacterId).ToHashSet();
        var newIds = newList.Select(p => p.CharacterId).ToHashSet();

        // 시야에서 사라진 사람들 — 양쪽에 NtfPlayerLeave
        foreach (var other in oldList)
        {
            if (other.CharacterId == me.CharacterId) continue;
            if (newIds.Contains(other.CharacterId)) continue;
            other.Session.SendPacket(PacketId.NtfPlayerLeave,
                new NtfPlayerLeave { Id = me.CharacterId });
            me.Session.SendPacket(PacketId.NtfPlayerLeave,
                new NtfPlayerLeave { Id = other.CharacterId });
        }
        // 새로 보이는 사람들 — 양쪽에 NtfPlayerEnter
        foreach (var other in newList)
        {
            if (other.CharacterId == me.CharacterId) continue;
            if (oldIds.Contains(other.CharacterId)) continue;
            other.Session.SendPacket(PacketId.NtfPlayerEnter,
                new NtfPlayerEnter { Player = me.ToSnapshot() });
            me.Session.SendPacket(PacketId.NtfPlayerEnter,
                new NtfPlayerEnter { Player = other.ToSnapshot() });
        }
    }

    private static List<PlayerCharacter> NeighborsOfCell(CellId c)
    {
        // (cx,cy) 셀의 중심 픽셀로 Neighbors 호출
        var cellSize = World.AoiCellSize;
        var cx = c.X * cellSize + cellSize / 2;
        var cy = c.Y * cellSize + cellSize / 2;
        return World.Default.Aoi.Neighbors(cx, cy);
    }
}
