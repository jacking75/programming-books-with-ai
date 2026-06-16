// 파일: samples/src/Chapter05/Program.cs
// 5장 — DelayAsync 재발명

using AsyncAwaitLab.Chapter05;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 05 — Re-inventing DelayAsync");
ConsoleHelpers.Log("NaiveDelay → start");
await new NaiveDelay(TimeSpan.FromMilliseconds(100));
ConsoleHelpers.Log("NaiveDelay → after 100ms");

ConsoleHelpers.Log("Delay (safer) → start");
await new Delay(TimeSpan.FromMilliseconds(100));
ConsoleHelpers.Log("Delay → after 100ms");

await Step3_Compare.RunAsync();
ConsoleHelpers.Log("Chapter 05 done.");
