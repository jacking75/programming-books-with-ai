// 파일: samples/src/Chapter16/Program.cs
// 16장 — Channel<T> based actors

using AsyncAwaitLab.Chapter16;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 16 — Channel<T> based actors");

await using var room = new RoomActor();

// 100명 동시 Join
var joins = Enumerable.Range(0, 100).Select(id => room.SendAsync(new Join(id)).AsTask());
await Task.WhenAll(joins);

// 50명 Leave
var leaves = Enumerable.Range(0, 50).Select(id => room.SendAsync(new Leave(id)).AsTask());
await Task.WhenAll(leaves);

int count = await room.GetPlayerCountAsync();
ConsoleHelpers.Log($"final player count = {count} (expect 50)");

ConsoleHelpers.Log("Chapter 16 done.");
