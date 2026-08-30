# Week 4 - Multiplayer sync

Built on top of week 3.

## Added

- Shared: `Packets/AoiPackets.cs` (PlayerSnapshot, NtfPlayerEnter/Leave/Move, NtfPlayerList).
- GameServer: `Game/AoiGrid.cs` (128px grid), `Game/Broadcaster.cs` (AOI fan-out helpers), `Game/PlayerCharacterExtensions.cs` (ToSnapshot).
- World now owns an `AoiGrid` and updates it on Add/Remove. `AoiGrid` is lock-free because it is only called from the packet worker thread.
- EnterGameHandler now calls `Broadcaster.NotifyEnter` after world add.
- MoveHandler now enforces 5 successful moves per second, calls `NotifyMove`, and calls `NotifyCellChange` on cell boundary.
- Client throttles `ReqMove` sends to 5 packets per second.
- GameServer.OnSessionClosed sends `NotifyLeave` before removing.
- Client tracks `others` dictionary, prints sight changes.

## Test

Run two `MMORPG2D.Client` instances with different accounts. Walk one near the other and watch the other's console print `[+] ... entered sight`.
