using MemoryPack;
using MMORPG2D.GameServer.Game;
using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;

namespace MMORPG2D.GameServer.Handlers;

public static class AttackHandler
{
    private const int CooldownMs = 1000;
    private const int RespawnMs = 5000;
    private static readonly Random Rng = new();
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
        if (s.Player is not { } me || !me.IsAlive) return;

        var now = DateTime.UtcNow;
        if ((now - me.LastAttackUtc).TotalMilliseconds < CooldownMs) return;
        me.LastAttackUtc = now;

        var req = MemoryPackSerializer.Deserialize<ReqAttack>(body)!;
        var (tx, ty) = ForwardTile(me, req.Dir);

        PlayerCharacter? targetP = null;
        foreach (var p in World.Default.All())
            if (p.CharacterId != me.CharacterId && p.IsAlive && p.X == tx && p.Y == ty)
            { targetP = p; break; }

        MonsterActor? targetM = null;
        if (targetP == null)
            foreach (var m in MonsterManager.Instance.All)
                if (m.IsAlive && m.X == tx && m.Y == ty) { targetM = m; break; }

        long targetId = targetP?.CharacterId ?? targetM?.Id ?? 0;
        var ntfAtk = new NtfAttack { ActorId = me.CharacterId, TargetId = targetId, Dir = req.Dir };
        foreach (var n in World.Default.Aoi.Neighbors(me.X, me.Y))
            n.Session.SendPacket(PacketId.NtfAttack, ntfAtk);

        if (targetP != null)
        {
            var dmg = Damage.Calc(me, targetP);
            targetP.Hp = Math.Max(0, targetP.Hp - dmg);
            var ntfHp = new NtfHpChange { Id = targetP.CharacterId, Hp = targetP.Hp, MaxHp = targetP.MaxHp };
            foreach (var n in World.Default.Aoi.Neighbors(targetP.X, targetP.Y))
                n.Session.SendPacket(PacketId.NtfHpChange, ntfHp);
            if (targetP.Hp == 0) OnKillPlayer(me, targetP);
        }
        else if (targetM != null)
        {
            var dmg = Math.Max(1, me.Attack - targetM.Defense + Rng.Next(-2, 3));
            var wasAlive = targetM.IsAlive;
            MonsterManager.Instance.TakeDamage(targetM, me, dmg);
            // 죽었으면 아이템 드롭
            if (wasAlive && !targetM.IsAlive)
                ItemHandler.DropFromMonster(targetM, me);
        }
    }

    private static void OnKillPlayer(PlayerCharacter killer, PlayerCharacter victim)
    {
        var ntfDie = new NtfDie { Id = victim.CharacterId, KillerId = killer.CharacterId };
        foreach (var n in World.Default.Aoi.Neighbors(victim.X, victim.Y))
            n.Session.SendPacket(PacketId.NtfDie, ntfDie);

        var gain = Reward.ExpFor(killer, victim);
        killer.Exp += gain;
        var lv = Level.MaybeLevelUp(killer);
        killer.Session.SendPacket(PacketId.NtfReward, new NtfReward
        { Exp = gain, NewExpTotal = killer.Exp, NewLevel = killer.Level, LeveledUp = lv });

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
                    { Id = victim.CharacterId, X = victim.X, Y = victim.Y, Hp = victim.Hp, MaxHp = victim.MaxHp };
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
