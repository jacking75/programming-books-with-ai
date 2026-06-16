// 파일: samples/src/Chapter09/Program.cs
// 9장 — File I/O and DB connection pool

using AsyncAwaitLab.Chapter09;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 09 — File I/O and DB connection pool");

await using var pool = new ConnectionPool<FakeConnection>(
    maxSize: 3,
    factory: _ => Task.FromResult(new FakeConnection()));

var tasks = Enumerable.Range(0, 6).Select(async i =>
{
    await using var lease = await pool.RentAsync(default);
    ConsoleHelpers.Log($"task#{i} rented {lease.Connection.Id.ToString().Substring(0,8)}");
    await Task.Delay(50);
});

await Task.WhenAll(tasks);
ConsoleHelpers.Log("Chapter 09 done.");
