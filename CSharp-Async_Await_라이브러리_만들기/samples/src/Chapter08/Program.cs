// 파일: samples/src/Chapter08/Program.cs
// 8장 — HTTP & stream wrappers
// 외부 HTTP를 부르지 않고 in-memory MemoryStream 두 개로 progress copy를 시연한다.

using AsyncAwaitLab.Chapter08;
using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 08 — HTTP & stream wrappers");

var src = new MemoryStream(new byte[1024 * 256]); // 256KB
var dst = new MemoryStream();
var progress = new Progress<double>(p =>
    ConsoleHelpers.Log($"copy progress: {p:P0}"));
await ProgressCopy.CopyWithProgressAsync(src, dst, src.Length, progress, default);
ConsoleHelpers.Log($"copied bytes = {dst.Length:N0}");

ConsoleHelpers.Log("Chapter 08 done.");
