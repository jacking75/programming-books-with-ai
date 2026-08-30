using MMORPG2D.Shared;
using MMORPG2D.Shared.Packets;
using MMORPG2D.GameServer.Database;

namespace MMORPG2D.GameServer.Game;

public class MonsterManager
{
    public static MonsterManager Instance { get; } = new();
    public PacketDispatchWorker? PacketWorker { get; set; }

    // Monster state is read and written only on PacketDispatchWorker.
    // The timer only posts small AI batches to that worker.
    private const int AiTickIntervalMs = 100;
    private const int AiFullScanMs = 1000;
    private const int AiTicksPerFullScan = AiFullScanMs / AiTickIntervalMs;

    private readonly Dictionary<int, MonsterDef> _defs = new();
    private readonly List<MonsterActor> _all = new();
    private long _nextId = 1_000_000_000;
    private int _nextAiIndex;
    private Timer? _aiTimer;
    private Timer? _respawnTimer;

    private record RespawnEntry(MonsterActor Monster, DateTime AtUtc);
    private readonly List<RespawnEntry> _respawnQueue = new();

    [ThreadStatic] private static Random? _rng;
    private static Random Rng => _rng ??= new Random();

    public IReadOnlyList<MonsterActor> All => _all;

    public void LoadAndStart(MonsterLoadResult data)
    {
        if (data.ResultCode != 0)
        {
            Logger.Error($"monster load failed. result={data.ResultCode}");
            return;
        }

        foreach (var d in data.Monsters)
        {
            var def = new MonsterDef
            {
                Id = d.Id,
                Name = d.Name,
                MaxHp = d.MaxHp,
                Attack = d.Attack,
                Defense = d.Defense,
                ExpReward = d.ExpReward,
                MoveSpeed = d.MoveSpeed,
                AggroRange = d.AggroRange,
                RespawnSec = d.RespawnSec,
            };
            _defs[def.Id] = def;
        }

        foreach (var s in data.Spawns)
        {
            if (!_defs.TryGetValue(s.MonsterId, out var def)) continue;
            var m = new MonsterActor
            {
                Id = Interlocked.Increment(ref _nextId),
                Def = def,
                SpawnX = s.X, SpawnY = s.Y,
                X = s.X, Y = s.Y,
                Hp = def.MaxHp,
            };
            _all.Add(m);
        }

        Logger.Info($"loaded {_defs.Count} monster defs, {_all.Count} spawns");

        _aiTimer = new Timer(_ => PostTick(AiTick), null, AiTickIntervalMs, AiTickIntervalMs);
        _respawnTimer = new Timer(_ => PostTick(RespawnTick), null, 500, 500);
    }

    private static void SafeTick(Action a)
    { try { a(); } catch (Exception ex) { Logger.Error("monster tick", ex); } }

    private void PostTick(Action tick)
    {
        var posted = PacketWorker?.Post(() => SafeTick(tick)) ?? false;
        if (!posted)
        {
            Logger.Warn("monster tick skipped: packet worker is not available");
        }
    }

    public MonsterActor? Get(long id)
    {
        return _all.FirstOrDefault(m => m.Id == id);
    }

    private void AiTick()
    {
        var count = _all.Count;
        if (count == 0) return;

        var batchSize = Math.Max(1, (count + AiTicksPerFullScan - 1) / AiTicksPerFullScan);
        for (int i = 0; i < batchSize; i++)
        {
            if (_nextAiIndex >= count) _nextAiIndex = 0;
            var m = _all[_nextAiIndex++];
            if (!m.IsAlive) continue;

            switch (m.State)
            {
                case AiState.Idle:    TickIdle(m);    break;
                case AiState.Wander:  TickWander(m);  break;
                case AiState.Chase:   TickChase(m);   break;
                case AiState.Attack:  TickAttack(m);  break;
            }
        }
    }

    private void TickIdle(MonsterActor m)
    {
        var prey = FindPrey(m);
        if (prey != null) { m.State = AiState.Chase; m.TargetId = prey.CharacterId; return; }
        if (Rng.Next(0, 4) == 0)
        {
            m.State = AiState.Wander;
            m.WanderRemaining = Rng.Next(2, 6);
        }
    }

