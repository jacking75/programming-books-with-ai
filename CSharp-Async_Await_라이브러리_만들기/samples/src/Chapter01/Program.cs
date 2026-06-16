// 파일: samples/src/Chapter01/Program.cs
// 1장 — Task, ValueTask, 그리고 그 너머
//
// 본문에 등장한 각 절(section)을 차례로 실행한다.

using AsyncAwaitLab.Chapter01;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 01 — Task, ValueTask, and beyond");

await TaskAllocationProbe.RunAsync();
await ValueTaskDoubleAwait.RunAsync();

ConsoleHelpers.Log("Chapter 01 done.");
