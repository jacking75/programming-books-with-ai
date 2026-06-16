// 파일: samples/src/Chapter19/Program.cs
// 19장 — Memory tracking & async leak hunting

using AsyncAwaitLab.Chapter19;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 19 — Memory tracking & async leak hunting");

using var global = new CancellationTokenSource();
var demo = new LeakDemo(global);

var before = AsyncDiagnostics.Snapshot();
for (int i = 0; i < 1000; i++) demo.Leak();
var after = AsyncDiagnostics.Snapshot();
ConsoleHelpers.Log($"1000 leaks: regCount={demo.RegistrationCount}, {AsyncDiagnostics.Diff(before, after)}");

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
ConsoleHelpers.Log($"after GC: total {GC.GetTotalMemory(forceFullCollection: true):N0} B");

ConsoleHelpers.Log("Chapter 19 done.");