    private void TickWander(MonsterActor m)
    {
        var prey = FindPrey(m);
        if (prey != null) { m.State = AiState.Chase; m.TargetId = prey.CharacterId; return; }
        if (m.WanderRemaining-- <= 0) { m.State = AiState.Idle; return; }
        int dir = Rng.Next(0, 4);
        int dx = 0, dy = 0;
        switch (dir)
        {
            case 0: dy = -World.CellSize; break;
            case 1: dx =  World.CellSize; break;
            case 2: dy =  World.CellSize; break;
            case 3: dx = -World.CellSize; break;
        }
        var nx = Math.Clamp(m.X + dx, 0, World.Default.Width  - World.CellSize);
        var ny = Math.Clamp(m.Y + dy, 0, World.Default.Height - World.CellSize);
        if (nx != m.X || ny != m.Y)
        {
            m.X = nx; m.Y = ny;
            BroadcastMove(m);
        }
    }

    private void TickChase(MonsterActor m)
    {
        var target = World.Default.Get(m.TargetId);
        if (target == null || !target.IsAlive) { m.State = AiState.Idle; m.TargetId = 0; return; }

        var dx = target.X - m.X; var dy = target.Y - m.Y;
        var distSq = (long)dx * dx + (long)dy * dy;
        if (distSq > (long)m.AggroRange * m.AggroRange * 2)
        { m.State = AiState.Idle; m.TargetId = 0; return; }

        if (distSq <= (long)World.CellSize * World.CellSize * 2)
        { m.State = AiState.Attack; return; }

        int sx = Math.Sign(dx), sy = Math.Sign(dy);
        var nx = Math.Clamp(m.X + sx * World.CellSize, 0, World.Default.Width  - World.CellSize);
        var ny = Math.Clamp(m.Y + sy * World.CellSize, 0, World.Default.Height - World.CellSize);
        if (nx != m.X || ny != m.Y)
        {
            m.X = nx; m.Y = ny;
            BroadcastMove(m);
        }
    }

    private void TickAttack(MonsterActor m)
    {
        var target = World.Default.Get(m.TargetId);
        if (target == null || !target.IsAlive)
        { m.State = AiState.Idle; m.TargetId = 0; return; }

        var dx = target.X - m.X; var dy = target.Y - m.Y;
        var distSq = (long)dx * dx + (long)dy * dy;
        if (distSq > (long)World.CellSize * World.CellSize * 2)
        { m.State = AiState.Chase; return; }

        if ((DateTime.UtcNow - m.LastAttackUtc).TotalMilliseconds < 1500) return;
        m.LastAttackUtc = DateTime.UtcNow;

        var dmg = MonsterDamage(m, target);
        target.Hp = Math.Max(0, target.Hp - dmg);

        var ntfAtk = new NtfAttack { ActorId = m.Id, TargetId = target.CharacterId, Dir = 0 };
        var ntfHp = new NtfHpChange { Id = target.CharacterId, Hp = target.Hp, MaxHp = target.MaxHp };
        foreach (var n in World.Default.Aoi.Neighbors(target.X, target.Y))
        {
            n.Session.SendPacket(PacketId.NtfAttack, ntfAtk);
            n.Session.SendPacket(PacketId.NtfHpChange, ntfHp);
        }

        if (target.Hp == 0)
        {
            var ntfDie = new NtfDie { Id = target.CharacterId, KillerId = m.Id };
            foreach (var n in World.Default.Aoi.Neighbors(target.X, target.Y))
                n.Session.SendPacket(PacketId.NtfDie, ntfDie);
        }
    }

    private static int MonsterDamage(MonsterActor m, PlayerCharacter target)
    {
        var raw = m.Attack - target.Defense;
        if (raw < 1) raw = 1;
        var spread = Math.Max(1, raw / 5);
        var dmg = raw + Rng.Next(-spread, spread + 1);
        return Math.Max(1, dmg);
    }

