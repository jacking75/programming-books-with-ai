using MemoryPack;
using MMORPG2D.GameServer.Game;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Handlers;

public static class AttackHandler
{
    private const int CooldownMs = 1000;
    private const int RespawnMs = 5000;
    private static PacketDispatchWorker? _packetWorker;

    public static void Init(PacketDispatchWorker packetWorker)
    {
        _packetWorker = packetWorker;
    }

    public static void Register(PacketHandler h)
    {
        h.Register(PacketId.ReqAttack, OnReqAttack);
    }

    private static void OnReqAttack(GameSession s, byte[] body)
    {
        if (s.Player is not { } me) return;
        if (!me.IsAlive) return;

        var now = DateTime.UtcNow;
        if ((now - me.LastAttackUtc).TotalMilliseconds < CooldownMs) return;
        me.LastAttackUtc = now;

        var req = MemoryPackSerializer.Deserialize<ReqAttack>(body)!;
        var (tx, ty) = ForwardTile(me, req.Dir);

        PlayerCharacter? target = null;
        foreach (var p in World.Default.All())
        {
            if (p.CharacterId == me.CharacterId) continue;
            if (!p.IsAlive) continue;
            if (p.X == tx && p.Y == ty) { target = p; break; }
        }

        var ntfAtk = new NtfAttack
        {
            ActorId = me.CharacterId,
            TargetId = target?.CharacterId ?? 0,
            Dir = req.Dir,
        };
        foreach (var n in World.Default.Aoi.Neighbors(me.X, me.Y))
            n.Session.SendPacket(PacketId.NtfAttack, ntfAtk);

        if (target == null) return;

        var dmg = Damage.Calc(me, target);
        target.Hp = Math.Max(0, target.Hp - dmg);

        var ntfHp = new NtfHpChange
        {
            Id = target.CharacterId,
            Hp = target.Hp,
            MaxHp = target.MaxHp,
        };
        foreach (var n in World.Default.Aoi.Neighbors(target.X, target.Y))
            n.Session.SendPacket(PacketId.NtfHpChange, ntfHp);

        if (target.Hp == 0)
            OnKill(me, target);
    }

    private static void OnKill(PlayerCharacter killer, PlayerCharacter victim)
    {
        var ntfDie = new NtfDie { Id = victim.CharacterId, KillerId = killer.CharacterId };
        foreach (var n in World.Default.Aoi.Neighbors(victim.X, victim.Y))
            n.Session.SendPacket(PacketId.NtfDie, ntfDie);

        var gain = Reward.ExpFor(killer, victim);
        killer.Exp += gain;
        var lv = Level.MaybeLevelUp(killer);

        killer.Session.SendPacket(PacketId.NtfReward, new NtfReward
        {
            Exp = gain,
            NewExpTotal = killer.Exp,
            NewLevel = killer.Level,
            LeveledUp = lv,
        });

        Timer? timer = null;
        timer = new Timer(_ =>
        {
            try
            {
                var posted = _packetWorker?.Post(() =>
                {
                    victim.Hp = victim.MaxHp;
                    victim.X = 400; victim.Y = 300;
                    var ntf = new NtfRespawn
                    {
                        Id = victim.CharacterId,
                        X = victim.X, Y = victim.Y,
                        Hp = victim.Hp, MaxHp = victim.MaxHp,
                    };
                    foreach (var n in World.Default.Aoi.Neighbors(victim.X, victim.Y))
                        n.Session.SendPacket(PacketId.NtfRespawn, ntf);
                }) ?? false;
                if (!posted)
                {
                    Logger.Warn($"respawn skipped for {victim.Name}: packet worker is not available");
                }
            }
            finally
            {
                timer?.Dispose();
            }
        }, null, RespawnMs, Timeout.Infinite);
    }

    private static (int X, int Y) ForwardTile(PlayerCharacter me, byte dir)
    {
        int dx = 0, dy = 0;
        switch (dir)
        {
            case 0: dy = -World.CellSize; break;
            case 1: dx =  World.CellSize; break;
            case 2: dy =  World.CellSize; break;
            case 3: dx = -World.CellSize; break;
        }
        return (me.X + dx, me.Y + dy);
    }
}
