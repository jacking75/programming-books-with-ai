using MemoryPack;
using MMORPG2D.GameServer.Game;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Handlers;

public static class MoveHandler
{
    private const int MaxMovePacketsPerSecond = 5;
    private const int MoveRateWindowMs = 1000;

    public static void OnReqMove(GameSession s, byte[] body)
    {
        if (s.Player is not { } player) return;
        var req = MemoryPackSerializer.Deserialize<ReqMove>(body)!;

        int dx = 0, dy = 0;
        switch (req.Dir)
        {
            case 0: dy = -World.CellSize; break;
            case 1: dx =  World.CellSize; break;
            case 2: dy =  World.CellSize; break;
            case 3: dx = -World.CellSize; break;
            default: return;
        }

        var oldX = player.X; var oldY = player.Y;
        var nx = Math.Clamp(player.X + dx, 0, World.Default.Width  - World.CellSize);
        var ny = Math.Clamp(player.Y + dy, 0, World.Default.Height - World.CellSize);
        if (nx == oldX && ny == oldY) return;

        if (!CheckMoveRateLimit(s, player)) return;

        var oldCell = World.Default.Aoi.ToCell(oldX, oldY);
        var newCell = World.Default.Aoi.ToCell(nx, ny);

        player.X = nx; player.Y = ny;

        s.SendPacket(PacketId.ResMove, new ResMove
        { CharacterId = player.CharacterId, X = nx, Y = ny });

        if (oldCell != newCell)
        {
            World.Default.Aoi.UpdateCell(player, oldCell, newCell);
            Broadcaster.NotifyCellChange(player, oldCell, newCell);
        }

        Broadcaster.NotifyMove(player);

        // Week 7: auto-pick item on the tile we moved to
        ItemHandler.TryAutoPick(player);
    }

    private static bool CheckMoveRateLimit(GameSession s, PlayerCharacter player)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs - player.MoveRateWindowStartMs >= MoveRateWindowMs)
        {
            player.MoveRateWindowStartMs = nowMs;
            player.MoveRateWindowCount = 0;
        }

        player.MoveRateWindowCount++;
        if (player.MoveRateWindowCount <= MaxMovePacketsPerSecond)
        {
            return true;
        }

        Logger.Warn(
            $"illegal move rate: {player.Name}({player.CharacterId}) count={player.MoveRateWindowCount}/sec");
        s.Close();
        return false;
    }
}