    private PlayerCharacter? FindPrey(MonsterActor m)
    {
        PlayerCharacter? best = null; long bestDist = long.MaxValue;
        foreach (var p in World.Default.All())
        {
            if (!p.IsAlive) continue;
            var dx = p.X - m.X; var dy = p.Y - m.Y;
            var dist2 = (long)dx * dx + (long)dy * dy;
            if (dist2 <= (long)m.AggroRange * m.AggroRange && dist2 < bestDist)
            { best = p; bestDist = dist2; }
        }
        return best;
    }

    public void TakeDamage(MonsterActor m, PlayerCharacter killer, int dmg)
    {
        m.Hp = Math.Max(0, m.Hp - dmg);
        var ntfHp = new NtfHpChange { Id = m.Id, Hp = m.Hp, MaxHp = m.MaxHp };
        foreach (var n in World.Default.Aoi.Neighbors(m.X, m.Y))
            n.Session.SendPacket(PacketId.NtfHpChange, ntfHp);

        if (m.Hp == 0)
            OnKilled(m, killer);
        else if (m.State == AiState.Idle || m.State == AiState.Wander)
        { m.State = AiState.Chase; m.TargetId = killer.CharacterId; }
    }

    private void OnKilled(MonsterActor m, PlayerCharacter killer)
    {
        var ntfDie = new NtfDie { Id = m.Id, KillerId = killer.CharacterId };
        foreach (var n in World.Default.Aoi.Neighbors(m.X, m.Y))
            n.Session.SendPacket(PacketId.NtfDie, ntfDie);

        killer.Exp += m.Def.ExpReward;
        var lv = Level.MaybeLevelUp(killer);
        killer.Session.SendPacket(PacketId.NtfReward, new NtfReward
        {
            Exp = m.Def.ExpReward, NewExpTotal = killer.Exp,
            NewLevel = killer.Level, LeveledUp = lv,
        });

        m.State = AiState.Dead;
        _respawnQueue.Add(new RespawnEntry(m, DateTime.UtcNow.AddSeconds(m.RespawnSec)));
    }

    private void RespawnTick()
    {
        var now = DateTime.UtcNow;
        var ready = _respawnQueue.Where(e => e.AtUtc <= now).Select(e => e.Monster).ToList();
        _respawnQueue.RemoveAll(e => e.AtUtc <= now);
        foreach (var m in ready)
        {
            m.X = m.SpawnX; m.Y = m.SpawnY;
            m.Hp = m.MaxHp;
            m.State = AiState.Idle;
            m.TargetId = 0;
            BroadcastEnter(m);
        }
    }

    public void BroadcastMove(MonsterActor m)
    {
        var ntf = new NtfMonsterMove { Id = m.Id, X = m.X, Y = m.Y };
        foreach (var n in World.Default.Aoi.Neighbors(m.X, m.Y))
            n.Session.SendPacket(PacketId.NtfMonsterMove, ntf);
    }

    public void BroadcastEnter(MonsterActor m)
    {
        var snap = new MonsterSnapshot
        { Id = m.Id, DefId = m.Def.Id, Name = m.Name,
          X = m.X, Y = m.Y, Hp = m.Hp, MaxHp = m.MaxHp };
        foreach (var n in World.Default.Aoi.Neighbors(m.X, m.Y))
            n.Session.SendPacket(PacketId.NtfMonsterEnter, new NtfMonsterEnter { Monster = snap });
    }

    public NtfMonsterList SnapshotForPlayer(int px, int py)
    {
        var list = new List<MonsterSnapshot>();
        foreach (var m in _all)
        {
            if (!m.IsAlive) continue;
            var dx = m.X - px; var dy = m.Y - py;
            if ((long)dx * dx + (long)dy * dy > 256L * 256L) continue;
            list.Add(new MonsterSnapshot
            {
                Id = m.Id, DefId = m.Def.Id, Name = m.Name,
                X = m.X, Y = m.Y, Hp = m.Hp, MaxHp = m.MaxHp,
            });
        }
        return new NtfMonsterList { Monsters = list.ToArray() };
    }
}
