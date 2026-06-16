// 파일: samples/src/Chapter04/Program.cs
// 4장 — 첫 커스텀 Awaitable

using AsyncAwaitLab.Chapter04;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 04 — Your first custom awaitable");
await Demo.RunAsync();
ConsoleHelpers.Log("Chapter 04 done.");
