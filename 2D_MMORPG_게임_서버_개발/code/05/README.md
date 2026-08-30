# Week 5 - Combat

Built on top of week 4.

## Schema patch

```bash
mysql -u mmorpg -p mmorpg2d < db/schema_week05.sql
```

## Added

- Shared: `Packets/CombatPackets.cs`, new packet ids in `PacketId.cs`.
- GameServer: `Game/Combat.cs` (Damage / Level / Reward), `Handlers/AttackHandler.cs`.
- PlayerCharacter now carries Hp/MaxHp/Attack/Defense and LastAttackUtc.
- EnterGameHandler reads new columns. GameServer.SavePlayer persists them.
- Program.cs registers AttackHandler.

## Try

In two clients face each other (one tile apart) and press SPACE to attack.
