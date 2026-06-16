// 파일: samples/src/Chapter03/Program.cs
// 3장 — Awaitable / Awaiter 패턴의 설계 철학

using AsyncAwaitLab.Chapter03;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 03 — Awaitable / Awaiter pattern design");
await TimeSpanAwaiter.RunAsync();
await CustomYieldDemo.RunAsync();
ConsoleHelpers.Log("Chapter 03 done.");
