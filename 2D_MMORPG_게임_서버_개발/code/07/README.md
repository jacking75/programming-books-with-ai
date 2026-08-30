# Week 7 - Items

Built on top of week 6.

## Schema patch

```bash
mysql -u mmorpg -p mmorpg2d < db/schema_week07.sql
```

## Added

- Shared: `Packets/ItemPackets.cs`, item ids in `PacketId.cs`.
- GameServer:
  - `Game/Items.cs`, `Game/WorldItems.cs`, `Game/Inventory.cs`.
  - PlayerCharacter has `Inventory`.
  - `Handlers/ItemHandler.cs` (auto-pick, drop, use).
  - AttackHandler triggers `DropFromMonster` on monster kill.
  - MoveHandler calls `TryAutoPick` after move.
  - EnterGameHandler loads inventory and sends `NtfInventory` + nearby `NtfItemList`.
  - GameServer.OnSessionClosed saves inventory.
  - Program.cs loads ItemDefs and registers ItemHandler.

## Try

Kill a slime; about half the time it drops a "Small Potion" on its tile.
Walk onto the tile to auto-pick. Send `ReqUseItem { SlotIdx = 0 }` to heal.
