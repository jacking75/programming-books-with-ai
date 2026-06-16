// 파일: samples/src/Chapter17/Program.cs
// 17장 — Game-server room/session patterns

using AsyncAwaitLab.Chapter17;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 17 — Game-server room/session patterns");

await using var room = new TickingRoom(TimeSpan.FromMilliseconds(33)); // ~30Hz
for (int i = 0; i < 5; i++) await room.SendAsync(new Join(i));
await Task.Delay(500);

var snap = await room.SnapshotAsync();
ConsoleHelpers.Log($"snapshot: players={snap.PlayerCount}, ticks={snap.Ticks} (~30Hz × 0.5s ≈ 15)");

ConsoleHelpers.Log("Chapter 17 done.");
