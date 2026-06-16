// 파일: samples/src/Chapter10/Program.cs
// 10장 — ConfigureAwait / ExecutionContext / AsyncLocal

using AsyncAwaitLab.Common;

ConsoleHelpers.Banner("Chapter 10 — ConfigureAwait / ExecutionContext / AsyncLocal");

var reqId = new AsyncLocal<string?>();
reqId.Value = "req-001";
ConsoleHelpers.Log($"부모: reqId={reqId.Value}, ctx={AsyncDiagnostics.CurrentContext()}");

await Task.Run(async () =>
{
    ConsoleHelpers.Log($"자식(before): reqId={reqId.Value}");
    reqId.Value = "req-XXX";              // 자식에서 변경 → 부모에는 영향 없음
    ConsoleHelpers.Log($"자식(after) : reqId={reqId.Value}");
    await Task.Delay(50);
});

ConsoleHelpers.Log($"부모 복귀: reqId={reqId.Value}"); // 여전히 req-001
ConsoleHelpers.Log("Chapter 10 done.");
