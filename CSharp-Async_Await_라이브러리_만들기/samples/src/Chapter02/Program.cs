// 파일: samples/src/Chapter02/Program.cs
// 2장 — 컴파일러가 만드는 상태 머신 해부

using AsyncAwaitLab.Chapter02;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 02 — The state machine the compiler builds");
await MoveNextCount.RunAsync();
ConsoleHelpers.Log("Chapter 02 done.");
