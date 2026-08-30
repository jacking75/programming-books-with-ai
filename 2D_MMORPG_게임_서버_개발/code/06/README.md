# Week 6 - Monsters

Built on top of week 5.

## Schema patch

```bash
mysql -u mmorpg -p mmorpg2d < db/schema_week06.sql
```

## Added

- Shared: `Packets/MonsterPackets.cs`, monster ids in `PacketId.cs`.
- GameServer:
  - `Game/MonsterDef.cs`, `Game/MonsterActor.cs`, `Game/MonsterManager.cs`.
  - AttackHandler now hits monsters too.
  - EnterGameHandler sends `NtfMonsterList` to a freshly entered player.
  - Program.cs calls `MonsterManager.Instance.LoadAndStartAsync` at boot.
- Monster ids start at 1_000_000_000 to disambiguate from player char ids.

## Try

Walk near a spawn point. The slime should chase, attack, and die when you hit it back. After 10 seconds it respawns.
